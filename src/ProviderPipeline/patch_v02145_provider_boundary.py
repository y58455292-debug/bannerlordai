from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002145_ProviderBoundary_20260921_1416")
p=root/"candidate"/"SubModule.cs"
t=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseDeliberationAdmissionPath =
            Root + @"\\patrol_defense_deliberation_admissions.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseDeliberationProviderPath =
            Root + @"\\patrol_defense_deliberation_provider_invocations.jsonl";
'''
if "PatrolDefenseDeliberationProviderPath" not in t:
    if path_anchor not in t:
        raise SystemExit("provider path anchor missing")
    t=t.replace(path_anchor,path_repl,1)

field_anchor='''        private readonly PatrolDefenseAdvisoryRuntimeGate
            _patrolDefenseAdvisoryRuntimeGate =
                new PatrolDefenseAdvisoryRuntimeGate();
'''
field_repl=field_anchor+'''        private string _pendingPatrolDefenseDeliberationRequestJson;
'''
if "_pendingPatrolDefenseDeliberationRequestJson" not in t:
    if field_anchor not in t:
        raise SystemExit("pending request field anchor missing")
    t=t.replace(field_anchor,field_repl,1)

reset_anchor='''            _patrolDefenseAdvisoryRuntimeGate.Reset();
'''
reset_repl='''            _patrolDefenseAdvisoryRuntimeGate.Reset();
            _pendingPatrolDefenseDeliberationRequestJson = null;
'''
if "_pendingPatrolDefenseDeliberationRequestJson = null;" not in t:
    t=t.replace(reset_anchor,reset_repl)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_ADVISORY_ADMIT "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_MOCK_RUN "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_MOCK_RUN ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_mock_invalid"
                        : TryRunPatrolDefenseMockProvider(
                            parts[0],
                            parts[1]);
                }
'''
if cmd not in t:
    if cmd_anchor not in t:
        raise SystemExit("provider command anchor missing")
    t=t.replace(cmd_anchor,cmd+cmd_anchor,1)

arm_anchor='''            if (route != null &&
                route.RouteEligible)
            {
                _patrolDefenseAdvisoryRuntimeGate.Arm(
                    route.RequestFingerprint);
            }
'''
arm_repl='''            if (route != null &&
                route.RouteEligible)
            {
                _patrolDefenseAdvisoryRuntimeGate.Arm(
                    route.RequestFingerprint);
                _pendingPatrolDefenseDeliberationRequestJson =
                    deliberationRequestJson;
            }
'''
if arm_anchor not in t:
    raise SystemExit("provider pending request arm anchor missing")
t=t.replace(arm_anchor,arm_repl,1)

method_anchor='''        private string TryAdmitPatrolDefenseAdvisory(
'''
method=r'''        private string TryRunPatrolDefenseMockProvider(
            string submittedRequestFingerprint,
            string disposition)
        {
            IPatrolDefenseDeliberationProvider provider =
                new PatrolDefenseMockDeliberationProvider(
                    disposition);

            PatrolDefenseProviderInvocationData invocation =
                PatrolDefenseDeliberationProviderRuntime.Invoke(
                    provider,
                    _patrolDefenseAdvisoryRuntimeGate,
                    submittedRequestFingerprint,
                    _pendingPatrolDefenseDeliberationRequestJson);

            string receipt =
                PatrolDefenseDeliberationProviderRuntime.ToJson(
                    invocation);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseDeliberationProviderPath,
                    receipt + Environment.NewLine);
            }

            bool admitted =
                invocation != null &&
                invocation.Admission != null &&
                invocation.Admission.Admitted;

            if (admitted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                invocation != null &&
                invocation.Admission != null &&
                invocation.Admission.RejectionReasons.Count > 0
                    ? invocation.Admission.RejectionReasons[0]
                    : admitted
                        ? "ADMITTED"
                        : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_DELIBERATION_PROVIDER_INVOCATION" +
                " providerId=" +
                Clean(
                    invocation == null
                        ? null
                        : invocation.ProviderId) +
                " providerInvoked=" +
                (invocation != null &&
                 invocation.ProviderInvoked
                    ? "True"
                    : "False") +
                " requestFingerprint=" +
                Clean(submittedRequestFingerprint) +
                " admitted=" +
                (admitted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return admitted
                ? "patrol_defense_provider_admitted"
                : "patrol_defense_provider_rejected:" +
                    firstReason;
        }


'''
if method not in t:
    if method_anchor not in t:
        raise SystemExit("provider method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

p.write_text(t,encoding="utf-8")

proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseDeliberationProvider.cs" Link="PatrolDefenseDeliberationProvider.cs" />
'''
if "PatrolDefenseDeliberationProvider.cs" not in x:
    x=x.replace(
        '''    <Compile Include="..\\candidate\\PatrolDefenseAdvisoryRuntimeGate.cs" Link="PatrolDefenseAdvisoryRuntimeGate.cs" />
''',
        '''    <Compile Include="..\\candidate\\PatrolDefenseAdvisoryRuntimeGate.cs" Link="PatrolDefenseAdvisoryRuntimeGate.cs" />
'''+include)
proj.write_text(x,encoding="utf-8")

fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")

class_anchor='''internal static class FixtureProgram
{
'''
classes=r'''internal sealed class FixtureMalformedProvider :
    IPatrolDefenseDeliberationProvider
{
    public string ProviderId
    {
        get { return "fixture_malformed"; }
    }

    public string Generate(
        string requestFingerprint,
        string deliberationRequestJson)
    {
        return "{\"schema\":";
    }
}

internal sealed class FixtureThrowingProvider :
    IPatrolDefenseDeliberationProvider
{
    public string ProviderId
    {
        get { return "fixture_throwing"; }
    }

    public string Generate(
        string requestFingerprint,
        string deliberationRequestJson)
    {
        throw new InvalidOperationException("fixture");
    }
}

internal static class FixtureProgram
{
'''
if "FixtureMalformedProvider" not in f:
    if class_anchor not in f:
        raise SystemExit("fixture class anchor missing")
    f=f.replace(class_anchor,classes,1)

needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        var providerGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        string providerRequestJson =
            requestJson;

        var providerNoPending =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerNoPending != null &&
            !providerNoPending.ProviderInvoked &&
            !providerNoPending.Admission.Admitted &&
            providerNoPending.Admission.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "provider_no_pending_not_invoked");

        providerGate.Arm(
            request.RequestFingerprint);
        var providerMismatch =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                "WRONG",
                providerRequestJson);
        Check(
            providerMismatch != null &&
            !providerMismatch.ProviderInvoked &&
            !providerMismatch.Admission.Admitted &&
            providerMismatch.Admission.RejectionReasons.Contains(
                "PENDING_REQUEST_FINGERPRINT_MISMATCH") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_mismatch_not_invoked_pending_retained");

        var providerMalformed =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new FixtureMalformedProvider(),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerMalformed.ProviderInvoked &&
            !providerMalformed.Admission.Admitted &&
            providerMalformed.Admission.RejectionReasons.Contains(
                "MALFORMED_JSON") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_malformed_rejected_pending_retained");

        var providerThrow =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new FixtureThrowingProvider(),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerThrow.ProviderInvoked &&
            !providerThrow.Admission.Admitted &&
            providerThrow.Admission.RejectionReasons.Contains(
                "PROVIDER_EXCEPTION:InvalidOperationException") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_exception_rejected_pending_retained");

        var providerKeep =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerKeep.ProviderInvoked &&
            providerKeep.ProviderId ==
                "deterministic_local_mock" &&
            providerKeep.RawAdvisoryJson.Contains(
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\"") &&
            providerKeep.Admission.Admitted &&
            providerKeep.Admission.Disposition ==
                "KEEP_BASELINE" &&
            providerGate.PendingRequestFingerprint == null,
            "provider_exact_admitted_consumes");

        var providerReplay =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            !providerReplay.ProviderInvoked &&
            !providerReplay.Admission.Admitted &&
            providerReplay.Admission.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "provider_replay_rejected");

        string[] providerDispositions =
            new[]
            {
                "KEEP_BASELINE",
                "REVIEW_ALTERNATIVE",
                "ABSTAIN"
            };
        bool allProviderDispositions = true;
        for (
            int i = 0;
            i < providerDispositions.Length;
            i++)
        {
            providerGate.Reset();
            providerGate.Arm(
                request.RequestFingerprint);
            var invocation =
                PatrolDefenseDeliberationProviderRuntime.Invoke(
                    new PatrolDefenseMockDeliberationProvider(
                        providerDispositions[i]),
                    providerGate,
                    request.RequestFingerprint,
                    providerRequestJson);
            if (
                !invocation.ProviderInvoked ||
                !invocation.Admission.Admitted ||
                invocation.Admission.Disposition !=
                    providerDispositions[i])
            {
                allProviderDispositions = false;
                break;
            }
        }
        Check(
            allProviderDispositions,
            "provider_all_allowed_dispositions");

        providerGate.Reset();
        providerGate.Arm(
            request.RequestFingerprint);
        var invalidProviderDisposition =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "ATTACK_NOW"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            invalidProviderDisposition.ProviderInvoked &&
            !invalidProviderDisposition.Admission.Admitted &&
            invalidProviderDisposition.Admission.RejectionReasons.Contains(
                "DISPOSITION_INVALID") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_invalid_disposition_rejected_pending_retained");

        string providerJson =
            PatrolDefenseDeliberationProviderRuntime.ToJson(
                providerKeep);
        Check(
            providerJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationProviderInvocation.v1\"") &&
            providerJson.Contains(
                "\"providerId\":\"deterministic_local_mock\"") &&
            providerJson.Contains(
                "\"providerInvoked\":true"),
            "provider_receipt_identity");
        Check(
            providerJson.Contains(
                "\"externalNetworkUsed\":false") &&
            providerJson.Contains(
                "\"modelInvoked\":false") &&
            providerJson.Contains(
                "\"executionAuthorized\":false"),
            "provider_receipt_no_network_model_execution");
        Check(
            providerJson.Contains(
                "\"behaviorMutation\":false") &&
            providerJson.Contains(
                "\"scoreMutation\":false") &&
            providerJson.Contains(
                "\"nativeMovementCalls\":0"),
            "provider_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("fixture insert anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02145 provider runtime + command + fixtures patched")
