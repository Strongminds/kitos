﻿using Core.Abstractions.Types;


namespace Core.ApplicationServices.ScheduledJobs
{
    public class AdviceTrigger
    {
        public Maybe<int> PartitionId { get; }
        public string Cron { get; }

        public AdviceTrigger(string cron, Maybe<int> partitionId)
        {
            Cron = cron;
            PartitionId = partitionId;
        }
    }
}
