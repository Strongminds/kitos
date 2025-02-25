﻿using PubSub.Application.Common;

namespace PubSub.Application.Subscribe
{
    public class Subscription
    {
        public string Callback { get; set; }
        public IEnumerable<string> Topics { get; set; }
    }
}
