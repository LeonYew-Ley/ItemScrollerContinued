param(
    [Parameter(Mandatory = $true)]
    [string]$GameManagedPath,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/releases')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$managed = (Resolve-Path -LiteralPath $GameManagedPath).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
$manifest = Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.name -notmatch '^[A-Za-z0-9_]+$' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Invalid Thunderstore package name or version'
}
$pluginSource = Get-Content -LiteralPath (Join-Path $root 'ItemScroller/Plugin.cs') -Raw
if (-not $pluginSource.Contains('VERSION = "' + $manifest.version_number + '"')) {
    throw 'Plugin and package versions differ'
}
& dotnet run --project (Join-Path $root 'tests/ItemScrollerContinued.Tests.csproj') --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Managed regression checks failed' }
& dotnet build (Join-Path $root 'ItemScroller/ItemScrollerContinued.csproj') --configuration Release "-p:GameManagedPath=$managed" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed' }

$stage = Join-Path $root ('artifacts/staging/' + [Guid]::NewGuid().ToString('N'))
$pluginDirectory = Join-Path $stage 'BepInEx/plugins/ItemScrollerContinued'
New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($name in @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'icon.png')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $stage $name)
}
$plugin = Join-Path $root 'ItemScroller/bin/Release/netstandard2.1/ItemScrollerContinued.dll'
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($plugin).Version
if ($assemblyVersion.ToString(3) -ne $manifest.version_number) { throw 'Assembly and package versions differ' }
Copy-Item -LiteralPath $plugin -Destination (Join-Path $pluginDirectory 'ItemScrollerContinued.dll')
$icon = [IO.File]::ReadAllBytes((Join-Path $stage 'icon.png'))
if (($icon[16..23] -join ',') -ne '0,0,1,0,0,0,1,0') { throw 'Thunderstore icon must be 256 by 256 pixels' }
$zip = Join-Path $output ($manifest.name + '-' + $manifest.version_number + '.zip')
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $expected = @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md',
        'icon.png', 'BepInEx/plugins/ItemScrollerContinued/ItemScrollerContinued.dll')
    $actual = @($archive.Entries | Where-Object Name | ForEach-Object { $_.FullName.Replace('\', '/') })
    if (Compare-Object $expected $actual) { throw 'Package contains missing or unexpected files' }
} finally { $archive.Dispose() }
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
($hash + '  ' + [IO.Path]::GetFileName($zip)) | Set-Content -LiteralPath ($zip + '.sha256') -Encoding utf8
Write-Output "Package: $zip"
Write-Output "SHA256: $hash"
