using Core.Abstractions.Types;
using Core.ApplicationServices.SystemUsage.GDPR;
using Core.DomainModel.ItSystemUsage.GDPR;
using Core.DomainModel.Organization;
using Core.DomainServices.Generic;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Presentation.Web.Controllers.API.V2.Internal
{

    [RoutePrefix("api/v2/internal/gdpr-report")]
    public class GdprExportReportInternalV2Controller : InternalApiV2Controller
    {
        private readonly IGDPRExportService _gdprExportService;
        private readonly IEntityIdentityResolver _entityIdentityResolver;

        public GdprExportReportInternalV2Controller(IGDPRExportService gdprExportService, IEntityIdentityResolver entityIdentityResolver)
        {
            _gdprExportService = gdprExportService;
            _entityIdentityResolver = entityIdentityResolver;
        }

        [HttpGet]
        [Route("{organizationUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(object))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetGdprReport([FromUri][NonEmptyGuid] Guid organizationUuid)
        {
            return _entityIdentityResolver.ResolveDbId<Organization>(organizationUuid)
                    .Match(_gdprExportService.GetGDPRData, () => new OperationError($"Cannot find organization with uuid {organizationUuid}", OperationFailure.NotFound))
                    .Select(MapGdprDataToDTO)
                    .Match(Ok, FromOperationError);
        }

        private IEnumerable<object> MapGdprDataToDTO(IEnumerable<GDPRExportReport> gdprData)
        {
            return gdprData.Select(MapGdprReportToDTO).ToList();
        }

        private object MapGdprReportToDTO(GDPRExportReport gdprReport)
        {
            return gdprReport;
        }
    }
}