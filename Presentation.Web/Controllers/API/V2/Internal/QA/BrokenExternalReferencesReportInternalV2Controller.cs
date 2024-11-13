using System.Linq;
using System.Net;
using Core.ApplicationServices.Qa;
using System.Web.Http;
using Core.Abstractions.Types;
using Core.DomainModel.Qa.References;
using Presentation.Web.Models.API.V2.Internal.Response.QA;

namespace Presentation.Web.Controllers.API.V2.Internal.QA
{
    [RoutePrefix("api/v2/internal/broken-external-references-report")]
    public class BrokenExternalReferencesReportInternalV2Controller : InternalApiV2Controller
    {
        private readonly IBrokenExternalReferencesReportService _brokenExternalReferencesReportService;

        public BrokenExternalReferencesReportInternalV2Controller(IBrokenExternalReferencesReportService brokenExternalReferencesReportService)
        {
            _brokenExternalReferencesReportService = brokenExternalReferencesReportService;
        }

        [HttpGet]
        [Route("status")]
        public IHttpActionResult GetStatus()
        {
            return _brokenExternalReferencesReportService
                .GetLatestReport()
                .Match
                (
                    report => Ok(MapStatus(report)),
                    error => error.FailureType == OperationFailure.NotFound
                        ? Ok(MapStatus(Maybe<BrokenExternalReferencesReport>.None))
                        : FromOperationError(error)
                );
        }

        [HttpPost]
        [Route("trigger")]
        public IHttpActionResult Trigger()
        {
            return _brokenExternalReferencesReportService
                .TriggerReportGeneration()
                .Match
                (
                    FromOperationError,
                    () => StatusCode(HttpStatusCode.Accepted)
                );
        }

        [HttpGet]
        [Route("current/csv")]
        public IHttpActionResult GetCurrentCsvReport()
        {
            return _brokenExternalReferencesReportService
                .GetLatestReport()
                .Match
                (
                    BrokenExternalReferencesReportCsvMapperV2.CreateReportCsvResponse,
                    FromOperationError
                );
        }

        private IHttpActionResult MapStatus(Maybe<BrokenExternalReferencesReport> reportMaybe)
        {
            return reportMaybe
                .Match
                (
                    report => Ok(new BrokenExternalReferencesReportStatusResponseDTO
                    {
                        Available = true,
                        CreatedDate = report.Created,
                        BrokenLinksCount = report.GetBrokenLinks().Count()
                    }),
                    onNone: () => Ok(new BrokenExternalReferencesReportStatusResponseDTO
                    {
                        Available = false
                    })
                );
        }
    }
}