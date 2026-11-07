[Setup]
AppId=Ergonomy.WellnessDock
AppName=Ergonomy
AppVersion=0.3.0
AppPublisher=Ergonomy
DefaultDirName={localappdata}\Programs\Ergonomy
DefaultGroupName=Ergonomy
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\release
OutputBaseFilename=Ergonomy-0.3.0-rc.1-setup-x64
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\Ergonomy.exe
CloseApplications=yes
[Files]
Source: "..\release\Ergonomy-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{userprograms}\Ergonomy"; Filename: "{app}\Ergonomy.exe"
[Run]
Filename: "{app}\Ergonomy.exe"; Description: "Open Ergonomy"; Flags: nowait postinstall skipifsilent
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Ergonomy"; Flags: uninsdeletevalue dontcreatekey
