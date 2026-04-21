using System.IO;
using System.Net;
using System.Xml.Linq;

namespace Infrastructure.Services.KLEDataBridge
{
    public class KLEDataBridge : IKLEDataBridge
    {
        private readonly string _kleOnlineUrl;

        public KLEDataBridge(string kleOnlineUrl)
        {
            _kleOnlineUrl = kleOnlineUrl;
        }

        public XDocument GetAllActiveKleNumbers()
        {
            using var client = new WebClient();
            using var stream = client.OpenRead(_kleOnlineUrl.TrimEnd('/') + "/emneplan");
            using var reader = new StreamReader(stream);
            return XDocument.Load(reader);
        }
    }
}
