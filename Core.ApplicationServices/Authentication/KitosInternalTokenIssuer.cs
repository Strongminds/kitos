using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Authentication;
using Core.DomainModel;
using Core.DomainServices.Queries.User;

namespace Core.ApplicationServices.Authentication;

public class KitosInternalTokenIssuer : IKitosInternalTokenIssuer
{
    private readonly IUserService _userService;
    private readonly ITokenValidator _tokenValidator;

    public KitosInternalTokenIssuer(IUserService userService, ITokenValidator tokenValidator)
    {
        _userService = userService;
        _tokenValidator = tokenValidator;
    }

    public Result<KitosApiToken, OperationError> GetToken()
    {
        return _userService.GetGlobalAdmin()
            .Select(_tokenValidator.CreateToken);
    }
}