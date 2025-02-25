namespace Tests.PubSubTester.DTOs
{
    public class PublishRequestDTO
    {
        public string Queue { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }
}
