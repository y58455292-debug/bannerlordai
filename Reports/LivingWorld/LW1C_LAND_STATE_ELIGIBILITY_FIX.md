# LW1-C — Land-state eligibility runtime fix

Source checkpoint: `dc1919b5b67248dc0bb78f8d1a43c4d6b271095d`.
Date: 2026-09-30 UTC.
Scope: fix the player-reported empty Home Assignment roster for ship-capable parties on land. This is not full LW1-C runtime closure.

## Defect and smallest fix

At owned Balgard, the player reported three secondary war parties with ships: Alrika, Nal Fairhair and Biliya the Swordsman. The menu reported `No eligible independent clan parties are available.` The same message was reproduced before this fix, with native campaign time visibly paused.

`HomeAssignmentStore.Eligible` rejected `party.NavigationCapability != MobileParty.NavigationType.Default`. That tests capability, not the party's current land/sea state.

The only production-code change replaces that expression with `party.IsCurrentlyAtSea`. A ship-capable party currently on land may now qualify if every other existing predicate passes. A party actually at sea remains excluded. All other eligibility exclusions are unchanged.

The 1.25 policy factor, HomeAssignmentRecords save key/D1 schema, HomeAssignmentLayer, native visit seam and its native land-route restriction, Phase 3 policies, composer, and frozen RC1 are unchanged. No naval Home Assignment implementation was added.

## Regression and build evidence

Tests were staged first against the old production predicate. With the existing .NET 11 RC runtime enabled through process-local roll-forward, the adapter suite failed at `ship-capable party on land remains eligible`. The initial attempt without roll-forward could not execute its net8.0 test executable; no runtime or package was installed to resolve that environment issue.

After the one-line fix:

| Check | Result |
|---|---|
| HomeAssignment policy/persistence suite | 43 PASS |
| Actual Store/VisitPatch/Layer adapter fixtures | 34 PASS |
| Existing Phase 3 Home Responsibility policy | 10 PASS |
| LW1 native authority/UI/save/cache/composer invariants | PASS |
| Phase 3 runtime wiring | PASS |
| Release build | 0 errors; 1 existing MSB3277 System.ValueTuple/Harmony conflict warning |

New coverage includes `NavigationType.All` on land remaining eligible, the same party at sea remaining ineligible, current sea state taking precedence even with Default capability, land return restoring eligibility, native naval home routes still being refused by the unchanged visit seam, and native land home routes remaining available to ship-capable land parties.

Commands: `dotnet run -c Release` for the three projects under `Tests/LivingWorld/HomeAssignment`, `Tests/LivingWorld/HomeAssignmentRuntime`, and `Tests/TerritorialResponsibility`; `python Tests/LivingWorld/test_home_assignment_invariants.py`; `python Tests/TerritorialResponsibility/test_runtime_wiring.py`; `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`. Phase 3 used isolated intermediate/output directories. Process-local `DOTNET_ROLL_FORWARD=Major` and `DOTNET_ROLL_FORWARD_TO_PRERELEASE=1` used the installed runtime. NuGet auditing was disabled for these local validation commands, not in project source.

## Exact new development build

Identity remains `v0.23.0-LW1B-dev`, assembly/file version `0.23.0.0`.
New DLL SHA-256: **B1911B17A04F42C11AB6E23DB3543AFE788FDFA07EFCF34DCF3638D4AB241271**.
Size: **351744 bytes**.
Retained candidate: `D:\BannerlordAIResearch\Builds\LW1C_NavalEligibilityFix_20260930\candidate\ClanAI.dll`.

Built from a fresh source archive at the exact source checkpoint. A hash comparison against the downloaded archive found only the Store predicate and three test files changed. The frozen package, RC1 archive, existing development DLL in Git, save schema, policy, layer and visit patch were unchanged. The original Git development DLL with SHA EEE566... remains historical; it is not this newly built local hotfix. Do not mistake it for the fixed installed candidate.

Local build/test receipts: `D:\BannerlordAIResearch\Builds\LW1C_NavalEligibilityFix_20260930` (`regression_red.txt`, `lw1_policy.txt`, `lw1_runtime.txt`, `phase3_policy.txt`, `lw1_invariants.txt`, `phase3_wiring.txt`, `release_build.txt`, `build_result.json`).

## Safe continuation boundary

The user's current organic Ishme campaign at Balgard was preserved through native Save As into **LW1C LAND FIX 20260930 0436.sav**, a new slot, before native no-save return to the main menu and Exit Game. No pre-existing save was overwritten. Zero Bannerlord/TaleWorlds/Watchdog processes were confirmed after exit.

This source/build commit precedes deployment. Next: preserve the installed EEE566... dev DLL as rollback, deploy the exact B1911B... DLL while Bannerlord is closed, reload that same organic campaign from the new preservation slot, verify the Balgard Home Assignments roster, and hand control to the user. No automated assignment, long peacetime observation, policy retuning, or LW2 work is authorized by this fix checkpoint.
