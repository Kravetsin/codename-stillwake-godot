// How the hero's companion drone shows its hero running out of air, in place of the red pulse: five ways by motion, number and shape,
// and the shipped pulse beside them. The drone is the shipped «Нити» (DroneInk) in «Соли в пламени»; the breath is VisorFog's, so the
// drone and the visor breathe as one. An art-pick page on tools/pick-kit (README.md).
// For a frame: index.html#at=<s into the loop>&colour=<index>&bg=space|room&pause
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-air.js <out-dir> [--break red]
const HD = 'game/art/hd/';

module.exports = {
  title: 'Спутник без воздуха',
  heading: 'Спутник без воздуха',
  window: false,
  tile: [620, 472],
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Выбери, <b>как спутник показывает, что у его героя кончается воздух</b>. Сейчас это красная пульсация, её почти не видно. В новых вариантах красного нет: говорят движение, число и форма, одинаково при любом цвете игрока.</p>
<p class="lead">В каждой плитке два места сразу. Крупный герой — это ты вблизи. <b>Маленький в дальнем углу — друг</b>, как на экране 1080p при обычном, самом дальнем виде. Смотри на него: вариант должен читаться и там. Точный размер даёт «Крупно 1:1» при масштабе браузера 100%.</p>
<p class="lead">Круг у всех общий: покой 3 с; воздух от 25% до нуля за 12 с, <b>примерно как в игре в обычном шлеме</b> (в покое без шлема около 5&nbsp;с, в тяжёлом около 25&nbsp;с, на бегу быстрее); последний вдох, настоящие 10&nbsp;с; мгновение без сознания; повтор, где воздух вернулся посреди последнего вдоха. Дышит спутник в такт стеклу шлема. Полоска над сценой только для страницы.</p>
<p class="miss">Не показано: звук; свет Godot (свечение выйдет чуть иначе); полёт спутника следом за бегущим героем; мысли и режимы спутника рядом с этим знаком; туман на стекле. На стекле последние вдохи затихают совсем, здесь только слабеют, чтобы их было видно. Цвет игрока здесь пока макет «Соли в пламени».</p>`,
  toggles: [
    { id: 'momentMode', bar: 2, label: 'Момент:', key: 'moment', opts: [['покой', 0], ['воздух кончается', 1], ['последний вдох', 2], ['без сознания', 3], ['повтор', 4], ['воздух вернулся', 5]] },
    { id: 'colourMode', bar: 2, label: 'Цвет:', key: 'colour', opts: [['бирюза', 0], ['синий', 1], ['розовый', 2], ['зелёный', 3], ['лайм', 4]] },
    { id: 'bgMode', bar: 1, label: 'Слева:', key: 'bg', opts: [['космос', 'space'], ['светлый отсек', 'room']] },
  ],
  pick: { doc: 'picks/droneAir', subject: 'Спутник без воздуха', what: ['вариант', 'вариантом'] },
  pictures: {
    core: HD + 'drone_core.png', strap: HD + 'hero_tracker.png', wall: HD + 'back_alloy.png',
    bush: HD + 'planet/bush_0.png', clouds: HD + 'planet/clouds.png',
  },

  page() {
    const TAU = Math.PI * 2, hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
    const frac = v => v - Math.floor(v);
    const hsh = i => frac(Math.sin(i * 127.1 + 311.7) * 43758.5453);
    const mix3 = (a, b, k) => k <= 0 ? a : k >= 1 ? b : [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];

    // ---------- DroneInk's numbers; about the drone everything is in its pick px, W of them a world px ----------
    const PX = .3968, W = 1 / PX, N = 120, SIDE = .95;
    const ORB = { r: 6.3, more: 1.4, wide: 1.05, flat: .3, tilt: -.3, tiltMore: 1.05, pace: .8, paceMore: .25, hurry: 2.2 };
    const CRACKS = [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]];
    const RED = [255, 58, 40], SOOT = [38, 39, 43], COAL = EMBER;

    // ---------- «Соли в пламени», as drone-colour.js drew it ----------
    const COLOURS = ['#5fc8c0', '#5a8ef0', '#e88cc8', '#8cd06a', '#d8ec54'].map(hex);
    const lum = c => (.2126 * c[0] + .7152 * c[1] + .0722 * c[2]) / 255, L0 = lum(EMBER);
    const CREAM = pale(EMBER, .65);
    function saltsOf(C) {
      const room = lum(C) <= L0 ? 1 : clamp(1 - (lum(C) - L0) * 2.3, .35, 1), gain = clamp(Math.sqrt(L0 / lum(C)), .85, 1.3);
      const flame = q => q < .25 ? COAL : q < .45 ? mix3(COAL, CREAM, (q - .25) / .2) : mix3(CREAM, C, sm((q - .45) / .3));
      return { C, room, gain, flame, salts: true };
    }
    const SALTS = COLOURS.map(saltsOf);
    const outOf = d => sm((d - 2.8) / 4.6);   // how far out of the coal a point is, for the salts

    // ---------- the loop ----------
    const STAGES = [
      { id: 'calm', len: 3, look: 1.5, say: 'покой' },
      { id: 'low', len: 12, look: 8.6, say: 'воздух кончается: 12 с' },
      { id: 'last', len: 10, look: 4, say: 'последний вдох: 10 с, как в игре' },
      { id: 'out', len: 2.5, look: 1.8, say: 'без сознания' },
      { id: 'again', len: 3, look: 1.5, say: 'повтор: снова последний вдох' },
      { id: 'refill', len: 3, look: .25, say: 'воздух вернулся' },
    ];
    const AT = {};
    let LOOP = 0;
    for (const s of STAGES) { s.t0 = LOOP; AT[s.id] = LOOP; LOOP += s.len; }
    const stageAt = u => { let i = 0; while (i + 1 < STAGES.length && u >= STAGES[i + 1].t0) i++; return i; };
    const REPLAY_DRY = 2;   // the replay opens this far into a last breath

    // ---------- the breath: VisorFog.Advance, run once over the loop; a frame can then be opened at any second ----------
    const FOG = { Calm: 3.2, Quick: .95, GaspFirst: 1.3, GaspSlows: 1.8, Evaporating: 2.4, ComeS: .7, EvaporateS: 2.4, LastBreathS: 10 };
    const DT = 1 / 120;
    const track = [], OUT_STARTS = [];
    (() => {
      let phase = 0, count = 0, depth = 0, on = 0, go = -1, dryS = 0, was = '';
      for (let i = 0; i <= Math.round(LOOP / DT); i++) {
        const u = i * DT, S = STAGES[Math.min(stageAt(u + 1e-6), STAGES.length - 1)], id = S.id, p = c01((u - S.t0) / S.len);
        if (id !== was) {
          if (id === 'low') { on = 0; depth = 0; }
          if (id === 'again') { on = 1; depth = 1; dryS = REPLAY_DRY; phase = .5; }
          if (id === 'refill') go = 0;
          was = id;
        }
        const fogged = id !== 'calm' && id !== 'refill', dry = id === 'last' || id === 'again', out = id === 'out';
        const share = id === 'calm' ? lerp(.32, .25, p) : id === 'low' ? .25 * (1 - p) : id === 'refill' ? sm((u - S.t0) / 1) : 0;
        if (go >= 0) go = Math.min(1, go + DT / FOG.EvaporateS);
        else if (fogged) {
          on = Math.min(1, on + DT / FOG.ComeS);
          const target = out ? 1 : c01((.25 - share) / .25), step = DT / 1;
          depth = depth < target ? Math.min(target, depth + step) : Math.max(target, depth - step);
        }
        dryS = dry ? dryS + DT : 0;
        const last = c01(dryS / FOG.LastBreathS);
        const period = out ? FOG.Calm : dry ? FOG.GaspFirst + FOG.GaspSlows * last : go >= 0 ? FOG.Evaporating : FOG.Calm + (FOG.Quick - FOG.Calm) * depth;
        phase += DT / period;
        if (Math.floor(phase) !== count) {
          count = Math.floor(phase);
          if (fogged && !out) OUT_STARTS.push({ u, period, d: depth, last, dry, id, again: id === 'again' });
        }
        track.push({ share, depth, on: 1 - Math.pow(1 - on, 3), last, period, phase });
      }
    })();

    function stateOf(time) {
      const u = ((time % LOOP) + LOOP) % LOOP, si = stageAt(u), S = STAGES[si], id = S.id, p = track[Math.round(u / DT)] || track[track.length - 1];
      const out = id === 'out', r = id === 'refill' ? u - AT.refill : -1, dry = id === 'last' || id === 'again';
      const live = id === 'low' || dry;
      // 0 at the end of a breath out, 1 at the end of a breath in; the visor's flattening would lose the last gasps, so they keep 0.7
      const gone = out ? sm((u - AT.out) / .5) : 0, ebb = out ? track[Math.round(AT.out / DT) - 1].phase : p.phase;
      const inn = live || out ? (.5 + .5 * Math.cos(ebb * TAU)) * (1 - gone) : 0, amp = dry ? 1 - .3 * p.last : out ? .7 : 1;
      return {
        time, u, si, id, say: S.say, share: p.share, period: p.period, phase: p.phase,
        on: id === 'calm' ? 0 : r >= 0 ? 1 - sm(r / 2.6) : p.on, d: id === 'calm' ? 0 : r >= 0 ? 1 : p.depth,
        inn, beat: inn * amp, live, dry, last: dry ? p.last : out ? 1 : 0,
        out, gone, r, replay: u >= AT.again, sunk: id === 'last' ? sm((u - AT.last) / .9) : dry || out ? 1 : 0,
        fallen: out ? Math.pow(c01((u - AT.out) / .45), 2) : 0, follows: out ? sm((u - AT.out) / .9) : 0,
      };
    }

    // the gasp the air comes back in: a recovery starts from there
    const CAUGHT = stateOf(AT.refill - DT);

    // ---------- the swarm, DroneSwarm's idle one ----------
    const tilts = o => ORB.tilt + o * ORB.tiltMore;
    function ellipseAt(o, th, way, g, more = 0) {
      const s = g.s ?? 1, level = g.level || 0, rx = (ORB.r + o * ORB.more + more) * ORB.wide * s * (1 + .25 * level);
      const flat = lerp(g.flat ?? ORB.flat, .04, level), tilt = lerp(tilts(o) + (g.turn || 0), o === 2 ? Math.PI : 0, level);
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * flat, tilt), v = rot(-Math.sin(th) * way, Math.cos(th) * flat * way, tilt), d = Math.hypot(v[0], v[1]) || 1;
      // a drooping orbit hangs off the hero's back, like cloth
      const droop = g.droop ? g.droop * q[0] * q[0] / 110 * (q[0] * g.back > 0 ? 1.7 : .25) : 0;
      return [q[0], q[1] + droop, v[0] / d, v[1] / d, Math.sin(th)];
    }
    function orbit(i, t, g = {}) {
      const P = DR.P[i], o = i % 3, way = o === 1 ? -1 : 1;
      return ellipseAt(o, P.ph + t * (ORB.pace + ORB.paceMore * o) * way * ORB.hurry, way, g, P.r1 - .5);
    }
    const idleHeat = (i, t) => hsh(i + 3) < .5 ? .55 + .3 * Math.sin(t * 11 + i * 7) : .2;
    const rankBy = key => { const r = []; Array.from({ length: N }, (_, i) => i).sort((a, b) => key(a) - key(b)).forEach((i, k) => { r[i] = k / N; }); return r; };
    const addTo = (p, b) => [p[0] + b[0], p[1] + b[1], p[2], p[3], p[4]];
    function idleSparks(X, b, g = {}, bright = 0, side = SIDE) {
      return Array.from({ length: N }, (_, i) => ({ p: addTo(orbit(i, X.t, g), b), h: idleHeat(i, X.t) + bright, side }));
    }

    function path(c, pts, shut) {
      c.beginPath();
      pts.forEach((p, i) => { if (i) c.lineTo(p[0], p[1]); else c.moveTo(p[0], p[1]); });
      if (shut) c.closePath();
    }
    function stroke(c, pts, w, col, a, shut) {
      if (a <= .004) return;
      path(c, pts, shut); c.lineWidth = w; c.strokeStyle = css(col, a); c.stroke();
    }
    function fill(c, pts, col, a) {
      if (a <= .004 || pts.length < 3) return;
      path(c, pts, true); c.fillStyle = css(col, a); c.fill();
    }

    // a thread ember by the coal and the colour at its ends
    function saltThread(c, K, g, o, at, width, alpha) {
      const levels = new Map();
      let last = null;
      for (let i = 0; i <= 28; i++) {
        const p = ellipseAt(o, i / 28 * TAU, 1, g);
        if (last) { const lv = Math.round(outOf(Math.hypot((p[0] + last[0]) / 2, (p[1] + last[1]) / 2)) * 6); if (!levels.has(lv)) levels.set(lv, []); levels.get(lv).push([last, p]); }
        last = p;
      }
      for (const [lv, pieces] of levels) {
        c.beginPath();
        for (const [a, b] of pieces) { c.moveTo(at[0] + a[0], at[1] + a[1]); c.lineTo(at[0] + b[0], at[1] + b[1]); }
        c.lineWidth = width; c.strokeStyle = css(K.flame(lv / 6), Math.min(1, alpha)); c.stroke();
      }
    }

    // the drone: the darks, the core and what is laid over them, then all that is added, in LightDrone's order
    function body(c, K, X, P) {
      const [bx, by] = P.at, s = P.core ?? 1, lit = P.lit ?? 1, g = P.orbit || {}, list = P.sparks;
      const darks = front => { for (const q of list) if ((q.p[4] >= 0) === front) c.fillRect(q.p[0] - q.side / 2, q.p[1] - q.side / 2, q.side, q.side); };
      c.globalCompositeOperation = 'source-over';
      if (P.under) P.under();
      c.fillStyle = css(SOOT); darks(false);
      const R = 5.04 * s;
      if (ready(IMG.core)) c.drawImage(IMG.core, bx - R, by - R, 2 * R, 2 * R);
      if (P.over) P.over();
      c.fillStyle = css(SOOT); darks(true);

      c.globalCompositeOperation = 'lighter';
      for (let o = 0; o < 3; o++) {
        const a = P.rings ? P.rings[o] : 1;
        if (a <= .01) continue;
        if (K.salts) { saltThread(c, K, g, o, P.at, .56, .36 * a * K.gain); continue; }
        path(c, Array.from({ length: 41 }, (_, i) => addTo(ellipseAt(o, i / 40 * TAU, 1, g), P.at)));
        c.lineWidth = .4; c.strokeStyle = css(K.hot, .12 * a); c.stroke();
      }
      const aura = P.aura ?? 1;
      if (K.salts) halo(c, bx, by, 17, K.C, .22 * K.gain * aura);
      for (const q of list) {
        const h = Math.min(1, q.h);
        if (h <= .02) continue;
        const col = K.salts ? K.flame(outOf(Math.hypot(q.p[0] - bx, q.p[1] - by))) : K.hot, room = K.salts ? K.room : 1, gain = K.salts ? K.gain : 1;
        for (let m = 0; m < 3; m++) {
          c.fillStyle = css(pale(col, .3 * (1 - m / 3) * room), (.95 - m * .3) * h * gain);
          c.fillRect(q.p[0] - q.p[2] * m * 1.3 - q.side / 2, q.p[1] - q.p[3] * m * 1.3 - q.side / 2, q.side, q.side);
        }
      }
      const core = K.salts ? COAL : K.hot, more = K.salts ? 1.3 * K.gain : 1, fl = (.75 + .25 * Math.sin(X.t * 7.3)) * lit, A = P.A ?? X.A;
      halo(c, bx, by, 8 * s, core, Math.min(.8, .35 * more * (.4 + .6 * lit)));
      c.beginPath();
      for (const [a, b] of CRACKS) { c.moveTo(bx + Math.cos(A + a) * .6 * s, by + Math.sin(A + a) * .6 * s); c.lineTo(bx + Math.cos(A + b * .5) * 2.6 * s, by + Math.sin(A + b * .5) * 2.6 * s); }
      c.lineWidth = .45 * s; c.strokeStyle = css(core, Math.min(1, .5 * fl * more)); c.stroke();
      const ex = bx + Math.cos(A) * 1.386 * s, ey = by + Math.sin(A) * 1.386 * s;
      c.fillStyle = css(pale(core, .5), Math.min(1, .9 * fl)); c.beginPath(); c.arc(ex, ey, 1.2 * s, 0, TAU); c.fill();
      halo(c, ex, ey, 4 * s, core, Math.min(1, .5 * fl));
      if (P.light) P.light();
      c.globalCompositeOperation = 'source-over';
    }
    const calmBody = (c, K, X) => body(c, K, X, { at: X.b, sparks: idleSparks(X, X.b) });

    // ---------- 1. «Уголь задыхается»: the swarm clenches and spreads, goes out, and the bare core flares on each gasp ----------
    const CHOKE_RANK = rankBy(i => hsh(i + 70)), CHOKE_DIES = CHOKE_RANK.map(p => AT.low + .8 + 11 * Math.pow(p, .75));
    const RAYS = Array.from({ length: 9 }, (_, i) => ({ a: (i + .7 * hsh(i + 900)) / 9 * TAU, len: .55 + .65 * hsh(i + 950) }));
    function choke(c, K, X) {
      const b = X.b, sparks = [];
      let s, flat, flare, alive = 1, threads = 0, lit = 1, turn = 0;
      if (X.r >= 0) {
        const pop = Math.sin(Math.PI * c01(X.r / .8)), settled = sm((X.r - .3) / 2.3);
        s = sm(X.r / .16) * lerp(1 + 2.2 * pop, 1, settled); flat = lerp(.65, ORB.flat, settled); flare = Math.exp(-X.r * 2.6);
        threads = pop;
        for (let i = 0; i < N; i++) sparks.push({ p: addTo(orbit(i, X.t, { s, flat }), b), h: lerp(1, idleHeat(i, X.t), settled), side: lerp(1.7, SIDE, settled) });
      } else {
        const deep = .65 + .35 * X.d, wide = sm(X.inn);
        s = lerp(1, lerp(1 - .78 * deep, 1 + 1.3 * deep, wide), X.on); flat = lerp(ORB.flat, .25 + .5 * wide, X.on); turn = .5 * wide * X.on;
        threads = X.on * wide;
        let n = 0;
        for (let i = 0; i < N; i++) {
          if (X.replay) continue;
          const age = X.u - CHOKE_DIES[i];
          if (age > .35) continue;
          const out = age > 0 ? 1 - age / .35 : 1;
          sparks.push({ p: addTo(orbit(i, X.t, { s: s * out, flat, turn }), b), h: (age > 0 ? 1 : idleHeat(i, X.t) + X.on * (.6 * wide - .08)) * out, side: SIDE * (1 + X.on * (.5 + 1.1 * X.d)) * out });
          if (age <= 0) n++;
        }
        alive = n / N;
        // the gasp blows on the bare coal: sharp up, quick down
        flare = X.dry || X.out ? Math.pow(X.beat, 1.8) * X.sunk : 0;
        lit = X.dry || X.out ? lerp(lerp(1, .55 + .45 * flare, X.sunk), .22, X.gone) : 1;
      }
      const core = X.dry || X.out ? lerp(1 + .6 * flare, .85, X.gone) : X.r >= 0 ? 1 + .4 * flare : lerp(1, lerp(.8, 1.15, sm(X.inn)), X.on);
      body(c, K, X, {
        at: b, sparks, core, lit, orbit: { s, flat, turn }, rings: [alive, alive, alive], aura: alive,
        over() {
          if (flare <= .02) return;
          // tongues laid, not added, so that they hold on a light sky too
          // each gasp's star stands its own way; it turns at the end of a breath out, when it is gone
          const turn1 = Math.floor(X.phase + .5) * 1.3, long = 24 * flare;
          for (const q of RAYS) {
            const a = q.a + turn1, ux = Math.cos(a), uy = Math.sin(a), to = 2.5 + long * q.len, w = 2.3;
            const ray = [[b[0] + ux * 2 - uy * w, b[1] + uy * 2 + ux * w], [b[0] + ux * to, b[1] + uy * to], [b[0] + ux * 2 + uy * w, b[1] + uy * 2 - ux * w]];
            stroke(c, ray, 1.5, SOOT, .7 * flare);
            fill(c, ray, COAL, .95 * Math.min(1, flare * 2));
            const tip = .55 * to;
            fill(c, [[b[0] + ux * tip - uy * .7, b[1] + uy * tip + ux * .7], [b[0] + ux * to, b[1] + uy * to], [b[0] + ux * tip + uy * .7, b[1] + uy * tip - ux * .7]], K.C, flare);
          }
          c.fillStyle = css(pale(COAL, .55), Math.min(1, flare * 1.4)); c.beginPath(); c.arc(b[0], b[1], 1 + 3.4 * flare, 0, TAU); c.fill();
        },
        light() {
          if (threads > .02) for (let j = 0; j < sparks.length; j += 4) stroke(c, [b, sparks[j].p], .45, K.flame(.7), .22 * threads);
          if (flare > .02) halo(c, b[0], b[1], 8 + 26 * flare, COAL, .5 * flare);
        },
      });
    }

    // ---------- 2. «Осыпается пеплом»: the sparks are the gauge; each falls away as ash ----------
    const ASH_RANK = rankBy(i => (2 - i % 3) + hsh(i + 130) * .95), ASH_DIES = ASH_RANK.map(p => AT.low + .5 + 11.4 * p);
    const CRUMBS = OUT_STARTS.filter(e => e.dry).flatMap((e, k) => [0, 1, 2, 3, 4].map(j => ({ born: e.u + j * .17, i: 200 + k * 5 + j, again: e.again })));
    const FLOOR = 46 * W, ASH = [22, 22, 25], ASH_TOP = [172, 164, 154];
    // where a flake is, age s after it left the swarm, about the standing hero's anchor
    function fallen(from, i, age, X) {
      const a = hsh(i + 410), b = hsh(i + 440), eased = age - (1 - Math.exp(-2.5 * age)) / 2.5;
      if (X.ground === 'space') {
        const way = Math.PI + (a - .5) * 1.7 * X.face, go = (10 + 11 * b) * eased;
        return { x: from[0] + Math.cos(way) * go * X.face, y: from[1] + Math.sin(way) * go + 5 * eased * (b - .3), turn: age * (1 + 2 * a), k: 1 - sm((age - 1.8) / 2.4) };
      }
      const speed = 34 + 18 * b, rest = FLOOR - 1.5, y = from[1] + speed * eased, landed = y >= rest;
      const fallS = (rest - from[1]) / speed + .4, moving = Math.min(age, fallS);
      return { x: from[0] + (a - .5) * 22 * (1 - Math.exp(-moving)) - X.face * 14 * moving + 2.5 * Math.sin(moving * 3 + i), y: Math.min(y, rest), turn: landed ? 0 : age * (2 + 2 * a), k: 1 - sm((age - fallS - .7) / 1.3) };
    }
    function flake(c, X, from, i, age, size) {
      const F = fallen(from, i, age, X);
      if (F.k <= .01) return;
      const x = F.x - X.shift[0], y = F.y - X.shift[1], p = [[-1, -.2], [-.3, -.8], [1, -.4], [.6, .6], [-.5, .7]].map(([a, b]) => { const q = rot(a * size / 2, b * size / 2, F.turn); return [x + q[0], y + q[1]]; });
      fill(c, p, ASH, .96 * F.k);
      fill(c, [p[0], p[1], p[2]], mix3(COAL, ASH_TOP, sm((age - .4) / 1.5)), .95 * F.k);
    }
    function ash(c, K, X) {
      const b = X.b, sparks = [], streaks = [], count = [0, 0, 0];
      let core = 1, lit = 1, heart = 0, g = {};
      if (X.r >= 0) {
        for (let i = 0; i < N; i++) {
          const start = .5 * hsh(i + 5), k = sm((X.r - start) / .6), O = addTo(orbit(i, X.t), b), way = (hsh(i + 9) - .5) * 1.5, far = 25 + 55 * hsh(i);
          const from = X.ground === 'space' ? [b[0] - X.face * far * Math.cos(way), b[1] + far * Math.sin(way)] : [b[0] - X.face * (10 + 40 * hsh(i + 9)), b[1] + 20 + 1.1 * far];
          // up first, then in: a curve, not a straight rain
          const on = q => [lerp(from[0], O[0], q * q), lerp(from[1], O[1], q)], p = on(k), was = on(Math.max(0, k - .16)), d = Math.hypot(p[0] - was[0], p[1] - was[1]);
          if (k < 1 && d > .5) streaks.push([was, p]);
          sparks.push({ p: k < 1 ? [p[0], p[1], d > 1e-6 ? (p[0] - was[0]) / d : 0, d > 1e-6 ? (p[1] - was[1]) / d : -1, 1] : O, h: lerp(1, idleHeat(i, X.t), sm((X.r - 1) / 1.6)), side: lerp(2.1, SIDE, sm((X.r - .6) / 1.6)) });
          count[i % 3] += k;
        }
        core = lerp(.81, 1, sm(X.r / .6));
      } else {
        g = { s: lerp(1, lerp(.8, 1.3, sm(X.inn)), X.on) };
        if (!X.replay) for (let i = 0; i < N; i++) {
          if (X.u >= ASH_DIES[i]) continue;
          const dying = sm((X.u - (ASH_DIES[i] - .3)) / .3);
          sparks.push({ p: addTo(orbit(i, X.t, g), b), h: idleHeat(i, X.t) + X.on * (.2 + .25 * X.d) + dying, side: SIDE * (1 + X.on * (.4 + .9 * X.d)) });
          count[i % 3]++;
        }
        if (X.dry || X.out) { core = lerp(1, .62, X.last); heart = X.beat * X.sunk; }
        lit = X.out ? lerp(1, .22, X.gone) : 1;
      }
      body(c, K, X, {
        at: b, sparks, core, lit, orbit: g, rings: count.map(v => v / 40), aura: (count[0] + count[1] + count[2]) / N,
        under() {
          if (X.r < 0 && !X.replay) for (let i = 0; i < N; i++) {
            const age = X.u - ASH_DIES[i];
            if (age < 0 || age > 7) continue;
            flake(c, X, orbit(i, ASH_DIES[i] + X.seed), i, age, 4.2 + 2.4 * hsh(i + 33));
          }
          if (X.r < 0) for (const q of CRUMBS) {
            const age = X.u - q.born;
            if (q.again !== X.replay || age < 0 || age > 7) continue;
            flake(c, X, [(hsh(q.i) - .5) * 5, 1], q.i, age, 7 + 2.6 * hsh(q.i + 33));
          }
        },
        over() {
          if (heart <= .02) return;
          // the crust gone, the ember heart shows through
          fill(c, Array.from({ length: 7 }, (_, i) => { const a = i / 7 * TAU, r = (1 + 3 * heart) * (i % 2 ? .6 : 1.1); return [b[0] + Math.cos(a) * r, b[1] + Math.sin(a) * r]; }), pale(COAL, .3 * heart), .95);
        },
        light() { for (const q of streaks) stroke(c, q, .9, K.C, .5); },
      });
    }

    // ---------- 3. «Лёгкие»: the threads draw lungs over it, breathing and emptying from the top ----------
    const LOBE_DRAWN = [[.18, -.5], [.22, -.68], [.34, -.8], [.5, -.78], [.67, -.58], [.82, -.26], [.92, .12], [.97, .48], [.94, .74], [.8, .86], [.58, .78], [.34, .64], [.17, .6], [.14, .38], [.22, .14], [.14, -.12], [.18, -.5]];
    // its corners cut twice, so the thread runs round
    const rounded = p => p.slice(0, -1).flatMap((a, i) => [lerp2(a, p[i + 1], .25), lerp2(a, p[i + 1], .75)]);
    const shut = p => p.concat([p[0]]), LOBE = shut(rounded(shut(rounded(LOBE_DRAWN))));
    const mirror = p => [-p[0], p[1]], LOBES = [LOBE, LOBE.map(mirror)];
    const BRANCH = [[0, -.45], [.14, -.3], [.34, -.14], [.5, .12]], PIPES = [[[0, -1.08], [0, -.45]], BRANCH, BRANCH.map(mirror)];
    const LUNG_SIZE = 27, LUNG_Y = -51;
    function within(poly, x, y) {
      let odd = false;
      for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) if ((poly[i][1] > y) !== (poly[j][1] > y) && x < (poly[j][0] - poly[i][0]) * (y - poly[i][1]) / (poly[j][1] - poly[i][1]) + poly[i][0]) odd = !odd;
      return odd;
    }
    const AIR_POINTS = (() => {
      const r = rng(77), out = [];
      while (out.length < N) { const x = .14 + r() * .84, y = -.8 + r() * 1.66; if (within(LOBE, x, y)) out.push(out.length % 2 ? [-x, y] : [x, y]); }
      return out;
    })();
    function below(poly, level) {
      const out = [];
      for (let i = 0; i < poly.length; i++) {
        const a = poly[i], b = poly[(i + 1) % poly.length], ia = a[1] >= level, ib = b[1] >= level;
        if (ia) out.push(a);
        if (ia !== ib) out.push(lerp2(a, b, (level - a[1]) / (b[1] - a[1])));
      }
      return out;
    }
    function lungs(c, K, X) {
      const b = X.b;
      let form, full, w, h, shown = 1, bright = 1;
      if (X.r >= 0) {
        const puff = Math.sin(Math.PI * c01(X.r / 1));
        const k = sm(X.r / .15), was = sm(CAUGHT.inn) * (1 - .3 * CAUGHT.last);
        form = 1 - sm((X.r - 1.7) / .9); full = sm(X.r / .5); w = lerp(lerp(.62, 1.2, was), 1 + .4 * puff, k); h = lerp(lerp(.8, 1.1, was), 1 + .18 * puff, k);
      } else {
        form = X.on; full = 1 - X.d;
        const wide = sm(X.inn) * (X.dry ? 1 - .3 * X.last : X.out ? .7 : 1);
        w = lerp(.62, 1.2, wide); h = lerp(.8, 1.1, wide);
        if (X.dry) bright = .75 + .25 * X.beat;
        if (X.out) { w = lerp(w, .4, X.gone); h = lerp(h, .5, X.gone); shown = lerp(1, .5, X.gone); bright = lerp(.75, .45, X.gone); }
      }
      // it grows out of the coal as a thought does, hung from its windpipe's top
      const at = ([x, y]) => { const px = x * LUNG_SIZE * w, py = LUNG_Y + LUNG_SIZE * (-1.08 + (y + 1.08) * h); return [b[0] + px * form, b[1] + py * form]; };
      const level = lerp(.86, -.95, full), sparks = [], count = [0, 0, 0];
      for (let i = 0; i < N; i++) {
        const O = addTo(orbit(i, X.t), b), k = sm((form - hsh(i + 50) * .3) / .7);
        if (i % 5 === 0 || k <= 0) {
          const dim = i % 5 === 0 ? lerp(1, .35 + .65 * full, form) : 1;
          sparks.push({ p: O, h: idleHeat(i, X.t) * dim * (X.out ? 1 - .7 * X.gone : 1), side: SIDE }); count[i % 3]++;
          continue;
        }
        const q = AIR_POINTS[i], left = sm((q[1] - level) / .12), swim = [Math.sin(X.t * 4 + i) * 1.1, Math.cos(X.t * 3.3 + i) * 1.1];
        if (left <= .02 && k >= .98) continue;
        const L = at(q), p = lerp2(O, [L[0] + swim[0], L[1] + swim[1]], k);
        sparks.push({ p: [p[0], p[1], 0, 0, 1], h: lerp(idleHeat(i, X.t), .85 + .15 * Math.sin(X.t * 13 + i), k) * lerp(1, left, k), side: lerp(SIDE, 1.5, k) });
      }
      body(c, K, X, {
        at: b, sparks, lit: X.out ? lerp(1, .3, X.gone) : 1, rings: count.map(v => clamp(v / 40 * (form > .5 ? 2 : 1), 0, 1)), aura: lerp(1, .5, form),
        under() {
          const a = form * shown;
          if (a <= .02) return;
          c.lineCap = 'round'; c.lineJoin = 'round';
          if (full > .015) for (const L of LOBES) fill(c, below(L, level).map(at), K.C, .5 * a);
          const lines = [...LOBES, ...PIPES].map(L => L.map(at));
          // a dark line under the thread, or a pale colour is lost on the day's sky
          lines.forEach((L, j) => stroke(c, L, j === 2 ? 5.6 : 3.9, SOOT, .6 * a));
          lines.forEach((L, j) => stroke(c, L, j === 2 ? 3.4 : 2.1, pale(K.C, .1), a * Math.max(.45, bright)));
        },
        light() { if (form > .02) for (const L of [...LOBES, ...PIPES]) stroke(c, L.map(at), .6, K.C, .4 * form * shown * bright); },
      });
    }

    // ---------- 4. «Чадит»: smoke with each breath out, the flame shrinking ----------
    const PUFFS = OUT_STARTS.flatMap((e, k) => {
      const n = 6 + Math.round(6 * e.d);
      return Array.from({ length: n }, (_, j) => ({ born: e.u + j / n * .48 * e.period, i: k * 14 + j, d: e.d, again: e.again, thin: e.dry ? 1 - .3 * e.last : 1 }));
    });
    // the replay opens mid-breath: smoke of the gasp before it still hangs
    for (let j = 0; j < 7; j++) PUFFS.push({ born: AT.again - 1.1 + j * .12, i: 600 + j, d: 1, again: true, thin: 1 });
    const SMOKE = [44, 42, 43], SMOKE_TOP = [146, 137, 128];
    function smoke(c, K, X) {
      const b = X.b, sparks = [], count = [0, 0, 0];
      let tall, share, rise, s, lit = 1;
      const sputter = X.live && hsh(Math.floor(X.time * 13) + 800) < .1 + .4 * X.d ? .4 : 1;
      if (X.r >= 0) {
        const settled = sm((X.r - .4) / 2.1);
        tall = 33 * sm(X.r / .14) * (1 - settled); share = 1; rise = 1 - settled; s = lerp(1.2, 1, settled);
      } else {
        tall = X.on * lerp(lerp(34, 10, X.d) * lerp(.6, 1.2, X.inn), 13 * Math.pow(X.beat, 3), X.sunk) * sputter;
        share = X.out ? .05 : X.dry ? .12 : 1 - .88 * X.d * X.on; rise = X.out ? 0 : X.on; s = lerp(1, .6, X.d * X.on);
        lit = X.dry || X.out ? lerp(lerp(sputter, .35 + .65 * X.beat, X.sunk), .2, X.gone) : sputter;
      }
      for (let i = 0; i < N; i++) {
        if (hsh(i + 610) > share) continue;
        const O = addTo(orbit(i, X.t, { s }), b);
        if (i % 4 === 0 || rise <= .01) { sparks.push({ p: O, h: idleHeat(i, X.t) * lit, side: SIDE }); count[i % 3]++; continue; }
        const q = frac(X.t * (1 + hsh(i + 620)) + hsh(i + 630)), x = (hsh(i + 640) - .5) * 11 * (1 - q) + Math.sin(q * 7 + i) * 1.4;
        const p = lerp2(O, [b[0] + x, b[1] - 2 - q * (tall * 1.1 + 4)], rise);
        sparks.push({ p: [p[0], p[1], 0, -1, 1], h: lerp(idleHeat(i, X.t), (1 - q) * sputter, rise), side: lerp(SIDE, 1.5 * (1 - .8 * q), rise) });
      }
      body(c, K, X, {
        at: b, sparks, lit, orbit: { s }, rings: count.map(v => Math.min(1, v / 40 * (rise > .5 ? 3 : 1)) * lit), aura: lit * share,
        under() {
          for (const q of PUFFS) {
            const age = X.u - q.born;
            if (q.again !== X.replay || age < 0 || age > 2.8) continue;
            const a = hsh(q.i + 710), e = hsh(q.i + 740), gone = X.r >= 0 ? 1 - sm(X.r / .9) : 1, blown = X.r >= 0 ? 70 * sm(X.r / .9) : 0;
            let x, y;
            if (X.ground === 'space') { const way = -Math.PI / 2 + (a - .5) * 2.2, go = (30 + 14 * e) * (1 - Math.exp(-age * 1.1)); x = Math.cos(way) * go - X.face * 6 * age; y = Math.sin(way) * go - 4; }
            else { x = (a - .5) * 10 * Math.min(1, age * 2) - (X.ground === 'planet' ? 8 : 1) * age * age + 3 * Math.sin(age * 2 + q.i); y = -4 - (24 + 8 * e) * age; }
            x -= X.shift[0] + blown * X.face; y -= X.shift[1];
            const size = (2.5 + (5.5 + 4 * q.d) * Math.sqrt(age / 2.8)) * (.75 + .5 * e), alpha = (.42 + .3 * q.d) * q.thin * Math.min(1, age * 7) * Math.pow(1 - age / 2.8, 1.2) * gone;
            if (alpha <= .01) continue;
            const puff = (ox, oy, r) => Array.from({ length: 11 }, (_, j) => { const an = j / 11 * TAU + a * 6 + age * (a - .5), far = r * (.72 + .5 * hsh(q.i * 11 + j)); return [x + ox + Math.cos(an) * far * 1.15, y + oy + Math.sin(an) * far * .85]; });
            fill(c, puff(0, 0, size), SMOKE, alpha);
            fill(c, puff(-size * .2, -size * .3, size * .6), mix3(COAL, SMOKE_TOP, sm(age / .6)), alpha * .75);
          }
        },
        over() {
          if (tall <= .6) return;
          const sway = Math.sin(X.t * 9) * 1.5, wide = Math.min(8, 2.4 + tall * .2);
          const tongue = k => {
            const pts = [], top = [b[0] + sway * k, b[1] - 2 - tall * k];
            for (let j = 0; j <= 8; j++) pts.push(bez([b[0] - wide * .7 * k, b[1]], [b[0] - wide * 1.5 * k, b[1] - tall * .45 * k], top, j / 8));
            for (let j = 1; j <= 8; j++) pts.push(bez(top, [b[0] + wide * 1.5 * k, b[1] - tall * .45 * k], [b[0] + wide * .7 * k, b[1]], j / 8));
            pts.push([b[0], b[1] + 2.4 * k]);
            return pts;
          };
          fill(c, tongue(1), K.C, .8); fill(c, tongue(.68), COAL, .95); fill(c, tongue(.36), pale(COAL, .6), .95);
        },
        light() { if (tall > .6) halo(c, b[0], b[1] - tall * .3, 5 + tall * .5, COAL, .3); },
      });
    }

    // ---------- 5. «Теряет силы»: the body sags with each breath out and heaves itself up with each breath in ----------
    function cling(c, K, X) {
      let drop, level, droop, heat = 0, side = SIDE, lit = 1, A = X.A, x;
      if (X.r >= 0) {
        const up = sm(X.r / .35), over = Math.sin(Math.PI * c01((X.r - .12) / 1.3)), lift = 4.5 * Math.pow(CAUGHT.inn, 1.6) * (1 - .6 * CAUGHT.last);
        drop = (15.5 - lift) * (1 - up) - 12 * over; level = (1 - .1 * lift) * (1 - sm(X.r / .6)); droop = 8 * level; heat = 1 - sm((X.r - .4) / 2.2);
      } else {
        // a heave up is hard work, a fall is quick
        const up = Math.pow(X.inn, 1.6);
        if (X.out) { drop = lerp(15.5, 10.5, X.follows); level = 1; droop = 8; lit = lerp(.6, .25, X.gone); }
        else {
          const lift = 4.5 * up * (1 - .6 * X.last) * (1 - sm((X.last - .85) / .15));
          drop = lerp(X.on * lerp(lerp(8, 15.5, X.d), lerp(-4, 2, X.d), up), 15.5 - lift, X.sunk);
          level = lerp(X.on * lerp(1, .15, up) * lerp(.8, 1, X.d), 1 - .1 * lift, X.sunk);
          droop = lerp(level * lerp(3, 8, X.d), 8, X.sunk);
        }
        heat = X.live ? X.on * (.3 * up * (1 - X.last) - .15) : 0; side = lerp(SIDE, 1.3, X.out ? .5 : X.on);
        if (X.live) lit = 1 - X.on * (1 - up) * (.2 + .2 * X.d);
        A = X.A + (X.face > 0 ? 1.5 : -1.5) * level;
      }
      // behind the helmet on the way down, then in onto the shoulder
      x = drop <= 0 ? 0 : drop < 8 ? -9 * sm(drop / 8) : lerp(-9, -6.5, sm((drop - 8) / 7.5));
      if (X.out) x = lerp(x, -3, X.follows);
      const bob = 1 - c01(drop / 6), b = [x * X.face * W, drop * W + X.b[1] * bob], g = { level, droop, back: -X.face, s: X.r >= 0 ? 1 + .9 * Math.sin(Math.PI * c01(X.r / 1.2)) : 1 };
      const sparks = Array.from({ length: N }, (_, i) => ({ p: addTo(orbit(i, X.t, g), b), h: (idleHeat(i, X.t) + heat) * lit, side }));
      const a = (1 + .5 * level) * lit;
      body(c, K, X, { at: b, sparks, orbit: g, lit, A, aura: lit, rings: [a, a, a] });
    }

    // ---------- 6. the shipped pulse: DroneWill.Tint and LightDrone.Hot, the embers without a player's colour ----------
    function now(c, K, X) {
      const phase = frac(X.time / 1.2), tint = X.live && phase < .4 ? Math.sin(Math.PI * phase / .4) : 0;
      const shipped = { salts: false, hot: mix3(EMBER, RED, Math.min(1, tint * 1.4)) };
      body(c, shipped, X, { at: X.b, sparks: idleSparks(X, X.b, {}, tint * .55) });
    }

    const breathes = draw => (c, K, X) => X.id === 'calm' ? calmBody(c, K, X) : draw(c, K, X);
    const cands = [
      {
        id: 'choke', name: 'Уголь задыхается',
        idea: 'Рой дышит: сжимается в комок на выдохе, раскрывается на вдохе, всё чаще. Искры гаснут вместе с воздухом. На последнем вдохе остаётся голый уголь и вспыхивает на каждом вдохе.',
        time: 'Воздух вернулся: весь рой вспыхивает разом.',
        draw: breathes(choke),
      },
      {
        id: 'ash', name: 'Осыпается пеплом',
        idea: 'Искры и есть шкала. Они гаснут по одной и падают тёмными хлопьями, в космосе уплывают прочь. Орбиты пустеют снаружи внутрь. На последнем вдохе крошится сам уголь, и видно его сердце.',
        time: 'Воздух вернулся: пепел взлетает обратно искрами.',
        draw: breathes(ash),
      },
      {
        id: 'lungs', name: 'Лёгкие',
        idea: 'Нити рисуют над спутником лёгкие, как его мысли. Они дышат, а искры в них убывают сверху вниз. На последнем вдохе остаётся пустой контур. Самый прямой знак.',
        time: 'Воздух вернулся: лёгкие наполняются снизу за полсекунды и расходятся.',
        draw: breathes(lungs),
      },
      {
        id: 'smoke', name: 'Чадит',
        idea: 'Уголь горит язычком, и тот всё меньше, с перебоями. На каждом выдохе из него идёт дым, всё гуще и чаще. На последнем вдохе огня почти нет, только дым.',
        time: 'Воздух вернулся: пламя встаёт столбом, дым сносит.',
        draw: breathes(smoke),
      },
      {
        id: 'cling', name: 'Теряет силы',
        idea: 'Говорит тело: на выдохе спутник проседает, на вдохе с трудом поднимается, всё ниже. Орбиты ложатся плашмя и свисают. На последнем вдохе он лежит у героя на плече.',
        time: 'Воздух вернулся: он вспархивает выше своего места.',
        draw: breathes(cling),
      },
      {
        id: 'now', name: 'Сейчас: красный пульс',
        idea: 'Как в игре сейчас, для сравнения: вспышка красного раз в 1,2 с. Спутник целиком оранжевый, как сейчас в игре, поэтому выбор цвета на него не действует. Без сознания пульса нет.',
        time: 'Воздух вернулся: пульс просто кончается.',
        draw: now,
      },
    ];

    // ---------- the world: two grounds, each a hero near and a friend far, screen px ----------
    const HEAD_H = 46, SCENE = { w: 308, h: 406, floor: 394, ledge: 190, ledgeW: 118, x: [0, 312] };
    const WHO = [
      { id: 'friend', x: 56, y: SCENE.ledge, z: 1.5, face: 1, seed: 4.7, hero: newHero('friend', 0, 4.7) },
      { id: 'me', x: 203, y: SCENE.floor, z: Z, face: -1, seed: 0, hero: newHero('me', 0, 0) },
    ];
    for (const q of WHO) { const h = q.hero; h.x = h.y = 0; h.face = q.face; h.P = poseOf(h); }

    function backSpace(g) {
      const sky = g.createLinearGradient(0, 0, 0, SCENE.h);
      sky.addColorStop(0, '#04060a'); sky.addColorStop(1, '#0a0f17');
      g.fillStyle = sky; g.fillRect(0, 0, SCENE.w, SCENE.h);
      for (let i = 0; i < 130; i++) { const s = hsh(i + 60) < .12 ? 1.5 : .8; g.fillStyle = css([220, 228, 240], .2 + .55 * hsh(i + 90)); g.fillRect(hsh(i) * SCENE.w, hsh(i + 30) * SCENE.floor, s, s); }
      hull(g);
    }
    function hull(g) {
      for (const [x, y, w, h] of [[0, SCENE.floor, SCENE.w, SCENE.h - SCENE.floor], [0, SCENE.ledge, SCENE.ledgeW, 8]]) {
        g.fillStyle = '#2f3138'; g.fillRect(x, y, w, h);
        g.fillStyle = '#4f525b'; g.fillRect(x, y, w, 2);
        for (let a = x + 18; a < x + w; a += 34) { g.fillStyle = INK; g.fillRect(a, y + 2, 1.5, h - 2); }
      }
    }
    function backRoom(g) {
      g.fillStyle = '#1b2327'; g.fillRect(0, 0, SCENE.w, SCENE.h);
      if (ready(IMG.wall)) {
        const walls = () => { for (let x = 0; x < SCENE.w; x += 256) for (let y = 0; y < SCENE.h; y += 128) g.drawImage(IMG.wall, x, y, 256, 128); };
        walls();
        g.save(); g.globalCompositeOperation = 'lighter'; g.globalAlpha = .7; walls(); g.restore();
      }
      g.save(); g.globalCompositeOperation = 'lighter';
      const light = g.createRadialGradient(170, 12, 6, 170, 12, 380);
      light.addColorStop(0, css([214, 172, 116], .36)); light.addColorStop(1, css([214, 172, 116], 0));
      g.fillStyle = light; g.fillRect(0, 0, SCENE.w, SCENE.h);
      g.restore();
      g.fillStyle = '#25282d'; g.fillRect(146, 0, 48, 12);
      g.fillStyle = '#e9cfa4'; g.fillRect(152, 9, 36, 4);
      hull(g);
    }
    function backPlanet(g) {
      const sky = g.createLinearGradient(0, 0, 0, SCENE.floor);
      [[0, [94, 112, 128]], [.25, [121, 139, 151]], [.5, [160, 171, 169]], [.75, [209, 203, 181]], [1, [221, 214, 190]]].forEach(([q, col]) => sky.addColorStop(q, css(col)));
      g.fillStyle = sky; g.fillRect(0, 0, SCENE.w, SCENE.h);
      if (ready(IMG.clouds)) { g.save(); g.globalAlpha = .7; g.drawImage(IMG.clouds, -220, 8, 1024, 80); g.restore(); }
      for (const [y0, h, col, f] of [[328, 14, '#8f9c8c', .013], [360, 10, '#66796a', .024]]) {
        const pts = [[0, SCENE.h]];
        for (let x = 0; x <= SCENE.w + 8; x += 8) pts.push([Math.min(x, SCENE.w), y0 + h * Math.sin(x * f + y0)]);
        pts.push([SCENE.w, SCENE.h]);
        path(g, pts, true); g.fillStyle = col; g.fill();
      }
      path(g, [[0, SCENE.ledge], [SCENE.ledgeW, SCENE.ledge], [SCENE.ledgeW - 12, SCENE.ledge + 13], [62, SCENE.ledge + 20], [26, SCENE.ledge + 44], [0, SCENE.ledge + 50]], true);
      g.fillStyle = '#4b5148'; g.fill();
      g.fillStyle = '#3f5b43'; g.fillRect(0, SCENE.ledge, SCENE.ledgeW, 3);
      if (ready(IMG.bush)) g.drawImage(IMG.bush, 236, SCENE.floor - 48, 100, 52);
      g.fillStyle = '#2b2925'; g.fillRect(0, SCENE.floor, SCENE.w, SCENE.h - SCENE.floor);
      g.fillStyle = '#3f5b43'; g.fillRect(0, SCENE.floor, SCENE.w, 4);
    }
    const BACKS = { space: backSpace, room: backRoom, planet: backPlanet };
    function backdrop(T) {
      const key = `${OPT.bg}|${T.S}|${Object.values(IMG).filter(ready).length}`;
      if (T.bgKey !== key) {
        T.bgKey = key;
        if (!T.bgc) T.bgc = document.createElement('canvas');
        T.bgc.width = Math.ceil(LW * T.S); T.bgc.height = Math.ceil(SCENE.h * T.S);
        const g = T.bgc.getContext('2d');
        [OPT.bg, 'planet'].forEach((ground, p) => {
          g.setTransform(T.S, 0, 0, T.S, SCENE.x[p] * T.S, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over';
          g.save(); g.beginPath(); g.rect(0, 0, SCENE.w, SCENE.h); g.clip();
          BACKS[ground](g);
          g.restore();
        });
      }
      return T.bgc;
    }

    // the rig at this who's zoom, on its back once passed out
    function figure(c, q, C, fallenBy) {
      const h = q.hero, k = q.z / Z;
      c.save();
      c.translate(q.x + q.face * 19 * q.z * fallenBy, q.y - 4.5 * q.z * fallenBy);
      if (fallenBy > 0) c.rotate(-q.face * Math.PI / 2 * fallenBy);
      c.scale(k, k);
      drawHero(c, h);
      const n = h.P.legF[1], f = h.P.legF[2];
      c.scale(h.face, 1); c.translate(n[0] * Z, n[1] * Z); c.rotate(Math.atan2(f[1] - n[1], f[0] - n[0]) - Math.PI / 2); c.translate(-24, -20.72);
      if (ready(IMG.strap)) c.drawImage(IMG.strap, 0, 0, 60, 68);
      rr(c, 15, 29, 22, 6, 2); c.fillStyle = css(C); c.fill();
      c.restore();
    }
    function companion(T, q, ground, st, colour) {
      const c = T.c;
      // DroneFlight.Anchor: 16 over the neck; lying, 16 over the body's middle
      const stand = -(RIG.Neck + 16), lies = -(4.5 + 16), y = lerp(stand, lies, st.follows);
      const X = {
        ...st, t: st.time + q.seed, seed: q.seed, face: q.face, ground,
        A: q.face > 0 ? -.6 : Math.PI + .6,
        b: [0, W * Math.sin(TAU * st.time / 1.8)], shift: [0, (y - stand) * W],
      };
      c.save();
      c.translate(q.x, q.y + y * q.z); c.scale(PX * q.z, PX * q.z);
      c.lineCap = 'round'; c.lineJoin = 'round';
      T.probe = `${ground}.${q.id}`;
      T.cand.draw(c, SALTS[colour], X);
      T.probe = null;
      c.restore();
    }
    function legend(T, st) {
      const c = T.c, font = 'system-ui,"Segoe UI",sans-serif';
      c.fillStyle = '#10141a'; c.fillRect(0, 0, LW, HEAD_H); c.fillRect(0, HEAD_H + SCENE.h, LW, LH - HEAD_H - SCENE.h);
      c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.font = `600 14px ${font}`; c.fillStyle = '#e3e0d8'; c.fillText(`${st.si + 1}/${STAGES.length} · ${st.say}`, 10, 21);
      c.font = `12px ${font}`; c.fillStyle = '#9a968c'; c.textAlign = 'right';
      const left = Math.max(0, 10 * (1 - st.last));
      c.fillText(`${OPT.slow ? '×¼ · ' : ''}${st.dry ? `воздуха нет, ещё ${left.toFixed(0)} с` : `воздух ${Math.round(st.share * 100)}%`}`, LW - 10, 21);
      c.fillStyle = '#272d36'; c.fillRect(10, 31, LW - 20, 6);
      c.fillStyle = '#8cc4ff'; c.fillRect(10, 31, (LW - 20) * st.share, 6);
      if (st.dry) { c.fillStyle = '#8b9199'; c.fillRect(10, 39, (LW - 20) * (1 - st.last), 2); }
      c.fillStyle = '#e3e0d8'; c.fillRect(10 + (LW - 20) * .25, 28, 1, 12);
      const y = HEAD_H + SCENE.h + 14;
      c.textAlign = 'left'; c.fillStyle = '#9a968c';
      c.fillText(OPT.bg === 'room' ? 'светлый отсек' : 'космос', 10, y); c.fillText('планета днём', SCENE.x[1] + 10, y);
    }

    // a frame straight from the address: #at=8.5&colour=2&bg=room&pause
    let clock = 0;
    OPT.moment = -1;   // a button jumps: none is the default
    function address(text) {
      for (const part of text.split('&')) {
        const [key, v] = part.split('=');
        if (key === 'pause') OPT.playing = false;
        else if (key === 'at' && isFinite(+v) && +v >= 0) clock = +v;
        else if (key === 'colour' || key === 'bg') { const t = PAGE.toggles.find(q => q.key === key).opts.find(o => String(o[1]) === v); if (t) OPT[key] = t[1]; }
      }
    }
    if (typeof location !== 'undefined') address(location.hash.slice(1));
    function seek(t) { clock = t; for (const T of tiles) T.time = t; }
    function moments(si, T, dt) {
      if (OPT.moment >= 0) {
        const s = STAGES[OPT.moment];
        // paused, a jump opens on the moment worth seeing
        const to = s.t0 + (OPT.playing ? 0 : s.look);
        seek(to);
        // the tiles after this one have still to step this frame
        for (const q of tiles) if (q !== T) q.time = to - dt;
        OPT.moment = -1;
        return true;
      }
      // each frame: the kit's refresh clears the mark
      [...document.getElementById('momentMode').children].filter(b => String(b.tagName || b.tag).toLowerCase() === 'button').forEach((b, i) => b.classList.toggle('on', i === si));
      return false;
    }

    return {
      cands,
      expose: { STAGES, AT, LOOP, RED, COLOURS, FOG, track, DT, stateOf, seek, address, clock: () => clock },
      init(T) { T.time = clock; },
      // one clock for every tile
      scene(T, dt) {
        clock = Math.max(clock, T.time); T.time = clock;
        let st = stateOf(clock);
        if (moments(st.si, T, dt)) { st = stateOf(clock); moments(st.si, T, dt); }
        const c = T.c;
        world(T);
        c.drawImage(backdrop(T), 0, HEAD_H, LW, SCENE.h);
        [OPT.bg, 'planet'].forEach((ground, p) => {
          c.save();
          c.translate(SCENE.x[p], HEAD_H);
          c.beginPath(); c.rect(0, 0, SCENE.w, SCENE.h); c.clip();
          WHO.forEach((q, i) => {
            const colour = (OPT.colour + (i ? 0 : 1)) % COLOURS.length;   // the friend is the next colour
            figure(c, q, COLOURS[colour], st.fallen);
            companion(T, q, ground, st, colour);
          });
          c.restore();
        });
        world(T);
        c.fillStyle = '#10141a'; c.fillRect(SCENE.w, HEAD_H, SCENE.x[1] - SCENE.w, SCENE.h);
        legend(T, st);
        T.dm = st;
      },
    };
  },

  // build.js --break <name>: the coal burns in the alarm's red
  breaks: { red: [['SOOT = [38, 39, 43], COAL = EMBER;', 'SOOT = [38, 39, 43], COAL = RED;']] },

  // the breath against VisorFog.cs; a loop live; every phase on every ground in every colour, read off what is set on the canvas
  check(K) {
    const X = K.X, fs = require('fs'), path = require('path'), FPS = 60;
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };

    // ---------- the breath is the visor's ----------
    const fog = fs.readFileSync(path.join(__dirname, '../../../game/features/crew/logic/VisorFog.cs'), 'utf8');
    for (const name of ['Calm', 'Quick', 'GaspFirst', 'GaspSlows', 'Evaporating', 'ComeS', 'EvaporateS']) {
      const m = new RegExp(`\\b${name} = ([\\d.]+)`).exec(fog);
      need(m && +m[1] === X.FOG[name], `VisorFog's ${name}: the page has ${X.FOG[name]}, the game ${m && m[1]}`);
    }
    const periodAt = u => X.track[Math.round(u / X.DT)].period;
    for (const [u, want] of [[X.AT.low + .01, 3.2], [X.AT.low + 6, 2.075], [X.AT.last - .02, .955], [X.AT.last + .02, 1.3], [X.AT.out - .02, 3.1], [X.AT.refill + .1, 2.4]])
      need(Math.abs(periodAt(u) - want) < .03, `a breath of ${want} s at ${u.toFixed(2)}, not ${periodAt(u).toFixed(3)}`);
    need(X.STAGES.map(s => s.len).join() === '3,12,10,2.5,3,3', 'the loop as briefed');

    // ---------- what is drawn for each drone: its colours and its silhouette, pick px, halos left out ----------
    const parse = s => { const m = /^rgba?\((\d+),(\d+),(\d+)(?:,([\d.]+))?\)$/.exec(s); return m && (m[4] === undefined || +m[4] > .02) ? [+m[1], +m[2], +m[3]] : null; };
    const reddish = ([r, g, b]) => {
      const M = Math.max(r, g, b), d = M - Math.min(r, g, b);
      if (M <= 120 || d / M <= .45) return false;
      const h = ((M === r ? (g - b) / d : M === g ? (b - r) / d + 2 : (r - g) / d + 4) * 60 + 360) % 360;
      return Math.abs(((h - 5 + 540) % 360) - 180) < 15;
    };
    const blank = () => ({ x0: 1e9, x1: -1e9, y0: 1e9, y1: -1e9, rects: 0, paths: 0 });
    let seen = K.tiles.map(() => ({ shapes: {}, colours: new Set() }));
    K.tiles.forEach((t, j) => {
      const take = v => { if (t.probe && typeof v === 'string') seen[j].colours.add(v); };
      const point = (x, y) => {
        if (!t.probe) return null;
        const S = seen[j].shapes, s = S[t.probe] || (S[t.probe] = blank());
        if (x < s.x0) s.x0 = x; if (x > s.x1) s.x1 = x; if (y < s.y0) s.y0 = y; if (y > s.y1) s.y1 = y;
        return s;
      };
      const o = t.c, fns = {
        createLinearGradient: (...a) => { const g = o.createLinearGradient(...a); return { addColorStop(s, col) { take(col); g.addColorStop(s, col); } }; },
        createRadialGradient: (...a) => { const g = o.createRadialGradient(...a); return { addColorStop(s, col) { take(col); g.addColorStop(s, col); } }; },
        fillRect: (x, y, w, h) => { const s = point(x, y); if (s) { point(x + w, y + h); s.rects++; } o.fillRect(x, y, w, h); },
        lineTo: (x, y) => { const s = point(x, y); if (s) s.paths++; o.lineTo(x, y); },
        moveTo: (x, y) => { point(x, y); o.moveTo(x, y); },
        ellipse: (x, y, rx, ry, ...a) => { const s = point(x - rx, y - ry); if (s) { point(x + rx, y + ry); s.paths++; } o.ellipse(x, y, rx, ry, ...a); },
        drawImage: (im, x, y, ...a) => { if (t.probe) { point(x, y); point(x + a[0], y + a[1]); } o.drawImage(im, x, y, ...a); },
      };
      t.c = new Proxy(o, { get: (q, k) => fns[k] || q[k], set(q, k, v) { if (k === 'fillStyle' || k === 'strokeStyle') take(v); q[k] = v; return true; } });
    });
    const FAR = .3968 * 1.5, shapeOf = s => s ? [(s.x1 - s.x0) * FAR, (s.y1 - s.y0) * FAR, (s.x0 + s.x1) / 2 * FAR, (s.y0 + s.y1) / 2 * FAR, s.rects / 12, s.paths / 40] : null;

    // ---------- the plan ----------
    const LOOPS = Math.round(X.LOOP * FPS), L = 2 * LOOPS + 3, plan = new Map(), BGS = ['space', 'room'], PARTS = [.15, .38, .62, .9];
    let f = L;
    K.at(LOOPS, () => { K.seg('bgMode', 1); X.seek(0); });
    BGS.forEach((bg, b) => X.STAGES.forEach((s, si) => PARTS.forEach((part, k) => X.COLOURS.forEach((_, i) => {
      K.at(f, () => { K.seg('bgMode', b); K.seg('colourMode', i); X.seek(s.t0 + part * s.len); });
      plan.set(f, { bg, i, stage: s.id, key: `${bg}|${si}|${k}` });
      f++;
    }))));
    const P = f + 2, M = P + 40, A = M + X.STAGES.length * 2 + 2, E = A + 18;
    K.at(P, () => K.seg('playMode', 1));
    X.STAGES.forEach((_, i) => K.at(M + i * 2, () => K.seg('momentMode', i)));
    K.at(A, () => { X.seek(0); X.address('at=18.25&colour=2&bg=room&pause'); });
    let doc = null, copied = '';
    K.at(A + 4, () => K.click(2, '.pick'));
    K.at(A + 11, () => { K.seg('playMode', 0); K.seg('momentMode', 2); });
    K.at(A + 8, () => { doc = K.picked(); K.byId.copy.listeners.click[0](); copied = K.byId.fallback.value; });

    const WINDOWS = { calm: [0, 3], low: [10.5, 14], last: [15.5, 20], refill: [X.AT.refill + .05, X.AT.refill + 1.05] };
    const GROUNDS = ['space.friend', 'room.friend', 'planet.friend'];
    const per = K.tiles.map(() => ({ win: {}, end: null, red: 0, pulses: 0, drawn: 0, geo: new Map(), paused: 0, held: null, stages: new Set(), jumps: [], addressed: false }));
    let apart = 0;
    K.run(E, (t, frame) => {
      const j = K.tiles.indexOf(t), q = per[j], id = t.cand.id, S = seen[j], d = t.dm;
      if (d) {
        if (frame < L) {
          q.stages.add(d.id);
          if (j && Math.abs(t.time - K.tiles[0].time) > 1e-6) apart++;
          for (const g of GROUNDS) {
            if (!S.shapes[g]) continue;
            for (const [name, [a, b]] of Object.entries(WINDOWS)) if (d.u >= a && d.u < b) ((q.win[g] = q.win[g] || {})[name] = q.win[g][name] || []).push(shapeOf(S.shapes[g]));
            if (d.u > X.LOOP - .05) (q.end = q.end || {})[g] = shapeOf(S.shapes[g]);
          }
        }
        const p = plan.get(frame);
        if (frame < L || p) {
          for (const col of S.colours) {
            const c = parse(col);
            if (!c || !reddish(c)) continue;
            q.red++;
            if (id !== 'now') K.fail(`${id}: ${col} reads as red at ${d.u.toFixed(2)} s`);
            else if (d.id === 'calm' || d.id === 'out' || d.id === 'refill') K.fail(`now: red while ${d.id}`);
            else if (Math.abs(c[1] - X.RED[1]) < 2) q.pulses++;
            break;
          }
        }
        if (p) {
          const shapes = Object.keys(S.shapes).sort();
          if (d.id !== p.stage || shapes.length !== 4) K.fail(`${id}: ${p.bg} ${p.stage}: drew ${d.id}, ${shapes.length} drones`);
          const geo = JSON.stringify(shapes.map(k => S.shapes[k]));
          if (!q.geo.has(p.key)) q.geo.set(p.key, geo);
          else if (q.geo.get(p.key) !== geo) K.fail(`${id}: ${p.key}: the shape hangs on the colour`);
          q.drawn++;
        }
        if (frame > P && frame < M) {
          // a paused tile is not drawn again: its clock holds, and no drone is drawn anew
          if (q.held !== null && (t.time !== q.held || Object.keys(S.shapes).length)) K.fail(`${id}: it moved while paused`);
          else q.paused++;
          q.held = t.time;
        }
        for (let i = 0; i < X.STAGES.length; i++) if (frame === M + i * 2) q.jumps.push(d.id);
        if (frame > A + 11) { if (d.id !== 'last') K.fail(`${id}: a jump while playing drew ${d.id}`); if (j && Math.abs(t.time - K.tiles[0].time) > 1e-6) apart++; }
        if (frame === A + 3) q.addressed = X.clock() === 18.25 && K.OPT.colour === 2 && K.OPT.bg === 'room' && !K.OPT.playing;
      }
      seen[j] = { shapes: {}, colours: new Set() };
    });

    // ---------- how it reads on the friend, in screen px at the farthest view ----------
    const five = K.tiles.map((t, j) => j).filter(j => K.tiles[j].cand.id !== 'now');
    const mean = list => list[0].map((_, i) => list.reduce((s, v) => s + v[i], 0) / list.length);
    const swing = list => Math.max(...[0, 1, 2, 3].map(i => Math.max(...list.map(v => v[i])) - Math.min(...list.map(v => v[i]))));
    const gap = (a, b) => a.reduce((s, v, i) => s + Math.abs(v - b[i]), 0);
    for (const g of GROUNDS) {
      const M1 = per.map(q => Object.fromEntries(Object.entries(q.win[g]).map(([k, v]) => [k, mean(v)])));
      for (const j of five) {
        const id = K.tiles[j].cand.id, win = per[j].win[g];
        console.log(`${id} ${g}: ${Object.keys(WINDOWS).map(k => `${k} ${M1[j][k][0].toFixed(0)}x${M1[j][k][1].toFixed(0)} px, moves ${swing(win[k]).toFixed(1)}`).join('; ')}`);
        for (const phase of ['low', 'last', 'refill']) {
          need(gap(M1[j][phase], M1[j].calm) >= 4, `${id} ${g} ${phase}: too like calm (${gap(M1[j][phase], M1[j].calm).toFixed(1)})`);
          need(swing(win[phase]) >= 3, `${id} ${g} ${phase}: too still for a far friend (${swing(win[phase]).toFixed(1)} px)`);
          for (const k of five) if (k > j) need(gap(M1[j][phase], M1[k][phase]) >= 3, `${id} and ${K.tiles[k].cand.id} ${g} ${phase}: too alike`);
        }
        need(gap(per[j].end[g], M1[j].calm) < 8, `${id} ${g}: not back to calm as the recovery ends (${gap(per[j].end[g], M1[j].calm).toFixed(1)})`);
      }
    }
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id;
      need(q.stages.size === X.STAGES.length, `${id}: every phase live`);
      need(q.drawn === BGS.length * X.STAGES.length * PARTS.length * X.COLOURS.length, `${id}: every phase, ground and colour drawn`);
      need(q.paused > 30, `${id}: the pause held`);
      need(id === 'now' ? q.pulses > 100 : q.red === 0, `${id}: ${id === 'now' ? `the red pulse is there (${q.pulses})` : 'no red'}`);
    });
    // the jumps are read where the page's clock is shared: the first tile
    need(per[0].jumps.join() === X.STAGES.map(s => s.id).join(), `«Момент» jumps to each phase (${per[0].jumps.join()})`);
    need(per[0].addressed, 'the address sets the moment, colour, ground and pause');
    need(apart === 0, 'every tile on one clock');
    const id = K.tiles[2].cand.id;
    need(doc && doc.pick === id && Object.keys(doc).join() === 'pick', 'the pick saved, nothing else');
    need(copied.includes(`(${id})`), 'the copy text names the pick');
    return ok;
  },
};
