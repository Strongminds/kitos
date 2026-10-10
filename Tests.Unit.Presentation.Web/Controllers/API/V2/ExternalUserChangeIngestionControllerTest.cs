using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using Core.Abstractions.Types;
using Core.ApplicationServices.Users;
using Core.DomainModel.Organization;
using Core.DomainModel.SSO;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Web.Controllers.API.V2.Integration;
using Presentation.Web.Models.API.V2.Integration.Response;
using Xunit;

namespace Tests.Unit.Presentation.Web.Controllers.API.V2;

public class ExternalUserChangeIngestionControllerTest
{
    [Fact]
    public async Task PubSubEnvelopeCreatesPendingChange()
    {
        var organization = new Organization { Id = 7, Uuid = Guid.NewGuid(), FkOrgUsersConnected = true };
        var organizations = new Mock<IOrganizationRepository>();
        organizations.Setup(x => x.GetByUuid(organization.Uuid)).Returns(organization);
        var identities = new Mock<ISsoUserIdentityRepository>();
        identities.Setup(x => x.GetByExternalUuid(It.IsAny<Guid>())).Returns(Maybe<SsoUserIdentity>.None);
        var repository = new Mock<IExternalUserChangeRepository>();
        ExternalUserChange? saved = null;
        repository.Setup(x => x.Insert(It.IsAny<ExternalUserChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalUserChange change, CancellationToken _) => { saved = change; return change; });
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            Payload = new
            {
                ExternalMessageId = "token-event",
                OrganizationUuid = organization.Uuid,
                ExternalUserUuid = Guid.NewGuid(),
                ChangeType = 1,
                OccurredAt = (DateTime?)null
            }
        });
        var controller = Create(body, new ExternalUserChangeIngestionService(repository.Object, organizations.Object, identities.Object));
        var response = Assert.IsType<OkObjectResult>(await controller.Receive(default));
        Assert.IsType<ExternalUserChangeIngestionResponse>(response.Value);
        Assert.NotNull(saved);
        Assert.Equal(ExternalUserChangeStatus.Pending, saved.Status);
        Assert.Equal("token-event", saved.ExternalMessageId);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"Payload\":null}")]
    public async Task MalformedPayloadIsRejected(string body)
    {
        Assert.IsType<BadRequestObjectResult>(await Create(body).Receive(default));
    }

    private static ExternalUserChangeIngestionController Create(string body, ExternalUserChangeIngestionService? service = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        // Any unexpected ingestion invocation fails this test; no application service is needed for rejected calls.
        return new ExternalUserChangeIngestionController(service!) { ControllerContext = new ControllerContext { HttpContext = context } };
    }
}
