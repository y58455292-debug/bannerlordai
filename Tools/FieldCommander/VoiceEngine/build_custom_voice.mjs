import fs from "fs";
import path from "path";

const base = path.join(process.cwd(), "node_modules", "kokoro-js", "voices");
const parts = [
  ["bm_george.bin", 0.45],
  ["bm_fable.bin", 0.25],
  ["bm_lewis.bin", 0.20],
  ["bm_daniel.bin", 0.10]
];

const arrays = parts.map(([name, weight]) => {
  const buf = fs.readFileSync(path.join(base, name));
  return {
    view: new Float32Array(buf.buffer, buf.byteOffset, buf.byteLength / 4),
    weight
  };
});

const len = arrays[0].view.length;
const out = new Float32Array(len);
for (let i = 0; i < len; i++) {
  let v = 0;
  for (const x of arrays) v += x.view[i] * x.weight;
  out[i] = v;
}

fs.writeFileSync(
  path.join(base, "bm_commander.bin"),
  Buffer.from(out.buffer)
);

console.log("custom voice written", out.length);
