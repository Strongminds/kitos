namespace UserSync.Transport;

public sealed class UserChangePublishException(Exception inner) : Exception("User change publish was not confirmed and routed.", inner);
