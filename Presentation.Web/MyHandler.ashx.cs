using System.Web;

namespace Presentation.Web
{
    /// <summary>
    /// Summary description for MyHandler
    /// </summary>
    public class MyHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Redirect("Login.ashx");
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