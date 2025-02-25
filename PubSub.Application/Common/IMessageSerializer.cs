namespace PubSub.Application.Common
{
    public interface IMessageSerializer
    {
        byte[] Serialize(string message);

        string Deserialize(byte[] bytes);
    }
}
