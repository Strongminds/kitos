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

[assembly: OwinStartup(typeof(Presentation.Web.Startup))]
namespace Presentation.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            InitializeHangfire(app);

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

        private static void InitializeHangfire(IAppBuilder app)
        {
            // Initializing the Hangfire scheduler
            var standardKernel = new KernelBuilder().ForHangFire().Build();

            GlobalConfiguration.Configuration.UseNinjectActivator(standardKernel);
            GlobalConfiguration.Configuration.UseSqlServerStorage("kitos_HangfireDB");

            app.UseHangfireDashboard();
            app.UseHangfireServer(new KeepReadModelsInSyncProcess());

            ServiceEndpointConfiguration.ConfigureValidationOfOutgoingConnections();

            var recurringJobManager = new RecurringJobManager();

            recurringJobManager.AddOrUpdate(
                recurringJobId: StandardJobIds.CheckExternalLinks,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchLinkCheckAsync(CancellationToken.None)),
                cronExpression: Cron.Weekly(DayOfWeek.Sunday, 0),
                timeZone: TimeZoneInfo.Local);

            new RecurringJobManager().AddOrUpdate(
                recurringJobId: StandardJobIds.PurgeOrphanedAdvice,
                job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchAdviceCleanupAsync(CancellationToken.None)),
                cronExpression: Cron.Hourly(),
                timeZone: TimeZoneInfo.Local);
        }
    }
}
