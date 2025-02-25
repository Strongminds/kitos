using System.Text;

namespace PubSub.Application.Subscribe
{
    public class HttpSubscriberNotifier : ISubscriberNotifier
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpSubscriberNotifier(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task Notify(string message, string recipient)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");
            await httpClient.PostAsync(recipient, content);
        }
    }
}
