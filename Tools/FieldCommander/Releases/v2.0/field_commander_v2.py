import ctypes
import hashlib
import os
import re
import sys
import time
import traceback
import winsound
from pathlib import Path

os.environ.setdefault("OMP_NUM_THREADS", "3")
os.environ.setdefault("OMP_WAIT_POLICY", "PASSIVE")

import numpy as np
import soundfile as sf
from kokoro_onnx import Kokoro

VERSION = "2.0"
VOICE_NAME = "Commander Aldric Neural v2"
ROOT = Path(r"D:\BannerlordAIResearch\Tools\FieldCommander")
RELEASE = Path(r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0")
MODEL = RELEASE / "Model" / "kokoro-v1.0.int8.onnx"
VOICES = RELEASE / "Model" / "voices-v1.0.bin"
TELEMETRY = Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
COMMAND = ROOT / "command.txt"
LATEST = ROOT / "latest.txt"
LOG = ROOT / "field_commander_v2.log"
READY = ROOT / "v2_ready.txt"
AUDIO = ROOT / "field_commander_v2_current.wav"

POLL_SECONDS = 0.45
AUTO_COOLDOWN = 7.0
DUPLICATE_SECONDS = 120.0

recent = {}
last_auto = 0.0
report_counter = 0

def log(msg):
    try:
        with LOG.open("a", encoding="utf-8") as f:
            f.write(time.strftime("%Y-%m-%dT%H:%M:%S") + " " + msg + "\n")
    except Exception:
        pass

def set_below_normal_priority():
    try:
        BELOW_NORMAL_PRIORITY_CLASS = 0x00004000
        handle = ctypes.windll.kernel32.GetCurrentProcess()
        ctypes.windll.kernel32.SetPriorityClass(handle, BELOW_NORMAL_PRIORITY_CLASS)
    except Exception as exc:
        log("PRIORITY_WARN " + type(exc).__name__)

def acquire_single_instance():
    try:
        handle = ctypes.windll.kernel32.CreateMutexW(None, False, "Global\\BannerlordAI_FieldCommander_v2")
        if not handle:
            return None
        if ctypes.windll.kernel32.GetLastError() == 183:
            return None
        return handle
    except Exception:
        return object()

def clean(value):
    return (value or "").replace("\r", " ").replace("\n", " ").strip()

def candidate_target(candidate):
    candidate = clean(candidate)
    if ":" in candidate:
        return candidate.split(":", 1)[1].strip()
    return candidate or "the reported position"

def choose(key, variants):
    digest = hashlib.sha1(key.encode("utf-8", "ignore")).digest()
    return variants[digest[0] % len(variants)]

def new_report(priority, key, text, speed=0.90):
    return {"priority": priority, "key": key, "text": text, "speed": speed}

def build_visual_report(line):
    m = re.search(r"actor=(.*?) party=.*? after=(.*?) reason=(\S+) applications=", line)
    if not m:
        return None
    actor, candidate, reason = map(clean, m.groups())
    target = candidate_target(candidate)
    key = f"{reason}|{actor}|{target}"

    if reason == "active-defense":
        text = choose(key, [
            f"Urgent word, my lord. {actor} has turned to defend {target}. Put your eyes on that front.",
            f"My lord, to {target}, quickly. {actor} is moving to hold it. The front is under pressure.",
            f"Riders from the line. {actor} has broken toward {target} to defend it. Watch that ground."
        ])
        return new_report(5, key, text, 0.96)

    if reason == "frontier-offense":
        text = choose(key, [
            f"Scouts report a push, my lord. {actor} is pressing toward {target}. Watch the approach.",
            f"My lord, look to {target}. {actor} is advancing there; that may be where the blow falls.",
            f"Movement on the frontier. {actor} is bearing down on {target}. Keep your eyes there."
        ])
        return new_report(4, key, text, 0.94)

    if reason == "frontier-defense":
        text = choose(key, [
            f"A change on the marches, my lord. {actor} is moving toward {target}. The border is drawing defenders.",
            f"My lord, watch {target}. {actor} has begun drifting toward that frontier.",
            f"The line is stiffening near {target}. {actor} is moving that way. Watch what follows."
        ])
        return new_report(3, key, text, 0.91)

    if reason == "rear-security":
        text = choose(key, [
            f"News from behind the line. {actor} has turned on {target}. Watch the roads in the rear.",
            f"My lord, {actor} is hunting {target} behind the front. That is how the roads stay open.",
            f"A smaller matter, but worth seeing. {actor} has moved against {target} in the rear."
        ])
        return new_report(2, key, text, 0.90)

    return new_report(
        2, key,
        f"My lord, a course has changed. Watch {target}. {actor} is moving there now.",
        0.91
    )

def build_report(line):
    if not line:
        return None

    if "VISUAL_WAR_WINNER_CHANGE" in line:
        return build_visual_report(line)

    if "WORLD_MEMORY event=RaidStarted" in line:
        m = re.search(r"raider=(.*?) target=(.*?) relation=", line)
        if not m:
            return None
        actor, target = map(clean, m.groups())
        key = f"raid|{actor}|{target}"
        text = choose(key, [
            f"Smoke on the horizon, my lord. {actor} has begun a raid at {target}. This will be remembered.",
            f"Riders bring bad news. {actor} is raiding {target}. Keep that place in mind; the grievance has begun.",
            f"My lord, {target} is under raid by {actor}. Watch what this does to the men who remember it."
        ])
        return new_report(3, key, text, 0.91)

    if "WORLD_MEMORY event=HeroCaptured" in line:
        m = re.search(r"rememberer=(.*?) remembererClan=.*? capturer=(.*?) capturerParty=", line)
        if not m:
            return None
        prisoner, capturer = map(clean, m.groups())
        key = f"capture|{prisoner}|{capturer}"
        text = choose(key, [
            f"Bad news from the field. {prisoner} has been taken by {capturer}. Remember those names.",
            f"My lord, {prisoner} is now prisoner to {capturer}. Their next meeting may carry the weight of this.",
            f"A capture worth marking. {capturer} has taken {prisoner}. That memory may return later."
        ])
        return new_report(2, key, text, 0.88)

    if "WORLD_MEMORY event=MercyRelease" in line:
        m = re.search(r"rememberer=(.*?) remembererClan=.*? facilitator=(.*?) relation=", line)
        if not m:
            return None
        prisoner, facilitator = map(clean, m.groups())
        key = f"mercy|{prisoner}|{facilitator}"
        text = choose(key, [
            f"An unexpected mercy, my lord. {facilitator} has released {prisoner} by choice. That debt may matter.",
            f"Word of mercy. {prisoner} walks free by {facilitator}'s choice. Remember it; they will.",
            f"My lord, {facilitator} has spared {prisoner} continued captivity. That act now lives in memory."
        ])
        return new_report(2, key, text, 0.87)

    if "MEMORY_CAUSAL_RESULT" in line and "PASS_FLIPPED" in line:
        m = re.search(r"party=(.*?) .*? finalBehavior=(\S+) finalTarget=(.*?) finalTargetId=", line)
        if not m:
            return None
        party = clean(m.group(1))
        target = clean(m.group(3))
        key = f"memory-causal|{party}|{target}"
        text = choose(key, [
            f"There, my lord. Watch {target}. Old memory has just changed {party}'s course.",
            f"This is one of the moments we came to see. Memory turned {party} toward {target}. Follow them now.",
            f"My lord, mark {target}. {party} changed course because of what they remember."
        ])
        return new_report(6, key, text, 0.93)

    return None

def is_duplicate(key):
    now = time.time()
    prior = recent.get(key)
    return prior is not None and now - prior < DUPLICATE_SECONDS

def mark_spoken(key):
    global last_auto
    now = time.time()
    last_auto = now
    recent[key] = now
    stale = [k for k, t in recent.items() if now - t > 600]
    for k in stale:
        recent.pop(k, None)

def read_new_lines(position):
    if not TELEMETRY.exists():
        return position, []
    size = TELEMETRY.stat().st_size
    if size < position:
        position = 0
    if size == position:
        return position, []
    with TELEMETRY.open("rb") as f:
        f.seek(position)
        data = f.read()
        position = f.tell()
    text = data.decode("utf-8", "ignore")
    return position, [x for x in re.split(r"\r?\n", text) if x]

def read_command():
    try:
        return COMMAND.read_text(encoding="utf-8").strip()
    except Exception:
        return ""

def apply_room_tone(samples):
    # Very light room presence; the neural voice remains dominant.
    y = np.asarray(samples, dtype=np.float32).copy()
    if y.size == 0:
        return y
    d1 = 720   # 30 ms at 24 kHz
    d2 = 1440  # 60 ms
    wet = y.copy()
    if len(y) > d1:
        wet[d1:] += y[:-d1] * 0.025
    if len(y) > d2:
        wet[d2:] += y[:-d2] * 0.012
    peak = float(np.max(np.abs(wet))) if wet.size else 1.0
    if peak > 0.97:
        wet *= 0.97 / peak
    return wet

def speak(kokoro, style, text, speed, source):
    LATEST.write_text(text, encoding="utf-8")
    started = time.perf_counter()
    samples, sample_rate = kokoro.create(
        text,
        voice=style,
        speed=float(speed),
        lang="en-gb",
        sentence_pause=0.18,
        clause_pause=0.07,
    )
    samples = apply_room_tone(samples)
    sf.write(AUDIO, samples, sample_rate, subtype="PCM_16")
    synth_ms = (time.perf_counter() - started) * 1000.0
    winsound.PlaySound(str(AUDIO), winsound.SND_FILENAME)
    log(f"SPOKE {source} voice={VOICE_NAME} synthMs={synth_ms:.0f} {text}")

def main():
    global report_counter
    mutex = acquire_single_instance()
    if mutex is None:
        return 0

    set_below_normal_priority()
    ROOT.mkdir(parents=True, exist_ok=True)
    COMMAND.touch(exist_ok=True)

    log("BOOT v2.0 neural engine loading")
    kokoro = Kokoro(str(MODEL), str(VOICES))

    style = (
        kokoro.get_voice_style("bm_george") * 0.45 +
        kokoro.get_voice_style("bm_fable") * 0.25 +
        kokoro.get_voice_style("bm_lewis") * 0.20 +
        kokoro.get_voice_style("bm_daniel") * 0.10
    ).astype(np.float32)

    # Warm the model once so the first real battlefield dispatch is not the slow one.
    warm_start = time.perf_counter()
    kokoro.create("Ready.", voice=style, speed=0.92, lang="en-gb")
    warm_ms = (time.perf_counter() - warm_start) * 1000.0

    position = TELEMETRY.stat().st_size if TELEMETRY.exists() else 0
    last_command = read_command()
    pending = None

    READY.write_text(
        f"{VOICE_NAME}\nversion={VERSION}\ntelemetryOffset={position}\nwarmupMs={warm_ms:.0f}\n",
        encoding="utf-8"
    )
    log(f"READY v2.0 voice={VOICE_NAME} telemetryOffset={position} warmupMs={warm_ms:.0f}")

    while True:
        command = read_command()
        if command != last_command:
            last_command = command
            if command.lower() == "!stop":
                log("STOP")
                break
            if command:
                speak(kokoro, style, command, 0.90, "MANUAL")

        position, lines = read_new_lines(position)
        for line in lines:
            report = build_report(line)
            if report is None:
                continue
            if is_duplicate(report["key"]):
                continue
            if pending is None or report["priority"] > pending["priority"]:
                pending = report

        if pending is not None and time.time() - last_auto >= AUTO_COOLDOWN:
            speak(
                kokoro,
                style,
                pending["text"],
                pending["speed"],
                "AUTO " + pending["key"],
            )
            mark_spoken(pending["key"])
            pending = None

        time.sleep(POLL_SECONDS)

    return 0

if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        log("FATAL " + type(exc).__name__ + " " + str(exc))
        log(traceback.format_exc().replace("\n", " | "))
        raise
