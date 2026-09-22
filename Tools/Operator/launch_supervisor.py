from pathlib import Path
import argparse, datetime, hashlib, json, os, subprocess, time
import xml.etree.ElementTree as ET

ROOT = Path(r"D:\BannerlordAIResearch")
STEAM_ROOT = Path(r"C:\Program Files (x86)\Steam")
STEAM_EXE = STEAM_ROOT / "steam.exe"
STEAM_CONNECTION_LOG = STEAM_ROOT / "logs/connection_log.txt"
GAME = STEAM_ROOT / "steamapps/common/Mount & Blade II Bannerlord"
BIN = GAME / "bin/Win64_Shipping_Client"
EXE = BIN / "Bannerlord.exe"
SAVES = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves")
LAUNCHER_DATA = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml")
ENGINE_CONFIG = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\engine_config.txt")
AUTO = ROOT / "Automation/TestRunner"
COMMAND = AUTO / "command.txt"
STATUS = AUTO / "status.txt"
LOG_ROOT = ROOT / "Longitudinal/LiveValidation"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def normalize_safe_mode_sentinel():
    if not ENGINE_CONFIG.exists():
        return False

    def is_ready():
        try:
            text = ENGINE_CONFIG.read_text(encoding="utf-8-sig", errors="replace")
            return any(
                line.strip().lower() == "safely_exited = 1"
                for line in text.splitlines())
        except Exception:
            return False

    if is_ready():
        return True

    for attempt in range(10):
        try:
            text = ENGINE_CONFIG.read_text(encoding="utf-8-sig", errors="replace")
            lines = text.splitlines()
            out = []
            found = False
            for line in lines:
                if line.strip().lower().startswith("safely_exited"):
                    found = True
                    out.append("safely_exited = 1")
                else:
                    out.append(line)
            if not found:
                out.append("safely_exited = 1")
            payload = "\n".join(out) + "\n"

            # Prefer atomic replacement, but OneDrive can transiently deny rename
            # while still allowing a normal file write. Fall back to in-place.
            tmp = ENGINE_CONFIG.with_suffix(".txt.tmp")
            try:
                tmp.write_text(payload, encoding="utf-8")
                os.replace(tmp, ENGINE_CONFIG)
            except (PermissionError, OSError):
                try:
                    tmp.unlink()
                except Exception:
                    pass
                with ENGINE_CONFIG.open("w", encoding="utf-8", newline="") as f:
                    f.write(payload)
                    f.flush()

            if is_ready():
                return True
        except (PermissionError, OSError):
            time.sleep(min(0.25 * (attempt + 1), 1.0))
    return is_ready()

def game_processes():
    cp = subprocess.run(["tasklist", "/fo", "csv"], capture_output=True, text=True)
    return [x for x in cp.stdout.splitlines()
            if "Bannerlord.exe" in x or "TaleWorlds.MountAndBlade.Launcher.exe" in x]

def steam_processes():
    cp = subprocess.run(
        ["tasklist", "/FI", "IMAGENAME eq steam.exe", "/FO", "CSV", "/NH"],
        capture_output=True, text=True)
    return [x for x in cp.stdout.splitlines() if '"steam.exe"' in x.lower()]

def steam_connection_state():
    if not STEAM_CONNECTION_LOG.exists():
        return "log_missing"
    try:
        raw = STEAM_CONNECTION_LOG.read_bytes()
        text = raw[-131072:].decode("utf-8", errors="replace")
    except Exception:
        return "log_unreadable"
    logged_on = max(
        text.rfind("[Logged On"),
        text.rfind("RecvMsgClientLogOnResponse() : processing complete"))
    logged_off = max(
        text.rfind("[Logged Off"),
        text.rfind("LogOff()"),
        text.rfind("AsyncDisconnect("))
    return "logged_on" if logged_on > logged_off else "logged_off"

def ensure_steam_ready(timeout=45):
    started_epoch = None
    if not steam_processes():
        if not STEAM_EXE.is_file():
            raise RuntimeError("Steam executable is missing")
        started_epoch = time.time()
        subprocess.Popen([str(STEAM_EXE), "-silent"], cwd=str(STEAM_ROOT))

    end = time.time() + timeout
    while time.time() < end:
        processes = steam_processes()
        state = steam_connection_state()
        fresh_log = True
        if started_epoch is not None:
            fresh_log = (
                STEAM_CONNECTION_LOG.exists()
                and STEAM_CONNECTION_LOG.stat().st_mtime >= started_epoch - 1.0
            )
        if processes and state == "logged_on" and fresh_log:
            return {
                "ready": True,
                "state": state,
                "started_by_supervisor": started_epoch is not None,
                "process_count": len(processes),
                "connection_log_mtime": (
                    STEAM_CONNECTION_LOG.stat().st_mtime
                    if STEAM_CONNECTION_LOG.exists()
                    else None
                ),
            }
        time.sleep(0.25)

    raise RuntimeError(
        "Steam readiness timeout state=" +
        steam_connection_state() +
        " process_count=" +
        str(len(steam_processes())))

def selected_modules():
    tree = ET.parse(LAUNCHER_DATA)
    ids = []
    for node in tree.findall(".//SingleplayerData/ModDatas/UserModData"):
        if node.findtext("IsSelected", "").strip().lower() != "true":
            continue
        ident = node.findtext("Id", "").strip()
        if ident and ident not in ids:
            ids.append(ident)
    return ids

def write_command(text):
    AUTO.mkdir(parents=True, exist_ok=True)
    tmp = COMMAND.with_suffix(".tmp")
    tmp.write_text(text + "\n", encoding="utf-8")
    os.replace(tmp, COMMAND)

def read_status():
    if not STATUS.exists():
        return {}
    data = {}
    for line in STATUS.read_text(encoding="utf-8-sig", errors="replace").splitlines():
        if "=" in line:
            k, v = line.split("=", 1)
            data[k] = v
    return data

def cleanup_stale_control_files():
    for x in (COMMAND, STATUS):
        try:
            x.unlink()
        except FileNotFoundError:
            pass

def stop_pre_ready_process(proc, record):
    if proc.poll() is not None:
        return
    record["cleanup"] = "terminate_pre_ready"
    proc.terminate()
    try:
        proc.wait(timeout=10)
    except subprocess.TimeoutExpired:
        record["cleanup"] = "kill_pre_ready"
        proc.kill()
        proc.wait(timeout=10)

def dismiss_safe_mode_prompt():
    script = (
        "$p=Get-Process Bannerlord -ErrorAction SilentlyContinue; "
        "if($p -and $p.MainWindowTitle -eq 'Safe Mode'){ "
        "$w=New-Object -ComObject WScript.Shell; "
        "if($w.AppActivate($p.Id)){Start-Sleep -Milliseconds 100; $w.SendKeys('%n'); exit 0}; exit 2}; exit 3"
    )
    try:
        cp = subprocess.run(
            ["powershell", "-NoProfile", "-Command", script],
            capture_output=True,
            text=True,
            timeout=3,
        )
        return cp.returncode == 0
    except Exception:
        return False

def wait_ready(proc, launch_time, timeout):
    end = time.time() + timeout
    while time.time() < end:
        dismiss_safe_mode_prompt()
        code = proc.poll()
        if code is not None:
            return None, "PROCESS_EXIT_BEFORE_READY", code
        if STATUS.exists() and STATUS.stat().st_mtime >= launch_time:
            st = read_status()
            if st.get("schema") == "BannerlordAI.TestRunner.v2" and st.get("campaignReady") == "True":
                return st, "READY", None
        time.sleep(0.25)
    return None, "READY_TIMEOUT", None

def wait_exact_named_load(
        slot,
        expected_hero,
        initial_generation,
        timeout=120,
        expected_hours=None,
        hours_tolerance=0.01):
    end = time.time() + timeout
    while time.time() < end:
        st = read_status()
        try:
            generation = int(st.get("campaignGeneration", "0") or 0)
        except Exception:
            generation = 0

        exact = (
            st.get("schema") == "BannerlordAI.TestRunner.v2"
            and st.get("campaignReady") == "True"
            and generation > int(initial_generation)
            and st.get("loadedSave") == slot
            and st.get("playerHero") == expected_hero
        )
        if exact and expected_hours is not None:
            try:
                exact = abs(float(st.get("campaignHours")) - float(expected_hours)) <= hours_tolerance
            except Exception:
                exact = False
        if exact:
            return st
        time.sleep(0.2)
    return None

def attempt_launch(save_path, timeout, attempt_dir):
    if game_processes():
        raise RuntimeError("Existing Bannerlord/TaleWorlds process blocks autonomous launch")

    steam_preflight = ensure_steam_ready()
    safe_mode_sentinel_normalized = normalize_safe_mode_sentinel()
    cleanup_stale_control_files()
    now = time.time()
    os.utime(save_path, (now, now))
    newest = max(SAVES.glob("*.sav"), key=lambda x: x.stat().st_mtime)
    if newest.resolve() != save_path.resolve():
        raise RuntimeError("Target save is not newest after touch")

    modules = selected_modules()
    if "ClanAI" not in modules or "BannerlordInspector" not in modules:
        raise RuntimeError("Expected ClanAI and BannerlordInspector selected")

    arg = "_MODULES_*" + "*".join(modules) + "*_MODULES_"
    command = [str(EXE), "/singleplayer", arg, "/continuegame"]
    launch_time = time.time()
    proc = None
    spawn_errors = []
    for spawn_try in range(1, 6):
        exe_visible = EXE.is_file()
        cwd_visible = BIN.is_dir()
        if not exe_visible or not cwd_visible:
            spawn_errors.append({
                "try": spawn_try,
                "type": "PATH_NOT_VISIBLE",
                "message": "Transient filesystem visibility check failed",
                "exe_exists": exe_visible,
                "cwd_exists": cwd_visible,
            })
            time.sleep(min(1.0 * spawn_try, 3.0))
            continue
        try:
            proc = subprocess.Popen(command, cwd=str(BIN))
            break
        except (FileNotFoundError, PermissionError) as ex:
            spawn_errors.append({
                "try": spawn_try,
                "type": type(ex).__name__,
                "message": str(ex),
                "exe_exists": EXE.is_file(),
                "cwd_exists": BIN.is_dir(),
            })
            time.sleep(min(1.0 * spawn_try, 3.0))
    if proc is None:
        raise RuntimeError("PROCESS_CREATE_FAILED " + json.dumps(spawn_errors))

    ready, state, code = wait_ready(proc, launch_time, timeout)

    record = {
        "launched_local": datetime.datetime.now().astimezone().isoformat(),
        "pid": proc.pid, "command": command, "state": state,
        "exit_code_before_ready": code, "ready_status": ready,
        "spawn_errors": spawn_errors,
        "steam_preflight": steam_preflight,
        "safe_mode_sentinel_normalized": safe_mode_sentinel_normalized,
        "launch_semantics": (
            "BOOTSTRAP_ONLY_CONTINUEGAME; target save identity is not "
            "established until explicit LOAD_SAVE plus wait_exact_named_load"
        ),
    }
    (attempt_dir / "launch_attempt.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
    return proc, ready, state, record

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--save", required=True)
    ap.add_argument("--ready-timeout", type=int, default=180)
    ap.add_argument("--retries", type=int, default=1)
    ap.add_argument("--expected-save-sha")
    ap.add_argument("--exit-nosave", action="store_true")
    args = ap.parse_args()

    save_path = SAVES / args.save
    if not save_path.exists():
        raise FileNotFoundError(save_path)
    before_sha = sha(save_path)
    if args.expected_save_sha and before_sha != args.expected_save_sha.upper():
        raise RuntimeError("Target save hash mismatch before launch")

    stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    run_dir = LOG_ROOT / ("LaunchSupervisor_" + stamp)
    run_dir.mkdir(parents=True)
    results = []
    proc = None
    ready = None

    for attempt in range(1, args.retries + 2):
        attempt_dir = run_dir / ("attempt_%02d" % attempt)
        attempt_dir.mkdir()
        proc, ready, state, record = attempt_launch(save_path, args.ready_timeout, attempt_dir)
        results.append(record)
        if state == "READY":
            break
        stop_pre_ready_process(proc, record)
        (attempt_dir / "launch_attempt.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
        time.sleep(2)

    success = ready is not None
    final = {
        "schema": "BannerlordAI.AutonomousLaunchSupervisor.v1",
        "success": success, "attempts": results,
        "save": str(save_path), "save_sha_before": before_sha,
        "manual_intervention_allowed": False,
        "pass_rule": (
            "Autonomous bootstrap launch + fresh runner v2 + campaignReady. "
            "This proves bootstrap readiness only; exact save identity requires "
            "explicit LOAD_SAVE plus wait_exact_named_load."
        ),
    }

    if success and args.exit_nosave:
        write_command("EXIT_NOSAVE")
        end = time.time() + 30
        while time.time() < end and proc.poll() is None:
            time.sleep(0.25)
        final["planned_exit_code"] = proc.poll()
        final["planned_exit_completed"] = proc.poll() is not None

    final["save_sha_after"] = sha(save_path)
    final["save_unchanged"] = final["save_sha_after"] == before_sha
    (run_dir / "final_result.json").write_text(json.dumps(final, indent=2), encoding="utf-8")
    print(json.dumps({"validation": str(run_dir), **final}, indent=2))

    if not success:
        raise SystemExit(2)
    if not final["save_unchanged"]:
        raise SystemExit(3)

if __name__ == "__main__":
    main()
