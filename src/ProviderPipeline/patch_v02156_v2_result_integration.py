from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549")
sub=root/"candidate"/"SubModule.cs"
s=sub.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderResultAdmissionPath =
            Root + @"\\patrol_defense_provider_result_admissions.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderResultV2AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v2_admissions.jsonl";
'''
if "PatrolDefenseProviderResultV2AdmissionPath" not in s:
    if path_anchor not in s:
        raise SystemExit("v2 result path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_result_v2_invalid"
                        : TryAdmitPatrolDefenseProviderResultV2(raw);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("v2 result command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderResult(
'''
method=r'''        private string TryAdmitPatrolDefenseProviderResultV2(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderResultV2AdmissionData result;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    Encoding.UTF8.GetString(bytes);

                result =
                    PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                result =
                    new PatrolDefenseProviderResultV2AdmissionData();
                result.ProviderResultAccepted = false;
                result.Retryable = true;
                result.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }

            string receipt =
                PatrolDefenseProviderResultV2Admission.ToJson(
                    result);

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    result == null
                        ? null
                        : result.RequestFingerprint,
                    result == null
                        ? null
                        : result.Status,
                    result != null &&
                        result.ProviderResultAccepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderResultV2AdmissionPath,
                    receipt + Environment.NewLine);

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                result != null &&
                result.RejectionReasons.Count > 0
                    ? result.RejectionReasons[0]
                    : accepted
                        ? "ACCEPTED"
                        : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMISSION" +
                " providerId=" +
                Clean(result == null ? null : result.ProviderId) +
                " attemptId=" +
                Clean(result == null ? null : result.AttemptId) +
                " requestFingerprint=" +
                Clean(result == null ? null : result.RequestFingerprint) +
                " providerRequestId=" +
                Clean(result == null ? null : result.ProviderRequestId) +
                " status=" +
                Clean(result == null ? null : result.Status) +
                " claimMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ClaimMatchReason) +
                " providerRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ProviderRequestMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " dispatchQueueBefore=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountBefore.ToString(
                        CultureInfo.InvariantCulture)) +
                " dispatchQueueAfter=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountAfter.ToString(
                        CultureInfo.InvariantCulture)) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_result_v2_admitted"
                : "patrol_defense_provider_result_v2_rejected:" +
                    firstReason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("v2 result method anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

sub.write_text(s,encoding="utf-8")

# Fixture project include v2 result file.
proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderResultV2Admission.cs" Link="PatrolDefenseProviderResultV2Admission.cs" />
'''
if "PatrolDefenseProviderResultV2Admission.cs" not in x:
    anchor='''    <Compile Include="..\\candidate\\PatrolDefenseProviderResultAdmission.cs" Link="PatrolDefenseProviderResultAdmission.cs" />
'''
    if anchor not in x:
        raise SystemExit("v2 fixture include anchor missing")
    x=x.replace(anchor,anchor+include,1)
proj.write_text(x,encoding="utf-8")

fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        string ProviderResultV2Envelope(
            string providerId,
            string attemptId,
            string fingerprint,
            string providerRequestId,
            string status,
            string advisoryBase64,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v2\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"providerRequestId\":\"" + providerRequestId + "\"," +
                "\"status\":\"" + status + "\"," +
                "\"advisoryBase64\":\"" + advisoryBase64 + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        string v2Advisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"}";
        string v2AdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    v2Advisory));

        var v2Queue =
            new PatrolDefenseProviderDispatchQueue(2);
        var v2Enqueue =
            v2Queue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1400);
        v2Queue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1401);

        string v2InputSha =
            ProviderRequestInputSha(
                requestJson);
        string v2RequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                v2InputSha,
                null);
        var v2RequestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                v2Queue,
                v2RequestEnvelope);
        Check(
            v2RequestRegistration.Registered &&
            !string.IsNullOrWhiteSpace(
                v2RequestRegistration.ProviderRequestId),
            "v2_request_registration_fixture");

        var v2Gate =
            new PatrolDefenseAdvisoryRuntimeGate();
        v2Gate.Arm(
            request.RequestFingerprint);

        string exactV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2RequestRegistration.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                null);

        var exactV2Result =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2Gate,
                v2Queue,
                exactV2Envelope);
        Check(
            exactV2Result.ProviderResultAccepted &&
            exactV2Result.ClaimMatchReason ==
                "CLAIM_MATCHED" &&
            exactV2Result.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_MATCHED" &&
            exactV2Result.ProviderRequestId ==
                v2RequestRegistration.ProviderRequestId &&
            exactV2Result.AdvisoryAdmission != null &&
            exactV2Result.AdvisoryAdmission.Admitted &&
            exactV2Result.AdvisoryAdmission.Disposition ==
                "KEEP_BASELINE" &&
            v2Gate.PendingRequestFingerprint == null,
            "v2_exact_registered_success_admitted");

        var exactV2Completion =
            v2Queue.ApplyProviderResult(
                exactV2Result.RequestFingerprint,
                exactV2Result.Status,
                exactV2Result.ProviderResultAccepted);
        Check(
            exactV2Completion.CompletionApplied &&
            exactV2Completion.Reason ==
                "SUCCESS_ACKED" &&
            v2Queue.Count == 0,
            "v2_exact_success_completion");

        var v2NoRegQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        v2NoRegQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1500);
        v2NoRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1501);
        var v2NoRegGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        v2NoRegGate.Arm(
            request.RequestFingerprint);
        var noRegV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2NoRegGate,
                v2NoRegQueue,
                exactV2Envelope);
        Check(
            !noRegV2.ProviderResultAccepted &&
            noRegV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_NOT_REGISTERED" &&
            noRegV2.RejectionReasons.Contains(
                "PROVIDER_REQUEST_NOT_REGISTERED") &&
            v2NoRegGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "v2_no_registration_rejected");

        var v2MismatchQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        v2MismatchQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1600);
        v2MismatchQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1601);
        var v2MismatchReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                v2MismatchQueue,
                v2RequestEnvelope);
        var v2MismatchGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        v2MismatchGate.Arm(
            request.RequestFingerprint);

        string wrongIdEnvelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                new string('F', 64),
                "SUCCESS",
                v2AdvisoryBase64,
                null);
        var wrongIdV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                wrongIdEnvelope);
        Check(
            !wrongIdV2.ProviderResultAccepted &&
            wrongIdV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_ID_MISMATCH" &&
            wrongIdV2.RejectionReasons.Contains(
                "PROVIDER_REQUEST_ID_MISMATCH") &&
            v2MismatchGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "v2_wrong_request_id_rejected");

        string wrongProviderV2Envelope =
            ProviderResultV2Envelope(
                "other_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                null);
        var wrongProviderV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                wrongProviderV2Envelope);
        Check(
            !wrongProviderV2.ProviderResultAccepted &&
            wrongProviderV2.ClaimMatchReason ==
                "PROVIDER_CLAIM_MISMATCH",
            "v2_wrong_provider_rejected");

        string wrongAttemptV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-2",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                null);
        var wrongAttemptV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                wrongAttemptV2Envelope);
        Check(
            !wrongAttemptV2.ProviderResultAccepted &&
            wrongAttemptV2.ClaimMatchReason ==
                "PROVIDER_CLAIM_MISMATCH",
            "v2_wrong_attempt_rejected");

        string transientV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "TRANSIENT_FAILURE",
                "",
                null);
        var transientV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                transientV2Envelope);
        Check(
            !transientV2.ProviderResultAccepted &&
            transientV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_MATCHED" &&
            transientV2.Retryable &&
            transientV2.RejectionReasons.Contains(
                "PROVIDER_TRANSIENT_FAILURE"),
            "v2_transient_exact_request");

        var transientV2Completion =
            v2MismatchQueue.ApplyProviderResult(
                transientV2.RequestFingerprint,
                transientV2.Status,
                transientV2.ProviderResultAccepted);
        Check(
            transientV2Completion.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            transientV2Completion.PendingRetained &&
            v2MismatchQueue.Count == 1,
            "v2_transient_completion_retains");

        var v2PermanentQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        v2PermanentQueue.Enqueue(
            changedRequest.RequestFingerprint,
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest),
            1700);
        v2PermanentQueue.Claim(
            changedRequest.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-p",
            1701);
        string changedJson =
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest);
        string changedSha =
            ProviderRequestInputSha(
                changedJson);
        string changedRequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-p",
                changedRequest.RequestFingerprint,
                changedJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                changedSha,
                null);
        var changedRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                v2PermanentQueue,
                changedRequestEnvelope);
        var permanentGateV2 =
            new PatrolDefenseAdvisoryRuntimeGate();
        permanentGateV2.Arm(
            changedRequest.RequestFingerprint);
        string permanentV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-p",
                changedRequest.RequestFingerprint,
                changedRegistration.ProviderRequestId,
                "PERMANENT_FAILURE",
                "",
                null);
        var permanentV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                permanentGateV2,
                v2PermanentQueue,
                permanentV2Envelope);
        Check(
            !permanentV2.ProviderResultAccepted &&
            permanentV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_MATCHED" &&
            permanentV2.RejectionReasons.Contains(
                "PROVIDER_PERMANENT_FAILURE"),
            "v2_permanent_exact_request");

        var permanentV2Completion =
            v2PermanentQueue.ApplyProviderResult(
                permanentV2.RequestFingerprint,
                permanentV2.Status,
                permanentV2.ProviderResultAccepted);
        Check(
            permanentV2Completion.CompletionApplied &&
            permanentV2Completion.Reason ==
                "PERMANENT_FAILURE_ACKED" &&
            v2PermanentQueue.Count == 0,
            "v2_permanent_completion_removes");

        var malformedV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                "{\"schema\":");
        Check(
            !malformedV2.ProviderResultAccepted &&
            malformedV2.RejectionReasons.Contains(
                "MALFORMED_JSON"),
            "v2_malformed_rejected");

        string extraV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                "\"command\":\"ATTACK\"");
        var extraV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                extraV2Envelope);
        Check(
            !extraV2.ProviderResultAccepted &&
            extraV2.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:command"),
            "v2_extra_field_rejected");

        var replayV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2Gate,
                v2Queue,
                exactV2Envelope);
        Check(
            !replayV2.ProviderResultAccepted &&
            replayV2.ProviderRequestMatchReason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            replayV2.RejectionReasons.Contains(
                "DISPATCH_JOB_NOT_FOUND"),
            "v2_replay_job_missing");

        string v2Receipt =
            PatrolDefenseProviderResultV2Admission.ToJson(
                exactV2Result);
        Check(
            v2Receipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderResultV2Admission.v1\"") &&
            v2Receipt.Contains(
                "\"providerRequestId\":\"" +
                v2RequestRegistration.ProviderRequestId +
                "\"") &&
            v2Receipt.Contains(
                "\"providerRequestMatchReason\":\"PROVIDER_REQUEST_MATCHED\"") &&
            v2Receipt.Contains(
                "\"providerResultAccepted\":true"),
            "v2_receipt_exact");
        Check(
            v2Receipt.Contains(
                "\"executionAuthorized\":false") &&
            v2Receipt.Contains(
                "\"modelInvoked\":false") &&
            v2Receipt.Contains(
                "\"behaviorMutation\":false") &&
            v2Receipt.Contains(
                "\"scoreMutation\":false"),
            "v2_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("v2 fixture anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02156 v2 result command + fixtures patched")
