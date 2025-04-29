using System.Text.Json;

namespace PubSub.Core.ApplicationServices.Serializer
{
    public interface IJsonPayloadSerializer
    {
        byte[] Serialize(JsonElement message);

        JsonElement Deserialize(byte[] bytes);
    }
}
