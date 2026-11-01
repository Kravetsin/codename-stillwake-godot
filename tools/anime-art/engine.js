// The anime-cel finish: a picture records its shapes as bezier paths, then one colour plate and one ink plate are drawn large and boxed down.
// Runs in a browser page (export.js opens it headless). Line widths are in world pixels, so every subject carries the same line.
(function () {
  'use strict';
  const n2 = (v) => +v.toFixed(3);
  function ell(cx, cy, rx, ry) {
    const R = ry === undefined ? rx : ry, a = rx * 0.5523, b = R * 0.5523;
    return `M${n2(cx - rx)} ${n2(cy)}C${n2(cx - rx)} ${n2(cy - b)} ${n2(cx - a)} ${n2(cy - R)} ${n2(cx)} ${n2(cy - R)}C${n2(cx + a)} ${n2(cy - R)} ${n2(cx + rx)} ${n2(cy - b)} ${n2(cx + rx)} ${n2(cy)}C${n2(cx + rx)} ${n2(cy + b)} ${n2(cx + a)} ${n2(cy + R)} ${n2(cx)} ${n2(cy + R)}C${n2(cx - a)} ${n2(cy + R)} ${n2(cx - rx)} ${n2(cy + b)} ${n2(cx - rx)} ${n2(cy)}Z`;
  }
  function rr(x, y, w, h, r) {
    const q = r === undefined ? Math.min(w, h) * 0.2 : r;
    return `M${n2(x + q)} ${n2(y)}L${n2(x + w - q)} ${n2(y)}Q${n2(x + w)} ${n2(y)} ${n2(x + w)} ${n2(y + q)}L${n2(x + w)} ${n2(y + h - q)}Q${n2(x + w)} ${n2(y + h)} ${n2(x + w - q)} ${n2(y + h)}L${n2(x + q)} ${n2(y + h)}Q${n2(x)} ${n2(y + h)} ${n2(x)} ${n2(y + h - q)}L${n2(x)} ${n2(y + q)}Q${n2(x)} ${n2(y)} ${n2(x + q)} ${n2(y)}Z`;
  }
  function poly() {
    const a = arguments.length === 1 ? arguments[0] : arguments;
    let s = `M${n2(a[0])} ${n2(a[1])}`;
    for (let i = 2; i < a.length; i += 2) s += `L${n2(a[i])} ${n2(a[i + 1])}`;
    return s + 'Z';
  }
  function star(x, y, r, t) {
    const q = r * (t || 0.16);
    return poly(x, y - r, x + q, y - q, x + r, y, x + q, y + q, x, y + r, x - q, y + q, x - r, y, x - q, y - q);
  }
  function rng(a) { return () => { a |= 0; a = (a + 0x6d2b79f5) | 0; let t = Math.imul(a ^ (a >>> 15), 1 | a); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
  const pathCache = new Map();
  function P(d) { let p = pathCache.get(d); if (!p) { p = new Path2D(d); pathCache.set(d, p); } return p; }
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
      if (cmd === 'Z') { if (cur) { cur.closed = true; cur.push(cur[0]); } continue; }
      if (cmd === 'M') { x = num(); y = num(); cur = [[x, y]]; out.push(cur); cmd = 'L'; continue; }
      if (cmd === 'L') { x = num(); y = num(); cur.push([x, y]); }
      else if (cmd === 'H') { x = num(); cur.push([x, y]); }
      else if (cmd === 'V') { y = num(); cur.push([x, y]); }
      else if (cmd === 'Q') {
        const a = num(), b = num(), e = num(), f = num();
        for (let k = 1; k <= 12; k++) { const t = k / 12, u = 1 - t; cur.push([u * u * x + 2 * u * t * a + t * t * e, u * u * y + 2 * u * t * b + t * t * f]); }
        x = e; y = f;
      } else if (cmd === 'C') {
        const a = num(), b = num(), c = num(), g = num(), e = num(), f = num();
        for (let k = 1; k <= 16; k++) { const t = k / 16, u = 1 - t; cur.push([u * u * u * x + 3 * u * u * t * a + 3 * u * t * t * c + t * t * t * e, u * u * u * y + 3 * u * u * t * b + 3 * u * t * t * g + t * t * t * f]); }
        x = e; y = f;
      }
    }
    flatCache.set(d, out);
    return out;
  }
  /** A line that comes to a point at both ends, as a pen does. */
  function taper(c, d, w) {
    for (const s of flatten(d)) {
      let len = 0;
      const at = [0];
      for (let i = 1; i < s.length; i++) { len += Math.hypot(s[i][0] - s[i - 1][0], s[i][1] - s[i - 1][1]); at.push(len); }
      if (!len) continue;
      const L = [], R = [];
      for (let i = 0; i < s.length; i++) {
        const a = s[Math.max(0, i - 1)], b = s[Math.min(s.length - 1, i + 1)];
        const dx = b[0] - a[0], dy = b[1] - a[1], l = Math.hypot(dx, dy) || 1;
        const q = (Math.pow(Math.sin(Math.PI * Math.max(0.06, Math.min(0.94, at[i] / len))), 0.55) * w) / 2;
        L.push([s[i][0] - (dy / l) * q, s[i][1] + (dx / l) * q]);
        R.unshift([s[i][0] + (dy / l) * q, s[i][1] - (dx / l) * q]);
      }
      c.beginPath();
      L.concat(R).forEach((p, i) => (i ? c.lineTo(p[0], p[1]) : c.moveTo(p[0], p[1])));
      c.closePath();
      c.fill();
    }
  }
  function canvas(w, h) { const c = document.createElement('canvas'); c.width = Math.max(1, Math.round(w)); c.height = Math.max(1, Math.round(h)); return c; }
  function record(draw) {
    const ops = [];
    const a = {
      /** A solid with a line round it. o: shade, deep, lite, spec, rim, lines, outer, edge. */
      shape(d, m, o) { ops.push({ k: 'shape', d, m, o: o || {} }); return a; },
      /** A thin line on what is drawn. o: w (a multiple of the panel line), c. */
      line(d, o) { ops.push({ k: 'line', d, o: o || {} }); return a; },
      /** A flat patch with no line. o: clip. */
      flat(d, c, o) { ops.push({ k: 'flat', d, c, o: o || {} }); return a; },
      /** A stroked bar with its line. o: bare, spec. */
      tube(d, w, m, o) { ops.push({ k: 'tube', d, w, m, o: o || {} }); return a; },
      /** A light source: flat here. o: core. */
      glow(d, c, o) { ops.push({ k: 'glow', d, c, o: o || {} }); return a; },
      /** A hole through both plates. o: grow, a line width round it too. */
      cut(d, o) { ops.push({ k: 'cut', d, o: o || {} }); return a; },
    };
    draw(a);
    return ops.filter((o) => !o.o.only || o.o.only === 'cel');
  }
  const toList = (v) => (v === undefined || v === null ? [] : Array.isArray(v) ? v : [v]);
  const INK = '#0f111b';
  function drawOps(ops, pic, C, I, sc, ox, oy, W, H) {
    const W0 = pic.W, Lt = pic.light;
    const set = (c) => { c.setTransform(sc, 0, 0, sc, ox, oy); c.lineJoin = 'round'; c.lineCap = 'round'; };
    set(C); set(I);
    const inkOf = (m) => m.ink || INK;
    let T = null, Tx = null;
    const crescent = (d, col, by) => {
      if (!T) { T = canvas(W, H); Tx = T.getContext('2d'); }
      Tx.setTransform(1, 0, 0, 1, 0, 0);
      Tx.globalCompositeOperation = 'source-over';
      Tx.clearRect(0, 0, W, H);
      set(Tx);
      Tx.fillStyle = col;
      Tx.fill(P(d));
      Tx.globalCompositeOperation = 'destination-out';
      Tx.translate(Lt[0] * by, Lt[1] * by);
      Tx.fill(P(d));
      C.save();
      C.setTransform(1, 0, 0, 1, 0, 0);
      C.drawImage(T, 0, 0);
      C.restore();
    };
    for (const op of ops) {
      if (op.k === 'shape' && op.o.outer !== false) { I.strokeStyle = inkOf(op.m); I.lineWidth = W0.outer; I.stroke(P(op.d)); }
      else if (op.k === 'tube' && !op.o.bare) { I.strokeStyle = inkOf(op.m); I.lineWidth = op.w + W0.outer; I.stroke(P(op.d)); }
    }
    let lastMat = null;
    const eraseInk = (fn) => { I.save(); I.globalCompositeOperation = 'destination-out'; fn(); I.restore(); };
    for (const op of ops) {
      if (op.k === 'shape') {
        const { d, m, o } = op, p = P(d);
        lastMat = m;
        eraseInk(() => { I.fillStyle = '#000'; I.fill(p); });
        C.fillStyle = m.base;
        C.fill(p);
        C.save();
        C.clip(p);
        for (const s of toList(o.shade)) { if (typeof s === 'number') crescent(d, m.shade, s); else { C.fillStyle = m.shade; C.fill(P(s)); } }
        for (const s of toList(o.deep)) { if (typeof s === 'number') crescent(d, m.deep || m.shade, s); else { C.fillStyle = m.deep || m.shade; C.fill(P(s)); } }
        for (const s of toList(o.lite)) { C.fillStyle = m.lite; C.fill(P(s)); }
        if (o.rim) crescent(d, m.rim, o.rim);
        for (const s of toList(o.spec)) { C.fillStyle = m.spec; C.fill(P(s)); }
        C.restore();
        if (o.edge !== false) { I.strokeStyle = inkOf(m); I.lineWidth = W0.edge; I.stroke(p); }
        if (o.lines) {
          I.save(); I.clip(p); I.fillStyle = inkOf(m);
          for (const l of toList(o.lines)) taper(I, l, W0.panel);
          I.restore();
        }
      } else if (op.k === 'line') {
        I.fillStyle = op.o.c || inkOf(lastMat || {});
        taper(I, op.d, W0.panel * (op.o.w || 1));
      } else if (op.k === 'flat') {
        eraseInk(() => { I.fillStyle = '#000'; I.fill(P(op.d)); });
        C.save();
        if (op.o.clip) C.clip(P(op.o.clip));
        C.fillStyle = op.c;
        C.fill(P(op.d));
        C.restore();
      } else if (op.k === 'tube') {
        const { d, w, m, o } = op, p = P(d);
        lastMat = m;
        eraseInk(() => { I.lineWidth = w; I.strokeStyle = '#000'; I.stroke(p); });
        C.strokeStyle = m.base; C.lineWidth = w; C.stroke(p);
        if (m.shade !== m.base) {
          C.save(); C.translate(-Lt[0] * w * 0.26, -Lt[1] * w * 0.26);
          C.strokeStyle = m.shade; C.lineWidth = w * 0.46; C.stroke(p);
          C.restore();
        }
        if (o.spec !== false && m.spec) {
          C.save(); C.translate(Lt[0] * w * 0.24, Lt[1] * w * 0.24);
          C.strokeStyle = m.spec; C.lineWidth = w * 0.2; C.stroke(p);
          C.restore();
        }
      } else if (op.k === 'glow') {
        eraseInk(() => { I.fillStyle = '#000'; I.fill(P(op.d)); });
        C.fillStyle = op.c; C.fill(P(op.d));
        if (op.o.core) { C.fillStyle = op.o.coreC || '#ffffff'; C.fill(P(op.o.core)); }
      } else if (op.k === 'cut') {
        for (const c of [C, I]) {
          c.save(); c.globalCompositeOperation = 'destination-out'; c.fillStyle = '#000'; c.strokeStyle = '#000';
          c.fill(P(op.d));
          if (op.o.grow) { c.lineWidth = op.o.grow; c.stroke(P(op.d)); }
          c.restore();
        }
      }
    }
  }
  /** Reduced by an exact box, premultiplied so no dark fringe; no sharpening: the game's mipmaps do the rest. */
  function boxDown(c, f) {
    if (f === 1) return c;
    const W = c.width, H = c.height, w = Math.floor(W / f), h = Math.floor(H / f);
    const a = c.getContext('2d').getImageData(0, 0, W, H).data;
    const o = canvas(w, h), x = o.getContext('2d'), out = x.createImageData(w, h), b = out.data, n = f * f;
    for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
      let R = 0, G = 0, B = 0, A = 0;
      for (let y = 0; y < f; y++) for (let e = 0; e < f; e++) {
        const q = ((j * f + y) * W + i * f + e) * 4, al = a[q + 3];
        R += a[q] * al; G += a[q + 1] * al; B += a[q + 2] * al; A += al;
      }
      const q = (j * w + i) * 4;
      if (A > 0) { b[q] = R / A; b[q + 1] = G / A; b[q + 2] = B / A; b[q + 3] = A / n; }
    }
    x.putImageData(out, 0, 0);
    return o;
  }
  /** The one line of every subject, in world pixels: heavy outside, thin between shapes, thinner on panels. */
  const LINE = { outer: 0.9, edge: 0.26, panel: 0.14 };
  /** The one light of the world: from the upper left. The hero's comes from the way it faces and mirrors with it. */
  const LIGHT = [-0.6, -0.8];
  /**
   * A picture's finished bitmap. pic: w, h and margin in world pixels of the picture; units, its paths' units a world pixel; px, the output's
   * pixels a world pixel (4, HiRes.Scale); K, how much larger it is drawn first; light; W {outer, edge, panel} in its units; draw(a);
   * tile, drawn wrapped; shift [x, y], its paths moved, in its units.
   */
  function bake(pic) {
    const px = pic.px || 4, K = pic.K || 4, m = pic.margin || 0, u = pic.units || 1;
    const W = Math.round((pic.w + m * 2) * px) * K, H = Math.round((pic.h + m * 2) * px) * K;
    const sc = (px * K) / u, ox = m * px * K + (pic.shift ? pic.shift[0] * sc : 0), oy = m * px * K + (pic.shift ? pic.shift[1] * sc : 0);
    const C = canvas(W, H), I = canvas(W, H), cx = C.getContext('2d'), ix = I.getContext('2d');
    const ops = record(pic.draw);
    const finish = { light: LIGHT, ...pic, W: pic.W || LINE };
    if (pic.tile) for (const dy of [-1, 0, 1]) for (const dx of [-1, 0, 1]) drawOps(ops, finish, cx, ix, sc, ox + dx * pic.w * u * sc, oy + dy * pic.h * u * sc, W, H);
    else drawOps(ops, finish, cx, ix, sc, ox, oy, W, H);
    cx.setTransform(1, 0, 0, 1, 0, 0);
    cx.drawImage(I, 0, 0);
    return boxDown(C, K);
  }
  window.Anime = { ell, rr, poly, star, rng, P, canvas, bake, boxDown, LINE, LIGHT, INK };
  window.Subjects = {};
})();
