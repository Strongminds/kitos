﻿using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Core.ApplicationServices;

namespace Presentation.Web.Controllers.API
{
    public class MoxController : BaseApiController
    {
        private readonly IMoxService _moxService;

        public MoxController(IMoxService moxService)
        {
            _moxService = moxService;
        }

        public HttpResponseMessage Get(int organizationId)
        {
            var dir = HttpContext.Current.Server.MapPath("~/Content/mox/");
            var file = File.OpenRead(dir + "OS2KITOS MOX Skabelon Organisation.xlsx");
            var stream = new MemoryStream();
            
            file.CopyTo(stream);
            const string filename = "OS2KITOS MOX Skabelon Organisation.xlsx";
            _moxService.Export(stream, organizationId, KitosUser);
            stream.Seek(0, SeekOrigin.Begin);
            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
            var mimeType = MimeMapping.GetMimeMapping(filename);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = filename
            };
            return result;
            
        }

        public async Task<HttpResponseMessage> Post(int organizationId)
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var provider = new MultipartMemoryStreamProvider();

            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                var file = provider.Contents.First();
                //var filename = Path.GetFileName(file.Headers.ContentDisposition.FileName);
                var buffer = await file.ReadAsByteArrayAsync();
                var stream = new MemoryStream(buffer);

                _moxService.Import(stream);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}
