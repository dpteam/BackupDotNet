# Заглушка GitHub API для проверки обновлений.
# Отдаёт релиз и архив так, как это делает github.com.
# Нужна, потому что анонимный лимит GitHub — 60 запросов в час на IP,
# и он легко исчерпывается при отладке.

$ErrorActionPreference = 'Stop'

$port = 9099
$zipPath = $args[0]
$version = if ($args.Count -gt 1) { $args[1] } else { '1.0.1' }
$badDigest = ($args.Count -gt 2 -and $args[2] -eq 'bad')

if (-not $zipPath -or -not (Test-Path $zipPath)) {
    Write-Host "Укажите путь к ZIP-архиву релиза"
    exit 1
}

$zipBytes = [System.IO.File]::ReadAllBytes($zipPath)
$sha = [System.Security.Cryptography.SHA256]::Create()
$digest = 'sha256:' + (($sha.ComputeHash($zipBytes) | ForEach-Object { $_.ToString('x2') }) -join '')
$sha.Dispose()
if ($badDigest) {
    # Намеренно неверная сумма: проверяем, что агент откажется ставить архив.
    $digest = 'sha256:' + ('0' * 64)
    Write-Host 'ВНИМАНИЕ: отдаю неверную контрольную сумму (режим проверки)'
}

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://127.0.0.1:$port/")
$listener.Start()
Write-Host "GitHub API заглушка: http://127.0.0.1:$port/  (версия $version)"

while ($true) {
    $ctx = $listener.GetContext()
    $path = $ctx.Request.Url.AbsolutePath
    Write-Host "  $($ctx.Request.HttpMethod) $path"

    try {
        if ($path -like '*/releases/latest') {
            $asset = @{
                name                 = "BackupDotNet-$version.zip"
                size                 = $zipBytes.Length
                browser_download_url = "http://127.0.0.1:$port/download/BackupDotNet-$version.zip"
                digest               = $digest
            }
            $body = @{
                tag_name     = "v$version"
                name         = "BackupDotNet $version"
                body         = "Тестовый релиз для проверки механизма обновления.`n`n- проверка скачивания`n- проверка установки"
                published_at = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
                prerelease   = $false
                assets       = @($asset)
            } | ConvertTo-Json -Depth 6
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
            $ctx.Response.ContentType = 'application/json; charset=utf-8'
            $ctx.Response.StatusCode = 200
            $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        }
        elseif ($path -like '/download/*') {
            $ctx.Response.ContentType = 'application/zip'
            $ctx.Response.ContentLength64 = $zipBytes.Length
            $ctx.Response.StatusCode = 200
            $ctx.Response.OutputStream.Write($zipBytes, 0, $zipBytes.Length)
        }
        else {
            $ctx.Response.StatusCode = 404
        }
    }
    catch {
        Write-Host "  ошибка: $($_.Exception.Message)"
    }
    finally {
        try { $ctx.Response.Close() } catch { }
    }
}
