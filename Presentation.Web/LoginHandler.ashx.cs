using System.Web;

namespace Presentation.Web
{
    /// <summary>
    /// Summary description for LoginHandler
    /// </summary>
    public class LoginHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Redirect("Login.ashx?forceAuthn=true");
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}