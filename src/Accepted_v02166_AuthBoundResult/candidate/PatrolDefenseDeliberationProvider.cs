using System;
using System.Text;

namespace BannerlordAITestRunner
{
    internal interface IPatrolDefenseDeliberationProvider
    {
        string ProviderId { get; }

        string Generate(
            string requestFingerprint,
            string deliberationRequestJson);
    }

    internal sealed class PatrolDefenseMockDeliberationProvider :
        IPatrolDefenseDeliberationProvider
    {
        private readonly string _disposition;

        internal PatrolDefenseMockDeliberationProvider(
            string disposition)
        {
            _disposition = disposition;
        }

        public string ProviderId
        {
            get { return "deterministic_local_mock"; }
        }

        public string Generate(
            string requestFingerprint,
            string deliberationRequestJson)
        {
            return
                "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"" +
                ",\"requestFingerprint\":" +
                Json(requestFingerprint) +
                ",\"disposition\":" +
                Json(_disposition) +
                "}";
        }

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
    }

    internal sealed class PatrolDefenseProviderInvocationData
    {
        internal string ProviderId;
        internal bool ProviderInvoked;
        internal string RequestFingerprint;
        internal string RawAdvisoryJson;
        internal PatrolDefenseDeliberationAdvisoryAdmissionData Admission;
    }

    internal static class PatrolDefenseDeliberationProviderRuntime
    {
        internal static PatrolDefenseProviderInvocationData Invoke(
            IPatrolDefenseDeliberationProvider provider,
            PatrolDefenseAdvisoryRuntimeGate admissionGate,
            string submittedRequestFingerprint,
            string deliberationRequestJson)
        {
            PatrolDefenseProviderInvocationData result =
                new PatrolDefenseProviderInvocationData();

            result.ProviderId =
                provider == null
                    ? null
                    : provider.ProviderId;
            result.RequestFingerprint =
                submittedRequestFingerprint;

            if (admissionGate == null)
            {
                result.ProviderInvoked = false;
                result.Admission =
                    Rejected(
                        submittedRequestFingerprint,
                        "ADMISSION_GATE_MISSING");
                return result;
            }

            string pending =
                admissionGate.PendingRequestFingerprint;

            if (string.IsNullOrWhiteSpace(pending))
            {
                result.ProviderInvoked = false;
                result.Admission =
                    admissionGate.Evaluate(
                        submittedRequestFingerprint,
                        null);
                return result;
            }

            if (!string.Equals(
                    submittedRequestFingerprint,
                    pending,
                    StringComparison.Ordinal))
            {
                result.ProviderInvoked = false;
                result.Admission =
                    admissionGate.Evaluate(
                        submittedRequestFingerprint,
                        null);
                return result;
            }

            if (provider == null)
            {
                result.ProviderInvoked = false;
                result.Admission =
                    Rejected(
                        pending,
                        "PROVIDER_MISSING");
                return result;
            }

            try
            {
                result.ProviderInvoked = true;
                result.RawAdvisoryJson =
                    provider.Generate(
                        pending,
                        deliberationRequestJson);
            }
            catch (Exception ex)
            {
                result.ProviderInvoked = true;
                result.Admission =
                    Rejected(
                        pending,
                        "PROVIDER_EXCEPTION:" +
                        ex.GetType().Name);
                return result;
            }

            result.Admission =
                admissionGate.Evaluate(
                    pending,
                    result.RawAdvisoryJson);

            return result;
        }

        private static PatrolDefenseDeliberationAdvisoryAdmissionData
            Rejected(
                string requestFingerprint,
                string reason)
        {
            PatrolDefenseDeliberationAdvisoryAdmissionData data =
                new PatrolDefenseDeliberationAdvisoryAdmissionData();

            data.RequestFingerprint =
                requestFingerprint;
            data.Admitted = false;
            data.RejectionReasons.Add(reason);
            return data;
        }

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

        internal static string ToJson(
            PatrolDefenseProviderInvocationData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationProviderInvocation.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"providerId\":");
            sb.Append(Json(data.ProviderId));
            sb.Append(",\"providerInvoked\":");
            sb.Append(
                data.ProviderInvoked
                    ? "true"
                    : "false");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(
                Json(
                    data.RequestFingerprint));
            sb.Append(",\"rawAdvisory\":");
            sb.Append(
                string.IsNullOrWhiteSpace(
                    data.RawAdvisoryJson)
                    ? "null"
                    : data.RawAdvisoryJson);
            sb.Append(",\"admission\":");
            sb.Append(
                PatrolDefenseDeliberationAdvisoryAdmission.ToJson(
                    data.Admission));
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
    }
}
