from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002164_MaxAttemptsPolicy_20260921_2218")
p=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=p.read_text(encoding="utf-8-sig")

if "using System.Security.Cryptography;" not in t:
    t=t.replace("using System.Globalization;\n", "using System.Globalization;\nusing System.Security.Cryptography;\n",1)

job_anchor='''        internal string ExecutionPolicyCredentialRef;
'''
job_repl='''        internal string ExecutionPolicyCredentialRef;
        internal int TransportExecutionAttemptCount;
        internal int LastTransportExecutionAttemptOrdinal;
        internal string LastTransportExecutionAuthorizationId;
'''
if "TransportExecutionAttemptCount" not in t:
    if job_anchor not in t:
        raise SystemExit("attempt job field anchor missing")
    t=t.replace(job_anchor,job_repl,1)

# Add result type after size policy type.
type_anchor='''    internal sealed class PatrolDefenseProviderResultSizePolicyResult
    {
        internal string RequestFingerprint;
        internal string ExecutionPolicyId;
        internal int ResultByteCount;
        internal int MaxResultBytes;
        internal bool Allowed;
        internal string Reason;
    }

'''
type_repl=type_anchor+'''    internal sealed class PatrolDefenseProviderTransportExecutionAuthorizationResult
    {
        internal string TransportExecutionAuthorizationId;
        internal string RequestFingerprint;
        internal string ExecutionPolicyId;
        internal int AttemptOrdinal;
        internal int AttemptCount;
        internal int MaxAttempts;
        internal bool Authorized;
        internal string Reason;
    }

'''
if "PatrolDefenseProviderTransportExecutionAuthorizationResult" not in t:
    if type_anchor not in t:
        raise SystemExit("attempt result type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

# Insert helper/method before result size evaluation.
method_anchor='''        internal PatrolDefenseProviderResultSizePolicyResult
            EvaluateRegisteredResultSizePolicy(
'''
method=r'''        private static string Sha256Upper(
            byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder sb =
                    new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(
                        hash[i].ToString(
                            "X2",
                            CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }

        private static void AppendLengthDelimited(
            StringBuilder sb,
            string value)
        {
            if (value == null)
            {
                sb.Append("-1:;");
                return;
            }

            byte[] bytes =
                Encoding.UTF8.GetBytes(value);
            sb.Append(
                bytes.Length.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(":");
            sb.Append(value);
            sb.Append(";");
        }

        internal PatrolDefenseProviderTransportExecutionAuthorizationResult
            AuthorizeTransportExecution(
                string requestFingerprint,
                string executionPolicyId)
        {
            PatrolDefenseProviderTransportExecutionAuthorizationResult result =
                new PatrolDefenseProviderTransportExecutionAuthorizationResult();

            result.RequestFingerprint =
                requestFingerprint;
            result.ExecutionPolicyId =
                executionPolicyId;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(executionPolicyId))
            {
                result.Authorized = false;
                result.Reason =
                    "EXECUTION_ATTEMPT_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Authorized = false;
                result.Reason =
                    "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                result.Authorized = false;
                result.Reason =
                    "EXECUTION_POLICY_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal))
            {
                result.Authorized = false;
                result.Reason =
                    "EXECUTION_POLICY_ID_MISMATCH";
                return result;
            }

            int maxAttempts;
            if (!int.TryParse(
                    job.ExecutionPolicyMaxAttempts,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out maxAttempts) ||
                maxAttempts <= 0)
            {
                result.Authorized = false;
                result.Reason =
                    "EXECUTION_ATTEMPT_POLICY_INVALID";
                return result;
            }

            result.MaxAttempts =
                maxAttempts;
            result.AttemptCount =
                job.TransportExecutionAttemptCount;

            if (job.TransportExecutionAttemptCount >=
                maxAttempts)
            {
                result.Authorized = false;
                result.AttemptOrdinal =
                    job.TransportExecutionAttemptCount + 1;
                result.Reason =
                    "EXECUTION_ATTEMPT_LIMIT_EXCEEDED";
                return result;
            }

            int ordinal =
                job.TransportExecutionAttemptCount + 1;

            StringBuilder material =
                new StringBuilder();
            AppendLengthDelimited(
                material,
                requestFingerprint);
            AppendLengthDelimited(
                material,
                executionPolicyId);
            AppendLengthDelimited(
                material,
                ordinal.ToString(
                    CultureInfo.InvariantCulture));

            string authorizationId =
                Sha256Upper(
                    Encoding.UTF8.GetBytes(
                        material.ToString()));

            job.TransportExecutionAttemptCount =
                ordinal;
            job.LastTransportExecutionAttemptOrdinal =
                ordinal;
            job.LastTransportExecutionAuthorizationId =
                authorizationId;

            result.TransportExecutionAuthorizationId =
                authorizationId;
            result.AttemptOrdinal =
                ordinal;
            result.AttemptCount =
                job.TransportExecutionAttemptCount;
            result.Authorized = true;
            result.Reason =
                "EXECUTION_ATTEMPT_AUTHORIZED";
            return result;
        }


'''
if "AuthorizeTransportExecution(" not in t:
    if method_anchor not in t:
        raise SystemExit("attempt method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

# Clear on release.
release_anchor='''            job.ExecutionPolicyCredentialRef = null;
'''
release_repl='''            job.ExecutionPolicyCredentialRef = null;
            job.TransportExecutionAttemptCount = 0;
            job.LastTransportExecutionAttemptOrdinal = 0;
            job.LastTransportExecutionAuthorizationId = null;
'''
if "job.TransportExecutionAttemptCount = 0;" not in t:
    if release_anchor not in t:
        raise SystemExit("attempt release clear anchor missing")
    t=t.replace(release_anchor,release_repl,1)

# Add receipt builder before completion json.
builder_anchor='''        internal static string ToCompletionJson(
'''
builder=r'''        internal static string ToTransportExecutionAuthorizationJson(
            PatrolDefenseProviderTransportExecutionAuthorizationResult result)
        {
            if (result == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"transportExecutionAuthorizationId\":");
            sb.Append(Json(result.TransportExecutionAuthorizationId));
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(result.RequestFingerprint));
            sb.Append(",\"executionPolicyId\":");
            sb.Append(Json(result.ExecutionPolicyId));
            sb.Append(",\"attemptOrdinal\":");
            sb.Append(
                result.AttemptOrdinal.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"attemptCount\":");
            sb.Append(
                result.AttemptCount.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"maxAttempts\":");
            sb.Append(
                result.MaxAttempts.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"authorized\":");
            sb.Append(
                result.Authorized
                    ? "true"
                    : "false");
            sb.Append(",\"reason\":");
            sb.Append(Json(result.Reason));
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
if "ToTransportExecutionAuthorizationJson(" not in t:
    if builder_anchor not in t:
        raise SystemExit("attempt receipt builder anchor missing")
    t=t.replace(builder_anchor,builder+builder_anchor,1)

p.write_text(t,encoding="utf-8")
print("v02164 queue attempt authorization + receipt + release clear patched")
