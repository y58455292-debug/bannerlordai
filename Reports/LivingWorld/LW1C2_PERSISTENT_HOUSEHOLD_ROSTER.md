# LW1-C2 — Persistent Household Responsibility Roster

**Checkpoint state: IMPLEMENTED; focused tests PASS; game-target Release build PASS.** Runtime status: **NOT YET TESTED**. No Bannerlord launch or deployment occurred.

## Observed problem and behavior

The old assignment picker derived visible household rows from parties currently eligible to perform home behavior. That made an assigned hero disappear while at sea, serving in an army, imprisoned, without an active party, or in another temporarily unavailable state. The assignment record could remain saved, but the player could not see the person or understand why the responsibility had paused.

The roster is now a bounded UI-demand helper over `Clan.PlayerClan.Heroes`, `Clan.PlayerClan.Companions`, and valid saved assignment identities. It sorts by rendered name using ordinal comparison, then by `Hero.StringId`. It excludes the main hero, children, templates, dead/invalid identities, and people outside the player clan. It never uses `Eligible` to decide whether someone belongs in the roster and does not scan `Hero.All`, all parties, or settlements globally.

Rows show `Name — Home: ...` plus a concise location and activity/status. Status is separate from assignment validity: Active, NoActiveParty, AtSea, InArmy, Prisoner, Disbanding, Attached, AiStopped, InBattleOrSiege, and OtherTemporaryUnavailable. Temporary states explain that responsibility is suspended. An existing home stays visible while its living hero remains a valid player-clan member.

Status and location use direct native references on menu demand. Location preference is prisoner, party current settlement, army, sea, target/short-term target, hero current settlement when no active party, then campaign/no-party fallback. Assignment/change is offered only to an eligible active party leader. Clear addresses the stable `Hero.StringId` and remains available for unavailable assigned members. No assignment is created for someone who never had a party.

## Identity, persistence, and eligibility

`Hero.StringId -> Settlement.StringId` remains authoritative under `ClanAI_HomeAssignment_v1` and the existing D1 record format. Load/lookup/ownership invalidation now preserves a valid assignment across temporary party states; dead/invalid/out-of-clan heroes, deleted settlements, and foreign/lost holdings remain legitimate invalidations. The strict runtime `Eligible` exclusions remain in force, including at-sea, army, prisoner, no-party, disabled/stopped AI, disbanding/waiting, attached/special states, map event/siege, invalid leader, and other existing party restrictions. Ship-capable parties on land remain eligible; parties currently at sea do not.

No AI hot path, movement, factor, save key/schema, native visit seam, or Phase 3 war responsibility policy was changed. The existing home factor remains 1.25. The roster is calculated only when the player opens the existing owned-town/castle Manage Home Assignments menu.

## Changed files

- `src/ClanAI/src/ClanAI/HomeAssignmentRoster.cs` — bounded roster, status, location, and ordering.
- `src/ClanAI/src/ClanAI/HomeAssignmentStore.cs` — identity-based temporary-state persistence/invalidations while preserving strict `Eligible`.
- `src/ClanAI/src/ClanAI/HomeAssignmentCampaignBehavior.cs` — persistent roster rows and safe assign/change/clear actions.
- `src/ClanAI/src/ClanAI/ReleaseIdentity.cs`, `src/ClanAI/src/ClanAI/ClanAI.csproj` - informational dev identity `v0.23.0-LW1C2-dev`; assembly/file versions remain 0.23.0.0.
- `Tests/LivingWorld/HomeAssignmentRuntime/NativeFixtures.cs`, `Tests/LivingWorld/HomeAssignmentRuntime/Program.cs`, `Tests/LivingWorld/HomeAssignmentRuntime/HomeAssignmentRuntimeTests.csproj` - deterministic roster and native adapter coverage.
- `DevBuilds/ClanAI-v0.23.0-LW1C2-dev/ClanAI.dll` - new project-target Release candidate; SHA-256 `B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`.
- `README.md`, `ROADMAP.md`, `Reports/AgentStatus/CODEX_STATUS.md` — checkpoint and validation state.

## Validation and preservation checks

Authoritative source was materialized from GitHub `main` at `5b9e47b2813373fe0b03bae58d41c88a8dfcff83` (tree `390d2615306e38458817254737305a414929dec2`). Materialized text files were checked against the tree's Git blob identifiers before use. The denied/dirty legacy checkout `D:\BannerlordAIResearch` was not modified or imported.

- HomeAssignmentRuntime focused deterministic suite: **68 PASS**.
- HomeAssignment policy/persistence D1 suite: **43 PASS**.
- Existing Phase3 Territorial Responsibility suite: **10 PASS**.
- ClanAI compile against installed native Bannerlord references: **0 warnings, 0 errors** using SDK 11 and command-line `TargetFramework=net11.0` solely as an API/compile check.
- `NETStandard.Library` 2.0.3 existed in the signed-in user's NuGet cache. Its original metadata identified `https://api.nuget.org/v3/index.json` as source; NuGet accepted its `.nupkg.sha512` sidecar when restoring through a read-only local package source into the task-local cache. No network/security setting changed.
- Required actual `netstandard2.0` Release build: **PASS, 0 errors, 1 inherited MSB3277 `System.ValueTuple` reference-conflict warning**.
- New dev candidate: `DevBuilds/ClanAI-v0.23.0-LW1C2-dev/ClanAI.dll`, SHA-256 **`B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`**. The existing LW1-B DLL was not overwritten.
- Public NuGet restore still fails with `NU1801`/`NU1101`; that is no longer a blocker because the verified cached package supplied the actual target framework.
- RC1 archive/package bytes: untouched; no Release payload paths were materialized for writing.
- Runtime status: **NOT YET TESTED**. No game launch, deployment, or save activity.

### Acceptance coverage audit

1. Active eligible hero appears — roster membership and Active status assertions.
2–5. Sea, army, prisoner, and no-party heroes remain visible — one combined roster assertion and an individual status assertion for each.
6–10. Existing homes remain visible and are retained for sea, army, prisoner, and no-party states — `CurrentHome` retention assertion for each; unavailable clear is separate.
11–13. Dead/out-of-clan heroes are excluded and invalidated; foreign/lost holding is invalidated — focused invalidation assertions.
14–15. Main hero, child, and template exclusions — dedicated roster assertions.
16. Ordinal name then `StringId` ordering — deterministic tie/order test.
17. Roster inclusion is independent of behavior eligibility — sea/army/prisoner/no-party are asserted ineligible, then included.
18–19. Eligible assignment/change and selected unavailable-row clear by `Hero.StringId`; no dormant assignment — focused action assertions.
20–21. Ship-capable land party eligible; party currently at sea ineligible — dedicated eligibility assertions.
22–23. No `Hero.All` or global roster scan; roster is called by menu demand and absent from `HomeAssignmentLayer` — runtime source assertions and baseline hash comparison.
24–25. Existing home-assignment policy/D1 tests **43 PASS**; Phase 3 Home Responsibility tests **10 PASS**.

The strict existing `Eligible` gates were compared with exact-main source: main-party, party type/active state, player-clan/faction, army, disbanding/waiting, disabled/stopped AI, map event/siege, attached, sea, valid adult leader/prisoner/activity/ownership remain. Only the existing waiting-for-disband query was factored into an equivalent helper. `HomeAssignmentRecords.cs`, `HomeAssignmentPolicy.cs` (including factor 1.25), `HomeAssignmentVisitPatch.cs`, and Phase 3 Home Responsibility files remain byte-identical to baseline.

All offline build, test, and preservation gates are complete. RC1 archive/package bytes remain untouched. Runtime and save/reload closure belong to the next checkpoint.

## Exact next checkpoint

**LW1-C3 - deploy persistent-roster dev candidate, verify stable names/statuses in player's organic campaign, finish bounded runtime/save-reload closure.** No launch or deployment occurred as part of C2.
