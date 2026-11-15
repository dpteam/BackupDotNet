# Мини-приёмник SMTP для проверки уведомлений.
# Слушает 2525, пишет всё в stdout. Никакой аутентификации — только для тестов.

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 2525)
$listener.Start()
Write-Host "SMTP test server on 127.0.0.1:2525"

$out = Join-Path $env:TEMP 'smtp-capture.txt'
Remove-Item $out -Force -ErrorAction SilentlyContinue

while ($true) {
    $client = $listener.AcceptTcpClient()
    $stream = $client.GetStream()
    $reader = [System.IO.StreamReader]::new($stream)
    $writer = [System.IO.StreamWriter]::new($stream)
    $writer.NewLine = "`r`n"
    $writer.AutoFlush = $true

    $writer.WriteLine("220 test.local ESMTP ready")

    $inData = $false
    $data = New-Object System.Text.StringBuilder

    while ($true) {
        $line = $reader.ReadLine()
        if ($null -eq $line) { break }

        if ($inData) {
            if ($line -eq '.') {
                $inData = $false
                $writer.WriteLine('250 2.0.0 queued as TEST')
                Add-Content -Path $out -Value $data.ToString() -Encoding UTF8
                continue
            }
            [void]$data.AppendLine($line)
            continue
        }

        $upper = $line.ToUpperInvariant()
        if ($upper.StartsWith('EHLO') -or $upper.StartsWith('HELO')) {
            $writer.WriteLine('250-test.local')
            $writer.WriteLine('250 OK')
        }
        elseif ($upper.StartsWith('MAIL FROM')) { $writer.WriteLine('250 OK') }
        elseif ($upper.StartsWith('RCPT TO')) { $writer.WriteLine('250 OK') }
        elseif ($upper -eq 'DATA') {
            $writer.WriteLine('354 End data with <CR><LF>.<CR><LF>')
            $inData = $true
        }
        elseif ($upper -eq 'QUIT') {
            $writer.WriteLine('221 Bye')
            break
        }
        elseif ($upper -eq 'RSET') { $writer.WriteLine('250 OK') }
        else { $writer.WriteLine('250 OK') }
    }

    $client.Close()
}
