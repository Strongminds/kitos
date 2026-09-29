using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Presentation.Web.Infrastructure.Attributes;

/// <summary>Token-authenticated integration API, excluded from API discovery.</summary>
public class IntegrationApiAttribute : AuthorizeAttribute, IApiDescriptionVisibilityProvider
{
    public IntegrationApiAttribute()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }

    public bool IgnoreApi => true;
}
