namespace PubSub.Application.StartupTasks
{
    public class StartupTaskRunner
    {
        public static async Task RunAsync(IEnumerable<IStartupTask> startupTasks)
        {
            foreach (var task in startupTasks)
            {
                await task.ExecuteAsync();
            }
        }
    }
}
