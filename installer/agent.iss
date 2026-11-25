; ============================================================================
;  Агентский установщик: только сбор данных и отправка на центральный сервер.
;
;  Отличия от серверного:
;    • нет каталога public (веб-интерфейса) — экономит ~1 МБ и убирает
;      лишнюю поверхность атаки;
;    • служба запускается с --service-remote, а не --service;
;    • нет порта, firewall и urlacl: агент сам стучится наружу;
;    • спрашивает адрес центрального сервера, токен и имя агента
;      и пишет config/remote.json.
;
;  Собирается tools\build-inno.ps1 с теми же /D, что и серверный.
; ============================================================================

#ifndef MyProductName
  #define MyProductName "BackupDotNet"
#endif
#ifndef MyVersion
  #define MyVersion "1.0.0"
#endif
#ifndef MyInstallDir
  #define MyInstallDir "C:\BackupDotNet"
#endif
#ifndef MyDataDir
  #define MyDataDir "C:\ProgramData\BackupDotNet"
#endif
#ifndef MyServiceNameRemote
  #define MyServiceNameRemote "BackupDotNetRemote"
#endif
#ifndef MyDescription
  #define MyDescription "Мониторинг бэкапов, доступности и железа"
#endif
#ifndef MyPayload
  #define MyPayload "..\artifacts\setup\payload"
#endif
#ifndef MyOutputDir
  #define MyOutputDir "..\artifacts\inno"
#endif
#ifndef MyAgentAppId
  #define MyAgentAppId "{{3C7B9E1A-6D4F-4B2C-8A5E-7F1D3C9B2A64}"
#endif
#ifndef MyIcon
  #define MyIcon "..\assets\agent-icon.ico"
#endif
#ifndef MyWizardImage
  #define MyWizardImage "..\assets\agent-icon-wizard.png"
#endif
#ifndef MyPrivileges
  #define MyPrivileges "admin"
#endif

[Setup]
AppId={#MyAgentAppId}
AppName={#MyProductName} (агент)
AppVersion={#MyVersion}
AppVerName={#MyProductName} {#MyVersion} — агент
AppPublisher={#MyProductName}
DefaultDirName={#MyInstallDir}
DefaultGroupName={#MyProductName}
DisableProgramGroupPage=yes
DisableDirPage=no
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyProductName}-Agent-{#MyVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired={#MyPrivileges}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyProductName} — агент
UninstallDisplayIcon={app}\BackupDotNet.exe
SetupIconFile={#MyIcon}
WizardSmallImageFile={#MyWizardImage}
VersionInfoVersion={#MyVersion}
VersionInfoProductName={#MyProductName}
VersionInfoProductVersion={#MyVersion}
VersionInfoDescription={#MyProductName} — установщик агента
VersionInfoCompany={#MyProductName}
MinVersion=6.3

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
; Без public: агенту веб-интерфейс не нужен. Исключаем каталог целиком,
; иначе Inno создаёт пустой public — лишний след в установке.
Source: "{#MyPayload}\*"; DestDir: "{app}"; Excludes: "public,install.ps1,README.md"; \
    Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Журналы агента"; Filename: "{#MyDataDir}\logs"
Name: "{group}\Удалить {#MyProductName} (агент)"; Filename: "{uninstallexe}"

[UninstallRun]
; Остановка службы — в коде: там можно дождаться её реальной остановки.
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#MyServiceNameRemote}"""; Flags: runhidden; RunOnceId: "StopAgent"

[UninstallDelete]
; Файлы, которые создал установщик. Без них каталог config не станет пустым
; и останется на диске после удаления.
Type: files; Name: "{app}\config\remote.json"
Type: files; Name: "{app}\config\settings.json"
Type: dirifempty; Name: "{app}\config"

[Code]
var
  ConnPage: TInputQueryWizardPage;

{ ---------------------------------------------------------------- утилиты }

function RunHidden(const FileName, Params: string; var Code: Integer): Boolean;
begin
  Result := Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

function ServiceExists(const Name: string): Boolean;
var
  Code: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'query "' + Name + '"', '', SW_HIDE,
    ewWaitUntilTerminated, Code);
  Result := (Code = 0);
end;

{ Проверяет состояние службы: 1 — работает, 0 — остановлена, -1 — нет службы. }
function ServiceState(const Name: string): Integer;
var
  Code: Integer;
  Tmp: string;
  Output: AnsiString;
  S: string;
begin
  Result := -1;
  Tmp := ExpandConstant('{tmp}\sc-state.txt');
  Exec(ExpandConstant('{cmd}'), '/c sc query "' + Name + '" > "' + Tmp + '"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
  Output := '';
  if not LoadStringFromFile(Tmp, Output) then
    Exit;
  DeleteFile(Tmp);
  S := UpperCase(String(Output));
  if Pos('RUNNING', S) > 0 then
    Result := 1
  else if Pos('STOPPED', S) > 0 then
    Result := 0;
end;

{ Останавливает службу и ждёт реальной остановки: иначе её процесс держит
  exe и деинсталлятор не сможет удалить файлы. }
function StopServiceAndWait(const Name: string): Boolean;
var
  Code: Integer;
  I: Integer;
begin
  Result := True;
  if ServiceState(Name) < 0 then
    Exit;

  RunHidden(ExpandConstant('{sys}\sc.exe'), 'stop "' + Name + '"', Code);

  for I := 1 to 30 do
  begin
    if ServiceState(Name) = 0 then
      Break;
    Sleep(500);
  end;

  Result := (ServiceState(Name) = 0);
end;

procedure StopService(const Name: string);
var
  Code: Integer;
begin
  if not ServiceExists(Name) then
    Exit;
  RunHidden(ExpandConstant('{sys}\sc.exe'), 'stop "' + Name + '"', Code);
  Sleep(2000);
end;

{ Экранирует строку для JSON: обратная косая и кавычка.
  Свой цикл вместо StringChangeEx: в Pascal Script эта функция капризна
  при вызове как оператор, а нам нужны ровно два символа. }
function JsonEsc(const S: string): string;
var
  I: Integer;
  C: Char;
  R: string;
begin
  R := '';
  for I := 1 to Length(S) do
  begin
    C := S[I];
    if C = '\' then
      R := R + '\\'
    else if C = '"' then
      R := R + '\"'
    else
      R := R + C;
  end;
  Result := R;
end;

function WriteRemoteJson: Boolean;
var
  Path: string;
  Json: string;
  Url, Token, AgentId: string;
begin
  Url := Trim(ConnPage.Values[0]);
  Token := Trim(ConnPage.Values[1]);
  AgentId := Trim(ConnPage.Values[2]);

  Path := ExpandConstant('{app}\config\remote.json');
  Json :=
    '{' + #13#10 +
    '  "centralUrl": "' + JsonEsc(Url) + '",' + #13#10 +
    '  "token": "' + JsonEsc(Token) + '",' + #13#10 +
    '  "agentId": "' + JsonEsc(AgentId) + '",' + #13#10 +
    '  "intervalSeconds": 300,' + #13#10 +
    '  "ignoreCertErrors": true,' + #13#10 +
    '  "timeoutSeconds": 60' + #13#10 +
    '}';
  Result := SaveStringToFile(Path, Json, False);
end;

{ Настройки приложения: без файла агент пишет журналы рядом с exe,
  а не в каталог данных, который создал установщик. }
function WriteSettingsJson: Boolean;
var
  Path: string;
  Json: string;
begin
  Path := ExpandConstant('{app}\config\settings.json');
  if FileExists(Path) then
  begin
    Result := True;
    Exit;
  end;

  Json :=
    '{' + #13#10 +
    '  "agent": {' + #13#10 +
    '    "name": "{#MyProductName}",' + #13#10 +
    '    "dataDir": "' + JsonEsc('{#MyDataDir}') + '",' + #13#10 +
    '    "publicDir": "public",' + #13#10 +
    '    "configDir": "config"' + #13#10 +
    '  }' + #13#10 +
    '}';
  Result := SaveStringToFile(Path, Json, False);
end;

{ ---------------------------------------------------------------- страницы }

procedure InitializeWizard;
var
  CentralParam, TokenParam, AgentParam: string;
begin
  ConnPage := CreateInputQueryPage(wpSelectDir,
    'Подключение к серверу',
    'Куда агент отправляет данные',
    'Адрес сервера и токен выдаёт веб-интерфейс центрального сервера: ' +
    'страница «Развёртывание» → кнопка «Агент вручную». ' +
    'Она создаст агента и покажет оба значения. Без них агент не заработает.');
  ConnPage.Add('Адрес сервера:', False);
  ConnPage.Add('Токен:', False);
  ConnPage.Add('Имя агента:', False);

  { Параметры для тихой установки из GPO:
    /CENTRAL=https://server:8080 /TOKEN=... /AGENT=имя }
  CentralParam := ExpandConstant('{param:CENTRAL|https://}');
  TokenParam := ExpandConstant('{param:TOKEN|}');
  AgentParam := ExpandConstant('{param:AGENT|}');
  if AgentParam = '' then
    AgentParam := GetComputerNameString();

  ConnPage.Values[0] := CentralParam;
  ConnPage.Values[1] := TokenParam;
  ConnPage.Values[2] := AgentParam;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Url, Token: string;
begin
  Result := True;
  if CurPageID = ConnPage.ID then
  begin
    Url := Trim(ConnPage.Values[0]);
    Token := Trim(ConnPage.Values[1]);

    if (Url = '') or (Url = 'https://') or (Url = 'http://') then
    begin
      MsgBox('Укажите адрес центрального сервера.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    { Токен короче 16 символов сервер всё равно отвергнет — лучше сказать
      это здесь, чем оставить человека с молчащим агентом. }
    if Length(Token) < 16 then
    begin
      MsgBox('Токен слишком короткий: нужно не меньше 16 символов.' + #13#10 + #13#10 +
        'Где взять: на сервере откройте «Развёртывание» → «Агент вручную», ' +
        'создайте агента и скопируйте токен оттуда целиком.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;

{ ---------------------------------------------------------------- установка }

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Code: Integer;
begin
  Result := '';
  if ServiceExists('{#MyServiceNameRemote}') then
  begin
    StopService('{#MyServiceNameRemote}');
    RunHidden(ExpandConstant('{sys}\sc.exe'), 'delete "{#MyServiceNameRemote}"', Code);
    Sleep(1000);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  BinPath: string;
begin
  if CurStep <> ssPostInstall then
    Exit;

  ForceDirectories('{#MyDataDir}');
  ForceDirectories('{#MyDataDir}\logs');

  if not WriteRemoteJson then
  begin
    MsgBox('Не удалось записать config\remote.json. Агент не запустится без него.',
      mbError, MB_OK);
    Exit;
  end;

  WriteSettingsJson;

  { Кавычки вокруг всего binPath: иначе sc.exe разбирает «--service-remote»
    как отдельный параметр и служба создаётся без него. }
#ifdef MyNoService
  { Тестовый режим: служба не создаётся. }
#else
  BinPath := '\"' + ExpandConstant('{app}\BackupDotNet.exe') + '\" --service-remote';
  if not RunHidden(ExpandConstant('{sys}\sc.exe'),
      'create "{#MyServiceNameRemote}" binPath= "' + BinPath + '" start= auto', Code) then
  begin
    MsgBox('Не удалось создать службу. Код: ' + IntToStr(Code), mbError, MB_OK);
    Exit;
  end;

  RunHidden(ExpandConstant('{sys}\sc.exe'),
    'description "{#MyServiceNameRemote}" "{#MyDescription} (агент)"', Code);
  RunHidden(ExpandConstant('{sys}\sc.exe'),
    'failure "{#MyServiceNameRemote}" reset= 86400 actions= restart/60000/restart/60000/restart/60000',
    Code);

  { Firewall не нужен: агент сам открывает исходящие соединения. }
  RunHidden(ExpandConstant('{sys}\sc.exe'), 'start "{#MyServiceNameRemote}"', Code);
  Sleep(2000);
#endif
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Code: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    { Дожидаемся остановки службы: её процесс держит exe, и без ожидания
      деинсталлятор оставит файлы на диске. }
    if StopServiceAndWait('{#MyServiceNameRemote}') then
      RunHidden(ExpandConstant('{sys}\sc.exe'), 'delete "{#MyServiceNameRemote}"', Code)
    else
      MsgBox('Служба {#MyServiceNameRemote} не остановилась за 15 секунд. ' +
        'Файлы могут остаться на диске — остановите службу вручную.',
        mbInformation, MB_OK);
  end;
end;
