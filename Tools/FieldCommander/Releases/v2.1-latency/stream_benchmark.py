import asyncio,time,numpy as np
from kokoro_onnx import Kokoro
MODEL=r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\kokoro-v1.0.int8.onnx"
VOICES=r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.0\Model\voices-v1.0.bin"
TEXT="To Saneopa. Watch Maritzios."
k=Kokoro(MODEL,VOICES)
style=(k.get_voice_style("bm_george")*.45+k.get_voice_style("bm_fable")*.25+k.get_voice_style("bm_lewis")*.20+k.get_voice_style("bm_daniel")*.10).astype(np.float32)

async def main():
    t=time.perf_counter()
    i=0
    async for samples,rate in k.create_stream(TEXT,voice=style,speed=.92,lang="en-gb",sentence_pause=.14,clause_pause=.05):
        now=(time.perf_counter()-t)*1000
        print("chunk",i,"ms",round(now),"samples",len(samples))
        i+=1
    print("total_ms",round((time.perf_counter()-t)*1000),"chunks",i)

asyncio.run(main())
