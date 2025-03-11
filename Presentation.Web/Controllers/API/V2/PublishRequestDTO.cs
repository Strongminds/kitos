using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Controllers.API.V2
{
	public class PublishRequestDTO
	{
        public string Topic { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }
}