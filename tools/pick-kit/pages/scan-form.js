// The light drone's scan form, round three: six abstract schematics of what a scanner is (as the lamp is a dish and a source), in the spyglass's place,
// beside today's spyglass and lamp: an art-pick page on tools/pick-kit (README.md). The scan itself stays as today: a bunch of
// 26 sparks flies to the wreck and stays as the dots gauge, hops to the next wreck without coming back, and returns when the
// pointer leaves. Here those 26 are the form's own sparks, seated in it: it shows the gap while they are out.
// Each form is data as drone-threads wants it: polylines in the drone's frame (pick px about the core, x along the aim) closed
// and open, a share of the sparks and a flow per part; the seeds' part is 26 polylines, seed j sitting `at` along line j.
// node tools/pick-kit/build.js tools/pick-kit/pages/scan-form.js <out-dir> [--break <name>]
module.exports = {
  title: 'Форма скана',
  heading: 'Форма скана: схема, как у фонаря',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Третий круг. Фонарь — схема: зеркало и источник, то, что он есть. Здесь шесть таких же <b>схем сканера</b>: того, что отправляет мерные точки и читает их. Сам скан как сейчас: горстка искр летит к обломку и висит над ним <b>точками прочности</b>, перепрыгивает на следующий обломок, не возвращаясь, и уходит назад, когда указатель покидает обломки. Эти <b>26 точек — искры самой формы</b>, у каждой своё место: пока они над обломком, в форме видна пустота, когда возвращаются — она снова полная.</p>
<p class="lead">Каждая плитка — один круг: покой → искры собираются в форму → указатель на первом обломке: форма раскрывается, точки улетают → указатель переходит на второй обломок: точки перепрыгивают туда → указатель уходит: точки возвращаются на свои места, форма закрывается → искры обратно на орбиты. Пока идёт скан, свет фонаря погашен. Первые две плитки — как сейчас в игре, для сравнения. Наведи мышь на плитку — форма повернётся к указателю. В лупе справа вверху дрон ×3: в ней видно, как форма пустеет.</p>
<p class="miss">Не показано: звук; удар по обломку (точки гаснут и осыпаются, как сейчас); что видят друзья; отрисовка Godot (свет и цвета выйдут чуть иначе).</p>`,
  toggles: [
    { id: 'loupeMode', bar: 1, label: 'Лупа ×3:', key: 'loupe', opts: [['есть', true], ['нет', false]] },
  ],
  pick: { doc: 'picks/scanForm', subject: 'Форма скана (третий круг)', what: ['схему', 'схемой'] },

  page() {
    const TAU = Math.PI * 2;
    const N = 120;   // DroneInk.Sparks
    // DroneInk: a shape forms in Form s, a spark into it late by its share of Lag over Span, out of it by LagOut over SpanOut
    const FORM = .6, LAG = .45, SPAN = .55, LAG_OUT = .4, SPAN_OUT = .6;
    // the loop, s: the pointer sets off; the form gathers; on the first wreck (dots out); across to the second (the hop); the
    // pointer leaves (dots home); the form lets go; rest
    const TL = { go: .4, form: .7, on: 1.4, hopGo: 2.8, hop: 3.2, off: 4.6, back: 5.4, loop: 6.6 };
    // ScanDots and DroneInk's gauge: 26 dots, each a 26th of the wreck's maximum; pick px over the world; the coming and going
    const NDOT = 26, DOT = { step: 7, side: 3.2, above: 20, in: .4, out: .4, lag: .4, span: .6, lagOut: .35, spanOut: .65 };
    const HERO_X = 560, LK = 3, SPARK = .95;
    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
    const circ = (cx, cy, r, n = 22) => arcP(cx, cy, r, r, 0, TAU, n);
    const R = (r, a) => [Math.cos(a) * r, Math.sin(a) * r];
    const bq = (a, b, d, n) => Array.from({ length: n + 1 }, (_, j) => bez(a, b, d, j / n));
    const unit = v => { const d = Math.hypot(v[0], v[1]) || 1; return [v[0] / d, v[1] / d]; };
    const lerpPts = (A, B, k) => A.map((p, j) => [lerp(p[0], B[j][0], k), lerp(p[1], B[j][1], k)]);
    // a polyline walked from p by headings h(s), n steps of len / n
    const walk = (p, len, n, h) => { const out = [p.slice()]; for (let m = 1; m <= n; m++) { const a = h((m - .5) / n), q = out[m - 1]; out.push([q[0] + Math.cos(a) * len / n, q[1] + Math.sin(a) * len / n]); } return out; };
    const tiny = (p, d, l) => [[p[0] - d[0] * l, p[1] - d[1] * l], [p[0] + d[0] * l, p[1] + d[1] * l]];

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
    // the form's frame: about the core, turned to the aim, mirrored (fy) when the aim is behind so its top stays up
    const fyOf = A => clamp(Math.cos(A) * 4, -1, 1) || 1;
    const frameOf = (O, k, A) => ({ O, k, A, ca: Math.cos(A), sa: Math.sin(A), fy: fyOf(A) });
    const toW = (F, q) => { const y = q[1] * F.fy, dy = (q[3] || 0) * F.fy; return [F.O[0] + (q[0] * F.ca - y * F.sa) * F.k, F.O[1] + (q[0] * F.sa + y * F.ca) * F.k, (q[2] || 0) * F.ca - dy * F.sa, (q[2] || 0) * F.sa + dy * F.ca]; };
    function strokeLines(g, F, polys, w, a) {
      if (a <= .003) return;
      g.beginPath();
      for (const p of polys) p.forEach((q, j) => { const r = toW(F, q); if (j) g.lineTo(r[0], r[1]); else g.moveTo(r[0], r[1]); });
      g.lineWidth = w; g.strokeStyle = css(EMBER, a); g.stroke();
    }
    function line2(g, a, b, w, al) { if (al <= .003) return; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.lineWidth = w; g.strokeStyle = css(EMBER, al); g.stroke(); }

    // ---------- today's shapes, as tools/anime-art/subjects/drone.js writes them into DroneInk.cs ----------
    const para = (back, half, cv, n) => { const p = []; for (let i = 0; i <= n; i++) { const v = -half + 2 * half * i / n; p.push([-back + v * v / cv, v]); } return p; };
    const rect = (u0, u1, h) => [[u0, -h], [u1, -h], [u1, h], [u0, h], [u0, -h]];
    const LAMP_LINES = [para(9, 12, 14, 14), para(7.2, 10, 15, 12)];
    const SCAN_LINES = [rect(3, 11, 3.6), rect(10, 19, 2.8), arcP(21.5, 0, 1.3, 4.4, 0, TAU, 14), arcP(-2, 0, 5.5, 5.5, Math.PI / 2, 3 * Math.PI / 2, 12)];

    // ---------- the candidates: key(L) gives each part's polylines, closed (0) and open to send (1) ----------
    // parts: { id, ru, share of the sparks, flow px/s along } or the seeds { seeds: true, at }: 26 lines, seed j at `at` of line j,
    // share 26/120 so they are the gauge's dots
    const SEED = NDOT / N;
    const cands = [
      {
        id: 'spyglass', name: 'Сейчас: подзорная труба', ref: true,
        idea: 'Как в игре сейчас: два колена трубы, кольцо на устье, капюшон за ядром. Точки — отдельные искры из устья: летят к обломку, перепрыгивают на следующий, уходят в глаз. Для сравнения, выбрать нельзя.',
        time: 'Собирается за 0,6 с · 80% искр в трубе, 20% бегут по орбитам · 26 точек сверх них, из устья трубы.',
        parts: [{ id: 'glass', ru: 'труба', share: .8, flow: 30 }],
        key: () => ({ lines: [SCAN_LINES], mouth: [21.5, 0] }),
      },
      {
        id: 'lamp', name: 'Сейчас: фонарь', ref: true, lamp: true,
        idea: 'Фонарь как в игре: зеркало из искр за ядром и сам огонь, свет туда, куда указатель. Это планка, до которой форма скана должна дотянуть. Выбрать нельзя.',
        time: 'Собирается за 0,6 с · 80% искр в зеркале · пока зеркало собрано, горит свет.',
        parts: [{ id: 'mirror', ru: 'зеркало', share: .8, flow: 30 }],
        key: () => ({ lines: [LAMP_LINES], mouth: [0, 0] }),
      },
      {
        id: 'lens', name: 'Линза', open: 'линза набухает, лучи сходятся',
        idea: 'Схема линзы: две встречные дуги ( ) перед ядром, ядро в фокусе за ней, два луча от ядра к краям линзы. 26 точек сидят пунктиром на оси — от ядра сквозь линзу к цели. Точки улетают — ось пустая, остаются линза и лучи.',
        parts: [{ id: 'dots', ru: 'точки на оси', seeds: true, at: .5, share: SEED }, { id: 'lens', ru: 'линза', share: .42, flow: 10 }, { id: 'rays', ru: 'лучи', share: 1 - SEED - .42, flow: 14 }],
        key(L) {
          const sag = lerp(2.2, 3.1, L), h = 7.2, x = 10.5;
          const arcs = [-1, 1].map(s => Array.from({ length: 13 }, (_, m) => { const v = m / 12 * 2 - 1; return [x + s * sag * (1 - v * v), v * h]; }));
          const rays = [-1, 1].map(s => [[3.6, s * lerp(1.4, .9, L)], [x, s * h * .96]]);
          const dots = Array.from({ length: 26 }, (_, j) => tiny([4.6 + j * .62, 0], [1, 0], .2));
          return { lines: [dots, arcs, rays], mouth: [x, 0] };
        },
      },
      {
        id: 'reticle', name: 'Прицел', open: 'риски сжимаются к центру',
        idea: 'Перекрестье: кольцо вокруг ядра и четыре риски, одна смотрит на цель. 26 точек сидят шкалой по внешнему кругу, как деления циферблата. Точки улетают — от шкалы остаются только кольцо и риски.',
        parts: [{ id: 'dots', ru: 'деления', seeds: true, at: .5, share: SEED }, { id: 'ring', ru: 'кольцо', share: .4, flow: 12 }, { id: 'ticks', ru: 'риски', share: 1 - SEED - .4, flow: 8 }],
        key(L) {
          const r = lerp(7.6, 7.1, L), rin = lerp(5.4, 4.3, L);
          const ticks = [0, 1, 2, 3].map(q => { const a = q * Math.PI / 2; return [R(rin, a), R(lerp(12.5, 13.5, L), a)]; });
          const dots = Array.from({ length: 26 }, (_, j) => { const a = (j + .5) / 26 * TAU; return tiny(R(10.2, a), R(1, a + Math.PI / 2), .22); });
          return { lines: [dots, [circ(0, 0, r, 26)], ticks], mouth: [r, 0] };
        },
      },
      {
        id: 'chevron', name: 'Стрела', open: 'наконечник заостряется',
        idea: 'Стрелка: наконечник > перед ядром, за ядром два пера оперения. 26 точек сидят внутри наконечника меньшей стрелкой из пунктира, в его горле. Точки улетают — наконечник пустой внутри.',
        parts: [{ id: 'dots', ru: 'точки в горле', seeds: true, at: .5, share: SEED }, { id: 'head', ru: 'наконечник', share: .5, flow: 14 }, { id: 'fletch', ru: 'оперение', share: 1 - SEED - .5, flow: 8 }],
        key(L) {
          const tip = [lerp(16, 17.5, L), 0], w = lerp(9, 7.2, L), x0 = lerp(4.5, 3.6, L);
          const head = [[x0, -w], tip, [x0, w], [x0 - 2.6, w], [tip[0] - 2.6, 0], [x0 - 2.6, -w], [x0, -w]];
          const dots = [];
          for (const s of [-1, 1]) for (let m = 0; m < 13; m++) { const e = (m + .5) / 13, a = [x0 + 1.5, s * w * .55], b = [tip[0] - 5.5, 0]; dots.push(tiny([lerp(a[0], b[0], e), lerp(a[1], b[1], e)], unit([b[0] - a[0], b[1] - a[1]]), .22)); }
          const fletch = [-1, 1].map(s => [[-4.4, 0], [-8.2, s * 4.2], [-10.6, s * 4.2]]);
          return { lines: [dots, [head], fletch], mouth: tip };
        },
      },
      {
        id: 'caliper', name: 'Штангенциркуль', open: 'подвижная губка отъезжает к цели',
        idea: 'Измерительная скоба: штанга от ядра к цели, неподвижная губка у ядра и подвижная впереди. На штанге 26 делений шкалы. На обломке подвижная губка отъезжает, раскрывая скобу, деления улетают точками; шкала пустая, пока они над обломком.',
        parts: [{ id: 'dots', ru: 'деления', seeds: true, at: .5, share: SEED }, { id: 'beam', ru: 'штанга', share: .34, flow: 12 }, { id: 'jaws', ru: 'губки', share: 1 - SEED - .34, flow: 8 }],
        key(L) {
          const x1 = lerp(13, 21, L), beam = [[4, -1.2], [23, -1.2], [23, 1.2], [4, 1.2], [4, -1.2]];
          const jaw = x => [[x, -1.2], [x, -7.4], [x + 1.6, -7.4], [x + 1.6, -5.6], [x + .8, -1.2]];
          const dots = Array.from({ length: 26 }, (_, j) => tiny([5 + j * .7, (j % 5 ? 0 : .3)], [0, 1], j % 5 ? .45 : .75));
          return { lines: [dots, [beam], [jaw(4), jaw(x1)]], mouth: [x1, -7.4] };
        },
      },
      {
        id: 'horn', name: 'Рупор', open: 'раструб раскрывается',
        idea: 'Рупор: от ядра к цели расходится раструб, на широком конце дуга устья. Внутри три дужки из точек, каждая шире прежней — 26 точек, готовых к отправке. Точки улетают — раструб пустой.',
        parts: [{ id: 'dots', ru: 'точки в раструбе', seeds: true, at: .5, share: SEED }, { id: 'walls', ru: 'стенки', share: .44, flow: 14 }, { id: 'mouth', ru: 'устье', share: 1 - SEED - .44, flow: 8 }],
        key(L) {
          const x1 = 18, w = lerp(7.4, 9.2, L), wall = s => bq([3.6, s * 1.8], [10, s * 2.8], [x1, s * w], 10);
          const mouth = arcP(x1 - 2, 0, 2, w, -Math.PI / 2, Math.PI / 2, 12);
          const dots = [];
          [[7.6, 8], [11.4, 9], [15, 9]].forEach(([x, n]) => { const h = lerp(1.8, w, (x - 3.6) / (x1 - 3.6)) * .7; for (let m = 0; m < n; m++) { const v = n > 1 ? m / (n - 1) * 2 - 1 : 0; dots.push(tiny([x + 1.2 * (1 - v * v), v * h], [0, 1], .22)); } });
          return { lines: [dots, [wall(-1), wall(1)], [mouth]], mouth: [x1, 0] };
        },
      },
      {
        id: 'ruler', name: 'Линейка', open: 'торцы раздвигаются',
        idea: 'Сама шкала: дрон держит у ядра короткую линейку, в рамке ряд из 26 точек — та же строка, что встанет над обломком. Точки вылетают всем рядом и висят над обломком; рамка пустая, пока они там.',
        parts: [{ id: 'dots', ru: 'точки', seeds: true, at: .5, share: SEED }, { id: 'frame', ru: 'рамка', share: .5, flow: 12 }, { id: 'ends', ru: 'торцы', share: 1 - SEED - .5, flow: 6 }],
        key(L) {
          const x0 = 4.4, x1 = 22.4, e = lerp(3, 4, L);
          const frame = [[x0, -1.5], [x1, -1.5], [x1, 1.5], [x0, 1.5], [x0, -1.5]];
          const ends = [x0 - .6, x1 + .6].map(x => [[x, -e], [x, e]]);
          const dots = Array.from({ length: 26 }, (_, j) => tiny([x0 + .7 + j * (x1 - x0 - 1.4) / 25, 0], [1, 0], .18));
          return { lines: [dots, [frame], ends], mouth: [x1, 0] };
        },
      },
    ];
    // the two keyframes, once; they must have the same polylines point for point, so the game lerps one into the other
    for (const cd of cands) {
      cd.K0 = cd.key(0); cd.K1 = cd.key(1);
      const sig = K => K.lines.map(pl => pl.map(p => p.length).join(',')).join('|');
      if (sig(cd.K0) !== sig(cd.K1) || cd.K0.lines.length !== cd.parts.length) throw new Error(cd.id + ': the keyframes differ in shape');
      cd.PL0 = cd.K0.lines.map(pl => mkPath(pl).L);
      cd.seedPart = cd.parts.findIndex(p => p.seeds);
      if (!cd.ref) cd.time = `Собирается за 0,6 с · на обломке ${cd.open} за 0,3 с · 26 искр (${cd.parts[cd.seedPart].ru}) улетают и возвращаются на свои места · доли искр: ${cd.parts.map(p => `${p.ru} ${Math.round(p.share * 100)}%`).join(', ')}.`;
    }
    // the form now: each part's polylines and the mouth, lerped from closed to open; nothing else moves it
    function liveOf(cd, L) {
      const B = cd.K1, lp = (a, b) => [lerp(a[0], b[0], L), lerp(a[1], b[1], L)];
      return { parts: cd.K0.lines.map((pl, pi) => pl.map((poly, li) => poly.map((p, j) => lp(p, B.lines[pi][li][j])))), mouth: lp(cd.K0.mouth, B.mouth) };
    }
    // which part each spark takes and its rank there: today's shapes by the game's rule (hash under the share), a candidate's
    // parts by their shares, rounded by largest remainder so the counts sum to the sparks the shares give; bySeed: seed j's spark
    function alloc(cd) {
      if (cd._al) return cd._al;
      const part = new Array(N).fill(-1), rank = new Array(N).fill(-1);
      let count;
      if (cd.ref) {
        let m = 0;
        for (let i = 0; i < N; i++) if (hsh(i * 3 + 1) < cd.parts[0].share) { part[i] = 0; rank[i] = m++; }
        count = [m];
      } else {
        const raw = cd.parts.map(p => p.share * N), total = Math.round(raw.reduce((a, b) => a + b, 0));
        count = raw.map(v => Math.floor(v + 1e-9));
        const byRest = raw.map((v, pi) => [v - Math.floor(v + 1e-9), pi]).sort((a, b) => b[0] - a[0]);
        for (let m = 0, left = total - count.reduce((a, b) => a + b, 0); m < left; m++) count[byRest[m % byRest.length][1]]++;
        const order = Array.from({ length: N }, (_, i) => i).sort((a, b) => hsh(a * 3 + 1) - hsh(b * 3 + 1));
        let at = 0;
        count.forEach((c, pi) => { for (let j = 0; j < c && at < N; j++) { const i = order[at++]; part[i] = pi; rank[i] = j; } });
      }
      const bySeed = [];
      for (let i = 0; i < N; i++) if (part[i] === cd.seedPart && cd.seedPart >= 0) bySeed[rank[i]] = i;
      return (cd._al = { part, rank, count, bySeed });
    }

    // ---------- the wrecks: a torn hull plate, and left of it a smaller broken frame (drone-embers.js's plate) ----------
    function mkWreck(pts, seed, o) {
      const s = mk(pts, seed, .8), OL = [];
      for (const q of s.segs) for (let i = 0; i < 5; i++) OL.push(bez([q[0], q[1]], [q[2], q[3]], [q[4], q[5]], i / 5));
      const xs = OL.map(p => p[0]), ys = OL.map(p => p[1]), box = [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
      const r = rng(seed + 1), rust = o.rust.map(([x, y, sz]) => blobAt(r, x, y, sz));
      return { s, box, C: [(box[0] + box[2]) / 2, (box[1] + box[3]) / 2], rust, rivets: o.rivets, seams: o.seams, cracks: o.cracks, rib: o.rib, hole: o.hole ? shardShape(r, 9) : null, HOLE: o.hole };
    }
    const rivetRows = (ys, x0, x1, step) => { const out = []; for (const y of ys) for (let x = x0; x < x1; x += step) out.push([x, y]); return out; };
    const WA = mkWreck([[392, 344], [404, 362], [436, 368], [450, 392], [440, 414], [452, 438], [447, 470], [190, 470], [195, 421], [213, 404], [225, 375], [262, 366], [300, 353], [348, 350], [372, 338]], 4242, {
      rust: [[258, 398, 4], [326, 432, 5], [398, 398, 3.5], [244, 452, 3], [418, 440, 4], [362, 398, 2.5]],
      rivets: rivetRows([394, 428], 204, 446, 18).concat([254, 322, 394].flatMap(x => [372, 390, 408, 426, 444, 462].map(y => [x, y]))),
      seams: [[[180, 398], [460, 398]], [[180, 432], [460, 432]], [[258, 330], [258, 470]], [[326, 330], [326, 470]], [[398, 330], [398, 470]]],
      cracks: [[[294, 406], [288, 399], [283, 397]], [[322, 420], [330, 424], [332, 431]], [[300, 426], [298, 433]], [[440, 380], [432, 386]]],
      rib: [[236, 384], [222, 356], [230, 334], [246, 326]], hole: [306, 416],
    });
    const WB = mkWreck([[36, 470], [40, 440], [58, 424], [70, 408], [96, 402], [122, 410], [150, 404], [166, 420], [160, 446], [170, 470]], 7171, {
      rust: [[70, 432, 3], [140, 442, 3.5], [104, 452, 2.5]],
      rivets: rivetRows([438], 50, 162, 16), seams: [[[30, 430], [176, 430]], [[118, 396], [118, 472]]],
      cracks: [[[96, 412], [102, 420], [100, 426]], [[150, 452], [156, 458]]], rib: [[150, 406], [158, 386], [170, 378]], hole: null,
    });
    // what the scanner reads on each: hit points left of the most, and where the row of dots stands
    const READS = [{ W: WA, left: 340, max: 520 }, { W: WB, left: 120, max: 300 }];
    for (const r of READS) r.O = [r.W.C[0], r.W.box[1] - DOT.above];
    const litOf = (r, j) => c01(r.left * NDOT / r.max - j);
    const homeOf = (r, j) => [r.O[0] + (j - (NDOT - 1) / 2) * DOT.step, r.O[1]];
    function lines(c, polys) { c.beginPath(); for (const p of polys) { c.moveTo(p[0][0], p[0][1]); for (let i = 1; i < p.length; i++) c.lineTo(p[i][0], p[i][1]); } }
    function drawWreck(c, W) {
      const b = W.box;
      c.fillStyle = 'rgba(0,0,0,.45)'; c.beginPath(); c.ellipse(W.C[0], FLOOR_Y + 1.5, (b[2] - b[0]) / 2 + 10, 4, 0, 0, TAU); c.fill();
      c.lineCap = 'round'; c.lineJoin = 'round';
      lines(c, [W.rib]); c.strokeStyle = INK; c.lineWidth = 7; c.stroke(); c.strokeStyle = '#3b3f46'; c.lineWidth = 4.4; c.stroke();
      c.save(); c.translate(-.8, -1.2); lines(c, [W.rib]); c.strokeStyle = 'rgba(122,129,138,.55)'; c.lineWidth = 1.2; c.stroke(); c.restore();
      c.save(); trace(c, W.s); c.clip();
      c.fillStyle = '#24272c'; c.fillRect(b[0] - 4, b[1] - 4, b[2] - b[0] + 8, b[3] - b[1] + 8);
      for (const [dy, col] of [[-5, '#353940'], [-15, '#40444c']]) { c.save(); c.translate(0, dy); c.fillStyle = col; trace(c, W.s); c.fill(); c.restore(); }
      lines(c, W.seams); c.lineWidth = 1.6; c.strokeStyle = '#25282d'; c.stroke();
      c.save(); c.translate(0, 1.6); lines(c, W.seams); c.lineWidth = .8; c.strokeStyle = 'rgba(112,120,130,.4)'; c.stroke(); c.restore();
      for (const [x, y] of W.rivets) { c.fillStyle = '#1d1f23'; c.fillRect(x - 1.2, y - .4, 2.6, 2.6); c.fillStyle = '#5d636c'; c.fillRect(x - 1.3, y - 1.3, 2.3, 2.3); }
      for (const bl of W.rust) {
        c.fillStyle = 'rgba(112,62,34,.85)'; trace(c, bl.s); c.fill();
        c.save(); c.translate(bl.x, bl.y); c.scale(.45, .45); c.translate(-bl.x, -bl.y); c.fillStyle = 'rgba(66,34,19,.9)'; trace(c, bl.s); c.fill(); c.restore();
      }
      if (W.hole) { c.save(); c.translate(W.HOLE[0], W.HOLE[1]); trace(c, W.hole); c.fillStyle = '#0a0b0e'; c.fill(); ink(c, W.hole, 0, 1.4); c.restore(); }
      lines(c, W.cracks); c.lineWidth = 1; c.strokeStyle = INK; c.stroke();
      c.save(); c.translate(0, 2); trace(c, W.s); c.strokeStyle = 'rgba(128,136,146,.45)'; c.lineWidth = 2; c.stroke(); c.restore();
      c.restore();
      ink(c, W.s, 0, 2.2);
    }

    // ---------- the drone «Нити» (drone-embers.js, threads): a coal core, an ember eye, sparks on three orbits ----------
    const P170 = DR.P;
    const eyeOf = X => [X.D[0] + Math.cos(X.A) * 2.2 * DS * DSC * X.k, X.D[1] + Math.sin(X.A) * 2.2 * DS * DSC * X.k];
    function coal(c, g, X) {
      const x = X.D[0], y = X.D[1], k = X.k, s = DS * DSC * k, A = X.A, fl = .75 + .25 * Math.sin(X.t * 7.3);
      halo(g, x, y, 8 * k, EMBER, .35);
      c.save(); c.translate(x, y); c.scale(s, s); paint(c, DR.core, A, DARK, .8, 2.6); ink(c, DR.core, A, 1.7); c.restore();
      g.strokeStyle = css(EMBER, .5 * fl); g.lineWidth = .45 * k; g.beginPath();
      for (const [a, b] of [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]]) { g.moveTo(x + Math.cos(A + a) * .6 * k, y + Math.sin(A + a) * .6 * k); g.lineTo(x + Math.cos(A + b * .5) * 2.6 * k, y + Math.sin(A + b * .5) * 2.6 * k); }
      g.stroke();
      const e = eyeOf(X);
      g.fillStyle = css(pale(EMBER, .5), .9 * fl); g.beginPath(); g.arc(e[0], e[1], 1.2 * k, 0, TAU); g.fill();
      halo(g, e[0], e[1], 4 * k, EMBER, .5 * fl);
    }
    function orbit(i, X) {
      const P = P170[i], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + X.t * (.8 + .25 * o) * dir * 2.2, rx = (6.3 + o * 1.4 + (P.r1 - .5)) * 1.05 * X.k, tl = -.3 + o * 1.05;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tl), v = rot(-Math.sin(th) * dir, Math.cos(th) * .3 * dir, tl), vl = Math.hypot(v[0], v[1]) || 1;
      return [X.D[0] + q[0], X.D[1] + q[1], v[0] / vl, v[1] / vl, Math.sin(th)];
    }
    // a spark: its dark square, and in the light three squares trailing back along its heading
    function mote(c, g, p, s, heat, k) {
      const h = Math.min(1, heat), dx = p[2] || 0, dy = p[3] || 0;
      c.fillStyle = '#26272b'; c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
      if (h <= .02) return;
      for (let m = 0; m < 3; m++) { const b = m * 1.3 * k; g.fillStyle = css(pale(EMBER, .3 * (1 - m / 3)), (.95 - m * .3) * h); g.fillRect(p[0] - dx * b - s / 2, p[1] - dy * b - s / 2, s, s); }
    }
    const idleHeat = (i, X) => hsh(i + 3) < .5 ? .55 + .3 * Math.sin(X.t * 11 + i * 7) : .2;
    const formHeat = (i, t) => .7 + .25 * Math.sin(t * 13 + i);
    const mkX = (c, g, D, A, k, t) => ({ c, g, D, A, k, t });

    // ---------- the clock ----------
    const REST = [520, 210], A0 = [262, 430], A1 = [410, 398], B0 = [118, 432], B1 = [86, 448];
    function cursorAt(u) {
      const seg = (a, b, u0, u1) => lerp2(a, b, sm((u - u0) / (u1 - u0)));
      if (u < TL.go) return { p: REST, a: 0 };
      if (u < TL.on) return { p: seg(REST, A0, TL.go, TL.on), a: c01((u - TL.go) / .12) };
      if (u < TL.hopGo) return { p: seg(A0, A1, TL.on, TL.hopGo), a: 1 };
      if (u < TL.hop) return { p: seg(A1, B0, TL.hopGo, TL.hop), a: 1 };
      if (u < TL.off) return { p: seg(B0, B1, TL.hop, TL.off), a: 1 };
      if (u < TL.back) return { p: seg(B1, REST, TL.off, TL.back), a: 1 };
      return { p: REST, a: 1 - c01((u - TL.back) / .15) };
    }
    // the gauge's phase (ScanDots): out to the first wreck, the hop to the second from where they are, home when the pointer leaves
    function dotsOf(u) {
      if (u < TL.on) return null;
      if (u < TL.hop) return { ph: 'a', e: u - TL.on, r: READS[0] };
      if (u < TL.off) return { ph: 'hop', e: u - TL.hop, r: READS[1] };
      if (u < TL.off + DOT.out + .05) return { ph: 'home', e: u - TL.off, r: READS[1] };
      return null;
    }
    const dotQ = (j, D) => D.ph === 'home' ? sm(c01((D.e / DOT.out - hsh(j + 2) * DOT.lagOut) / DOT.spanOut)) : sm(c01((D.e / DOT.in - hsh(j + 5) * DOT.lag) / DOT.span));
    // whether dot j is away from its seat, and how far: 0 on the seat, 1 at or between the wrecks
    const outOf = (j, D) => { if (!D) return false; const q = dotQ(j, D); return D.ph === 'a' ? q > 0 : D.ph === 'hop' ? true : q < 1; };
    const awayOf = (j, D) => { if (!D) return 0; const q = dotQ(j, D); return D.ph === 'a' ? q : D.ph === 'hop' ? 1 : 1 - q; };
    function stateOf(T) {
      const r = T.time - T.t0, u = ((r % TL.loop) + TL.loop) % TL.loop;
      const inF = u >= TL.form && u < TL.back;
      const e = inF ? (u - TL.form) / FORM : ((u - TL.back + TL.loop) % TL.loop) / FORM;
      // the sender opens as the pointer reaches the first wreck, and closes once its seeds are home
      const L = c01(sm((u - TL.on + .35) / .3) * (1 - sm((u - TL.off - .4) / .4)));
      const ph = u < TL.form ? 'rest' : u < TL.on ? 'gather' : u < TL.hopGo ? 'a' : u < TL.hop ? 'hop' : u < TL.off ? 'b' : u < TL.back ? 'leave' : 'back';
      return { u, inF, e, L, ph, dots: dotsOf(u), cur: cursorAt(u), form: inF ? sm(e) : 1 - sm(e) };
    }
    // the lamp's light: only the lamp shines, while its mirror stands; a scanning drone's light is out
    const lightOf = (cd, st) => cd.lamp ? st.form : 0;

    // ---------- the body: idle sparks, each part's sparks flying along straight threads onto their places; the seeds on their
    // seats unless away as dots; roles: 0 orbit, 1 between orbit and form, 2 on the form, 3 away as a dot ----------
    function drawBody(T, X, st, rec) {
      const c = X.c, g = X.g, cd = T.cand, al = alloc(cd), k = X.k, si = cd.seedPart;
      const F = frameOf(X.D, X.k, X.A);
      const lv = liveOf(cd, st.L), PP = lv.parts.map((pl, pi) => pi === si ? pl.map(l => mkPath([l])) : mkPath(pl));
      const D = si >= 0 ? st.dots : null, away = Array.from({ length: NDOT }, (_, j) => awayOf(j, D));
      const seats = si >= 0 ? lv.parts[si].map((l, j) => onPath(PP[si][j], cd.parts[si].at)) : [];
      g.lineWidth = .4 * k; g.strokeStyle = css(EMBER, .12);
      for (let o = 0; o < 3; o++) { const rx = (6.3 + o * 1.4) * 1.05 * k; g.beginPath(); g.ellipse(X.D[0], X.D[1], rx, rx * .3, -.3 + o * 1.05, 0, TAU); g.stroke(); }
      // the form's threads; a seed's own thread fades while the seed is away, so the sender stands emptier
      let seatShown = 0;
      if (st.form > .003) lv.parts.forEach((pl, pi) => {
        if (pi !== si) { strokeLines(g, F, pl, 1.6 * k, .06 * st.form); strokeLines(g, F, pl, .45 * k, .3 * st.form); return; }
        pl.forEach((l, j) => { const a = st.form * (1 - away[j]); seatShown += a; strokeLines(g, F, [l], 1.6 * k, .06 * a); strokeLines(g, F, [l], .45 * k, .3 * a); });
      });
      const back = [], front = [], sp = rec ? [] : null, roles = rec ? new Array(N).fill(-1) : null;
      for (let i = 0; i < N; i++) {
        const pi = al.part[i], j = pi === si ? al.rank[i] : -1;
        if (j >= 0 && outOf(j, D)) continue;   // away as its dot, drawn with the gauge
        const I = orbit(i, X);
        let p = I, z = I[4], heat = idleHeat(i, X), q = 0;
        if (pi >= 0) {
          const lag = hsh(i + 50);
          q = st.inF ? sm(c01((st.e - lag * LAG) / SPAN)) : 1 - sm(c01((st.e - lag * LAG_OUT) / SPAN_OUT));
          if (q > 0) {
            const q0 = j >= 0 ? seats[j] : onPath(PP[pi], (al.rank[i] + .5) / al.count[pi] + X.t * cd.parts[pi].flow / cd.PL0[pi]), sp_ = toW(F, q0);
            if (q >= 1) p = sp_;
            else {
              const e = sm(q), dx = sp_[0] - I[0], dy = sp_[1] - I[1], d = Math.hypot(dx, dy) || 1;
              p = [lerp(I[0], sp_[0], e), lerp(I[1], sp_[1], e), dx / d, dy / d];
              if (i % 4 === 0) line2(g, I, p, .35 * k, .07);
            }
            z = q > .5 ? 1 : z;
            heat = formHeat(i, X.t) + .3 * Math.sin(q * Math.PI) + (cd.lamp ? .12 * q : 0);
          }
        }
        (z < 0 ? back : front).push([p, heat]);
        if (sp) sp.push([p[0], p[1], pi, q]);
        if (roles) roles[i] = q <= 0 ? 0 : q < 1 ? 1 : 2;
      }
      for (const [p, h] of back) mote(c, g, p, SPARK * k, h, k);
      coal(c, g, X);
      for (const [p, h] of front) mote(c, g, p, SPARK * k, h, k);
      return { F, lv, sp, roles, seatShown, mouth: toW(F, lv.mouth), seats: seats.map(q => toW(F, q)) };
    }

    // ---------- the gauge (ScanDots): out from the sender (today: the spyglass's mouth) to the row over the first wreck, from
    // there to the second, home to the sender (today: into the eye, fading). A seed grows from a spark into a dot and back ----------
    function drawDots(T, X, st, b) {
      const D = st.dots, cd = T.cand, c = X.c, g = X.g; if (!D || cd.lamp) return null;
      const seeds = cd.seedPart >= 0, al = alloc(cd), eye = eyeOf(X), pos = [];
      let home = 0, sum = 0, drawn = 0, dup = 0;
      for (let j = 0; j < NDOT; j++) {
        const q = dotQ(j, D), out = outOf(j, D);
        let start, goal, alpha = 1;
        if (D.ph === 'a') { start = seeds ? b.seats[j] : b.mouth; goal = homeOf(READS[0], j); }
        else if (D.ph === 'hop') { start = homeOf(READS[0], j); goal = homeOf(READS[1], j); }
        else { start = homeOf(READS[1], j); goal = seeds ? b.seats[j] : eye; if (!seeds) alpha = 1 - q; }
        if (seeds ? !out : alpha <= .003) { pos.push(null); continue; }
        const there = D.ph === 'home' ? q <= 0 : q >= 1, e = sm(q), wx = goal[0] - start[0], wy = goal[1] - start[1], dd = Math.hypot(wx, wy);
        const fly = !there && dd > 1e-4, p = there ? (D.ph === 'home' ? start : goal) : [start[0] + wx * e, start[1] + wy * e];
        const lit = litOf(D.r, j), fl = .82 + .18 * Math.sin(X.t * 9 + j * 2.3), dotHeat = (lit > 0 ? (.3 + .62 * lit) * fl : .04) * alpha;
        const aw = seeds ? awayOf(j, D) : 1, heat = lerp(formHeat(j, X.t), dotHeat, aw), side = seeds ? lerp(SPARK, DOT.side, aw) : DOT.side;
        if (fly && j % 2 === 0) line2(g, start, p, .35, .07 * alpha);
        mote(c, g, [p[0], p[1], fly ? wx / dd : 0, fly ? wy / dd : 0], side, heat, 1);
        pos.push(p); drawn++;
        if (seeds && b.roles) { const i = al.bySeed[j]; if (b.roles[i] !== -1) dup++; b.roles[i] = 3; }
        if (there && D.ph !== 'home') { home++; sum += lit; }
      }
      return { home, shown: sum * D.r.max / NDOT, want: D.r.left, O: D.r.O, box: D.r.W.box, ph: D.ph, drawn, dup, pos };
    }

    // ---------- the lamp's light: a cone along the aim, a pool where it lands, the hero lit ----------
    function beam(c, D, A, to, L) {
      if (L <= .003) return;
      const len = Math.hypot(to[0] - D[0], to[1] - D[1]) + 50;
      c.save(); c.globalCompositeOperation = 'lighter'; c.translate(D[0], D[1]); c.rotate(A);
      const gr = c.createLinearGradient(0, 0, len, 0); gr.addColorStop(0, css(LAMP, .2 * L)); gr.addColorStop(1, css(LAMP, 0));
      c.fillStyle = gr; c.beginPath(); c.moveTo(2, -3); c.lineTo(len, -len * .26); c.lineTo(len, len * .26); c.lineTo(2, 3); c.closePath(); c.fill(); c.restore();
      halo(c, to[0], to[1], 64, LAMP, .2 * L);
    }

    // ---------- the light buffer: everything that glows, added onto the picture ----------
    function lightStart(T) {
      const g = T.hbg; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.hb.width, T.hb.height);
      g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalCompositeOperation = 'lighter'; g.lineCap = 'round'; g.lineJoin = 'round';
      return g;
    }
    function lightEnd(T) {
      const g = T.hbg; g.globalCompositeOperation = 'source-over';
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0); c.restore();
    }
    // the loupe: the drone and its form ×3, the same moment; a seed away is missing from it
    const LP = { x: 756, y: 10, w: 234, h: 234 };
    function loupe(T) {
      if (!OPT.loupe || !T.st) return false;
      const c = T.c, h = T.me;
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.fillStyle = '#05070b'; c.fill(); c.strokeStyle = 'rgba(140,150,165,.35)'; c.lineWidth = 1; c.stroke();
      c.save(); rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip();
      const g = lightStart(T), D = [LP.x + 142, LP.y + 108 + Math.sin(T.time * 1.7 + h.seed) * 1.5 * LK];
      drawBody(T, mkX(c, g, D, h.d.ang, LK, T.time + h.seed), T.st, false);
      lightEnd(T);
      c.restore();
      c.font = '11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(200,205,214,.55)';
      c.fillText('лупа ×3', LP.x + 8, LP.y + LP.h - 8);
      return true;
    }
    const aimOf = (T, st) => T.real ? T.ptr : st.cur.p;

    return {
      cands, expose: { TL, READS: READS.map(r => ({ O: r.O, left: r.left, max: r.max, box: r.W.box })), N, NDOT, alloc },
      init(T) {
        T.me.face = -1; T.t0 = 0; T.st = null;
        if (T.cand.ref) {   // today's, shown beside the candidates: no pick
          const b = T.sec.querySelector('.pick'); b.hidden = true; b.disabled = true;
          T.sec.querySelector('.tag').textContent = 'сейчас в игре · не выбирается';
          T.sec.style.borderStyle = 'dashed';
        }
      },
      onScale(T) { const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S); T.hb = cv; T.hbg = cv.getContext('2d'); },
      // the hero stands right of the wrecks, facing them; his head follows the pointer while it is out
      step(T, dt) {
        const me = T.me; me.x = HERO_X;
        const st = T.st = stateOf(T);
        if (!T.real) {
          const look = st.cur.a > 0 ? st.cur.p : null, nk = toS(me, NECK());
          turnHead(me, look ? headTurn((look[0] - nk[0]) * me.face, look[1] - nk[1]) : 0, dt);
        }
      },
      // the form and the lens turn to the pointer while the form is wanted or leaving; else the lamp's way
      aim(T) { const st = T.st; return !st ? null : st.inF || st.e < 1.2 ? aimOf(T, st) : null; },
      ownDrone: () => true,
      back(T) {
        drawBack(T); drawWreck(T.c, WB); drawWreck(T.c, WA);
        const st = T.st, h = T.me; if (!st) return;
        const L = lightOf(T.cand, st);
        if (L > 0) { lampOn(T.c, h.d.p[0], h.d.p[1], h.x, h.y, h.face, L, 2.6); beam(T.c, h.d.p, h.d.ang, aimOf(T, st), L); }
      },
      overDrone(T, h) {
        const st = T.st, c = T.c;
        const g = lightStart(T), X = mkX(c, g, h.d.p.slice(), h.d.ang, 1, T.time + h.seed);
        const b = drawBody(T, X, st, true), rd = drawDots(T, X, st, b);
        T.dm = { ph: st.ph, u: st.u, F: b.F, live: b.lv.parts, sp: b.sp, roles: b.roles, seatShown: b.seatShown, rd, light: lightOf(T.cand, st), aim: aimOf(T, st), D: X.D,
          real: !!T.real, stands: st.inF && st.e >= 1, loupe: false, time: T.time };
        lightEnd(T);
      },
      screen(T) {
        const cur = T.st && T.st.cur;
        if (cur && !T.real && cur.a > .003) { T.c.globalAlpha = cur.a; drawCursor(T.c, cur.p[0], cur.p[1]); T.c.globalAlpha = 1; }
        if (T.dm) T.dm.loupe = loupe(T);
      },
      hud(T) {
        const st = T.st; if (!st) return '';
        const lamp = !!T.cand.lamp, today = T.cand.id === 'spyglass';
        if (st.ph === 'rest') return 'покой: искры бегут по орбитам';
        if (st.ph === 'gather') return lamp ? 'искры летят по нитям в зеркало' : 'искры летят по нитям в форму';
        if (lamp) return st.ph === 'leave' ? 'указатель ушёл с обломков' : st.ph === 'back' ? 'зеркало распускается' : 'фонарь светит туда, куда указатель';
        if (st.ph === 'a') return `${today ? 'точки летят из устья трубы' : 'форма раскрылась: её точки улетают'} · первый обломок: ${READS[0].left} из ${READS[0].max}`;
        if (st.ph === 'hop') return 'указатель на другом обломке: точки перепрыгивают, не возвращаясь';
        if (st.ph === 'b') return `второй обломок: ${READS[1].left} из ${READS[1].max} (на экране без цифр)`;
        if (st.ph === 'leave') return today ? 'указатель ушёл: точки уходят в глаз' : 'указатель ушёл: точки возвращаются на места, форма закрывается';
        return st.e < 1.1 ? 'форма распускается: искры обратно на орбиты' : 'покой';
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    off: [['const q0 = j >= 0 ? seats[j]', 'const q0 = j >= 0 ? [seats[j][0], seats[j][1] + .4, 0, 0]']],
    turn: [['const F = frameOf(X.D, X.k, X.A);', 'const F = frameOf(X.D, X.k, Math.PI);']],
    share: [["{ id: 'lens', ru: 'линза', share: .42", "{ id: 'lens', ru: 'линза', share: .38"]],
    twice: [['if (j >= 0 && outOf(j, D)) continue;', 'if (false) continue;']],
    full: [['const a = st.form * (1 - away[j]);', 'const a = st.form;']],
    hop: [["else if (D.ph === 'hop') { start = homeOf(READS[0], j);", "else if (D.ph === 'hop') { start = seeds ? b.seats[j] : b.mouth;"]],
    light: [['const lightOf = (cd, st) => cd.lamp ? st.form : 0;', 'const lightOf = (cd, st) => cd.lamp ? st.form : .3;']],
    ref: [['b.hidden = true;', 'b.hidden = false;']],
  },

  // one whole loop; then slow, a pause, the loupe off, a real pointer; a pick
  check(K) {
    const X = K.X, N = X.N, L = Math.round(X.TL.loop * 60), B = L + 10, E = B + 220;
    const ORDER = ['rest', 'gather', 'a', 'hop', 'b', 'leave', 'back'];
    K.at(B, () => K.seg('speedMode', 1));
    K.at(B + 60, () => { K.seg('speedMode', 0); K.seg('playMode', 1); });
    K.at(B + 110, () => { K.seg('playMode', 0); K.seg('loupeMode', 1); K.ptrAll([300, 400]); });
    K.at(B + 160, () => K.ptrAll(null));
    let st1 = '', doc1 = null, fb = '';
    K.at(E - 30, () => K.click(2, '.pick'));
    K.at(E - 20, () => { st1 = K.byId.status.textContent; doc1 = K.picked(); K.byId.copy.listeners.click[0](); fb = K.byId.fallback.value; });
    const dist = (p, polys) => {
      let best = 1e9;
      for (const pl of polys) for (let j = 1; j < pl.length; j++) {
        const a = pl[j - 1], b = pl[j], vx = b[0] - a[0], vy = b[1] - a[1], l2 = vx * vx + vy * vy, t = l2 ? Math.max(0, Math.min(1, ((p[0] - a[0]) * vx + (p[1] - a[1]) * vy) / l2)) : 0;
        best = Math.min(best, Math.hypot(p[0] - a[0] - vx * t, p[1] - a[1] - vy * t));
      }
      return best;
    };
    const toL = (F, p) => { const dx = (p[0] - F.O[0]) / F.k, dy = (p[1] - F.O[1]) / F.k; return [dx * F.ca + dy * F.sa, (-dx * F.sa + dy * F.ca) / F.fy]; };
    const angDiff = (a, b) => { let d = a - b; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI; return Math.abs(d); };
    const per = K.tiles.map(() => ({ seq: [], stood: 0, offs: 0, worst: 0, sums: 0, badSum: 0, full: 0, bald: 0, badSeat: 0, turned: 0, worstTurn: 0, aMin: 9, aMax: -9,
      seen: {}, hopF: 0, jump: 0, prev: null, lit: 0, dark: 0, lOn: 0, lOff: 0, paused: 0, lastT: -1 }));
    K.run(E, (t, i) => {
      const q = per[K.tiles.indexOf(t)], d = t.dm, cd = t.cand; if (!d) return;
      const loop = i < L - 1, rd = d.rd;
      if (loop && q.seq[q.seq.length - 1] !== d.ph) q.seq.push(d.ph);
      if (!cd.ref) {
        // every spark drawn once: on an orbit, between, on the form or away as a dot; while the form stands, form + away = all
        const n = [0, 0, 0, 0]; let unset = 0;
        d.roles.forEach(r => { if (r < 0) unset++; else n[r]++; });
        const dup = rd ? rd.dup : 0;
        if (unset || dup || n[0] + n[1] + n[2] + n[3] !== N || (d.stands && n[2] + n[3] !== N) || (rd && rd.drawn !== n[3])) {
          q.badSum++; if (q.badSum < 3) K.fail(`${cd.id}: sparks orbit ${n[0]} between ${n[1]} form ${n[2]} away ${n[3]}, unset ${unset}, twice ${dup}, dots ${rd ? rd.drawn : 0} at ${d.ph}, frame ${i}`);
        } else q.sums++;
        if (d.stands && Math.abs(d.F.fy) >= .5) {
          q.stood++;
          for (const s of d.sp) {
            if (s[2] < 0 || s[3] < 1) continue;
            const m = dist(toL(d.F, s), d.live[s[2]]);
            q.worst = Math.max(q.worst, m); if (m > .05) q.offs++;
          }
          // the sender emptier while its seeds are away: their threads gone with them, back as they return
          if (rd && (rd.ph === 'hop' || (rd.ph === 'a' && rd.home === X.NDOT))) { if (d.seatShown > 1e-6 || n[3] !== X.NDOT) q.badSeat++; else q.bald++; }
          if (n[3] === 0 && loop && d.ph !== 'back') { if (Math.abs(d.seatShown - X.NDOT) > 1e-6) q.badSeat++; else q.full++; }
        }
      }
      if (loop && (d.ph === 'a' || d.ph === 'b') && d.stands && !d.real && d.u > (d.ph === 'a' ? X.TL.on : X.TL.hop) + .3) {
        const want = Math.atan2(d.aim[1] - d.D[1], d.aim[0] - d.D[0]), m = angDiff(d.F.A, want);
        q.worstTurn = Math.max(q.worstTurn, m); q.turned++; q.aMin = Math.min(q.aMin, d.F.A); q.aMax = Math.max(q.aMax, d.F.A);
      }
      if (cd.lamp) { if (rd) K.fail(`${cd.id}: dots by the lamp`); if (d.stands && d.light > .9) q.lit++; }
      else if (d.light !== 0) K.fail(`${cd.id}: the lamp's light ${d.light.toFixed(2)} at ${d.ph}, frame ${i}`); else q.dark++;
      if (rd && loop) {
        if (rd.home === X.NDOT) {
          if (Math.abs(rd.shown - rd.want) > .01) K.fail(`${cd.id}: the dots show ${rd.shown.toFixed(1)}, want ${rd.want}`);
          if (Math.abs(rd.O[0] - (rd.box[0] + rd.box[2]) / 2) > .01 || rd.O[1] >= rd.box[1]) K.fail(`${cd.id}: the dots at ${rd.O} are not over their wreck's top`);
          q.seen[rd.ph] = (q.seen[rd.ph] || 0) + 1;
        }
        // the hop: wreck to wreck, every dot out the whole time, none jumping between frames
        if (rd.ph === 'hop') { q.hopF++; if (rd.drawn !== X.NDOT) K.fail(`${cd.id}: ${rd.drawn} dots out during the hop, frame ${i}`); }
        if (q.prev && rd.ph === 'hop') rd.pos.forEach((p, j) => { const o = q.prev[j]; if (p && o && Math.hypot(p[0] - o[0], p[1] - o[1]) > 60) q.jump++; });
      }
      q.prev = rd && loop ? rd.pos : null;
      if (i < B + 110) { if (d.loupe) q.lOn++; } else if (i > B + 111) { if (d.loupe) K.fail(`${cd.id}: the loupe drawn while off, frame ${i}`); else q.lOff++; }
      if (i > B + 61 && i < B + 109) { if (q.lastT >= 0 && d.time !== q.lastT) K.fail(`${cd.id}: time ran while paused`); q.paused++; }
      q.lastT = d.time;
    });
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    K.tiles.forEach((t, j) => {
      const q = per[j], cd = t.cand, al = X.alloc(cd), shares = cd.parts.reduce((a, p) => a + p.share, 0), counts = al.count.reduce((a, b) => a + b, 0), si = cd.seedPart;
      const pickHidden = !!K.byId.cands.children[j].querySelector('.pick').hidden;
      console.log(`${cd.id}${cd.ref ? ' (today)' : ''}: order ${q.seq.join(' ')} | stood ${q.stood} off ${q.offs} worst ${q.worst.toFixed(4)} | sums ok ${q.sums} bad ${q.badSum} | bald ${q.bald} full ${q.full} badSeat ${q.badSeat} | turn ${q.turned} worst ${q.worstTurn.toFixed(3)} swing ${(q.aMax - q.aMin).toFixed(3)} | sparks ${al.count.join('+')}=${counts} | dots ${JSON.stringify(q.seen)} hop ${q.hopF} jumps ${q.jump} | light ${cd.lamp ? 'lit ' + q.lit : 'out ' + q.dark} | pick ${pickHidden ? 'hidden' : 'shown'}`);
      need(q.seq.slice(0, ORDER.length).join() === ORDER.join(), `${cd.id}: rest, gather, first wreck, hop, second wreck, leave, back in order`);
      need(q.lOn > 300 && q.lOff > 40 && q.paused > 40, `${cd.id}: the loupe on and off, the pause`);
      need(pickHidden === !!cd.ref, `${cd.id}: ${cd.ref ? "today's is not pickable" : 'a candidate is pickable'}`);
      need(q.turned > 120 && q.worstTurn < .2 && q.aMax - q.aMin > .15, `${cd.id}: the form turns toward the aim (worst ${q.worstTurn.toFixed(3)} rad, swing ${(q.aMax - q.aMin).toFixed(3)})`);
      if (cd.lamp) { need(q.lit > 150, `${cd.id}: the lamp shines while its mirror stands`); return; }
      need(q.dark > 500, `${cd.id}: the lamp's light is out`);
      need((q.seen.a || 0) > 40 && (q.seen.hop || 0) > 40 && q.hopF > 60 && q.jump === 0, `${cd.id}: the dots over the first wreck, then hopping straight to the second (${q.jump} jumps)`);
      if (cd.ref) return;
      need(q.badSum === 0 && q.sums > 500, `${cd.id}: the sparks out and in the form sum to all ${N} at every moment`);
      need(q.stood > 200 && q.offs === 0, `${cd.id}: every spark on the form's polylines once it has formed (${q.offs} off, worst ${q.worst.toFixed(3)} px)`);
      need(q.bald > 60 && q.full > 20 && q.badSeat === 0, `${cd.id}: the sender emptier while its seeds are out, full when they are home`);
      need(Math.abs(shares - 1) < 1e-9 && counts === N && al.part.every(p => p >= 0) && si >= 0 && al.count[si] === X.NDOT && cd.K0.lines[si].length === X.NDOT,
        `${cd.id}: the shares sum to all ${N} sparks, ${X.NDOT} of them seeds on ${X.NDOT} lines (${shares.toFixed(4)}, ${counts})`);
    });
    const cand = K.tiles[2].cand;
    console.log('after the pick:', st1, '| doc', JSON.stringify(doc1), '| copy:', fb);
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    need(doc1 && doc1.pick === cand.id, 'the pick is in the db doc');
    need(st1.includes(`«${cand.name}»`) && fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the status and the copy text name the pick');
    return ok;
  },
};
