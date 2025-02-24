namespace PubSub.Application
{
    public class Topic
    {
        public required string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Topic)obj;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }
}
