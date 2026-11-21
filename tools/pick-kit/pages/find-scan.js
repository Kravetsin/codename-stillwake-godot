// A new find, told by the light drone: when the hero first holds an item it never found (Knowledge.Find), the drone scans the
// item's picture and makes a record of it that goes into the research key on the tablet's edge. Four ways, each with one find
// and a batch of three, in the drone's «Нити» language: an art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/find-scan.js <out-dir> [--break <name>]
module.exports = {
  title: 'Скан находки',
  heading: 'Новая находка: дрон сканирует предмет и пишет запись для доски',
  // window: true only so build.js embeds the bag's item pictures; the page draws its whole tile (scene): no bag, schedule or Tab key
  window: true,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Когда герой впервые берёт в руки предмет, которого ещё не находил, об этом говорит дрон: он <b>сканирует картинку предмета</b> и пишет из неё <b>запись</b>, а запись уходит в кнопку <b>«Изучения»</b> на нижнем крае — её число растёт. Это заменит нынешнюю янтарную вспышку по краю. Всё на языке «Нитей»: искры на нитях, тлеющие квадратики, рамка-уголки, значки одной нитью. Без слов, только свет и движение; игра не останавливается.</p>
<p class="lead">Четыре способа. Каждый по кругу показывает <b>одну находку</b> (кристалл), потом <b>пачку из трёх</b> (медь, батарея, схема: находки часто приходят разом, например из ящика). Картинки — настоящие иконки предметов игры (<code>game/art/hd/items</code>, их рисует <code>tools/anime-art/subjects/items.js</code>). Масштаб игры: клетка 8 px мира, вид ×4, герой ~42 px мира, предмет в руке 6 px мира, кнопки края 28 px. В лупе слева вверху то же ×1,8. «Показ» ниже оставляет только одну находку или только пачку.</p>
<p class="miss">Не показано: звук; что видят друзья (вспышка только у нашедшего); отрисовка Godot (свет выйдет чуть иначе); герой на бегу (здесь он стоит, в игре всё идёт за рукой и дроном); сам планшет (здесь полоса-заглушка с шестью кнопками); находки при открытом планшете; подсказка под указателем.</p>`,
  toggles: [
    { id: 'showMode', bar: 1, label: 'Показ:', key: 'show', opts: [['по кругу', 'all'], ['одна находка', 'single'], ['пачка из трёх', 'batch']] },
    { id: 'loupeMode', label: 'Лупа:', key: 'loupe', opts: [['есть', true], ['нет', false]] },
  ],
  pick: { doc: 'picks/findScan', subject: 'Скан находки', what: ['версию', 'версией'] },

  page() {
    const TAU = Math.PI * 2;
    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const hot = k => pale(EMBER, k), deep = k => [EMBER[0] * k, EMBER[1] * k, EMBER[2] * k];
    const unit = (a, b) => { const dx = b[0] - a[0], dy = b[1] - a[1], d = Math.hypot(dx, dy) || 1; return [dx / d, dy / d]; };
    const ein = e => e * e;   // gathering speed toward the key, as today's flash does, so the eye is pulled along
    const LIT = (c, fn) => { c.save(); c.globalCompositeOperation = 'lighter'; fn(); c.restore(); };

    // ---------- what the game has: the edge's keys (Main.cs's order), 28 px, the research key's flare ----------
    const KEYS = ['air', 'build', 'research', 'market', 'suit', 'journal'];
    const KEY = { side: 28, icon: 9, gap: 34 }, LEVEL = { rest: .34, wait: .85 }, FLARE = .45;
    const EDGE_Y = 494, KEY_Y = 500, KX0 = LW / 2 - (KEYS.length * KEY.gap - (KEY.gap - KEY.side)) / 2;
    const keyBox = id => [KX0 + KEYS.indexOf(id) * KEY.gap, KEY_Y];
    const keyMid = () => { const b = keyBox('research'); return [b[0] + KEY.side / 2, b[1] + KEY.side / 2]; };
    const REC_TO = () => keyMid();   // where every record ends

    // ---------- the finds: one, then a batch of three; the hand comes up, the items land in it ----------
    const SLOTS = { single: ['crystal'], batch: ['copper', 'battery', 'circuit'] };
    const NAMES = { crystal: 'кристалл', copper: 'медь', battery: 'батарея', circuit: 'схема' };
    const UP = .25, APPEAR = .2, APART = .12, DOWN = .3, REST = 1.1;
    const HX = 720, FACE = -1, HAND = [9.5, -21], ICON = 24;   // the hero; the hand, world px from the feet; a held item, page px
    const LP = { x: 16, y: 16, w: 330, h: 230, k: 1.8 };

    // ---------- paths: polylines walked by a share of their length (thread-icons.js) ----------
    function mkPath(polys, br) {
      const seg = []; let L = 0;
      polys.forEach((p, pi) => { for (let i = 1; i < p.length; i++) { const a = p[i - 1], b = p[i], d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 1e-6) continue; seg.push([a[0], a[1], b[0], b[1], d, L, br ? br[pi][i] : 0]); L += d; } });
      return { seg, L, polys };
    }
    function onPath(P, s) {
      const d = (((s % 1) + 1) % 1) * P.L; let lo = 0, hi = P.seg.length - 1;
      while (lo < hi) { const m = (lo + hi + 1) >> 1; if (P.seg[m][5] <= d) lo = m; else hi = m - 1; }
      const q = P.seg[lo], e = c01((d - q[5]) / q[4]);
      return [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e, (q[2] - q[0]) / q[4], (q[3] - q[1]) / q[4]];
    }
    function joinOne(polys) {
      const left = polys.slice(1).map(p => p.slice()), cur = polys[0].slice(), fl = cur.map(() => 0);
      while (left.length) {
        const e = cur[cur.length - 1]; let best = 0, rev = false, bd = 1e9;
        left.forEach((p, i) => { const d0 = Math.hypot(e[0] - p[0][0], e[1] - p[0][1]), d1 = Math.hypot(e[0] - p[p.length - 1][0], e[1] - p[p.length - 1][1]); if (d0 < bd) { bd = d0; best = i; rev = false; } if (d1 < bd) { bd = d1; best = i; rev = true; } });
        let p = left.splice(best, 1)[0]; if (rev) p = p.slice().reverse();
        p.forEach((q, j) => { cur.push(q); fl.push(j === 0 ? 1 : 0); });
      }
      return mkPath([cur], [fl]);
    }
    const mkIcon = polys => ({ P: mkPath(polys), one: joinOne(polys) });
    const arc = (cx, cy, r, n = 28, a0 = 0, a1 = TAU) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]); } return p; };
    function roundSq(rc) {
      const p = [];
      for (const [cx, cy, a0] of [[1 - rc, -1 + rc, -Math.PI / 2], [1 - rc, 1 - rc, 0], [-1 + rc, 1 - rc, Math.PI / 2], [-1 + rc, -1 + rc, Math.PI]])
        for (let i = 0; i <= 4; i++) { const a = a0 + i / 4 * Math.PI / 2; p.push([cx + Math.cos(a) * rc, cy + Math.sin(a) * rc]); }
      p.push(p[0].slice()); return p;
    }
    // the edge keys' motifs, as tools/anime-art/subjects/edgekeys.js draws them in a 16-unit square: outlines become threads
    function svg(d, n = 8) {
      const tk = d.match(/[MLCQZ]|-?\d*\.?\d+/g), out = []; let cur = null, p = [0, 0], st = [0, 0], i = 0, cmd = '';
      const num = () => +tk[i++];
      while (i < tk.length) {
        if (/[MLCQZ]/.test(tk[i])) cmd = tk[i++];
        if (cmd === 'M') { p = [num(), num()]; st = p; cur = [p]; out.push(cur); cmd = 'L'; }
        else if (cmd === 'L') { p = [num(), num()]; cur.push(p); }
        else if (cmd === 'Q') { const a = [num(), num()], b = [num(), num()]; for (let k = 1; k <= n; k++) cur.push(bez(p, a, b, k / n)); p = b; }
        else if (cmd === 'C') {
          const a = [num(), num()], b = [num(), num()], e = [num(), num()];
          for (let k = 1; k <= n; k++) { const s = k / n, m = 1 - s; cur.push([m * m * m * p[0] + 3 * m * m * s * a[0] + 3 * m * s * s * b[0] + s * s * s * e[0], m * m * m * p[1] + 3 * m * m * s * a[1] + 3 * m * s * s * b[1] + s * s * s * e[1]]); }
          p = e;
        } else if (cmd === 'Z') { cur.push(st.slice()); p = st; }
      }
      return out.map(q => q.map(([x, y]) => [(x - 8) / 8, (y - 8) / 8]));
    }
    const rrD = (x, y, w, h, r) => `M ${x + r} ${y} L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
    const motif = ds => ds.flatMap(d => svg(d));
    const ICONS = {
      air: mkIcon(motif(['M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 'M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 'M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2'])),
      build: mkIcon(motif([rrD(0.4, 1.8, 7.1, 3.5, .55), 'M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z',
        rrD(0.4, 6.2, 3.1, 3.5, .5), rrD(4.4, 6.25, 7.2, 3.5, .55), rrD(12.5, 6.2, 3.1, 3.5, .5), rrD(0.4, 10.6, 7.1, 3.5, .55),
        'M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z'])),
      research: mkIcon(motif(['M 5.3 10.8 C 2.4 9.3 1.8 5.4 3.8 3.0 C 5.8 0.6 10.2 0.6 12.2 3.0 C 14.2 5.4 13.6 9.3 10.7 10.8 L 10.4 12.2 L 5.6 12.2 Z', rrD(5.5, 12.7, 5, 1.6, .3),
        'M 6.7 14.8 L 9.3 14.8 L 8.7 15.8 L 7.3 15.8 Z', 'M 6.5 9.8 L 7 6.6 L 8 8.2 L 9 6.6 L 9.5 9.8'])),
      market: mkIcon(motif(['M 8 0.8 Q 8.5 2.6 9.7 3.7 Q 14.9 6.5 14.3 11.0 Q 13.5 15.2 8 15.4 Q 2.5 15.2 1.7 11.0 Q 1.1 6.5 6.3 3.7 Q 7.5 2.6 8 0.8 Z', 'M 8 4.6 Q 4.0 7.4 4.6 11.2 Q 5.2 14.0 8 14.4', 'M 8 4.6 Q 12.0 7.4 11.4 11.2 Q 10.8 14.0 8 14.4'])),
      suit: mkIcon([...motif(['M 5.5 1.7 Q 8 5.3 10.5 1.7 L 12.9 2.2 Q 15.5 2.9 15.7 5.6 L 15.6 9.3 Q 14.6 9.7 13.6 9.4 L 13.25 6.9 Q 12.3 7.5 12.4 9.2 L 11.95 12.4 Q 12.5 14.0 12.35 15.2 Q 8 15.9 3.65 15.2 Q 3.5 14.0 4.05 12.4 L 3.6 9.2 Q 3.7 7.5 2.75 6.9 L 2.4 9.4 Q 1.4 9.7 0.4 9.3 L 0.3 5.6 Q 0.5 2.9 3.1 2.2 Z']), arc(0, -.0375, 1.95 / 8, 14)]),
      journal: mkIcon(motif(['M 7.75 4.6 Q 4.8 2.7 0.6 3.3 L 0.4 13.4 Q 4.6 12.7 7.75 14.7 Z', 'M 8.25 4.6 Q 11.2 2.7 15.4 3.3 L 15.6 13.4 Q 11.4 12.7 8.25 14.7 Z',
        'M 2.6 6.4 Q 4.6 6.0 6.6 6.8', 'M 2.6 9.0 Q 4.6 8.6 6.6 9.4', 'M 9.4 6.8 Q 11.4 6.0 13.4 6.4', 'M 9.4 9.4 Q 11.4 8.6 13.4 9.0'])),
    };
    const RIM = mkIcon([roundSq(.24)]);

    // ---------- a thread icon, «Одна нить» (thread-icons.js): F = { x, y, R px per unit, u a thread's px } ----------
    const P2 = (F, q) => [F.x + q[0] * F.R, F.y + q[1] * F.R];
    function pathStroke(c, P, F, w, col, a, pick) {
      if (a <= .004) return 0;
      c.beginPath(); let last = null, any = false;
      for (const s of P.seg) {
        if (pick && !pick(s)) { last = null; continue; }
        const A = P2(F, s), B = P2(F, [s[2], s[3]]);
        if (!last || last[0] !== s[0] || last[1] !== s[1]) c.moveTo(A[0], A[1]);
        c.lineTo(B[0], B[1]); last = [s[2], s[3]]; any = true;
      }
      if (!any) return 0;
      c.lineWidth = w; c.strokeStyle = css(col, a); c.stroke(); return 1;
    }
    function runTail(c, P, F, s, len, w, h, n = 10) {
      for (let i = 0; i < n; i++) {
        const s0 = s - len * (1 - i / n), s1 = Math.min(.9999, s - len * (1 - (i + 1) / n));
        if (s0 < 0 || s1 <= s0) continue;
        const a = P2(F, onPath(P, s0)), b = P2(F, onPath(P, s1));
        c.beginPath(); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]); c.lineWidth = w; c.strokeStyle = css(hot(.3 + .4 * i / n), h * (i + 1) / n); c.stroke();
      }
    }
    // st: lv how bright (EdgeKey's level), wait (a slow spark and a pulse), flare (1 → 0 after a record lands), rim, t, sd
    function threadIcon(c, I, F, st) {
      const P = st.rim ? I.P : I.one, u = F.u, t = st.t, fl = st.flare || 0, lv = st.lv * (st.wait ? .7 + .3 * Math.sin(t * TAU * 1.2) : 1);
      LIT(c, () => {
        pathStroke(c, P, F, 3 * u, EMBER, (.06 + .1 * fl) * lv);
        const a = Math.min(1, .3 + .45 * lv + .4 * fl);
        pathStroke(c, P, F, u, hot(.12 + .4 * fl), a, s => !s[6]);
        pathStroke(c, P, F, .6 * u, hot(.12), a * .35, s => s[6]);
        const s = st.rim ? null : fl > 0 ? 1 - fl : st.wait ? ((t + st.sd * 3) / 2.4) % 1 : null;
        if (s == null) return;
        const h = Math.min(1, .5 + .5 * lv);
        runTail(c, P, F, s, .22, 1.3 * u, h);
        const q = onPath(P, Math.min(.9999, s)), p = P2(F, q);
        halo(c, p[0], p[1], 3.2 * u, hot(.4), .45 * h);
        spk(c, p[0], p[1], q[2], q[3], h, 1.3 * u, .01);
      });
    }

    // ---------- embers: a lit square, a spark with its tail, a thread (all inside LIT) ----------
    function gsq(c, x, y, s, h) { if (h <= .02) return; h = Math.min(1, h); c.fillStyle = css(pale(EMBER, .25 * h), .95 * h); c.fillRect(x - s / 2, y - s / 2, s, s); }
    function spk(c, x, y, dx, dy, h, s = .95, tail = 1.3) {
      if (h <= .02) return; h = Math.min(1, h);
      for (let m = 0; m < 3; m++) { const b = m * tail; c.fillStyle = css(pale(EMBER, .3 * (1 - m / 3)), (.95 - m * .3) * h); c.fillRect(x - dx * b - s / 2, y - dy * b - s / 2, s, s); }
    }
    function stroke(c, a, b, w, al) { if (al <= .003) return; c.beginPath(); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]); c.lineWidth = w; c.strokeStyle = css(EMBER, al); c.stroke(); }

    // ---------- an item's picture, read once: its solid cells (16 × 16) and its outline, from the game's own PNG ----------
    const GN = 16, GRID = {};
    function gridOf(id) {
      if (GRID[id]) return GRID[id];
      const im = IMG[id]; if (!ready(im)) return null;
      const cv = document.createElement('canvas'); cv.width = GN; cv.height = GN;
      const g = cv.getContext('2d'); g.drawImage(im, 0, 0, GN, GN);
      const d = g.getImageData(0, 0, GN, GN).data, cells = [], mid = (GN - 1) / 2;
      for (let y = 0; y < GN; y++) for (let x = 0; x < GN; x++) {
        const i = (y * GN + x) * 4; if (d[i + 3] < 90) continue;
        cells.push({ x: x - mid, y: y - mid, l: (.3 * d[i] + .55 * d[i + 1] + .15 * d[i + 2]) / 255, h: hsh(x * 31 + y * 7 + id.length) });
      }
      const solid = (x, y) => { const X = Math.round(x + mid), Y = Math.round(y + mid); return X >= 0 && Y >= 0 && X < GN && Y < GN && d[(Y * GN + X) * 4 + 3] >= 90; };
      const R = [];
      for (let j = 0; j < 40; j++) { const a = j / 40 * TAU; let r = 1; for (let s = 0; s < GN; s += .25) if (solid(Math.cos(a) * s, Math.sin(a) * s)) r = s; R.push(r + .6); }
      const pts = R.map((r, j) => { const rs = (R[(j + 39) % 40] + 2 * r + R[(j + 1) % 40]) / 4, a = j / 40 * TAU; return [Math.cos(a) * rs, Math.sin(a) * rs]; });
      pts.push(pts[0].slice());
      return (GRID[id] = { cells, out: mkPath([pts]) });
    }

    // ---------- the drone «Нити» (drone-embers.js, threads): a coal core, an ember eye, sparks on three orbits ----------
    const eyeOf = X => [X.D[0] + Math.cos(X.A) * 2.2 * DS * DSC, X.D[1] + Math.sin(X.A) * 2.2 * DS * DSC];
    function orbitPt(i, D, t) {
      const P = DR.P[i % DR.P.length], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + t * (.8 + .25 * o) * dir * 2.2, rx = (6.3 + o * 1.4 + (P.r1 - .5)) * 1.05, tl = -.3 + o * 1.05;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tl), v = rot(-Math.sin(th) * dir, Math.cos(th) * .3 * dir, tl), vl = Math.hypot(v[0], v[1]) || 1;
      return [D[0] + q[0], D[1] + q[1], v[0] / vl, v[1] / vl, Math.sin(th)];
    }
    function coal(c, X) {
      const [x, y] = X.D, s = DS * DSC, A = X.A, fl = Math.min(1, (.75 + .25 * Math.sin(X.t * 7.3)) * (1 + X.flare)), e = eyeOf(X);
      halo(c, x, y, 8 + 6 * X.flare, EMBER, .35 + .4 * X.flare);
      c.save(); c.translate(x, y); c.scale(s, s); paint(c, DR.core, A, DARK, .8, 2.6); ink(c, DR.core, A, 1.7); c.restore();
      LIT(c, () => {
        c.strokeStyle = css(EMBER, .5 * fl); c.lineWidth = .45; c.beginPath();
        for (const [a, b] of [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]]) { c.moveTo(x + Math.cos(A + a) * .6, y + Math.sin(A + a) * .6); c.lineTo(x + Math.cos(A + b * .5) * 2.6, y + Math.sin(A + b * .5) * 2.6); }
        c.stroke();
        c.fillStyle = css(pale(EMBER, .5), .9 * fl); c.beginPath(); c.arc(e[0], e[1], 1.2, 0, TAU); c.fill();
      });
      halo(c, e[0], e[1], 4, EMBER, .5 * fl);
    }
    function threadDrone(c, X) {
      const D = X.D, pts = [];
      for (let i = 0; i < 120; i++) pts.push(orbitPt(i, D, X.t));
      LIT(c, () => { c.lineWidth = .4; c.strokeStyle = css(EMBER, .12); for (let o = 0; o < 3; o++) { const rx = (6.3 + o * 1.4) * 1.05; c.beginPath(); c.ellipse(D[0], D[1], rx, rx * .3, -.3 + o * 1.05, 0, TAU); c.stroke(); } });
      const layer = front => {
        c.fillStyle = '#26272b';
        for (const p of pts) if ((p[4] >= 0) === front) c.fillRect(p[0] - .475, p[1] - .475, .95, .95);
        LIT(c, () => pts.forEach((p, i) => { if ((p[4] >= 0) === front) spk(c, p[0], p[1], p[2], p[3], hsh(i + 3) < .5 ? .55 + .3 * Math.sin(X.t * 11 + i * 7) : .2); }));
      };
      layer(false); coal(c, X); layer(true);
    }
    // the scanner's spyglass, page px in the aim's frame (drone-embers.js), its mouth where the beam leaves
    const rect = (u0, u1, h) => [[u0, -h], [u1, -h], [u1, h], [u0, h], [u0, -h]];
    const GLASS = mkPath([rect(3, 11, 3.6), rect(10, 19, 2.8), arc(21.5, 0, 1, 14).map(([x, y]) => [x * 1.3 - 21.5 * .3, y * 4.4]), arc(-2, 0, 5.5, 12, Math.PI / 2, 3 * Math.PI / 2)]);
    const inAim = (X, q) => { const r = rot(q[0], q[1], X.A); return [X.D[0] + r[0], X.D[1] + r[1]]; };
    const mouthOf = X => inAim(X, [21.5, 0]);
    function glass(c, X, form) {
      const cut = form * GLASS.L;
      c.beginPath();
      for (const sg of GLASS.seg) { if (sg[5] > cut) break; const a = inAim(X, sg), b = inAim(X, [sg[2], sg[3]]); c.moveTo(a[0], a[1]); c.lineTo(b[0], b[1]); }
      c.lineWidth = 1.6; c.strokeStyle = css(EMBER, .06); c.stroke(); c.lineWidth = .45; c.strokeStyle = css(EMBER, .32); c.stroke();
      for (let j = 0; j < 26; j++) { const s = (j / 26 + X.t * 30 / GLASS.L) % 1; if (s * GLASS.L > cut) continue; const q = onPath(GLASS, s), p = inAim(X, q), d = rot(q[2], q[3], X.A); spk(c, p[0], p[1], d[0], d[1], .85); }
    }

    // ---------- where things are: the hand, the held items (one, or a fan of three) ----------
    const FIST_UP = (() => { const h = newHero('me', HX, 0); h.face = FACE; h.rk = 1; h.rp = HAND; h.P = poseOf(h); return toS(h, h.P.armF[2]); })();
    const fist = X => toS(X.me, X.me.P.armF[2]);
    function heldAt(fp, n, k) { if (n === 1) return [fp[0] - 6, fp[1] - 10]; const o = [[-40, -10], [-14, -24], [12, -10]][k]; return [fp[0] + o[0], fp[1] + o[1]]; }
    const scanOf = (q, u) => c01((u - q.s0) / (q.s1 - q.s0));

    // ---------- the candidates ----------
    // M: their times; scans(ap, M): [start, end] of each find's scan; landOf: when its record reaches the key; draw: under the
    // drone; recAt / recDraw: the record, v seconds after it began; aim, look, droneAt, flareOf optional; focus: the loupe's middle
    const cands = [
      {
        id: 'holo', name: 'Голограмма', focus: [676, 300],
        M: { build: .3, scan: .45, fold: .15, fly: .4 },
        idea: 'Искры срываются с предмета в руке и складывают над ним его картинку — голограмму из тлеющих квадратиков. Нить скана из глаза дрона проходит её сверху вниз, и пройденное разгорается. Картинка схлопывается в одну искру — это запись — и та летит по прямой нити в кнопку «Изучения»; кнопка вспыхивает, число растёт. В пачке три голограммы встают в ряд, скан проходит их по очереди, и каждая искра улетает сразу после своего скана.',
        time: 'Голограмма собирается за 0,3 с · скан 0,45 с · схлопывается в искру за 0,15 с · искра летит 0,4 с · кнопка вспыхивает 0,45 с · одна находка: запись в кнопке через 1,35 с после того, как предмет в руке; пачка: через 1,35, 1,8 и 2,25 с.',
        scans(ap, M) { let prev = 0; return ap.map(a => { const s0 = Math.max(a + .05 + M.build, prev); prev = s0 + M.scan; return [s0, prev]; }); },
        landOf(q, k, it, M) { return q.r0 + M.fold + M.fly; },
        at(X, k) { return X.n === 1 ? [640, 262] : [[596, 266], [638, 258], [680, 266]][k]; },
        aim(X) {
          const it = X.S.it, k = it.findIndex(q => X.u >= q.ap && X.u < q.s1); if (k < 0) return null;
          const C = this.at(X, k); return [C[0], C[1] + (scanOf(it[k], X.u) * 16 - 8) * 2.3];
        },
        look(X) { const k = X.S.it.findIndex(q => X.u >= q.ap && X.u < q.land); return k < 0 ? null : this.at(X, k); },
        draw(c, X) {
          const M = this.M, u = X.u, cs = 2.3;
          X.S.it.forEach((q, k) => {
            const G = gridOf(q.id); if (!G) return;
            const b = c01((u - q.ap - .05) / M.build), f = u >= q.r0 ? c01((u - q.r0) / M.fold) : 0;
            if (b <= 0 || f >= 1) return;
            const C = this.at(X, k), from = heldAt(fist(X), X.n, k), s = scanOf(q, u), ly = C[1] + (s * 16 - 8) * cs;
            LIT(c, () => {
              for (const cl of G.cells) {
                const home = [C[0] + cl.x * cs, C[1] + cl.y * cs], qb = sm(c01((b - cl.h * .45) / .55));
                if (qb <= 0) continue;
                let p = qb < 1 ? lerp2(from, home, qb) : home, h = .12 + .3 * cl.l + .05 * Math.sin(X.t * 9 + cl.h * 40);
                if (s > 0 && home[1] < ly) h += .22 + .6 * Math.exp((home[1] - ly) / 5);   // scanned: burnt in, hottest just behind the line
                if (qb < 1) h = .75;
                if (f > 0) { p = lerp2(home, C, sm(f)); h += .4 * f; }
                gsq(c, p[0], p[1], 1.7, h);
              }
              if (s > 0 && s < 1) {
                const a = [C[0] - 8.5 * cs, ly], e = [C[0] + 8.5 * cs, ly];
                stroke(c, X.eye, a, .35, .12); stroke(c, X.eye, e, .35, .12);
                stroke(c, a, e, 2.4, .1); stroke(c, a, e, .6, .85);
                spk(c, a[0], a[1], 0, 0, 1); spk(c, e[0], e[1], 0, 0, 1);
              }
            });
          });
        },
        recAt(X, q, k, v) { const C = this.at(X, k); return v < this.M.fold ? C : lerp2(C, REC_TO(), ein(c01((v - this.M.fold) / this.M.fly))); },
        recDraw(c, X, q, k, v, p) {
          const M = this.M, C = this.at(X, k);
          if (v < M.fold) { const e = v / M.fold; halo(c, C[0], C[1], 5 + 6 * e, EMBER, .3 + .4 * e); LIT(c, () => gsq(c, C[0], C[1], 1.4 + 1.2 * e, 1)); return; }
          const e = c01((v - M.fold) / M.fly), d = unit(C, REC_TO());
          LIT(c, () => { stroke(c, C, p, .35, .16 * (1 - .6 * e)); spk(c, p[0], p[1], d[0], d[1], 1, 2.2); });
          halo(c, p[0], p[1], 7, EMBER, .5);
        },
      },
      {
        id: 'outline', name: 'Контур', focus: [690, 350],
        M: { hop: .12, scan: .4, shrink: .15, drop: .2, slide: 520, sink: .12, R: [21, 17] },
        idea: 'Без копии: дрон срывается от головы и облетает сам предмет в руке, а его нить обводит контур предмета — по готовой части бегут искры. Обвод замкнулся — контур сжимается в маленькую карточку из нити (это запись), она падает на край планшета и скользит по нему в кнопку «Изучения», как сейчас бежит вспышка. В пачке дрон облетает предметы по одному, а карточки едут по краю друг за другом.',
        time: 'Перелёт дрона 0,12 с · обвод 0,4 с · сжатие в карточку 0,15 с · падение на край 0,2 с · по краю 0,3–0,4 с (520 px/с) · в кнопку 0,12 с · одна находка: запись в кнопке через ≈1,4 с после того, как предмет в руке; пачка: через ≈1,6, 2,15 и 2,75 с.',
        scans(ap, M) { let prev = ap[ap.length - 1] + .05; return ap.map(() => { const s0 = prev + M.hop; prev = s0 + M.scan; return [s0, prev]; }); },
        landOf(q, k, it, M) { const C = heldAt(FIST_UP, it.length, k); return q.r0 + M.shrink + M.drop + Math.abs(C[0] - REC_TO()[0]) / M.slide + M.sink; },
        orbitAt(X, k, a) { const C = heldAt(fist(X), X.n, k), R = this.M.R; return [C[0] + Math.cos(a) * R[0], C[1] + Math.sin(a) * R[1]]; },
        droneAt(X) {
          const M = this.M, u = X.u, it = X.S.it, L = it[it.length - 1];
          if (u >= L.s1) return lerp2(this.orbitAt(X, it.length - 1, TAU), X.post, sm((u - L.s1) / M.hop));
          for (let k = it.length - 1; k >= 0; k--) {
            const q = it[k];
            if (u >= q.s0) return this.orbitAt(X, k, scanOf(q, u) * TAU);
            if (u >= q.s0 - M.hop) return lerp2(k ? this.orbitAt(X, k - 1, TAU) : X.post, this.orbitAt(X, k, 0), sm((u - q.s0 + M.hop) / M.hop));
          }
          return X.post;
        },
        aim(X) { const k = X.S.it.findIndex(q => X.u >= q.s0 - this.M.hop && X.u < q.s1); return k < 0 ? null : heldAt(fist(X), X.n, k); },
        draw(c, X) {
          const sc = ICON / 16 * 1.1;
          X.S.it.forEach((q, k) => {
            const G = gridOf(q.id), s = scanOf(q, X.u); if (!G || s <= 0 || X.u >= q.r0) return;
            const C = heldAt(fist(X), X.n, k), F = { x: C[0], y: C[1], R: sc, u: 1 }, cut = s * G.out.L;
            LIT(c, () => {
              pathStroke(c, G.out, F, 2, EMBER, .08, sg => sg[5] < cut);
              pathStroke(c, G.out, F, .5, hot(.15), .8, sg => sg[5] < cut);
              for (let j = 0; j < 12; j++) { const qq = onPath(G.out, ((j / 12 + X.t * .7) % 1) * s), p = P2(F, qq); spk(c, p[0], p[1], qq[2], qq[3], .8); }
              const hq = onPath(G.out, Math.min(.9999, s)), hp = P2(F, hq);
              stroke(c, X.eye, hp, .35, .3); spk(c, hp[0], hp[1], hq[2], hq[3], 1, 1.4);
            });
          });
        },
        route(X, q, k) { const M = this.M, C = heldAt(fist(X), X.n, k), K = REC_TO(); return { C, K, E1: [C[0], EDGE_Y], E2: [K[0], EDGE_Y], slide: q.land - q.r0 - M.shrink - M.drop - M.sink }; },
        recAt(X, q, k, v) {
          const M = this.M, R = this.route(X, q, k);
          if (v < M.shrink) return R.C;
          v -= M.shrink; if (v < M.drop) return lerp2(R.C, R.E1, sm(v / M.drop));
          v -= M.drop; if (v < R.slide) return lerp2(R.E1, R.E2, sm(v / R.slide));
          v -= R.slide; return lerp2(R.E2, R.K, ein(c01(v / M.sink)));
        },
        recDraw(c, X, q, k, v, p) {
          const M = this.M, G = gridOf(q.id), R = this.route(X, q, k), sc = ICON / 16 * 1.1;
          LIT(c, () => {
            if (v < M.shrink && G) {   // the outline folds into the card
              const e = sm(v / M.shrink);
              c.beginPath();
              G.out.polys[0].forEach(([x, y], j) => {
                const a = Math.atan2(y, x), ca = Math.cos(a), sa = Math.sin(a), r = Math.min(4 / (Math.abs(ca) || 1e-6), 5 / (Math.abs(sa) || 1e-6));
                const P = [R.C[0] + lerp(x * sc, ca * r, e), R.C[1] + lerp(y * sc, sa * r, e)];
                if (j) c.lineTo(P[0], P[1]); else c.moveTo(P[0], P[1]);
              });
              c.lineWidth = .6; c.strokeStyle = css(hot(.2), .85); c.stroke();
              return;
            }
            if (v >= M.shrink + M.drop) { stroke(c, R.E1, [p[0], EDGE_Y], 1.8, .08); stroke(c, R.E1, [p[0], EDGE_Y], .5, .35); }
            rr(c, p[0] - 4, p[1] - 5, 8, 10, 1.5); c.lineWidth = 2; c.strokeStyle = css(EMBER, .1); c.stroke(); c.lineWidth = .6; c.strokeStyle = css(hot(.2), .85); c.stroke();
            if (G) pathStroke(c, G.out, { x: p[0], y: p[1], R: .4, u: 1 }, .4, hot(.2), .7);
          });
          halo(c, p[0], p[1], 6, EMBER, .35);
        },
      },
      {
        id: 'card', name: 'Проекция', focus: [672, 262],
        M: { open: .12, print: .6, snap: .12, pull: .2, beat: .06, down: .35, apart: .2 },
        idea: 'Глаз дрона выстреливает четыре искры, и они встают уголками («Уголки») — карточка-экран рядом с героем. Нить из глаза печатает в ней картинку предмета строка за строкой тлеющими квадратиками. Готово — карточка схлопывается в черту, черта втягивается в глаз (ядро вспыхивает: записано), и глаз пускает искру по нити вниз, в кнопку «Изучения». Пачку дрон печатает тремя карточками разом и закрывает их по очереди.',
        time: 'Уголки встают за 0,12 с · печать 0,6 с (в пачке все три разом) · карточка схлопывается за 0,12 с · в глаз 0,2 с · искра вниз 0,35 с · в пачке карточки закрываются через 0,2 с · одна находка: запись в кнопке через 1,55 с после того, как предмет в руке; пачка: через 1,8, 2,0 и 2,2 с.',
        scans(ap, M) { const s0 = ap[ap.length - 1] + .1 + M.open; return ap.map(() => [s0, s0 + M.print]); },
        landOf(q, k, it, M) { return q.r0 + k * M.apart + M.snap + M.pull + M.beat + M.down; },
        at(X, k) { return X.n === 1 ? [640, 214] : [[596, 222], [640, 212], [684, 222]][k]; },
        aim(X) {
          const it = X.S.it, M = this.M, k = it.findIndex(q => X.u >= q.ap && X.u < q.r0 + it.indexOf(q) * M.apart + M.snap + M.pull);
          if (k >= 0) return this.at(X, k);
          return it.some(q => X.u >= q.r0 && X.u < q.land) ? REC_TO() : null;
        },
        look(X) { const k = X.S.it.findIndex(q => X.u >= q.ap && X.u < q.s1); return k < 0 ? null : this.at(X, k); },
        flareOf(X) {
          const M = this.M; let f = 0;
          X.S.it.forEach((q, k) => { const v = X.u - q.r0 - k * M.apart - M.snap - M.pull; if (v >= 0 && v < .3) f = Math.max(f, 1 - v / .3); });
          return f;
        },
        draw(c, X) {
          const M = this.M, u = X.u, cs = 2.2;
          X.S.it.forEach((q, k) => {
            const G = gridOf(q.id); if (!G) return;
            const o = c01((u - q.ap - .1) / M.open), sn = u >= q.r0 ? c01((u - q.r0 - k * M.apart) / M.snap) : 0;
            if (o <= 0 || sn >= 1) return;
            const C = this.at(X, k), hk = 1 - sm(sn), hw = 20, hh = 20 * hk, s = scanOf(q, u), rows = s * 16, hr = Math.floor(rows), fr = rows - hr;
            if (o >= 1) { c.fillStyle = 'rgba(6,9,13,.72)'; c.fillRect(C[0] - hw, C[1] - hh, 2 * hw, 2 * hh); }
            LIT(c, () => {
              for (let j = 0; j < 4; j++) {
                const sx = j === 0 || j === 3 ? -1 : 1, sy = j < 2 ? -1 : 1, P0 = [C[0] + sx * hw, C[1] + sy * hh];
                if (o < 1) { const p = lerp2(X.eye, P0, sm(o)); stroke(c, X.eye, p, .4, .2); spk(c, p[0], p[1], 0, 0, 1, 1.6); continue; }
                const Ax = [P0[0] - sx * 7, P0[1]], Ay = [P0[0], P0[1] - sy * 7 * Math.max(hk, .15)];
                for (const [w, al] of [[2.2, .07], [.6, .5]]) { c.beginPath(); c.moveTo(Ax[0], Ax[1]); c.lineTo(P0[0], P0[1]); c.lineTo(Ay[0], Ay[1]); c.lineWidth = w; c.strokeStyle = css(EMBER, al); c.stroke(); }
                gsq(c, P0[0], P0[1], 2.2, .9);
                const m = (X.t * 1.3 + j * .17) % 1, B = m < .5 ? lerp2(Ax, P0, m * 2) : lerp2(P0, Ay, m * 2 - 1);
                spk(c, B[0], B[1], 0, 0, .8);
              }
              if (o < 1) return;
              for (const cl of G.cells) {
                const r = cl.y + 7.5, col = cl.x + 7.5;
                if (r > hr || (r === hr && col >= fr * 16)) continue;
                const h = .14 + .45 * cl.l + (r >= hr - 1 ? .35 : 0) + .4 * sn;
                gsq(c, C[0] + cl.x * cs, C[1] + cl.y * cs * hk, 1.6, h);
              }
              if (s > 0 && s < 1) { const hp = [C[0] + (fr * 16 - 8) * cs, C[1] + (hr - 7.5) * cs]; stroke(c, X.eye, hp, .35, .28); spk(c, hp[0], hp[1], 1, 0, 1, 1.3); }
            });
          });
        },
        recAt(X, q, k, v) {
          const M = this.M, C = this.at(X, k), w = k * M.apart + M.snap;
          if (v < w) return C;
          v -= w; if (v < M.pull) return lerp2(C, X.eye, sm(v / M.pull));
          v -= M.pull; if (v < M.beat) return X.eye;
          v -= M.beat; return lerp2(X.eye, REC_TO(), ein(c01(v / M.down)));
        },
        recDraw(c, X, q, k, v, p) {
          const M = this.M, w = v - k * M.apart - M.snap;
          if (w < 0) return;   // the printed card is the record while it waits and snaps shut
          if (w < M.pull) {
            const hw = 20 * (1 - sm(w / M.pull));
            LIT(c, () => { stroke(c, [p[0] - hw, p[1]], [p[0] + hw, p[1]], 2, .1); stroke(c, [p[0] - hw, p[1]], [p[0] + hw, p[1]], .6, .85); gsq(c, p[0], p[1], 1.6, 1); });
            return;
          }
          if (w < M.pull + M.beat) return;
          const e = c01((w - M.pull - M.beat) / M.down), d = unit(X.eye, REC_TO());
          LIT(c, () => { stroke(c, X.eye, p, .4, .2 * (1 - .5 * e)); spk(c, p[0], p[1], d[0], d[1], 1, 2); });
          halo(c, p[0], p[1], 7, EMBER, .5);
        },
      },
      {
        id: 'queue', name: 'Очередь', focus: [700, 330],
        M: { glass: .3, scan: .4, gap: .1, send: .2, apart: .12, run: .45 },
        idea: 'Находки встают в очередь: над дроном загорается ряд тусклых точек, по одной на находку, как точки шкалы скана. Дрон собирает из искр подзорную трубу и проводит лучом по предмету в руке — пройденная часть просвечивает; скан готов — его точка разгорается (это запись). Когда горят все точки, ряд срывается: точки одна за другой уходят по нитям в кнопку «Изучения», число считает их. Одна находка — одна точка.',
        time: 'Точка встаёт за 0,1 с · труба собирается за 0,3 с · луч по предмету 0,4 с, между предметами 0,1 с · ряд уходит через 0,2 с после последнего скана, точки с шагом 0,12 с, полёт 0,45 с · одна находка: запись в кнопке через 1,4 с после того, как предмет в руке; пачка: через 2,65, 2,75 и 2,9 с.',
        scans(ap, M) { let prev = ap[ap.length - 1] + .05 + M.glass - M.gap; return ap.map(() => { const s0 = prev + M.gap; prev = s0 + M.scan; return [s0, prev]; }); },
        landOf(q, k, it, M) { return it[it.length - 1].s1 + M.send + k * M.apart + M.run; },
        dotAt(X, k) { return [X.post[0] + (k - (X.n - 1) / 2) * 8, X.post[1] - 18]; },
        aim(X) {
          const it = X.S.it, k = it.findIndex(q => X.u >= q.s0 - .12 && X.u < q.s1);
          if (k >= 0) return heldAt(fist(X), X.n, k);
          return X.u >= it[0].s0 - this.M.glass && X.u < it[0].s0 ? heldAt(fist(X), X.n, 0) : null;
        },
        draw(c, X) {
          const M = this.M, S = X.S, u = X.u, L = S.it[S.n - 1], g0 = S.it[0].s0 - M.glass, form = c01((u - g0) / M.glass) * (1 - c01((u - L.s1 - .05) / .25));
          S.it.forEach((q, k) => { const a = c01((u - q.ap - .05) / .1), dp = this.dotAt(X, k); if (a > 0 && u < q.r0) { c.fillStyle = css([38, 39, 43], a); c.fillRect(dp[0] - 2, dp[1] - 2, 4, 4); } });
          LIT(c, () => {
            if (form > 0) glass(c, X, form);
            S.it.forEach((q, k) => {
              const a = c01((u - q.ap - .05) / .1), dp = this.dotAt(X, k);
              if (a > 0 && u < q.r0) gsq(c, dp[0], dp[1], 3.2, a * (.16 + .06 * Math.sin(X.t * 6 + k)));
              const s = scanOf(q, u), G = gridOf(q.id);
              if (s <= 0 || s >= 1 || !G) return;
              const C = heldAt(fist(X), X.n, k), sc = ICON / 16, x = C[0] + (8 - s * 16) * sc, top = [x, C[1] - 8 * sc - 2], bot = [x, C[1] + 8 * sc + 2], mo = mouthOf(X);
              stroke(c, mo, top, .35, .25); stroke(c, mo, bot, .35, .25); stroke(c, top, bot, 2.2, .1); stroke(c, top, bot, .6, .85);
              for (const cl of G.cells) { const cx = C[0] + cl.x * sc; if (cx < x) continue; gsq(c, cx, C[1] + cl.y * sc, 1.1, (.1 + .35 * cl.l) * Math.exp(-(cx - x) / 10) + .08); }
            });
          });
        },
        recAt(X, q, k, v) { const dp = this.dotAt(X, k), go = q.land - this.M.run; return X.u < go ? dp : lerp2(dp, REC_TO(), ein(c01((X.u - go) / this.M.run))); },
        recDraw(c, X, q, k, v, p) {
          const go = q.land - this.M.run, dp = this.dotAt(X, k), d = unit(dp, REC_TO());
          c.fillStyle = '#26272b'; c.fillRect(p[0] - 2, p[1] - 2, 4, 4);
          LIT(c, () => {
            if (X.u >= go) { stroke(c, dp, p, .35, .14); spk(c, p[0], p[1], d[0], d[1], 1, 2.4); }
            else gsq(c, p[0], p[1], 3.2, .85 + .15 * Math.sin(X.t * 9 + k));
          });
          if (v < .25) halo(c, p[0], p[1], 8, EMBER, .5 * (1 - v / .25));
          if (X.u >= go) halo(c, p[0], p[1], 6, EMBER, .4);
        },
      },
    ];

    // ---------- the plan: when each find lands in the hand, is scanned, its record begins and reaches the key ----------
    const PLANS = {};
    function planOf(cand, kind) {
      const key = cand.id + ':' + kind; if (PLANS[key]) return PLANS[key];
      const ids = SLOTS[kind], M = cand.M, ap = ids.map((_, k) => APPEAR + k * APART);
      const it = cand.scans(ap, M).map(([s0, s1], k) => ({ id: ids[k], ap: ap[k], s0, s1, r0: s1 }));
      it.forEach((q, k) => { q.land = cand.landOf(q, k, it, M); });
      const S = { kind, n: it.length, it, down: Math.max(...it.map(q => q.land)) + .25 };
      S.end = S.down + DOWN + REST;
      return (PLANS[key] = S);
    }
    function slotsOf(cand) {
      const kinds = OPT.show === 'all' ? ['single', 'batch'] : [OPT.show];
      let t = 0; return kinds.map(kind => { const S = planOf(cand, kind), q = { S, t0: t }; t += S.end; return q; });
    }
    function stateOf(T) {
      const sl = slotsOf(T.cand), L = sl[sl.length - 1].t0 + sl[sl.length - 1].S.end, e = Math.max(0, T.time - T.t0), cyc = Math.floor(e / L), w = e - cyc * L;
      let ix = sl.findIndex(q => w < q.t0 + q.S.end); if (ix < 0) ix = sl.length - 1;
      let base = 0; for (let j = 0; j < ix; j++) base += sl[j].S.n;
      return { S: sl[ix].S, u: w - sl[ix].t0, base, cyc, ix };
    }
    // a record: null before it begins, on its way, or landed in the key
    function recOf(X, q, k) {
      if (X.u < q.r0) return null;
      if (X.u >= q.land) return { p: REC_TO(), landed: true };
      return { p: X.cand.recAt(X, q, k, X.u - q.r0), landed: false };
    }

    // ---------- a frame: the hero's pose, the drone, the records, then the world drawn from them ----------
    function buildX(T, st, dt) {
      const me = T.me, cand = T.cand, S = st.S, u = st.u;
      const X = { T, me, cand, S, u, n: S.n, base: st.base, t: T.time + 1.3 };
      X.up = sm(c01(u / UP)) * (1 - sm(c01((u - S.down) / DOWN)));
      me.x = HX; me.y = FLOOR_Y; me.face = FACE; me.rk = X.up; me.rp = HAND;
      me.P = poseOf(me);
      const b = toS(me, [-9, -44]); X.post = [b[0], b[1] + Math.sin(X.t * 1.7) * 1.5];
      X.D = cand.droneAt ? cand.droneAt(X) : X.post;
      const aim = cand.aim(X), want = aim ? Math.atan2(aim[1] - X.D[1], aim[0] - X.D[0]) : Math.atan2(46, 22 * FACE);
      T.A = angLerp(T.A, want, OPT.playing ? Math.min(1, dt * 14) : 1);
      X.A = T.A; X.eye = eyeOf(X);
      X.recs = S.it.map((q, k) => recOf(X, q, k));
      X.num = X.base + X.recs.filter(r => r && r.landed).length;
      X.keyFl = Math.max(0, ...S.it.map(q => u >= q.land ? 1 - c01((u - q.land) / FLARE) : 0));
      X.flare = cand.flareOf ? cand.flareOf(X) : 0;
      const held = S.it.map((q, k) => heldAt(fist(X), X.n, k)), look = (cand.look && cand.look(X)) || (X.up > .3 ? [held.reduce((a, p) => a + p[0], 0) / held.length, held.reduce((a, p) => a + p[1], 0) / held.length] : null);
      const nk = toS(me, NECK());
      turnHead(me, look ? headTurn((look[0] - nk[0]) * me.face, look[1] - nk[1]) : 0, dt);
      me.P = poseOf(me);
      return X;
    }
    function back(c) {
      const g = c.createLinearGradient(0, 0, 0, FLOOR_Y); g.addColorStop(0, '#04060a'); g.addColorStop(1, '#0a0f17'); c.fillStyle = g; c.fillRect(0, 0, LW, FLOOR_Y);
      for (const [x, y, sz, a] of STARS) { const xx = x - 200; if (xx < -2 || xx > LW + 2) continue; c.fillStyle = `rgba(220,228,240,${a.toFixed(3)})`; c.fillRect(xx, y, sz, sz); }
      c.fillStyle = '#2f3138'; c.fillRect(0, FLOOR_Y, LW, LH - FLOOR_Y);
      c.fillStyle = '#4f525b'; c.fillRect(0, FLOOR_Y, LW, 3);
      c.fillStyle = 'rgba(0,0,0,.35)'; c.fillRect(0, FLOOR_Y + 3, LW, 3);
      for (let x = 0; x < LW + 32; x += 32) { c.fillStyle = 'rgba(0,0,0,.4)'; c.fillRect(x, FLOOR_Y + 6, 2, LH - FLOOR_Y - 6); }
    }
    const heldAlpha = (X, q) => c01((X.u - q.ap) / .1) * (1 - c01((X.u - X.S.down) / .25));
    // the items in the hand, as the game draws them; what each was drawn at goes to the check
    function drawHeld(c, X) {
      const fp = fist(X);
      return X.S.it.map((q, k) => {
        const a = heldAlpha(X, q), im = IMG[q.id];
        if (a <= .003 || !ready(im)) return 0;
        const p = heldAt(fp, X.n, k), s = ICON * (1 + .3 * (1 - c01((X.u - q.ap) / .12)));
        c.globalAlpha = a; c.drawImage(im, p[0] - s / 2, p[1] - s / 2, s, s); c.globalAlpha = 1;
        return a;
      });
    }
    function drawKey(c, X, id) {
      const [x, y] = keyBox(id), rs = id === 'research', wait = rs && X.num > 0, b = LEVEL[wait ? 'wait' : 'rest'], fl = rs ? X.keyFl : 0;
      const cx = x + KEY.side / 2, cy = y + KEY.side / 2, sd = KEYS.indexOf(id) * 2.3 + 1;
      rr(c, x + .5, y + .5, KEY.side - 1, KEY.side - 1, 3); c.fillStyle = css(deep(.1), Math.min(.75, .25 + .5 * b)); c.fill();
      threadIcon(c, RIM, { x: cx, y: cy, R: KEY.side / 2 - .5, u: .7 }, { lv: b * .5, rim: 1, t: X.t, sd });
      threadIcon(c, ICONS[id], { x: cx, y: cy, R: KEY.icon, u: 1 }, { lv: b, wait, t: X.t, sd, flare: fl });
      if (fl > 0) LIT(c, () => {   // as today's key flare: a ring spreading off it
        const sp = 14 * (1 - fl);
        c.lineWidth = 2; c.strokeStyle = css(EMBER, .7 * fl); c.strokeRect(x - 2 - sp, y - 2 - sp, KEY.side + 4 + 2 * sp, KEY.side + 4 + 2 * sp);
        c.lineWidth = 4; c.strokeStyle = css(EMBER, .22 * fl); c.strokeRect(x - 4 - sp * 1.6, y - 4 - sp * 1.6, KEY.side + 8 + 3.2 * sp, KEY.side + 8 + 3.2 * sp);
      });
      if (!rs || X.num <= 0) return;
      const base = y + KEY.side + 5, wd = 15;
      c.fillStyle = 'rgba(4,6,9,.85)'; c.fillRect(cx - wd / 2, base - 12, wd, 14);
      c.strokeStyle = css(EMBER, .55 * b); c.lineWidth = 1; c.strokeRect(cx - wd / 2 + .5, base - 11.5, wd - 1, 13);
      c.font = '600 13px ui-monospace,Consolas,monospace'; c.textAlign = 'center'; c.textBaseline = 'alphabetic';
      c.fillStyle = css(hot(.45 + .5 * fl), Math.min(1, b + fl)); c.fillText(String(X.num), cx, base); c.textAlign = 'left';
    }
    function drawWorld(c, T, X) {
      back(c);
      lampOn(c, X.D[0], X.D[1], T.me.x, T.me.y, T.me.face, .6, 2.6);
      drawHero(c, T.me);
      c.lineCap = 'butt'; c.lineJoin = 'miter';
      const shown = drawHeld(c, X);
      T.cand.draw(c, X);
      threadDrone(c, X);
      // the tablet's edge: a stand-in plate and its six keys
      rr(c, 200, EDGE_Y, 600, LH - EDGE_Y + 8, 6); c.fillStyle = 'rgba(10,12,16,.95)'; c.fill();
      c.fillStyle = 'rgba(120,128,140,.35)'; c.fillRect(206, EDGE_Y, 588, 1);
      for (const id of KEYS) drawKey(c, X, id);
      X.recs.forEach((r, k) => { if (r && !r.landed) T.cand.recDraw(c, X, X.S.it[k], k, X.u - X.S.it[k].r0, r.p); });
      return shown;
    }
    function say(X) {
      const S = X.S, u = X.u, it = S.it, names = it.map(q => NAMES[q.id]).join(', ');
      const what = u < it[0].ap ? 'герой берёт предмет'
        : it.some(q => u >= q.s0 && u < q.s1) ? 'дрон сканирует'
        : u < it[0].s0 ? 'новое в руке'
        : it.some(q => u >= q.r0 && u < q.land) ? 'запись идёт в кнопку «Изучения»'
        : u < S.down + DOWN ? `запись на доске, на кнопке ${X.num}` : 'покой';
      return `${S.n === 1 ? 'одна находка' : 'пачка из трёх'}: ${names} · ${what}`;
    }
    function scene(T, dt) {
      if (T.showWas !== OPT.show) { T.showWas = OPT.show; T.t0 = T.time; }
      const st = stateOf(T), X = buildX(T, st, dt), c = T.c;
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      const shown = drawWorld(c, T, X);
      if (OPT.loupe) {
        const f = T.cand.focus;
        c.save(); rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip();
        c.translate(LP.x + LP.w / 2, LP.y + LP.h / 2); c.scale(LP.k, LP.k); c.translate(-f[0], -f[1]);
        drawWorld(c, T, X);
        c.restore();
        rr(c, LP.x + .5, LP.y + .5, LP.w - 1, LP.h - 1, 8); c.strokeStyle = 'rgba(140,150,165,.45)'; c.lineWidth = 1; c.stroke();
        c.font = '11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(200,205,214,.6)'; c.fillText('лупа ×1,8', LP.x + 8, LP.y + LP.h - 8);
      }
      c.font = '13px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(214,226,240,.78)';
      c.fillText(say(X), 362, 30);
      if (OPT.slow) { c.fillStyle = 'rgba(214,190,140,.85)'; c.fillText('×¼', LW - 36, 30); }
      T.dm = {
        mode: OPT.show, t0: T.t0, cyc: st.cyc, ix: st.ix, kind: st.S.kind, u: st.u, end: st.S.end, base: X.base, num: X.num, loupe: !!OPT.loupe, time: T.time,
        items: st.S.it.map((q, k) => ({ id: q.id, shown: shown[k], scan: scanOf(q, st.u), rec: X.recs[k] })),
      };
    }

    const LOOPS = {}, TIMES = {};
    for (const cd of cands) {
      const s = planOf(cd, 'single'), b = planOf(cd, 'batch');
      LOOPS[cd.id] = { single: s.end, batch: b.end, all: s.end + b.end };
      TIMES[cd.id] = [s, b].map(S => S.it.map(q => `${q.id} in ${q.ap.toFixed(2)} scan ${q.s0.toFixed(2)}-${q.s1.toFixed(2)} key ${q.land.toFixed(2)}`).join('; '));
    }
    return {
      cands, expose: { KEY: keyMid(), LOOPS, TIMES, WANT: { single: SLOTS.single.length, batch: 3 } },
      init(T) { T.t0 = 0; T.showWas = OPT.show; T.A = Math.atan2(46, 22 * FACE); T.me.x = HX; T.me.face = FACE; },
      scene,
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    early: [['s0, s1, r0: s1 }', 's0, s1, r0: s0 }']],
    short: [['const REC_TO = () => keyMid();', 'const REC_TO = () => add(keyMid(), [0, -70]);']],
    drop: [['const a = heldAlpha(X, q), im = IMG[q.id];', 'const a = k === 2 ? 0 : heldAlpha(X, q), im = IMG[q.id];']],
    count: [['X.num = X.base + X.recs.filter(r => r && r.landed).length;', 'X.num = X.base + (X.recs.some(r => r && r.landed) ? 1 : 0);']],
  },

  // a whole loop (one find, then a batch), then one find alone, a batch alone; slow, a pause, the loupe off, a real pointer; a pick
  check(K) {
    const X = K.X, F = s => Math.ceil(s * 60), ids = Object.keys(X.LOOPS), mx = k => Math.max(...ids.map(i => X.LOOPS[i][k]));
    const A = F(mx('all')) + 12, B = A + F(mx('single')) + 12, C = B + F(mx('batch')) + 12, D = C + 60, E = D + 40, G = E + 40, END = G + 20;
    K.at(A, () => K.seg('showMode', 1));
    K.at(B, () => K.seg('showMode', 2));
    K.at(C, () => { K.seg('showMode', 0); K.seg('speedMode', 1); });
    K.at(D, () => { K.seg('speedMode', 0); K.seg('playMode', 1); });
    K.at(E, () => { K.seg('playMode', 0); K.seg('loupeMode', 1); K.ptrAll([500, 300]); });
    let st1 = '', doc1 = null, fb = '';
    K.at(G, () => { K.ptrAll(null); K.click(1, '.pick'); });
    K.at(G + 10, () => { st1 = K.byId.status.textContent; doc1 = K.picked(); K.byId.copy.listeners.click[0](); fb = K.byId.fallback.value; });
    const per = K.tiles.map(() => ({ occ: {}, bad: {}, order: 0, slow: 0, paused: 0, lOn: 0, lOff: 0, lastT: -1 }));
    // a broken per-frame rule, counted per candidate with its first case
    const bad = (q, rule, m) => { const b = q.bad[rule] || (q.bad[rule] = { n: 0, m }); b.n++; };
    K.run(END, (t, i) => {
      const q = per[K.tiles.indexOf(t)], d = t.dm; if (!d) return;
      const id = t.cand.id, key = `${d.mode}|${d.t0}|${d.cyc}|${d.ix}`;
      const o = q.occ[key] || (q.occ[key] = { mode: d.mode, kind: d.kind, n: d.items.length, u0: d.u, u1: d.u, end: d.end, shownF: d.items.map(() => 0), scan: d.items.map(() => 0), landed: d.items.map(() => false), minD: d.items.map(() => 1e9) });
      o.u1 = d.u;
      let landed = 0;
      d.items.forEach((it, k) => {
        if (it.shown >= .9) o.shownF[k]++;
        o.scan[k] = Math.max(o.scan[k], it.scan);
        if (!it.rec) return;
        // the scan comes before the record: no record is drawn while its find's scan is unfinished
        if (it.scan < 1) bad(q, 'early', `${it.id}'s record drawn at ${d.u.toFixed(2)} s with its scan ${(it.scan * 100).toFixed(0)}% done`); else q.order++;
        const dd = Math.hypot(it.rec.p[0] - X.KEY[0], it.rec.p[1] - X.KEY[1]);
        if (it.rec.landed) { landed++; o.landed[k] = true; if (dd > 1) bad(q, 'off', `${it.id}'s record landed ${dd.toFixed(0)} px from the research key`); }
        else o.minD[k] = Math.min(o.minD[k], dd);
      });
      if (d.num !== d.base + landed) bad(q, 'count', `the key shows ${d.num} with ${d.base + landed} records in it, frame ${i}`);
      if (i > C + 1 && i < D) { if (Math.abs(t.time - q.lastT - 1 / 240) > 1e-4) K.fail(`${id}: slow motion ran ${(t.time - q.lastT).toFixed(4)} s a frame`); else q.slow++; }
      if (i > D + 1 && i < E) { if (t.time !== q.lastT) K.fail(`${id}: time ran while paused`); else q.paused++; }
      if (i < E) { if (d.loupe) q.lOn++; } else if (i > E + 1) { if (d.loupe) K.fail(`${id}: the loupe drawn while off`); else q.lOff++; }
      q.lastT = t.time;
    });
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    const full = o => o.u0 < .05 && o.u1 > o.end - .05;
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id, occs = Object.values(q.occ), whole = occs.filter(full);
      console.log(`${id}: loop ${X.LOOPS[id].all.toFixed(2)} s | one: ${X.TIMES[id][0]} | batch: ${X.TIMES[id][1]}`);
      console.log(`   whole runs ${whole.map(o => `${o.mode}/${o.kind} shown ${o.shownF.join(',')} frames, nearest ${o.minD.map(v => v.toFixed(0)).join(',')} px`).join(' | ')} | records after scans ${q.order} | slow ${q.slow} paused ${q.paused} loupe ${q.lOn}/${q.lOff}`);
      for (const [rule, what] of [['early', 'no record before its scan ends'], ['off', 'records land in the research key'], ['count', 'the key counts every record']]) {
        const b = q.bad[rule]; need(!b, `${id}: ${what}: ${b ? `${b.n} frames, first: ${b.m}` : ''}`);
      }
      const all = whole.filter(o => o.mode === 'all');
      need(all.some(o => o.kind === 'single') && all.some(o => o.kind === 'batch'), `${id}: a whole loop shows one find, then a batch`);
      for (const o of whole) {
        const w = `${id}: ${o.mode}/${o.kind}`;
        need(o.n === X.WANT[o.kind], `${w} has ${o.n} finds, want ${X.WANT[o.kind]}`);
        o.shownF.forEach((f, k) => need(f >= 12, `${w}: find ${k + 1} shown in the hand ${f} frames, want 12`));
        o.scan.forEach((s, k) => need(s >= 1, `${w}: find ${k + 1} scanned ${(s * 100).toFixed(0)}%`));
        o.landed.forEach((l, k) => need(l, `${w}: find ${k + 1}'s record never reached the research key`));
        o.minD.forEach((v, k) => need(v <= 40, `${w}: find ${k + 1}'s record came no nearer the key than ${v.toFixed(0)} px`));
      }
      need(whole.some(o => o.mode === 'single') && !occs.some(o => o.mode === 'single' && o.kind !== 'single'), `${id}: «одна находка» shows only one find`);
      need(whole.some(o => o.mode === 'batch') && !occs.some(o => o.mode === 'batch' && o.kind !== 'batch'), `${id}: «пачка из трёх» shows only the batch`);
      need(q.order > 100, `${id}: records drawn`);
      need(q.slow > 30 && q.paused > 30 && q.lOn > 100 && q.lOff > 10, `${id}: slow, the pause, the loupe on and off`);
    });
    const cand = K.tiles[1].cand;
    console.log('after the pick:', st1, '| doc', JSON.stringify(doc1), '| copy:', fb);
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    need(doc1 && doc1.pick === cand.id, 'the pick is in the db doc');
    need(st1.includes(`«${cand.name}»`) && fb.includes(`(${cand.id})`), 'the status and the copy text name the pick');
    return ok;
  },
};
