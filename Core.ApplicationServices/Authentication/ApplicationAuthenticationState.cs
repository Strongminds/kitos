using System;
using Core.DomainModel;

namespace Core.ApplicationServices.Authentication
{
    public class ApplicationAuthenticationState : IApplicationAuthenticationState
    {
        public void SetAuthenticatedUser(User user, AuthenticationScope scope)
        {
            // Forms authentication cookie is set by the presentation layer (ASP.NET Core middleware).
            // This implementation is intentionally a no-op; the host layer overrides via IApplicationAuthenticationState.
            throw new NotSupportedException(
                "SetAuthenticatedUser must be implemented by the hosting layer (Presentation.Web).");
        }
    }
}
