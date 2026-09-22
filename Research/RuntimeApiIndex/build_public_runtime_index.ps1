$ErrorActionPreference='Stop'
$bin='C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client'
$outRoot='D:\BannerlordAIResearch\Research\RuntimeApiIndex\2026-09-18'
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

$refs=@('TaleWorlds.Library.dll','TaleWorlds.Core.dll')
foreach($r in $refs){
  $p=Join-Path $bin $r
  if(Test-Path $p){[void][Reflection.Assembly]::LoadFrom($p)}
}
$a=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'TaleWorlds.CampaignSystem.dll'))

$nsPrefixes=@(
 'TaleWorlds.CampaignSystem.Party',
 'TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors',
 'TaleWorlds.CampaignSystem.Actions',
 'TaleWorlds.CampaignSystem.Map',
 'TaleWorlds.CampaignSystem.MapEvents',
 'TaleWorlds.CampaignSystem.ComponentInterfaces',
 'TaleWorlds.CampaignSystem.GameComponents',
 'TaleWorlds.CampaignSystem.Settlements'
)

$keywords='AI|Behavior|Party|Target|Engage|Patrol|Flee|Escort|Raid|Siege|Defend|Army|Nearby|Locator|MapEvent|Encounter|Battle|Move|Initiative|Threat|Strength|Settlement|War'

$types=$a.GetExportedTypes() | Where-Object {
  $fn=$_.FullName
  ($nsPrefixes | Where-Object {$fn.StartsWith($_,[StringComparison]::Ordinal)}).Count -gt 0
}

$rows=New-Object System.Collections.Generic.List[object]
foreach($t in $types){
  $flags=[Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly'

  foreach($m in $t.GetMethods($flags)){
    if($m.IsSpecialName){continue}
    $params=($m.GetParameters() | ForEach-Object {
      $_.ParameterType.FullName+' '+$_.Name
    }) -join ', '
    $sig=$m.ReturnType.FullName+' '+$m.Name+'('+$params+')'
    if(($t.FullName+' '+$sig) -match $keywords){
      $rows.Add([pscustomobject]@{
        Kind='Method';Type=$t.FullName;Member=$m.Name;Signature=$sig;
        Static=$m.IsStatic;Namespace=$t.Namespace
      })
    }
  }

  foreach($p in $t.GetProperties($flags)){
    $sig=$p.PropertyType.FullName+' '+$p.Name
    if(($t.FullName+' '+$sig) -match $keywords){
      $rows.Add([pscustomobject]@{
        Kind='Property';Type=$t.FullName;Member=$p.Name;Signature=$sig;
        Static=$false;Namespace=$t.Namespace
      })
    }
  }

  if($t.IsEnum -and $t.FullName -match $keywords){
    foreach($name in [Enum]::GetNames($t)){
      $rows.Add([pscustomobject]@{
        Kind='EnumValue';Type=$t.FullName;Member=$name;
        Signature=$name+'='+([int][Enum]::Parse($t,$name));
        Static=$true;Namespace=$t.Namespace
      })
    }
  }
}

# CampaignEvents separately: public static event properties are central.
$ce=$a.GetType('TaleWorlds.CampaignSystem.CampaignEvents')
if($ce){
  $flags=[Reflection.BindingFlags]'Public,Static'
  foreach($p in $ce.GetProperties($flags)){
    if(($p.Name+' '+$p.PropertyType.FullName) -match $keywords){
      $rows.Add([pscustomobject]@{
        Kind='CampaignEvent';Type=$ce.FullName;Member=$p.Name;
        Signature=$p.PropertyType.FullName;Static=$true;Namespace=$ce.Namespace
      })
    }
  }
}

$csv=Join-Path $outRoot 'public_ai_runtime_surface.csv'
$rows | Sort-Object Namespace,Type,Kind,Member | Export-Csv $csv -NoTypeInformation -Encoding UTF8

# Compact category counts
$counts=$rows | Group-Object Kind | Sort-Object Count -Descending |
  ForEach-Object {[pscustomobject]@{Kind=$_.Name;Count=$_.Count}}
$counts | Export-Csv (Join-Path $outRoot 'surface_counts.csv') -NoTypeInformation -Encoding UTF8

# High-value candidates only
$high=$rows | Where-Object {
  ($_.Type -match 'MobilePartyAi|SetPartyAiAction|AiBehavior|MobileParty$|CampaignEvents|MapEvent|LocatorGrid|MobilePartyAIModel|DefaultMobilePartyAIModel') -and
  ($_.Signature -match 'Engage|Patrol|Flee|Escort|Nearby|Locator|Target|Behavior|Battle|MapEvent|Move|Initiative|Strength|Threat|Army|Settlement')
}
$high | Sort-Object Type,Kind,Member | Export-Csv (Join-Path $outRoot 'breakthrough_candidates.csv') -NoTypeInformation -Encoding UTF8

# Human-readable summary
$txt=New-Object System.Collections.Generic.List[string]
$txt.Add('BANNERLORD PUBLIC RUNTIME API INDEX — AI RESEARCH')
$txt.Add('Generated: '+(Get-Date -Format o))
$txt.Add('Assembly: '+$a.FullName)
$txt.Add('Scope: exported/public metadata only; no method bodies/decompilation.')
$txt.Add('')
$txt.Add('COUNTS')
foreach($c in $counts){$txt.Add('  '+$c.Kind+': '+$c.Count)}
$txt.Add('')
$txt.Add('HIGH-VALUE SURFACES')
$grouped=$high | Group-Object Type | Sort-Object Name
foreach($g in $grouped){
  $txt.Add('')
  $txt.Add($g.Name)
  foreach($x in ($g.Group | Sort-Object Kind,Member)){
    $txt.Add('  ['+$x.Kind+'] '+$x.Signature)
  }
}
[IO.File]::WriteAllLines((Join-Path $outRoot 'breakthrough_candidates.txt'),$txt)

'ROWS='+$rows.Count
'HIGH='+$high.Count
'OUT='+$outRoot
