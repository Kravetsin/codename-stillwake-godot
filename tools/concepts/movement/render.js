'use strict';
// ===== the figure (Rig.cs, Pose.cs, Figure.cs) on a canvas, the proposal's additions behind `this.p` =====
const RIG = (() => {
  const W = 0.95 * CELL, H = 2.625 * CELL, Hip = H * 0.42, Shoulder = H * 0.61, Neck = H * 0.71;
  return { W, H, Hip, Shoulder, Neck, HipApart: W * 0.14, Arm: { u: H * 0.145, l: H * 0.145 }, Leg: { u: Hip * 0.5, l: Hip * 0.5 }, Back: W * 0.62 };
})();
const JOINTS = { helmet: [6.5, 12], torso: [6, 0], pack: [0, 0], upperArm: [2.5, 2.5], forearm: [2.5, 2.9], thigh: [3, 2.5], shin: [3, 2.18] };
const TAU = Math.PI * 2;
const B = (u, l) => ({ u, l });
const pos = n => Math.max(0, n);
const L = (fa, ba, fl, bl, tilt = 0, lift = 0) => ({ fa, ba, fl, bl, tilt, lift });
const smooth = t => t * t * (3 - 2 * t);
function endOf(b, s) { const t = b.u + b.l; return { x: Math.sin(b.u) * s.u + Math.sin(t) * s.l, y: Math.cos(b.u) * s.u + Math.cos(t) * s.l }; }
function reach(tg, s, bends) {
  const a = s.u, b = s.l, far = Math.hypot(tg.x, tg.y);
  const d = Math.min(a + b - 1e-3, Math.max(Math.abs(a - b) + 1e-3, far));
  const toward = Math.atan2(tg.x, tg.y);
  const atJ = Math.acos(clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1));
  const atB = Math.acos(clamp((a * a + b * b - d * d) / (2 * a * b), -1, 1));
  return { u: toward - bends * atJ, l: bends * (Math.PI - atB) };
}
const apart = (f, t) => ((t - f) % TAU + 3 * Math.PI) % TAU - Math.PI;
const mixA = (a, b, k) => a + apart(a, b) * k;
const mixB = (a, b, k) => ({ u: mixA(a.u, b.u, k), l: mixA(a.l, b.l, k) });
const mixL = (a, b, k) => ({ fa: mixB(a.fa, b.fa, k), ba: mixB(a.ba, b.ba, k), fl: mixB(a.fl, b.fl, k), bl: mixB(a.bl, b.bl, k), tilt: mixA(a.tilt, b.tilt, k), lift: a.lift + (b.lift - a.lift) * k });
const settle = (f, t, dt, within) => f + (t - f) * Math.min(1, dt / within);
const settleA = (f, t, dt, within) => { const ap = apart(f, t); return (t - ap) + ap * Math.min(1, dt / within); };
const settleB = (f, t, dt, w) => ({ u: settleA(f.u, t.u, dt, w), l: settleA(f.l, t.l, dt, w) });

function poseOf(h, afloat) {
  if (afloat) return 'Float';
  if (h.climbing) return 'Climb';
  if (h.vy !== 0) return 'Air';
  if (h.dx === 0) return 'Stand';
  return Math.abs(h.dx) > SPEC.walk ? 'Run' : 'Walk';
}
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
function climbing(cycle) {
  const arm = RIG.Arm.u + RIG.Arm.l;
  const hand = at => onRung(at, -arm * 0.85, arm * 0.6, RIG.W * 0.6, RIG.W * 0.2);
  const foot = at => onRung(at, RIG.Hip - arm * 0.75, arm * 0.7, RIG.W * 0.25, RIG.W * 0.25);
  return L(reach(hand(cycle), RIG.Arm, 1), reach(hand(cycle + 0.5), RIG.Arm, 1), reach(foot(cycle + 0.5), RIG.Leg, -1), reach(foot(cycle), RIG.Leg, -1), 0, 0.8 * Math.abs(Math.sin(cycle * TAU)));
}
function paddling(h, stroke, lean, rch, drift) {
  const p = stroke * TAU, along = Math.sign(h.dx) * h.facing, sinking = Math.sign(h.dy);
  const paddle = lag => B(0.75 + rch * Math.sin(p + lag), 0.75 + 0.35 * Math.cos(p + lag));
  const leg = lag => { const k = 0.1 * Math.sin(p + lag); if (sinking > 0) return B(0.85 + k, -1.35); return along !== 0 ? B(-0.2 + k, -(0.55 + 0.15 * Math.cos(p + lag))) : B(0.05 + k, -0.2); };
  return L(paddle(Math.PI), paddle(0), leg(Math.PI), leg(0), along * lean + (sinking > 0 ? 0.06 : 0), 0.5 * drift);
}
function floating(h, time, swim, fast) {
  const drift = Math.sin(time / 3400 * TAU), sway = Math.sin(time / 5200 * TAU);
  const still = L(B(0.15 + 0.06 * drift, 0.35), B(0.4 - 0.08 * drift, 0.55), B(0.25 + 0.1 * sway, -(0.5 + 0.1 * drift)), B(-0.08 * sway, -(0.35 - 0.1 * drift)), 0.03 * sway, 2 * drift);
  if (swim <= 0) return still;
  let sw = paddling(h, time / 1100, 0.22, 0.4, drift);
  if (fast > 0) sw = mixL(sw, paddling(h, time / 700, 0.38, 0.6, drift), fast);
  return swim >= 1 ? sw : mixL(still, sw, swim);
}
const RISE = L(B(2.3, 0.35), B(1.9, 0.5), B(1, -1.3), B(0.25, -0.8));
const FALL = L(B(1.4, 0.4), B(1.1, 0.5), B(0.35, -0.45), B(-0.05, -0.3));
function bodyOf(pose, h, m) {
  const phase = c => m.trav / c * TAU, wave = ms => Math.sin(m.time / ms * TAU);
  switch (pose) {
    case 'Stand': { const br = wave(2600); return L(B(0.05 * br, 0.18), B(-0.05 * br, 0.22), B(0.02, -0.04), B(-0.02, -0.04)); }
    case 'Walk': { const p = phase(2.8), s = Math.sin(p), c = Math.cos(p);
      return L(B(-0.3 * s, 0.25 + 0.25 * pos(-c)), B(0.3 * s, 0.25 + 0.25 * pos(c)), B(0.5 * s, -(0.08 + 0.8 * pos(c))), B(-0.5 * s, -(0.08 + 0.8 * pos(-c))), 0.03); }
    case 'Run': { const p = phase(4.6), s = Math.sin(p), q = p / TAU; return L(B(-0.8 * s, 1.4), B(0.8 * s, 1.4), running(q), running(q + 0.5), 0.15); }
    case 'Air': return h.vy < 0 ? { ...RISE } : { ...FALL };
    case 'Climb': return climbing(m.trav / m.climbStride);
    case 'Float': return floating(h, m.time, m.swim, m.fast);
  }
}
function groundedL(l) { const lowest = Math.max(endOf(l.fl, RIG.Leg).y, endOf(l.bl, RIG.Leg).y); return { ...l, lift: lowest - RIG.Hip }; }
function landingOf(l, landing, depth) {
  if (landing == null || landing === 0) return l;
  const t = 1 - landing, sink = RIG.Hip * 0.22 * depth * (t < 1 / 3 ? t * 3 : (1 - t) * 1.5);
  const foot = x => reach({ x, y: RIG.Hip - sink }, RIG.Leg, -1);
  return { ...l, fl: foot(RIG.W * 0.08), bl: foot(-RIG.W * 0.06), fa: B(0.55, 0.6), ba: B(0.35, 0.7) };
}

// ---------- particles: crisp motes, thin streaks, flat rings ----------
class Parts {
  constructor() { this.list = []; }
  add(p) { p.age = 0; this.list.push(p); if (this.list.length > 600) this.list.shift(); }
  step(dt) {
    const s = dt / 1000;
    for (const p of this.list) {
      p.age += dt;
      if (p.drag) { const k = Math.exp(-p.drag * s); p.vx *= k; p.vy *= k; }
      p.vy += (p.g || 0) * s; p.x += p.vx * s; p.y += p.vy * s;
      if (p.floor != null && p.y > p.floor) { p.y = p.floor; p.vy = 0; p.vx *= 0.8; }
    }
    this.list = this.list.filter(p => p.age < p.life);
  }
  draw(ctx, px) {
    for (const p of this.list) {
      const k = 1 - p.age / p.life, a = (p.a ?? 1) * (p.fade === 'in' ? Math.min(1, p.age / 60) * k : k);
      ctx.globalAlpha = a; ctx.strokeStyle = ctx.fillStyle = p.col;
      if (p.kind === 'mote') { const s = (p.size || 1); ctx.fillRect(p.x - s / 2, p.y - s / 2, s, s); }
      else if (p.kind === 'streak') {
        const sp = Math.hypot(p.vx, p.vy) || 1, len = (p.len || 4) * (0.4 + 0.6 * k);
        ctx.lineWidth = px * (p.w || 1); ctx.beginPath(); ctx.moveTo(p.x, p.y); ctx.lineTo(p.x - p.vx / sp * len, p.y - p.vy / sp * len); ctx.stroke();
      } else if (p.kind === 'ring') {
        const r = p.r0 + (p.r1 - p.r0) * (1 - k * k);
        ctx.lineWidth = px; ctx.beginPath(); ctx.ellipse(p.x, p.y, r * (p.sx ?? 1), r * (p.sy ?? 0.22), p.rot || 0, 0, TAU); ctx.stroke();
      }
    }
    ctx.globalAlpha = 1;
  }
}
const rnd = (a, b) => a + Math.random() * (b - a);
const DUST = '#b8b2a8', LIGHT = '#eef2f8', SPARK = '#ffd27a', THRUST = '#bfe6ff';

// ---------- the figure ----------
class Figure {
  constructor(prop) {
    Object.assign(this, { p: prop, shown: null, last: null, trav: 0, strideFrom: 0, wasPose: null, time: 0, head: 0,
      landMs: 0, landTotal: 220, landDepth: 1, fall: 0, stepN: null, swim: 0, fast: 0, idle: 0, airMs: 0, sinceJump: 999,
      skidT: 0, skidW: 0, along: 1, acc: 0, mantle: 0, pushMs: 999, pushDir: null,
      pack: { x: 0, y: 0, vx: 0, vy: 0 }, helm: { x: 0, y: 0, vx: 0, vy: 0 }, pdx: 0, pdy: 0, ant: null, antAnchor: null, antDir: null, kick: 0 });
  }
  // once a controller step: springs, lean, skid, step-born effects
  onStep(run, h) {
    const ax = h.dx - this.pdx, ay = h.dy - this.pdy; this.pdx = h.dx; this.pdy = h.dy;
    const pose = poseOf(h, run.grid && bodyAfloat(run.grid, h));
    if (!this.p) return;
    const ground = pose === 'Walk' || pose === 'Run' || pose === 'Stand';
    this.acc += ((ground ? ax : 0) - this.acc) * 0.3;
    const braking = ground && Math.abs(h.dx) > 50 && Math.sign(ax) === -Math.sign(h.dx) && Math.abs(ax) >= 3;
    if (braking && P.skid) { this.skidT = 7; this.along = Math.sign(h.dx) * h.facing; } else this.skidT = Math.max(0, this.skidT - 1);
    if (braking && P.skid && Math.random() < 0.7) {
      const fx = h.x / U * CELL + Math.sign(h.dx) * 4, fy = h.y / U * CELL;
      run.parts.add({ kind: 'streak', x: fx, y: fy - 0.5, vx: Math.sign(h.dx) * rnd(40, 90), vy: -rnd(0, 6), len: rnd(3, 7), life: rnd(180, 300), col: DUST, a: 0.8, drag: 6 });
      if (Math.random() < 0.5) run.parts.add({ kind: 'mote', x: fx, y: fy - 1, vx: Math.sign(h.dx) * rnd(10, 40), vy: -rnd(5, 18), g: 40, life: rnd(250, 450), col: DUST, a: 0.7, floor: fy - 0.5 });
    }
    // springs: the air pack and the helmet lag the body's change of speed
    const dt = 1 / 60, k = 600, c = 16, toPx = CELL / U * 3600;
    const lag = P.packLag;
    for (const [s, gain] of [[this.pack, 1], [this.helm, 0.4]]) {
      const fx = -ax * toPx * 0.35 * lag * gain, fy = -ay * toPx * 0.05 * lag * gain;
      s.vx += (-k * s.x - c * s.vx + fx) * dt; s.vy += (-k * s.y - c * s.vy + fy) * dt;
      s.x = clamp(s.x + s.vx * dt, -2, 2); s.y = clamp(s.y + s.vy * dt, -2, 2);
    }
    // ladder slide: the gloves throw sparks
    if (h.slide && this.hands) for (const hp of this.hands) if (Math.random() < 0.6)
      run.parts.add({ kind: 'streak', x: hp.x, y: hp.y, vx: rnd(-30, 30), vy: -rnd(40, 110), len: rnd(2, 4), life: rnd(120, 260), col: SPARK, g: 260 });
    if (h.topped) this.mantle = 170;
    if (h.pushed) {
      this.pushMs = 0; this.pushDir = h.pushed;
      const mx = h.x / U * CELL + h.pushed.x * RIG.W * 0.5, my = (h.y - MID) / U * CELL + h.pushed.y * RIG.H * 0.5;
      run.parts.add({ kind: 'ring', x: mx, y: my, r0: 2, r1: 11, life: 320, col: LIGHT, a: 0.45, sx: h.pushed.x ? 0.25 : 1, sy: h.pushed.x ? 1 : 0.25 });
      for (let i = 0; i < 6; i++) run.parts.add({ kind: 'mote', x: mx, y: my, vx: -h.pushed.x * rnd(5, 25) + rnd(-15, 15), vy: -h.pushed.y * rnd(5, 25) + rnd(-15, 15), life: rnd(400, 800), col: DUST, a: 0.6, drag: 1.5 });
    }
    if (h.rcs && this.nozzle && Math.random() < 0.35) {
      const sp = Math.hypot(h.rcs.x, h.rcs.y) || 1, ux = h.rcs.x / sp, uy = h.rcs.y / sp;
      const side = Math.random() < 0.5 ? -1 : 1;
      run.parts.add({ kind: 'streak', x: this.nozzle.x + side * 2, y: this.nozzle.y - 6, vx: ux * rnd(60, 110) + side * 20, vy: uy * rnd(60, 110), len: rnd(3, 5), life: rnd(120, 200), col: LIGHT, a: 0.75 });
    }
  }
  update(run, h, dt) {
    const afloat = bodyAfloat(run.grid, h), pose = poseOf(h, afloat), prop = this.p;
    this.time += dt;
    if (this.last) this.trav += (pose === 'Climb' ? Math.abs(h.y - this.last.y) : (h.x - this.last.x) * h.facing) / U;
    this.last = { x: h.x, y: h.y };
    const striding = pose === 'Walk' || pose === 'Run', strode = this.wasPose === 'Walk' || this.wasPose === 'Run';
    if (pose !== this.wasPose && !(striding && strode)) { this.strideFrom = this.trav; this.stepN = null; }
    const feetX = h.x / U * CELL, feetY = h.y / U * CELL;
    const was = this.wasPose;
    const jumped = (was === 'Stand' || was === 'Walk' || was === 'Run' || was === 'Climb') && pose === 'Air' && h.vy <= -SPEC.jump / 2;
    if (jumped) {
      this.sinceJump = 0;
      if (prop) for (const s of [-1, 1]) {
        for (let i = 0; i < 3; i++) run.parts.add({ kind: 'mote', x: feetX + s * rnd(1, 4), y: feetY - 0.5, vx: s * rnd(10, 40), vy: -rnd(2, 10), life: rnd(250, 450), col: DUST, a: 0.7, drag: 3, floor: feetY - 0.4 });
        run.parts.add({ kind: 'streak', x: feetX + s * 2, y: feetY - 0.4, vx: s * rnd(50, 80), vy: 0, len: rnd(3, 6), life: 200, col: DUST, a: 0.6, drag: 8 });
      }
    }
    this.sinceJump += dt;
    if (pose === 'Air') { this.fall = Math.max(0, h.vy); this.airMs += dt; } else this.airMs = 0;
    const ground = pose === 'Stand' || pose === 'Walk' || pose === 'Run';
    if (was === 'Air' && ground && this.fall > 0) {
      if (prop) {
        this.landDepth = clamp(this.fall / 420, 0.35, 1.8) * P.landScale * (Math.abs(h.dx) > 60 ? 0.6 : 1);
        this.landTotal = 150 + 110 * clamp(this.fall / 600, 0, 1);
      } else { this.landDepth = 1; this.landTotal = 220; }
      this.landMs = this.landTotal;
      const grains = clamp(Math.round(this.fall / 50), 3, 9);
      for (const s of [-1, 1]) for (let i = 0; i < grains; i++) {
        run.parts.add({ kind: 'mote', x: feetX + s * rnd(1, 4), y: feetY - 0.5, vx: s * rnd(30, 80), vy: -rnd(2, 16), g: 30, drag: 4, life: rnd(300, 600), col: DUST, a: 0.8, floor: feetY - 0.4 });
      }
      const heavy = prop && this.fall >= 480;
      run.parts.add({ kind: 'ring', x: feetX, y: feetY - 0.4, r0: 3, r1: heavy ? 34 : 3 + 26 * clamp(this.fall / 1200, 0.2, 1), life: heavy ? 260 : 170, col: LIGHT, a: heavy ? 0.45 : 0.3, sy: 0.12 });
      if (heavy) { run.camKick = 2.2; for (const s of [-1, 1]) for (let i = 0; i < 2; i++) run.parts.add({ kind: 'streak', x: feetX + s * 3, y: feetY - 0.6, vx: s * rnd(90, 140), vy: -rnd(0, 8), len: rnd(5, 9), life: 220, col: LIGHT, a: 0.55, drag: 7 }); }
    }
    const stride = pose === 'Run' ? 4.6 : pose === 'Walk' ? 2.8 : null;
    const stepping = stride ? Math.floor((this.trav - this.strideFrom - stride / 4) / (stride / 2)) : null;
    if (stepping != null && this.stepN != null && stepping !== this.stepN && pose === 'Run') {
      const kx = feetX + h.facing * RIG.W * 0.3;
      for (let i = 0; i < 5; i++) run.parts.add({ kind: 'mote', x: kx + rnd(-1.5, 1.5), y: feetY - 0.4, vx: h.facing * rnd(-24, 6), vy: -rnd(4, 15), g: 20, life: rnd(300, 550), col: DUST, a: 0.6, floor: feetY - 0.3 });
    }
    this.stepN = stepping;
    this.landMs = Math.max(0, this.landMs - dt);
    this.wasPose = pose;
    if (pose === 'Stand') this.idle += dt; else this.idle = 0;
    this.swim = afloat ? settle(this.swim, h.dx || h.dy ? 1 : 0, dt, 320) : 0;
    this.fast = afloat ? settle(this.fast, Math.abs(h.dx) > SPEC.swim || Math.abs(h.dy) > SPEC.swim ? 1 : 0, dt, 320) : 0;
    const m = { trav: this.trav - this.strideFrom, time: this.time, swim: this.swim, fast: this.fast, climbStride: prop ? P.climbStride : 3.6 };
    let to = bodyOf(pose, h, m);
    let look = 0;
    if (prop) to = this.propose(pose, h, to, dt, (l) => { look = l; });
    if (ground) to = groundedL(landingOf(to, this.landMs / this.landTotal, this.landDepth));
    if (prop && pose === 'Stand' && P.idleLife) to.lift += 0.45 * (0.5 + 0.5 * Math.sin(this.time / 2600 * TAU));
    const from = this.shown || to, within = pose === 'Float' ? 110 : 30;
    this.shown = {
      fa: settleB(from.fa, to.fa, dt, within), ba: settleB(from.ba, to.ba, dt, within), fl: settleB(from.fl, to.fl, dt, within), bl: settleB(from.bl, to.bl, dt, within),
      tilt: settleA(from.tilt, to.tilt, dt, afloat ? 450 : prop ? 60 : 30), lift: settle(from.lift, to.lift, dt, 30),
    };
    this.head = settle(this.head, look, dt, prop && pose === 'Stand' ? 260 : 90);
    this.pushMs += dt; this.mantle = Math.max(0, this.mantle - dt);
    this.skidW = settle(this.skidW, this.skidT > 0 ? 1 : 0, dt, 45);
    this.facing = h.facing; this.feet = { x: feetX, y: feetY }; this.h = h; this.pose = pose;
  }
  propose(pose, h, to, dt, setLook) {
    if (pose === 'Air' && P.airBlend) {
      const k = smooth(clamp((h.vy + 150) / 450, 0, 1));
      to = mixL(RISE, FALL, k);
      const apex = Math.abs(h.vy) < 60 ? (1 - Math.abs(h.vy) / 60) * 0.5 : 0;
      if (apex) to = mixL(to, L(B(2.0, 0.5), B(1.7, 0.6), B(1.25, -1.6), B(0.6, -1.3)), apex);
      if (this.sinceJump < 110) to = mixL(to, L(B(2.6, 0.2), B(2.2, 0.25), B(-0.1, -0.08), B(-0.35, -0.1)), 1 - this.sinceJump / 110);
      if (this.airMs > 450 && h.vy > 300) { const f = clamp((this.airMs - 450) / 300, 0, 1); to.fa = { ...to.fa, u: to.fa.u + 0.35 * f * Math.sin(this.time / 70) }; to.ba = { ...to.ba, u: to.ba.u - 0.3 * f * Math.sin(this.time / 85 + 1) }; to.fl = { ...to.fl, u: to.fl.u + 0.15 * f * Math.sin(this.time / 110) }; }
      if (Math.abs(h.dx) > 100) to.tilt = 0.1 * Math.sign(h.dx) * h.facing;
      if (h.vy > 500) setLook(0.35);
    }
    if (pose === 'Walk' || pose === 'Run' || pose === 'Stand') {
      to.tilt += clamp(this.acc * h.facing * 0.006 * P.lean, -0.22, 0.25);
      if (this.skidW > 0.01 && P.skid) {
        const sk = this.along > 0 ? L(B(0.6, 0.9), B(0.9, 0.6), B(0.75, -0.15), B(0.1, -0.9), -0.14) : L(B(-0.4, 1.3), B(0.8, 1.3), B(0.35, -1.0), B(-0.6, -0.2), 0.22);
        to = mixL(to, sk, this.skidW);
      }
    }
    if (pose === 'Stand' && P.idleLife && this.idle > 2500) {
      const t = this.idle - 2500, shift = smooth(clamp(Math.sin(t / 7000 * TAU) * 1.6, 0, 1));
      to = mixL(to, { ...to, fl: B(0.12, -0.06), bl: B(-0.12, -0.28), tilt: to.tilt + 0.025 }, shift);
      const g = (t % 5200) / 5200;
      setLook(g < 0.25 ? -0.4 : g > 0.45 && g < 0.62 ? 0.32 : 0);
    }
    if (pose === 'Climb' && h.slide) to = L(B(2.78, 0.12), B(2.58, 0.18), B(0.2, -0.15), B(-0.12, -0.08));
    if (this.mantle > 0 && pose !== 'Climb') to = mixL(to, L(B(0.45, 0.3), B(0.6, 0.2), B(1.3, -1.8), B(-0.2, -0.3), 0.15), this.mantle / 170);
    if (pose === 'Float') {
      if (this.pushMs < 320 && this.pushDir) { const k = 1 - this.pushMs / 320; to = mixL(to, L(B(1.5, 0.25), B(1.3, 0.3), B(-0.35, -0.05), B(-0.15, -0.05), 0.2 * Math.sign(h.dx) * h.facing), k); }
      if (h.thrust) setLook(clamp(Math.atan2(h.thrust.y, Math.abs(h.thrust.x) + 0.15), -0.6, 0.5));
    }
    return to;
  }
  // the antenna: three points chasing their rest along the helmet, kept a segment apart; stepped as drawn
  antStep() {
    const a = this.antAnchor, d = this.antDir, seg = 2.4, now = this.time;
    const far = this.ant && Math.hypot(this.ant[0].x - a.x, this.ant[0].y - a.y) > 8;
    if (!this.ant || far) { this.ant = [1, 2, 3].map(i => ({ x: a.x + d.x * seg * i, y: a.y + d.y * seg * i, px: a.x + d.x * seg * i, py: a.y + d.y * seg * i })); this.antT = now; return; }
    let n = Math.min(6, Math.round((now - this.antT) / 16.67)); this.antT += n * 16.67;
    while (n-- > 0) {
      let prev = a;
      this.ant.forEach((q, i) => {
        const vx = (q.x - q.px) * 0.9, vy = (q.y - q.py) * 0.9 + 0.02; q.px = q.x; q.py = q.y;
        q.x += vx; q.y += vy;
        const tx = a.x + d.x * seg * (i + 1), ty = a.y + d.y * seg * (i + 1), st = [0.3, 0.13, 0.08][i];
        q.x += (tx - q.x) * st; q.y += (ty - q.y) * st;
        const dx = q.x - prev.x, dy = q.y - prev.y, l = Math.hypot(dx, dy) || 1;
        q.x = prev.x + dx / l * seg; q.y = prev.y + dy / l * seg; prev = q;
      });
    }
  }
  draw(ctx, cam, px) {
    const s = this.shown; if (!s) return;
    const prop = this.p, f = this.facing;
    const M = new DOMMatrix().translate(this.feet.x, this.feet.y).scale(f, 1).translate(0, -RIG.H / 2 - s.lift).rotate(s.tilt * 180 / Math.PI).translate(0, RIG.H / 2);
    const put = (m, img, j) => { ctx.setTransform(cam.multiply(m)); ctx.drawImage(img, -j[0], -j[1]); };
    const deg = r => r * 180 / Math.PI;
    const hands = [];
    const limb = (m, b, up, lo, far, arm) => {
      const mu = m.rotate(-deg(b.u)), seg = arm ? RIG.Arm : RIG.Leg;
      const ml = mu.translate(0, seg.u).rotate(-deg(b.l));
      put(ml, IMG[far ? 'far' : 'near'][lo], JOINTS[lo]);
      put(mu, IMG[far ? 'far' : 'near'][up], JOINTS[up]);
      if (arm) hands.push(ml.translate(0, seg.l).transformPoint({ x: 0, y: 0 }));
    };
    limb(M.translate(0, -RIG.Shoulder), s.ba, 'upperArm', 'forearm', true, true);
    limb(M.translate(-RIG.HipApart, -RIG.Hip), s.bl, 'thigh', 'shin', true, false);
    limb(M.translate(RIG.HipApart, -RIG.Hip), s.fl, 'thigh', 'shin', false, false);
    const po = prop ? this.pack : { x: 0, y: 0 }, ho = prop ? this.helm : { x: 0, y: 0 };
    const packM = M.translate(-RIG.Back + po.x * f, -RIG.Shoulder - RIG.H * 0.02 + po.y);
    put(packM, IMG.near.pack, JOINTS.pack);
    if (this.jet) { const jm = packM.translate(-2, -1); put(jm, IMG.jetpack, [0, 0]); this.nozzle = jm.transformPoint({ x: 3, y: 14 }); } else this.nozzle = null;
    put(M.translate(0, -RIG.Neck), IMG.near.torso, JOINTS.torso);
    const hm = M.translate(ho.x * f, -RIG.Neck + ho.y).rotate(deg(this.head));
    // the antenna behind the helmet
    if (prop && P.antenna) {
      this.antAnchor = hm.transformPoint({ x: -4.2, y: -10.6 });
      const tip = hm.transformPoint({ x: -4.2 - 0.45, y: -10.6 - 1 }); const l = Math.hypot(tip.x - this.antAnchor.x, tip.y - this.antAnchor.y);
      this.antDir = { x: (tip.x - this.antAnchor.x) / l, y: (tip.y - this.antAnchor.y) / l };
      this.antStep();
      if (this.ant) {
        ctx.setTransform(cam); ctx.strokeStyle = '#8b8f98'; ctx.lineWidth = 0.75; ctx.beginPath(); ctx.moveTo(this.antAnchor.x, this.antAnchor.y);
        for (const q of this.ant) ctx.lineTo(q.x, q.y); ctx.stroke();
        const t = this.ant[2]; ctx.fillStyle = '#f6e7b8'; ctx.fillRect(t.x - 0.5, t.y - 0.5, 1, 1);
      }
    } else { this.ant = null; this.antAnchor = null; }
    put(hm, IMG.near.helmet, JOINTS.helmet);
    limb(M.translate(0, -RIG.Shoulder), s.fa, 'upperArm', 'forearm', false, true);
    this.hands = hands;
    // the plume while thrusting
    if (this.nozzle && this.h && this.h.thrust) {
      const t = this.h.thrust, n = this.nozzle, len = rnd(5, 8);
      ctx.setTransform(cam); ctx.lineCap = 'round';
      ctx.strokeStyle = THRUST; ctx.globalAlpha = 0.85; ctx.lineWidth = 1.6; ctx.beginPath(); ctx.moveTo(n.x, n.y); ctx.lineTo(n.x - t.x * len, n.y - t.y * len); ctx.stroke();
      ctx.strokeStyle = '#ffffff'; ctx.globalAlpha = 1; ctx.lineWidth = 0.8; ctx.beginPath(); ctx.moveTo(n.x, n.y); ctx.lineTo(n.x - t.x * len * 0.5, n.y - t.y * len * 0.5); ctx.stroke();
      ctx.globalAlpha = 1; ctx.lineCap = 'butt';
    }
  }
}

// ---------- one hero in one little world ----------
class Run {
  constructor(cfg, mode) { this.cfg = cfg; this.mode = mode; this.prop = mode === 'new'; this.reset(); }
  reset() {
    const c = this.cfg; this.grid = new Grid(MAPS[c.map]);
    const [col, feet] = c.spawn;
    this.hero = { x: col * U + 500, y: feet * U, vx: 0, vy: 0, dx: 0, dy: 0, climbing: false, facing: c.face || 1 };
    this.prev = this.hero; this.fig = new Figure(this.prop); this.parts = new Parts(); this.labels = []; this.trail = [];
    this.t = 0; this.mem = {}; this.acc = 0; this.camKick = 0;
    const m = this.mid(this.hero); this.cam = { x: m.x, y: m.y, refY: m.y, lead: 0 };
  }
  mid(h) { return { x: h.x / U * CELL, y: (h.y - HH / 2) / U * CELL }; }
  withP(fn) {
    const over = this.prop ? this.cfg.over : null; if (!over) return fn();
    const keep = {}; for (const k in over) { keep[k] = P[k]; P[k] = over[k]; }
    try { return fn(); } finally { Object.assign(P, keep); }
  }
  step(inp) {
    this.withP(() => {
      this.prev = this.hero;
      const h = this.prop ? stepNew(this.grid, this.hero, inp) : stepNow(this.grid, this.hero, inp);
      this.hero = h;
      this.fig.jet = !!inp.jet;
      this.fig.onStep(this, h);
      if (h.ev && this.prop && this.showLabels !== false) this.labels.push({ text: h.ev, x: h.x / U * CELL, y: (h.y - HH) / U * CELL - 3, age: 0 });
      if (h.thrust) {
        const n = this.fig.nozzle; if (n) for (let i = 0; i < 2; i++) this.parts.add({ kind: 'streak', x: n.x, y: n.y, vx: -h.thrust.x * rnd(70, 130) + rnd(-12, 12), vy: -h.thrust.y * rnd(70, 130) + rnd(-12, 12), len: rnd(3, 6), life: rnd(140, 260), col: i ? '#ffffff' : THRUST, a: 0.8 });
      }
      if (this.trailOn) { this.trail.push({ x: h.x / U * CELL, y: (h.y - MID) / U * CELL }); if (this.trail.length > 150) this.trail.shift(); }
    });
    this.t++;
  }
  frame(dt, scale, input) {
    this.acc += dt * scale;
    let n = 0;
    while (this.acc >= STEP_MS && n < 8) {
      this.acc -= STEP_MS; n++;
      const inp = input ? input(this) : this.cfg.script(this.hero, this.t, this.mem, this);
      this.step(inp);
      if (!input && this.cfg.loop && this.t >= this.cfg.loop) { const keepTrail = this.trailOn; this.reset(); this.trailOn = keepTrail; }
    }
    const a = this.acc / STEP_MS, h = this.hero, p = this.prev;
    const jump = Math.abs(h.x - p.x) > 3000 || Math.abs(h.y - p.y) > 3000;
    this.drawn = jump ? h : { ...h, x: p.x + (h.x - p.x) * a, y: p.y + (h.y - p.y) * a };
    this.withP(() => { this.fig.update(this, this.drawn, dt * scale); this.camera(dt * scale); });
    this.parts.step(dt * scale);
    for (const l of this.labels) l.age += dt; this.labels = this.labels.filter(l => l.age < 1100);
  }
  camera(dt) {
    const m = this.mid(this.drawn), c = this.cam, h = this.hero;
    if (!this.prop || this.cfg.camNow) { c.x = m.x; c.y = this.cfg.camY != null ? this.cfg.camY * CELL : m.y; return; }
    const settled = h.climbing || bodyAfloat(this.grid, h) || (h.vy === 0 && grounded(this.grid, h.x, h.y));
    c.lead = settle(c.lead, clamp(h.dx / SPEC.run, -1, 1), dt, 350);
    const tx = m.x + c.lead * P.camLead * CELL;
    if (!P.camSnap || settled) c.refY = m.y;
    const ty = clamp(c.refY, m.y - CELL * 1.0, m.y + CELL * 1.6);
    const k = P.camSmoothMs > 0 ? 1 - Math.exp(-dt / P.camSmoothMs) : 1;
    c.x += (tx - c.x) * k; c.y += (ty - c.y) * k;
    c.y = clamp(c.y, m.y - CELL * 1.2, m.y + CELL * 1.6); c.x = clamp(c.x, m.x - CELL * (P.camLead + 1), m.x + CELL * (P.camLead + 1));
    this.camKick = this.camKick * Math.exp(-dt / 70);
  }
  draw(ctx, cw, ch, scale) {
    ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.imageSmoothingEnabled = false;
    ctx.fillStyle = '#06080c'; ctx.fillRect(0, 0, cw, ch);
    const cx = Math.round((cw / 2 - this.cam.x * scale)), cy = Math.round((ch / 2 - (this.cam.y + this.camKick) * scale));
    const cam = new DOMMatrix([scale, 0, 0, scale, cx, cy]);
    ctx.setTransform(cam); ctx.drawImage(sceneArt(this.cfg.map), 0, 0);
    if (this.trail.length) { ctx.fillStyle = this.prop ? '#5fc8c0' : '#9aa3ad'; for (let i = 0; i < this.trail.length; i += 2) { const t = this.trail[i]; ctx.globalAlpha = 0.25 + 0.6 * i / this.trail.length; ctx.fillRect(t.x - 0.5, t.y - 0.5, 1, 1); } ctx.globalAlpha = 1; }
    this.fig.draw(ctx, cam, 1 / scale);
    ctx.setTransform(cam); this.parts.draw(ctx, 1 / scale);
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.font = `${Math.round(11 * (scale / 2.5))}px "JetBrains Mono", monospace`; ctx.textAlign = 'center';
    for (const l of this.labels) {
      const a = 1 - l.age / 1100, x = cx + l.x * scale, y = cy + (l.y - l.age / 1100 * 6) * scale;
      ctx.globalAlpha = a; ctx.fillStyle = '#06080c'; ctx.fillText(l.text, x + 1, y + 1); ctx.fillStyle = '#9fe9e2'; ctx.fillText(l.text, x, y);
    }
    ctx.globalAlpha = 1;
  }
}

// ---------- the scene's tiles, drawn once ----------
const SCENE_ART = {};
function sceneArt(id) {
  if (SCENE_ART[id]) return SCENE_ART[id];
  const m = MAPS[id], c = document.createElement('canvas'); c.width = m.w * CELL; c.height = m.h * CELL;
  const g = c.getContext('2d'); g.imageSmoothingEnabled = false;
  const ch = (x, y) => x < 0 || y < 0 || x >= m.w || y >= m.h ? '#' : m.rows[y][x];
  if (m.planet) { const gr = g.createLinearGradient(0, 0, 0, c.height); gr.addColorStop(0, '#1a1426'); gr.addColorStop(1, '#3a2630'); g.fillStyle = gr; g.fillRect(0, 0, c.width, c.height); }
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) {
    const k = ch(x, y), X = x * CELL, Y = y * CELL;
    if (k === '#') continue;
    if (!m.planet) g.drawImage(IMG.tile.back, X, Y);
    if (k === '~') { g.fillStyle = 'rgba(70,120,190,0.16)'; g.fillRect(X, Y, CELL, CELL); }
  }
  if (m.planet) { g.fillStyle = '#fff'; let s = 7; for (let i = 0; i < 140; i++) { s = (s * 9301 + 49297) % 233280; const x = s / 233280 * c.width; s = (s * 9301 + 49297) % 233280; const y = s / 233280 * c.height * 0.7; g.globalAlpha = 0.3 + (i % 5) * 0.12; g.fillRect(Math.floor(x), Math.floor(y), 1, 1); } g.globalAlpha = 1; }
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) {
    const k = ch(x, y), X = x * CELL, Y = y * CELL;
    if (k === 'H') g.drawImage(IMG.tile.ladder, X, Y);
    if (k === '=') g.drawImage(IMG.tile.floor, X, Y);
    if (k === '#') {
      { const tw = IMG.tile.wall, n = Math.max(1, tw.width / CELL); g.drawImage(tw, (x % n) * CELL, (y % n) * CELL, CELL, CELL, X, Y, CELL, CELL); }
      const open = (ch(x, y - 1) !== '#' ? 1 : 0) | (ch(x + 1, y) !== '#' ? 2 : 0) | (ch(x, y + 1) !== '#' ? 4 : 0) | (ch(x - 1, y) !== '#' ? 8 : 0);
      if (open) g.drawImage(IMG.edge[open], X, Y);
      if (m.planet) { g.globalCompositeOperation = 'multiply'; g.fillStyle = '#b07a56'; g.fillRect(X, Y, CELL, CELL); g.globalCompositeOperation = 'source-over'; }
    }
  }
  g.fillStyle = 'rgba(8,12,20,0.3)'; g.fillRect(0, 0, c.width, c.height);
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) if (ch(x, y) === '#' && !m.planet) { g.fillStyle = 'rgba(10,14,24,0.38)'; g.fillRect(x * CELL, y * CELL, CELL, CELL); }
  return SCENE_ART[id] = c;
}

// ---------- the pictures, recoloured into the first suit as SuitArt does ----------
const IMG = { near: {}, far: {}, tile: {}, edge: [] };
function shade(c, by) { const ch = s => { const v = (c >> s) & 255; return Math.round(by >= 0 ? v * by : v + (255 - v) * -by); }; return (ch(16) << 16) | (ch(8) << 8) | ch(0); }
const shadesOf = c => [shade(c, -0.35), c, shade(c, 0.72), shade(c, 0.42)];
function loadImage(src) { return new Promise((ok, no) => { const i = new Image(); i.onload = () => ok(i); i.onerror = no; i.src = src; }); }
function recolour(img, colour) {
  const c = document.createElement('canvas'); c.width = img.width; c.height = img.height;
  const g = c.getContext('2d'); g.drawImage(img, 0, 0);
  const d = g.getImageData(0, 0, c.width, c.height), from = shadesOf(0xe0a030), to = shadesOf(colour);
  for (let i = 0; i < d.data.length; i += 4) {
    if (!d.data[i + 3]) continue;
    const at = from.indexOf((d.data[i] << 16) | (d.data[i + 1] << 8) | d.data[i + 2]);
    if (at >= 0) { d.data[i] = to[at] >> 16 & 255; d.data[i + 1] = to[at] >> 8 & 255; d.data[i + 2] = to[at] & 255; }
  }
  g.putImageData(d, 0, 0); return c;
}
async function loadArt(suit) {
  const far = shade(suit, 0.7);
  for (const k of ['helmet', 'torso', 'pack', 'upperArm', 'forearm', 'thigh', 'shin']) {
    const img = await loadImage(ART['hero_' + k]);
    IMG.near[k] = recolour(img, suit); IMG.far[k] = recolour(img, far);
  }
  IMG.jetpack = await loadImage(ART.hero_jetpack);
  IMG.tile.back = await loadImage(ART.back_iron_plain); IMG.tile.ladder = await loadImage(ART.ladder);
  IMG.tile.floor = await loadImage(ART.floor); IMG.tile.wall = await loadImage(ART.wall_iron);
  for (let i = 1; i < 16; i++) IMG.edge[i] = await loadImage(ART['wall-edge_iron_' + i]);
}
