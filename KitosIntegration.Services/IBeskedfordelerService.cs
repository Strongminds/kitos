using Kitos.Integration.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KitosIntegration.Services;

public interface IBeskedfordelerService
{
    Task<BeskedfordelerStatus> Receive(Stream requestBody, CancellationToken cancellation, string? traceId = null);
}
