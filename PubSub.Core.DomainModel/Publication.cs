using System.Text.Json;

namespace PubSub.Core.DomainModel
{
    public record Publication(Topic Topic, JsonElement Payload);
}
