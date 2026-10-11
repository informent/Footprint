#ifndef AppVersion
  #error AppVersion must be supplied
#endif
#ifndef PayloadDir
  #error PayloadDir must be supplied
#endif
#ifndef OutputDir
  #error OutputDir must be supplied
#endif
[Setup]
AppId={{846CB3D0-31A1-5BAB-819D-5E50A2639DAA}
AppName=Footprint
AppVersion={#AppVersion}
AppPublisher=informent
AppPublisherURL=https://github.com/informent/Footprint
DefaultDirName={localappdata}\Programs\Footprint
DefaultGroupName=Footprint
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#OutputDir}
OutputBaseFilename=Footprint-v{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Footprint.exe
CloseApplications=yes
CloseApplicationsFilter=Footprint.exe
RestartApplications=no
SetupLogging=yes
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\Footprint"; Filename: "{app}\Footprint.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Footprint"; Filename: "{app}\Footprint.exe"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\Footprint.exe"; Description: "Open Footprint"; Flags: nowait postinstall skipifsilent unchecked
