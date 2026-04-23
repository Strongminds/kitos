namespace Tools.MigrateSQLServer2Postgres.Cli;

internal sealed record CommandLineOptions(
    string SourceConnectionString,
    string TargetConnectionString,
    bool AllowNonEmptyTarget,
    bool Resume,
    bool ContinueOnError,
    bool DisableForeignKeyChecks);
