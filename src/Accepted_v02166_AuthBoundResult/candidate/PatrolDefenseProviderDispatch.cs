using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseProviderDispatchJob
    {
        internal long Sequence;
        internal string RequestFingerprint;
        internal string DeliberationRequestJson;
        internal string State;
        internal long EnqueuedUtcTicks;
        internal string ProviderId;
        internal string AttemptId;
        internal long ClaimedUtcTicks;
        internal string ProviderRequestId;
        internal string ProviderRequestModelId;
        internal string ProviderRequestPromptContractVersion;
        internal string ProviderRequestResponseSchema;
        internal string ProviderRequestInputSha256;
        internal string TransportRequestId;
        internal string TransportId;
        internal string ExecutionPolicyId;
        internal string ExecutionPolicyTimeoutMs;
        internal string ExecutionPolicyMaxAttempts;
        internal string ExecutionPolicyMaxResultBytes;
        internal string ExecutionPolicyCredentialRef;
        internal int TransportExecutionAttemptCount;
        internal int LastTransportExecutionAttemptOrdinal;
        internal string LastTransportExecutionAuthorizationId;
    }

    internal sealed class PatrolDefenseProviderDispatchResult
    {
        internal bool Enqueued;
        internal string Reason;
        internal int QueueCount;
        internal int Capacity;
        internal PatrolDefenseProviderDispatchJob Job;
    }

    internal sealed class PatrolDefenseProviderDispatchCompletionResult
    {
        internal string RequestFingerprint;
        internal string ResultStatus;
        internal bool CompletionApplied;
        internal string Reason;
        internal int QueueCountBefore;
        internal int QueueCountAfter;
        internal bool PendingRetained;
    }

    internal sealed class PatrolDefenseProviderJobClaimResult
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

    internal sealed class PatrolDefenseProviderClaimMatchResult
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

    internal sealed class PatrolDefenseProviderJobReleaseResult
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

    internal sealed class PatrolDefenseProviderRequestRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string PromptContractVersion;
        internal string ResponseSchema;
        internal string InputSha256;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

    internal sealed class PatrolDefenseProviderRequestMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RegisteredProviderRequestId;
        internal bool Matched;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderTransportRegistrationResult
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

    internal sealed class PatrolDefenseProviderTransportMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string RegisteredTransportRequestId;
        internal bool Matched;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderTransportReceiptMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RequestInputSha256;
        internal string TransportId;
        internal string TransportRequestId;
        internal bool Matched;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderExecutionPolicyRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string TimeoutMs;
        internal string MaxAttempts;
        internal string MaxResultBytes;
        internal string CredentialRef;
        internal string ExecutionPolicyId;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

    internal sealed class PatrolDefenseProviderExecutionPolicyMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string ExecutionPolicyId;
        internal string RegisteredExecutionPolicyId;
        internal bool Matched;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderResultSizePolicyResult
    {
        internal string RequestFingerprint;
        internal string ExecutionPolicyId;
        internal int ResultByteCount;
        internal int MaxResultBytes;
        internal bool Allowed;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderTransportExecutionAuthorizationResult
    {
        internal string TransportExecutionAuthorizationId;
        internal string RequestFingerprint;
        internal string ExecutionPolicyId;
        internal int AttemptOrdinal;
        internal int AttemptCount;
        internal int MaxAttempts;
        internal int TimeoutMs;
        internal bool Authorized;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderTransportExecutionAuthorizationMatchResult
    {
        internal string RequestFingerprint;
        internal string ExecutionPolicyId;
        internal string TransportExecutionAuthorizationId;
        internal int AttemptOrdinal;
        internal string RegisteredExecutionPolicyId;
        internal string RegisteredTransportExecutionAuthorizationId;
        internal int RegisteredAttemptOrdinal;
        internal bool Matched;
        internal string Reason;
    }

    internal sealed class PatrolDefenseProviderDispatchQueue
    {
        private readonly int _capacity;
        private readonly Dictionary<string, PatrolDefenseProviderDispatchJob>
            _pending =
                new Dictionary<string, PatrolDefenseProviderDispatchJob>(
                    StringComparer.Ordinal);
        private long _sequence;

        internal PatrolDefenseProviderDispatchQueue(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("capacity");
            _capacity = capacity;
        }

        internal int Count
        {
            get { return _pending.Count; }
        }

        internal int Capacity
        {
            get { return _capacity; }
        }

        internal void Reset()
        {
            _pending.Clear();
        }

        internal PatrolDefenseProviderExecutionPolicyMatchResult
            MatchRegisteredExecutionPolicy(
                string requestFingerprint,
                string providerId,
                string attemptId,
                string providerRequestId,
                string transportRequestId,
                string executionPolicyId)
        {
            PatrolDefenseProviderExecutionPolicyMatchResult result =
                new PatrolDefenseProviderExecutionPolicyMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.TransportRequestId = transportRequestId;
            result.ExecutionPolicyId = executionPolicyId;

            PatrolDefenseProviderTransportMatchResult transportMatch =
                MatchRegisteredTransportRequest(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    providerRequestId,
                    transportRequestId);

            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                result.Matched = false;
                result.Reason =
                    transportMatch == null
                        ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                        : transportMatch.Reason;
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    executionPolicyId))
            {
                result.Matched = false;
                result.Reason =
                    "EXECUTION_POLICY_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason =
                    "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            result.ModelId =
                job.ProviderRequestModelId;

            result.RegisteredExecutionPolicyId =
                job.ExecutionPolicyId;

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                result.Matched = false;
                result.Reason =
                    "EXECUTION_POLICY_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "EXECUTION_POLICY_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason =
                "EXECUTION_POLICY_MATCHED";
            return result;
        }


        private static string Sha256Upper(
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

            int timeoutMs;
            if (!int.TryParse(
                    job.ExecutionPolicyTimeoutMs,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out timeoutMs) ||
                timeoutMs <= 0)
            {
                result.Authorized = false;
                result.Reason =
                    "EXECUTION_TIMEOUT_POLICY_INVALID";
                return result;
            }

            result.TimeoutMs =
                timeoutMs;

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

        internal PatrolDefenseProviderTransportExecutionAuthorizationMatchResult
            MatchRegisteredTransportExecutionAuthorization(
                string requestFingerprint,
                string executionPolicyId,
                string transportExecutionAuthorizationId,
                int attemptOrdinal)
        {
            PatrolDefenseProviderTransportExecutionAuthorizationMatchResult result =
                new PatrolDefenseProviderTransportExecutionAuthorizationMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ExecutionPolicyId = executionPolicyId;
            result.TransportExecutionAuthorizationId =
                transportExecutionAuthorizationId;
            result.AttemptOrdinal = attemptOrdinal;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(executionPolicyId) ||
                string.IsNullOrWhiteSpace(transportExecutionAuthorizationId) ||
                attemptOrdinal <= 0)
            {
                result.Matched = false;
                result.Reason = "EXECUTION_AUTHORIZATION_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(requestFingerprint, out job))
            {
                result.Matched = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            result.RegisteredExecutionPolicyId = job.ExecutionPolicyId;
            result.RegisteredTransportExecutionAuthorizationId =
                job.LastTransportExecutionAuthorizationId;
            result.RegisteredAttemptOrdinal =
                job.LastTransportExecutionAttemptOrdinal;

            if (string.IsNullOrWhiteSpace(job.ExecutionPolicyId))
            {
                result.Matched = false;
                result.Reason = "EXECUTION_POLICY_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason = "EXECUTION_POLICY_ID_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.LastTransportExecutionAuthorizationId) ||
                job.LastTransportExecutionAttemptOrdinal <= 0)
            {
                result.Matched = false;
                result.Reason = "EXECUTION_AUTHORIZATION_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.LastTransportExecutionAuthorizationId,
                    transportExecutionAuthorizationId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason = "EXECUTION_AUTHORIZATION_ID_MISMATCH";
                return result;
            }

            if (job.LastTransportExecutionAttemptOrdinal != attemptOrdinal)
            {
                result.Matched = false;
                result.Reason = "EXECUTION_AUTHORIZATION_ORDINAL_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason = "EXECUTION_AUTHORIZATION_MATCHED";
            return result;
        }


        internal PatrolDefenseProviderResultSizePolicyResult
            EvaluateRegisteredResultSizePolicy(
                string requestFingerprint,
                string executionPolicyId,
                int resultByteCount)
        {
            PatrolDefenseProviderResultSizePolicyResult result =
                new PatrolDefenseProviderResultSizePolicyResult();

            result.RequestFingerprint = requestFingerprint;
            result.ExecutionPolicyId = executionPolicyId;
            result.ResultByteCount = resultByteCount;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(executionPolicyId) ||
                resultByteCount < 0)
            {
                result.Allowed = false;
                result.Reason = "RESULT_SIZE_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Allowed = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                result.Allowed = false;
                result.Reason =
                    "EXECUTION_POLICY_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal))
            {
                result.Allowed = false;
                result.Reason =
                    "EXECUTION_POLICY_ID_MISMATCH";
                return result;
            }

            int maxResultBytes;
            if (!int.TryParse(
                    job.ExecutionPolicyMaxResultBytes,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out maxResultBytes) ||
                maxResultBytes <= 0)
            {
                result.Allowed = false;
                result.Reason =
                    "RESULT_SIZE_POLICY_INVALID";
                return result;
            }

            result.MaxResultBytes =
                maxResultBytes;

            if (resultByteCount >
                maxResultBytes)
            {
                result.Allowed = false;
                result.Reason =
                    "RESULT_SIZE_LIMIT_EXCEEDED";
                return result;
            }

            result.Allowed = true;
            result.Reason =
                "RESULT_SIZE_WITHIN_LIMIT";
            return result;
        }


        internal PatrolDefenseProviderExecutionPolicyRegistrationResult
            RegisterExecutionPolicy(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string transportRequestId,
                string timeoutMs,
                string maxAttempts,
                string maxResultBytes,
                string credentialRef,
                string executionPolicyId)
        {
            PatrolDefenseProviderExecutionPolicyRegistrationResult result =
                new PatrolDefenseProviderExecutionPolicyRegistrationResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.TransportRequestId = transportRequestId;
            result.TimeoutMs = timeoutMs;
            result.MaxAttempts = maxAttempts;
            result.MaxResultBytes = maxResultBytes;
            result.CredentialRef = credentialRef;
            result.ExecutionPolicyId = executionPolicyId;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(modelId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(providerRequestId) ||
                string.IsNullOrWhiteSpace(transportRequestId) ||
                string.IsNullOrWhiteSpace(timeoutMs) ||
                string.IsNullOrWhiteSpace(maxAttempts) ||
                string.IsNullOrWhiteSpace(maxResultBytes) ||
                string.IsNullOrWhiteSpace(credentialRef) ||
                string.IsNullOrWhiteSpace(executionPolicyId))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "INVALID_EXECUTION_POLICY";
                return result;
            }

            PatrolDefenseProviderTransportMatchResult transportMatch =
                MatchRegisteredTransportRequest(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    providerRequestId,
                    transportRequestId);

            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason =
                    transportMatch == null
                        ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                        : transportMatch.Reason;
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
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason =
                    "PROVIDER_REQUEST_MODEL_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                job.ExecutionPolicyId =
                    executionPolicyId;
                job.ExecutionPolicyTimeoutMs =
                    timeoutMs;
                job.ExecutionPolicyMaxAttempts =
                    maxAttempts;
                job.ExecutionPolicyMaxResultBytes =
                    maxResultBytes;
                job.ExecutionPolicyCredentialRef =
                    credentialRef;

                result.Registered = true;
                result.Idempotent = false;
                result.Reason = "REGISTERED";
                return result;
            }

            if (string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyTimeoutMs,
                    timeoutMs,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyMaxAttempts,
                    maxAttempts,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyMaxResultBytes,
                    maxResultBytes,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ExecutionPolicyCredentialRef,
                    credentialRef,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = true;
                result.Reason =
                    "SAME_EXECUTION_POLICY_ALREADY_REGISTERED";
                return result;
            }

            result.Registered = false;
            result.Idempotent = false;
            result.Reason =
                "EXECUTION_POLICY_ALREADY_REGISTERED";
            return result;
        }


        internal PatrolDefenseProviderTransportReceiptMatchResult
            MatchRegisteredTransportReceipt(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string requestInputSha256,
                string transportId,
                string transportRequestId)
        {
            PatrolDefenseProviderTransportReceiptMatchResult result =
                new PatrolDefenseProviderTransportReceiptMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.RequestInputSha256 = requestInputSha256;
            result.TransportId = transportId;
            result.TransportRequestId = transportRequestId;

            PatrolDefenseProviderTransportMatchResult transportMatch =
                MatchRegisteredTransportRequest(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    providerRequestId,
                    transportRequestId);

            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                result.Matched = false;
                result.Reason =
                    transportMatch == null
                        ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                        : transportMatch.Reason;
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_RECEIPT_MODEL_MISMATCH";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestInputSha256,
                    requestInputSha256,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_RECEIPT_INPUT_SHA_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(job.TransportId))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_REQUEST_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.TransportId,
                    transportId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_RECEIPT_TRANSPORT_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason = "TRANSPORT_RECEIPT_MATCHED";
            return result;
        }


        internal PatrolDefenseProviderTransportMatchResult
            MatchRegisteredTransportRequest(
                string requestFingerprint,
                string providerId,
                string attemptId,
                string providerRequestId,
                string transportRequestId)
        {
            PatrolDefenseProviderTransportMatchResult result =
                new PatrolDefenseProviderTransportMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.TransportRequestId = transportRequestId;

            PatrolDefenseProviderRequestMatchResult
                providerRequestMatch =
                    MatchRegisteredProviderRequest(
                        requestFingerprint,
                        providerId,
                        attemptId,
                        providerRequestId);

            if (providerRequestMatch == null ||
                !providerRequestMatch.Matched)
            {
                result.Matched = false;
                result.Reason =
                    providerRequestMatch == null
                        ? "PROVIDER_REQUEST_MATCH_UNAVAILABLE"
                        : providerRequestMatch.Reason;
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    transportRequestId))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_REQUEST_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason =
                    "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            result.RegisteredTransportRequestId =
                job.TransportRequestId;

            if (string.IsNullOrWhiteSpace(
                    job.TransportRequestId))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_REQUEST_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.TransportRequestId,
                    transportRequestId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_REQUEST_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason =
                "TRANSPORT_REQUEST_MATCHED";
            return result;
        }


        internal PatrolDefenseProviderTransportRegistrationResult
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


        internal PatrolDefenseProviderRequestMatchResult
            MatchRegisteredProviderRequest(
                string requestFingerprint,
                string providerId,
                string attemptId,
                string providerRequestId)
        {
            PatrolDefenseProviderRequestMatchResult result =
                new PatrolDefenseProviderRequestMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(providerRequestId))
            {
                result.Matched = false;
                result.Reason = "PROVIDER_REQUEST_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (!string.Equals(
                    job.State,
                    "CLAIMED",
                    StringComparison.Ordinal))
            {
                result.Matched = false;
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
                result.Matched = false;
                result.Reason = "PROVIDER_CLAIM_MISMATCH";
                return result;
            }

            result.RegisteredProviderRequestId =
                job.ProviderRequestId;

            if (string.IsNullOrWhiteSpace(
                    job.ProviderRequestId))
            {
                result.Matched = false;
                result.Reason =
                    "PROVIDER_REQUEST_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestId,
                    providerRequestId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "PROVIDER_REQUEST_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason =
                "PROVIDER_REQUEST_MATCHED";
            return result;
        }


        internal PatrolDefenseProviderRequestRegistrationResult
            RegisterProviderRequest(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string promptContractVersion,
                string responseSchema,
                string decodedRequestJson,
                string inputSha256,
                string providerRequestId)
        {
            PatrolDefenseProviderRequestRegistrationResult result =
                new PatrolDefenseProviderRequestRegistrationResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.PromptContractVersion = promptContractVersion;
            result.ResponseSchema = responseSchema;
            result.InputSha256 = inputSha256;
            result.QueueCount = _pending.Count;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(modelId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(promptContractVersion) ||
                string.IsNullOrWhiteSpace(responseSchema) ||
                string.IsNullOrWhiteSpace(decodedRequestJson) ||
                string.IsNullOrWhiteSpace(inputSha256) ||
                string.IsNullOrWhiteSpace(providerRequestId))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "INVALID_PROVIDER_REQUEST";
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

            if (!string.Equals(
                    job.DeliberationRequestJson,
                    decodedRequestJson,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = false;
                result.Reason = "DELIBERATION_REQUEST_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ProviderRequestId))
            {
                job.ProviderRequestId = providerRequestId;
                job.ProviderRequestModelId = modelId;
                job.ProviderRequestPromptContractVersion =
                    promptContractVersion;
                job.ProviderRequestResponseSchema =
                    responseSchema;
                job.ProviderRequestInputSha256 =
                    inputSha256;

                result.Registered = true;
                result.Idempotent = false;
                result.Reason = "REGISTERED";
                return result;
            }

            if (string.Equals(
                    job.ProviderRequestId,
                    providerRequestId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestPromptContractVersion,
                    promptContractVersion,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestResponseSchema,
                    responseSchema,
                    StringComparison.Ordinal) &&
                string.Equals(
                    job.ProviderRequestInputSha256,
                    inputSha256,
                    StringComparison.Ordinal))
            {
                result.Registered = false;
                result.Idempotent = true;
                result.Reason =
                    "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED";
                return result;
            }

            result.Registered = false;
            result.Idempotent = false;
            result.Reason =
                "PROVIDER_REQUEST_ALREADY_REGISTERED";
            return result;
        }


        internal PatrolDefenseProviderJobReleaseResult ReleaseClaim(
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
            job.ProviderRequestId = null;
            job.ProviderRequestModelId = null;
            job.ProviderRequestPromptContractVersion = null;
            job.ProviderRequestResponseSchema = null;
            job.ProviderRequestInputSha256 = null;
            job.TransportRequestId = null;
            job.TransportId = null;
            job.ExecutionPolicyId = null;
            job.ExecutionPolicyTimeoutMs = null;
            job.ExecutionPolicyMaxAttempts = null;
            job.ExecutionPolicyMaxResultBytes = null;
            job.ExecutionPolicyCredentialRef = null;
            job.TransportExecutionAttemptCount = 0;
            job.LastTransportExecutionAttemptOrdinal = 0;
            job.LastTransportExecutionAuthorizationId = null;

            result.ReleaseApplied = true;
            result.Reason = "CLAIM_RELEASED";
            result.StateAfter = job.State;
            return result;
        }


        internal PatrolDefenseProviderClaimMatchResult MatchClaim(
            string requestFingerprint,
            string providerId,
            string attemptId)
        {
            PatrolDefenseProviderClaimMatchResult result =
                new PatrolDefenseProviderClaimMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(attemptId))
            {
                result.Matched = false;
                result.Reason = "CLAIM_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            result.State = job.State;
            result.ClaimedProviderId = job.ProviderId;
            result.ClaimedAttemptId = job.AttemptId;

            if (!string.Equals(
                    job.State,
                    "CLAIMED",
                    StringComparison.Ordinal))
            {
                result.Matched = false;
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
                result.Matched = false;
                result.Reason = "PROVIDER_CLAIM_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason = "CLAIM_MATCHED";
            return result;
        }


        internal PatrolDefenseProviderJobClaimResult Claim(
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


        internal PatrolDefenseProviderDispatchCompletionResult
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


        internal PatrolDefenseProviderDispatchResult Enqueue(
            string requestFingerprint,
            string deliberationRequestJson,
            long utcTicks)
        {
            PatrolDefenseProviderDispatchResult result =
                new PatrolDefenseProviderDispatchResult();

            result.Capacity = _capacity;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(deliberationRequestJson))
            {
                result.Enqueued = false;
                result.Reason = "INVALID_REQUEST";
                result.QueueCount = _pending.Count;
                return result;
            }

            PatrolDefenseProviderDispatchJob existing;
            if (_pending.TryGetValue(requestFingerprint, out existing))
            {
                result.Enqueued = false;
                result.Reason = "DUPLICATE_REQUEST";
                result.QueueCount = _pending.Count;
                result.Job = existing;
                return result;
            }

            if (_pending.Count >= _capacity)
            {
                result.Enqueued = false;
                result.Reason = "QUEUE_FULL";
                result.QueueCount = _pending.Count;
                return result;
            }

            PatrolDefenseProviderDispatchJob job =
                new PatrolDefenseProviderDispatchJob();

            job.Sequence = ++_sequence;
            job.RequestFingerprint = requestFingerprint;
            job.DeliberationRequestJson = deliberationRequestJson;
            job.State = "PENDING";
            job.EnqueuedUtcTicks = utcTicks;

            _pending.Add(requestFingerprint, job);

            result.Enqueued = true;
            result.Reason = "ENQUEUED";
            result.QueueCount = _pending.Count;
            result.Job = job;
            return result;
        }
    }

    internal static class PatrolDefenseProviderDispatchBuilder
    {
        private static string Json(string value)
        {
            if (value == null)
                return "null";

            return "\"" +
                value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n") +
                "\"";
        }

        internal static string ToReceiptJson(
            PatrolDefenseProviderDispatchResult result,
            string requestFingerprint)
        {
            if (result == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatch.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(requestFingerprint));
            sb.Append(",\"enqueued\":");
            sb.Append(result.Enqueued ? "true" : "false");
            sb.Append(",\"reason\":");
            sb.Append(Json(result.Reason));
            sb.Append(",\"queueCount\":");
            sb.Append(
                result.QueueCount.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"capacity\":");
            sb.Append(
                result.Capacity.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"sequence\":");
            sb.Append(
                result.Job == null
                    ? "null"
                    : result.Job.Sequence.ToString(
                        CultureInfo.InvariantCulture));
            sb.Append(",\"providerInvoked\":false");
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

        internal static string ToReleaseJson(
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


        internal static string ToClaimJson(
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


        internal static string ToTransportExecutionAuthorizationJson(
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
            sb.Append(",\"timeoutMs\":");
            sb.Append(
                result.TimeoutMs.ToString(
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


        internal static string ToCompletionJson(
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


        internal static string ToOutboxJobJson(
            PatrolDefenseProviderDispatchJob job)
        {
            if (job == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatchJob.v1\"");
            sb.Append(",\"sequence\":");
            sb.Append(
                job.Sequence.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(job.RequestFingerprint));
            sb.Append(",\"state\":");
            sb.Append(Json(job.State));
            sb.Append(",\"enqueuedUtcTicks\":");
            sb.Append(
                job.EnqueuedUtcTicks.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"deliberationRequest\":");
            sb.Append(job.DeliberationRequestJson);
            sb.Append("}");

            return sb.ToString();
        }

        internal static string AttachToShadow(
            string shadowReceiptJson,
            string dispatchReceiptJson)
        {
            if (string.IsNullOrWhiteSpace(shadowReceiptJson) ||
                string.IsNullOrWhiteSpace(dispatchReceiptJson) ||
                shadowReceiptJson.Length < 2 ||
                shadowReceiptJson[shadowReceiptJson.Length - 1] != '}' ||
                dispatchReceiptJson[0] != '{')
                return shadowReceiptJson;

            return
                shadowReceiptJson.Substring(
                    0,
                    shadowReceiptJson.Length - 1) +
                ",\"providerDispatch\":" +
                dispatchReceiptJson +
                "}";
        }
    }
}
