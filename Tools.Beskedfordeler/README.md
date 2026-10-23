# Beskedfordeler test sender

Publishes one `ORG_Bruger` event (`94ef3c82-10e9-4d12-a9d9-7a7242d5ba83`,
version `1.0`) with the immediate `BrugerNedlagt` action to KOMBIT's actual
Beskedfordeler using AMQP over TLS. A matching KITOS subscription queues this
business event and creates a pending deletion for administrator review.

Requires .NET 10, access to port 5671, a registered client certificate with its
private key, and an approved **SF1462 / BeskedAfsend** service agreement for the
municipality CVR and sender system. Receiving-only SF1461 access is insufficient.
Configure the receiving subscription to match ORG_Bruger 1.0, BrugerNedlagt and the CVR.

Copy `sts-config.example.json` to `sts-config.local.json` and supply the certificate
thumbprints for your environment. The tool reuses KITOS's `TokenFetcher` and
certificate loader (LocalMachine\My, falling back to CurrentUser\My). The STS issuer
and endpoint must match the environment. Only the client certificate and STS signing
certificate are configured; no separate WSP/Beskedfordeler service certificate is needed.
The client certificate is normally KITOS's `AppSettings:SsoCertificateThumbprint`;
the STS signing certificate is `AppSettings:StsCertificateThumbprint`.

Alternatively, the tool supports `SsoCertFilePath`, `SsoCertPassword`,
`StsCertFilePath`, and `StsCertPassword` for PFX files.
Use absolute paths. Keep local configuration and certificates out of source control;
prefer certificate-store access to storing passwords in a file.

Internally, the shared SOAP token library requires a non-null WSP certificate even
though token issuance does not use it. This AMQP-only tool supplies the STS certificate
for that unused constructor parameter. It never creates a WSP SOAP channel; AMQP
server identity is validated independently through TLS. Legacy `WspCertificateThumbprint`,
`StsOrganisationCertFilePath`, and `StsOrganisationCertPassword` settings are ignored.

From the repository root:

```powershell
dotnet run --project .\Tools.Beskedfordeler -- .\Tools.Beskedfordeler\sts-config.local.json <sender-system-uuid> <municipality-cvr> <fk-bruger-uuid>
```

The required fourth argument is the affected FK Bruger UUID (not a Person UUID or
the sender UUID). The optional fifth argument overrides the AMQP hostname. By default it targets
`beskedfordeler.eksterntest-stoettesystemerne.dk:5671`, virtual host `BF`.
There is no TLS certificate-validation bypass.

Each message creation generates fresh message and registration UUIDs, synthetic
Base64-encoded UTF-8 XML audit metadata containing the message UUID, and current UTC timestamps. It restricts
`TilladtModtager` and `ObjektAnsvarligMyndighed` to the supplied eight-digit CVR, and
sets `ObjektId` to the supplied FK Bruger UUID. The audit `Bruger` identifies the
sender, not the affected user. The synthetic audit metadata is for testing; it is
not a captured FK Organisation payload. The STS audience is
`http://entityid.kombit.dk/service/bfo_afsend/2`. Its SAML assertion is supplied
through SASL EXTERNAL and the AMQP `token` header.

The sender consumes the correlated direct reply on the publishing channel, uses
publisher confirmations, and exits successfully **only** for KOMBIT `StatusKode=20`.
AMQP connection/publication/reply operations have a 60-second deadline. Errors and
non-20 statuses return exit code 1 without printing tokens or raw responses.
Failures include nested exception type names (and socket error codes) to distinguish
network, TLS, and AMQP authentication errors without exposing exception messages.
A timeout can mean that KOMBIT accepted the event but its reply was lost; there is
no automatic retry. A rerun creates a new message.

Acceptance is not proof of callback delivery. For the KITOS receiver, verify durable
queue processing and the pending user change for the affected UUID. The municipality
must have FK-org user synchronization enabled. Receipt does not itself remove access.
Live KOMBIT access requires your own configured credentials and agreements.

Protocol references:
- [KOMBIT integration overview](https://digitaliseringskatalog.dk/bliv-klar-til-kode-beskedfordeler)
- [Official sender example and wire settings](https://github.com/kombit/bfo_infrastructureSamples-java)
