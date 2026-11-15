# Интеграция с Zabbix

BackupDotNet умеет отправлять данные двумя способами и поддерживает pull-режим.
Все три можно включить одновременно — они не конфликтуют.

| Способ | Когда использовать |
|---|---|
| **sender** (push) | Zabbix-сервер доступен с агента; данные приходят сразу |
| **pull** (`--zabbix`) | Zabbix-сервер сам опрашивает агента; удобно при закрытом контуре |
| **API** (автосоздание хостов) | Нужно, чтобы хосты и шаблоны создавались сами |

---

## Sender (push)

### Настройка

```json
{
  "zabbix": {
    "enabled": true,
    "server": "10.0.0.50",
    "port": 10051,
    "mode": "sender",
    "timeoutSeconds": 5,
    "batchSize": 500,
    "batchMaxBytes": 65536
  }
}
```

| Ключ | Описание |
|---|---|
| `enabled` | Включить отправку |
| `server` | Адрес Zabbix-сервера |
| `port` | Порт траппера, по умолчанию `10051` |
| `timeoutSeconds` | Таймаут соединения |
| `batchSize` | Сколько значений в одном пакете |
| `batchMaxBytes` | Предел размера пакета |

### Что отправляется

После каждого прогона источника:

| Ключ | Тип | Описание |
|---|---|---|
| `bc.job.status[{#JOB}]` | Число | `0` OK, `1` WARN, `2` FAIL, `3` NODATA, `4` SKIP |
| `bc.job.age[{#JOB}]` | Число | Секунд с последнего успешного бэкапа |
| `bc.job.size[{#JOB}]` | Число | Размер последнего бэкапа, байт |
| `bc.job.duration[{#JOB}]` | Число | Длительность прогона, секунд |
| `bc.job.failures[{#JOB}]` | Число | Неудач подряд |
| `bc.job.message[{#JOB}]` | Текст | Пояснение (обрезается до 500 символов) |
| `bc.job.responseMs[{#JOB}]` | Число | Время отклика (для проверок доступности) |
| `bc.discovery` | Текст | JSON для LLD: `{#JOB}`, `{#KIND}`, `{#UPTIME}` |
| `bc.company.status` | Число | Худший статус по компании |
| `bc.company.jobs.total` / `.failed` | Число | Счётчики по компании |
| `bc.jobs.total` / `bc.jobs.failed` | Число | Счётчики по агенту |

Раз в 5 минут — heartbeat агента:

| Ключ | Описание |
|---|---|
| `bc.agent.heartbeat` | Unix-время отправки |
| `bc.agent.version` | Версия агента |

### Хост в Zabbix

Имя хоста формируется так: `zabbix.host` компании, если задан, иначе `BC-<id компании>`.
Для агентских метрик — `BC-AGENT`.

### Очередь при недоступности

Если Zabbix не отвечает, значения складываются в
`<dataDir>\state\zabbix-queue.ndjson` и досылаются на следующем тике.
Потеря связи на часы не приводит к дырам в данных.

---

## Pull-режим

Для Zabbix agent 2 или `UserParameter`:

```
UserParameter=bc.discovery,"C:\BackupDotNet\BackupDotNet.exe" --zabbix discovery
UserParameter=bc.job[*],  "C:\BackupDotNet\BackupDotNet.exe" --zabbix job "$1" "$2"
```

| Команда | Что вернёт |
|---|---|
| `--zabbix discovery` | JSON `{"data":[{"{#JOB}":"...","{#KIND}":"..."}]}` |
| `--zabbix job <имя> status` | Числовой статус |
| `--zabbix job <имя> age` | Секунд с последнего успеха |
| `--zabbix job <имя> size` | Размер, байт |
| `--zabbix job <имя> duration` | Длительность, секунд |
| `--zabbix job <имя> failures` | Неудач подряд |
| `--zabbix job <имя> message` | Текст сообщения |

Код возврата `1`, если задание не найдено (Zabbix пометит как unsupported).

### LLD-правило

1. Item prototype: `bc.job.status[{#JOB}]`, тип «Zabbix agent».
2. Ключ: `bc.job.status[{#JOB}]`.
3. Триггер: `last(/Host/bc.job.status[{#JOB}])=2`.

Имя задания содержит кириллицу и пробелы — Zabbix это допускает,
но в макросе `{#JOB}` их надо использовать без экранирования.

---

## API (автосоздание хостов)

```json
{
  "zabbix": {
    "api": {
      "enabled": true,
      "url": "https://zabbix.example.org/api_jsonrpc.php",
      "token": "СЕКРЕТНЫЙ_ТОКЕН_API",
      "autoCreateHosts": true,
      "hostGroup": "Backup",
      "templateName": "BackupDotNet"
    }
  }
}
```

Что делает:

1. Находит группу `hostGroup` (создаёт, если нет).
2. Находит шаблон `templateName`.
3. Создаёт или обновляет хост `BC-<id>` с интерфейсом агента.
4. Прописывает макросы: `{$BC.MAX_AGE_H}`, `{$BC.MIN_SIZE}`, плюс макросы из
   `company.zabbix.macros`.
5. Ставит тег `company=<id>` — удобно для фильтрации.

Токен создаётся в Zabbix: **Users → API tokens → Create**.

Ошибки API не роняют прогон — пишутся в журнал как `WARN`.

---

## Шаблон

Шаблон `BackupDotNet` содержит:

- **LLD-правило** `bc.discovery` → прототипы items по `{#JOB}`
- **Триггеры**:
  - `FAIL` — `bc.job.status[{#JOB}]=2`
  - `WARN` — `bc.job.status[{#JOB}]=1`
  - устаревание — `bc.job.age[{#JOB]} > {$BC.MAX_AGE_H:43200}`
  - падение размера — `bc.job.size[{#JOB}] < {$BC.MIN_SIZE}`
- **Агентские items**: `bc.agent.heartbeat`, `bc.agent.version`,
  `bc.jobs.total`, `bc.jobs.failed`
- **Триггер «агент молчит»** — `now() - bc.agent.heartbeat > 900`

Макросы на уровне шаблона:

| Макрос | По умолчанию | Смысл |
|---|---|---|
| `{$BC.MAX_AGE_H}` | `26h` | Допустимый возраст бэкапа |
| `{$BC.MIN_SIZE}` | `0` | Минимальный размер |
| `{$BC.AGENT_SILENCE}` | `15m` | Сколько молчание агента считать нормой |

---

## Значения статусов

| Число | Статус | Смысл |
|---|---|---|
| 0 | OK | Всё в порядке |
| 1 | WARN | Есть замечания (размер, температура, срок) |
| 2 | FAIL | Проверка провалилась |
| 3 | NODATA | Данных нет: источник не ответил или не настроен |
| 4 | SKIP | Проверка пропущена (выключена, окно тишины) |

---

## Отладка

```powershell
# Что реально уходит в Zabbix
Get-Content 'C:\ProgramData\BackupDotNet\logs\BackupDotNet_*.log' |
  Select-String 'Zabbix'

# Проверить приём на сервере
zabbix_get -s 10.0.0.50 -k 'bc.job.status[1С · бэкап]'

# Очередь не уходит?
Get-Content 'C:\ProgramData\BackupDotNet\state\zabbix-queue.ndjson' | Measure-Object -Line
```

Частые причины:

| Симптом | Причина |
|---|---|
| `Zabbix: timeout` | Брандмауэр или неверный `server`/`port` |
| `Zabbix: processed 0; failed N` | Хост не создан в Zabbix или ключ не в шаблоне |
| Значения есть, триггеры нет | Не применён шаблон к хосту |
| Данные идут с задержкой | Растёт очередь — сервер недоступен |
