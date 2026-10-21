param(
    [Parameter(Mandatory = $true)]
    [string]$TokenFile,
    [string]$TcliPath = 'tcli',
    [string]$PackagePath = ''
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$manifest = Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
$configPath = Join-Path $root 'thunderstore.toml'
$config = Get-Content -LiteralPath $configPath -Raw
$expectedConfig = [ordered]@{
    namespace = 'LeonYew'
    name = $manifest.name
    versionNumber = $manifest.version_number
    description = $manifest.description
    websiteUrl = $manifest.website_url
    repository = 'https://thunderstore.io'
}
foreach ($entry in $expectedConfig.GetEnumerator()) {
    $pattern = '(?m)^' + [regex]::Escape($entry.Key) + '\s*=\s*"' + [regex]::Escape($entry.Value) + '"\s*$'
    if ($config -notmatch $pattern) { throw "Thunderstore config differs from manifest: $($entry.Key)" }
}
if (-not $PackagePath) {
    $PackagePath = Join-Path $root ('artifacts/releases/' + $manifest.name + '-' + $manifest.version_number + '.zip')
}
$package = (Resolve-Path -LiteralPath $PackagePath).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entry = $archive.GetEntry('manifest.json')
    if ($null -eq $entry) { throw 'Package is missing manifest.json' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { $packagedManifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    foreach ($property in @('name', 'version_number', 'website_url', 'description')) {
        if ($packagedManifest.$property -ne $manifest.$property) { throw "Packaged manifest differs: $property" }
    }
    if (Compare-Object @($manifest.dependencies) @($packagedManifest.dependencies)) {
        throw 'Packaged dependencies differ from manifest'
    }
    $expectedFiles = @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md',
        'icon.png', 'BepInEx/plugins/ItemScrollerContinued/ItemScrollerContinued.dll')
    $actualFiles = @($archive.Entries | Where-Object Name | ForEach-Object { $_.FullName.Replace('\', '/') })
    if (Compare-Object $expectedFiles $actualFiles) { throw 'Package contains missing or unexpected files' }
} finally { $archive.Dispose() }

# Check the exact version rather than only the latest package version.
$versionApi = 'https://thunderstore.io/api/experimental/package/LeonYew/' + $manifest.name + '/' + $manifest.version_number + '/'
# Bypass cached 404 responses left over from a pre-publication lookup.
$versionApi += '?verify=' + [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
try { $existing = Invoke-RestMethod $versionApi -Headers @{ 'Cache-Control' = 'no-cache' } } catch {
    if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 404) { throw }
    $existing = $null
}
if ($null -ne $existing) { throw 'This version is already published. Do not upload it again.' }

$token = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $TokenFile).Path).Trim()
if ([string]::IsNullOrWhiteSpace($token)) { throw 'Token file is empty.' }
# TCLI checks the Windows user environment before the process environment.
$userToken = [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'User')
if (-not [string]::IsNullOrWhiteSpace($userToken) -and $userToken -ne $token) {
    throw 'A different user-level TCLI_AUTH_TOKEN is configured; resolve the mismatch before publishing.'
}
$previousToken = [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process')
$previousRollForward = [Environment]::GetEnvironmentVariable('DOTNET_ROLL_FORWARD', 'Process')
Push-Location $root
try {
    $env:TCLI_AUTH_TOKEN = $token
    $env:DOTNET_ROLL_FORWARD = 'Major'
    & $TcliPath publish --config-path $configPath --file $package 2>&1 |
        ForEach-Object { ([string]$_).Replace($token, '[REDACTED]') | Write-Output }
    if ($LASTEXITCODE -ne 0) { throw ('Thunderstore publishing failed with exit code ' + $LASTEXITCODE) }
} finally {
    [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', $previousToken, 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_ROLL_FORWARD', $previousRollForward, 'Process')
    $token = $null
    $userToken = $null
    Pop-Location
}
