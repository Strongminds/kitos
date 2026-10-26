# Shared by the local pilot launchers; environment files are data, never scripts.
function Invoke-LocalCommand([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed (exit $LASTEXITCODE)." }
}

function Get-LocalStateRoot {
    Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))) 'TestResults/mcp-local'
}

function Get-LocalEnvironmentPath([string]$Client, [string]$EnvFile) {
    if (-not $EnvFile) {
        $pointer = Join-Path (Get-LocalStateRoot) "$($Client.ToLowerInvariant())-env-path.txt"
        # Keep existing inspector installations usable.
        if ($Client -eq 'Inspector') { $pointer = Join-Path (Get-LocalStateRoot) 'local-inspector-env-path.txt' }
        if (-not (Test-Path -LiteralPath $pointer)) { throw "No saved $Client setup. Run Start-Local.ps1 -Client $Client first." }
        $EnvFile = [IO.File]::ReadAllText($pointer).Trim()
    }
    (Resolve-Path -LiteralPath $EnvFile -ErrorAction Stop).Path
}

function Read-LocalEnvironment([string]$EnvFile, [string]$Client) {
    $settings = @{}
    foreach ($line in [IO.File]::ReadAllLines($EnvFile)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
        if ($line -notmatch "^([A-Z][A-Z0-9_]*)='([^'\r\n]*)'$" -or $settings.ContainsKey($Matches[1])) {
            throw 'Expected a generated environment file with unique, single-quoted literal values.'
        }
        $settings[$Matches[1]] = $Matches[2]
    }
    $expectedClient = if ($Client -eq 'Inspector') { 'kitos-local-pilot' } else { 'kitos-chatgpt-pilot' }
    if (-not $settings['MCP_SECRET_ROOT'] -or $settings['MCP_CLIENT_ID'] -ne $expectedClient) {
        throw "This environment is not a $Client configuration."
    }
    if ($Client -eq 'Inspector' -and ($settings['MCP_REDIRECT_URI'] -ne 'https://localhost:7445/callback' -or
        $settings['MCP_ENABLE_DYNAMIC_CLIENT_REGISTRATION'] -ne 'false' -or $settings['MCP_RESOURCE_IDENTIFIER'])) {
        throw 'Inspector requires its fixed localhost callback/resource and static registration.'
    }
    if ($Client -eq 'ChatGPT') {
        $resource = $null
        if ($settings['MCP_REDIRECT_URI'] -cnotmatch '^https://chatgpt\.com/connector/oauth/[A-Za-z0-9]+$' -or
            -not [Uri]::TryCreate($settings['MCP_RESOURCE_IDENTIFIER'], [UriKind]::Absolute, [ref]$resource) -or
            $resource.Scheme -ne 'https' -or -not $resource.IsWellFormedOriginalString() -or
            $resource.UserInfo -or $resource.Query -or $resource.Fragment) {
            throw 'ChatGPT requires its exact management callback and an HTTPS OAuth resource without credentials, query or fragment.'
        }
    }
    return $settings
}

function Assert-LocalCertificates($Settings) {
    foreach ($name in @('broker-server', 'client-server', 'worker-server')) {
        $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPem(
            [IO.File]::ReadAllText((Join-Path $Settings['MCP_SECRET_ROOT'] "public/$name.pem")))
        try {
            if ($certificate.NotAfter.ToUniversalTime() -le [DateTime]::UtcNow -or
                $certificate.NotBefore.ToUniversalTime() -gt [DateTime]::UtcNow) {
                throw 'Certificates are outside their validity period. Run Start-Local.ps1 with -NewCertificates and trust the replacements.'
            }
        }
        finally { $certificate.Dispose() }
    }
}

function Assert-LocalResource([string]$Identifier) {
    $resource = $null
    if (-not [Uri]::TryCreate($Identifier, [UriKind]::Absolute, [ref]$resource) -or
        $resource.Scheme -ne 'https' -or -not $resource.IsWellFormedOriginalString() -or
        $resource.UserInfo -or $resource.Query -or $resource.Fragment -or
        $Identifier -match "['\r\n]") { throw 'Resource must be an exact HTTPS identifier without credentials, query or fragment.' }
}

function Assert-LocalCallback([string]$Callback) {
    if ($Callback -cnotmatch '^https://chatgpt\.com/connector/oauth/[A-Za-z0-9]+$') {
        throw 'Expected the exact ChatGPT connection callback (https://chatgpt.com/connector/oauth/...).'
    }
}

function Read-LocalAuthorizationRequest([string]$Address) {
    $request = $null
    if ($Address.Length -gt 16384 -or -not [Uri]::TryCreate($Address, [UriKind]::Absolute, [ref]$request) -or
        $request.GetLeftPart([UriPartial]::Path) -cne 'https://localhost:7443/connect/authorize' -or
        $request.UserInfo -or $request.Fragment) {
        throw 'Paste the broker authorization page address, not a plugin sharing link.'
    }
    $values = @{}
    try {
        foreach ($part in $request.Query.TrimStart('?').Split('&')) {
            $pair = $part.Split('=', 2)
            if ($pair.Count -ne 2) { throw 'Malformed query.' }
            $name = [Uri]::UnescapeDataString($pair[0].Replace('+', ' '))
            if ($name -notin @('redirect_uri', 'resource', 'client_id')) { continue }
            if ($values.ContainsKey($name)) { throw 'Duplicate query parameter.' }
            $values[$name] = [Uri]::UnescapeDataString($pair[1].Replace('+', ' '))
        }
        if ($values['client_id'] -cne 'kitos-chatgpt-pilot') { throw 'Wrong OAuth client.' }
        Assert-LocalCallback $values['redirect_uri']
        Assert-LocalResource $values['resource']
    }
    catch { throw 'Authorization address requires one valid ChatGPT callback/resource and client_id=kitos-chatgpt-pilot. No settings changed.' }
    return @{ CallbackUri = $values['redirect_uri']; ResourceIdentifier = $values['resource'] }
}

function Find-LocalTunnelClient {
    $installed = Get-Command tunnel-client -ErrorAction SilentlyContinue
    if ($installed) { return $installed.Source }
    $roots = @(
        (Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))) 'TestResults/W01ChatGPT'),
        (Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads')
    )
    $candidates = @($roots | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object {
        Get-ChildItem -LiteralPath $_ -Filter 'tunnel-client.exe' -File -Recurse -Depth 4 -ErrorAction SilentlyContinue
    } | Select-Object -ExpandProperty FullName -Unique)
    if ($candidates.Count -eq 1) { return $candidates[0] }
    return $null
}

function Assert-LocalPrerequisites {
    foreach ($command in @('dotnet', 'podman')) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Missing $command. See deployment/mcp-local/README.md for installation." }
    }
    $sdks = & dotnet --list-sdks
    if ($LASTEXITCODE -ne 0 -or -not ($sdks -match '^10\.')) { throw '.NET 10 SDK is required (a runtime alone is insufficient).' }
    Invoke-LocalCommand 'podman' @('compose', 'version')
}

function Start-LocalPodman {
    $available = $false
    try { & podman info --format '{{.Host.Arch}}' *> $null; $available = $LASTEXITCODE -eq 0 } catch { }
    if ($available) { return }
    $machines = & podman machine list --format json
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Podman machines.' }
    if (-not @($machines | ConvertFrom-Json)) { Invoke-LocalCommand 'podman' @('machine', 'init') }
    Invoke-LocalCommand 'podman' @('machine', 'start')
}
