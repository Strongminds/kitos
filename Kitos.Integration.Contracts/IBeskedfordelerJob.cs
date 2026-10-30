using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;

namespace Kitos.Integration.Contracts;

// Shared serialization contract: only KITOS registers and executes its implementation.
public interface IBeskedfordelerJob
{
    const string QueueName = "beskedfordeler";
    static readonly Guid TestMessageType = new("7654895c-a3f3-4c77-8afd-4c77330f963c");

    [Queue(QueueName)]
    [AutomaticRetry(Attempts = 8, DelaysInSeconds = new[] { 2, 4, 8, 16, 32, 64, 128, 256 }, OnAttemptsExceeded = AttemptsExceededAction.Fail, ExceptOn = new[] { typeof(BeskedfordelerQuarantineException) })]
    Task Process(string originalMessage, CancellationToken cancellationToken);
}
