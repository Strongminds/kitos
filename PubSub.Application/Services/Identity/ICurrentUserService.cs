using CSharpFunctionalExtensions;

namespace PubSub.Application.Services.Identity;

public interface ICurrentUserService
{
    Maybe<string> UserId { get; }
}