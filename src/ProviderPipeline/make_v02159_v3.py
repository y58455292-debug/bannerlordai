from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658")

v2p=root/"candidate"/"PatrolDefenseProviderResultV2Admission.cs"
v3p=root/"candidate"/"PatrolDefenseProviderResultV3Admission.cs"
v3=v2p.read_text(encoding="utf-8-sig")

v3=v3.replace(
    "PatrolDefenseProviderResultV2AdmissionData",
    "PatrolDefenseProviderResultV3AdmissionData"
).replace(
    "PatrolDefenseProviderResultV2Admission",
    "PatrolDefenseProviderResultV3Admission"
).replace(
    "BannerlordAI.PatrolDefenseProviderResult.v2",
    "BannerlordAI.PatrolDefenseProviderResult.v3"
).replace(
    "BannerlordAI.PatrolDefenseProviderResultV2Admission.v1",
    "BannerlordAI.PatrolDefenseProviderResultV3Admission.v1"
)

v3=v3.replace(
'''        internal string ProviderRequestId;
        internal string Status;
''',
'''        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string Status;
''',1)
v3=v3.replace(
'''        internal string ProviderRequestMatchReason;
        internal bool ProviderResultAccepted;
''',
'''        internal string ProviderRequestMatchReason;
        internal string TransportRequestMatchReason;
        internal bool ProviderResultAccepted;
''',1)
v3=v3.replace(
'''                        "providerRequestId",
                        "status",
''',
'''                        "providerRequestId",
                        "transportRequestId",
                        "status",
''',1)
v3=v3.replace(
'''            fields.TryGetValue(
                "status",
                out data.Status);
''',
'''            fields.TryGetValue(
                "transportRequestId",
                out data.TransportRequestId);
            if (!Present(data.TransportRequestId))
                Reject(data, "TRANSPORT_REQUEST_ID_MISSING");

            fields.TryGetValue(
                "status",
                out data.Status);
''',1)

old='''            if (requestMatch == null ||
                !requestMatch.Matched)
            {
                Reject(
                    data,
                    data.ProviderRequestMatchReason);
                return data;
            }

            if (gate == null)
'''
new='''            if (requestMatch == null ||
                !requestMatch.Matched)
            {
                Reject(
                    data,
                    data.ProviderRequestMatchReason);
                return data;
            }

            PatrolDefenseProviderTransportMatchResult
                transportMatch =
                    dispatchQueue.MatchRegisteredTransportRequest(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.AttemptId,
                        data.ProviderRequestId,
                        data.TransportRequestId);

            data.TransportRequestMatchReason =
                transportMatch == null
                    ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                    : transportMatch.Reason;

            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportRequestMatchReason);
                return data;
            }

            if (gate == null)
'''
if old not in v3:
    raise SystemExit("v3 transport insertion anchor missing")
v3=v3.replace(old,new,1)

v3=v3.replace(
'''            sb.Append(",\"providerRequestId\":");
            sb.Append(Json(data.ProviderRequestId));
            sb.Append(",\"status\":");
''',
'''            sb.Append(",\"providerRequestId\":");
            sb.Append(Json(data.ProviderRequestId));
            sb.Append(",\"transportRequestId\":");
            sb.Append(Json(data.TransportRequestId));
            sb.Append(",\"status\":");
''',1)
v3=v3.replace(
'''            sb.Append(",\"providerRequestMatchReason\":");
            sb.Append(Json(data.ProviderRequestMatchReason));
            sb.Append(",\"providerResultAccepted\":");
''',
'''            sb.Append(",\"providerRequestMatchReason\":");
            sb.Append(Json(data.ProviderRequestMatchReason));
            sb.Append(",\"transportRequestMatchReason\":");
            sb.Append(Json(data.TransportRequestMatchReason));
            sb.Append(",\"providerResultAccepted\":");
''',1)

v3p.write_text(v3,encoding="utf-8")

sub=root/"candidate"/"SubModule.cs"
s=sub.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderResultV2AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v2_admissions.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderResultV3AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v3_admissions.jsonl";
'''
if "PatrolDefenseProviderResultV3AdmissionPath" not in s:
    if path_anchor not in s:
        raise SystemExit("v3 path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT "))
'''
cmd='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_result_v3_invalid"
                        : TryAdmitPatrolDefenseProviderResultV3(raw);
                }
'''
if "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT " not in s:
    if cmd_anchor not in s:
        raise SystemExit("v3 command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

start=s.index("        private string TryAdmitPatrolDefenseProviderResultV2(")
end=s.index("\n\n        private string TryAdmitPatrolDefenseProviderResult(",start)
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

if "TryAdmitPatrolDefenseProviderResultV3(" not in s:
    s=s[:start]+v3m+"\n\n"+s[start:]

sub.write_text(s,encoding="utf-8")

print("v02159 v3 parser and runtime command generated")
