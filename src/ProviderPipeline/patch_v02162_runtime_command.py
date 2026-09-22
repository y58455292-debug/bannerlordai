from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051")
p=root/"candidate"/"SubModule.cs"
t=p.read_text(encoding="utf-8-sig")

# Add v4 paths after existing v3/binding paths.
path_anchor='''        private const string PatrolDefenseProviderResultV3AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v3_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultBindingPath =
            Root + @"\\patrol_defense_provider_transport_result_bindings.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderResultV4AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v4_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultV4BindingPath =
            Root + @"\\patrol_defense_provider_transport_result_v4_bindings.jsonl";
'''
if "PatrolDefenseProviderResultV4AdmissionPath" not in t:
    if path_anchor not in t:
        raise SystemExit("v4 path anchor missing")
    t=t.replace(path_anchor,path_repl,1)

# Add command before v3 transport result command.
cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT "))
'''
cmd='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_result_v4_invalid"
                        : TryAdmitPatrolDefenseProviderTransportResultV4(
                            parts[0],
                            parts[1]);
                }
'''
if "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT " not in t:
    if cmd_anchor not in t:
        raise SystemExit("v4 command anchor missing")
    t=t.replace(cmd_anchor,cmd+cmd_anchor,1)

# Clone v3 method mechanically.
sig='''        private string TryAdmitPatrolDefenseProviderTransportResult(
'''
start=t.find(sig)
if start<0:
    raise SystemExit("v3 transport method missing")
# next method is v3 direct admission
end=t.find('''        private string TryAdmitPatrolDefenseProviderResultV3(
''', start)
if end<0:
    raise SystemExit("v3 transport method end missing")
method=t[start:end]
v4=method
v4=v4.replace(
    "TryAdmitPatrolDefenseProviderTransportResult(",
    "TryAdmitPatrolDefenseProviderTransportResultV4(",
    1)
v4=v4.replace(
    "PatrolDefenseProviderTransportReceiptResultBindingData",
    "PatrolDefenseProviderTransportReceiptResultV4BindingData")
v4=v4.replace(
    "PatrolDefenseProviderTransportReceiptResultBinding",
    "PatrolDefenseProviderTransportReceiptResultV4Binding")
v4=v4.replace(
    "PatrolDefenseProviderResultV3AdmissionData",
    "PatrolDefenseProviderResultV4AdmissionData")
v4=v4.replace(
    "PatrolDefenseProviderResultV3Admission",
    "PatrolDefenseProviderResultV4Admission")
v4=v4.replace(
    "PatrolDefenseProviderTransportResultBindingPath",
    "PatrolDefenseProviderTransportResultV4BindingPath")
v4=v4.replace(
    "PatrolDefenseProviderResultV3AdmissionPath",
    "PatrolDefenseProviderResultV4AdmissionPath")
v4=v4.replace(
    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMISSION",
    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMISSION")
v4=v4.replace(
    "patrol_defense_provider_transport_result_admitted",
    "patrol_defense_provider_transport_result_v4_admitted")
v4=v4.replace(
    "patrol_defense_provider_transport_result_rejected:",
    "patrol_defense_provider_transport_result_v4_rejected:")
# Add execution policy info in log after transport receipt reason.
needle='''                " transportReceiptMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportReceiptMatchReason) +
                " accepted=" +
'''
repl='''                " transportReceiptMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportReceiptMatchReason) +
                " executionPolicyId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ExecutionPolicyId) +
                " executionPolicyMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ExecutionPolicyMatchReason) +
                " accepted=" +
'''
if needle not in v4:
    raise SystemExit("v4 log insertion anchor missing")
v4=v4.replace(needle,repl,1)

if "TryAdmitPatrolDefenseProviderTransportResultV4(" not in t:
    t=t[:start]+v4+t[start:]

p.write_text(t,encoding="utf-8")
print("v02162 SubModule v4 command + runtime path wired")
