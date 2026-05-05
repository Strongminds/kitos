using System.Collections.Generic;

namespace Infrastructure.Services
{
    public interface IEndpointValidationConfiguration
    {
        IEnumerable<string> IgnoredProtocols { get; }
    }
}
