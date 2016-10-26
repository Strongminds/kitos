﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace Presentation.Web.Controllers.API
{
    public class UploadFileController : BaseApiController
    {
        public HttpResponseMessage Post()
        {

            var context = HttpContext.Current.Request;

            var file = context.Files[0];
            var savePath = HttpContext.Current.Server.MapPath("~/Content/excel/");

            var fileExtension = file.FileName.Substring(file.FileName.IndexOf("."));

            try
            {
                file.SaveAs(savePath + "Kontrakt_Indgåelse_Skabelon" + fileExtension);
            }
            catch (Exception e) {
                return CreateResponse(System.Net.HttpStatusCode.InternalServerError,e);
            }

            return Ok();
        }
    }
}