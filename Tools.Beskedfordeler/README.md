# Beskedfordeler test sender

Publishes one `Test/TestbeskedType1` (sometimes called Test/Testbesked1) event
(`7654895c-a3f3-4c77-8afd-4c77330f963c`, version `1.0`) to KOMBIT's actual
Beskedfordeler using AMQP over TLS. It does not call KITOS directly or deactivate users.

Requires .NET 10, access to port 5671, a registered client certificate with its
private key, and an approved **SF1462 / BeskedAfsend** service agreement for the
municipality CVR and sender system. Receiving-only SF1461 access is insufficient.
Configure the receiving subscription to match the test message type and CVR.

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
dotnet run --project .\Tools.Beskedfordeler -- .\Tools.Beskedfordeler\sts-config.local.json <sender-system-uuid> <municipality-cvr>
```

The optional fourth argument overrides the AMQP hostname. By default it targets
`beskedfordeler.eksterntest-stoettesystemerne.dk:5671`, virtual host `BF`.
There is no TLS certificate-validation bypass.

Each message creation generates fresh message and registration UUIDs, a unique test
payload containing the message UUID, and current UTC timestamps. It restricts
`TilladtModtager` and `ObjektAnsvarligMyndighed` to the supplied eight-digit CVR, and
uses a synthetic text payload without personal data. The STS audience is
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

Acceptance is not proof of callback delivery. For the KITOS receiver, check its
Warning log for the printed message UUID when the callback is received. Test messages
are logged immediately without being added to Hangfire; business events are durably queued.
Live KOMBIT access requires your own configured credentials and agreements.

Protocol references:
- [KOMBIT integration overview](https://digitaliseringskatalog.dk/bliv-klar-til-kode-beskedfordeler)
- [Official sender example and wire settings](https://github.com/kombit/bfo_infrastructureSamples-java)
