[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$toolsDir = Join-Path $repoRoot 'build/tools'
$compilerDir = Join-Path $toolsDir 'inno'
$compiler = Join-Path $compilerDir 'ISCC.exe'
if (Test-Path -LiteralPath $compiler) { Write-Output $compiler; return }
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
$download = Join-Path $toolsDir 'innosetup-7.1.0-x64.exe'
$expected = '0362A383ED217D4C4239B5933866DD96D3EB2102737DA92F80F6057A4B40DF2F'
Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe' -OutFile $download
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $expected) { throw 'Inno Setup SHA256 mismatch.' }
$signature = Get-AuthenticodeSignature -LiteralPath $download
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike '*O=Pyrsys B.V.*') {
    throw 'Inno Setup publisher signature is not valid.'
}
$compilerProcess = Start-Process -FilePath $download -ArgumentList @(
    '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/CURRENTUSER', '/NOICONS', '/TASKS=',
    ('/DIR="' + $compilerDir + '"')
) -WindowStyle Hidden -PassThru -Wait
if ($compilerProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $compiler)) { throw 'Inno Setup installation failed.' }
Write-Output $compiler
