using System;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseAdvisoryRuntimeGate
    {
        private string _pendingRequestFingerprint;

        internal string PendingRequestFingerprint
        {
            get { return _pendingRequestFingerprint; }
        }

        internal void Reset()
        {
            _pendingRequestFingerprint = null;
        }

        internal bool Arm(string requestFingerprint)
        {
            if (string.IsNullOrWhiteSpace(
                    requestFingerprint))
                return false;

            _pendingRequestFingerprint =
                requestFingerprint;
            return true;
        }

        internal PatrolDefenseDeliberationAdvisoryAdmissionData
            Evaluate(
                string submittedRequestFingerprint,
                string responseJson)
        {
            if (string.IsNullOrWhiteSpace(
                    _pendingRequestFingerprint))
            {
                PatrolDefenseDeliberationAdvisoryAdmissionData
                    none =
                        new PatrolDefenseDeliberationAdvisoryAdmissionData();
                none.RequestFingerprint =
                    submittedRequestFingerprint;
                none.Admitted = false;
                none.RejectionReasons.Add(
                    "NO_PENDING_ROUTE_REQUEST");
                return none;
            }

            if (!string.Equals(
                    submittedRequestFingerprint,
                    _pendingRequestFingerprint,
                    StringComparison.Ordinal))
            {
                PatrolDefenseDeliberationAdvisoryAdmissionData
                    mismatch =
                        new PatrolDefenseDeliberationAdvisoryAdmissionData();
                mismatch.RequestFingerprint =
                    _pendingRequestFingerprint;
                mismatch.Admitted = false;
                mismatch.RejectionReasons.Add(
                    "PENDING_REQUEST_FINGERPRINT_MISMATCH");
                return mismatch;
            }

            PatrolDefenseDeliberationAdvisoryAdmissionData
                admission =
                    PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                        _pendingRequestFingerprint,
                        responseJson);

            if (admission != null &&
                admission.Admitted)
            {
                _pendingRequestFingerprint = null;
            }

            return admission;
        }
    }
}
