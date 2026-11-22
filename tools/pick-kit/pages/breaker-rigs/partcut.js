'use strict';
// node partcut.js <sheet> <out dir> <cols> <rows> <hd px per sheet px> <name>...: each cell cut by alpha, hi and at the game's size
const { spawnSync } = require('child_process');
const fs = require('fs');
const [src, dir, C, R, K, ...names] = process.argv.slice(2);
const cols = +C, rows = +R, head = fs.readFileSync(src);
const W = head.readUInt32BE(16), H = head.readUInt32BE(20);
const px = spawnSync('ffmpeg', ['-v', 'error', '-i', src, '-f', 'rawvideo', '-pix_fmt', 'rgba', '-'], { maxBuffer: 1 << 27 }).stdout;
const save = (file, w, h, buf) => { const r = spawnSync('ffmpeg', ['-v', 'error', '-y', '-f', 'rawvideo', '-pix_fmt', 'rgba', '-s', `${w}x${h}`, '-i', '-', '-frames:v', '1', file], { input: buf }); if (r.status) throw new Error(String(r.stderr)); };
const label = new Int32Array(W * H), parts = [], A = j => px[j * 4 + 3];
for (let i = 0; i < W * H; i++) {
  if (label[i] || A(i) < 140) continue;
  const id = parts.length + 1, q = [i]; label[i] = id; let sx = 0, sy = 0;
  for (let n = 0; n < q.length; n++) {
    const j = q[n], x = j % W, y = (j - x) / W; sx += x; sy += y;
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const u = x + dx, v = y + dy, k = v * W + u;
      if (u < 0 || v < 0 || u >= W || v >= H || label[k] || A(k) < 140) continue;
      label[k] = id; q.push(k);
    }
  }
  if (q.length >= 80) parts.push({ q, x: sx / q.length, y: sy / q.length }); else parts.push({ q: [], x: -1, y: -1 });
}
const cuts = {};
names.forEach((spec, n) => {
  const [name, pk] = spec.split('@'), f = +K * +(pk || 1);
  if (name === '-') return;
  const mine = parts.filter(p => p.q.length && Math.floor(p.x / (W / cols)) === n % cols && Math.floor(p.y / (H / rows)) === Math.floor(n / cols));
  let a = W, b = H, z = -1, t = -1;
  for (const p of mine) for (const j of p.q) { const x = j % W, y = (j - x) / W; a = Math.min(a, x); b = Math.min(b, y); z = Math.max(z, x); t = Math.max(t, y); }
  if (z < 0) { console.log(name, 'MISSING'); return; }
  const w = z - a + 1, h = t - b + 1, hi = Buffer.alloc(w * h * 4);
  for (const p of mine) for (const j of p.q) { const x = j % W, y = (j - x) / W; px.copy(hi, ((y - b) * w + x - a) * 4, j * 4, j * 4 + 4); }
  save(`${dir}/${name}_hi.png`, w, h, hi);
  const sw = Math.max(1, Math.round(w * f)), sh = Math.max(1, Math.round(h * f)), lo = Buffer.alloc(sw * sh * 4);
  for (let y = 0; y < sh; y++) for (let x = 0; x < sw; x++) {
    let red = 0, g = 0, bl = 0, al = 0, all = 0;
    for (let v = Math.floor(y * h / sh); v < Math.ceil((y + 1) * h / sh); v++) for (let u = Math.floor(x * w / sw); u < Math.ceil((x + 1) * w / sw); u++) {
      const cover = (Math.min(u + 1, (x + 1) * w / sw) - Math.max(u, x * w / sw)) * (Math.min(v + 1, (y + 1) * h / sh) - Math.max(v, y * h / sh));
      const o = (v * w + u) * 4, aa = hi[o + 3] * cover;
      red += hi[o] * aa; g += hi[o + 1] * aa; bl += hi[o + 2] * aa; al += aa; all += cover;
    }
    if (al) lo.set([red / al, g / al, bl / al, al / all].map(Math.round), (y * sw + x) * 4);
  }
  save(`${dir}/${name}_hd.png`, sw, sh, lo);
  cuts[name] = [a, b, w, h];
});
fs.writeFileSync(`${dir}/cuts.json`, JSON.stringify(cuts));
console.log(JSON.stringify(cuts));