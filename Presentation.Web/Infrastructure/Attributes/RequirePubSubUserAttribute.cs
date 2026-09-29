using Core.ApplicationServices.Authentication;
using Core.DomainServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Web.Infrastructure.Attributes;

/// <summary>Checks current database permissions so revocation also affects existing tokens.</summary>
public class RequirePubSubUserAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var authentication = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationContext>();
        if (authentication.Method != AuthenticationMethod.KitosToken || !authentication.UserId.HasValue)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var user = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>()
            .GetById(authentication.UserId.Value);
        if (user == null || !user.CanAuthenticate() || user.HasApiAccess != true || !user.IsPubSubUser)
            context.Result = new ForbidResult();
    }
}
