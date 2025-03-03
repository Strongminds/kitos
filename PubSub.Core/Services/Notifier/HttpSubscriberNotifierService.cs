using System.Text;

namespace PubSub.Core.Services.Notifier
{
    public class HttpSubscriberNotifierService : ISubscriberNotifierService
    {
        private IHttpClientFactory _httpClientFactory;

        public HttpSubscriberNotifierService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task Notify(string message, string recipient)
        {
            Console.WriteLine("at start of Notify()");
            using var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");

            try
            {
                var debug = await httpClient.PostAsync(recipient, content);
                Console.WriteLine($"response for callback to {recipient} is {debug.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending POST request to {recipient}: {ex.Message}");
            }
        }

    }
}
