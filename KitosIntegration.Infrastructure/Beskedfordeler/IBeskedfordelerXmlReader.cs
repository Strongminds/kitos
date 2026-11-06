using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KitosIntegration.Infrastructure.Beskedfordeler;

public interface IBeskedfordelerXmlReader
{
    Task<XElement> Read(Stream body, CancellationToken cancellationToken);
}
