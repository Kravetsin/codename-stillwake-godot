// The light drone made of embers alone: no plates, no hologram language. Its body, the lamp's mirror, the scanner's spyglass,
// the gesture's fan, its thoughts, the scan's gauge and a window's frame, all from its embers: an art-pick page on
// tools/pick-kit (README.md). node tools/pick-kit/build.js tools/pick-kit/pages/drone-embers.js <out-dir> [--break <name>]
module.exports = {
  title: 'Дрон из углей',
  heading: 'Дрон из одних углей: тело, фигуры, мысли, скан и окна',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Дрон без пластин: только тлеющее ядро и его угли. Всё, что делали пластины и голограммы, собирают сами угли: <b>зеркало фонаря</b>, <b>подзорную трубу</b> сканера, <b>веер</b> жеста, <b>знаки мыслей</b> (как «Угли» на прошлой странице), <b>шкалу прочности</b> над обломком и <b>рамку окна</b>. Здесь пять способов, как угли становятся телом и фигурами. Каждый проходит один круг: покой → мысль «холод» → фонарь → скан обломка (на 3,3 с по нему удар) → мысль «метеорит» с тревогой: угли краснеют → сумка открывается и закрывается → «молния» → жест → «любопытно».</p>
<p class="lead">Шкала скана без цифр, в двух видах: <b>точки</b> (горящие — сколько осталось) или <b>полоса</b>, как полоска здоровья; переключатель вверху сохраняется вместе с выбором. Окно внутри остаётся экраном (сетка ячеек здесь заглушка), а его рамка — сотни тлеющих квадратиков: они вылетают из ядра, и рамка тлеет, пока окно открыто. Масштаб игры: клетка 8 px мира, вид ×4, герой ~42 px мира; в лупе справа вверху дрон ×2,5.</p>
<p class="miss">Не показано: звук; что видят друзья; отрисовка Godot (свет и цвета выйдут чуть иначе); окно плана — рамка у него та же, что у сумки; что внутри окна (заглушка); какие мысли будут в игре и что их вызывает.</p>`,
  toggles: [
    { id: 'gaugeMode', bar: 1, label: 'Шкала скана:', key: 'gauge', opts: [['точки', 'dots'], ['полоса', 'bar']], saved: true, short: 'шкала' },
    { id: 'showMode', label: 'Показ:', key: 'show', opts: [['весь круг', 'all'], ['только мысли', 'thoughts'], ['только скан', 'scan'], ['только окно', 'window']] },
    { id: 'loupeMode', label: 'Лупа:', key: 'loupe', opts: [['есть', true], ['нет', false]] },
  ],
  pick: { doc: 'picks/droneEmbers', subject: 'Дрон из углей', what: ['сборку', 'сборкой'] },

  page() {
    const TAU = Math.PI * 2;
    // ---------- the loop: a slot per state; a thought holds 1.5 s ----------
    const SLOT = 2.5, HOLD = 1.5;
    const LEN = { idle: 1.5, cold: SLOT, lamp: 3.2, scan: 6, meteor: SLOT, window: 4.2, lightning: SLOT, gesture: 2.8, curious: SLOT };
    const THOUGHTS = ['cold', 'meteor', 'lightning', 'curious'];
    const ORDER = ['idle', 'cold', 'lamp', 'scan', 'meteor', 'window', 'lightning', 'gesture', 'curious'];
    const SC = { arrive: .35, sweep: [.35, 1.55], show: [1.55, 1.95], hit: 3.3, gone: [5, 5.4] };
    const VAL = [340, 230], MAX = 520, NDOT = 26, STEP = MAX / NDOT, BAR = { w: 180, h: 7, col: 3 };
    // a window: its embers fly from the core (each WN.one s, the last landing at WN.fly), the screen inside after; closing, the reverse
    const WN = { fly: .75, one: .3, inside: [.8, 1], close: 3, back: [3.15, 3.9] };
    const FORM = .6;   // the lamp's mirror, the spyglass and the fan form and fall apart in this long
    const SAY = { idle: 'покой: угли вокруг ядра', cold: 'холод: уходи отсюда', meteor: 'летит метеорит · тревога: угли красные', lightning: 'молния рядом', curious: 'любопытно (настроение)', lamp: 'фонарь: зеркало из углей за ядром', gesture: 'жест: веер из углей' };
    const OFF = [0, -44], RI = 17, LOUPE_K = 2.5, HERO_X = 640;   // a thought's centre from the drone's and its half size, page px at ×1
    const RED = [255, 58, 40], COOL = [140, 196, 255];
    const LOOP = ORDER.reduce((v, id) => v + LEN[id], 0);

    const noise = (t, s) => c01(.5 + .3 * Math.sin(t * 31.7 + s) + .22 * Math.sin(t * 13.1 + s * 2.3) + .18 * Math.sin(t * 77.3 + s * .7));
    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
    const circ = (cx, cy, r, n = 22) => arcP(cx, cy, r, r, 0, TAU, n);

    // ---------- paths: polylines walked by a share of their length; a frame places them on screen ----------
    function mkPath(polys) {
      const seg = []; let L = 0;
      for (const p of polys) for (let i = 1; i < p.length; i++) { const a = p[i - 1], b = p[i], d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 1e-6) continue; seg.push([a[0], a[1], b[0], b[1], d, L]); L += d; }
      return { seg, L, polys };
    }
    // the point s of the way along (wrapping), and its heading
    function onPath(P, s) {
      const d = (((s % 1) + 1) % 1) * P.L; let lo = 0, hi = P.seg.length - 1;
      while (lo < hi) { const m = (lo + hi + 1) >> 1; if (P.seg[m][5] <= d) lo = m; else hi = m - 1; }
      const q = P.seg[lo], e = c01((d - q[5]) / q[4]);
      return [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e, (q[2] - q[0]) / q[4], (q[3] - q[1]) / q[4]];
    }
    const frameOf = (O, sc, A) => ({ O, sc, ca: Math.cos(A), sa: Math.sin(A) });
    const toW = (F, q) => [F.O[0] + (q[0] * F.ca - q[1] * F.sa) * F.sc, F.O[1] + (q[0] * F.sa + q[1] * F.ca) * F.sc, q[2] * F.ca - q[3] * F.sa, q[2] * F.sa + q[3] * F.ca];
    function strokeShape(g, sh, w, col, a) {
      if (a <= .003) return;
      g.beginPath();
      for (const p of sh.P.polys) p.forEach((q, j) => { const r = toW(sh.F, [q[0], q[1], 0, 0]); if (j) g.lineTo(r[0], r[1]); else g.moveTo(r[0], r[1]); });
      g.lineWidth = w; g.strokeStyle = css(col, a); g.stroke();
    }
    function line2(g, a, b, w, col, al) { if (al <= .003) return; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.lineWidth = w; g.strokeStyle = css(col, al); g.stroke(); }

    // ---------- the signs (box -1..1, y down), as on the previous page ----------
    const GLYPH = (() => {
      const G = {};
      G.cold = [];
      for (let i = 0; i < 3; i++) { const a = Math.PI / 2 + i * Math.PI / 3; G.cold.push([[-Math.cos(a), -Math.sin(a)], [Math.cos(a), Math.sin(a)]]); }
      for (let i = 0; i < 6; i++) { const a = Math.PI / 2 + i * Math.PI / 3, P = [Math.cos(a) * .56, Math.sin(a) * .56], b = q => [P[0] + Math.cos(q) * .32, P[1] + Math.sin(q) * .32]; G.cold.push([b(a - .9), P, b(a + .9)]); }
      const C = [-.42, .42], rock = [];
      [.36, .3, .38, .29, .35, .31, .37].forEach((r, i) => { const a = i / 7 * TAU + .3; rock.push([C[0] + Math.cos(a) * r, C[1] + Math.sin(a) * r]); });
      rock.push(rock[0].slice());
      const d = [.7071, -.7071], n = [.7071, .7071];
      G.meteor = [rock, ...[[-.24, .75], [0, 1.05], [.24, .7]].map(([o, L]) => { const s = [C[0] + d[0] * .44 + n[0] * o, C[1] + d[1] * .44 + n[1] * o]; return [s, [s[0] + d[0] * L, s[1] + d[1] * L]]; })];
      G.lightning = [[[.3, -1], [-.34, .08], [.14, .08], [-.28, 1]], [[-.28, 1], [-.27, .66]], [[-.28, 1], [-.02, .8]]];
      G.curious = [arcP(0, -.44, .4, .4, Math.PI - .15, TAU + 1.05, 18).concat([[0, .1], [0, .4]]), circ(0, .8, .08, 8)];
      return G;
    })();
    // the mode shapes, page px at ×1 in the aim's frame (x along the aim): the lamp's mirror cupping the core, the spyglass on
    // the aim with its hood behind, the gesture's fan (x up)
    const PATH = {};
    for (const id of THOUGHTS) PATH[id] = mkPath(GLYPH[id]);
    const para = (back, half, cv, n) => { const p = []; for (let i = 0; i <= n; i++) { const v = -half + 2 * half * i / n; p.push([-back + v * v / cv, v]); } return p; };
    PATH.lamp = mkPath([para(9, 12, 14, 14), para(7.2, 10, 15, 12)]);
    const rect = (u0, u1, h) => [[u0, -h], [u1, -h], [u1, h], [u0, h], [u0, -h]];
    PATH.scan = mkPath([rect(3, 11, 3.6), rect(10, 19, 2.8), arcP(21.5, 0, 1.3, 4.4, 0, TAU, 14), arcP(-2, 0, 5.5, 5.5, Math.PI / 2, 3 * Math.PI / 2, 12)]);
    PATH.gesture = mkPath((() => { const f = []; for (let j = 0; j < 7; j++) { const a = (j / 3 - 1) * .8; f.push([[Math.cos(a) * 6, Math.sin(a) * 6], [Math.cos(a) * 19, Math.sin(a) * 19]]); } f.push(arcP(0, 0, 19, 19, -.8, .8, 14)); return f; })());
    const MOUTH = [21.5, 0, 1, 0];

    // ---------- the wreck: a torn hull plate on the deck, left of the hero (the previous page's) ----------
    const WRECK = (() => {
      const pts = [[392, 344], [404, 362], [436, 368], [450, 392], [440, 414], [452, 438], [447, 470], [190, 470], [195, 421], [213, 404], [225, 375], [262, 366], [300, 353], [348, 350], [372, 338]];
      const s = mk(pts, 4242, .8), OL = [];
      for (const q of s.segs) for (let i = 0; i < 5; i++) OL.push(bez([q[0], q[1]], [q[2], q[3]], [q[4], q[5]], i / 5));
      OL.push(OL[0].slice());
      const xs = OL.map(p => p[0]), ys = OL.map(p => p[1]), box = [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
      const r = rng(909), rust = [];
      for (const [x, y, sz] of [[258, 398, 4], [326, 432, 5], [398, 398, 3.5], [244, 452, 3], [418, 440, 4], [362, 398, 2.5]]) rust.push(blobAt(r, x, y, sz));
      const rivets = [];
      for (const y of [394, 428]) for (let x = 204; x < 446; x += 18) rivets.push([x, y]);
      for (const x of [254, 322, 394]) for (let y = 372; y < 466; y += 18) rivets.push([x, y]);
      return {
        s, OL, box, C: [(box[0] + box[2]) / 2, (box[1] + box[3]) / 2], rust, rivets, hole: shardShape(r, 9), HOLE: [306, 416],
        seams: [[[180, 398], [460, 398]], [[180, 432], [460, 432]], [[258, 330], [258, 470]], [[326, 330], [326, 470]], [[398, 330], [398, 470]]],
        cracks: [[[294, 406], [288, 399], [283, 397]], [[322, 420], [330, 424], [332, 431]], [[300, 426], [298, 433]], [[440, 380], [432, 386]]],
        rib: [[236, 384], [222, 356], [230, 334], [246, 326]],
      };
    })();
    const READ_AT = [WRECK.C[0], WRECK.box[1] - 20], HIT_AT = [440, 372];
    const OLP = mkPath([WRECK.OL]), tipOL = f => onPath(OLP, Math.min(.9999, Math.max(0, f)));
    const TR = Array.from({ length: 90 }, (_, m) => tipOL(m / 90));   // the smouldering trail the scan leaves on the edge
    function lines(c, polys) { c.beginPath(); for (const p of polys) { c.moveTo(p[0][0], p[0][1]); for (let i = 1; i < p.length; i++) c.lineTo(p[i][0], p[i][1]); } }
    function drawWreck(c) {
      const W = WRECK, b = W.box;
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
      c.save(); c.translate(W.HOLE[0], W.HOLE[1]); trace(c, W.hole); c.fillStyle = '#0a0b0e'; c.fill(); ink(c, W.hole, 0, 1.4); c.restore();
      lines(c, W.cracks); c.lineWidth = 1; c.strokeStyle = INK; c.stroke();
      c.save(); c.translate(0, 2); trace(c, W.s); c.strokeStyle = 'rgba(128,136,146,.45)'; c.lineWidth = 2; c.stroke(); c.restore();
      c.restore();
      ink(c, W.s, 0, 2.2);
    }
    // the hit: sparks off the wreck's right shoulder
    function sparks(c, u) {
      const q = u - SC.hit; if (q < 0 || q > .7) return;
      halo(c, HIT_AT[0], HIT_AT[1], 16, EMBER, .55 * (1 - c01(q / .25)));
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round';
      for (let i = 0; i < 9; i++) {
        const a = -1.9 + hsh(i) * 1.7, v = 60 + hsh(i + 20) * 90, p = [HIT_AT[0] + Math.cos(a) * v * q, HIT_AT[1] + Math.sin(a) * v * q + 160 * q * q];
        const vx = Math.cos(a) * v, vy = Math.sin(a) * v + 320 * q;
        c.strokeStyle = css(pale(EMBER, .3), .9 * (1 - q / .7)); c.lineWidth = 1.1; c.beginPath(); c.moveTo(p[0] - vx * .03, p[1] - vy * .03); c.lineTo(p[0], p[1]); c.stroke();
      }
      c.restore();
    }
    // the danger the meteor thought warns of: a streak over the sky
    function meteorStreak(c, u) {
      if (u > 1.9) return;
      const e = u / 1.9, H = [470 - 340 * e, 26 + 120 * e], Tl = [H[0] + 70, H[1] - 26], a = 1 - c01((e - .8) / .2);
      const gr = c.createLinearGradient(Tl[0], Tl[1], H[0], H[1]); gr.addColorStop(0, 'rgba(255,190,120,0)'); gr.addColorStop(1, css([255, 222, 176], .85 * a));
      c.save(); c.lineCap = 'round'; c.strokeStyle = gr; c.lineWidth = 2; c.beginPath(); c.moveTo(Tl[0], Tl[1]); c.lineTo(H[0], H[1]); c.stroke(); c.restore();
      halo(c, H[0], H[1], 9, [255, 200, 140], .5 * a);
    }

    // ---------- an ember, its glow, the core ----------
    const P170 = DR.P, NP = DR.P.length;
    // a soot square on the picture, its heat in the light buffer
    function glowSq(g, x, y, s, heat, X) { const h = Math.min(1, heat + X.alarm * .55); if (h <= .02) return; g.fillStyle = css(pale(X.hot, .25 * h), .95 * h); g.fillRect(x - s / 2, y - s / 2, s, s); }
    function soot(c, g, x, y, s, heat, i, X) {
      const e = s * .25;
      c.fillStyle = '#1a1b1f'; c.fillRect(x - s / 2 - e, y - s / 2 - e, s + 2 * e, s + 2 * e);
      c.fillStyle = hsh(i + 7) < .5 ? '#606369' : '#4c4f55'; c.fillRect(x - s / 2, y - s / 2, s, s);
      glowSq(g, x, y, s, heat, X);
    }
    const baseHeat = (i, X, share) => hsh(i + 3) < share ? .45 + .4 * Math.sin(X.t * 11 + i * 7.1) : .05 + .05 * Math.sin(X.t * 3 + i);
    // the coal: the old core's crust, its heart glowing through, an ember eye where it looks
    function coal(c, g, X, gr, ga) {
      const x = X.D[0], y = X.D[1], k = X.k, s = DS * DSC * k, A = X.A, fl = .75 + .25 * Math.sin(X.t * 7.3);
      halo(g, x, y, gr * k, X.hot, ga);
      c.save(); c.translate(x, y); c.scale(s, s); paint(c, DR.core, A, DARK, .8, 2.6); ink(c, DR.core, A, 1.7); c.restore();
      g.strokeStyle = css(X.hot, .5 * fl); g.lineWidth = .45 * k; g.beginPath();
      for (const [a, b] of [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]]) { g.moveTo(x + Math.cos(A + a) * .6 * k, y + Math.sin(A + a) * .6 * k); g.lineTo(x + Math.cos(A + b * .5) * 2.6 * k, y + Math.sin(A + b * .5) * 2.6 * k); }
      g.stroke();
      const ex = x + Math.cos(A) * 2.2 * s, ey = y + Math.sin(A) * 2.2 * s;
      g.fillStyle = css(pale(X.hot, .5), .9 * fl); g.beginPath(); g.arc(ex, ey, 1.2 * k, 0, TAU); g.fill();
      halo(g, ex, ey, 4 * k, X.hot, .5 * fl);
    }
    // today's three tilted orbits: a place on them, its heading, and whether it is before the core
    function orbit(i, X, pace, rs) {
      const P = P170[i % NP], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + X.t * (.8 + .25 * o) * dir * pace, rx = (6.3 + o * 1.4 + (P.r1 - .5)) * rs * X.k, tl = -.3 + o * 1.05;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tl), v = rot(-Math.sin(th) * dir, Math.cos(th) * .3 * dir, tl), vl = Math.hypot(v[0], v[1]) || 1;
      return [X.D[0] + q[0], X.D[1] + q[1], v[0] / vl, v[1] / vl, Math.sin(th)];
    }
    // a flight on an arc; going home it sags first, as if crumbling
    function arcFly(S, E, q, i, X, way, lift, side) {
      const k = X.k, mx = (S[0] + E[0]) / 2, my = (S[1] + E[1]) / 2;
      const ctl = way === 'out' ? [mx + (hsh(i + 1) - .5) * side * k, Math.max(S[1], E[1]) + lift * .7 * k] : [mx + (hsh(i + 1) - .5) * side * 2 * k, Math.min(S[1], E[1], my) - lift * k];
      const p = bez(S, ctl, E, q), p2 = bez(S, ctl, E, Math.min(1, q + .03)), dx = p2[0] - p[0], dy = p2[1] - p[1], d = Math.hypot(dx, dy) || 1;
      return [p[0], p[1], dx / d, dy / d];
    }

    // ---------- the candidates ----------
    // idle(i, X): [x, y, dx, dy, z]; place(sh, s, i, X): ember i on the shape s of the way; fly(S, E, q, i, X, way): on its way;
    // heat(i, X, f) with f how far into the shape; mote(c, g, p, s, heat, i, f, X); core; under (drawn before the embers);
    // winLag(sq, j): when a frame square's ember leaves, 0..1; frameSq: a landed frame square
    const cands = [
      {
        id: 'swarm', name: 'Рой', tin: .5, tout: .5, n: 190,
        idea: 'Тело — свободное облако углей без орбит, оно дышит и клубится вокруг ядра. Фигуру рисует плотность: угли толпятся на линиях знака, края мохнатые, отдельные угли блуждают рядом. Перелетают стаей, четырьмя ручьями по дуге; знак осыпается вниз и затягивается обратно в облако.',
        time: 'Знак слетается за 0,5 с · держится 1,5 с · осыпается за 0,5 с · зеркало, труба и веер — за 0,6 с · рамка окна: стая летит 0,75 с, экран — после последнего угля · шкала скана собирается за 0,4 с.',
        idle(i, X) {
          const P = P170[i % NP], k = X.k, t = X.t, a = P.ph + t * (.35 + P.r1 * .5) * (i % 2 ? -1 : 1), r = (3 + 7.5 * Math.sqrt(hsh(i + 21))) * k;
          return [X.D[0] + Math.cos(a) * r + 1.6 * k * Math.sin(t * 1.7 + i * 2.3), X.D[1] + Math.sin(a) * r * .7 + 1.6 * k * Math.cos(t * 1.3 + i * 1.7), -Math.sin(a), Math.cos(a), Math.sin(a + i)];
        },
        place(sh, s, i, X) {
          const q = toW(sh.F, onPath(sh.P, s)), far = hsh(i + 40) < .14 ? 4.2 : 1.7, w = (.35 + .65 * hsh(i + 41)) * far * X.k;
          return [q[0] + w * Math.sin(X.t * 3.1 + i * 1.3), q[1] + w * Math.cos(X.t * 2.7 + i * 2.1), q[2], q[3]];
        },
        fly(S, E, q, i, X, way) {
          const dx = E[0] - S[0], dy = E[1] - S[1], d = Math.hypot(dx, dy) || 1, nx = -dy / d, ny = dx / d, o = (i % 4 - 1.5) * 5 * X.k * (1 + d / 80);
          const ctl = [(S[0] + E[0]) / 2 + nx * o, (S[1] + E[1]) / 2 + ny * o + (way === 'out' ? 7 : -9) * X.k];
          const p = bez(S, ctl, E, q), p2 = bez(S, ctl, E, Math.min(1, q + .03)), w = Math.sin(q * Math.PI) * 1.4 * X.k;
          return [p[0] + w * Math.sin(X.t * 9 + i), p[1] + w * Math.cos(X.t * 8 + i), p2[0] - p[0], p2[1] - p[1]];
        },
        heat(i, X, f) { return baseHeat(i, X, .22) + .22 * f; },
        size(i, X) { return P170[i % NP].sz * .5 * X.k; },
        mote(c, g, p, s, heat, i, f, X) { soot(c, g, p[0], p[1], s, heat, i, X); },
        core(c, g, X) { coal(c, g, X, 10, .3); },
        winLag(sq, j) { return hsh(j * 1.37 + 9); },
        frameSq(c, g, sq, j, X) { soot(c, g, sq.p[0], sq.p[1], 1.9, .18 + .6 * noise(X.t * .45, j * 1.7), j, X); },
      },
      {
        id: 'grid', name: 'Сетка', tin: .45, tout: .4, n: 170, G: 3,
        idea: 'В покое угли кружат свободно, как сейчас. Собираясь в фигуру, каждый защёлкивается в клетку крупной сетки со вспышкой — знак, зеркало и труба выходят чёткими, как пиксель-арт, клетка к клетке. Ядро — тоже пиксели: крестик из клеток с горящей серединой. Отпускает — и снова россыпь.',
        time: 'Знак защёлкивается за 0,45 с · держится 1,5 с · рассыпается за 0,4 с · зеркало, труба и веер — за 0,6 с · рамка окна строится строками сверху вниз за 0,75 с, экран — после последней клетки · шкала за 0,4 с.',
        idle(i, X) { return orbit(i, X, 1, 1); },
        place(sh, s, i, X) {
          const q = toW(sh.F, onPath(sh.P, s)), G = this.G * X.k, ox = Math.round(sh.F.O[0]), oy = Math.round(sh.F.O[1]);
          return [ox + Math.round((q[0] - ox) / G) * G, oy + Math.round((q[1] - oy) / G) * G, q[2], q[3]];
        },
        fly(S, E, q, i, X, way) { return arcFly(S, E, sm(Math.min(1, q / .82)), i, X, way, 8, 6); },
        heat(i, X, f) { return f >= .82 ? (hsh(i + 3) < .5 ? .42 : .2) + .08 * Math.sin(X.t * 6 + i) + (f < 1 ? .4 : 0) : baseHeat(i, X, .2); },
        size(i, X) { return P170[i % NP].sz * .63 * X.k; },
        mote(c, g, p, s, heat, i, f, X) {
          if (f < .82) { soot(c, g, p[0], p[1], s, heat, i, X); return; }
          const G = this.G * X.k, x = p[0] - G / 2, y = p[1] - G / 2, e = .35 * X.k;
          c.fillStyle = '#1c1d21'; c.fillRect(x, y, G, G);
          c.fillStyle = hsh(i + 7) < .5 ? '#55585f' : '#45484e'; c.fillRect(x + e, y + e, G - 2 * e, G - 2 * e);
          glowSq(g, p[0], p[1], G - 2 * e, heat, X);
        },
        core(c, g, X) {
          const k = X.k, G = 2.4 * k, cx = Math.round(X.D[0]), cy = Math.round(X.D[1]), ea = Math.round(Math.cos(X.A)), eb = Math.round(Math.sin(X.A));
          halo(g, cx, cy, 9 * k, X.hot, .28);
          for (let a = -1; a <= 1; a++) for (let b = -1; b <= 1; b++) {
            if (a && b) continue;
            const x = cx + a * G - G / 2, y = cy + b * G - G / 2;
            c.fillStyle = '#17181c'; c.fillRect(x, y, G, G); c.fillStyle = '#3b3e44'; c.fillRect(x + .3 * k, y + .3 * k, G - .6 * k, G - .6 * k);
            const h = !a && !b ? .95 : a === ea && b === eb ? .8 : .22 + .1 * Math.sin(X.t * 5 + a * 3 + b);
            glowSq(g, x + G / 2, y + G / 2, G - .6 * k, h, X);
          }
        },
        winLag(sq) { return (sq.p[1] - WR.y) / WR.h * .75 + (sq.p[0] - WR.x) / WR.w * .25; },
        frameSq(c, g, sq, j, X) {
          const x = Math.round(sq.p[0]) - 1.4, y = Math.round(sq.p[1]) - 1.4;
          c.fillStyle = '#1c1d21'; c.fillRect(x, y, 2.8, 2.8); c.fillStyle = '#4b4e55'; c.fillRect(x + .35, y + .35, 2.1, 2.1);
          glowSq(g, x + 1.4, y + 1.4, 2.1, (hsh(j + 3) < .5 ? .55 : .2) + .25 * (noise(X.t * .3, j) > .7 ? 1 : 0), X);
        },
      },
      {
        id: 'threads', name: 'Нити', tin: .4, tout: .4, n: 120,
        idea: 'Угли бегут по нитям. В покое — три тонкие светящиеся орбиты, по ним носятся искры с хвостиками. Фигура — контур из нитей, по которому течёт ток искр: знак не стоит, а бежит. В фигуру и в рамку окна искры летят по прямой нити из ядра, рамку обегают от ближнего к дрону угла.',
        time: 'Нити знака протягиваются за 0,4 с · держится 1,5 с · сматываются за 0,4 с · зеркало, труба и веер — за 0,6 с · рамка: искры обегают её за 0,75 с, экран — после · шкала за 0,4 с.',
        idle(i, X) { return orbit(i, X, 2.2, 1.05); },
        place(sh, s, i, X) { const Lw = sh.P.L * sh.F.sc; return toW(sh.F, onPath(sh.P, s + X.t * 30 * X.k / Lw)); },
        fly(S, E, q, i, X, way) {
          const e = sm(q), p = [lerp(S[0], E[0], e), lerp(S[1], E[1], e)], dx = E[0] - S[0], dy = E[1] - S[1], d = Math.hypot(dx, dy) || 1;
          if (i % 4 === 0) line2(X.g, S, p, .35 * X.k, X.hot, .07);
          return [p[0], p[1], dx / d, dy / d];
        },
        heat(i, X, f) { return f > 0 ? .7 + .25 * Math.sin(X.t * 13 + i) : hsh(i + 3) < .5 ? .55 + .3 * Math.sin(X.t * 11 + i * 7) : .2; },
        size(i, X) { return .95 * X.k; },
        mote(c, g, p, s, heat, i, f, X) {
          const k = X.k, h = Math.min(1, heat + X.alarm * .55), dx = p[2] || 0, dy = p[3] || 0;
          c.fillStyle = '#26272b'; c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
          if (h <= .02) return;
          for (let m = 0; m < 3; m++) { const b = m * 1.3 * k; g.fillStyle = css(pale(X.hot, .3 * (1 - m / 3)), (.95 - m * .3) * h); g.fillRect(p[0] - dx * b - s / 2, p[1] - dy * b - s / 2, s, s); }
        },
        under(c, g, X, sh, form) {
          const k = X.k;
          g.lineWidth = .4 * k; g.strokeStyle = css(X.hot, .12);
          for (let o = 0; o < 3; o++) { const rx = (6.3 + o * 1.4) * 1.05 * k; g.beginPath(); g.ellipse(X.D[0], X.D[1], rx, rx * .3, -.3 + o * 1.05, 0, TAU); g.stroke(); }
          if (sh) { strokeShape(g, sh, 1.6 * k, X.hot, .06 * form); strokeShape(g, sh, .45 * k, X.hot, .3 * form); }
        },
        core(c, g, X) { coal(c, g, X, 8, .35); },
        winLag(sq) { return Math.abs(sq.per - .5) * 2; },
        frameSq(c, g, sq, j, X) {
          c.fillStyle = '#26272b'; c.fillRect(sq.p[0] - .9, sq.p[1] - .9, 1.8, 1.8);
          glowSq(g, sq.p[0], sq.p[1], 1.6, .14 + .7 * Math.pow(.5 + .5 * Math.sin(sq.per * TAU * 6 - X.t * 4 + sq.row), 6) + .1 * noise(X.t * .4, j), X);
        },
      },
      {
        id: 'tiles', name: 'Чешуя', tin: .55, tout: .45, n: 66,
        idea: 'Угли спеклись в крошечные плитки — броня из пикселей: в покое три кольца плиток вокруг ядра, швы между ними тлеют. Плитка перелетает, переворачиваясь раскалённой изнанкой, и ложится в мозаику знака, зеркала или трубы. Рамка окна выкладывается плитками от дрона наружу.',
        time: 'Знак выкладывается за 0,55 с · держится 1,5 с · разбирается за 0,45 с · зеркало, труба и веер — за 0,6 с · рамка: плитки ложатся 0,75 с, ближние первыми, экран — после · шкала за 0,4 с.',
        idle(i, X) {
          const r = i < 14 ? 0 : i < 36 ? 1 : 2, cnt = [14, 22, 30][r], idx = i - [0, 14, 36][r], dir = r % 2 ? -1 : 1;
          const th = idx / cnt * TAU + X.t * (.55 - .15 * r) * dir, rr_ = (4.6 + r * 2.6) * X.k, tl = -.3 + r * 1.05;
          const q = rot(Math.cos(th) * rr_, Math.sin(th) * rr_ * .5, tl);
          return [X.D[0] + q[0], X.D[1] + q[1], 0, 0, Math.sin(th)];
        },
        place(sh, s, i, X) { const q = toW(sh.F, onPath(sh.P, s)); return [Math.round(q[0] * 2) / 2, Math.round(q[1] * 2) / 2, q[2], q[3]]; },
        fly(S, E, q, i, X, way) { const p = arcFly(S, E, q, i, X, way, 10, 8); p.push(Math.cos(q * Math.PI)); return p; },
        heat(i, X, f) { return .28 + .4 * noise(X.t * .35, i * 2.1) + .2 * f; },
        size(i, X) { return 2.5 * X.k; },
        mote(c, g, p, s, heat, i, f, X) {
          const k = X.k, fl = p.length > 4 ? p[4] : 1, w = s * Math.max(.14, Math.abs(fl)), x = p[0] - w / 2, y = p[1] - s / 2, h = Math.min(1, heat + X.alarm * .55);
          if (fl < 0) { c.fillStyle = '#2a1d17'; c.fillRect(x, y, w, s); g.fillStyle = css(pale(X.hot, .2), .85 * Math.max(h, .55)); g.fillRect(x, y, w, s); return; }
          c.fillStyle = '#17181c'; c.fillRect(x - .3 * k, y - .3 * k, w + .6 * k, s + .6 * k);
          c.fillStyle = hsh(i + 7) < .5 ? '#5b5f66' : '#474a51'; c.fillRect(x, y, w, s);
          c.fillStyle = 'rgba(150,156,166,.35)'; c.fillRect(x, y, w, .45 * k);
          if (h > .02) { g.strokeStyle = css(X.hot, .9 * h); g.lineWidth = .5 * k; g.strokeRect(x, y, w, s); }
        },
        core(c, g, X) { coal(c, g, X, 9, .4); },
        winLag(sq) { return c01((Math.hypot(sq.p[0] - 676, sq.p[1] - 294) - 60) / 280); },
        frameSq(c, g, sq, j, X) { this.mote(c, g, [sq.p[0], sq.p[1]], 2.6, .18 + .55 * noise(X.t * .3, j * 1.3), j, 1, X); },
      },
      {
        id: 'haze', name: 'Жар', tin: .5, tout: .5, n: 210,
        idea: 'Главное — жар ядра: оно горит, а угли вокруг — дымка, что поднимается над ним и оседает, как над костром. Фигура — силуэт из марева: мягкое свечение по линиям знака, зеркала или трубы, по краю всплывают искры. Тревога окрашивает весь жар в красный.',
        time: 'Марево знака встаёт за 0,5 с · держится 1,5 с · оседает за 0,5 с · зеркало, труба и веер — за 0,6 с · рамка окна сгущается снизу вверх за 0,75 с, экран — после · шкала за 0,4 с.',
        life(i, X) { return (X.t * (.35 + .25 * hsh(i + 8)) + hsh(i)) % 1; },
        idle(i, X) {
          const k = X.k, life = this.life(i, X), a = hsh(i + 2) * TAU, r0 = (2 + 3 * hsh(i + 4)) * k;
          return [X.D[0] + Math.cos(a) * r0 + Math.sin(X.t * 1.3 + i) * 2.5 * k * life, X.D[1] + Math.sin(a) * r0 * .6 - life * 16 * k + 3 * k, 0, -1, hsh(i + 9) - .5];
        },
        place(sh, s, i, X) { const q = toW(sh.F, onPath(sh.P, s)), up = (X.t * .8 + hsh(i)) % 1; return [q[0] + Math.sin(X.t * 2 + i) * .6 * X.k, q[1] - up * up * 4.5 * X.k, 0, -1]; },
        fly(S, E, q, i, X, way) { return arcFly(S, E, q, i, X, way, 14, 10); },
        heat(i, X, f) { return f > 0 ? .45 + .3 * f + .2 * Math.sin(X.t * 9 + i) : (1 - this.life(i, X)) * (.25 + .55 * hsh(i + 3)); },
        size(i, X) { return .85 * X.k; },
        mote(c, g, p, s, heat, i, f, X) {
          const h = Math.min(1, heat + X.alarm * .55); if (h <= .02) return;
          c.fillStyle = 'rgba(30,24,22,.45)'; c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
          g.fillStyle = css(pale(X.hot, .35 * h), .9 * h); g.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
        },
        under(c, g, X, sh, form) {
          const k = X.k, fl = .85 + .15 * noise(X.t * .3, 4);
          halo(g, X.D[0], X.D[1] - 4 * k, 26 * k, X.hot, .2 * fl);
          if (sh) for (const [w, a] of [[7, .05], [3.6, .1], [1.5, .2]]) strokeShape(g, sh, w * X.k, X.hot, a * form * fl);
        },
        core(c, g, X) { const k = X.k; halo(g, X.D[0], X.D[1], 13 * k, X.hot, .5); coal(c, g, X, 7, .5); halo(g, X.D[0], X.D[1], 3.5 * k, pale(X.hot, .5), .8); },
        winLag(sq, j) { return c01((WR.y + WR.h - sq.p[1]) / WR.h * .6 + hsh(j + 4) * .4); },
        frameSq(c, g, sq, j, X) {
          const h = .2 + .5 * noise(X.t * .25, j) * (.6 + .4 * Math.sin(sq.p[1] * .08 - X.t * 3));
          c.fillStyle = 'rgba(30,24,22,.5)'; c.fillRect(sq.p[0] - 1, sq.p[1] - 1, 2, 2);
          glowSq(g, sq.p[0], sq.p[1], 1.8, h, X);
        },
      },
    ];

    // ---------- the clock ----------
    function planOf(mode) {
      const ids = mode === 'thoughts' ? THOUGHTS : mode === 'scan' ? ['scan'] : mode === 'window' ? ['window'] : ORDER;
      let t = 0; return ids.map(id => { const q = { id, t0: t, len: LEN[id] }; t += q.len; return q; });
    }
    function thoughtOf(cand, u) {
      if (u < cand.tin) return { ph: 'in', e: u / cand.tin };
      if (u < cand.tin + HOLD) return { ph: 'hold', e: (u - cand.tin) / HOLD };
      if (u < cand.tin + HOLD + cand.tout) return { ph: 'out', e: (u - cand.tin - HOLD) / cand.tout };
      return null;
    }
    // what the body forms in a slot, and how far: a thought, the lamp's mirror, the spyglass, the fan
    function bodyOf(cand, id, u) {
      if (THOUGHTS.includes(id)) { const r = thoughtOf(cand, u); return r && { id, ...r }; }
      if (id === 'lamp' || id === 'gesture') {
        const L = LEN[id] - .2;
        if (u < FORM) return { id, ph: 'in', e: u / FORM };
        if (u < L - FORM) return { id, ph: 'hold', e: 0 };
        if (u < L) return { id, ph: 'out', e: (u - L + FORM) / FORM };
        return null;
      }
      if (id === 'scan') {
        if (u < .1) return null;
        if (u < .1 + FORM) return { id, ph: 'in', e: (u - .1) / FORM };
        if (u < SC.gone[0]) return { id, ph: 'hold', e: 0 };
        if (u < SC.gone[0] + FORM) return { id, ph: 'out', e: (u - SC.gone[0]) / FORM };
      }
      return null;
    }
    // the embers flash red while the meteor thought warns
    function alarmOf(st) {
      if (st.id !== 'meteor' || !st.body) return 0;
      const b = st.body, a = b.ph === 'in' ? b.e : b.ph === 'out' ? 1 - b.e : 1;
      return a * (.6 + .4 * Math.sin(st.u * TAU * 2.4));
    }
    function stateOf(T) {
      const P = planOf(OPT.show), L = P[P.length - 1].t0 + P[P.length - 1].len, u = ((T.time - T.t0) % L + L) % L;
      const q = P.find(p => u >= p.t0 && u < p.t0 + p.len) || P[0];
      const st = { id: q.id, u: Math.max(0, u - q.t0), body: null, alarm: 0 };
      st.body = bodyOf(T.cand, st.id, st.u);
      st.alarm = alarmOf(st);
      return st;
    }
    const formOf = b => !b ? 0 : b.ph === 'in' ? sm(b.e) : b.ph === 'out' ? 1 - sm(b.e) : 1;

    // ---------- the body: idle embers, a share flying into the slot's shape and back ----------
    const CH = {};
    function chosen(n, share) {
      const key = n + ':' + share; if (CH[key]) return CH[key];
      const rank = []; let m = 0;
      for (let i = 0; i < n; i++) rank.push(hsh(i * 3 + 1) < share ? m++ : -1);
      return (CH[key] = { rank, m });
    }
    function shapeOf(st, X) {
      const b = st.body; if (!b) return null;
      const k = X.k;
      if (THOUGHTS.includes(b.id)) return { P: PATH[b.id], F: frameOf([X.D[0] + OFF[0] * k, X.D[1] + OFF[1] * k], RI * k, 0), ph: b.ph, e: b.e, share: .72, thought: true };
      if (b.id === 'gesture') return { P: PATH.gesture, F: frameOf(X.D, k, -Math.PI / 2 + .22 * Math.sin(X.t * 3.2)), ph: b.ph, e: b.e, share: .8 };
      return { P: PATH[b.id], F: frameOf(X.D, k, X.A), ph: b.ph, e: b.e, share: .8 };
    }
    function mkX(c, g, D, A, k, t, st) { return { c, g, D, A, k, t, alarm: st.alarm, hot: st.alarm > 0 ? mix3(EMBER, RED, Math.min(1, st.alarm * 1.4)) : EMBER }; }
    function drawBody(c, g, cand, X, st) {
      const sh = shapeOf(st, X), n = cand.n, pick = sh ? chosen(n, sh.share) : null, list = [];
      const lamp = !!sh && st.body.id === 'lamp';
      for (let i = 0; i < n; i++) {
        const I = cand.idle(i, X);
        let p = I, f = 0, z = I[4], heat = cand.heat(i, X, 0);
        const j = pick ? pick.rank[i] : -1;
        if (j >= 0) {
          const lag = hsh(i + 50);
          let q = 1, out = false;
          if (sh.ph === 'in') q = sm(c01((sh.e - lag * .45) / .55));
          else if (sh.ph === 'out') { q = 1 - sm(c01((sh.e - lag * .4) / .6)); out = true; }
          if (q > 0) {
            const S = cand.place(sh, (j + .5) / pick.m, i, X);
            p = q >= 1 ? S : out ? cand.fly(S, I, 1 - q, i, X, 'out') : cand.fly(I, S, q, i, X, 'in');
            f = q; z = q > .5 ? 1 : z;
            heat = cand.heat(i, X, q) + .3 * Math.sin(q * Math.PI) + (lamp ? .12 * q : 0);
          }
        }
        list.push([p, z, f, heat, i]);
      }
      if (cand.under) cand.under(c, g, X, sh, formOf(st.body));
      for (const e of list) if (e[1] < 0) cand.mote(c, g, e[0], cand.size(e[4], X), e[3], e[4], e[2], X);
      cand.core(c, g, X);
      for (const e of list) if (e[1] >= 0) cand.mote(c, g, e[0], cand.size(e[4], X), e[3], e[4], e[2], X);
      return sh;
    }

    // ---------- the scan: embers run from the spyglass over the wreck's edge, then gather into the gauge over it ----------
    function drawSweep(c, g, cand, X, u) {
      if (u < SC.sweep[0] || u >= SC.show[1]) return;
      const mouth = toW(frameOf(X.D, X.k, X.A), MOUTH), F = c01((u - .65) / .85);
      if (u < SC.show[0] + .25) {
        const fade = u < SC.show[0] ? 1 : 1 - c01((u - SC.show[0]) / .25);
        TR.forEach((p, m) => { const f = m / TR.length; if (F <= f) return; cand.mote(c, g, p, 1.5, (.12 + .8 * Math.exp(-(F - f) * 8)) * fade, m, 1, X); });
      }
      for (let j = 0; j < 18; j++) {
        const d0 = .35 + j * .015; if (u < d0) continue;
        let p;
        if (u < d0 + .28) p = cand.fly(mouth, tipOL(0), sm((u - d0) / .28), j, X, 'in');
        else if (u < SC.show[0]) p = tipOL(F - j * .02);
        else { const q = c01((u - SC.show[0]) / .35); if (q >= 1) continue; p = cand.fly(tipOL(1 - j * .02), X.D, sm(q), j, X, 'out'); }
        cand.mote(c, g, p, 1.6, j % 3 ? .5 : .85, j + 300, 1, X);
      }
    }
    // the gauge's homes: a row of dots, or a bar of ember columns in a cold outline
    const GDOTS = Array.from({ length: NDOT }, (_, j) => ({ p: [READ_AT[0] + (j - (NDOT - 1) / 2) * 7, READ_AT[1]], lit: j, s: 3.2 }));
    const GBAR = (() => {
      const out = [], x0 = READ_AT[0] - BAR.w / 2, y0 = READ_AT[1] - BAR.h / 2 - 2, w = BAR.w + 4, h = BAR.h + 4, per = 2 * (w + h), n = Math.round(per / 3);
      for (let j = 0; j < n; j++) { let d = j / n * per, p; if (d < w) p = [x0 - 2 + d, y0]; else if ((d -= w) < h) p = [x0 + w - 2, y0 + d]; else if ((d -= h) < w) p = [x0 + w - 2 - d, y0 + h]; else { d -= w; p = [x0 - 2, y0 + h - d]; } out.push({ p, lit: -1, s: 1.4 }); }
      for (let col = 0; col < BAR.w / BAR.col; col++) for (const dy of [-1.6, 1.6]) out.push({ p: [x0 + (col + .5) * BAR.col, READ_AT[1] + dy], lit: col, s: 2.4 });
      return out;
    })();
    // how lit each dot or column is for a value: what is drawn, and so what the gauge shows
    function levels(val, form) {
      const lv = [];
      if (form === 'dots') for (let j = 0; j < NDOT; j++) lv.push(c01(val / STEP - j));
      else { const cols = BAR.w / BAR.col, fw = BAR.w * val / MAX; for (let j = 0; j < cols; j++) lv.push(c01(fw / BAR.col - j)); }
      return lv;
    }
    const shownOf = (lv, form) => lv.reduce((a, b) => a + b, 0) * (form === 'dots' ? STEP : BAR.col / BAR.w * MAX);
    function drawGauge(c, g, cand, X, u) {
      const form = OPT.gauge;
      const val = u < SC.hit ? VAL[0] : VAL[1];
      const items = form === 'dots' ? GDOTS : GBAR, lv = levels(val, form), was = levels(VAL[0], form), hk = u < SC.hit ? 1 : c01((u - SC.hit) / .8);
      const ph = u < SC.show[1] ? 'in' : u < SC.gone[0] ? 'hold' : 'out', e = ph === 'in' ? (u - SC.show[0]) / (SC.show[1] - SC.show[0]) : ph === 'out' ? (u - SC.gone[0]) / (SC.gone[1] - SC.gone[0]) : 0;
      const shake = hk < 1 ? Math.sin(hk * 60) * 1.4 * (1 - hk) : 0;
      items.forEach((it, j) => {
        const L = it.lit >= 0 ? lv[it.lit] : 0, L0 = it.lit >= 0 ? was[it.lit] : 0, home = [it.p[0] + shake, it.p[1]];
        let p = home, f = 1, a = 1;
        if (ph === 'in') { const q = sm(c01((e - hsh(j + 5) * .4) / .6)); p = q < 1 ? cand.fly(TR[j % TR.length], home, q, j, X, 'in') : home; f = q; }
        else if (ph === 'out') { const q = sm(c01((e - hsh(j + 2) * .35) / .65)); if (q > 0) { p = cand.fly(home, X.D, q, j, X, 'out'); f = 1 - q; a = 1 - q; } }
        const fl = .82 + .18 * Math.sin(X.t * 9 + j * 2.3);
        let heat = L > 0 ? (.3 + .62 * L) * fl : .04;
        if (hk < 1 && L > 0) heat += .6 * (1 - hk);
        cand.mote(c, g, p, it.s, heat * a, j + 500, f, X);
        // the strength lost to the hit bursts off as embers
        if (hk < 1 && L0 - L > .01 && ph === 'hold') {
          const b = hsh(j + 11) * Math.PI - Math.PI, v = 14 + 16 * hsh(j + 12), q = [home[0] + Math.cos(b) * v * hk, home[1] + Math.sin(b) * v * hk + 34 * hk * hk];
          cand.mote(c, g, q, it.s * (1 - .5 * hk), (L0 - L) * (1.2 - hk), j + 700, 0, X);
        }
      });
      return { form, shown: shownOf(lv, form), O: READ_AT, ph };
    }

    // ---------- a window: its frame of ember squares, the screen inside ----------
    const WR = { x: 470, y: 62, w: 300, h: 172 };
    const FR = (() => {
      const sq = [];
      for (const ins of [0, 3.4]) {
        const x0 = WR.x + ins, y0 = WR.y + ins, w = WR.w - 2 * ins, h = WR.h - 2 * ins, per = 2 * (w + h), n = Math.round(per / 3.4);
        for (let j = 0; j < n; j++) { let d = j / n * per, p; if (d < w) p = [x0 + d, y0]; else if ((d -= w) < h) p = [x0 + w, y0 + d]; else if ((d -= h) < w) p = [x0 + w - d, y0 + h]; else { d -= w; p = [x0, y0 + h - d]; } sq.push({ p, per: j / n, row: ins ? 1 : 0 }); }
      }
      return sq;
    })();
    function screenIn(c, a) {
      if (a <= .003) return;
      const x = WR.x + 8, y = WR.y + 8, w = WR.w - 16, h = WR.h - 16;
      c.save(); c.globalAlpha = a;
      c.fillStyle = 'rgba(6,12,20,.92)'; c.fillRect(x, y, w, h);
      c.font = '600 11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.fillStyle = css(pale(COOL, .4), .9); c.fillText('СУМКА', x + 8, y + 15);
      c.fillStyle = css(COOL, .5); c.fillText('9 / 35', x + w - 44, y + 15);
      for (let r = 0; r < 3; r++) for (let q = 0; q < 8; q++) {
        const cx = x + 8 + q * 34, cy = y + 24 + r * 34, full = hsh(r * 8 + q + 70) < .4;
        rr(c, cx + .5, cy + .5, 29, 29, 2); c.fillStyle = 'rgba(20,32,50,.78)'; c.fill(); c.strokeStyle = css(COOL, .36); c.lineWidth = 1; c.stroke();
        if (full) { rr(c, cx + 8, cy + 8, 14, 14, 3); c.fillStyle = '#5e636c'; c.fill(); c.fillStyle = '#80868f'; c.fillRect(cx + 10, cy + 10, 6, 3); }
      }
      c.fillStyle = css(COOL, .45); c.fillText('Tab — закрыть', x + 8, y + h - 7);
      c.restore();
    }
    function drawWin(c, g, cand, X, u) {
      const n = FR.length, span = WN.fly - WN.one, back = WN.back[1] - WN.back[0] - WN.one, opening = u < WN.back[0];
      const L = cand._lag || (cand._lag = FR.map((sq, j) => c01(cand.winLag(sq, j))));
      let landed = 0, flying = 0;
      const inside = opening ? c01((u - WN.inside[0]) / (WN.inside[1] - WN.inside[0])) * (1 - c01((u - WN.close) / .15)) : 0;
      screenIn(c, inside);
      for (let j = 0; j < n; j++) {
        const sq = FR[j], q = opening ? c01((u - L[j] * span) / WN.one) : 1 - c01((u - WN.back[0] - (1 - L[j]) * back) / WN.one);
        if (q <= 0) continue;
        if (q >= 1) { landed++; cand.frameSq(c, g, sq, j, X); continue; }
        flying++;
        const p = opening ? cand.fly(X.D, sq.p, q, j, X, 'in') : cand.fly(sq.p, X.D, 1 - q, j, X, 'out');
        cand.mote(c, g, p, 1.7, .75, j, opening ? q * .8 : .5, X);
      }
      return { landed, flying, total: n, inside };
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
    // the loupe: the drone and what its body forms, ×2.5
    const LP = { x: 786, y: 12, w: 200, h: 218 };
    function loupe(T) {
      if (!OPT.loupe || !T.st) return false;
      const c = T.c, h = T.me, k = LOUPE_K;
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.fillStyle = '#05070b'; c.fill(); c.strokeStyle = 'rgba(140,150,165,.35)'; c.lineWidth = 1; c.stroke();
      c.save(); rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip();
      const D = [LP.x + LP.w / 2, LP.y + 160 + Math.sin(T.time * 1.7 + h.seed) * 1.5 * k];
      const g = lightStart(T);
      drawBody(c, g, T.cand, mkX(c, g, D, h.d.ang, k, T.time + h.seed, T.st), T.st);
      lightEnd(T);
      c.restore();
      c.font = '11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(200,205,214,.55)';
      c.fillText('лупа ×2,5', LP.x + 8, LP.y + LP.h - 8);
      return true;
    }
    const CUR_REST = [560, 190], CUR_AT = [WRECK.C[0] + 14, WRECK.C[1] - 6];
    function cursorOf(st) {
      if (st.id !== 'scan' || st.u >= SC.gone[1]) return null;
      const u = st.u;
      if (u < SC.arrive) return { p: lerp2(CUR_REST, CUR_AT, sm(u / SC.arrive)), a: c01(u / .12) };
      if (u < SC.gone[0]) return { p: CUR_AT, a: 1 };
      return { p: lerp2(CUR_AT, CUR_REST, sm((u - SC.gone[0]) / .4)), a: 1 - c01((u - SC.gone[0] - .2) / .2) };
    }
    const WIN_C = [WR.x + WR.w / 2, WR.y + WR.h / 2];

    return {
      cands, expose: { LOOP, SC, READ_AT, BOX: WRECK.box, WN },
      init(T) { T.me.face = -1; T.t0 = 0; T.showWas = OPT.show; T.st = null; },
      onScale(T) { const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S); T.hb = cv; T.hbg = cv.getContext('2d'); },
      // the hero stands right of the wreck, facing it; the loop says what the drone does; the head follows the scan and the window
      step(T, dt) {
        const me = T.me; me.x = HERO_X;
        if (T.showWas !== OPT.show) { T.showWas = OPT.show; T.t0 = T.time; }
        const st = T.st = stateOf(T);
        if (!T.real) {
          const look = st.id === 'scan' && st.u < SC.gone[1] ? (st.u >= SC.show[0] ? READ_AT : WRECK.C) : st.id === 'window' && st.u < WN.back[1] ? WIN_C : null, nk = toS(me, NECK());
          turnHead(me, look ? headTurn((look[0] - nk[0]) * me.face, look[1] - nk[1]) : 0, dt);
        }
      },
      aim(T, h) {
        const st = T.st; if (!st) return null;
        if (st.body && THOUGHTS.includes(st.body.id)) return [h.d.p[0] + OFF[0], h.d.p[1] + OFF[1]];
        if (st.id === 'scan' && st.u < SC.gone[1]) return st.u >= SC.show[0] ? READ_AT : WRECK.C;
        if (st.id === 'window' && st.u < WN.back[1]) return WIN_C;
        if (st.id === 'gesture') return [h.d.p[0], h.d.p[1] - 50];
        return null;
      },
      ownDrone: () => true,
      back(T) {
        drawBack(T); drawWreck(T.c);
        const st = T.st, h = T.me; if (!st) return;
        if (st.id === 'scan') sparks(T.c, st.u);
        if (st.id === 'meteor') meteorStreak(T.c, st.u);
        lampOn(T.c, h.d.p[0], h.d.p[1], h.x, h.y, h.face, .6 + (st.body && st.body.id === 'lamp' ? .9 * formOf(st.body) : 0), 2.6);
      },
      overDrone(T, h) {
        const st = T.st, cand = T.cand, c = T.c;
        const g = lightStart(T), X = mkX(c, g, h.d.p.slice(), h.d.ang, 1, T.time + h.seed, st);
        const dm = T.dm = { id: st.id, u: st.u, th: null, rd: null, win: null, alarm: st.alarm, loupe: false };
        if (st.id === 'window') dm.win = drawWin(c, g, cand, X, st.u);
        const sh = drawBody(c, g, cand, X, st);
        if (sh && sh.thought) dm.th = { ph: st.body.ph, O: sh.F.O, D: X.D };
        if (st.id === 'scan') { drawSweep(c, g, cand, X, st.u); if (st.u >= SC.show[0] && st.u < SC.gone[1]) dm.rd = drawGauge(c, g, cand, X, st.u); }
        lightEnd(T);
      },
      screen(T) {
        const cur = T.st && cursorOf(T.st);
        if (cur && cur.a > .003) { T.c.globalAlpha = cur.a; drawCursor(T.c, cur.p[0], cur.p[1]); T.c.globalAlpha = 1; }
        if (T.dm) T.dm.loupe = loupe(T);
      },
      hud(T) {
        const st = T.st; if (!st) return '';
        if (THOUGHTS.includes(st.id)) return st.body ? `мысль: ${SAY[st.id]}` : 'мысль осыпалась';
        if (st.id === 'scan') {
          if (st.u < SC.sweep[0]) return 'скан: указатель идёт на обломок, угли складывают трубу';
          if (st.u < SC.show[0]) return 'скан: угли бегут по краю обломка';
          if (st.u < SC.gone[1]) return `скан: прочность ${st.u < SC.hit ? VAL[0] : VAL[1]} из ${MAX} (на экране без цифр)${st.u >= SC.hit && st.u < SC.hit + .8 ? ' · удар' : ''}`;
          return 'скан: угли вернулись';
        }
        if (st.id === 'window') return st.u < WN.fly ? 'окно: угли летят из ядра к рамке' : st.u < WN.close ? 'окно открыто: рамка тлеет, внутри экран' : st.u < WN.back[1] ? 'окно закрывается: угли возвращаются в ядро' : 'окно закрыто';
        return SAY[st.id] || '';
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    hold: [['const SLOT = 2.5, HOLD = 1.5;', 'const SLOT = 2.5, HOLD = .7;']],
    far: [['OFF = [0, -44]', 'OFF = [-130, -44]']],
    value: [['const val = u < SC.hit ? VAL[0] : VAL[1];', 'const val = VAL[0];']],
    dots: [['lv.push(c01(val / STEP - j))', 'lv.push(c01(Math.round(val / STEP) - j))']],
    form: [['const form = OPT.gauge;', "const form = 'dots';"]],
    frame: [['inside: [.8, 1]', 'inside: [.3, .5]']],
    skip: [["'meteor', 'window', 'lightning', 'gesture'", "'meteor', 'window', 'gesture'"]],
    alarm: [["if (st.id !== 'meteor' || !st.body) return 0;", 'if (!st.body) return 0;']],
  },

  // one whole loop with the dots; then only thoughts, only the scan with the bar, only the window; the loupe off, slow, a pause,
  // a real pointer; a pick saved with the bar, then the dots
  check(K) {
    const X = K.X, ORD = ['idle', 'cold', 'lamp', 'scan', 'meteor', 'window', 'lightning', 'gesture', 'curious'], SIGNS = ['cold', 'meteor', 'lightning', 'curious'], HOLD_S = 1.5;
    const L = Math.round(X.LOOP * 60), A = L + 30, B = A + 620, C = B + 380, D = C + 270, E = D + 470;
    K.at(A, () => K.seg('showMode', 1));
    K.at(B, () => { K.seg('showMode', 2); K.seg('gaugeMode', 1); });
    K.at(C, () => K.seg('showMode', 3));
    K.at(D, () => { K.seg('showMode', 0); K.seg('loupeMode', 1); K.seg('speedMode', 1); });
    K.at(D + 200, () => { K.seg('speedMode', 0); K.seg('playMode', 1); });
    K.at(D + 260, () => { K.seg('playMode', 0); K.seg('loupeMode', 0); K.ptrAll([300, 120]); });
    K.at(D + 330, () => K.ptrAll(null));
    let st1 = '', st2 = '', doc1 = null, doc2 = null, fb = '';
    K.at(E - 40, () => K.click(2, '.pick'));
    K.at(E - 30, () => { st1 = K.byId.status.textContent; doc1 = K.picked(); K.byId.copy.listeners.click[0](); fb = K.byId.fallback.value; });
    K.at(E - 20, () => K.seg('gaugeMode', 0));
    K.at(E - 10, () => { st2 = K.byId.status.textContent; doc2 = K.picked(); });
    const per = K.tiles.map(() => ({ seq: [], hold: {}, vals: { dots: new Set(), bar: new Set() }, near: 0, th: 0, sc: 0, wn: 0, lOn: 0, lOff: 0, paused: 0, lastT: -1, al: 0, flew: 0, full: 0, closed: 0, wasFull: false }));
    const topMid = (X.BOX[0] + X.BOX[2]) / 2;
    K.run(E, (t, i) => {
      const q = per[K.tiles.indexOf(t)], d = t.dm; if (!d) return;
      const id = t.cand.id;
      if (i < L - 1) {
        if (q.seq[q.seq.length - 1] !== d.id) q.seq.push(d.id);
        if (d.th && d.th.ph === 'hold') { const h = q.hold[d.id] || (q.hold[d.id] = [t.time, t.time]); h[1] = t.time; }
      }
      if (d.th && d.th.ph === 'hold') {
        const Dp = d.th.D, O = d.th.O, far = Math.hypot(O[0] - Dp[0], O[1] - Dp[1]);
        if (far > 60 || O[1] > Dp[1] - 20) K.fail(`${id}: ${d.id} at ${O.map(Math.round)}, the drone at ${Dp.map(Math.round)}, frame ${i}`); else q.near++;
      }
      if (d.rd) {
        if (d.rd.form !== K.OPT.gauge) K.fail(`${id}: the gauge drawn as ${d.rd.form} with ${K.OPT.gauge} on, frame ${i}`);
        if (d.rd.ph === 'hold') {
          const want = d.u < X.SC.hit ? 340 : 230;
          if (Math.abs(d.rd.shown - want) > .01) K.fail(`${id}: the ${d.rd.form} show ${d.rd.shown.toFixed(1)} at ${d.u.toFixed(2)} s, want ${want}`); else q.vals[d.rd.form].add(want);
          if (Math.abs(d.rd.O[0] - topMid) > .01 || d.rd.O[1] >= X.BOX[1]) K.fail(`${id}: the gauge at ${d.rd.O} is not over the wreck's top`);
        }
      }
      if (d.win) {
        const w = d.win;
        if (w.inside > 0 && w.landed < w.total) K.fail(`${id}: the window's inside shows with ${w.landed} of ${w.total} frame embers landed, frame ${i}`);
        if (!q.wasFull && w.flying > 0 && w.landed < w.total) q.flew++;
        if (w.landed === w.total && w.inside >= 1) { q.full++; q.wasFull = true; }
        if (q.wasFull && w.landed === 0 && w.flying === 0) { q.closed++; q.wasFull = false; }
      }
      if (d.alarm > 0) { if (d.id !== 'meteor') K.fail(`${id}: the embers red at ${d.id}, frame ${i}`); else q.al++; }
      if (i > A + 1 && i < B) { if (d.rd || d.win || d.id === 'scan' || d.id === 'window') K.fail(`${id}: not a thought with only thoughts on, frame ${i}`); else if (d.th) q.th++; }
      if (i > B + 1 && i < C) { if (d.th || d.win) K.fail(`${id}: not the scan with only the scan on, frame ${i}`); else if (d.rd) q.sc++; }
      if (i > C + 1 && i < D) { if (d.th || d.rd) K.fail(`${id}: not the window with only the window on, frame ${i}`); else if (d.win) q.wn++; }
      if (i < D) { if (d.loupe) q.lOn++; } else if (i > D + 1 && i < D + 260) { if (d.loupe) K.fail(`${id}: the loupe drawn while off, frame ${i}`); else q.lOff++; }
      if (i > D + 201 && i < D + 259) { if (q.lastT >= 0 && t.time !== q.lastT) K.fail(`${id}: time ran while paused`); q.paused++; }
      q.lastT = t.time;
    });
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id, holds = SIGNS.map(s => q.hold[s] ? q.hold[s][1] - q.hold[s][0] + 1 / 60 : 0);
      console.log(`${id}: order ${q.seq.join(' ')} | held ${holds.map(v => v.toFixed(3)).join(' ')} s | dots ${[...q.vals.dots].join(' ')} bar ${[...q.vals.bar].join(' ')} | window flew ${q.flew} full ${q.full} closed ${q.closed} | red ${q.al} | near ${q.near} only: thoughts ${q.th} scan ${q.sc} window ${q.wn} | loupe ${q.lOn}/${q.lOff} paused ${q.paused}`);
      need(q.seq.slice(0, ORD.length).join() === ORD.join(), `${id}: every state in order`);
      need(holds.every(v => Math.abs(v - HOLD_S) <= 1.01 / 60), `${id}: every thought held ${HOLD_S} s`);
      need(q.vals.dots.has(340) && q.vals.dots.has(230) && q.vals.bar.has(340) && q.vals.bar.has(230), `${id}: the gauge before and after the hit, dots and bar`);
      need(q.flew > 10 && q.full > 60 && q.closed >= 2, `${id}: the window's embers fly, its frame fills, it closes`);
      need(q.al > 30, `${id}: the embers red at the meteor`);
      need(q.near > 300 && q.th > 300 && q.sc > 100 && q.wn > 100, `${id}: thoughts by the drone; each mode alone`);
      need(q.lOn > 100 && q.lOff > 30 && q.paused > 30, `${id}: the loupe on and off; the pause held`);
    });
    const cand = K.tiles[2].cand;
    console.log('after the pick:', st1, '| doc', JSON.stringify(doc1), '| copy:', fb, '| after the dots:', st2, '| doc', JSON.stringify(doc2));
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    need(doc1 && doc1.pick === cand.id && doc1.gauge === 'bar', 'the pick and its gauge (bar) are in the db doc');
    need(doc2 && doc2.pick === cand.id && doc2.gauge === 'dots', 'flipping the gauge saves it with the pick');
    need(st1.includes(`«${cand.name}»`) && st1.includes('шкала: полоса') && st2.includes('шкала: точки'), 'the status names the pick and its gauge');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name) && fb.includes('полоса'), 'the copy text names the pick and its gauge');
    return ok;
  },
};
