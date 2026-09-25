# Codex Status

## Current task
Finish the already-started Phase 2D-L1 save/reload proof, then return to Phase 3 territorial responsibility.

## Last completed checkpoint
Phase 2C passed at Bannerlord's supported native boundary. Phase 2D-L1 is implemented, build-proven, deployed with rollback, and runtime-proven for initial seven-kingdom reconciliation.

## Current state
PHASE 2D-L1 SAVE MATERIALIZATION PROVEN; RELOAD IN PROGRESS; PHASE 3 OFFLINE SOURCE AUDIT COMPLETE.

The uniquely named test save `ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130` was created through TestRunner and verified with `SAVE_TEST_END ... exists=True`. The protected demo-gate fixture was not overwritten.

The configured-module launch path is already proven. After `EXIT_NOSAVE`, Bannerlord/TestRunner/watchdog absence is expected process shutdown, not a new blocker. Relaunch through the recorded configured-module path; do not rediscover alternate launch methods.

The TestRunner interface is also established through Desktop Commander:
- command bus: `D:\BannerlordAIResearch\Automation\TestRunner\command.txt`
- status: `D:\BannerlordAIResearch\Automation\TestRunner\status.txt`

When status reports `escape_menu_open`, write `CLOSE_ESCAPE_MENU` and verify it clears. `menu_active:town_wait_menus` is separate and is not an escape-overlay condition.

## Current reload
A fresh configured-module launch reached `campaignReady=True` on the protected fixture and accepted `LOAD_SAVE ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130`.

Latest status names that test save but still reports `screen_active:GameLoadingScreen`. Do **not** issue another load while this state is current. Loading has not yet completed, so continuity restore, seven-record restoration, duplicate-notice absence, and post-load political state remain unverified.

## Exact next action
Poll/read the existing TestRunner status/logs until the loading screen clears and the loaded campaign is ready. Do not resend `LOAD_SAVE` unless there is affirmative evidence the current load failed.

Once ready, verify:
1. continuity restore/reconcile succeeds;
2. seven continuity records are present;
3. no duplicate succession/destruction notice is emitted from load reconciliation;
4. no native political mutation occurred.

Then commit the result/evidence and update README/roadmap status as needed. Only after that checkpoint continue the already-defined Phase 3 deterministic Home Responsibility test seam.

## Resource discipline
Do not re-debug the configured launch path, TestRunner command bus, escape-menu command, or save materialization unless new evidence shows one of them failed. These paths are already established. Preserve Codex/tool usage for missing implementation or evidence.

## Local/deployment note
The L1 DLL was deployed only with Bannerlord closed and has a verified rollback at `D:\BannerlordAIResearch\Builds\Rollback_Phase2D_L1_20260924_ClanAI`. The protected demo-gate save remains the baseline fixture; use only uniquely named test saves for validation.
