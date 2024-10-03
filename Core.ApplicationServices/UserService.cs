﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Web;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices;
using System.Security.Cryptography;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Organizations;
using Core.DomainModel.Commands;
using Core.DomainModel.Events;
using Core.DomainModel.Organization.DomainEvents;
using Core.DomainModel.Users;
using Infrastructure.Services.Cryptography;
using Core.DomainServices.Authorization;
using Core.DomainServices.Extensions;
using Core.DomainServices.Queries;
using Infrastructure.Services.DataAccess;


namespace Core.ApplicationServices
{
    public class UserService : IUserService
    {
        private readonly TimeSpan _ttl;
        private readonly string _baseUrl;
        private readonly string _mailSuffix;
        private readonly string _defaultUserPassword;
        private readonly bool _useDefaultUserPassword;
        private readonly IUserRepository _repository;
        private readonly IOrganizationService _organizationService;
        private readonly ITransactionManager _transactionManager;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Organization> _orgRepository;
        private readonly IGenericRepository<PasswordResetRequest> _passwordResetRequestRepository;
        private readonly IMailClient _mailClient;
        private readonly ICryptoService _cryptoService;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly IDomainEvents _domainEvents;
        private readonly SHA256Managed _crypt;
        private readonly IOrganizationalUserContext _organizationalUserContext;
        private readonly ICommandBus _commandBus;
        private static readonly RNGCryptoServiceProvider rngCsp = new();
        private const string KitosManualsLink = "https://info.kitos.dk/s/qZPox9byHBRsMi2";

        public UserService(TimeSpan ttl,
            string baseUrl,
            string mailSuffix,
            string defaultUserPassword,
            bool useDefaultUserPassword,
            IGenericRepository<User> userRepository,
            IGenericRepository<Organization> orgRepository,
            IGenericRepository<PasswordResetRequest> passwordResetRequestRepository,
            IMailClient mailClient,
            ICryptoService cryptoService,
            IAuthorizationContext authorizationContext,
            IDomainEvents domainEvents,
            IUserRepository repository,
            IOrganizationService organizationService,
            ITransactionManager transactionManager,
            IOrganizationalUserContext organizationalUserContext,
            ICommandBus commandBus)
        {
            _ttl = ttl;
            _baseUrl = baseUrl;
            _mailSuffix = mailSuffix;
            _defaultUserPassword = defaultUserPassword;
            _useDefaultUserPassword = useDefaultUserPassword;
            _userRepository = userRepository;
            _orgRepository = orgRepository;
            _passwordResetRequestRepository = passwordResetRequestRepository;
            _mailClient = mailClient;
            _cryptoService = cryptoService;
            _authorizationContext = authorizationContext;
            _domainEvents = domainEvents;
            _repository = repository;
            _organizationService = organizationService;
            _transactionManager = transactionManager;
            _organizationalUserContext = organizationalUserContext;
            _commandBus = commandBus;
            _crypt = new SHA256Managed();
            if (useDefaultUserPassword && string.IsNullOrWhiteSpace(defaultUserPassword))
            {
                throw new ArgumentException(nameof(defaultUserPassword) + " must be defined, when it must be used.");
            }
        }

        public User AddUser(User user, bool sendMailOnCreation, int orgId)
        {
            // hash his salt and default password
            var utcNow = DateTime.UtcNow;
            user.Salt = _cryptoService.Encrypt(utcNow + " spices");
            user.Uuid = Guid.NewGuid();

            user.Password = _useDefaultUserPassword
                ? _cryptoService.Encrypt(_defaultUserPassword + user.Salt)
                : _cryptoService.Encrypt(utcNow + user.Salt);

            if (!_authorizationContext.AllowCreate<User>(orgId))
            {
                throw new SecurityException();
            }

            _userRepository.Insert(user);
            _userRepository.Save();

            var savedUser = _userRepository.Get(u => u.Id == user.Id).FirstOrDefault();

            _domainEvents.Raise(new EntityCreatedEvent<User>(savedUser));

            if (sendMailOnCreation)
                IssueAdvisMail(savedUser, false, orgId);

            return savedUser;
        }

        public void UpdateUser(User user, bool sendMailOnUpdate, int orgId)
        {
            _userRepository.Update(user);

            _domainEvents.Raise(new EntityUpdatedEvent<User>(user));

            if (sendMailOnUpdate)
            {
                IssueAdvisMail(user, false, orgId);
            }
        }

        public void IssueAdvisMail(User user, bool reminder, int orgId)
        {
            if (user == null || _userRepository.GetByKey(user.Id) == null)
                throw new ArgumentNullException(nameof(user));

            var org = _orgRepository.GetByKey(orgId);

            var reset = GenerateResetRequest(user);
            var resetLink = _baseUrl + "#/reset-password/" + HttpUtility.UrlEncode(reset.Hash);

            var subject = (reminder ? "Påmindelse: " : string.Empty) + "Oprettelse som ny bruger i KITOS " + _mailSuffix;
            var content = "<h2>Kære " + user.Name + "</h2>" +
                          "<p>Du er blevet oprettet, som bruger i KITOS (Kommunernes IT Overblikssystem) under organisationen " +
                          org.Name + ".</p>" +
                          "<p>Du bedes klikke <a href='" + resetLink +
                          "'>her</a>, hvor du første gang bliver bedt om at indtaste et nyt password for din KITOS profil.</p>" +
                          "<p>Linket udløber om " + _ttl.TotalDays + " dage. <a href='" + resetLink + "'>Klik her</a>, " +
                          "hvis dit link er udløbet og du vil blive ledt til 'Glemt password' proceduren.</p>" +
                          "<p><a href='" + KitosManualsLink + "'>Klik her for at få hjælp, vejledning og inspiration til Kitos</a></p>" +
                          "<p>Bemærk at denne mail ikke kan besvares.</p>";

            IssuePasswordReset(user, subject, content);

            user.LastAdvisDate = DateTime.UtcNow.Date;
            _userRepository.Update(user);
            _userRepository.Save();
        }

        public PasswordResetRequest IssuePasswordReset(User user, string subject, string content)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var mailContent = "";
            var reset = new PasswordResetRequest();
            if (content == null)
            {
                reset = GenerateResetRequest(user);
                var resetLink = _baseUrl + "#/reset-password/" + HttpUtility.UrlEncode(reset.Hash);
                mailContent = "<p>Du har bedt om at få nulstillet dit password.</p>" +
                              "<p><a href='" + resetLink +
                              "'>Klik her for at nulstille passwordet for din KITOS profil</a>.</p>" +
                              "<p>Linket udløber om " + _ttl.TotalDays + " dage.</p>" +
                              "<p><a href='" + KitosManualsLink + "'>Klik her for at få hjælp, vejledning og inspiration til Kitos</a></p>" +
                              "<p>Bemærk at denne mail ikke kan besvares.</p>";
            }
            var mailSubject = "Nulstilning af dit KITOS password" + _mailSuffix;

            var message = new MailMessage
            {
                Subject = (subject ?? mailSubject).Replace('\r', ' ').Replace('\n', ' '),
                Body = content ?? mailContent,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
            };
            message.To.Add(user.Email);

            _mailClient.Send(message);

            return reset;
        }

        public bool IsEmailInUse(string email)
        {
            var matchingEmails = _userRepository.Get(x => x.Email == email);
            return matchingEmails.Any();
        }

        public Result<User, OperationError> GetUserByEmail(Guid organizationUuid, string email)
        {
            return _organizationService.GetPermissions(organizationUuid)
                .Match(permissions =>
                {
                    if (permissions.Read == false)
                        return new OperationError(
                            $"User not allowed to read organization with uuid: {organizationUuid}",
                            OperationFailure.Forbidden);
                    return Maybe<OperationError>.None;
                }, error => error)
                .Match<Result<User, OperationError>>(error => error,
                    () =>
                    {
                        return _repository.AsQueryable().FirstOrDefault(u =>
                            u.Email == email);
                    });
        }

        private PasswordResetRequest GenerateResetRequest(User user)
        {
            var now = DateTime.UtcNow;

            byte[] randomNumber = new byte[8];
            rngCsp.GetBytes(randomNumber);

            byte[] encrypted = _crypt.ComputeHash(randomNumber);

            var hash = HttpServerUtility.UrlTokenEncode(encrypted);

            var request = new PasswordResetRequest { Hash = hash, Time = now, UserId = user.Id };

            _passwordResetRequestRepository.Insert(request);
            _passwordResetRequestRepository.Save();

            return request;
        }

        public PasswordResetRequest GetPasswordReset(string hash)
        {
            var passwordReset = _passwordResetRequestRepository.Get(req => req.Hash == hash).FirstOrDefault();

            if (passwordReset == null) return null;

            var timespan = DateTime.UtcNow - passwordReset.Time;
            if (timespan > _ttl)
            {
                // the reset request is too old, delete it
                _passwordResetRequestRepository.DeleteByKey(passwordReset.Id);
                _passwordResetRequestRepository.Save();
                return null;
            }

            return passwordReset;
        }

        public void ResetPassword(PasswordResetRequest passwordResetRequest, string newPassword)
        {
            if (passwordResetRequest == null)
                throw new ArgumentNullException(nameof(passwordResetRequest));

            if (!IsValidPassword(newPassword))
                throw new ArgumentException("New password is invalid");

            var user = passwordResetRequest.User;

            user.Password = _cryptoService.Encrypt(newPassword + user.Salt);
            _userRepository.Update(user);
            _userRepository.Save();

            _passwordResetRequestRepository.DeleteByKey(passwordResetRequest.Id);
            _passwordResetRequestRepository.Save();
        }


        private static bool IsValidPassword(string password)
        {
            return password.Length >= 6;
        }

        public void Dispose()
        {
            _crypt?.Dispose();
        }

        public Result<IQueryable<User>, OperationError> GetUsersWithCrossOrganizationPermissions()
        {
            if (_authorizationContext.GetCrossOrganizationReadAccess() < CrossOrganizationDataReadAccessLevel.All)
            {
                return new OperationError(OperationFailure.Forbidden);
            }
            return Result<IQueryable<User>, OperationError>.Success(_repository.GetUsersWithCrossOrganizationPermissions());
        }

        public Result<IQueryable<User>, OperationError> GetUsersWithRoleAssignedInAnyOrganization(OrganizationRole role)
        {
            if (_authorizationContext.GetCrossOrganizationReadAccess() < CrossOrganizationDataReadAccessLevel.All)
            {
                return new OperationError(OperationFailure.Forbidden);
            }

            return Result<IQueryable<User>, OperationError>.Success(_repository.GetUsersWithRoleAssignment(role));
        }

        public Result<IQueryable<User>, OperationError> GetUsersInOrganization(Guid organizationUuid, params IDomainQuery<User>[] queries)
        {
            return _organizationService
                .GetOrganization(organizationUuid, OrganizationDataReadAccessLevel.All)
                .Bind(organization =>
                {
                    var query = new IntersectionQuery<User>(queries);

                    return _repository
                        .GetUsersInOrganization(organization.Id)
                        .Transform(query.Apply)
                        .Transform(Result<IQueryable<User>, OperationError>.Success);
                });
        }

        public Result<User, OperationError> GetUserInOrganization(Guid organizationUuid, Guid userUuid)
        {
            return GetUsersInOrganization(organizationUuid)
                .Select(x => x.ByUuid(userUuid).FromNullable())
                .Bind(user =>
                    user.Match<Result<User, OperationError>>
                    (
                        foundUser => foundUser,
                        () => new OperationError("User is not member of the organization", OperationFailure.NotFound)
                    )
                );
        }

        public Result<IQueryable<User>, OperationError> SearchAllKitosUsers(params IDomainQuery<User>[] queries)
        {
            if (_authorizationContext.GetCrossOrganizationReadAccess() < CrossOrganizationDataReadAccessLevel.All)
            {
                return Result<IQueryable<User>, OperationError>.Failure(OperationFailure.Forbidden);
            }

            var query = new IntersectionQuery<User>(queries);

            return _repository
                .GetUsers()
                .Where(x => !x.Deleted)
                .Transform(query.Apply)
                .Transform(Result<IQueryable<User>, OperationError>.Success);
        }

        public Result<UserAdministrationPermissions, OperationError> GetAdministrativePermissions(Guid organizationUuid)
        {
            return _organizationService
                .GetOrganization(organizationUuid, OrganizationDataReadAccessLevel.All)
                .Select(organization => new UserAdministrationPermissions(AllowDelete(organization.Id)));
        }

        private bool AllowDelete(int? optionalOrganizationScopeId)
        {
            return _authorizationContext.HasPermission(new DeleteAnyUserPermission(optionalOrganizationScopeId.FromNullableValueType()));
        }

        public Maybe<OperationError> DeleteUser(Guid userUuid, Guid scopedToOrganizationUuid)
        {
            var orgDbId = _identityResolver.
        };

        public Maybe<OperationError> DeleteUser(Guid userUuid, int? scopedToOrganizationId = null)
        {
            var hasOrganizationIdValue = scopedToOrganizationId.HasValue;

            if (AllowDelete(scopedToOrganizationId))
            {
                var user = _userRepository.AsQueryable().ByUuid(userUuid);
                if (user == null)
                {
                    return new OperationError($"User with Uuid {userUuid} was not found", OperationFailure.NotFound);
                }

                if (_organizationalUserContext.UserId == user.Id)
                {
                    return new OperationError("You cannot delete a user you are currently logged in as", OperationFailure.Forbidden);
                }

                using var transaction = _transactionManager.Begin();
                var result = hasOrganizationIdValue
                    ? DeleteUser(scopedToOrganizationId.Value, user)
                    : DeleteUserFromKitos(user);

                if (result.HasValue)
                {
                    transaction.Rollback();
                }
                else
                {
                    _userRepository.Save();
                    transaction.Commit();

                    _domainEvents.Raise(new AdministrativeAccessRightsChanged(user.Id));
                }

                return result;
            }

            return new OperationError(OperationFailure.Forbidden);
        }

        private Maybe<OperationError> DeleteUserFromKitos(User userToDelete)
        {
            return _commandBus.Execute<RemoveUserFromKitosCommand, Maybe<OperationError>>(new RemoveUserFromKitosCommand(userToDelete));
        }

        private Maybe<OperationError> DeleteUser(int scopedToOrganizationId, User userToDelete)
        {
            var deletionStrategy = GetOrganizationalUserDeletionStrategy(scopedToOrganizationId, userToDelete);

            if (deletionStrategy.Failed)
                return deletionStrategy.Error;

            return deletionStrategy.Value switch
            {
                OrganizationalUserDeletionStrategy.Global =>
                    DeleteUserFromKitos(userToDelete),
                OrganizationalUserDeletionStrategy.Local => _commandBus
                    .Execute<RemoveUserFromOrganizationCommand, Maybe<OperationError>>(new RemoveUserFromOrganizationCommand(userToDelete, scopedToOrganizationId)),
                _ =>
                    new OperationError(OperationFailure.Forbidden)
            };
        }

        private static Result<OrganizationalUserDeletionStrategy, OperationError> GetOrganizationalUserDeletionStrategy(int orgId, User userToDelete)
        {
            if (userToDelete.Deleted)
                return new OperationError("User is already deleted", OperationFailure.BadState);
            var organizationIds = userToDelete.GetOrganizationIds().ToList();

            var memberOfTargetOrganization = organizationIds.Contains(orgId);
            if (!memberOfTargetOrganization)
                return new OperationError("User is part of the current organization", OperationFailure.BadInput);

            var memberOfMoreOrganizations = organizationIds.Count > 1;
            if (memberOfMoreOrganizations)
                return OrganizationalUserDeletionStrategy.Local;

            return userToDelete.IsGlobalAdmin
                ? OrganizationalUserDeletionStrategy.Local //Global admins are not automatically removed from kitos when removed from the last organization
                : OrganizationalUserDeletionStrategy.Global;
        }
    }
}
