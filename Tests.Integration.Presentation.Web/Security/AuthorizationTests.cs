﻿using System;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.Model;
using Tests.Integration.Presentation.Web.Tools.XUnit;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Integration.Presentation.Web.Security
{
    [Collection(nameof(SequentialTestGroup))]
    public class AuthorizationTests : WithAutoFixture
    {
        private readonly KitosCredentials _regularApiUser, _globalAdmin;
        private readonly Uri _getTokenUrl;

        public AuthorizationTests()
        {
            _regularApiUser = TestEnvironment.GetCredentials(OrganizationRole.User, true);
            _globalAdmin = TestEnvironment.GetCredentials(OrganizationRole.GlobalAdmin);
            _getTokenUrl = TestEnvironment.CreateUrl("api/authorize/GetToken");
        }

        [Fact]
        public async Task Api_Access_User_Can_Get_Token()
        {
            //Arrange
            var loginDto = ObjectCreateHelper.MakeSimpleLoginDto(_regularApiUser.Username, _regularApiUser.Password);

            //Act
            var tokenResponse = await HttpApi.GetTokenAsync(loginDto);

            //Assert
            Assert.NotNull(tokenResponse);
            Assert.True(tokenResponse.LoginSuccessful);
            Assert.True(tokenResponse.Expires > DateTime.UtcNow);
            Assert.False(string.IsNullOrWhiteSpace(tokenResponse.Token));
        }

        [Fact]
        public async Task Api_Access_User_Cannot_Get_Cookie()
        {
            //Act
            using var cookieResponse = await HttpApi.SendGetCookieAsync(_regularApiUser);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, cookieResponse.StatusCode);
        }

        [Fact]
        public async Task User_Without_Api_Access_Can_Not_Get_Token()
        {
            //Arrange
            var url = TestEnvironment.CreateUrl("api/authorize/GetToken");
            var loginDto = ObjectCreateHelper.MakeSimpleLoginDto(_globalAdmin.Username, _globalAdmin.Password);

            //Act
            var tokenResponse = await HttpApi.PostAsync(url, loginDto);

            //Assert that unauthorized is returend .. token auth not enabled for the user
            Assert.Equal(HttpStatusCode.Unauthorized, tokenResponse.StatusCode);
        }

        [Fact]
        public async Task Get_Token_Returns_401_On_Invalid_Password()
        {
            //Arrange
            var loginDto = ObjectCreateHelper.MakeSimpleLoginDto(_globalAdmin.Username, A<string>());

            //Act
            using (var httpResponseMessage = await HttpApi.PostAsync(_getTokenUrl, loginDto))
            {
                //Assert
                Assert.Equal(HttpStatusCode.Unauthorized, httpResponseMessage.StatusCode);
            }
        }

        [Fact]
        public async Task Get_Token_Returns_401_On_Invalid_Username()
        {
            //Arrange
            var loginDto = ObjectCreateHelper.MakeSimpleLoginDto(A<string>(), A<string>());

            //Act
            using (var httpResponseMessage = await HttpApi.PostAsync(_getTokenUrl, loginDto))
            {
                //Assert
                Assert.Equal(HttpStatusCode.Unauthorized, httpResponseMessage.StatusCode);
            }
        }

        [Theory]
        [InlineData("api/authorize")]
        [InlineData("api/authorize/GetToken")]
        public async Task Too_Many_Failed_Login_Or_Get_Token_Attempts_Should_Eventually_Return_429(string route)
        {
            const int maxAttempts = 20;

            for (int i = 1; i <= maxAttempts; i++)
            {
                var loginDto = ObjectCreateHelper.MakeSimpleLoginDto(A<string>(), A<string>());
                using var response = await HttpApi.PostAsync(TestEnvironment.CreateUrl(route), loginDto);
                var statusCode = (int)response.StatusCode;

                if (statusCode == 429)
                {
                    return; //Test succeeded, so we return
                }
            }

            //If we get to this point, we never encountered the 429 status code.
            Assert.True(false, $"Expected a 429 after at most {maxAttempts} attempts, but all returned < 429.");
        }


    }
}
