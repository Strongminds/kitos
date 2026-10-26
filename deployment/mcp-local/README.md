# Local KITOS MCP pilot (Windows)

Open PowerShell 7 (`pwsh`), from the repository root. KITOS itself stays on
**https://staging.kitos.dk**; no local KITOS database is needed.

## Check setup

```powershell
pwsh ./deployment/mcp-local/Invoke-Local.ps1 Check
```

If dependencies are missing, install only what you need, then reopen PowerShell:

```powershell
winget install --exact --id Microsoft.PowerShell
winget install --exact --id Microsoft.DotNet.SDK.10
winget install --exact --id RedHat.Podman-Desktop
```

For a new Podman installation, complete its onboarding with the Podman engine and
Compose CLI enabled. [Windows setup](https://podman-desktop.io/docs/installation/windows-install)
and [Compose setup](https://podman-desktop.io/docs/compose/setting-up-compose).
The launcher creates/starts a Podman machine when needed. It installs no software.

## Choose a workflow

- **[Try ChatGPT end to end](CHATGPT.md)** — containers, tunnel, authorization and a real question.
- **[Developer checks](DEVELOPING.md)** — local inspector, browser regressions and code tests.

Both workflows share ports 7443 and 7444 inside one Compose project. Run one
configuration at a time; switching requires fresh authorization.

## Obtain a KITOS token

```powershell
Start-Process 'https://staging.kitos.dk/swagger'
```

Select **KITOS API V1**, find `POST /api/authorize/GetToken`, and use **Try it out**
with your KITOS **API user** email/password. Copy the response's `token` into the
broker login page at **https://localhost:7443**. An ordinary UI login is not an
API account; ask your KITOS administrator if you need one.

Keep tokens and tunnel keys out of chat, commands and logs. Certificate trust is
handled by an explicit command in each guide; TLS validation stays enabled.
