using System.Linq;
using Core.Abstractions.Extensions;
using Core.DomainModel;
using Core.DomainServices.Queries.User;
using NotImplementedException = System.NotImplementedException;

namespace Core.ApplicationServices;

public class KitosInternalTokenIssuer : IKitosInternalTokenIssuer
{
    private readonly IUserService _userService;
    
    public KitosInternalTokenIssuer(IUserService userService)
    {
        _userService = userService;
    }

    public object GetToken()
    {
        var 
       //return _tokenValidator.CreateToken(
       throw new NotImplementedException();
    }

    private User GetGlobalAdminUser()
    {
        return _userService.GetUsers(new QueryByGlobalAdmin()).FirstOrDefault();
    }
}