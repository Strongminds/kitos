using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.Response.System;
using Presentation.Web.Models.API.V2.Response.SystemUsage;
using Tests.Integration.Presentation.Web.Tools;
using Xunit;

namespace Tests.Integration.Presentation.Web.SystemUsage.V2
{
    public class ItSystemUsageArchivesV2ApiTest : BaseItSystemUsageApiV2Test
    {
        [Fact]
        public async Task Can_Archive_SystemUsage_With_Valid_UUID_And_DTO()
        {
            // Arrange
            var (token, _, organization, system) = await CreatePrerequisitesAsync();
            var systemUsage = await TakeSystemIntoUsageAsync(system.Uuid, organization.Uuid);
            var archivingDate = A<DateTime>();
            var referenceName = A<string>();
            var note = A<string>();

            var request = new CreateItSystemUsageArchiveRequestDTO
            {
                ArchivingDate = archivingDate,
                ReferenceName = referenceName,
                Note = note
            };

            // Act
            using var response = await SendArchiveSystemUsageAsync(token, systemUsage.Uuid, request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var archive = await response.ReadResponseBodyAsAsync<ItSystemArchiveResponseDTO>();
            Assert.NotNull(archive);
            Assert.Equal(archivingDate, archive.ArchivingDate);
            Assert.Equal(referenceName, archive.ReferenceName);
            Assert.Equal(note, archive.Note);
            Assert.Equal(organization.Uuid, archive.Organization.Uuid);
        }

        [Fact]
        public async Task Cannot_Archive_SystemUsage_With_Invalid_UUID()
        {
            // Arrange
            var (token, _, _, _) = await CreatePrerequisitesAsync();
            var invalidUuid = Guid.NewGuid();
            var request = new CreateItSystemUsageArchiveRequestDTO
            {
                ArchivingDate = A<DateTime>(),
                ReferenceName = A<string>(),
                Note = A<string>()
            };

            // Act
            using var response = await SendArchiveSystemUsageAsync(token, invalidUuid, request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Cannot_Archive_SystemUsage_With_Missing_DTO_Properties()
        {
            // Arrange
            var (token, _, organization, system) = await CreatePrerequisitesAsync();
            var systemUsage = await TakeSystemIntoUsageAsync(system.Uuid, organization.Uuid);
            
            // Null ArchivingDate - required property
            var request = new CreateItSystemUsageArchiveRequestDTO
            {
                ArchivingDate = default(DateTime),
                ReferenceName = A<string>(),
                Note = A<string>()
            };

            // Act
            using var response = await SendArchiveSystemUsageAsync(token, systemUsage.Uuid, request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Cannot_Archive_SystemUsage_With_Empty_ReferenceName()
        {
            // Arrange
            var (token, _, organization, system) = await CreatePrerequisitesAsync();
            var systemUsage = await TakeSystemIntoUsageAsync(system.Uuid, organization.Uuid);

            var request = new CreateItSystemUsageArchiveRequestDTO
            {
                ArchivingDate = A<DateTime>(),
                ReferenceName = string.Empty,
                Note = A<string>()
            };

            // Act
            using var response = await SendArchiveSystemUsageAsync(token, systemUsage.Uuid, request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<HttpResponseMessage> SendArchiveSystemUsageAsync(
            string token,
            Guid systemUsageUuid,
            CreateItSystemUsageArchiveRequestDTO request)
        {
            var url = TestEnvironment.CreateUrl($"api/v2/it-system-usages/archives/{systemUsageUuid:D}");
            return await HttpApi.PostWithTokenAsync(url, request, token);
        }

        private async Task<(string token, User user, ShallowOrganizationResponseDTO organization, ItSystemResponseDTO system)> CreatePrerequisitesAsync()
        {
            var organization = await CreateOrganizationAsync();
            var (user, token) = await CreateApiUser(organization.Uuid);
            await HttpApi.SendAssignRoleToUserAsync(user.Uuid, OrganizationRole.LocalAdmin, organization.Uuid).DisposeAsync();
            var system = await CreateItSystemAsync(organization.Uuid);
            return (token, user, organization, system);
        }
    }
}
