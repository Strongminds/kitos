using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Controllers.API.V2
{
	public class SubscribeRequestWithTokenDTO
	{
        public SubscriptionDTO Subscription { get; set; }
        public string Token { get; set; }
    }
}