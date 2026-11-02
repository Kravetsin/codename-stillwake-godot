// Ink and watercolour: draws the same shapes as the cel engine, as transparent washes on paper with a loose, broken pen line. Replaces Anime.bake, so the subjects keep their drawings.
(function () {
'use strict';
const { P, canvas, boxDown, rng } = Anime;
const hash2 = (x, y) => { let h = Math.imul(x, 374761393) + Math.imul(y, 668265263); h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
const sm = (t) => t * t * (3 - 2 * t);
function vn(x, y) { const X = Math.floor(x), Y = Math.floor(y), u = sm(x - X), v = sm(y - Y), a = hash2(X, Y), b = hash2(X + 1, Y), c = hash2(X, Y + 1), d = hash2(X + 1, Y + 1); return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v; }
function fbm(x, y, n) { let s = 0, a = .5, f = 1; for (let i = 0; i < n; i++) { s += a * vn(x * f, y * f); a *= .5; f *= 2.03; } return s; }

/** A pigment from a cel colour: greyer and a little darker, as paint on paper is beside light on a screen. */
const memo = new Map();
function pigment(c) {
  if (typeof c !== 'string' || !/^#[0-9a-f]{6}$/i.test(c)) return c;
  if (memo.has(c)) return memo.get(c);
  const r = parseInt(c.substr(1, 2), 16), g = parseInt(c.substr(3, 2), 16), b = parseInt(c.substr(5, 2), 16), l = .3 * r + .59 * g + .11 * b;
  const mix = (v) => Math.round((l + (v - l) * .8) * .97).toString(16).padStart(2, '0');
  const o = '#' + mix(r) + mix(g) + mix(b);
  memo.set(c, o);
  return o;
}

const flatCache = new Map();
function flatten(d) {
  let out = flatCache.get(d);
  if (out) return out;
  out = [];
  const tok = d.match(/[MLHVCQZ]|-?\d*\.?\d+(?:e-?\d+)?/g);
  let i = 0, cmd = '', x = 0, y = 0, cur = null;
  const num = () => parseFloat(tok[i++]);
  while (i < tok.length) {
    if (/[MLHVCQZ]/.test(tok[i])) cmd = tok[i++];
    if (cmd === 'Z') { if (cur) cur.push(cur[0]); continue; }
    if (cmd === 'M') { x = num(); y = num(); cur = [[x, y]]; out.push(cur); cmd = 'L'; continue; }
    if (cmd === 'L') { x = num(); y = num(); cur.push([x, y]); }
    else if (cmd === 'H') { x = num(); cur.push([x, y]); }
    else if (cmd === 'V') { y = num(); cur.push([x, y]); }
    else if (cmd === 'Q') { const a = num(), b = num(), e = num(), f = num(); for (let k = 1; k <= 12; k++) { const t = k / 12, u = 1 - t; cur.push([u * u * x + 2 * u * t * a + t * t * e, u * u * y + 2 * u * t * b + t * t * f]); } x = e; y = f; }
    else if (cmd === 'C') { const a = num(), b = num(), c = num(), g = num(), e = num(), f = num(); for (let k = 1; k <= 16; k++) { const t = k / 16, u = 1 - t; cur.push([u * u * u * x + 3 * u * u * t * a + 3 * u * t * t * c + t * t * t * e, u * u * u * y + 3 * u * u * t * b + 3 * u * t * t * g + t * t * t * f]); } x = e; y = f; }
  }
  flatCache.set(d, out);
  return out;
}

/** The pen: a line that wanders, swells and lifts off the paper. u: path units a world pixel; lift: how often it breaks. */
function pen(c, d, w, u, lift, seed) {
  for (const raw of flatten(d)) {
    const s = [];
    for (let i = 1; i < raw.length; i++) {
      const a = raw[i - 1], b = raw[i], L = Math.hypot(b[0] - a[0], b[1] - a[1]), n = Math.max(1, Math.ceil(L / (u * .35)));
      for (let k = 0; k < n; k++) { const t = k / n; s.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
    }
    s.push(raw[raw.length - 1]);
    if (s.length < 2) continue;
    let run = [];
    const flush = () => {
      if (run.length < 2) { run = []; return; }
      const L = [], R = [];
      run.forEach((p, i) => {
        const a = run[Math.max(0, i - 1)], b = run[Math.min(run.length - 1, i + 1)], dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1, end = Math.pow(Math.sin(Math.PI * Math.min(.96, Math.max(.04, i / (run.length - 1)))), .4);
        const q = (w / 2) * (.45 + 1.15 * vn(p[0] / u * .55 + seed, p[1] / u * .55)) * end;
        L.push([p[0] - (dy / l) * q, p[1] + (dx / l) * q]); R.unshift([p[0] + (dy / l) * q, p[1] - (dx / l) * q]);
      });
      c.beginPath();
      L.concat(R).forEach((p, i) => (i ? c.lineTo(p[0], p[1]) : c.moveTo(p[0], p[1])));
      c.closePath(); c.fill();
      run = [];
    };
    for (const p of s) {
      const X = p[0] / u, Y = p[1] / u;
      if (vn(X * .42 + 9 + seed, Y * .42) < lift) { flush(); continue; }
      const a = u * .16;
      run.push([p[0] + (fbm(X * .5 + seed, Y * .5, 2) - .375) * 2 * a, p[1] + (fbm(X * .5, Y * .5 + 5 + seed, 2) - .375) * 2 * a]);
    }
    flush();
  }
}

function record(draw) {
  const ops = [], a = {
    shape(d, m, o) { ops.push({ k: 'shape', d, m, o: o || {} }); return a; },
    line(d, o) { ops.push({ k: 'line', d, o: o || {} }); return a; },
    flat(d, c, o) { ops.push({ k: 'flat', d, c, o: o || {} }); return a; },
    tube(d, w, m, o) { ops.push({ k: 'tube', d, w, m, o: o || {} }); return a; },
    glow(d, c, o) { ops.push({ k: 'glow', d, c, o: o || {} }); return a; },
    cut(d, o) { ops.push({ k: 'cut', d, o: o || {} }); return a; },
  };
  draw(a);
  return ops.filter((o) => !o.o.only || o.o.only === 'cel');
}
const list = (v) => (v === undefined || v === null ? [] : Array.isArray(v) ? v : [v]);
const INK = '#17120f';

function wash(pic) {
  const Z = window.WZ || 1, px = (pic.px || 4) * Z, K = Z > 1 ? 1 : 3, m = pic.margin || 0, u = pic.units || 1;
  const W = Math.round((pic.w + m * 2) * px) * K, H = Math.round((pic.h + m * 2) * px) * K, sc = (px * K) / u;
  const ox0 = m * px * K + (pic.shift ? pic.shift[0] * sc : 0), oy0 = m * px * K + (pic.shift ? pic.shift[1] * sc : 0);
  let ox = ox0, oy = oy0;
  const L0 = pic.W || Anime.LINE, Lt = pic.light || Anime.LIGHT, uw = L0.panel / .14;
  const A = canvas(W, H), I = canvas(W, H), C = A.getContext('2d'), N = I.getContext('2d');
  const set = (c) => { c.setTransform(sc, 0, 0, sc, ox, oy); c.lineJoin = 'round'; c.lineCap = 'round'; };
  set(C); set(N); N.fillStyle = INK;
  const ops = record(pic.draw), seed = pic.w * 3 + pic.h;
  const T = canvas(W, H), Tx = T.getContext('2d');
  const crescent = (d, col, by) => {
    Tx.setTransform(1, 0, 0, 1, 0, 0); Tx.globalCompositeOperation = 'source-over'; Tx.clearRect(0, 0, W, H); set(Tx);
    Tx.fillStyle = col; Tx.fill(P(d)); Tx.globalCompositeOperation = 'destination-out'; Tx.translate(Lt[0] * by, Lt[1] * by); Tx.fill(P(d));
    C.save(); C.setTransform(1, 0, 0, 1, 0, 0); C.drawImage(T, 0, 0); C.restore();
  };
  const erase = (fn) => { N.save(); N.globalCompositeOperation = 'destination-out'; fn(); N.restore(); };
  // A sheet that repeats is drawn with its neighbours, so what crosses its edge comes back on the other side.
  const offsets = pic.tile ? [-1, 0, 1].flatMap((dy) => [-1, 0, 1].map((dx) => [dx, dy])) : [[0, 0]];
  for (const [dx, dy] of offsets) {
  ox = ox0 + dx * pic.w * u * sc; oy = oy0 + dy * pic.h * u * sc;
  set(C); set(N);
  // The heavy outer line is thinner here: the pen, not a sticker's border.
  for (const op of ops) {
    if (op.k === 'shape' && op.o.outer !== false) pen(N, op.d, L0.outer * .4, uw, .16, seed);
    else if (op.k === 'tube' && !op.o.bare) { N.save(); N.strokeStyle = INK; N.lineWidth = op.w + L0.outer * .28; N.stroke(P(op.d)); N.restore(); }
  }
  let i = 0;
  for (const op of ops) {
    i++;
    if (op.k === 'shape') {
      const { d, m: mt, o } = op, p = P(d);
      erase(() => { N.fillStyle = '#000'; N.fill(p); });
      C.fillStyle = pigment(mt.base); C.fill(p);
      C.save(); C.clip(p);
      for (const s of list(o.shade)) { if (typeof s === 'number') crescent(d, pigment(mt.shade), s); else { C.fillStyle = pigment(mt.shade); C.fill(P(s)); } }
      for (const s of list(o.deep)) { if (typeof s === 'number') crescent(d, pigment(mt.deep || mt.shade), s); else { C.fillStyle = pigment(mt.deep || mt.shade); C.fill(P(s)); } }
      for (const s of list(o.lite)) { C.fillStyle = pigment(mt.lite); C.globalAlpha = .55; C.fill(P(s)); C.globalAlpha = 1; }
      // The edge that catches the light, inside an object's outline: what parts it from a dark wall behind it.
      if (o.outer !== false && mt.lite) crescent(d, pigment(mt.lite), -.2 * u);
      for (const s of list(o.spec)) { C.fillStyle = '#efe9da'; C.globalAlpha = .4; C.fill(P(s)); C.globalAlpha = 1; }
      C.restore();
      if (o.edge !== false) pen(N, d, L0.edge * .75, uw, .36, seed + i);
      if (o.lines) { N.save(); N.clip(p); for (const l of list(o.lines)) pen(N, l, L0.panel * 1.1, uw, .4, seed + i); N.restore(); }
    } else if (op.k === 'line') {
      N.save(); N.fillStyle = op.o.c ? pigment(op.o.c) : INK; pen(N, op.d, L0.panel * (op.o.w || 1) * 1.3, uw, .28, seed + i); N.restore();
    } else if (op.k === 'flat') {
      erase(() => { N.fillStyle = '#000'; N.fill(P(op.d)); });
      C.save(); if (op.o.clip) C.clip(P(op.o.clip)); C.fillStyle = pigment(op.c); C.fill(P(op.d)); C.restore();
    } else if (op.k === 'tube') {
      const { d, w, m: mt, o } = op, p = P(d);
      erase(() => { N.lineWidth = w; N.strokeStyle = '#000'; N.stroke(p); });
      C.strokeStyle = pigment(mt.base); C.lineWidth = w; C.stroke(p);
      if (mt.shade !== mt.base) { C.save(); C.translate(-Lt[0] * w * .26, -Lt[1] * w * .26); C.strokeStyle = pigment(mt.shade); C.lineWidth = w * .46; C.stroke(p); C.restore(); }
      if (o.spec !== false && mt.spec) { C.save(); C.translate(Lt[0] * w * .24, Lt[1] * w * .24); C.strokeStyle = '#efe9da'; C.globalAlpha = .5; C.lineWidth = w * .2; C.stroke(p); C.restore(); }
    } else if (op.k === 'glow') {
      erase(() => { N.fillStyle = '#000'; N.fill(P(op.d)); });
      C.fillStyle = pigment(op.c); C.fill(P(op.d));
      if (op.o.core) { C.fillStyle = '#efe9da'; C.fill(P(op.o.core)); }
    } else if (op.k === 'cut') {
      for (const c of [C, N]) { c.save(); c.globalCompositeOperation = 'destination-out'; c.fillStyle = '#000'; c.strokeStyle = '#000'; c.fill(P(op.d)); if (op.o.grow) { c.lineWidth = op.o.grow; c.stroke(P(op.d)); } c.restore(); }
    }
  }
  }
  // The water: each pixel taken from a slightly wandering place, so the paint misses the line; pigment pools at the edges of a wash, thins in blooms and settles in the paper's grain.
  C.setTransform(1, 0, 0, 1, 0, 0);
  const src = C.getImageData(0, 0, W, H), s = src.data, out = C.createImageData(W, H), o = out.data, wpx = px * K;
  const B = canvas(W, H), Bx = B.getContext('2d'); Bx.filter = `blur(${wpx * .55}px)`;
  for (const [dx, dy] of offsets) Bx.drawImage(A, dx * W, dy * H);
  const b = Bx.getImageData(0, 0, W, H).data;
  const reach = wpx * .34, sx = seed * 1.7;
  // On a sheet that repeats, a noise fades into its copy one sheet away near the far edges, so the water has no seam.
  const fade = 6, seamless = (fn) => (X, Y) => {
    if (!pic.tile) return fn(X, Y);
    const ax = Math.max(0, (X - (pic.w - fade)) / fade), ay = Math.max(0, (Y - (pic.h - fade)) / fade), row = (y) => (ax ? fn(X, y) * (1 - ax) + fn(X - pic.w, y) * ax : fn(X, y));
    return ay ? row(Y) * (1 - ay) + row(Y - pic.h) * ay : row(Y);
  };
  const wanderX = seamless((X, Y) => fbm(X * .35 + sx, Y * .35, 3)), wanderY = seamless((X, Y) => fbm(X * .35, Y * .35 + 11 + sx, 3));
  const grain = seamless((X, Y) => (vn(X * 5.2 + sx, Y * 5.2) - .5) * .05 + (vn(X * 11, Y * 11 + sx) - .5) * .02), blooms = seamless((X, Y) => fbm(X * .22 + sx, Y * .22, 3)), tides = seamless((X, Y) => fbm(X * .42 + sx + 3, Y * .42 + 7, 3));
  const at = (v, n) => (pic.tile ? ((v | 0) % n + n) % n : Math.min(n - 1, Math.max(0, v | 0)));
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const X = x / wpx, Y = y / wpx, dx = (wanderX(X, Y) - .44) * 2 * reach, dy = (wanderY(X, Y) - .44) * 2 * reach;
    const u0 = at(x + dx, W), v0 = at(y + dy, H), q = (v0 * W + u0) * 4, k = (y * W + x) * 4, al = s[q + 3];
    if (!al) continue;
    const l = .3 * s[q] + .59 * s[q + 1] + .11 * s[q + 2], lb = .3 * b[q] + .59 * b[q + 1] + .11 * b[q + 2];
    const pool = Math.max(0, (lb - l)) / 255 * 1.1, gran = grain(X, Y), bloom = blooms(X, Y);
    // Thinned paint keeps its hue: mixing toward paper greys a dark colour far more than a light one, and reads as dust.
    const thin = Math.max(0, Math.min(.06, (bloom - .45) * .4));
    // Layers of wash that dried one over another: a few hard-edged steps of tone, a darker tide line along each step.
    const tide = tides(X, Y) * 7.5, step = Math.floor(tide), line = tide - step < .09 ? .04 : 0;
    const dark = 1.05 - (step % 4) * .035 - line - pool * .6 + gran;
    for (let c = 0; c < 3; c++) o[k + c] = Math.max(0, Math.min(255, s[q + c] * dark * (1 + thin)));
    o[k + 3] = al;
  }
  C.putImageData(out, 0, 0);
  C.drawImage(I, 0, 0);
  return boxDown(A, K);
}
Anime.bake = wash;
window.Wash = { wash };
})();
