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
PrivilegesRequired=admin
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
Source: "{#MyPayload}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#MyProductName}"; Filename: "http://localhost:{code:GetPort}/"
Name: "{group}\Журналы"; Filename: "{#MyDataDir}\logs"
Name: "{group}\Удалить {#MyProductName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyProductName}"; Filename: "http://localhost:{code:GetPort}/"; Tasks: desktopicon

[Run]
; Открыть веб-интерфейс после установки — но только если служба поднялась.
Filename: "http://localhost:{code:GetPort}/"; Description: "Открыть веб-интерфейс"; Flags: postinstall shellexec nowait skipifsilent

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "StopService"
Filename: "{sys}\sc.exe"; Parameters: "delete ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "DeleteService"

[UninstallDelete]
; Ничего не удаляем сверх установленного: в {app}\config лежат рабочие
; настройки и компании, созданные при работе. Inno и так уберёт свои файлы
; (шаблоны, branding.json), а пользовательские останутся — их удаляет человек.
Type: dirifempty; Name: "{app}\config"
Type: dirifempty; Name: "{app}\public"

[Code]
var
  PortPage: TInputQueryWizardPage;
  PortValue: Integer;

{ ---------------------------------------------------------------- утилиты }

function RunHidden(const FileName, Params: string; var Code: Integer): Boolean;
begin
  Result := Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, Code);
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
    '  "installDir": "' + ExpandConstant('{app}') + '",' + #13#10 +
    '  "dataDir": "{#MyDataDir}",' + #13#10 +
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

function ReadPortFile: Integer;
var
  { AnsiString, а не string: такова сигнатура LoadStringFromFile
    в Pascal Script, с UnicodeString он не компилируется. }
  S: AnsiString;
begin
  Result := 8080;
  if LoadStringFromFile(ExpandConstant('{app}\config\install-port.txt'), S) then
    Result := StrToIntDef(Trim(String(S)), 8080);
end;

{ ---------------------------------------------------------------- страницы }

procedure InitializeWizard;
begin
  PortPage := CreateInputQueryPage(wpSelectDir,
    'Параметры сервера',
    'Порт веб-интерфейса',
    'На этом порту будет доступен веб-интерфейс. По умолчанию 8080 — ' +
    'если он занят, укажите другой.');
  PortPage.Add('Порт:', False);
  PortPage.Values[0] := '8080';
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
  if CurPageID = PortPage.ID then
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
  { Прошлую версию снимаем: иначе sc create вернёт «служба уже существует». }
  if ServiceExists('{#MyServiceName}') then
  begin
    StopService('{#MyServiceName}');
    RunHidden(ExpandConstant('{sys}\sc.exe'), 'delete "{#MyServiceName}"', Code);
    Sleep(1000);
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

  { Служба: автозапуск, перезапуск при падении. }
  BinPath := '"' + ExpandConstant('{app}\BackupDotNet.exe') + '" --service';
  if not RunHidden(ExpandConstant('{sys}\sc.exe'),
      'create "{#MyServiceName}" binPath= ' + BinPath + ' start= auto', Code) then
  begin
    MsgBox('Не удалось создать службу. Код: ' + IntToStr(Code), mbError, MB_OK);
    Exit;
  end;

  RunHidden(ExpandConstant('{sys}\sc.exe'),
    'description "{#MyServiceName}" "{#MyDescription}"', Code);
  RunHidden(ExpandConstant('{sys}\sc.exe'),
    'failure "{#MyServiceName}" reset= 86400 actions= restart/60000/restart/60000/restart/60000',
    Code);

  { Firewall и urlacl: без них служба не сможет слушать порт на всех адресах. }
  Rules := 'advfirewall firewall add rule name="{#MyProductName}" dir=in action=allow protocol=TCP localport=' +
    IntToStr(PortValue);
  RunHidden(ExpandConstant('{sys}\netsh.exe'), Rules, Code);

  RunHidden(ExpandConstant('{sys}\netsh.exe'),
    'http add urlacl url=http://+:' + IntToStr(PortValue) + '/ user="NT AUTHORITY\SYSTEM"', Code);

  RunHidden(ExpandConstant('{sys}\sc.exe'), 'start "{#MyServiceName}"', Code);

  { Даём службе подняться и проверяем, что она действительно работает. }
  Sleep(3000);
  if RunHidden(ExpandConstant('{sys}\sc.exe'), 'query "{#MyServiceName}"', Code) then
  begin
    if Code <> 0 then
      MsgBox('Служба установлена, но не запустилась. Проверьте журнал: ' +
        '{#MyDataDir}\logs', mbInformation, MB_OK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Code: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    { Правило firewall и резервация urlacl остались бы висеть после удаления.
      Без второй порт 8080 был бы навсегда закреплён за SYSTEM, и запустить
      агент консолью от обычного пользователя стало бы невозможно. }
    RunHidden(ExpandConstant('{sys}\netsh.exe'),
      'advfirewall firewall delete rule name="{#MyProductName}"', Code);
    RunHidden(ExpandConstant('{sys}\netsh.exe'),
      'http delete urlacl url=http://+:' + IntToStr(ReadPortFile) + '/', Code);
  end;
end;
