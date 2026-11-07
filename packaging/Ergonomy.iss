[Setup]
AppId=Ergonomy.WellnessDock
AppName=همیار سلامت
AppVersion=0.6.0
AppPublisher=میرجعفری
DefaultDirName={localappdata}\Programs\Ergonomy
DefaultGroupName=همیار سلامت
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\release
OutputBaseFilename=Ergonomy-0.6.0-rc.1-setup-x64
SetupIconFile=..\src\Ergonomy\Assets\AppIcon.ico
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\Ergonomy.exe
CloseApplications=yes
[Files]
Source: "..\release\Ergonomy-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{userprograms}\همیار سلامت"; Filename: "{app}\Ergonomy.exe"
[Run]
Filename: "{app}\Ergonomy.exe"; Description: "اجرای همیار سلامت"; Flags: nowait postinstall skipifsilent
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "Ergonomy"; Flags: uninsdeletevalue dontcreatekey

[InstallDelete]
Type: files; Name: "{userprograms}\Ergonomy.lnk"
