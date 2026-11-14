// The power signs over machines and before power numbers, as picked (the ink-signs ADR): glyphs a screen lights in hard steps,
// drawn straight on the canvas at 4 times their logical pixels, no finish (as edgekeys.js). The bolt short of power, none, unplugged, broken;
// the tips' oxygen made and air leaked (tip-signs), sky, or red where their line is short.
(function () {
'use strict';
const { canvas } = Anime;

/** The logical sizes the game lays them out by: the pixel signs' own. */
const SIZES = { sign_bolt: [7, 9], sign_boltNone: [7, 9], sign_unplugged: [7, 7], sign_broken: [13, 13], sign_oxygen: [11, 11], sign_oxygenShort: [11, 11], sign_leak: [9, 9], sign_leakShort: [9, 9],
  sign_mouseLeft: [13, 10], sign_mouseRight: [13, 10], sign_mouseMiddle: [13, 10], sign_mouseMiddleKey: [13, 10], sign_drag: [8, 7], sign_holdTrack: [9, 9], sign_cap: [16, 16], sign_off: [9, 9], sign_cold: [11, 11] };
for (let step = 1; step <= 12; step++) SIZES[`sign_hold_${step}`] = [9, 9];
/** The bolt in the 7 by 9 picture: a heavy zigzag, top right to a point at the bottom left. */
const BOLT = [3.6, .5, 6.5, .5, 4.7, 3.5, 6.4, 3.5, 1.0, 8.6, 2.5, 5.3, .6, 5.3];
/** A shape moved by (dx, dy). */
const moved = (sd, dx, dy) => (x, y) => sd(x - dx, y - dy);
/** BOLT scaled by k round its middle, moved to (cx, cy). */
const fit = (pts, cx, cy, k) => pts.map((v, i) => (i % 2 ? (v - 4.55) * k + cy : (v - 3.5) * k + cx));

/** Signed distance to a polygon, negative inside (Inigo Quilez). */
function sdPoly(p) {
  const n = p.length / 2;
  return (x, y) => {
    let d = (x - p[0]) ** 2 + (y - p[1]) ** 2, s = 1;
    for (let i = 0, j = n - 1; i < n; j = i, i++) {
      const vx = p[2 * i], vy = p[2 * i + 1], ex = p[2 * j] - vx, ey = p[2 * j + 1] - vy, wx = x - vx, wy = y - vy;
      const t = Math.max(0, Math.min(1, (wx * ex + wy * ey) / (ex * ex + ey * ey))), bx = wx - ex * t, by = wy - ey * t;
      d = Math.min(d, bx * bx + by * by);
      const c1 = y >= vy, c2 = y < p[2 * j + 1], c3 = ex * wy > ey * wx;
      if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
    }
    return s * Math.sqrt(d);
  };
}
const sdCapsule = (ax, ay, bx, by, r) => (x, y) => {
  const px = x - ax, py = y - ay, ex = bx - ax, ey = by - ay, t = Math.max(0, Math.min(1, (px * ex + py * ey) / (ex * ex + ey * ey || 1)));
  return Math.hypot(px - ex * t, py - ey * t) - r;
};
const sdCircle = (cx, cy, r) => (x, y) => Math.hypot(x - cx, y - cy) - r;
/** A rounded box, centre (cx, cy), half sizes hx, hy, turned by ang. */
const sdBox = (cx, cy, hx, hy, r, ang = 0) => (x, y) => {
  const c = Math.cos(ang), s = Math.sin(ang), dx = x - cx, dy = y - cy, lx = Math.abs(dx * c + dy * s) - hx + r, ly = Math.abs(-dx * s + dy * c) - hy + r;
  return Math.hypot(Math.max(lx, 0), Math.max(ly, 0)) + Math.min(Math.max(lx, ly), 0) - r;
};
const union = (...f) => (x, y) => { let d = Infinity; for (const g of f) d = Math.min(d, g(x, y)); return d; };
const minus = (f, g) => (x, y) => Math.max(f(x, y), -g(x, y));
const ring = (f, t) => (x, y) => Math.abs(f(x, y)) - t;
const rgb = (h) => [parseInt(h.substr(1, 2), 16), parseInt(h.substr(3, 2), 16), parseInt(h.substr(5, 2), 16)];

/**
 * A glyph as a screen lights it: per layer a dim halo one step out, a dark rim, the lit body and a pale core, each a hard step; every
 * fourth bitmap row a scan line. Distances in the bitmap's pixels (4 a logical pixel); 4 by 4 samples a pixel for a clean edge only.
 */
function glyph(name, layers) {
  const [w, h] = SIZES[name], W = w * 4, H = h * 4, c = canvas(W, H), x = c.getContext('2d'), im = x.createImageData(W, H), D = im.data, SS = 4;
  const L = layers.map((l) => ({ ...l, body: rgb(l.body), core: rgb(l.core ?? l.body), rim: l.rim ? rgb(l.rim) : null, halo: l.halo ? rgb(l.halo) : null }));
  for (let py = 0; py < H; py++) for (let px = 0; px < W; px++) {
    let R = 0, G = 0, B = 0, A = 0;
    for (let sy = 0; sy < SS; sy++) for (let sx = 0; sx < SS; sx++) {
      const lx = (px + (sx + .5) / SS) / 4, ly = (py + (sy + .5) / SS) / 4;
      let r = 0, g = 0, b = 0, a = 0;
      for (const l of L) {
        const d = l.sd(lx, ly) * 4, rimW = l.rimW ?? 2, haloW = l.haloW ?? 2.5;
        let col = null, al = 1;
        if (d < -(l.coreAt ?? 2.2)) col = l.core;
        else if (d < 0) col = l.body;
        else if (l.rim && d < rimW) { col = l.rim; al = .94; }
        else if (l.halo && l.haloA && d < (l.rim ? rimW : 0) + haloW) { col = l.halo; al = l.haloA; }
        if (!col) continue;
        const lit = col === l.body || col === l.core, k = lit && py % 4 === 3 ? .8 : 1;
        r = r * (1 - al) + col[0] * k * al; g = g * (1 - al) + col[1] * k * al; b = b * (1 - al) + col[2] * k * al; a = a * (1 - al) + al;
      }
      R += r; G += g; B += b; A += a;
    }
    const q = (py * W + px) * 4;
    if (A > 0) { D[q] = R / A; D[q + 1] = G / A; D[q + 2] = B / A; D[q + 3] = 255 * A / (SS * SS); }
  }
  x.putImageData(im, 0, 0);
  return c;
}

const bolt = (name, body, core, rim, halo) => glyph(name, [{ sd: sdPoly(fit(BOLT, 3.5, 4.55, .72)), body, core, rim, halo, haloA: .42 }]);
const unplugged = () => glyph('sign_unplugged', [{
  sd: union(sdCapsule(2.75, 1.65, 2.75, 2.35, .34), sdCapsule(4.25, 1.65, 4.25, 2.35, .34), sdBox(3.5, 3.2, 1.45, .8, .35), sdCapsule(3.5, 4.45, 3.5, 4.7, .3), sdCapsule(3.5, 5.4, 3.5, 5.55, .3)),
  body: '#8ea6bc', core: '#dbe8f4', rim: '#0a1016', halo: '#3e5a74', haloA: .4, coreAt: 1.8,
}]);
const broken = () => {
  const dir = -Math.PI / 4, head = minus(sdCircle(8.2, 4.8, 1.65), sdBox(8.2 + Math.cos(dir) * 1.25, 4.8 + Math.sin(dir) * 1.25, 1.05, .52, .05, dir));
  const end = minus(sdCircle(4.0, 9.0, .95), sdCircle(4.0, 9.0, .38));
  return glyph('sign_broken', [
    { sd: ring(sdBox(6.5, 6.5, 4.4, 4.4, 1.1), .55), body: '#ff7c2e', core: '#ffd2ac', rim: '#160a04', halo: '#8a3a12', haloA: .42, coreAt: 1.4 },
    { sd: union(sdCapsule(4.4, 8.6, 7.3, 5.7, .62), head, end), body: '#e6edf3', core: '#ffffff', rim: '#0a0c10', halo: '#000000', haloA: 0, coreAt: 2 },
  ]);
};

/** The tips' lit tones (tip-signs): the air's sky, and red where the line is short. */
const SKY = { body: '#38bdf8', core: '#d4f3ff', rim: '#04121c', halo: '#165a7a' };
const RED = { body: '#ec3f36', core: '#ffd0c4', rim: '#1c0606', halo: '#7c1a18' };
/** O₂ in screen letters, the user's pick (o2.glyph): an O ring and a small 2 beside its foot; on 11 by 11, moved in, so its halo
 * clears every edge (9 by 9 cut it at the left and right). */
const oxygen = (name, tone) => glyph(name, [{
  sd: moved(union(ring(sdCircle(3.5, 4.1, 2.3), .55), sdCapsule(6.4, 5.7, 7.1, 5.3, .36), sdCapsule(7.1, 5.3, 7.7, 5.8, .36), sdCapsule(7.7, 5.8, 6.4, 7.9, .36), sdCapsule(6.4, 7.9, 8.0, 7.9, .36)), 1, .75),
  ...tone, haloA: .42, coreAt: 1.6,
}]);
/** A wall broken through, in the unplugged sign's grey, and an arrow out of the gap (leak.glyph). */
const leak = (name, tone) => glyph(name, [
  { sd: union(sdCapsule(1.6, .9, 1.6, 3.2, .5), sdCapsule(1.6, 5.8, 1.6, 8.1, .5)), body: '#8ea6bc', core: '#dbe8f4', rim: '#0a1016', halo: '#3e5a74', haloA: .4, coreAt: 1.8 },
  { sd: union(sdCapsule(2.4, 4.5, 6.4, 4.5, .45), sdPoly([5.6, 2.6, 8.4, 4.5, 5.6, 6.4])), ...tone, haloA: .42, coreAt: 1.6 },
]);

// The build hints' signs, as picked (build-hints: the wing's column, the buttons alone): the wing screen's slate, lit where pressed.
const WING = { lit: { body: '#9cb6c8', core: '#d2e4ef', rim: '#03080c', halo: '#2c4a5e' }, dim: { body: '#3d5366', core: '#486074' } };
/** The layer keys' brighter label tone, for the middle button's sign in their corner. */
const LABEL = { lit: { body: '#dfe7ef', core: '#ffffff', rim: '#05090e', halo: '#3e5a74' }, dim: { body: '#8a94a3', core: '#8a94a3' } };
const inter = (...f) => (x, y) => { let d = -Infinity; for (const g of f) d = Math.max(d, g(x, y)); return d; };
/** A ring's arc from angle a0 clockwise by span, thickness t. */
const sdArc = (cx, cy, r, t, a0, span) => {
  const e0x = cx + r * Math.cos(a0), e0y = cy + r * Math.sin(a0), e1x = cx + r * Math.cos(a0 + span), e1y = cy + r * Math.sin(a0 + span);
  return (x, y) => {
    let rel = Math.atan2(y - cy, x - cx) - a0;
    rel = ((rel % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
    if (rel <= span) return Math.abs(Math.hypot(x - cx, y - cy) - r) - t;
    return Math.min(Math.hypot(x - e0x, y - e0y), Math.hypot(x - e1x, y - e1y)) - t;
  };
};
/** A part of a sign, lit when pressed. */
const part = (sd, on, T, extra) => Object.assign({ sd, body: on ? T.lit.body : T.dim.body, core: on ? T.lit.core : T.dim.core, coreAt: on ? 1.6 : 99,
  rim: T.lit.rim, rimW: 1.2, halo: on ? T.lit.halo : null, haloA: on ? .4 : 0 }, extra || {});
/** A mouse's top alone, the two halves and the wheel, the pressed one lit: no body, so it sits in a line of text. */
const mouse = (name, lit, T) => {
  const top = inter(sdBox(6.5, 7.0, 5.0, 5.6, 3.8), sdBox(6.5, 4.6, 6, 3.7, 0));
  const left = inter(top, sdBox(3.1, 5, 2.55, 5, 0)), right = inter(top, sdBox(9.9, 5, 2.55, 5, 0));
  return glyph(name, [part(left, lit === 'L', T), part(right, lit === 'R', T), part(sdCapsule(6.5, 3.0, 6.5, 5.6, .62), lit === 'M', T, { rimW: 1.4 })]);
};
/** A drag's short arrow after its button; the game pulses it. */
const drag = () => glyph('sign_drag', [{ sd: union(sdCapsule(1.0, 3.5, 4.8, 3.5, .5), sdPoly([4.2, 1.1, 7.2, 3.5, 4.2, 5.9])), ...WING.lit, coreAt: 1.6, rimW: 1.2, haloA: .38 }]);
/** A hold's ring: the dim track, or the lit arc filled to step of HOLD_STEPS, which the game steps through. */
const HOLD_STEPS = 12;
const hold = (name, step) => glyph(name, step === 0
  ? [{ sd: ring(sdCircle(4.5, 4.5, 2.9), .5), body: WING.dim.body, coreAt: 99, rim: WING.lit.rim, rimW: 1.2 }]
  : [{ sd: sdArc(4.5, 4.5, 2.9, .62, -Math.PI / 2, 2 * Math.PI * step / HOLD_STEPS - .001), ...WING.lit, coreAt: 1.8, rimW: 1, haloA: .4 }]);
/** A key cap's lit face with no letter, the game writing the key on it dark; nine-sliced 5 pixels in, so wide keys stretch its middle. */
const cap = () => {
  const w = 16, h = 16, cx = w / 2, cy = h / 2, hx = w / 2 - 1.5, hy = h / 2 - 1.4;
  return glyph('sign_cap', [
    { sd: sdBox(cx, cy, hx, hy, 2), ...WING.lit, coreAt: 3.2, rimW: 1.5, haloA: .32, haloW: 3 },
    { sd: sdCapsule(cx - hx + 1.8, cy + hy - 1.5, cx + hx - 1.8, cy + hy - 1.5, .3), body: WING.dim.body, coreAt: 99 },
  ]);
};

/** The power symbol over a machine a player switched off (industrial-oxygen, the user's pick A): a ring open at the top, a bar into
 * it, periwinkle, so it reads apart from the bolt's amber and red and the wrench's orange; steady, never blinking. */
const off = () => glyph('sign_off', [{
  sd: union(sdArc(4.5, 4.9, 2.5, .5, -Math.PI / 2 + .8, 2 * Math.PI - 1.6), sdCapsule(4.5, 1.6, 4.5, 4.4, .5)),
  body: '#8c9cf0', core: '#e2e6ff', rim: '#07081a', halo: '#3a3f86', haloA: .42, coreAt: 1.6,
}]);

/** The thermometer's sign (the-thermometer, the user's pick B): a six-armed frost star, a small fork on each arm, in pale ice. */
const ICE = { body: '#bfe3f5', core: '#f0fbff', rim: '#04121c', halo: '#3a6a86' };
const cold = () => {
  const arms = [];
  for (let k = 0; k < 6; k++) {
    const a = k * Math.PI / 3 - Math.PI / 2, c = Math.cos(a), s = Math.sin(a), mx = 5.5 + c * 2.6, my = 5.5 + s * 2.6;
    arms.push(sdCapsule(5.5, 5.5, 5.5 + c * 4.1, 5.5 + s * 4.1, .42));
    for (const d of [-.9, .9]) arms.push(sdCapsule(mx, my, mx + Math.cos(a + d) * 1.3, my + Math.sin(a + d) * 1.3, .34));
  }
  return glyph('sign_cold', [{ sd: union(...arms), ...ICE, haloA: .42, coreAt: 1.4 }]);
};

Subjects.signs = function () {
  out('sign_bolt', bolt('sign_bolt', '#f5a623', '#fff0c0', '#1c1004', '#8a5410'));
  out('sign_boltNone', bolt('sign_boltNone', '#ec3f36', '#ffd0c4', '#1c0606', '#7c1a18'));
  out('sign_unplugged', unplugged());
  out('sign_broken', broken());
  out('sign_off', off());
  out('sign_oxygen', oxygen('sign_oxygen', SKY));
  out('sign_oxygenShort', oxygen('sign_oxygenShort', RED));
  out('sign_leak', leak('sign_leak', SKY));
  out('sign_leakShort', leak('sign_leakShort', RED));
  for (const [name, part] of [['Left', 'L'], ['Right', 'R'], ['Middle', 'M']]) out(`sign_mouse${name}`, mouse(`sign_mouse${name}`, part, WING));
  out('sign_mouseMiddleKey', mouse('sign_mouseMiddleKey', 'M', LABEL));
  out('sign_drag', drag());
  out('sign_holdTrack', hold('sign_holdTrack', 0));
  for (let step = 1; step <= HOLD_STEPS; step++) out(`sign_hold_${step}`, hold(`sign_hold_${step}`, step));
  out('sign_cap', cap());
  out('sign_cold', cold());
};
})();
