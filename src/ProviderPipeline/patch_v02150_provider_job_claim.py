from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002150_ProviderJobClaim_20260921_1457")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

# Extend job fields.
job_anchor='''        internal string State;
        internal long EnqueuedUtcTicks;
'''
job_repl='''        internal string State;
        internal long EnqueuedUtcTicks;
        internal string ProviderId;
        internal string AttemptId;
        internal long ClaimedUtcTicks;
'''
if "internal string ProviderId;" not in t:
    if job_anchor not in t:
        raise SystemExit("job field anchor missing")
    t=t.replace(job_anchor,job_repl,1)

# Add claim result type after completion result.
type_anchor='''    internal sealed class PatrolDefenseProviderDispatchCompletionResult
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
claim_type=type_anchor+r'''    internal sealed class PatrolDefenseProviderJobClaimResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal bool ClaimApplied;
        internal bool Idempotent;
        internal string Reason;
        internal string StateBefore;
        internal string StateAfter;
        internal int QueueCount;
        internal long? ClaimedUtcTicks;
    }

'''
if "PatrolDefenseProviderJobClaimResult" not in t:
    if type_anchor not in t:
        raise SystemExit("claim result type anchor missing")
    t=t.replace(type_anchor,claim_type,1)

# Add claim method before ApplyProviderResult.
method_anchor='''        internal PatrolDefenseProviderDispatchCompletionResult
            ApplyProviderResult(
'''
claim_method=r'''        internal PatrolDefenseProviderJobClaimResult Claim(
            string requestFingerprint,
            string providerId,
            string attemptId,
            long claimedUtcTicks)
        {
            PatrolDefenseProviderJobClaimResult result =
                new PatrolDefenseProviderJobClaimResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(attemptId))
            {
                result.ClaimApplied = false;
                result.Idempotent = false;
                result.Reason = "INVALID_CLAIM";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.ClaimApplied = false;
                result.Idempotent = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            result.StateBefore = job.State;

            if (string.Equals(
                    job.State,
                    "PENDING",
                    StringComparison.Ordinal))
            {
                job.State = "CLAIMED";
                job.ProviderId = providerId;
                job.AttemptId = attemptId;
                job.ClaimedUtcTicks = claimedUtcTicks;

                result.ClaimApplied = true;
                result.Idempotent = false;
                result.Reason = "CLAIMED";
                result.StateAfter = job.State;
                result.ClaimedUtcTicks = job.ClaimedUtcTicks;
                return result;
            }

            if (string.Equals(
                    job.State,
                    "CLAIMED",
                    StringComparison.Ordinal))
            {
                result.StateAfter = job.State;
                result.ClaimedUtcTicks = job.ClaimedUtcTicks;

                if (string.Equals(
                        job.ProviderId,
                        providerId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        job.AttemptId,
                        attemptId,
                        StringComparison.Ordinal))
                {
                    result.ClaimApplied = false;
                    result.Idempotent = true;
                    result.Reason =
                        "SAME_ATTEMPT_ALREADY_CLAIMED";
                    return result;
                }

                result.ClaimApplied = false;
                result.Idempotent = false;
                result.Reason = "ALREADY_CLAIMED";
                return result;
            }

            result.StateAfter = job.State;
            result.ClaimApplied = false;
            result.Idempotent = false;
            result.Reason = "STATE_INVALID";
            return result;
        }


'''
if "internal PatrolDefenseProviderJobClaimResult Claim(" not in t:
    if method_anchor not in t:
        raise SystemExit("claim method anchor missing")
    t=t.replace(method_anchor,claim_method+method_anchor,1)

# Add claim receipt builder before completion builder.
builder_anchor='''        internal static string ToCompletionJson(
'''
claim_json=r'''        internal static string ToClaimJson(
            PatrolDefenseProviderJobClaimResult result)
        {
            if (result == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderJobClaim.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(result.RequestFingerprint));
            sb.Append(",\"providerId\":");
            sb.Append(Json(result.ProviderId));
            sb.Append(",\"attemptId\":");
            sb.Append(Json(result.AttemptId));
            sb.Append(",\"claimApplied\":");
            sb.Append(result.ClaimApplied ? "true" : "false");
            sb.Append(",\"idempotent\":");
            sb.Append(result.Idempotent ? "true" : "false");
            sb.Append(",\"reason\":");
            sb.Append(Json(result.Reason));
            sb.Append(",\"stateBefore\":");
            sb.Append(Json(result.StateBefore));
            sb.Append(",\"stateAfter\":");
            sb.Append(Json(result.StateAfter));
            sb.Append(",\"queueCount\":");
            sb.Append(
                result.QueueCount.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"claimedUtcTicks\":");
            sb.Append(
                result.ClaimedUtcTicks.HasValue
                    ? result.ClaimedUtcTicks.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : "null");
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
if "ToClaimJson(" not in t:
    if builder_anchor not in t:
        raise SystemExit("claim json anchor missing")
    t=t.replace(builder_anchor,claim_json+builder_anchor,1)

dispatch.write_text(t,encoding="utf-8")

# Patch SubModule.
p=root/"candidate"/"SubModule.cs"
s=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderDispatchCompletionPath =
            Root + @"\\patrol_defense_provider_dispatch_completions.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderJobClaimPath =
            Root + @"\\patrol_defense_provider_job_claims.jsonl";
'''
if "PatrolDefenseProviderJobClaimPath" not in s:
    if path_anchor not in s:
        raise SystemExit("claim path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_JOB_CLAIM ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        3,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 3
                        ? "patrol_defense_provider_job_claim_invalid"
                        : TryClaimPatrolDefenseProviderJob(
                            parts[0],
                            parts[1],
                            parts[2]);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("claim command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderResult(
'''
method=r'''        private string TryClaimPatrolDefenseProviderJob(
            string requestFingerprint,
            string providerId,
            string attemptId)
        {
            PatrolDefenseProviderJobClaimResult claim =
                _patrolDefenseProviderDispatchQueue.Claim(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    DateTime.UtcNow.Ticks);

            string receipt =
                PatrolDefenseProviderDispatchBuilder.ToClaimJson(
                    claim);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderJobClaimPath,
                    receipt + Environment.NewLine);
            }

            string reason =
                claim == null
                    ? "REJECTED"
                    : claim.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM" +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " providerId=" +
                Clean(providerId) +
                " attemptId=" +
                Clean(attemptId) +
                " claimApplied=" +
                (claim != null &&
                 claim.ClaimApplied
                    ? "True"
                    : "False") +
                " idempotent=" +
                (claim != null &&
                 claim.Idempotent
                    ? "True"
                    : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            if (claim != null &&
                claim.ClaimApplied)
            {
                return
                    "patrol_defense_provider_job_claimed";
            }

            if (claim != null &&
                claim.Idempotent)
            {
                return
                    "patrol_defense_provider_job_claim_idempotent";
            }

            return
                "patrol_defense_provider_job_claim_rejected:" +
                reason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("claim method insertion anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

p.write_text(s,encoding="utf-8")

# Add fixtures.
fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        var claimQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var claimEnqueue =
            claimQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                400);
        Check(
            claimEnqueue.Enqueued &&
            claimEnqueue.Job.State == "PENDING",
            "claim_fixture_enqueued_pending");

        var firstClaim =
            claimQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1",
                500);
        Check(
            firstClaim.ClaimApplied &&
            !firstClaim.Idempotent &&
            firstClaim.Reason == "CLAIMED" &&
            firstClaim.StateBefore == "PENDING" &&
            firstClaim.StateAfter == "CLAIMED" &&
            firstClaim.QueueCount == 1 &&
            firstClaim.ClaimedUtcTicks == 500 &&
            claimEnqueue.Job.State == "CLAIMED" &&
            claimEnqueue.Job.ProviderId ==
                "deterministic_external_worker" &&
            claimEnqueue.Job.AttemptId ==
                "attempt-1",
            "claim_first_exact");

        var sameClaim =
            claimQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1",
                501);
        Check(
            !sameClaim.ClaimApplied &&
            sameClaim.Idempotent &&
            sameClaim.Reason ==
                "SAME_ATTEMPT_ALREADY_CLAIMED" &&
            sameClaim.StateBefore == "CLAIMED" &&
            sameClaim.StateAfter == "CLAIMED" &&
            sameClaim.ClaimedUtcTicks == 500,
            "claim_same_attempt_idempotent");

        var competingClaim =
            claimQueue.Claim(
                request.RequestFingerprint,
                "other_worker",
                "attempt-2",
                502);
        Check(
            !competingClaim.ClaimApplied &&
            !competingClaim.Idempotent &&
            competingClaim.Reason ==
                "ALREADY_CLAIMED" &&
            claimEnqueue.Job.ProviderId ==
                "deterministic_external_worker" &&
            claimEnqueue.Job.AttemptId ==
                "attempt-1",
            "claim_competing_provider_rejected");

        var sameProviderNewAttempt =
            claimQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-2",
                503);
        Check(
            !sameProviderNewAttempt.ClaimApplied &&
            !sameProviderNewAttempt.Idempotent &&
            sameProviderNewAttempt.Reason ==
                "ALREADY_CLAIMED",
            "claim_same_provider_new_attempt_rejected");

        var invalidClaim =
            claimQueue.Claim(
                null,
                "worker",
                "attempt",
                504);
        Check(
            !invalidClaim.ClaimApplied &&
            invalidClaim.Reason ==
                "INVALID_CLAIM" &&
            claimQueue.Count == 1,
            "claim_invalid_rejected");

        var missingJobClaim =
            claimQueue.Claim(
                "missing",
                "worker",
                "attempt",
                505);
        Check(
            !missingJobClaim.ClaimApplied &&
            missingJobClaim.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            claimQueue.Count == 1,
            "claim_missing_job_rejected");

        var transientClaimed =
            claimQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "TRANSIENT_FAILURE",
                false);
        Check(
            !transientClaimed.CompletionApplied &&
            transientClaimed.PendingRetained &&
            claimQueue.Count == 1 &&
            claimEnqueue.Job.State == "CLAIMED",
            "claim_transient_retains_claimed_job");

        var successClaimed =
            claimQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            successClaimed.CompletionApplied &&
            successClaimed.Reason ==
                "SUCCESS_ACKED" &&
            claimQueue.Count == 0,
            "claim_success_removes_claimed_job");

        claimQueue.Reset();
        var afterClaimReset =
            claimQueue.Enqueue(
                changedRequest.RequestFingerprint,
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    changedRequest),
                506);
        Check(
            afterClaimReset.Enqueued &&
            afterClaimReset.Job.Sequence >
                claimEnqueue.Job.Sequence &&
            afterClaimReset.Job.State ==
                "PENDING",
            "claim_reset_clears_and_sequence_monotonic");

        string claimReceipt =
            PatrolDefenseProviderDispatchBuilder.ToClaimJson(
                firstClaim);
        Check(
            claimReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderJobClaim.v1\"") &&
            claimReceipt.Contains(
                "\"providerId\":\"deterministic_external_worker\"") &&
            claimReceipt.Contains(
                "\"attemptId\":\"attempt-1\"") &&
            claimReceipt.Contains(
                "\"claimApplied\":true") &&
            claimReceipt.Contains(
                "\"stateBefore\":\"PENDING\"") &&
            claimReceipt.Contains(
                "\"stateAfter\":\"CLAIMED\"") &&
            claimReceipt.Contains(
                "\"claimedUtcTicks\":500"),
            "claim_receipt_exact");
        Check(
            claimReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            claimReceipt.Contains(
                "\"modelInvoked\":false") &&
            claimReceipt.Contains(
                "\"executionAuthorized\":false") &&
            claimReceipt.Contains(
                "\"behaviorMutation\":false") &&
            claimReceipt.Contains(
                "\"scoreMutation\":false"),
            "claim_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("claim fixture anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02150 provider claim state + command + fixtures patched")
