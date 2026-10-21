'use strict';
// ===== the hero afloat: the game's figure (Pose.cs, Figure.cs, TwoHand.cs) with the arm variants =====
// Arms are worked out as hand points in the shoulder's frame (+x the way it faces, +y down, px), then
// bent by the rig's own reach(); legs, lean and drift are the game's Floating() untouched.
const ARM = RIG.Arm.u + RIG.Arm.l;
const PXS = CELL / U * 60;   // thousandths a step -> px a second
const VARIANTS = [
  { id: 'now', name: 'Сейчас', base: 'now', short: 'Как в игре: обе руки гребут по кругу, раз в 1,1 с, на ранце раз в 0,7 с.' },
  { id: 'A', name: 'Нейтральная поза', base: 'nbp', short: 'Поза NASA: руки сами всплывают вперёд, локти согнуты, кисти у груди. Гребка нет совсем.' },
  { id: 'B', name: 'Руки по инерции', base: 'nbp', spring: 1, short: 'A плюс пружины: кисти отстают от рывков тела и раскачиваются, потом успокаиваются.' },
  { id: 'C', name: 'Руки к опоре', base: 'nbp', reach: 1, short: 'A плюс опора: ближняя рука тянется к стене или перекладине и держится, толчок делают руки, удар гасят руки.' },
  { id: 'D', name: 'Руки на ранце', base: 'nbp', jet: 1, short: 'A плюс ранец: пока тяга, руки на груди, на лямках и пульте, а без тяги возвращаются.' },
  { id: 'E', name: 'Минимум', base: 'stand', short: 'Руки как в стойке, только тело медленно дышит и плавает.' },
  { id: 'F', name: 'Рука вперёд', base: 'nbp', lead: 1, short: 'Моё: ведущая рука вытянута туда, куда герой летит. Курс читается без слов.' },
  { id: 'G', name: 'Гребок и скольжение', base: 'nbp', stroke: 1, short: 'Моё: на старте плавания один гребок двумя руками вместе, дальше скольжение. Повтор не чаще раза в 1,8 с.' },
  { id: 'M', name: 'Микс A+B+C+D', base: 'nbp', spring: 1, reach: 1, jet: 1, short: 'Что я бы взял: нейтральная поза, инерция, опора у стен, руки на ранце при тяге.' },
];
const VAR = Object.fromEntries(VARIANTS.map(v => [v.id, v]));
const SPRING = { 0: { hz: 2.1, zeta: 0.3, gain: 1 }, 1: { hz: 1.75, zeta: 0.3, gain: 1 }, axe: { hz: 1.5, zeta: 0.4, gain: 0.35, most: 3 }, hold: { hz: 1.8, zeta: 0.42, gain: 0.7 } };
const STROKE = { ms: 650, every: 1800 };
const NBP = { f: [1.0, 0.85], b: [1.4, 0.75] };
const CHEST = [{ x: 6.5, y: 2.2 }, { x: 5.6, y: 1.2 }];

// the two-handed hold (TwoHand.cs): the mount on a pivot that moves with the turn, both fists on their grips
const DEG = Math.PI / 180;
const TW = { keys: [[-55 * DEG, { x: 5, y: 5.5 }], [0, { x: 2.5, y: 8 }], [35 * DEG, { x: 3, y: 6 }], [85 * DEG, { x: 5.5, y: 3.5 }]], rest: 0.3 };
const DRILL = { grip: [3, 10.5], fore: [12, 10.5], mount: [7.5, 10.5] }, AXE = { grip: [7.5, 7], hold: Math.PI / 2 };
const TOFIST = { u: RIG.Arm.u, l: RIG.Arm.l - 1 };
function pivotAt(a) {
  const k = TW.keys; if (a <= k[0][0]) return k[0][1];
  for (let i = 1; i < k.length; i++) if (a <= k[i][0]) { const t = (a - k[i - 1][0]) / (k[i][0] - k[i - 1][0]), p = k[i - 1][1], q = k[i][1]; return { x: p.x + (q.x - p.x) * t, y: p.y + (q.y - p.y) * t }; }
  return k[k.length - 1][1];
}
function twoAt(a, px, po) { const pv = pivotAt(a), x = px[0] - DRILL.mount[0], y = px[1] - DRILL.mount[1], c = Math.cos(a), s = Math.sin(a); return { x: pv.x + po.x + x * c - y * s, y: pv.y + po.y + x * s + y * c }; }
const fistOf = b => { const e = endOf(b, RIG.Arm), t = b.u + b.l; return { x: e.x - Math.sin(t), y: e.y - Math.cos(t) }; };

const lerpP = (a, b, k) => ({ x: a.x + (b.x - a.x) * k, y: a.y + (b.y - a.y) * k });
const len = p => Math.sqrt(p.x * p.x + p.y * p.y);
// hands kept off the visor: up by the helmet they come out in front of it, as the climb's do
function clearVisor(p) { return p.y < -3 && p.x > -7 && p.x < 8.5 ? { x: 8.5, y: p.y } : p; }

// ---------- what the hands can hold: walls, and a ladder's rungs (seen in front of the body, as climbing) ----------
function nearestSurface(grid, S, from, R, f) {
  let best = null;
  const c0 = Math.floor((from.x - R) / CELL), c1 = Math.floor((from.x + R) / CELL), r0 = Math.floor((from.y - R) / CELL), r1 = Math.floor((from.y + R) / CELL);
  for (let r = r0; r <= r1; r++) for (let c = c0; c <= c1; c++) {
    const k = grid.ch(c, r), X = c * CELL, Y = r * CELL;
    let pts = null;
    if (k === '#') pts = [{ x: clamp(from.x, X, X + CELL), y: clamp(from.y, Y, Y + CELL) }];
    else if (k === 'h' && Math.abs(S.x - (X + 8)) < 9) pts = [Y + 5, Y + 12].filter(y => y >= S.y - 4 && y <= S.y + 8).map(y => ({ x: S.x + f * RIG.W * 0.6, y, rung: true }));
    if (!pts) continue;
    for (const p of pts) { const d = Math.hypot(p.x - from.x, p.y - from.y); if (d <= R && (!best || d < best.d)) best = { ...p, d }; }
  }
  return best;
}
function rayHit(grid, S, dir, L) {
  for (let d = 2; d <= L; d += 1) { const x = S.x + dir.x * d, y = S.y + dir.y * d; if (grid.ch(Math.floor(x / CELL), Math.floor(y / CELL)) === '#') return { x, y, d }; }
  return null;
}

class ZFigure {
  constructor(v) {
    Object.assign(this, { v, time: 0, swim: 0, fast: 0, shown: null, head: 0, facing: 1, pv: null,
      hands: [0, 1].map(() => ({ o: { x: 0, y: 0 }, ov: { x: 0, y: 0 }, grip: null, aim: null, w: 0, last: null })),
      hold: { o: { x: 0, y: 0 }, ov: { x: 0, y: 0 } }, holdA: null,
      jetW: 0, rcsMs: 999, leadW: 0, leadDir: { x: 1, y: 0 }, stroke: null, sinceStroke: 99999, keysWere: false,
      streamW: 0, pushMs: 9999, bumpMs: 9999, released: null, releasedAt: -9999, hasTargets: null });
  }
  setVariant(v) { this.v = v; for (const hd of this.hands) { hd.grip = hd.aim = null; hd.w = 0; hd.o = { x: 0, y: 0 }; hd.ov = { x: 0, y: 0 }; } this.stroke = null; }
  frameM(feet, f, s) { return new DOMMatrix().translate(feet.x, feet.y).scale(f, 1).translate(0, -RIG.H / 2 - s.lift).rotate(s.tilt * 180 / Math.PI).translate(0, RIG.H / 2); }

  // once a controller step: what the body's change of speed does to loose hands, and the step-born effects
  onStep(run, h, inp) {
    const v = this.v;
    if (this.pv) {
      const dvx = (h.vx - this.pv.x) * PXS * h.facing, dvy = (h.vy - this.pv.y) * PXS;
      if (v.spring) {
        this.hands.forEach((hd, i) => { const g = SPRING[run.item === 'axe' && i === 0 ? 'axe' : i].gain; hd.ov.x -= dvx * g; hd.ov.y -= dvy * g; });
        this.hold.ov.x -= dvx * SPRING.hold.gain; this.hold.ov.y -= dvy * SPRING.hold.gain;
      }
    }
    this.pv = { x: h.vx, y: h.vy };
    if (h.rcs) this.rcsMs = 0;
    if (h.bumped) this.bumpMs = 0;
    if (h.pushed) {
      this.pushMs = 0;
      const mx = h.x / U * CELL + h.pushed.x * RIG.W * 0.5, my = (h.y - MID) / U * CELL + h.pushed.y * RIG.H * 0.5;
      run.parts.add({ kind: 'ring', x: mx, y: my, r0: 2, r1: 11, life: 320, col: LIGHT, a: 0.45, sx: h.pushed.x ? 0.25 : 1, sy: h.pushed.x ? 1 : 0.25 });
      for (let i = 0; i < 6; i++) run.parts.add({ kind: 'mote', x: mx, y: my, vx: -h.pushed.x * rnd(5, 25) + rnd(-15, 15), vy: -h.pushed.y * rnd(5, 25) + rnd(-15, 15), life: rnd(400, 800), col: DUST, a: 0.6, drag: 1.5 });
      if (v.reach) this.plant(run, h);
    }
    if (h.rcs && this.nozzle && Math.random() < 0.35) {
      const sp = Math.hypot(h.rcs.x, h.rcs.y) || 1, ux = h.rcs.x / sp, uy = h.rcs.y / sp, side = Math.random() < 0.5 ? -1 : 1;
      run.parts.add({ kind: 'streak', x: this.nozzle.x + side * 2, y: this.nozzle.y - 6, vx: ux * rnd(60, 110) + side * 20, vy: uy * rnd(60, 110), len: rnd(3, 5), life: rnd(120, 200), col: LIGHT, a: 0.75 });
    }
    // G: one stroke as the keys go down, then no more than one every STROKE.every
    const moving = !!(inp.x || inp.y) && !(inp.jet && inp.sprint);
    if (v.stroke && moving && !this.stroke && (!this.keysWere || this.sinceStroke > STROKE.every)) {
      const l = Math.hypot(inp.x, inp.y); this.stroke = { t: 0, dir: { x: Math.abs(inp.x) / l, y: inp.y / l } }; this.sinceStroke = 0;
    }
    this.keysWere = moving;
  }
  // C: the push is the arms': both free hands set on the wall beside each other
  plant(run, h) {
    const S = this.shoulderW; if (!S) return;
    let q = nearestSurface(run.grid, S, S, ARM * 1.2, h.facing); if (!q) return;
    if (this.toL && this.toL(q).x < -1) q = this.behind(run, h.facing) || q;
    const tx = -h.pushed.y, ty = h.pushed.x;
    this.free(run.item).forEach((i, k) => { const off = (k ? -1 : 1) * 2.5; this.hands[i].grip = { x: q.x + tx * off, y: q.y + ty * off }; this.hands[i].aim = null; });
  }
  // a wall at the back is held at the hip, the arms swung back as at the end of a stride, not reached for behind the pack
  behind(run, f) {
    const hip = this.toW({ x: -2, y: 8 }), q = nearestSurface(run.grid, this.shoulderW, hip, ARM * 0.9, f);
    return q && Math.hypot(q.x - this.shoulderW.x, q.y - this.shoulderW.y) <= ARM * 0.97 ? q : null;
  }
  free(item) { return item === 'drill' ? [] : item === 'axe' ? [1] : [0, 1]; }

  update(run, h, dt) {
    const v = this.v, item = run.item;
    this.time += dt; this.rcsMs += dt; this.pushMs += dt; this.bumpMs += dt; this.sinceStroke += dt;
    this.swim = settle(this.swim, h.vx || h.vy ? 1 : 0, dt, 320);
    this.fast = settle(this.fast, Math.abs(h.vx) > SPEC.swim || Math.abs(h.vy) > SPEC.swim ? 1 : 0, dt, 320);
    const base = floating(h, this.time, this.swim, this.fast);
    if (item === 'axe') {   // Pose.cs: wielding, that arm keeps calm rather than paddling
      const wf = k => B(-0.05 + 0.12 * Math.sin(this.time / k * TAU), 0.35);
      base.fa = mixB(B(0.15 + 0.06 * Math.sin(this.time / 3400 * TAU), 0.35), mixB(wf(1100), wf(700), this.fast), this.swim);
    }
    const from = this.shown || base;
    const sh = { fl: settleB(from.fl, base.fl, dt, 110), bl: settleB(from.bl, base.bl, dt, 110), tilt: settleA(from.tilt, base.tilt, dt, 450), lift: settle(from.lift, base.lift, dt, 30) };
    const feet = { x: h.x / U * CELL, y: h.y / U * CELL }, f = h.facing;
    const M = this.frameM(feet, f, sh).translate(0, -RIG.Shoulder);
    this.shoulderW = M.transformPoint({ x: 0, y: 0 });
    let fa, ba, plain = v.base === 'now' && !v.spring && !v.reach && !v.jet && !v.lead && !v.stroke;
    if (plain) { fa = settleB(from.fa, base.fa, dt, 110); ba = settleB(from.ba, base.ba, dt, 110); }
    else {
      const t = this.targets(run, h, dt, base, M);
      fa = settleB(from.fa, reach(t[0], RIG.Arm, 1), dt, 25); ba = settleB(from.ba, reach(t[1], RIG.Arm, 1), dt, 25);
      this.hasTargets = t;
    }
    if (item === 'drill') {
      let a = TW.rest, po = { x: 0, y: 0 };
      if (v.spring) { this.spring(this.hold, SPRING.hold, dt); po = this.hold.o; a += this.hold.o.y * 0.05 - this.hold.o.x * 0.02; }
      if (v.lead && this.leadDir.x > -0.2) a = mixA(a, clamp(Math.atan2(this.leadDir.y, Math.max(0.2, this.leadDir.x)), -40 * DEG, 85 * DEG), smooth(this.leadW));
      this.holdA = this.holdA == null ? a : settleA(this.holdA, a, dt, 40);
      this.drill = { a: this.holdA, po };
      fa = reach(twoAt(this.holdA, DRILL.grip, po), TOFIST, 1); ba = reach(twoAt(this.holdA, DRILL.fore, po), TOFIST, 1);
    } else this.drill = null;
    let look = 0;
    if (h.thrust) look = clamp(Math.atan2(h.thrust.y, Math.abs(h.thrust.x) + 0.15), -0.6, 0.5);
    this.head = settle(this.head, look, dt, 90);
    this.shown = { fa, ba, ...sh };
    this.facing = f; this.feet = feet; this.h = h;
  }
  spring(s, p, dt) {
    const w = TAU * p.hz, c = 2 * p.zeta * w;
    for (let left = dt / 1000; left > 1e-6;) {
      const d = Math.min(0.004, left); left -= d;
      s.ov.x += (-w * w * s.o.x - c * s.ov.x) * d; s.ov.y += (-w * w * s.o.y - c * s.ov.y) * d;
      s.o.x += s.ov.x * d; s.o.y += s.ov.y * d;
      const l = len(s.o), most = p.most || 7.5; if (l > most) { s.o.x *= most / l; s.o.y *= most / l; }
    }
  }
  // the hands' points for this frame, layer by layer
  targets(run, h, dt, base, M) {
    const v = this.v, item = run.item, f = h.facing, inv = M.inverse();
    const toL = p => inv.transformPoint(p);
    this.toL = toL; this.toW = p => M.transformPoint(p);
    const s1 = Math.sin(this.time / 3400 * TAU), s2 = Math.sin(this.time / 4700 * TAU + 1), s3 = Math.sin(this.time / 5900 * TAU + 2);
    let t;
    if (v.base === 'stand') { const br = Math.sin(this.time / 2600 * TAU); t = [endOf(B(0.05 * br + 0.04 * s1, 0.18 + 0.05 * s2), RIG.Arm), endOf(B(-0.05 * br - 0.03 * s3, 0.22 + 0.04 * s1), RIG.Arm)]; }
    else if (v.base === 'now') t = [endOf(base.fa, RIG.Arm), endOf(base.ba, RIG.Arm)];
    else t = [endOf(B(NBP.f[0] + 0.05 * s1, NBP.f[1] + 0.07 * s2), RIG.Arm), endOf(B(NBP.b[0] + 0.05 * s3, NBP.b[1] + 0.06 * s1), RIG.Arm)];
    // the axe hand floats lower, so the head stands out in front of the visor, not across it
    if (item === 'axe' && v.base === 'nbp') t[0] = endOf(B(0.3 + 0.04 * s1, 0.5 + 0.05 * s2), RIG.Arm);
    const free = this.free(item);
    const vb = { x: h.vx * PXS * f, y: h.vy * PXS }, sp = len(vb), swimPx = Z.swim * PXS;
    const neutral = t.map(p => ({ ...p }));
    // G: one stroke, both hands together, from where it goes to behind
    if (v.stroke && this.stroke) {
      const st = this.stroke; st.t += dt;
      for (const i of free) {
        const k = clamp((st.t - i * 60) / STROKE.ms, 0, 1), d = st.dir, p = { x: d.y, y: -d.x };
        const start = clearVisor({ x: d.x * 10.5 + p.x * 2, y: Math.max(-5.5, d.y * 10.5 + p.y * 2) }), end = { x: -d.x * 7 + p.x * -1, y: -d.y * 7 + 3 };
        let q;
        if (k < 0.3) q = lerpP(neutral[i], start, smooth(k / 0.3));
        else if (k < 0.65) { const u = smooth((k - 0.3) / 0.35); q = lerpP(start, end, u); q.x -= p.x * 3 * Math.sin(u * Math.PI); q.y -= p.y * 3 * Math.sin(u * Math.PI); }
        else q = lerpP(end, neutral[i], smooth((k - 0.65) / 0.35));
        t[i] = clearVisor(q);
      }
      if (st.t > STROKE.ms + 60) this.stroke = null;
    }
    // F: the leading hand out where the body goes
    if (v.lead) {
      if (sp > 6) { const dx = settle(this.leadDir.x, vb.x / sp, dt, 120), dy = settle(this.leadDir.y, vb.y / sp, dt, 120), l = Math.hypot(dx, dy) || 1; this.leadDir = { x: dx / l, y: dy / l }; }
      this.leadW = settle(this.leadW, clamp((sp - 6) / 22, 0, 1), dt, 200);
      const d = this.leadDir, i = d.x < -0.35 && free.includes(1) ? 1 : free.includes(0) ? 0 : free[0];
      if (i != null) {
        // pointed up no steeper than 30°, so the arm never crosses the visor
        const k = smooth(this.leadW), back = i === 1 && d.x < -0.35, th = clamp(Math.atan2(d.y, back ? -d.x : d.x), -30 * DEG, 80 * DEG);
        t[i] = lerpP(t[i], { x: (back ? -1 : 1) * Math.cos(th) * ARM * 1.05, y: Math.sin(th) * ARM * 1.05 - 1 }, k);
        for (const j of free) if (j !== i) t[j] = lerpP(t[j], { x: t[j].x - d.x * 4.5, y: t[j].y - d.y * 3 + 2 }, k);
      }
    }
    // D: thrusting, the hands on the chest's straps and controls; braking by its gas, nearly
    if (v.jet) {
      const want = h.thrust ? 1 : this.rcsMs < 120 ? 0.45 : 0;
      this.jetW = settle(this.jetW, want, dt, want > this.jetW ? 90 : 240);
      for (const i of free) t[i] = lerpP(t[i], CHEST[i], smooth(this.jetW));
    }
    // B: loose hands lag the body's changes of speed
    if (v.spring) this.hands.forEach((hd, i) => { this.spring(hd, SPRING[item === 'axe' && i === 0 ? 'axe' : i], dt); const k = 1 - 0.8 * (free.includes(i) ? this.jetW : 0); if (item !== 'drill') t[i] = { x: t[i].x + hd.o.x * k, y: t[i].y + hd.o.y * k }; });
    // C: hands for whatever is in reach
    if (v.reach && free.length) {
      const S = M.transformPoint({ x: 0, y: 0 }), spW = Math.hypot(h.vx, h.vy) * PXS;
      const dirW = spW > 1 ? { x: h.vx * PXS / spW, y: h.vy * PXS / spW } : null;
      // a rung is held in front of the body, wherever the body is along the ladder
      for (const hd of this.hands) for (const g of [hd.grip, hd.last]) if (g && g.rung) g.x = S.x + f * RIG.W * 0.6;
      for (const i of free) { const hd = this.hands[i]; if (hd.grip && len(toL(hd.grip)) > ARM * 1.03) { hd.grip = null; this.released = i; this.releasedAt = this.time; } }
      const gliding = h.glide && this.pushMs < 450 && spW > swimPx + 4;
      let cushion = dirW && spW > swimPx + 14 ? rayHit(run.grid, S, dirW, ARM * 2.6) : null;
      if (cushion && cushion.d / spW * 1000 > 260) cushion = null;
      for (const i of free) if (!this.hands[i].grip) this.hands[i].aim = null;
      if (cushion) {
        free.forEach((i, k) => {
          const hd = this.hands[i]; if (hd.grip) return;
          const off = (k ? -1 : 1) * 2.5, p = { x: cushion.x - dirW.y * off, y: cushion.y + dirW.x * off };
          if (Math.hypot(p.x - S.x, p.y - S.y) <= ARM * 0.97) hd.grip = p; else hd.aim = p;
        });
      } else if (!gliding && !free.some(i => this.hands[i].grip)) {
        const k = dirW ? clamp(spW / swimPx, 0, 1) : 0, ahead = dirW ? { x: S.x + dirW.x * ARM * 0.45 * k, y: S.y + dirW.y * ARM * 0.45 * k } : S;
        let q = nearestSurface(run.grid, S, ahead, ARM * 1.3, f);
        if (q && toL(q).x < -1) q = this.behind(run, f);
        if (q) {
          const ql = toL(q);
          let i = ql.x >= -1 ? 0 : 1;
          if (ql.x >= -1 && this.released != null && this.time - this.releasedAt < 500) i = 1 - this.released;
          if (!free.includes(i)) i = free[0];
          const hd = this.hands[i];
          if (len(ql) <= ARM * 0.97) hd.grip = q; else hd.aim = q;
        }
      }
      // after a push, until the glide fades, the arms stay swept back toward the wall they pushed
      const kS = h.glide && spW > swimPx + 4 ? clamp((spW - swimPx) / (Z.pushOff * PXS - swimPx), 0, 1) : 0;
      this.streamW = settle(this.streamW, this.pushMs > 40 ? Math.sqrt(kS) : 0, dt, this.pushMs < 400 ? 90 : 260);
      for (const i of free) {
        const hd = this.hands[i], goal = hd.grip || hd.aim;
        if (!goal && this.streamW > 0.01 && sp > 1) t[i] = lerpP(t[i], { x: -vb.x / sp * ARM * 0.9 + (i ? -1 : 1), y: -vb.y / sp * ARM * 0.9 + 3 + i }, smooth(this.streamW));
        hd.w = settle(hd.w, goal ? 1 : 0, dt, goal ? (cushion ? 60 : hd.grip && this.pushMs < 60 ? 30 : 130) : 220);
        if (goal) hd.last = goal;
        if (hd.w > 0.001 && hd.last) t[i] = lerpP(t[i], toL(hd.last), smooth(hd.w));
      }
    }
    // never so near the shoulder that the elbow would turn up and back
    return t.map(p => { let q = p; if (q.x < 1 && q.y < 0) q = { x: 1, y: q.y }; const l = len(q); return l < 5.5 ? { x: q.x / (l || 1) * 5.5, y: q.y / (l || 1) * 5.5 } : q; });
  }
  draw(ctx, cam, debug) {
    const s = this.shown; if (!s) return;
    const f = this.facing;
    const M = this.frameM(this.feet, f, s);
    const put = (m, img, j) => { ctx.setTransform(cam.multiply(m)); ctx.drawImage(img, -j[0], -j[1]); };
    const deg = r => r * 180 / Math.PI;
    const limb = (m, b, up, lo, far, arm) => {
      const mu = m.rotate(-deg(b.u)), seg = arm ? RIG.Arm : RIG.Leg, ml = mu.translate(0, seg.u).rotate(-deg(b.l));
      put(ml, IMG[far ? 'far' : 'near'][lo], JOINTS[lo]); put(mu, IMG[far ? 'far' : 'near'][up], JOINTS[up]);
    };
    const SH = M.translate(0, -RIG.Shoulder);
    limb(SH, s.ba, 'upperArm', 'forearm', true, true);
    limb(M.translate(-RIG.HipApart, -RIG.Hip), s.bl, 'thigh', 'shin', true, false);
    limb(M.translate(RIG.HipApart, -RIG.Hip), s.fl, 'thigh', 'shin', false, false);
    const packM = M.translate(-RIG.Back, -RIG.Shoulder - RIG.H * 0.02);
    put(packM, IMG.near.pack, JOINTS.pack);
    if (this.jet) { const jm = packM.translate(-2, -1); put(jm, IMG.jetpack, [0, 0]); this.nozzle = jm.transformPoint({ x: 3, y: 14 }); } else this.nozzle = null;
    put(M.translate(0, -RIG.Neck), IMG.near.torso, JOINTS.torso);
    put(M.translate(0, -RIG.Neck).rotate(deg(this.head)), IMG.near.helmet, JOINTS.helmet);
    if (this.drill) { const p = pivotAt(this.drill.a); put(SH.translate(p.x + this.drill.po.x, p.y + this.drill.po.y).rotate(deg(this.drill.a)), IMG.drill, DRILL.mount); }
    else if (this.axe) { const fi = fistOf(s.fa), tr = s.fa.u + s.fa.l; put(SH.translate(fi.x, fi.y).rotate(deg(Math.atan2(Math.cos(tr), Math.sin(tr)) - AXE.hold)), IMG.axe, AXE.grip); }
    limb(SH, s.fa, 'upperArm', 'forearm', false, true);
    if (this.nozzle && this.h && this.h.thrust) {
      const t = this.h.thrust, n = this.nozzle, l = rnd(5, 8);
      ctx.setTransform(cam); ctx.lineCap = 'round';
      ctx.strokeStyle = THRUST; ctx.globalAlpha = 0.85; ctx.lineWidth = 1.6; ctx.beginPath(); ctx.moveTo(n.x, n.y); ctx.lineTo(n.x - t.x * l, n.y - t.y * l); ctx.stroke();
      ctx.strokeStyle = '#ffffff'; ctx.globalAlpha = 1; ctx.lineWidth = 0.8; ctx.beginPath(); ctx.moveTo(n.x, n.y); ctx.lineTo(n.x - t.x * l * 0.5, n.y - t.y * l * 0.5); ctx.stroke();
      ctx.globalAlpha = 1; ctx.lineCap = 'butt';
    }
    if (debug && this.hasTargets) {
      ctx.setTransform(cam);
      this.hasTargets.forEach((p, i) => { const w = SH.transformPoint(p); ctx.fillStyle = i ? '#ff6' : '#f6f'; ctx.fillRect(w.x - 0.5, w.y - 0.5, 1, 1); });
      for (const hd of this.hands) if (hd.grip) { ctx.fillStyle = '#6f6'; ctx.fillRect(hd.grip.x - 0.5, hd.grip.y - 0.5, 1, 1); }
    }
  }
}

// ---------- one hero in one airless room ----------
class ZRun {
  constructor(cfg) { this.cfg = cfg; this.reset(); }
  get item() { return this.cfg.item || null; }
  reset() {
    const c = this.cfg; this.grid = new Grid(MAPS['z_' + c.map]);
    this.hero = { x: Math.round(c.at[0] * U), y: Math.round(c.at[1] * U), vx: 0, vy: 0, dx: 0, dy: 0, facing: c.face || 1 };
    this.prev = this.hero; this.fig = new ZFigure(c.variant); this.parts = new Parts(); this.t = 0; this.acc = 0; this.cam = null;
  }
  step(inp) {
    this.prev = this.hero;
    const h = stepZ(this.grid, this.hero, inp, !!this.cfg.old);
    this.hero = h; this.fig.jet = !!inp.jet; this.fig.axe = this.item === 'axe';
    this.fig.onStep(this, h, inp);
    if (h.thrust) { const n = this.fig.nozzle; if (n) for (let i = 0; i < 2; i++) this.parts.add({ kind: 'streak', x: n.x, y: n.y, vx: -h.thrust.x * rnd(70, 130) + rnd(-12, 12), vy: -h.thrust.y * rnd(70, 130) + rnd(-12, 12), len: rnd(3, 6), life: rnd(140, 260), col: i ? '#ffffff' : THRUST, a: 0.8 }); }
    this.t++;
  }
  frame(dt, scale, input) {
    this.acc += dt * scale;
    let n = 0;
    while (this.acc >= STEP_MS && n < 8) {
      this.acc -= STEP_MS; n++;
      if (!input && this.cfg.state && this.t >= this.cfg.state.len) this.reset();
      this.step(input ? input(this) : keysAt(this.cfg.state, this.t));
    }
    const a = this.acc / STEP_MS, h = this.hero, p = this.prev;
    this.drawn = Math.abs(h.x - p.x) > 3000 || Math.abs(h.y - p.y) > 3000 ? h : { ...h, x: p.x + (h.x - p.x) * a, y: p.y + (h.y - p.y) * a };
    this.fig.update(this, this.drawn, dt * scale);
    this.parts.step(dt * scale);
  }
  draw(ctx, cw, ch, scale, debug) {
    ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.imageSmoothingEnabled = false;
    ctx.fillStyle = '#06080c'; ctx.fillRect(0, 0, cw, ch);
    const m = this.grid.m, W = m.w * CELL, H = m.h * CELL, vw = cw / scale, vh = ch / scale, h = this.drawn || this.hero;
    const hx = h.x / U * CELL, hy = (h.y - MID) / U * CELL;
    const tx = W <= vw ? W / 2 : clamp(hx, vw / 2, W - vw / 2), ty = H <= vh ? H / 2 : clamp(hy, vh / 2, H - vh / 2);
    if (!this.cam) this.cam = { x: tx, y: ty }; else { this.cam.x += (tx - this.cam.x) * 0.2; this.cam.y += (ty - this.cam.y) * 0.2; }
    const cx = Math.round(cw / 2 - this.cam.x * scale), cy = Math.round(ch / 2 - this.cam.y * scale);
    const cam = new DOMMatrix([scale, 0, 0, scale, cx, cy]);
    ctx.setTransform(cam); ctx.drawImage(zScene(this.cfg.map), 0, 0);
    this.fig.draw(ctx, cam, debug);
    ctx.setTransform(cam); this.parts.draw(ctx, 1 / scale);
    ctx.setTransform(1, 0, 0, 1, 0, 0);
  }
}

// ---------- the rooms' tiles, drawn once ----------
const ZSCENE = {};
function zScene(id) {
  if (ZSCENE[id]) return ZSCENE[id];
  const m = MAPS['z_' + id], c = document.createElement('canvas'); c.width = m.w * CELL; c.height = m.h * CELL;
  const g = c.getContext('2d'); g.imageSmoothingEnabled = false;
  const ch = (x, y) => x < 0 || y < 0 || x >= m.w || y >= m.h ? '#' : m.rows[y][x];
  let s = 11;
  const r = () => (s = (s * 9301 + 49297) % 233280) / 233280;
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) {
    const k = ch(x, y), X = x * CELL, Y = y * CELL;
    if (k === '#') continue;
    if (k === ' ') { g.fillStyle = '#020306'; g.fillRect(X, Y, CELL, CELL); for (let i = 0; i < 3; i++) { g.fillStyle = '#fff'; g.globalAlpha = 0.3 + r() * 0.6; g.fillRect(X + Math.floor(r() * 16), Y + Math.floor(r() * 16), 1, 1); } g.globalAlpha = 1; continue; }
    g.drawImage(IMG.tile.back, X, Y);
    if (k === 'h') g.drawImage(IMG.tile.ladder, X, Y);
  }
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) {
    if (ch(x, y) !== '#') continue;
    const X = x * CELL, Y = y * CELL, tw = IMG.tile.wall, n = Math.max(1, tw.width / CELL);
    g.drawImage(tw, (x % n) * CELL, (y % n) * CELL, CELL, CELL, X, Y, CELL, CELL);
    const open = (ch(x, y - 1) !== '#' ? 1 : 0) | (ch(x + 1, y) !== '#' ? 2 : 0) | (ch(x, y + 1) !== '#' ? 4 : 0) | (ch(x - 1, y) !== '#' ? 8 : 0);
    if (open) g.drawImage(IMG.edge[open], X, Y);
  }
  g.fillStyle = 'rgba(8,12,20,0.3)'; g.fillRect(0, 0, c.width, c.height);
  for (let y = 0; y < m.h; y++) for (let x = 0; x < m.w; x++) if (ch(x, y) === '#') { g.fillStyle = 'rgba(10,14,24,0.38)'; g.fillRect(x * CELL, y * CELL, CELL, CELL); }
  return ZSCENE[id] = c;
}
async function loadZArt() {
  await loadArt(0xe8b04a);
  IMG.axe = await loadImage(ART.hero_axe); IMG.drill = await loadImage(ART.hero_drill);
}
