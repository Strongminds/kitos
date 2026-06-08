using Tools.MigrateSQLServer2Postgres.Cli;
using Tools.MigrateSQLServer2Postgres.Migration;

namespace Tools.MigrateSQLServer2Postgres;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        if (CommandLineParser.IsHelpRequested(args))
        {
            CliConsole.RenderHelp();
            return 0;
        }

        try
        {
            var isInteractive = CommandLineParser.IsInteractiveRequested(args);
            var options = isInteractive
                ? InteractivePrompt.PromptForOptions(args)
                : CommandLineParser.Parse(args);

            var runner = new MigrationRunner();
            return await runner.RunAsync(options, isInteractive);
        }
        catch (CommandLineException cliException)
        {
            CliConsole.Error(cliException.Message);
            CliConsole.RenderHelp();
            return -1;
        }
        catch (Exception exception)
        {
            CliConsole.Exception("Migration failed with an unexpected error.", exception);
            return -2;
        }
    }
}
