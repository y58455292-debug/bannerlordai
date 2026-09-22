import os,time,numpy as np
from kokoro_onnx import Kokoro
MODEL=r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\kokoro-v1.0.int8.onnx"
VOICES=r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\voices-v1.0.bin"
texts=[
"Ready.",
"My lord, watch Saneopa.",
"My lord, watch Saneopa. Maritzios is moving there.",
"My lord, scouts bring word from the western frontier. Keep your eyes on the road ahead. Something is moving."
]
print("provider",os.environ.get("ONNX_PROVIDER"))
t=time.perf_counter(); k=Kokoro(MODEL,VOICES); print("load_ms",round((time.perf_counter()-t)*1000))
style=(k.get_voice_style("bm_george")*.45+k.get_voice_style("bm_fable")*.25+k.get_voice_style("bm_lewis")*.20+k.get_voice_style("bm_daniel")*.10).astype(np.float32)
for txt in texts:
 t=time.perf_counter(); s,r=k.create(txt,voice=style,speed=.90,lang="en-gb",sentence_pause=.18,clause_pause=.07); dt=(time.perf_counter()-t)*1000
 print(round(dt),len(txt),len(s),txt)
