using System.Linq;
using Core.ApplicationServices.Authentication;
using Core.ApplicationServices.Model.Authentication;
using Core.DomainModel;
using Core.DomainServices.Queries.User;

namespace Core.ApplicationServices;

public class KitosInternalTokenIssuer : IKitosInternalTokenIssuer
{
    private readonly IUserService _userService;
    private readonly ITokenValidator _tokenValidator;

    public KitosInternalTokenIssuer(IUserService userService, ITokenValidator tokenValidator)
    {
        _userService = userService;
        _tokenValidator = tokenValidator;
    }

    public KitosApiToken GetToken()
    {
        var globalAdminUser = GetGlobalAdminUser();
        return _tokenValidator.CreateToken(globalAdminUser);
    }

    private User GetGlobalAdminUser()
    {
        return _userService.GetUsers(new QueryByGlobalAdmin()).FirstOrDefault();
    }
}