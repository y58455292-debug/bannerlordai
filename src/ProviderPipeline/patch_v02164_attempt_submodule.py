from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002164_MaxAttemptsPolicy_20260921_2218\candidate\SubModule.cs")
t=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseProviderExecutionPolicyRegistrationPath =
            Root + @"\\patrol_defense_provider_execution_policy_registrations.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderTransportExecutionAuthorizationPath =
            Root + @"\\patrol_defense_provider_transport_execution_authorizations.jsonl";
'''
if "PatrolDefenseProviderTransportExecutionAuthorizationPath" not in t:
    if path_anchor not in t:
        raise SystemExit("authorization path anchor missing")
    t=t.replace(path_anchor,path_repl,1)

cmd_anchor='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT "))
'''
cmd='''                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_execution_authorization_invalid"
                        : TryAuthorizePatrolDefenseProviderTransportExecution(
                            parts[0],
                            parts[1]);
                }
'''
if "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE " not in t:
    if cmd_anchor not in t:
        raise SystemExit("authorization command anchor missing")
    t=t.replace(cmd_anchor,cmd+cmd_anchor,1)

method_anchor='''        private string TryAdmitPatrolDefenseProviderTransportResultV4(
'''
method=r'''        private string TryAuthorizePatrolDefenseProviderTransportExecution(
            string requestFingerprint,
            string executionPolicyId)
        {
            PatrolDefenseProviderTransportExecutionAuthorizationResult authorization =
                _patrolDefenseProviderDispatchQueue.AuthorizeTransportExecution(
                    requestFingerprint,
                    executionPolicyId);

            string receipt =
                PatrolDefenseProviderDispatchBuilder.ToTransportExecutionAuthorizationJson(
                    authorization);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportExecutionAuthorizationPath,
                    receipt + Environment.NewLine);
            }

            bool authorized =
                authorization != null &&
                authorization.Authorized;

            string reason =
                authorization == null
                    ? "REJECTED"
                    : authorization.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZATION" +
                " transportExecutionAuthorizationId=" +
                Clean(
                    authorization == null
                        ? null
                        : authorization.TransportExecutionAuthorizationId) +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " executionPolicyId=" +
                Clean(executionPolicyId) +
                " attemptOrdinal=" +
                (authorization == null
                    ? "<null>"
                    : authorization.AttemptOrdinal.ToString(
                        CultureInfo.InvariantCulture)) +
                " attemptCount=" +
                (authorization == null
                    ? "<null>"
                    : authorization.AttemptCount.ToString(
                        CultureInfo.InvariantCulture)) +
                " maxAttempts=" +
                (authorization == null
                    ? "<null>"
                    : authorization.MaxAttempts.ToString(
                        CultureInfo.InvariantCulture)) +
                " authorized=" +
                (authorized ? "True" : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return authorized
                ? "patrol_defense_provider_transport_execution_authorized"
                : "patrol_defense_provider_transport_execution_rejected:" +
                    reason;
        }


'''
if "private string TryAuthorizePatrolDefenseProviderTransportExecution(" not in t:
    if method_anchor not in t:
        raise SystemExit("authorization method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

p.write_text(t,encoding="utf-8")
print("v02164 SubModule execution authorization command/receipt wired")
