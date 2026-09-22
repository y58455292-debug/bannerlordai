$ErrorActionPreference = "Continue"

$InspectorBase = "http://127.0.0.1:8420"
$TelemetryRoot = "D:\BannerlordAIResearch\Telemetry\BannerlordInspector"
$Port = 8421

function Get-InspectorData([string]$route)
{
    try {
        return Invoke-RestMethod `
            -Uri ($InspectorBase + $route) `
            -Method Get `
            -TimeoutSec 10
    }
    catch {
        return [ordered]@{
            error = $_.Exception.Message
            route = $route
        }
    }
}

function Get-Tail([string]$path, [int]$count)
{
    if (-not (Test-Path $path)) {
        return @()
    }

    return @(Get-Content $path -Tail $count)
}

function Get-LatestSession
{
    $root = Join-Path $TelemetryRoot "ai\behavior-delta"

    if (-not (Test-Path $root)) {
        return $null
    }

    return Get-ChildItem $root -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Make-LivePayload
{
    $session = Get-LatestSession

    $ai = [ordered]@{
        session = $null
        status = @()
        behaviorChanges = @()
        decisionCandidates = @()
        radiusDeltas = @()
    }

    if ($session -ne $null) {
        $ai.session = $session.Name

        $ai.status =
            Get-Tail `
                (Join-Path $session.FullName "status.txt") `
                100

        $ai.behaviorChanges =
            Get-Tail `
                (Join-Path $session.FullName "behavior_changes.csv") `
                100

        $ai.decisionCandidates =
            Get-Tail `
                (Join-Path $session.FullName "decision_candidates.csv") `
                200

        $ai.radiusDeltas =
            Get-Tail `
                (Join-Path $session.FullName "radius_deltas.csv") `
                100
    }

    return [ordered]@{
        bridge = "ClanAI Live World Bridge"
        generated = (Get-Date).ToString("o")

        campaign = Get-InspectorData "/status"

        military = Get-InspectorData "/military"

        lordParties = Get-InspectorData "/lordparties"

        ai = $ai

        inspectorTrace =
            Get-Tail `
                (Join-Path $TelemetryRoot "logs\inspector.log") `
                300
    }
}


function Get-InspectorDataFast([string]$route)
{
    try {
        return Invoke-RestMethod `
            -Uri ($InspectorBase + $route) `
            -Method Get `
            -TimeoutSec 2
    }
    catch {
        return [ordered]@{
            error = $_.Exception.Message
            route = $route
        }
    }
}

function Make-WarPayload
{
    $session = Get-LatestSession

    $ai = [ordered]@{
        session = $null
        status = @()
        behaviorChanges = @()
        decisionCandidates = @()
        radiusDeltas = @()
    }

    if ($session -ne $null)
    {
        $ai.session = $session.Name
        $ai.status = Get-Tail (Join-Path $session.FullName "status.txt") 40
        $ai.behaviorChanges = Get-Tail (Join-Path $session.FullName "behavior_changes.csv") 40
        $ai.decisionCandidates = Get-Tail (Join-Path $session.FullName "decision_candidates.csv") 80
        $ai.radiusDeltas = Get-Tail (Join-Path $session.FullName "radius_deltas.csv") 40
    }

    $tracePath = Join-Path $TelemetryRoot "logs\inspector.log"

    $importantTrace = @(
        Get-Tail $tracePath 160 |
        Where-Object {
            $_ -match "AI TRACE|SELECTED ACTION|STRATEGIC COMMIT|AI STATE TRANSITION|candidate score=|party=|before=|after="
        }
    )

    return [ordered]@{
        bridge = "ClanAI War Feed"
        generated = (Get-Date).ToString("o")
        campaign = Get-InspectorDataFast "/status"
        ai = $ai
        trace = $importantTrace
    }
}

function Make-PulsePayload
{
    $session = Get-LatestSession

    if ($null -eq $session)
    {
        return [ordered]@{
            bridge = "ClanAI War Pulse"
            generated = (Get-Date).ToString("o")
            session = $null
            behaviorChanges = @()
            decisionCandidates = @()
            radiusDeltas = @()
        }
    }

    return [ordered]@{
        bridge = "ClanAI War Pulse"
        generated = (Get-Date).ToString("o")
        session = $session.Name

        behaviorChanges =
            Get-Tail `
                (Join-Path $session.FullName "behavior_changes.csv") `
                20

        decisionCandidates =
            Get-Tail `
                (Join-Path $session.FullName "decision_candidates.csv") `
                30

        radiusDeltas =
            Get-Tail `
                (Join-Path $session.FullName "radius_deltas.csv") `
                20
    }
}
function Send-Response(
    $stream,
    [int]$status,
    [string]$statusText,
    [object]$data)
{
    $json =
        $data |
        ConvertTo-Json -Depth 30 -Compress

    $body =
        [System.Text.Encoding]::UTF8.GetBytes($json)

    $headerText =
        "HTTP/1.1 $status $statusText`r`n" +
        "Content-Type: application/json; charset=utf-8`r`n" +
        "Content-Length: $($body.Length)`r`n" +
        "Access-Control-Allow-Origin: *`r`n" +
        "Cache-Control: no-store`r`n" +
        "Connection: close`r`n" +
        "`r`n"

    $header =
        [System.Text.Encoding]::ASCII.GetBytes($headerText)

    $stream.Write($header, 0, $header.Length)
    $stream.Write($body, 0, $body.Length)
    $stream.Flush()
}

$listener =
    New-Object System.Net.Sockets.TcpListener(
        [System.Net.IPAddress]::Loopback,
        $Port
    )

$listener.Start()

Write-Host ""
Write-Host "ClanAI Live World Bridge ACTIVE"
Write-Host "http://127.0.0.1:$Port/health"
Write-Host "http://127.0.0.1:$Port/live"
Write-Host ""
Write-Host "Only safe read-only telemetry is exposed."
Write-Host ""

while ($true)
{
    $client = $listener.AcceptTcpClient()

    try
    {
        $stream = $client.GetStream()

        $reader =
            New-Object System.IO.StreamReader(
                $stream,
                [System.Text.Encoding]::ASCII,
                $false,
                4096,
                $true
            )

        $request = $reader.ReadLine()

        while ($true)
        {
            $line = $reader.ReadLine()

            if ($null -eq $line -or $line -eq "") {
                break
            }
        }

        if ([string]::IsNullOrWhiteSpace($request))
        {
            Send-Response `
                $stream `
                400 `
                "Bad Request" `
                @{ error = "empty request" }

            continue
        }

        $parts = $request.Split(" ")

        if ($parts.Count -lt 2 -or $parts[0] -ne "GET")
        {
            Send-Response `
                $stream `
                405 `
                "Method Not Allowed" `
                @{ error = "GET only" }

            continue
        }

        $uri =
            New-Object System.Uri(
                "http://127.0.0.1" + $parts[1]
            )

        $path = $uri.AbsolutePath.ToLowerInvariant()

        switch ($path)
        {
            "/health"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    ([ordered]@{
                        ok = $true
                        bridge = "ClanAI Live World Bridge"
                        inspector = Get-InspectorData "/health"
                        time = (Get-Date).ToString("o")
                    })
            }

            "/live"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    (Make-LivePayload)
            }

            "/pulse"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    (Make-PulsePayload)
            }

            "/war"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    (Make-WarPayload)
            }

            "/status"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    (Get-InspectorData "/status")
            }

            "/military"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    (Get-InspectorData "/military")
            }

            "/lordparties"
            {
                Send-Response `
                    $stream `
                    200 `
                    "OK" `
                    (Get-InspectorData "/lordparties")
            }

            default
            {
                Send-Response `
                    $stream `
                    404 `
                    "Not Found" `
                    @{
                        error = "route not exposed"
                        allowed = @(
                            "/health",
                            "/live",
                            "/status",
                            "/military",
                            "/lordparties"
                        )
                    }
            }
        }
    }
    catch
    {
        try {
            Send-Response `
                $stream `
                500 `
                "Internal Server Error" `
                @{ error = $_.Exception.Message }
        }
        catch {}
    }
    finally
    {
        $client.Close()
    }
}


