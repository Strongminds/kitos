# MCP developer checks

Complete the [setup check](README.md#check-setup). For actual ChatGPT acceptance,
use [the ChatGPT walkthrough](CHATGPT.md).

## Run the local inspector

```powershell
./deployment/mcp-local/Start-Local.ps1
```

Leave it running. In a second PowerShell terminal at the repository root:

```powershell
./deployment/mcp-local/Invoke-Local.ps1 Trust
./deployment/mcp-local/Invoke-Local.ps1 Status
Start-Process 'https://localhost:7445'
```

Trust adds the two development browser certificates to **Windows Current User
Trusted Root**; restart the browser afterward. Expected: broker discovery HTTP 200
and the inspector page on port 7445.

Choose **Connect through KITOS broker**. Enter a [KITOS API token](README.md#obtain-a-kitos-token)
only on port 7443, consent, and select **Connect**. Back on port 7445, refresh
status and **Query systems** with a known accessible name. Finish token entry
within five minutes; reconnect after expiry or broker restart.

## Credential-free browser regressions

Install Node.js LTS and Google Chrome if missing, then reopen PowerShell:

```powershell
winget install --exact --id OpenJS.NodeJS.LTS
winget install --exact --id Google.Chrome
```

With the inspector running:

```powershell
./deployment/mcp-local/Invoke-Local.ps1 BrowserTests
```

The command installs the Playwright Node package in ignored test output on first
use. Both checks should print `PASS`: token-page navigation/reload and a synthetic
callback redirect. No KITOS credentials are submitted; real authorization and
ChatGPT acceptance require their separate walkthroughs. Chrome must trust the
certificates; the checks do not bypass certificate errors.

## Code tests

```powershell
pwsh ./scripts/tests/Test-McpLocalLaunchers.ps1
./deployment/mcp-local/Invoke-Local.ps1 UnitTests
pwsh ./scripts/quality-check.ps1
```

The launcher checks simulate external commands. MCP tests use fixtures; the full
quality gate also includes existing tests requiring internet and Windows TLS access.
No database or real KITOS token is needed for these commands.

## Stop, resume or renew

Ctrl+C stops the inspector. Stop shared containers with:

```powershell
./deployment/mcp-local/Invoke-Local.ps1 Stop
```

Run `Start-Local.ps1` again to resume. For expired certificates, with the inspector stopped:

```powershell
./deployment/mcp-local/Invoke-Local.ps1 Untrust
./deployment/mcp-local/Start-Local.ps1 -NewCertificates
```

Then repeat Trust in a second terminal and restart the browser. To change KITOS
instance, add `-KitosOrigin 'https://your-instance.example'` when generating a new setup.
Keep tokens, OAuth URLs/state and private certificates out of diagnostics.
