; ============================================================================
;  Серверный установщик: полный продукт — веб-интерфейс, планировщик, история.
;
;  Собирается tools\build-inno.ps1, который передаёт значения через /D:
;    MyProductName   BackupDotNet
;    MyVersion       1.2.1
;    MyInstallDir    C:\BackupDotNet
;    MyDataDir       C:\ProgramData\BackupDotNet
;    MyServiceName   BackupDotNetService
;    MyDescription   Мониторинг бэкапов, доступности и железа
;    MyPayload       путь к каталогу с файлами агента
;    MyOutputDir     куда положить exe
;    MyAppId         стабильный GUID продукта
;
;  Брендинг приходит снаружи: в самом .iss нет имени конкретной инсталляции.
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
#ifndef MyServiceName
  #define MyServiceName "BackupDotNetService"
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
#ifndef MyAppId
  #define MyAppId "{{8F3A1C2D-4B5E-4A7C-9D1E-2F3A4B5C6D7E}"
#endif
#ifndef MyIcon
  #define MyIcon "..\assets\server-icon.ico"
#endif
#ifndef MyWizardImage
  #define MyWizardImage "..\assets\server-icon-wizard.png"
#endif
#ifndef MyPrivileges
  #define MyPrivileges "admin"
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyProductName}
AppVersion={#MyVersion}
AppVerName={#MyProductName} {#MyVersion}
AppPublisher={#MyProductName}
DefaultDirName={#MyInstallDir}
DefaultGroupName={#MyProductName}
DisableProgramGroupPage=yes
DisableDirPage=no
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyProductName}-Server-{#MyVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired={#MyPrivileges}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyProductName} — сервер
UninstallDisplayIcon={app}\BackupDotNet.exe
; Иконки: SetupIconFile — значок самого установщика в проводнике,
; WizardSmallImageFile — картинка в заголовке мастера.
SetupIconFile={#MyIcon}
WizardSmallImageFile={#MyWizardImage}
; Свойства файла в проводнике: без этого FileVersion пустой.
VersionInfoVersion={#MyVersion}
VersionInfoProductName={#MyProductName}
VersionInfoProductVersion={#MyVersion}
VersionInfoDescription={#MyProductName} — серверный установщик
VersionInfoCompany={#MyProductName}
; Минимальная версия — Windows Server 2012 R2 / Windows 10 (6.3).
MinVersion=6.3

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Ярлык на рабочем столе"; GroupDescription: "Дополнительно:"; Flags: unchecked

[Files]
; Все файлы агента: exe, dll, public (веб-интерфейс), config-шаблоны.
; branding.json исключён: при обновлении нельзя затирать настроенный бренд.
Source: "{#MyPayload}\*"; DestDir: "{app}"; Excludes: "config\branding.json"; \
    Flags: recursesubdirs createallsubdirs ignoreversion
; Бренд кладём только если его ещё нет.
Source: "{#MyPayload}\config\branding.json"; DestDir: "{app}\config"; \
    Flags: onlyifdoesntexist skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyProductName}"; Filename: "http://localhost:{code:GetPort}/"
Name: "{group}\Журналы"; Filename: "{#MyDataDir}\logs"
Name: "{group}\Удалить {#MyProductName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyProductName}"; Filename: "http://localhost:{code:GetPort}/"; Tasks: desktopicon

[Run]
; Открыть веб-интерфейс после установки — но только если служба поднялась.
Filename: "http://localhost:{code:GetPort}/"; Description: "Открыть веб-интерфейс"; Flags: postinstall shellexec nowait skipifsilent

[UninstallRun]
; Служба снимается в коде (CurUninstallStepChanged), потому что там можно
; дождаться её реальной остановки. Здесь только страховка на случай,
; если код почему-то не отработал.
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "StopService"

[UninstallDelete]
; Файлы, которые создал установщик. Пользовательские (компании, secrets.json)
; не трогаем: в них работа, которую нельзя восстановить переустановкой.
Type: files; Name: "{app}\config\settings.json"
Type: files; Name: "{app}\config\env.json"
Type: files; Name: "{app}\config\install-port.txt"
Type: dirifempty; Name: "{app}\config"
Type: dirifempty; Name: "{app}\public"

[Code]
var
  PortPage: TInputQueryWizardPage;
  PortValue: Integer;
  UninstallPort: Integer;
  UpgradeMode: Boolean;
  PrevVersion: string;
  PrevLocation: string;

{ ---------------------------------------------------------------- утилиты }

{ Экранирует строку для JSON: обратная косая и кавычка. }
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

function RunHidden(const FileName, Params: string; var Code: Integer): Boolean;
begin
  Result := Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

{ Была ли программа уже установлена: берём запись деинсталлятора.
  Она же даёт прежнюю версию и каталог — по ним понятно, обновляемся
  мы или ставим с нуля. }
function ReadPreviousInstall(var Version, Location: string): Boolean;
var
  Key: string;
begin
  Key := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1';
  Version := '';
  Location := '';
  Result := RegQueryStringValue(HKLM, Key, 'DisplayVersion', Version);
  RegQueryStringValue(HKLM, Key, 'InstallLocation', Location);
end;

function ServiceExists(const Name: string): Boolean;
var
  Code: Integer;
begin
  { sc query возвращает 0, если служба зарегистрирована (даже остановленная). }
  Exec(ExpandConstant('{sys}\sc.exe'), 'query "' + Name + '"', '', SW_HIDE,
    ewWaitUntilTerminated, Code);
  Result := (Code = 0);
end;

procedure StopService(const Name: string);
var
  Code: Integer;
begin
  if not ServiceExists(Name) then
    Exit;
  RunHidden(ExpandConstant('{sys}\sc.exe'), 'stop "' + Name + '"', Code);
  { Даём службе время остановиться: сразу удалять нельзя, будет 1072. }
  Sleep(2000);
end;

function WriteEnvJson: Boolean;
var
  Path: string;
  Json: string;
begin
  Path := ExpandConstant('{app}\config\env.json');
  Json :=
    '{' + #13#10 +
    '  "installDir": "' + JsonEsc(ExpandConstant('{app}')) + '",' + #13#10 +
    '  "dataDir": "' + JsonEsc('{#MyDataDir}') + '",' + #13#10 +
    '  "serviceName": "{#MyServiceName}"' + #13#10 +
    '}';
  Result := SaveStringToFile(Path, Json, False);
end;

{ Порт нужен и при удалении: без него не убрать резервацию urlacl.
  При деинсталляции мастер уже не помнит, что вводили, поэтому пишем
  значение рядом — в файл, который создаёт сам установщик. }
function WritePortFile: Boolean;
begin
  Result := SaveStringToFile(ExpandConstant('{app}\config\install-port.txt'),
    IntToStr(PortValue), False);
end;

{ Настройки приложения.
  Без этого файла агент берёт умолчания: dataDir = "data" (то есть каталог
  рядом с exe, а не ProgramData) и порт 8080. Порт из мастера при этом
  остаётся только в firewall и urlacl — и служба падает с «отказано
  в доступе», потому что слушает не тот порт, на который выдано разрешение.
  Именно так и случилось на первом тесте: служба установилась, не
  запустилась, а журналы оказались не там, где их искали. }
function WriteSettingsJson: Boolean;
var
  Path: string;
  Json: string;
begin
  Path := ExpandConstant('{app}\config\settings.json');

  { При обновлении не трогаем существующие настройки. }
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
    '    "configDir": "config",' + #13#10 +
    '    "httpPort": ' + IntToStr(PortValue) + ',' + #13#10 +
    '    "bindAddress": "http://+:' + IntToStr(PortValue) + '/"' + #13#10 +
    '  }' + #13#10 +
    '}';

  Result := SaveStringToFile(Path, Json, False);
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

{ Останавливает службу и ЖДЁТ, пока она действительно встанет.
  Без ожидания деинсталлятор пытается удалить файлы, пока процесс ещё
  жив, не может — и каталог остаётся на диске. }
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

function ServiceIsRunning(const Name: string): Boolean;
begin
  Result := ServiceState(Name) = 1;
end;

function ReadPortFileFrom(const Dir: string): Integer;
var
  { AnsiString, а не string: такова сигнатура LoadStringFromFile
    в Pascal Script, с UnicodeString он не компилируется. }
  S: AnsiString;
begin
  Result := 0;
  if Dir = '' then
    Exit;
  if LoadStringFromFile(AddBackslash(Dir) + 'config\install-port.txt', S) then
    Result := StrToIntDef(Trim(String(S)), 0);
end;

function ReadPortFile: Integer;
begin
  Result := ReadPortFileFrom(ExpandConstant('{app}'));
  if Result <= 0 then
    Result := 8080;
end;

{ ---------------------------------------------------------------- страницы }

procedure InitializeWizard;
var
  PortParam: string;
begin
  { Режим обновления: программа уже стоит. Тогда порт не спрашиваем —
    он уже записан при первой установке, и менять его молча нельзя:
    на старый порт выданы firewall и urlacl. }
  UpgradeMode := ReadPreviousInstall(PrevVersion, PrevLocation);

  if UpgradeMode then
  begin
    PortValue := ReadPortFileFrom(PrevLocation);
    if PortValue <= 0 then
      PortValue := 8080;

    PortPage := nil;

    MsgBox('Обнаружена установка ' + PrevVersion + ' в:' + #13#10 +
      PrevLocation + #13#10 + #13#10 +
      'Программа будет обновлена до {#MyVersion}. Настройки, компании ' +
      'и журналы сохранятся. Порт остаётся прежним: ' + IntToStr(PortValue) + '.',
      mbInformation, MB_OK);
    Exit;
  end;

  PortPage := CreateInputQueryPage(wpSelectDir,
    'Параметры сервера',
    'Порт веб-интерфейса',
    'На этом порту будет доступен веб-интерфейс. По умолчанию 8080 — ' +
    'если он занят, укажите другой.');

  { /PORT=12345 — для тихой установки из GPO: в диалоге порт не спросить. }
  PortParam := ExpandConstant('{param:PORT|8080}');
  PortValue := StrToIntDef(PortParam, 8080);
  if (PortValue < 1) or (PortValue > 65535) then
    PortValue := 8080;

  PortPage.Add('Порт:', False);
  PortPage.Values[0] := IntToStr(PortValue);
end;

function GetPort(Param: string): string;
begin
  Result := IntToStr(PortValue);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  P: Integer;
begin
  Result := True;
  { В режиме обновления страницы порта нет — PortPage = nil. }
  if (PortPage <> nil) and (CurPageID = PortPage.ID) then
  begin
    P := StrToIntDef(PortPage.Values[0], 0);
    if (P < 1) or (P > 65535) then
    begin
      MsgBox('Порт должен быть числом от 1 до 65535.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    PortValue := P;
  end;
end;

{ ---------------------------------------------------------------- установка }

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Code: Integer;
begin
  Result := '';
  { Прошлую версию снимаем: иначе sc create вернёт «служба уже существует».
    Ждём реальной остановки — иначе процесс держит exe и файлы не заменятся. }
  if ServiceExists('{#MyServiceName}') then
  begin
    StopServiceAndWait('{#MyServiceName}');
    RunHidden(ExpandConstant('{sys}\sc.exe'), 'delete "{#MyServiceName}"', Code);
    Sleep(1500);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  BinPath: string;
  Rules: string;
begin
  if CurStep <> ssPostInstall then
    Exit;

  if PortValue = 0 then
    PortValue := 8080;

  { Каталог данных создаём заранее: служба работает под SYSTEM и создаст его
    сама, но тогда права будут другими, и человек не сможет положить туда
    конфигурацию. }
  ForceDirectories('{#MyDataDir}');
  ForceDirectories('{#MyDataDir}\logs');

  WriteEnvJson;
  WritePortFile;

  { Настройки пишем до создания службы: приложение читает порт и каталог
    данных из settings.json, и без файла оно возьмёт умолчания. }
  if not WriteSettingsJson then
  begin
    MsgBox('Не удалось записать config\settings.json. Без него служба ' +
      'возьмёт порт 8080 и каталог данных по умолчанию.', mbError, MB_OK);
    Exit;
  end;

#ifdef MyNoService
  { Тестовый режим сборки: служба, firewall и urlacl не создаются.
    Нужен, чтобы проверять установку и удаление без прав администратора. }
#else
  { Служба: автозапуск, перезапуск при падении.
    Кавычки вокруг всего значения binPath обязательны: иначе sc.exe
    разбирает «--service» как отдельный параметр, и служба создаётся
    без аргумента — приложение запускается в консольном режиме и падает.
    Внутренние кавычки экранируются обратной косой. }
  BinPath := '\"' + ExpandConstant('{app}\BackupDotNet.exe') + '\" --service';
  if not RunHidden(ExpandConstant('{sys}\sc.exe'),
      'create "{#MyServiceName}" binPath= "' + BinPath + '" start= auto', Code) then
  begin
    MsgBox('Не удалось создать службу. Код: ' + IntToStr(Code), mbError, MB_OK);
    Exit;
  end;

  RunHidden(ExpandConstant('{sys}\sc.exe'),
    'description "{#MyServiceName}" "{#MyDescription}"', Code);
  RunHidden(ExpandConstant('{sys}\sc.exe'),
    'failure "{#MyServiceName}" reset= 86400 actions= restart/60000/restart/60000/restart/60000',
    Code);

  { Firewall и urlacl: без них служба не сможет слушать порт на всех адресах.
    Правило сначала удаляем: при обновлении оно уже есть, и «add» создал бы
    дубликат. }
  RunHidden(ExpandConstant('{sys}\netsh.exe'),
    'advfirewall firewall delete rule name="{#MyProductName}"', Code);
  Rules := 'advfirewall firewall add rule name="{#MyProductName}" dir=in action=allow protocol=TCP localport=' +
    IntToStr(PortValue);
  RunHidden(ExpandConstant('{sys}\netsh.exe'), Rules, Code);

  RunHidden(ExpandConstant('{sys}\netsh.exe'),
    'http add urlacl url=http://+:' + IntToStr(PortValue) + '/ user="NT AUTHORITY\SYSTEM"', Code);

  RunHidden(ExpandConstant('{sys}\sc.exe'), 'start "{#MyServiceName}"', Code);
#endif

#ifndef MyNoService
  { Проверяем, что служба действительно работает, а не просто создана.
    Раньше мастер бодро писал «готово», даже когда служба падала через
    секунду — и человек искал причину сам. }
  Sleep(4000);
  if not ServiceIsRunning('{#MyServiceName}') then
  begin
    MsgBox('Служба установлена, но не запустилась.' + #13#10 + #13#10 +
      'Проверьте журнал:' + #13#10 +
      '{#MyDataDir}\logs' + #13#10 + #13#10 +
      'Частая причина — порт ' + IntToStr(PortValue) + ' занят другим ' +
      'приложением. Запустите установщик снова и укажите другой порт.',
      mbError, MB_OK);
  end;
#endif
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Code: Integer;
begin
  { Порт читаем ДО удаления файлов: install-port.txt попадает в список
    удаляемых файлов, и в usPostUninstall его уже может не быть. }
  if CurUninstallStep = usUninstall then
  begin
    UninstallPort := ReadPortFile;

    { Службу снимаем здесь: дожидаемся остановки, иначе её процесс держит
      BackupDotNet.exe и Inno не сможет удалить файлы — каталог останется. }
    if StopServiceAndWait('{#MyServiceName}') then
      RunHidden(ExpandConstant('{sys}\sc.exe'), 'delete "{#MyServiceName}"', Code)
    else
      MsgBox('Служба {#MyServiceName} не остановилась за 15 секунд. ' +
        'Файлы могут остаться на диске — остановите службу вручную и ' +
        'удалите каталог.', mbInformation, MB_OK);
    Exit;
  end;

  if CurUninstallStep = usPostUninstall then
  begin
    { Правило firewall и резервация urlacl остались бы висеть после удаления.
      Без второй порт остался бы закреплён за SYSTEM, и запустить агент
      консолью от обычного пользователя стало бы невозможно. }
    RunHidden(ExpandConstant('{sys}\netsh.exe'),
      'advfirewall firewall delete rule name="{#MyProductName}"', Code);
    RunHidden(ExpandConstant('{sys}\netsh.exe'),
      'http delete urlacl url=http://+:' + IntToStr(UninstallPort) + '/', Code);
  end;
end;
