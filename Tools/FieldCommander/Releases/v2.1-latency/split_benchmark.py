import time,numpy as np
from kokoro_onnx import Kokoro
MODEL=r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\kokoro-v1.0.int8.onnx"
VOICES=r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\voices-v1.0.bin"
k=Kokoro(MODEL,VOICES)
style=(k.get_voice_style("bm_george")*.45+k.get_voice_style("bm_fable")*.25+k.get_voice_style("bm_lewis")*.20+k.get_voice_style("bm_daniel")*.10).astype(np.float32)
for txt in ["To Saneopa.","Watch Maritzios.","Follow Maritzios.","Watch the raid."]:
    t=time.perf_counter()
    s,r=k.create(txt,voice=style,speed=.92,lang="en-gb",sentence_pause=.08,clause_pause=.04)
    print(round((time.perf_counter()-t)*1000),len(txt),txt)
