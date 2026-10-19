# Local remote MCP pilot

Runs the trusted broker and untrusted worker as separate .NET 10 containers. The
public MCP endpoint remains Streamable HTTP over HTTPS at
`https://localhost:7443/mcp`. Only the broker is published, on IPv4 loopback.
The worker has no host port and joins an internal network without external egress.
Public broker port 7443 does not request client certificates. Worker reads use
`https://broker:7446/reads` on an unpublished listener requiring the pinned worker
certificate. That listener denies OAuth/MCP requests; the public listener denies
read-boundary requests.
The broker additionally joins an upstream network to call the explicitly selected
KITOS staging origin. This is a development pilot, not a production deployment.

Prerequisites: PowerShell 7 and the existing Podman machine with its Compose
provider. No Docker installation is required. The local inspector uses the official
MCP SDK's OAuth client and HTTPS callback. Container startup, HTTPS metadata and
private secret/network boundaries were verified; real staging permissions still
require a token entered through the broker page.

From the repository root, supply the authorized staging origin and the callback
served by your local OAuth client:

```powershell
$stagingOrigin = 'https://staging.kitos.dk'
$envFile = ./deployment/mcp-local/Initialize-Certificates.ps1 `
    -KitosOrigin $stagingOrigin `
    -ClientId 'kitos-local-pilot' `
    -RedirectUri 'https://localhost:7445/callback'
podman compose --env-file $envFile -f deployment/mcp-local/compose.yaml config --quiet
podman compose --env-file $envFile -f deployment/mcp-local/compose.yaml up --build -d
```

Start the local inspector from the repository root:

```powershell
$certificateRoot = Split-Path $envFile
$env:LocalClient__ServerCertificatePath = Join-Path $certificateRoot 'client/server.pfx'
$env:LocalClient__BrokerCertificatePath = Join-Path $certificateRoot 'public/broker-server.pem'
dotnet run --project Tools.Mcp.LocalClient -c Release
```

Open `https://localhost:7445`, select **Connect through KITOS broker**, and enter
the login-issued KITOS JWT only on `https://localhost:7443`. After authorization,
refresh connection status and query a known system name. The fixed client is
`kitos-local-pilot`, callback `https://localhost:7445/callback`, scope `kitos.read`.
KITOS credentials never belong in the worker, Compose environment, shell history,
or MCP client authorization header. Stop the inspector with Ctrl+C.
Complete token entry within five minutes of starting Connect. The inspector gives
interactive authorization the same finite budget as the overall connection; if
it expires, start a fresh Connect attempt. Failure status displays only a trusted
stage label and HTTP status, never token values or dependency response bodies.
The inspector is a separate host process on port 7445; restarting the Podman
broker/worker does not restart it. After restarting the broker or an expired
connection, click **Connect through KITOS broker** again. It clears the inspector's
cached client and prior results and starts fresh authorization/token entry. Use
the broker's `/disconnect` page to revoke delegated broker access.

Each initializer run creates a fresh, owner-restricted directory under ignored
`TestResults/mcp-local/<run-id>`, prints its `.env` path, and refuses to overwrite
material. Seven independent, passwordless development certificates expire after
seven days. It changes no certificate trust stores or OS accounts. Broker server
SANs are `localhost` and `broker`; worker server SAN is `worker`. Peer certificate
SHA-256 pins and TLS hostname checks secure private connections. Browser/public
client trust is separate: public browser certificates are
`public/broker-server.pem` (port 7443) and `public/client-server.pem` (port 7445)
beside `.env`. Configure the local client's broker certificate pin as above.
The developer must configure browser certificate trust before browser inspection;
do not disable TLS validation or automate bypassing certificate warnings. For
Chrome on Windows, the developer can import these two public certificates into
their Windows current-user Trusted Root Certification Authorities store. Do not
import private `.pfx` files. Remove the development trust entries when the pilot
ends; a fresh initializer run needs trust for its new public certificates.

With Playwright available through Node module resolution and Google Chrome
installed, run the credential-free browser check from the repository root:

```powershell
node deployment/mcp-local/verify-browser.cjs
```

It opens the inspector, clicks **Connect through KITOS broker**, verifies the
broker token-entry form, and reloads the form. Certificate validation stays
enabled. It neither enters credentials nor submits the broker form. A certificate
failure is a developer handoff, not browser acceptance. This check does not
establish staging permissions, callback completion or ChatGPT compatibility.

To check Chromium's form-redirect CSP handling without submitting credentials:

```powershell
node deployment/mcp-local/verify-callback-policy.cjs
```

This intercepts the token form POST and returns a synthetic redirect to the
registered callback. The inspector rejects the synthetic error callback; reaching
it proves that browser policy permits the redirect. It does not validate a real
authorization-code exchange. Broker `form-action` permits its own origin and the
configured callback origin; OAuth still enforces the exact registered callback.

To check Chromium's form-redirect CSP handling without submitting credentials:

```powershell
node deployment/mcp-local/verify-callback-policy.cjs
```

This intercepts the token form POST and returns a synthetic redirect to the
registered callback. The inspector rejects the synthetic error callback; reaching
it proves that browser policy permits the redirect. It does not validate a real
authorization-code exchange. Broker `form-action` permits its own origin and the
configured callback origin; OAuth still enforces the exact registered callback.

Compose mounts broker server/workload/signing/encryption keys exclusively into the
broker and worker server/client keys exclusively into the worker, as read-only
file secrets. No Docker socket or host source tree is mounted. The image build
context excludes unrelated repository files, local secrets, and build artifacts.
Both services run as the image's non-root app user, with read-only roots, dropped
capabilities, no privilege escalation, and a bounded temporary filesystem. Broker
ASP.NET data-protection keys, credentials and grants are ephemeral; restart
invalidates the pilot's state. Compose file secrets are host bind mounts: verify the app
user can read its mounted files without granting access to other host users.

Inspect configuration and availability before entering any real credentials:

```powershell
podman compose --env-file $envFile -f deployment/mcp-local/compose.yaml ps
$brokerCertificate = Join-Path (Split-Path $envFile) 'public/broker-server.pem'
curl.exe --cacert $brokerCertificate https://localhost:7443/.well-known/oauth-authorization-server
curl.exe --cacert $brokerCertificate -i https://localhost:7443/mcp
```

Inspect discovery and the unauthenticated MCP challenge, then use the configured
OAuth client to complete authorization and run initialize, tools/list, catalog,
and the bounded caller-visible system-list query. Validate denied organization
access and absence of raw KITOS credentials in tool results. A successful HTTP
response alone does not prove end-to-end MCP or security correctness. Keep traces
sanitized; no bearer tokens, credentials, or private certificate content.

Stop the isolated services after inspection:

```powershell
podman compose --env-file $envFile -f deployment/mcp-local/compose.yaml down
```

## ChatGPT W01 pilot

Use the approved OpenAI Secure MCP Tunnel with the broker's private HTTPS endpoint.
The [tunnel launcher](Start-ChatGptTunnel.ps1) prompts locally for a runtime API key
with Tunnels Read + Use permissions; keys never belong in chat or Compose settings.
Tunnel health is supporting evidence, not OAuth or ChatGPT query acceptance.

Set `MCP_RESOURCE_IDENTIFIER` to the exact resource identifier presented by the
actual ChatGPT tunnel OAuth flow. The broker uses this one identifier consistently
in protected-resource metadata, authorization, token issuance and audience
validation; its listener and issuer remain `https://localhost:7443`. This is an
identifier, never a URL the broker fetches. Leaving the setting empty preserves
the inspector's `https://localhost:7443/mcp` resource. Do not accept both resources
or copy full OAuth URLs/state into logs.

When ChatGPT exposes no static client fields, opt into the pilot's bounded client
registration. Configure `MCP_CLIENT_ID='kitos-chatgpt-pilot'`,
`MCP_REDIRECT_URI` as the exact `https://chatgpt.com/connector/oauth/<callback-id>`
shown in the plugin management UI, and
`MCP_ENABLE_DYNAMIC_CLIENT_REGISTRATION='true'` in a separate Compose environment
file. A new certificate initializer can generate this configuration with
`-EnableDynamicClientRegistration`; its new certificates require developer trust.
The current pilot reuses existing trusted certificates instead.

Registration accepts only that configured callback and public authorization-code
client, with `none` token authentication and `kitos.read`; repeated registration
returns the same configured client ID. It issues no access token and does not
broaden KITOS permissions. Static inspector registration remains the default.
Switching configurations restarts the broker, clears its in-memory authorization
state and requires fresh token entry; the two clients are not enabled together.

In ChatGPT, select Tunnel and OAuth for the custom MCP server. Complete token
entry yourself only on the trusted broker page, then install/select the plugin
and ask for `KMD Nexus Video` in Danish. Verify UUID/name and caller-visible,
single-page coverage. OAuth endpoint reachability through the selected tunnel
must be verified by the actual ChatGPT connection before claiming W01 acceptance.

For the created W01 plugin, static client fields are available: client ID
`kitos-chatgpt-pilot`, empty secret and token authentication method `none`.
The bounded registration endpoint is optional; CIMD is not advertised.
