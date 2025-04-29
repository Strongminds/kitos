using System.Text.Json;

namespace PubSub.Core.ApplicationServices.Serializer
{
    public interface IPayloadSerializer
    {
        byte[] Serialize(JsonElement message);

        JsonElement Deserialize(byte[] bytes);
    }
}
