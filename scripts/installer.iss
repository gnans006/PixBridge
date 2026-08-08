; PixBridge Inno Setup Installer Script
; Requires Inno Setup 6.x — https://jrsoftware.org/isinfo.php

#define MyAppName "PixBridge"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PixBridge Studio"
#define MyAppURL "http://localhost:5000"
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
Name: "fixnetwork"; Description: "Configure firewall and set Wi-Fi profile to Private (recommended)"; GroupDescription: "Network:"; Flags: checked
Name: "desktopicon"; Description: "Create a desktop shortcut to the Admin Panel"; GroupDescription: "Additional icons:"; Flags: checked

[Files]
Source: "..\publish\api\*"; DestDir: "{app}\api"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\worker\*"; DestDir: "{app}\worker"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\face-recognition\*"; DestDir: "{app}\face-recognition"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\setup\setup-postgresql.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\publish\setup\install-service.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\publish\setup\uninstall-service.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\publish\setup\setup-face-search.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\publish\setup\fix-network-access.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\publish\setup\repair.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "..\publish\README.md"; DestDir: "{app}"; DestName: "README.md"; Flags: ignoreversion
Source: "..\publish\deployment-guide.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\publish\CLIENT-DEPLOYMENT-GUIDE.md"; DestDir: "{app}\docs"; DestName: "CLIENT-DEPLOYMENT-GUIDE.md"; Flags: ignoreversion

[Dirs]
Name: "{app}\logs"

[Icons]
Name: "{group}\PixBridge Admin"; Filename: "{#MyAppURL}/admin"
Name: "{group}\Deployment Guide"; Filename: "{app}\docs\deployment-guide.md"
Name: "{group}\Uninstall PixBridge"; Filename: "{uninstallexe}"
Name: "{commondesktop}\PixBridge Admin"; Filename: "{#MyAppURL}/admin"; Tasks: desktopicon

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\setup-postgresql.ps1"" -LogFile ""{app}\logs\setup-db.log"""; StatusMsg: "Setting up PostgreSQL database..."; Description: "Set up PostgreSQL database"; Flags: waituntilterminated
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\fix-network-access.ps1"""; StatusMsg: "Configuring firewall and Wi-Fi..."; Description: "Configure firewall and Wi-Fi profile"; Flags: waituntilterminated; Tasks: fixnetwork
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\install-service.ps1"" -InstallDir ""{app}"" -LogFile ""{app}\logs\setup-services.log"""; StatusMsg: "Installing Windows Services..."; Description: "Install Windows Services"; Flags: waituntilterminated; Tasks: installservice

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\uninstall-service.ps1"""; Flags: runhidden waituntilterminated

[Code]

var
  DbSetupOk:      Boolean;
  ServiceSetupOk: Boolean;

{ ────────────────────────────────────────────────────────────────────────────
  Helpers
  ──────────────────────────────────────────────────────────────────────────── }

function FileContains(const FilePath, SubStr: String): Boolean;
var
  Lines: TArrayOfString;
  i: Integer;
begin
  Result := False;
  if not LoadStringsFromFile(FilePath, Lines) then Exit;
  for i := 0 to GetArrayLength(Lines) - 1 do
    if Pos(SubStr, Lines[i]) > 0 then begin
      Result := True;
      Exit;
    end;
end;

function ReadLastLines(const FilePath: String; Count: Integer): String;
var
  Lines: TArrayOfString;
  i, Start: Integer;
begin
  Result := '(log not available: ' + FilePath + ')';
  if not LoadStringsFromFile(FilePath, Lines) then Exit;
  Result := '';
  Start := GetArrayLength(Lines) - Count;
  if Start < 0 then Start := 0;
  for i := Start to GetArrayLength(Lines) - 1 do
    Result := Result + Lines[i] + #13#10;
end;

{ ────────────────────────────────────────────────────────────────────────────
  Pre-install checks
  ──────────────────────────────────────────────────────────────────────────── }

function PostgreSQLInstalled: Boolean;
var
  Code: Integer;
begin
  Exec('powershell.exe',
    '-NoProfile -ExecutionPolicy Bypass -Command ' +
    '"if (Get-Command psql -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
  Result := (Code = 0);
end;

function Port5000Free: Boolean;
var
  Code: Integer;
begin
  Exec('powershell.exe',
    '-NoProfile -ExecutionPolicy Bypass -Command ' +
    '"if (Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue) ' +
    '{ exit 1 } else { exit 0 }"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
  Result := (Code = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result     := True;
  DbSetupOk  := True;
  ServiceSetupOk := True;

  if not PostgreSQLInstalled then begin
    MsgBox(
      'PostgreSQL is not found in PATH.' + #13#10 + #13#10 +
      'Please install PostgreSQL 15+ from:' + #13#10 +
      '  https://www.postgresql.org/download/windows/' + #13#10 + #13#10 +
      'During install, tick "Add to PATH", then re-run this installer.',
      mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if not Port5000Free then begin
    if MsgBox(
      'Port 5000 is already in use on this machine.' + #13#10 +
      'PixBridge API requires port 5000.' + #13#10 + #13#10 +
      'Stop the application using port 5000, then re-run this installer.' + #13#10 + #13#10 +
      'Continue anyway (not recommended)?',
      mbConfirmation, MB_YESNO) = IDNO then begin
      Result := False;
      Exit;
    end;
  end;
end;

{ ────────────────────────────────────────────────────────────────────────────
  Post-install: read log files, detect failures, show actionable dialog
  ──────────────────────────────────────────────────────────────────────────── }

procedure CurStepChanged(CurStep: TSetupStep);
var
  LogDir:     String;
  DbLog:      String;
  SvcLog:     String;
  Msg:        String;
  DbOk:       Boolean;
  SvcOk:      Boolean;
  LanIp:      String;
  GuestUrl:   String;
  TmpIpFile:  String;
  IpLines:    TArrayOfString;
  ErrorCode:  Integer;
begin
  if CurStep <> ssPostInstall then Exit;

  LogDir := ExpandConstant('{app}\logs');
  DbLog  := LogDir + '\setup-db.log';
  SvcLog := LogDir + '\setup-services.log';

  // Check DB log — success marker written by setup-postgresql.ps1
  DbOk := FileExists(DbLog) and FileContains(DbLog, 'successfully');

  // Check service log — success marker written by install-service.ps1
  SvcOk := FileExists(SvcLog) and FileContains(SvcLog, 'Running');

  if DbOk and SvcOk then begin
    // Detect current LAN IP to show guest URL
    LanIp := '';
    Exec('powershell.exe',
      '-NoProfile -ExecutionPolicy Bypass -Command ' +
      '"(Get-NetIPAddress -AddressFamily IPv4 | ' +
      'Where-Object { $_.IPAddress -match \"^(192\.168|10\.|172\.(1[6-9]|2[0-9]|3[01]))\"}  | ' +
      'Select-Object -First 1 -ExpandProperty IPAddress)"',
      '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);

    // Fallback: read from fix-network-access log if psql call above can''t return a value
    // (Inno Setup Exec doesn''t capture stdout — use a temp file instead)
    TmpIpFile := ExpandConstant('{tmp}\pixbridge_ip.txt');
    Exec('powershell.exe',
      '-NoProfile -ExecutionPolicy Bypass -Command ' +
      '"(Get-NetIPAddress -AddressFamily IPv4 | ' +
      'Where-Object { $_.IPAddress -match ' +
      "'" + '^(192\.168|10\.|172\.(1[6-9]|2[0-9]|3[01]))' + "'" + ' } | ' +
      'Select-Object -First 1 -ExpandProperty IPAddress) | ' +
      'Set-Content \"' + TmpIpFile + '\""',
      '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);

    if FileExists(TmpIpFile) then begin
      LoadStringsFromFile(TmpIpFile, IpLines);
      if GetArrayLength(IpLines) > 0 then
        LanIp := Trim(IpLines[0]);
    end;

    if LanIp <> '' then
      GuestUrl := 'http://' + LanIp + ':5000'
    else
      GuestUrl := '(Connect to Wi-Fi first, then check System → Network Settings)';

    MsgBox(
      '✔ PixBridge installed successfully!' + #13#10 + #13#10 +
      '── ADMIN ACCESS (this machine) ──────────────' + #13#10 +
      '  http://localhost:5000/admin' + #13#10 +
      '  Username : admin' + #13#10 +
      '  Password : Admin@1234!' + #13#10 +
      '  ⚠ Change the password on first login!' + #13#10 + #13#10 +
      '── GUEST ACCESS (phones on same Wi-Fi) ──────' + #13#10 +
      '  ' + GuestUrl + #13#10 +
      '  (Share the event QR code from the admin panel)' + #13#10 + #13#10 +
      '── SHORTCUTS ────────────────────────────────' + #13#10 +
      '  Desktop  : PixBridge Admin shortcut created' + #13#10 +
      '  Start Menu: PixBridge → PixBridge Admin',
      mbInformation, MB_OK);

    // Auto-open admin panel in default browser
    ShellExec('open', 'http://localhost:5000/admin', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Exit;
  end;

  // Build failure message
  Msg := 'Installation completed with errors.' + #13#10 +
         'Logs are saved to: ' + LogDir + #13#10 + #13#10;

  if not DbOk then begin
    Msg := Msg +
      '[DATABASE SETUP FAILED]' + #13#10 +
      ReadLastLines(DbLog, 6) + #13#10 +
      'Fix: Ensure PostgreSQL is running and the postgres password is correct.' + #13#10 +
      'Then rerun: scripts\repair.ps1 -Step DB' + #13#10 + #13#10;
  end;

  if not SvcOk then begin
    Msg := Msg +
      '[SERVICE INSTALLATION FAILED]' + #13#10 +
      ReadLastLines(SvcLog, 6) + #13#10 +
      'Fix: Check that port 5000 is free and appsettings.Production.json has a JWT secret.' + #13#10 +
      'Then rerun: scripts\repair.ps1 -Step Services' + #13#10;
  end;

  MsgBox(Msg, mbError, MB_OK);
end;
