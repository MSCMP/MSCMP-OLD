; My Summer Car Multiplayer Installation script.
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

#define MyAppVersion 		"0.2.5-64bit" 	; the version of the mod - update with new releases.
#define MyConfiguration 	"Release" 		; if you want to build installer from different configuration change that variable possible configurations "Debug", "Release" or "Public Release"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; As long as you are not doing anything extreme touching just the variables above will be enough;
;

#define MyAppName 			"MSCMP"
#define MyAppPublisher		"MSCMP Team"
#define MyAppURL 			"http://mysummercar.mp"
#define MyAppExeName 		"MSCMP.exe"
#define OutputFileNamePattern "MSCMP_" + MyConfiguration + "_Setup_" + GetDateTimeString('ddmmyyyyhhnnss', '', '')

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C6F722AF-D388-428D-8C23-15008CD55B65}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf64}\{#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename={#OutputFileNamePattern}
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
Source: "..\..\bin\{#MyConfiguration}\{#MyAppExeName}"; DestDir: "{app}\bin\{#MyConfiguration}"; Flags: ignoreversion
Source: "..\..\bin\{#MyConfiguration}\MSCMPClient.dll"; DestDir: "{app}\bin\{#MyConfiguration}"; Flags: ignoreversion
Source: "..\..\bin\{#MyConfiguration}\MSCMPNetwork.dll"; DestDir: "{app}\bin\{#MyConfiguration}"; Flags: ignoreversion
Source: "..\..\bin\{#MyConfiguration}\MSCMPInjector.dll"; DestDir: "{app}\bin\{#MyConfiguration}"; Flags: ignoreversion
Source: "..\..\bin\{#MyConfiguration}\steam_api64.dll"; DestDir: "{app}\bin\{#MyConfiguration}"; Flags: ignoreversion
Source: "..\..\bin\{#MyConfiguration}\steam_appid.txt"; DestDir: "{app}\bin\{#MyConfiguration}"; Flags: ignoreversion
Source: "..\..\data\mpdata"; DestDir: "{app}\data"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\bin\{#MyConfiguration}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\bin\{#MyConfiguration}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\bin\{#MyConfiguration}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\bin\{#MyConfiguration}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\bin\{#MyConfiguration}\path.txt"
Type: files; Name: "{app}\bin\{#MyConfiguration}\clientLog.txt"
Type: files; Name: "{app}\bin\{#MyConfiguration}\unityLog.txt"
Type: files; Name: "{localappdata}\{#MyAppName}\clientLog.txt"
Type: files; Name: "{localappdata}\{#MyAppName}\unityLog.txt"
Type: dirifempty; Name: "{localappdata}\{#MyAppName}"