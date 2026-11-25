# Разработка

## Окружение

- Visual Studio 2022 (или VS Code + C# Dev Kit)
- .NET SDK 8/9/10 — для сборки под `net48` (SDK-стиль проектов)
- MSBuild из VS тоже подходит
- Windows PowerShell 5.1 — для модулей развёртывания

Сторонних NuGet-пакетов **нет и не должно появиться**. Если нужна функциональность
библиотеки — она либо уже в Windows, либо декомпилируется и встраивается в исходники
(так лежит `reference/` — исторические исходники RVTools и утилит).

## Сборка и запуск

```powershell
build.cmd                    # прод-инстанс в bin\ + брендинг DPT
run.cmd                      # запуск прод-инстанса
```

`bin\` — рабочий экземпляр (сборка + рабочие конфиги + данные). Каталог целиком
в `.gitignore`. Повторный `build.cmd` сохраняет данные и конфиги, обновляя только код.
Подробности — в `README.md`.

Отладочный запуск из дерева сборки:

```powershell
dotnet build BackupDotNet.sln -c Release
cd src\BackupDotNet.App\bin\Release
.\BackupDotNet.exe --console
```

Веб-интерфейс копируется в выходной каталог автоматически (`public\**`),
конфигурация — как `*.example.json`.

Полезные команды:

```powershell
dotnet build BackupDotNet.sln -c Release -v m       # краткий вывод
dotnet clean BackupDotNet.sln
dotnet msbuild BackupDotNet.sln -t:Rebuild
build.cmd -NoBuild                                  # синхронизация без сборки
build.cmd -ResetBranding                            # вернуть брендинг DPT.Backup
powershell -File tools\package.ps1                  # ZIP для переноса на другую машину
```

## Структура

```
src/BackupDotNet.Core/       # доменная логика, никаких ссылок на Web/Deploy
src/BackupDotNet.Web/        # HTTP и API
src/BackupDotNet.Deploy/     # WinRM
src/BackupDotNet.Manage/     # очередь операций
src/BackupDotNet.App/        # точка входа и службы
public/                      # фронтенд без сборки: правьте и обновляйте страницу
config/                      # шаблоны + branding.dpt.json (прод-брендинг)
tools/generate-assets.ps1    # логотип, фавикон, иконка
tools/build-prod.ps1         # сборка прод-инстанса в bin\
tools/package.ps1            # ZIP-комплект для переноса
bin/                         # прод-инстанс, в .gitignore
```

## Стиль кода

Код после дебрендинга приведён к единому стилю — придерживайтесь его.

### Форматирование

- Отступы — **табы**, ширина 4.
- Фигурные скобки — на новой строке (Allman).
- Пространства имён — file-scoped: `namespace BackupDotNet.Core;`
- Один публичный тип на файл, имя файла = имя типа.

### Именование

| Что | Стиль | Пример |
|---|---|---|
| Приватные поля | `_camelCase` | `_settings`, `_sync` |
| Локальные, параметры | `camelCase` | `collector`, `maxMb` |
| Публичные поля и свойства | `PascalCase` | `Settings`, `CurrentFile` |
| Константы | `PascalCase` | `ListenerTag` |
| Типы | `PascalCase` | `CollectResult` |

Публичные **поля** (не свойства) — сознательное наследие: классы-модели
(`Settings`, `Company`, `Source`, `JobState`) так устроены намеренно, менять не нужно.

### Язык

- Комментарии и XML-документация — **русский**.
- Сообщения в журнале, интерфейсе и ошибках — **русский**.
- Идентификаторы кода — английский.

### Обработка ошибок

- Коллектор **никогда** не бросает наружу: любая проблема превращается в
  `CollectResult` с `Reachable = false` и текстом ошибки.
- `Log` не бросает исключений — логирование не должно ронять агент.
- Пустой `catch { }` допустим только там, где сбой действительно неважен
  (закрытие файла, чтение чужого свойства WMI). Везде остальное — логировать.
- В API-обработчиках исключение превращается в `500` с текстом, а не в разрыв соединения.

## Как добавить источник проверки

1. **Класс** в `src/BackupDotNet.Core`, реализующий `ICollector`:

```csharp
public class MyCollector : ICollector
{
    public string Type => "my-type";

    public List<CollectResult> Collect(Company company, Source source, AgentCtx ctx)
    {
        List<CollectResult> results = new List<CollectResult>();
        CollectResult r = new CollectResult();
        r.CompanyId = company.Id;
        r.CompanyName = company.Name;
        r.SourceId = source.Id;
        r.Job = source.GetStr("jobName", source.Id);
        r.Kind = Type;
        r.Reachable = true;

        try
        {
            // ... сама проверка ...
            r.SizeBytes = 1234;
            r.Message = "всё хорошо";
        }
        catch (Exception ex)
        {
            r.Reachable = false;
            r.Fail(ex.Message);
        }

        results.Add(r);
        return results;
    }
}
```

2. **Регистрация** в `Collectors` (статический конструктор): `Register(new MyCollector());`

3. **Валидация** в `ApiHandler.NormalizeSourceType` — добавить `case "my-type":`.

4. **Форма**: `SOURCE_TYPES` в `public/js/app.js` — появится в выпадающем списке.

5. **Документация**: строка в `docs/SOURCES.md`.

Планировщик, хранилище и политики менять не нужно.

## Фронтенд

- Никакой сборки: `public/js/app.js` подключается как есть.
- ES5-совместимый синтаксис (`var`, `function`) — интерфейс должен работать
  и в старых браузерах на терминальных серверах.
- HTML собирается строками; **всегда** через `esc()` для пользовательских данных.
- Роутинг — по хешу: `#/dashboard`, `#/company/<id>`, `#/logs`.
- Стили — `public/css/app.css`, переменные темы в `:root` и `[data-theme="dark"]`.

### Палитра

| Переменная | Светлая | Тёмная |
|---|---|---|
| `--brand` | `#0066CC` | `#4D9BFF` |
| `--brand-hi` | `#0088FF` | `#0088FF` |
| `--brand-dark` | `#000088` | `#000B33` |
| `--logo1` / `--logo2` | `#0088FF` / `#000088` | — |

Фирменные цвета названия (`part1Color`, `part2Color`) приходят из `branding.json`
и подставляются в CSS-переменные `--brand-part1` / `--brand-part2`.

## Выпуск релиза

Порядок, который не оставляет сюрпризов:

1. Поднять `VersionPrefix` в `Directory.Build.props`.
2. Перенести «Не выпущено» из `CHANGELOG.md` в раздел с версией и датой.
3. Написать заметки в `.github/release-notes/v<версия>.md` — CI возьмёт
   описание оттуда. Без файла GitHub сгенерирует только список коммитов.
4. `build.cmd` — обновить `bin\`.
5. `tools\package.ps1` и `tools\build-setup.ps1 -Zip`.
6. Проверить версии **внутри** архивов, а не только в `bin\`.
7. Коммит, тег `v<версия>`, пуш тега — CI соберёт архив агента и опубликует
   релиз, затем вручную добавить установщик.

**Опубликованный релиз не меняем.** Если в выпуске нашлась ошибка — выпускаем
патч (`1.1.1`) со своим описанием. Подмена файлов в уже опубликованном
релизе ломает контрольные суммы у тех, кто скачал раньше, и стирает историю:
через месяц никто не поймёт, почему у двух людей с «одной версией» разное
поведение.

**Проверять версию в собранном артефакте, а не в исходниках.** Уже дважды
ловились на этом: `bin\` обновляет только `build.cmd`, а `dotnet build`
кладёт результат в `src\...\bin\Release`. Обе ловушки теперь закрыты
проверками в `tools\build-setup.ps1`.

## Логотип и иконка

```powershell
powershell -ExecutionPolicy Bypass -File tools\generate-assets.ps1
```

Скрипт рисует щит с галочкой средствами `System.Drawing` и создаёт:

- `assets/logo.png`, `assets/logo-dark.png`, `assets/icon.ico`
- `public/img/logo.png`, `logo-dark.png`, `favicon.ico`, `apple-touch-icon.png`

Иконка — мультиразмерная (16…256), PNG-кадры внутри ICO.

## Журналирование в коде

```csharp
Log.Debug("подробность для отладки");
Log.Info("обычное событие");
Log.Warn("что-то не так, но работаем");
Log.Error("сломалось", ex);   // стек попадёт в журнал при уровне debug
```

Не создавайте свои `TraceSource` и не подключайте листенеры вручную —
всё через `Log`.

## Отладка

| Задача | Как |
|---|---|
| Быстрый прогон без службы | `--once` |
| Прогон одной компании | `--check <id>` |
| Подробный журнал | `logging.level = "debug"` |
| Смотреть живой журнал | страница **Журналы** в интерфейсе |
| Проверить API | `Invoke-WebRequest http://localhost:8080/api/health` |
| Проверить конфигурацию | страница **Настройки → Ошибки конфигурации** |
| Точка входа в отладчике | `BackupDotNet.App`, аргументы `--console` |

Службу удобно отлаживать так: остановить её и запустить тот же exe с `--console`
из той же папки с теми же `config\`.

## Коммиты

Формат — Conventional Commits, по-русски или по-английски, но единообразно:

```
feat(sources): добавить коллектор Proxmox
fix(log): убрать дублирование листенеров при повторном Init
docs(readme): описать установку службы
refactor(core): вынести ротацию журнала в FileTraceListener
```

Правила:

- Один коммит — одна мысль.
- Не коммитить `bin/`, `obj/`, `data/`, `config/secrets.json`, рабочие `config/*.json`.
- Перед коммитом: `dotnet build BackupDotNet.sln -c Release` без предупреждений.

## Чек-лист перед PR

- [ ] Сборка без ошибок и предупреждений
- [ ] Новый источник зарегистрирован в `Collectors` и описан в `docs/SOURCES.md`
- [ ] Все пользовательские строки в HTML проходят через `esc()`
- [ ] Ошибки логируются, а не глотаются
- [ ] Проверено на чистой конфигурации (`config/*.example.json`)
- [ ] Обновлён `ROADMAP.md`, если этап закрыт
