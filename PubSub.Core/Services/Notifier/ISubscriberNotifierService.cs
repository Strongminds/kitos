using System.Text.Json;

namespace PubSub.Core.Services.Notifier
{
    public interface ISubscriberNotifierService
    {
        Task Notify(JsonElement message, string recipient);
    }
}
