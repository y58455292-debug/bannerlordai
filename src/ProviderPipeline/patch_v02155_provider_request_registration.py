from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002155_ProviderRequestRegistration_20260921_1541")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

job_anchor='''        internal string AttemptId;
        internal long ClaimedUtcTicks;
'''
job_repl='''        internal string AttemptId;
        internal long ClaimedUtcTicks;
        internal string ProviderRequestId;
        internal string ProviderRequestModelId;
        internal string ProviderRequestPromptContractVersion;
        internal string ProviderRequestResponseSchema;
        internal string ProviderRequestInputSha256;
'''
if "internal string ProviderRequestId;" not in t:
    if job_anchor not in t:
        raise SystemExit("provider request job fields anchor missing")
    t=t.replace(job_anchor,job_repl,1)

type_anchor='''    internal sealed class PatrolDefenseProviderJobReleaseResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal bool ReleaseApplied;
        internal string Reason;
        internal string StateBefore;
        internal string StateAfter;
        internal int QueueCount;
    }

'''
type_repl=type_anchor+r'''    internal sealed class PatrolDefenseProviderRequestRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string PromptContractVersion;
        internal string ResponseSchema;
        internal string InputSha256;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

'''
if "PatrolDefenseProviderRequestRegistrationResult" not in t:
    if type_anchor not in t:
        raise SystemExit("provider request registration type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderJobReleaseResult ReleaseClaim(
'''
register_method=r'''        internal PatrolDefenseProviderRequestRegistrationResult
            RegisterProviderRequest(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string promptContractVersion,
                string responseSchema,
                string decodedRequestJson,
                string inputSha256,
                string providerRequestId)
        {
            PatrolDefenseProviderRequestRegistrationResult result =
                new PatrolDefenseProviderRequestRegistrationResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.PromptContractVersion = promptContractVersion;
            result.ResponseSchema = responseSchema;
            result.InputSha256 = inputSha256;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(modelId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(promptContractVersion) ||
                string.IsNullOrWhiteSpace(responseSchema) ||
                string.IsNullOrWhiteSpace(decodedRequestJson) ||
                string.IsNullOrWhiteSpace(inputSha256) ||
                string.IsNullOrWhiteSpace(providerRequestId))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "INVALID_PROVIDER_REQUEST";
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
                    job.State,
                    "CLAIMED",
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "PROVIDER_JOB_NOT_CLAIMED";
                return result;
            }

            if (!string.Equals(
                    job.ProviderId,
                    providerId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    job.AttemptId,
                    attemptId,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "PROVIDER_CLAIM_MISMATCH";
                return result;
            }

            if (!string.Equals(
                    job.DeliberationRequestJson,
                    decodedRequestJson,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "DELIBERATION_REQUEST_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ProviderRequestId))
            {
                job.ProviderRequestId = providerRequestId;
                job.ProviderRequestModelId = modelId;
                job.ProviderRequestPromptContractVersion =
                    promptContractVersion;
                job.ProviderRequestResponseSchema =
                    responseSchema;
                job.ProviderRequestInputSha256 =
                    inputSha256;

                result.Registered = true;
                result.Idempotent = false;
                result.Reason = "REGISTERED";
                return result;
            }

            if (string.Equals(
                    job.ProviderRequestId,
                    providerRequestId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestPromptContractVersion,
                    promptContractVersion,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestResponseSchema,
                    responseSchema,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestInputSha256,
                    inputSha256,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = true;
                result.Reason =
                    "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED";
                return result;
            }

            result.Registered = false;
            result.Idempotent = false;
            result.Reason =
                "PROVIDER_REQUEST_ALREADY_REGISTERED";
            return result;
        }


'''
if "RegisterProviderRequest(" not in t:
    if method_anchor not in t:
        raise SystemExit("provider request registration method anchor missing")
    t=t.replace(method_anchor,register_method+method_anchor,1)

release_anchor='''            job.State = "PENDING";
            job.ProviderId = null;
            job.AttemptId = null;
            job.ClaimedUtcTicks = 0;
'''
release_repl='''            job.State = "PENDING";
            job.ProviderId = null;
            job.AttemptId = null;
            job.ClaimedUtcTicks = 0;
            job.ProviderRequestId = null;
            job.ProviderRequestModelId = null;
            job.ProviderRequestPromptContractVersion = null;
            job.ProviderRequestResponseSchema = null;
            job.ProviderRequestInputSha256 = null;
'''
if release_anchor not in t:
    raise SystemExit("provider request release clear anchor missing")
t=t.replace(release_anchor,release_repl,1)

dispatch.write_text(t,encoding="utf-8")

# SubModule command/path/runtime method.
sub=root/"candidate"/"SubModule.cs"
s=sub.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderJobReleasePath =
            Root + @"\\patrol_defense_provider_job_releases.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderRequestRegistrationPath =
            Root + @"\\patrol_defense_provider_request_registrations.jsonl";
'''
if "PatrolDefenseProviderRequestRegistrationPath" not in s:
    if path_anchor not in s:
        raise SystemExit("provider request registration path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_request_registration_invalid"
                        : TryRegisterPatrolDefenseProviderRequest(raw);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("provider request registration command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderResult(
'''
method=r'''        private string TryRegisterPatrolDefenseProviderRequest(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderRequestRegistrationData registration;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    new UTF8Encoding(false, true)
                        .GetString(bytes);

                registration =
                    PatrolDefenseProviderRequestRegistration.Evaluate(
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                registration =
                    new PatrolDefenseProviderRequestRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_BASE64";
            }
            catch (DecoderFallbackException)
            {
                registration =
                    new PatrolDefenseProviderRequestRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_UTF8";
            }

            string receipt =
                PatrolDefenseProviderRequestRegistration.ToJson(
                    registration);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderRequestRegistrationPath,
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
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTRATION" +
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
                    "patrol_defense_provider_request_registered";
            }

            if (idempotent)
            {
                return
                    "patrol_defense_provider_request_registration_idempotent";
            }

            return
                "patrol_defense_provider_request_registration_rejected:" +
                reason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("provider request registration method anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

sub.write_text(s,encoding="utf-8")

# Fixture project include.
proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderRequestRegistration.cs" Link="PatrolDefenseProviderRequestRegistration.cs" />
'''
if "PatrolDefenseProviderRequestRegistration.cs" not in x:
    anchor='''    <Compile Include="..\\candidate\\PatrolDefenseProviderResultAdmission.cs" Link="PatrolDefenseProviderResultAdmission.cs" />
'''
    if anchor not in x:
        raise SystemExit("fixture project include anchor missing")
    x=x.replace(anchor,anchor+include,1)
proj.write_text(x,encoding="utf-8")

# Fixture cases.
fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        string ProviderRequestInputSha(
            string rawRequest)
        {
            byte[] bytes =
                System.Text.Encoding.UTF8.GetBytes(
                    rawRequest);
            using (
                System.Security.Cryptography.SHA256 sha =
                    System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                System.Text.StringBuilder sb =
                    new System.Text.StringBuilder(
                        hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("X2"));
                return sb.ToString();
            }
        }

        string ProviderRequestEnvelope(
            string providerId,
            string modelId,
            string attemptId,
            string fingerprint,
            string rawRequest,
            string promptVersion,
            string responseSchema,
            string inputSha,
            string extra)
        {
            string encoded =
                Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        rawRequest));
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderRequest.v1\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"modelId\":\"" + modelId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"promptContractVersion\":\"" + promptVersion + "\"," +
                "\"responseSchema\":\"" + responseSchema + "\"," +
                "\"deliberationRequestBase64\":\"" + encoded + "\"," +
                "\"inputSha256\":\"" + inputSha + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        var requestRegQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var requestRegEnqueue =
            requestRegQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1000);
        requestRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1001);

        string requestInputSha =
            ProviderRequestInputSha(
                requestJson);
        string requestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);

        var requestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                requestEnvelope);
        Check(
            requestRegistration.Registered &&
            !requestRegistration.Idempotent &&
            requestRegistration.Reason ==
                "REGISTERED" &&
            requestRegistration.QueueCount == 1 &&
            !string.IsNullOrWhiteSpace(
                requestRegistration.ProviderRequestId) &&
            requestRegistration.ProviderRequestId.Length == 64 &&
            requestRegEnqueue.Job.ProviderRequestId ==
                requestRegistration.ProviderRequestId &&
            requestRegEnqueue.Job.ProviderRequestModelId ==
                "deterministic_mock_no_model" &&
            requestRegEnqueue.Job.ProviderRequestInputSha256 ==
                requestInputSha,
            "provider_request_register_exact");

        var requestReplay =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                requestEnvelope);
        Check(
            !requestReplay.Registered &&
            requestReplay.Idempotent &&
            requestReplay.Reason ==
                "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED" &&
            requestReplay.ProviderRequestId ==
                requestRegistration.ProviderRequestId,
            "provider_request_replay_idempotent");

        string differentEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "different_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var differentRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                differentEnvelope);
        Check(
            !differentRegistration.Registered &&
            !differentRegistration.Idempotent &&
            differentRegistration.Reason ==
                "PROVIDER_REQUEST_ALREADY_REGISTERED" &&
            requestRegEnqueue.Job.ProviderRequestModelId ==
                "deterministic_mock_no_model",
            "provider_request_different_envelope_rejected");

        var unclaimedRegQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        unclaimedRegQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1100);
        var unclaimedRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                unclaimedRegQueue,
                requestEnvelope);
        Check(
            !unclaimedRegistration.Registered &&
            unclaimedRegistration.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED",
            "provider_request_unclaimed_rejected");

        var mismatchRegQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        mismatchRegQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1200);
        mismatchRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1201);

        string wrongProviderEnvelope =
            ProviderRequestEnvelope(
                "other_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongProviderRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongProviderEnvelope);
        Check(
            !wrongProviderRegistration.Registered &&
            wrongProviderRegistration.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "provider_request_wrong_provider_rejected");

        string wrongAttemptEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongAttemptRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongAttemptEnvelope);
        Check(
            !wrongAttemptRegistration.Registered &&
            wrongAttemptRegistration.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "provider_request_wrong_attempt_rejected");

        string wrongFingerprintEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                "WRONG",
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongFingerprintRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongFingerprintEnvelope);
        Check(
            !wrongFingerprintRegistration.Registered &&
            wrongFingerprintRegistration.Reason ==
                "DISPATCH_JOB_NOT_FOUND",
            "provider_request_wrong_fingerprint_rejected");

        string alteredRequestJson =
            requestJson.Replace(
                "CONTINUE_PATROL",
                "WAIT");
        string alteredRequestSha =
            ProviderRequestInputSha(
                alteredRequestJson);
        string alteredRequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                alteredRequestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                alteredRequestSha,
                null);
        var alteredRequestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                alteredRequestEnvelope);
        Check(
            !alteredRequestRegistration.Registered &&
            alteredRequestRegistration.Reason ==
                "DELIBERATION_REQUEST_MISMATCH",
            "provider_request_altered_payload_rejected");

        string badShaEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                new string('0', 64),
                null);
        var badShaRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                badShaEnvelope);
        Check(
            !badShaRegistration.Registered &&
            badShaRegistration.Reason ==
                "INPUT_SHA256_MISMATCH",
            "provider_request_bad_sha_rejected");

        string wrongPromptEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "Wrong.Prompt",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongPromptRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongPromptEnvelope);
        Check(
            !wrongPromptRegistration.Registered &&
            wrongPromptRegistration.Reason ==
                "PROMPT_CONTRACT_VERSION_INVALID",
            "provider_request_wrong_prompt_rejected");

        string wrongSchemaEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "Wrong.Response",
                requestInputSha,
                null);
        var wrongSchemaRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongSchemaEnvelope);
        Check(
            !wrongSchemaRegistration.Registered &&
            wrongSchemaRegistration.Reason ==
                "RESPONSE_SCHEMA_INVALID",
            "provider_request_wrong_response_schema_rejected");

        string extraEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                "\"action\":\"ATTACK\"");
        var extraRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                extraEnvelope);
        Check(
            !extraRegistration.Registered &&
            extraRegistration.Reason ==
                "UNEXPECTED_FIELD:action",
            "provider_request_extra_field_rejected");

        var malformedRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                "{\"schema\":");
        Check(
            !malformedRegistration.Registered &&
            malformedRegistration.Reason ==
                "MALFORMED_JSON",
            "provider_request_malformed_rejected");

        var releaseRegistration =
            requestRegQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            releaseRegistration.ReleaseApplied &&
            requestRegEnqueue.Job.ProviderRequestId == null &&
            requestRegEnqueue.Job.ProviderRequestModelId == null &&
            requestRegEnqueue.Job.ProviderRequestPromptContractVersion == null &&
            requestRegEnqueue.Job.ProviderRequestResponseSchema == null &&
            requestRegEnqueue.Job.ProviderRequestInputSha256 == null,
            "provider_request_release_clears_registration");

        requestRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-2",
            1300);
        string attempt2Envelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var attempt2Registration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                attempt2Envelope);
        Check(
            attempt2Registration.Registered &&
            attempt2Registration.Reason ==
                "REGISTERED" &&
            requestRegEnqueue.Job.ProviderRequestId ==
                attempt2Registration.ProviderRequestId &&
            requestRegEnqueue.Job.AttemptId ==
                "attempt-2",
            "provider_request_retry_attempt_registers_new_request");

        string registrationReceipt =
            PatrolDefenseProviderRequestRegistration.ToJson(
                requestRegistration);
        Check(
            registrationReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderRequestRegistration.v1\"") &&
            registrationReceipt.Contains(
                "\"reason\":\"REGISTERED\"") &&
            registrationReceipt.Contains(
                "\"registered\":true") &&
            registrationReceipt.Contains(
                "\"providerRequestId\":\"" +
                requestRegistration.ProviderRequestId +
                "\""),
            "provider_request_registration_receipt_exact");
        Check(
            registrationReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            registrationReceipt.Contains(
                "\"modelInvoked\":false") &&
            registrationReceipt.Contains(
                "\"executionAuthorized\":false") &&
            registrationReceipt.Contains(
                "\"behaviorMutation\":false") &&
            registrationReceipt.Contains(
                "\"scoreMutation\":false"),
            "provider_request_registration_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("provider request registration fixture anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02155 provider request registration state + command + fixtures patched")
