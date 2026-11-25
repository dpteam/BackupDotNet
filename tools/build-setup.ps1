# Сборка пакета установщика.
#
# Раскладывает рядом с BackupDotNet.Setup.exe каталог payload с файлами агента
# и упаковывает всё в один zip. Так установщик можно перенести на другую
# машину одним файлом.
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\build-setup.ps1
#   powershell -ExecutionPolicy Bypass -File tools\build-setup.ps1 -Zip

param(
    [switch]$Zip,
    [string]$OutDir = "artifacts\setup",
    # Брендинг для раздачи. По умолчанию — дебрендированный (BackupDotNet):
    # релиз не должен уезжать с именем чьей-то инсталляции. Для своего
    # продукта передать свой файл: -Branding config\branding.dpt.json
    [string]$Branding = "config\branding.example.json"
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$setupProj = Join-Path $root 'src\BackupDotNet.Setup\BackupDotNet.Setup.csproj'
$setupBin = Join-Path $root 'src\BackupDotNet.Setup\bin\Release'
$agentBin = Join-Path $root 'bin'
$out = Join-Path $root $OutDir

Write-Host 'Сборка установщика...'
& dotnet build $setupProj -c Release -v q --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'сборка установщика не удалась'
}

# Версия установщика должна совпадать с версией продукта из
# Directory.Build.props. Та же ловушка, что была с агентом: собрали решение,
# а упаковали exe прошлой версии — и в релиз уезжает старое число.
$setupExe = Join-Path $setupBin 'BackupDotNet.Setup.exe'
if (-not (Test-Path $setupExe)) {
    throw "нет собранного установщика: $setupExe"
}
$propsPath = Join-Path $root 'Directory.Build.props'
# PropertyGroup в файле несколько, поэтому берём первое непустое значение,
# а не свойство всего массива групп.
$wantVer = @(([xml](Get-Content $propsPath -Raw)).Project.PropertyGroup.VersionPrefix |
    Where-Object { $_ }) | Select-Object -First 1
if ($wantVer) {
    $haveVer = (Get-Item $setupExe).VersionInfo.FileVersion
    if ($haveVer -and $haveVer -notlike "$wantVer*") {
        throw ("установщик собран другой версией: exe={0}, ожидается {1}. " +
            "Удалите src\BackupDotNet.Setup\bin и пересоберите.") -f $haveVer, $wantVer
    }
    Write-Host "  установщик: версия $haveVer"
}

# Агент должен быть уже собран в bin (build.cmd).
$agentExe = Join-Path $agentBin 'BackupDotNet.exe'
if (-not (Test-Path $agentExe)) {
    throw "нет собранного агента: $agentExe (сначала build.cmd)"
}

# Версия в bin и в свежей сборке должны совпадать. Иначе легко упаковать
# установщик со старым агентом: bin обновляет только build.cmd, а dotnet build
# кладёт результат в src\...\bin\Release. Ошибка молчаливая и дорогая —
# в релиз уезжает прошлая версия.
$builtExe = Join-Path $root 'src\BackupDotNet.App\bin\Release\BackupDotNet.exe'
if (Test-Path $builtExe) {
    $binVer = (Get-Item $agentExe).VersionInfo.FileVersion
    $builtVer = (Get-Item $builtExe).VersionInfo.FileVersion
    if ($binVer -ne $builtVer) {
        throw ("агент в bin устарел: bin={0}, сборка={1}. Запустите build.cmd, " +
            "иначе установщик упакует старую версию.") -f $binVer, $builtVer
    }
    Write-Host "  агент: версия $binVer"
}

Write-Host "Собираю пакет в $out ..."
if (Test-Path $out) {
    Remove-Item $out -Recurse -Force
}
New-Item -ItemType Directory -Path $out -Force | Out-Null

# 1. Сам установщик и его зависимости.
Copy-Item (Join-Path $setupBin 'BackupDotNet.Setup.exe') $out
Copy-Item (Join-Path $setupBin 'BackupDotNet.Setup.exe.config') $out -ErrorAction SilentlyContinue
Copy-Item (Join-Path $setupBin 'setup-ui') $out -Recurse

# Брендинг рядом с установщиком: из него мастер берёт название продукта,
# пути и имя службы. Кладём копию выбранного файла, а не то, что лежит
# в bin — там рабочая конфигурация конкретной инсталляции.
$brandSrc = Join-Path $root $Branding
if (-not (Test-Path $brandSrc)) {
    throw "нет файла брендинга: $brandSrc"
}
$brandDir = Join-Path $out 'config'
New-Item -ItemType Directory -Path $brandDir -Force | Out-Null
Copy-Item $brandSrc (Join-Path $brandDir 'branding.json')
$brandName = (Get-Content $brandSrc -Raw -Encoding UTF8 | ConvertFrom-Json).brand.productName
Write-Host "  брендинг:   $brandName ($Branding)"

# Зависимости установщика (Core — ради Log и Json). Без них exe не запустится:
# проверено на практике, падает с FileNotFoundException ещё до окна.
Get-ChildItem $setupBin -Filter '*.dll' | ForEach-Object {
    Copy-Item $_.FullName $out
}

# 2. payload: файлы агента.
#
# Берём только то, что нужно для работы: exe, dll, public, config-шаблоны.
# Каталог data не копируем — это данные конкретной машины, их не должно
# быть в дистрибутиве.
$payload = Join-Path $out 'payload'
New-Item -ItemType Directory -Path $payload -Force | Out-Null

$copyFiles = @('BackupDotNet.exe', 'BackupDotNet.exe.config',
    'BackupDotNet.Core.dll', 'BackupDotNet.Web.dll',
    'BackupDotNet.Deploy.dll', 'BackupDotNet.Manage.dll',
    'install.ps1', 'LICENSE', 'README.md')

foreach ($name in $copyFiles) {
    $src = Join-Path $agentBin $name
    if (Test-Path $src) {
        Copy-Item $src $payload
    }
}

# public и config — целиком, но без *.json из config (это рабочие настройки).
foreach ($dir in @('public')) {
    $src = Join-Path $agentBin $dir
    if (Test-Path $src) {
        Copy-Item $src $payload -Recurse
    }
}

$cfgSrc = Join-Path $agentBin 'config'
if (Test-Path $cfgSrc) {
    $cfgDst = Join-Path $payload 'config'
    New-Item -ItemType Directory -Path $cfgDst -Force | Out-Null
    # Только шаблоны: *.example.json. branding.json не берём из bin —
    # там рабочая конфигурация этой машины; в payload кладём тот же
    # брендинг, что и рядом с установщиком.
    Get-ChildItem $cfgSrc -File | Where-Object {
        $_.Name -like '*.example.json'
    } | ForEach-Object { Copy-Item $_.FullName $cfgDst }

    Copy-Item $brandSrc (Join-Path $cfgDst 'branding.json')
}

$payloadFiles = (Get-ChildItem $payload -Recurse -File | Measure-Object).Count
Write-Host ("  payload: {0} файлов" -f $payloadFiles)

# 3. Проверка, что главное на месте.
$need = Join-Path $payload 'BackupDotNet.exe'
if (-not (Test-Path $need)) {
    throw 'в payload нет BackupDotNet.exe'
}

if ($Zip) {
    $zipPath = Join-Path $root "$OutDir.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Write-Host "Упаковываю в $zipPath ..."
    Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zipPath
    $mb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host ("  архив: {0} МБ" -f $mb)
}

Write-Host ''
Write-Host 'Готово:'
Write-Host ("  установщик: " + (Join-Path $out 'BackupDotNet.Setup.exe'))
Write-Host ("  payload:    " + $payload)
if ($Zip) {
    Write-Host ("  архив:      " + (Join-Path $root "$OutDir.zip"))
}
