using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core.Abstractions.Types;

public class HttpKitosEventPublisher
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly string _publishUrl;

    public HttpKitosEventPublisher(HttpClient client, string baseUrl, string publishUrl)
    {
        _client = client;
        _baseUrl = baseUrl;
        _publishUrl = publishUrl;
    }

    public async Task<Maybe<OperationError>> PublishEvent(string payload)
    {
        var payloadObject = new { Topic = eventSomething.Topic, Message = eventSomething.EventBody };
        var payloadJson = JsonConvert.SerializeObject(payloadObject);
        _client.BaseAddress = new Uri(_baseUrl);
        var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(_publishUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            return new OperationError(OperationFailure.UnknownError);
        }
        return Maybe<OperationError>.None;
    }
}