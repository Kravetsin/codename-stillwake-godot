'use strict';
// The live demo: a hero run, jumped and floated by a script or the keys, its hair and cape on chains (verlet) that
// trail its motion, rasterised into pixels each frame. Uses page.js's C, A, HD, get, el, canvasOf, devCanvas, deck.

const TAU = Math.PI * 2;
// Pixels of the world a second, and a second squared (Hero's numbers at 8 px a cell, 0104).
const MOVE = { walk: 115, run: 211, jump: 403, g: 1210, swim: 260 };
const STAGE = { w: 260, h: 84 };
const Bd = (u, l) => [u, l], pos0 = n => Math.max(0, n);

// ---- poses (Pose.cs, the movement page's port)
function runLeg(cycle) {
  const p = cycle - Math.floor(cycle), st = 0.42;
  if (p < st) return Bd(0.4 - (0.95 * p) / st, -0.12);
  const u = (p - st) / (1 - st);
  const thigh = u < 0.7 ? -0.55 + 1.65 * Math.sin((u / 0.7) * (Math.PI / 2)) : 1.1 - (0.7 * (u - 0.7)) / 0.3;
  return Bd(thigh, -(0.12 + 1.9 * Math.sin(u * Math.PI)));
}
function poseOf(h, t) {
  const wave = ms => Math.sin(((t * 1000) / ms) * TAU);
  if (h.float) {
    const d = wave(3400), s = wave(5200);
    return { fa: Bd(0.15 + 0.06 * d, 0.35), ba: Bd(0.4 - 0.08 * d, 0.55), fl: Bd(0.25 + 0.1 * s, -(0.5 + 0.1 * d)), bl: Bd(-0.08 * s, -(0.35 - 0.1 * d)), tilt: 0.03 * s + (Math.abs(h.vx) / MOVE.swim) * 0.25, lift: 0 };
  }
  if (!h.ground)
    return h.vy < 0
      ? { fa: Bd(2.3, 0.35), ba: Bd(1.9, 0.5), fl: Bd(1, -1.3), bl: Bd(0.25, -0.8), tilt: 0, lift: 0 }
      : { fa: Bd(1.4, 0.4), ba: Bd(1.1, 0.5), fl: Bd(0.35, -0.45), bl: Bd(-0.05, -0.3), tilt: 0, lift: 0 };
  const v = Math.abs(h.vx);
  if (v < 10) { const br = wave(2600); return { fa: Bd(0.05 * br, 0.18), ba: Bd(-0.05 * br, 0.22), fl: Bd(0.02, -0.04), bl: Bd(-0.02, -0.04), tilt: 0, lift: 0 }; }
  if (v < 160) {
    const p = (h.trav / 45) * TAU, s = Math.sin(p), c = Math.cos(p);
    return { fa: Bd(-0.3 * s, 0.25 + 0.25 * pos0(-c)), ba: Bd(0.3 * s, 0.25 + 0.25 * pos0(c)), fl: Bd(0.5 * s, -(0.08 + 0.8 * pos0(c))), bl: Bd(-0.5 * s, -(0.08 + 0.8 * pos0(-c))), tilt: 0.03, lift: 0 };
  }
  const p = (h.trav / 74) * TAU, s = Math.sin(p);
  return { fa: Bd(-0.8 * s, 1.4), ba: Bd(0.8 * s, 1.4), fl: runLeg(p / TAU), bl: runLeg(p / TAU + 0.5), tilt: 0.15, lift: 0 };
}

// ---- a chain of points (verlet): its first pinned to an anchor, the rest trailing under gravity, kept a segment apart
class Chain {
  constructor(n, seg, { damp = 0.9, gravity = 900, back = 30, stiff = 0 } = {}) {
    Object.assign(this, { n, seg, damp, gravity, back, stiff, p: null, q: null });
  }
  step(anchor, dt, { weightless, facing, solid }) {
    if (!this.p) {
      this.p = Array.from({ length: this.n }, (_, i) => [anchor[0], anchor[1] + i * this.seg]);
      this.q = this.p.map(v => [...v]);
    }
    const g = weightless ? 0 : this.gravity, back = -facing * this.back * (weightless ? 0.3 : 1);
    this.p[0] = [...anchor]; this.q[0] = [...anchor];
    for (let i = 1; i < this.n; i++) {
      const p = this.p[i], q = this.q[i];
      const vx = (p[0] - q[0]) * this.damp, vy = (p[1] - q[1]) * this.damp;
      this.q[i] = [...p];
      p[0] += vx + back * dt * dt; p[1] += vy + g * dt * dt;
    }
    for (let k = 0; k < 4; k++)
      for (let i = 1; i < this.n; i++) {
        const a = this.p[i - 1], b = this.p[i];
        // Stiffness: a link leans toward its parent's direction, so a ponytail keeps a curve rather than a rope's.
        if (this.stiff && i > 1) {
          const z = this.p[i - 2], want = [a[0] + (a[0] - z[0]), a[1] + (a[1] - z[1])];
          b[0] += (want[0] - b[0]) * this.stiff; b[1] += (want[1] - b[1]) * this.stiff;
        }
        const dx = b[0] - a[0], dy = b[1] - a[1], d = Math.hypot(dx, dy) || 1;
        b[0] = a[0] + (dx / d) * this.seg; b[1] = a[1] + (dy / d) * this.seg;
        if (solid) solid(b, i);
      }
  }
}

/** The chain's points as shaded volumes (hd.js), on a grid density pixels a world pixel, in the ramp: an op for C.draw. */
function rasterChain(points, radii, density, ramp, feet, capsules) {
  let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
  points.forEach(([x, y], i) => { x0 = Math.min(x0, x - radii[i]); y0 = Math.min(y0, y - radii[i]); x1 = Math.max(x1, x + radii[i]); y1 = Math.max(y1, y + radii[i]); });
  x0 = Math.floor(x0) - 1; y0 = Math.floor(y0) - 1;
  const w = Math.ceil((x1 - x0 + 1) * density), h = Math.ceil((y1 - y0 + 1) * density);
  const g = ([x, y]) => [(x - x0) * density, (y - y0) * density];
  const parts = [];
  // The root last, so it lies over what hangs from it.
  for (let i = points.length - 1; i >= 0; i--) {
    const [x, y] = g(points[i]), r = radii[i] * density;
    if (capsules && i > 0) { const [px, py] = g(points[i - 1]); parts.push(HD.c(px, py, x, y, radii[i - 1] * density, r)); }
    else parts.push(HD.e(x, y, r, r * 0.92));
  }
  const rows = HD.shade({ parts, w, h, at: null });
  const img = C.blank(w, h);
  rows.forEach((row, y) => [...row].forEach((k, x) => {
    if (k === '.') return;
    const n = parseInt(ramp['1234'.indexOf(k)].slice(1), 16);
    img.data.set([n >> 16, (n >> 8) & 255, n & 255, 255], (y * w + x) * 4);
  }));
  return { img, at: [x0 - feet[0], y0 - feet[1]], turn: 0, joint: [0, 0], density };
}

/**
 * The cape seen from the side: the area between the body's back (back, top to bottom) and the cloth's chain (edge, from
 * the neck), with a band of cloth round the chain itself; lit along the edge, folds from the chain's points to the hip,
 * an outline outside and a softer line where it lies on the back. On the world's own grid: an op for C.draw.
 */
function rasterCape(edge, back, ramp, feet) {
  const poly = [...edge, ...back.slice().reverse()];
  let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
  for (const [x, y] of poly) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
  x0 = Math.floor(x0) - 3; y0 = Math.floor(y0) - 3;
  const w = Math.ceil(x1 - x0) + 6, h = Math.ceil(y1 - y0) + 6;
  const near = (px, py, a, b, r) => {
    const vx = b[0] - a[0], vy = b[1] - a[1], t = Math.max(0, Math.min(1, ((px - a[0]) * vx + (py - a[1]) * vy) / (vx * vx + vy * vy || 1)));
    return Math.hypot(px - a[0] - vx * t, py - a[1] - vy * t) <= r;
  };
  const inPoly = (px, py) => {
    let inside = false;
    for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) {
      const [xi, yi] = poly[i], [xj, yj] = poly[j];
      if (yi > py !== yj > py && px < ((xj - xi) * (py - yi)) / (yj - yi) + xi) inside = !inside;
    }
    return inside;
  };
  const cloth = Array.from({ length: h }, (_, y) => Array.from({ length: w }, (_, x) => {
    const px = x0 + x + 0.5, py = y0 + y + 0.5;
    return inPoly(px, py) || edge.some((p, i) => i > 0 && near(px, py, edge[i - 1], p, 1.4));
  }));
  const hip = back[back.length - 1];
  const img = C.blank(w, h);
  const set = (x, y, k) => { const n = parseInt(ramp[k].slice(1), 16); img.data.set([n >> 16, (n >> 8) & 255, n & 255, 255], (y * w + x) * 4); };
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (!cloth[y][x]) continue;
    const px = x0 + x + 0.5, py = y0 + y + 0.5;
    const on = (dx, dy) => cloth[y + dy] && cloth[y + dy][x + dx];
    const outside = [[1, 0], [-1, 0], [0, 1], [0, -1]].filter(([dx, dy]) => !on(dx, dy));
    let k = 1;
    if (edge.some((p, i) => i > 0 && near(px, py, edge[i - 1], p, 1.6))) k = 0;
    if ([3, 5, 7].some(i => edge[i] && near(px, py, edge[i], hip, 0.5))) k = 2;
    if (outside.length) k = outside.every(([dx, dy]) => back.some((p, i) => i > 0 && near(px + dx, py + dy, back[i - 1], p, 1.2))) ? 2 : 3;
    set(x, y, k);
  }
  return { img, at: [x0 - feet[0], y0 - feet[1]], turn: 0, joint: [0, 0], density: 1 };
}

// ---- the hero and its hair and cape
const STYLES = {
  ponytail: { look: { suit: 2, skin: 0, hair: 3, hairColour: 3, face: 2, crown: 0 }, strands: [{ at: [8.5, 11.5], n: 8, seg: 1.9, r: [1.9, 0.9], stiff: 0.25 }] },
  long: {
    look: { suit: 0, skin: 3, hair: 1, hairColour: 2, face: 0, crown: 1 },
    smooth: true,
    strands: [{ at: [10, 16], n: 6, seg: 2.3, r: [3, 2.2] }, { at: [9.5, 21], n: 5, seg: 2.3, r: [2.8, 2] }, { at: [13, 24], n: 5, seg: 2.2, r: [2.6, 1.8] }],
  },
  still: { look: { suit: 1, skin: 5, hair: 2, hairColour: 0, face: 1 }, strands: [] },
};

/** The demo on the page; headless, its simulation and drawing alone, for check-live.js. */
function live(headless) {
  const opts = { style: 'ponytail', cape: true, zoom: 2.5, manual: false, weightless: false };
  const h = { x: 60, y: 0, vx: 0, vy: 0, ground: true, float: false, facing: 1, trav: 0, turn: 0 };
  let chains = [], cape = null;
  const reset = () => {
    chains = STYLES[opts.style].strands.map(s => new Chain(s.n, s.seg, { damp: 0.9, gravity: 700, back: 40, stiff: s.stiff || 0 }));
    cape = new Chain(11, 2.4, { damp: 0.93, gravity: 900, back: 25, stiff: 0.08 });
  };
  reset();

  // The autoplay: go to x (running or walking, jumping on the way), wait, or float weightless a while.
  const SCRIPT = [
    { wait: 0.6 }, { to: 220, run: true, jumpAt: 130 }, { wait: 0.7 }, { to: 70 }, { wait: 0.5 },
    { to: 200, run: true }, { wait: 0.3 }, { to: 40, run: true, jumpAt: 120 }, { wait: 0.8 }, { float: 5 }, { wait: 1 },
  ];
  let step = 0, stepT = 0, jumped = false;
  const keys = new Set();
  let lastKey = -1e9;

  function input(t, dt) {
    if (t - lastKey < 4) {
      const dir = (keys.has('ArrowRight') || keys.has('KeyD') ? 1 : 0) - (keys.has('ArrowLeft') || keys.has('KeyA') ? 1 : 0);
      const up = (keys.has('ArrowDown') || keys.has('KeyS') ? 1 : 0) - (keys.has('ArrowUp') || keys.has('KeyW') ? 1 : 0);
      return { dir, up, run: !keys.has('ShiftLeft') && !keys.has('ShiftRight'), jump: keys.has('Space') || keys.has('ArrowUp') || keys.has('KeyW'), float: opts.weightless };
    }
    const s = SCRIPT[step];
    stepT += dt;
    let done = false, out = { dir: 0, up: 0, run: false, jump: false, float: false };
    if (s.wait != null) done = stepT >= s.wait;
    else if (s.float != null) {
      out = { dir: Math.sin(stepT * 1.1) > 0 ? 1 : -1, up: Math.sin(stepT * 1.7) * 0.8, run: true, jump: false, float: true };
      done = stepT >= s.float;
    } else {
      const dir = Math.sign(s.to - h.x);
      out = { dir, up: 0, run: !!s.run, jump: false, float: false };
      if (s.jumpAt != null && !jumped && (h.x - s.jumpAt) * dir >= 0) { out.jump = true; jumped = true; }
      done = Math.abs(s.to - h.x) < 4;
    }
    if (done) { step = (step + 1) % SCRIPT.length; stepT = 0; jumped = false; }
    return out;
  }

  function move(inp, dt) {
    h.float = inp.float;
    if (h.float) {
      h.vx += inp.dir * MOVE.swim * 1.2 * dt; h.vy += inp.up * MOVE.swim * 1.2 * dt;
      h.vx *= 0.985; h.vy *= 0.985;
      h.ground = false;
    } else {
      const want = inp.dir * (inp.run ? MOVE.run : MOVE.walk);
      h.vx += (want - h.vx) * Math.min(1, dt * 14);
      if (inp.jump && h.ground) { h.vy = -MOVE.jump; h.ground = false; }
      h.vy += MOVE.g * dt;
    }
    h.x += h.vx * dt; h.y += h.vy * dt;
    if (h.x < 12) { h.x = 12; h.vx = Math.abs(h.vx) * (h.float ? 0.5 : 0); }
    if (h.x > STAGE.w - 12) { h.x = STAGE.w - 12; h.vx = -Math.abs(h.vx) * (h.float ? 0.5 : 0); }
    if (h.y > 0) { h.y = 0; h.vy = h.float ? -Math.abs(h.vy) * 0.5 : 0; h.ground = !h.float; }
    if (h.y < -(STAGE.h - 50)) { h.y = -(STAGE.h - 50); h.vy = Math.abs(h.vy) * 0.5; }
    if (Math.abs(h.vx) > 12) h.facing = Math.sign(h.vx);
    else if (inp.dir) h.facing = inp.dir;
    if (h.ground) h.trav += Math.abs(h.vx) * dt;
  }

  function physics(t, dt) {
    const style = STYLES[opts.style], f = h.facing;
    const look = { ...style.look, fine: true, pose: poseOf(h, t), turn: h.turn };
    const feet = [h.x, h.y];
    const world = ([x, y]) => [feet[0] + f * x, feet[1] + y];
    // Out of the body: hair and cape stay behind the back and outside the head.
    const head = world(C.onHead(look, [19, 19], [19, 30], 2));
    const body = (minBack, minBackLow) => (p) => {
      const lx = (p[0] - feet[0]) * f, ly = p[1] - feet[1];
      if (ly < -C.Hip + 2 && ly > -C.Neck - 1 && lx > -minBack) p[0] = feet[0] - f * minBack;
      else if (ly >= -C.Hip + 2 && lx > -minBackLow) p[0] = feet[0] - f * minBackLow;
      const dx = p[0] - head[0], dy = p[1] - head[1], d = Math.hypot(dx, dy);
      if (d < 5.5 && d > 0) { p[0] = head[0] + (dx / d) * 5.5; p[1] = head[1] + (dy / d) * 5.5; }
      if (p[1] > -0.5) p[1] = -0.5;
    };
    style.strands.forEach((s, i) => chains[i].step(world(C.onHead(look, s.at, [19, 30], 2)), dt, { weightless: h.float, facing: f, solid: body(4.5, 3) }));
    if (opts.cape) cape.step(world(C.bodied([-3.5, -C.Neck + 2], look.pose)), dt, { weightless: h.float, facing: f, solid: body(6.3, 5) });
    return { look, feet, f };
  }

  const bgCache = {};
  function background(z) {
    if (bgCache[z]) return bgCache[z];
    const floorY = STAGE.h - 5, world = deck(STAGE.w, STAGE.h, floorY);
    const w = Math.round(STAGE.w * z), hh = Math.round(STAGE.h * z), img = C.blank(w, hh);
    for (let y = 0; y < hh; y++) for (let x = 0; x < w; x++) {
      const s = (Math.floor(y / z) * world.w + Math.floor(x / z)) * 4;
      img.data.set(world.data.subarray(s, s + 4), (y * w + x) * 4);
    }
    return (bgCache[z] = img);
  }

  function render({ look, feet, f }) {
    const z = opts.zoom, bg = background(z);
    const img = { w: bg.w, h: bg.h, data: new Uint8ClampedArray(bg.data) };
    const floorY = STAGE.h - 5;
    const style = STYLES[opts.style];
    const hairRamp = A.HAIRS[look.hairColour], capeRamp = C.shadesOf(C.shade(A.SUITS[look.suit], 0.78)).map(n => '#' + n.toString(16).padStart(6, '0'));
    const strands = style.strands.map((s, i) => rasterChain(chains[i].p, chains[i].p.map((_, j) => s.r[0] + (s.r[1] - s.r[0]) * (j / (s.n - 1))), 2, hairRamp, feet, style.smooth));
    // The cape's inner side lies on the back, from the neck to the hip.
    const back = [[-3.5, -C.Neck + 2], [-4.8, -C.Neck + 5], [-4.8, -C.Hip + 1]].map(p => { const [x, y] = C.bodied(p, look.pose); return [feet[0] + f * x, feet[1] + y]; });
    const behind = opts.cape ? [rasterCape(cape.p, back, capeRamp, feet)] : [];
    const ops = C.figure({ ...look, flip: f < 0 }, get, { behind, beforeHead: strands });
    C.draw(img, ops, [feet[0] * z, (floorY + feet[1]) * z], null, { zoom: z });
    return img;
  }

  /** One step of a 60th of a second at time t: the hero moved, its chains trailing; what render draws. */
  function tick(t) {
    move(input(t, 1 / 60), 1 / 60);
    h.turn = h.float ? 0.25 * Math.sin(t * 0.9) : h.ground && Math.abs(h.vx) < 10 ? 0.2 * Math.sin(t * 0.7) : -0.05;
    return physics(t, 1 / 60);
  }
  if (headless) return { opts, h, tick, render, restyle: st => { opts.style = st; reset(); } };

  // ---- the page's controls
  const root = $('live'), view = $('live-view');
  const chip = (row, text, on, pick) => {
    const b = el('button', 'chip', text);
    b.type = 'button'; b.style.padding = '6px 10px';
    b.addEventListener('click', () => { pick(); sync(); });
    $(row).append(b);
    return [b, on];
  };
  const chips = [
    chip('live-style', 'хвост', () => opts.style === 'ponytail', () => { opts.style = 'ponytail'; reset(); }),
    chip('live-style', 'длинные', () => opts.style === 'long', () => { opts.style = 'long'; reset(); }),
    chip('live-style', 'кудри, без цепочек', () => opts.style === 'still', () => { opts.style = 'still'; reset(); }),
    chip('live-cape', 'с плащом', () => opts.cape, () => (opts.cape = true)),
    chip('live-cape', 'без плаща', () => !opts.cape, () => (opts.cape = false)),
    chip('live-zoom', '1080p · ближний', () => opts.zoom === 2.5, () => (opts.zoom = 2.5)),
    chip('live-zoom', '1080p · дальний', () => opts.zoom === 1.25, () => (opts.zoom = 1.25)),
    chip('live-zoom', '1440p · ближний', () => opts.zoom === 10 / 3, () => (opts.zoom = 10 / 3)),
  ];
  chips.forEach(([b], i) => (b.id = 'live-chip-' + i));
  function sync() { for (const [b, on] of chips) b.setAttribute('aria-pressed', String(on())); }
  sync();
  root.addEventListener('keydown', ev => {
    if (ev.code === 'KeyG') { opts.weightless = !opts.weightless; }
    keys.add(ev.code); lastKey = performance.now() / 1000;
    if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Space'].includes(ev.code)) ev.preventDefault();
  });
  root.addEventListener('keyup', ev => { keys.delete(ev.code); lastKey = performance.now() / 1000; });
  root.addEventListener('blur', () => keys.clear());

  const still = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  let last = null, acc = 0, drawn = -1;
  function frame(ms) {
    const t = ms / 1000;
    if (last == null) last = t;
    acc += Math.min(0.1, t - last); last = t;
    let state = null;
    while (acc >= 1 / 60) {
      state = tick(t);
      acc -= 1 / 60;
    }
    if (state && ms - drawn > 30) {
      drawn = ms;
      const img = render(state);
      // Beside the stage at 1:1, the hero up close: a window round it three times larger.
      const cw = Math.round(70 * opts.zoom), ch = Math.round(64 * opts.zoom);
      const cx = Math.max(0, Math.min(img.w - cw, Math.round(state.feet[0] * opts.zoom - cw / 2))), cy = Math.max(0, img.h - ch);
      const big = canvasOf(crop(img, cx, cy, cw, ch), 3 / (window.devicePixelRatio || 1));
      big.className = 'big';
      view.replaceChildren(devCanvas(img), el('div', 'one', 'вокруг героя, втрое крупнее'), big);
    }
    if (!still) requestAnimationFrame(frame);
  }
  if (still) { for (let i = 0; i < 90; i++) frame(i * (1000 / 60)); } else requestAnimationFrame(frame);
}
