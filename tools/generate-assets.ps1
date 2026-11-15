<#
.SYNOPSIS
    Генератор логотипа, фавикона и иконки приложения BackupDotNet.

.DESCRIPTION
    Рисует фирменный знак (щит с галочкой) средствами System.Drawing —
    никаких сторонних библиотек и внешних сервисов.
    Результат:
        assets/logo.png        512x512  (светлая тема: тёмный щит)
        assets/logo-dark.png   512x512  (тёмная тема: светлый щит)
        assets/icon.ico        мультиразмерная иконка для exe и favicon
        public/img/logo.png
        public/img/logo-dark.png
        public/img/favicon.ico
        public/img/apple-touch-icon.png

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\generate-assets.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }

$assets = Join-Path $root 'assets'
$imgDir = Join-Path $root 'public\img'
New-Item -ItemType Directory -Force -Path $assets, $imgDir | Out-Null

# ------------------------------------------------------------------ палитра
# Синяя гамма: #0088FF / #0080FF / #FFFFFF / #000088
$blueBright  = [System.Drawing.Color]::FromArgb(255, 0x00, 0x88, 0xFF)  # #0088FF
$blueDeep    = [System.Drawing.Color]::FromArgb(255, 0x00, 0x80, 0xFF)  # #0080FF
$blueDark    = [System.Drawing.Color]::FromArgb(255, 0x00, 0x4C, 0xCC)  # #004CCC — низ градиента
$blueSoft    = [System.Drawing.Color]::FromArgb(255, 0x55, 0xAA, 0xFF)  # #55AAFF — для тёмной темы
$navyInk     = [System.Drawing.Color]::FromArgb(255, 0x00, 0x00, 0x88)  # #000088
$white       = [System.Drawing.Color]::White

function New-ShieldPath {
    param([single]$Scale = 1.0)

    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    # координаты в системе 0..1000
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

function New-Mark {
    param(
        [int]$Size = 512,
        [System.Drawing.Color]$ShieldColor,
        [System.Drawing.Color]$Accent1,
        [System.Drawing.Color]$Accent2,
        [switch]$RoundedTile
    )
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 1000.0

    if ($RoundedTile) {
        $tile = New-Object System.Drawing.Drawing2D.GraphicsPath
        $inset = 40.0 * $scale
        $side = $Size - 2 * $inset
        $rad = $side * 0.22
        $tile.AddArc($inset, $inset, $rad, $rad, 180, 90)
        $tile.AddArc($inset + $side - $rad, $inset, $rad, $rad, 270, 90)
        $tile.AddArc($inset + $side - $rad, $inset + $side - $rad, $rad, $rad, 0, 90)
        $tile.AddArc($inset, $inset + $side - $rad, $rad, $rad, 90, 90)
        $tile.CloseFigure()
        $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            (New-Object System.Drawing.Point(0, 0)),
            (New-Object System.Drawing.Point($Size, $Size)),
            [System.Drawing.Color]::FromArgb(255, 0x00, 0x00, 0x88),
            [System.Drawing.Color]::FromArgb(255, 0x00, 0x1B, 0x4D))
        $g.FillPath($bgBrush, $tile)
        $bgBrush.Dispose()
        $tile.Dispose()
    }

    # тень (только для «чистого» логотипа — на плитке иконки она лишняя)
    if (-not $RoundedTile) {
        $shadow = New-Object System.Drawing.Drawing2D.GraphicsPath
        $shadow.AddEllipse(220.0 * $scale, 830.0 * $scale, 560.0 * $scale, 90.0 * $scale)
        $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38, 0, 0, 0))
        $g.FillPath($shadowBrush, $shadow)
        $shadowBrush.Dispose()
        $shadow.Dispose()
    }

    $shield = New-ShieldPath -Scale $scale

    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, (110.0 * $scale))),
        (New-Object System.Drawing.Point(0, (900.0 * $scale))),
        $Accent1, $Accent2)
    $g.FillPath($grad, $shield)
    $grad.Dispose()

    # внутренний контур
    $inner = New-ShieldPath -Scale ($scale * 0.76)
    $innerMatrix = New-Object System.Drawing.Drawing2D.Matrix
    $innerMatrix.Translate([single](120.0 * $scale), [single](105.0 * $scale))
    $inner.Transform($innerMatrix)
    $innerMatrix.Dispose()
    $pen = New-Object System.Drawing.Pen($ShieldColor, [single][Math]::Max(1.0, 26.0 * $scale))
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($pen, $inner)
    $pen.Dispose()
    $inner.Dispose()

    # галочка
    $checkPen = New-Object System.Drawing.Pen($ShieldColor, [single][Math]::Max(1.6, 92.0 * $scale))
    $checkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $checkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $checkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pts = @(
        (New-Object System.Drawing.PointF([single](360.0 * $scale), [single](470.0 * $scale))),
        (New-Object System.Drawing.PointF([single](455.0 * $scale), [single](565.0 * $scale))),
        (New-Object System.Drawing.PointF([single](640.0 * $scale), [single](380.0 * $scale)))
    )
    $g.DrawLines($checkPen, [System.Drawing.PointF[]]$pts)
    $checkPen.Dispose()

    $shield.Dispose()
    $g.Dispose()
    return $bmp
}

function Save-Png {
    param([System.Drawing.Bitmap]$Bitmap, [string]$Path)
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

# ------------------------------------------------------------ логотипы тем
# Светлая тема: насыщенный синий градиент #0088FF → #004CCC, белая галочка.
$logoLight = New-Mark -Size 512 -ShieldColor $white -Accent1 $blueBright -Accent2 $blueDark
# Тёмная тема: чуть более светлый градиент #55AAFF → #0080FF, чтобы не тонул на тёмном фоне.
$logoDark  = New-Mark -Size 512 -ShieldColor $white -Accent1 $blueSoft  -Accent2 $blueDeep

Save-Png $logoLight (Join-Path $assets 'logo.png')
Save-Png $logoDark  (Join-Path $assets 'logo-dark.png')
Save-Png $logoLight (Join-Path $imgDir 'logo.png')
Save-Png $logoDark  (Join-Path $imgDir 'logo-dark.png')

# ------------------------------------------------------------ иконки
$iconSizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = @()
foreach ($s in $iconSizes) {
    $bmp = New-Mark -Size $s -ShieldColor $white -Accent1 $blueBright -Accent2 $blueDark -RoundedTile
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += [pscustomobject]@{ Size = $s; Bytes = $ms.ToArray() }
    $bmp.Dispose()
    $ms.Dispose()
}

function Write-Ico {
    param([object[]]$Frames, [string]$Path)
    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter($fs)
    $bw.Write([uint16]0)                 # reserved
    $bw.Write([uint16]1)                 # type: icon
    $bw.Write([uint16]$Frames.Count)     # count
    $offset = 6 + 16 * $Frames.Count
    foreach ($f in $Frames) {
        $dim = if ($f.Size -ge 256) { 0 } else { $f.Size }
        $bw.Write([byte]$dim)
        $bw.Write([byte]$dim)
        $bw.Write([byte]0)               # palette
        $bw.Write([byte]0)               # reserved
        $bw.Write([uint16]1)             # planes
        $bw.Write([uint16]32)            # bpp
        $bw.Write([uint32]$f.Bytes.Length)
        $bw.Write([uint32]$offset)
        $offset += $f.Bytes.Length
    }
    foreach ($f in $Frames) { $bw.Write($f.Bytes) }
    $bw.Flush()
    $bw.Dispose()
    $fs.Dispose()
}

Write-Ico -Frames $frames -Path (Join-Path $assets 'icon.ico')
Copy-Item (Join-Path $assets 'icon.ico') (Join-Path $imgDir 'favicon.ico') -Force

$touch = New-Mark -Size 180 -ShieldColor $white -Accent1 $blueBright -Accent2 $blueDark -RoundedTile
Save-Png $touch (Join-Path $imgDir 'apple-touch-icon.png')

$logoLight.Dispose(); $logoDark.Dispose(); $touch.Dispose()

Write-Host 'Готово:' (Join-Path $assets 'logo.png') ', logo-dark.png, icon.ico, public/img/*'
