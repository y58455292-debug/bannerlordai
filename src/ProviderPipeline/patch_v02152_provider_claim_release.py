from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

type_anchor='''    internal sealed class PatrolDefenseProviderClaimMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal bool Matched;
        internal string Reason;
        internal string State;
        internal string ClaimedProviderId;
        internal string ClaimedAttemptId;
    }

'''
type_repl=type_anchor+r'''    internal sealed class PatrolDefenseProviderJobReleaseResult
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
if "PatrolDefenseProviderJobReleaseResult" not in t:
    if type_anchor not in t:
        raise SystemExit("release result type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderClaimMatchResult MatchClaim(
'''
release_method=r'''        internal PatrolDefenseProviderJobReleaseResult ReleaseClaim(
            string requestFingerprint,
            string providerId,
            string attemptId)
        {
            PatrolDefenseProviderJobReleaseResult result =
                new PatrolDefenseProviderJobReleaseResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(attemptId))
            {
                result.ReleaseApplied = false;
                result.Reason = "INVALID_RELEASE";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.ReleaseApplied = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            result.StateBefore = job.State;

            if (!string.Equals(
                    job.State,
                    "CLAIMED",
                    StringComparison.Ordinal))
            {
                result.ReleaseApplied = false;
                result.Reason = "PROVIDER_JOB_NOT_CLAIMED";
                result.StateAfter = job.State;
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
                result.ReleaseApplied = false;
                result.Reason = "PROVIDER_CLAIM_MISMATCH";
                result.StateAfter = job.State;
                return result;
            }

            job.State = "PENDING";
            job.ProviderId = null;
            job.AttemptId = null;
            job.ClaimedUtcTicks = 0;

            result.ReleaseApplied = true;
            result.Reason = "CLAIM_RELEASED";
            result.StateAfter = job.State;
            return result;
        }


'''
if "ReleaseClaim(" not in t:
    if method_anchor not in t:
        raise SystemExit("release method anchor missing")
    t=t.replace(method_anchor,release_method+method_anchor,1)

builder_anchor='''        internal static string ToClaimJson(
'''
release_json=r'''        internal static string ToReleaseJson(
            PatrolDefenseProviderJobReleaseResult result)
        {
            if (result == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderJobRelease.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(result.RequestFingerprint));
            sb.Append(",\"providerId\":");
            sb.Append(Json(result.ProviderId));
            sb.Append(",\"attemptId\":");
            sb.Append(Json(result.AttemptId));
            sb.Append(",\"releaseApplied\":");
            sb.Append(result.ReleaseApplied ? "true" : "false");
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
if "ToReleaseJson(" not in t:
    if builder_anchor not in t:
        raise SystemExit("release json anchor missing")
    t=t.replace(builder_anchor,release_json+builder_anchor,1)

dispatch.write_text(t,encoding="utf-8")

p=root/"candidate"/"SubModule.cs"
s=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderJobClaimPath =
            Root + @"\\patrol_defense_provider_job_claims.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderJobReleasePath =
            Root + @"\\patrol_defense_provider_job_releases.jsonl";
'''
if "PatrolDefenseProviderJobReleasePath" not in s:
    if path_anchor not in s:
        raise SystemExit("release path anchor missing")
    s=s.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "))
'''
cmd=r'''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_JOB_RELEASE "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_JOB_RELEASE ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        3,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 3
                        ? "patrol_defense_provider_job_release_invalid"
                        : TryReleasePatrolDefenseProviderJob(
                            parts[0],
                            parts[1],
                            parts[2]);
                }
'''
if cmd not in s:
    if cmd_anchor not in s:
        raise SystemExit("release command anchor missing")
    s=s.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryClaimPatrolDefenseProviderJob(
'''
method=r'''        private string TryReleasePatrolDefenseProviderJob(
            string requestFingerprint,
            string providerId,
            string attemptId)
        {
            PatrolDefenseProviderJobReleaseResult release =
                _patrolDefenseProviderDispatchQueue.ReleaseClaim(
                    requestFingerprint,
                    providerId,
                    attemptId);

            string receipt =
                PatrolDefenseProviderDispatchBuilder.ToReleaseJson(
                    release);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderJobReleasePath,
                    receipt + Environment.NewLine);
            }

            string reason =
                release == null
                    ? "REJECTED"
                    : release.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_JOB_RELEASE" +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " providerId=" +
                Clean(providerId) +
                " attemptId=" +
                Clean(attemptId) +
                " releaseApplied=" +
                (release != null &&
                 release.ReleaseApplied
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

            return release != null &&
                release.ReleaseApplied
                    ? "patrol_defense_provider_job_released"
                    : "patrol_defense_provider_job_release_rejected:" +
                        reason;
        }


'''
if method not in s:
    if method_anchor not in s:
        raise SystemExit("release method insertion anchor missing")
    s=s.replace(method_anchor,method+method_anchor,1)

p.write_text(s,encoding="utf-8")

fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        var releaseQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var releaseEnqueue =
            releaseQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                800);
        var releaseClaim =
            releaseQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1",
                801);
        Check(
            releaseEnqueue.Enqueued &&
            releaseClaim.ClaimApplied &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_fixture_claimed");

        var wrongRelease =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "other_worker",
                "attempt-1");
        Check(
            !wrongRelease.ReleaseApplied &&
            wrongRelease.Reason ==
                "PROVIDER_CLAIM_MISMATCH" &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_wrong_provider_rejected");

        var wrongAttemptRelease =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-2");
        Check(
            !wrongAttemptRelease.ReleaseApplied &&
            wrongAttemptRelease.Reason ==
                "PROVIDER_CLAIM_MISMATCH" &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_wrong_attempt_rejected");

        var invalidRelease =
            releaseQueue.ReleaseClaim(
                null,
                "worker",
                "attempt");
        Check(
            !invalidRelease.ReleaseApplied &&
            invalidRelease.Reason ==
                "INVALID_RELEASE" &&
            releaseQueue.Count == 1,
            "release_invalid_rejected");

        var missingRelease =
            releaseQueue.ReleaseClaim(
                "missing",
                "worker",
                "attempt");
        Check(
            !missingRelease.ReleaseApplied &&
            missingRelease.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            releaseQueue.Count == 1,
            "release_missing_job_rejected");

        var transientBeforeRelease =
            releaseQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "TRANSIENT_FAILURE",
                false);
        Check(
            transientBeforeRelease.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            transientBeforeRelease.PendingRetained &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_transient_retains_claim");

        var exactRelease =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            exactRelease.ReleaseApplied &&
            exactRelease.Reason ==
                "CLAIM_RELEASED" &&
            exactRelease.StateBefore ==
                "CLAIMED" &&
            exactRelease.StateAfter ==
                "PENDING" &&
            exactRelease.QueueCount == 1 &&
            releaseEnqueue.Job.State ==
                "PENDING" &&
            releaseEnqueue.Job.ProviderId == null &&
            releaseEnqueue.Job.AttemptId == null &&
            releaseEnqueue.Job.ClaimedUtcTicks == 0,
            "release_exact_to_pending");

        var releaseReplay =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            !releaseReplay.ReleaseApplied &&
            releaseReplay.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED" &&
            releaseEnqueue.Job.State == "PENDING",
            "release_replay_noop");

        long releaseSequence =
            releaseEnqueue.Job.Sequence;
        string releaseJson =
            releaseEnqueue.Job.DeliberationRequestJson;

        var retryClaim =
            releaseQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-2",
                802);
        Check(
            retryClaim.ClaimApplied &&
            retryClaim.Reason == "CLAIMED" &&
            releaseEnqueue.Job.State ==
                "CLAIMED" &&
            releaseEnqueue.Job.ProviderId ==
                "deterministic_external_worker" &&
            releaseEnqueue.Job.AttemptId ==
                "attempt-2" &&
            releaseEnqueue.Job.Sequence ==
                releaseSequence &&
            releaseEnqueue.Job.DeliberationRequestJson ==
                releaseJson,
            "release_retry_claim_same_job_identity");

        var retrySuccess =
            releaseQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            retrySuccess.CompletionApplied &&
            retrySuccess.Reason ==
                "SUCCESS_ACKED" &&
            releaseQueue.Count == 0,
            "release_retry_success_removes");

        var pendingQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        pendingQueue.Enqueue(
            changedRequest.RequestFingerprint,
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest),
            900);
        var pendingRelease =
            pendingQueue.ReleaseClaim(
                changedRequest.RequestFingerprint,
                "worker",
                "attempt");
        Check(
            !pendingRelease.ReleaseApplied &&
            pendingRelease.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED" &&
            pendingRelease.StateBefore == "PENDING" &&
            pendingRelease.StateAfter == "PENDING",
            "release_pending_rejected");

        string releaseReceipt =
            PatrolDefenseProviderDispatchBuilder.ToReleaseJson(
                exactRelease);
        Check(
            releaseReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderJobRelease.v1\"") &&
            releaseReceipt.Contains(
                "\"reason\":\"CLAIM_RELEASED\"") &&
            releaseReceipt.Contains(
                "\"stateBefore\":\"CLAIMED\"") &&
            releaseReceipt.Contains(
                "\"stateAfter\":\"PENDING\"") &&
            releaseReceipt.Contains(
                "\"releaseApplied\":true"),
            "release_receipt_exact");
        Check(
            releaseReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            releaseReceipt.Contains(
                "\"modelInvoked\":false") &&
            releaseReceipt.Contains(
                "\"executionAuthorized\":false") &&
            releaseReceipt.Contains(
                "\"behaviorMutation\":false") &&
            releaseReceipt.Contains(
                "\"scoreMutation\":false"),
            "release_receipt_zero_authority");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("release fixture anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02152 provider claim release + command + fixtures patched")
