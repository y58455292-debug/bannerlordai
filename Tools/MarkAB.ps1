param([string]$Label)

$log = "D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ab_markers.log"

try {
    $status = Invoke-RestMethod "http://127.0.0.1:8420/status" -TimeoutSec 3 |
        ConvertTo-Json -Depth 8 -Compress
}
catch {
    $status = "{}"
}

Add-Content $log (
    (Get-Date -Format o) + "|" + $Label + "|" + $status
)

Write-Host "MARKED: $Label"
