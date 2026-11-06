using System;
using System.Text;
using System.Threading.Tasks;
using KitosIntegration.Services;
using Kitos.Integration.Contracts;
using KitosIntegration.Mapping;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KitosIntegration.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/v2/integration/beskedfordeler")]
    public sealed class BeskedfordelerV2Controller(
        IBeskedfordelerService service, ILogger<BeskedfordelerV2Controller> logger) : ControllerBase
    {
        /// <summary>Validates a Beskedfordeler envelope and durably queues it for KITOS processing.</summary>
        [HttpPost]
        [Produces("application/xml")]
        public async Task<IActionResult> Receive()
        {
            var traceId = HttpContext.TraceIdentifier;
            logger.LogWarning("Beskedfordeler callback invoked. TraceId {TraceId}, ContentLength {ContentLength}.",
                traceId, Request.ContentLength);
            try
            {
                var type = Request.ContentType?.Split(';')[0].Trim();
                var supported = string.Equals(type, "application/xml", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(type, "text/xml", StringComparison.OrdinalIgnoreCase);
                logger.LogWarning("Beskedfordeler content type validation completed. TraceId {TraceId}, Supported {Supported}.",
                    traceId, supported);
                var status = BeskedfordelerStatus.CannotReceive;
                if (supported)
                {
                    logger.LogWarning("Beskedfordeler invoking receive service. TraceId {TraceId}.", traceId);
                    status = await service.Receive(Request.Body, HttpContext.RequestAborted, traceId);
                }
                else
                {
                    logger.LogWarning("Beskedfordeler callback rejected: unsupported or missing content type. TraceId {TraceId}.", traceId);
                }
                var response = Content(BeskedfordelerResponseMapper.Serialize(status), "application/xml", Encoding.UTF8);
                logger.LogWarning("Beskedfordeler callback response prepared. TraceId {TraceId}, StatusKode {StatusKode}.",
                    traceId, (int)status);
                return response;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Beskedfordeler callback cancelled. TraceId {TraceId}.", traceId);
                throw;
            }
            catch (Exception ex)
            {
                // Exception messages may contain request content or integration credentials.
                logger.LogWarning("Beskedfordeler callback failed. TraceId {TraceId}, ExceptionType {ExceptionType}.",
                    traceId, ex.GetType().Name);
                throw;
            }
        }
    }
}
