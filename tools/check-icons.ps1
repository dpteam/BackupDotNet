# Проверка иконок: все ли классы fa-* из кода есть в подключённом шрифте.
#
# Зачем: в проекте Font Awesome 5.15.4, а имена в FA6 другие
# (fa-circle-check вместо fa-check-circle, fa-hard-drive вместо fa-hdd).
# Опечатка не ломает сборку — кнопка просто остаётся без картинки,
# и заметить это можно только глазами на нужной странице.
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\check-icons.ps1
#
# Возвращает код 1, если есть битые классы (удобно для CI).

param(
    [string]$PublicDir = "public",
    [string]$CssPath = "public/vendor/fa/css/all.min.css"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $CssPath)) {
    Write-Host "Нет файла стилей: $CssPath" -ForegroundColor Red
    exit 1
}

$css = Get-Content $CssPath -Raw -Encoding UTF8

# Модификаторы Font Awesome: они не рисуют иконку, поэтому :before у них нет.
$modifiers = @('fa-spin', 'fa-pulse', 'fa-fw', 'fa-lg', 'fa-sm', 'fa-xs',
    'fa-2x', 'fa-3x', 'fa-4x', 'fa-5x', 'fa-ul', 'fa-li', 'fa-border',
    'fa-pull-left', 'fa-pull-right', 'fa-rotate-90', 'fa-rotate-180',
    'fa-rotate-270', 'fa-flip-horizontal', 'fa-flip-vertical', 'fa-stack',
    'fa-inverse')

$used = @{}
foreach ($file in (Get-ChildItem $PublicDir -Recurse -Include *.js, *.html)) {
    $text = Get-Content $file.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($text, 'fa-[a-z0-9-]+')) {
        $used[$m.Value] = $file.Name
    }
}

$broken = @()
foreach ($name in $used.Keys) {
    if ($modifiers -contains $name) {
        continue
    }
    # Важно: склейка через переменную. Строка "`.$name:before" PowerShell
    # читает как переменную $name с областью before — проверка молча врёт.
    $needle = "." + $name + ":before"
    if ($css.IndexOf($needle) -lt 0) {
        $broken += [pscustomobject]@{ Icon = $name; File = $used[$name] }
    }
}

Write-Host ("Классов найдено: {0}" -f $used.Count)

if ($broken.Count -eq 0) {
    Write-Host "Битых иконок нет." -ForegroundColor Green
    exit 0
}

Write-Host ("Битых иконок: {0}" -f $broken.Count) -ForegroundColor Red
$broken | Sort-Object Icon | ForEach-Object {
    Write-Host ("  {0,-30} {1}" -f $_.Icon, $_.File)
}
Write-Host ""
Write-Host "Подсказка: FA5 имена отличаются от FA6. Частые замены:"
Write-Host "  fa-circle-check          -> fa-check-circle"
Write-Host "  fa-circle-info           -> fa-info-circle"
Write-Host "  fa-circle-question       -> fa-question-circle"
Write-Host "  fa-triangle-exclamation  -> fa-exclamation-triangle"
Write-Host "  fa-hard-drive            -> fa-hdd"
Write-Host "  fa-box-archive           -> fa-archive"
Write-Host "  fa-trash-can             -> fa-trash"
Write-Host "  fa-file-lines            -> fa-file-alt"
Write-Host "  fa-list-check            -> fa-tasks"
Write-Host "  fa-clock-rotate-left     -> fa-history"
Write-Host "  fa-rotate-left           -> fa-undo"
Write-Host "  fa-calendar-days         -> fa-calendar-alt"
Write-Host "  fa-cloud-arrow-down      -> fa-cloud-download-alt"
Write-Host "  fa-file-zipper           -> fa-file-archive"
Write-Host "  fa-folder-tree           -> fa-sitemap"
Write-Host "  fa-shield-check          -> fa-shield-alt"
Write-Host "  fa-circle-up             -> fa-arrow-circle-up"
Write-Host "  fa-diagram-project       -> fa-project-diagram"

exit 1
