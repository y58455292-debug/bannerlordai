from pathlib import Path
import json, os, time, uuid, hashlib

ROOT=Path(r"D:\BannerlordAIResearch")
CONTROL=ROOT/"Automation/Control"
RUNNER=ROOT/"Automation/TestRunner"
ACTIVE=CONTROL/"active_run.json"
LOG=CONTROL/"command_provenance.jsonl"
COMMAND=RUNNER/"command.txt"
META=RUNNER/"command_meta.json"
EXIT_AUTH=CONTROL/"exit_authorization.json"
DEPLOYMENT_OWNER=ROOT/"workspace/DEPLOYMENT_OWNER.json"

def _atomic_text(path, text):
    path.parent.mkdir(parents=True,exist_ok=True)
    tmp=path.with_suffix(path.suffix+".tmp")
    tmp.write_text(text,encoding="utf-8")
    os.replace(tmp,path)

def _load_active():
    if not ACTIVE.exists():
        return None
    return json.loads(ACTIVE.read_text(encoding="utf-8"))

def _load_deployment_owner():
    if not DEPLOYMENT_OWNER.exists():
        return None
    return json.loads(DEPLOYMENT_OWNER.read_text(encoding="utf-8"))

def start_run(issuer="rapid_manan_lab", expected_campaign_id="ZJtX6IZXozIG",
              expected_runner_version=None, run_id=None,
              deployment_owner=None, deployment_operation_id=None,
              exit_capability_sha256=None):
    run_id=run_id or ("manan-"+time.strftime("%Y%m%d-%H%M%S")+"-"+uuid.uuid4().hex[:8])
    rec={
        "schema":"BannerlordAI.ControlRun.v1",
        "run_id":run_id,
        "issuer":issuer,
        "created_unix":time.time(),
        "expected_campaign_id":expected_campaign_id,
        "expected_runner_version":expected_runner_version,
        "deployment_owner":deployment_owner,
        "deployment_operation_id":deployment_operation_id,
        "exit_capability_sha256":exit_capability_sha256,
        "sequence":0,
        "state":"ACTIVE"
    }
    _atomic_text(ACTIVE,json.dumps(rec,indent=2))
    for p in (COMMAND,META,EXIT_AUTH):
        try:p.unlink()
        except FileNotFoundError:pass
    return rec

def authorize_exit(issuer, reason, ttl_seconds=30, capability_token=None):
    active=_load_active()
    if not active or active.get("state")!="ACTIVE":
        raise RuntimeError("No active control run")
    owner=_load_deployment_owner()
    expected_owner=active.get("deployment_owner")
    expected_operation=active.get("deployment_operation_id")
    if not expected_owner or not expected_operation:
        raise RuntimeError("EXIT_NOSAVE denied: active run is not bound to deployment ownership")
    expected_capability=active.get("exit_capability_sha256")
    if expected_capability:
        if not capability_token:
            raise RuntimeError("EXIT_NOSAVE denied: capability token required")
        actual_capability=hashlib.sha256(
            capability_token.encode("utf-8")
        ).hexdigest().upper()
        if actual_capability!=expected_capability:
            raise RuntimeError("EXIT_NOSAVE denied: capability token mismatch")
    if issuer!=expected_owner:
        raise RuntimeError("EXIT_NOSAVE denied: issuer is not active deployment owner")
    if not owner:
        raise RuntimeError("EXIT_NOSAVE denied: no deployment lease")
    if owner.get("owner")!=expected_owner:
        raise RuntimeError("EXIT_NOSAVE denied: deployment owner changed")
    if owner.get("operation_id")!=expected_operation:
        raise RuntimeError("EXIT_NOSAVE denied: deployment operation changed")
    auth={
        "schema":"BannerlordAI.ExitAuthorization.v2",
        "run_id":active["run_id"],
        "issuer":issuer,
        "deployment_owner":expected_owner,
        "deployment_operation_id":expected_operation,
        "exit_capability_sha256":expected_capability,
        "reason":reason,
        "created_unix":time.time(),
        "expires_unix":time.time()+max(1,min(int(ttl_seconds),120))
    }
    _atomic_text(EXIT_AUTH,json.dumps(auth,indent=2))
    return auth

def _consume_exit_authorization(active, issuer):
    if not EXIT_AUTH.exists():
        raise RuntimeError("EXIT_NOSAVE denied: no exit authorization")
    auth=json.loads(EXIT_AUTH.read_text(encoding="utf-8"))
    if auth.get("run_id")!=active.get("run_id"):
        raise RuntimeError("EXIT_NOSAVE denied: wrong run authorization")
    if auth.get("issuer")!=issuer:
        raise RuntimeError("EXIT_NOSAVE denied: issuer not authorized")
    if auth.get("deployment_owner")!=active.get("deployment_owner"):
        raise RuntimeError("EXIT_NOSAVE denied: active deployment owner mismatch")
    if auth.get("deployment_operation_id")!=active.get("deployment_operation_id"):
        raise RuntimeError("EXIT_NOSAVE denied: active deployment operation mismatch")
    if auth.get("exit_capability_sha256")!=active.get("exit_capability_sha256"):
        raise RuntimeError("EXIT_NOSAVE denied: exit capability binding mismatch")
    owner=_load_deployment_owner()
    if not owner:
        raise RuntimeError("EXIT_NOSAVE denied: deployment lease missing")
    if owner.get("owner")!=active.get("deployment_owner"):
        raise RuntimeError("EXIT_NOSAVE denied: deployment owner changed")
    if owner.get("operation_id")!=active.get("deployment_operation_id"):
        raise RuntimeError("EXIT_NOSAVE denied: deployment operation changed")
    if time.time()>float(auth.get("expires_unix",0)):
        raise RuntimeError("EXIT_NOSAVE denied: authorization expired")
    try: EXIT_AUTH.unlink()
    except FileNotFoundError: pass
    return auth

def send(command, issuer, run_id=None, expected_campaign_id=None):
    active=_load_active()
    if not active or active.get("state")!="ACTIVE":
        raise RuntimeError("No active control run")
    if run_id is not None and run_id!=active["run_id"]:
        raise RuntimeError("Stale/wrong run_id")
    if expected_campaign_id and active.get("expected_campaign_id") not in (None,expected_campaign_id):
        raise RuntimeError("Campaign expectation mismatch")
    is_exit=command.strip().upper().startswith("EXIT_NOSAVE")
    exit_auth=None
    if is_exit:
        exit_auth=_consume_exit_authorization(active,issuer)
    active["sequence"]=int(active.get("sequence",0))+1
    seq=active["sequence"]
    if is_exit:
        active["state"]="EXITING"
        active["exit_requested_unix"]=time.time()
        active["exit_issuer"]=issuer
        active["exit_reason"]=exit_auth.get("reason") if exit_auth else None
    _atomic_text(ACTIVE,json.dumps(active,indent=2))
    meta={
        "schema":"BannerlordAI.CommandEnvelope.v1",
        "run_id":active["run_id"],
        "sequence":seq,
        "issuer":issuer,
        "wall_unix":time.time(),
        "expected_campaign_id":active.get("expected_campaign_id"),
        "expected_runner_version":active.get("expected_runner_version"),
        "command":command
    }
    with LOG.open("a",encoding="utf-8") as f:
        f.write(json.dumps(meta,separators=(",",":"))+"\n")
    _atomic_text(META,json.dumps(meta,indent=2))
    _atomic_text(COMMAND,command.rstrip()+"\n")
    return meta

def send_active(command, issuer="controller"):
    return send(command,issuer)

def close_run(reason="closed"):
    active=_load_active()
    if not active:return None
    active["state"]="CLOSED"
    active["closed_unix"]=time.time()
    active["close_reason"]=reason
    _atomic_text(ACTIVE,json.dumps(active,indent=2))
    return active
