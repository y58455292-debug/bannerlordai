import { KokoroTTS } from "kokoro-js";

const model_id = "onnx-community/Kokoro-82M-v1.0-ONNX";
console.log("loading");
const tts = await KokoroTTS.from_pretrained(model_id, {
  dtype: "q4",
  device: "wasm",
  progress_callback: (x) => {
    if (x && typeof x.progress === "number" && x.progress === 100) {
      console.log("loaded", x.file || x.name || "");
    }
  },
});
console.log("ready");
const audio = await tts.generate("My lord, scouts bring word from the western frontier.", {
  voice: "bm_george",
  speed: 0.92,
});
audio.save("D:/BannerlordAIResearch/Tools/FieldCommander/VoiceEngine/test_george.wav");
console.log("saved");
