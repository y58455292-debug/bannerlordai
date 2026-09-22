$ErrorActionPreference = "Stop"
$taskName = "BannerlordAI Continuity Watchdog"
$wrapper = "D:\BannerlordAIResearch\Tools\Handoff\continuity_watchdog.cmd"
$action = New-ScheduledTaskAction -Execute "cmd.exe" -Argument ('/c "' + $wrapper + '"')
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 5) -RepetitionDuration (New-TimeSpan -Days 3650)
$settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -StartWhenAvailable
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Force | Out-Null
Get-ScheduledTask -TaskName $taskName | Select-Object TaskName,State | ConvertTo-Json
