# Источники проверок

Один источник = один `type` в блоке `sources` компании. Все параметры — в том же объекте.
Общие поля у всех типов:

| Поле | Тип | По умолчанию | Описание |
|---|---|---|---|
| `id` | строка | = `type` | Уникальный идентификатор внутри компании |
| `type` | строка | — | Тип источника (см. таблицу ниже) |
| `enabled` | bool | `true` | Включён ли источник |
| `intervalMinutes` | int | `scheduler.defaultIntervalMinutes` | Период проверки |
| `jobName` | строка | = `id` | Имя задания в интерфейсе и Zabbix |

---

## Хранилища и файлы

### `file-age` — свежесть файлов

```json
{ "id": "1c", "type": "file-age", "jobName": "1С · бэкап",
  "paths": ["D:\\Backup\\1C\\*.dt"], "maxAgeHours": 26 }
```

| Поле | Описание |
|---|---|
| `paths` | Массив путей/масок. Можно и одиночный `path` |
| `maxAgeHours` | Допустимый возраст самого свежего файла (по умолчанию — политика компании) |

Проверяет самый свежий файл из найденных. Если файлов нет — `FAIL`.

### `zip` — целостность архива

```json
{ "id": "nightly", "type": "zip", "path": "D:\\Backup\\latest.zip", "maxAgeHours": 26 }
```

Читает центральный каталог ZIP и проверяет CRC. Битый архив — `FAIL` с указанием записи.

### `veeam-files` — репозиторий Veeam без API

```json
{ "id": "vbk", "type": "veeam-files", "paths": ["E:\\Veeam\\*.vbk"], "maxAgeHours": 26 }
```

Работает по файлам `.vbk`/`.vib`, когда REST API недоступен.

### `veeam-rest` — Veeam B&R REST API

```json
{ "id": "veeam", "type": "veeam-rest",
  "url": "https://veeam.local:9419", "username": "monitor",
  "passwordSecret": "veeam-monitor", "ignoreCertErrors": true,
  "apiVersion": "1.1-rev1" }
```

| Поле | Описание |
|---|---|
| `url` | Адрес REST API (порт 9419 по умолчанию) |
| `username` / `password` | Учётные данные (или `passwordSecret`) |
| `passwordSecret` | Ключ в `config/secrets.json` |
| `ignoreCertErrors` | Не проверять сертификат (самоподписанные) |
| `apiVersion` | Версия API, по умолчанию `1.1-rev1` |

### `iperius-ftp` — архивы на FTP

```json
{ "id": "ftp", "type": "iperius-ftp", "jobName": "Iperius · FTP",
  "ftp": { "host": "ftp.example.org", "port": 21, "username": "backup",
           "passwordSecret": "ftp-backup", "remotePath": "/nightly",
           "pattern": "*.zip", "useTls": true, "maxFileAgeHours": 26 } }
```

Параметры лежат в подобъекте `ftp.*` (так их читает коллектор).

---

## Доступность

### `http` — HTTP(S)

```json
{ "id": "portal", "type": "http", "url": "https://portal/health",
  "method": "GET", "expectedStatus": [200], "keyword": "ok",
  "keywordMode": "must", "timeoutSeconds": 10, "ignoreCertErrors": false }
```

| Поле | Описание |
|---|---|
| `expectedStatus` | Массив допустимых кодов (по умолчанию `200`) |
| `keyword` | Подстрока или регулярка в теле |
| `keywordMode` | `must` — должна быть, `must-not` — не должна |
| `keywordRegex` | Трактовать `keyword` как регулярное выражение |
| `method`, `body`, `contentType` | Метод и тело запроса |
| `userAgent` | Переопределение User-Agent |

### `tcp` — порт

```json
{ "id": "rdp", "type": "tcp", "host": "10.0.0.10", "port": 3389, "timeoutSeconds": 5 }
```

### `ping` — ICMP

```json
{ "id": "gw", "type": "ping", "host": "10.0.0.1", "attempts": 3, "timeoutMs": 3000 }
```

### `dns` — разрешение имени

```json
{ "id": "dns", "type": "dns", "name": "portal.example.org",
  "recordType": "A", "expect": "10.0.0.5", "timeoutSeconds": 5 }
```

### `push` — heartbeat от сервиса

```json
{ "id": "push", "type": "push", "jobName": "Агент 1С", "staleMinutes": 15 }
```

Сервис сам шлёт `POST /api/push/<ключ>`; если молчит дольше `staleMinutes` — `FAIL`.
URL для отправки показывается в карточке компании.

---

## Железо

### `smart` — диски

```json
{ "id": "smart", "type": "smart", "jobName": "SMART",
  "maxTempC": 55, "warnTempC": 45, "minLifePercent": 20, "checkSmartctl": true }
```

Читает SMART через WMI; если установлен `smartctl` — использует его для деталей.

### `raid` — контроллеры

```json
{ "id": "raid", "type": "raid", "jobName": "RAID" }
```

Опрашивает Storage Spaces, MegaRAID (через `storcli`/`MegaCli`), HP Smart Array (HPSSACLI).

### `hardware` — температуры и вентиляторы

```json
{ "id": "hw", "type": "hardware", "jobName": "Железо",
  "useIpmi": true, "bmcHost": "10.0.0.20", "bmcUser": "monitor",
  "passwordSecret": "bmc-monitor", "bmcTransport": "ipmi",
  "cpuTempWarn": 75, "cpuTempFail": 90, "fanMinRpm": 600,
  "psuFailIsCritical": true }
```

Источники данных: IPMI (`ipmitool`), WMI ACPI, LibreHardwareMonitor (если запущен).

---

## Система

### `inventory` — инвентаризация

```json
{ "id": "inv", "type": "inventory", "jobName": "Сервер",
  "checkVolumes": true, "volumeMinFreePercent": 10,
  "collectSoftware": true, "softwareMaxItems": 60,
  "checkHotfixes": false, "hotfixMaxAgeDays": 60,
  "services": ["Spooler", "1C:Enterprise 8.3 Server"] }
```

ОС, тома, службы, установленный софт, обновления, время работы.

### `vss` — теневые копии

```json
{ "id": "vss", "type": "vss", "jobName": "VSS" }
```

Проверяет писателей VSS и наличие теневых копий. Именно здесь чаще всего
ломаются бэкапы Windows — «писатель в состоянии failed».

### `eventlog` — журнал событий

```json
{ "id": "evt", "type": "eventlog", "jobName": "События",
  "hours": 24, "failCount": 30, "warnCount": 5, "includeWarnings": false,
  "maxEvents": 500 }
```

Считает ошибки и предупреждения за период. Шум (DCOM, Perflib) игнорируется
по встроенному списку.

### `netconfig` — сеть

```json
{ "id": "net", "type": "netconfig", "jobName": "Сеть",
  "checkGateway": true, "checkDns": true, "minLinkSpeedMbps": 0 }
```

Следит за сменой IP, доступностью шлюза, DNS и скоростью линка.

### `tasks` — планировщик задач

```json
{ "id": "tasks", "type": "tasks", "jobName": "Задачи",
  "depth": 3, "includeAutostart": true, "maxLastRunAgeHours": 0 }
```

Для бэкапов по расписанию: задача существует, включена, когда отработала,
с каким кодом вернулась.

---

## Виртуализация и сеть

### `hyperv` — виртуальные машины Hyper-V

```json
{ "id": "hv", "type": "hyperv", "jobName": "ВМ",
  "checkHeartbeat": true, "checkDiskFree": true,
  "diskFreeWarnPercent": 10, "uptimeWarnMinutes": 10 }
```

Состояние ВМ, heartbeat, снимки, свободное место на дисках ВМ.

### `esxi` — VMware ESXi

```json
{ "id": "esxi", "type": "esxi", "host": "10.0.0.30",
  "username": "monitor", "passwordSecret": "esxi-monitor",
  "checkSnapshots": true, "snapshotMaxAgeDays": 7,
  "checkDatastores": true, "datastoreWarnPercent": 15, "datastoreFailPercent": 5,
  "checkSmart": false, "sshUser": "root", "sshPasswordSecret": "esxi-ssh" }
```

Работает по SOAP (`/sdk`). Снимки старше `snapshotMaxAgeDays` — предупреждение.

### `mikrotik` — RouterOS

```json
{ "id": "mt", "type": "mikrotik", "host": "10.0.0.1",
  "transport": "auto", "username": "monitor", "passwordSecret": "mt-monitor",
  "cpuWarnPercent": 80, "tempWarnC": 70, "uptimeWarnMinutes": 60,
  "minVersion": "7.10" }
```

`transport`: `auto` (API → REST → SSH → SNMP), либо явно `api`, `rest`, `ssh`, `snmp`.

---

## Как добавить свой источник

1. Класс в `src/BackupDotNet.Core`, реализующий `ICollector`.
2. Регистрация в статическом конструкторе `Collectors`.
3. Описание в `SOURCE_TYPES` (`public/js/app.js`) — чтобы появился в форме.
4. Строка в этом документе.

Планировщик, хранилище и политики трогать не нужно — они работают с `CollectResult`.
