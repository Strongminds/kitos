namespace PubSub.Core.Models;

public struct Topic
{
    public required string Name { get; set; }
    public Topic(string name)
    {
        Name = name;
    }
}
