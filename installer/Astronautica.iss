#ifndef AppVersion
  #error AppVersion must be provided by scripts/build-release.ps1
#endif
#ifndef PayloadDir
  #error PayloadDir must be provided by scripts/build-release.ps1
#endif
#ifndef OutputPath
  #error OutputPath must be provided by scripts/build-release.ps1
#endif

[Setup]
AppId={{B6375469-9746-4852-A47F-B4CC2901D9DC}
AppName=Astronautica
AppVersion={#AppVersion}
AppVerName=Astronautica {#AppVersion}
AppPublisher=Astronautica contributors
AppPublisherURL=https://github.com/XayerMorgan/galactic_view
AppSupportURL=https://github.com/XayerMorgan/galactic_view/issues
AppUpdatesURL=https://github.com/XayerMorgan/galactic_view/releases
VersionInfoVersion={#AppVersion}
VersionInfoDescription=Astronautica - Cosmic Explorer Setup
DefaultDirName={localappdata}\Programs\Astronautica
DefaultGroupName=Astronautica
DisableProgramGroupPage=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19043
OutputDir={#OutputPath}
OutputBaseFilename=Astronautica-{#AppVersion}-Windows-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dynamic
WizardSizePercent=115
UninstallDisplayIcon={app}\CosmicZoomEngine.exe
UninstallDisplayName=Astronautica - Cosmic Explorer
CloseApplications=yes
CloseApplicationsFilter=CosmicZoomEngine.exe
RestartApplications=no
SetupLogging=yes
InfoBeforeFile=release-info.txt
LicenseFile={#PayloadDir}\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Astronautica"; Filename: "{app}\CosmicZoomEngine.exe"; WorkingDir: "{app}"
Name: "{group}\Astronautica Field Guide"; Filename: "{app}\CosmicZoomEngine_Data\StreamingAssets\Help\index.html"
Name: "{group}\Uninstall Astronautica"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Astronautica"; Filename: "{app}\CosmicZoomEngine.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\CosmicZoomEngine.exe"; Description: "Launch Astronautica"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
