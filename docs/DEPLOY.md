# Установка и развёртывание

## Требования

- Windows Server 2012 R2 / Windows 10 и новее
- .NET Framework 4.8 (входит в состав Windows 10 1903+ и Server 2019+)
- Права администратора — для установки службы и urlacl

Сторонние компоненты **не требуются**. Опционально (агент сам их находит):
`smartctl`, `storcli`/`MegaCli`, `HPSSACLI`, `ipmitool`.

## Сборка

```powershell
git clone <репозиторий> BackupDotNet
cd BackupDotNet
dotnet build BackupDotNet.sln -c Release
```

Результат — `src\BackupDotNet.App\bin\Release\`:

```
BackupDotNet.exe
BackupDotNet.Core.dll
BackupDotNet.Web.dll
BackupDotNet.Deploy.dll
BackupDotNet.Manage.dll
public\            ← веб-интерфейс
config\*.example.json
```

`dotnet build` собирает под .NET Framework 4.8; MSBuild из Visual Studio тоже подходит.

## Установка вручную

```powershell
# 1. Каталог программы
$dir = 'C:\BackupDotNet'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
Copy-Item 'src\BackupDotNet.App\bin\Release\*' $dir -Recurse -Force

# 2. Конфигурация из шаблонов
Push-Location "$dir\config"
Copy-Item settings.example.json settings.json
Copy-Item branding.example.json branding.json
Copy-Item env.example.json env.json
Copy-Item secrets.example.json secrets.json
Pop-Location

# 3. Каталог данных (для службы — обязательно вне папки программы)
New-Item -ItemType Directory -Force -Path 'C:\ProgramData\BackupDotNet' | Out-Null

# 4. Проверка в консоли (до установки службы)
& "$dir\BackupDotNet.exe" --console
```

Откройте <http://localhost:8080/>, войдите как `admin`, пароль — в `auth\AGENT.md`.
**Смените пароль** через Настройки → Смена пароля.

### Пути для службы

Служба работает под `LocalSystem`, поэтому относительные пути (`data`, `public`)
указывают не туда, куда ожидается. Задайте абсолютные в `config\env.json`:

```json
{
  "installDir": "C:\\BackupDotNet\\",
  "dataDir": "C:\\ProgramData\\BackupDotNet",
  "serviceName": "BackupDotNetService",
  "serviceNameRemote": "BackupDotNetRemote"
}
```

### Регистрация службы

```powershell
$dir = 'C:\BackupDotNet'

sc.exe create BackupDotNetService binPath= "`"$dir\BackupDotNet.exe`" --service" start= auto
sc.exe description BackupDotNetService "Мониторинг бэкапов, доступности и железа"
sc.exe failure BackupDotNetService reset= 86400 actions= restart/60000/restart/60000/restart/60000

# urlacl, чтобы служба могла слушать порт на всех интерфейсах
netsh http add urlacl url=http://+:8080/ user="NT AUTHORITY\SYSTEM"

# правило брандмауэра
New-NetFirewallRule -DisplayName "BackupDotNet" -Direction Inbound -Protocol TCP `
  -LocalPort 8080 -Action Allow

sc.exe start BackupDotNetService
```

Проверка:

```powershell
Get-Service BackupDotNetService
Invoke-WebRequest http://localhost:8080/api/health -UseBasicParsing
Get-Content 'C:\ProgramData\BackupDotNet\logs\BackupDotNet_*.log' -Tail 20
```

## Удаление службы

```powershell
sc.exe stop BackupDotNetService
sc.exe delete BackupDotNetService
netsh http delete urlacl url=http://+:8080/
Remove-NetFirewallRule -DisplayName "BackupDotNet"
```

Каталог данных не удаляется — история сохраняется.

## Обновление

```powershell
sc.exe stop BackupDotNetService
Copy-Item 'новые\файлы\*' 'C:\BackupDotNet' -Recurse -Force   # config\ не перетираем
sc.exe start BackupDotNetService
```

`config\` и `data\` переносятся как есть. Совместимость форматов данных — обратная:
новая версия читает старые файлы состояния.

## Развёртывание на другие машины (WinRM)

Интерфейс: **Развёртывание**. Что умеет:

1. **Учётные данные** — доменные или локальные, хранятся зашифрованными.
2. **Сканирование** — диапазон адресов или список из AD, проверка доступности WinRM.
3. **Проба** — проверка прав, версии .NET, свободного места на целевой машине.
4. **Установка** — копирование файлов, создание каталогов, регистрация службы, urlacl.
5. **Удаление** — остановка и снятие службы.
6. **Журнал заданий** — что и куда установлено, с результатом.

Требования на целевой машине:

```powershell
Enable-PSRemoting -Force
winrm quickconfig -q
```

Порт 5985 (HTTP) или 5986 (HTTPS), группа `Remote Management Users`.

## Удалённые агенты (филиалы)

Схема: один **центральный** агент с веб-интерфейсом и N **филиальных**, которые
не имеют интерфейса и только выполняют проверки.

### На центральном

1. **Настройки → Удалённые агенты → Добавить**: имя филиала, идентификатор.
2. Скопируйте токен — он показывается один раз.
3. В карточке компании укажите `remoteAgent` = идентификатор филиала.

### На филиале

```powershell
# config\remote.json
@'
{
  "centralUrl": "https://central.example.org:8080",
  "token": "ВСТАВИТЬ_ТОКЕН",
  "agentId": "branch-omsk",
  "intervalSeconds": 300,
  "ignoreCertErrors": true,
  "timeoutSeconds": 60
}
'@ | Set-Content 'C:\BackupDotNet\config\remote.json' -Encoding UTF8

# разовый прогон для проверки
& 'C:\BackupDotNet\BackupDotNet.exe' --remote --once

# служба
sc.exe create BackupDotNetRemote binPath= "C:\BackupDotNet\BackupDotNet.exe --service-remote" start= auto
sc.exe start BackupDotNetRemote
```

Филиал забирает конфигурацию (`GET /api/agent/config`), выполняет проверки локально
и отправляет результаты (`POST /api/agent/results`). Секреты передаются в памяти,
на диск филиала не пишутся.

Если филиал недоступен, центральный агент не помечает все его задания как упавшие —
состояние просто перестаёт обновляться, а в журнале появляется запись.

## Проверки после установки

| Что | Как |
|---|---|
| Служба работает | `Get-Service BackupDotNetService` |
| API отвечает | `Invoke-WebRequest http://localhost:8080/api/health` |
| Журнал пишется | `Get-ChildItem C:\ProgramData\BackupDotNet\logs` |
| Планировщик тикает | в журнале `Планировщик запущен, тик 60 с` |
| Проверки идут | `BackupDotNet.exe --once` и смотреть вывод |
| Zabbix получает | `zabbix_sender`-тест или в интерфейсе Zabbix «Latest data» |

## Типичные проблемы

| Симптом | Причина | Решение |
|---|---|---|
| `HTTP-сервер слушает http://localhost:8080/` вместо `http://+:8080/` | нет urlacl | `netsh http add urlacl ... user="NT AUTHORITY\SYSTEM"` |
| Служба не стартует, в `boot.log` пусто | нет прав на каталог данных | дать `LocalSystem` полный доступ к `C:\ProgramData\BackupDotNet` |
| В журнале нет строк | уровень `error`, а события `info` | `logging.level = "info"` |
| Пароль неизвестен | `auth\AGENT.md` потерян | удалить `config\users.json`, перезапустить — пароль создастся заново |
| Задания «нет данных» | источник неизвестного типа или файлов нет | проверить журнал и пути в источнике |
| Zabbix молчит | не включён sender или неверный сервер | `zabbix.enabled = true`, проверить порт 10051 |
