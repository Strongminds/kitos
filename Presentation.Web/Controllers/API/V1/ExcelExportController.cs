using System;
using System.IO;
using Core.ApplicationServices;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V1
{
    [InternalApi]
    [Route("api/excel")]
    public class ExcelExportController : BaseApiController
    {
        private readonly IExcelService _excelService;

        public ExcelExportController(IExcelService excelService)
        {
            _excelService = excelService;
        }

        [HttpGet]
        [Route("it-system-usage/{systemUsageUuid}")]
        public IActionResult GetItSystemUsageByUuid([NonEmptyGuid][FromRoute] Guid systemUsageUuid)
        {
            var stream = new MemoryStream();
            return _excelService.ExportItSystemUsage(stream, systemUsageUuid)
                .Match(
                    result => GetFileResult(result.stream, result.fileName),
                    FromOperationError
                );
        }

        private static IActionResult GetFileResult(Stream stream, string filename)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = filename
            };
        }
    }
}
