// The form vision (the design form-vision): when the light drone learns a form, the world fades to black for 5 s, the hero
// floats held, and the drone before him shows what the form does: the scanner on two wreck pieces, the lamp finding a hidden
// crate, the alarms at cold and a meteor. The open question is how the props in the void are drawn: four ways, each playing
// all three forms. The drone is «Нити» as DroneInk.cs draws it; the props are the game's own pictures (`pictures`), their
// outlines traced from those pictures' alpha. An art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/form-vision.js <out-dir> [--break <name>]
module.exports = {
  title: 'Видение формы',
  heading: 'Видение формы: чем рисовать вещи в пустоте',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Дрон выучил форму: мир гаснет, HUD уходит, герой висит, дрон перед ним — и <b>за 5 секунд</b> показывает, что форма делает. <b>Сканер:</b> два обломка, прицел на целом — полный ряд точек, прыжок на треснувший — половина. <b>Фонарь:</b> в темноте висит лампа, дрон копирует её чашу, луч находит ящик, которого не было видно. <b>Тревоги:</b> холод ползёт с края — снежинка; метеор — дрон дёргается, краснеет, знак метеора.</p>
<p class="lead">Решаем одно: <b>как нарисованы вещи в пустоте</b>. Четыре способа, в каждом все три формы: «Форма» — по очереди или одна. Масштаб игровой: клетка 8 px, герой в пять клеток. Внизу картинки — секунда видения и что сейчас идёт.</p>
<p class="miss">Не показано: звук; иконка, летящая в «Изучения» после видения; что видят друзья; мир до и после — заглушка (палуба и полоска HUD); отрисовка Godot (свет и цвета выйдут чуть иначе).</p>`,
  toggles: [{ id: 'formMode', bar: 1, label: 'Форма:', key: 'form', opts: [['все три по очереди', 'all'], ['сканер', 'scan'], ['фонарь', 'lamp'], ['тревоги', 'alarms']] }],
  pick: { doc: 'picks/formVision', subject: 'Видение формы, вещи в пустоте', what: ['вариант', 'вариантом'] },
  pictures: {
    section: 'game/art/hd/debris/section_01.png', machine: 'game/art/hd/debris/machine_01.png', lamp: 'game/art/hd/machine_lamp.png',
    crate: 'game/art/hd/debris/crate_03.png', rock: 'game/art/hd/debris/smallrock_03.png',
  },

  page() {
    const TAU = Math.PI * 2, N = 120;   // DroneInk.Sparks
    const VIS = { in: .6, out: .6, len: 5 };   // the vision, s: in, shown, out
    const FORMS = ['scan', 'lamp', 'alarms'], RU = { scan: 'сканер', lamp: 'фонарь', alarms: 'тревоги' };
    // DroneInk: a shape forms in Form s, a spark into it late by its share of Lag over Span, out of it by LagOut over SpanOut
    const FORM = .6, LAG = .45, SPAN = .55, LAG_OUT = .4, SPAN_OUT = .6;
    // ScanDots: 26 dots, each a 26th of the wreck's maximum, pick px over the world; their coming and going
    const NDOT = 26, DOT = { step: 7, side: 3.2, above: 20, in: .4, out: .4, lag: .4, span: .6, lagOut: .35, spanOut: .65 };
    const SPARK = .95, RED = [255, 58, 40], ICE = [206, 224, 236], HALF = .26;   // HALF: the beam's half-angle (scan-form's cone)
    const HX = 780, DV = [672, 292];   // the hero's feet; the drone before him in the void
    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const bump = (u, a, b, d) => sm((u - a) / (b - a)) * (1 - sm((u - b) / (d - b)));
    const pairs = a => { const p = []; for (let i = 0; i < a.length; i += 2) p.push([a[i], a[i + 1]]); return p; };
    const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
    const circ = (cx, cy, r, n = 22) => arcP(cx, cy, r, r, 0, TAU, n);
    const R = (r, a) => [Math.cos(a) * r, Math.sin(a) * r];
    const rect = (x0, y0, x1, y1) => [[x0, y0], [x1, y0], [x1, y1], [x0, y1], [x0, y0]];
    const unit = v => { const d = Math.hypot(v[0], v[1]) || 1; return [v[0] / d, v[1] / d]; };
    const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
    const angDiff = (a, b) => { let d = a - b; while (d > Math.PI) d -= TAU; while (d < -Math.PI) d += TAU; return Math.abs(d); };
    const angTo = (a, b) => Math.atan2(b[1] - a[1], b[0] - a[0]);
    const fmt = v => v.toFixed(2).replace(/0$/, '').replace(/\.0$/, '').replace('.', ',');

    // ---------- paths: polylines walked by a share of their length (DroneSwarm's ThreadPath) ----------
    function mkPath(polys) {
      const seg = []; let L = 0;
      for (const p of polys) for (let i = 1; i < p.length; i++) { const a = p[i - 1], b = p[i], d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 1e-6) continue; seg.push([a[0], a[1], b[0], b[1], d, L]); L += d; }
      return { seg, L };
    }
    function onPath(P, s) {
      const d = (((s % 1) + 1) % 1) * P.L; let lo = 0, hi = P.seg.length - 1;
      while (lo < hi) { const m = (lo + hi + 1) >> 1; if (P.seg[m][5] <= d) lo = m; else hi = m - 1; }
      const q = P.seg[lo], e = c01((d - q[5]) / q[4]);
      return [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e, (q[2] - q[0]) / q[4], (q[3] - q[1]) / q[4]];
    }
    // the polylines up to share f of their whole length, in order: a diagram drawing itself
    function cut(polys, f) {
      if (f >= 1) return polys; if (f <= 0) return [];
      const lens = polys.map(p => { let L = 0; for (let i = 1; i < p.length; i++) L += dist(p[i], p[i - 1]); return L; });
      let left = f * lens.reduce((a, b) => a + b, 0); const out = [];
      for (let k = 0; k < polys.length && left > 1e-6; k++) {
        const p = polys[k];
        if (lens[k] <= left) { out.push(p); left -= lens[k]; continue; }
        const q = [p[0]];
        for (let i = 1; i < p.length; i++) { const d = dist(p[i], p[i - 1]); if (d >= left) { q.push(lerp2(p[i - 1], p[i], left / (d || 1))); left = 0; break; } q.push(p[i]); left -= d; }
        out.push(q);
      }
      return out;
    }
    function lines(c, polys) { c.beginPath(); for (const p of polys) { if (p.length < 2) continue; c.moveTo(p[0][0], p[0][1]); for (let i = 1; i < p.length; i++) c.lineTo(p[i][0], p[i][1]); } }
    // the form's frame: about the core, turned to the aim, mirrored (fy) when the aim is behind so its top stays up
    const fyOf = A => clamp(Math.cos(A) * 4, -1, 1) || 1;
    const frameOf = (O, A) => ({ O, ca: Math.cos(A), sa: Math.sin(A), fy: fyOf(A) });
    const toW = (F, q) => { const y = q[1] * F.fy; return [F.O[0] + q[0] * F.ca - y * F.sa, F.O[1] + q[0] * F.sa + y * F.ca]; };

    // ---------- the props: the game's pictures, outlines traced from their alpha, lines placed by hand on them (picture px) ----------
    const PR = {
      section: {
        img: 'section', w: 192, h: 128, s: 1,
        ol: pairs([93.5, .5, 151.5, 1.5, 153.5, 14.5, 145.5, 24.5, 155.5, 38.5, 155.5, 50.5, 151.5, 53.5, 155.5, 55.5, 155.5, 67.5, 153.5, 69.5, 155.5, 72.5, 155.5, 106.5, 152.5, 119.5, 148.5, 121.5, 101.5, 121.5, 97.5, 123.5, 81.5, 121.5, 73.5, 123.5, 67.5, 121.5, 12.5, 123.5, 3.5, 112.5, 2.5, 104.5, 5.5, 91.5, 2.5, 75.5, 4.5, 74.5, 4.5, 7.5, 15.5, 1.5, 93.5, .5]),
        lines: [circ(29, 37, 25, 28), circ(29, 37, 17, 22), [[60, 5], [60, 76]], [[111, 5], [111, 76]], [[60, 43], [148, 43]], [[4, 78], [156, 78]], [[4, 92], [156, 92]], [[80, 95], [80, 120]], rect(122, 13, 137, 28),
          [[155, 67], [168, 70], [183, 83]], [[155, 72], [166, 82], [174, 104]], [[156, 84], [171, 90], [188, 103]]],
      },
      machine: {
        img: 'machine', w: 192, h: 128, s: 1,
        ol: pairs([39.5, .5, 57.5, 4.5, 58.5, 12.5, 85.5, 13.5, 99.5, 11.5, 104.5, 20.5, 102.5, 22.5, 103.5, 24.5, 108.5, 22.5, 111.5, 10.5, 123.5, 16.5, 132.5, 6.5, 140.5, 18.5, 153.5, 15.5, 153.5, 28.5, 161.5, 34.5, 166.5, 34.5, 157.5, 44.5, 166.5, 57.5, 153.5, 63.5, 154.5, 76.5, 141.5, 73.5, 133.5, 77.5, 124.5, 76.5, 117.5, 80.5, 151.5, 81.5, 153.5, 91.5, 174.5, 91.5, 176.5, 84.5, 189.5, 85.5, 189.5, 106.5, 175.5, 108.5, 174.5, 100.5, 152.5, 100.5, 151.5, 113.5, 113.5, 113.5, 99.5, 126.5, 13.5, 125.5, 3.5, 116.5, 3.5, 108.5, 5.5, 106.5, 5.5, 99.5, 3.5, 98.5, 3.5, 70.5, 5.5, 70.5, 3.5, 59.5, 5.5, 50.5, 3.5, 22.5, 12.5, 13.5, 35.5, 13.5, 39.5, .5]),
        lines: [[[4, 67], [103, 67]], rect(21, 24, 66, 60), ...[30, 37, 43, 49, 55].map(y => [[24, y], [63, y]]), circ(125, 43, 13, 20), circ(125, 43, 4, 10),
          rect(110, 84, 152, 113), ...[118, 127, 136, 145].map(x => [[x, 87], [x, 110]]), [[152, 94], [176, 94]], [[152, 99], [176, 99]], ...[16, 34, 52].map(x => [[x, 105], [x + 9, 90]])],
        // the cracked one's network out of its blow, as a wreck's cracks are drawn (WreckCracks)
        cracks: [[[70, 82], [60, 76], [52, 74], [44, 69]], [[70, 82], [80, 88], [90, 86], [99, 93]], [[70, 82], [66, 94], [72, 104], [68, 116]], [[60, 76], [61, 70]], [[80, 88], [83, 97]], [[132, 50], [141, 58], [147, 57]]],
      },
      lamp: {
        img: 'lamp', w: 64, h: 64, s: 1, bulb: [32, 35],
        ol: pairs([17.5, 1.5, 46.5, 1.5, 45.5, 8.5, 35.5, 9.5, 35.5, 14.5, 41.5, 15.5, 41.5, 22.5, 46.5, 30.5, 45.5, 41.5, 41.5, 46.5, 35.5, 49.5, 28.5, 49.5, 22.5, 46.5, 17.5, 39.5, 17.5, 30.5, 19.5, 25.5, 22.5, 22.5, 22.5, 15.5, 29.5, 14.5, 28.5, 9.5, 17.5, 9.5, 17.5, 1.5]),
        lines: [[[18, 9], [46, 9]], rect(23, 15, 41, 22), circ(32, 35, 11, 18), arcP(32, 35, 7, 7, 3.6, 4.9, 6)],
      },
      crate: {
        img: 'crate', w: 68, h: 68, s: 1,
        ol: pairs([3.5, 1.5, 6.5, 3.5, 12.5, 3.5, 12.5, 1.5, 66.5, 2.5, 65.5, 66.5, 1.5, 65.5, 1.5, 3.5, 3.5, 1.5]),
        lines: [[[2, 13], [66, 13]], [[2, 20], [66, 20]], [[2, 33], [66, 33]], [[2, 46], [66, 46]], [[2, 52], [66, 52]]],
      },
      rock: {
        img: 'rock', w: 96, h: 96, s: .55,
        ol: pairs([57.5, 4.5, 62.5, 4.5, 76.5, 13.5, 82.5, 28.5, 85.5, 49.5, 74.5, 67.5, 56.5, 82.5, 38.5, 85.5, 33.5, 84.5, 16.5, 75.5, 5.5, 57.5, 8.5, 33.5, 42.5, 7.5, 57.5, 4.5]),
        lines: [[[42, 8], [50, 40], [83, 30]], [[50, 40], [38, 84]], circ(30, 62, 6, 10)],
      },
    };
    const put = (pr, p, a) => ({ pr, p, a, ca: Math.cos(a), sa: Math.sin(a), s: pr.s, al: 1, crack: 0 });
    const toP = (q, v) => { const x = (v[0] - q.pr.w / 2) * q.s, y = (v[1] - q.pr.h / 2) * q.s; return [q.p[0] + x * q.ca - y * q.sa, q.p[1] + x * q.sa + y * q.ca]; };
    const olOf = q => q.pr.ol.map(v => toP(q, v));
    const linesOf = q => q.pr.lines.map(l => l.map(v => toP(q, v)));
    const cracksOf = q => (q.pr.cracks || []).map(l => l.map(v => toP(q, v)));
    const topOf = q => q.p[1] - q.pr.h * q.s / 2;

    // ---------- the drone's shapes (DroneInk.cs): the reticle «Прицел», the lamp's mirror, the glyphs «Контур» ----------
    const para = (back, half, cv, n) => { const p = []; for (let i = 0; i <= n; i++) { const v = -half + 2 * half * i / n; p.push([-back + v * v / cv, v]); } return p; };
    const LAMP_LINES = [para(9, 12, 14, 14), para(7.2, 10, 15, 12)];
    const tiny = (p, d, l) => [[p[0] - d[0] * l, p[1] - d[1] * l], [p[0] + d[0] * l, p[1] + d[1] * l]];
    const markA = j => (j + .5) / NDOT * TAU;
    function reticle(L) {
      const r = lerp(7.6, 7.1, L), rin = lerp(5.4, 4.3, L);
      return [Array.from({ length: NDOT }, (_, j) => tiny(R(10.2, markA(j)), R(1, markA(j) + Math.PI / 2), .22)), [circ(0, 0, r, 26)],
        [0, 1, 2, 3].map(q => [R(rin, q * Math.PI / 2), R(lerp(12.5, 13.5, L), q * Math.PI / 2)])];
    }
    const GLYPH = {
      cold: [[0, -1, 0, 1], [.866, -.5, -.866, .5], [.866, .5, -.866, -.5], [.2507, .7589, 0, .56, -.2507, .7589], [-.5319, .5965, -.485, .28, -.7826, .1624], [-.7826, -.1624, -.485, -.28, -.5319, -.5965], [-.2507, -.7589, 0, -.56, .2507, -.7589], [.5319, -.5965, .485, -.28, .7826, -.1624], [.7826, .1624, .485, .28, .5319, .5965]].map(pairs),
      meteor: [[-.0761, .5264, -.3106, .6993, -.6103, .7489, -.7068, .463, -.6764, .1817, -.3966, .1109, -.1141, .2118, -.0761, .5264], [-.2786, -.0608, .2517, -.5912], [-.1089, .1089, .6336, -.6336], [.0608, .2786, .5558, -.2164]].map(pairs),
    };
    const lenOf = polys => mkPath(polys).L;
    // which sparks each shape takes: by the game's hash order, the counts its shares give (the reticle: 26 marks, 40% ring, the rest ticks)
    const ORDER = Array.from({ length: N }, (_, i) => i).sort((a, b) => hsh(a * 3 + 1) - hsh(b * 3 + 1));
    function allocOf(counts) {
      const part = new Array(N).fill(-1), rank = new Array(N).fill(-1); let at = 0;
      counts.forEach((n, pi) => { for (let j = 0; j < n; j++) { const i = ORDER[at++]; part[i] = pi; rank[i] = j; } });
      return { part, rank, counts };
    }
    const AL = { scan: allocOf([NDOT, 48, N - NDOT - 48]), lamp: allocOf([96]), glyph: allocOf([84]) };
    const RET0 = reticle(0), L0 = { ring: lenOf(RET0[1]), ticks: lenOf(RET0[2]), lamp: lenOf(LAMP_LINES) };

    // ---------- the clock: the forms in turn, each 5 s; what each does when, s from the vision's start ----------
    const TS = { formA: 1.0, read: 1.75, hop: 2.75, home: 3.6, letA: 4.15, lampAt: 1.3, lightOn: 1.9, sweep: [2.0, 3.0], lightOff: 4.0, letL: 4.15, frost: [.7, 1.5], meteor: [2.6, .8] };
    const ALARMS = [{ id: 'cold', t0: 1.0, tin: .4, hold: .9, tout: .3 }, { id: 'meteor', t0: 2.6, tin: .4, hold: .9, tout: .3 }];
    // the pieces: hit points left of the most; the first whole, the second cracked to half
    const READS = [{ left: 520, max: 520 }, { left: 150, max: 300 }];
    function clockOf(T) {
      const seq = OPT.form === 'all' ? FORMS : [OPT.form], L = VIS.len * seq.length;
      const r = (((T.time - T.t0) % L) + L) % L, n = Math.min(seq.length - 1, Math.floor(r / VIS.len));
      return { form: seq[n], u: r - n * VIS.len, n: Math.floor((T.time - T.t0) / L) * seq.length + n };
    }
    const veilOf = u => u < VIS.in ? sm(u / VIS.in) : u > VIS.len - VIS.out ? 1 - sm((u - (VIS.len - VIS.out)) / VIS.out) : 1;
    const hudOf = u => u < .3 ? 1 - sm(u / .3) : u > VIS.len - .3 ? sm((u - (VIS.len - .3)) / .3) : 0;

    function scanProps(u) {
      const eA = sm((u - .2) / 1.1), eB = sm((u - .4) / 1.1);
      const A = put(PR.section, [lerp(-140, 250, eA) + 4 * Math.sin(u * .5), lerp(150, 200, eA) + 3 * Math.sin(u * .7)], lerp(-.3, -.05, eA));
      const B = put(PR.machine, [lerp(330, 455, eB) + 3 * Math.sin(u * .6 + 1), lerp(640, 402, eB) + 3 * Math.sin(u * .8)], lerp(.25, .05, eB));
      A.al = sm((u - .15) / .8); B.al = sm((u - .35) / .8); B.crack = 1;
      return [A, B];
    }
    function lampProps(u) {
      const sw = .05 * Math.sin(u * 1.4), piv = [470, -12], top = [piv[0] - Math.sin(sw) * 132, piv[1] + Math.cos(sw) * 132];
      const Lm = put(PR.lamp, [top[0] - Math.sin(sw) * 30, top[1] + Math.cos(sw) * 30], sw);
      Lm.al = sm((u - .25) / .7); Lm.piv = piv; Lm.top = top;
      const C = put(PR.crate, [300 + 3 * Math.sin(u * .6), 420 + 2 * Math.sin(u * .9)], .12);
      return [Lm, C];
    }
    const frostX = u => lerp(-10, 300, sm((u - TS.frost[0]) / TS.frost[1]));
    const frostA = u => sm((u - .6) / .3) * (1 - sm((u - 2.8) / .7));
    const edgeX = (x, y) => x + 16 * Math.sin(y * .045 + 1.3) + 9 * Math.sin(y * .13);
    const MP = [[1060, 20], [-80, 300]];
    const meteorOf = u => { const e = (u - TS.meteor[0]) / TS.meteor[1]; return e < 0 || e > 1 ? null : { head: lerp2(MP[0], MP[1], e), dir: unit([MP[1][0] - MP[0][0], MP[1][1] - MP[0][1]]), e, spin: u * 5 }; };
    const FROSTS = (() => { const r = rng(321), out = []; for (let i = 0; i < 46; i++) out.push({ x: 4 + Math.pow(r(), 1.4) * 280, y: 10 + r() * 520, s: 4 + r() * 7, a: r() * TAU, tw: r() * TAU }); return out; })();
    function alarmOf(u) {
      for (const a of ALARMS) {
        const t1 = a.t0 + a.tin + a.hold;
        if (u < a.t0 || u >= t1 + a.tout) continue;
        const f = u < a.t0 + a.tin ? sm((u - a.t0) / a.tin) : u < t1 ? 1 : 1 - sm((u - t1) / a.tout);
        return { ...a, t1, f, red: f * (.6 + .4 * Math.sin(u * TAU * 2.4)) };   // DroneInk AlarmLow, AlarmSwing, AlarmBeats
      }
      return null;
    }

    // ---------- the scene's state at a moment: props, the drone's place and aim, its shape, the light ----------
    const REST_A = Math.atan2(46, -22);   // the lamp's way with nothing to look at (stepDrone)
    function aimSched(keys, u) { let A = keys[0][1](); for (let i = 1; i < keys.length; i++) { if (u < keys[i][0]) break; A = angLerp(A, keys[i][1](), sm((u - keys[i][0]) / .3)); } return A; }
    function stateOf(T) {
      const ck = clockOf(T), u = ck.u, t = T.time, v = veilOf(u);
      const st = { form: ck.form, u, n: ck.n, v, hud: hudOf(u), t };
      // the drone: from its place by the helmet into the void before him; a dart from the meteor
      const DW = [HX + 36, FLOOR_Y - 176];
      st.alarm = ck.form === 'alarms' ? alarmOf(u) : null;
      st.dart = ck.form === 'alarms' ? bump(u, 2.6, 2.68, 3.05) : 0;
      st.D = [lerp(DW[0], DV[0], v) + 9 * st.dart, lerp(DW[1], DV[1], v) + 1.5 * Math.sin(t * 1.7) + 10 * st.dart];
      st.hot = st.alarm ? mix3(EMBER, RED, Math.min(1, st.alarm.red * 1.4)) : EMBER;
      if (ck.form === 'scan') {
        st.props = scanProps(u);
        const [A, B] = st.props;
        st.A = aimSched([[0, () => REST_A], [.9, () => angTo(st.D, A.p)], [TS.hop, () => angTo(st.D, B.p)], [3.75, () => REST_A]], u);
        st.reads = READS.map((r, i) => ({ ...r, q: st.props[i], O: [st.props[i].p[0], topOf(st.props[i]) - DOT.above] }));
      } else if (ck.form === 'lamp') {
        st.props = lampProps(u);
        const [Lm, C] = st.props, thL = angTo(st.D, Lm.p), thC = angTo(st.D, C.p);
        st.beamA = angLerp(thL, thC, sm((u - TS.sweep[0]) / (TS.sweep[1] - TS.sweep[0])));
        st.light = sm(c01((u - TS.lightOn) / .15)) * (1 - sm(c01((u - TS.lightOff) / .15)));
        st.inCone = sm(c01(1 - angDiff(st.beamA, thC) / HALF));
        st.seen = u >= TS.sweep[1] ? 1 : u < TS.lightOn ? 0 : st.inCone;   // the sweep closes in on the crate and rests there
        st.A = aimSched([[0, () => REST_A], [.9, () => thL], [TS.lightOn, () => st.beamA]], u);
        st.study = bump(u, 1.0, 1.15, 1.6);
      } else {
        st.props = [];
        st.met = meteorOf(u);
        const mh = () => { const e = clamp((u - TS.meteor[0]) / TS.meteor[1], 0, 1); return angTo(st.D, lerp2(MP[0], MP[1], e)); };
        st.A = aimSched([[0, () => REST_A], [.8, () => Math.PI], [2.5, mh], [3.5, () => REST_A]], u);
      }
      st.shape = shapeOf(st);
      return st;
    }
    // the shape the drone holds now: its parts' polylines in page px, its sparks' gather times
    function shapeOf(st) {
      const u = st.u, F = frameOf(st.D, st.A);
      if (st.form === 'scan') {
        const L = c01(sm((u - TS.read + .35) / .3) * (1 - sm((u - TS.home - .4) / .4)));
        const polys = reticle(L).map(pl => pl.map(l => l.map(q => toW(F, q))));
        const dots = dotsOf(u, st.reads);
        return {
          kind: 'scan', al: AL.scan, t0: TS.formA, t1: TS.letA, tin: FORM, tout: FORM, L, dots,
          parts: [{ seeds: true, polys: polys[0] }, { polys: polys[1], P: mkPath(polys[1]), L0: L0.ring, flow: 12 }, { polys: polys[2], P: mkPath(polys[2]), L0: L0.ticks, flow: 8 }],
          seats: Array.from({ length: NDOT }, (_, j) => toW(F, R(10.2, markA(j)))),
        };
      }
      if (st.form === 'lamp') {
        const polys = LAMP_LINES.map(l => l.map(q => toW(F, q)));
        return { kind: 'lamp', al: AL.lamp, t0: TS.lampAt, t1: TS.letL, tin: FORM, tout: FORM, parts: [{ polys, P: mkPath(polys), L0: L0.lamp, flow: 30 }] };
      }
      const a = st.alarm; if (!a) return null;
      const O = [st.D[0], st.D[1] - 50], polys = GLYPH[a.id].map(l => l.map(q => [O[0] + q[0] * 22, O[1] + q[1] * 22]));   // ThoughtAbove, ThoughtSize
      const P = mkPath(polys);
      return { kind: 'glyph', id: a.id, al: AL.glyph, t0: a.t0, t1: a.t1, tin: a.tin, tout: a.tout, parts: [{ polys, P, L0: P.L, flow: 42 }] };   // AlarmFlow
    }
    const gq = (sh, i, u) => {
      if (!sh || u < sh.t0) return 0;
      const lag = hsh(i + 50); let q = sm(c01(((u - sh.t0) / sh.tin - lag * LAG) / SPAN));
      if (u >= sh.t1) q *= 1 - sm(c01(((u - sh.t1) / sh.tout - lag * LAG_OUT) / SPAN_OUT));
      return q;
    };
    const shapeForm = (sh, u) => !sh ? 0 : u < sh.t0 ? 0 : u < sh.t1 ? sm((u - sh.t0) / sh.tin) : 1 - sm((u - sh.t1) / sh.tout);
    // the gauge's phase (ScanDots): out to the first piece, the hop to the second from where they are, home
    function dotsOf(u, reads) {
      if (u < TS.read) return null;
      if (u < TS.hop) return { ph: 'a', e: u - TS.read, r: reads[0] };
      if (u < TS.home) return { ph: 'hop', e: u - TS.hop, r: reads[1], from: reads[0] };
      if (u < TS.home + DOT.out + .05) return { ph: 'home', e: u - TS.home, r: reads[1] };
      return null;
    }
    const dotQ = (j, D) => D.ph === 'home' ? sm(c01((D.e / DOT.out - hsh(j + 2) * DOT.lagOut) / DOT.spanOut)) : sm(c01((D.e / DOT.in - hsh(j + 5) * DOT.lag) / DOT.span));
    const outOf = (j, D) => { if (!D) return false; const q = dotQ(j, D); return D.ph === 'a' ? q > 0 : D.ph === 'hop' ? true : q < 1; };
    const awayOf = (j, D) => { if (!D) return 0; const q = dotQ(j, D); return D.ph === 'a' ? q : D.ph === 'hop' ? 1 : 1 - q; };
    const litOf = (r, j) => c01(r.left * NDOT / r.max - j);
    const rowAt = (r, j) => [r.O[0] + (j - (NDOT - 1) / 2) * DOT.step, r.O[1]];

    // ---------- the drone «Нити»: a coal core, an ember eye, sparks on three orbits; the shape's sparks on their threads ----------
    function coal(c, g, X) {
      const x = X.D[0], y = X.D[1], s = DS * DSC, A = X.A, fl = .75 + .25 * Math.sin(X.t * 7.3);
      halo(g, x, y, 8, X.hot, .35);
      c.save(); c.translate(x, y); c.scale(s, s); paint(c, DR.core, A, DARK, .8, 2.6); ink(c, DR.core, A, 1.7); c.restore();
      g.strokeStyle = css(X.hot, .5 * fl); g.lineWidth = .45; g.beginPath();
      for (const [a, b] of [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]]) { g.moveTo(x + Math.cos(A + a) * .6, y + Math.sin(A + a) * .6); g.lineTo(x + Math.cos(A + b * .5) * 2.6, y + Math.sin(A + b * .5) * 2.6); }
      g.stroke();
      const e = X.eye;
      g.fillStyle = css(pale(X.hot, .5), .9 * fl); g.beginPath(); g.arc(e[0], e[1], 1.2, 0, TAU); g.fill();
      halo(g, e[0], e[1], 4, X.hot, .5 * fl);
    }
    function orbit(i, X) {
      const P = DR.P[i], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + X.t * (.8 + .25 * o) * dir * 2.2, rx = (6.3 + o * 1.4 + (P.r1 - .5)) * 1.05, tl = -.3 + o * 1.05;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tl), v = rot(-Math.sin(th) * dir, Math.cos(th) * .3 * dir, tl), vl = Math.hypot(v[0], v[1]) || 1;
      return [X.D[0] + q[0], X.D[1] + q[1], v[0] / vl, v[1] / vl, Math.sin(th)];
    }
    function mote(c, g, p, s, heat, col) {
      const h = Math.min(1, heat), dx = p[2] || 0, dy = p[3] || 0;
      c.fillStyle = '#26272b'; c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
      if (h <= .02) return;
      for (let m = 0; m < 3; m++) { const b = m * 1.3; g.fillStyle = css(pale(col, .3 * (1 - m / 3)), (.95 - m * .3) * h); g.fillRect(p[0] - dx * b - s / 2, p[1] - dy * b - s / 2, s, s); }
    }
    function line2(g, a, b, w, col, al) { if (al <= .003) return; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.lineWidth = w; g.strokeStyle = css(col, al); g.stroke(); }
    const idleHeat = (i, X) => hsh(i + 3) < .5 ? .55 + .3 * Math.sin(X.t * 11 + i * 7) : .2;
    const formHeat = (i, t) => .7 + .25 * Math.sin(t * 13 + i);
    function drawBody(X, sh) {
      const c = X.c, g = X.g, u = X.u, D = sh && sh.dots, form = shapeForm(sh, u), out = { seated: 0, mirror: 0, nm: 0, away: 0 };
      g.lineWidth = .4; g.strokeStyle = css(X.hot, .12);
      for (let o = 0; o < 3; o++) { const rx = (6.3 + o * 1.4) * 1.05; g.beginPath(); g.ellipse(X.D[0], X.D[1], rx, rx * .3, -.3 + o * 1.05, 0, TAU); g.stroke(); }
      // the shape's threads; a mark's thread fades while its spark is away as a dot
      if (sh && form > .003) sh.parts.forEach(pt => {
        if (!pt.seeds) { lines(g, pt.polys); g.lineWidth = 1.6; g.strokeStyle = css(X.hot, .06 * form); g.stroke(); g.lineWidth = .45; g.strokeStyle = css(X.hot, .3 * form); g.stroke(); return; }
        pt.polys.forEach((l, j) => { const a = form * (1 - awayOf(j, D)); lines(g, [l]); g.lineWidth = .45; g.strokeStyle = css(X.hot, .3 * a); g.stroke(); });
      });
      const back = [], front = [];
      for (let i = 0; i < N; i++) {
        const I = orbit(i, X); let p = I, z = I[4], heat = idleHeat(i, X);
        const pi = sh ? sh.al.part[i] : -1;
        if (pi >= 0) {
          const r = sh.al.rank[i], pt = sh.parts[pi];
          if (pt.seeds && outOf(r, D)) { out.away++; continue; }   // away as its dot, drawn with the gauge
          const q = gq(sh, i, u);
          if (sh.kind === 'lamp') { out.mirror += q; out.nm++; }
          if (q > 0) {
            const S = pt.seeds ? sh.seats[r] : onPath(pt.P, (r + .5) / sh.al.counts[pi] + X.t * pt.flow / pt.L0);
            if (q >= 1) p = S;
            else {
              const e = sm(q), dx = S[0] - I[0], dy = S[1] - I[1], d = Math.hypot(dx, dy) || 1;
              p = [lerp(I[0], S[0], e), lerp(I[1], S[1], e), dx / d, dy / d];
              if (i % 4 === 0) line2(g, I, p, .35, X.hot, .07);
            }
            z = q > .5 ? 1 : z;
            heat = formHeat(i, X.t) + .3 * Math.sin(q * Math.PI) + (sh.kind === 'lamp' ? .12 * q : 0);
            if (pt.seeds && q >= 1) out.seated++;
          }
        }
        (z < 0 ? back : front).push([p, heat]);
      }
      for (const [p, h] of back) mote(c, g, p, SPARK, h, X.hot);
      coal(c, g, X);
      for (const [p, h] of front) mote(c, g, p, SPARK, h, X.hot);
      return out;
    }
    // the gauge: out from the marks to the row over the first piece, across to the second, home to the marks
    function drawDots(X, sh) {
      const D = sh.dots, c = X.c, g = X.g; if (!D) return null;
      let home = 0, lit = 0, drawn = 0;
      for (let j = 0; j < NDOT; j++) {
        if (!outOf(j, D)) continue;
        const q = dotQ(j, D);
        let start, goal;
        if (D.ph === 'a') { start = sh.seats[j]; goal = rowAt(D.r, j); }
        else if (D.ph === 'hop') { start = rowAt(D.from, j); goal = rowAt(D.r, j); }
        else { start = rowAt(D.r, j); goal = sh.seats[j]; }
        const there = D.ph === 'home' ? q <= 0 : q >= 1, e = sm(q), wx = goal[0] - start[0], wy = goal[1] - start[1], dd = Math.hypot(wx, wy);
        const fly = !there && dd > 1e-4, p = there ? (D.ph === 'home' ? start : goal) : [start[0] + wx * e, start[1] + wy * e];
        const lt = litOf(D.r, j), fl = .82 + .18 * Math.sin(X.t * 9 + j * 2.3), dotHeat = lt > 0 ? (.3 + .62 * lt) * fl : .04;
        const aw = awayOf(j, D), heat = lerp(formHeat(j, X.t), dotHeat, aw), side = lerp(SPARK, DOT.side, aw);
        if (fly && j % 2 === 0) line2(g, start, p, .35, EMBER, .07);
        mote(c, g, [p[0], p[1], fly ? wx / dd : 0, fly ? wy / dd : 0], side, heat, EMBER);
        drawn++;
        if (there && D.ph !== 'home') { home++; lit += lt; }
      }
      return { ph: D.ph, home, lit, drawn };
    }

    // ---------- the void, the world before and after ----------
    const VSTARS = (() => { const r = rng(77), s = []; for (let i = 0; i < 140; i++) { const b = r(); s.push([r() * LW, r() * LH, b < .08 ? 1.5 : 1, .1 + r() * .3]); } return s; })();
    function voidBack(c, v) {
      c.fillStyle = css([3, 4, 7], v); c.fillRect(0, 0, LW, LH);
      for (const [x, y, sz, al] of VSTARS) { c.fillStyle = `rgba(206,216,232,${(al * v).toFixed(3)})`; c.fillRect(x, y, sz, sz); }
    }
    // the HUD's stand-in: equal ring dials over a long air bar
    function hudStandIn(c, a) {
      if (a <= .003) return;
      c.save(); c.globalAlpha = a; c.lineWidth = 2;
      [[38, .8], [68, .55], [98, .9]].forEach(([x, f]) => { c.strokeStyle = 'rgba(200,206,214,.25)'; c.beginPath(); c.arc(x, 34, 10, 0, TAU); c.stroke(); c.strokeStyle = 'rgba(226,214,180,.75)'; c.beginPath(); c.arc(x, 34, 10, -Math.PI / 2, -Math.PI / 2 + TAU * f); c.stroke(); });
      c.fillStyle = 'rgba(20,26,34,.8)'; c.fillRect(26, 52, 190, 6); c.fillStyle = 'rgba(140,196,255,.7)'; c.fillRect(27, 53, 150, 4);
      c.restore();
    }
    function vignette(c, v) {
      const W = LW, H = LH, Rv = Math.hypot(W, H) * .6, gr = c.createRadialGradient(W * .55, H * .5, Rv * .3, W * .55, H * .5, Rv);
      gr.addColorStop(0, 'rgba(2,3,5,0)'); gr.addColorStop(1, css([2, 3, 5], .6 * v)); c.fillStyle = gr; c.fillRect(0, 0, W, H);
    }

    // ---------- the four ways to draw a prop ----------
    // the picture as the game draws it
    function picture(c, q, al, filter) {
      const im = IMG[q.pr.img]; if (al <= .003 || !ready(im)) return;
      c.save(); c.globalAlpha = al; c.translate(q.p[0], q.p[1]); c.rotate(q.a); if (filter) c.filter = filter;
      c.drawImage(im, -q.pr.w * q.s / 2, -q.pr.h * q.s / 2, q.pr.w * q.s, q.pr.h * q.s); c.restore();
    }
    function inkCracks(c, q, al) {
      if (!q.crack || al <= .003) return;
      const k = cracksOf(q); c.save(); c.lineCap = 'round'; c.lineJoin = 'round';
      c.save(); c.translate(.7, .9); lines(c, k); c.strokeStyle = css([160, 168, 178], .35 * al); c.lineWidth = .7; c.stroke(); c.restore();
      lines(c, k); c.strokeStyle = css([22, 23, 27], .95 * al); c.lineWidth = 1.3; c.stroke(); c.restore();
    }
    // the drone's eyes: the outline and lines in ember threads drawn up to share f, sparks running along the outline
    function emberProp(c, q, f, al, t, o = {}) {
      if (f <= .003 || al <= .003) return;
      const ol = olOf(q), col = o.col || EMBER;
      if (o.fill !== false) { c.save(); c.globalAlpha = al * sm(f) * .92; lines(c, [ol]); c.closePath(); c.fillStyle = '#06070a'; c.fill(); c.restore(); }
      emberLines(c, cut([ol], f), al, col, 1);
      emberLines(c, cut(linesOf(q), f), al, col, .6);
      if (q.crack) { const k = cut(cracksOf(q), f), fl = .7 + .3 * Math.sin(t * 17); emberLines(c, k, al * fl, mix3(EMBER, RED, .75), 1.1); }
      const P = mkPath([ol]), n = Math.ceil(P.L / 12);
      c.save(); c.globalCompositeOperation = 'lighter';
      for (let i = 0; i < n; i++) {
        const s = ((i + .5) / n + t * 28 / P.L) % 1; if (s > f) continue;
        const p = onPath(P, s), h = (.7 + .25 * Math.sin(t * 13 + i)) * al;
        for (let m = 0; m < 3; m++) { const b = m * 1.3; c.fillStyle = css(pale(col, .3 * (1 - m / 3)), (.95 - m * .3) * h); c.fillRect(p[0] - p[2] * b - .6, p[1] - p[3] * b - .6, 1.2, 1.2); }
      }
      c.restore();
    }
    function emberLines(c, polys, al, col, k) {
      if (!polys.length || al <= .003) return;
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
      lines(c, polys); c.strokeStyle = css(col, .08 * al * k); c.lineWidth = 2.4; c.stroke(); c.strokeStyle = css(pale(col, .2), .55 * al * k); c.lineWidth = .7; c.stroke();
      c.restore();
    }
    // only light: the picture near black, its edges lit where they face a light, by how near it is
    function rim(c, q, al, lights) {
      const ol = olOf(q); let area = 0;
      for (let i = 1; i < ol.length; i++) area += ol[i - 1][0] * ol[i][1] - ol[i][0] * ol[i - 1][1];
      const sg = area >= 0 ? 1 : -1;
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round';
      for (let i = 1; i < ol.length; i++) {
        const a = ol[i - 1], b = ol[i], dx = b[0] - a[0], dy = b[1] - a[1], L = Math.hypot(dx, dy) || 1, n = [sg * dy / L, -sg * dx / L], m = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
        for (const Lt of lights) {
          const d = [Lt.p[0] - m[0], Lt.p[1] - m[1]], dl = Math.hypot(d[0], d[1]) || 1, face = (n[0] * d[0] + n[1] * d[1]) / dl;
          const k = al * Lt.pw * Math.max(0, face) / (1 + (dl / Lt.R) * (dl / Lt.R)); if (k <= .01) continue;
          c.beginPath(); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]);
          c.strokeStyle = css(Lt.col, .25 * Math.min(1, k)); c.lineWidth = 3; c.stroke();
          c.strokeStyle = css(pale(Lt.col, .3), Math.min(1, k)); c.lineWidth = 1.1; c.stroke();
        }
      }
      c.restore();
    }
    const DARKEN = 'brightness(0.13) saturate(0.5)';

    // ---------- what each way draws in each form ----------
    // trace windows of «Проступает схема»: when the drone takes each thing, s
    const TR = { A: TS.read - .15, B: TS.hop, lamp: 1.0, rock: 2.65, frost: 1.0 };
    const trOf = (u, t0) => sm(c01((u - t0) / .7));
    function drawScanProps(c, X, st, way) {
      const [A, B] = st.props, u = st.u, v = st.v;
      if (way === 'real') { for (const q of [A, B]) { picture(c, q, q.al * v); inkCracks(c, q, q.al * v); } return; }
      if (way === 'ember') { emberProp(c, A, sm((u - .25) / 1.0), v, X.t); emberProp(c, B, sm((u - .45) / 1.0), v, X.t); return; }
      if (way === 'trace') {
        [[A, TR.A], [B, TR.B]].forEach(([q, t0]) => { const f = trOf(u, t0); picture(c, q, q.al * v * (1 - .65 * f)); inkCracks(c, q, q.al * v * (1 - .65 * f)); emberProp(c, q, f, v, X.t, { fill: false }); });
        return;
      }
      const lights = [{ p: X.D, col: X.hot, pw: 1, R: 150 }];
      const rd = X.rd; if (rd && rd.home) lights.push({ p: [st.reads[rd.ph === 'a' ? 0 : 1].O[0], st.reads[rd.ph === 'a' ? 0 : 1].O[1]], col: EMBER, pw: 1.4 * rd.lit / NDOT + .2, R: 80 });
      for (const q of [A, B]) { picture(c, q, q.al * v, DARKEN); rim(c, q, q.al * v, lights); }
      if (B.crack) emberLines(c, cracksOf(B), .25 * B.al * v, EMBER, 1);
    }
    function coneOf(D, A, len) { return [D, [D[0] + Math.cos(A - HALF) * len, D[1] + Math.sin(A - HALF) * len], [D[0] + Math.cos(A + HALF) * len, D[1] + Math.sin(A + HALF) * len]]; }
    function beam(c, X, st, way) {
      const Lt = st.light * st.v; if (Lt <= .003) return;
      const D = X.D, A = st.beamA, len = 900;
      c.save(); c.globalCompositeOperation = 'lighter';
      if (way === 'ember') {   // the cone as the drone draws it: its two edges in threads, a faint fill
        const k = coneOf(D, A, 520); lines(c, [[k[1], D, k[2]]]); c.strokeStyle = css(LAMP, .3 * Lt); c.lineWidth = .7; c.setLineDash([5, 6]); c.stroke(); c.setLineDash([]);
      }
      c.translate(D[0], D[1]); c.rotate(A);
      const gr = c.createLinearGradient(0, 0, len, 0); gr.addColorStop(0, css(LAMP, .22 * Lt)); gr.addColorStop(1, css(LAMP, 0));
      c.fillStyle = gr; c.beginPath(); c.moveTo(2, -3); c.lineTo(len, -len * Math.tan(HALF)); c.lineTo(len, len * Math.tan(HALF)); c.lineTo(2, 3); c.closePath(); c.fill();
      c.restore();
    }
    function crateInBeam(c, X, st, q) {
      const a = st.light * st.inCone * st.v;
      if (a <= .003) return 0;
      c.save(); const k = coneOf(X.D, st.beamA, 900); lines(c, [k]); c.closePath(); c.clip(); picture(c, q, st.light * st.v); c.restore();
      halo(c, q.p[0], q.p[1], 70, LAMP, .18 * a);
      return a;
    }
    function drawLampProps(c, X, st, way) {
      const [Lm, C] = st.props, u = st.u, v = st.v, a = Lm.al * v, bulb = toP(Lm, Lm.pr.bulb);
      const cable = [Lm.piv, Lm.top];
      const out = { pic: 0, diag: 0 };
      // the lamp is lit: its glow, whatever the way
      halo(c, bulb[0], bulb[1], 46, LAMP, .28 * a);
      if (way === 'real' || way === 'trace') {
        const f = way === 'trace' ? trOf(u, TR.lamp) : 0;
        lines(c, [cable]); c.strokeStyle = css([22, 23, 27], a); c.lineWidth = 1.6; c.stroke(); c.strokeStyle = css([120, 126, 134], .5 * a); c.lineWidth = .6; c.stroke();
        picture(c, Lm, a * (1 - .5 * f));
        if (f > 0) { emberLines(c, cut([cable], f), v, EMBER, .6); emberProp(c, Lm, f, v, X.t, { fill: false }); }
        out.pic = crateInBeam(c, X, st, C);
        if (way === 'trace') { out.diag = st.seen; emberProp(c, C, st.seen, v, X.t, { fill: false }); }
      } else if (way === 'ember') {
        const f = sm((u - .3) / 1.0);
        emberLines(c, cut([cable], f), v, EMBER, .6);
        emberProp(c, Lm, f, v, X.t);
        halo(c, bulb[0], bulb[1], 12, LAMP, .5 * a * f);
        out.diag = st.seen; emberProp(c, C, st.seen, v, X.t);
      } else {
        lines(c, [cable]); c.strokeStyle = css([10, 11, 14], a); c.lineWidth = 1.6; c.stroke();
        picture(c, Lm, a, 'brightness(0.8)');
        out.pic = crateInBeam(c, X, st, C);
      }
      // the drone studies the lamp: a thread from its eye, a spark running home along it
      if (st.study > .003) { const e = X.eye, s = (u - 1.0) / .6 % 1, p = lerp2(bulb, e, sm(c01(s))); c.save(); c.globalCompositeOperation = 'lighter'; line2(c, e, bulb, .4, EMBER, .25 * st.study * v); halo(c, p[0], p[1], 5, EMBER, .7 * st.study * v); c.restore(); }
      return out;
    }
    function crystal(c, k, s, col, w) {
      c.beginPath();
      for (let m = 0; m < 6; m++) {
        const a = k.a + m * Math.PI / 3, e = [k.x + Math.cos(a) * s, k.y + Math.sin(a) * s], mid = [k.x + Math.cos(a) * s * .55, k.y + Math.sin(a) * s * .55];
        c.moveTo(k.x, k.y); c.lineTo(e[0], e[1]);
        for (const b of [-.7, .7]) { c.moveTo(mid[0], mid[1]); c.lineTo(mid[0] + Math.cos(a + b) * s * .32, mid[1] + Math.sin(a + b) * s * .32); }
      }
      c.strokeStyle = col; c.lineWidth = w; c.stroke();
    }
    const frontLine = x => { const p = []; for (let y = -4; y <= LH + 4; y += 12) p.push([edgeX(x, y), y]); return p; };
    function drawFrost(c, X, st, way) {
      const u = st.u, a = frostA(u) * st.v, x = frostX(u); if (a <= .003) return;
      const grow = k => sm(c01((x - k.x) / 40));
      if (way === 'real' || way === 'trace') {
        const fl = frontLine(x); c.save(); c.globalAlpha = a;
        c.beginPath(); c.moveTo(-4, -4); for (const p of fl) c.lineTo(p[0], p[1]); c.lineTo(-4, LH + 4); c.closePath();
        const gr = c.createLinearGradient(0, 0, Math.max(x + 30, 1), 0); gr.addColorStop(0, css(ICE, .3)); gr.addColorStop(1, css(ICE, .04)); c.fillStyle = gr; c.fill();
        lines(c, [fl]); c.strokeStyle = css(ICE, .3); c.lineWidth = 1; c.stroke();
        for (const k of FROSTS) { const g0 = grow(k); if (g0 > .01) crystal(c, k, k.s * g0, css(ICE, .7), .8); }
        c.restore();
        if (way === 'trace') emberLines(c, cut([fl], trOf(u, TR.frost)), a, EMBER, 1);
      } else if (way === 'ember') {
        emberLines(c, [frontLine(x)], a, pale(EMBER, .5), 1);
        c.save(); c.globalCompositeOperation = 'lighter';
        for (const k of FROSTS) { const g0 = grow(k); if (g0 > .01) crystal(c, k, k.s * g0, css(pale(EMBER, .55), .6 * a), .7); }
        c.restore();
      } else {
        const gr = c.createLinearGradient(0, 0, Math.max(x + 60, 1), 0); gr.addColorStop(0, css(ICE, .08 * a)); gr.addColorStop(1, css(ICE, 0)); c.fillStyle = gr; c.fillRect(0, 0, Math.max(x + 60, 1), LH);
        c.save(); c.globalCompositeOperation = 'lighter';
        for (const k of FROSTS) { const g0 = grow(k); if (g0 <= .01) continue; const tw = .5 + .5 * Math.sin(X.t * 3 + k.tw); crystal(c, k, k.s * g0, css(ICE, (.12 + .3 * tw) * a), .7); if (tw > .85) halo(c, k.x, k.y, 6, ICE, .3 * a * (tw - .85) / .15); }
        c.restore();
      }
    }
    function drawMeteor(c, X, st, way) {
      const m = st.met; if (!m) return null;
      const v = st.v, q = put(PR.rock, m.head, m.spin), tail = [m.head[0] - m.dir[0] * 170, m.head[1] - m.dir[1] * 170], nrm = [-m.dir[1], m.dir[0]];
      const fire = () => {
        c.save(); c.globalCompositeOperation = 'lighter';
        const gr = c.createLinearGradient(tail[0], tail[1], m.head[0], m.head[1]); gr.addColorStop(0, 'rgba(255,190,120,0)'); gr.addColorStop(1, css([255, 214, 168], .8 * v));
        c.fillStyle = gr; c.beginPath(); c.moveTo(tail[0], tail[1]); c.lineTo(m.head[0] + nrm[0] * 9, m.head[1] + nrm[1] * 9); c.lineTo(m.head[0] - nrm[0] * 9, m.head[1] - nrm[1] * 9); c.closePath(); c.fill();
        c.restore(); halo(c, m.head[0], m.head[1], 34, EMBER, .5 * v);
      };
      const path = al => { const p = [m.head, [m.head[0] + m.dir[0] * 700, m.head[1] + m.dir[1] * 700]]; c.save(); c.globalCompositeOperation = 'lighter'; lines(c, [p]); c.setLineDash([4, 7]); c.strokeStyle = css(mix3(EMBER, RED, .5), .45 * al); c.lineWidth = .8; c.stroke(); c.setLineDash([]); c.restore(); };
      if (way === 'real' || way === 'trace') {
        fire(); picture(c, q, v);
        if (way === 'trace') { const f = trOf(st.u, TR.rock); emberProp(c, q, f, v, X.t, { fill: false, col: mix3(EMBER, RED, .5) }); path(f * v); }
      } else if (way === 'ember') {
        const hot = mix3(EMBER, RED, .5);
        emberProp(c, q, 1, v, X.t, { col: hot });
        emberLines(c, [-1, 0, 1].map(s => [[m.head[0] - m.dir[0] * 30 + nrm[0] * s * 9, m.head[1] - m.dir[1] * 30 + nrm[1] * s * 9], [m.head[0] - m.dir[0] * (90 + 30 * (1 - Math.abs(s))) + nrm[0] * s * 9, m.head[1] - m.dir[1] * (90 + 30 * (1 - Math.abs(s))) + nrm[1] * s * 9]]), v, hot, 1);
        path(v);
      } else {
        halo(c, m.head[0], m.head[1], 240, [255, 190, 130], .12 * v);
        fire(); picture(c, q, v, 'brightness(0.18)');
        rim(c, q, v, [{ p: [m.head[0] + m.dir[0] * 40, m.head[1] + m.dir[1] * 40], col: [255, 200, 140], pw: 1.6, R: 60 }]);
      }
      return m;
    }

    // ---------- the hero: held, floating where he stood ----------
    const HB = newHero('me', HX, 3);
    function heroOf(st, X) {
      const v = st.v, t = st.t;
      HB.face = -1; HB.x = HX; HB.y = lerp(FLOOR_Y, FLOOR_Y - 12 + 3 * Math.sin(t * .9), v);
      HB.kn = .16 * v; HB.lean = (-.07 + .03 * Math.sin(t * .53)) * v; HB.air = false; HB.amp = 0; HB.ph = 0; HB.e = 0; HB.hl = 0; HB.rk = 0;
      const nk = toS(HB, NECK()), aim = [X.D[0] + Math.cos(X.A) * 60, X.D[1] + Math.sin(X.A) * 60];
      HB.look = lerp(0, clamp(headTurn((aim[0] - nk[0]) * HB.face, aim[1] - nk[1]), -HEAD.Up, HEAD.Down), v);
      HB.P = poseOf(HB);
      return HB;
    }

    // ---------- the light buffer: everything of the drone that glows, added onto the picture ----------
    function lightStart(T) {
      const g = T.hbg; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.hb.width, T.hb.height);
      g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalCompositeOperation = 'lighter'; g.lineCap = 'round'; g.lineJoin = 'round';
      return g;
    }
    function lightEnd(T) { const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0); c.restore(); }

    function draw(T) {
      const c = T.c, st = T.st, way = T.cand.id, u = st.u;
      const X = { c, g: null, D: st.D, A: st.A, t: st.t, u, hot: st.hot, rd: null };
      X.eye = [X.D[0] + Math.cos(X.A) * 2.2 * DS * DSC, X.D[1] + Math.sin(X.A) * 2.2 * DS * DSC];
      // the world, then black over it, then the void's things
      if (st.v < .999) { drawBack(T); c.setTransform(T.S, 0, 0, T.S, 0, 0); }
      voidBack(c, st.v);
      // the gauge first, so the rim lights know where the row is (drawn later, onto the buffer)
      const sh = st.shape;
      X.rd = sh && sh.kind === 'scan' && sh.dots ? { ph: sh.dots.ph, home: 0, lit: 0 } : null;
      if (X.rd) { let h = 0, l = 0; for (let j = 0; j < NDOT; j++) { const D = sh.dots; if (D.ph !== 'home' && dotQ(j, D) >= 1) { h++; l += litOf(D.r, j); } } X.rd.home = h; X.rd.lit = l; }
      const dm = { form: st.form, u, v: st.v, hud: st.hud, n: st.n, red: st.alarm ? st.alarm.red : 0, glyph: st.alarm ? st.alarm.id : null, dart: st.dart };
      if (st.form === 'scan') drawScanProps(c, X, st, way);
      else if (st.form === 'lamp') { beam(c, X, st, way); const o = drawLampProps(c, X, st, way); Object.assign(dm, { light: st.light, inCone: st.inCone, seen: st.seen, pic: o.pic, diag: o.diag }); }
      else { drawFrost(c, X, st, way); drawMeteor(c, X, st, way); }
      // the hero, held; dark too in «Только свет», lit by the drone and by the meteor going by
      const h = heroOf(st, X);
      if (way === 'glow') {
        const m = st.met, near = m ? c01(1 - dist(m.head, [HX, FLOOR_Y - 90]) / 420) : 0, lit = lerp(1, .3 + .5 * near, st.v);
        c.save(); if (lit < .999) c.filter = `brightness(${lit.toFixed(3)})`; drawHero(c, h); c.restore();
      } else drawHero(c, h);
      // the drone, its shape and the gauge, glowing onto the buffer
      const g = X.g = lightStart(T);
      const b = drawBody(X, sh), rd = sh && sh.kind === 'scan' ? drawDots(X, sh) : null;
      lightEnd(T);
      c.setTransform(T.S, 0, 0, T.S, 0, 0);
      vignette(c, st.v);
      hudStandIn(c, st.hud);
      Object.assign(dm, { seated: b.seated, away: b.away, mirror: b.nm ? b.mirror / b.nm : 0, rd, D: st.D, A: st.A,
        nums: [st.D[0], st.D[1], st.A, h.y, h.look, st.v, st.hud, ...(st.props || []).flatMap(q => [q.p[0], q.p[1], q.a])] });
      T.dm = dm;
    }

    const SEQ = 'Видение 5 с: вход 0,6 · показ 3,8 · выход 0,6. Сканер: прицел на 1 с, точки на целом с 1,75 с, на треснувшем с 2,75 с, домой с 3,6 с · фонарь: взгляд на лампу 1 с, чаша 1,3 с, луч 1,9–4 с, ящик в луче к 3 с · тревоги: холод с 0,7 с, снежинка 1–2,6 с, метеор 2,6–3,4 с, его знак 2,6–4,2 с.';
    const cands = [
      { id: 'real', name: 'Настоящие', idea: 'Вещи — те же картинки, что в игре: кусок отсека, сломанная машина, лампа, ящик, камень. Трещины тушью, как у обломков сейчас. Ящик виден только в луче. Холод — акварельный иней с края, метеор — камень с огненным хвостом.', note: 'Картинки 1:1 из game/art/hd: debris/section_01, machine_01, crate_03, smallrock_03 (×0,55), machine_lamp.' },
      { id: 'ember', name: 'Глазами дрона', idea: 'Вещи такими, какими их видит дрон: схемы из тех же нитей и искр, что его формы. Контур и главные линии, по контуру бегут искры, трещина рдеет красным. Ящик дорисовывается там, где прошёл луч, и остаётся. Холод — колючие знаки с края, у метеора впереди пунктир пути.', note: 'Схема прорисовывается за 1 с, пока вещь выплывает · искра по контуру каждые 12 px, 28 px/с · контуры сняты с игровых картинок.' },
      { id: 'trace', name: 'Проступает схема', idea: 'Сначала настоящие картинки. Когда дрон берёт вещь — прицелом, взглядом, лучом, тревогой, — по ней пробегает нить искр, картинка темнеет и остаётся схема: дрон её понял.', note: 'Схема пробегает за 0,7 с, картинка темнеет до 35%: первый обломок с 1,6 с, второй с 2,75 с, лампа с 1 с, ящик вслед за лучом, холод с 1 с, метеор с 2,65 с.' },
      { id: 'glow', name: 'Только свет', idea: 'В пустоте нет света: вещи — чёрные силуэты, видны лишь по отсвету. Края горят от углей дрона и от ряда точек, ящик виден только в луче, метеор освещает всё на пролёте, иней поблёскивает холодом. Герой тоже тёмный.', note: 'Вещи — те же картинки на 13% яркости · край светится, если смотрит на огонь, и тем ярче, чем огонь ближе · герой на 30%, на пролёте метеора светлее.' },
    ];
    for (const cd of cands) cd.time = `${SEQ} ${cd.note}`;
    const SAY = {
      scan: [[0, 'мир гаснет, HUD уходит'], [VIS.in, 'пустота: герой висит, дрон перед ним'], [.9, 'из темноты выплывают два обломка'], [TS.formA, 'дрон собирает прицел'], [TS.read, 'прицел на целом: полный ряд точек'], [TS.hop, 'прыжок на треснувший: ряд наполовину'], [TS.home, 'точки домой, прицел закрывается'], [VIS.len - VIS.out, 'мир возвращается']],
      lamp: [[0, 'мир гаснет, HUD уходит'], [VIS.in, 'в темноте висит лампа'], [1.0, 'дрон смотрит на лампу'], [TS.lampAt, 'дрон копирует её чашу'], [TS.lightOn, 'луч идёт по темноте'], [2.7, 'луч находит ящик, которого не было видно'], [TS.lightOff, 'свет гаснет'], [VIS.len - VIS.out, 'мир возвращается']],
      alarms: [[0, 'мир гаснет, HUD уходит'], [VIS.in, 'пустота'], [TS.frost[0], 'с края ползёт холод'], [1.0, 'тревога: снежинка, дрон краснеет'], [TS.meteor[0], 'метеор: дрон дёргается, знак метеора'], [VIS.len - VIS.out, 'мир возвращается']],
    };

    return {
      cands, expose: { VIS, TS, READS, NDOT, FORMS },
      init(T) { T.time = 0; T.t0 = 0; T.heroes = []; T.form = OPT.form; T.st = stateOf(T); },
      onScale(T) { const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S); T.hb = cv; T.hbg = cv.getContext('2d'); },
      step(T) {
        if (T.form !== OPT.form) { T.form = OPT.form; T.t0 = T.time; }   // another form starts its vision from the top
        T.st = stateOf(T);
      },
      back(T) { draw(T); },
      hud(T) {
        const st = T.st, ev = SAY[st.form]; let say = ev[0][1];
        for (const q of ev) if (st.u >= q[0]) say = q[1];
        return `${RU[st.form]} · ${fmt(st.u)} с · ${say}`;
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    long: [['const VIS = { in: .6, out: .6, len: 5 };', 'const VIS = { in: .6, out: .6, len: 5.6 };']],
    hudStays: [['sm((u - (VIS.len - .3)) / .3) : 0;', 'sm((u - (VIS.len - .3)) / .3) : .2;']],
    half: [['{ left: 150, max: 300 }', '{ left: 170, max: 300 }']],
    twice: [['if (pt.seeds && outOf(r, D)) { out.away++; continue; }', 'if (pt.seeds && outOf(r, D)) { out.away++; }']],
    seen: [['c.save(); const k = coneOf(X.D, st.beamA, 900);', 'c.save(); const k = coneOf(X.D, st.beamA + 1.2, 900);'], ['const a = st.light * st.inCone * st.v;', 'const a = st.light * st.v;']],
    early: [["st.seen = u >= TS.sweep[1] ? 1 : u < TS.lightOn ? 0 : st.inCone;", 'st.seen = 1;']],
    dark: [['sm(c01((u - TS.lightOn) / .15))', 'sm(c01((u - TS.lampAt) / .15))']],
    three: [["{ id: 'meteor', t0: 2.6, tin: .4, hold: .9, tout: .3 }];", "{ id: 'meteor', t0: 2.6, tin: .4, hold: .5, tout: .3 }, { id: 'cold', t0: 3.9, tin: .2, hold: .2, tout: .2 }];"]],
    still: [['bump(u, 2.6, 2.68, 3.05) : 0;', 'bump(u, 1.6, 1.68, 2.05) : 0;']],
    nan: [['const HX = 780, DV', 'const HX = NaN, DV']],
  },

  // Every candidate: the 'all' loop (scan, lamp, alarms) once, then a pause, slow, and the alarms alone; then a pick.
  // Per frame: numbers finite; the veil whole while shown and clear at both ends; the HUD gone while the void is up; no mark's
  // spark drawn twice. Per scan: the row on the whole piece full (26 of 26), on the cracked one half (13). Per lamp: the light
  // only on a formed mirror, the crate's picture only where the light falls, its diagram only once the beam came. Per alarms:
  // exactly two dangers, cold then meteor; red only while a glyph is up; the dart only with the meteor's.
  check(K) {
    const X = K.X, ALL = 930, FLIP = 1080, N = FLIP + 330, SPEC = { in: .6, out: .6, len: 5 };   // the decided scene, not the page's own numbers
    K.at(950, () => K.seg('playMode', 1)); K.at(1000, () => K.seg('playMode', 0));
    K.at(1010, () => K.seg('speedMode', 1)); K.at(1060, () => K.seg('speedMode', 0));
    K.at(FLIP, () => K.seg('formMode', 3));
    K.at(N - 40, () => K.click(2, '.pick'));
    let st = '';
    K.at(N - 20, () => { st = K.byId.status.textContent; K.byId.copy.listeners.click[0](); });
    const S = K.tiles.map(() => ({ cur: null, done: { scan: 0, lamp: 0, alarms: 0 }, lines: [] }));
    const close = (s, j) => {
      const L = s.cur, id = K.tiles[j].cand.id; if (!L) return;
      if (L.u0 > .05 || L.uMax < SPEC.len - .05) return;   // cut by a switch or the run's end
      if (L.form === 'scan' && !(L.fullA > 0 && L.halfB > 0)) K.fail(`${id}: a scan without its full row (${L.fullA}) and half row (${L.halfB})`);
      if (L.form === 'lamp' && !(Math.max(L.pic, L.diag) > .9)) K.fail(`${id}: the lamp never showed the crate (picture ${L.pic.toFixed(2)}, diagram ${L.diag.toFixed(2)})`);
      if (L.form === 'alarms' && L.seq.join(',') !== 'cold,meteor') K.fail(`${id}: alarms ${L.seq.join(',')}, not cold then meteor`);
      s.done[L.form]++;
      if (s.lines.length < 4) s.lines.push(`${L.form}: ${L.form === 'scan' ? `full ${L.fullA} f, half ${L.halfB} f` : L.form === 'lamp' ? `crate ${L.pic.toFixed(2)}/${L.diag.toFixed(2)}, light ${L.light} f` : `${L.seq.join('>')}, red ${L.red} f, dart ${L.dart} f`}`);
    };
    let paused = 0, lastT = -1;
    K.run(N, (t, i) => {
      const j = K.tiles.indexOf(t), s = S[j], d = t.dm, id = t.cand.id; if (!d) return;
      if (d.nums.some(v => !isFinite(v))) K.fail(`${id}: a number not finite, frame ${i}`);
      if (j === 0) { if (!K.OPT.playing) { if (t.time !== lastT && lastT >= 0) K.fail('time ran while paused'); paused++; } lastT = t.time; }
      if (!K.OPT.playing) return;
      if (!s.cur || d.form !== s.cur.form || d.n !== s.cur.n) { close(s, j); s.cur = { form: d.form, n: d.n, u0: d.u, uMax: d.u, fullA: 0, halfB: 0, pic: 0, diag: 0, light: 0, seq: [], red: 0, dart: 0 }; }
      const L = s.cur, at = `${d.form} ${d.u.toFixed(2)} s`; L.uMax = Math.max(L.uMax, d.u);
      // the vision's frame: 0.6 in, shown, 0.6 out; the HUD away while the void is up
      if (d.u >= SPEC.in + .02 && d.u <= SPEC.len - SPEC.out - .02 && d.v < .999) K.fail(`${id}: the void at ${d.v.toFixed(3)} while shown, ${at}`);
      if ((d.u < .02 || d.u > SPEC.len - .02) && d.v > .01) K.fail(`${id}: the void at ${d.v.toFixed(3)} at the vision's edge, ${at}`);
      if (d.u > SPEC.len + .02) K.fail(`${id}: the vision runs past ${SPEC.len} s, ${at}`);
      if (d.hud > .01 && d.v > .99) K.fail(`${id}: the HUD at ${d.hud.toFixed(2)} in the void, ${at}`);
      if (d.form === 'scan') {
        const r = d.rd;
        if (r && d.seated + r.drawn > X.NDOT) K.fail(`${id}: ${d.seated} marks seated and ${r.drawn} dots out, ${at}`);
        if (r && r.ph === 'a' && r.home === X.NDOT) { if (Math.abs(r.lit - X.NDOT) > 1e-6) K.fail(`${id}: the whole piece lit ${r.lit}, ${at}`); else L.fullA++; }
        if (r && r.ph === 'hop' && r.home === X.NDOT) { const want = X.READS[1].left * X.NDOT / X.READS[1].max; if (Math.abs(r.lit - 13) > 1e-6 || Math.abs(want - 13) > 1e-6) K.fail(`${id}: the cracked piece lit ${r.lit}, ${at}`); else L.halfB++; }
      }
      if (d.form === 'lamp') {
        if (d.light > .5 && d.mirror < .9) K.fail(`${id}: the light on with the mirror at ${d.mirror.toFixed(2)}, ${at}`);
        if (d.pic > d.light * d.inCone * d.v + 1e-6) K.fail(`${id}: the crate's picture ${d.pic.toFixed(2)} outside the light (${d.light.toFixed(2)} × ${d.inCone.toFixed(2)}), ${at}`);
        if (d.diag > 1e-6 && d.u < X.TS.lightOn) K.fail(`${id}: the crate drawn at ${d.diag.toFixed(2)} before the beam, ${at}`);
        if (d.light > 0) L.light++;
        L.pic = Math.max(L.pic, d.pic); L.diag = Math.max(L.diag, d.diag);
      }
      if (d.form === 'alarms') {
        if (d.glyph && L.seq[L.seq.length - 1] !== d.glyph) L.seq.push(d.glyph);
        if (d.red > 0 && !d.glyph) K.fail(`${id}: red with no glyph up, ${at}`);
        if (d.dart > 1e-6 && d.glyph !== 'meteor') K.fail(`${id}: the dart without the meteor's glyph, ${at}`);
        if (d.red > 0) L.red++; if (d.dart > 1e-6) L.dart++;
      }
      if (i === ALL && j === 0 && Object.values(s.done).some(n => n < 1)) K.fail(`by frame ${ALL} not every form played whole: ${JSON.stringify(s.done)}`);
    });
    S.forEach((s, j) => close(s, j));
    const doc = K.picked(), fb = K.byId.fallback.value, cand = K.tiles[2].cand;
    S.forEach((s, j) => console.log(`${K.tiles[j].cand.id}: whole ${JSON.stringify(s.done)} | ${s.lines.join(' || ')}`));
    console.log('paused frames', paused, '| pick doc:', JSON.stringify(doc), '| copy text:', fb);
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    need(K.tiles.length === 4, 'four candidates');
    need(S.every(s => s.done.scan >= 1 && s.done.lamp >= 1 && s.done.alarms >= 2), 'every candidate: a whole scan, lamp and two alarms');
    need(paused > 30, 'the pause held');
    need(this.pick.doc === 'picks/formVision', 'the pick at picks/formVision');
    need(doc && doc.pick === cand.id, 'the pick is in the db doc');
    need(st.includes(`«${cand.name}»`), 'the status names the pick');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the copy text names the pick');
    return ok;
  },
};
