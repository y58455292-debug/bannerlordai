from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041\fixtures\FixtureProgram.cs")
f=p.read_text(encoding="utf-8-sig")

helper_anchor='''        var transportQueue =
            new PatrolDefenseProviderDispatchQueue(2);
'''
helper=r'''        string ExecutionPolicyEnvelope(
            string transportRequestId,
            string providerRequestId,
            string providerId,
            string modelId,
            string attemptId,
            string fingerprint,
            string timeoutMs,
            string maxAttempts,
            string maxResultBytes,
            string credentialRef,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\"," +
                "\"transportRequestId\":\"" + transportRequestId + "\"," +
                "\"providerRequestId\":\"" + providerRequestId + "\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"modelId\":\"" + modelId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"timeoutMs\":\"" + timeoutMs + "\"," +
                "\"maxAttempts\":\"" + maxAttempts + "\"," +
                "\"maxResultBytes\":\"" + maxResultBytes + "\"," +
                "\"credentialRef\":\"" + credentialRef + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

'''
if helper not in f:
    if helper_anchor not in f:
        raise SystemExit("execution policy helper anchor missing")
    f=f.replace(helper_anchor,helper+helper_anchor,1)

needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        var policyQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var policyEnqueue =
            policyQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1900);
        policyQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1901);
        string policyProviderEnvelope =
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
        var policyProviderRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                policyQueue,
                policyProviderEnvelope);
        string policyTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var policyTransportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                policyQueue,
                policyTransportEnvelope);
        Check(
            policyEnqueue.Enqueued &&
            policyProviderRegistration.Registered &&
            policyTransportRegistration.Registered,
            "execution_policy_prepare");

        string policyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var policyRegistration =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                policyEnvelope);
        Check(
            policyRegistration.Registered &&
            !policyRegistration.Idempotent &&
            policyRegistration.Reason == "REGISTERED" &&
            policyRegistration.QueueCount == 1 &&
            !string.IsNullOrWhiteSpace(
                policyRegistration.ExecutionPolicyId) &&
            policyRegistration.ExecutionPolicyId.Length == 64 &&
            policyEnqueue.Job.ExecutionPolicyId ==
                policyRegistration.ExecutionPolicyId &&
            policyEnqueue.Job.ExecutionPolicyTimeoutMs == "1000" &&
            policyEnqueue.Job.ExecutionPolicyMaxAttempts == "1" &&
            policyEnqueue.Job.ExecutionPolicyMaxResultBytes == "65536" &&
            policyEnqueue.Job.ExecutionPolicyCredentialRef ==
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
            "execution_policy_register_exact");

        var policyReplay =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                policyEnvelope);
        Check(
            !policyReplay.Registered &&
            policyReplay.Idempotent &&
            policyReplay.Reason ==
                "SAME_EXECUTION_POLICY_ALREADY_REGISTERED" &&
            policyReplay.ExecutionPolicyId ==
                policyRegistration.ExecutionPolicyId,
            "execution_policy_replay_idempotent");

        string differentPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "2000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var differentPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                differentPolicyEnvelope);
        Check(
            !differentPolicy.Registered &&
            !differentPolicy.Idempotent &&
            differentPolicy.Reason ==
                "EXECUTION_POLICY_ALREADY_REGISTERED" &&
            policyEnqueue.Job.ExecutionPolicyTimeoutMs == "1000",
            "execution_policy_different_rejected");

        var noTransportPolicyQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        noTransportPolicyQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1910);
        noTransportPolicyQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1911);
        var noTransportProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                noTransportPolicyQueue,
                policyProviderEnvelope);
        string noTransportPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                noTransportProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var noTransportPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                noTransportPolicyQueue,
                noTransportPolicyEnvelope);
        Check(
            !noTransportPolicy.Registered &&
            noTransportPolicy.Reason ==
                "TRANSPORT_REQUEST_NOT_REGISTERED",
            "execution_policy_no_transport_rejected");

        string wrongProviderRequestPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                new string('E', 64),
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongProviderRequestPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongProviderRequestPolicyEnvelope);
        Check(
            !wrongProviderRequestPolicy.Registered &&
            wrongProviderRequestPolicy.Reason ==
                "PROVIDER_REQUEST_ID_MISMATCH",
            "execution_policy_wrong_provider_request_rejected");

        string wrongTransportPolicyEnvelope =
            ExecutionPolicyEnvelope(
                new string('F', 64),
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongTransportPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongTransportPolicyEnvelope);
        Check(
            !wrongTransportPolicy.Registered &&
            wrongTransportPolicy.Reason ==
                "TRANSPORT_REQUEST_ID_MISMATCH",
            "execution_policy_wrong_transport_rejected");

        string wrongModelPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "wrong_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongModelPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongModelPolicyEnvelope);
        Check(
            !wrongModelPolicy.Registered &&
            wrongModelPolicy.Reason ==
                "PROVIDER_REQUEST_MODEL_MISMATCH",
            "execution_policy_wrong_model_rejected");

        string wrongProviderPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "other_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongProviderPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongProviderPolicyEnvelope);
        Check(
            !wrongProviderPolicy.Registered &&
            wrongProviderPolicy.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "execution_policy_wrong_provider_rejected");

        string wrongAttemptPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongAttemptPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongAttemptPolicyEnvelope);
        Check(
            !wrongAttemptPolicy.Registered &&
            wrongAttemptPolicy.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "execution_policy_wrong_attempt_rejected");

        string wrongFingerprintPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                "WRONG",
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongFingerprintPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongFingerprintPolicyEnvelope);
        Check(
            !wrongFingerprintPolicy.Registered &&
            wrongFingerprintPolicy.Reason ==
                "DISPATCH_JOB_NOT_FOUND",
            "execution_policy_wrong_fingerprint_rejected");

        foreach (
            var invalidNumeric in new[]
            {
                new[] { "0", "1", "65536", "TIMEOUT_MS_INVALID" },
                new[] { "-1", "1", "65536", "TIMEOUT_MS_INVALID" },
                new[] { "x", "1", "65536", "TIMEOUT_MS_INVALID" },
                new[] { "1000", "0", "65536", "MAX_ATTEMPTS_INVALID" },
                new[] { "1000", "-1", "65536", "MAX_ATTEMPTS_INVALID" },
                new[] { "1000", "x", "65536", "MAX_ATTEMPTS_INVALID" },
                new[] { "1000", "1", "0", "MAX_RESULT_BYTES_INVALID" },
                new[] { "1000", "1", "-1", "MAX_RESULT_BYTES_INVALID" },
                new[] { "1000", "1", "x", "MAX_RESULT_BYTES_INVALID" }
            })
        {
            string invalidEnvelope =
                ExecutionPolicyEnvelope(
                    policyTransportRegistration.TransportRequestId,
                    policyProviderRegistration.ProviderRequestId,
                    "deterministic_external_worker",
                    "deterministic_mock_no_model",
                    "attempt-1",
                    request.RequestFingerprint,
                    invalidNumeric[0],
                    invalidNumeric[1],
                    invalidNumeric[2],
                    "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                    null);
            var invalidResult =
                PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                    policyQueue,
                    invalidEnvelope);
            Check(
                !invalidResult.Registered &&
                invalidResult.Reason == invalidNumeric[3],
                "execution_policy_numeric_" +
                    invalidNumeric[3] + "_" +
                    invalidNumeric[0] + "_" +
                    invalidNumeric[1] + "_" +
                    invalidNumeric[2]);
        }

        foreach (
            string badCredential in new[]
            {
                "",
                "secret",
                "env:",
                "env:BAD-NAME",
                "env:BAD NAME",
                "env:KEY=value"
            })
        {
            string badCredentialEnvelope =
                ExecutionPolicyEnvelope(
                    policyTransportRegistration.TransportRequestId,
                    policyProviderRegistration.ProviderRequestId,
                    "deterministic_external_worker",
                    "deterministic_mock_no_model",
                    "attempt-1",
                    request.RequestFingerprint,
                    "1000",
                    "1",
                    "65536",
                    badCredential,
                    null);
            var badCredentialResult =
                PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                    policyQueue,
                    badCredentialEnvelope);
            Check(
                !badCredentialResult.Registered &&
                badCredentialResult.Reason ==
                    "CREDENTIAL_REF_INVALID",
                "execution_policy_bad_credential_" +
                    badCredential.Replace(":", "_").Replace(" ", "_"));
        }

        string extraPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                "\"secret\":\"DO_NOT_STORE\"");
        var extraPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                extraPolicyEnvelope);
        Check(
            !extraPolicy.Registered &&
            extraPolicy.Reason ==
                "UNEXPECTED_FIELD:secret",
            "execution_policy_extra_field_rejected");

        var malformedPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                "{\"schema\":");
        Check(
            !malformedPolicy.Registered &&
            malformedPolicy.Reason ==
                "MALFORMED_JSON",
            "execution_policy_malformed_rejected");

        var policyRelease =
            policyQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            policyRelease.ReleaseApplied &&
            policyEnqueue.Job.ExecutionPolicyId == null &&
            policyEnqueue.Job.ExecutionPolicyTimeoutMs == null &&
            policyEnqueue.Job.ExecutionPolicyMaxAttempts == null &&
            policyEnqueue.Job.ExecutionPolicyMaxResultBytes == null &&
            policyEnqueue.Job.ExecutionPolicyCredentialRef == null,
            "execution_policy_release_clears");

        policyQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-2",
            1920);
        string retryPolicyProviderEnvelope =
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
        var retryPolicyProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                policyQueue,
                retryPolicyProviderEnvelope);
        string retryPolicyTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                retryPolicyProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var retryPolicyTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                policyQueue,
                retryPolicyTransportEnvelope);
        string retryPolicyEnvelope =
            ExecutionPolicyEnvelope(
                retryPolicyTransportReg.TransportRequestId,
                retryPolicyProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                "1500",
                "2",
                "131072",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var retryPolicyReg =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                retryPolicyEnvelope);
        Check(
            retryPolicyProviderReg.Registered &&
            retryPolicyTransportReg.Registered &&
            retryPolicyReg.Registered &&
            retryPolicyReg.Reason == "REGISTERED" &&
            retryPolicyReg.ExecutionPolicyId !=
                policyRegistration.ExecutionPolicyId &&
            policyEnqueue.Job.ExecutionPolicyId ==
                retryPolicyReg.ExecutionPolicyId &&
            policyEnqueue.Job.AttemptId == "attempt-2",
            "execution_policy_retry_registers_new_policy");

        string policyReceipt =
            PatrolDefenseProviderExecutionPolicyRegistration.ToJson(
                policyRegistration);
        Check(
            policyReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1\"") &&
            policyReceipt.Contains(
                "\"executionPolicyId\":\"" +
                policyRegistration.ExecutionPolicyId +
                "\"") &&
            policyReceipt.Contains(
                "\"timeoutMs\":\"1000\"") &&
            policyReceipt.Contains(
                "\"maxAttempts\":\"1\"") &&
            policyReceipt.Contains(
                "\"maxResultBytes\":\"65536\"") &&
            policyReceipt.Contains(
                "\"credentialRef\":\"env:BANNERLORDAI_TEST_PROVIDER_KEY\"") &&
            policyReceipt.Contains(
                "\"registered\":true") &&
            policyReceipt.Contains(
                "\"reason\":\"REGISTERED\""),
            "execution_policy_receipt_exact");
        Check(
            policyReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            policyReceipt.Contains(
                "\"modelInvoked\":false") &&
            policyReceipt.Contains(
                "\"executionAuthorized\":false") &&
            policyReceipt.Contains(
                "\"behaviorMutation\":false") &&
            policyReceipt.Contains(
                "\"scoreMutation\":false"),
            "execution_policy_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("execution policy fixture insertion anchor missing")
    f=f.replace(needle,extra+needle,1)

p.write_text(f,encoding="utf-8")
print("v02161 execution policy fixtures patched")
