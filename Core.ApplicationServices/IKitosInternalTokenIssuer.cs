using Core.ApplicationServices.Model.Authentication;

namespace Core.ApplicationServices;

public interface IKitosInternalTokenIssuer
{
    KitosApiToken GetToken();
}