from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\validate_v02165_timeout_budget.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\validate_v02166_authorization_bound_result.py")
t=src.read_text(encoding="utf-8-sig")

t=t.replace("import provider_transport_loopback_v02165 as loop","import provider_transport_loopback_v02166 as loop")
t=t.replace(r'RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002165_TimeoutBudget_20260921_2317\candidate"',r'RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\candidate"')
t=t.replace('RUNNER_SHA = "5E92F65137C6DBDF114BD77613CE5F384E7B240B496ACC6D7C7E39BDA07ADEB5"','RUNNER_SHA = "D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401"')
t=t.replace('OWNER = "sol_timeout_budget_v02165"','OWNER = "sol_auth_bound_result_v02166"')
t=t.replace('SOURCE_PREFIX = "ClanAI V02165 TIMEOUT BUDGET SOURCE "','SOURCE_PREFIX = "ClanAI V02166 AUTH BOUND RESULT SOURCE "')
t=t.replace(r'AutonomousOperator_v002165_TimeoutBudget_20260921_2317',r'AutonomousOperator_v002166_AuthBoundResult_20260921_2331')
t=t.replace(r'LOOPBACK_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02165.py"',r'LOOPBACK_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02166.py"')
t=t.replace('V4_FIXTURES = ROOT / r"workspace\\_PatchStaging\\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\\fixtures_v4\\FixturesV4.csproj"',
'''V4_FIXTURES = ROOT / r"workspace\\_PatchStaging\\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\\fixtures_v4\\FixturesV4.csproj"
V5_FIXTURES = ROOT / r"workspace\\_PatchStaging\\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\\fixtures_v5\\FixturesV5.csproj"''')
t=t.replace('RESULT_V4 = ROOT / r"Automation\\TestRunner\\patrol_defense_provider_result_v4_admissions.jsonl"\nBINDINGS_V4 = ROOT / r"Automation\\TestRunner\\patrol_defense_provider_transport_result_v4_bindings.jsonl"',
'''RESULT_V5 = ROOT / r"Automation\\TestRunner\\patrol_defense_provider_result_v5_admissions.jsonl"
BINDINGS_V5 = ROOT / r"Automation\\TestRunner\\patrol_defense_provider_transport_result_v5_bindings.jsonl"''')

fn_start=t.index("def binding_success_checks(")
main_start=t.index("def main():",fn_start)
functions=r'''def binding_success_checks(row, receipt, result, result_json, policy_registration, authorization):
    nested=row.get("providerResultAdmission")
    expected_sha=hashlib.sha256(result_json.encode("utf-8")).hexdigest().upper()
    return {
        "schema": row.get("schema")=="BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1",
        "mode": row.get("mode")=="observe",
        "transport_id": row.get("transportId")==receipt.get("transportId"),
        "transport_request": row.get("transportRequestId")==receipt.get("transportRequestId")==result.get("transportRequestId"),
        "provider_request": row.get("providerRequestId")==receipt.get("providerRequestId")==result.get("providerRequestId"),
        "provider": row.get("providerId")==receipt.get("providerId"),
        "model": row.get("modelId")==receipt.get("modelId"),
        "attempt": row.get("attemptId")==receipt.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")==receipt.get("requestFingerprint"),
        "input_sha": row.get("requestInputSha256")==receipt.get("requestInputSha256"),
        "status": row.get("resultStatus")==receipt.get("resultStatus")==result.get("status"),
        "execution_policy": row.get("executionPolicyId")==receipt.get("executionPolicyId")==result.get("executionPolicyId")==policy_registration.get("executionPolicyId"),
        "auth_id": row.get("transportExecutionAuthorizationId")==receipt.get("transportExecutionAuthorizationId")==result.get("transportExecutionAuthorizationId")==authorization.get("transportExecutionAuthorizationId"),
        "auth_ordinal": row.get("transportExecutionAttemptOrdinal")==int(receipt.get("transportExecutionAttemptOrdinal"))==int(result.get("transportExecutionAttemptOrdinal"))==authorization.get("attemptOrdinal"),
        "receipt_sha": row.get("resultSha256")==expected_sha,
        "computed_sha": row.get("computedResultSha256")==expected_sha,
        "receipt_match": row.get("transportReceiptMatchReason")=="TRANSPORT_RECEIPT_MATCHED",
        "policy_match": row.get("executionPolicyMatchReason")=="EXECUTION_POLICY_MATCHED",
        "auth_match": row.get("transportExecutionAuthorizationMatchReason")=="EXECUTION_AUTHORIZATION_MATCHED",
        "size_reason": row.get("resultSizePolicyReason")=="RESULT_SIZE_WITHIN_LIMIT",
        "size_count": row.get("resultByteCount")==len(result_json.encode("utf-8")),
        "size_max": row.get("maxResultBytes")==int(v61.TEST_MAX_RESULT_BYTES),
        "binding": row.get("bindingAccepted") is True,
        "no_rejections": row.get("rejectionReasons")==[],
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "receipt_success": row.get("receiptSuccess") is True,
        "error_null": row.get("errorCode") is None,
        "nested_present": isinstance(nested,dict),
        "execution_false": row.get("executionAuthorized") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "intent_false": row.get("intentMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls")==0,
    }

def result_v5_checks(row, result, policy_registration, authorization):
    advisory=row.get("advisoryAdmission")
    return {
        "schema": row.get("schema")=="BannerlordAI.PatrolDefenseProviderResultV5Admission.v1",
        "mode": row.get("mode")=="observe",
        "provider": row.get("providerId")==result.get("providerId"),
        "attempt": row.get("attemptId")==result.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")==result.get("requestFingerprint"),
        "provider_request": row.get("providerRequestId")==result.get("providerRequestId"),
        "transport_request": row.get("transportRequestId")==result.get("transportRequestId"),
        "execution_policy": row.get("executionPolicyId")==result.get("executionPolicyId")==policy_registration.get("executionPolicyId"),
        "auth_id": row.get("transportExecutionAuthorizationId")==result.get("transportExecutionAuthorizationId")==authorization.get("transportExecutionAuthorizationId"),
        "auth_ordinal": row.get("transportExecutionAttemptOrdinal")==int(result.get("transportExecutionAttemptOrdinal"))==authorization.get("attemptOrdinal"),
        "status": row.get("status")=="SUCCESS",
        "claim_match": row.get("claimMatchReason")=="CLAIM_MATCHED",
        "provider_request_match": row.get("providerRequestMatchReason")=="PROVIDER_REQUEST_MATCHED",
        "transport_request_match": row.get("transportRequestMatchReason")=="TRANSPORT_REQUEST_MATCHED",
        "execution_policy_match": row.get("executionPolicyMatchReason")=="EXECUTION_POLICY_MATCHED",
        "authorization_match": row.get("transportExecutionAuthorizationMatchReason")=="EXECUTION_AUTHORIZATION_MATCHED",
        "accepted": row.get("providerResultAccepted") is True,
        "retryable_false": row.get("retryable") is False,
        "no_rejections": row.get("rejectionReasons")==[],
        "advisory_admitted": isinstance(advisory,dict) and advisory.get("admitted") is True,
        "disposition": isinstance(advisory,dict) and advisory.get("disposition")=="KEEP_BASELINE",
        "execution_false": row.get("executionAuthorized") is False,
        "model_false": row.get("modelInvoked") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls")==0,
    }

def binding_replay_checks(row,fingerprint):
    reasons=row.get("rejectionReasons")
    return {
        "schema": row.get("schema")=="BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1",
        "fingerprint": row.get("requestFingerprint")==fingerprint,
        "binding_false": row.get("bindingAccepted") is False,
        "job_missing_match": row.get("transportReceiptMatchReason")=="DISPATCH_JOB_NOT_FOUND",
        "job_missing_reason": isinstance(reasons,list) and "DISPATCH_JOB_NOT_FOUND" in reasons,
        "nested_null": row.get("providerResultAdmission") is None,
        "execution_false": row.get("executionAuthorized") is False,
        "movement_zero": row.get("nativeMovementCalls")==0,
    }

'''
t=t[:fn_start]+functions+t[main_start:]

t=t.replace('(RUNNER_SRC / "PatrolDefenseProviderResultV4Admission.cs")','(RUNNER_SRC / "PatrolDefenseProviderResultV5Admission.cs")')
t=t.replace('RUNNER_SRC / "PatrolDefenseProviderTransportReceiptResultV4Binding.cs"','RUNNER_SRC / "PatrolDefenseProviderTransportReceiptResultV5Binding.cs"')
t=t.replace('"v4_command": "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT "',
            '"v5_command": "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "')
t=t.replace('"v4_result_path": "patrol_defense_provider_result_v4_admissions.jsonl"',
            '"v5_result_path": "patrol_defense_provider_result_v5_admissions.jsonl"')
t=t.replace('"v4_binding_path":\n        "patrol_defense_provider_transport_result_v4_bindings.jsonl"',
            '"v5_binding_path":\n        "patrol_defense_provider_transport_result_v5_bindings.jsonl"')
t=t.replace('"result_schema": "BannerlordAI.PatrolDefenseProviderResult.v4"',
            '"result_schema": "BannerlordAI.PatrolDefenseProviderResult.v5"')
t=t.replace('"receipt_schema": "BannerlordAI.PatrolDefenseProviderTransportReceipt.v3"',
            '"receipt_schema": "BannerlordAI.PatrolDefenseProviderTransportReceipt.v4"')
t=t.replace('"BannerlordAI.PatrolDefenseProviderTransportReceiptResultV4Binding.v1"',
            '"BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1"')
t=t.replace('"PASS_FIXTURES checks=57"','"PASS_FIXTURES checks=63"')
fixture_anchor='''    loop_fixture = run_text(
        [sys.executable, "-X", "utf8", str(LOOPBACK_FIXTURE)],
        "PASS_FIXTURES checks=63",
    )
'''
if "v5_fixture = run_text" not in t:
    t=t.replace(fixture_anchor,'''    v5_fixture = run_text(
        ["dotnet", "run", "--project", str(V5_FIXTURES), "-c", "Release"],
        "PASS_V5_FIXTURES checks=40",
    )
'''+fixture_anchor,1)

t=t.replace('"PatrolDefense_v02165_TimeoutBudget_" + stamp','"PatrolDefense_v02166_AuthorizationBoundResult_" + stamp')
t=t.replace('"Before_TimeoutBudget_" + stamp','"Before_AuthorizationBoundResult_" + stamp')
t=t.replace('operation = "timeout-budget-v02165-" + stamp','operation = "authorization-bound-result-v02166-" + stamp')
t=t.replace('"schema": "BannerlordAI.TimeoutBudgetBindingValidation.v1"','"schema": "BannerlordAI.AuthorizationBoundResultValidation.v1"')
t=t.replace('"loopback_v4_fixtures": loop_fixture,','"v5_fixtures": v5_fixture,\n        "loopback_v5_fixtures": loop_fixture,')
t=t.replace('"patrol_defense_timeout_budget_binding_shadow"','"patrol_defense_authorization_bound_result_shadow"')
t=t.replace('issuer="timeout_budget_binding_live"','issuer="authorization_bound_result_live"')
t=t.replace('"v02165 timeout-budget binding environmental inconclusive"','"v02166 authorization-bound result environmental inconclusive"')
t=t.replace("RESULT_V4,\n            BINDINGS_V4,","RESULT_V5,\n            BINDINGS_V5,")

live_start=t.index("            policy, policy_json = v61.policy_envelope(")
live_end=t.index("            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)",live_start)
live=r'''            policy, _ = v61.policy_envelope(
                request_registration,
                transport_registration,
                request_envelope,
            )
            policy["maxAttempts"]="2"
            policy_json=json.dumps(policy,separators=(",",":"),ensure_ascii=False)
            expected_policy_id=hashlib.sha256(policy_json.encode("utf-8")).hexdigest().upper()
            policy_cmd=(
                "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER "
                + base64.b64encode(policy_json.encode("utf-8")).decode("ascii")
            )
            before_policy=len(h.read_jsonl(POLICY_REGISTRATIONS))
            h.send_wait(OWNER,run,policy_cmd,"patrol_defense_provider_execution_policy_registered",10)
            policy_rows=h.read_jsonl(POLICY_REGISTRATIONS)
            if len(policy_rows)<=before_policy:
                raise RuntimeError("execution policy registration missing")
            policy_registration=policy_rows[-1]
            policy_checks={
                "schema":policy_registration.get("schema")=="BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1",
                "execution_policy_id":policy_registration.get("executionPolicyId")==expected_policy_id,
                "transport_request":policy_registration.get("transportRequestId")==expected_transport_request_id,
                "provider_request":policy_registration.get("providerRequestId")==expected_provider_request_id,
                "fingerprint":policy_registration.get("requestFingerprint")==fingerprint,
                "timeout":policy_registration.get("timeoutMs")==v61.TEST_TIMEOUT_MS,
                "max_attempts":policy_registration.get("maxAttempts")=="2",
                "max_result_bytes":policy_registration.get("maxResultBytes")==v61.TEST_MAX_RESULT_BYTES,
                "registered":policy_registration.get("registered") is True,
                "reason":policy_registration.get("reason")=="REGISTERED",
                "network_false":policy_registration.get("externalNetworkUsed") is False,
                "model_false":policy_registration.get("modelInvoked") is False,
            }

            before_authorization=len(h.read_jsonl(AUTHORIZATIONS))
            authorization_cmd=(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE "
                + fingerprint+" "+expected_policy_id
            )
            h.send_wait(OWNER,run,authorization_cmd,"patrol_defense_provider_transport_execution_authorized",10)
            h.send_wait(OWNER,run,authorization_cmd,"patrol_defense_provider_transport_execution_authorized",10)
            authorization_rows=h.read_jsonl(AUTHORIZATIONS)
            if len(authorization_rows)<before_authorization+2:
                raise RuntimeError("two transport execution authorizations missing")
            authorization1=authorization_rows[before_authorization]
            authorization2=authorization_rows[before_authorization+1]
            authorization_checks={
                "rows_two":len(authorization_rows)==before_authorization+2,
                "auth1":authorization1.get("authorized") is True and authorization1.get("reason")=="EXECUTION_ATTEMPT_AUTHORIZED",
                "auth2":authorization2.get("authorized") is True and authorization2.get("reason")=="EXECUTION_ATTEMPT_AUTHORIZED",
                "ordinals":authorization1.get("attemptOrdinal")==1 and authorization2.get("attemptOrdinal")==2,
                "counts":authorization1.get("attemptCount")==1 and authorization2.get("attemptCount")==2,
                "max_two":authorization1.get("maxAttempts")==2 and authorization2.get("maxAttempts")==2,
                "timeout":authorization1.get("timeoutMs")==1000 and authorization2.get("timeoutMs")==1000,
                "ids_distinct":authorization1.get("transportExecutionAuthorizationId")!=authorization2.get("transportExecutionAuthorizationId"),
                "network_false":authorization1.get("externalNetworkUsed") is False and authorization2.get("externalNetworkUsed") is False,
                "model_false":authorization1.get("modelInvoked") is False and authorization2.get("modelInvoked") is False,
            }

            stale_outcome=loop.send_loopback_v5(
                request_json,request_registration,transport_json,transport_registration,
                policy_registration,authorization1,
            )
            if not stale_outcome.success:
                raise RuntimeError("stale loopback construction failed "+json.dumps(stale_outcome.reasons))
            stale_command=(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "
                + base64.b64encode(stale_outcome.receipt_json.encode("utf-8")).decode("ascii")
                + " "
                + base64.b64encode(stale_outcome.result_json.encode("utf-8")).decode("ascii")
            )
            before_stale_binding=len(h.read_jsonl(BINDINGS_V5))
            before_stale_result=len(h.read_jsonl(RESULT_V5))
            before_stale_completion=len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,run,stale_command,
                "patrol_defense_provider_transport_result_v5_rejected:EXECUTION_AUTHORIZATION_ID_MISMATCH",10,
            )
            stale_bindings=h.read_jsonl(BINDINGS_V5)
            stale_results=h.read_jsonl(RESULT_V5)
            stale_completions=h.read_jsonl(COMPLETIONS)
            if len(stale_bindings)<=before_stale_binding:
                raise RuntimeError("stale v5 binding receipt missing")
            if len(stale_results)!=before_stale_result:
                raise RuntimeError("stale v5 unexpectedly admitted nested result")
            if len(stale_completions)<=before_stale_completion:
                raise RuntimeError("stale completion receipt missing")
            stale_binding=stale_bindings[-1]
            stale_completion=stale_completions[-1]
            stale_checks={
                "binding_false":stale_binding.get("bindingAccepted") is False,
                "auth_reason":stale_binding.get("transportExecutionAuthorizationMatchReason")=="EXECUTION_AUTHORIZATION_ID_MISMATCH",
                "reason_list":"EXECUTION_AUTHORIZATION_ID_MISMATCH" in (stale_binding.get("rejectionReasons") or []),
                "nested_null":stale_binding.get("providerResultAdmission") is None,
                "auth1_carried":stale_binding.get("transportExecutionAuthorizationId")==authorization1.get("transportExecutionAuthorizationId"),
                "ordinal1":stale_binding.get("transportExecutionAttemptOrdinal")==1,
                "completion_retained":stale_completion.get("reason")=="SUCCESS_NOT_ADMITTED_RETAINED",
                "queue_retained":stale_completion.get("queueCountBefore")==1 and stale_completion.get("queueCountAfter")==1,
                "pending_retained":stale_completion.get("pendingRetained") is True,
            }

            outcome=loop.send_loopback_v5(
                request_json,request_registration,transport_json,transport_registration,
                policy_registration,authorization2,
            )
            if not outcome.success:
                raise RuntimeError("exact loopback v5 failed "+json.dumps(outcome.reasons))
            result=outcome.result
            receipt=outcome.receipt
            result_json=outcome.result_json
            receipt_json=outcome.receipt_json
            expected_result_sha=hashlib.sha256(result_json.encode("utf-8")).hexdigest().upper()
            loop_checks={
                "receipt_schema":receipt.get("schema")=="BannerlordAI.PatrolDefenseProviderTransportReceipt.v4",
                "result_schema":result.get("schema")=="BannerlordAI.PatrolDefenseProviderResult.v5",
                "policy":receipt.get("executionPolicyId")==result.get("executionPolicyId")==expected_policy_id,
                "auth_id":receipt.get("transportExecutionAuthorizationId")==result.get("transportExecutionAuthorizationId")==authorization2.get("transportExecutionAuthorizationId"),
                "auth_ordinal":int(receipt.get("transportExecutionAttemptOrdinal"))==int(result.get("transportExecutionAttemptOrdinal"))==2,
                "result_hash":receipt.get("resultSha256")==expected_result_sha,
                "network_false":receipt.get("externalNetworkUsed") is False,
                "model_false":receipt.get("modelInvoked") is False,
                "success":receipt.get("success") is True,
            }
            command=(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "
                + base64.b64encode(receipt_json.encode("utf-8")).decode("ascii")
                + " "
                + base64.b64encode(result_json.encode("utf-8")).decode("ascii")
            )
            before_binding=len(h.read_jsonl(BINDINGS_V5))
            before_result=len(h.read_jsonl(RESULT_V5))
            before_completion=len(h.read_jsonl(COMPLETIONS))
            h.send_wait(OWNER,run,command,"patrol_defense_provider_transport_result_v5_admitted",10)
            binding_rows=h.read_jsonl(BINDINGS_V5)
            result_rows=h.read_jsonl(RESULT_V5)
            completion_rows=h.read_jsonl(COMPLETIONS)
            if len(binding_rows)<=before_binding or len(result_rows)<=before_result or len(completion_rows)<=before_completion:
                raise RuntimeError("exact v5 receipts missing")
            binding=binding_rows[-1]
            admitted=result_rows[-1]
            completion=completion_rows[-1]
            binding_checks=binding_success_checks(binding,receipt,result,result_json,policy_registration,authorization2)
            result_checks=result_v5_checks(admitted,result,policy_registration,authorization2)
            completion_checks=v49.completion_success_checks(completion,fingerprint)

            before_replay_binding=len(binding_rows)
            before_replay_result=len(result_rows)
            before_replay_completion=len(completion_rows)
            h.send_wait(
                OWNER,run,command,
                "patrol_defense_provider_transport_result_v5_rejected:DISPATCH_JOB_NOT_FOUND",10,
            )
            replay_binding_rows=h.read_jsonl(BINDINGS_V5)
            replay_result_rows=h.read_jsonl(RESULT_V5)
            replay_completion_rows=h.read_jsonl(COMPLETIONS)
            if len(replay_binding_rows)<=before_replay_binding:
                raise RuntimeError("v5 replay binding missing")
            if len(replay_result_rows)!=before_replay_result:
                raise RuntimeError("v5 replay unexpectedly admitted nested result")
            if len(replay_completion_rows)<=before_replay_completion:
                raise RuntimeError("v5 replay completion missing")
            replay_binding=replay_binding_rows[-1]
            replay_completion=replay_completion_rows[-1]
            replay_binding_checks=binding_replay_checks(replay_binding,fingerprint)
            replay_completion_checks=v49.completion_replay_checks(replay_completion,fingerprint)

'''
t=t[:live_start]+live+t[live_end:]

tail_start=t.index("            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)",live_start)
tail_end=t.index("            run = None\n\n        for name in binaries:",tail_start)+len("            run = None\n")
tail=r'''            provider_rows=h.read_jsonl(PROVIDER_INVOCATIONS)
            checks={}
            checks.update({"claim_"+k:v for k,v in claim_checks.items()})
            checks.update({"request_"+k:v for k,v in request_checks.items()})
            checks.update({"transport_"+k:v for k,v in transport_checks.items()})
            checks.update({"policy_"+k:v for k,v in policy_checks.items()})
            checks.update({"authorization_"+k:v for k,v in authorization_checks.items()})
            checks.update({"stale_"+k:v for k,v in stale_checks.items()})
            checks.update({"loop_"+k:v for k,v in loop_checks.items()})
            checks.update({"binding_"+k:v for k,v in binding_checks.items()})
            checks.update({"result_"+k:v for k,v in result_checks.items()})
            checks.update({"completion_"+k:v for k,v in completion_checks.items()})
            checks.update({"replay_binding_"+k:v for k,v in replay_binding_checks.items()})
            checks.update({"replay_completion_"+k:v for k,v in replay_completion_checks.items()})
            checks["provider_invocations_empty"]=len(provider_rows)==0
            checks["no_apply_side_effects"]=h.no_apply_side_effects()
            if not all(checks.values()):
                raise RuntimeError("v02166 live checks "+json.dumps(checks,sort_keys=True))

            evidence["shadow_receipt"]=row
            evidence["provider_request_registration"]=request_registration
            evidence["transport_registration"]=transport_registration
            evidence["execution_policy_registration"]=policy_registration
            evidence["transport_execution_authorization_1"]=authorization1
            evidence["transport_execution_authorization_2"]=authorization2
            evidence["stale_transport_receipt"]=stale_outcome.receipt
            evidence["stale_provider_result_v5"]=stale_outcome.result
            evidence["stale_transport_result_v5_binding"]=stale_binding
            evidence["stale_dispatch_completion"]=stale_completion
            evidence["transport_receipt"]=receipt
            evidence["provider_result_v5"]=result
            evidence["transport_result_v5_binding"]=binding
            evidence["provider_result_v5_admission"]=admitted
            evidence["dispatch_completion"]=completion
            evidence["transport_result_v5_binding_replay"]=replay_binding
            evidence["dispatch_completion_replay"]=replay_completion
            evidence["provider_invocation_rows"]=len(provider_rows)
            evidence["live_checks"]=checks
            evidence["result"]="PASS_AUTHORIZATION_BOUND_RESULT_SHADOW"
            h.base.atomic_json(validation/"validation.json",evidence)
            h.safe_exit(OWNER,token,run,"v02166 authorization-bound result verified")
            run=None
'''
t=t[:tail_start]+tail+t[tail_end:]

t=t.replace('return 0 if evidence["result"] == "PASS_TIMEOUT_BUDGET_BINDING_SHADOW" else 2',
            'return 0 if evidence["result"] == "PASS_AUTHORIZATION_BOUND_RESULT_SHADOW" else 2')
t=t.replace('"v02165 timeout-budget binding failure"','"v02166 authorization-bound result failure"')
t=t.replace('h.base.bus.close_run("v02165 pre-ready failure")','h.base.bus.close_run("v02166 pre-ready failure")')
t=t.replace('"result": "PASS_TIMEOUT_BUDGET_BINDING_SHADOW"','"result": "PASS_AUTHORIZATION_BOUND_RESULT_SHADOW"')

dst.write_text(t,encoding="utf-8")
print(dst)
