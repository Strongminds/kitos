namespace PubSub.Application.StartupTasks
{
    public class StartSubscribeLoopStartupTask : IStartupTask
    {
        private readonly ISubscribeLoopHostedService _subscribeLoopHostedService;

        public StartSubscribeLoopStartupTask(ISubscribeLoopHostedService subscribeLoopHostedService)
        {
            _subscribeLoopHostedService = subscribeLoopHostedService;
        }

        public async Task ExecuteAsync()
        {
            await _subscribeLoopHostedService.StartAsync(CancellationToken.None);
        }
    }
}
