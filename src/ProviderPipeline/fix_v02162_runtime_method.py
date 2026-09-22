from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\candidate\SubModule.cs")
t=p.read_text(encoding="utf-8-sig")

private_sig='''        private string TryAdmitPatrolDefenseProviderTransportResultV4(
'''
if private_sig not in t:
    src_sig='''        private string TryAdmitPatrolDefenseProviderTransportResult(
'''
    start=t.find(src_sig)
    if start<0:
        raise SystemExit("v3 transport method missing")
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
        raise SystemExit("v4 log anchor missing")
    v4=v4.replace(needle,repl,1)
    t=t[:start]+v4+t[start:]
    p.write_text(t,encoding="utf-8")
    print("inserted v4 runtime method")
else:
    print("v4 runtime method already present")
