[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $repoRoot 'VERSION') -Raw).Trim()
$installer = Join-Path $repoRoot "dist/$version/Astronautica-$version-Windows-x64-Setup.exe"
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B6375469-9746-4852-A47F-B4CC2901D9DC}_is1'
if (Test-Path -LiteralPath $registryPath) { throw 'An Astronautica installation is registered. Use a separate Windows account for the clean-install test.' }
if (Get-Process CosmicZoomEngine -ErrorAction SilentlyContinue) { throw 'Close Astronautica before the release test.' }
if (!(Test-Path -LiteralPath $installer)) { throw 'Build the release first.' }
$testId = [guid]::NewGuid().ToString('N').Substring(0, 10)
$testRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "build/installer-test-$testId"))
$buildRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'build')) + [IO.Path]::DirectorySeparatorChar
if (!$testRoot.StartsWith($buildRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Test folder must be inside build/.' }
$installDir = Join-Path $testRoot 'Astronautica'
$reportDir = Join-Path $repoRoot 'visual_tests/release'
New-Item -ItemType Directory -Force -Path $testRoot, $reportDir | Out-Null
$results = [Collections.Generic.List[string]]::new()
function Check([bool]$condition, [string]$message) {
    if (!$condition) { throw $message }
    $results.Add("PASS $message")
}
$group = "Astronautica Release Test $testId"
$setupArgs = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/TASKS=',
    ('/DIR="' + $installDir + '"'), ('/GROUP="' + $group + '"'))
try {
    $setup = Start-Process -FilePath $installer -ArgumentList ($setupArgs + ('/LOG="' + (Join-Path $testRoot 'install.log') + '"')) -WindowStyle Hidden -PassThru -Wait
    Check ($setup.ExitCode -eq 0) 'Clean per-user installation succeeds'
    Check (Test-Path -LiteralPath $registryPath) 'Windows uninstall entry is registered'
    $manifest = Get-Content -LiteralPath (Join-Path $installDir 'file-manifest.json') -Raw | ConvertFrom-Json
    foreach ($item in $manifest) {
        $installed = Join-Path $installDir $item.path
        Check ((Test-Path -LiteralPath $installed) -and (Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash -eq $item.sha256) ("Installed file matches payload: " + $item.path)
    }
    $programs = [Environment]::GetFolderPath('Programs')
    foreach ($name in @('Astronautica.lnk', 'Astronautica Field Guide.lnk', 'Uninstall Astronautica.lnk')) {
        Check (Test-Path -LiteralPath (Join-Path $programs "$group/$name")) ("Start menu shortcut: $name")
    }
    $qaLog = Join-Path $testRoot 'player.log'
    $player = Start-Process -FilePath (Join-Path $installDir 'CosmicZoomEngine.exe') -ArgumentList @('--cosmic-qa', '-logFile', ('"' + $qaLog + '"')) -WorkingDirectory $testRoot -PassThru
    if (!$player.WaitForExit(180000)) { $player.Kill(); throw 'Player QA timed out.' }
    Check ($player.ExitCode -eq 0) 'Installed player regression run exits successfully'
    $qaReport = Join-Path $testRoot 'visual_tests/interface-repair/verification.txt'
    Check ((Get-Content -LiteralPath $qaReport -Tail 1) -eq 'Failures: 0') 'Native player assertions all pass'
    Copy-Item -LiteralPath $qaReport -Destination (Join-Path $reportDir 'player-verification.txt')
    $setup = Start-Process -FilePath $installer -ArgumentList ($setupArgs + ('/LOG="' + (Join-Path $testRoot 'upgrade.log') + '"')) -WindowStyle Hidden -PassThru -Wait
    Check ($setup.ExitCode -eq 0) 'Reinstallation into the same folder succeeds'
    foreach ($item in $manifest) {
        Check ((Get-FileHash -LiteralPath (Join-Path $installDir $item.path) -Algorithm SHA256).Hash -eq $item.sha256) ("Reinstalled file matches payload: " + $item.path)
    }
    # A file the installer does not own must survive uninstall.
    Set-Content -LiteralPath (Join-Path $installDir 'user-note.txt') -Value 'Preserve user-owned files.'
} finally {
    $uninstaller = Join-Path $installDir 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        $uninstall = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/LOG="' + (Join-Path $testRoot 'uninstall.log') + '"')) -WindowStyle Hidden -PassThru -Wait
        if ($uninstall.ExitCode -ne 0) { throw 'Test uninstall failed.' }
    }
}
Check (!(Test-Path -LiteralPath (Join-Path $installDir 'CosmicZoomEngine.exe'))) 'Uninstall removes the application'
Check (!(Test-Path -LiteralPath $registryPath)) 'Uninstall removes its Windows registration'
Check (!(Test-Path -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) $group))) 'Uninstall removes its Start menu shortcuts'
Check (Test-Path -LiteralPath (Join-Path $installDir 'user-note.txt')) 'Uninstall preserves files it does not own'
$results.Add('Failures: 0')
$results | Set-Content -LiteralPath (Join-Path $reportDir 'installer-verification.txt') -Encoding UTF8
Write-Output "$($results.Count - 1) installer checks passed. Report: $reportDir. Detailed logs: $testRoot"
