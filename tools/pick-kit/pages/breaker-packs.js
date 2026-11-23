// «Живые разборщики»: the nine painted rigs of breaker-rigs/, each through its states and as packs in one staged fight on one
// clock. The rigs and their cuts are pasted in as they are; the fight, the hero and the companion are the page's.
'use strict';
const fs = require('fs'), path = require('path');

const RIG_DIR = path.join(__dirname, 'breaker-rigs');
const BODIES = [
  ['triad', 'Триада'], ['maw', 'Прорва'], ['patched', 'Латаный'], ['shroud', 'Саван'], ['split', 'Расщеп'],
  ['twin', 'Сросток'], ['crown', 'Венец'], ['visage', 'Лик'], ['knot', 'Клубок'],
];
const CUTS = {};
const pictures = { axe: 'game/art/hd/hero_axe.png', slab: 'game/art/hd/debris/slab_01.png', section: 'game/art/hd/debris/section_01.png' };
for (const [id] of BODIES) {
  CUTS[id] = JSON.parse(fs.readFileSync(path.join(RIG_DIR, 'cut', id, 'cuts.json'), 'utf8'));
  for (const part of Object.keys(CUTS[id])) pictures[`${id}_${part}_hd`] = `tools/pick-kit/pages/breaker-rigs/cut/${id}/${part}_hd.png`;
}
// the rigs' files fill an object of their own in place of the window
const RIG_FILES = ['rig.js', 'triad.rig.js', 'rigs.js'].map(f => fs.readFileSync(path.join(RIG_DIR, f), 'utf8').split('\r\n').join('\n')).join('\n');
const SHARED = `const RIGGED = (window => {\n${RIG_FILES}\nreturn window;\n})({});
const BODIES = ${JSON.stringify(BODIES)};
const CUTS = ${JSON.stringify(CUTS)};`;

function page() {
  const { BreakerRig, RIGS } = RIGGED;
  const IDS = BODIES.map(b => b[0]), NAME = Object.fromEntries(BODIES), FREE = { triad: true, maw: true };
  for (const id of IDS) { BreakerRig.ready(RIGS[id]); RIGS[id].cuts = CUTS[id]; }

  // ---------- the game's numbers (sim/Content/Creatures.cs, Weapons, Abilities), in world px and seconds ----------
  const PI = Math.PI, TAU = PI * 2, CELL = 8, TICKS = 20, perS = v => v / 1000 * CELL * TICKS;
  const FPS = 60, LOOP = 14, HP = 4, DAMAGE = 2;
  const SPEED = perS(700), ACCEL = perS(150) * TICKS, POUNCE = perS(1500), DART = 6 / TICKS, WINDUP = 12 / TICKS;
  const KNOCK = perS(1200), FLUNG = 4 * CELL / KNOCK, RING = [4 * CELL + 16, 4 * CELL + 9];
  const STROKE = 10 / TICKS, HIT_AT = .35 + .12 * 2.1 / 2.4, STOP = .05, FAN = .46, REACH = 4.5 * CELL + 8;
  const RANGE = 18 * CELL, OUT = perS(3600), HOME = perS(4500), PULL = perS(600) * TICKS, CAST = .12, BLADE = 14;
  const LEAD = .815, AXE = { w: 34, h: 13, grip: [4.1, 3.85], size: .75, weight: [13.26, 3.9] };
  const ARM = RIG.Arm[0] + RIG.Arm[1] - .01, MID = 21, REST = 1.1, DIE = 1.7, DEAD = 1.6;

  const sub = (a, b) => [a[0] - b[0], a[1] - b[1]], mul = (a, k) => [a[0] * k, a[1] * k], len = a => Math.hypot(a[0], a[1]);
  const unit = a => { const l = len(a); return l ? [a[0] / l, a[1] / l] : [1, 0]; };
  const dirOf = a => [Math.cos(a), Math.sin(a)], angOf = v => Math.atan2(v[1], v[0]);
  const ease = (t, from, dur) => sm((t - from) / dur), pulse = (t, from, rise, till, fall) => ease(t, from, rise) * (1 - ease(t, till, fall));
  const swung = (a, b) => { let d = (b - a) % TAU; if (d > PI) d -= TAU; if (d < -PI) d += TAU; return d; };
  const follow = (v, to, rate, dt) => v + (to - v) * (1 - Math.exp(-dt * rate));
  function keyed(keys, t) {
    if (t <= keys[0][0]) return keys[0][1];
    for (let i = 1; i < keys.length; i++) if (t <= keys[i][0]) return lerp(keys[i - 1][1], keys[i][1], sm((t - keys[i - 1][0]) / (keys[i][0] - keys[i - 1][0])));
    return keys[keys.length - 1][1];
  }
  function offSegment(p, a, b) { const d = sub(b, a), u = c01(((p[0] - a[0]) * d[0] + (p[1] - a[1]) * d[1]) / (d[0] * d[0] + d[1] * d[1] || 1)); return len(sub(p, lerp2(a, b, u))); }
  const hurtAt = u => (u < 0 ? 0 : sm(u / .07) * Math.exp(-3.5 * Math.max(0, u - .07)));
  const deadAt = u => (u <= 0 ? 0 : DEAD * (1 - (1 - c01(u / DIE)) ** 1.4));

  // ---------- the hero's arm, shared by the fight and by a body's own tile ----------
  function swingArm(swings, t) {
    for (const s of swings) {
      let u = (t - s.at) / STROKE;
      if (u < 0 || u >= 1) continue;
      const aim = Math.atan2(Math.sin(s.aim), Math.abs(Math.cos(s.aim))) + LEAD, stop = STOP / STROKE;
      if (s.struck && u > HIT_AT) u = u <= HIT_AT + stop ? HIT_AT : HIT_AT + (u - HIT_AT - stop) * (1 - HIT_AT) / (1 - HIT_AT - stop);
      if (u < .35) return lerp(REST, aim - 2.1, sm(u / .35));
      if (u < .47) return aim - 2.1 + 2.4 * (u - .35) / .12;
      return lerp(aim + .3, REST, sm((u - .62) / .38));
    }
    return null;
  }
  const figure = { suit: 'me', x: 0, y: 0, face: 1, ph: 0, amp: 0, air: true, look: 0, hl: 0, e: 0, kn: 0, rk: 1, lean: 0 };
  function handOf(hero, theta) {
    Object.assign(figure, { x: 0, y: 0, face: hero.face, lean: hero.lean, look: hero.look, rk: 0 });
    figure.rp = add(poseOf(figure).sh, mul(dirOf(theta), ARM)); figure.rk = 1;
    const P = poseOf(figure), fist = P.armF[2];
    return { rp: figure.rp, grip: [hero.x + hero.face * fist[0], hero.y + fist[1]], turn: angOf(sub(fist, P.armF[1])) - PI / 2 };
  }

  // ---------- the fight's staging ----------
  const HERO_Y = 102, PIECE = { key: 'slab', w: 64, h: 32, at: [208, 50] };
  // where one sleeps on the piece, in waking order: the spot, the way out of the piece, its lean, the way it faces
  const NEST = [
    [172, 51, -PI / 2, -.2, -1], [187, 72, PI, 0, -1], [187, 28, 0, 0, -1], [208, 73, PI, 0, 1],
    [208, 27, 0, 0, 1], [230, 72, PI, -.12, 1], [229, 31, .5, .36, 1], [246, 56, PI / 2, .2, 1],
  ];
  const WAKE = { at: 1.8, each: .17 }, REGROUP = 8.6, THROW = { raise: 10.1, go: 10.35 }, FINISH = 11.25;
  const HERO_X = [[0, 62], [2.6, 62], [4.5, 96], [8.5, 96], [9.9, 42], [12.7, 42], [13.8, 62], [LOOP, 62]];
  // where one winds up from the hero's middle and when; held: how long it grips, 0 when a blow cuts it
  const ACTS = [
    { at: 4.8, from: 3.8, post: [46, -6], hook: [16, -4], held: .55 },
    { at: 6.3, from: 5.2, post: [-46, -6], hook: [-16, -4], held: .5 },
    { at: 7.8, from: 6.8, post: [35, -6], held: 0 },
    { at: 11.75, from: FINISH, post: [34, -10], held: 0, again: 2 },
  ];
  const SWINGS = [{ at: 8, act: 2 }, { at: 11.98, act: 3 }];
  const SAYS = [
    [0, 'Гнездо спит на обломке'], [1.8, 'Просыпаются один за другим'], [3.5, 'Окружают героя'], [4.8, 'Замах'], [5.4, 'Рывок и хватка'],
    [6.3, 'Следующий заходит с другой стороны'], [7.8, 'Снова замах'], [8.22, 'Удар сбивает замах: отлёт на четыре клетки'],
    [9.1, 'Стая заходит снова'], [10.1, 'Бросок: топор летит сквозь всех'], [10.85, 'Спутник тянет топор нитью обратно в руку'],
    [11.3, 'Распадаются на детали'], [12.2, 'Второй удар добивает'], [13.6, 'Дальше следующее гнездо'],
  ];

  function staged(N) {
    const dt = 1 / FPS, frames = [], hits = [], middle = h => [h.x, h.y - MID];
    const bodies = NEST.slice(0, N).map(([x, y, out, lean, face], i) => ({
      i, p: [x, y], v: [0, 0], own: [0, 0], a: lean, lean, roll: out, k: face, want: face, hp: HP, wake: WAKE.at + i * WAKE.each,
      act: null, acts: [], hurts: [], dead: null, knock: null, line: null,
    }));
    const acts = ACTS.map(a => ({ ...a, who: null, go: a.at + WINDUP, landed: null, off: null, cut: null }));
    const swings = SWINGS.map(s => ({ ...s, aim: 0, struck: false, done: false, who: [] }));
    const axe = { leg: null, caught: null, out: [], home: [] };
    const flings = [];
    const hero = { x: HERO_X[0][1], y: HERO_Y, face: 1, side: 1, lean: 0, look: null };
    let turnWas = null;

    function strike(b, dir, t, by) {
      b.hp -= DAMAGE; b.hurts.push(t);
      for (const a of b.acts) if (a.off === null && t >= a.at) { a.off = t; if (t < a.go) a.cut = t; }
      b.act = null;
      const dies = b.hp <= 0;
      if (dies) b.dead = t;
      b.knock = { t, dir, v: dies ? KNOCK / 2 : KNOCK };
      flings.push({ i: b.i, t, by, from: b.p.slice(), dies });
    }
    function chooseActs(t) {
      for (const a of acts) {
        if (a.who !== null || t < a.from) continue;
        const to = add(middle(hero), a.post);
        let best = null;
        for (const b of bodies) {
          if (b.dead !== null || t < b.wake + .35 || (a.again === undefined ? b.acts.length : acts[a.again].who !== b.i)) continue;
          if (!best || len(sub(b.p, to)) < len(sub(best.p, to))) best = b;
        }
        if (best) { a.who = best.i; best.act = a; best.acts.push(a); }
      }
    }
    function lineUp() {
      const line = bodies.filter(b => b !== bodies[acts[2].who]).sort((a, b) => a.p[0] - b.p[0]);
      line.forEach((b, j) => { b.line = [lerp(78, 160, line.length > 1 ? j / (line.length - 1) : 0), j % 2 ? 8.5 : -8.5]; });
    }
    function targetOf(b, t) {
      const M = middle(hero), a = b.act;
      if (a) return round(b.p, M, add(M, mul(a.post, a.held ? 1 + .17 * ease(t, a.at, WINDUP) : 1)));
      // let go, it backs off the way it came
      const done = b.acts[0];
      if (done && done.held && done.off !== null && t - done.off < .55) return add(M, mul(done.hook, 3.2));
      if (t >= REGROUP && b.line) {
        // one behind the hero comes round over or under him, never through
        if (b.p[0] < hero.x + 18) return [hero.x + 34, b.p[1] < M[1] ? M[1] - 46 : M[1] + 44];
        return [b.line[0], HERO_Y - RIG.Shoulder + 1 + b.line[1] + 1.2 * Math.sin(t * 2.3 + b.i)];
      }
      if (t >= REGROUP) return [84, 34 + 1.5 * Math.sin(t * 2.1)];
      // the ring opens on the side of the one winding up
      const w = t * .8 + b.i * TAU / N, side = Math.cos(w);
      const right = 28 * Math.max(pulse(t, 3.9, .6, 5.5, .6), pulse(t, 6.7, .6, 8.3, .6)), left = 22 * pulse(t, 5.3, .6, 7, .6), room = (side > 0 ? right : left) * side * side;
      return round(b.p, M, [M[0] + (RING[0] + room) * Math.cos(w), M[1] + RING[1] * Math.sin(w)]);
    }
    // a way to the hero's other side goes along the ring, never through them
    function round(from, M, to) {
      const around = p => Math.atan2((p[1] - M[1]) / RING[1], (p[0] - M[0]) / RING[0]), here = around(from), turn = swung(here, around(to));
      if (len(sub(from, M)) > 100 || Math.abs(turn) < 1.1) return to;
      const w = here + Math.sign(turn) * .9;
      return [M[0] + (RING[0] + 6) * Math.cos(w), M[1] + (RING[1] + 4) * Math.sin(w)];
    }
    function moveBody(b, t) {
      const was = b.p, M = middle(hero), a = b.act, K = b.knock;
      if (t < b.wake + .35) { b.v = [0, 0]; return; }
      if (K && (b.dead !== null || t - K.t < .8)) {
        const u = t - K.t, d = K.v * FLUNG * (Math.exp(-Math.max(0, u - dt) / FLUNG) - Math.exp(-u / FLUNG));
        b.p = add(b.p, mul(K.dir, d)); b.v = mul(sub(b.p, was), FPS);
        return;
      }
      if (a && a.held && t >= a.go) {
        const to = add(M, a.hook);
        if (!a.dart) a.dart = { from: b.p.slice(), dur: Math.min(DART, Math.max(.14, 1.5 * len(sub(b.p, to)) / POUNCE)) };
        const u = (t - a.go) / a.dart.dur;
        b.p = lerp2(a.dart.from, to, sm(u)); b.v = mul(sub(b.p, was), FPS);
        if (u >= 1 && a.landed === null) { a.landed = t; hits.push({ t, dir: -Math.sign(a.hook[0]) }); }
        if (a.landed !== null && t >= a.landed + a.held) { a.off = t; b.act = null; }
        return;
      }
      const to = targetOf(b, t), d = sub(to, b.p), far = len(d);
      const want = mul(unit(d), Math.min(SPEED, Math.sqrt(1.4 * ACCEL * far), far * 6));
      const change = sub(want, b.v), most = ACCEL * dt;
      b.v = add(b.v, len(change) > most ? mul(unit(change), most) : change);
      b.p = add(b.p, mul(b.v, dt));
      // room between two, and round the hero
      if (!a) for (const o of bodies) {
        if (o === b || o.dead !== null) continue;
        const q = sub(b.p, o.p), l = len(q);
        if (l < 26) b.p = add(b.p, mul(unit(q), (26 - l) * (o.act ? 13 : 6) * dt));
      }
      const q = sub(b.p, M), near = Math.hypot(q[0] / 27, q[1] / 34);
      if (near < 1) b.p = add(b.p, mul(unit(q), (1 - near) * 170 * dt));
    }
    function turnBody(b, t) {
      const M = middle(hero), a = b.act;
      for (let i = 0; i < 2; i++) b.own[i] = follow(b.own[i], clamp(b.v[i] * (i ? 1 : b.k) / SPEED, -1, 1), 9, dt);
      if (b.dead !== null) return;
      let lean = b.lean;
      if (t >= b.wake) {
        const d = sub(M, b.p), attacking = !!a && t >= a.at, coming = !a && t < REGROUP && len(d) > 80 && Math.abs(b.v[0]) > 25;
        const along = coming ? b.v[0] : d[0];
        if (!(b.knock && t - b.knock.t < .8) && Math.abs(along) > (coming ? 0 : 7)) b.want = along < 0 ? -1 : 1;
        lean = attacking ? b.want * clamp(Math.atan2(d[1], Math.abs(d[0])), -.4, .4) : clamp(b.v[0] * .0019, -.22, .22);
        b.roll += dt * (clamp(b.v[0] / SPEED, -1, 1) * 2 + (attacking && t < a.go ? -b.want : 0));
      }
      b.a = follow(b.a, lean, 9, dt);
      b.k += clamp(b.want - b.k, -dt * 10, dt * 10);
    }
    function stateOf(b, t) {
      let tell = 0, grip = 0, hurt = 0;
      for (const a of b.acts) {
        const till = a.cut !== null ? a.cut : a.go;
        tell = Math.max(tell, pulse(t, a.at, .4, till, .14));
        if (a.dart) grip = Math.max(grip, ease(t, a.go + a.dart.dur - .08, .16) * (a.off === null ? 1 : 1 - ease(t, a.off, .25)));
      }
      for (const at of b.hurts) hurt = Math.max(hurt, hurtAt(t - at));
      return { sleep: 1 - ease(t, b.wake, .6), tell, grip, hurt, dead: b.dead === null ? 0 : deadAt(t - b.dead), v: b.own.slice() };
    }
    function moveHero(t) {
      let push = 0, jolt = 0;
      for (const h of hits) { const u = t - h.t; push += 6 * h.dir * sm(u / .1) * (1 - ease(u, .2, 1)); jolt += h.dir * hero.face * .22 * hurtAt(u); }
      const was = hero.x;
      hero.x = keyed(HERO_X, t) + push;
      hero.y = HERO_Y + 1.2 * Math.sin(t * TAU * 3 / LOOP);
      hero.face = t >= 6.25 && t < 7.72 ? -1 : 1;
      hero.side = follow(hero.side, hero.face, 9, dt);
      hero.lean = follow(hero.lean, clamp((hero.x - was) * FPS * hero.face * .0022, -.2, .22) + jolt, 16, dt);
      let watch = PIECE.at;
      if (axe.leg) watch = axe.p;
      else if (t > 2.6) {
        const M = middle(hero);
        let far = 1e9;
        for (const b of bodies) if (b.dead === null && len(sub(b.p, M)) < far) { far = len(sub(b.p, M)); watch = b.p; }
      }
      const look = headTurn((watch[0] - hero.x) * hero.face, watch[1] - (hero.y - RIG.Neck));
      hero.look = hero.look === null ? look : follow(hero.look, look, 10, dt);
    }
    function armOf(t) {
      const s = swingArm(swings, t);
      if (s !== null) return s;
      if (t < THROW.raise) return REST;
      if (t < THROW.go) return t < THROW.go - .07 ? lerp(REST, -1.65, sm((t - THROW.raise) / .16)) : lerp(-1.65, .1, (t - THROW.go + .07) / .07);
      return axe.caught === null ? .1 : lerp(.1, REST, ease(t, axe.caught + .08, .3));
    }
    function swingBlows(t) {
      const from = [hero.x + .4 * hero.face, hero.y - RIG.Shoulder];
      for (const s of swings) {
        if (s.done) continue;
        const a = acts[s.act];
        if (a.who === null) continue;
        s.aim = angOf(sub(bodies[a.who].p, from));
        if (t < s.at + HIT_AT * STROKE) continue;
        s.done = true;
        for (const b of bodies) {
          if (b.dead !== null) continue;
          const d = sub(b.p, from);
          if (len(d) > REACH || Math.abs(swung(s.aim, angOf(d))) > FAN) continue;
          s.struck = true; s.who.push(b.i); strike(b, dirOf(s.aim), t, 'swing');
        }
      }
    }
    function stepAxe(t, hand) {
      const A = axe, weight = rot(AXE.weight[0], AXE.weight[1], hand.turn);
      if (!A.leg && A.caught === null && t >= THROW.go) Object.assign(A, { leg: 'out', p: hand.grip.slice(), v: [OUT, 0], angle: hand.turn, offHand: weight, cast: 0 });
      if (!A.leg) return { grip: hand.grip, turn: hand.turn, side: hero.face, flying: false, cast: 0 };
      const was = A.p, slow = OUT * OUT / (2 * RANGE);
      if (A.leg === 'out') {
        A.v = [Math.max(0, A.v[0] - slow * dt), 0];
        A.p = add(A.p, mul(A.v, dt));
        if (A.v[0] < slow * CAST) A.cast = Math.min(1, A.cast + dt / CAST);
        if (!A.v[0]) Object.assign(A, { leg: 'home', pull: 0, cast: 1, far: len(sub(A.p, hand.grip)) });
      } else {
        A.pull = Math.min(HOME, A.pull + PULL * dt);
        const d = sub(hand.grip, A.p);
        A.v = mul(unit(d), A.pull);
        A.p = len(d) <= Math.max(6, A.pull * dt) ? hand.grip.slice() : add(A.p, mul(A.v, dt));
      }
      const list = A.leg === 'out' ? A.out : A.home;
      if (len(A.v)) for (const b of bodies) {
        if (b.dead !== null || list.includes(b.i) || offSegment(b.p, was, A.p) > BLADE) continue;
        list.push(b.i); strike(b, unit(A.v), t, A.leg);
      }
      A.angle += 3.2 * TAU * dt * (.16 + .84 * Math.min(1, len(A.v) / 500));
      A.offHand = mul(A.offHand, Math.exp(-dt / .05));
      const landed = A.leg === 'home' ? c01(1 - len(sub(A.p, hand.grip)) / Math.min(A.far, 96)) : 0;
      const turn = angLerp(A.angle, hand.turn, sm(landed)), grip = lerp2(sub(add(A.p, A.offHand), rot(AXE.weight[0], AXE.weight[1], turn)), hand.grip, landed);
      if (A.leg === 'home' && len(sub(A.p, hand.grip)) < 1e-6) { A.leg = null; A.caught = t; return { grip: hand.grip, turn: hand.turn, side: hero.face, flying: false, cast: 0 }; }
      return { grip, turn, side: 1, flying: true, cast: A.cast };
    }

    let lined = false;
    for (let f = 0; f <= LOOP * FPS; f++) {
      const t = f / FPS;
      moveHero(t);
      chooseActs(t);
      if (!lined && t >= REGROUP) { lineUp(); lined = true; }
      for (const b of bodies) { moveBody(b, t); turnBody(b, t); }
      swingBlows(t);
      const hand = handOf(hero, armOf(t)), held = stepAxe(t, hand);
      // the axe's turn never jumps a whole round, so two frames may be blended
      if (turnWas !== null) held.turn = turnWas + swung(turnWas, held.turn);
      turnWas = held.turn;
      const home = [hero.x - 9 * hero.side, hero.y - 44 + .5 * Math.sin(t * TAU * 5 / LOOP)];
      let companion = home;
      if (held.flying && held.cast > 0) companion = add(home, mul(unit(sub(held.grip, home)), 2.5 * held.cast));
      frames.push({
        hero: { x: hero.x, y: hero.y, face: hero.face, lean: hero.lean, look: hero.look, rp: hand.rp }, axe: held, companion, home,
        bodies: bodies.map(b => ({
          x: b.p[0], y: b.p[1], a: b.a, roll: b.roll, k: b.k, s: stateOf(b, t),
          front: b.acts.some(a => t >= a.at && (a.off === null || t < a.off + .8)),
        })),
      });
    }
    return { N, frames, bodies, acts, swings, axe, flings };
  }
  const SIZES = [7, 8, 6], PASSED = [0, 7, 15], FIGHTS = Object.fromEntries(SIZES.map(n => [n, staged(n)]));

  // two worked-out frames blended, so the slow speed is as smooth as the game's
  function mix(a, b, u) {
    if (typeof a === 'number') return a + (b - a) * u;
    if (Array.isArray(a)) return a.map((v, i) => mix(v, b[i], u));
    if (a && typeof a === 'object') {
      const out = {};
      for (const k in a) out[k] = k === 'face' || k === 'side' ? a[k] : mix(a[k], b[k], u);
      return out;
    }
    return a;
  }
  function momentOf(sim, t) {
    const f = clamp(t * FPS, 0, sim.frames.length - 1), i = Math.floor(f), a = sim.frames[i], b = sim.frames[Math.min(i + 1, sim.frames.length - 1)];
    return f === i || a.hero.face !== b.hero.face ? a : mix(a, b, f - i);
  }

  // ---------- who is in a pack ----------
  const taken = () => IDS.filter(id => OPT[id] !== 'without');
  function mixedRoster(n) {
    const size = SIZES[n % 3], start = Math.floor(n / 3) * 21 + PASSED[n % 3], all = taken();
    if (!all.length) return [];
    const base = [];
    for (let i = 0; i < size; i++) base.push(IDS[(start + i) % IDS.length]);
    const out = base.map(id => (all.includes(id) ? id : null)), spare = all.filter(id => !base.includes(id));
    let again = start;
    for (let i = 0; i < size; i++) if (!out[i]) out[i] = spare.length ? spare.shift() : all[again++ % all.length];
    return out;
  }
  function kinRoster(n) {
    const all = taken();
    return all.length ? Array(SIZES[n % 3]).fill(all[n % all.length]) : [];
  }

  // ---------- one body's own loop ----------
  const LIFE = { w: 130, h: 98, hero: [104, 78], nest: [26, 54], post: [58, 55], piece: { key: 'section', w: 48, h: 32, at: [26, 78] }, blows: [8.3, 10.1], again: 12.2 };
  const STATES = [[0, 'спит'], [2, 'просыпается'], [3.1, 'летит'], [5.6, 'замах'], [5.6 + WINDUP, 'хватка'], [LIFE.blows[0], 'удар'], [LIFE.blows[1], 'распад'], [LIFE.again, 'спит']];
  const LIFE_SWINGS = LIFE.blows.map(t => ({ at: t - HIT_AT * STROKE, struck: true, aim: 0 }));
  function lifeBody(t) {
    const [hx, hy] = LIFE.hero, hook = [hx - 16, hy - MID - 4], [first, second] = LIFE.blows, dart = 5.6 + WINDUP;
    if (t >= LIFE.again) return LIFE.nest.slice();
    if (t < 5.6) return bez(LIFE.nest, [36, 8], LIFE.post, ease(t, 2.6, 3));
    if (t < dart) return [LIFE.post[0] - 7 * ease(t, 5.6, WINDUP), LIFE.post[1]];
    const pushed = [hook[0] + 4 * pulse(t, dart + .24, .1, dart + .5, .9), hook[1]];
    if (t < first) return lerp2([LIFE.post[0] - 7, LIFE.post[1]], pushed, ease(t, dart, .26));
    const flung = 4 * CELL * (1 - Math.exp(-(t - first) / FLUNG)), back = 13 * ease(Math.min(t, second), 9.2, .85);
    const last = t < second ? 0 : 15 * (1 - Math.exp(-(t - second) / FLUNG));
    return [hook[0] - flung + back - last, hook[1]];
  }
  LIFE_SWINGS.forEach((s, i) => { s.aim = angOf(sub(lifeBody(LIFE.blows[i] - 1e-4), [LIFE.hero[0] - .4, LIFE.hero[1] - RIG.Shoulder])); });
  function lifeAt(t) {
    const again = t >= LIFE.again, dart = 5.6 + WINDUP, [first, second] = LIFE.blows;
    const fly = again ? 0 : pulse(t, 2.9, .5, 5.2, .5) + .8 * pulse(t, dart, .12, dart + .2, .2);
    const s = {
      sleep: again ? 1 : 1 - ease(t, 2, 1), tell: again ? 0 : pulse(t, 5.6, .45, dart, .16), grip: again ? 0 : pulse(t, dart + .16, .16, first, .22),
      hurt: again ? 0 : Math.max(hurtAt(t - first), hurtAt(t - second)), dead: again ? 0 : deadAt(t - second), v: [fly, -.2 * fly],
    };
    const a = .15 * fly - .12 * s.tell - .15 * s.hurt, roll = again ? 0 : a + TAU / 3 * ease(t, 2.9, 2.6);
    const large = [again ? 0 : -3 * s.tell + 3 * s.grip - 5 * s.hurt, again ? 0 : -1.4 * fly * Math.sin(t * 4)];
    const alpha = again ? ease(t, LIFE.again + .1, .6) : 1, p = lifeBody(t);
    const push = again ? 0 : 4 * pulse(t, dart + .24, .1, dart + .5, .9);
    const hero = { x: LIFE.hero[0] + push, y: LIFE.hero[1] + .7 * Math.sin(t * TAU * 3 / LOOP), face: -1, lean: -.16 * pulse(t, dart + .24, .1, dart + .4, .5) };
    hero.look = headTurn((hero.x - p[0]), p[1] - (hero.y - RIG.Neck));
    const swing = swingArm(LIFE_SWINGS, t), hand = handOf(hero, swing === null ? REST : swing);
    hero.rp = hand.rp;
    return {
      x: p[0], y: p[1], k: 1, a, roll, s, large, alpha, hero, axe: { grip: hand.grip, turn: hand.turn, side: -1, flying: false, cast: 0 },
      companion: [hero.x + 9, hero.y - 44 + .5 * Math.sin(t * TAU * 5 / LOOP)],
    };
  }

  // ---------- drawing ----------
  const turnOf = (id, b) => (FREE[id] ? b.roll : b.a);
  const mirrorOf = (id, b) => (FREE[id] ? 1 : (b.k < 0 ? -1 : 1) * Math.max(.1, Math.abs(b.k)));
  function drawBody(c, id, b, t, zoom, at = [b.x, b.y]) {
    if (b.s.dead >= DEAD - 1e-4) return;
    c.save();
    c.translate(at[0] * zoom, at[1] * zoom); c.rotate(turnOf(id, b)); c.scale(mirrorOf(id, b) * zoom, zoom);
    BreakerRig.draw(c, RIGS[id], BreakerRig.rest({ ...b.s, t }), IMG, 'hd');
    c.restore();
  }
  function drawPiece(c, piece) {
    const im = IMG[piece.key];
    if (ready(im)) c.drawImage(im, (piece.at[0] - piece.w / 2) * Z, (piece.at[1] - piece.h / 2) * Z, piece.w * Z, piece.h * Z);
  }
  function drawAxe(c, axe) {
    if (!ready(IMG.axe)) return;
    c.save(); c.translate(axe.grip[0] * Z, axe.grip[1] * Z); c.scale(axe.side * Z, Z); c.rotate(axe.turn); c.scale(AXE.size, AXE.size);
    c.drawImage(IMG.axe, -AXE.grip[0], -AXE.grip[1], AXE.w, AXE.h);
    c.restore();
  }
  function drawThread(c, axe, companion, t) {
    if (!axe.flying || !axe.cast) return;
    const from = mul(companion, Z), end = mul(sub(mul(axe.grip, Z), from), axe.cast), across = unit([-end[1], end[0]]), loose = 1 - axe.cast * axe.cast, pts = [];
    for (let j = 0; j <= 20; j++) {
      const u = j / 20, bend = 6 * loose * Math.sin(u * 8 - t * 11) + .7 * (1 - loose) * Math.sin(u * 21 - t * 70);
      pts.push(add(add(from, mul(end, u)), mul(across, bend * Z * Math.sin(PI * u))));
    }
    c.save();
    c.lineCap = 'round'; c.lineJoin = 'round';
    c.beginPath(); pts.forEach((p, i) => (i ? c.lineTo(p[0], p[1]) : c.moveTo(p[0], p[1])));
    c.strokeStyle = 'rgba(8,10,14,.55)'; c.lineWidth = 3; c.stroke();
    c.strokeStyle = css(EMBER, .9); c.lineWidth = 1.5; c.stroke();
    // the beads of the companion's own thread run along it
    c.fillStyle = css(pale(EMBER, .35));
    for (let m = 0; m < 3; m++) { const u = ((t * .9 + m / 3) % 1) * 20, i = Math.min(19, Math.floor(u)), p = lerp2(pts[i], pts[i + 1], u - i); c.fillRect(p[0] - 1, p[1] - 1, 2, 2); }
    c.fillRect(pts[20][0] - 2, pts[20][1] - 2, 4, 4);
    c.restore();
  }
  function drawCrew(c, h, axe, companion, t) {
    Object.assign(figure, { x: h.x * Z, y: h.y * Z, face: h.face, lean: h.lean, look: h.look, rp: h.rp, rk: 1 });
    figure.P = poseOf(figure);
    drawHero(c, figure);
    if (!axe.flying) drawAxe(c, axe);
    c.save(); c.translate(figure.x, figure.y); c.scale(h.face * Z, Z); fist(c, figure.P.armF[2], SUITS.me[4]); c.restore();
    drawThread(c, axe, companion, t);
    const look = axe.flying ? angOf(sub(axe.grip, companion)) : Math.atan2(22, 22 * h.face);
    drawDrone(c, companion[0] * Z, companion[1] * Z, look, 0, t, DSC, true);
  }

  const SKY = '#070a10', STAR_FIELD = STARS.slice(0, 100).map(([x, y, s, a]) => [x * LW / 1400, y * LH / 450, s * 1.3, css([206, 214, 226], a * .5)]);
  function sky(c, x0, x1, shift = 0) {
    c.fillStyle = SKY; c.fillRect(x0, 0, x1 - x0, LH);
    for (const [x, y, s, col] of STAR_FIELD) {
      let at = x;
      if (at < x0 || at >= x1) continue;
      if (shift) { const w = x1 - x0; at = x0 + (((x - x0 - shift) % w) + w) % w; }
      c.fillStyle = col; c.fillRect(at, y, s, s);
    }
  }
  const FONT = 'system-ui,"Segoe UI",sans-serif';
  const fontFor = T => clamp(Math.round(14 * LW / (T.cv.clientWidth || 600)), 18, 34);
  function text(c, words, x, y, fs, align = 'left', col = '#d8d4ca') {
    c.font = `600 ${fs}px ${FONT}`; c.textAlign = align; c.textBaseline = 'alphabetic';
    c.fillStyle = 'rgba(4,6,10,.75)'; c.fillText(words, x + 1.5, y + 1.5);
    c.fillStyle = col; c.fillText(words, x, y);
    c.textAlign = 'left';
  }
  const saidAt = (list, t) => list.reduce((said, [at, words]) => (t >= at ? words : said), '');

  function drawPack(T, clockNow) {
    const c = T.c, n = Math.floor(clockNow / LOOP), fs = fontFor(T);
    const roster = T.cand.id === 'kin' ? kinRoster(n) : mixedRoster(n);
    const t = roster.length ? clockNow - n * LOOP : .5, m = momentOf(FIGHTS[SIZES[n % 3]], t), shown = ease(t, 0, .45);
    T.roster = roster; T.t = t;
    sky(c, 0, LW);
    drawPiece(c, PIECE);
    const bodies = front => {
      c.save(); c.globalAlpha = shown;
      for (let i = roster.length - 1; i >= 0; i--) if (m.bodies[i].front === front) drawBody(c, roster[i], m.bodies[i], clockNow + i * 1.37, Z);
      c.restore();
    };
    bodies(false);
    drawCrew(c, m.hero, m.axe, m.companion, clockNow);
    bodies(true);
    if (m.axe.flying) drawAxe(c, m.axe);
    T.caption = roster.length ? saidAt(SAYS, t) : 'Все девять отмечены «без него»: стаи нет';
    text(c, T.caption, 18, LH - 16, fs);
    if (T.cand.id === 'kin' && roster.length) text(c, NAME[roster[0]], LW - 18, 10 + fs, fs, 'right', '#a9a499');
  }

  const PANE = 600;
  function drawLife(T, clockNow) {
    const c = T.c, id = T.cand.id, t = clockNow % LOOP, L = lifeAt(t), fs = fontFor(T);
    T.t = t; T.caption = saidAt(STATES, t);
    // the stars stream by while it flies, a whole pane's width a loop, so the loop has no seam
    sky(c, 0, PANE, PANE * ease(t, 2.9, 2.8));
    c.save(); c.beginPath(); c.rect(0, 0, PANE, LH); c.clip();
    c.globalAlpha = L.alpha;
    drawBody(c, id, L, clockNow, 10, [PANE / 20 + L.large[0], LH / 20 + L.large[1]]);
    c.restore();
    sky(c, PANE, LW);
    const small = { x: PANE + (LW - PANE - LIFE.w * 2) / 2, y: LH - LIFE.h * 2 };
    for (const [x, y, k] of [[PANE, 0, 1], [small.x, small.y, .5]]) {
      c.save(); c.translate(x, y); c.scale(k, k);
      c.beginPath(); c.rect(0, 0, LIFE.w * Z, LIFE.h * Z); c.clip();
      drawPiece(c, LIFE.piece);
      drawCrew(c, L.hero, L.axe, L.companion, clockNow);
      c.globalAlpha = L.alpha;
      drawBody(c, id, L, clockNow, Z);
      c.restore();
    }
    c.strokeStyle = '#1d222b'; c.lineWidth = 2;
    c.beginPath(); c.moveTo(PANE, 0); c.lineTo(PANE, LH); c.moveTo(PANE, small.y); c.lineTo(LW, small.y); c.stroke();
    text(c, T.caption, 18, 14 + fs * 1.3, Math.round(fs * 1.3));
    for (const [words, x, y] of [['×10', 18, LH - 16], ['×4', PANE + 14, 10 + fs], ['×2', PANE + 14, small.y + 10 + fs]]) text(c, words, x, y, fs, 'left', '#8f8b82');
  }

  // ---------- one clock, and a moment from the address: #at=7.3&pause ----------
  let clock = .5, frameSeen = null;
  function open(address) {
    for (const part of String(address || '').replace(/^#/, '').split('&')) {
      const [key, v] = part.split('=');
      if (key === 'pause') OPT.playing = false;
      else if (key === 'at' && v && isFinite(+v) && +v >= 0) { clock = +v; dirtyAll(); }
      else if (IDS.includes(key) && (v === 'take' || v === 'without')) OPT[key] = v;
    }
  }
  if (typeof location !== 'undefined') open(location.hash);
  function tick(dt) {
    // the kit's `last` is this animation frame's time: its first drawn tile moves the clock for all
    if (frameSeen === last) return;
    frameSeen = last; clock += dt;
  }

  const PACK_TIME = 'Петля боя 14 с: сон, побудка, окружение, замах и хватка, удар, бросок топора, распад.';
  const cands = [
    { id: 'mixed', name: 'Стая вперемешку', idea: 'В одной стае разные тела. За несколько стай увидишь всех девятерых.', time: PACK_TIME, pack: true },
    { id: 'kin', name: 'У каждой стаи свой вид', idea: 'Стая целиком из одного вида, у следующего гнезда уже другой.', time: PACK_TIME, pack: true },
    ...IDS.map(id => ({ id, name: NAME[id], idea: 'Только этот вид во всех стаях.', time: 'Слева все состояния крупно, справа как в игре рядом с героем и мелко.' })),
  ];

  return {
    cands,
    expose: { IDS, RIGS, CUTS, IMG, BreakerRig, FREE, FIGHTS, SIZES, STATES, SAYS, LOOP, FPS, STOP, WINDUP, CELL, DEAD, HIT_AT, STROKE, mixedRoster, kinRoster, lifeAt, momentOf, turnOf, mirrorOf, open, clock: () => clock },
    init(T) {
      T.draws = 0; T.roster = null; T.caption = '';
      T.cv.style.touchAction = 'pan-y';
      // the two fights open one to one, so they show the game's pixels
      if (T.cand.pack) { T.sec.classList.add('wide'); T.sec.querySelector('.big').textContent = 'Обратно'; }
    },
    scene(T, dt) {
      tick(dt);
      T.time = clock; T.draws++;
      const c = T.c;
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      if (T.cand.pack) drawPack(T, clock); else drawLife(T, clock);
    },
  };
}

module.exports = {
  title: 'Живые разборщики', heading: 'Живые разборщики', window: false, tile: [1120, 600],
  lead: `<p class="lead">Все девять из кусков, нарисованных GPT в нашем стиле, в разрешении игры. Движение делает код: каждый спит свёрнутым, просыпается, летит, замахивается, хватает, отлетает от удара и разваливается на свои детали.</p>
<p class="miss">Это ещё не игра: бой поставлен страницей, герой и спутник нарисованы упрощённо, звуков пока нет. В игре у всех девяти будут одни правила боя, разные только тело и движения.</p>`,
  kitBars: ['speedMode', 'playMode'],
  toggles: BODIES.map(([id, name]) => ({ id: `with_${id}`, key: id, label: name, saved: true, short: name, opts: [['берём', 'take'], ['без него', 'without']] })),
  pick: { doc: 'picks/breakerPacks', subject: 'Живые разборщики', what: ['как живут разборщики', 'той картинкой'] },
  pictures,
  // the page goes in with the rigs' own files before it: they are never copied by hand
  page: { toString: () => `function () {\n${SHARED}\nreturn (${page.toString()})();\n}` },

  // build.js --break <name>: each breaks one rule of check(K)
  breaks: {
    picture: [["pic: off(s) ? 'dead' : 'shell'", "pic: off(s) ? 'gone' : 'shell'"]],
    roster: [["IDS.filter(id => OPT[id] !== 'without')", 'IDS.slice()']],
    companion: [['companion = add(home, mul(unit(sub(held.grip, home)), 2.5 * held.cast));', 'companion = lerp2(home, held.grip, held.cast);']],
    stop: [['HIT_AT = .35 + .12 * 2.1 / 2.4, STOP = .05', 'HIT_AT = .35 + .12 * 2.1 / 2.4, STOP = .12']],
    snap: [['sleep: 1 - ease(t, b.wake, .6)', 'sleep: t < b.wake ? 1 : 0']],
    upright: [['lean = attacking ?', 'lean = b.v[0] < -20 ? PI : attacking ?']],
  },

  check(K) {
    const X = K.X, ids = BODIES.map(b => b[0]), at = t => Math.round(t * X.FPS);
    let ok = true;
    const need = (good, words) => { if (!good) { ok = false; K.fail(words); } };

    need(K.tiles.map(T => T.cand.id).join() === ['mixed', 'kin', ...ids].join(), 'eleven tiles in order');

    // ---------- every picture, every pose, in every state ----------
    let asked = 0;
    const seen = (id, state, where) => {
      for (const p of X.BreakerRig.posed(X.RIGS[id], X.BreakerRig.rest(state))) {
        asked++;
        if (![p.o[0], p.o[1], p.a, p.k].every(isFinite)) need(false, `${id}: a pose is non-finite ${where}`);
        if (!p.hidden && (!X.CUTS[id][p.pic] || !X.IMG[`${id}_${p.pic}_hd`])) need(false, `${id}: no picture ${p.pic} ${where}`);
      }
    };
    const levels = [0, .3, .51, .61, 1];
    for (const id of ids) {
      for (const sleep of levels) for (const tell of [0, .5, 1]) for (const grip of [0, .49, .51, 1]) for (const hurt of [0, 1]) for (const dead of [0, .01, .8, 1.6]) for (const v of [[0, 0], [1, 1]]) seen(id, { t: 1.7, sleep, tell, grip, hurt, dead, v }, 'in the sweep');
      for (let f = 0; f <= at(X.LOOP); f++) seen(id, { ...X.lifeAt(f / X.FPS).s, t: f / X.FPS }, `at ${f / X.FPS} of its tile`);
    }

    // ---------- the fight ----------
    const gap = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
    for (const N of X.SIZES) {
      const S = X.FIGHTS[N], flung = S.flings.filter(f => f.by === 'swing');
      const tells = S.acts.map(a => a.at), held = S.acts.filter(a => a.held);
      need(S.acts.every(a => a.who !== null) && tells.every((t, i) => !i || t - tells[i - 1] >= 1.5 - 1e-6), `${N}: one at a time, 1.5 s apart`);
      need(held.every(a => a.landed !== null && Math.abs(a.go - a.at - .6) < .02 && a.landed - a.go <= .32), `${N}: a tell of 0.6 s, then a dart and a grip`);
      need(S.swings.every(s => s.who.length === 1 && s.who[0] === S.acts[s.act].who), `${N}: a swing strikes the one that winds up (${S.swings.map(s => s.who)})`);
      need(flung.length === 2 && !flung[0].dies && flung[1].dies, `${N}: the first blow flings, the second kills`);
      if (flung.length) {
        const b = S.frames[at(flung[0].t + .78)].bodies[flung[0].i], cells = Math.hypot(b.x - flung[0].from[0], b.y - flung[0].from[1]) / X.CELL;
        need(cells > 3.4 && cells < 4.6, `${N}: flung about four cells (${cells.toFixed(1)})`);
      }
      need(S.axe.out.length === N - 1 && S.axe.home.length === N - 1, `${N}: the axe through all ${N - 1}, out and home (${S.axe.out.length}, ${S.axe.home.length})`);
      need(S.axe.caught !== null && S.axe.caught < 11.5, `${N}: hauled back into the hand`);
      need(S.bodies.every(b => b.dead !== null) && S.frames[at(X.LOOP)].bodies.every(b => b.s.dead >= X.DEAD - 1e-4), `${N}: every one has come apart by the loop's end`);
      let strayed = 0, stopped = 0;
      for (let f = 0; f < S.frames.length; f++) {
        const F = S.frames[f], was = S.frames[f - 1];
        strayed = Math.max(strayed, Math.hypot(F.companion[0] - F.home[0], F.companion[1] - F.home[1]));
        F.bodies.forEach((b, i) => {
          const when = `${N}/${i} at ${(f / X.FPS).toFixed(2)}`;
          if (![b.x, b.y, b.a, b.roll, b.k].every(isFinite)) need(false, `${when}: non-finite`);
          if (Math.abs(b.a) > .65) need(false, `${when}: upside down or too far over (${b.a.toFixed(2)})`);
          if (b.s.dead < .8 && (b.x < 10 || b.x > 270 || b.y < 12 || b.y > 138)) need(false, `${when}: out of the picture (${b.x.toFixed(0)}, ${b.y.toFixed(0)})`);
          if (f % 3 === 0) for (const id of ids) seen(id, { ...b.s, t: f / X.FPS }, when);
          if (!was) return;
          const q = was.bodies[i];
          if (gap(b, q) > 6.5 || Math.abs(b.a - q.a) > .2) need(false, `${when}: a jump (${gap(b, q).toFixed(1)} px, ${(b.a - q.a).toFixed(2)} rad)`);
          for (const k of ['sleep', 'tell', 'grip', 'hurt', 'dead']) if (Math.abs(b.s[k] - q.s[k]) > .42) need(false, `${when}: ${k} snaps`);
        });
      }
      for (const s of S.swings) {
        let still = 0;
        for (let f = at(s.at + .15); f < at(s.at + .37); f++) {
          still = S.frames[f].hero.rp.every((v, i) => Math.abs(v - S.frames[f - 1].hero.rp[i]) < .002) ? still + 1 : 0;
          stopped = Math.max(stopped, still);
        }
      }
      need(strayed <= 3, `${N}: the companion keeps its place (${strayed.toFixed(1)})`);
      need(stopped <= 4, `${N}: a hit-stop of 50 ms at most (${stopped} frames)`);
      const first = S.frames[0].hero, end = S.frames[at(X.LOOP)].hero;
      need(Math.abs(first.x - end.x) < .5 && Math.abs(first.y - end.y) < .5 && first.face === end.face && Math.hypot(first.rp[0] - end.rp[0], first.rp[1] - end.rp[1]) < .5, `${N}: the hero has no seam between two loops`);
      console.log(`  pack of ${N}: grips ${held.map(a => (a.landed === null ? '-' : a.landed.toFixed(2)))}, blows ${flung.map(f => f.t.toFixed(2))}, axe out ${S.axe.out.length} home ${S.axe.home.length}, caught ${S.axe.caught && S.axe.caught.toFixed(2)}, companion ${strayed.toFixed(1)} px`);
    }
    for (let f = 1; f <= at(X.LOOP); f++) {
      const a = X.lifeAt((f - 1) / X.FPS), b = X.lifeAt(f / X.FPS);
      if (a.alpha > .01 && b.alpha > .01 && a.s.dead < X.DEAD - 1e-4 && gap(a, b) > 6.5) need(false, `a body's own loop jumps at ${(f / X.FPS).toFixed(2)}`);
    }
    console.log(`  ${asked} posed parts: each finite, its picture there`);

    // ---------- rosters ----------
    const kinds = new Set(), kins = new Set();
    for (let n = 0; n < 9; n++) {
      const mixed = X.mixedRoster(n), kin = X.kinRoster(n);
      need(mixed.length >= 6 && mixed.length <= 8 && new Set(mixed).size === mixed.length, `loop ${n}: a pack of 6 to 8 different bodies`);
      need(new Set(kin).size === 1, `loop ${n}: one kind`);
      if (n < 3) mixed.forEach(id => kinds.add(id));
      kins.add(kin[0]);
    }
    need(kinds.size === 9 && kins.size === 9, 'all nine come by');

    // ---------- live: a whole loop, the toggles, the pick, a moment from the address ----------
    const mixed = K.tiles[0], kin = K.tiles[1], end = at(X.LOOP) + 4, named = K.tiles.map(() => new Set());
    let hidden = 0;
    need(mixed.sec.classList.s.has('wide') && kin.sec.classList.s.has('wide'), 'the fights open one to one');
    K.at(end, () => { X.open('#at=0.6'); });
    K.at(end + 2, () => {
      need(mixed.roster.includes('triad') && kin.roster[0] === 'triad', 'Триада is in both packs to begin with');
      K.seg('with_triad', 1);
    });
    K.at(end + 3, () => {
      need(!mixed.roster.includes('triad') && !kin.roster.includes('triad'), '«без него» leaves both rosters at once');
      for (let n = 0; n < 12; n++) need(!X.mixedRoster(n).includes('triad') && !X.kinRoster(n).includes('triad'), '«без него» holds for every loop');
      K.click(0, '.pick');
      const doc = K.picked();
      need(doc && doc.pick === 'mixed' && ids.every(id => doc[id] === (id === 'triad' ? 'without' : 'take')), 'a pick writes every toggle');
      for (const id of ids) K.seg(`with_${id}`, 1);
    });
    K.at(end + 5, () => {
      need(!mixed.roster.length && !kin.roster.length, 'with all nine out there is no pack');
      for (const id of ids) K.seg(`with_${id}`, 0);
    });
    K.at(end + 7, () => {
      need(mixed.roster.includes('triad') && ids.every(id => K.picked()[id] === 'take'), '«берём» brings it back, saved with the pick');
      hidden = kin.draws; kin.visible = false;
    });
    K.at(end + 12, () => { need(kin.draws === hidden, 'only a tile in view is drawn'); kin.visible = true; X.open('#at=7.25&pause'); });
    K.at(end + 15, () => {
      need(Math.abs(X.clock() - 7.25) < 1e-6 && K.tiles.every(T => Math.abs(T.t - 7.25) < 1e-6), '#at and pause open at a moment');
    });
    K.run(end + 16, (T, f) => { if (f < end) named[K.tiles.indexOf(T)].add(T.caption); });
    named.slice(2).forEach((set, i) => need(new Set(X.STATES.map(s => s[1])).size === 7 && X.STATES.every(s => set.has(s[1])), `${ids[i]}: its seven states are named`));
    need(named[0].size === X.SAYS.length, `the fight is captioned (${named[0].size})`);
    return ok;
  },
};
