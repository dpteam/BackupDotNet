# Мини-сервис, изображающий Veeam Backup & Replication REST API.
#
# Зачем: проверить коллектор veeam-rest без настоящего Veeam. Отвечает на те
# три запроса, которые делает коллектор, и умеет несколько сценариев.
#
#   ok      — задание "Backup 1C" завершилось успешно;
#   failed  — последняя сессия Failed с сообщением;
#   warning — последняя сессия Warning;
#   working — сессия идёт прямо сейчас;
#   empty   — заданий и сессий нет (проверка «пустого» Veeam).
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\veeam-mock.ps1 -Port 9419 -Scenario failed
#
# Порт по умолчанию 9419 — тот же, что у настоящего Veeam (в этом и смысл:
# проверить, что мы не перепутали порт и путь).
#
# Реализация на TcpListener, а не HttpListener: HttpListener на этом порту
# требует urlacl, а мок должен запускаться без прав администратора.
#
# JSON собирается строками, а не ConvertTo-Json: тот разворачивает массив
# из одного элемента в объект, и коллектор получает `data: {...}` вместо
# `data: [{...}]`. Настоящий Veeam всегда отдаёт массив — мок обязан так же.

param(
    [int]$Port = 9419,
    [ValidateSet('ok', 'failed', 'warning', 'working', 'empty')]
    [string]$Scenario = 'ok'
)

$ErrorActionPreference = 'Stop'

$now = (Get-Date).ToUniversalTime()
$created = $now.AddMinutes(-25).ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$ended = $now.AddMinutes(-20).ToString('yyyy-MM-ddTHH:mm:ss.fffZ')

function Json-Str($s) {
    if ($null -eq $s) { return 'null' }
    $escaped = $s.Replace('\', '\\').Replace('"', '\"').Replace("`r", '').Replace("`n", ' ')
    return '"' + $escaped + '"'
}

# Одна сессия задания. Поля те же, что отдаёт Veeam 12: result — объект,
# creationTime/endTime — ISO-время, sessionType — тип сессии.
function New-SessionJson($result, $message, $state, $withEnd) {
    $resultJson = if ($result) {
        '{"result":' + (Json-Str $result) + ',"message":' + (Json-Str $message) + '}'
    } else {
        'null'
    }
    $endJson = if ($withEnd) { Json-Str $ended } else { 'null' }
    return '{' +
        '"id":"sess-1",' +
        '"jobId":"job-1",' +
        '"name":"Backup 1C",' +
        '"sessionType":"Backup",' +
        '"state":' + (Json-Str $state) + ',' +
        '"creationTime":' + (Json-Str $created) + ',' +
        '"endTime":' + $endJson + ',' +
        '"result":' + $resultJson +
        '}'
}

$sessionJson = switch ($Scenario) {
    'ok'      { New-SessionJson 'Success' '' 'Stopped' $true }
    'failed'  { New-SessionJson 'Failed' 'Недостаточно места на репозитории' 'Stopped' $true }
    'warning' { New-SessionJson 'Warning' 'Часть объектов пропущена' 'Stopped' $true }
    'working' { New-SessionJson '' '' 'Working' $false }
    'empty'   { '' }
}

$hasJob = ($Scenario -ne 'empty')

function Write-Response($stream, $code, $json) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $head = "HTTP/1.1 $code OK`r`n" +
            "Content-Type: application/json; charset=utf-8`r`n" +
            "Content-Length: $($bytes.Length)`r`n" +
            "Connection: close`r`n`r`n"
    $hb = [System.Text.Encoding]::ASCII.GetBytes($head)
    $stream.Write($hb, 0, $hb.Length)
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Flush()
}

$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $Port)
$listener.Start()
Write-Host "Veeam mock на http://127.0.0.1:$Port/ (сценарий: $Scenario)"

try {
    while ($true) {
        $client = $listener.AcceptTcpClient()
        try {
            $stream = $client.GetStream()
            $stream.ReadTimeout = 10000

            # Читаем заголовки по байтам, пока не встретим пустую строку.
            # Конец заголовков — CRLF CRLF, то есть первые четыре байта
            # разрыва, а не первый: на первом CRLF заголовки ещё не кончились.
            $sb = New-Object System.Text.StringBuilder
            $tail = New-Object System.Collections.Generic.List[int]
            while ($true) {
                $b = $stream.ReadByte()
                if ($b -lt 0) { break }
                [void]$sb.Append([char]$b)
                $tail.Add($b)
                if ($tail.Count -gt 4) { $tail.RemoveAt(0) }
                if ($tail.Count -eq 4 -and
                    $tail[0] -eq 13 -and $tail[1] -eq 10 -and
                    $tail[2] -eq 13 -and $tail[3] -eq 10) { break }
            }
            $head = $sb.ToString()
            if (-not $head) { continue }

            $lines = $head -split "`r`n"
            $parts = $lines[0] -split ' '
            $method = $parts[0]
            $path = $parts[1]
            if ($path.Contains('?')) { $path = $path.Substring(0, $path.IndexOf('?')) }

            # Дочитываем тело по Content-Length: коллектор шлёт форму логина.
            $contentLength = 0
            $auth = ''
            foreach ($l in $lines) {
                if ($l -like 'Content-Length:*') { $contentLength = [int](($l -split ':')[1].Trim()) }
                if ($l -like 'Authorization:*') { $auth = ($l -split ':', 2)[1].Trim() }
            }
            if ($contentLength -gt 0) {
                $buf = New-Object byte[] $contentLength
                $read = 0
                while ($read -lt $contentLength) {
                    $n = $stream.Read($buf, $read, $contentLength - $read)
                    if ($n -le 0) { break }
                    $read += $n
                }
            }

            Write-Host "$method $path"

            # Авторизация: коллектор шлёт form-urlencoded, ждёт access_token.
            if ($path -eq '/api/oauth2/token' -and $method -eq 'POST') {
                Write-Response $stream 200 '{"access_token":"mock-token","token_type":"bearer","expires_in":3600}'
                continue
            }

            # Без токена не пускаем: так проверяется, что коллектор его передаёт.
            if ($auth -ne 'Bearer mock-token') {
                Write-Response $stream 401 '{"message":"unauthorized"}'
                continue
            }

            if ($path -eq '/api/v1/jobs' -and $method -eq 'GET') {
                $data = if ($hasJob) { '[{"id":"job-1","name":"Backup 1C","type":"Backup"}]' } else { '[]' }
                $total = if ($hasJob) { 1 } else { 0 }
                Write-Response $stream 200 ('{"data":' + $data + ',"pagination":{"total":' + $total + ',"count":' + $total + ',"skip":0}}')
                continue
            }

            if ($path -eq '/api/v1/sessions' -and $method -eq 'GET') {
                $data = if ($sessionJson) { '[' + $sessionJson + ']' } else { '[]' }
                $total = if ($sessionJson) { 1 } else { 0 }
                Write-Response $stream 200 ('{"data":' + $data + ',"pagination":{"total":' + $total + ',"count":' + $total + ',"skip":0}}')
                continue
            }

            Write-Response $stream 404 '{"message":"not found"}'
        }
        catch {
            Write-Host "ошибка обработки: $($_.Exception.Message)"
        }
        finally {
            $client.Close()
        }
    }
}
finally {
    $listener.Stop()
}
