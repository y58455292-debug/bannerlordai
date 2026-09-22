from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\candidate\SubModule.cs")
s=p.read_text(encoding="utf-8-sig")
sig="        private string TryAdmitPatrolDefenseProviderResultV3("
if sig in s:
    print("v3 method already present")
    raise SystemExit(0)

start=s.index("        private string TryAdmitPatrolDefenseProviderResultV2(")
end=s.index("\n\n        private string TryAdmitPatrolDefenseProviderResult(", start)
m=s[start:end]

v3m=m.replace(
    "TryAdmitPatrolDefenseProviderResultV2",
    "TryAdmitPatrolDefenseProviderResultV3"
).replace(
    "PatrolDefenseProviderResultV2AdmissionData",
    "PatrolDefenseProviderResultV3AdmissionData"
).replace(
    "PatrolDefenseProviderResultV2Admission",
    "PatrolDefenseProviderResultV3Admission"
).replace(
    "PatrolDefenseProviderResultV2AdmissionPath",
    "PatrolDefenseProviderResultV3AdmissionPath"
).replace(
    "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMISSION",
    "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMISSION"
).replace(
    "patrol_defense_provider_result_v2_admitted",
    "patrol_defense_provider_result_v3_admitted"
).replace(
    "patrol_defense_provider_result_v2_rejected:",
    "patrol_defense_provider_result_v3_rejected:"
)

v3m=v3m.replace(
'''                " providerRequestId=" +
                Clean(result == null ? null : result.ProviderRequestId) +
                " status=" +
''',
'''                " providerRequestId=" +
                Clean(result == null ? null : result.ProviderRequestId) +
                " transportRequestId=" +
                Clean(result == null ? null : result.TransportRequestId) +
                " status=" +
''',1)

v3m=v3m.replace(
'''                " providerRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ProviderRequestMatchReason) +
                " accepted=" +
''',
'''                " providerRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ProviderRequestMatchReason) +
                " transportRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.TransportRequestMatchReason) +
                " accepted=" +
''',1)

s=s[:start]+v3m+"\n\n"+s[start:]
p.write_text(s,encoding="utf-8")
print("v3 method inserted")
