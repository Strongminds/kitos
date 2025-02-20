namespace PubSub.Application
{
    public interface IMessageSerializer
    {
        byte[] Serialize(string message);

        string Deserialize(byte[] bytes);
    }
}
