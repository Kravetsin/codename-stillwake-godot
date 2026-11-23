// The companion on a thread in a long straight flight: six ways it hooks, lives in tow and lets go, over one flight of the hero.
// The drone is drone-salts.js's «С облачком» idle, its free flight DroneFlight's and DroneWill's perch, the hero's pose Pose.cs's
// Floating and Paddling. Each tile's flight is worked out once, step by step, then shown from the clock alone.
// For a frame: index.html#at=<s into the loop>&wait=2|3|5&colour=<0..7>&slow&pause
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-tow.js <out-dir> [--break jump|snap|head|long]
module.exports = {
  title: 'Спутник на нитке',
  heading: 'Спутник на нитке',
  window: false,
  tile: [720, 540],
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Герой несколько секунд летит в одну сторону, и спутник цепляется за него ниткой. Выбираешь, <b>как он едет</b>: как цепляется, как живёт на нитке и как отпускает. Нажми «Выбрать этот» под тем, что нравится, хватит одного щелчка. Вместе с ним запомнится, через сколько секунд он цепляется.</p>
<p class="lead">Во всех плитках один и тот же полёт: старт, прямая, герой оглядывается на лету, разворот, подъём, остановка. Внизу плитки видно, какой сейчас момент. «Крупно 1:1» показывает плитку крупнее.</p>
<p class="miss">Не показано: свет Godot, с ним цвета выйдут чуть иначе; звук; стены; выхлоп ранца. Предупреждения и окна важнее нитки: в игре ради них спутник сразу отцепится.</p>`,
  toggles: [
    { id: 'waitMode', label: 'Цепляется через:', key: 'wait', opts: [['2 с', 2], ['3 с', 3], ['5 с', 5]], saved: true, short: 'цепляется через' },
    { id: 'colourMode', label: 'Цвет игрока:', key: 'colour', opts: ['лайм', 'бирюза', 'синий', 'пурпур', 'голубой', 'фиолетовый', 'розовый', 'зелёный'].map((n, i) => [n, i]) },
  ],
  pick: { doc: 'picks/droneTow', subject: 'Спутник на нитке', what: ['повадку', 'повадкой'] },
  pictures: { core: 'game/art/hd/drone_core.png', strap: 'game/art/hd/hero_tracker.png' },

  page() {
    const TAU = Math.PI * 2, FPS = 60, SUB = 5, LOOP = 28, N = LOOP * FPS;
    const hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
    const CREW = ['#d8ec54', '#5fc8c0', '#5a8ef0', '#dc54cc', '#8cc4f8', '#8c64e8', '#e88cc8', '#8cd06a'].map(h => ({ hex: h, rgb: hex(h) }));

    // ---------- vectors ----------
    const sub = (a, b) => [a[0] - b[0], a[1] - b[1]], mul = (a, k) => [a[0] * k, a[1] * k], dot = (a, b) => a[0] * b[0] + a[1] * b[1];
    const len = a => Math.hypot(a[0], a[1]);
    const unit = a => { const l = len(a); return l > 1e-6 ? [a[0] / l, a[1] / l] : [0, 0]; };
    const perp = a => [-a[1], a[0]];
    const mod = (v, m) => (v % m + m) % m, frac = v => v - Math.floor(v);
    const hsh = i => frac(Math.sin(i * 127.1 + 311.7) * 43758.5453);
    const angTo = (a, b) => Math.atan2(b[1] - a[1], b[0] - a[0]);
    const span = (q, a, b) => sm((q - a) / (b - a));
    const bump = k => Math.sin(Math.PI * c01(k));

    // ---------- the game's numbers, in world px and seconds ----------
    const CELL = 8, UNIT = CELL * FPS / 1000;   // a thousandth of a cell a step, as px a second
    const JET = { accel: 16 * UNIT, top: 500 * UNIT, drag: 8 * UNIT }, SWIM = 60 * UNIT;   // Jetpack.cs, Hero.Swim
    const ADRIFT = { lean: .22, leanFast: .38, tuck: .06, trail: .15, trailFast: .3, paddle: 1.1, paddleFast: .7, sway: LOOP / 5, drift: LOOP / 8 };   // sway and drift fitted to the loop, within 8 %
    const SETTLE = { blend: .32, float: .11, lean: .45, lift: .03 };   // Figure.cs
    const FLY = { above: 24, follow: .26, learn: .18, far: 60, hurry: 2, ease: .08, homeX: 18 };   // DroneFlight.Tune
    const MID = RIG.H / 2, OVER = -(RIG.Neck - MID) - FLY.above, PERCH = [-15.5, -1];   // DroneWill's, between 12 and 18 aside
    const HOME = [PERCH[0], OVER + PERCH[1]];

    // ---------- one flight for every tile ----------
    const AT = { start: .6, twist: 9.2, back: 10.8, turn: 12.2, climb: 14.3, brake: 24.2, rest: 26.2 };
    const THRUST = [[0, null], [AT.start, [1, 0]], [AT.turn, [-1, 0]], [AT.climb, [0, -1]], [AT.brake, null]];
    const FACES = [[0, 1], [AT.twist, -1], [AT.back, 1], [AT.turn, -1], [AT.rest, 1]];
    const during = (list, t) => { let v = list[0][1]; for (const [t0, x] of list) if (t + 1e-6 >= t0) v = x; return v; };

    // Pose.Floating and Pose.Paddling: the arms, the legs, the tilt and the lift
    function floating(t, vel, face, swim, fast) {
      const drift = Math.sin(t / ADRIFT.drift * TAU), sway = Math.sin(t / ADRIFT.sway * TAU);
      const still = { fa: [.15 + .06 * drift, .35], ba: [.4 - .08 * drift, .55], fl: [.25 + .1 * sway, -(.5 + .1 * drift)], bl: [-.08 * sway, -(.35 - .1 * drift)], tilt: .03 * sway, lift: 2 * drift };
      if (swim <= .0005) return still;
      const signOf = v => Math.abs(v) < 5 ? 0 : Math.sign(v), along = signOf(vel[0]) * face, sinking = signOf(vel[1]);
      const paddling = (every, lean, trail) => {
        const p = t / every * TAU;
        const leg = lag => { const kick = .1 * Math.sin(p + lag); return sinking > 0 ? [.85 + kick, -1.35] : along ? [-.2 + kick, -(.55 + .15 * Math.cos(p + lag))] : [.05 + kick, -.2]; };
        return { fa: [.15 + .06 * drift - along * trail, .35], ba: [.4 - .08 * drift - along * trail, .55], fl: leg(Math.PI), bl: leg(0), tilt: along * lean + (sinking > 0 ? ADRIFT.tuck : 0), lift: .5 * drift };
      };
      return mixPose(still, mixPose(paddling(ADRIFT.paddle, ADRIFT.lean, ADRIFT.trail), paddling(ADRIFT.paddleFast, ADRIFT.leanFast, ADRIFT.trailFast), fast), swim);
    }
    function mixPose(a, b, k, kTilt = k, kLift = k) {
      return { fa: mixBend(a.fa, b.fa, k), ba: mixBend(a.ba, b.ba, k), fl: mixBend(a.fl, b.fl, k), bl: mixBend(a.bl, b.bl, k), tilt: angLerp(a.tilt, b.tilt, kTilt), lift: lerp(a.lift, b.lift, kLift) };
    }
    // the kit hero's joints, upright: the figure turns whole about its middle
    function jointsOf(q) {
      const sh = [.4, -RIG.Shoulder], rise = x => x * Math.sin(2 * q.tilt) / 2;   // Rig.HipRise
      const leg = (s, bend) => {
        const x = s * RIG.HipApart, p = limbPts([x, -RIG.Hip - rise(x)], bend, RIG.Leg), t = bend[0] + bend[1];
        return [...p, [p[2][0] + Math.cos(t) * 2.6, p[2][1] - Math.sin(t) * 2.6]];
      };
      return { bob: 0, lean: 0, hip: [0, -RIG.Hip], neck0: NECK(), neck: NECK(), sh, legF: leg(1, q.fl), legB: leg(-1, q.bl), armF: limbPts(sh, q.fa, RIG.Arm), armB: limbPts(sh, q.ba, RIG.Arm) };
    }
    const inWorld = (p, q, face) => { const r = rot(p[0], p[1] + MID, q.tilt); return [face * r[0], r[1] - q.lift]; };   // from the hero's middle
    const downShin = (leg, d) => add(leg[1], mul(unit(sub(leg[2], leg[1])), d));
    // where a thread holds: the same side of the world through a twist (R, the rear)
    const HOLDS = {
      ankle: J => downShin(J.legF, 4.5),
      shin: J => downShin(J.legF, 3.2),
      heel: J => downShin(J.legB, 5.6),
      crown: (J, R, f) => [HELM[0] + R * f * 3.7, HELM[1] - 5.2],
      beltRear: (J, R, f) => [R * f * 3.9, -RIG.Hip - 1.2],
      beltFore: (J, R, f) => [-R * f * 3.7, -RIG.Hip - 1.2],
      shoulderRear: (J, R, f) => [R * f * 4.1, -27.3],
      shoulderFore: (J, R, f) => [-R * f * 4.2, -24.6],
    };
    const SLIDE = .3;   // the hero mirrors at once on a twist: a loop slides round to its new place
    const HERO_RUN = (() => {
      const out = [], slid = {};
      let vel = [0, 0], pos = [0, 0], swim = 0, fast = 0, q = floating(-2, [0, 0], 1, 0, 0), faceWas = 1, cam = [0, -5];
      for (let i = -120; i <= N; i++) {
        const t = i / FPS;
        if (i > 0) {
          const was = vel, push = during(THRUST, (i - 1) / FPS);
          if (push) vel = add(vel, mul(push, JET.accel));
          else vel = len(vel) <= JET.drag ? [0, 0] : sub(vel, mul(unit(vel), JET.drag));
          if (len(vel) > JET.top) vel = mul(unit(vel), JET.top);
          pos = add(pos, mul(lerp2(was, vel, .5), 1 / FPS));
        }
        const face = during(FACES, t), speed = len(vel);
        swim = lerp(swim, speed > 1 ? 1 : 0, 1 / FPS / SETTLE.blend);
        fast = lerp(fast, speed > SWIM ? 1 : 0, 1 / FPS / SETTLE.blend);
        q = mixPose(q, floating(t, vel, face, swim, fast), 1 / FPS / SETTLE.float, 1 / FPS / SETTLE.lean, Math.min(1, 1 / FPS / SETTLE.lift));
        if (i < 0) continue;
        cam = lerp2(cam, [-vel[0] * .095, -5 - vel[1] * .048], 1 / FPS / .45);
        const J = jointsOf(q), h = { pos, vel, face, q, cam, pts: {}, helm: inWorld(HELM, q, face), visor: inWorld([HELM[0] + 3, HELM[1] - .1], q, face), off: {} };
        for (const R of [-1, 1]) for (const id in HOLDS) {
          const key = id + R, at = inWorld(HOLDS[id](J, R, face), q, face);
          const S = slid[key] || (slid[key] = { off: [0, 0], t0: -9, last: at });
          if (face !== faceWas) { S.off = sub(S.last, at); S.t0 = t; }
          const k = (t - S.t0) / SLIDE, off = k < 1 ? mul(S.off, 1 - sm(k)) : [0, 0];
          h.pts[key] = S.last = add(at, off);
          h.off[key] = len(off);
        }
        faceWas = face;
        out.push(h);
      }
      return out;
    })();

    // ---------- a hook's frame: b behind along the flight, n up in a level flight and out behind the back in a climb ----------
    const from = (p, O, back, out) => [p[0] + O.b[0] * back + O.n[0] * out, p[1] + O.b[1] * back + O.n[1] * out];
    const ELL = [14, 28];
    // round the body, never by the angle `avoid`: angles on an ellipse about the hero's middle
    function round(a, b, k, avoid) {
      if (k <= 0) return a;
      if (k >= 1) return b;
      const A = [a[0] / ELL[0], a[1] / ELL[1]], B = [b[0] / ELL[0], b[1] / ELL[1]];
      const th = lerp(mod(Math.atan2(A[1], A[0]) - avoid, TAU), mod(Math.atan2(B[1], B[0]) - avoid, TAU), k) + avoid;
      const r = lerp(lerp(len(A), len(B), k), 1.45, bump(k) * .8);
      return [Math.cos(th) * r * ELL[0], Math.sin(th) * r * ELL[1]];
    }
    const DOWN = Math.PI / 2, OVERHEAD = -Math.PI / 2;
    const frontOf = O => O.R < 0 ? 0 : Math.PI;
    const underFeet = O => O.climb ? Math.PI : OVERHEAD, overHead = O => O.climb ? Math.PI : DOWN;
    const soft = (v, w = .7) => v / w > 30 ? v : w * Math.log(1 + Math.exp(v / w));
    const REST = { A: -2.5, sc: 1, flat: 1, tilt: 0, lid: 0, tail: 0, rib: 0, pace: 1, spin: 0, buzz: 0 };
    const thrown = (t0, t1, arc = 5) => (q, O, z, A) => add(lerp2(z, A, span(q, t0, t1)), mul(O.n, arc * bump(span(q, t0, t1))));

    // ---------- the candidates ----------
    // near: where it goes to hook, reached at `arrive`; life: where it rides, eased into over `out`; leave: its way off.
    // A thread: from and hit in seconds of the hook, at which hold, its tip until then, its slack after, the way it bows.
    const ANKLE = { out: 34, creep: 21 };
    function draggedAt(q) {
      const p = frac(Math.max(0, q - 2.5) / 2.8);
      return p < .14 ? ANKLE.out : p < .7 ? lerp(ANKLE.out, ANKLE.creep, span(p, .14, .7)) : lerp(ANKLE.creep, ANKLE.out, span(p, .7, .92));
    }
    const KITE = { back: 27, up: 19 };
    const eight = q => [6 * Math.sin(q * TAU / 2.6), 4.5 * Math.sin(q * TAU / 1.3) - 9 * bump((frac((q - 1.8) / 3.9) - .55) / .3)];
    const carve = q => Math.sin(Math.max(0, q - 2.1) * TAU / 2.8);
    const heaveAt = q => { const p = frac(Math.max(0, q - 1.7) / 1.3); return p < .5 ? span(p, 0, .5) : 1 - span(p, .62, 1); };
    const glanceAt = q => { const p = frac(Math.max(0, q - 1.7) / 3.9); return span(p, .7, .76) * (1 - span(p, .9, .97)); };
    const swayAt = q => Math.sin(Math.max(0, q - 1.9) * TAU / 2.2);

    const cands = [
      {
        id: 'ankle', name: 'За щиколотку',
        idea: 'Висит на длинной нитке у браслета, подтягивается к ноге, и его снова откидывает назад.',
        time: 'Первый бросок мимо, второй ловит. На нитке кувыркается и болтается. Отцепившись, пролетает под ногами.',
        w: 15, damp: .78, max: 44, arrive: .75, out: [1.5, 2.5], caught: 1.4,
        near: (q, O) => from(O.pt('ankle'), O, 17, 4),
        life: (q, O) => from(O.pt('ankle'), O, draggedAt(q), 7.5 * Math.sin(q * TAU / 1.9) - 1),
        threads: [{
          hold: () => 'ankle', from: .4, hit: 1.4,
          tip(q, O, z, A) {
            const past = from(A, O, -6, -7);
            if (q < .75) return lerp2(z, past, span(q, .4, .75));
            if (q < 1.05) return add(lerp2(past, z, span(q, .75, 1.05)), mul(O.b, 7 * bump(span(q, .75, 1.05))));
            return thrown(1.05, 1.4)(q, O, z, A);
          },
          slack: (q, O, d) => { const left = soft(ANKLE.out - .8 - d); return 7 * Math.tanh(left / 7); },
          bow: O => mul(O.n, -1), wave: 1,
        }],
        mood(q, O) {
          const A = O.pt('ankle');
          return {
            A: angTo(O.r, A), tilt: .75 * Math.sin(q * 2.3) * span(q, 1.6, 2.3), flat: lerp(1, 1.5, span(q, 1.6, 2.3)),
            spin: TAU * span(q, 2.1, 2.85), pace: 1.5, tail: .3 * span(q, 2, 2.5),
          };
        },
        leave: { dur: 1, avoid: underFeet, reel: [[.05, .45]], mood: q => ({ spin: TAU * span(q, 0, .8), pace: 2.2 }) },
        tail: { n: 12, life: .35, speed: 45, wide: 1.5 },
      },
      {
        id: 'kite', name: 'Змей на макушке',
        idea: 'Летит змеем над героем и позади, рисует восьмёрки, ныряет и тянет хвост с бантиками.',
        time: 'Крутит петлю над шлемом и набрасывает на макушку. Отцепившись, теряет ветер и порхает, как листок.',
        w: 12, damp: .85, max: 54, arrive: .6, out: [1.4, 2.3], caught: 1.4,
        near: (q, O) => add(O.pt('crown'), O.climb ? [O.R * 11, -14] : [O.b[0] * 9, -15]),
        life(q, O) {
          const [a, e] = eight(q);
          return O.climb ? add(O.pt('crown'), [O.R * (KITE.back + e), 7 + a]) : add(O.pt('crown'), [O.b[0] * (KITE.back + a), -KITE.up - e]);
        },
        threads: [{
          hold: () => 'crown', from: .35, hit: 1.4,
          tip(q, O, z, A) {
            const a = (q - .35) * TAU / .42, r = 1 - span(q, 1.05, 1.4);
            return lerp2(z, add(A, [Math.cos(a) * 5 * r, (-4.5 + Math.sin(a) * 1.5) * r]), span(q, .35, .7));
          },
          slack: q => .7 + .7 * (1 + Math.sin(q * TAU / 2.6)) / 2,
          bow: O => O.climb ? [O.R * .3, -1] : [0, 1], wave: .5, arc: O => O.climb ? [O.R * 3, -5] : [0, -6],
        }],
        mood(q, O) {
          const e = span(q, 1.4, 2.1), line = angTo(O.r, O.pt('crown'));
          return { A: line, sc: lerp(1, 1.18, e), flat: lerp(1, 1.7, e), tilt: e * (.5 * Math.sin(q * TAU / 2.6) + .35), rib: span(q, 1.45, 2.3), pace: lerp(1, .7, e) };
        },
        leave: {
          dur: 1.15, avoid: overHead, reel: [[0, .4]],
          extra: (q, O, k) => mul(O.climb ? [1, 0] : [0, 1], 5 * Math.sin(q * 15) * bump(k)),
          mood: q => ({ tilt: 1.1 * Math.sin(q * 15) * (1 - c01(q / 1.15)), rib: 1 - span(q, .3, 1), flat: 1.4 }),
        },
      },
      {
        id: 'ski', name: 'На лыжах',
        idea: 'Держится за две нитки от пояса и лениво режет волну от края к краю, брызгая искрами.',
        time: 'Кидает нитки по очереди и берётся за рукоятку. Отпускает обе разом и по инерции скользит вперёд.',
        w: 13, damp: .9, max: 48, arrive: .75, out: [1.45, 2.3], caught: 1.35, bar: [.45, .9],
        near: (q, O) => from(lerp2(O.pt('beltRear'), O.pt('beltFore'), .5), O, 24, 5),
        life: (q, O) => { const e = carve(q); return from(lerp2(O.pt('beltRear'), O.pt('beltFore'), .5), O, 37 - 6 * e * e, 17 * e); },
        threads: [
          { hold: () => 'beltRear', from: .5, hit: .9, tip: thrown(.5, .9, 6), slack: q => .2 + 4 * ((1 - carve(q)) / 2) ** 2, bow: O => O.n, wave: .3 },
          { hold: () => 'beltFore', from: .95, hit: 1.35, tip: thrown(.95, 1.35, -6), slack: q => .2 + 4 * ((1 + carve(q)) / 2) ** 2, bow: O => mul(O.n, -1), wave: .3 },
        ],
        mood(q, O) {
          const e = span(q, 1.5, 2.2), bank = Math.cos(Math.max(0, q - 2.1) * TAU / 2.8);
          return { A: angTo(O.r, O.pt('beltRear')), tilt: e * 1.05 * bank, flat: lerp(1, .55, e), sc: lerp(1, 1.1, e), tail: e };
        },
        leave: { dur: 1.05, avoid: underFeet, reel: [[0, .4], [0, .4]], extra: (q, O, k) => mul(O.n, -5 * bump(k * 1.6)), mood: () => ({ flat: .5, tail: .5, tilt: .4 }) },
        tail: { n: 60, life: .75, speed: 52, wide: 2, spray: .4 },
      },
      {
        id: 'shin', name: 'Клубком на голени',
        idea: 'Обматывается вокруг голени и едет на ней, съёжившись и щурясь, а искры сдувает длинным хвостом.',
        time: 'Подлетает к икре, обвивает её и подтягивается. Отпуская, разматывается волчком и отряхивается.',
        w: 24, damp: 1, max: 18, arrive: .6, out: [1, 1.45], caught: 1, wrap: true,
        near: (q, O) => from(O.pt('shin'), O, 7, 6),
        life: (q, O) => from(O.pt('shin'), O, (O.climb ? -.8 : 2.2) + .25 * Math.sin(q * 23), 4 + .2 * Math.sin(q * 17)),
        threads: [{
          hold: () => 'shin', from: .5, hit: 1,
          tip(q, O, z, A) {
            const a = (q - .5) * TAU / .25, r = 1 - span(q, .8, 1);
            return lerp2(z, add(A, [Math.cos(a) * 3.6 * r, Math.sin(a) * 2 * r]), span(q, .5, .75));
          },
          slack: () => .2, bow: O => O.n, wave: 0,
        }],
        mood(q, O) {
          const e = span(q, 1, 1.6), blink = bump((frac(q / 3.3) - .86) / .12);
          return { A: e > .5 ? Math.atan2(-O.b[1], -O.b[0]) : angTo(O.r, O.pt('shin')), sc: lerp(1, .72, e), flat: lerp(1, 1.9, e), lid: e * (.68 + .3 * blink), tail: e, pace: lerp(1, 2.4, e), tilt: e * .2 };
        },
        leave: {
          dur: 1.05, avoid: underFeet, reel: [[.1, .45]],
          extra: (q, O, k) => mul([Math.cos(q * 13), Math.sin(q * 13)], 4.5 * bump(k * 1.3)),
          mood: q => ({ spin: 2 * TAU * span(q, 0, .9), pace: 3, tail: 1 - span(q, 0, .5), tilt: .5 * Math.sin(q * 24) * bump((q - .7) / .5) }),
        },
        tail: { n: 70, life: .65, speed: 62, wide: 2.4 },
      },
      {
        id: 'tug', name: 'Буксир',
        idea: 'Обгоняет героя, берёт его на нитку и рывками тащит за собой, пыхтит искрами и оглядывается.',
        time: 'Забегает вперёд и кидает нитку назад, к плечу. Нитка срывается, он улетает один и возвращается.',
        w: 14, damp: .8, max: 62, arrive: .85, out: [1.3, 1.75], caught: 1.3, avoid: overHead,
        near: (q, O) => O.climb ? add(O.pt('shoulderRear'), [O.R * 20, -31]) : from(O.pt('shoulderFore'), O, -29, 14),
        life(q, O) {
          const e = heaveAt(q), shake = Math.sin(q * 41) * .5 * e;
          return O.climb ? add(O.pt('shoulderRear'), [O.R * (19 + shake), -31 - 7 * e]) : from(O.pt('shoulderFore'), O, -30 - 9 * e, 15 + shake);
        },
        threads: [{
          hold: O => O.climb ? 'shoulderRear' : 'shoulderFore', from: .85, hit: 1.3,
          tip: (q, O, z, A) => add(lerp2(z, A, span(q, .85, 1.3)), mul(O.climb ? [O.R, 0] : [0, 1], 7 * bump(span(q, .85, 1.3)))),
          slack: q => 4.5 * (1 - heaveAt(q)) ** 2,
          bow: O => O.climb ? [O.R, 0] : [0, 1], wave: .3, arc: O => O.climb ? [O.R * 4, 0] : [0, 5],
        }],
        mood(q, O) {
          const ahead = Math.atan2(-O.b[1], -O.b[0]), back = angTo(O.r, O.pt('shoulderRear'));
          if (q < 1.4) return { A: q < .6 ? ahead : back };
          const e = heaveAt(q);
          return { A: angLerp(ahead, back, glanceAt(q)), sc: 1 - .12 * e, flat: lerp(1.2, .5, e), tilt: O.climb ? 1.5 : .2, lid: .4 * e, tail: e, pace: 1 + 2 * e, buzz: e };
        },
        leave: {
          dur: 1.05, avoid: overHead, reel: [[0, .3]],
          extra: (q, O, k) => mul(O.b, (O.climb ? -5 : -12) * bump(k * 1.5)),
          mood: (q, O) => ({ A: q < .3 ? Math.atan2(-O.b[1], -O.b[0]) : angTo(O.r, [0, 0]), flat: .7, tail: 1 - span(q, 0, .3) }),
        },
        tail: { n: 34, life: .3, speed: 55, wide: 2.2 },
      },
      {
        id: 'hammock', name: 'Гамак',
        idea: 'Растягивает сетку от плеча до пятки и дремлет в ней, покачиваясь за спиной.',
        time: 'Вяжет верх, ныряет к пятке и валится в сетку. Верх развязывается первым, он вываливается и виснет.',
        w: 14, damp: .72, max: 46, arrive: .5, out: [1.45, 2.05], caught: 1.4, net: true,
        near(q, O) {
          const top = from(O.pt('shoulderRear'), O, 13, 8), low = O.climb ? from(O.pt('heel'), O, 6, 13) : from(O.pt('heel'), O, 12, -1);
          return lerp2(top, low, span(q, .7, 1.25));
        },
        life(q, O) {
          const e = swayAt(q), mid = lerp2(O.pt('shoulderRear'), O.pt('heel'), .55);
          return O.climb ? from(mid, O, 9 + e, 14 + 2.5 * e) : from(mid, O, 16 + 1.8 * Math.abs(e), 3.2 * e);
        },
        threads: [
          { hold: () => 'shoulderRear', from: .3, hit: .7, tip: thrown(.3, .7), slack: () => 1.3, bow: O => unit(add(mul(O.b, 1), mul(O.n, O.climb ? 1 : .2))), wave: 0 },
          { hold: () => 'heel', from: 1.1, hit: 1.4, tip: thrown(1.1, 1.4, -4), slack: () => 1.1, bow: O => unit(add(mul(O.b, 1), mul(O.n, O.climb ? 1 : -.2))), wave: 0 },
        ],
        mood(q, O) {
          if (q < 1.5) return { A: angTo(O.r, O.pt(q < .8 ? 'shoulderRear' : 'heel')) };
          const e = span(q, 1.5, 2.1), peek = bump((frac(q / 3.4) - .72) / .2);
          return { A: OVERHEAD + .3 * swayAt(q), sc: lerp(1, .9, e), flat: lerp(1, .6, e), tilt: e * ((O.climb ? 1.1 : .55 * -O.R) + .2 * swayAt(q)), lid: e * (.95 - .85 * peek), pace: lerp(1, .35, e) };
        },
        leave: {
          dur: 1.1, avoid: underFeet, reel: [[0, .35], [.35, .7]],
          path(q, O, g0, E) {
            const hung = lerp2(g0, from(O.pt('heel'), O, 19, -4), span(q, 0, .4));
            return round(hung, E, span(q, .35, 1.1), underFeet(O));
          },
          mood: q => ({ lid: 1 - span(q, 0, .2), spin: TAU * span(q, .3, 1), pace: 2 }),
        },
      },
    ];

    // ---------- a candidate's flight, step by step ----------
    // how far into its hook's own time: in a climb the way round the body is longer, so it begins slower
    const ageOf = (e, t) => e.climb ? t - e.t0 - .5 * Math.tanh((t - e.t0) / 2) : t - e.t0;
    function caughtAt(e, caught) {
      let t = e.t0;
      while (ageOf(e, t) < caught - 1e-6) t += 1 / FPS;
      return t;
    }
    // flown round once first: at rest the hero still drifts up and down, and the drone with it
    function flightOf(cand, wait, last = null) {
      const out = [], moments = [], vis = { ...(last ? last.vis : REST), spin: 0 };
      let { at, v, pace, rate, wind } = last || { at: add(HOME, [0, -HERO_RUN[0].q.lift]), v: [0, 0], pace: [0, 0], rate: 1 / FLY.follow, wind: [-1, 0] };
      let ep = null, run = null, op = 0;
      const O = {
        pt: id => {
          const key = id + O.R, a = HERO_RUN[O.i].pts[key];
          return O.k ? lerp2(a, HERO_RUN[O.i + 1].pts[key], O.k) : a;
        },
      };
      function goalOf(e, t) {
        const q = ageOf(e, t);
        if (e.off !== null) {
          const u = t - e.off, E = add(HOME, mul(e.dir, e.climb ? 14 : 36)), L = cand.leave, k = c01(u / L.dur);
          if (L.path) return L.path(u, O, e.g0, E);
          const g = round(e.g0, E, sm(k), L.avoid(O));
          return L.extra ? add(g, L.extra(u, O, k)) : g;
        }
        if (q < cand.arrive) return round(e.p0, cand.near(q, O), span(q, 0, cand.arrive), (cand.avoid || frontOf)(O));
        return lerp2(cand.near(q, O), cand.life(q, O), span(q, cand.out[0], cand.out[1]));
      }
      function threadsOf(e, t) {
        const q = ageOf(e, t), held = e.off === null ? 1 : 1 - span(t - e.off, 0, .4);
        let ends = cand.threads.map(() => O.r);
        if (cand.bar) {
          // the skier's handle: two ends on a short bar before it
          const mid = lerp2(O.pt('beltRear'), O.pt('beltFore'), .5), k = span(q, cand.bar[0], cand.bar[1]) * held, to = unit(sub(mid, O.r));
          const side = mul(perp(to), Math.sign(dot(perp(mul(O.b, -1)), O.n)) * 2.5 * k), B = add(O.r, mul(to, Math.min(5.5, len(sub(mid, O.r)) * .25) * k));
          ends = [add(B, side), sub(B, side)];
        }
        return cand.threads.map((th, j) => {
          if (q < th.from) return null;
          const A = O.pt(th.hold(O)), z = ends[j];
          let tip = A, s = null, on = span(q, th.hit, th.hit + .2);
          if (e.off === null) {
            if (q < th.hit) tip = th.tip(q, O, z, A);
          } else {
            const was = e.was[j];
            if (!was) return null;
            const [r0, r1] = cand.leave.reel[j], u = t - e.off, k = span(u, r0, r1);
            if (k >= 1) return null;
            tip = add(lerp2(add(A, mul(was.by, 1 - k)), z, k), mul(th.arc ? th.arc(O) : mul(O.n, 3), bump(k)));
            s = was.s * (1 - k);
            on = 1 - span(u, r0, r0 + .15);
          }
          const d = len(sub(tip, z));
          if (s === null) s = lerp(.09 * d, th.slack(q, O, d), span(q, th.hit, th.hit + .45));
          s = Math.min(s, Math.max(0, cand.max - d));
          return { tip, z, d, s, L: d + s, bow: th.bow(O), wave: th.wave, held: on, by: sub(tip, A), hold: th.hold(O) + O.R, hit: e.off === null && q >= th.hit };
        });
      }

      for (let i = 0; i <= N; i++) {
        const H = HERO_RUN[i], t = i / FPS, speed = len(H.vel), dir = unit(H.vel), fast = speed >= .8 * JET.top;
        if (!fast) run = null;
        else if (!run || dot(run.dir, dir) < Math.cos(Math.PI / 15)) run = { dir, t };
        Object.assign(O, { i, k: 0, r: sub(at, H.pos) });
        if (ep && ep.off === null && (!fast || dot(ep.ref, dir) < Math.cos(Math.PI / 12.8))) {
          ep.was = threadsOf(ep, t);
          ep.off = t; ep.g0 = ep.g;
          ep.note.release = t;
        } else if (!ep && run && t - run.t >= wait - 1e-6) {
          const climb = Math.abs(dir[1]) > Math.abs(dir[0]), d = climb ? [0, Math.sign(dir[1])] : [Math.sign(dir[0]), 0];
          const R = climb ? -H.face : -d[0], note = { hook: t, climb };
          ep = { t0: t, off: null, ref: dir, dir: d, climb, R, b: mul(d, -1), n: climb ? [R, 0] : [0, -1], p0: O.r, g: O.r, note };
          note.catch = caughtAt(ep, cand.caught);
          moments.push(note);
          Object.assign(O, { R, climb, b: ep.b, n: ep.n });
        }
        if (ep && ep.off !== null && t - ep.off > cand.leave.dur + .3) { ep.note.free = t; ep = null; }
        const q = ep ? ageOf(ep, t) : 0, leaving = !!ep && ep.off !== null;
        const want = { ...REST, ...(!ep ? {} : leaving ? cand.leave.mood(t - ep.off, O) : cand.mood(q, O)) };
        const ease = 1 - Math.exp(-1 / FPS / .11);
        for (const key in REST) vis[key] = key === 'spin' ? want.spin : key === 'A' ? angLerp(vis.A, want.A, ease) : lerp(vis[key], want[key], ease);
        if (ep) wind = lerp2(wind, ep.b, ease);
        op += (vis.pace - 1) / FPS;
        out.push({
          r: O.r, v, vis: { ...vis }, op, wind, th: ep ? threadsOf(ep, t) : cand.threads.map(() => null),
          phase: !ep ? (fast ? 'straight' : 'free') : leaving ? 'leave' : q < cand.caught ? 'hook' : 'tow',
        });
        if (i === N) break;

        const G = HERO_RUN[i + 1], heroAcc = mul(sub(G.vel, H.vel), FPS), dt = 1 / FPS / SUB;
        for (let s = 1; s <= SUB; s++) {
          const k = s / SUB, pos = lerp2(H.pos, G.pos, k), vel = lerp2(H.vel, G.vel, k), now = t + s * dt;
          const anchor = add(pos, [0, OVER - lerp(H.q.lift, G.q.lift, k)]);
          // DroneFlight.Step
          const behind = Math.max(0, len(sub(at, anchor)) - FLY.homeX);
          rate += ((1 + Math.min(behind / FLY.far, FLY.hurry)) / FLY.follow - rate) * (dt / FLY.ease);
          pace = add(pace, mul(sub(vel, pace), dt * rate * FLY.follow / FLY.learn));
          let pull = add(mul(sub(add(anchor, PERCH), at), rate * rate), mul(sub(pace, v), 2 * rate));
          if (ep) {
            O.k = k; O.r = sub(at, pos);
            const g = goalOf(ep, now), gv = mul(sub(g, ep.g), 1 / dt);
            ep.g = g;
            const tow = add(heroAcc, add(mul(sub(g, O.r), cand.w * cand.w), mul(sub(gv, sub(v, vel)), 2 * cand.damp * cand.w)));
            const free = cand.leave.dur - .15;
            pull = lerp2(pull, tow, ep.off === null ? span(now - ep.t0, 0, .3) : 1 - span(now - ep.off, free, free + .4));
          }
          v = add(v, mul(pull, dt));
          at = add(at, mul(v, dt));
        }
      }
      if (!last) return flightOf(cand, wait, { at: sub(at, HERO_RUN[N].pos), v, pace, rate, wind, vis });
      return { frames: out, moments, op };
    }
    const TRACKS = new Map();
    function trackOf(cand, wait) {
      const key = cand.id + wait;
      if (!TRACKS.has(key)) TRACKS.set(key, flightOf(cand, wait));
      return TRACKS.get(key);
    }

    // a thread as drawn: bowed by its slack, a wave running along it
    function curveOf(th, t, buzz = 0) {
      const along = sub(th.tip, th.z), across = perp(unit(along)), side = clamp(dot(th.bow, across) * 2, -1, 1);
      const h = Math.sqrt(.375 * th.L) * th.s / Math.sqrt(th.s + 1.5) * side, wave = th.wave * Math.min(2.4, th.s * .5);
      return Array.from({ length: 17 }, (_, j) => {
        const u = j / 16, e = Math.sin(Math.PI * u);
        const off = h * 4 * u * (1 - u) + wave * e * Math.sin(u * 8 - t * 11) + buzz * .55 * e * Math.sin(t * 47);
        return [th.z[0] + along[0] * u + across[0] * off, th.z[1] + along[1] * u + across[1] * off];
      });
    }

    function sampleAt(cand, wait, time) {
      const lap = Math.floor(time / LOOP), u = time - lap * LOOP, f = u * FPS, i = Math.min(N - 1, Math.floor(f)), k = c01(f - i);
      const T = trackOf(cand, wait), a = T.frames[i], b = T.frames[i + 1], A = HERO_RUN[i], B = HERO_RUN[i + 1], vis = {};
      for (const key in REST) vis[key] = key === 'A' || key === 'spin' ? angLerp(a.vis[key], b.vis[key], k) : lerp(a.vis[key], b.vis[key], k);
      const th = a.th.map((p, j) => {
        const q = b.th[j];
        if (!p || !q) return p;
        return { ...p, tip: lerp2(p.tip, q.tip, k), z: lerp2(p.z, q.z, k), L: lerp(p.L, q.L, k), s: lerp(p.s, q.s, k), d: lerp(p.d, q.d, k), held: lerp(p.held, q.held, k) };
      });
      return {
        time, u, i, vis, th, cand, track: T, phase: a.phase, r: lerp2(a.r, b.r, k), wind: lerp2(a.wind, b.wind, k),
        op: time + lap * T.op + lerp(a.op, b.op, k),
        pos: add(mul(HERO_RUN[N].pos, lap), lerp2(A.pos, B.pos, k)), vel: lerp2(A.vel, B.vel, k), cam: lerp2(A.cam, B.cam, k),
        face: A.face, q: A.face === B.face ? mixPose(A.q, B.q, k) : A.q, hero: A,
      };
    }

    // ---------- the drone, drone-salts.js's candidate `flame` at rest ----------
    const SOOT = [38, 39, 43], PX = .3968, K1 = PX * Z, SPARKS = 120, SIDE = .95;
    const ORB = { r: 6.3, more: 1.4, wide: 1.05, flat: .3, tilt: -.3, tiltMore: 1.05, pace: .8, paceMore: .25, hurry: 2.2, line: .4, a: .12 };
    const FLAME = { coreA: 1.3, crackA: 1.3, aura: .22, orbA: 3, orbW: 1.4 }, CRACKS = [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]];
    const lum = c => (.2126 * c[0] + .7152 * c[1] + .0722 * c[2]) / 255, L0 = lum(EMBER), CREAM = pale(EMBER, .65);
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const burning = (C, q) => q < .25 ? EMBER : q < .45 ? mix3(EMBER, CREAM, (q - .25) / .2) : mix3(CREAM, C, sm((q - .45) / .3));
    const outOf = d => sm((d - 2.8) / 4.6);
    function inkOf(C) {
      const room = lum(C) <= L0 ? 1 : clamp(1 - (lum(C) - L0) * 2.3, .35, 1);
      return { C, gain: clamp(Math.sqrt(L0 / lum(C)), .85, 1.3), col: (q, p = 0) => p ? pale(burning(C, q), p * room) : burning(C, q) };
    }
    function orbitAt(o, th, vis, r1 = 0) {
      const rx = (ORB.r + o * ORB.more + r1) * ORB.wide * vis.sc, flat = ORB.flat * vis.flat, tl = ORB.tilt + o * ORB.tiltMore + vis.tilt + vis.spin;
      return { p: rot(Math.cos(th) * rx, Math.sin(th) * rx * flat, tl), d: unit(rot(-Math.sin(th), Math.cos(th) * flat, tl)) };
    }
    function stroke(c, pts, w, col, a) {
      if (a <= .004 || pts.length < 2) return;
      c.beginPath();
      pts.forEach(([x, y], i) => { if (i) c.lineTo(x, y); else c.moveTo(x, y); });
      c.lineWidth = w; c.strokeStyle = css(col, a); c.stroke();
    }
    function square(c, x, y, side, col, a) { if (a <= .004) return; c.fillStyle = css(col, a); c.fillRect(x - side / 2, y - side / 2, side, side); }
    function glowTail(c, K, p, d, side, step, heat, q) {
      const h = Math.min(1, heat);
      if (h <= .02) return;
      for (let m = 0; m < 3; m++) square(c, p[0] - d[0] * m * step, p[1] - d[1] * m * step, side, K.col(q, .3 * (1 - m / 3)), (.95 - m * .3) * h * K.gain);
    }
    function drawDroneSalts(c, K, st, D) {
      const k = K1, vis = st.vis, A = vis.A, at = q => [D[0] + q[0] * k, D[1] + q[1] * k], side = SIDE * k, step = 1.3 * k, list = [];
      for (let i = 0; i < SPARKS; i++) {
        const P = DR.P[i], o = i % 3, way = o === 1 ? -1 : 1, th = P.ph + st.op * (ORB.pace + ORB.paceMore * o) * way * ORB.hurry, O = orbitAt(o, th, vis, P.r1 - .5);
        const heat = hsh(i + 3) < .5 ? .55 + .3 * Math.sin(st.time * 11 + i * 7) : .2;
        list.push({ p: at(O.p), d: mul(O.d, way), heat, front: Math.sin(th) >= 0, q: outOf(len(O.p)) });
      }
      c.save();
      c.lineCap = 'round'; c.lineJoin = 'round'; c.globalCompositeOperation = 'source-over';
      for (const s of list) if (!s.front) square(c, s.p[0], s.p[1], side, SOOT, 1);
      const R = 5.04 * k;
      if (ready(IMG.core)) c.drawImage(IMG.core, D[0] - R, D[1] - R, 2 * R, 2 * R);
      else { c.beginPath(); c.arc(D[0], D[1], .75 * R, 0, TAU); c.fillStyle = css(SOOT); c.fill(); }
      for (const s of list) if (s.front) square(c, s.p[0], s.p[1], side, SOOT, 1);
      c.globalCompositeOperation = 'lighter';
      halo(c, D[0], D[1], 17 * k * vis.sc, K.C, FLAME.aura * K.gain);
      const levels = new Map();
      for (let o = 0; o < 3; o++) {
        let run = null, level = -1, last = null;
        for (let j = 0; j <= 36; j++) {
          const p = orbitAt(o, j / 36 * TAU, vis).p;
          if (last) {
            const lv = Math.round(outOf(len(lerp2(p, last, .5))) * 6);
            if (lv !== level) { level = lv; run = [at(last)]; if (!levels.has(lv)) levels.set(lv, []); levels.get(lv).push(run); }
            run.push(at(p));
          }
          last = p;
        }
      }
      for (const [lv, runs] of levels) {
        c.beginPath();
        for (const run of runs) run.forEach(([x, y], i) => { if (i) c.lineTo(x, y); else c.moveTo(x, y); });
        c.lineWidth = ORB.line * FLAME.orbW * k; c.strokeStyle = css(K.col(lv / 6), Math.min(1, ORB.a * FLAME.orbA * K.gain)); c.stroke();
      }
      for (const s of list) glowTail(c, K, s.p, s.d, side, step, s.heat, s.q);
      const fl = .75 + .25 * Math.sin(st.time * 7.3), shut = c01(vis.lid);
      halo(c, D[0], D[1], 8 * k, EMBER, Math.min(.8, .35 * FLAME.coreA * K.gain));
      c.beginPath();
      for (const [a, b] of CRACKS) { c.moveTo(D[0] + Math.cos(A + a) * .6 * k, D[1] + Math.sin(A + a) * .6 * k); c.lineTo(D[0] + Math.cos(A + b * .5) * 2.6 * k, D[1] + Math.sin(A + b * .5) * 2.6 * k); }
      c.lineWidth = .45 * k; c.strokeStyle = css(EMBER, Math.min(1, .5 * fl * FLAME.crackA * K.gain)); c.stroke();
      const E = at([Math.cos(A) * 1.386, Math.sin(A) * 1.386]);
      c.fillStyle = css(pale(EMBER, .5), .9 * fl);
      c.beginPath(); c.ellipse(E[0], E[1], 1.2 * k * (1 + .4 * shut), 1.2 * k * (1 - .82 * shut), 0, 0, TAU); c.fill();
      halo(c, E[0], E[1], 4 * k * (1 - .4 * shut), EMBER, .5 * fl * K.gain * (1 - .6 * shut));
      c.restore();
    }

    // ---------- what it trails ----------
    const frameBack = (st, age) => st.track.frames[clamp(Math.round((st.u - age) * FPS), 0, N)];
    function drawTail(c, K, st, S) {
      const spec = st.cand.tail;
      if (!spec) return;
      const side = SIDE * K1;
      for (let i = 0; i < spec.n; i++) {
        const f = frac(st.time / spec.life + hsh(i + 7)), age = f * spec.life, born = frameBack(st, age), a = born.vis.tail;
        if (a < .03) continue;
        const shift = age * spec.speed, flutter = (hsh(i + 91) - .5) * 2 * spec.wide * (.4 + f) + Math.sin(f * 9 + i) * .5;
        let p = add(add(born.r, mul(born.wind, shift)), mul(perp(born.wind), flutter));
        if (spec.spray) {
          // thrown out from the carve
          const was = frameBack(st, age + 1 / FPS);
          p = add(p, mul(sub(born.r, was.r), -spec.spray * FPS * age * hsh(i + 33)));
        }
        glowTail(c, K, S(p), born.wind, side, 1.6 * K1, a * (1 - f) * (.55 + .4 * hsh(i + 5)), sm(f * 2.2));
      }
    }
    function drawRibbon(c, K, st, S) {
      const rib = st.vis.rib;
      if (rib < .02) return;
      const t = st.time, pts = Array.from({ length: 17 }, (_, j) => {
        const u = j / 16, born = frameBack(st, u * .4), w = st.wind, p = add(add(lerp2(st.r, born.r, u), mul(w, u * 27)), mul(perp(w), 3.2 * u * Math.sin(u * 9 - t * 10)));
        return lerp2(st.r, p, rib);
      });
      const P = pts.map(S);
      for (let j = 1; j < P.length; j++) stroke(c, [P[j - 1], P[j]], 1.15, K.col(Math.min(1, j / 5)), .7 * rib * (1 - j / 22));
      for (const j of [6, 10, 14]) {
        const d = unit(sub(P[j + 1], P[j - 1])), o = mul(perp(d), 4.4 * rib), e = mul(d, 2.6 * rib), p = P[j];
        stroke(c, [p, add(add(p, o), e), sub(add(p, o), e), p, sub(sub(p, o), e), add(sub(p, o), e), p], 1.1, K.col(1), .75 * rib);
        square(c, p[0], p[1], 1.8, K.col(1, .3), rib);
      }
    }

    // ---------- the threads and what they hold ----------
    function drawThreads(c, K, st, S) {
      const curves = st.th.map(th => th && curveOf(th, st.time, st.vis.buzz));
      c.save();
      c.lineJoin = 'round'; c.lineCap = 'butt';
      for (const pts of curves) if (pts) { c.globalCompositeOperation = 'source-over'; stroke(c, pts.map(S), 2.4, [8, 10, 14], .4); }
      c.globalCompositeOperation = 'lighter';
      st.th.forEach((th, j) => {
        if (!th) return;
        const P = curves[j].map(S);
        stroke(c, P, 3.6, K.C, .07 * K.gain);
        for (let i = 1; i < P.length; i++) stroke(c, [P[i - 1], P[i]], 1.15, K.col(sm(Math.max(.02, th.L * (i - .5) / 16) / 6)), Math.min(1, .72 * K.gain));
        for (let m = 0; m < 3; m++) {
          const u = frac(st.time * .9 + m / 3 + j * .17) * 16, i = Math.min(15, Math.floor(u));
          glowTail(c, K, lerp2(P[i], P[i + 1], u - i), unit(sub(P[i + 1], P[i])), SIDE * K1, 1.3 * K1, .9 * Math.min(1, th.L / 5), sm(th.L * u / 16 / 6));
        }
        square(c, P[16][0], P[16][1], 2.1, K.col(1, .35), Math.min(1, th.L / 4));
        if (th.held < 1) halo(c, P[16][0], P[16][1], 6, K.C, .3 * Math.min(1, th.L / 6));
      });
      if (st.cand.bar && curves[0] && curves[1]) {
        const a = S(st.th[0].z), b = S(st.th[1].z), D = S(st.r);
        stroke(c, [a, b], 1.6, K.col(1), .8); stroke(c, [a, D, b], .8, K.col(.5), .5);
      }
      if (st.cand.net && curves[0] && curves[1]) {
        const held = Math.min(st.th[0].held, st.th[1].held);
        if (held > .02) {
          const rope = [...curves[0].slice().reverse(), ...curves[1].slice(1)], top = rope[0], low = rope[rope.length - 1], last = rope.length - 1;
          const inner = rope.map((p, i) => lerp2(p, lerp2(top, low, i / last), .24 * Math.sin(Math.PI * i / last)));
          stroke(c, inner.map(S), .9, K.col(.9), .5 * held);
          for (let i = 3; i < last - 1; i += 3) { stroke(c, [S(rope[i]), S(inner[i + 1])], .7, K.col(.9), .45 * held); stroke(c, [S(inner[i]), S(rope[i + 1])], .7, K.col(.9), .3 * held); }
        }
      }
      c.restore();
      return curves;
    }
    function drawLoops(c, K, st, J, S) {
      const th = st.th[0];
      if (!th || th.held < .02) return;
      const W = p => S(inWorld(p, st.q, st.face)), a = th.held * c01(1 - st.hero.off[th.hold] / 5);
      c.save();
      c.globalCompositeOperation = 'lighter'; c.lineCap = 'round';
      if (st.cand.id === 'kite') {
        const band = Array.from({ length: 9 }, (_, i) => { const u = i / 8 * 2 - 1; return W([HELM[0] + u * 3.9, HELM[1] - 5.1 + .9 * (1 - u * u)]); });
        stroke(c, band, 1.3, K.col(1), .85 * a);
      } else if (st.cand.id === 'ankle' || st.cand.wrap) {
        const d = unit(sub(J.legF[2], J.legF[1])), o = perp(d);
        for (const y of st.cand.wrap ? [.5, 3.6, 4.6, 5.7] : [4.5]) {
          const p = add(J.legF[1], mul(d, y)), ring = Array.from({ length: 7 }, (_, i) => { const u = i / 6 * 2 - 1; return W(add(p, add(mul(o, u * 2.4), mul(d, .8 * (1 - u * u) + .4 * u)))); });
          stroke(c, ring, 1.2, K.col(1), .85 * a);
        }
      }
      c.restore();
    }

    // ---------- the scene ----------
    const VIEW = [LW / 2, LH / 2];
    const DUST = (() => {
      const r = rng(17), make = (n, par, size, a) => Array.from({ length: n }, () => ({ x: r() * (LW + 80), y: r() * (LH + 80), par, size: size * (.6 + .6 * r()), a: a * (.4 + .6 * r()) }));
      return [...make(150, .012, 1.2, .55), ...make(45, .07, 1.5, .55), ...make(34, .38, 1.4, .4), ...make(22, .85, 1.5, .5)];
    })();
    function space(c, st, cam) {
      const g = c.createLinearGradient(0, 0, 0, LH);
      g.addColorStop(0, '#04060a'); g.addColorStop(1, '#0a0f17');
      c.fillStyle = g; c.fillRect(0, 0, LW, LH);
      for (const s of DUST) {
        const x = mod(s.x - cam[0] * Z * s.par, LW + 80) - 40, y = mod(s.y - cam[1] * Z * s.par, LH + 80) - 40, dx = st.vel[0] * Z * s.par * .022, dy = st.vel[1] * Z * s.par * .022;
        if (s.par > .05 && Math.hypot(dx, dy) > 1.2) stroke(c, [[x, y], [x + dx, y + dy]], s.size * .8, [196, 208, 224], s.a * .7);
        else square(c, x, y, s.size, [220, 228, 240], s.a);
      }
    }
    // the tracker on the near shin, drone-salts.js's
    function bracelet(c, h, C) {
      const k = h.P.legF[1], f = h.P.legF[2];
      c.save();
      c.translate(h.x, h.y); c.scale(h.face, 1); c.translate(k[0] * Z, k[1] * Z); c.rotate(Math.atan2(f[1] - k[1], f[0] - k[0]) - Math.PI / 2);
      c.translate(-24, -20.72);
      if (ready(IMG.strap)) c.drawImage(IMG.strap, 0, 0, 60, 68);
      rr(c, 15, 29, 22, 6, 2); c.fillStyle = css(C); c.fill();
      halo(c, 26, 32, 20, C, .3);
      c.restore();
    }
    function sayOf(st) {
      const u = st.u;
      if (st.phase === 'hook') return 'цепляется';
      if (st.phase === 'leave') return u >= AT.brake ? 'тормозит, спутник отпускает' : 'разворот, спутник отпускает';
      if (st.phase === 'tow') return u >= AT.twist && u < AT.back ? 'на нитке, герой оглянулся' : u >= AT.climb ? 'вверх, на нитке' : 'на нитке';
      if (u < AT.start) return 'стоит рядом';
      if (u < AT.start + 1.6) return 'старт, догоняет';
      if (u >= AT.brake) return u < AT.brake + 1.8 ? 'тормозит, догоняет' : 'стоит рядом';
      if (u >= AT.turn && u < AT.climb) return 'назад, догоняет';
      return u >= AT.climb ? 'вверх, летит прямо' : 'летит прямо';
    }
    function draw(T, st) {
      const c = T.c, col = CREW[OPT.colour], K = inkOf(col.rgb), cam = add(st.pos, st.cam);
      const S = p => [VIEW[0] + (p[0] - st.cam[0]) * Z, VIEW[1] + (p[1] - st.cam[1]) * Z];
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      space(c, st, cam);

      const h = T.me, J = jointsOf(st.q), M = S([0, -st.q.lift]);
      Object.assign(h, { x: M[0], y: M[1] + MID * Z, face: st.face, look: 0, P: J });
      c.save();
      c.translate(M[0], M[1]); c.rotate(st.face * st.q.tilt); c.translate(-M[0], -M[1]);
      drawHero(c, h);
      bracelet(c, h, col.rgb);
      c.restore();

      drawLoops(c, K, st, J, S);
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
      drawTail(c, K, st, S);
      drawRibbon(c, K, st, S);
      c.restore();
      const curves = drawThreads(c, K, st, S), D = S(st.r);
      drawDroneSalts(c, K, st, D);

      c.globalCompositeOperation = 'source-over';
      c.font = '13px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.fillStyle = 'rgba(190,198,208,.6)';
      c.fillText(`${st.u.toFixed(1).replace('.', ',')} с · ${sayOf(st)}${OPT.slow ? ' · ×¼' : ''}`, 12, LH - 12);
      T.dm = { u: st.u, phase: st.phase, D, r: st.r, hero: st.hero, curves };
    }

    // ---------- the clock, the address and the buttons ----------
    OPT.wait = 3; OPT.colour = 0;
    let clock = 0, waitWas = null, made = 0, swatched = false;
    const asked = typeof location === 'undefined' ? '' : location.hash.slice(1);
    for (const part of asked.split('&')) {
      const [key, v] = part.split('=');
      if (key === 'pause') OPT.playing = false;
      else if (key === 'slow') OPT.slow = true;
      else if (key === 'at' && isFinite(+v) && +v >= 0) clock = +v;
      else {
        const t = PAGE.toggles.find(q => q.key === key), opt = t && t.opts.find(o => String(o[1]) === v);
        if (opt) OPT[key] = opt[1];
      }
    }
    const seek = t => { clock = t; for (const T of tiles) T.time = t; };
    function swatches() {
      swatched = true;
      [...document.getElementById('colourMode').children].filter(b => String(b.tagName || b.tag).toLowerCase() === 'button').forEach((b, i) => { b.style.borderLeft = `10px solid ${CREW[i].hex}`; });
    }

    return {
      cands,
      expose: { LOOP, FPS, N, AT, CREW, EMBER, HERO_RUN, HELM_R, Z, curveOf, trackOf, seek },
      init(T) {
        T.num = made++; T.time = clock;
        T.me = newHero('me', LW / 2, 1.3);
      },
      // one clock for every tile; another wait starts the flight again, as the drone would jump
      scene(T) {
        if (!swatched) swatches();
        if (waitWas !== null && waitWas !== OPT.wait && OPT.playing) seek(0);
        waitWas = OPT.wait;
        clock = Math.max(clock, T.time); T.time = clock;
        draw(T, sampleAt(T.cand, OPT.wait, T.time));
      },
    };
  },

  // build.js --break <name>: the hold jumps on a twist; it snaps to its perch letting go; the kite flies into the helmet; the thread
  // is let out past its limit
  breaks: {
    jump: [['const SLIDE = .3;', 'const SLIDE = .001;']],
    head: [['const KITE = { back: 27, up: 19 };', 'const KITE = { back: 4, up: -6 };']],
    snap: [['ep.note.release = t;', 'ep.note.release = t; at = add(H.pos, HOME);']],
    long: [['const ANKLE = { out: 34, creep: 21 };', 'const ANKLE = { out: 58, creep: 21 };']],
  },

  // every candidate's whole flight at each wait, read frame by frame; a loop live; every colour drawn; a pause; a pick saved again
  check(K) {
    const X = K.X, WAITS = [2, 3, 5], TAU = Math.PI * 2;
    let ok = true;
    const said = new Set(), need = (c, m) => { if (c || said.has(m)) return; said.add(m); ok = false; console.log('  RULE', m); };
    const len = a => Math.hypot(a[0], a[1]), sub = (a, b) => [a[0] - b[0], a[1] - b[1]], add = (a, b) => [a[0] + b[0], a[1] + b[1]];
    // a frame's most, in world px; clear leaves the orbits' 3 px outside the helmet
    const MOST = { step: 6.5, dv: 48, hold: 2, length: 4.5, slack: .8, clear: X.HELM_R + 3.2, stray: .25 };
    const inVisor = (h, p) => {
      const d = sub(p, h.visor), a = -h.face * h.q.tilt, x = d[0] * Math.cos(a) - d[1] * Math.sin(a), y = d[0] * Math.sin(a) + d[1] * Math.cos(a);
      return (x / 3.7) ** 2 + (y / 3.3) ** 2 < 1;
    };

    const h0 = X.HERO_RUN[0], hN = X.HERO_RUN[X.N];
    need(len(sub(h0.cam, hN.cam)) < .2 && len(sub(h0.helm, hN.helm)) < .2 && len(hN.vel) === 0 && h0.face === hN.face, 'the hero and the camera close the loop');
    for (const t of K.tiles) for (const wait of WAITS) {
      const cand = t.cand, T = X.trackOf(cand, wait), F = T.frames, id = `${cand.id} at ${wait} s`;
      const got = { step: [0], dv: [0], hold: [0], length: [0], slack: [0], clear: [1e9], visor: [0], over: [0], stray: [0] };
      const most = (k, v, i) => { if (v > got[k][0]) got[k] = [v, i / X.FPS]; };
      const box = [1e9, 1e9, -1e9, -1e9];
      for (let i = 0; i <= X.N; i++) {
        const f = F[i], h = X.HERO_RUN[i], near = len(sub(f.r, h.helm));
        if (near < got.clear[0]) got.clear = [near, i / X.FPS];
        if (f.phase === 'tow') { box[0] = Math.min(box[0], f.r[0]); box[1] = Math.min(box[1], f.r[1]); box[2] = Math.max(box[2], f.r[0]); box[3] = Math.max(box[3], f.r[1]); }
        f.th.forEach((th, j) => {
          const was = i ? F[i - 1].th[j] : null;
          if (!th) { if (was) most('length', was.L, i); return; }
          most('over', Math.max(th.d, th.L) - cand.max, i);
          if (th.s < -1e-9) most('over', 1 - th.s, i);
          if (was) {
            most('length', Math.abs(th.L - was.L), i); most('slack', Math.abs(th.s - was.s), i);
            if (th.hit && was.hit) most('hold', len(sub(th.tip, was.tip)), i);
          } else most('length', th.L, i);
          if (X.curveOf(th, i / X.FPS, f.vis.buzz).some(p => inVisor(h, p))) most('visor', 1, i);
        });
        if (!i) continue;
        const p = F[i - 1], g = X.HERO_RUN[i - 1], moved = sub(add(h.pos, f.r), add(g.pos, p.r)), dv = len(sub(f.v, p.v));
        most('step', len(sub(f.r, p.r)), i);
        most('dv', dv, i);
        most('stray', len(sub(moved, [(f.v[0] + p.v[0]) / 2 / X.FPS, (f.v[1] + p.v[1]) / 2 / X.FPS])), i);
      }
      const say = k => `${got[k][0].toFixed(2)} at ${(got[k][1] || 0).toFixed(2)}`;
      const m = T.moments.map(q => `${q.climb ? 'climb' : 'level'} hook ${q.hook.toFixed(2)} catch ${q.catch.toFixed(2)} let go ${q.release === undefined ? '-' : q.release.toFixed(2)} free ${q.free === undefined ? '-' : q.free.toFixed(2)}`).join('; ');
      console.log(`${id}: ${m}`);
      console.log(`  step ${say('step')} | dv ${say('dv')} | hold ${say('hold')} | length ${say('length')} | slack ${say('slack')} | over ${say('over')} | helmet ${say('clear')} | towed in ${box.map(v => v.toFixed(0)).join(',')}`);
      need(got.step[0] <= MOST.step, `${id}: the drone jumps (${say('step')})`);
      need(got.stray[0] <= MOST.stray, `${id}: the drone moves by more than its velocity (${say('stray')})`);
      need(got.dv[0] <= MOST.dv, `${id}: the drone's velocity jumps (${say('dv')})`);
      need(got.hold[0] <= MOST.hold, `${id}: the thread's hold jumps (${say('hold')})`);
      need(got.length[0] <= MOST.length, `${id}: the thread's length jumps (${say('length')})`);
      need(got.slack[0] <= MOST.slack, `${id}: the slack jumps (${say('slack')})`);
      need(got.over[0] <= 1e-6, `${id}: the thread is past its ${cand.max} (${say('over')})`);
      need(got.clear[0] >= MOST.clear, `${id}: the drone is on the helmet (${say('clear')})`);
      need(got.visor[0] === 0, `${id}: a thread crosses the visor (${say('visor')})`);
      const [a, b] = T.moments;
      need(T.moments.length === 2 && !a.climb && b.climb, `${id}: it hooks in the level flight and in the climb, once each`);
      for (const q of T.moments) need(q.release !== undefined && q.release > q.catch + 1 && q.free !== undefined, `${id}: it catches and lets go`);
      if (a && b) {
        need(a.release - a.catch >= 4, `${id}: the first tow is ${(a.release - a.catch).toFixed(2)} s, not 4`);
        need(a.catch + .5 <= X.AT.twist && a.release >= X.AT.turn, `${id}: it is towed through the twist till the turn`);
      }
      const end = F[X.N];
      need(len(sub(end.r, F[0].r)) < .3 && len(sub(end.v, F[0].v)) < 1 && end.th.every(th => !th), `${id}: the loop closes without a jump`);
      need(Math.abs(Math.atan2(Math.sin(end.vis.A - F[0].vis.A), Math.cos(end.vis.A - F[0].vis.A))) < .05 * TAU, `${id}: the eye closes the loop`);
    }

    // ---------- live: a loop at the default wait, the toggles, a pause, the pick ----------
    const L = X.LOOP * X.FPS, P = L + 60, E = P + 90, used = K.tiles.map(() => new Set()), docs = [];
    const per = K.tiles.map(() => ({ hook: 0, tow: 0, leave: 0, threads: 0, paused: 0, last: -1, off: 0, offAt: 0 }));
    K.tiles.forEach((t, j) => {
      const take = v => { if (typeof v === 'string' && v.startsWith('rgba(')) used[j].add(v.slice(0, v.lastIndexOf(',') + 1)); };
      t.c = new Proxy(t.c, {
        get: (o, k) => o[k],
        set(o, k, v) { if (k === 'fillStyle' || k === 'strokeStyle') take(v); o[k] = v; return true; },
      });
    });
    let shown = '', status = '', copied = '', colours = 0, apart = 0;
    K.at(0, () => { shown = K.byId.waitMode.children.filter(b => b.listeners.click).map(b => b.classList.s.has('on') ? b.textContent : '').join(''); });
    X.CREW.forEach((col, i) => K.at(L + 2 + i * 4, () => { for (const s of used) s.clear(); K.seg('colourMode', i); X.seek(8); }));
    K.at(P, () => K.seg('playMode', 1));
    K.at(P + 30, () => K.seg('playMode', 0));
    K.at(P + 36, () => { K.click(0, '.pick'); });
    K.at(P + 40, () => { docs.push(K.picked()); K.seg('waitMode', 2); });
    K.at(P + 44, () => { docs.push(K.picked()); status = K.byId.status.textContent; K.byId.copy.listeners.click[0](); copied = K.byId.fallback.value; });
    const [W, H] = [720, 540], EDGE = 10;
    K.run(E, (t, frame) => {
      const j = K.tiles.indexOf(t), q = per[j], d = t.dm;
      if (!d) return;
      if (frame < L) {
        if (q[d.phase] !== undefined) q[d.phase]++;
        if (d.curves.some(Boolean)) q.threads++;
        const out = Math.max(EDGE - d.D[0], d.D[0] - (W - EDGE), EDGE - d.D[1], d.D[1] - (H - EDGE));
        if (out > q.off) { q.off = out; q.offAt = d.u; }
        if (j && Math.abs(t.time - K.tiles[0].time) > 2 / 60) apart++;
      }
      X.CREW.forEach((col, i) => {
        if (frame !== L + 4 + i * 4) return;
        if (used[j].has(`rgba(${col.rgb.join(',')},`) && used[j].has(`rgba(${X.EMBER.join(',')},`)) colours++;
      });
      if (frame > P && frame < P + 29) { if (q.last >= 0 && t.time !== q.last) K.fail(`${t.cand.id}: time ran while paused`); q.paused++; }
      q.last = t.time;
    });
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id;
      console.log(`${id}: drawn hook ${(q.hook / X.FPS).toFixed(1)} s, tow ${(q.tow / X.FPS).toFixed(1)} s, let go ${(q.leave / X.FPS).toFixed(1)} s, out of the tile ${q.off.toFixed(0)} px at ${q.offAt.toFixed(1)}`);
      need(q.hook > 30 && q.tow > 240 && q.leave > 30 && q.threads > 240, `${id}: hook, tow and letting go are drawn`);
      need(q.off <= 0, `${id}: the drone leaves the tile at ${q.offAt.toFixed(1)}`);
      need(q.paused > 20, `${id}: the pause held`);
    });
    need(apart === 0, 'every tile is at the same moment of the loop');
    need(shown === '3 с', `three seconds is the default (${shown})`);
    need(colours === X.CREW.length * K.tiles.length, `every colour burns beside the ember (${colours})`);
    const first = K.tiles[0].cand;
    console.log('docs:', docs.map(d => JSON.stringify(d)).join(' | '), '| copy:', copied);
    [3, 5].forEach((wait, i) => need(docs[i] && docs[i].pick === first.id && docs[i].wait === wait && Object.keys(docs[i]).sort().join() === 'pick,wait', `pick ${i}: the way and the wait saved, nothing else`));
    need(status.includes(`«${first.name}»`) && status.includes('5 с') && copied.includes(`(${first.id})`) && copied.includes('5 с'), 'the status and the copy text name both');
    return ok;
  },
};
