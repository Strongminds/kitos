﻿namespace Tests.PubSubTester.DTOs
{
    public class SubscriptionDTO
    {
        public string Callback { get; set; }
        public IEnumerable<string> Queues { get; set; }
    }
}
