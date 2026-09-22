$ErrorActionPreference = "Stop"
$taskName = "BannerlordAI Continuity Watchdog"
$before = (Get-Item "D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\Continuity\latest_runtime.json").LastWriteTimeUtc
Start-ScheduledTask -TaskName $taskName
Start-Sleep -Seconds 3
$after = (Get-Item "D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\Continuity\latest_runtime.json").LastWriteTimeUtc
$info = Get-ScheduledTaskInfo -TaskName $taskName
[pscustomobject]@{
  BeforeUtc = $before.ToString("O")
  AfterUtc = $after.ToString("O")
  Refreshed = ($after -gt $before)
  LastRunTime = $info.LastRunTime.ToString("O")
  LastTaskResult = $info.LastTaskResult
  NextRunTime = $info.NextRunTime.ToString("O")
} | ConvertTo-Json
