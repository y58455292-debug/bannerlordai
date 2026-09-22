from pathlib import Path
import atexit, datetime, hashlib, json, os, shutil, subprocess, time
import xml.etree.ElementTree as ET

ROOT = Path(r"D:\BannerlordAIResearch")
GAME = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
BIN = GAME / "bin/Win64_Shipping_Client"
EXE = BIN / "Bannerlord.exe"
LAUNCHER_DATA = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml")
BANNERLORD_CONFIG = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\BannerlordConfig.txt")
SAVES = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves")
TARGET_SAVE = SAVES / "ClanAI v020K MEMORY TARGETED OBSERVE TEST.sav"
EXPECTED_SAVE_SHA = "11E3F8C04A1DF3ACA4C0DA4D51F9BB912C4D2970114C66B403729F2E5577FA6B"
RUNNER = ROOT / "Automation/TestRunner"
COMMAND = RUNNER / "command.txt"
STATUS = RUNNER / "status.txt"
RUNNER_LOG = RUNNER / "runner.log"
SESSIONS = ROOT / "Telemetry/ClanAI/sessions"
MAX_RUN_SECONDS = 180
_original_focus_setting = None

def get_focus_setting():
    text = BANNERLORD_CONFIG.read_text(
        encoding="utf-8-sig",
        errors="replace",
    )
    for line in text.splitlines():
        if line.startswith("StopGameOnFocusLost="):
            return line.split("=", 1)[1].strip()
    return None

def set_focus_setting(value):
    text = BANNERLORD_CONFIG.read_text(
        encoding="utf-8-sig",
        errors="replace",
    )
    lines = text.splitlines()
    replaced = False
    for i, line in enumerate(lines):
        if line.startswith("StopGameOnFocusLost="):
            lines[i] = "StopGameOnFocusLost=" + value
            replaced = True
            break
    if not replaced:
        lines.append("StopGameOnFocusLost=" + value)

    BANNERLORD_CONFIG.write_text(
        "\n".join(lines) + "\n",
        encoding="utf-8",
    )

def restore_focus_setting():
    global _original_focus_setting

    if _original_focus_setting is None:
        return

    value = _original_focus_setting
    _original_focus_setting = None

    try:
        set_focus_setting(value)
    except Exception:
        pass

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def log(msg, path=None):
    line = datetime.datetime.now().astimezone().isoformat() + " " + msg
    print(line, flush=True)
    if path:
        with path.open("a", encoding="utf-8") as f:
            f.write(line + "\n")

def game_processes():
    cp = subprocess.run(["tasklist", "/fo", "csv"], capture_output=True, text=True)
    return [x for x in cp.stdout.splitlines()
            if "Bannerlord.exe" in x or "TaleWorlds.MountAndBlade.Launcher.exe" in x]

def selected_modules():
    tree = ET.parse(LAUNCHER_DATA)
    ids = []
    for node in tree.findall(".//SingleplayerData/ModDatas/UserModData"):
        sel = node.findtext("IsSelected", "").strip().lower()
        ident = node.findtext("Id", "").strip()
        if sel == "true" and ident and ident not in ids:
            ids.append(ident)
    return ids
def write_command(text):
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

def newest_session(after_ts):
    candidates = [
        p for p in SESSIONS.glob("*_v020K_review.log")
        if p.stat().st_mtime >= after_ts
    ]
    return max(candidates, key=lambda p: p.stat().st_mtime) if candidates else None

def wait_for(predicate, timeout, interval=0.25):
    end = time.time() + timeout
    while time.time() < end:
        val = predicate()
        if val:
            return val
        time.sleep(interval)
    return None
def main():
    global _original_focus_setting

    if game_processes():
        raise RuntimeError("Bannerlord/TaleWorlds process already running")
    if not TARGET_SAVE.exists():
        raise FileNotFoundError(TARGET_SAVE)
    if sha(TARGET_SAVE) != EXPECTED_SAVE_SHA:
        raise RuntimeError("Target disposable save hash mismatch")

    _original_focus_setting = get_focus_setting()
    set_focus_setting("False")
    atexit.register(restore_focus_setting)

    stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    validation = ROOT / "Longitudinal/LiveValidation" / ("SocialMemoryCausal_v020K_TargetedAuto_" + stamp)
    validation.mkdir(parents=True)
    controller_log = validation / "controller.log"

    # Make the disposable target the latest save so vanilla /continuegame selects it.
    now = time.time()
    os.utime(TARGET_SAVE, (now, now))
    newest = max(SAVES.glob("*.sav"), key=lambda p: p.stat().st_mtime)
    if newest.resolve() != TARGET_SAVE.resolve():
        raise RuntimeError("Target save is not newest after touch")

    for p in [COMMAND, STATUS]:
        if p.exists():
            p.unlink()
    if RUNNER_LOG.exists():
        shutil.copy2(RUNNER_LOG, validation / "runner_preexisting.log")
        RUNNER_LOG.unlink()
    modules = selected_modules()
    if "ClanAI" not in modules or "BannerlordInspector" not in modules:
        raise RuntimeError("Expected ClanAI/Inspector not selected")

    module_arg = "_MODULES_*" + "*".join(modules) + "*_MODULES_"
    cmd = [str(EXE), "/singleplayer", module_arg, "/continuegame"]
    launch_time = time.time()

    manifest = {
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "target_save": str(TARGET_SAVE),
        "target_save_sha256": sha(TARGET_SAVE),
        "modules": modules,
        "command": cmd,
        "stop_condition": "MEMORY_CAUSAL_WOULD_FLIP or timeout",
        "max_run_seconds": MAX_RUN_SECONDS,
        "mode": "Observe",
    }
    (validation / "launch_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    log("LAUNCH " + " ".join(cmd), controller_log)
    proc = subprocess.Popen(cmd, cwd=str(BIN))
    def campaign_ready():
        if proc.poll() is not None:
            return None
        st = read_status()
        return st if st.get("campaignReady") == "True" else None

    ready = wait_for(campaign_ready, 180, 0.5)
    if not ready:
        raise RuntimeError("Campaign did not reach runner campaign_ready within 180 seconds")

    log("CAMPAIGN_READY timeControl=" + ready.get("timeControl", "?"), controller_log)

    session = wait_for(lambda: newest_session(launch_time), 30, 0.25)
    if not session:
        raise RuntimeError("No new v0.20K ClanAI session log appeared")
    log("SESSION " + str(session), controller_log)

    write_command("AUTO_PLAY")
    armed = wait_for(
        lambda: (
            read_status()
            if read_status().get("autoDesiredMode") == "StoppablePlay"
            else None
        ),
        10,
        0.1,
    )
    if not armed:
        raise RuntimeError("Runner did not arm AUTO_PLAY")
    log(
        "AUTO_PLAY_ARMED timeControl=" +
        armed.get("timeControl", "?") +
        " blockers=" +
        armed.get("blockers", "?"),
        controller_log,
    )

    hit = None
    near_count = 0
    fen_giall_seen = False
    start_hours = None
    last_hours = None
    max_auto_resumes = 0
    blockers_seen = set()
    start_run = time.time()
    while time.time() - start_run < MAX_RUN_SECONDS:
        if proc.poll() is not None:
            raise RuntimeError("Bannerlord exited unexpectedly during simulation")

        st = read_status()
        try:
            hours = float(st.get("campaignHours", "nan"))
            if start_hours is None:
                start_hours = hours
            last_hours = hours
        except Exception:
            pass

        try:
            max_auto_resumes = max(
                max_auto_resumes,
                int(st.get("autoResumeCount", "0")),
            )
        except Exception:
            pass

        blocker_text = st.get("blockers", "<none>")
        if blocker_text not in ("", "<none>"):
            blockers_seen.add(blocker_text)

        text = session.read_text(encoding="utf-8-sig", errors="replace")
        near_count = text.count("MEMORY_CAUSAL_NEAR ")
        fen_giall_seen = (
            "CONSEQUENCE_CONSIDER actor=Guaran" in text and
            "ownerClan=fen Giall" in text
        )

        for line in text.splitlines():
            if "MEMORY_CAUSAL_WOULD_FLIP " in line:
                hit = line
                break
        if hit:
            break
        time.sleep(0.4)

    write_command("PAUSE")
    paused = wait_for(lambda: (read_status().get("timeControl") == "Stop"), 10, 0.1)
    if not paused:
        raise RuntimeError("Runner did not confirm pause")

    reason = "WOULD_FLIP" if hit else "TIMEOUT"
    campaign_hours_advanced = (
        None if start_hours is None or last_hours is None
        else last_hours - start_hours
    )
    log(
        "PAUSED reason=" + reason +
        " nearCount=" + str(near_count) +
        " fenGiallSeen=" + str(fen_giall_seen) +
        " campaignHoursAdvanced=" + str(campaign_hours_advanced) +
        " autoResumes=" + str(max_auto_resumes) +
        " blockersSeen=" + json.dumps(sorted(blockers_seen)),
        controller_log,
    )
    # Freeze evidence before asking the game to exit.
    shutil.copy2(session, validation / "clanai_session.log")
    diag = session.with_suffix(".diagnostics.txt")
    if diag.exists():
        shutil.copy2(diag, validation / diag.name)
    if RUNNER_LOG.exists():
        shutil.copy2(RUNNER_LOG, validation / "runner.log")
    shutil.copy2(STATUS, validation / "runner_status_at_pause.txt")

    result = {
        "reason": reason,
        "would_flip_line": hit,
        "near_count": near_count,
        "fen_giall_seen": fen_giall_seen,
        "run_seconds": time.time() - start_run,
        "campaign_hours_advanced": campaign_hours_advanced,
        "max_auto_resumes": max_auto_resumes,
        "blockers_seen": sorted(blockers_seen),
        "session": str(session),
        "session_sha256_at_pause": sha(validation / "clanai_session.log"),
        "target_save_sha256_at_pause": sha(TARGET_SAVE),
        "runner_status": read_status(),
    }
    (validation / "result_at_pause.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    log("EVIDENCE_FROZEN " + json.dumps(result), controller_log)

    # Clean no-save game shutdown through TaleWorlds native API.
    write_command("EXIT_NOSAVE")
    end = time.time() + 30
    while time.time() < end and proc.poll() is None:
        time.sleep(0.25)

    if proc.poll() is None:
        log("EXIT_TIMEOUT process still alive; leaving it untouched", controller_log)
        result["exit_clean"] = False
    else:
        result["exit_clean"] = True
        log("EXIT_CLEAN code=" + str(proc.returncode), controller_log)

    post_sha = sha(TARGET_SAVE)
    result["target_save_sha256_post_exit"] = post_sha
    result["save_unchanged"] = post_sha == EXPECTED_SAVE_SHA
    restore_focus_setting()
    result["focus_setting_restored"] = get_focus_setting()
    (validation / "final_result.json").write_text(json.dumps(result, indent=2), encoding="utf-8")

    if not result["save_unchanged"]:
        changed = validation / "UNEXPECTED_CHANGED_TARGET_SAVE.sav"
        shutil.copy2(TARGET_SAVE, changed)
        log("SAVE_CHANGED preserved=" + str(changed), controller_log)

    print(json.dumps({"validation": str(validation), **result}, indent=2))

if __name__ == "__main__":
    main()
