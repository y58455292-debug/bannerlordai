from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002158_TransportRegistration_20260921_1650")

# --- Dispatch queue state ---
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

job_anchor='''        internal string ProviderRequestInputSha256;
'''
job_repl='''        internal string ProviderRequestInputSha256;
        internal string TransportRequestId;
        internal string TransportId;
'''
if "internal string TransportRequestId;" not in t:
    if job_anchor not in t:
        raise SystemExit("transport job fields anchor missing")
    t=t.replace(job_anchor,job_repl,1)

type_anchor='''    internal sealed class PatrolDefenseProviderRequestMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RegisteredProviderRequestId;
        internal bool Matched;
        internal string Reason;
    }

'''
type_repl=type_anchor+r'''    internal sealed class PatrolDefenseProviderTransportRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RequestInputSha256;
        internal string TransportId;
        internal string TransportRequestId;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

'''
if "PatrolDefenseProviderTransportRegistrationResult" not in t:
    if type_anchor not in t:
        raise SystemExit("transport registration type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderRequestMatchResult
            MatchRegisteredProviderRequest(
'''
register_method=r'''        internal PatrolDefenseProviderTransportRegistrationResult
            RegisterTransportRequest(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string requestInputSha256,
                string transportId,
                string transportRequestId)
        {
            PatrolDefenseProviderTransportRegistrationResult result =
                new PatrolDefenseProviderTransportRegistrationResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.RequestInputSha256 = requestInputSha256;
            result.TransportId = transportId;
            result.TransportRequestId = transportRequestId;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(modelId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(providerRequestId) ||
                string.IsNullOrWhiteSpace(requestInputSha256) ||
                string.IsNullOrWhiteSpace(transportId) ||
                string.IsNullOrWhiteSpace(transportRequestId))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "INVALID_TRANSPORT_REQUEST";
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

            if (string.IsNullOrWhiteSpace(
                    job.ProviderRequestId))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "PROVIDER_REQUEST_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestId,
                    providerRequestId,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "PROVIDER_REQUEST_ID_MISMATCH";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "PROVIDER_REQUEST_MODEL_MISMATCH";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestInputSha256,
                    requestInputSha256,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason =
                    "PROVIDER_REQUEST_INPUT_SHA_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.TransportRequestId))
            {
                job.TransportRequestId =
                    transportRequestId;
                job.TransportId =
                    transportId;

                result.Registered = true;
                result.Idempotent = false;
                result.Reason = "REGISTERED";
                return result;
            }

            if (string.Equals(
                    job.TransportRequestId,
                    transportRequestId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.TransportId,
                    transportId,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = true;
                result.Reason =
                    "SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED";
                return result;
            }

            result.Registered = false;
            result.Idempotent = false;
            result.Reason =
                "TRANSPORT_REQUEST_ALREADY_REGISTERED";
            return result;
        }


'''
if "RegisterTransportRequest(" not in t:
    if method_anchor not in t:
        raise SystemExit("transport register method anchor missing")
    t=t.replace(method_anchor,register_method+method_anchor,1)

release_anchor='''            job.ProviderRequestInputSha256 = null;
'''
release_repl='''            job.ProviderRequestInputSha256 = null;
            job.TransportRequestId = null;
            job.TransportId = null;
'''
if "job.TransportRequestId = null;" not in t:
    if release_anchor not in t:
        raise SystemExit("transport release clear anchor missing")
    t=t.replace(release_anchor,release_repl,1)

dispatch.write_text(t,encoding="utf-8")

# --- SubModule path, command, runtime method ---
sub=root/"candidate"/"SubModule.cs"
s=sub.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderRequestRegistrationPath =
            Root + @"\\patrol_defense_provider_request_registrations.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderTransportRegistrationPath =
            Root + @"\\patrol_defense_provider_transport_registrations.jsonl";
'''
if "PatrolDefenseProviderTransportRegistrationPath" not in s:
    if path_anchor not in s:
        raise SystemExit("transport registration path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_transport_registration_invalid"
                        : TryRegisterPatrolDefenseProviderTransport(raw);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("transport registration command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderResultV2(
'''
method=r'''        private string TryRegisterPatrolDefenseProviderTransport(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderTransportRegistrationData registration;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    new UTF8Encoding(false, true)
                        .GetString(bytes);

                registration =
                    PatrolDefenseProviderTransportRegistration.Evaluate(
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                registration =
                    new PatrolDefenseProviderTransportRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_BASE64";
            }
            catch (DecoderFallbackException)
            {
                registration =
                    new PatrolDefenseProviderTransportRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_UTF8";
            }

            string receipt =
                PatrolDefenseProviderTransportRegistration.ToJson(
                    registration);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportRegistrationPath,
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
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTRATION" +
                " transportRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TransportRequestId) +
                " transportId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TransportId) +
                " providerRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderRequestId) +
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
                    "patrol_defense_provider_transport_registered";
            }

            if (idempotent)
            {
                return
                    "patrol_defense_provider_transport_registration_idempotent";
            }

            return
                "patrol_defense_provider_transport_registration_rejected:" +
                reason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("transport registration method anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

sub.write_text(s,encoding="utf-8")

# --- Fixture project include ---
proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderTransportRegistration.cs" Link="PatrolDefenseProviderTransportRegistration.cs" />
'''
if "PatrolDefenseProviderTransportRegistration.cs" not in x:
    anchor='''    <Compile Include="..\\candidate\\PatrolDefenseProviderRequestRegistration.cs" Link="PatrolDefenseProviderRequestRegistration.cs" />
'''
    if anchor not in x:
        raise SystemExit("transport fixture project anchor missing")
    x=x.replace(anchor,anchor+include,1)
proj.write_text(x,encoding="utf-8")

# --- Fixture cases ---
fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        string TransportRequestEnvelope(
            string transportId,
            string providerRequestId,
            string providerId,
            string modelId,
            string attemptId,
            string fingerprint,
            string requestSha,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\"," +
                "\"transportId\":\"" + transportId + "\"," +
                "\"providerRequestId\":\"" + providerRequestId + "\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"modelId\":\"" + modelId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"requestInputSha256\":\"" + requestSha + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        var transportQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var transportEnqueue =
            transportQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1800);
        transportQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1801);
        var transportProviderRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                transportQueue,
                requestEnvelope);
        Check(
            transportProviderRegistration.Registered &&
            transportEnqueue.Job.ProviderRequestId ==
                transportProviderRegistration.ProviderRequestId,
            "transport_provider_request_registered");

        string transportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);

        var transportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                transportEnvelope);
        Check(
            transportRegistration.Registered &&
            !transportRegistration.Idempotent &&
            transportRegistration.Reason ==
                "REGISTERED" &&
            transportRegistration.QueueCount == 1 &&
            !string.IsNullOrWhiteSpace(
                transportRegistration.TransportRequestId) &&
            transportRegistration.TransportRequestId.Length == 64 &&
            transportRegistration.TransportId ==
                "deterministic_local_loopback" &&
            transportEnqueue.Job.TransportRequestId ==
                transportRegistration.TransportRequestId &&
            transportEnqueue.Job.TransportId ==
                "deterministic_local_loopback",
            "transport_register_exact");

        var transportReplay =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                transportEnvelope);
        Check(
            !transportReplay.Registered &&
            transportReplay.Idempotent &&
            transportReplay.Reason ==
                "SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED" &&
            transportReplay.TransportRequestId ==
                transportRegistration.TransportRequestId,
            "transport_replay_idempotent");

        string secondTransportEnvelope =
            TransportRequestEnvelope(
                "other_loopback",
                transportProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var secondTransportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                secondTransportEnvelope);
        Check(
            !secondTransportRegistration.Registered &&
            !secondTransportRegistration.Idempotent &&
            secondTransportRegistration.Reason ==
                "TRANSPORT_REQUEST_ALREADY_REGISTERED" &&
            transportEnqueue.Job.TransportId ==
                "deterministic_local_loopback",
            "transport_second_envelope_rejected");

        var noProviderRequestQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        noProviderRequestQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1810);
        noProviderRequestQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1811);
        var noProviderRequestTransport =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                noProviderRequestQueue,
                transportEnvelope);
        Check(
            !noProviderRequestTransport.Registered &&
            noProviderRequestTransport.Reason ==
                "PROVIDER_REQUEST_NOT_REGISTERED",
            "transport_no_provider_request_rejected");

        var transportMismatchQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        transportMismatchQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1820);
        transportMismatchQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1821);
        var transportMismatchProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                transportMismatchQueue,
                requestEnvelope);

        string wrongProviderRequestIdTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                new string('F', 64),
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongProviderRequestIdReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongProviderRequestIdTransport);
        Check(
            !wrongProviderRequestIdReg.Registered &&
            wrongProviderRequestIdReg.Reason ==
                "PROVIDER_REQUEST_ID_MISMATCH",
            "transport_wrong_provider_request_id_rejected");

        string wrongProviderTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "other_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongProviderTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongProviderTransport);
        Check(
            !wrongProviderTransportReg.Registered &&
            wrongProviderTransportReg.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "transport_wrong_provider_rejected");

        string wrongAttemptTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongAttemptTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongAttemptTransport);
        Check(
            !wrongAttemptTransportReg.Registered &&
            wrongAttemptTransportReg.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "transport_wrong_attempt_rejected");

        string wrongModelTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "wrong_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongModelTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongModelTransport);
        Check(
            !wrongModelTransportReg.Registered &&
            wrongModelTransportReg.Reason ==
                "PROVIDER_REQUEST_MODEL_MISMATCH",
            "transport_wrong_model_rejected");

        string wrongShaTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                new string('0', 64),
                null);
        var wrongShaTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongShaTransport);
        Check(
            !wrongShaTransportReg.Registered &&
            wrongShaTransportReg.Reason ==
                "PROVIDER_REQUEST_INPUT_SHA_MISMATCH",
            "transport_wrong_input_sha_rejected");

        string wrongFingerprintTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                "WRONG",
                requestInputSha,
                null);
        var wrongFingerprintTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongFingerprintTransport);
        Check(
            !wrongFingerprintTransportReg.Registered &&
            wrongFingerprintTransportReg.Reason ==
                "DISPATCH_JOB_NOT_FOUND",
            "transport_wrong_fingerprint_rejected");

        var unclaimedTransportQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        unclaimedTransportQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1830);
        var unclaimedTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                unclaimedTransportQueue,
                transportEnvelope);
        Check(
            !unclaimedTransportReg.Registered &&
            unclaimedTransportReg.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED",
            "transport_unclaimed_rejected");

        var malformedTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                "{\"schema\":");
        Check(
            !malformedTransportReg.Registered &&
            malformedTransportReg.Reason ==
                "MALFORMED_JSON",
            "transport_malformed_rejected");

        string extraTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                "\"command\":\"ATTACK\"");
        var extraTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                extraTransportEnvelope);
        Check(
            !extraTransportReg.Registered &&
            extraTransportReg.Reason ==
                "UNEXPECTED_FIELD:command",
            "transport_extra_field_rejected");

        string missingTransportFieldEnvelope =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\"," +
            "\"providerRequestId\":\"" +
            transportMismatchProviderReg.ProviderRequestId +
            "\",\"providerId\":\"deterministic_external_worker\"," +
            "\"modelId\":\"deterministic_mock_no_model\"," +
            "\"attemptId\":\"attempt-1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\",\"requestInputSha256\":\"" +
            requestInputSha +
            "\"}";
        var missingTransportFieldReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                missingTransportFieldEnvelope);
        Check(
            !missingTransportFieldReg.Registered &&
            missingTransportFieldReg.Reason ==
                "MISSING_FIELD:transportId",
            "transport_missing_field_rejected");

        var transportRelease =
            transportQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            transportRelease.ReleaseApplied &&
            transportEnqueue.Job.TransportRequestId == null &&
            transportEnqueue.Job.TransportId == null &&
            transportEnqueue.Job.ProviderRequestId == null,
            "transport_release_clears_registration");

        transportQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-2",
            1840);
        string retryProviderRequestEnvelope =
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
        var retryProviderRequestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                transportQueue,
                retryProviderRequestEnvelope);
        string retryTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                retryProviderRequestRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var retryTransportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                retryTransportEnvelope);
        Check(
            retryProviderRequestRegistration.Registered &&
            retryTransportRegistration.Registered &&
            retryTransportRegistration.Reason ==
                "REGISTERED" &&
            retryTransportRegistration.TransportRequestId !=
                transportRegistration.TransportRequestId &&
            transportEnqueue.Job.TransportRequestId ==
                retryTransportRegistration.TransportRequestId &&
            transportEnqueue.Job.AttemptId ==
                "attempt-2",
            "transport_retry_registers_new_transport_request");

        string transportReceipt =
            PatrolDefenseProviderTransportRegistration.ToJson(
                transportRegistration);
        Check(
            transportReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRegistration.v1\"") &&
            transportReceipt.Contains(
                "\"transportId\":\"deterministic_local_loopback\"") &&
            transportReceipt.Contains(
                "\"transportRequestId\":\"" +
                transportRegistration.TransportRequestId +
                "\"") &&
            transportReceipt.Contains(
                "\"registered\":true") &&
            transportReceipt.Contains(
                "\"reason\":\"REGISTERED\""),
            "transport_receipt_exact");
        Check(
            transportReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            transportReceipt.Contains(
                "\"modelInvoked\":false") &&
            transportReceipt.Contains(
                "\"executionAuthorized\":false") &&
            transportReceipt.Contains(
                "\"behaviorMutation\":false") &&
            transportReceipt.Contains(
                "\"scoreMutation\":false"),
            "transport_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("transport fixture insertion anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02158 transport registration queue/command/fixtures patched")
