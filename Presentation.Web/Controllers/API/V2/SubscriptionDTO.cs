using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Controllers.API.V2
{
	public class SubscriptionDTO
	{
        public Uri Callback { get; set; }
        public IEnumerable<string> Topics { get; set; }
    }
}