using System;
using System.Security.Cryptography;
using System.Text;
using BannerlordAITestRunner;

internal static class FixtureV3Program
{
    private static int checks;

    private static void Check(bool value, string name)
    {
        checks++;
        if (!value)
            throw new Exception("fixture_failed:" + name);
    }

    private static string Sha256Upper(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }
    }

    private static string ProviderRequestEnvelope(
        string providerId,
        string modelId,
        string attemptId,
        string fingerprint,
        string requestJson)
    {
        string encoded =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(requestJson));
        return
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderRequest.v1\"" +
            ",\"providerId\":\"" + providerId + "\"" +
            ",\"modelId\":\"" + modelId + "\"" +
            ",\"attemptId\":\"" + attemptId + "\"" +
            ",\"requestFingerprint\":\"" + fingerprint + "\"" +
            ",\"promptContractVersion\":\"BannerlordAI.PatrolDefenseAdvisoryPrompt.v1\"" +
            ",\"responseSchema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"" +
            ",\"deliberationRequestBase64\":\"" + encoded + "\"" +
            ",\"inputSha256\":\"" + Sha256Upper(requestJson) + "\"}";
    }

    private static string TransportEnvelope(
        string transportId,
        string providerRequestId,
        string providerId,
        string modelId,
        string attemptId,
        string fingerprint,
        string requestSha)
    {
        return
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\"" +
            ",\"transportId\":\"" + transportId + "\"" +
            ",\"providerRequestId\":\"" + providerRequestId + "\"" +
            ",\"providerId\":\"" + providerId + "\"" +
            ",\"modelId\":\"" + modelId + "\"" +
            ",\"attemptId\":\"" + attemptId + "\"" +
            ",\"requestFingerprint\":\"" + fingerprint + "\"" +
            ",\"requestInputSha256\":\"" + requestSha + "\"}";
    }

    private static string TransportReceiptV2(
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

    private static string ResultV3Envelope(
        string providerId,
        string attemptId,
        string fingerprint,
        string providerRequestId,
        string transportRequestId,
        string status,
        string advisoryBase64,
        string extra)
    {
        string json =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v3\"" +
            ",\"providerId\":\"" + providerId + "\"" +
            ",\"attemptId\":\"" + attemptId + "\"" +
            ",\"requestFingerprint\":\"" + fingerprint + "\"" +
            ",\"providerRequestId\":\"" + providerRequestId + "\"" +
            ",\"transportRequestId\":\"" + transportRequestId + "\"" +
            ",\"status\":\"" + status + "\"" +
            ",\"advisoryBase64\":\"" + advisoryBase64 + "\"";
        if (!string.IsNullOrEmpty(extra))
            json += "," + extra;
        return json + "}";
    }

    private static PatrolDefenseProviderDispatchQueue Prepare(
        string fingerprint,
        string requestJson,
        string attemptId,
        out PatrolDefenseProviderRequestRegistrationData providerReg,
        out PatrolDefenseProviderTransportRegistrationData transportReg,
        out PatrolDefenseAdvisoryRuntimeGate gate)
    {
        var queue = new PatrolDefenseProviderDispatchQueue(2);
        var enqueue = queue.Enqueue(fingerprint, requestJson, 100);
        if (!enqueue.Enqueued)
            throw new Exception("prepare_enqueue");

        var claim = queue.Claim(
            fingerprint,
            "deterministic_external_worker",
            attemptId,
            101);
        if (!claim.ClaimApplied)
            throw new Exception("prepare_claim");

        string providerEnvelope = ProviderRequestEnvelope(
            "deterministic_external_worker",
            "deterministic_mock_no_model",
            attemptId,
            fingerprint,
            requestJson);
        providerReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                queue,
                providerEnvelope);
        if (!providerReg.Registered)
            throw new Exception("prepare_provider_registration");

        string transportEnvelope = TransportEnvelope(
            "deterministic_local_loopback",
            providerReg.ProviderRequestId,
            "deterministic_external_worker",
            "deterministic_mock_no_model",
            attemptId,
            fingerprint,
            Sha256Upper(requestJson));
        transportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                queue,
                transportEnvelope);
        if (!transportReg.Registered)
            throw new Exception("prepare_transport_registration");

        gate = new PatrolDefenseAdvisoryRuntimeGate();
        gate.Arm(fingerprint);
        return queue;
    }

    public static int Main()
    {
        string fingerprint = new string('A', 64);
        string requestJson =
            "{\"requestFingerprint\":\"" + fingerprint +
            "\",\"baselineDecision\":{\"candidateAction\":\"CONTINUE_PATROL\"}}";
        string advisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"" +
            ",\"requestFingerprint\":\"" + fingerprint + "\"" +
            ",\"disposition\":\"KEEP_BASELINE\"}";
        string advisoryBase64 =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(advisory));

        PatrolDefenseProviderRequestRegistrationData providerReg;
        PatrolDefenseProviderTransportRegistrationData transportReg;
        PatrolDefenseAdvisoryRuntimeGate gate;
        var queue = Prepare(
            fingerprint,
            requestJson,
            "attempt-1",
            out providerReg,
            out transportReg,
            out gate);

        string exactEnvelope = ResultV3Envelope(
            "deterministic_external_worker",
            "attempt-1",
            fingerprint,
            providerReg.ProviderRequestId,
            transportReg.TransportRequestId,
            "SUCCESS",
            advisoryBase64,
            null);
        var exact =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                gate,
                queue,
                exactEnvelope);

        Check(exact.ProviderResultAccepted, "exact_accepted");
        Check(exact.ClaimMatchReason == "CLAIM_MATCHED", "exact_claim");
        Check(
            exact.ProviderRequestMatchReason == "PROVIDER_REQUEST_MATCHED",
            "exact_provider_request");
        Check(
            exact.TransportRequestMatchReason == "TRANSPORT_REQUEST_MATCHED",
            "exact_transport");
        Check(
            exact.ProviderRequestId == providerReg.ProviderRequestId,
            "exact_provider_request_id");
        Check(
            exact.TransportRequestId == transportReg.TransportRequestId,
            "exact_transport_request_id");
        Check(
            exact.AdvisoryAdmission != null &&
            exact.AdvisoryAdmission.Admitted &&
            exact.AdvisoryAdmission.Disposition == "KEEP_BASELINE",
            "exact_advisory");
        Check(gate.PendingRequestFingerprint == null, "exact_consumes_pending");

        var completion = queue.ApplyProviderResult(
            exact.RequestFingerprint,
            exact.Status,
            exact.ProviderResultAccepted);
        Check(
            completion.CompletionApplied &&
            completion.Reason == "SUCCESS_ACKED" &&
            queue.Count == 0,
            "exact_completion");

        var noTransportQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        noTransportQueue.Enqueue(fingerprint, requestJson, 200);
        noTransportQueue.Claim(
            fingerprint,
            "deterministic_external_worker",
            "attempt-1",
            201);
        var noTransportProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                noTransportQueue,
                ProviderRequestEnvelope(
                    "deterministic_external_worker",
                    "deterministic_mock_no_model",
                    "attempt-1",
                    fingerprint,
                    requestJson));
        var noTransportGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        noTransportGate.Arm(fingerprint);
        var noTransport =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                noTransportGate,
                noTransportQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    fingerprint,
                    noTransportProviderReg.ProviderRequestId,
                    transportReg.TransportRequestId,
                    "SUCCESS",
                    advisoryBase64,
                    null));
        Check(!noTransport.ProviderResultAccepted, "no_transport_rejected");
        Check(
            noTransport.TransportRequestMatchReason ==
                "TRANSPORT_REQUEST_NOT_REGISTERED",
            "no_transport_reason");
        Check(
            noTransportGate.PendingRequestFingerprint == fingerprint,
            "no_transport_pending");

        PatrolDefenseProviderRequestRegistrationData mismatchProviderReg;
        PatrolDefenseProviderTransportRegistrationData mismatchTransportReg;
        PatrolDefenseAdvisoryRuntimeGate mismatchGate;
        var mismatchQueue = Prepare(
            fingerprint,
            requestJson,
            "attempt-1",
            out mismatchProviderReg,
            out mismatchTransportReg,
            out mismatchGate);

        var wrongTransport =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    fingerprint,
                    mismatchProviderReg.ProviderRequestId,
                    new string('E', 64),
                    "SUCCESS",
                    advisoryBase64,
                    null));
        Check(!wrongTransport.ProviderResultAccepted, "wrong_transport_rejected");
        Check(
            wrongTransport.TransportRequestMatchReason ==
                "TRANSPORT_REQUEST_ID_MISMATCH",
            "wrong_transport_reason");
        Check(
            mismatchGate.PendingRequestFingerprint == fingerprint,
            "wrong_transport_pending");

        var wrongProviderRequest =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    fingerprint,
                    new string('F', 64),
                    mismatchTransportReg.TransportRequestId,
                    "SUCCESS",
                    advisoryBase64,
                    null));
        Check(
            wrongProviderRequest.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_ID_MISMATCH",
            "wrong_provider_request");

        var wrongProvider =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                ResultV3Envelope(
                    "other_worker",
                    "attempt-1",
                    fingerprint,
                    mismatchProviderReg.ProviderRequestId,
                    mismatchTransportReg.TransportRequestId,
                    "SUCCESS",
                    advisoryBase64,
                    null));
        Check(
            wrongProvider.ClaimMatchReason == "PROVIDER_CLAIM_MISMATCH",
            "wrong_provider");

        var wrongAttempt =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-2",
                    fingerprint,
                    mismatchProviderReg.ProviderRequestId,
                    mismatchTransportReg.TransportRequestId,
                    "SUCCESS",
                    advisoryBase64,
                    null));
        Check(
            wrongAttempt.ClaimMatchReason == "PROVIDER_CLAIM_MISMATCH",
            "wrong_attempt");

        var transient =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    fingerprint,
                    mismatchProviderReg.ProviderRequestId,
                    mismatchTransportReg.TransportRequestId,
                    "TRANSIENT_FAILURE",
                    "",
                    null));
        Check(!transient.ProviderResultAccepted, "transient_not_accepted");
        Check(transient.Retryable, "transient_retryable");
        Check(
            transient.TransportRequestMatchReason ==
                "TRANSPORT_REQUEST_MATCHED",
            "transient_transport_match");
        Check(
            transient.RejectionReasons.Contains(
                "PROVIDER_TRANSIENT_FAILURE"),
            "transient_reason");
        var transientCompletion =
            mismatchQueue.ApplyProviderResult(
                transient.RequestFingerprint,
                transient.Status,
                transient.ProviderResultAccepted);
        Check(
            transientCompletion.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            transientCompletion.PendingRetained &&
            mismatchQueue.Count == 1,
            "transient_completion");

        string fingerprint2 = new string('B', 64);
        string requestJson2 =
            "{\"requestFingerprint\":\"" + fingerprint2 + "\"}";
        PatrolDefenseProviderRequestRegistrationData permanentProviderReg;
        PatrolDefenseProviderTransportRegistrationData permanentTransportReg;
        PatrolDefenseAdvisoryRuntimeGate permanentGate;
        var permanentQueue = Prepare(
            fingerprint2,
            requestJson2,
            "attempt-p",
            out permanentProviderReg,
            out permanentTransportReg,
            out permanentGate);
        var permanent =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                permanentGate,
                permanentQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-p",
                    fingerprint2,
                    permanentProviderReg.ProviderRequestId,
                    permanentTransportReg.TransportRequestId,
                    "PERMANENT_FAILURE",
                    "",
                    null));
        Check(!permanent.ProviderResultAccepted, "permanent_not_accepted");
        Check(!permanent.Retryable, "permanent_not_retryable");
        Check(
            permanent.TransportRequestMatchReason ==
                "TRANSPORT_REQUEST_MATCHED",
            "permanent_transport_match");
        Check(
            permanent.RejectionReasons.Contains(
                "PROVIDER_PERMANENT_FAILURE"),
            "permanent_reason");
        var permanentCompletion =
            permanentQueue.ApplyProviderResult(
                permanent.RequestFingerprint,
                permanent.Status,
                permanent.ProviderResultAccepted);
        Check(
            permanentCompletion.CompletionApplied &&
            permanentCompletion.Reason ==
                "PERMANENT_FAILURE_ACKED" &&
            permanentQueue.Count == 0,
            "permanent_completion");

        var malformed =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                "{\"schema\":");
        Check(
            malformed.RejectionReasons.Contains("MALFORMED_JSON"),
            "malformed");

        var extra =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                mismatchGate,
                mismatchQueue,
                ResultV3Envelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    fingerprint,
                    mismatchProviderReg.ProviderRequestId,
                    mismatchTransportReg.TransportRequestId,
                    "SUCCESS",
                    advisoryBase64,
                    "\"command\":\"ATTACK\""));
        Check(
            extra.RejectionReasons.Contains("UNEXPECTED_FIELD:command"),
            "extra_field");

        var replay =
            PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                gate,
                queue,
                exactEnvelope);
        Check(!replay.ProviderResultAccepted, "replay_not_accepted");
        Check(
            replay.ClaimMatchReason == "DISPATCH_JOB_NOT_FOUND",
            "replay_job_missing");
        Check(replay.ProviderRequestMatchReason == null, "replay_no_request_match");
        Check(replay.TransportRequestMatchReason == null, "replay_no_transport_match");

        string receipt =
            PatrolDefenseProviderResultV3Admission.ToJson(exact);
        Check(
            receipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderResultV3Admission.v1\""),
            "receipt_schema");
        Check(
            receipt.Contains(
                "\"providerRequestId\":\"" +
                providerReg.ProviderRequestId +
                "\""),
            "receipt_provider_request");
        Check(
            receipt.Contains(
                "\"transportRequestId\":\"" +
                transportReg.TransportRequestId +
                "\""),
            "receipt_transport_request");
        Check(
            receipt.Contains(
                "\"transportRequestMatchReason\":\"TRANSPORT_REQUEST_MATCHED\""),
            "receipt_transport_match");
        Check(
            receipt.Contains("\"providerResultAccepted\":true"),
            "receipt_accepted");
        Check(
            receipt.Contains("\"executionAuthorized\":false") &&
            receipt.Contains("\"modelInvoked\":false") &&
            receipt.Contains("\"behaviorMutation\":false") &&
            receipt.Contains("\"scoreMutation\":false"),
            "receipt_zero_authority");

        PatrolDefenseProviderRequestRegistrationData bindProviderReg;
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

        Console.WriteLine("PASS_V3_FIXTURES checks=" + checks);
        return 0;
    }
}
