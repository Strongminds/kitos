using System.Net.Http;
using System.Web.Http;
using Core.DomainServices.FKOrganization;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V1
{
    //TODO: Remove this
    [InternalApi]
    [RoutePrefix("api/v1/test")]
    public class TestController : BaseApiController
    {
        private readonly IFKOrganizationUnitImportService _importService;

        public TestController(IFKOrganizationUnitImportService importService)
        {
            _importService = importService;
        }

        [HttpGet]
        [Route("")]
        public HttpResponseMessage Get()
        {
            _importService.ImportOrganizationTree(1);
            return Ok();
        }
    }
}