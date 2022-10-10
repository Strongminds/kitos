﻿using System;
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
using Core.ApplicationServices.Organizations;
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
        private static readonly RNGCryptoServiceProvider rngCsp = new();
        private const string KitosManualsLink = "https://os2.eu/Kitosvejledning";

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
            IOrganizationalUserContext organizationalUserContext)
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

            _domainEvents.Raise(new EntityBeingDeletedEvent<User>(savedUser));

            if (sendMailOnCreation)
                IssueAdvisMail(savedUser, false, orgId);

            return savedUser;
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
                          "<p><a href='" + KitosManualsLink + "'>Klik her for at få Hjælp til log ind og brugerkonto</a></p>" +
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
                              "<p><a href='" + KitosManualsLink + "'>Klik her for at få Hjælp til log ind og brugerkonto</a></p>" +
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

        public Maybe<OperationError> DeleteUserFromKitos(Guid userUuid, int? scopedToOrganizationId = null)
        {
            var user = _userRepository.AsQueryable().ByUuid(userUuid);
            if (user == null)
                return new OperationError(OperationFailure.NotFound);
            if(_organizationalUserContext.UserId == user.Id)
                return new OperationError("You cannot delete a user you are currently logged in as", OperationFailure.Forbidden);

            if (scopedToOrganizationId.HasValue && scopedToOrganizationId.Value != 0)
                return DeleteUserFromOrganizationOrKitos(scopedToOrganizationId.Value, user);
            
            return _authorizationContext.AllowDelete(user) 
                ? DeleteUser(user) 
                : new OperationError(OperationFailure.Forbidden);
        }

        private Maybe<OperationError> DeleteUser(User user)
        {
            using var transaction = _transactionManager.Begin();
            
            _domainEvents.Raise(new EntityBeingDeletedEvent<User>(user));

            Delete(user);
            _userRepository.Save();
            transaction.Commit();

            _domainEvents.Raise(new AdministrativeAccessRightsChanged(user.Id));

            return Maybe<OperationError>.None;
        }

        private Maybe<OperationError> DeleteUserFromOrganizationOrKitos(int scopedToOrganizationId, User user)
        {
            var deletionStrategy = GetUserDeletionStrategy(scopedToOrganizationId, user);
            if (deletionStrategy.Failed)
                return deletionStrategy.Error;

            switch (deletionStrategy.Value)
            {
                case UserDeletionStrategy.Global:
                    var organization = _orgRepository.AsQueryable().ById(scopedToOrganizationId);
                    return _authorizationContext.AllowModify(organization) 
                        ? DeleteUser(user) 
                        : new OperationError(OperationFailure.Forbidden);
                case UserDeletionStrategy.Local:
                    _domainEvents.Raise(new EntityBeingRemovedEvent<User>(user, scopedToOrganizationId));
                    return Maybe<OperationError>.None;
                default:
                    return new OperationError(OperationFailure.Forbidden);
            }
        }

        private static void Delete(User user)
        {
            user.LockedOutDate = DateTime.Now;
            user.Name = "Slettet bruger";
            user.Email = $"{Guid.NewGuid()}_deleted_user@kitos.dk";
            user.PhoneNumber = null;
            user.LastName = "";
            user.DeletedDate = DateTime.Now;
            user.Deleted = true;
            user.IsGlobalAdmin = false;
            user.HasApiAccess = false;
            user.HasStakeHolderAccess = false;
        }

        private Result<UserDeletionStrategy, OperationError> GetUserDeletionStrategy(int orgId, User user)
        {
            if (user.Deleted)
                return new OperationError(OperationFailure.BadInput);
            if (user.OrganizationRights.Any(x => x.OrganizationId == orgId) == false)
                return new OperationError(OperationFailure.BadInput);

            if (user.OrganizationRights.Any(x => x.OrganizationId != orgId))
                return UserDeletionStrategy.Local;
            if (!_organizationalUserContext.HasRoleInSameOrganizationAs(user))
                return UserDeletionStrategy.Local;
            if (_organizationalUserContext.HasRole(orgId, OrganizationRole.LocalAdmin))
                return user.IsGlobalAdmin
                    ? UserDeletionStrategy.Local
                    : UserDeletionStrategy.Global;

            return _organizationalUserContext.HasRole(orgId, OrganizationRole.GlobalAdmin)
                ? UserDeletionStrategy.Global
                : new OperationError(OperationFailure.Forbidden);
        }
    }
}
