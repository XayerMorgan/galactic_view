[CmdletBinding()]
param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
    [string]$InnoCompiler,
    [switch]$SkipPlayerBuild
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $repoRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain a three-part numeric version.' }
$project = Join-Path $repoRoot 'unity_cosmic_engine'
$playerDir = Join-Path $repoRoot 'CosmicZoomEngine_App'
$buildDir = Join-Path $repoRoot 'build'
$distDir = Join-Path $repoRoot "dist/$version"
New-Item -ItemType Directory -Force -Path $buildDir, $distDir | Out-Null
if (!(Test-Path -LiteralPath $UnityEditor)) { throw "Unity editor not found: $UnityEditor" }
if (!$SkipPlayerBuild) {
    if (Get-Process CosmicZoomEngine -ErrorAction SilentlyContinue) { throw 'Close Astronautica before rebuilding.' }
    $buildLog = Join-Path $buildDir 'unity-release.log'
    $unityProcess = Start-Process -FilePath $UnityEditor -ArgumentList @(
        '-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
        '-executeMethod', 'CosmicZoom.Editor.CosmicSceneBuilder.BuildStandalonePlayer',
        '-logFile', ('"' + $buildLog + '"')
    ) -WindowStyle Hidden -PassThru -Wait
    if ($unityProcess.ExitCode -ne 0) { throw "Unity build failed; see $buildLog" }
}
$required = @('CosmicZoomEngine.exe', 'UnityPlayer.dll', 'CosmicZoomEngine_Data/StreamingAssets/Help/index.html',
    'CosmicZoomEngine_Data/StreamingAssets/Help/help.css', 'CosmicZoomEngine_Data/StreamingAssets/Gallery/CREDITS.txt')
foreach ($file in $required) {
    if (!(Test-Path -LiteralPath (Join-Path $playerDir $file))) { throw "Missing player file: $file" }
}
if (!(Test-Path -LiteralPath (Join-Path $playerDir 'VERSION')) -or
    (Get-Content -LiteralPath (Join-Path $playerDir 'VERSION') -Raw).Trim() -ne $version) {
    throw 'Player version does not match VERSION. Rebuild the player.'
}
if (!$InnoCompiler) { $InnoCompiler = Join-Path $buildDir 'tools/inno/ISCC.exe' }
if (!(Test-Path -LiteralPath $InnoCompiler)) { throw 'Run scripts/bootstrap-inno.ps1 first, or specify -InnoCompiler.' }

# Documentation is plain StreamingAssets content; include the latest guide even
# when packaging an existing, version-checked native player.
Get-ChildItem -LiteralPath (Join-Path $project 'Assets/StreamingAssets/Help') -File |
    Where-Object { $_.Extension -ne '.meta' } |
    Copy-Item -Destination (Join-Path $playerDir 'CosmicZoomEngine_Data/StreamingAssets/Help') -Force

# Fresh staging directory: never package stale files from an earlier release.
$stage = Join-Path $buildDir ('release-stage-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $stage 'Astronautica'
New-Item -ItemType Directory -Path $payload | Out-Null
Get-ChildItem -LiteralPath $playerDir | Where-Object { $_.Name -notmatch 'DoNotShip|BackUpThisFolder|\.pdb$|\.log$' } |
    Copy-Item -Destination $payload -Recurse
foreach ($file in @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'VERSION')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $file) -Destination $payload
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'installer/START_HERE.txt') -Destination $payload
$licenses = Join-Path $payload 'Licenses'
New-Item -ItemType Directory -Path $licenses | Out-Null
Copy-Item -LiteralPath (Join-Path (Split-Path $InnoCompiler -Parent) 'license.txt') -Destination (Join-Path $licenses 'Inno-Setup-License.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'licenses/Unity-Player-Windows-Mono-6000.6.0f1.pdf') -Destination $licenses
$editorData = Join-Path (Split-Path $UnityEditor -Parent) 'Data'
Copy-Item -LiteralPath (Join-Path $editorData 'Resources/UnityEditorSoftwareTerms.rtf') -Destination $licenses
$packageCache = Join-Path $project 'Library/PackageCache'
foreach ($package in Get-ChildItem -LiteralPath $packageCache -Directory) {
    $packageNotices = @(Get-ChildItem -LiteralPath $package.FullName -File | Where-Object { $_.Name -match '^(LICENSE|Third Party Notices)' -and $_.Extension -ne '.meta' })
    if ($packageNotices.Count -eq 0) { continue }
    $packageLicenses = Join-Path $licenses $package.Name
    New-Item -ItemType Directory -Path $packageLicenses | Out-Null
    $packageNotices | Copy-Item -Destination $packageLicenses
}

$manifest = Get-ChildItem -LiteralPath $payload -File -Recurse | Sort-Object FullName | ForEach-Object {
    [ordered]@{path = $_.FullName.Substring($payload.Length + 1).Replace('\', '/'); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLower()}
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $payload 'file-manifest.json') -Encoding UTF8
& $InnoCompiler "/DAppVersion=$version" "/DPayloadDir=$payload" "/DOutputPath=$distDir" (Join-Path $repoRoot 'installer/Astronautica.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$zip = Join-Path $distDir "Astronautica-$version-Windows-x64-Portable.zip"
Compress-Archive -LiteralPath $payload -DestinationPath $zip -CompressionLevel Optimal -Force
$artifacts = @((Join-Path $distDir "Astronautica-$version-Windows-x64-Setup.exe"), $zip)
$hashes = $artifacts | ForEach-Object { ((Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLower() + '  ' + (Split-Path $_ -Leaf)) }
$hashes | Set-Content -LiteralPath (Join-Path $distDir 'SHA256SUMS.txt') -Encoding ascii
[ordered]@{version=$version; payload=$payload; artifacts=$artifacts} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $buildDir 'last-release.json') -Encoding UTF8
Write-Output "Release files: $distDir"
