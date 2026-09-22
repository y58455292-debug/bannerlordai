import os, time, numpy as np
from kokoro_onnx import Kokoro

MODEL = r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\kokoro-v1.0.int8.onnx"
VOICES = r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\voices-v1.0.bin"
text = "My lord, scouts bring word from the western frontier. Keep your eyes on the road ahead. Something is moving."

print("provider", os.environ.get("ONNX_PROVIDER"))
t0=time.perf_counter()
k=Kokoro(MODEL,VOICES)
print("load_ms", round((time.perf_counter()-t0)*1000))
style=(k.get_voice_style("bm_george")*.45+k.get_voice_style("bm_fable")*.25+k.get_voice_style("bm_lewis")*.20+k.get_voice_style("bm_daniel")*.10).astype(np.float32)

for i,txt in enumerate(["Ready.", text, text]):
    t=time.perf_counter()
    samples,rate=k.create(txt,voice=style,speed=.90,lang="en-gb",sentence_pause=.18,clause_pause=.07)
    print("run",i,"ms",round((time.perf_counter()-t)*1000),"samples",len(samples))
