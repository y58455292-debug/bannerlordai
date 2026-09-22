from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948")
sub=root/"candidate"/"SubModule.cs"
s=sub.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderResultV3AdmissionPath =
            Root + @"\\patrol_defense_provider_result_v3_admissions.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderTransportResultBindingPath =
            Root + @"\\patrol_defense_provider_transport_result_bindings.jsonl";
'''
if "PatrolDefenseProviderTransportResultBindingPath" not in s:
    if path_anchor not in s:
        raise SystemExit("binding path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT "))
'''
cmd='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_result_invalid"
                        : TryAdmitPatrolDefenseProviderTransportResult(
                            parts[0],
                            parts[1]);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("binding command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderResultV3(
'''
method=r'''        private string TryAdmitPatrolDefenseProviderTransportResult(
            string base64ReceiptJson,
            string base64ResultJson)
        {
            PatrolDefenseProviderTransportReceiptResultBindingData binding =
                null;

            try
            {
                byte[] receiptBytes =
                    Convert.FromBase64String(
                        base64ReceiptJson);
                byte[] resultBytes =
                    Convert.FromBase64String(
                        base64ResultJson);

                string receiptJson =
                    new UTF8Encoding(false, true)
                        .GetString(receiptBytes);
                string resultJson =
                    new UTF8Encoding(false, true)
                        .GetString(resultBytes);

                binding =
                    PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        receiptJson,
                        resultJson,
                        resultBytes);
            }
            catch (FormatException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultBindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }
            catch (DecoderFallbackException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultBindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_UTF8");
            }

            PatrolDefenseProviderResultV3AdmissionData result =
                binding == null
                    ? null
                    : binding.ProviderResultAdmission;

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            string bindingReceipt =
                PatrolDefenseProviderTransportReceiptResultBinding.ToJson(
                    binding);
            string resultReceipt =
                PatrolDefenseProviderResultV3Admission.ToJson(
                    result);

            string requestFingerprint =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.RequestFingerprint)
                    ? binding.RequestFingerprint
                    : result == null
                        ? null
                        : result.RequestFingerprint;

            string resultStatus =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.ResultStatus)
                    ? binding.ResultStatus
                    : result == null
                        ? null
                        : result.Status;

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    requestFingerprint,
                    resultStatus,
                    accepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportResultBindingPath,
                    bindingReceipt + Environment.NewLine);

                if (!string.IsNullOrWhiteSpace(
                        resultReceipt))
                {
                    File.AppendAllText(
                        PatrolDefenseProviderResultV3AdmissionPath,
                        resultReceipt + Environment.NewLine);
                }

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                binding != null &&
                binding.RejectionReasons.Count > 0
                    ? binding.RejectionReasons[0]
                    : result != null &&
                        result.RejectionReasons.Count > 0
                        ? result.RejectionReasons[0]
                        : accepted
                            ? "ACCEPTED"
                            : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMISSION" +
                " transportId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportId) +
                " transportRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportRequestId) +
                " providerRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ProviderRequestId) +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " resultStatus=" +
                Clean(resultStatus) +
                " resultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ResultSha256) +
                " computedResultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ComputedResultSha256) +
                " transportReceiptMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportReceiptMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_transport_result_admitted"
                : "patrol_defense_provider_transport_result_rejected:" +
                    firstReason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("binding method anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

sub.write_text(s,encoding="utf-8")

fixture=root/"fixtures_v3"/"FixtureV3Program.cs"
f=fixture.read_text(encoding="utf-8-sig")

helper_anchor='''    private static string ResultV3Envelope(
'''
helper=r'''    private static string TransportReceiptV2(
        string transportId,
        string transportRequestId,
        string providerRequestId,
        string providerId,
        string modelId,
        string attemptId,
        string fingerprint,
        string requestInputSha256,
        string resultStatus,
        string resultJson,
        bool success,
        string errorCode,
        string extra)
    {
        string resultSha =
            string.IsNullOrEmpty(resultJson)
                ? null
                : Sha256Upper(resultJson);
        string json =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceipt.v2\"" +
            ",\"transportId\":\"" + transportId + "\"" +
            ",\"transportRequestId\":\"" + transportRequestId + "\"" +
            ",\"providerRequestId\":\"" + providerRequestId + "\"" +
            ",\"providerId\":\"" + providerId + "\"" +
            ",\"modelId\":\"" + modelId + "\"" +
            ",\"attemptId\":\"" + attemptId + "\"" +
            ",\"requestFingerprint\":\"" + fingerprint + "\"" +
            ",\"requestInputSha256\":\"" + requestInputSha256 + "\"" +
            ",\"resultStatus\":" +
                (resultStatus == null ? "null" : "\"" + resultStatus + "\"") +
            ",\"resultSha256\":" +
                (resultSha == null ? "null" : "\"" + resultSha + "\"") +
            ",\"externalNetworkUsed\":false" +
            ",\"modelInvoked\":false" +
            ",\"success\":" + (success ? "true" : "false") +
            ",\"errorCode\":" +
                (errorCode == null ? "null" : "\"" + errorCode + "\"");
        if (!string.IsNullOrEmpty(extra))
            json += "," + extra;
        return json + "}";
    }

'''
if helper not in f:
    if helper_anchor not in f:
        raise SystemExit("fixture helper anchor missing")
    f=f.replace(helper_anchor,helper+helper_anchor,1)

needle='''        Console.WriteLine("PASS_V3_FIXTURES checks=" + checks);
'''
extra=r'''        PatrolDefenseProviderRequestRegistrationData bindProviderReg;
        PatrolDefenseProviderTransportRegistrationData bindTransportReg;
        PatrolDefenseAdvisoryRuntimeGate bindGate;
        var bindQueue = Prepare(
            fingerprint,
            requestJson,
            "attempt-1",
            out bindProviderReg,
            out bindTransportReg,
            out bindGate);
        string bindResult =
            ResultV3Envelope(
                "deterministic_external_worker",
                "attempt-1",
                fingerprint,
                bindProviderReg.ProviderRequestId,
                bindTransportReg.TransportRequestId,
                "SUCCESS",
                advisoryBase64,
                null);
        string bindReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                bindTransportReg.TransportRequestId,
                bindProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                bindResult,
                true,
                null,
                null);
        var bindExact =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                bindGate,
                bindQueue,
                bindReceipt,
                bindResult,
                Encoding.UTF8.GetBytes(bindResult));
        Check(bindExact.BindingAccepted, "bind_exact_binding");
        Check(
            bindExact.TransportReceiptMatchReason ==
                "TRANSPORT_RECEIPT_MATCHED",
            "bind_exact_receipt_match");
        Check(
            bindExact.ResultSha256 ==
                bindExact.ComputedResultSha256 &&
            bindExact.ResultSha256 ==
                Sha256Upper(bindResult),
            "bind_exact_result_hash");
        Check(
            bindExact.ProviderResultAdmission != null &&
            bindExact.ProviderResultAdmission.ProviderResultAccepted &&
            bindExact.ProviderResultAdmission.AdvisoryAdmission != null &&
            bindExact.ProviderResultAdmission.AdvisoryAdmission.Admitted,
            "bind_exact_nested_admission");

        var bindCompletion =
            bindQueue.ApplyProviderResult(
                bindExact.RequestFingerprint,
                bindExact.ResultStatus,
                bindExact.ProviderResultAdmission.ProviderResultAccepted);
        Check(
            bindCompletion.CompletionApplied &&
            bindCompletion.Reason == "SUCCESS_ACKED" &&
            bindQueue.Count == 0,
            "bind_exact_completion");

        PatrolDefenseProviderRequestRegistrationData badHashProviderReg;
        PatrolDefenseProviderTransportRegistrationData badHashTransportReg;
        PatrolDefenseAdvisoryRuntimeGate badHashGate;
        var badHashQueue = Prepare(
            fingerprint,
            requestJson,
            "attempt-1",
            out badHashProviderReg,
            out badHashTransportReg,
            out badHashGate);
        string badHashResult =
            ResultV3Envelope(
                "deterministic_external_worker",
                "attempt-1",
                fingerprint,
                badHashProviderReg.ProviderRequestId,
                badHashTransportReg.TransportRequestId,
                "SUCCESS",
                advisoryBase64,
                null);
        string badHashReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null).Replace(
                    Sha256Upper(badHashResult),
                    new string('0', 64));
        var badHashBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                badHashReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(!badHashBinding.BindingAccepted, "bind_bad_hash_rejected");
        Check(
            badHashBinding.RejectionReasons.Contains(
                "RESULT_SHA256_MISMATCH"),
            "bind_bad_hash_reason");
        Check(
            badHashGate.PendingRequestFingerprint == fingerprint &&
            badHashQueue.Count == 1,
            "bind_bad_hash_retains");

        string alteredBytes =
            badHashResult + " ";
        var alteredBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                TransportReceiptV2(
                    "deterministic_local_loopback",
                    badHashTransportReg.TransportRequestId,
                    badHashProviderReg.ProviderRequestId,
                    "deterministic_external_worker",
                    "deterministic_mock_no_model",
                    "attempt-1",
                    fingerprint,
                    Sha256Upper(requestJson),
                    "SUCCESS",
                    badHashResult,
                    true,
                    null,
                    null),
                alteredBytes,
                Encoding.UTF8.GetBytes(alteredBytes));
        Check(
            alteredBinding.RejectionReasons.Contains(
                "RESULT_SHA256_MISMATCH"),
            "bind_altered_bytes_rejected");

        string wrongStatusReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "PERMANENT_FAILURE",
                badHashResult,
                true,
                null,
                null);
        var wrongStatusBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongStatusReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongStatusBinding.RejectionReasons.Contains(
                "RECEIPT_RESULT_STATUS_MISMATCH"),
            "bind_status_mismatch");

        string wrongTransportReceipt =
            TransportReceiptV2(
                "other_transport",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null);
        var wrongTransportBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongTransportReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongTransportBinding.RejectionReasons.Contains(
                "TRANSPORT_RECEIPT_TRANSPORT_ID_MISMATCH"),
            "bind_transport_id_mismatch");

        string wrongModelReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "wrong_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null);
        var wrongModelBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongModelReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongModelBinding.RejectionReasons.Contains(
                "TRANSPORT_RECEIPT_MODEL_MISMATCH"),
            "bind_model_mismatch");

        string wrongInputReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                new string('C', 64),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null);
        var wrongInputBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongInputReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongInputBinding.RejectionReasons.Contains(
                "TRANSPORT_RECEIPT_INPUT_SHA_MISMATCH"),
            "bind_input_sha_mismatch");

        string wrongRequestIdReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                new string('D', 64),
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null);
        var wrongRequestIdBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongRequestIdReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongRequestIdBinding.RejectionReasons.Contains(
                "RECEIPT_RESULT_PROVIDER_REQUEST_MISMATCH"),
            "bind_provider_request_mismatch");

        string wrongAttemptReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null);
        var wrongAttemptBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongAttemptReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongAttemptBinding.RejectionReasons.Contains(
                "RECEIPT_RESULT_ATTEMPT_MISMATCH"),
            "bind_attempt_mismatch");

        string wrongFingerprintReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                new string('B', 64),
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                null);
        var wrongFingerprintBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                wrongFingerprintReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            wrongFingerprintBinding.RejectionReasons.Contains(
                "RECEIPT_RESULT_FINGERPRINT_MISMATCH"),
            "bind_fingerprint_mismatch");

        string extraReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                true,
                null,
                "\"command\":\"ATTACK\"");
        var extraBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                extraReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            extraBinding.RejectionReasons.Contains(
                "TRANSPORT_RECEIPT_UNEXPECTED_FIELD:command"),
            "bind_extra_receipt_field");

        var malformedBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                "{\"schema\":",
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            malformedBinding.RejectionReasons.Contains(
                "TRANSPORT_RECEIPT_MALFORMED_JSON"),
            "bind_malformed_receipt");

        string failedReceipt =
            TransportReceiptV2(
                "deterministic_local_loopback",
                badHashTransportReg.TransportRequestId,
                badHashProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                fingerprint,
                Sha256Upper(requestJson),
                "SUCCESS",
                badHashResult,
                false,
                "NETWORK_ERROR",
                null);
        var failedBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                badHashGate,
                badHashQueue,
                failedReceipt,
                badHashResult,
                Encoding.UTF8.GetBytes(badHashResult));
        Check(
            failedBinding.RejectionReasons.Contains(
                "TRANSPORT_RECEIPT_NOT_SUCCESS"),
            "bind_failed_receipt_rejected");

        var replayBinding =
            PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                bindGate,
                bindQueue,
                bindReceipt,
                bindResult,
                Encoding.UTF8.GetBytes(bindResult));
        Check(!replayBinding.BindingAccepted, "bind_replay_not_bound");
        Check(
            replayBinding.RejectionReasons.Contains(
                "DISPATCH_JOB_NOT_FOUND"),
            "bind_replay_job_missing");

        string bindingReceiptJson =
            PatrolDefenseProviderTransportReceiptResultBinding.ToJson(
                bindExact);
        Check(
            bindingReceiptJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceiptResultBinding.v1\"") &&
            bindingReceiptJson.Contains(
                "\"transportReceiptMatchReason\":\"TRANSPORT_RECEIPT_MATCHED\"") &&
            bindingReceiptJson.Contains(
                "\"resultSha256\":\"" + Sha256Upper(bindResult) + "\"") &&
            bindingReceiptJson.Contains(
                "\"computedResultSha256\":\"" + Sha256Upper(bindResult) + "\""),
            "bind_receipt_exact");
        Check(
            bindingReceiptJson.Contains(
                "\"executionAuthorized\":false") &&
            bindingReceiptJson.Contains(
                "\"behaviorMutation\":false") &&
            bindingReceiptJson.Contains(
                "\"scoreMutation\":false"),
            "bind_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("v60 fixture insert anchor missing")
    f=f.replace(needle,extra+needle,1)

fixture.write_text(f,encoding="utf-8")

print("v02160 submodule + v3 fixtures patched")
