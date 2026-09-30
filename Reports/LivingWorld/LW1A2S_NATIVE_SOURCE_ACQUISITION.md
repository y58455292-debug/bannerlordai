# LW1-A2S — Native Source Acquisition

## Classification

**LW1-A2S: COMPLETE**

This is read-only acquisition from the exact connected Bannerlord installation used by the RC1 development/runtime environment. It does not answer the Persistent Home Assignment design question and does not select an implementation seam. Those conclusions belong to **LW1-A2R**.

| Required field | Result |
|---|---|
| Source GitHub checkpoint | d340bfccf051c87ff90a7c7a344c8a34076f9a9f |
| Machine accessible | **YES** — DESKTOP-JO4B7VH |
| Bannerlord launched | **NO** |
| Bannerlord install root | C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord |
| Bin directory | C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client |
| Supported runtime version | **v1.5.3, engine build 122374**; Steam buildid 25302170 |
| TaleWorlds.CampaignSystem.dll path | C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll |
| TaleWorlds.CampaignSystem.dll size | 5658464 bytes |
| TaleWorlds.CampaignSystem.dll SHA-256 | 5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F |
| SandBox.dll path | C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\bin\Win64_Shipping_Client\SandBox.dll |
| SandBox.dll size | 1344864 bytes |
| SandBox.dll SHA-256 | 16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A |
| Decompiler/tool | ilspycmd 11.0.0.9375 / ICSharpCode.Decompiler 11.0.0.9375; existing local install, no download |
| Relevant native types/methods located | **YES** — selector entry, visit, patrol, defend, army-member, party-control and candidate-list surfaces |
| Targeted source files retained | **7** focused evidence files under Reports/LivingWorld/evidence/native/ |
| Sufficient evidence to resume LW1-A2 | **YES** |
| Gameplay changed | **NO** |
| Save schema changed | **NO** |
| ClanAI built | **NO** |
| ClanAI deployed | **NO** |
| RC1 changed | **NO** |
| Exact next checkpoint | **LW1-A2R — resume supported-native peacetime candidate-generation/control-seam closure using acquired source evidence.** |

## Installation and version provenance

The connected machine is the established runtime/development machine DESKTOP-JO4B7VH. A fresh process enumeration before acquisition found zero Bannerlord/TaleWorlds/Watchdog processes. The game was not started.

Version identity is based on the installed Native module version (v1.5.3), the retained engine-log build header from the same RC1 environment (Build Version 122374), and installed Steam app-manifest buildid 25302170. The managed DLL Windows/assembly resources are generic 1.0.0.x and are recorded separately rather than treated as the Bannerlord runtime version.

## Exact installed native inputs

### TaleWorlds.CampaignSystem.dll
- Absolute path: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll
- Bytes: 5658464
- SHA-256: 5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F
- File version: 1.0.0.0
- Product version: 1.0.0
- Assembly version: 1.0.0.0
- Last-write UTC: 2026-09-16T05:55:57.2113841Z

### SandBox.dll
- Absolute path: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\bin\Win64_Shipping_Client\SandBox.dll
- Bytes: 1344864
- SHA-256: 16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A
- File version: 1.0.0.0
- Product version: 1.0.0
- Assembly version: 1.0.0.0
- Last-write UTC: 2026-09-16T05:55:58.0228196Z

Each required name had exactly one installed match beneath the selected Bannerlord root. Hashes were recomputed from current installed bytes. Matching earlier retained reference hashes is an observed result, not an assumption.

## Tooling

Existing local decompiler:
- C:\Users\csala\.dotnet\tools\ilspycmd.exe
- ilspycmd 11.0.0.9375 / ICSharpCode.Decompiler 11.0.0.9375

The tool requested .NET 10 while .NET 11 RC is installed. No runtime/tool was installed or downloaded; local DOTNET_ROLL_FORWARD=Major was used. Full assembly decompilation was temporary and local only for reference searching. No full proprietary DLL and no full decompiled assembly is committed.

## Native selector source acquired

The focused evidence retains exact supported symbols for:

- PartyThinkParams score storage, reset, equality lookup/update and append;
- AIBehaviorData behavior/target/navigation identity;
- CampaignEventDispatcher.AiHourlyTick;
- AiPartyThinkBehavior hourly entry, main-party/AI-control gates, score-list iteration and native action commit;
- AiVisitSettlementBehavior candidate enumeration, eligibility/navigation/distance gates, food/recovery/recruit/prisoner/trade/owner/home inputs and GoToSettlement insertion;
- AiPatrollingBehavior land/naval target generation and PatrolAroundPoint insertion;
- AiMilitaryBehavior defender target generation, hostile-state/distance logic and DefendSettlement insertion;
- AiArmyMemberBehavior army-member escort control;
- MobilePartyAi disable/decision-stop controls;
- MobileParty and LordPartyComponent main/lord/clan/home identity surfaces.

SandBox.dll was independently hashed and inspected. Its 509-class inventory contains mission-side patrol objects and behavior classes but no CampaignBehaviors.AiBehaviors namespace match. The world-map PartyThinkParams producers required for this closure resolve in the exact CampaignSystem assembly. This is a source-location observation only.

## Evidence sufficiency

The retained source bodies are sufficient for the next reviewer to answer the six LW1-A2 questions about non-main player-clan selector participation, near/far GoToSettlement generation, peacetime PatrolAroundPoint generation, producer/final-list survival, and any earlier native visit filter/score seam. This checkpoint deliberately does not answer those questions.

## Targeted retained evidence

- Reports/LivingWorld/evidence/native/native_manifest_20260929.txt
- Reports/LivingWorld/evidence/native/native_party_ai_entry_20260929.txt
- Reports/LivingWorld/evidence/native/native_visit_settlement_20260929.txt
- Reports/LivingWorld/evidence/native/native_patrol_generation_20260929.txt
- Reports/LivingWorld/evidence/native/native_defend_generation_20260929.txt
- Reports/LivingWorld/evidence/native/native_player_clan_party_control_20260929.txt
- Reports/LivingWorld/evidence/native/native_sandbox_selector_inventory_20260929.txt

## RC1 preservation

Frozen RC1 source checkpoint remains 9ec113e738af35254e113fbde7c05babbf3c405d. Tested ClanAI DLL remains A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5. No Bannerlord launch, save/load, gameplay edit, build, deployment, policy tuning, package edit, RC archive edit, or save-schema edit occurred.

**Exact next checkpoint: LW1-A2R — resume supported-native peacetime candidate-generation/control-seam closure using acquired source evidence.**

Stop here. Do not begin LW1-A2R or LW1-B in this checkpoint.