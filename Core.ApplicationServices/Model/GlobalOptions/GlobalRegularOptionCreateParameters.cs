namespace Core.ApplicationServices.Model.GlobalOptions
{
    public class GlobalRegularOptionCreateParameters
    {
        public required string Name { get; set; }
        public bool IsObligatory { get; set; }
        public required string Description { get; set; }
    }
}
