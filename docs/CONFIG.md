# Конфигурация

Все файлы лежат в `config/` рядом с `BackupDotNet.exe`. После правки `settings.json`
нужен перезапуск агента; `branding.json` подхватывается без перезапуска
(кэш инвалидируется по времени изменения файла).

Шаблоны для копирования: `config/*.example.json`.

---

## `settings.json`

```json
{
  "agent": {
    "name": "BackupDotNet",
    "dataDir": "data",
    "publicDir": "public",
    "configDir": "config",
    "httpPort": 8080,
    "bindAddress": "http://+:8080/",
    "timezone": "Europe/Moscow",
    "language": "ru"
  },
  "scheduler": {
    "tickSeconds": 60,
    "jitterSeconds": 120,
    "maxParallelChecks": 4,
    "defaultIntervalMinutes": 60
  },
  "veeam": {
    "requestTimeoutSeconds": 30,
    "ignoreCertErrorsDefault": true,
    "restApiVersion": "1.1-rev1"
  },
  "zip": { "tempDir": "", "crcReadBufferKb": 256 },
  "zabbix": { "...": "см. docs/ZABBIX.md" },
  "web": { "requireAuth": true, "sessionTimeoutMinutes": 480 },
  "logging": { "level": "info", "maxFileSizeMb": 32, "keepDays": 90, "publicEnabled": true }
}
```

### `agent`

| Ключ | По умолчанию | Описание |
|---|---|---|
| `name` | имя продукта | Отображается в интерфейсе и Zabbix |
| `dataDir` | `data` | Каталог состояния, истории, журналов. Относительный — от папки exe |
| `publicDir` | `public` | Каталог веб-интерфейса |
| `configDir` | `config` | Каталог конфигурации |
| `httpPort` | `8080` | Порт веб-интерфейса |
| `bindAddress` | `http://+:8080/` | Префикс `HttpListener` |
| `timezone` | `Europe/Moscow` | Часовой пояс для отображения |
| `language` | `ru` | Язык интерфейса |

**Про `bindAddress`.** Агент пробует префиксы по порядку: `bindAddress`, затем
`http://+:порт/`, затем `http://localhost:порт/`. Первый успешный используется.
Для `http://+:` нужен urlacl:

```powershell
netsh http add urlacl url=http://+:8080/ user="NT AUTHORITY\SYSTEM"
```

### `scheduler`

| Ключ | По умолчанию | Описание |
|---|---|---|
| `tickSeconds` | `60` | Период тика планировщика |
| `jitterSeconds` | `120` | Разброс первого запуска (сглаживание нагрузки) |
| `maxParallelChecks` | `4` | Сколько источников проверяется одновременно |
| `defaultIntervalMinutes` | `60` | Интервал для источников без `intervalMinutes` |

### `logging`

| Ключ | По умолчанию | Описание |
|---|---|---|
| `level` | `info` | `debug`, `info`, `warn`, `error` |
| `maxFileSizeMb` | `32` | Размер файла до ротации |
| `keepDays` | `90` | Сколько дней хранить журналы (0 — не чистить) |
| `publicEnabled` | `true` | Вести второй журнал — без секретов, адресов и имён |

На уровне `debug` в журнал попадают стеки исключений — полезно при разборе инцидентов,
но файл растёт заметно быстрее.

`publicEnabled` включает второй файл `<Продукт>.public_<дата>.log`: тот же поток
сообщений, но каждая строка проходит через `Redactor` — пароли, токены, адреса,
почта, имена машин и пути с именами пользователей заменяются плейсхолдерами.
Такой журнал можно отдать подрядчику или приложить к тикету, не вычитывая
построчно. Переключается на лету кнопкой «публичный» на странице «Журналы».

### `notifications`
| Ключ | По умолчанию | Описание |
|---|---|---|
| `enabled` | `false` | Главный выключатель |
| `on.fail` / `on.warn` / `on.recover` / `on.nodata` | `true/true/true/false` | О каких переходах сообщать |
| `minIntervalMinutes` | `30` | Не чаще одного события по заданию |
| `quietHours.from` / `.to` | — | Тихие часы (`HH:mm`), пусто — выключено |
| `smtp.*` | — | Сервер, порт, SSL, учётные данные, отправитель, получатели |
| `telegram.*` | — | Токен бота (лучше через `tokenSecret`) и список чатов |

Списки получателей (`smtp.to`, `telegram.chats`) принимают и массив, и строку
через запятую. Подробности и правила отправки — в
[ARCHITECTURE.md](ARCHITECTURE.md#уведомления).

### `history` и `updates`
| Ключ | По умолчанию | Описание |
|---|---|---|
| `history.journalKeepDays` | `400` | сколько дней хранить подробный журнал прогонов |
| `history.rollupKeepMonths` | `60` | сколько месяцев хранить суточные сводки |
| `history.autoPrune` | `true` | чистить старое при запуске |
| `vm.protectedPatterns` | `[]` | шаблоны имён ВМ, которые нельзя гасить и удалять без подтверждения (`*`, `?`): например `PROD-*`, `*-DC` |
| `vm.maxBatchSize` | `5` | сколько машин можно обработать одной массовой операцией |
| `vm.quarantineDays` | `7` | сколько дней машина лежит в карантине до окончательного удаления |
| `vm.requireForceForProtected` | `true` | требовать подтверждение для защищённых ВМ |
| `storage.targets` | `[]` | хранилища для проверки: `[{"path":"\\\\srv\\share","label":"бэкапы"}]` или просто список путей |
| `updates.enabled` | `false` | включить проверку обновлений |
| `updates.source` | `local` | `local` (папка) или `github` (GitHub Releases) |
| `updates.path` | — | каталог или сетевая папка с `latest.json` (для `local`) |
| `updates.githubRepo` | — | репозиторий в виде `владелец/название` (для `github`) |
| `updates.githubApiBase` | — | адрес API; пусто — `https://api.github.com`, меняется для GitHub Enterprise |
| `updates.githubTokenSecret` | — | ключ в `secrets.json` с токеном GitHub (приватные репозитории) |
| `updates.allowInstall` | `false` | разрешить установку обновления кнопкой из интерфейса |
| `updates.checkOnStart` | `true` | проверять при старте |
| `backup.destination` | — | куда складывать бэкапы по умолчанию (пусто — `dataDir\backups\store`) |
| `backup.retention.*` | — | политика хранения копий: `keepCount`, `keepDays`, `gfs.daily/weekly/monthly`, `protectLast`, `maxDeletePerRun` |

Подробности — в [ARCHITECTURE.md](ARCHITECTURE.md#хранение-истории-и-обновления).

---

## `branding.json`

Всё оформление — в одном файле. Вложенность `brand` необязательна.

```json
{
  "documentTitle": "BackupDotNet · Мониторинг",
  "brand": {
    "part1": "Backup",
    "part2": "DotNet",
    "part1Color": "#0088FF",
    "part2Color": "#FFFFFF",
    "tagline": "Мониторинг бэкапов, доступности и железа",
    "logoLight": "img/logo.png",
    "logoDark": "img/logo-dark.png",
    "favicon": "img/favicon.ico",
    "footer": "BackupDotNet",
    "footerUrl": "",
    "footerUrlText": "",
    "productName": "BackupDotNet",
    "company": "BackupDotNet",
    "description": "Мониторинг бэкапов, доступности и железа",
    "installDir": "C:\\BackupDotNet\\",
    "dataDir": "C:\\ProgramData\\BackupDotNet",
    "serviceName": "BackupDotNetService",
    "serviceNameRemote": "BackupDotNetRemote",
    "userAgent": "BackupDotNet",
    "zabbixTemplateName": "BackupDotNet"
  }
}
```

| Ключ | Что меняет |
|---|---|
| `part1` / `part2` | Название: первая часть цветная, вторая — белая |
| `part1Color` / `part2Color` | Цвета частей названия (CSS) |
| `tagline` | Подпись под логотипом на экране входа |
| `logoLight` / `logoDark` | Логотипы для светлой и тёмной темы |
| `favicon` | Иконка вкладки |
| `footer` / `footerUrl` | Подвал. Пустой `footerUrl` — ссылка скрывается |
| `productName` | Имя в заголовке окна и именах файлов журнала |
| `installDir` / `dataDir` | Пути, которые показываются в интерфейсе |
| `serviceName` / `serviceNameRemote` | Имена служб Windows |
| `userAgent` | User-Agent исходящих HTTP-запросов |
| `zabbixTemplateName` | Шаблон для автосоздания хостов |

Цвета по умолчанию — синяя гамма: `#0088FF`, `#0080FF`, `#FFFFFF`, `#000088`.

---

## `env.json`

Пути конкретной инсталляции. Перекрывает `branding.json` и не влияет на оформление.

```json
{
  "installDir": "C:\\BackupDotNet\\",
  "dataDir": "C:\\ProgramData\\BackupDotNet",
  "serviceName": "BackupDotNetService",
  "serviceNameRemote": "BackupDotNetRemote"
}
```

Нужен, когда данные и журналы должны лежать вне папки программы (типичный случай для службы).

---

## `secrets.json`

Секреты источников по ключу. Не коммитится в git.

```json
{
  "veeam-monitor": "dpapi:AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA...",
  "esxi-root": "AQB...открытым текстом..."
}
```

- Префикс `dpapi:` — значение расшифровывается через DPAPI `LocalMachine`.
- Без префикса — используется как есть (не рекомендуется).
- Записать секрет из интерфейса: форма компании → поле «Секрет» → сохранить.
  Программа сама зашифрует и положит в файл.

Ссылка на секрет из источника — поле `passwordSecret` (или `ftp.passwordSecret` и т.п.).

---

## `remote.json`

Только для удалённого агента (`--remote`).

```json
{
  "centralUrl": "https://central.example.org:8080",
  "token": "0123456789abcdef0123456789abcdef",
  "agentId": "branch-omsk",
  "intervalSeconds": 300,
  "ignoreCertErrors": true,
  "timeoutSeconds": 60
}
```

| Поле | Описание |
|---|---|
| `centralUrl` | Адрес центрального агента (без завершающего `/`) |
| `token` | Токен, выданный на центральном агенте (не короче 16 символов) |
| `agentId` | Идентификатор филиала |
| `intervalSeconds` | Период прогона проверок |
| `ignoreCertErrors` | Не проверять сертификат центрального агента |
| `timeoutSeconds` | Таймаут HTTP-запросов |

Токен выдаётся в интерфейсе: **Настройки → Удалённые агенты → Добавить**.

---

## `companies/*.json`

Одна компания — один файл. Имя файла произвольное, но файлы, начинающиеся с `_`,
игнорируются (удобно для заметок).

```json
{
  "id": "acme",
  "name": "ООО «Акме»",
  "shortName": "Акме",
  "timezone": "Europe/Moscow",
  "enabled": true,
  "tags": ["филиал", "1С"],
  "contacts": [
    { "role": "ИТ", "name": "Иван", "phone": "+7 900 000-00-00", "email": "it@acme.local" }
  ],
  "policy": {
    "freshnessHours": 26,
    "freshnessHoursWeekly": 192,
    "minSizeMb": 10,
    "consecutiveFailuresToFail": 2,
    "maxSizeDropPercent": 0,
    "requireOffsite": false,
    "silenceWindow": { "from": "", "to": "" }
  },
  "zabbix": {
    "enabled": true,
    "host": "BC-acme",
    "displayName": "Акме · бэкапы",
    "macros": { "{$BC.MAX_AGE_H}": "26h" }
  },
  "remoteAgent": "",
  "sources": [ "..." ]
}
```

### Политика (`policy`)

| Ключ | По умолчанию | Описание |
|---|---|---|
| `freshnessHours` | `26` | Допустимый возраст ежедневного бэкапа |
| `freshnessHoursWeekly` | `192` | Допустимый возраст недельного |
| `minSizeMb` | `0` | Минимальный размер файла бэкапа |
| `consecutiveFailuresToFail` | `2` | Сколько неудач подряд до статуса `FAIL` |
| `maxSizeDropPercent` | `0` | Падение размера в % до тревоги (0 — выключено) |
| `requireOffsite` | `false` | Требовать наличие офсайт-копии |
| `silenceWindow` | — | Окно, когда тревоги подавляются (`HH:mm`) |

### `remoteAgent`

Если заполнено — компания проверяется удалённым агентом с этим `agentId`,
а центральный её пропускает.

---

## Приоритет настроек

```
config/env.json  →  перекрывает пути из branding.json
config/settings.json  →  значения по умолчанию в коде
```

Если `settings.json` отсутствует — используются значения по умолчанию, а в журнал
пишется предупреждение. Агент при этом работает.

## Перенос конфигурации

Всё содержимое `config/` (кроме секретов) переносится одним ZIP:**Настройки → Конфигурация → Скачать архив**. Обратно — «Импорт…»: сначала
показывается предпросмотр изменений, затем запись с резервной копией.

Подробности, API и правила — в [ARCHITECTURE.md](ARCHITECTURE.md#перенос-конфигурации).

## Проверка конфигурации
- Ошибки чтения файлов попадают в журнал (`WARN Конфигурация: ...`) и на страницу
  **Настройки → Ошибки конфигурации**.
- Дублирующиеся `id` компаний — файл пропускается с предупреждением.
- Компания без `id` — файл пропускается.
- Источник неизвестного типа — задание получает `NODATA` с пояснением.
