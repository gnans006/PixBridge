; PixBridge Inno Setup Installer Script
; Requires Inno Setup 6.x — https://jrsoftware.org/isinfo.php

#define MyAppName "PixBridge"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PixBridge Studio"
#define MyAppURL "http://192.168.10.10"
#define MyAppExeName "EventPhoto.Api.exe"
#define MyWorkerExeName "EventPhoto.Worker.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\publish\installer
OutputBaseFilename=PixBridge-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.17763
SetupIconFile=..\docs\icon.ico
UninstallDisplayIcon={app}\api\{#MyAppExeName}
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "installservice"; Description: "Install PixBridge as Windows Services (auto-start)"; GroupDescription: "Service Options:"; Flags: checked
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "..\publish\api\*"; DestDir: "{app}\api"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\worker\*"; DestDir: "{app}\worker"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\scripts\setup-postgresql.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\scripts\install-service.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\scripts\uninstall-service.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\docs\README.md"; DestDir: "{app}"; DestName: "README.md"; Flags: ignoreversion
Source: "..\docs\deployment-guide.md"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{group}\PixBridge Admin"; Filename: "{#MyAppURL}/admin"
Name: "{group}\Deployment Guide"; Filename: "{app}\docs\deployment-guide.md"
Name: "{group}\Uninstall PixBridge"; Filename: "{uninstallexe}"
Name: "{commondesktop}\PixBridge Admin"; Filename: "{#MyAppURL}/admin"; Tasks: desktopicon

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\setup-postgresql.ps1"""; Description: "Set up PostgreSQL database"; Flags: runhidden waituntilterminated
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\install-service.ps1"" -InstallDir ""{app}"""; Description: "Install Windows Services"; Flags: runhidden waituntilterminated; Tasks: installservice
Filename: "{#MyAppURL}/admin"; Description: "Open PixBridge Admin Panel"; Flags: shellexec postinstall skipifsilent nowait

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\uninstall-service.ps1"""; Flags: runhidden waituntilterminated

[Code]
function PostgreSQLInstalled: Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('powershell.exe', '-NoProfile -ExecutionPolicy Bypass -Command "Get-Command psql -ErrorAction SilentlyContinue | Out-Null"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := Result and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  if not PostgreSQLInstalled then begin
    MsgBox('PostgreSQL is required but not found in PATH.' + #13#10 +
           'Please install PostgreSQL 15+ and add it to your PATH before running this installer.' + #13#10 +
           'Download from: https://www.postgresql.org/download/windows/', mbError, MB_OK);
    Result := False;
  end
  else begin
    Result := True;
  end;
end;
