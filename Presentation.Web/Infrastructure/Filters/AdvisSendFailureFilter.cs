﻿using System;
using System.Linq;
using Core.ApplicationServices;
using Core.DomainModel.Advice;
using Core.DomainModel.Notification;
using Core.DomainModel.Shared;
using Core.DomainServices.Advice;
using Core.DomainServices.Notifications;
using Core.DomainServices.Repositories.Contract;
using Core.DomainServices.Repositories.GDPR;
using Core.DomainServices.Repositories.Project;
using Core.DomainServices.Repositories.SystemUsage;
using Hangfire.Common;
using Hangfire.States;
using Infrastructure.Services.Types;
using Microsoft.Extensions.DependencyInjection;
using Ninject;
using Presentation;
using Presentation.Web;
using Presentation.Web.Infrastructure;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Infrastructure.Filters;
using Presentation.Web.Ninject;
using Serilog;

namespace Presentation.Web.Infrastructure.Filters
{
    public class AdvisSendFailureFilter : IElectStateFilter, IJobFilter
    {
        private static readonly string HangfireRetryCountKey = "RetryCount";
        private static readonly string HangfireNoMoreRetriesKey = "NoMoreRetries";

        private static readonly string MatchType = typeof(AdviceService).FullName;
        protected static readonly string MatchMethod = typeof(AdviceService)
            .GetMethods()
            .First(x => x.Name.Equals(nameof(AdviceService.SendAdvice)))
            .Name;

        private readonly StandardKernel _kernel;

        public AdvisSendFailureFilter(StandardKernel kernel)
        {
            _kernel = kernel;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState failedState)
            {
                if(context.GetJobParameter<bool>(HangfireNoMoreRetriesKey))
                {
                    if (IsSendAdvice(context))
                    {
                        // Get argument input to method. In this case it's only one argument and it is the Id of the advice.
                        int adviceId = Convert.ToInt32(context.BackgroundJob.Job.Args.First());
                        var logger = _kernel.GetService<ILogger>();
                        try
                        {
                            using (new HangfireNinjectResolutionScope(_kernel))
                            {
                                var advisService = _kernel.GetService<IAdviceService>();
                                var userNotificationService = _kernel.GetService<IUserNotificationService>();

                                var failedAdvice = advisService.GetAdviceById(adviceId);
                                if (failedAdvice.IsNone)
                                {
                                    logger.Error($"Failed to create user notification for advis with Id: {adviceId} as it could not be found.");
                                    return;
                                }
                                var advice = failedAdvice.Value;

                                if (advice.HasInvalidState())
                                {
                                    logger.Error($"Failed to create user notification for advis with Id: {adviceId} as it has an invalid state.");
                                    return;
                                }
                                var organizationIdOfRelatedEntityId = GetRelatedEntityOrganizationId(advice);
                                if (organizationIdOfRelatedEntityId.IsNone)
                                {
                                    logger.Error($"Failed to create user notification as get root resolution for advis with Id: {adviceId} failed to resolve root.");
                                    return;
                                }

                                userNotificationService.AddUserNotification(organizationIdOfRelatedEntityId.Value, advice.ObjectOwnerId.Value, advice.Name, $"Afsendelse af advis fejlede efter {KitosConstants.MaxHangfireRetries} forsøg. Undersøg gerne nærmere og rapportér evt. fejlen.", advice.RelationId.Value, advice.Type.Value, NotificationType.Advice);
                            }
                        }
                        catch(Exception e)
                        {
                            logger.Error(e, $"Failed to create user notification for failed advis with Id: {adviceId}");
                        }
                    }
                }
                else if (context.GetJobParameter<int>(HangfireRetryCountKey) >= (KitosConstants.MaxHangfireRetries - 1))
                {
                    context.SetJobParameter<bool>(HangfireNoMoreRetriesKey, true);
                }
            }
        }

        private static bool IsSendAdvice(StateContext context)
        {
            return context.BackgroundJob?.Job?.Type?.FullName?.Equals(MatchType) == true &&
                   context.BackgroundJob?.Job?.Method?.Name?.Equals(MatchMethod) == true;
        }

        private Maybe<int> GetRelatedEntityOrganizationId(Advice advice)
        {
            var advisRootResolution = _kernel.GetService<IAdviceRootResolution>();
            return advisRootResolution.Resolve(advice).Select(x => x.OrganizationId);
        }

        public bool AllowMultiple => false;
        public int Order => 10;
    }
}
