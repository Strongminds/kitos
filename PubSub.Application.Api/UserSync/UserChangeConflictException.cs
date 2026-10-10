namespace PubSub.Application.Api.UserSync;

public sealed class UserChangeConflictException() : InvalidOperationException("Message ID already belongs to another event.");
