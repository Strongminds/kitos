using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainModel.PublicMessage;
using Presentation.Web.Controllers.API.V2.Internal.Messages.Mapping;
using Presentation.Web.Models.API.V2.Internal.Response;
using Presentation.Web.Models.API.V2.Response.Shared;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Integration.Presentation.Web.Internal.Messages
{
    public class PublicMessagesApiV2Test : WithAutoFixture
    {
        private readonly Uri _rootUrl = TestEnvironment.CreateUrl("api/v2/internal/public-messages");

        [Fact]
        public async Task Can_GET()
        {
            //Arrange
            var texts = ChangePublicMessages();

            //Act
            using var response = await HttpApi.GetAsync(_rootUrl);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var messages = await response.ReadResponseBodyAsAsync<PublicMessagesResponseDTO>();
            Assert.Equivalent(texts, messages);
        }

        [Theory]
        [InlineData(OrganizationRole.LocalAdmin, false)]
        [InlineData(OrganizationRole.User, false)]
        [InlineData(OrganizationRole.GlobalAdmin, true)]
        public async Task Can_GET_Permissions(OrganizationRole role, bool allowModify)
        {
            //Arrange
            var cookie = await HttpApi.GetCookieAsync(role);

            //Act
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl("api/v2/internal/public-messages/permissions"), cookie);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var permissions = await response.ReadResponseBodyAsAsync<ResourcePermissionsResponseDTO>();
            Assert.True(permissions.Read);      // All users can read
            Assert.False(permissions.Delete);   // No one can delete
            Assert.Equal(allowModify, permissions.Modify);
        }

        [Theory]
        [InlineData(OrganizationRole.LocalAdmin)]
        [InlineData(OrganizationRole.User)]
        public async Task Cannot_PATCH_If_Not_Global_Admin(OrganizationRole role)
        {
            //Arrange
            var cookie = await HttpApi.GetCookieAsync(role);
            var expected = ChangePublicMessages();
            var newText = A<string>();
            expected.LongDescription = newText;

            //Act
            using var patchResponse = await HttpApi.PatchWithCookieAsync(_rootUrl, cookie, new
            {
                About = newText
            });

            //Assert that only changed property was actually changed
            Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);
        }

        [Fact]
        public async Task Can_PATCH()
        {
            //Arrange
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var expected = ChangePublicMessages();
            expected.Link = A<string>();
            expected.LongDescription = A<string>();
            expected.ShortDescription = A<string>();
            expected.Status = A<PublicMessageStatusChoice>();

            //Act
            using var patchResponse = await HttpApi.PatchWithCookieAsync(_rootUrl, cookie, expected);

            //Assert that only changed property was actually changed
            await AssertPatchSucceeded(patchResponse, expected);
        }

        private PublicMessagesResponseDTO ChangePublicMessages()
        {
            var expectedResponse = new PublicMessagesResponseDTO();
            DatabaseAccess.MutateEntitySet<PublicMessage>(textsRepo =>
            {
                var text = textsRepo.AsQueryable().First();
                text.LongDescription = Guid.NewGuid().ToString();
                text.ShortDescription = Guid.NewGuid().ToString();
                text.Link = Guid.NewGuid().ToString();
                text.Status = A<PublicMessageStatus>();

                expectedResponse = new PublicMessagesResponseDTO
                {
                    LongDescription = text.LongDescription,
                    ShortDescription = text.ShortDescription,
                    Link = text.Link,
                    Status = text.Status?.ToPublicMessageStatusChoice()
                };
            });
            return expectedResponse;
        }

        private static async Task AssertPatchSucceeded(HttpResponseMessage patchResponse, PublicMessagesResponseDTO expected)
        {
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
            var changedMessages = await patchResponse.ReadResponseBodyAsAsync<PublicMessagesResponseDTO>();
            Assert.Equivalent(expected, changedMessages);
        }
    }
}
