param([string]$buildConfiguration = "Release")

$ErrorActionPreference = "Stop"

$repoRoot   = Resolve-Path "$PSScriptRoot\.."
$infraProject  = "$repoRoot\Infrastructure.DataAccess\Infrastructure.DataAccess.csproj"
$startupProject = "$repoRoot\Presentation.Web\Presentation.Web.csproj"
$bundleDir  = "$repoRoot\Output\MigrationsBundle"
$bundleExe  = "$bundleDir\efbundle.exe"

New-Item -ItemType Directory -Path $bundleDir -Force | Out-Null

# Supply a dummy connection string so the design-time factory can instantiate KitosContext
# during bundle creation. The real connection string is passed at runtime via --connection.
$Env:ConnectionStrings__KitosContext = "Server=.;Database=Kitos;Trusted_Connection=True;TrustServerCertificate=True"

Write-Host "Creating EF Core migrations bundle ($buildConfiguration)..."

dotnet ef migrations bundle `
    --project "$infraProject" `
    --startup-project "$startupProject" `
    --output "$bundleExe" `
    --configuration $buildConfiguration `
    --force

if ($LASTEXITCODE -ne 0) { Throw "Failed to create EF Core migrations bundle" }

Write-Host "Migrations bundle created at $bundleExe"
