﻿namespace PubSub.Application
{
    public record Publication(string Queue, string Message, string Token);
}
