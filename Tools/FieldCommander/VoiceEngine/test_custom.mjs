import { KokoroTTS } from "kokoro-js";

const tts = await KokoroTTS.from_pretrained("onnx-community/Kokoro-82M-v1.0-ONNX", {
  dtype: "q8",
  device: "wasm",
  progress_callback: (x) => {
    if (x && x.file && typeof x.progress === "number") {
      const p = Math.floor(x.progress);
      if (p === 0 || p === 25 || p === 50 || p === 75 || p === 100) {
        console.log(p + "%", x.file);
      }
    }
  }
});
console.log("MODEL_READY");
const audio = await tts.generate(
  "My lord, scouts report movement along the western frontier. Watch the road ahead.",
  { voice: "bm_george", speed: 0.91 }
);
audio.save("D:/BannerlordAIResearch/Tools/FieldCommander/VoiceEngine/commander_test.wav");
console.log("CUSTOM_SAMPLE_READY");
