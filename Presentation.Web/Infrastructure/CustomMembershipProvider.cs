﻿using System;
using System.Collections.Specialized;
using System.Web.Security;
using Core.DomainModel;
using Core.DomainServices;
using Ninject;
using Ninject.Extensions.Logging;

namespace Presentation.Web.Infrastructure
{
    public class CustomMembershipProvider : MembershipProvider
    {
        [Inject]
        public IUserRepositoryFactory UserRepositoryFactory { get; set; }

        [Inject]
        public ICryptoService CryptoService { get; set; }

        [Inject]
        public ILogger Logger { get; set; }

        private int _maxInvalidPasswordAttempts;
        private int _passwordAttemptWindow;

        #region not implemented

        public override string ApplicationName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password,
                                                             string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email,
                                                  string passwordQuestion, string passwordAnswer, bool isApproved,
                                                  object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override bool EnablePasswordReset
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EnablePasswordRetrieval
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize,
                                                                  out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize,
                                                                 out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotImplementedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override int MaxInvalidPasswordAttempts => _maxInvalidPasswordAttempts;
        public override int PasswordAttemptWindow => _passwordAttemptWindow;
        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
            _maxInvalidPasswordAttempts = Convert.ToInt32(config["maxInvalidPasswordAttempts"]);
            _passwordAttemptWindow = Convert.ToInt32(config["passwordAttemptWindow"]);
        }

        public override bool ValidateUser(string username, string password)
        {
            var userRepository = UserRepositoryFactory.GetUserRepository();
            var user = userRepository.GetByEmail(username);

            var isValid = false;

            if (user == null)
            {
                Logger.Info("{0} not found.", username);

                return isValid;
            }

            // having a LockedOutDate means that the user is locked out
            if (user.LockedOutDate != null)
            {
                var lastLockoutDate = user.LockedOutDate;
                var unlockDate = lastLockoutDate.Value.AddMinutes(PasswordAttemptWindow);

                // check if user should be allowed to login again
                if (DateTime.Now >= unlockDate)
                {
                    ResetLockedOutDate(user);
                    ResetAttempts(user);
                    Logger.Info("{0} has been unlocked.", user.Email);
                    isValid = CheckPassword(user, password);
                }
                else
                {
                    Logger.Info("{0} will be unlocked {unlockDate}.", user.Email, unlockDate);
                }

            }
            else
            {
                isValid = CheckPassword(user, password);
            }

            userRepository.Save();
            var userInfo = new { user.Email, user.FailedAttempts, user.LockedOutDate };
            Logger.Info("Current User: {userInfo}", userInfo);

            return isValid;
        }

        private bool CheckPassword(User user, string password)
        {
            var isValid = user.Password == CryptoService.Encrypt(password + user.Salt);

            if (isValid)
            {
                ResetAttempts(user);
            }
            else
            {
                user.FailedAttempts++;

                if (user.FailedAttempts >= MaxInvalidPasswordAttempts)
                {
                    user.LockedOutDate = DateTime.Now;
                    Logger.Info(MaxInvalidPasswordAttempts + " invalid login attempts. {0} has been locked.", user.Email);
                    ResetAttempts(user);
                }
            }

            return isValid;
        }

        private void ResetAttempts(User user)
        {
            user.FailedAttempts = 0;
        }

        private void ResetLockedOutDate(User user)
        {
            user.LockedOutDate = null;
        }
    }
}
