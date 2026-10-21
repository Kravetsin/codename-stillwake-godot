'use strict';
// ===== the rig (Rig.cs), the poses (Pose.cs), the two-handed hold (TwoHand.cs) and the figure (Figure.cs) =====
const CELL = 16, TAU = Math.PI * 2;
const SPEC = { walk: 120, run: 220, climb: 180, jump: 420, g: 21 }; // thousandths of a cell a step, 60 steps a second
const HW = 0.95, HH = 2.625;
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const B = (u, l) => ({ u, l });
const L = (fa, ba, fl, bl, tilt = 0, lift = 0) => ({ fa, ba, fl, bl, tilt, lift });
const pos = n => Math.max(0, n);
const smooth = t => t * t * (3 - 2 * t);
const apart = (f, t) => ((t - f) % TAU + 3 * Math.PI) % TAU - Math.PI;
const mixA = (a, b, k) => a + apart(a, b) * k;
const mixB = (a, b, k) => ({ u: mixA(a.u, b.u, k), l: mixA(a.l, b.l, k) });
const mixL = (a, b, k) => ({ fa: mixB(a.fa, b.fa, k), ba: mixB(a.ba, b.ba, k), fl: mixB(a.fl, b.fl, k), bl: mixB(a.bl, b.bl, k), tilt: mixA(a.tilt, b.tilt, k), lift: a.lift + (b.lift - a.lift) * k });
const settle = (f, t, dt, w) => f + (t - f) * Math.min(1, dt / w);
const settleA = (f, t, dt, w) => f + apart(f, t) * Math.min(1, dt / w);
const settleB = (f, t, dt, w) => ({ u: settleA(f.u, t.u, dt, w), l: settleA(f.l, t.l, dt, w) });

function makeRig(p) {
  const W = HW * CELL, H = HH * CELL, Hip = H * p.hip;
  return { W, H, Hip, Shoulder: H * p.shoulder, Neck: H * p.neck, HipApart: W * p.hipApart, Arm: { u: H * p.arm, l: H * p.arm }, Leg: { u: Hip * 0.5, l: Hip * 0.5 }, Back: W * p.back };
}
function endOf(b, s) { const t = b.u + b.l; return { x: Math.sin(b.u) * s.u + Math.sin(t) * s.l, y: Math.cos(b.u) * s.u + Math.cos(t) * s.l }; }
function reach(tg, s, bends) {
  const a = s.u, b = s.l, far = Math.hypot(tg.x, tg.y);
  const d = Math.min(a + b - 1e-3, Math.max(Math.abs(a - b) + 1e-3, far));
  const toward = Math.atan2(tg.x, tg.y);
  const atJ = Math.acos(clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1));
  const atB = Math.acos(clamp((a * a + b * b - d * d) / (2 * a * b), -1, 1));
  return { u: toward - bends * atJ, l: bends * (Math.PI - atB) };
}

// ---------- Pose.cs ----------
function running(cycle) {
  const p = cycle - Math.floor(cycle), st = 0.42;
  if (p < st) return B(0.4 - 0.95 * p / st, -0.12);
  const u = (p - st) / (1 - st);
  const thigh = u < 0.7 ? -0.55 + 1.65 * Math.sin(u / 0.7 * Math.PI / 2) : 1.1 - 0.7 * (u - 0.7) / 0.3;
  return B(thigh, -(0.12 + 1.9 * Math.sin(u * Math.PI)));
}
function onRung(cycle, top, range, x, outward) {
  const p = cycle - Math.floor(cycle), grip = 0.6;
  if (p < grip) return { x, y: top + p / grip * range };
  const q = (p - grip) / (1 - grip);
  return { x: x - outward * Math.sin(q * Math.PI), y: top + (1 - q) * range };
}
function climbing(R, cycle) {
  const arm = R.Arm.u + R.Arm.l;
  const hand = at => onRung(at, -arm * 0.85, arm * 0.6, R.W * 0.6, R.W * 0.2);
  const foot = at => onRung(at, R.Hip - arm * 0.75, arm * 0.7, R.W * 0.25, R.W * 0.25);
  return L(reach(hand(cycle), R.Arm, 1), reach(hand(cycle + 0.5), R.Arm, 1), reach(foot(cycle + 0.5), R.Leg, -1), reach(foot(cycle), R.Leg, -1), 0, 0.8 * Math.abs(Math.sin(cycle * TAU)));
}
function paddling(along, sinking, stroke, lean, rch, drift) {
  const p = stroke * TAU;
  const paddle = lag => B(0.75 + rch * Math.sin(p + lag), 0.75 + 0.35 * Math.cos(p + lag));
  const leg = lag => { const k = 0.1 * Math.sin(p + lag); if (sinking > 0) return B(0.85 + k, -1.35); return along !== 0 ? B(-0.2 + k, -(0.55 + 0.15 * Math.cos(p + lag))) : B(0.05 + k, -0.2); };
  return L(paddle(Math.PI), paddle(0), leg(Math.PI), leg(0), along * lean + (sinking > 0 ? 0.06 : 0), 0.5 * drift);
}
function floating(m) {
  const drift = Math.sin(m.time / 3400 * TAU), sway = Math.sin(m.time / 5200 * TAU);
  const still = L(B(0.15 + 0.06 * drift, 0.35), B(0.4 - 0.08 * drift, 0.55), B(0.25 + 0.1 * sway, -(0.5 + 0.1 * drift)), B(-0.08 * sway, -(0.35 - 0.1 * drift)), 0.03 * sway, 2 * drift);
  if (m.swim <= 0) return still;
  let sw = paddling(m.along, 0, m.time / 1100, 0.22, 0.4, drift);
  if (m.fast > 0) sw = mixL(sw, paddling(m.along, 0, m.time / 700, 0.38, 0.6, drift), m.fast);
  return m.swim >= 1 ? sw : mixL(still, sw, m.swim);
}
const RISE = L(B(2.3, 0.35), B(1.9, 0.5), B(1, -1.3), B(0.25, -0.8));
const FALL = L(B(1.4, 0.4), B(1.1, 0.5), B(0.35, -0.45), B(-0.05, -0.3));
function bodyOf(R, pose, m) {
  const phase = c => m.trav / c * TAU, wave = ms => Math.sin(m.time / ms * TAU);
  switch (pose) {
    case 'Stand': { const br = wave(2600); return L(B(0.05 * br, 0.18), B(-0.05 * br, 0.22), B(0.02, -0.04), B(-0.02, -0.04)); }
    case 'Walk': { const p = phase(2.8), s = Math.sin(p), c = Math.cos(p);
      return L(B(-0.3 * s, 0.25 + 0.25 * pos(-c)), B(0.3 * s, 0.25 + 0.25 * pos(c)), B(0.5 * s, -(0.08 + 0.8 * pos(c))), B(-0.5 * s, -(0.08 + 0.8 * pos(-c))), 0.03); }
    case 'Run': { const p = phase(4.6), s = Math.sin(p), q = p / TAU; return L(B(-0.8 * s, 1.4), B(0.8 * s, 1.4), running(q), running(q + 0.5), 0.15); }
    case 'Air': return m.vy < 0 ? { ...RISE } : { ...FALL };
    case 'Climb': return climbing(R, m.trav / 3.6);
    case 'Float': return floating(m);
  }
}
function grounded(R, l) { const lowest = Math.max(endOf(l.fl, R.Leg).y, endOf(l.bl, R.Leg).y); return { ...l, lift: lowest - R.Hip }; }
function landingOf(R, l, landing) {
  if (!landing) return l;
  const t = 1 - landing, sink = R.Hip * 0.22 * (t < 1 / 3 ? t * 3 : (1 - t) * 1.5);
  const foot = x => reach({ x, y: R.Hip - sink }, R.Leg, -1);
  return { ...l, fl: foot(R.W * 0.08), bl: foot(-R.W * 0.06), fa: B(0.55, 0.6), ba: B(0.35, 0.7) };
}

// ---------- TwoHand.cs, the drill (HeldLook.Drill) ----------
const DEG = Math.PI / 180;
const KEYS = [[-55 * DEG, { x: 5, y: 5.5 }], [0, { x: 2.5, y: 8 }], [35 * DEG, { x: 3, y: 6 }], [85 * DEG, { x: 5.5, y: 3.5 }]];
const DRILL = { grip: { x: 3, y: 10.5 }, fore: { x: 12, y: 10.5 }, mount: { x: 7.5, y: 10.5 }, edgeTo: { x: 32, y: 5.5 }, lamp: { x: 1.5, y: 4.5 } };
function pivotAt(a) {
  if (a <= KEYS[0][0]) return KEYS[0][1];
  for (let i = 1; i < KEYS.length; i++) if (a <= KEYS[i][0]) { const [ta, pa] = KEYS[i - 1], [tb, pb] = KEYS[i], t = (a - ta) / (tb - ta); return { x: pa.x + (pb.x - pa.x) * t, y: pa.y + (pb.y - pa.y) * t }; }
  return KEYS[KEYS.length - 1][1];
}
function heldAt(a, px) { const p = pivotAt(a), x = px.x - DRILL.mount.x, y = px.y - DRILL.mount.y, c = Math.cos(a), s = Math.sin(a); return { x: p.x + x * c - y * s, y: p.y + x * s + y * c }; }
function twoArms(R, a) { const toFist = { u: R.Arm.u, l: R.Arm.l - 1 }; return { near: reach(heldAt(a, DRILL.grip), toFist, 1), far: reach(heldAt(a, DRILL.fore), toFist, 1) }; }

// ---------- pictures ----------
function hexRgb(h) { const n = parseInt(h.slice(1), 16); return [n >> 16 & 255, n >> 8 & 255, n & 255]; }
function shadeInt(c, by) { const ch = s => { const v = (c >> s) & 255; return Math.round(by >= 0 ? v * by : v + (255 - v) * -by); }; return (ch(16) << 16) | (ch(8) << 8) | ch(0); }
const shadesOf = c => [shadeInt(c, -0.35), c, shadeInt(c, 0.72), shadeInt(c, 0.42)];
const intRgb = c => [c >> 16 & 255, c >> 8 & 255, c & 255];
const rgbCss = a => `rgb(${a[0] | 0},${a[1] | 0},${a[2] | 0})`;
function mixRgb(a, b, k) { return [a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k, a[2] + (b[2] - a[2]) * k]; }
function canvasOf(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }

// a part from pixel rows: its picture and its glow mask (black where it covers, its colour where it glows)
function partFromRows(rows, suit, dir) {
  const h = rows.length, w = rows[0].length, alb = canvasOf(w, h), em = canvasOf(w, h);
  const a = alb.getContext('2d'), e = em.getContext('2d'), sh = shadesOf(suit).map(intRgb);
  const vg = dir.visorGlow || {};
  const suitLight = sh[0], suitCore = mixRgb(sh[0], [255, 255, 255], 0.55);
  let glows = false;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const k = rows[y][x]; if (k === '.') continue;
    let col, glow = null;
    if (k === 'l') col = sh[0]; else if (k === 's') col = sh[1]; else if (k === 'd') col = sh[2]; else if (k === 'o') col = sh[3];
    else if (k === 'e') glow = hexRgb(vg.e || '#9ff0e0');
    else if (k === 'E') glow = hexRgb(vg.E || '#ffffff');
    else if (k === 'c') glow = suitLight;
    else if (k === 'C') glow = suitCore;
    else col = hexRgb(PAL[k]);
    a.fillStyle = glow ? '#000' : rgbCss(col); a.fillRect(x, y, 1, 1);
    e.fillStyle = glow ? rgbCss(glow) : '#000'; e.fillRect(x, y, 1, 1);
    if (glow) glows = true;
  }
  return { img: alb, em, glows, w, h };
}
// a real PNG recoloured as SuitArt does
function partFromImage(img, suit) {
  const w = img.width, h = img.height, alb = canvasOf(w, h), em = canvasOf(w, h);
  const g = alb.getContext('2d'); g.drawImage(img, 0, 0);
  const d = g.getImageData(0, 0, w, h), from = shadesOf(0xe0a030), to = shadesOf(suit);
  for (let i = 0; i < d.data.length; i += 4) {
    if (!d.data[i + 3]) continue;
    const at = from.indexOf((d.data[i] << 16) | (d.data[i + 1] << 8) | d.data[i + 2]);
    if (at >= 0) { d.data[i] = to[at] >> 16 & 255; d.data[i + 1] = to[at] >> 8 & 255; d.data[i + 2] = to[at] & 255; }
  }
  g.putImageData(d, 0, 0);
  const e = em.getContext('2d'); e.drawImage(img, 0, 0); e.globalCompositeOperation = 'source-in'; e.fillStyle = '#000'; e.fillRect(0, 0, w, h);
  return { img: alb, em, glows: false, w, h };
}
function plainPart(img) { const em = canvasOf(img.width, img.height), e = em.getContext('2d'); e.drawImage(img, 0, 0); e.globalCompositeOperation = 'source-in'; e.fillStyle = '#000'; e.fillRect(0, 0, img.width, img.height); return { img, em, glows: false, w: img.width, h: img.height }; }

const PART_NAMES = ['helmet', 'torso', 'pack', 'upperArm', 'forearm', 'thigh', 'shin', 'jetpack'];
const LOOKS = {};
// a direction's look in one suit colour: near and far parts, rig, joints, extras
function lookOf(dir, suit, dark) {
  const key = dir.id + ':' + suit + (dark ? ':dark' : '');
  if (LOOKS[key]) return LOOKS[key];
  const R = makeRig(dir.rig), far = shadeInt(suit, 0.7), near = {}, farP = {};
  for (const n of PART_NAMES) {
    if (dir.id === 'now') {
      const img = IMG['hero_' + n];
      near[n] = n === 'jetpack' ? plainPart(img) : partFromImage(img, suit);
      farP[n] = n === 'jetpack' ? near[n] : partFromImage(img, far);
    } else { const rows = dark ? dir.parts[n].map(r => r.replace(/[eEcC]/g, ch => ({ e: 'v', E: 'R', c: 'V', C: 'R' })[ch])) : dir.parts[n]; near[n] = partFromRows(rows, suit, dir); farP[n] = partFromRows(rows, far, dir); }
  }
  const joints = { ...dir.joints };
  // the boot's sole on the floor: the shin's joint sits a leg's lower length above its last row
  joints.shin = [joints.shin[0], near.shin.h - R.Leg.l];
  const vg = dark ? null : dir.visorGlow ? (dir.visorGlow.suit ? { light: intRgb(shadesOf(suit)[0]).map(v => v / 255), reach: dir.visorGlow.reach } : dir.visorGlow) : null;
  return LOOKS[key] = { dir, R, near, far: farP, joints, vg, suit };
}

// ---------- the figure ----------
class Figure {
  constructor(look, opts = {}) {
    this.look = look; this.o = opts;
    Object.assign(this, { shown: null, time: 0, head: 0, pack: { x: 0, y: 0, vx: 0, vy: 0 }, helm: { x: 0, y: 0, vx: 0, vy: 0 }, ant: null, grasp: 0, hold: 0.3, spin: 0, spinFrame: 0, idle: 0 });
  }
  // s: { pose, trav, vy, landing, along, swim, fast, x, y (feet px), facing, look, drill: {angle, spinning}, ax, ay (px/s² body accel) }
  update(s, dt, live) {
    const R = this.look.R; this.time += dt; this.s = s;
    const m = { trav: s.trav || 0, time: this.time, vy: s.vy || 0, swim: s.swim || 0, fast: s.fast || 0, along: s.along || 0 };
    let to = bodyOf(R, s.pose, m);
    const ground = s.pose === 'Stand' || s.pose === 'Walk' || s.pose === 'Run';
    if (s.pose === 'Air' && live.airBlend) {
      const k = smooth(clamp((s.vy * 1000 / 60 + 150) / 450, 0, 1));
      to = mixL(RISE, FALL, k);
    }
    // idle life (movement item 7): a breath, a weight shift, a glance
    let look = s.look || 0;
    if (s.pose === 'Stand' && !s.drill) this.idle += dt; else this.idle = 0;
    if (s.pose === 'Stand' && !s.drill && this.idle > 2500) {
      const t = this.idle - 2500, shift = smooth(clamp(Math.sin(t / 7000 * TAU) * 1.6, 0, 1));
      if (live.shift) to = mixL(to, { ...to, fl: B(0.12, -0.06), bl: B(-0.12, -0.28), tilt: to.tilt + 0.025 }, shift);
      const g = (t % 5200) / 5200; if (live.glance) look = g < 0.25 ? -0.4 : g > 0.45 && g < 0.62 ? 0.32 : 0;
    }
    if (ground) to = grounded(R, landingOf(R, to, s.landing));
    if (live.breath && s.pose === 'Stand' && !s.drill) to.lift += 0.45 * (0.5 + 0.5 * Math.sin(this.time / 2600 * TAU));
    const from = this.shown || to, within = s.pose === 'Float' ? 110 : 30;
    this.shown = { fa: settleB(from.fa, to.fa, dt, within), ba: settleB(from.ba, to.ba, dt, within), fl: settleB(from.fl, to.fl, dt, within), bl: settleB(from.bl, to.bl, dt, within),
      tilt: settleA(from.tilt, to.tilt, dt, s.pose === 'Float' ? 450 : 30), lift: settle(from.lift, to.lift, dt, 30) };
    this.head = settle(this.head, look, dt, s.pose === 'Stand' && live.glance ? 260 : 90);
    // the drill in both hands
    if (s.drill) {
      this.hold = this.grasp === 0 ? s.drill.angle : settle(this.hold, s.drill.angle, dt, 30);
      this.grasp = Math.min(1, this.grasp + dt / 120);
      this.spin = s.drill.spinning ? Math.min(1, this.spin + dt / 120) : Math.max(0, this.spin - dt / 700);
      this.spinFrame = this.spin > 0 ? (this.spinFrame + this.spin * 18 * dt / 1000) % 4 : 0;
    } else this.grasp = 0;
    // springs: the pack and the helmet lag the body's change of speed (movement item 8)
    const st = Math.min(dt, 34) / 1000, k = 600, c = 16;
    for (const [sp, gain] of [[this.pack, 1], [this.helm, 0.4]]) {
      const on = live.lag ? 1 : 0;
      const fx = -(s.ax || 0) * 0.0035 * gain * on, fy = -(s.ay || 0) * 0.0012 * gain * on;
      sp.vx += (-k * sp.x - c * sp.vx + fx * k) * st; sp.vy += (-k * sp.y - c * sp.vy + fy * k) * st;
      sp.x = clamp(sp.x + sp.vx * st, -2, 2); sp.y = clamp(sp.y + sp.vy * st, -2, 2);
    }
  }
  // the antenna: three points chasing their rest along the helmet, a segment apart
  antStep(anchor, dir) {
    const seg = 2.4, far = this.ant && Math.hypot(this.ant[0].x - anchor.x, this.ant[0].y - anchor.y) > 8;
    if (!this.ant || far) { this.ant = [1, 2, 3].map(i => ({ x: anchor.x + dir.x * seg * i, y: anchor.y + dir.y * seg * i, px: anchor.x + dir.x * seg * i, py: anchor.y + dir.y * seg * i })); this.antT = this.time; return; }
    let n = Math.min(6, Math.round((this.time - this.antT) / 16.67)); this.antT += n * 16.67;
    while (n-- > 0) {
      let prev = anchor;
      this.ant.forEach((q, i) => {
        const vx = (q.x - q.px) * 0.9, vy = (q.y - q.py) * 0.9 + 0.02; q.px = q.x; q.py = q.y; q.x += vx; q.y += vy;
        const tx = anchor.x + dir.x * seg * (i + 1), ty = anchor.y + dir.y * seg * (i + 1), stf = [0.3, 0.13, 0.08][i];
        q.x += (tx - q.x) * stf; q.y += (ty - q.y) * stf;
        const dx = q.x - prev.x, dy = q.y - prev.y, l = Math.hypot(dx, dy) || 1; q.x = prev.x + dx / l * seg; q.y = prev.y + dy / l * seg; prev = q;
      });
    }
  }
  // draws into the picture (a) and the glow mask (e); returns the lights it gives
  draw(a, e, cam, live) {
    const sh = this.shown; if (!sh) return [];
    const LK = this.look, R = LK.R, J = LK.joints, dir = LK.dir, s = this.s, f = s.facing || 1;
    const lights = [];
    const M = new DOMMatrix().translate(s.x, s.y).scale(f, 1).translate(0, -R.H / 2 - sh.lift).rotate(sh.tilt * 180 / Math.PI).translate(0, R.H / 2);
    const deg = r => r * 180 / Math.PI;
    const put = (m, p, j) => { const t = cam.multiply(m); a.setTransform(t); a.drawImage(p.img, -j[0], -j[1]); e.setTransform(t); e.drawImage(p.em, -j[0], -j[1]); };
    const limb = (m, b, up, lo, far, arm) => {
      const mu = m.rotate(-deg(b.u)), seg = arm ? R.Arm : R.Leg, ml = mu.translate(0, seg.u).rotate(-deg(b.l));
      const set = far ? LK.far : LK.near;
      put(ml, set[lo], J[lo]); put(mu, set[up], J[up]);
    };
    let fa = sh.fa, ba = sh.ba;
    if (s.drill) { const t = twoArms(R, this.hold), g = smooth(this.grasp); fa = mixB(fa, t.near, g); ba = mixB(ba, t.far, g); }
    limb(M.translate(0, -R.Shoulder), ba, 'upperArm', 'forearm', true, true);
    limb(M.translate(-R.HipApart, -R.Hip), sh.bl, 'thigh', 'shin', true, false);
    limb(M.translate(R.HipApart, -R.Hip), sh.fl, 'thigh', 'shin', false, false);
    const po = live.lag ? this.pack : { x: 0, y: 0 }, ho = live.lag ? this.helm : { x: 0, y: 0 };
    const packM = M.translate(-R.Back + po.x, -R.Shoulder - R.H * 0.02 + po.y);
    put(packM, LK.near.pack, J.pack);
    this.nozzle = null;
    if (s.jet) {
      const jm = packM.translate(dir.jetAt[0], dir.jetAt[1]);
      put(jm, LK.near.jetpack, [0, 0]);
      this.nozzle = jm.transformPoint({ x: dir.nozzle[0], y: dir.nozzle[1] });
    }
    put(M.translate(0, -R.Neck), LK.near.torso, J.torso);
    const hm = M.translate(ho.x, -R.Neck + ho.y).rotate(deg(this.head));
    // the antenna behind the helmet
    if (live.antenna === 'all' || live.antenna !== 'none' && dir.antenna) {
      const an = hm.transformPoint({ x: dir.ant[0], y: dir.ant[1] }), tip = hm.transformPoint({ x: dir.ant[0] - 0.45, y: dir.ant[1] - 1 });
      const l = Math.hypot(tip.x - an.x, tip.y - an.y) || 1;
      this.antStep(an, { x: (tip.x - an.x) / l, y: (tip.y - an.y) / l });
      for (const c of [a, e]) {
        c.setTransform(cam); c.strokeStyle = c === a ? '#5d636e' : '#000'; c.lineWidth = 0.8; c.beginPath(); c.moveTo(an.x, an.y);
        for (const q of this.ant) c.lineTo(q.x, q.y); c.stroke();
      }
      const t = this.ant[2]; a.setTransform(cam); a.fillStyle = '#000'; a.fillRect(t.x - 0.5, t.y - 0.5, 1, 1);
      e.setTransform(cam); e.fillStyle = '#ffcf6a'; e.fillRect(t.x - 0.5, t.y - 0.5, 1, 1);
      lights.push({ x: t.x, y: t.y, r: 10, c: [1, 0.8, 0.4], k: 0.25 });
    } else this.ant = null;
    put(hm, LK.near.helmet, J.helmet);
    // the visor's own light, when it glows
    const vis = hm.transformPoint({ x: dir.visor[0], y: dir.visor[1] });
    this.visorAt = vis;
    if (LK.vg) lights.push({ x: vis.x, y: vis.y, r: LK.vg.reach, c: LK.vg.light, k: 0.55 });
    // the helmet lamp: its bulb, its beam the way the head looks
    if (s.lamp) {
      const bm = cam.multiply(hm.translate(dir.lamp[0], dir.lamp[1]));
      for (const c of [a, e]) { c.setTransform(bm); c.fillStyle = c === a ? '#000' : '#ffe59e'; c.fillRect(-1.5, -1, 3, 2); c.fillStyle = c === a ? '#000' : '#8c7a66'; c.fillRect(-1.5, 0, 1, 1); c.fillRect(0.5, 0, 1, 1); }
      const p0 = hm.transformPoint({ x: dir.lamp[0], y: dir.lamp[1] }), p1 = hm.transformPoint({ x: dir.lamp[0] + 10, y: dir.lamp[1] });
      lights.push({ beam: true, x: p0.x, y: p0.y, ang: Math.atan2(p1.y - p0.y, p1.x - p0.x), spread: 30 * DEG, r: 9 * CELL, c: [1, 0.93, 0.78], k: 1.7 });
      lights.push({ x: p0.x, y: p0.y, r: 2 * CELL, c: [1, 0.92, 0.75], k: 0.35 });
    }
    // the drill in both hands, drawn before the near arm
    this.bit = null;
    if (s.drill) {
      const frame = Math.floor(this.spinFrame), pic = frame > 0 ? IMG['hero_drill_spin' + frame] : IMG.hero_drill;
      const p = pivotAt(this.hold), dm = M.translate(p.x, -R.Shoulder + p.y).rotate(deg(this.hold)).translate(-DRILL.mount.x, -DRILL.mount.y);
      put(dm, plainCache(pic), [0, 0]);
      if (s.drill.spinning) {
        const lp = dm.transformPoint(DRILL.lamp);
        for (const c of [a, e]) { c.setTransform(cam); c.fillStyle = c === a ? '#000' : '#9dff8a'; c.fillRect(lp.x - 0.5, lp.y - 0.5, 1, 1); }
        lights.push({ x: lp.x, y: lp.y, r: 18, c: [0.6, 1, 0.55], k: 0.25 });
      }
      this.bit = dm.transformPoint(DRILL.edgeTo);
      const bk = dm.transformPoint({ x: 25, y: 5.5 }); this.bitDir = { x: this.bit.x - bk.x, y: this.bit.y - bk.y };
    }
    limb(M.translate(0, -R.Shoulder), fa, 'upperArm', 'forearm', false, true);
    return lights;
  }
}
const PLAIN = new Map();
function plainCache(img) { if (!PLAIN.has(img)) PLAIN.set(img, plainPart(img)); return PLAIN.get(img); }
