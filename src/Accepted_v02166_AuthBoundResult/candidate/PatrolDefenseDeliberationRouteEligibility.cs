using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseDeliberationRouteEligibilityData
    {
        internal string RequestFingerprint;
        internal bool RouteEligible;
        internal string RouteReason;
        internal bool Duplicate;
        internal int SeenCount;
        internal int Capacity;
    }

    internal sealed class PatrolDefenseDeliberationRouteTracker
    {
        private readonly int _capacity;
        private readonly HashSet<string> _seen =
            new HashSet<string>(
                StringComparer.Ordinal);
        private readonly Queue<string> _order =
            new Queue<string>();

        internal PatrolDefenseDeliberationRouteTracker(
            int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("capacity");
            _capacity = capacity;
        }

        internal int Count
        {
            get { return _seen.Count; }
        }

        internal int Capacity
        {
            get { return _capacity; }
        }

        internal void Reset()
        {
            _seen.Clear();
            _order.Clear();
        }

        internal PatrolDefenseDeliberationRouteEligibilityData
            Evaluate(string requestFingerprint)
        {
            PatrolDefenseDeliberationRouteEligibilityData data =
                new PatrolDefenseDeliberationRouteEligibilityData();

            data.RequestFingerprint =
                requestFingerprint;
            data.Capacity =
                _capacity;

            if (string.IsNullOrWhiteSpace(
                    requestFingerprint))
            {
                data.RouteEligible = false;
                data.RouteReason = "INVALID_REQUEST";
                data.Duplicate = false;
                data.SeenCount = _seen.Count;
                return data;
            }

            if (_seen.Contains(
                    requestFingerprint))
            {
                data.RouteEligible = false;
                data.RouteReason = "DUPLICATE_REQUEST";
                data.Duplicate = true;
                data.SeenCount = _seen.Count;
                return data;
            }

            while (_seen.Count >= _capacity)
            {
                if (_order.Count == 0)
                    break;

                string evict =
                    _order.Dequeue();
                _seen.Remove(evict);
            }

            _seen.Add(requestFingerprint);
            _order.Enqueue(requestFingerprint);

            data.RouteEligible = true;
            data.RouteReason = "FIRST_UNSEEN_REQUEST";
            data.Duplicate = false;
            data.SeenCount = _seen.Count;
            return data;
        }
    }

    internal static class
        PatrolDefenseDeliberationRouteEligibilityBuilder
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

        internal static string ToJson(
            PatrolDefenseDeliberationRouteEligibilityData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationRouteEligibility.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(
                Json(
                    data.RequestFingerprint));
            sb.Append(",\"routeEligible\":");
            sb.Append(
                data.RouteEligible
                    ? "true"
                    : "false");
            sb.Append(",\"routeReason\":");
            sb.Append(
                Json(
                    data.RouteReason));
            sb.Append(",\"duplicate\":");
            sb.Append(
                data.Duplicate
                    ? "true"
                    : "false");
            sb.Append(",\"seenCount\":");
            sb.Append(
                data.SeenCount.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"capacity\":");
            sb.Append(
                data.Capacity.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"modelInvoked\":false");
            sb.Append(",\"llmInvoked\":false");
            sb.Append(",\"plannerInvoked\":false");
            sb.Append(",\"interpretationApplied\":false");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");

            return sb.ToString();
        }

        internal static string AttachToShadow(
            string shadowReceiptJson,
            string routeEligibilityJson)
        {
            if (string.IsNullOrWhiteSpace(
                    shadowReceiptJson) ||
                string.IsNullOrWhiteSpace(
                    routeEligibilityJson) ||
                shadowReceiptJson.Length < 2 ||
                shadowReceiptJson[
                    shadowReceiptJson.Length - 1] != '}' ||
                routeEligibilityJson[0] != '{')
            {
                return shadowReceiptJson;
            }

            return
                shadowReceiptJson.Substring(
                    0,
                    shadowReceiptJson.Length - 1) +
                ",\"deliberationRouteEligibility\":" +
                routeEligibilityJson +
                "}";
        }
    }
}
