from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002149_ProviderWorkLifecycle_20260921_1451")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

anchor='''    internal sealed class PatrolDefenseProviderDispatchResult
    {
        internal bool Enqueued;
        internal string Reason;
        internal int QueueCount;
        internal int Capacity;
        internal PatrolDefenseProviderDispatchJob Job;
    }

'''
insert=anchor+r'''    internal sealed class PatrolDefenseProviderDispatchCompletionResult
    {
        internal string RequestFingerprint;
        internal string ResultStatus;
        internal bool CompletionApplied;
        internal string Reason;
        internal int QueueCountBefore;
        internal int QueueCountAfter;
        internal bool PendingRetained;
    }

'''
if "PatrolDefenseProviderDispatchCompletionResult" not in t:
    if anchor not in t:
        raise SystemExit("completion result anchor missing")
    t=t.replace(anchor,insert,1)

method_anchor='''        internal PatrolDefenseProviderDispatchResult Enqueue(
            string requestFingerprint,
            string deliberationRequestJson,
            long utcTicks)
'''
# insert completion method before Enqueue
completion=r'''        internal PatrolDefenseProviderDispatchCompletionResult
            ApplyProviderResult(
                string requestFingerprint,
                string resultStatus,
                bool providerResultAccepted)
        {
            PatrolDefenseProviderDispatchCompletionResult result =
                new PatrolDefenseProviderDispatchCompletionResult();

            result.RequestFingerprint =
                requestFingerprint;
            result.ResultStatus =
                resultStatus;
            result.QueueCountBefore =
                _pending.Count;

            if (string.IsNullOrWhiteSpace(
                    requestFingerprint))
            {
                result.CompletionApplied = false;
                result.Reason = "INVALID_REQUEST";
                result.QueueCountAfter = _pending.Count;
                result.PendingRetained = false;
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.CompletionApplied = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                result.QueueCountAfter = _pending.Count;
                result.PendingRetained = false;
                return result;
            }

            if (string.Equals(
                    resultStatus,
                    "SUCCESS",
                    StringComparison.Ordinal))
            {
                if (providerResultAccepted)
                {
                    _pending.Remove(
                        requestFingerprint);
                    result.CompletionApplied = true;
                    result.Reason = "SUCCESS_ACKED";
                    result.PendingRetained = false;
                }
                else
                {
                    result.CompletionApplied = false;
                    result.Reason =
                        "SUCCESS_NOT_ADMITTED_RETAINED";
                    result.PendingRetained = true;
                }
            }
            else if (string.Equals(
                         resultStatus,
                         "PERMANENT_FAILURE",
                         StringComparison.Ordinal))
            {
                _pending.Remove(
                    requestFingerprint);
                result.CompletionApplied = true;
                result.Reason =
                    "PERMANENT_FAILURE_ACKED";
                result.PendingRetained = false;
            }
            else if (string.Equals(
                         resultStatus,
                         "TRANSIENT_FAILURE",
                         StringComparison.Ordinal))
            {
                result.CompletionApplied = false;
                result.Reason =
                    "TRANSIENT_FAILURE_RETAINED";
                result.PendingRetained = true;
            }
            else
            {
                result.CompletionApplied = false;
                result.Reason =
                    "RESULT_INVALID_RETAINED";
                result.PendingRetained = true;
            }

            result.QueueCountAfter =
                _pending.Count;
            return result;
        }


'''
if "ApplyProviderResult(" not in t:
    if method_anchor not in t:
        raise SystemExit("queue method anchor missing")
    t=t.replace(method_anchor,completion+method_anchor,1)

builder_anchor='''        internal static string ToOutboxJobJson(
            PatrolDefenseProviderDispatchJob job)
'''
completion_json=r'''        internal static string ToCompletionJson(
            PatrolDefenseProviderDispatchCompletionResult result)
        {
            if (result == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatchCompletion.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(
                Json(
                    result.RequestFingerprint));
            sb.Append(",\"resultStatus\":");
            sb.Append(
                Json(
                    result.ResultStatus));
            sb.Append(",\"completionApplied\":");
            sb.Append(
                result.CompletionApplied
                    ? "true"
                    : "false");
            sb.Append(",\"reason\":");
            sb.Append(
                Json(
                    result.Reason));
            sb.Append(",\"queueCountBefore\":");
            sb.Append(
                result.QueueCountBefore.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"queueCountAfter\":");
            sb.Append(
                result.QueueCountAfter.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"pendingRetained\":");
            sb.Append(
                result.PendingRetained
                    ? "true"
                    : "false");
            sb.Append(",\"externalNetworkUsed\":false");
            sb.Append(",\"modelInvoked\":false");
            sb.Append(",\"llmInvoked\":false");
            sb.Append(",\"plannerInvoked\":false");
            sb.Append(",\"executionAuthorized\":false");
            sb.Append(",\"interpretationApplied\":false");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");

            return sb.ToString();
        }


'''
if "ToCompletionJson(" not in t:
    if builder_anchor not in t:
        raise SystemExit("completion json anchor missing")
    t=t.replace(builder_anchor,completion_json+builder_anchor,1)

dispatch.write_text(t,encoding="utf-8")

p=root/"candidate"/"SubModule.cs"
s=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderResultAdmissionPath =
            Root + @"\\patrol_defense_provider_result_admissions.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderDispatchCompletionPath =
            Root + @"\\patrol_defense_provider_dispatch_completions.jsonl";
'''
if "PatrolDefenseProviderDispatchCompletionPath" not in s:
    if path_anchor not in s:
        raise SystemExit("completion path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

result_anchor='''            string receipt =
                PatrolDefenseProviderResultAdmission.ToJson(
                    result);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderResultAdmissionPath,
                    receipt + Environment.NewLine);
            }

            bool accepted =
'''
result_repl='''            string receipt =
                PatrolDefenseProviderResultAdmission.ToJson(
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
                    PatrolDefenseProviderResultAdmissionPath,
                    receipt + Environment.NewLine);

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            bool accepted =
'''
if result_anchor not in s:
    raise SystemExit("result completion integration anchor missing")
s=s.replace(result_anchor,result_repl,1)

log_anchor='''                " reason=" +
                Clean(firstReason) +
                " modelInvoked=False" +
'''
log_repl='''                " reason=" +
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
'''
if log_anchor not in s:
    raise SystemExit("completion log anchor missing")
s=s.replace(log_anchor,log_repl,1)

p.write_text(s,encoding="utf-8")

fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        var lifecycleQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        var lifecycleEnqueue =
            lifecycleQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                200);
        Check(
            lifecycleEnqueue.Enqueued &&
            lifecycleQueue.Count == 1,
            "lifecycle_enqueue");

        var lifecycleTransient =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "TRANSIENT_FAILURE",
                false);
        Check(
            !lifecycleTransient.CompletionApplied &&
            lifecycleTransient.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            lifecycleTransient.QueueCountBefore == 1 &&
            lifecycleTransient.QueueCountAfter == 1 &&
            lifecycleTransient.PendingRetained &&
            lifecycleQueue.Count == 1,
            "lifecycle_transient_retained");

        var lifecycleInvalidStatus =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                null,
                false);
        Check(
            !lifecycleInvalidStatus.CompletionApplied &&
            lifecycleInvalidStatus.Reason ==
                "RESULT_INVALID_RETAINED" &&
            lifecycleInvalidStatus.PendingRetained &&
            lifecycleQueue.Count == 1,
            "lifecycle_invalid_retained");

        var lifecycleSuccessRejected =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                false);
        Check(
            !lifecycleSuccessRejected.CompletionApplied &&
            lifecycleSuccessRejected.Reason ==
                "SUCCESS_NOT_ADMITTED_RETAINED" &&
            lifecycleSuccessRejected.PendingRetained &&
            lifecycleQueue.Count == 1,
            "lifecycle_success_not_admitted_retained");

        var lifecycleSuccess =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            lifecycleSuccess.CompletionApplied &&
            lifecycleSuccess.Reason ==
                "SUCCESS_ACKED" &&
            lifecycleSuccess.QueueCountBefore == 1 &&
            lifecycleSuccess.QueueCountAfter == 0 &&
            !lifecycleSuccess.PendingRetained &&
            lifecycleQueue.Count == 0,
            "lifecycle_success_removes");

        var lifecycleDouble =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            !lifecycleDouble.CompletionApplied &&
            lifecycleDouble.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            lifecycleDouble.QueueCountBefore == 0 &&
            lifecycleDouble.QueueCountAfter == 0,
            "lifecycle_double_complete_noop");

        var lifecycleAfterSuccess =
            lifecycleQueue.Enqueue(
                changedRequest.RequestFingerprint,
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    changedRequest),
                201);
        Check(
            lifecycleAfterSuccess.Enqueued &&
            lifecycleAfterSuccess.Job.Sequence >
                lifecycleEnqueue.Job.Sequence &&
            lifecycleQueue.Count == 1,
            "lifecycle_capacity_freed_sequence_monotonic");

        var lifecyclePermanent =
            lifecycleQueue.ApplyProviderResult(
                changedRequest.RequestFingerprint,
                "PERMANENT_FAILURE",
                false);
        Check(
            lifecyclePermanent.CompletionApplied &&
            lifecyclePermanent.Reason ==
                "PERMANENT_FAILURE_ACKED" &&
            lifecyclePermanent.QueueCountBefore == 1 &&
            lifecyclePermanent.QueueCountAfter == 0 &&
            !lifecyclePermanent.PendingRetained &&
            lifecycleQueue.Count == 0,
            "lifecycle_permanent_removes");

        var unrelatedQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        unrelatedQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            300);
        var lifecycleWrong =
            unrelatedQueue.ApplyProviderResult(
                "WRONG",
                "SUCCESS",
                true);
        Check(
            !lifecycleWrong.CompletionApplied &&
            lifecycleWrong.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            unrelatedQueue.Count == 1,
            "lifecycle_wrong_fingerprint_preserves_other");

        var lifecycleInvalidRequest =
            unrelatedQueue.ApplyProviderResult(
                null,
                "SUCCESS",
                true);
        Check(
            !lifecycleInvalidRequest.CompletionApplied &&
            lifecycleInvalidRequest.Reason ==
                "INVALID_REQUEST" &&
            unrelatedQueue.Count == 1,
            "lifecycle_invalid_request_preserves_queue");

        string lifecycleReceipt =
            PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                lifecycleSuccess);
        Check(
            lifecycleReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatchCompletion.v1\"") &&
            lifecycleReceipt.Contains(
                "\"reason\":\"SUCCESS_ACKED\"") &&
            lifecycleReceipt.Contains(
                "\"queueCountBefore\":1") &&
            lifecycleReceipt.Contains(
                "\"queueCountAfter\":0") &&
            lifecycleReceipt.Contains(
                "\"pendingRetained\":false"),
            "lifecycle_receipt_exact");
        Check(
            lifecycleReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            lifecycleReceipt.Contains(
                "\"modelInvoked\":false") &&
            lifecycleReceipt.Contains(
                "\"executionAuthorized\":false") &&
            lifecycleReceipt.Contains(
                "\"behaviorMutation\":false") &&
            lifecycleReceipt.Contains(
                "\"scoreMutation\":false"),
            "lifecycle_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("lifecycle fixture anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02149 dispatch completion lifecycle + integration + fixtures patched")
