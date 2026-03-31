using System;
using System.Threading;
using Hangfire.Server;
using Infrastructure.Services.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Presentation.Web.Hangfire
{
    public class KeepReadModelsInSyncProcess : IBackgroundProcess
    {
        private readonly IServiceProvider _serviceProvider;

        public KeepReadModelsInSyncProcess(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.ShutdownToken, context.StoppingToken);
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
            try
            {
                var backgroundJobLauncher = scope.ServiceProvider.GetRequiredService<IBackgroundJobLauncher>();
                PurgeDuplicateUpdates(backgroundJobLauncher, combinedTokenSource);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error in KeepReadModelsInSyncProcess");
            }
        }

        private static void PurgeDuplicateUpdates(IBackgroundJobLauncher launcher, CancellationTokenSource tokenSource)
        {
            // Background job logic - moved to Hangfire recurring jobs
        }
    }
}
