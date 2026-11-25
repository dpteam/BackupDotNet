# Иконки установщиков Inno Setup: сервер и агент.
#
# Тот же щит, что у продукта, но с разными знаками внутри:
#   • сервер — стойка (три блока с индикаторами): «это полноценный сервер»;
#   • агент  — стрелка вниз: «ставим агента на машину».
#
# Отличие от setup-icon.ico (мастер на WebView2): тот же щит со стрелкой,
# но здесь знаки разные, чтобы два установщика не путались в проводнике
# и в «Установке и удалении программ».
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\generate-installer-icons.ps1
#
# Результат:
#   assets/server-icon.ico|png   — серверный установщик
#   assets/agent-icon.ico|png    — установщик агента

param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
if (-not (Test-Path $assets)) {
    New-Item -ItemType Directory -Path $assets -Force | Out-Null
}

# Палитра продукта
$white = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
$blueBright = [System.Drawing.Color]::FromArgb(255, 0x00, 0x88, 0xFF)
$blueDeep = [System.Drawing.Color]::FromArgb(255, 0x00, 0x80, 0xFF)

function New-ShieldPath {
    param([single]$Scale = 1.0)

    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $l = 150.0 * $Scale; $r = 850.0 * $Scale; $t = 110.0 * $Scale
    $shoulder = 520.0 * $Scale; $bottom = 900.0 * $Scale; $mid = 500.0 * $Scale
    $radius = 150.0 * $Scale

    $p.AddArc($l, $t, $radius, $radius, 180, 90)
    $p.AddLine($l + $radius, $t, $r - $radius, $t)
    $p.AddArc($r - $radius, $t, $radius, $radius, 270, 90)
    $p.AddLine($r, $t + $radius, $r, $shoulder)
    $p.AddBezier($r, $shoulder, $r, $shoulder + 170.0 * $Scale, $mid + 200.0 * $Scale, $bottom - 120.0 * $Scale, $mid, $bottom)
    $p.AddBezier($mid, $bottom, $mid - 200.0 * $Scale, $bottom - 120.0 * $Scale, $l, $shoulder + 170.0 * $Scale, $l, $shoulder)
    $p.CloseFigure()
    return $p
}

function New-ShieldBase {
    param([int]$Size = 512)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 1000.0

    $shield = New-ShieldPath -Scale $scale
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, [int](110 * $scale))),
        (New-Object System.Drawing.Point(0, [int](900 * $scale))),
        $blueBright, $blueDeep)
    $g.FillPath($grad, $shield)
    $grad.Dispose()

    $inner = New-ShieldPath -Scale ($scale * 0.76)
    $m = New-Object System.Drawing.Drawing2D.Matrix
    $m.Translate([single](120.0 * $scale), [single](105.0 * $scale))
    $inner.Transform($m)
    $m.Dispose()
    $pen = New-Object System.Drawing.Pen($white, [single][Math]::Max(1.0, 26.0 * $scale))
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($pen, $inner)
    $pen.Dispose()
    $inner.Dispose()
    $shield.Dispose()

    return @{ Bitmap = $bmp; Graphics = $g; Scale = $scale }
}

# --- сервер: стойка из трёх блоков с индикаторами ---------------------------

function New-ServerMark {
    param([int]$Size = 512)

    $base = New-ShieldBase -Size $Size
    $g = $base.Graphics
    $scale = $base.Scale

    $unitPen = New-Object System.Drawing.Pen($white, [single][Math]::Max(1.2, 38.0 * $scale))
    $unitPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $dotBrush = New-Object System.Drawing.SolidBrush($white)

    # Три блока стойки. Зазор больше толщины обводки: иначе блоки слипаются
    # в одно пятно — проверено на первом варианте.
    $left = 335.0 * $scale
    $right = 665.0 * $scale
    $top = 290.0 * $scale
    $height = 76.0 * $scale
    $gap = 62.0 * $scale

    for ($i = 0; $i -lt 3; $i++) {
        $y = $top + $i * ($height + $gap)
        $rect = New-Object System.Drawing.RectangleF(
            [single]$left, [single]$y, [single]($right - $left), [single]$height)
        $g.DrawRectangle($unitPen, $rect.X, $rect.Y, $rect.Width, $rect.Height)

        # Индикатор слева внутри блока — «работает».
        $d = 22.0 * $scale
        $g.FillEllipse($dotBrush,
            [single]($left + 30.0 * $scale),
            [single]($y + ($height - $d) / 2),
            [single]$d, [single]$d)
    }

    $unitPen.Dispose()
    $dotBrush.Dispose()
    $g.Dispose()
    return $base.Bitmap
}

# --- агент: стрелка вниз ----------------------------------------------------

function New-AgentMark {
    param([int]$Size = 512)

    $base = New-ShieldBase -Size $Size
    $g = $base.Graphics
    $scale = $base.Scale

    $arrowPen = New-Object System.Drawing.Pen($white, [single][Math]::Max(1.6, 92.0 * $scale))
    $arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $arrowPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $shaft = @(
        (New-Object System.Drawing.PointF([single](500.0 * $scale), [single](300.0 * $scale))),
        (New-Object System.Drawing.PointF([single](500.0 * $scale), [single](580.0 * $scale)))
    )
    $g.DrawLines($arrowPen, [System.Drawing.PointF[]]$shaft)

    $head = @(
        (New-Object System.Drawing.PointF([single](372.0 * $scale), [single](462.0 * $scale))),
        (New-Object System.Drawing.PointF([single](500.0 * $scale), [single](596.0 * $scale))),
        (New-Object System.Drawing.PointF([single](628.0 * $scale), [single](462.0 * $scale)))
    )
    $g.DrawLines($arrowPen, [System.Drawing.PointF[]]$head)
    $arrowPen.Dispose()

    # Точек под стрелкой нет: они налезали на внутренний контур щита
    # и читались как грязь. Стрелка вниз и так означает установку.

    $g.Dispose()
    return $base.Bitmap
}

# --- запись файлов ----------------------------------------------------------

function Write-Ico {
    param([object[]]$Frames, [string]$Path)

    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter($fs)
    try {
        $bw.Write([uint16]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]$Frames.Count)

        $offset = 6 + 16 * $Frames.Count
        foreach ($f in $Frames) {
            $s = [int]$f.Size
            $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
            $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
            $bw.Write([byte]0)
            $bw.Write([byte]0)
            $bw.Write([uint16]1)
            $bw.Write([uint16]32)
            $bw.Write([uint32]$f.Data.Length)
            $bw.Write([uint32]$offset)
            $offset += $f.Data.Length
        }
        foreach ($f in $Frames) {
            $bw.Write([byte[]]$f.Data)
        }
    }
    finally {
        $bw.Dispose()
        $fs.Dispose()
    }
}

function Save-IconSet {
    param([string]$Name, [scriptblock]$Make)

    $big = & $Make 512
    $big.Save((Join-Path $assets "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $big.Dispose()

    # Отдельный маленький PNG для заголовка мастера Inno (55x55).
    # Из 512 картинка ужимается с потерей чёткости, а в заголовке она
    # на виду всё время установки.
    $small = & $Make 55
    $small.Save((Join-Path $assets "$Name-wizard.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $small.Dispose()

    $frames = @()
    foreach ($s in @(16, 24, 32, 48, 64, 128, 256)) {
        $bmp = & $Make $s
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $frames += , @{ Size = $s; Data = $ms.ToArray() }
        $ms.Dispose()
        $bmp.Dispose()
    }

    $ico = Join-Path $assets "$Name.ico"
    Write-Ico -Frames $frames -Path $ico
    Write-Host ("  {0,-24} {1,6} КБ" -f "$Name.ico", [math]::Round((Get-Item $ico).Length / 1KB, 1))
}

Write-Host 'Генерация иконок установщиков...'
Save-IconSet 'server-icon' { param($s) New-ServerMark -Size $s }
Save-IconSet 'agent-icon' { param($s) New-AgentMark -Size $s }
Write-Host 'Готово.'
