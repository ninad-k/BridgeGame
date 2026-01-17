; Script generated for Inno Setup
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Honor Bridge"
#define MyAppVersion "1.0"
#define MyAppPublisher "Sanjay"
#define MyAppURL "https://github.com/ninad-k/BridgeGame"
#define MyAppExeName "HonorBridge.Client.Wpf.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C6D2D7F3-9F31-4F2A-8B6A-91D8E9C7E5F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=HonorBridgeSetup_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Icon for the setup file itself (Optional, needs an .ico file)
; SetupIconFile=..\src\HonorBridge.Client.Wpf\Assets\icon.ico 

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; IMPORTANT: This path assumes you ran "dotnet publish" first!
Source: "..\src\HonorBridge.Client.Wpf\bin\Release\net9.0-windows\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\HonorBridge.Client.Wpf\bin\Release\net9.0-windows\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
