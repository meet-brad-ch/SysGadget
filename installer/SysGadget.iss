; The SysGadget installer (Inno Setup 6.3 or later).
;
;   powershell -File build-installer.ps1          (publishes, then compiles this script)
;
; or by hand:
;
;   dotnet publish src\SysGadget.App\SysGadget.App.csproj -c Release -r win-x64 --self-contained true -o out\publish
;   iscc /DAppVersion=1.2.3 /DNumVersion=1.2.3.0 installer\SysGadget.iss
;
; It installs the published app (self-contained: no .NET needed) and asks whether SysGadget starts
; with Windows: that task writes the same per-user Run value as Settings > "Start SysGadget with Windows".
; The settings in %APPDATA%\SysGadget stay on an uninstall. The setup is written to out\installer.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
#ifndef NumVersion
  #define NumVersion "0.0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\out\publish"
#endif

[Setup]
AppId={{6E2B9C1A-4F7D-4E21-9B8C-3D5A7F0E1C42}
AppName=SysGadget
AppVersion={#AppVersion}
AppVerName=SysGadget {#AppVersion}
AppPublisher=Brad Chamberlain
VersionInfoVersion={#NumVersion}
DefaultDirName={autopf}\SysGadget
DefaultGroupName=SysGadget
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\out\installer
OutputBaseFilename=SysGadget-Setup-{#AppVersion}
SetupIconFile=..\src\SysGadget.App\SysGadget.ico
UninstallDisplayIcon={app}\SysGadget.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; a running gadget holds its files: setup asks to close it (the app's single-instance mutex)
AppMutex=Local\SysGadget.SingleInstance
; the start-with-Windows entry belongs to the user who installs
UsedUserAreasWarning=no

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: autostart; Description: "Start SysGadget with Windows"; GroupDescription: "Start-up:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SysGadget"; Filename: "{app}\SysGadget.exe"
Name: "{autodesktop}\SysGadget"; Filename: "{app}\SysGadget.exe"; Tasks: desktopicon

[Registry]
; the same per-user Run value the Settings checkbox writes (AutostartRegistration)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SysGadget"; ValueData: """{app}\SysGadget.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\SysGadget.exe"; Description: "Start SysGadget"; Flags: nowait postinstall skipifsilent runasoriginaluser
