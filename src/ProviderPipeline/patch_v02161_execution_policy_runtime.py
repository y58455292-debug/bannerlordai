from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

job_anchor='''        internal string TransportRequestId;
        internal string TransportId;
'''
job_repl='''        internal string TransportRequestId;
        internal string TransportId;
        internal string ExecutionPolicyId;
        internal string ExecutionPolicyTimeoutMs;
        internal string ExecutionPolicyMaxAttempts;
        internal string ExecutionPolicyMaxResultBytes;
        internal string ExecutionPolicyCredentialRef;
'''
if "internal string ExecutionPolicyId;" not in t:
    if job_anchor not in t:
        raise SystemExit("execution policy job field anchor missing")
    t=t.replace(job_anchor,job_repl,1)

type_anchor='''    internal sealed class PatrolDefenseProviderTransportReceiptMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RequestInputSha256;
        internal string TransportId;
        internal string TransportRequestId;
        internal bool Matched;
        internal string Reason;
    }

'''
type_repl=type_anchor+'''    internal sealed class PatrolDefenseProviderExecutionPolicyRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string TimeoutMs;
        internal string MaxAttempts;
        internal string MaxResultBytes;
        internal string CredentialRef;
        internal string ExecutionPolicyId;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

'''
if "PatrolDefenseProviderExecutionPolicyRegistrationResult" not in t:
    if type_anchor not in t:
        raise SystemExit("execution policy result type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderTransportReceiptMatchResult
            MatchRegisteredTransportReceipt(
'''
method='''        internal PatrolDefenseProviderExecutionPolicyRegistrationResult
            RegisterExecutionPolicy(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string transportRequestId,
                string timeoutMs,
                string maxAttempts,
                string maxResultBytes,
                string credentialRef,
                string executionPolicyId)
        {
            PatrolDefenseProviderExecutionPolicyRegistrationResult result =
                new PatrolDefenseProviderExecutionPolicyRegistrationResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.TransportRequestId = transportRequestId;
            result.TimeoutMs = timeoutMs;
            result.MaxAttempts = maxAttempts;
            result.MaxResultBytes = maxResultBytes;
            result.CredentialRef = credentialRef;
            result.ExecutionPolicyId = executionPolicyId;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(modelId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(providerRequestId) ||
                string.IsNullOrWhiteSpace(transportRequestId) ||
                string.IsNullOrWhiteSpace(timeoutMs) ||
                string.IsNullOrWhiteSpace(maxAttempts) ||
                string.IsNullOrWhiteSpace(maxResultBytes) ||
                string.IsNullOrWhiteSpace(credentialRef) ||
                string.IsNullOrWhiteSpace(executionPolicyId))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "INVALID_EXECUTION_POLICY";
                return result;
            }

            PatrolDefenseProviderTransportMatchResult transportMatch =
                MatchRegisteredTransportRequest(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    providerRequestId,
                    transportRequestId);

            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason =
                    transportMatch == null
                        ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                        : transportMatch.Reason;
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason =
                    "PROVIDER_REQUEST_MODEL_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                job.ExecutionPolicyId =
                    executionPolicyId;
                job.ExecutionPolicyTimeoutMs =
                    timeoutMs;
                job.ExecutionPolicyMaxAttempts =
                    maxAttempts;
                job.ExecutionPolicyMaxResultBytes =
                    maxResultBytes;
                job.ExecutionPolicyCredentialRef =
                    credentialRef;

                result.Registered = true;
                result.Idempotent = false;
                result.Reason = "REGISTERED";
                return result;
            }

            if (string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyTimeoutMs,
                    timeoutMs,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyMaxAttempts,
                    maxAttempts,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyMaxResultBytes,
                    maxResultBytes,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyCredentialRef,
                    credentialRef,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = true;
                result.Reason =
                    "SAME_EXECUTION_POLICY_ALREADY_REGISTERED";
                return result;
            }

            result.Registered = false;
            result.Idempotent = false;
            result.Reason =
                "EXECUTION_POLICY_ALREADY_REGISTERED";
            return result;
        }


'''
if "RegisterExecutionPolicy(" not in t:
    if method_anchor not in t:
        raise SystemExit("execution policy method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

release_anchor='''            job.TransportRequestId = null;
            job.TransportId = null;
'''
release_repl='''            job.TransportRequestId = null;
            job.TransportId = null;
            job.ExecutionPolicyId = null;
            job.ExecutionPolicyTimeoutMs = null;
            job.ExecutionPolicyMaxAttempts = null;
            job.ExecutionPolicyMaxResultBytes = null;
            job.ExecutionPolicyCredentialRef = null;
'''
if release_anchor not in t:
    raise SystemExit("release transport clear anchor missing")
t=t.replace(release_anchor,release_repl,1)

dispatch.write_text(t,encoding="utf-8")

# SubModule path/command/runtime method
sub=root/"candidate"/"SubModule.cs"
s=sub.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderTransportResultBindingPath =
            Root + @"\\patrol_defense_provider_transport_result_bindings.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderExecutionPolicyRegistrationPath =
            Root + @"\\patrol_defense_provider_execution_policy_registrations.jsonl";
'''
if "PatrolDefenseProviderExecutionPolicyRegistrationPath" not in s:
    if path_anchor not in s:
        raise SystemExit("execution policy path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT "))
'''
cmd='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_execution_policy_registration_invalid"
                        : TryRegisterPatrolDefenseProviderExecutionPolicy(raw);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("execution policy command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderTransportResult(
'''
method=r'''        private string TryRegisterPatrolDefenseProviderExecutionPolicy(
            string base64PolicyJson)
        {
            PatrolDefenseProviderExecutionPolicyRegistrationData registration;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64PolicyJson);
                string policyJson =
                    new UTF8Encoding(false, true)
                        .GetString(bytes);

                registration =
                    PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                        _patrolDefenseProviderDispatchQueue,
                        policyJson);
            }
            catch (FormatException)
            {
                registration =
                    new PatrolDefenseProviderExecutionPolicyRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_BASE64";
            }
            catch (DecoderFallbackException)
            {
                registration =
                    new PatrolDefenseProviderExecutionPolicyRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_UTF8";
            }

            string receipt =
                PatrolDefenseProviderExecutionPolicyRegistration.ToJson(
                    registration);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderExecutionPolicyRegistrationPath,
                    receipt + Environment.NewLine);
            }

            bool registered =
                registration != null &&
                registration.Registered;
            bool idempotent =
                registration != null &&
                registration.Idempotent;

            string reason =
                registration == null
                    ? "REJECTED"
                    : registration.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTRATION" +
                " executionPolicyId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ExecutionPolicyId) +
                " transportRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TransportRequestId) +
                " providerRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderRequestId) +
                " providerId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderId) +
                " modelId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ModelId) +
                " attemptId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.AttemptId) +
                " requestFingerprint=" +
                Clean(
                    registration == null
                        ? null
                        : registration.RequestFingerprint) +
                " timeoutMs=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TimeoutMs) +
                " maxAttempts=" +
                Clean(
                    registration == null
                        ? null
                        : registration.MaxAttempts) +
                " maxResultBytes=" +
                Clean(
                    registration == null
                        ? null
                        : registration.MaxResultBytes) +
                " credentialRef=" +
                Clean(
                    registration == null
                        ? null
                        : registration.CredentialRef) +
                " registered=" +
                (registered ? "True" : "False") +
                " idempotent=" +
                (idempotent ? "True" : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            if (registered)
            {
                return
                    "patrol_defense_provider_execution_policy_registered";
            }

            if (idempotent)
            {
                return
                    "patrol_defense_provider_execution_policy_registration_idempotent";
            }

            return
                "patrol_defense_provider_execution_policy_registration_rejected:" +
                    reason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("execution policy method anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

sub.write_text(s,encoding="utf-8")

# Fixture project include new class
proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderExecutionPolicyRegistration.cs" Link="PatrolDefenseProviderExecutionPolicyRegistration.cs" />
'''
if "PatrolDefenseProviderExecutionPolicyRegistration.cs" not in x:
    anchor='''    <Compile Include="..\\candidate\\PatrolDefenseProviderTransportRegistration.cs" Link="PatrolDefenseProviderTransportRegistration.cs" />
'''
    if anchor not in x:
        raise SystemExit("fixture include anchor missing")
    x=x.replace(anchor,anchor+include,1)
proj.write_text(x,encoding="utf-8")

print("v02161 execution policy queue + runtime integration patched")
