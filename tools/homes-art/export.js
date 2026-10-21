// Exports the arc makers' homes (ADR 0091) from the concept page's own drawing code (decor.js, unchanged but for its require):
// each piece's picture, what of it glows, its dark look where its lamp may be out, its lights and its live emitters, into
// game/art/homes and game/Content/HomesArt.cs; the plaster's wall and back wall into game/art/planet; the cloth's and the arc
// lamp's icons; the music box's placeholder tine. Change the drawing code and run `node tools/homes-art/export.js`; never edit
// what it writes.
const fs = require('fs'), path = require('path');
const P = require('./png');
const D = require('./decor');
const { hex, mix, mul, hash, vnoise } = require('./engine');

const GAME = path.join(__dirname, '..', '..', 'game');
const HOMES = path.join(GAME, 'art', 'homes');
fs.mkdirSync(HOMES, { recursive: true });

// The pieces whose lamp the page lets go out ("lit()" in its scenes): they have a dark look too. The rest always burn, or
// never had a lamp. The resonator always talks to itself: its zap is the game's and must be seen.
const DARK = ['ceilLamp', 'sconce', 'floorLamp', 'lantern', 'lightWindow', 'kettle', 'cradle', 'chest', 'holoPortrait'];
// Looks a kind has beside its first: the chair turned the other way, the banner in the makers' indigo.
const VARIANTS = { chair: [{}, { flip: true }], banner: [{}, { col: D.PAL.indigo }] };
// The hearth's flue up to the ceiling of a room five cells high, as the scenes draw it.
const OPTIONS = { hearth: { flue: 5 * 16 - 50 } };
const M = 8;

/** A transparent canvas the drawing code paints into: albedo over a key colour, so what it blends over nothing is seen. */
class Pic {
  constructor(W, H, key) {
    this.w = { PW: W, PH: H }; const n = W * H;
    this.alb = new Float32Array(n * 3).fill(key); this.op = new Uint8Array(n); this.emis = new Float32Array(n * 3);
    this.isE = new Uint8Array(n); this.nob = new Uint8Array(n); this.lights = []; this.emitters = [];
  }
  inb(x, y) { return x >= 0 && y >= 0 && x < this.w.PW && y < this.w.PH; }
  set(x, y, c) { x |= 0; y |= 0; if (!this.inb(x, y)) return; const i = y * this.w.PW + x; this.alb.set(c, i * 3); this.op[i] = 1; this.isE[i] = 0; this.emis.fill(0, i * 3, i * 3 + 3); }
  glow(x, y, c, k = 1) { x |= 0; y |= 0; if (!this.inb(x, y)) return; const i = y * this.w.PW + x; this.alb.set(c, i * 3); this.emis.set(mul(c, k), i * 3); this.isE[i] = 1; this.op[i] = 1; }
  light(l) { this.lights.push(l); }
}

function blur(src, W, H, rad) {
  const n = W * H, tmp = new Float32Array(n * 3), dst = new Float32Array(n * 3), wts = []; let sum = 0;
  for (let k = -rad * 3; k <= rad * 3; k++) { const v = Math.exp(-(k * k) / (2 * rad * rad)); wts.push(v); sum += v; }
  for (let k = 0; k < wts.length; k++) wts[k] /= sum; const R = rad * 3;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { let a = 0, b = 0, c = 0;
    for (let k = -R; k <= R; k++) { const xx = x + k; if (xx < 0 || xx >= W) continue; const j = (y * W + xx) * 3, wv = wts[k + R]; a += src[j] * wv; b += src[j + 1] * wv; c += src[j + 2] * wv; }
    const o = (y * W + x) * 3; tmp[o] = a; tmp[o + 1] = b; tmp[o + 2] = c; }
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { let a = 0, b = 0, c = 0;
    for (let k = -R; k <= R; k++) { const yy = y + k; if (yy < 0 || yy >= H) continue; const j = (yy * W + x) * 3, wv = wts[k + R]; a += tmp[j] * wv; b += tmp[j + 1] * wv; c += tmp[j + 2] * wv; }
    const o = (y * W + x) * 3; dst[o] = a; dst[o + 1] = b; dst[o + 2] = c; }
  return dst;
}

const byte = (v) => Math.round(Math.min(1, Math.max(0, v)) * 255);

/** One look of a piece: drawn over black and over white, so a pixel blended over nothing keeps its alpha; and its glow. */
function render(id, opts) {
  const pc = D.PIECES[id], top = (opts.flue || 0) + M, W = pc.w + 2 * M, H = pc.h + top + M;
  const draw = (key) => { const p = new Pic(W, H, key); pc.draw(p, M, top, { lit: true, ...opts }); return p; };
  const a = draw(0), b = draw(1), n = W * H;
  const rgba = new Float32Array(n * 4);
  for (let i = 0; i < n; i++) {
    if (a.op[i]) { rgba.set([a.alb[i * 3], a.alb[i * 3 + 1], a.alb[i * 3 + 2], 1], i * 4); continue; }
    const k = ((b.alb[i * 3] - a.alb[i * 3]) + (b.alb[i * 3 + 1] - a.alb[i * 3 + 1]) + (b.alb[i * 3 + 2] - a.alb[i * 3 + 2])) / 3, al = 1 - k;
    if (al < 0.01) continue;
    rgba.set([a.alb[i * 3] / al, a.alb[i * 3 + 1] / al, a.alb[i * 3 + 2] / al, al], i * 4);
  }
  // What glows: each glowing pixel and its tight bloom (the page's), a fine one's bloom faint (its nob).
  const E = new Float32Array(n * 3), B = new Float32Array(n * 3);
  let glows = false;
  for (let i = 0; i < n; i++) if (a.isE[i]) { glows = true; const nb = a.nob[i] ? 0.12 : 1; for (let c = 0; c < 3; c++) { E[i * 3 + c] = a.emis[i * 3 + c]; B[i * 3 + c] = a.emis[i * 3 + c] * nb; } }
  let glow = null;
  if (glows) {
    const b1 = blur(B, W, H, 2); glow = new Float32Array(n * 4);
    for (let i = 0; i < n; i++) {
      const v = [0, 1, 2].map((c) => E[i * 3 + c] + b1[i * 3 + c] * 0.9), al = Math.min(1, Math.max(...v));
      if (al < 0.01) continue;
      glow.set([v[0] / al, v[1] / al, v[2] / al, al], i * 4);
    }
  }
  return { W, H, top, rgba, glow, lights: a.lights, emitters: a.emitters };
}

/** The box round everything drawn in any of <looks>, glow included, a pixel more. */
function boundsOf(looks) {
  let x0 = Infinity, y0 = Infinity, x1 = -1, y1 = -1;
  for (const r of looks) for (let y = 0; y < r.H; y++) for (let x = 0; x < r.W; x++) {
    const i = (y * r.W + x) * 4;
    if (r.rgba[i + 3] > 0 || (r.glow && r.glow[i + 3] > 0.02)) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
  }
  return { x0: Math.max(0, x0 - 1), y0: Math.max(0, y0 - 1), x1: Math.min(looks[0].W - 1, x1 + 1), y1: Math.min(looks[0].H - 1, y1 + 1) };
}

function write(src, W, bx, file) {
  const w = bx.x1 - bx.x0 + 1, h = bx.y1 - bx.y0 + 1, img = P.create(w, h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const s = ((y + bx.y0) * W + x + bx.x0) * 4, o = (y * w + x) * 4;
    if (src[s + 3] <= 0) continue;
    img.data[o] = byte(src[s]); img.data[o + 1] = byte(src[s + 1]); img.data[o + 2] = byte(src[s + 2]); img.data[o + 3] = byte(src[s + 3]);
  }
  P.encode(img, file);
  return [w, h];
}

const f = (v) => { const r = Math.round(v * 100) / 100; return Number.isInteger(r) ? `${r}` : `${r}f`; };
const c255 = (col) => col.map((v) => (v <= 1 && !Number.isInteger(v) ? Math.round(v * 255) : v === 1 ? 255 : v));
function emitterCs(e, ox, oy) {
  const at = (x, y) => `${f(x - ox)}, ${f(y - oy)}`;
  const col = (c) => `Colour = (${c255(c).join(', ')})`;
  switch (e.kind) {
    case 'arc': {
      const air = e.b === 'air';
      const parts = [`X2 = ${air ? 0 : f(e.b[0] - ox)}`, `Y2 = ${air ? 0 : f(e.b[1] - oy)}`, `Rate = ${f(e.rate || 0.02)}`, `Life = ${f(e.life || 4)}`, `Width = ${f(e.w || 0.6)}`];
      if (e.light) parts.push(`Light = ${f(e.light)}`);
      if (air) parts.push('ToAir = true', `Reach = ${f(e.reach || 10)}`);
      if (e.beat) parts.push('Beat = true');
      return `new(Emits.Arc, ${at(e.a[0], e.a[1])}) { ${parts.join(', ')} }`;
    }
    case 'pulse': return `new(Emits.Pulse, ${at(e.x, e.y)}) { Radius = ${f(e.r)}, ${col(e.col)}, Amp = ${f(e.amp)}${e.slow ? ', Slow = true' : ''} }`;
    case 'sputter': return `new(Emits.Sputter, ${at(e.x, e.y)}) { Radius = ${f(e.r)}, X2 = ${f(e.b[0] - ox)}, Y2 = ${f(e.b[1] - oy)}, From = (${f(e.a[0] - ox)}, ${f(e.a[1] - oy)}) }`;
    case 'mobile': return `new(Emits.Mobile, ${at(e.x, e.y)}) { Radius = ${f(e.rx)}, Radius2 = ${f(e.ry)} }`;
    case 'holo': return `new(Emits.Holo, ${at(e.x, e.y)}) { Width = ${f(e.w)}, Height = ${f(e.h)} }`;
    case 'pixel': return `new(Emits.Pixel, ${at(e.x, e.y)})`;
    case 'twinkle': return `new(Emits.Twinkle, 0, 0) { Points = [${e.pts.map((q) => `(${at(q[0], q[1])})`).join(', ')}] }`;
    case 'spore': return `new(Emits.Spore, ${at(e.x, e.y)}) { ${col(e.col)}, Count = ${e.n || 4} }`;
    case 'embers': return `new(Emits.Embers, ${at(e.x, e.y)}) { Width = ${f(e.w)}, ${col(e.col)} }`;
    case 'motes': return `new(Emits.Motes, ${at(e.x0, e.y0)}) { X2 = ${f(e.x1 - ox)}, Y2 = ${f(e.y1 - oy)}, ${col(e.col)}, Count = ${e.n} }`;
    case 'sway': return `new(Emits.Sway, ${at(e.x, e.y)})`;
    default: throw new Error('emitter ' + e.kind);
  }
}

const entries = [];
for (const id of Object.keys(D.PIECES)) {
  const variants = (VARIANTS[id] || [{}]).map((v) => ({ ...(OPTIONS[id] || {}), ...v }));
  const looks = variants.map((v) => render(id, v));
  const darks = DARK.includes(id) ? variants.map((v) => render(id, { ...v, lit: false })) : [];
  const bx = boundsOf([...looks, ...darks]);
  let size;
  looks.forEach((r, v) => {
    size = write(r.rgba, r.W, bx, path.join(HOMES, `${id}_${v}.png`));
    if (r.glow) write(r.glow, r.W, bx, path.join(HOMES, `${id}_${v}_glow.png`));
    else if (fs.existsSync(path.join(HOMES, `${id}_${v}_glow.png`))) fs.unlinkSync(path.join(HOMES, `${id}_${v}_glow.png`));
  });
  darks.forEach((r, v) => {
    write(r.rgba, r.W, bx, path.join(HOMES, `${id}_${v}_dark.png`));
    if (r.glow) write(r.glow, r.W, bx, path.join(HOMES, `${id}_${v}_dark_glow.png`));
  });
  const r = looks[0], pc = D.PIECES[id], ox = M, oy = r.top;
  const lights = r.lights.map((l) => `new(${f(l.x - ox)}, ${f(l.y - oy)}, (${l.col.map(f).join(', ')}), ${f(l.r)}, ${f(l.k || 1)})`);
  const emitters = r.emitters.map((e) => emitterCs(e, ox, oy));
  entries.push(`        ["${id}"] = new(${variants.length}, ${pc.w}, ${pc.h}, ${M - bx.x0}, ${r.top - bx.y0}, ${size[0]}, ${size[1]}, Dark: ${darks.length > 0})\n` +
    `        {\n            Lights = [${lights.join(', ')}],\n            Emitters = [${emitters.join(', ')}],\n        },`);
}

const cs = `// Generated by tools/homes-art/export.js from the homes concept page's drawing code (ADR 0091): export again, never edit.
using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class HomesArt
{
    /// <summary>Every piece's pictures (<c>art/homes/{kind}_{variant}</c>), its box in them, its lights and its live emitters, from its box's corner.</summary>
    public static readonly IReadOnlyDictionary<string, Picture> Of = new Dictionary<string, Picture>
    {
${entries.join('\n')}
    };
}
`;
fs.writeFileSync(path.join(GAME, 'Content', 'HomesArt.cs'), cs);
console.log(entries.length, 'pieces');

// ---- plaster: the homes' warm back wall (decor's backColor), pre-darkened so the game's recede (0088) brings it to the page's ----
const PLASTER = (x, y) => {
  let c = mix(hex('#5c4f45'), hex('#6a5a4c'), vnoise(x, y, 11, 21));
  if (vnoise(x, y, 4, 22) > 0.78) c = mul(c, 1.06);
  if ((y % 24) === 0 && hash(x >> 3, y, 23) < 0.8) c = mul(c, 0.8);
  const h = hash(x, y, 24); if (h < 0.03) c = mul(c, 0.82); else if (h > 0.985) c = mul(c, 1.15);
  return c;
};
// GroundLooks.BackDim, BackPale, BackCool and CoolShift: what the game's recede does to a back wall's picture, undone.
const DIM = 0.62, PALE = 0.3, COOL = 0.6, SHIFT = [-0.14, -0.05, 0.16], L = [0.3, 0.59, 0.11];
const unrecede = (t) => {
  const y = t.map((v, i) => v / ((1 + SHIFT[i] * COOL) * DIM));
  const g = L[0] * y[0] + L[1] * y[1] + L[2] * y[2];
  // (1 - PALE) s + PALE (L.s) 1 = y: L.s = L.y, so s = (y - PALE (L.y)) / (1 - PALE).
  return y.map((v) => (v - PALE * g) / (1 - PALE));
};
const sheet = (fn) => { const img = P.create(128, 64); for (let y = 0; y < 64; y++) for (let x = 0; x < 128; x++) { const c = fn(x, y), o = (y * 128 + x) * 4; if (!c) continue; img.data[o] = byte(c[0]); img.data[o + 1] = byte(c[1]); img.data[o + 2] = byte(c[2]); img.data[o + 3] = 255; } return img; };
const PLANET = path.join(GAME, 'art', 'planet');
P.encode(sheet((x, y) => unrecede(mul(PLASTER(x, y), 0.62))), path.join(PLANET, 'back_plaster.png'));
P.encode(sheet((x, y) => PLASTER(x, y)), path.join(PLANET, 'wall_plaster.png'));
// No wall of plaster stands anywhere: no living edge, no details.
P.encode(sheet(() => null), path.join(PLANET, 'top_plaster.png'));
P.encode(sheet(() => null), path.join(PLANET, 'detail_plaster.png'));

// ---- the new items' icons, 16 x 16, drawn with the page's own palette and primitives ----
function icon(file, paint) {
  const a = new Pic(16, 16, 0); paint(a);
  const img = P.create(16, 16);
  for (let i = 0; i < 256; i++) if (a.op[i]) { const o = i * 4; img.data[o] = byte(a.alb[i * 3]); img.data[o + 1] = byte(a.alb[i * 3 + 1]); img.data[o + 2] = byte(a.alb[i * 3 + 2]); img.data[o + 3] = 255; }
  P.encode(img, path.join(GAME, 'art', 'items', file));
}
const A = D.PAL.alloy, ARC = D.ARC;
icon('arcLamp.png', (p) => {
  // The ceiling lamp's cap over its globe, lit: a whole lamp of theirs.
  D.R(p, 7, 1, 2, 2, A[0]); D.abox(p, 4, 3, 8, 3);
  for (let y = -4; y <= 4; y++) for (let x = -4; x <= 4; x++) {
    const d = Math.sqrt(x * x + y * y); if (d > 4.3) continue;
    p.set(8 + x, 10 + y, d > 3.4 ? ARC.lo : d < 1.5 ? ARC.core : d < 2.8 ? ARC.hi : ARC.mid);
  }
});
icon('cloth.png', (p) => {
  // A folded length of the makers' red cloth, its ochre band.
  const R = D.PAL.red, O = D.PAL.ochre;
  D.box(p, 2, 4, 12, 4, R, { round: true }); D.box(p, 1, 7, 14, 5, R, { round: true });
  for (let x = 2; x < 14; x++) p.set(x, 9, O[3]);
  p.set(3, 5, R[4]); p.set(2, 8, R[4]);
});

// ---- placeholder one-shots till the user picks (Sound.cs): synthesised, normalised to -3 dBFS peak ----
function wav(name, secs, sample) {
  const rate = 44100, n = Math.round(rate * secs), data = Buffer.alloc(44 + n * 2), s = new Float64Array(n);
  let peak = 0;
  for (let i = 0; i < n; i++) { s[i] = sample(i / rate, i); peak = Math.max(peak, Math.abs(s[i])); }
  const k = 0.708 / peak;
  data.write('RIFF', 0); data.writeUInt32LE(36 + n * 2, 4); data.write('WAVE', 8); data.write('fmt ', 12); data.writeUInt32LE(16, 16);
  data.writeUInt16LE(1, 20); data.writeUInt16LE(1, 22); data.writeUInt32LE(rate, 24); data.writeUInt32LE(rate * 2, 28); data.writeUInt16LE(2, 32); data.writeUInt16LE(16, 34);
  data.write('data', 36); data.writeUInt32LE(n * 2, 40);
  for (let i = 0; i < n; i++) data.writeInt16LE(Math.round(s[i] * k * 32767), 44 + i * 2);
  fs.mkdirSync(path.join(GAME, 'audio', 'ruins'), { recursive: true });
  fs.writeFileSync(path.join(GAME, 'audio', 'ruins', `${name}-1.wav`), data);
}
// The music box's tine: a plucked steel tine, its inharmonic partials decaying, played at a tune's pitches.
const TINE = [[1, 1, 3.2], [2.756, 0.32, 6.5], [5.404, 0.12, 11], [8.933, 0.05, 16]];
wav('music-box', 0.9, (t) => Math.min(1, t / 0.002) * TINE.reduce((v, [m, a, d]) => v + a * Math.exp(-d * t) * Math.sin(2 * Math.PI * 1568 * m * t), 0));
// The resonator's zap: a short crackle, bursts of noise each a few milliseconds, falling off over a quarter second.
let seed = 7; const rnd = () => ((seed = (Math.imul(seed, 1103515245) + 12345) >>> 0) / 4294967296);
let burst = 0, prev = 0;
wav('zap', 0.28, (t) => { if (rnd() < 0.004) burst = 0.6 + rnd() * 0.4; burst *= 0.9985; const white = rnd() * 2 - 1; prev = prev * 0.35 + white * 0.65; return (prev * burst + white * 0.08) * Math.exp(-9 * t) * Math.min(1, t / 0.001); });
console.log('plaster, icons, tine');
