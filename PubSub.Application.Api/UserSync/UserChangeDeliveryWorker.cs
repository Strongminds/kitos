using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Application.Api.UserSync;

public class UserChangeDeliveryWorker(IServiceScopeFactory scopes, IHttpClientFactory clients,
    IConfiguration config, ILogger<UserChangeDeliveryWorker> logger) : BackgroundService
{
    private readonly KitosTokenProvider _tokens = new(clients, config);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue<bool>("UserSync:Enabled")) return;
        var endpoint = config["UserSync:KitosEndpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new InvalidOperationException("UserSync requires an HTTPS KitosEndpoint.");
        _tokens.ValidateConfiguration();

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DeliverBatch(uri, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "User sync delivery batch failed; persisted messages will be retried."); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public async Task DeliverBatch(Uri endpoint, CancellationToken cancellationToken)
    {
        if (endpoint.Scheme != "https") throw new InvalidOperationException("UserSync requires HTTPS.");
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PubSubContext>();
        var now = DateTime.UtcNow;
        var deliveries = await db.UserChangeDeliveries.Where(x => x.DeliveredAt == null && !x.DeadLettered && x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt).Take(25).ToListAsync(cancellationToken);
        using var client = clients.CreateClient("UserSync");
        foreach (var delivery in deliveries)
        {
            // A persisted lease plus optimistic concurrency allows multiple PubSub instances.
            delivery.Version = Guid.NewGuid();
            delivery.NextAttemptAt = DateTime.UtcNow.AddMinutes(2);
            delivery.Attempts++;
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { db.Entry(delivery).State = EntityState.Detached; continue; }
            try
            {
                // Login and the one-time 401 retry must also finish within the two-minute lease.
                using var deliveryTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deliveryTimeout.CancelAfter(TimeSpan.FromSeconds(90));
                using var response = await SendDelivery(client, endpoint, delivery.Payload, deliveryTimeout.Token);
                delivery.LastStatusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode) delivery.DeliveredAt = DateTime.UtcNow;
                else if (IsPermanentFailure(response.StatusCode)) delivery.DeadLettered = true;
            }
            catch (HttpRequestException ex) { delivery.LastStatusCode = (int?)ex.StatusCode; }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { delivery.LastStatusCode = null; }
            delivery.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(8, delivery.Attempts))));
            delivery.Version = Guid.NewGuid();
            await db.SaveChangesAsync(cancellationToken);
            if (delivery.DeliveredAt == null)
                logger.LogWarning("User change delivery {DeliveryId}: attempt {Attempt}, status {Status}, dead letter {DeadLetter}",
                    delivery.Uuid, delivery.Attempts, delivery.LastStatusCode, delivery.DeadLettered);
        }
    }

    private async Task<HttpResponseMessage> SendDelivery(HttpClient client, Uri endpoint, string payload, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var token = await _tokens.GetToken(endpoint, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
            _tokens.Invalidate();
            if (attempt == 1) return response;
            response.Dispose();
        }
    }

    public static bool IsPermanentFailure(HttpStatusCode status) =>
        (int)status is >= 400 and < 500 && status is not (HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
}
