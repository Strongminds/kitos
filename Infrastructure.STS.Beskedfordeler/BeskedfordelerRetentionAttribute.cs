using Kitos.Integration.Contracts;
using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Infrastructure.STS.Beskedfordeler;

// Failed jobs do not expire. Successful payloads remain recoverable for seven days.
public sealed class BeskedfordelerRetentionAttribute : JobFilterAttribute, IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (typeof(IBeskedfordelerJob).IsAssignableFrom(context.BackgroundJob.Job.Type) && context.NewState is SucceededState) context.JobExpirationTimeout = TimeSpan.FromDays(7);
    }
    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }
}
