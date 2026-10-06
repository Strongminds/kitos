#Requires -Version 7.0
<#
.SYNOPSIS
    KITOS code quality gate. Same checks run locally, by AI agents and in CI (.github/workflows/pr-quality.yml).

.DESCRIPTION
    1. Format      - dotnet format (whitespace) on changed *.cs files (boy-scout rule; legacy files are not reformatted wholesale)
    2. Build       - KITOS.sln with -p:KitosQualityGate=true: every compiler/analyzer warning is an error (rules: .editorconfig)
    3. Conventions - KITOS-specific checks on the diff (debug output, suppressions, migrations, tests accompanying changes)
    4. Tests       - unit test projects

    See docs/CODE_QUALITY.md.

.PARAMETER BaseRef
    Git ref the branch is compared to when finding changed files. Default: origin/master.

.PARAMETER Fix
    Apply formatting fixes instead of only verifying.

.PARAMETER SkipBuild
    Skip the build step (useful for a quick format/conventions check).

.PARAMETER SkipTests
    Skip running unit tests.

.EXAMPLE
    pwsh ./scripts/quality-check.ps1
    pwsh ./scripts/quality-check.ps1 -Fix -SkipTests
#>
[CmdletBinding()]
param(
    [string]$BaseRef = "origin/master",
    [switch]$Fix,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$solution = "KITOS.sln"
$unitTestProjects = @(
    "Tests.Unit.Core.ApplicationServices/Tests.Unit.Core.csproj",
    "Tests.Unit.Presentation.Web/Tests.Unit.Presentation.Web.csproj"
)
# Presentation.Web pins a Windows RID unless ContainerBuild=true (same switch the Dockerfile uses)
$platformArgs = if ($IsWindows) { @() } else { @("-p:ContainerBuild=true") }

$results = [System.Collections.Generic.List[object]]::new()
function Add-Result([string]$Check, [string]$Status, [string]$Details = "") {
    $results.Add([pscustomobject]@{ Check = $Check; Status = $Status; Details = $Details })
    $color = switch ($Status) { "PASS" { "Green" } "WARN" { "Yellow" } "SKIP" { "DarkGray" } default { "Red" } }
    Write-Host ("[{0}] {1} {2}" -f $Status, $Check, $Details) -ForegroundColor $color
}
function Write-Section([string]$Title) { Write-Host "`n=== $Title ===" -ForegroundColor Cyan }

#region Changed files
Write-Section "Changed files (vs $BaseRef)"
$mergeBase = git merge-base $BaseRef HEAD 2>$null
if (-not $mergeBase) {
    Write-Host "Could not resolve '$BaseRef' - fetching it" -ForegroundColor Yellow
    $remote, $branch = $BaseRef -split "/", 2
    git fetch --no-tags --quiet $remote $branch
    $mergeBase = git merge-base $BaseRef HEAD
}
# Committed branch changes + uncommitted work + untracked files (so agents can run this before committing)
$untracked = @(git ls-files --others --exclude-standard)
$changedFiles = @(
    git diff --name-only --diff-filter=ACMR $mergeBase
    $untracked
) | Where-Object { $_ } | Sort-Object -Unique | Where-Object { Test-Path $_ }
$changedCs = @($changedFiles | Where-Object { $_ -like "*.cs" -and $_ -notmatch "/Migrations/" -and $_ -notmatch "/(obj|bin)/" })

# Added lines per file ("path: line"), used by the convention checks
$addedLines = [System.Collections.Generic.List[string]]::new()
$currentFile = ""
foreach ($line in (git diff --unified=0 $mergeBase -- "*.cs")) {
    if ($line -match "^\+\+\+ b/(.+)$") { $currentFile = $Matches[1]; continue }
    if ($line -match "^\+(?!\+\+)") { $addedLines.Add("${currentFile}: $($line.Substring(1).Trim())") }
}
foreach ($file in ($untracked | Where-Object { $_ -like "*.cs" })) {
    Get-Content $file | ForEach-Object { $addedLines.Add("${file}: $($_.Trim())") }
}
Write-Host "$($changedFiles.Count) changed file(s), $($changedCs.Count) C# file(s)"
#endregion

#region 1. Format
Write-Section "1. Formatting (dotnet format whitespace, changed files)"
if ($changedCs.Count -eq 0) {
    Add-Result "Format" "SKIP" "no changed C# files"
}
else {
    $formatArgs = @("format", "whitespace", $solution, "--include") + $changedCs
    if (-not $Fix) { $formatArgs += "--verify-no-changes" }
    & dotnet @formatArgs
    if ($LASTEXITCODE -eq 0) { Add-Result "Format" "PASS" $(if ($Fix) { "fixes applied" } else { "" }) }
    else { Add-Result "Format" "FAIL" "run: pwsh ./scripts/quality-check.ps1 -Fix -SkipBuild -SkipTests" }
}
#endregion

#region 2. Build
Write-Section "2. Build with warnings as errors"
if ($SkipBuild) {
    Add-Result "Build (0 warnings)" "SKIP"
}
else {
    & dotnet build $solution -c $Configuration -nologo -p:KitosQualityGate=true @platformArgs
    if ($LASTEXITCODE -eq 0) { Add-Result "Build (0 warnings)" "PASS" }
    else { Add-Result "Build (0 warnings)" "FAIL" "fix the reported warnings/errors - do not suppress them" }
}
#endregion

#region 3. KITOS conventions
Write-Section "3. KITOS conventions"
$prodAdded = @($addedLines | Where-Object { $_ -notmatch "^(Tests\.|Tools\.|PubSub\.Test)" -and $_ -notmatch "^[^:]*/Migrations/" })

# 3a. Debug leftovers in production code
$debug = @($prodAdded | Where-Object { $_ -match "Console\.Write|Debugger\.Break|Debug\.Write" })
if ($debug.Count) { Add-Result "No debug output" "FAIL" ($debug -join "; ") } else { Add-Result "No debug output" "PASS" }

# 3b. Warning suppressions must be justified
$suppressions = @($prodAdded | Where-Object {
        ($_ -match "#pragma warning disable" -and $_ -notmatch "#pragma warning disable[^/]*//\s*\S") -or
        ($_ -match "SuppressMessage\(" -and $_ -notmatch "Justification\s*=")
    })
if ($suppressions.Count) { Add-Result "Justified suppressions" "FAIL" ("add a justification (// comment or Justification=): " + ($suppressions -join "; ")) }
else { Add-Result "Justified suppressions" "PASS" }

# 3c. Analyzer baseline in .editorconfig must only shrink
if ($changedFiles -contains ".editorconfig") {
    $baselineAdds = @(git diff --unified=0 $mergeBase -- .editorconfig | Where-Object { $_ -match "^\+\[" -or $_ -match "^\+dotnet_diagnostic\..*= *(none|silent|suggestion)" })
    if ($baselineAdds.Count) { Add-Result "Analyzer baseline not extended" "WARN" "rules relaxed in .editorconfig - requires reviewer approval: $($baselineAdds -join ', ')" }
    else { Add-Result "Analyzer baseline not extended" "PASS" }
}

# 3d. EF Core migrations must come with Designer file and updated model snapshot
$newMigrations = @($changedFiles | Where-Object { $_ -match "Migrations/EfCore/\d+_.+\.cs$" -and $_ -notmatch "\.Designer\.cs$" })
if ($newMigrations.Count) {
    $hasSnapshot = [bool]($changedFiles -match "Migrations/EfCore/KitosContextModelSnapshot\.cs$")
    $missingDesigner = @($newMigrations | Where-Object { -not (Test-Path ($_ -replace "\.cs$", ".Designer.cs")) })
    if ($hasSnapshot -and -not $missingDesigner.Count) { Add-Result "Migration consistency" "PASS" ($newMigrations -join ", ") }
    else { Add-Result "Migration consistency" "FAIL" "migration added without matching .Designer.cs and KitosContextModelSnapshot.cs update" }
}

# 3e. Manual SQL scripts - database compatibility (PR checklist)
$sqlScripts = @($changedFiles | Where-Object { $_ -match "Migrations/SQLScripts/.+\.sql$" })
if ($sqlScripts.Count) { Add-Result "Database compatibility" "WARN" "SQL scripts changed - verify SQL Server and PostgreSQL versions: $($sqlScripts -join ', ')" }

# 3f. Changes to business logic should be accompanied by tests
$logicChanged = @($changedCs | Where-Object { $_ -match "^(Core\.ApplicationServices|Core\.DomainServices|Core\.DomainModel|Presentation\.Web/Controllers)/" })
$testsChanged = @($changedCs | Where-Object { $_ -match "^Tests\." })
if ($logicChanged.Count -and -not $testsChanged.Count) { Add-Result "Tests accompany changes" "WARN" "$($logicChanged.Count) logic file(s) changed without test changes" }
elseif ($logicChanged.Count) { Add-Result "Tests accompany changes" "PASS" }
#endregion

#region 4. Tests
Write-Section "4. Unit tests"
if ($SkipTests) {
    Add-Result "Unit tests" "SKIP"
}
else {
    $failed = @()
    foreach ($project in $unitTestProjects) {
        $testArgs = @("test", $project, "-c", $Configuration, "-nologo", "--logger", "trx", "--results-directory", "TestResults") + $platformArgs
        if (-not $SkipBuild) { $testArgs += "--no-build" }
        & dotnet @testArgs
        if ($LASTEXITCODE -ne 0) { $failed += $project }
    }
    if ($failed.Count) { Add-Result "Unit tests" "FAIL" ($failed -join ", ") } else { Add-Result "Unit tests" "PASS" }
}
#endregion

#region Summary
Write-Section "Summary"
$results | Format-Table -AutoSize | Out-String -Width 300 | Write-Host
if ($env:GITHUB_STEP_SUMMARY) {
    $md = @("## KITOS quality gate", "", "| Check | Status | Details |", "|---|---|---|")
    $md += $results | ForEach-Object { "| $($_.Check) | $($_.Status) | $($_.Details -replace '\|', '\|') |" }
    $md | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
$failures = @($results | Where-Object Status -eq "FAIL")
if ($failures.Count) {
    Write-Host "Quality gate FAILED ($($failures.Count) check(s))" -ForegroundColor Red
    exit 1
}
Write-Host "Quality gate PASSED" -ForegroundColor Green
exit 0
#endregion
