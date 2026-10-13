using Microsoft.AspNetCore.Authorization;
using Presentation.Web.Infrastructure.Authentication;

namespace Presentation.Web.Infrastructure.Attributes;

public sealed class IntegrationApiAttribute : AuthorizeAttribute
{
    public IntegrationApiAttribute()
    {
        AuthenticationSchemes = IntegrationCertificateAuthenticationHandler.SchemeName;
    }
}
