from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002148_ProviderResultEnvelope_20260921_1436")
p=root/"candidate"/"SubModule.cs"
t=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderOutboxPath =
            Root + @"\\patrol_defense_provider_outbox.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderResultAdmissionPath =
            Root + @"\\patrol_defense_provider_result_admissions.jsonl";
'''
if "PatrolDefenseProviderResultAdmissionPath" not in t:
    if path_anchor not in t:
        raise SystemExit("result admission path anchor missing")
    t=t.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_MOCK_RUN "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_result_invalid"
                        : TryAdmitPatrolDefenseProviderResult(raw);
                }
'''
if cmd not in t:
    if cmd_anchor not in t:
        raise SystemExit("result command anchor missing")
    t=t.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryRunPatrolDefenseMockProvider(
'''
method=r'''        private string TryAdmitPatrolDefenseProviderResult(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderResultAdmissionData result;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    Encoding.UTF8.GetString(bytes);

                result =
                    PatrolDefenseProviderResultAdmission.Evaluate(
                        _patrolDefenseAdvisoryRuntimeGate,
                        envelopeJson);
            }
            catch (FormatException)
            {
                result =
                    new PatrolDefenseProviderResultAdmissionData();
                result.ProviderResultAccepted = false;
                result.Retryable = true;
                result.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }

            string receipt =
                PatrolDefenseProviderResultAdmission.ToJson(
                    result);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderResultAdmissionPath,
                    receipt + Environment.NewLine);
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
                "PATROL_DEFENSE_PROVIDER_RESULT_ADMISSION" +
                " providerId=" +
                Clean(result == null ? null : result.ProviderId) +
                " attemptId=" +
                Clean(result == null ? null : result.AttemptId) +
                " requestFingerprint=" +
                Clean(result == null ? null : result.RequestFingerprint) +
                " status=" +
                Clean(result == null ? null : result.Status) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_result_admitted"
                : "patrol_defense_provider_result_rejected:" +
                    firstReason;
        }


'''
if method not in t:
    if method_anchor not in t:
        raise SystemExit("result method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

p.write_text(t,encoding="utf-8")

proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderResultAdmission.cs" Link="PatrolDefenseProviderResultAdmission.cs" />
'''
if "PatrolDefenseProviderResultAdmission.cs" not in x:
    x=x.replace(
        '''    <Compile Include="..\\candidate\\PatrolDefenseProviderDispatch.cs" Link="PatrolDefenseProviderDispatch.cs" />
''',
        '''    <Compile Include="..\\candidate\\PatrolDefenseProviderDispatch.cs" Link="PatrolDefenseProviderDispatch.cs" />
'''+include)
proj.write_text(x,encoding="utf-8")

fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        string providerResultAdvisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"}";
        string providerResultAdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    providerResultAdvisory));

        string ProviderEnvelope(
            string providerId,
            string attemptId,
            string fingerprint,
            string status,
            string advisoryBase64,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"status\":\"" + status + "\"," +
                "\"advisoryBase64\":\"" + advisoryBase64 + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        var resultGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        resultGate.Arm(
            request.RequestFingerprint);

        var resultSuccess =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            resultSuccess.ProviderResultAccepted &&
            !resultSuccess.Retryable &&
            resultSuccess.ProviderId ==
                "deterministic_external_worker" &&
            resultSuccess.AttemptId == "attempt-1" &&
            resultSuccess.RequestFingerprint ==
                request.RequestFingerprint &&
            resultSuccess.Status == "SUCCESS" &&
            resultSuccess.AdvisoryAdmission != null &&
            resultSuccess.AdvisoryAdmission.Admitted &&
            resultSuccess.AdvisoryAdmission.Disposition ==
                "KEEP_BASELINE" &&
            resultGate.PendingRequestFingerprint == null,
            "provider_result_success_consumes");

        var resultReplay =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-2",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            !resultReplay.ProviderResultAccepted &&
            resultReplay.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "provider_result_replay_rejected");

        resultGate.Arm(
            request.RequestFingerprint);
        var resultMismatch =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-mismatch",
                    "WRONG",
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            !resultMismatch.ProviderResultAccepted &&
            resultMismatch.RejectionReasons.Contains(
                "PENDING_REQUEST_FINGERPRINT_MISMATCH") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_mismatch_retains_pending");

        var resultTransient =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-transient",
                    request.RequestFingerprint,
                    "TRANSIENT_FAILURE",
                    "",
                    null));
        Check(
            !resultTransient.ProviderResultAccepted &&
            resultTransient.Retryable &&
            resultTransient.RejectionReasons.Contains(
                "PROVIDER_TRANSIENT_FAILURE") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_transient_retryable");

        var resultPermanent =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-permanent",
                    request.RequestFingerprint,
                    "PERMANENT_FAILURE",
                    "",
                    null));
        Check(
            !resultPermanent.ProviderResultAccepted &&
            !resultPermanent.Retryable &&
            resultPermanent.RejectionReasons.Contains(
                "PROVIDER_PERMANENT_FAILURE") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_permanent_fail_closed");

        var resultBadBase64 =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-bad64",
                    request.RequestFingerprint,
                    "SUCCESS",
                    "%%%",
                    null));
        Check(
            !resultBadBase64.ProviderResultAccepted &&
            resultBadBase64.Retryable &&
            resultBadBase64.RejectionReasons.Contains(
                "ADVISORY_BASE64_INVALID") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_bad_advisory_base64_retry");

        string actionAdvisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"," +
            "\"action\":\"ATTACK\"}";
        string actionAdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    actionAdvisory));
        var resultActionBearing =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-action",
                    request.RequestFingerprint,
                    "SUCCESS",
                    actionAdvisoryBase64,
                    null));
        Check(
            !resultActionBearing.ProviderResultAccepted &&
            resultActionBearing.Retryable &&
            resultActionBearing.RejectionReasons.Contains(
                "ADVISORY_NOT_ADMITTED") &&
            resultActionBearing.AdvisoryAdmission != null &&
            resultActionBearing.AdvisoryAdmission.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:action") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_action_advisory_rejected");

        var resultUnknownStatus =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-unknown",
                    request.RequestFingerprint,
                    "MAYBE",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            !resultUnknownStatus.ProviderResultAccepted &&
            resultUnknownStatus.RejectionReasons.Contains(
                "STATUS_INVALID"),
            "provider_result_unknown_status_rejected");

        var resultExtraField =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-extra",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    "\"command\":\"ATTACK\""));
        Check(
            !resultExtraField.ProviderResultAccepted &&
            resultExtraField.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:command"),
            "provider_result_extra_field_rejected");

        string missingProviderEnvelope =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
            "\"attemptId\":\"attempt-x\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"status\":\"SUCCESS\"," +
            "\"advisoryBase64\":\"" +
            providerResultAdvisoryBase64 +
            "\"}";
        var resultMissingProvider =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                missingProviderEnvelope);
        Check(
            !resultMissingProvider.ProviderResultAccepted &&
            resultMissingProvider.RejectionReasons.Contains(
                "PROVIDER_ID_MISSING"),
            "provider_result_missing_provider_rejected");

        string missingAttemptEnvelope =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
            "\"providerId\":\"deterministic_external_worker\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"status\":\"SUCCESS\"," +
            "\"advisoryBase64\":\"" +
            providerResultAdvisoryBase64 +
            "\"}";
        var resultMissingAttempt =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                missingAttemptEnvelope);
        Check(
            !resultMissingAttempt.ProviderResultAccepted &&
            resultMissingAttempt.RejectionReasons.Contains(
                "ATTEMPT_ID_MISSING"),
            "provider_result_missing_attempt_rejected");

        var resultMalformedEnvelope =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                "{\"schema\":");
        Check(
            !resultMalformedEnvelope.ProviderResultAccepted &&
            resultMalformedEnvelope.RejectionReasons.Contains(
                "MALFORMED_JSON"),
            "provider_result_malformed_envelope_rejected");

        resultGate.Reset();
        resultGate.Arm(
            request.RequestFingerprint);
        var resultReceiptSample =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-receipt",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        string resultReceiptJson =
            PatrolDefenseProviderResultAdmission.ToJson(
                resultReceiptSample);
        Check(
            resultReceiptJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderResultAdmission.v1\"") &&
            resultReceiptJson.Contains(
                "\"providerId\":\"deterministic_external_worker\"") &&
            resultReceiptJson.Contains(
                "\"attemptId\":\"attempt-receipt\"") &&
            resultReceiptJson.Contains(
                "\"providerResultAccepted\":true") &&
            resultReceiptJson.Contains(
                "\"executionAuthorized\":false") &&
            resultReceiptJson.Contains(
                "\"modelInvoked\":false") &&
            resultReceiptJson.Contains(
                "\"behaviorMutation\":false"),
            "provider_result_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("fixture insertion anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02148 provider result command + fixtures patched")
