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
TARGET_SAVE = SAVES / "ClanAI v020P BLACKBOARD MERGED TEST.sav"
INSTALLED_CLANAI = GAME / "Modules/ClanAI/bin/Win64_Shipping_Client/ClanAI.dll"
DEPLOYMENT_LOCK = ROOT / "workspace/DEPLOYMENT_OWNER.json"
EXPECTED_CLANAI_SHA = "76C7BFAF0AFB8CC6666067F0C3B71191979ABE19FDA0C0A0ABDB82FE5E7CCC49"
EXPECTED_LOCK_OWNER = "sol_actor_blackboard_v020P"
EXPECTED_SAVE_SHA = "11E3F8C04A1DF3ACA4C0DA4D51F9BB912C4D2970114C66B403729F2E5577FA6B"
RUNNER = ROOT / "Automation/TestRunner"
COMMAND = RUNNER / "command.txt"
STATUS = RUNNER / "status.txt"
RUNNER_LOG = RUNNER / "runner.log"
SESSIONS = ROOT / "Telemetry/ClanAI/sessions"
MAX_RUN_SECONDS = 120
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
        p for p in SESSIONS.glob("*_v020P_review.log")
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

def verify_deployment():
    if not DEPLOYMENT_LOCK.exists():
        raise RuntimeError("Deployment ownership lock is missing")

    lock = json.loads(
        DEPLOYMENT_LOCK.read_text(
            encoding="utf-8-sig",
            errors="replace",
        )
    )

    installed_hash = sha(INSTALLED_CLANAI)

    if lock.get("owner") != EXPECTED_LOCK_OWNER:
        raise RuntimeError(
            "Deployment lock owner mismatch: " +
            str(lock.get("owner"))
        )

    if (
        lock.get("expected_installed_clanai_sha256") !=
        EXPECTED_CLANAI_SHA
    ):
        raise RuntimeError(
            "Deployment lock expected hash mismatch"
        )

    if installed_hash != EXPECTED_CLANAI_SHA:
        raise RuntimeError(
            "Installed ClanAI hash changed under deployment lock"
        )

    return {
        "owner": lock.get("owner"),
        "milestone": lock.get("milestone"),
        "installed_sha256": installed_hash,
    }

def main():
    global _original_focus_setting

    if game_processes():
        raise RuntimeError("Bannerlord/TaleWorlds process already running")
    if not TARGET_SAVE.exists():
        raise FileNotFoundError(TARGET_SAVE)
    if sha(TARGET_SAVE) != EXPECTED_SAVE_SHA:
        raise RuntimeError("Target disposable save hash mismatch")

    pre_deployment = verify_deployment()

    _original_focus_setting = get_focus_setting()
    set_focus_setting("False")
    atexit.register(restore_focus_setting)

    stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    validation = ROOT / "Longitudinal/LiveValidation" / ("ActorBlackboard_v020P_MergedAuto_" + stamp)
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
        "deployment_preflight": pre_deployment,
        "stop_condition": ">=100 blackboard captures/validations, reuse>0, visual-war reads>0, weak-recovery reads>0, composer owner writes>0, zero blackboard/composer mismatches/failures, or timeout",
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
        raise RuntimeError("No new v0.20P ClanAI session log appeared")
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

    stop_reason = None
    composer = {
        "frames": 0,
        "applied_frames": 0,
        "applied_contributions": 0,
        "proposal_contributions": 0,
        "score_mismatches": 0,
        "sequence_mismatches": 0,
        "winner_mismatches": 0,
        "equivalent_frames": 0,
        "score_writes": 0,
        "failures": 0,
    }
    blackboard = {
        "states": 0,
        "captures": 0,
        "states_created": 0,
        "states_reused": 0,
        "validations": 0,
        "visual_war_reads": 0,
        "weak_recovery_reads": 0,
        "mismatches": 0,
        "failures": 0,
    }
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

        text_now = session.read_text(
            encoding="utf-8-sig",
            errors="replace",
        )

        summaries = [
            x for x in text_now.splitlines()
            if "SUMMARY " in x and "composerFrames=" in x
        ]

        if summaries:
            latest = summaries[-1]
            composer_mapping = {
                "composerFrames": "frames",
                "composerAppliedFrames": "applied_frames",
                "composerAppliedContributions": "applied_contributions",
                "composerProposalContributions": "proposal_contributions",
                "composerScoreMismatches": "score_mismatches",
                "composerSequenceMismatches": "sequence_mismatches",
                "composerWinnerMismatches": "winner_mismatches",
                "composerEquivalentFrames": "equivalent_frames",
                "composerScoreWrites": "score_writes",
                "composerFailures": "failures",
            }

            blackboard_mapping = {
                "blackboardStates": "states",
                "blackboardCaptures": "captures",
                "blackboardStatesCreated": "states_created",
                "blackboardStatesReused": "states_reused",
                "blackboardValidations": "validations",
                "blackboardVisualWarReads": "visual_war_reads",
                "blackboardWeakRecoveryReads": "weak_recovery_reads",
                "blackboardMismatches": "mismatches",
                "blackboardFailures": "failures",
            }

            for raw in latest.split():
                if "=" not in raw:
                    continue

                key, value = raw.split("=", 1)

                try:
                    if key in composer_mapping:
                        composer[composer_mapping[key]] = int(value)
                    elif key in blackboard_mapping:
                        blackboard[blackboard_mapping[key]] = int(value)
                except Exception:
                    pass

        if (
            composer["score_mismatches"] or
            composer["sequence_mismatches"] or
            composer["winner_mismatches"] or
            composer["failures"]
        ):
            stop_reason = "COMPOSER_MISMATCH"
            break

        if blackboard["mismatches"] or blackboard["failures"]:
            stop_reason = "BLACKBOARD_MISMATCH"
            break

        if (
            blackboard["captures"] >= 100 and
            blackboard["validations"] >= 100 and
            blackboard["states_reused"] > 0 and
            blackboard["visual_war_reads"] > 0 and
            blackboard["weak_recovery_reads"] > 0 and
            composer["applied_frames"] >= 100 and
            composer["score_writes"] > 0
        ):
            stop_reason = "BLACKBOARD_CONSUMPTION_GATE_REACHED"
            break

        time.sleep(0.35)

    if stop_reason is None:
        stop_reason = "TIMEOUT"

    write_command("PAUSE")
    paused = wait_for(lambda: (read_status().get("timeControl") == "Stop"), 10, 0.1)
    if not paused:
        raise RuntimeError("Runner did not confirm pause")

    reason = stop_reason
    campaign_hours_advanced = (
        None if start_hours is None or last_hours is None
        else last_hours - start_hours
    )
    log(
        "PAUSED reason=" + reason +
        " composer=" + json.dumps(composer, sort_keys=True) +
        " blackboard=" + json.dumps(blackboard, sort_keys=True) +
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
        "composer": composer,
        "blackboard": blackboard,
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

    try:
        result["deployment_post_exit"] = verify_deployment()
        result["deployment_intact"] = True
    except Exception as ex:
        result["deployment_post_exit"] = {
            "error": type(ex).__name__ + ": " + str(ex)
        }
        result["deployment_intact"] = False

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
