# Иконка установщика BackupDotNet.
#
# Тот же щит, что у агента, но вместо галочки — стрелка вниз: символ установки.
# Узнаваемо как один продукт, отличимо от уже установленного агента.
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\generate-setup-icon.ps1
#
# Результат:
#   assets/setup-icon.ico   — иконка exe установщика
#   assets/setup-icon.png   — 512x512 для окна и документации

param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
if (-not (Test-Path $assets)) {
    New-Item -ItemType Directory -Path $assets -Force | Out-Null
}

# Палитра продукта: #0088FF / #0080FF / #FFFFFF / #000088
$white = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
$blueBright = [System.Drawing.Color]::FromArgb(255, 0x00, 0x88, 0xFF)
$blueDeep = [System.Drawing.Color]::FromArgb(255, 0x00, 0x80, 0xFF)
$navyInk = [System.Drawing.Color]::FromArgb(255, 0x00, 0x00, 0x88)

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

function New-SetupMark {
    param([int]$Size = 512)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 1000.0

    # Щит с градиентом — как у агента
    $shield = New-ShieldPath -Scale $scale
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, [int](110 * $scale))),
        (New-Object System.Drawing.Point(0, [int](900 * $scale))),
        $blueBright, $blueDeep)
    $g.FillPath($grad, $shield)
    $grad.Dispose()

    # Внутренний контур
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

    # Стрелка вниз: древко + острие. Полоску-«пол» убрал: стрелка
    # упиралась в неё и сливалась в одно пятно.
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

    $shield.Dispose()
    $g.Dispose()
    return $bmp
}

function Save-Png {
    param([System.Drawing.Bitmap]$Bitmap, [string]$Path)
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

# --- PNG 512 для окна и документации
$big = New-SetupMark -Size 512
Save-Png $big (Join-Path $assets 'setup-icon.png')
$big.Dispose()

# --- ICO с несколькими размерами
$iconSizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = @()
foreach ($s in $iconSizes) {
    $bmp = New-SetupMark -Size $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += , @{ Size = $s; Data = $ms.ToArray() }
    $ms.Dispose()
    $bmp.Dispose()
}

function Write-Ico {
    param([object[]]$Frames, [string]$Path)

    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter($fs)
    try {
        $bw.Write([uint16]0)              # reserved
        $bw.Write([uint16]1)              # type: icon
        $bw.Write([uint16]$Frames.Count)  # count

        $offset = 6 + 16 * $Frames.Count
        foreach ($f in $Frames) {
            $s = [int]$f.Size
            $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
            $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
            $bw.Write([byte]0)            # palette
            $bw.Write([byte]0)            # reserved
            $bw.Write([uint16]1)          # planes
            $bw.Write([uint16]32)         # bpp
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

$icoPath = Join-Path $assets 'setup-icon.ico'
Write-Ico -Frames $frames -Path $icoPath

Write-Host 'Готово:'
Write-Host ('  ' + $icoPath)
Write-Host ('  ' + (Join-Path $assets 'setup-icon.png'))
