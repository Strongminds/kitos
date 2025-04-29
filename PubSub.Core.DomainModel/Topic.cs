namespace PubSub.Core.DomainModel;

public class Topic
{
    public string Name { get; set; }
    public Topic(string name)
    {
        Name = name;
    }
}
