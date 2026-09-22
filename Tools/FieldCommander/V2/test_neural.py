import numpy as np
import soundfile as sf
from kokoro_onnx import Kokoro

MODEL = r"D:\BannerlordAIResearch\Tools\FieldCommander\V2\Model\kokoro-v1.0.int8.onnx"
VOICES = r"D:\BannerlordAIResearch\Tools\FieldCommander\V2\Model\voices-v1.0.bin"

kokoro = Kokoro(MODEL, VOICES)
style = (
    kokoro.get_voice_style("bm_george") * 0.45 +
    kokoro.get_voice_style("bm_fable") * 0.25 +
    kokoro.get_voice_style("bm_lewis") * 0.20 +
    kokoro.get_voice_style("bm_daniel") * 0.10
).astype(np.float32)

text = "My lord, scouts report movement along the western frontier. Watch the road ahead."
samples, rate = kokoro.create(text, voice=style, speed=0.90, lang="en-gb")
sf.write(r"D:\BannerlordAIResearch\Tools\FieldCommander\V2\commander_aldrin_test.wav", samples, rate)
print("READY", rate, len(samples))
