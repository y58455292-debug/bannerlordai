$ErrorActionPreference = "Stop"
$taskName = "BannerlordAI Continuation Supervisor"
$wrapper = "D:\BannerlordAIResearch\Tools\Autopilot\continuation_supervisor.cmd"
$action = New-ScheduledTaskAction -Execute "cmd.exe" -Argument ('/c "' + $wrapper + '"')
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddSeconds(20) -RepetitionInterval (New-TimeSpan -Minutes 1) -RepetitionDuration (New-TimeSpan -Days 3650)
$settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -StartWhenAvailable
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Force | Out-Null
Get-ScheduledTask -TaskName $taskName | Select-Object TaskName,State | ConvertTo-Json
