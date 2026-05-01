using System;
using System.Linq;
using Core.DomainModel.Organization;
using Infrastructure.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tools.Test.Database.Model.Cli;
using Tools.Test.Database.Model.Environment;
using Tools.Test.Database.Model.Parameters;
using Tools.Test.Database.Model.Tasks;

namespace Tools.Test.Database
{
    class Program
    {
        static int Main(string[] args)
        {
            // Test data creators use legacy/local DateTime values; enable compatibility for PostgreSQL seeding.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var task = GetArgument(args, 0);
            var additionalArgs = args.Skip(1).ToArray();
            var rawConnectionString = GetArgument(additionalArgs, 0);
            var provider = ResolveDatabaseProvider(rawConnectionString);
            var connectionString = NormalizeConnectionString(rawConnectionString, provider);

            var databaseTask = CreateTask(task, additionalArgs, provider, connectionString);
            var taskName = $"{task}(implemented in {databaseTask.GetType().Name})";

            try
            {
                Console.WriteLine($"Executing {taskName}");
                Console.WriteLine($"Database provider: {provider}");
                FailOnConnectionToProd(connectionString);
                var dbOptionsBuilder = new DbContextOptionsBuilder<KitosContext>()
                    .UseLazyLoadingProxies();

                if (IsPostgreSqlProvider(provider))
                {
                    dbOptionsBuilder.UseNpgsql(connectionString);
                }
                else
                {
                    dbOptionsBuilder.UseSqlServer(connectionString);
                }

                var dbOptions = dbOptionsBuilder.Options;
                using (var context = new KitosContext(dbOptions))
                {
                    var success = databaseTask.Execute(context);
                    if (success == false)
                    {
                        Console.WriteLine($"Failed to execute {taskName}");
                        return -1;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine($"Executed {taskName} with success");
            return 0;
        }

        private static DatabaseTask CreateTask(string task, string[] additionalArgs, string provider, string connectionString)
        {
            switch (task)
            {
                case CliTargets.DropDatabase:
                    Console.WriteLine("Expecting the following arguments: <connectionString>");
                    return new DropDatabaseTask(connectionString, provider);

                case CliTargets.CreateOrganization:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <organizationType> <organizationName>");
                    var createOrganizationArgs = new
                    {
                        OrganizationType = GetIntegerArgument(additionalArgs, 1),
                        OrganizationName = GetArgument(additionalArgs, 2)
                    };

                    return new CreateOrganizationTask(createOrganizationArgs.OrganizationType, createOrganizationArgs.OrganizationName);

                case CliTargets.CreateTestUser:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <email> <password> <role> <organizationNames>");
                    var createUserArgs = new
                    {
                        Email = GetArgument(additionalArgs, 1),
                        Password = GetArgument(additionalArgs, 2),
                        Role = ParseRole(GetArgument(additionalArgs, 3)),
                        OrganizationNames = GetArgument(additionalArgs, 4)
                    };

                    return new CreateKitosUserTask(createUserArgs.Email, createUserArgs.Password, createUserArgs.Role, createUserArgs.OrganizationNames);

                case CliTargets.CreateApiTestUser:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <email> <password> <role> <organizationNames>");
                    var createApiUserArgs = new
                    {
                        Email = GetArgument(additionalArgs, 1),
                        Password = GetArgument(additionalArgs, 2),
                        Role = ParseRole(GetArgument(additionalArgs, 3)),
                        OrganizationNames = GetArgument(additionalArgs, 4)
                    };

                    return new CreateKitosUserTask(createApiUserArgs.Email, createApiUserArgs.Password, createApiUserArgs.Role, createApiUserArgs.OrganizationNames, true);

                case CliTargets.EnableAllOptions:
                    Console.WriteLine("Expecting the following arguments: <connectionString>");
                    return new EnableAllOptionsTask();

                case CliTargets.CreateItSystem:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <it_system_name>");
                    var createItSystemArgs = new
                    {
                        ItSystemName = GetArgument(additionalArgs, 1),
                        OrganizationName = GetArgument(additionalArgs, 2)
                    };

                    return new CreateItSystemTask(createItSystemArgs.ItSystemName, createItSystemArgs.OrganizationName, null);

                case CliTargets.CreateItSystemWithParent:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <it_system_name>");
                    var createItSystemWithParentArgs = new
                    {
                        ItSystemName = GetArgument(additionalArgs, 1),
                        OrganizationName = GetArgument(additionalArgs, 2),
                        ParentId = GetIntegerArgument(additionalArgs, 3)
                    };

                    return new CreateItSystemTask(createItSystemWithParentArgs.ItSystemName, createItSystemWithParentArgs.OrganizationName, createItSystemWithParentArgs.ParentId);

                case CliTargets.CreateItContract:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <it_contract_name>");
                    var createContractArgs = new
                    {
                        ItSystemName = GetArgument(additionalArgs, 1)
                    };

                    return new CreateItContractTask(createContractArgs.ItSystemName);

                case CliTargets.CreateTaskRef:
                    Console.WriteLine("Expecting the following arguments: <connectionString> <organizationName>");
                    var createTaskRefArgs = new
                    {
                        OrganizationName = GetArgument(additionalArgs, 1)
                    };

                    return new CreateTaskRefTask(createTaskRefArgs.OrganizationName);
                case CliTargets.CreateCleanTestDatabase:
                    var options = new
                    {
                        GlobalAdmin = new Credentials(GetArgument(additionalArgs, 1), GetArgument(additionalArgs, 2)),
                        LocalAdmin = new Credentials(GetArgument(additionalArgs, 3), GetArgument(additionalArgs, 4)),
                        NormalUser = new Credentials(GetArgument(additionalArgs, 5), GetArgument(additionalArgs, 6)),
                        ApiUser = new Credentials(GetArgument(additionalArgs, 7), GetArgument(additionalArgs, 8)),
                        GlobalAdminApiUser = new Credentials(GetArgument(additionalArgs, 9), GetArgument(additionalArgs, 10)),
                        SystemIntegrator = new Credentials(GetArgument(additionalArgs, 11), GetArgument(additionalArgs, 12))
                    };
                    return new CreateFullTestDatabaseTask(options.GlobalAdmin, options.LocalAdmin, options.NormalUser, options.ApiUser, options.GlobalAdminApiUser, options.SystemIntegrator);

                default:
                    throw new ArgumentOutOfRangeException(nameof(task), task, "Unknown task provided");
            }
        }

        private static OrganizationRole ParseRole(string role)
        {
            var organizationRoles = Enum.GetValues(typeof(OrganizationRole)).Cast<OrganizationRole>().ToDictionary(x => x.ToString("G"), StringComparer.OrdinalIgnoreCase);
            if (!organizationRoles.TryGetValue(role, out var actualRole))
            {
                throw new ArgumentException($"{nameof(role)} must be one of [{string.Join(",", organizationRoles.Keys)}]");
            }

            return actualRole;
        }
        private static void FailOnConnectionToProd(string connectionString)
        {
            if (Production.ContainsProductionIp(connectionString))
            {
                throw new NotSupportedException("This operation is not allowed in prod.");
            }
        }

        private static string EnsureTrustServerCertificate(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.TrustServerCertificate = true;
            return builder.ConnectionString;
        }

        private static string NormalizeConnectionString(string connectionString, string provider)
        {
            if (IsPostgreSqlProvider(provider))
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Host = "127.0.0.1";
                }

                if (string.IsNullOrWhiteSpace(builder.Database) == false)
                {
                    builder.Database = builder.Database.ToLowerInvariant();
                }

                return builder.ConnectionString;
            }

            return EnsureTrustServerCertificate(connectionString);
        }

        private static string ResolveDatabaseProvider(string connectionString)
        {
            var provider = Environment.GetEnvironmentVariable("Database__Provider");
            if (string.IsNullOrWhiteSpace(provider) == false)
            {
                return provider;
            }

            return LooksLikePostgreSqlConnectionString(connectionString) ? "PostgreSql" : "SqlServer";
        }

        private static bool IsPostgreSqlProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return false;
            }

            return provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
                   || provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                   || provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikePostgreSqlConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            return connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                   || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
                   || connectionString.Contains("User ID=", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetArgument(string[] additionalArgs, int index, bool trimEnclosingQuotes = true)
        {
            var arg = additionalArgs[index];
            if (trimEnclosingQuotes)
            {
                arg = arg.Trim('"');
            }
            return arg;
        }

        private static int GetIntegerArgument(string[] additionalArgs, int index, bool trimEnclosingQuotes = true)
        {
            var arg = GetArgument(additionalArgs, index, trimEnclosingQuotes);
            if (int.TryParse(arg, out int res))
            {
                return res;
            }

            throw new ArgumentException($"argument at index {index} must be an integer");
        }

    }
}
