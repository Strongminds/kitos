using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace UserSync.Transport;

public sealed record UserChangeContract(UserDeletionEvent Payload)
{
    public const string Version = "kitos.user-deletion.v1";

    public static UserDeletionEvent Normalize(UserDeletionEvent message)
    {
        if (!message.IsValid()) throw new ArgumentException("Invalid deletion event.", nameof(message));
        return message with
        {
            OccurredAt = message.OccurredAt is { } time
                ? new DateTime(time.Ticks - time.Ticks % 10, DateTimeKind.Utc) : null
        };
    }

    public static string Hash(UserDeletionEvent message)
    {
        var normalized = Normalize(message);
        // Fixed field order, GUID casing, UTC precision and invariant numbers; no transport formatting.
        var canonical = string.Join("\n", normalized.OrganizationUuid.ToString("D"),
            normalized.ExternalUserUuid.ToString("D"), normalized.ChangeType.ToString(CultureInfo.InvariantCulture),
            normalized.OccurredAt?.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture) ?? "null");
        return "v1:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static byte[] Serialize(UserDeletionEvent message) => JsonSerializer.SerializeToUtf8Bytes(new UserChangeContract(Normalize(message)));

    public static UserDeletionEvent Read(ReadOnlySpan<byte> body)
    {
        var contract = JsonSerializer.Deserialize<UserChangeContract>(body);
        return Normalize(contract?.Payload ?? throw new ArgumentException("Missing payload."));
    }
}
