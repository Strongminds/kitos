using Core.ApplicationServices.Model.Authentication;

namespace Core.ApplicationServices.Authentication;

public interface IKitosInternalTokenIssuer
{
    KitosApiToken GetToken();
}