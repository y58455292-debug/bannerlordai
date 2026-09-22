from pathlib import Path

p = Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\candidate\SubModule.cs")
t = p.read_text(encoding="utf-8-sig")

v4_paths = '''        private const string PatrolDefenseProviderResultV4AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v4_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultV4BindingPath =
            Root + @"\\patrol_defense_provider_transport_result_v4_bindings.jsonl";
'''
v5_paths = v4_paths + '''        private const string PatrolDefenseProviderResultV5AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v5_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultV5BindingPath =
            Root + @"\\patrol_defense_provider_transport_result_v5_bindings.jsonl";
'''
if "PatrolDefenseProviderResultV5AdmissionPath" not in t:
    if v4_paths not in t: raise SystemExit("v4 path anchor missing")
    t = t.replace(v4_paths, v5_paths, 1)

cmd_anchor = '''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT "))
'''
cmd_v5 = '''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_result_v5_invalid"
                        : TryAdmitPatrolDefenseProviderTransportResultV5(
                            parts[0],
                            parts[1]);
                }
'''
if "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT " not in t:
    if cmd_anchor not in t: raise SystemExit("v4 command anchor missing")
    t = t.replace(cmd_anchor, cmd_v5 + cmd_anchor, 1)

v4_start = t.find("        private string TryAdmitPatrolDefenseProviderTransportResultV4(")
next_start = t.find("        private string TryAdmitPatrolDefenseProviderTransportResult(\n", v4_start)
if v4_start < 0 or next_start < 0: raise SystemExit("v4 method bounds missing")
if "private string TryAdmitPatrolDefenseProviderTransportResultV5(" not in t:
    block = t[v4_start:next_start]
    v5 = block.replace("V4", "V5").replace("v4", "v5")
    t = t[:v4_start] + v5 + "\n" + t[v4_start:]

p.write_text(t, encoding="utf-8")
print("v02166 SubModule v5 paths/command/admission wired")
