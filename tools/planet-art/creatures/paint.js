// node paint.js [outDir] [designId ...] — the loper's parts as index maps (R ramp 0-31, G tone 0-5, A painted), their glow maps,
// colour previews in every palette and a manifest per design; manifest.json beside them holds the palettes' ramps.
// Plain Node, no packages. Painted from loper-parts.js, the same code the pick page runs.
const fs = require('fs'), path = require('path'), zlib = require('zlib');
const L = require('./loper-parts.js');
const OUT = process.argv[2] || __dirname, ONLY = process.argv.slice(3);

function crc32(buf) { let c, crc = 0xffffffff; for (let n = 0; n < buf.length; n++) { c = (crc ^ buf[n]) & 0xff; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; } return (crc ^ 0xffffffff) >>> 0; }
function png(file, w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) Buffer.from(rgba.buffer, rgba.byteOffset + y * w * 4, w * 4).copy(raw, y * (w * 4 + 1) + 1);
  const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc32(td)); return Buffer.concat([l, td, c]); };
  const ih = Buffer.alloc(13); ih.writeUInt32BE(w, 0); ih.writeUInt32BE(h, 4); ih[8] = 8; ih[9] = 6;
  fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]));
}
const EYE = (n) => /^(eye_|lid_|pupil)/.test(n);

for (const d of L.DESIGNS) {
  if (ONLY.length && !ONLY.includes(d.id)) continue;
  const b = L.build(d), dir = path.join(OUT, d.id); fs.mkdirSync(dir, { recursive: true });
  const kids = {}; for (const n of d.rig) if (n.parent) { const p = d.rig.find((r) => r.id === n.parent).part; (kids[p] = kids[p] || {})[n.id] = n.at; }
  const parts = {};
  for (const [name, P] of Object.entries(b.parts)) {
    png(path.join(dir, name + '.png'), P.w, P.h, L.indexMap(d, P));
    const glow = P.role.some((r, q) => r === 'glow' && P.tone[q] >= 3);
    if (glow) { const m = L.indexMap(d, P); for (let q = 0; q < P.w * P.h; q++) if (P.role[q] !== 'glow' || P.tone[q] < 3) m[q * 4 + 3] = 0; png(path.join(dir, name + '_glow.png'), P.w, P.h, m); }
    parts[name] = { file: name + '.png', size: [P.w, P.h], pivot: [-P.ox, -P.oy], ramps: [...new Set(P.role.filter((r) => r))].map((r) => L.RAMP_INDEX[d.ramps[r] || r]), ...(glow ? { glow: name + '_glow.png' } : {}), ...(kids[name] ? { attach: kids[name] } : {}) };
  }
  // a colour sheet per palette: every part side by side, 4x, to check a bake against
  for (const [pk, pal] of Object.entries(L.PALETTES)) {
    const K = 4, list = Object.values(b.parts), W = list.reduce((s, P) => s + P.w * K + 4, 4), H = Math.max(...list.map((P) => P.h)) * K + 8, img = new Uint8ClampedArray(W * H * 4);
    for (let i = 0; i < W * H; i++) img.set([28, 42, 45, 255], i * 4);
    let x0 = 4;
    for (const P of list) { const c = L.colours(d, P, pal); for (let y = 0; y < P.h * K; y++) for (let x = 0; x < P.w * K; x++) { const q = (((y / K) | 0) * P.w + ((x / K) | 0)) * 4; if (c[q + 3]) img.set(c.slice(q, q + 4), ((y + 4) * W + x0 + x) * 4); } x0 += P.w * K + 4; }
    png(path.join(dir, `preview_${pk}.png`), W, H, img);
  }
  const manifest = {
    design: d.id, name: d.name, cell: 16, box: [32, 32], origin: 'middle of the feet, facing right, y down; pixels of the world', gait: d.gait,
    indexMap: 'R = ramp index (manifest.json ramps), G = tone 0 outline, 1 deep, 2 shade, 3 base, 4 lit, 5 highlight; A = painted. _glow: the same, glowing pixels only, drawn unlit and added',
    ramps: Object.fromEntries(Object.entries(d.ramps).map(([role, slot]) => [role, L.RAMP_INDEX[slot]])),
    parts,
    eye: { at: d.eye.at, states: { open: ['eye_open', 'pupil'], half: ['eye_open', 'pupil', 'lid_half'], blink: ['eye_open', 'lid_blink'], wide: ['eye_wide', 'pupil_wide'] }, pupilTravel: b.eye.travel.map((v) => +v.toFixed(2)), pupilTravelWide: b.eye.travelWide.map((v) => +v.toFixed(2)), pupilSnap: 'offset rounded to whole pixels' },
    rig: d.rig.map((n) => ({ id: n.id, part: n.part, parent: n.parent, at: n.at, ...(n.drive ? { drive: n.drive } : {}), ...(n.phase != null ? { phase: n.phase } : {}), ...(n.hind ? { hind: true } : {}), ...(n.base ? { base: n.base } : {}) })),
    pose: d.pose,
  };
  fs.writeFileSync(path.join(dir, 'manifest.json'), JSON.stringify(manifest, null, 1));
  console.log(d.id, Object.keys(parts).length, 'parts');
}
fs.writeFileSync(path.join(OUT, 'manifest.json'), JSON.stringify({
  rampIndex: L.RAMP_INDEX, tones: ['outline', 'deep', 'shade', 'base', 'lit', 'highlight'],
  palettes: Object.fromEntries(Object.entries(L.PALETTES).map(([k, p]) => [k, { name: p.name, ramps: Object.fromEntries(Object.entries(p.ramps).map(([r, c]) => [L.RAMP_INDEX[r], c])) }])),
  designs: L.DESIGNS.map((d) => d.id),
}, null, 1));
