using System.Text.Json;

namespace PubSub.Core.ApplicationServices.Notifier
{
    public interface ISubscriberNotifierService
    {
        Task Notify(JsonElement payload, string recipient);
    }
}
