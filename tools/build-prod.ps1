<#
.SYNOPSIS
    Сборка продакшн-инстанса в каталог bin\.

.DESCRIPTION
    bin\ — это рабочий экземпляр: сюда попадает сборка, здесь живут рабочие
    конфиги и данные. Каталог целиком в .gitignore, поэтому ни бинарники,
    ни конфиги, ни история в репозиторий не попадают.

    Что делает скрипт:
      1. собирает решение в Release;
      2. останавливает запущенный bin\BackupDotNet.exe, если он держит файлы;
      3. копирует сборку и веб-интерфейс в bin\ (не трогая config\ и data\);
      4. создаёт рабочие конфиги из шаблонов, если их ещё нет;
      5. кладёт брендинг DPT (config\branding.dpt.json → bin\config\branding.json);
      6. готовит bin\data, bin\data\logs, bin\auth, bin\docs.

    Повторный запуск безопасен: данные и уже созданные конфиги сохраняются.

.PARAMETER NoBuild
    Не собирать, только синхронизировать файлы (быстро).

.PARAMETER ResetBranding
    Перезаписать bin\config\branding.json из шаблона DPT.

.PARAMETER KeepRunning
    Не останавливать запущенный агент (сборка может упасть на занятых файлах).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\build-prod.ps1

.EXAMPLE
    build.cmd
#>

[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$ResetBranding,
    [switch]$KeepRunning
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $root 'bin'
$buildOut = Join-Path $root 'src\BackupDotNet.App\bin\Release'
$brandingTemplate = Join-Path $root 'config\branding.dpt.json'

function Write-Step([string]$Text) {
    Write-Host ''
    Write-Host "== $Text" -ForegroundColor Cyan
}

# ---------------------------------------------------------------- 1. сборка
if (-not $NoBuild) {
    Write-Step 'Сборка Release'
    & dotnet build (Join-Path $root 'BackupDotNet.sln') -c Release -v m
    if ($LASTEXITCODE -ne 0) {
        throw "Сборка завершилась с ошибкой (код $LASTEXITCODE)"
    }
}

if (-not (Test-Path (Join-Path $buildOut 'BackupDotNet.exe'))) {
    throw "Не найден $buildOut\BackupDotNet.exe — соберите решение."
}

# ------------------------------------------------- 2. остановка инстанса
if (-not $KeepRunning) {
    $running = Get-Process -Name 'BackupDotNet' -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and $_.Path.StartsWith($bin, 'OrdinalIgnoreCase') }
    if ($running) {
        Write-Step "Останавливаю запущенный агент (PID $($running.Id -join ', '))"
        $running | Stop-Process -Force
        Start-Sleep -Milliseconds 800
    }
}

# ------------------------------------------------------ 3. каталоги
New-Item -ItemType Directory -Force -Path `
    $bin,
    (Join-Path $bin 'config'),
    (Join-Path $bin 'config\companies'),
    (Join-Path $bin 'data'),
    (Join-Path $bin 'data\logs'),
    (Join-Path $bin 'auth') | Out-Null

# --------------------------------------------- 4. файлы программы
Write-Step 'Копирую сборку в bin'

# Старые сборки удаляем, чтобы не остались dll от переименованных проектов.
Get-ChildItem $bin -File |
    Where-Object { $_.Extension -in '.dll', '.exe', '.pdb', '.xml' } |
    Remove-Item -Force -ErrorAction SilentlyContinue

# robocopy умеет исключать каталог config из источника и не трогает data\.
$roboArgs = @(
    $buildOut,
    $bin,
    '/E',
    '/XF', '*.xml',
    '/XD', (Join-Path $buildOut 'config'),
    '/NFL', '/NDL', '/NJH', '/NJS', '/NP'
)
& robocopy @roboArgs | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy завершился с ошибкой (код $LASTEXITCODE)"
}
$global:LASTEXITCODE = 0

# Каталог config из сборки не нужен: шаблоны кладём отдельно и осмысленно.
Get-ChildItem (Join-Path $bin 'config\*.example.json') -ErrorAction SilentlyContinue |
    Remove-Item -Force

# ------------------------------------------------ 5. конфигурация
Write-Step 'Готовлю конфигурацию'

$pairs = @(
    @{ Template = 'settings.example.json'; Target = 'settings.json' },
    @{ Template = 'branding.example.json'; Target = 'branding.json' },
    @{ Template = 'secrets.example.json';  Target = 'secrets.json' }
)

foreach ($p in $pairs) {
    $src = Join-Path $root ('config\' + $p.Template)
    $dst = Join-Path $bin ('config\' + $p.Target)
    if (-not (Test-Path $src)) { continue }
    if ($p.Target -eq 'branding.json') {
        if (-not (Test-Path $dst) -or $ResetBranding) {
            if (Test-Path $brandingTemplate) {
                Copy-Item $brandingTemplate $dst -Force
                Write-Host "  branding.json  <- branding.dpt.json (DPT.Backup)"
            } else {
                Copy-Item $src $dst -Force
            }
        }
        continue
    }
    if (-not (Test-Path $dst)) {
        Copy-Item $src $dst -Force
        Write-Host "  config\$($p.Target)  создан из шаблона"
    }
}

# Портативный инстанс: данные лежат внутри bin, а не в ProgramData.
# env.json перекрывает пути из branding.json, поэтому пишем его всегда,
# когда рабочего файла ещё нет.
$envPath = Join-Path $bin 'config\env.json'
$envExample = Join-Path $root 'config\env.example.json'
$envIsPortable = $false
if (Test-Path $envPath) {
    try {
        $envJson = Get-Content $envPath -Raw | ConvertFrom-Json
        $envIsPortable = ($envJson.dataDir -eq 'data')
    } catch { }
}
if (-not (Test-Path $envPath) -or -not $envIsPortable) {
    @'
{
  "_comment": "Портативный инстанс: всё внутри bin. Для службы задайте абсолютные пути.",
  "installDir": "bin\\",
  "dataDir": "data",
  "serviceName": "DPTBackupService",
  "serviceNameRemote": "DPTBackupRemote"
}
'@ | Set-Content $envPath -Encoding UTF8
    Write-Host '  config\env.json  портативные пути (data внутри bin)'
}

# Пример компании — только если компаний ещё нет вообще.
# Сеется выключенным: чтобы демо-пути не шумели в проде, включите в интерфейсе.
# Правим текстом, а не через ConvertTo-Json: иначе файл получает уродливое
# форматирование и его неприятно открывать руками.
$companiesDir = Join-Path $bin 'config\companies'
if (-not (Get-ChildItem $companiesDir -Filter '*.json' -ErrorAction SilentlyContinue)) {
    $sample = Join-Path $root 'config\companies\example.example.json'
    if (Test-Path $sample) {
        $text = Get-Content $sample -Raw -Encoding UTF8
        $text = [regex]::Replace($text, '"enabled"\s*:\s*true', '"enabled": false', 'None', [TimeSpan]::FromSeconds(1))
        $target = Join-Path $companiesDir 'example.json'
        [System.IO.File]::WriteAllText($target, $text, (New-Object System.Text.UTF8Encoding($false)))
        Write-Host '  config\companies\example.json  создан (выключен)'
    }
}

# ---------------------------------------------------- 6. документация
Write-Step 'Копирую документацию'
foreach ($doc in 'README.md', 'ROADMAP.md', 'CHANGELOG.md', 'LICENSE') {
    $p = Join-Path $root $doc
    if (Test-Path $p) { Copy-Item $p $bin -Force }
}
$docsSrc = Join-Path $root 'docs'
if (Test-Path $docsSrc) {
    $docsDst = Join-Path $bin 'docs'
    New-Item -ItemType Directory -Force -Path $docsDst | Out-Null
    Copy-Item (Join-Path $docsSrc '*') $docsDst -Recurse -Force
    # docs/HANDOFF-* — заметки для продолжения работы, а не документация:
    # в прод-инстанс им незачем.
    Get-ChildItem $docsDst -Filter 'HANDOFF-*' -File -ErrorAction SilentlyContinue | Remove-Item -Force
}

# ------------------------------------------------------- 7. итог
$exe = Join-Path $bin 'BackupDotNet.exe'
$sizeMb = (Get-ChildItem $bin -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
$port = 8080
try {
    $settings = Get-Content (Join-Path $bin 'config\settings.json') -Raw | ConvertFrom-Json
    if ($settings.agent.httpPort) { $port = $settings.agent.httpPort }
} catch { }

Write-Host ''
Write-Host 'Готово.' -ForegroundColor Green
Write-Host ("  exe     : {0}" -f $exe)
Write-Host ("  запуск  : run.cmd            (или run.cmd --once)")
Write-Host ("  веб     : http://localhost:{0}/" -f $port)
Write-Host  "  бренд   : DPT.Backup"
Write-Host ("  конфиг  : {0}" -f (Join-Path $bin 'config'))
Write-Host ("  данные  : {0}" -f (Join-Path $bin 'data'))
Write-Host ("  журналы : {0}" -f (Join-Path $bin 'data\logs'))
Write-Host ("  размер  : {0:N1} МБ" -f $sizeMb)
Write-Host ''
Write-Host '  Пароль администратора — в bin\auth\AGENT.md после первого запуска.' -ForegroundColor DarkGray
Write-Host '  Каталог bin\ целиком в .gitignore: в репозиторий он не попадает.' -ForegroundColor DarkGray
