﻿using System;
using System.Threading;
using Microsoft.Owin;
using Owin;
using Hangfire;
using Core.BackgroundJobs.Model;
using Hangfire.Common;
using Infrastructure.Services.BackgroundJobs;
using Infrastructure.Services.Http;
using Microsoft.IdentityModel.Tokens;
using Presentation.Web.Hangfire;
using Presentation.Web.Infrastructure.Middleware;
using Presentation.Web.Infrastructure.Model.Authentication;
using Presentation.Web.Ninject;
using Presentation.Web.Infrastructure.Filters;
using Presentation.Web.Infrastructure;

[assembly: OwinStartup(typeof(Presentation.Web.Startup))]
namespace Presentation.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            InitializeHangfire(app);

            //Replace value in web.config with the value from the environment variable
            
            

            // Setup token authentication
            app.UseJwtBearerAuthentication(new Microsoft.Owin.Security.Jwt.JwtBearerAuthenticationOptions
            {
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidIssuer = BearerTokenConfig.Issuer,
                    ValidateIssuer = true,
                    IssuerSigningKey = BearerTokenConfig.SecurityKey,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                }
            });

            app.UseNinject();
            app.Use<CorrelationIdMiddleware>();
            app.Use<ApiRequestsLoggingMiddleware>();
            app.Use<DenyUsersWithoutApiAccessMiddleware>();
            app.Use<DenyModificationsThroughApiMiddleware>();
            app.Use<DenyTooLargeQueriesMiddleware>();
        }

        //Create a method that gets kitos_HangfireDB connection string from the web.config file, checks if the variable is base64 encoded, and decodes it if it is encoded
        private static string GetHangfireConnectionString()
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["kitos_HangfireDB"]?.ConnectionString ?? "kitos_HangfireDB";
            if (!connectionString.StartsWith("base64:")) 
                return connectionString;
            
            var base64EncodedString = connectionString.Substring("base64:".Length);
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedString);
            connectionString = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            return connectionString;
        }

        private static void InitializeHangfire(IAppBuilder app)
        {
            // Initializing the Hangfire scheduler
            var standardKernel = new KernelBuilder().ForHangFire().Build();

            GlobalConfiguration.Configuration.UseNinjectActivator(standardKernel);
            GlobalConfiguration.Configuration.UseSqlServerStorage(GetHangfireConnectionString());
            GlobalJobFilters.Filters.Add(new AdvisSendFailureFilter(standardKernel));
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = KitosConstants.MaxHangfireRetries });

            app.UseHangfireDashboard();
            app.UseHangfireServer(new KeepReadModelsInSyncProcess());

            ServiceEndpointConfiguration.ConfigureValidationOfOutgoingConnections();

            var recurringJobManager = new RecurringJobManager();

            /******************
             * RECURRING JOBS *
             *****************/

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.CheckExternalLinks,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchLinkCheckAsync(CancellationToken.None)),
                cronExpression: Cron.Weekly(DayOfWeek.Sunday, 0),
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.ScheduleUpdatesForItSystemUsageReadModelsWhichChangesActiveState,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateStaleSystemUsageRmAsync(CancellationToken.None)),
                cronExpression: Cron.Daily(), // Every night at 00:00
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.ScheduleUpdatesForItContractOverviewReadModelsWhichChangesActiveState,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateStaleContractRmAsync(CancellationToken.None)),
                cronExpression: Cron.Daily(), // Every night at 00:00
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.ScheduleUpdatesForDataProcessingReadModelsWhichChangesActiveState,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateStaleDataProcessingRegistrationReadModels(CancellationToken.None)),
                cronExpression: Cron.Daily(), // Every night at 00:00
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.ScheduleFkOrgUpdates,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateFkOrgSync(CancellationToken.None)),
                cronExpression: Cron.Weekly(DayOfWeek.Monday,3), // Every monday at 3 AM
                timeZone: TimeZoneInfo.Local);

            /******************
             * ON-DEMAND JOBS *
             *****************/

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.RebuildDataProcessingReadModels,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchFullReadModelRebuild(ReadModelRebuildScope.DataProcessingRegistration, CancellationToken.None)),
                cronExpression: Cron.Never(), //On demand
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.RebuildItSystemUsageReadModels,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchFullReadModelRebuild(ReadModelRebuildScope.ItSystemUsage, CancellationToken.None)),
                cronExpression: Cron.Never(), //On demand
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.RebuildItContractReadModels,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchFullReadModelRebuild(ReadModelRebuildScope.ItContract, CancellationToken.None)),
                cronExpression: Cron.Never(), //On demand
                timeZone: TimeZoneInfo.Local);

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.PurgeOrphanedHangfireJobs,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchPurgeOrphanedHangfireJobs(CancellationToken.None)),
                cronExpression: Cron.Never(), //On demand
                timeZone: TimeZoneInfo.Local);
        }
    }
}
