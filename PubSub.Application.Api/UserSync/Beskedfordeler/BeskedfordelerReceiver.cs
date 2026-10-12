using System.Xml.Linq;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

public sealed class BeskedfordelerReceiver(IBeskedfordelerDeletionMapper mapper, UserChangeIngress ingress)
{
    public async Task<BeskedfordelerStatus> Receive(XElement input, CancellationToken cancellationToken)
    {
        var message = input.Element(BeskedfordelerResponse.Envelope + "Haendelsesbesked");
        var id = message?.Element(BeskedfordelerResponse.Envelope + "BeskedId")?
            .Element(BeskedfordelerResponse.Sagdok + "UUIDIdentifikator")?.Value;
        if (!Guid.TryParse(id, out var uuid) || uuid == Guid.Empty)
            return BeskedfordelerStatus.CannotReceive;
        if (message?.Element(BeskedfordelerResponse.Envelope + "BeskedVersion")?.Value != "1.0")
            return BeskedfordelerStatus.UnsupportedVersion;

        var mapped = mapper.Map(input);
        if (mapped == null) return BeskedfordelerStatus.NotImplemented;
        var deletion = mapped with { ExternalMessageId = uuid.ToString("D") };
        if (!deletion.IsValid()) return BeskedfordelerStatus.CannotReceive;

        try
        {
            // Success requires routed broker confirmation and committed receipt acceptance.
            await ingress.Enqueue(deletion, cancellationToken);
            return BeskedfordelerStatus.Accepted;
        }
        catch (UserChangeConflictException)
        {
            return BeskedfordelerStatus.CannotReceive;
        }
    }
}
