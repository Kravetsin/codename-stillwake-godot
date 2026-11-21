// The form vision (docs/plans/form-vision.md): when the light drone learns a form, the world fades to black, the hero floats
// held, and the drone shows what the form does, one thing at a time: a thing arrives alone, holds while the eye reads it, the
// drone takes it, the result holds. The look is picked («Проступает схема»: the game's pictures, a spark thread running over a
// thing as the drone takes it, the picture darkening and its diagram staying); the open question is how the eye is led: a
// spotlight, a frame that follows, or a thread from the drone's eye. An art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/form-vision.js <out-dir> [--break <name>]
module.exports = {
  title: 'Видение формы',
  heading: 'Видение формы: куда смотреть',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Вид выбран: «Проступает схема». Теперь темп. Видение играется один раз на форму, это знакомство с ней, поэтому вещи идут по одной: вещь приходит, стоит, пока её разглядываешь, дрон её берёт, результат стоит. Пока что-то движется, новое не появляется. Длина: сканер 13,8 с, фонарь 12,7 с, тревоги 13,5 с.</p>
<p class="lead">Решаем одно: <b>как вести взгляд</b>. Три способа, сценарий у всех один.</p>
<p class="miss">Не показано: звук; иконка, летящая в «Изучения»; что видят друзья; мир до и после — заглушка; отрисовка Godot.</p>`,
  toggles: [{ id: 'formMode', bar: 1, label: 'Форма:', key: 'form', opts: [['все три по очереди', 'all'], ['сканер · 13,8 с', 'scan'], ['фонарь · 12,7 с', 'lamp'], ['тревоги · 13,5 с', 'alarms']] }],
  pick: { doc: 'picks/formVisionFocus', subject: 'Видение формы, фокус внимания', what: ['вариант', 'вариантом'] },
  pictures: {
    section: 'game/art/hd/debris/section_01.png', machine: 'game/art/hd/debris/machine_01.png', lamp: 'game/art/hd/machine_lamp.png',
    crate: 'game/art/hd/debris/crate_03.png', rock: 'game/art/hd/debris/smallrock_03.png',
  },

  page() {
    const TAU = Math.PI * 2, N = 120;   // DroneInk.Sparks
    const VIS = { in: .6, out: .6 };   // the world going and coming back, s
    const FORMS = ['scan', 'lamp', 'alarms'], RU = { scan: 'сканер', lamp: 'фонарь', alarms: 'тревоги' };
    // DroneInk: a spark into a shape late by its share of Lag over Span, out of it by LagOut over SpanOut
    const LAG = .45, SPAN = .55, LAG_OUT = .4, SPAN_OUT = .6;
    // ScanDots: 26 dots, each a 26th of the wreck's maximum, pick px over the world; a dot's flight, s
    const NDOT = 26, DOT = { step: 7, side: 3.2, above: 20, fl: .3 };
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
    const fmt1 = v => v.toFixed(1).replace('.', ',');

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
        blow: [70, 82],
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

    // ---------- the beats: each form's in order, s; a thing arrives, is read, taken, its result read, then it goes ----------
    // [the first beat's start, [name, length], …]; the vision is the last beat's end and the way out
    const SEQ = {
      scan: [.4, ['aIn', 1.1], ['aRead', 1.25], ['form', .5], ['formHold', .4], ['aTrace', .7], ['aDots', 1.3], ['aResult', 1.15], ['aOut', .7], ['aGap', .25],
        ['bIn', 1.1], ['bRead', 1.2], ['bAim', .3], ['bTrace', .7], ['bDots', 1.05], ['bResult', 1.1]],
      lamp: [.5, ['lIn', 1.2], ['lRead', 1.3], ['go', 1.0], ['lTrace', .8], ['dish', .6], ['lResult', 1.2], ['lOut', 1.0], ['lGap', .25],
        ['lightOn', .3], ['sweep', 2.0], ['cTrace', .7], ['cResult', 1.25]],
      alarms: [.6, ['cIn', 1.4], ['cRead', 1.2], ['cGlyph', .6], ['cHold', .4], ['cTrace', .6], ['cResult', 1.1], ['cOut', .9], ['mGap', .25],
        ['mIn', .8], ['mRead', 1.2], ['dart', .6], ['mHold', .4], ['mTrace', .6], ['mResult', 1.1], ['pass', 1.15]],
    };
    function beatsOf(s) { const B = {}; let t = s[0]; for (let i = 1; i < s.length; i++) { B[s[i][0]] = [t, t + s[i][1]]; t += s[i][1]; } B.end = [t, t + VIS.out]; B.len = t + VIS.out; return B; }
    const BT = {}, LEN = {};
    for (const f of FORMS) { BT[f] = beatsOf(SEQ[f]); LEN[f] = BT[f].len; }
    const ph = (w, u) => w[1] <= w[0] ? (u >= w[0] ? 1 : 0) : c01((u - w[0]) / (w[1] - w[0]));
    const ez = (w, u) => sm(ph(w, u));
    function clockOf(T) {
      const seq = OPT.form === 'all' ? FORMS : [OPT.form], L = seq.reduce((a, f) => a + LEN[f], 0);
      const el = T.time - T.t0, cyc = Math.floor(el / L), r = el - cyc * L;
      let n = 0, acc = 0; while (n < seq.length - 1 && r >= acc + LEN[seq[n]]) { acc += LEN[seq[n]]; n++; }
      return { form: seq[n], u: r - acc, n: cyc * seq.length + n, len: LEN[seq[n]] };
    }
    const veilOf = (u, len) => u < VIS.in ? sm(u / VIS.in) : u > len - VIS.out ? 1 - sm((u - (len - VIS.out)) / VIS.out) : 1;
    const hudOf = (u, len) => u < .3 ? 1 - sm(u / .3) : u > len - .3 ? sm((u - (len - .3)) / .3) : 0;

    // ---------- the things: where each rests, comes from and goes ----------
    const RA = [330, 236], RB = [345, 250], LAMP_P = [470, 150], LAMP_PIV = [470, -12], LD = [560, 204], CRATE_P = [300, 420], COLD_P = [140, 270];
    const MF = [905, 70], MN = [893, 79], MP = [-150, 390], DART = (() => { const d = unit([MF[0] - DV[0], MF[1] - DV[1]]); return [d[0] * 40, d[1] * 40]; })();
    // the pieces: hit points left of the most; the first whole, the second cracked to half
    const READS = [{ left: 520, max: 520 }, { left: 150, max: 300 }];
    const ROWS = [[RA[0], RA[1] - 64 - DOT.above], [RB[0], RB[1] - 64 - DOT.above]];   // the gauge's row over each piece at rest
    const REST_A = Math.atan2(46, -22);   // the lamp's way with nothing to look at (stepDrone)
    const SW0 = angTo(DV, LAMP_P), SW1 = angTo(DV, CRATE_P);   // the beam from where the lamp hung down to the crate
    const floatOf = (t, k) => [2 * Math.sin(t * .5 + k), 2 * Math.sin(t * .7 + k * 1.3)];   // a thing at rest still floats a little

    function scanProps(u, B, t) {
      const ia = ez(B.aIn, u), oa = ez(B.aOut, u), ib = ez(B.bIn, u), fa = floatOf(t, 0), fb = floatOf(t, 2);
      const pa = [lerp(-160, RA[0], ia) - 60 * oa, lerp(190, RA[1], ia) + 50 * oa], pb = [lerp(190, RB[0], ib), lerp(650, RB[1], ib)];
      const A = put(PR.section, [pa[0] + fa[0], pa[1] + fa[1]], lerp(-.3, -.05, ia) - .1 * oa);
      A.al = sm(ph(B.aIn, u) / .7) * (1 - oa); A.base = pa;
      const Bq = put(PR.machine, [pb[0] + fb[0], pb[1] + fb[1]], lerp(.25, .05, ib));
      Bq.al = sm(ph(B.bIn, u) / .7); Bq.crack = 1; Bq.base = pb;
      return [A, Bq];
    }
    function lampProps(u, B, t) {
      const sw = .03 * Math.sin(t * 1.1), top = [LAMP_PIV[0] - Math.sin(sw) * 132, LAMP_PIV[1] + Math.cos(sw) * 132];
      const Lm = put(PR.lamp, [top[0] - Math.sin(sw) * 30, top[1] + Math.cos(sw) * 30], sw);
      Lm.al = ez(B.lIn, u) * (1 - ez(B.lOut, u)); Lm.top = top;
      const f = floatOf(t, 4), C = put(PR.crate, [CRATE_P[0] + f[0], CRATE_P[1] + f[1]], .12);
      return [Lm, C];
    }
    const frostX = (u, B) => lerp(-10, 260, ez(B.cIn, u)) - 270 * ez(B.cOut, u);
    const frostA = (u, B) => sm(ph(B.cIn, u) * 2.5) * (1 - sm((ph(B.cOut, u) - .4) / .6));
    const edgeX = (x, y) => x + 16 * Math.sin(y * .045 + 1.3) + 9 * Math.sin(y * .13);
    const FROSTS = (() => { const r = rng(321), out = []; for (let i = 0; i < 46; i++) out.push({ x: 4 + Math.pow(r(), 1.4) * 280, y: 10 + r() * 520, s: 4 + r() * 7, a: r() * TAU, tw: r() * TAU }); return out; })();
    // the meteor: a glint far off creeping nearer, then it passes
    function meteorOf(u, B) {
      if (u < B.mIn[0] || u >= B.pass[1]) return null;
      const c = c01((u - B.mIn[0]) / (B.pass[0] - B.mIn[0])), e = ez(B.pass, u);
      return { head: e > 0 ? lerp2(MN, MP, e) : lerp2(MF, MN, c), dir: unit([MP[0] - MN[0], MP[1] - MN[1]]), s: lerp(.32, .4, c) + .35 * e, al: ez(B.mIn, u), spin: u * .8 };
    }
    const alarmsOf = B => [{ id: 'cold', t0: B.cGlyph[0], tin: .6, t1: B.cOut[0], tout: .6 }, { id: 'meteor', t0: B.dart[0], tin: .6, t1: B.pass[0], tout: 1 }];
    function alarmOf(u, B) {
      for (const a of alarmsOf(B)) {
        if (u < a.t0 || u >= a.t1 + a.tout) continue;
        const f = u < a.t0 + a.tin ? sm((u - a.t0) / a.tin) : u < a.t1 ? 1 : 1 - sm((u - a.t1) / a.tout);
        return { ...a, f, red: f * (.6 + .4 * Math.sin(u * TAU * 2.4)) };   // DroneInk AlarmLow, AlarmSwing, AlarmBeats
      }
      return null;
    }

    // ---------- the gauge (ScanDots): out from the marks one by one to the row over a piece, home together ----------
    function dotPhases(B) {
      return [
        { t0: B.aDots[0], st: (B.aDots[1] - B.aDots[0] - DOT.fl) / (NDOT - 1), lag: 0, fl: DOT.fl, from: 'seat', to: 0 },
        { t0: B.aOut[0], st: 0, lag: .25, fl: .45, from: 0, to: 'seat' },
        { t0: B.bDots[0], st: (B.bDots[1] - B.bDots[0] - DOT.fl) / (NDOT - 1), lag: 0, fl: DOT.fl, from: 'seat', to: 1 },
        { t0: B.end[0], st: 0, lag: .2, fl: .4, from: 1, to: 'seat' },
      ];
    }
    // each dot's flight now, or null while its spark sits in its mark
    function dotsAt(u, B) {
      const P = dotPhases(B);
      return Array.from({ length: NDOT }, (_, j) => {
        let cur = null;
        for (const p of P) { const s = p.t0 + j * p.st + hsh(j + 2) * p.lag; if (u >= s) cur = { p, q: c01((u - s) / p.fl) }; }
        return !cur || (cur.p.to === 'seat' && cur.q >= 1) ? null : cur;
      });
    }
    const outOf = (j, D) => !!(D && D[j]);
    const awayOf = d => !d ? 0 : d.p.from === 'seat' ? sm(d.q) : d.p.to === 'seat' ? 1 - sm(d.q) : 1;
    const litOf = (r, j) => c01(r.left * NDOT / r.max - j);
    const rowAt = (O, j) => [O[0] + (j - (NDOT - 1) / 2) * DOT.step, O[1]];
    // how far the gauge has gone over piece k: its dots there, a 26th each
    const dotsAct = (D, k) => D.reduce((a, d) => a + (!d ? 0 : d.p.to === k ? sm(d.q) : d.p.from === k ? 1 - sm(d.q) : 0), 0) / NDOT;

    // ---------- the focus: where the eye is led, eased between keys inside the beats that move anyway ----------
    // a key: [window, kind, the thing's point, its radius]; kind 'home' (nothing), 'thing' (the thing alone), 'act' (it and the drone), 'drone'
    const beamTip = st => { const d = dist(DV, CRATE_P); return [st.Db[0] + Math.cos(st.A) * d, st.Db[1] + Math.sin(st.A) * d]; };
    function focusKeys(st) {
      const B = st.B;
      if (st.form === 'scan') return [['home', RA, 120], [B.aIn, 'thing', RA, 120], [B.form, 'act', RA, 120], [B.aOut, 'home', RA, 120], [[B.aGap[0], B.aGap[0]], 'home', RB, 120],
        [B.bIn, 'thing', RB, 120], [B.bAim, 'act', RB, 120], [B.end, 'home', RB, 120]];
      if (st.form === 'lamp') return [['home', LAMP_P, 70], [B.lIn, 'thing', LAMP_P, 70], [B.go, 'act', LAMP_P, 70], [B.lOut, 'drone', LAMP_P, 70],
        [[B.lightOn[0], B.sweep[1]], 'act', beamTip(st), 80], [B.end, 'home', CRATE_P, 80]];
      return [['home', COLD_P, 190], [B.cIn, 'thing', COLD_P, 190], [B.cGlyph, 'act', COLD_P, 190], [B.cOut, 'home', COLD_P, 190], [[B.mGap[0], B.mGap[0]], 'home', MF, 80],
        [B.mIn, 'thing', MF, 80], [B.dart, 'act', MF, 80], [B.pass, 'home', MF, 80]];
    }
    // the focus as numbers: the frame [cx, cy, zoom], the spot [x, y, r, on], the drone's spot, the thread [on, x, y]
    function focusVec(kind, at, r, D) {
      let cx = LW / 2, cy = LH / 2, z = 1;
      if (kind === 'thing') { cx = at[0]; cy = at[1]; z = 1.3; }
      if (kind === 'act') { cx = (at[0] + D[0]) / 2; cy = (at[1] + D[1]) / 2; z = clamp(Math.min(LW * .8 / (Math.abs(at[0] - D[0]) + 2 * r), LH * .8 / (Math.abs(at[1] - D[1]) + 2 * r)), 1, 1.3); }
      if (kind === 'drone') { cx = D[0]; cy = D[1]; z = 1.15; }
      const on = kind === 'thing' || kind === 'act' ? 1 : 0;
      return [cx, cy, z, at[0], at[1], r, on, kind === 'act' || kind === 'drone' ? 1 : 0, on, at[0], at[1]];
    }
    function focusOf(st) {
      const keys = focusKeys(st), u = st.u;
      let F = focusVec(keys[0][0], keys[0][1], keys[0][2], st.Db);
      for (let i = 1; i < keys.length; i++) {
        const [w, kind, at, r] = keys[i]; if (u < w[0]) break;
        const to = focusVec(kind, at, r, st.Db), e = ez(w, u), lead = sm(ph(w, u) * 1.7);   // the thread gets there first
        F = F.map((x, j) => lerp(x, to[j], j >= 8 ? lead : e));
      }
      return F;
    }
    const camOf = F => { const z = F[2], hw = LW / 2 / z, hh = LH / 2 / z; return [clamp(F[0], hw, LW - hw), clamp(F[1], hh, LH - hh), z]; };
    function aimTrack(keys, u) { let A = keys[0]; for (let i = 1; i < keys.length; i++) { const [w, a] = keys[i]; if (u < w[0]) break; A = angLerp(A, a, ez(w, u)); } return A; }

    // ---------- the scene's state at a moment ----------
    function stateOf(T) {
      const ck = clockOf(T), u = ck.u, t = T.time, B = BT[ck.form], v = veilOf(u, ck.len);
      const st = { form: ck.form, u, n: ck.n, len: ck.len, v, hud: hudOf(u, ck.len), t, B, light: 0, dart: 0, alarm: null, inCone: 0, fC: 0 };
      if (ck.form === 'scan') {
        st.Db = DV;
        st.A = aimTrack([REST_A, [B.form, angTo(DV, RA)], [B.bAim, angTo(DV, RB)], [B.end, REST_A]], u);
        st.props = scanProps(u, B, t);
        st.fA = ez(B.aTrace, u); st.fB = ez(B.bTrace, u);
        st.dots = dotsAt(u, B);
      } else if (ck.form === 'lamp') {
        st.Db = lerp2(DV, LD, ez(B.go, u) * (1 - ez(B.lOut, u)));
        st.A = aimTrack([REST_A, [B.go, angTo(LD, LAMP_P)], [B.lOut, SW0], [B.sweep, SW1], [[B.end[0] + .25, B.len], REST_A]], u);
        st.props = lampProps(u, B, t);
        st.fL = ez(B.lTrace, u);
        st.light = ez(B.lightOn, u) * (1 - sm(c01((u - B.end[0]) / .25)));
        st.fC = ez(B.cTrace, u);
      } else {
        st.dart = ez(B.dart, u) * (1 - sm(ph(B.pass, u) / .6));
        st.Db = [DV[0] + DART[0] * st.dart, DV[1] + DART[1] * st.dart];
        st.A = aimTrack([REST_A, [B.cGlyph, Math.PI], [B.cOut, REST_A], [B.dart, angTo(DV, MF)], [B.pass, REST_A]], u);
        st.props = [];
        st.fx = frostX(u, B); st.fa = frostA(u, B); st.fCold = ez(B.cTrace, u);
        st.met = meteorOf(u, B); st.fM = ez(B.mTrace, u);
        st.alarm = alarmOf(u, B);
      }
      // the drone: from its place by the helmet into the void before him, a slow bob
      const DW = [HX + 36, FLOOR_Y - 176], d0 = lerp2(DW, st.Db, v);
      st.D = [d0[0], d0[1] + 1.5 * Math.sin(t * 1.7)];
      st.hot = st.alarm ? mix3(EMBER, RED, Math.min(1, st.alarm.red * 1.4)) : EMBER;
      if (ck.form === 'lamp') st.inCone = sm(c01(1 - angDiff(st.A, angTo(st.D, CRATE_P)) / HALF));
      st.F = focusOf(st); st.cam = camOf(st.F);
      st.shape = shapeOf(st);
      return st;
    }
    // the shape the drone holds now: its parts' polylines in page px, its sparks' gather times
    function shapeOf(st) {
      const u = st.u, B = st.B, F = frameOf(st.D, st.A);
      if (st.form === 'scan') {
        const L = sm((u - B.aTrace[0]) / .3) * (1 - sm((u - B.end[0]) / .4));
        const polys = reticle(L).map(pl => pl.map(l => l.map(q => toW(F, q))));
        return {
          kind: 'scan', al: AL.scan, t0: B.form[0], t1: B.end[0], tin: B.form[1] - B.form[0], tout: VIS.out, dots: st.dots,
          parts: [{ seeds: true, polys: polys[0] }, { polys: polys[1], P: mkPath(polys[1]), L0: L0.ring, flow: 12 }, { polys: polys[2], P: mkPath(polys[2]), L0: L0.ticks, flow: 8 }],
          seats: Array.from({ length: NDOT }, (_, j) => toW(F, R(10.2, markA(j)))),
        };
      }
      if (st.form === 'lamp') {
        const polys = LAMP_LINES.map(l => l.map(q => toW(F, q)));
        return { kind: 'lamp', al: AL.lamp, t0: B.dish[0], t1: B.end[0] + .3, tin: B.dish[1] - B.dish[0], tout: .3, parts: [{ polys, P: mkPath(polys), L0: L0.lamp, flow: 30 }] };
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
        pt.polys.forEach((l, j) => { const a = form * (1 - awayOf(D && D[j])); lines(g, [l]); g.lineWidth = .45; g.strokeStyle = css(X.hot, .3 * a); g.stroke(); });
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
    // the gauge's dots: from the marks to a row, from a row home
    function drawDots(X, sh) {
      const c = X.c, g = X.g; let home = 0, lit = 0, drawn = 0, to = null;
      sh.dots.forEach((d, j) => {
        if (!d) return;
        const at = w => w === 'seat' ? sh.seats[j] : rowAt(ROWS[w], j);
        const start = at(d.p.from), goal = at(d.p.to), e = sm(d.q), there = d.q >= 1;
        const wx = goal[0] - start[0], wy = goal[1] - start[1], dd = Math.hypot(wx, wy), fly = !there && dd > 1e-4, p = [start[0] + wx * e, start[1] + wy * e];
        const lt = d.p.to !== 'seat' ? litOf(READS[d.p.to], j) * e : d.p.from !== 'seat' ? litOf(READS[d.p.from], j) * (1 - e) : 0;
        const fl = .82 + .18 * Math.sin(X.t * 9 + j * 2.3), dotHeat = lt > 0 ? (.3 + .62 * lt) * fl : .04;
        const aw = awayOf(d), heat = lerp(formHeat(j, X.t), dotHeat, aw), side = lerp(SPARK, DOT.side, aw);
        if (fly && j % 2 === 0) line2(g, start, p, .35, EMBER, .07);
        mote(c, g, [p[0], p[1], fly ? wx / dd : 0, fly ? wy / dd : 0], side, heat, EMBER);
        drawn++;
        if (d.p.to !== 'seat') { to = d.p.to; if (there) { home++; lit += litOf(READS[d.p.to], j); } }
      });
      return { ph: to === null ? 'home' : to === 0 ? 'A' : 'B', home, lit, drawn };
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

    // ---------- «Проступает схема»: the picture, a spark thread over it as the drone takes it, the picture darkening ----------
    function picture(c, q, al) {
      const im = IMG[q.pr.img]; if (al <= .003 || !ready(im)) return;
      c.save(); c.globalAlpha = al; c.translate(q.p[0], q.p[1]); c.rotate(q.a);
      c.drawImage(im, -q.pr.w * q.s / 2, -q.pr.h * q.s / 2, q.pr.w * q.s, q.pr.h * q.s); c.restore();
    }
    function inkCracks(c, q, al) {
      if (!q.crack || al <= .003) return;
      const k = cracksOf(q); c.save(); c.lineCap = 'round'; c.lineJoin = 'round';
      c.save(); c.translate(.7, .9); lines(c, k); c.strokeStyle = css([160, 168, 178], .35 * al); c.lineWidth = .7; c.stroke(); c.restore();
      lines(c, k); c.strokeStyle = css([22, 23, 27], .95 * al); c.lineWidth = 1.3; c.stroke(); c.restore();
    }
    function emberLines(c, polys, al, col, k) {
      if (!polys.length || al <= .003) return;
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
      lines(c, polys); c.strokeStyle = css(col, .08 * al * k); c.lineWidth = 2.4; c.stroke(); c.strokeStyle = css(pale(col, .2), .55 * al * k); c.lineWidth = .7; c.stroke();
      c.restore();
    }
    // the diagram: the outline and lines in ember threads drawn up to share f, sparks running along the outline, the crack red
    function emberProp(c, q, f, al, t, col = EMBER) {
      if (f <= .003 || al <= .003) return;
      const ol = olOf(q);
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
    function drawScan(c, X, st) {
      st.props.forEach((q, i) => {
        const f = i ? st.fB : st.fA, k = q.al * st.v; if (k <= .003) return;
        picture(c, q, k * (1 - .65 * f)); inkCracks(c, q, k * (1 - .65 * f));
        emberProp(c, q, f, k, X.t);
        if (q.crack && f > 0) { const o = toP(q, q.pr.blow); halo(c, o[0], o[1], 26, RED, .3 * f * k * (.75 + .25 * Math.sin(X.t * 5))); }   // the crack glows red
      });
    }
    function coneOf(D, A, len) { return [D, [D[0] + Math.cos(A - HALF) * len, D[1] + Math.sin(A - HALF) * len], [D[0] + Math.cos(A + HALF) * len, D[1] + Math.sin(A + HALF) * len]]; }
    function beam(c, X, st) {
      const Lt = st.light * st.v; if (Lt <= .003) return;
      const D = X.D, len = 900;
      c.save(); c.globalCompositeOperation = 'lighter'; c.translate(D[0], D[1]); c.rotate(st.A);
      const gr = c.createLinearGradient(0, 0, len, 0); gr.addColorStop(0, css(LAMP, .22 * Lt)); gr.addColorStop(1, css(LAMP, 0));
      c.fillStyle = gr; c.beginPath(); c.moveTo(2, -3); c.lineTo(len, -len * Math.tan(HALF)); c.lineTo(len, len * Math.tan(HALF)); c.lineTo(2, 3); c.closePath(); c.fill();
      c.restore();
    }
    function crateInBeam(c, X, st, q) {
      const a = st.light * st.inCone * st.v;
      if (a <= .003) return 0;
      c.save(); const k = coneOf(X.D, st.A, 900); lines(c, [k]); c.closePath(); c.clip(); picture(c, q, st.light * st.v * (1 - .65 * st.fC)); c.restore();
      halo(c, q.p[0], q.p[1], 70, LAMP, .18 * a);
      return a;
    }
    function drawLamp(c, X, st) {
      const [Lm, C] = st.props, u = st.u, v = st.v, a = Lm.al * v, B = st.B, out = { pic: 0, diag: 0 };
      if (a > .003) {
        const bulb = toP(Lm, Lm.pr.bulb), cable = [LAMP_PIV, Lm.top], f = st.fL;
        halo(c, bulb[0], bulb[1], 46, LAMP, .28 * a);
        lines(c, [cable]); c.strokeStyle = css([22, 23, 27], a); c.lineWidth = 1.6; c.stroke(); c.strokeStyle = css([120, 126, 134], .5 * a); c.lineWidth = .6; c.stroke();
        picture(c, Lm, a * (1 - .5 * f));
        if (f > 0) { emberLines(c, cut([cable], f), a, EMBER, .6); emberProp(c, Lm, f, a, X.t); }
        // the drone copies the dish: a thread from its eye to the bulb, a spark running home along it
        const study = bump(u, B.dish[0], B.dish[0] + .15, B.dish[1]);
        if (study > .003) { const e = X.eye, s = ph(B.dish, u), p = lerp2(bulb, e, sm(s)); c.save(); c.globalCompositeOperation = 'lighter'; line2(c, e, bulb, .4, EMBER, .25 * study * a); halo(c, p[0], p[1], 5, EMBER, .7 * study * a); c.restore(); }
      }
      out.pic = crateInBeam(c, X, st, C);
      out.diag = st.fC; emberProp(c, C, st.fC, v * st.light, X.t);
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
    function drawFrost(c, X, st) {
      const a = st.fa * st.v, x = st.fx; if (a <= .003) return;
      const grow = k => sm(c01((x - k.x) / 40)), fl = frontLine(x);
      c.save(); c.globalAlpha = a;
      c.beginPath(); c.moveTo(-4, -4); for (const p of fl) c.lineTo(p[0], p[1]); c.lineTo(-4, LH + 4); c.closePath();
      const gr = c.createLinearGradient(0, 0, Math.max(x + 30, 1), 0); gr.addColorStop(0, css(ICE, .3)); gr.addColorStop(1, css(ICE, .04)); c.fillStyle = gr; c.fill();
      lines(c, [fl]); c.strokeStyle = css(ICE, .3); c.lineWidth = 1; c.stroke();
      for (const k of FROSTS) { const g0 = grow(k); if (g0 > .01) crystal(c, k, k.s * g0, css(ICE, .7), .8); }
      c.restore();
      emberLines(c, cut([fl], st.fCold), a, EMBER, 1);
    }
    function drawMeteor(c, X, st) {
      const m = st.met; if (!m) return;
      const a = m.al * st.v, q = put(PR.rock, m.head, m.spin), tl = 300 * m.s, w = 16 * m.s;
      q.s = m.s;
      const tail = [m.head[0] - m.dir[0] * tl, m.head[1] - m.dir[1] * tl], nrm = [-m.dir[1], m.dir[0]];
      c.save(); c.globalCompositeOperation = 'lighter';
      const gr = c.createLinearGradient(tail[0], tail[1], m.head[0], m.head[1]); gr.addColorStop(0, 'rgba(255,190,120,0)'); gr.addColorStop(1, css([255, 214, 168], .8 * a));
      c.fillStyle = gr; c.beginPath(); c.moveTo(tail[0], tail[1]); c.lineTo(m.head[0] + nrm[0] * w, m.head[1] + nrm[1] * w); c.lineTo(m.head[0] - nrm[0] * w, m.head[1] - nrm[1] * w); c.closePath(); c.fill();
      c.restore(); halo(c, m.head[0], m.head[1], 60 * m.s, EMBER, .5 * a);
      picture(c, q, a);
      const f = st.fM; if (f <= 0) return;
      emberProp(c, q, f, a, X.t, mix3(EMBER, RED, .5));
      const p = [m.head, [m.head[0] + m.dir[0] * 700, m.head[1] + m.dir[1] * 700]];   // its way ahead, dashed
      c.save(); c.globalCompositeOperation = 'lighter'; lines(c, [p]); c.setLineDash([4, 7]); c.strokeStyle = css(mix3(EMBER, RED, .5), .45 * f * a); c.lineWidth = .8; c.stroke(); c.setLineDash([]); c.restore();
    }

    // ---------- the three ways to lead the eye ----------
    // «Нить взгляда»: a thin thread from the eye, its pin where to look
    function drawThread(g, X, st) {
      const F = st.F, tw = F[8] * st.v; if (tw <= .003) return;
      const e = X.eye, tip = lerp2(e, [F[9], F[10]], F[8]), d = dist(e, tip); if (d < 2) return;
      let n = [-(tip[1] - e[1]) / d, (tip[0] - e[0]) / d]; if (n[1] < 0) n = [-n[0], -n[1]];
      const sag = Math.min(36, d * .12), mid = [(e[0] + tip[0]) / 2 + n[0] * sag, (e[1] + tip[1]) / 2 + n[1] * sag], pts = [];
      for (let i = 0; i <= 24; i++) { const s = i / 24, a = (1 - s) * (1 - s), b = 2 * (1 - s) * s, cc = s * s; pts.push([a * e[0] + b * mid[0] + cc * tip[0], a * e[1] + b * mid[1] + cc * tip[1]]); }
      lines(g, [pts]); g.lineWidth = 1.4; g.strokeStyle = css(X.hot, .05 * tw); g.stroke(); g.lineWidth = .4; g.strokeStyle = css(X.hot, .32 * tw); g.stroke();
      const P = mkPath([pts]);
      for (let i = 0; i < 4; i++) { const p = onPath(P, i / 4 + X.t * .35); halo(g, p[0], p[1], 2.2, X.hot, .5 * tw); }
      g.beginPath(); g.arc(tip[0], tip[1], 3.2, 0, TAU); g.lineWidth = .5; g.strokeStyle = css(X.hot, .55 * tw); g.stroke();
      halo(g, tip[0], tip[1], 7, X.hot, .35 * tw);
    }
    // «Прожектор»: all but the thing in focus and the acting drone dims to a third
    function spotMask(T, st, M) {
      const g = T.mbg, F = st.F, k = .7 * st.v * Math.max(F[6], F[7]);
      g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.mb.width, T.mb.height);
      if (k <= .003) return;
      g.setTransform(M[0], M[1], M[2], M[3], M[4], M[5]); g.fillStyle = css([1, 2, 4], k); g.fillRect(-20, -20, LW + 40, LH + 40);
      g.globalCompositeOperation = 'destination-out';
      for (const [x, y, r, w] of [[F[3], F[4], F[5], F[6]], [st.D[0], st.D[1], 64, F[7]]]) {
        if (w <= .003) continue;
        const gr = g.createRadialGradient(x, y, 0, x, y, r); gr.addColorStop(0, css([0, 0, 0], w)); gr.addColorStop(.5, css([0, 0, 0], w)); gr.addColorStop(1, 'rgba(0,0,0,0)');
        g.fillStyle = gr; g.fillRect(x - r, y - r, 2 * r, 2 * r);
      }
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'source-over'; c.globalAlpha = 1; c.drawImage(T.mb, 0, 0); c.restore();
    }
    // «Кадр следует»: the view's matrix, zoomed about the focus
    function viewOf(T, st) {
      const S = T.S; if (T.cand.id !== 'frame') return [S, 0, 0, S, 0, 0];
      const [cx, cy, z] = st.cam; return [S * z, 0, 0, S * z, S * (LW / 2 - cx * z), S * (LH / 2 - cy * z)];
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
    function lightStart(T, M) {
      const g = T.hbg; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.hb.width, T.hb.height);
      g.setTransform(M[0], M[1], M[2], M[3], M[4], M[5]); g.globalCompositeOperation = 'lighter'; g.lineCap = 'round'; g.lineJoin = 'round';
      return g;
    }
    function lightEnd(T) { const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0); c.restore(); }

    // what the check reads: each thing's presence, place and how far the drone has taken it
    function thingsOf(st, o) {
      if (st.form === 'scan') { const [A, Bq] = st.props; return [{ id: 'A', al: A.al, p: A.base, act: st.fA + dotsAct(st.dots, 0) }, { id: 'B', al: Bq.al, p: Bq.base, act: st.fB + dotsAct(st.dots, 1) }]; }
      if (st.form === 'lamp') return [{ id: 'lamp', al: st.props[0].al, p: LAMP_P, act: st.fL }, { id: 'crate', al: o.pic, p: CRATE_P, act: st.fC, found: true }];
      const m = st.met;
      return [{ id: 'cold', al: st.fa, p: [st.fx, COLD_P[1]], act: st.fCold }, { id: 'meteor', al: m ? m.al : 0, p: m ? m.head : MP, act: st.fM }];
    }
    function draw(T) {
      const c = T.c, st = T.st, way = T.cand.id, u = st.u, M = viewOf(T, st), F = st.F;
      const X = { c, g: null, D: st.D, A: st.A, t: st.t, u, hot: st.hot };
      X.eye = [X.D[0] + Math.cos(X.A) * 2.2 * DS * DSC, X.D[1] + Math.sin(X.A) * 2.2 * DS * DSC];
      // the world, then black over it, then the void's things
      if (st.v < .999) drawBack(T);
      c.setTransform(M[0], M[1], M[2], M[3], M[4], M[5]); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      voidBack(c, st.v);
      const sh = st.shape, o = { pic: 0, diag: 0 };
      if (st.form === 'scan') drawScan(c, X, st);
      else if (st.form === 'lamp') { beam(c, X, st); Object.assign(o, drawLamp(c, X, st)); }
      else { drawFrost(c, X, st); drawMeteor(c, X, st); }
      const h = heroOf(st, X);
      drawHero(c, h);
      // the drone, its shape and the gauge, glowing onto the buffer
      const g = X.g = lightStart(T, M);
      const b = drawBody(X, sh), rd = sh && sh.kind === 'scan' ? drawDots(X, sh) : null;
      if (way === 'thread') drawThread(g, X, st);
      lightEnd(T);
      if (way === 'spot') spotMask(T, st, M);
      c.setTransform(T.S, 0, 0, T.S, 0, 0);
      vignette(c, st.v);
      hudStandIn(c, st.hud);
      const tv = lerp2(st.Db, [F[9], F[10]], F[8]);
      T.dm = {
        form: st.form, u, n: st.n, len: st.len, v: st.v, hud: st.hud, things: thingsOf(st, o), shape: shapeForm(sh, u), Db: st.Db, A: st.A, light: st.light,
        focPx: [[st.cam[0], st.cam[1]], [F[3] * F[6], F[4] * F[6]], [F[5] * F[6], 0], tv], focK: [st.cam[2], F[6], F[7], F[8]],
        seated: b.seated, away: b.away, mirror: b.nm ? b.mirror / b.nm : 0, rd, red: st.alarm ? st.alarm.red : 0, glyph: st.alarm ? st.alarm.id : null, dart: st.dart,
        pic: o.pic, diag: o.diag, inCone: st.inCone,
        nums: [st.D[0], st.D[1], st.A, h.y, h.look, st.v, st.hud, ...F, ...st.cam, ...(st.props || []).flatMap(q => [q.p[0], q.p[1], q.a])],
      };
    }

    const cands = [
      { id: 'spot', name: 'Прожектор', idea: 'Светло только то, на что сейчас смотреть; всё остальное, и герой тоже, гаснет до трети.', time: '' },
      { id: 'frame', name: 'Кадр следует', idea: 'Кадр плавно подъезжает к вещи и берёт её крупно, потом так же плавно возвращается к герою и дрону.', time: '' },
      { id: 'thread', name: 'Нить взгляда', idea: 'Из глаза дрона тянется тонкая нить и первой приходит туда, куда смотреть; вещь приплывает к её концу.', time: '' },
    ];
    const SAY = {
      scan: B => [[0, 'мир гаснет'], [B.aIn[0], 'выплывает обломок'], [B.aRead[0], 'обломок висит: разгляди'], [B.form[0], 'дрон собирается в прицел'], [B.aTrace[0], 'нить бежит по обломку, остаётся схема'],
        [B.aDots[0], 'точки по одной: ряд полный'], [B.aResult[0], 'полный ряд держится'], [B.aOut[0], 'обломок уплывает'], [B.bIn[0], 'выплывает треснувший'], [B.bRead[0], 'треснувший висит: разгляди'],
        [B.bAim[0], 'прицел берёт его, трещина краснеет'], [B.bDots[0], 'ряд заполняется наполовину'], [B.bResult[0], 'половина держится'], [B.end[0], 'мир возвращается']],
      lamp: B => [[0, 'мир гаснет'], [B.lIn[0], 'в темноте появляется лампа'], [B.lRead[0], 'лампа висит: разгляди'], [B.go[0], 'дрон подлетает к ней'], [B.lTrace[0], 'нить бежит по лампе'],
        [B.dish[0], 'дрон копирует её чашу'], [B.lResult[0], 'чаша держится'], [B.lOut[0], 'лампа гаснет'], [B.lightOn[0], 'луч в темноте'], [B.sweep[0], 'луч медленно ищет'],
        [B.cTrace[0], 'луч нашёл ящик'], [B.cResult[0], 'ящик в свете держится'], [B.end[0], 'мир возвращается']],
      alarms: B => [[0, 'мир гаснет'], [B.cIn[0], 'с края ползёт холод'], [B.cRead[0], 'холод стоит: разгляди'], [B.cGlyph[0], 'дрон показывает снежинку'], [B.cTrace[0], 'нить по краю холода'],
        [B.cResult[0], 'снежинка держится'], [B.cOut[0], 'холод отступает'], [B.mIn[0], 'вдали метеор'], [B.mRead[0], 'метеор медленно ближе'], [B.dart[0], 'дрон бросается к нему, краснеет'],
        [B.mTrace[0], 'нить по метеору и его пути'], [B.mResult[0], 'знак метеора держится'], [B.pass[0], 'метеор проходит мимо'], [B.end[0], 'мир возвращается']],
    };

    return {
      cands, expose: { LEN, BT, READS, NDOT, FORMS, RU, fmt1 },
      init(T) { T.time = 0; T.t0 = 0; T.heroes = []; T.form = OPT.form; T.st = stateOf(T); },
      onScale(T) {
        const mk = () => { const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S); return cv; };
        T.hb = mk(); T.hbg = T.hb.getContext('2d'); T.mb = mk(); T.mbg = T.mb.getContext('2d');
      },
      step(T) {
        if (T.form !== OPT.form) { T.form = OPT.form; T.t0 = T.time; }   // another form starts its vision from the top
        T.st = stateOf(T);
      },
      back(T) { draw(T); },
      hud(T) {
        const st = T.st, ev = SAY[st.form](st.B); let say = ev[0][1];
        for (const q of ev) if (st.u >= q[0]) say = q[1];
        return `${RU[st.form]} · ${fmt1(st.u)} из ${fmt1(st.len)} с · ${say}`;
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    two: [['A.al = sm(ph(B.aIn, u) / .7) * (1 - oa);', 'A.al = sm(ph(B.aIn, u) / .7) * (1 - .9 * oa);']],
    rush: [["['aRead', 1.25]", "['aRead', .7]"]],
    busy: [["['aGap', .25]", "['aGap', 0]"]],
    quick: [["['aResult', 1.15]", "['aResult', .5]"]],
    snap: [["['formHold', .4]", "['formHold', 0]"]],
    long: [["['bResult', 1.1]", "['bResult', 1.9]"]],
    hudStays: [['sm((u - (len - .3)) / .3) : 0;', 'sm((u - (len - .3)) / .3) : .2;']],
    half: [['{ left: 150, max: 300 }', '{ left: 170, max: 300 }']],
    twice: [['if (pt.seeds && outOf(r, D)) { out.away++; continue; }', 'if (pt.seeds && outOf(r, D)) { out.away++; }']],
    seen: [['c.save(); const k = coneOf(X.D, st.A, 900);', 'c.save(); const k = coneOf(X.D, st.A + 1.2, 900);'], ['const a = st.light * st.inCone * st.v;', 'const a = st.light * st.v;']],
    early: [['st.fC = ez(B.cTrace, u);', 'st.fC = ez(B.lTrace, u);']],
    dark: [['st.light = ez(B.lightOn, u)', 'st.light = ez(B.dish, u)']],
    three: [["{ id: 'meteor', t0: B.dart[0], tin: .6, t1: B.pass[0], tout: 1 }];", "{ id: 'meteor', t0: B.dart[0], tin: .6, t1: B.pass[0], tout: 1 }, { id: 'cold', t0: B.pass[1], tin: .2, t1: B.pass[1] + .2, tout: .1 }];"]],
    still: [['st.dart = ez(B.dart, u)', 'st.dart = ez(B.cGlyph, u)']],
    nan: [['const HX = 780, DV', 'const HX = NaN, DV']],
  },

  // Every candidate: the 'all' loop (scan, lamp, alarms) once, then a pause, slow, and the alarms alone; then a pick.
  // Pacing, read from what each frame shows (things' presence, places and how far they are taken; the drone's shape, place, aim
  // and light; the focus): one thing at a time; a thing arriving holds still 1.2 s before anything moves; a thing appears only
  // after 0.2 s of stillness; a result holds 1 s before its thing goes or the world returns; the drone's new shape holds 0.35 s;
  // a form lasts 10–14 s, as its button and the lead say. And as before: the veil, the HUD away, the scan's full and half rows,
  // the lamp's light and crate, cold then meteor, red and the dart only with their glyph, no mark's spark drawn twice.
  check(K) {
    const X = K.X, LEN = X.LEN, SPEC = { in: .6, out: .6, min: 10, max: 14, read: 1.2, result: 1.0, shape: .35, gap: .2, tol: .02 };
    const fr = s => Math.ceil(s * 60), ALL = fr(LEN.scan + LEN.lamp + LEN.alarms) + 10, P0 = ALL + 20, FLIP = ALL + 150, N = FLIP + fr(LEN.alarms) + 60;
    K.at(P0, () => K.seg('playMode', 1)); K.at(P0 + 50, () => K.seg('playMode', 0));
    K.at(P0 + 60, () => K.seg('speedMode', 1)); K.at(P0 + 110, () => K.seg('speedMode', 0));
    K.at(FLIP, () => K.seg('formMode', 3));
    K.at(N - 40, () => K.click(2, '.pick'));
    let st = '';
    K.at(N - 20, () => { st = K.byId.status.textContent; K.byId.copy.listeners.click[0](); });
    const S = K.tiles.map(() => ({ cur: null, done: { scan: 0, lamp: 0, alarms: 0 }, lines: [] }));
    const fmt = v => v === Infinity ? '-' : v.toFixed(2);
    const close = (s, j) => {
      const L = s.cur, id = K.tiles[j].cand.id; if (!L) return;
      if (L.u0 > .05 || L.uMax < L.len - .05) return;   // cut by a switch or the run's end
      if (L.form === 'scan' && !(L.fullA > 0 && L.halfB > 0)) K.fail(`${id}: a scan without its full row (${L.fullA}) and half row (${L.halfB})`);
      if (L.form === 'lamp' && !(L.pic > .9 && L.diag > .9)) K.fail(`${id}: the lamp never showed the crate (picture ${L.pic.toFixed(2)}, diagram ${L.diag.toFixed(2)})`);
      if (L.form === 'alarms' && L.seq.join(',') !== 'cold,meteor') K.fail(`${id}: alarms ${L.seq.join(',')}, not cold then meteor`);
      for (const k of L.form === 'lamp' ? ['read', 'result', 'shape'] : ['read', 'result', 'shape', 'new']) if (!L.cnt[k]) K.fail(`${id}: ${L.form} with no ${k} hold measured`);
      s.done[L.form]++;
      if (s.lines.length < 3) s.lines.push(`${L.form} ${L.len.toFixed(2)} s: read ≥${fmt(L.min.read)}, result ≥${fmt(L.min.result)}, shape ≥${fmt(L.min.shape)}, before new ≥${fmt(L.min.new)}`);
    };
    // the still run that ended at u (motion resumed within 0.15 s before), or the one still going
    const runBefore = (L, u) => { if (L.from != null) return [L.from, u]; const r = L.runs[L.runs.length - 1]; return r && r[1] >= u - .15 ? r : null; };
    const hold = (L, id, kind, need, len, at) => {
      L.cnt[kind] = (L.cnt[kind] || 0) + 1; L.min[kind] = Math.min(L.min[kind], len);
      if (len < need - SPEC.tol) K.fail(`${id}: ${kind} held ${len.toFixed(2)} s, under ${need} s, ${at}`);
    };
    let paused = 0, lastT = -1;
    K.run(N, (t, i) => {
      const j = K.tiles.indexOf(t), s = S[j], d = t.dm, id = t.cand.id; if (!d) return;
      if (d.nums.some(v => !isFinite(v))) K.fail(`${id}: a number not finite, frame ${i}`);
      if (j === 0) { if (!K.OPT.playing) { if (t.time !== lastT && lastT >= 0) K.fail('time ran while paused'); paused++; } lastT = t.time; }
      if (!K.OPT.playing) return;
      if (!s.cur || d.form !== s.cur.form || d.n !== s.cur.n) {
        close(s, j);
        s.cur = { form: d.form, n: d.n, len: d.len, u0: d.u, uMax: d.u, fullA: 0, halfB: 0, pic: 0, diag: 0, seq: [], prev: null, runs: [], from: null, moved: false, pend: [], seen: {}, arrived: {}, acted: {}, gone: {}, ended: false, beam: false,
          cnt: {}, min: { read: Infinity, result: Infinity, shape: Infinity, new: Infinity } };
        if (d.len < SPEC.min || d.len > SPEC.max) K.fail(`${id}: ${d.form} lasts ${d.len.toFixed(2)} s, not ${SPEC.min}–${SPEC.max}`);
      }
      const L = s.cur, at = `${d.form} ${d.u.toFixed(2)} s`; L.uMax = Math.max(L.uMax, d.u);
      // the vision's frame: 0.6 in, shown, 0.6 out; the HUD away while the void is up
      if (d.u >= SPEC.in + .02 && d.u <= d.len - SPEC.out - .02 && d.v < .999) K.fail(`${id}: the void at ${d.v.toFixed(3)} while shown, ${at}`);
      if ((d.u < .02 || d.u > d.len - .02) && d.v > .01) K.fail(`${id}: the void at ${d.v.toFixed(3)} at the vision's edge, ${at}`);
      if (d.u > d.len + .02) K.fail(`${id}: the vision runs past ${d.len} s, ${at}`);
      if (d.hud > .01 && d.v > .99) K.fail(`${id}: the HUD at ${d.hud.toFixed(2)} in the void, ${at}`);
      // one thing at a time
      const inView = d.things.filter(q => q.al > .02);
      if (inView.length > 1) K.fail(`${id}: ${inView.map(q => q.id).join(' and ')} at once, ${at}`);
      // what moved since the last frame
      const P = L.prev, du = P ? d.u - P.u : 0;
      if (P && du > 0) {
        const sp = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]) / du;
        const leaving = [], settled = {};
        let mv = null;
        d.things.forEach((q, k) => {
          const o = P.things[k], fade = (q.al - o.al) / du, run = sp(q.p, o.p) > 12 && Math.max(q.al, o.al) > .02;
          settled[q.id] = Math.abs(fade) <= .08 && !run;
          if (Math.abs(fade) > .08) mv = 'presence of ' + q.id;
          if (run) mv = 'place of ' + q.id;
          if (Math.abs(q.act - o.act) > 1e-5) mv = 'taking ' + q.id;
          if (L.acted[q.id] && !L.gone[q.id] && (fade < -.08 || run)) { L.gone[q.id] = true; leaving.push(q.id); }
        });
        if (Math.abs(d.shape - P.shape) > 1e-5) mv = 'shape';
        if (sp(d.Db, P.Db) > 12) mv = 'drone';
        if (Math.abs(Math.atan2(Math.sin(d.A - P.A), Math.cos(d.A - P.A))) / du > .15) mv = 'aim';
        if (Math.abs(d.light - P.light) / du > .08) mv = 'light';
        d.focPx.forEach((p, k) => { if (sp(p, P.focPx[k]) > 12) mv = 'focus'; });
        d.focK.forEach((x, k) => { if (Math.abs(x - P.focK[k]) / du > .05) mv = 'focus'; });
        // the runs of stillness, and the holds they measure
        if (!mv && L.from == null) L.from = P.u;
        if (mv && L.from != null) {
          const r = [L.from, d.u]; L.runs.push(r); L.from = null;
          // a hold counts only the stillness that starts at its event; motion carrying straight on holds nothing
          L.pend = L.pend.filter(w => { if (r[1] <= w.u) return true; hold(L, id, w.kind, w.need, r[0] <= w.u + .15 ? r[1] - Math.max(r[0], w.u) : 0, `${w.what} at ${w.u.toFixed(2)} s`); return false; });
        }
        // the events: a thing appears, has arrived, goes; the drone's shape is whole; the world returns
        d.things.forEach(q => {
          if (q.act > 1e-4) L.acted[q.id] = true;
          if (q.found) return;
          if (q.al > 1e-4 && !L.seen[q.id]) {
            L.seen[q.id] = true;
            if (L.moved) { const r = runBefore(L, d.u); hold(L, id, 'new', SPEC.gap, r ? r[1] - r[0] : 0, `${q.id} appearing, ${at}`); }
          }
          if (q.al >= .99 && settled[q.id] && !L.arrived[q.id]) { L.arrived[q.id] = true; L.pend.push({ kind: 'read', need: SPEC.read, u: d.u, what: `${q.id} arrived` }); }
        });
        for (const q of leaving) { const r = runBefore(L, d.u); hold(L, id, 'result', SPEC.result, r ? r[1] - r[0] : 0, `before ${q} goes, ${at}`); }
        if (d.shape >= .999 && P.shape < .999) L.pend.push({ kind: 'shape', need: SPEC.shape, u: d.u, what: 'the shape whole' });
        if (!L.ended && d.u >= d.len - SPEC.out) {
          L.ended = true;
          if (inView.length) { const r = runBefore(L, d.u); hold(L, id, 'result', SPEC.result, r ? r[1] - r[0] : 0, `before the world returns over ${inView[0].id}, ${at}`); }
        }
        if (mv) L.moved = true;
      }
      L.prev = d;
      if (d.form === 'scan') {
        const r = d.rd;
        if (r && d.seated + r.drawn > X.NDOT) K.fail(`${id}: ${d.seated} marks seated and ${r.drawn} dots out, ${at}`);
        if (r && r.ph === 'A' && r.home === X.NDOT) { if (Math.abs(r.lit - X.NDOT) > 1e-6) K.fail(`${id}: the whole piece lit ${r.lit}, ${at}`); else L.fullA++; }
        if (r && r.ph === 'B' && r.home === X.NDOT) { const want = X.READS[1].left * X.NDOT / X.READS[1].max; if (Math.abs(r.lit - 13) > 1e-6 || Math.abs(want - 13) > 1e-6) K.fail(`${id}: the cracked piece lit ${r.lit}, ${at}`); else L.halfB++; }
      }
      if (d.form === 'lamp') {
        if (d.light > .5 && d.mirror < .9) K.fail(`${id}: the light on with the mirror at ${d.mirror.toFixed(2)}, ${at}`);
        if (d.pic > d.light * d.inCone * d.v + 1e-6) K.fail(`${id}: the crate's picture ${d.pic.toFixed(2)} outside the light (${d.light.toFixed(2)} × ${d.inCone.toFixed(2)}), ${at}`);
        if (d.light > .5 && d.inCone > .9) L.beam = true;
        if (d.diag > 1e-6 && !L.beam) K.fail(`${id}: the crate drawn at ${d.diag.toFixed(2)} before the beam found it, ${at}`);
        L.pic = Math.max(L.pic, d.pic); L.diag = Math.max(L.diag, d.diag);
      }
      if (d.form === 'alarms') {
        if (d.glyph && L.seq[L.seq.length - 1] !== d.glyph) L.seq.push(d.glyph);
        if (d.red > 0 && !d.glyph) K.fail(`${id}: red with no glyph up, ${at}`);
        if (d.dart > 1e-6 && d.glyph !== 'meteor') K.fail(`${id}: the dart without the meteor's glyph, ${at}`);
      }
      if (i === ALL && j === 0 && Object.values(s.done).some(n => n < 1)) K.fail(`by frame ${ALL} not every form played whole: ${JSON.stringify(s.done)}`);
    });
    S.forEach((s, j) => close(s, j));
    const doc = K.picked(), fb = K.byId.fallback.value, cand = K.tiles[2].cand;
    S.forEach((s, j) => console.log(`${K.tiles[j].cand.id}: whole ${JSON.stringify(s.done)}${j ? '' : ' | ' + s.lines.join(' || ')}`));
    console.log('lengths', X.FORMS.map(f => `${f} ${LEN[f].toFixed(2)}`).join(', '), '| paused frames', paused, '| pick doc:', JSON.stringify(doc), '| copy text:', fb);
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    need(K.tiles.length === 3, 'three candidates');
    need(S.every(s => s.done.scan >= 1 && s.done.lamp >= 1 && s.done.alarms >= 2), 'every candidate: a whole scan, lamp and two alarms');
    for (const f of X.FORMS) {
      const say = `${X.fmt1(LEN[f])} с`, opt = this.toggles[0].opts.find(o => o[1] === f);
      need(opt && opt[0].includes(say), `the ${f} button says ${say}`);
      need(this.lead.includes(`${X.RU[f]} ${say}`), `the lead says ${X.RU[f]} ${say}`);
    }
    need(paused > 30, 'the pause held');
    need(this.pick.doc === 'picks/formVisionFocus', 'the pick at picks/formVisionFocus');
    need(doc && doc.pick === cand.id, 'the pick is in the db doc');
    need(st.includes(`«${cand.name}»`), 'the status names the pick');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the copy text names the pick');
    return ok;
  },
};
