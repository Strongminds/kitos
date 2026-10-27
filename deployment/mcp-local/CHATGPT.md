# Try KITOS MCP in ChatGPT

Use PowerShell 7 at the repository root. Complete the [setup check](README.md#check-setup).

## 1. Prepare OpenAI access (once)

```powershell
Start-Process 'https://platform.openai.com/settings/organization/tunnels'
Start-Process 'https://platform.openai.com/settings/organization/api-keys'
```

In tunnel settings, create/select a tunnel linked to your ChatGPT workspace.
Download the Windows client there and extract the ZIP into Downloads.
Create a runtime API key with **Tunnels Read + Use**; creating a tunnel needs
**Read + Manage**. Ask your administrator if these options are unavailable.
[Official instructions](https://developers.openai.com/api/docs/guides/secure-mcp-tunnels).

## 2. Start locally

```powershell
./deployment/mcp-local/Start-Local.ps1 -Client ChatGPT
./deployment/mcp-local/Invoke-Local.ps1 Trust -Client ChatGPT
./deployment/mcp-local/Start-ChatGptTunnel.ps1
```

Trust adds the broker certificate to Windows Current User Trusted Root; restart
your browser. The tunnel script finds the extracted executable or asks for its
full path, then asks for the tunnel ID and privately prompts for the runtime key.
It remembers the path/ID. No callback or resource lookup is needed to start.

## 3. Connect ChatGPT

Open [ChatGPT Plugins](https://chatgpt.com/#settings/Connectors), add a custom MCP
server, select **Tunnel** and your tunnel, then **OAuth**. In advanced OAuth
settings, provide client ID `kitos-chatgpt-pilot`, empty secret, authentication
`none` (static/provided client). This pilot needs those static client settings.

On first connection, the broker may show an authorization error. Copy that
browser page's address (`https://localhost:7443/connect/authorize?...`) and run:

```powershell
./deployment/mcp-local/Complete-ChatGptSetup.ps1
```

Paste the address at its hidden prompt. The script extracts and saves the exact
callback/resource and restarts the containers, keeping the certificates.
**Kopier link** on the plugin card is not this authorization address.
Retry **Connect/Forbind igen** in ChatGPT. Paste [your KITOS API token](README.md#obtain-a-kitos-token)
only on **https://localhost:7443**, consent and select **Connect**.
Do not share the authorization address, tokens or network logs.

**Connection expired?** Select **Genopret forbindelsen / Forbind igen**, paste a valid
KITOS token on the broker, check consent, **Connect**, then retry the question.
Pilot access lasts five minutes; if services are stopped, repeat step 2 first.

**“We couldn't connect your account”?** Run
`./deployment/mcp-local/Invoke-Local.ps1 Status -Client ChatGPT`.
If the tunnel is `stopped`, run:

```powershell
./deployment/mcp-local/Start-ChatGptTunnel.ps1
```

Enter the OpenAI runtime API key locally, then reconnect in ChatGPT with a valid
KITOS token. Containers and tunnel must both be running.

## 4. Try it and stop

Select the plugin with `@` in a new conversation and ask:

> Find IT-systemet KMD Nexus Video i KITOS, og vis navn og UUID.

Compare the name/UUID with KITOS. Coverage is caller-visible, one page and best-effort.

```powershell
./deployment/mcp-local/Invoke-Local.ps1 Status -Client ChatGPT
./deployment/mcp-local/Invoke-Local.ps1 Stop -Client ChatGPT
```

To resume, repeat step 2 and reconnect. For expired certificates (seven days),
run `Invoke-Local.ps1 Untrust -Client ChatGPT`, add `-NewCertificates` to the
startup command, then repeat Trust. Saved callback/resource values are retained.
To revoke KITOS access, open **https://localhost:7443/disconnect**.
