from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\validate_v02167_execution_package.py")
t=p.read_text(encoding="utf-8-sig")

# Static external package boundary checks.
anchor='''    binding_source = (
        RUNNER_SRC / "PatrolDefenseProviderTransportReceiptResultV5Binding.cs"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
'''
repl='''    binding_source = (
        RUNNER_SRC / "PatrolDefenseProviderTransportReceiptResultV5Binding.cs"
    ).read_text(encoding="utf-8", errors="replace")
    package_source = Path(execution_package.__file__).read_text(
        encoding="utf-8", errors="replace"
    )

    static_checks = {
        "package_builder": "build_execution_package(" in package_source,
        "package_validator": "validate_execution_package(" in package_source,
        "adapter_boundary": "class DeterministicProviderAdapter" in package_source,
        "package_no_env_read": "os.environ" not in package_source
        and "getenv(" not in package_source,
        "package_no_http": "requests." not in package_source
        and "HttpClient" not in package_source,
        "package_no_socket": "socket." not in package_source
        and "Socket" not in package_source,
        "package_no_model_api": "OpenAI" not in package_source
        and "ChatCompletion" not in package_source,
'''
if anchor not in t:
    raise SystemExit("static anchor missing")
t=t.replace(anchor,repl,1)

# Offline package fixtures.
anchor='''    contract_fixture = run_text(
        [sys.executable, "-X", "utf8", str(CONTRACT_FIXTURE)],
        "PASS_FIXTURES checks=49",
    )
'''
repl=anchor+'''    package_fixture = run_text(
        [sys.executable, "-X", "utf8", str(PACKAGE_FIXTURE)],
        "PASS_EXECUTION_PACKAGE_FIXTURES checks=44",
    )
'''
if anchor not in t:
    raise SystemExit("fixture anchor missing")
t=t.replace(anchor,repl,1)

# Version-specific paths and evidence schema.
replacements={
    '"PatrolDefense_v02166_AuthorizationBoundResult_" + stamp':'"PatrolDefense_v02167_ExecutionPackage_" + stamp',
    '"Before_AuthorizationBoundResult_" + stamp':'"Before_ExecutionPackage_" + stamp',
    'operation = "authorization-bound-result-v02166-" + stamp':'operation = "execution-package-v02167-" + stamp',
    '"schema": "BannerlordAI.AuthorizationBoundResultValidation.v1"':'"schema": "BannerlordAI.ExecutionPackageValidation.v1"',
    '"patrol_defense_authorization_bound_result_shadow"':'"patrol_defense_execution_package_shadow"',
    'issuer="authorization_bound_result_live"':'issuer="execution_package_live"',
    '"v02166 authorization-bound result environmental inconclusive"':'"v02167 execution-package environmental inconclusive"',
}
for old,new in replacements.items():
    if old not in t:
        raise SystemExit("missing replacement: "+old)
    t=t.replace(old,new,1)

anchor='''        "request_contract_fixtures": contract_fixture,
        "static_checks": static_checks,
'''
repl='''        "request_contract_fixtures": contract_fixture,
        "execution_package_fixtures": package_fixture,
        "static_checks": static_checks,
'''
if anchor not in t:
    raise SystemExit("evidence fixture anchor missing")
t=t.replace(anchor,repl,1)

# Replace v66 stale-result live section with v67 external package/adapter proof.
start=t.index("            stale_outcome=loop.send_loopback_v5(")
end=t.index("            provider_rows=h.read_jsonl(PROVIDER_INVOCATIONS)",start)
live='''            stale_package, stale_package_json = execution_package.build_execution_package(
                request_json,
                transport_json,
                policy_json,
                authorization1,
            )
            stale_package_validation = execution_package.validate_execution_package(
                stale_package_json,
                authorization2,
            )
            adapter = execution_package.DeterministicProviderAdapter()
            stale_adapter = adapter.execute(
                stale_package_json,
                authorization2,
            )
            package_stale_checks = {
                "invalid": stale_package_validation.valid is False,
                "latest_id_reason":
                    "LATEST_AUTHORIZATION_ID_MISMATCH"
                    in stale_package_validation.reasons,
                "latest_ordinal_reason":
                    "LATEST_AUTHORIZATION_ORDINAL_MISMATCH"
                    in stale_package_validation.reasons,
                "adapter_not_invoked": stale_adapter.invoked is False,
                "adapter_count_zero": adapter.invocation_count == 0,
                "no_outcome": stale_adapter.outcome is None,
            }

            exact_package, exact_package_json = execution_package.build_execution_package(
                request_json,
                transport_json,
                policy_json,
                authorization2,
            )
            package_validation = execution_package.validate_execution_package(
                exact_package_json,
                authorization2,
            )
            if not package_validation.valid:
                raise RuntimeError(
                    "exact execution package invalid "
                    + json.dumps(package_validation.reasons)
                )

            adapter_result = adapter.execute(
                exact_package_json,
                authorization2,
            )
            if (
                not adapter_result.invoked
                or adapter_result.outcome is None
                or not adapter_result.outcome.success
            ):
                raise RuntimeError(
                    "deterministic adapter failed "
                    + json.dumps(
                        adapter_result.validation.reasons
                        if adapter_result.validation is not None
                        else ["NO_VALIDATION"]
                    )
                )
            outcome = adapter_result.outcome
            result = outcome.result
            receipt = outcome.receipt
            result_json = outcome.result_json
            receipt_json = outcome.receipt_json
            expected_result_sha = hashlib.sha256(
                result_json.encode("utf-8")
            ).hexdigest().upper()

            package_checks = {
                "schema":
                    exact_package.get("schema")
                    == "BannerlordAI.ProviderExecutionPackage.v1",
                "stable_id":
                    isinstance(exact_package.get("executionPackageId"), str)
                    and len(exact_package.get("executionPackageId")) == 64,
                "provider_id":
                    exact_package.get("providerRequestId")
                    == expected_provider_request_id,
                "transport_id":
                    exact_package.get("transportRequestId")
                    == expected_transport_request_id,
                "policy_id":
                    exact_package.get("executionPolicyId")
                    == expected_policy_id,
                "auth_id":
                    exact_package.get("transportExecutionAuthorizationId")
                    == authorization2.get("transportExecutionAuthorizationId"),
                "auth_ordinal":
                    exact_package.get("transportExecutionAttemptOrdinal") == 2,
                "auth_count":
                    exact_package.get("transportExecutionAttemptCount") == 2,
                "timeout":
                    exact_package.get("timeoutMs") == v61.TEST_TIMEOUT_MS,
                "max_attempts":
                    exact_package.get("maxAttempts") == "2",
                "max_result_bytes":
                    exact_package.get("maxResultBytes")
                    == v61.TEST_MAX_RESULT_BYTES,
                "credential_ref":
                    exact_package.get("credentialRef")
                    == v61.TEST_CREDENTIAL_REF,
                "validation": package_validation.valid is True,
                "no_reasons": package_validation.reasons == [],
                "adapter_invoked": adapter_result.invoked is True,
                "adapter_count_one": adapter.invocation_count == 1,
            }

            loop_checks = {
                "receipt_schema":
                    receipt.get("schema")
                    == "BannerlordAI.PatrolDefenseProviderTransportReceipt.v4",
                "result_schema":
                    result.get("schema")
                    == "BannerlordAI.PatrolDefenseProviderResult.v5",
                "policy":
                    receipt.get("executionPolicyId")
                    == result.get("executionPolicyId")
                    == expected_policy_id,
                "auth_id":
                    receipt.get("transportExecutionAuthorizationId")
                    == result.get("transportExecutionAuthorizationId")
                    == authorization2.get("transportExecutionAuthorizationId"),
                "auth_ordinal":
                    int(receipt.get("transportExecutionAttemptOrdinal"))
                    == int(result.get("transportExecutionAttemptOrdinal"))
                    == 2,
                "result_hash":
                    receipt.get("resultSha256") == expected_result_sha,
                "network_false":
                    receipt.get("externalNetworkUsed") is False,
                "model_false":
                    receipt.get("modelInvoked") is False,
                "success": receipt.get("success") is True,
            }

            command = (
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "
                + base64.b64encode(
                    receipt_json.encode("utf-8")
                ).decode("ascii")
                + " "
                + base64.b64encode(
                    result_json.encode("utf-8")
                ).decode("ascii")
            )
            before_binding = len(h.read_jsonl(BINDINGS_V5))
            before_result = len(h.read_jsonl(RESULT_V5))
            before_completion = len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_transport_result_v5_admitted",
                10,
            )
            binding_rows = h.read_jsonl(BINDINGS_V5)
            result_rows = h.read_jsonl(RESULT_V5)
            completion_rows = h.read_jsonl(COMPLETIONS)
            if (
                len(binding_rows) <= before_binding
                or len(result_rows) <= before_result
                or len(completion_rows) <= before_completion
            ):
                raise RuntimeError("exact v5 receipts missing")

            binding = binding_rows[-1]
            admitted = result_rows[-1]
            completion = completion_rows[-1]
            binding_checks = binding_success_checks(
                binding,
                receipt,
                result,
                result_json,
                policy_registration,
                authorization2,
            )
            result_checks = result_v5_checks(
                admitted,
                result,
                policy_registration,
                authorization2,
            )
            completion_checks = v49.completion_success_checks(
                completion,
                fingerprint,
            )

            before_replay_binding = len(binding_rows)
            before_replay_result = len(result_rows)
            before_replay_completion = len(completion_rows)
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_transport_result_v5_rejected:DISPATCH_JOB_NOT_FOUND",
                10,
            )
            replay_binding_rows = h.read_jsonl(BINDINGS_V5)
            replay_result_rows = h.read_jsonl(RESULT_V5)
            replay_completion_rows = h.read_jsonl(COMPLETIONS)
            if len(replay_binding_rows) <= before_replay_binding:
                raise RuntimeError("v5 replay binding missing")
            if len(replay_result_rows) != before_replay_result:
                raise RuntimeError(
                    "v5 replay unexpectedly admitted nested result"
                )
            if len(replay_completion_rows) <= before_replay_completion:
                raise RuntimeError("v5 replay completion missing")

            replay_binding = replay_binding_rows[-1]
            replay_completion = replay_completion_rows[-1]
            replay_binding_checks = binding_replay_checks(
                replay_binding,
                fingerprint,
            )
            replay_completion_checks = v49.completion_replay_checks(
                replay_completion,
                fingerprint,
            )

'''
t=t[:start]+live+t[end:]

# Replace checks/evidence tail to use package evidence instead of v66 stale runtime result.
old='''            checks.update({"authorization_"+k:v for k,v in authorization_checks.items()})
            checks.update({"stale_"+k:v for k,v in stale_checks.items()})
            checks.update({"loop_"+k:v for k,v in loop_checks.items()})
'''
new='''            checks.update({"authorization_"+k:v for k,v in authorization_checks.items()})
            checks.update({"package_stale_"+k:v for k,v in package_stale_checks.items()})
            checks.update({"package_"+k:v for k,v in package_checks.items()})
            checks.update({"loop_"+k:v for k,v in loop_checks.items()})
'''
if old not in t:
    raise SystemExit("checks tail anchor missing")
t=t.replace(old,new,1)

old='''            evidence["transport_execution_authorization_1"]=authorization1
            evidence["transport_execution_authorization_2"]=authorization2
            evidence["stale_transport_receipt"]=stale_outcome.receipt
            evidence["stale_provider_result_v5"]=stale_outcome.result
            evidence["stale_transport_result_v5_binding"]=stale_binding
            evidence["stale_dispatch_completion"]=stale_completion
            evidence["transport_receipt"]=receipt
'''
new='''            evidence["transport_execution_authorization_1"]=authorization1
            evidence["transport_execution_authorization_2"]=authorization2
            evidence["stale_execution_package"]=stale_package
            evidence["stale_execution_package_validation"]={
                "valid": stale_package_validation.valid,
                "reasons": stale_package_validation.reasons,
                "adapterInvoked": stale_adapter.invoked,
            }
            evidence["execution_package"]=exact_package
            evidence["execution_package_validation"]={
                "valid": package_validation.valid,
                "reasons": package_validation.reasons,
                "adapterInvoked": adapter_result.invoked,
                "adapterInvocationCount": adapter.invocation_count,
            }
            evidence["transport_receipt"]=receipt
'''
if old not in t:
    raise SystemExit("evidence tail anchor missing")
t=t.replace(old,new,1)

t=t.replace(
    'raise RuntimeError("v02166 live checks "+json.dumps(checks,sort_keys=True))',
    'raise RuntimeError("v02167 live checks "+json.dumps(checks,sort_keys=True))',
    1,
)
t=t.replace(
    'evidence["result"]="PASS_AUTHORIZATION_BOUND_RESULT_SHADOW"',
    'evidence["result"]="PASS_EXTERNAL_EXECUTION_PACKAGE_SHADOW"',
    1,
)
t=t.replace(
    'h.safe_exit(OWNER,token,run,"v02166 authorization-bound result verified")',
    'h.safe_exit(OWNER,token,run,"v02167 execution-package verified")',
    1,
)

# Terminal summary and closeout strings.
t=t.replace(
    'return 0 if evidence["result"] == "PASS_AUTHORIZATION_BOUND_RESULT_SHADOW" else 2',
    'return 0 if evidence["result"] == "PASS_EXTERNAL_EXECUTION_PACKAGE_SHADOW" else 2',
    1,
)
t=t.replace(
    '"v02166 authorization-bound result failure"',
    '"v02167 execution-package failure"',
    1,
)
t=t.replace(
    'h.base.bus.close_run("v02166 pre-ready failure")',
    'h.base.bus.close_run("v02167 pre-ready failure")',
    1,
)

# Keep terminal print package-focused and avoid stale v66 wording.
summary_old='''            "executionPolicyId": None if row is None else expected_policy_id,
            "timeoutMs": None if row is None else authorization2.get("timeoutMs"),
            "binding": None if row is None else binding.get("bindingAccepted"),
'''
summary_new='''            "executionPolicyId": None if row is None else expected_policy_id,
            "executionPackageId": None if row is None else exact_package.get("executionPackageId"),
            "adapterInvocationCount": None if row is None else adapter.invocation_count,
            "binding": None if row is None else binding.get("bindingAccepted"),
'''
if summary_old not in t:
    raise SystemExit("summary anchor missing")
t=t.replace(summary_old,summary_new,1)

p.write_text(t,encoding="utf-8")
print("v02167 validator patched")
