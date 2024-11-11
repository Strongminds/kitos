using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.HelpTexts;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Models.API.V2.Internal.Response;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.Internal
{
    [RoutePrefix("api/v2/internal/help-texts")]
    public class HelpTextsInternalV2Controller: InternalApiV2Controller
    {
        private readonly IHelpTextApplicationService _helpTextApplicationService;
        private readonly IHelpTextResponseMapper _responseMapper;

        public HelpTextsInternalV2Controller(IHelpTextApplicationService helpTextApplicationService, IHelpTextResponseMapper responseMapper)
        {
            _helpTextApplicationService = helpTextApplicationService;
            _responseMapper = responseMapper;
        }

        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<HelpTextResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult Get()
        {
            return _helpTextApplicationService.GetHelpTexts()
                .Select(_responseMapper.ToResponseDTOs)
                .Match(Ok, FromOperationError);
        }
    }
}