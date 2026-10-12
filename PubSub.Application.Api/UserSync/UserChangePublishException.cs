namespace PubSub.Application.Api.UserSync;

public sealed class UserChangePublishException(Exception inner) : Exception("User change publish was not confirmed and routed.", inner);
