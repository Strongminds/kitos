namespace PubSub.Application.Api.UserSync;

internal sealed record TokenResponse(string Token, DateTimeOffset Expires, bool LoginSuccessful);
