﻿namespace PubSub.Core.Models
{
    public class Subscription
    {
        public Uri Callback { get; set; }
        public IEnumerable<Topic> Topics { get; set; }
        public string OwnerId { get; set; } //todo
    }
}
