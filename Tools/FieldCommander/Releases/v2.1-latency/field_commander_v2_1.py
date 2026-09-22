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

VERSION = "2.1"
VOICE_NAME = "Commander Aldric Neural v2"
ROOT = Path(r"D:\BannerlordAIResearch\Tools\FieldCommander")
RELEASE = Path(r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.1-latency")
MODEL = RELEASE / "Model" / "kokoro-v1.0.int8.onnx"
VOICES = RELEASE / "Model" / "voices-v1.0.bin"
TELEMETRY = Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
COMMAND = ROOT / "command.txt"
LATEST = ROOT / "latest.txt"
LOG = ROOT / "field_commander_v2_1.log"
READY = ROOT / "v2_1_ready.txt"
AUDIO = ROOT / "field_commander_v2_1_current.wav"
CACHE = ROOT / "cache_v2_1"

POLL_SECONDS = 0.45
AUTO_COOLDOWN = 5.0
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
        return new_report(5, key, f"To {target}. Watch {actor} defend.", 0.96)

    if reason == "frontier-offense":
        return new_report(4, key, f"To {target}. Watch {actor}'s advance.", 0.94)

    if reason == "frontier-defense":
        return new_report(3, key, f"To {target}. Watch {actor} reinforce.", 0.91)

    if reason == "rear-security":
        return new_report(2, key, f"Find {actor}. Watch them hunt {target}.", 0.90)

    return new_report(2, key, f"To {target}. Follow {actor}.", 0.91)

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
        return new_report(3, key, f"To {target}. Watch {actor}'s raid.", 0.91)

    if "WORLD_MEMORY event=HeroCaptured" in line:
        m = re.search(r"rememberer=(.*?) remembererClan=.*? capturer=(.*?) capturerParty=", line)
        if not m:
            return None
        prisoner, capturer = map(clean, m.groups())
        key = f"capture|{prisoner}|{capturer}"
        return None

    if "WORLD_MEMORY event=MercyRelease" in line:
        m = re.search(r"rememberer=(.*?) remembererClan=.*? facilitator=(.*?) relation=", line)
        if not m:
            return None
        prisoner, facilitator = map(clean, m.groups())
        key = f"mercy|{prisoner}|{facilitator}"
        return None

    if "MEMORY_CAUSAL_RESULT" in line and "PASS_FLIPPED" in line:
        m = re.search(r"party=(.*?) .*? finalBehavior=(\S+) finalTarget=(.*?) finalTargetId=", line)
        if not m:
            return None
        party = clean(m.group(1))
        target = clean(m.group(3))
        key = f"memory-causal|{party}|{target}"
        return new_report(6, key, f"To {target}. Follow {party}. Memory changed the course.", 0.93)

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
    CACHE.mkdir(parents=True, exist_ok=True)

    cache_key = hashlib.sha1(
        (f"{VOICE_NAME}|{speed:.3f}|" + text).encode("utf-8", "ignore")
    ).hexdigest()
    cache_file = CACHE / f"{cache_key}.wav"

    if cache_file.exists():
        winsound.PlaySound(str(cache_file), winsound.SND_FILENAME)
        log(f"SPOKE {source} voice={VOICE_NAME} cache=HIT synthMs=0 {text}")
        return

    started = time.perf_counter()
    samples, sample_rate = kokoro.create(
        text,
        voice=style,
        speed=float(speed),
        lang="en-gb",
        sentence_pause=0.14,
        clause_pause=0.05,
    )
    samples = apply_room_tone(samples)
    sf.write(cache_file, samples, sample_rate, subtype="PCM_16")
    synth_ms = (time.perf_counter() - started) * 1000.0
    winsound.PlaySound(str(cache_file), winsound.SND_FILENAME)
    log(f"SPOKE {source} voice={VOICE_NAME} cache=MISS synthMs={synth_ms:.0f} {text}")

def main():
    global report_counter
    mutex = acquire_single_instance()
    if mutex is None:
        return 0

    set_below_normal_priority()
    ROOT.mkdir(parents=True, exist_ok=True)
    COMMAND.touch(exist_ok=True)

    log("BOOT v2.1 spotter mode neural engine loading")
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
    log(f"READY v2.1 voice={VOICE_NAME} mode=SPOTTER telemetryOffset={position} warmupMs={warm_ms:.0f}")

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
