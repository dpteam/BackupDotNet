<#
.SYNOPSIS
    Показывает ROADMAP.md в браузере.

.DESCRIPTION
    Конвертирует Markdown роадмапа в самодостаточный HTML (стили встроены,
    палитра как у веб-интерфейса) и открывает его в браузере по умолчанию.

    Нужно, чтобы прогресс можно было смотреть всегда — в том числе когда
    встроенная панель просмотра OpenCode недоступна.

    Результат: artifacts\roadmap.html (перезаписывается при каждом запуске).

.PARAMETER File
    Какой файл показывать. По умолчанию ROADMAP.md.

.PARAMETER NoOpen
    Только собрать HTML, не открывать браузер.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\show-roadmap.ps1
#>

[CmdletBinding()]
param(
    [string]$File = 'ROADMAP.md',
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root $File
if (-not (Test-Path $src)) {
    throw "Не найден $src"
}

$md = Get-Content $src -Raw -Encoding UTF8

function ConvertTo-HtmlText([string]$s) {
    $s = $s -replace '&', '&amp;'
    $s = $s -replace '<', '&lt;'
    $s = $s -replace '>', '&gt;'
    return $s
}

# Инлайн-разметка: код, жирный, курсив, ссылки. Порядок важен.
function Format-Inline([string]$s) {
    $s = ConvertTo-HtmlText $s
    $s = [regex]::Replace($s, '`([^`]+)`', '<code>$1</code>')
    $s = [regex]::Replace($s, '\*\*([^*]+)\*\*', '<strong>$1</strong>')
    $s = [regex]::Replace($s, '(?<![\w*])\*([^*\n]+)\*(?![\w*])', '<em>$1</em>')
    $s = [regex]::Replace($s, '\[([^\]]+)\]\(([^)]+)\)', '<a href="$2">$1</a>')
    return $s
}

$lines = $md -split "`r?`n"
$out = New-Object System.Text.StringBuilder
$inCode = $false
$inTable = $false
$listStack = New-Object System.Collections.ArrayList
$para = New-Object System.Collections.ArrayList

function Close-Para {
    if ($script:para.Count -gt 0) {
        $text = ($script:para -join ' ')
        [void]$script:out.AppendLine('<p>' + (Format-Inline $text) + '</p>')
        $script:para.Clear()
    }
}

function Close-List {
    while ($script:listStack.Count -gt 0) {
        $tag = $script:listStack[$script:listStack.Count - 1]
        [void]$script:out.AppendLine("</$tag>")
        $script:listStack.RemoveAt($script:listStack.Count - 1)
    }
}

function Close-Table {
    if ($script:inTable) {
        [void]$script:out.AppendLine('</tbody></table>')
        $script:inTable = $false
    }
}

function Close-All {
    Close-Para
    Close-List
    Close-Table
}

foreach ($line in $lines) {
    # --- блок кода ---
    if ($line -match '^\s*```') {
        Close-Para
        Close-List
        Close-Table
        if ($inCode) {
            [void]$out.AppendLine('</code></pre>')
            $inCode = $false
        } else {
            [void]$out.AppendLine('<pre><code>')
            $inCode = $true
        }
        continue
    }
    if ($inCode) {
        [void]$out.AppendLine((ConvertTo-HtmlText $line))
        continue
    }

    # --- горизонтальная линия ---
    if ($line -match '^\s*(---|\*\*\*|___)\s*$') {
        Close-All
        [void]$out.AppendLine('<hr>')
        continue
    }

    # --- заголовки ---
    if ($line -match '^(#{1,6})\s+(.*)$') {
        Close-All
        $level = $Matches[1].Length
        $text = $Matches[2]
        $anchor = ($text -replace '[^\w\u0400-\u04FF]+', '-').Trim('-').ToLowerInvariant()
        [void]$out.AppendLine("<h$level id=`"$anchor`">" + (Format-Inline $text) + "</h$level>")
        continue
    }

    # --- таблица ---
    if ($line -match '^\s*\|(.+)\|\s*$') {
        $cells = ($Matches[1] -split '\|') | ForEach-Object { $_.Trim() }
        # строка-разделитель |---|---|
        if (($cells | Where-Object { $_ -notmatch '^:?-{2,}:?$' }).Count -eq 0) {
            continue
        }
        if (-not $inTable) {
            Close-Para
            Close-List
            [void]$out.AppendLine('<table><thead><tr>')
            foreach ($c in $cells) {
                [void]$out.AppendLine('<th>' + (Format-Inline $c) + '</th>')
            }
            [void]$out.AppendLine('</tr></thead><tbody>')
            $inTable = $true
        } else {
            [void]$out.AppendLine('<tr>')
            foreach ($c in $cells) {
                [void]$out.AppendLine('<td>' + (Format-Inline $c) + '</td>')
            }
            [void]$out.AppendLine('</tr>')
        }
        continue
    } else {
        Close-Table
    }

    # --- списки ---
    if ($line -match '^(\s*)([-*+]|\d+\.)\s+(.*)$') {
        Close-Para
        $indent = $Matches[1].Length
        $marker = $Matches[2]
        $text = $Matches[3]
        $tag = if ($marker -match '^\d') { 'ol' } else { 'ul' }
        $depth = [Math]::Floor($indent / 2) + 1

        while ($listStack.Count -gt $depth) {
            $t = $listStack[$listStack.Count - 1]
            [void]$out.AppendLine("</$t>")
            $listStack.RemoveAt($listStack.Count - 1)
        }
        if ($listStack.Count -lt $depth) {
            [void]$out.AppendLine("<$tag>")
            [void]$listStack.Add($tag)
        }
        [void]$out.AppendLine('<li>' + (Format-Inline $text) + '</li>')
        continue
    }

    # --- пустая строка ---
    if ($line -match '^\s*$') {
        Close-Para
        Close-List
        continue
    }

    # --- цитата ---
    if ($line -match '^\s*&gt;\s?(.*)$' -or $line -match '^\s*>\s?(.*)$') {
        Close-Para
        [void]$out.AppendLine('<blockquote>' + (Format-Inline $Matches[1]) + '</blockquote>')
        continue
    }

    # --- обычный абзац ---
    Close-List
    [void]$para.Add($line.Trim())
}
Close-All

$title = 'ROADMAP'
if ($md -match '(?m)^#\s+(.+)$') { $title = $Matches[1].Trim() }

$html = @"
<!DOCTYPE html>
<html lang="ru" data-theme="light">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>$title</title>
<style>
  :root {
    --brand: #0066CC; --brand-hi: #0088FF; --brand-dark: #000088;
    --bg: #F6F8FF; --surface: #FFFFFF; --ink: #1A2233; --ink-soft: #4A5568;
    --muted: #8A90A2; --border: #D8DCEA; --code-bg: #0B1220; --code-ink: #D6E2F5;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0; padding: 0 20px 80px;
    background: var(--bg); color: var(--ink);
    font: 15px/1.65 -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
  }
  .wrap { max-width: 980px; margin: 0 auto; }
  header {
    position: sticky; top: 0; z-index: 10;
    margin: 0 -20px 28px; padding: 14px 20px;
    background: linear-gradient(180deg, #0047B3 0%, #002E7A 55%, #001A4D 100%);
    border-bottom: 1px solid #00005A;
    box-shadow: 0 1px 8px rgba(0, 24, 74, .35);
  }
  header .wrap { display: flex; align-items: baseline; gap: 12px; }
  header .name { font-weight: 800; font-size: 17px; color: #fff; letter-spacing: .3px; }
  header .name .a { color: #0088FF; }
  header .stamp { margin-left: auto; font-size: 12px; color: #9FB6E0; }
  h1 { font-size: 27px; margin: 34px 0 14px; letter-spacing: -.2px; }
  h2 {
    font-size: 21px; margin: 40px 0 14px; padding-bottom: 8px;
    border-bottom: 2px solid var(--brand); color: #00225C;
  }
  h3 { font-size: 16.5px; margin: 26px 0 10px; color: #003A99; }
  h4 { font-size: 14.5px; margin: 20px 0 8px; color: var(--ink-soft); }
  p { margin: 10px 0; }
  a { color: var(--brand); text-decoration: none; }
  a:hover { text-decoration: underline; }
  code {
    background: #E8EEFB; color: #003A99;
    padding: 1px 5px; border-radius: 4px;
    font-family: Consolas, "Cascadia Mono", monospace; font-size: 13px;
  }
  pre {
    background: var(--code-bg); color: var(--code-ink);
    padding: 14px 16px; border-radius: 8px; overflow-x: auto;
    border: 1px solid #1E2A44; line-height: 1.5;
  }
  pre code { background: none; color: inherit; padding: 0; font-size: 12.5px; }
  ul, ol { margin: 10px 0; padding-left: 26px; }
  li { margin: 5px 0; }
  li > ul, li > ol { margin: 5px 0; }
  hr { border: none; border-top: 1px solid var(--border); margin: 32px 0; }
  table {
    width: 100%; border-collapse: collapse; margin: 16px 0;
    background: var(--surface); border: 1px solid var(--border);
    border-radius: 8px; overflow: hidden; font-size: 14px;
  }
  th {
    background: linear-gradient(180deg, #EEF3FD, #E4ECFA);
    text-align: left; padding: 9px 12px; font-size: 12.5px;
    color: var(--ink-soft); text-transform: uppercase; letter-spacing: .4px;
    border-bottom: 1px solid var(--border);
  }
  td { padding: 8px 12px; border-bottom: 1px solid #EEF1F8; vertical-align: top; }
  tr:last-child td { border-bottom: none; }
  tbody tr:hover { background: #F7FAFF; }
  blockquote {
    margin: 14px 0; padding: 10px 16px;
    border-left: 4px solid var(--brand-hi);
    background: #EEF4FE; color: var(--ink-soft); border-radius: 0 6px 6px 0;
  }
  strong { color: #001A4D; }
  @media (max-width: 700px) {
    body { font-size: 14px; }
    h1 { font-size: 22px; }
    h2 { font-size: 18px; }
    table { font-size: 13px; }
  }
  @media print {
    header { display: none; }
    body { background: #fff; }
    pre { background: #f4f6fa; color: #111; border-color: #ccc; }
    pre code { color: #111; }
  }
</style>
</head>
<body>
<header>
  <div class="wrap">
    <span class="name"><span class="a">DPT</span>.Backup</span>
    <span class="stamp">роадмап · обновлён $(Get-Date -Format 'dd.MM.yyyy HH:mm')</span>
  </div>
</header>
<div class="wrap">
$($out.ToString())
</div>
</body>
</html>
"@

$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$dest = Join-Path $artifacts 'roadmap.html'
[System.IO.File]::WriteAllText($dest, $html, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "HTML: $dest"

if (-not $NoOpen) {
    Start-Process $dest
    Write-Host 'Открыто в браузере по умолчанию.'
}
