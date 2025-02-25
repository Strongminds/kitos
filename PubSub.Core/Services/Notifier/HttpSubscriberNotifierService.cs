using System.Text;

namespace PubSub.Core.Services.Notifier
{
    public class HttpSubscriberNotifierService : ISubscriberNotifierService
    {
        public async Task Notify(string message, string recipient)
        {
            var httpClient = new HttpClient();
            var content = new StringContent($"\"{message}\"", Encoding.UTF8, "application/json");
            await httpClient.PostAsync(recipient, content);
        }
    }
}
