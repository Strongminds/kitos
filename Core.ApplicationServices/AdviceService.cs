﻿using Core.DomainModel;
using Core.DomainModel.Advice;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItProject;
using Core.DomainModel.ItSystem;
using Core.DomainServices;
using Ninject;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.ScheduledJobs;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices.Extensions;
using Core.DomainServices.Time;
using Infrastructure.Services.DataAccess;
using Ninject.Extensions.Logging;
using Infrastructure.Services.Types;
using Core.DomainModel.Shared;
using Core.DomainServices.Notifications;
using Core.DomainModel.Notification;
using Core.DomainServices.Advice;

namespace Core.ApplicationServices
{
    public class AdviceService : IAdviceService
    {
        #region Properties

        [Inject]
        public IMailClient MailClient { get; set; }

        [Inject]
        public IGenericRepository<User> UserRepository { get; set; }

        [Inject]
        public IGenericRepository<DomainModel.Advice.Advice> AdviceRepository { get; set; }

        [Inject]
        public IGenericRepository<AdviceSent> AdviceSentRepository { get; set; }

        [Inject]
        public IGenericRepository<ItContractRight> ItContractRights { get; set; }

        [Inject]
        public IGenericRepository<ItProjectRight> ItprojectRights { get; set; }

        [Inject]
        public IGenericRepository<ItSystemRight> ItSystemRights { get; set; }

        [Inject]
        public IGenericRepository<DataProcessingRegistrationRight> DataProcessingRegistrationRights { get; set; }

        [Inject]
        public ILogger Logger { get; set; }

        [Inject]
        public IGenericRepository<ItContract> ItContractRepository { get; set; }

        [Inject]
        public IGenericRepository<ItInterface> ItInterfaceRepository { get; set; }

        [Inject]
        public IGenericRepository<ItProject> ItProjectRepository { get; set; }

        [Inject]
        public IGenericRepository<ItSystemUsage> ItSystemUsageRepository { get; set; }

        [Inject]
        public IGenericRepository<DataProcessingRegistration> DataProcessingRegistrations { get; set; }

        [Inject]
        public ITransactionManager TransactionManager { get; set; }

        [Inject]
        public IOrganizationalUserContext OrganizationalUserContext { get; set; }

        [Inject] public IHangfireApi HangfireApi { get; set; }

        [Inject] public IOperationClock OperationClock { get; set; }

        [Inject] public IUserNotificationService UserNotificationService { get; set; }

        [Inject] public IAdviceRootResolution AdviceRootResolution { get; set; }

        #endregion

        public void CreateAdvice(Advice advice)
        {
            ScheduleAdvice(advice);
            AdviceRepository.Update(advice);
            AdviceRepository.Save();
        }

        public IQueryable<Advice> GetAdvicesForOrg(int orgKey)
        {
            var result = AdviceRepository.SQL($"SELECT a.* FROM[kitos].[dbo].[Advice] a Join ItContract c on c.Id = a.RelationId Where c.OrganizationId = {orgKey} and a.Type = 0 Union SELECT a.* FROM[kitos].[dbo].[Advice] a Join ItProject p on p.Id = a.RelationId Where p.OrganizationId = {orgKey} and a.Type = 2 Union SELECT a.* FROM[kitos].[dbo].[Advice] a Join ItSystemUsage u on u.Id = a.RelationId Where u.OrganizationId = {orgKey} and a.Type = 1 Union SELECT a.* FROM[kitos].[dbo].[Advice] a Join ItInterface i on i.Id = a.RelationId Where i.OrganizationId = {orgKey} and a.Type = 3 Union SELECT a.* FROM[kitos].[dbo].[Advice] a Join DataProcessingRegistrations i on i.Id = a.RelationId Where i.OrganizationId = {orgKey} and a.Type = 4");
            return result.AsQueryable();
        }

        public IQueryable<Advice> GetAdvicesAccessibleToCurrentUser()
        {
            return OrganizationalUserContext.IsGlobalAdmin()
                ? AdviceRepository.AsQueryable()
                : OrganizationalUserContext
                    .OrganizationIds
                    .Select(GetAdvicesForOrg)
                    .Aggregate<IQueryable<Advice>, IQueryable<Advice>>
                    (
                        null,
                        (acc, next) => acc == null ? next : acc.Concat(next)
                    );
        }

        public bool SendAdvice(int id)
        {
            using var transaction = TransactionManager.Begin(IsolationLevel.ReadCommitted);
            try
            {
                var advice = AdviceRepository.AsQueryable().ById(id);
                if (advice != null)
                {
                    if (advice.AdviceType == AdviceType.Immediate || IsAdviceInScope(advice))
                    {
                        if (DispatchEmails(advice))
                        {
                            AdviceRepository.Update(advice);

                            AdviceSentRepository.Insert(new AdviceSent { AdviceId = id, AdviceSentDate = OperationClock.Now });
                        }
                    }

                    if (advice.AdviceType == AdviceType.Immediate)
                    {
                        advice.IsActive = false;
                    }
                    else if (IsAdviceExpired(advice))
                    {
                        advice.IsActive = false;
                        DeleteJobFromHangfire(advice);
                    }

                    AdviceRepository.Save();
                    AdviceSentRepository.Save();
                    transaction.Commit();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger?.Error(e, "General error sending emails in advice service");
                transaction.Rollback();
                throw;
            }
        }

        private bool IsAdviceExpired(Advice advice)
        {
            return advice.StopDate != null && advice.StopDate.Value.Date < OperationClock.Now.Date;
        }

        private bool IsAdviceInScope(Advice advice)
        {
            return advice.AlarmDate != null && advice.AlarmDate.Value.Date <= OperationClock.Now.Date && !IsAdviceExpired(advice);
        }

        private bool DispatchEmails(Advice advice)
        {
            var message = new MailMessage
            {
                Body = advice.Body,
                Subject = (advice.Subject).Replace('\r', ' ').Replace('\n', ' '),
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };

            foreach (var r in advice.Reciepients)
            {
                switch (r.RecieverType)
                {
                    case RecieverType.RECIEVER:
                        switch (r.RecpientType)
                        {
                            case RecieverType.USER:
                                AddRecipientByName(r, message.To);
                                break;
                            case RecieverType.ROLE:
                                AddRecipientByRole(advice, r, message.To);
                                break;
                        }
                        break;
                    case RecieverType.CC:
                        switch (r.RecpientType)
                        {
                            case RecieverType.USER:
                                AddRecipientByName(r, message.CC);
                                break;
                            case RecieverType.ROLE:
                                AddRecipientByRole(advice, r, message.CC);
                                break;
                        }
                        break;
                }
            }

            if (message.To.Any() || message.CC.Any())
            {
                MailClient.Send(message);
                advice.SentDate = OperationClock.Now;
                return true;
            }
            else
            {
                var organizationIdOfRelatedEntityId = GetRelatedEntityOrganizationId(advice);
                if(organizationIdOfRelatedEntityId.IsNone)
                {
                    Logger?.Error($"Advis doesn't have valid/correct related entity (RelationId and Type mismatch). Advice Id: {advice.Id}, Advice RelationId: {advice.RelationId}, Advice RelatedEntityType: {advice.Type}");
                }
                else
                {
                    if (advice.HasInvalidState())
                    {
                        Logger?.Error($"Advis is missing critical function information. Advice Id: {advice.Id}, Advice RelationId: {advice.RelationId}, Advice RelatedEntityType: {advice.Type}, Advice ownerId: {advice.ObjectOwnerId}");
                    }
                    UserNotificationService.AddUserNotification(organizationIdOfRelatedEntityId.Value, advice.ObjectOwnerId.Value, advice.Name, "Advis kunne ikke sendes da der ikke blev fundet nogen gyldig modtager. Dette kan skyldes at der ikke er nogen bruger tilknyttet den/de valgte rolle(r).", advice.RelationId.Value, advice.Type.Value, NotificationType.Advice);
                }
                return false;
            }
        }

        private static void AddRecipientByName(AdviceUserRelation r, MailAddressCollection mailAddressCollection)
        {
            if (!string.IsNullOrEmpty(r.Name))
            {
                mailAddressCollection.Add(r.Name);
            }
        }

        private void AddRecipientByRole(Advice advice, AdviceUserRelation r, MailAddressCollection mailAddressCollection)
        {
            switch (advice.Type)
            {
                case RelatedEntityType.itContract:

                    var itContractRoles = ItContractRights.AsQueryable().Where(I => I.ObjectId == advice.RelationId
                        && I.Role.Name == r.Name);
                    foreach (var t in itContractRoles)
                    {
                        mailAddressCollection.Add(t.User.Email);
                    }

                    break;
                case RelatedEntityType.itProject:
                    var projectRoles = ItprojectRights.AsQueryable().Where(I => I.ObjectId == advice.RelationId
                        && I.Role.Name == r.Name);
                    foreach (var t in projectRoles)
                    {
                        mailAddressCollection.Add(t.User.Email);
                    }

                    break;
                case RelatedEntityType.itSystemUsage:

                    var systemRoles = ItSystemRights.AsQueryable().Where(I => I.ObjectId == advice.RelationId
                                                                              && I.Role.Name == r.Name);
                    foreach (var t in systemRoles)
                    {
                        mailAddressCollection.Add(t.User.Email);
                    }

                    break;
                case RelatedEntityType.dataProcessingRegistration:

                    var dpaRoles = DataProcessingRegistrationRights.AsQueryable().Where(I =>
                        I.ObjectId == advice.RelationId
                        && I.Role.Name == r.Name);
                    foreach (var t in dpaRoles)
                    {
                        mailAddressCollection.Add(t.User.Email);
                    }

                    break;
            }
        }

        public void Delete(Advice advice)
        {
            RemoveAdviceAndItsRelatedEntities(advice);
            AdviceRepository.Save();
        }

        private void RemoveAdviceAndItsRelatedEntities(Advice advice)
        {
            DeleteJobFromHangfire(advice);
            AdviceRepository.DeleteByKeyWithReferencePreload(advice.Id);
        }

        private void DeleteJobFromHangfire(Advice advice)
        {
            //Remove pending shcedules if any
            var jobsScheduled =
                HangfireApi
                    .GetScheduledJobs(0, int.MaxValue)
                    .Where(x => x.Value.Job.Method.Name == nameof(CreateOrUpdateJob));

            foreach (var j in jobsScheduled)
            {
                var t = j.Value.Job.Args[1].ToString(); // Pick "Advice: nn"
                if (t.Equals(advice.JobId))
                {
                    HangfireApi.DeleteScheduledJob(j.Key);
                    break;
                }
            }

            //Remove the job by main job id + any partitions (max 12 - one pr. month)
            HangfireApi.RemoveRecurringJobIfExists(advice.JobId);
            for (var i = 0; i < 12; i++)
            {
                HangfireApi.RemoveRecurringJobIfExists(CreatePartitionJobId(advice.JobId, i));
            }
        }

        public void BulkDeleteAdvice(IEnumerable<Advice> toBeDeleted)
        {
            using var transaction = TransactionManager.Begin(IsolationLevel.ReadCommitted);
            foreach (var advice in toBeDeleted)
            {
                RemoveAdviceAndItsRelatedEntities(advice);
            }
            AdviceRepository.Save();
            transaction.Commit();
        }

        public void Deactivate(Advice advice)
        {
            DeleteJobFromHangfire(advice);
            advice.IsActive = false;
            AdviceRepository.Update(advice);
            AdviceRepository.Save();
        }

        private void ScheduleAdvice(Advice advice)
        {
            if (advice.AdviceType == AdviceType.Immediate)
            {
                HangfireApi.Schedule(() => SendAdvice(advice.Id));
            }
            else if (advice.AdviceType == AdviceType.Repeat)
            {
                var alarmDate = advice.AlarmDate;

                if (alarmDate == null)
                    throw new ArgumentException(nameof(alarmDate) + " must be defined");

                HangfireApi.Schedule(
                    () => CreateOrUpdateJob(advice.Id, advice.JobId, advice.Scheduling.GetValueOrDefault(), alarmDate.Value), new DateTimeOffset(alarmDate.Value));
            }
        }

        public void RescheduleRecurringJob(Advice advice)
        {
            var alarmDate = advice.AlarmDate;

            if (alarmDate == null)
                throw new ArgumentException(nameof(alarmDate) + " must be defined");

            DeleteJobFromHangfire(advice); // Remove existing hangfire job
            HangfireApi.Schedule(() => CreateOrUpdateJob(advice.Id, advice.JobId, advice.Scheduling.GetValueOrDefault(), alarmDate.Value), new DateTimeOffset(alarmDate.Value));
        }

        public void CreateOrUpdateJob(int entityId, string name, Scheduling schedule, DateTime alarmDate)
        {
            var advice = AdviceRepository.GetByKey(entityId);
            if (advice == null)
            {
                throw new ArgumentException(nameof(entityId) + " does not point to a valid id");
            }

            foreach (var adviceTrigger in AdviceTriggerFactory.CreateFrom(alarmDate, schedule))
            {
                var jobId = adviceTrigger.PartitionId.Match(partitionId => CreatePartitionJobId(name, partitionId), () => name);
                HangfireApi.AddOrUpdateRecurringJob(jobId, () => SendAdvice(entityId), adviceTrigger.Cron);
            }
        }

        private static string CreatePartitionJobId(string prefix, int partitionIndex)
        {
            return $"{prefix}_part_{partitionIndex}";
        }

        public Maybe<Advice> GetAdviceById(int id)
        {
            return AdviceRepository.GetByKey(id);
        }

        private Maybe<int> GetRelatedEntityOrganizationId(Advice advice)
        {
            return AdviceRootResolution.Resolve(advice).Select(x => x.OrganizationId);
        }
    }
}
