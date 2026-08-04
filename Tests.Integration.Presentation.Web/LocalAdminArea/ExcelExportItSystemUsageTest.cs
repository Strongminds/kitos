using System;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Tests.Integration.Presentation.Web.Tools;
using Xunit;
using OrganizationType = Presentation.Web.Models.API.V2.Types.Organization.OrganizationType;

namespace Tests.Integration.Presentation.Web.LocalAdminArea
{
    public class ExcelExportItSystemUsageTest : BaseTest
    {
        private const string ExcelApiPrefix = "api/excel";

        [Fact]
        public async Task Can_Export_ItSystemUsage_As_Excel_By_Uuid()
        {
            //Arrange
            var organization = await CreateOrganizationAsync(type: OrganizationType.Municipality);
            var systemName = A<string>();
            var system = await CreateItSystemAsync(organization.Uuid, name: systemName);
            var usage = await TakeSystemIntoUsageAsync(system.Uuid, organization.Uuid);
            var (_, _, loginCookie) = await HttpApi.CreateUserAndLogin(
                CreateEmail(),
                OrganizationRole.LocalAdmin,
                organization.Uuid);

            //Act
            using var response = await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl($"{ExcelApiPrefix}/{usage.Uuid}"),
                loginCookie);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                response.Content.Headers.ContentType?.MediaType);

            var contentDisposition = response.Content.Headers.ContentDisposition;
            Assert.NotNull(contentDisposition);
            var fileName = contentDisposition.FileNameStar ?? contentDisposition.FileName;
            Assert.Contains(systemName, fileName?.Trim('"'));

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public async Task Cannot_Export_ItSystemUsage_When_Usage_Uuid_Does_Not_Exist()
        {
            //Arrange
            var organization = await CreateOrganizationAsync(type: OrganizationType.Municipality);
            var (_, _, loginCookie) = await HttpApi.CreateUserAndLogin(
                CreateEmail(),
                OrganizationRole.LocalAdmin,
                organization.Uuid);

            //Act
            using var response = await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl($"{ExcelApiPrefix}/{Guid.NewGuid()}"),
                loginCookie);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Cannot_Export_ItSystemUsage_In_Another_Organization_Without_Access()
        {
            //Arrange
            var ownerOrganization = await CreateOrganizationAsync(type: OrganizationType.Municipality);
            var otherOrganization = await CreateOrganizationAsync(type: OrganizationType.Municipality);

            var system = await CreateItSystemAsync(ownerOrganization.Uuid);
            var usage = await TakeSystemIntoUsageAsync(system.Uuid, ownerOrganization.Uuid);

            var (_, _, loginCookie) = await HttpApi.CreateUserAndLogin(
                CreateEmail(),
                OrganizationRole.LocalAdmin,
                otherOrganization.Uuid);

            //Act
            using var response = await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl($"{ExcelApiPrefix}/{usage.Uuid}"),
                loginCookie);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
