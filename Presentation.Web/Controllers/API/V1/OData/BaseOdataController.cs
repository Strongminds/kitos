﻿using System.Net.Http;
using System.Web.Http;
using Core.Abstractions.Types;
using Microsoft.AspNet.OData;
using Presentation.Web.Extensions;

namespace Presentation.Web.Controllers.API.V1.OData
{
    [Authorize]
    public class BaseOdataController : ODataController
    {
        protected IHttpActionResult FromOperationFailure(OperationFailure failure)
        {
            return FromOperationError(failure);
        }

        protected IHttpActionResult FromOperationError(OperationError failure)
        {
            var statusCode = failure.FailureType.ToHttpStatusCode();

            return ResponseMessage(new HttpResponseMessage(statusCode) {Content = new StringContent(failure.Message.GetValueOrFallback(statusCode.ToString("G")))});
        }
    }
}