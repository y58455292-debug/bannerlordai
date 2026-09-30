param([string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference = 'Stop'
$bin = Join-Path $GameRoot 'bin\Win64_Shipping_Client'
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $bin 'TaleWorlds.CampaignSystem.dll'))
$type = $assembly.GetType('TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$fill = $type.GetMethod('FillSettlementsToVisitWithDistancesAsDays', $flags)
$suitable = $type.GetMethod('IsSettlementSuitableForVisitingCondition', $flags)
$nav = $type.GetMethod('GetBestNavigationDataForVisitingSettlement', $flags)
$row = $type.GetNestedType('SettlementNavigationData', [Reflection.BindingFlags]::NonPublic)
if (!$fill -or !$suitable -or !$nav -or !$row) { throw 'Missing supported native method' }
if ($fill.GetParameters().Count -ne 2 -or $suitable.ReturnType -ne [bool] -or $nav.GetParameters().Count -ne 6) {throw 'Wrong native helper signature'}
$ctor = $row.GetConstructors([Reflection.BindingFlags]'Instance,Public,NonPublic')
if ($ctor.Count -ne 1 -or $ctor[0].GetParameters().Count -ne 6 -or !$row.GetField('Settlement')) {throw 'Wrong navigation row signature'}
$fill.ToString()
$suitable.ToString()
$nav.ToString()
$ctor[0].ToString()
'PASS exact supported native visit metadata (offline CLR only; no Bannerlord launch)'
