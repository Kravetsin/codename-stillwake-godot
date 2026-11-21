// The light drone's thoughts (small holograms it shows by itself, like moods) and its scanner's readout over a wreck, one
// look for both: an art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-thoughts.js <out-dir> [--break hold|far|value|skip]
module.exports = {
  title: 'Мысли дрона',
  heading: 'Мысли дрона и его скан: один вид голограмм',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Дрон говорит голограммами: герой вошёл в холод — над дроном снежинка, «уходи отсюда». Здесь пять видов таких мыслей, и у каждого свой скан обломка — тем же языком, вместо нынешних зелёных цифр на тёмном стекле. Набор знаков пробный: <b>холод, жара, мало воздуха, метеорит, молния, «за тобой следят»</b> и одно настроение — <b>любопытство</b>; какие знаки будут в игре, решим отдельно. Каждый знак появляется у дрона, держится <b>1,5 с</b> и гаснет. Между ними — скан: указатель на обломке, дрон его читает, над ним встаёт прочность «осталось / всего»; на 3,3 с по обломку прилетает удар, и число падает.</p>
<p class="lead">Масштаб игры: клетка 8 px мира, вид ×4, герой ~42 px мира, дрон у его головы. В лупе справа вверху — дрон и знак в 2,5 раза крупнее, во время скана — табличка вдвое крупнее.</p>
<p class="miss">Не показано: звук; что видят друзья; отрисовка Godot (свет, строки и цвета выйдут чуть иначе); гребень сканера на шлеме; что именно вызывает каждую мысль. Набор знаков — набросок, не решение; рисунок самих знаков тоже поправим после выбора вида.</p>`,
  toggles: [
    { id: 'showMode', bar: 1, label: 'Показ:', key: 'show', opts: [['мысли и скан', 'all'], ['только мысли', 'thoughts'], ['только скан', 'scan']] },
    { id: 'loupeMode', bar: 1, label: 'Лупа:', key: 'loupe', opts: [['есть', true], ['нет', false]] },
  ],
  pick: { doc: 'picks/droneThoughts', subject: 'Мысли дрона и скан', what: ['голограмму', 'голограммой'] },

  page() {
    const TAU = Math.PI * 2;
    // ---------- the loop: a slot per thought, the scan between ----------
    const SLOT = 2.5, HOLD = 1.5, SCAN = 6;
    const SC = { arrive: .35, sweep: [.35, 1.55], show: [1.55, 1.95], hit: 3.3, gone: [5, 5.4] };
    const VAL = [340, 230], MAX = 520;
    const ICONS = ['cold', 'heat', 'air', 'meteor', 'lightning', 'hunted', 'curious'];
    const ORDER = ['cold', 'heat', 'scan', 'air', 'meteor', 'lightning', 'hunted', 'curious'];
    const SAY = { cold: 'холод: уходи отсюда', heat: 'жара: уходи отсюда', air: 'мало воздуха', meteor: 'летит метеорит', lightning: 'молния рядом', hunted: 'за тобой следят', curious: 'любопытно (настроение)' };
    const HERO_X = 640, OFF = [0, -44], RI = 17, LOUPE_K = 2.5, LOUPE_RK = 2;   // a thought's centre from the drone's and its half size, page px at ×1
    const TEAL = [118, 236, 206], AMBER = [255, 176, 72], RED = [255, 84, 64], CREAM = [238, 230, 212];
    const LOOP = ICONS.length * SLOT + SCAN;

    const noise = (t, s) => c01(.5 + .3 * Math.sin(t * 31.7 + s) + .22 * Math.sin(t * 13.1 + s * 2.3) + .18 * Math.sin(t * 77.3 + s * .7));
    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const fracCol = v => v / MAX > .5 ? TEAL : v / MAX > .25 ? AMBER : RED;

    // ---------- lines ----------
    const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
    const circ = (cx, cy, r, n = 22) => arcP(cx, cy, r, r, 0, TAU, n);
    const plen = p => { let L = 0; for (let i = 1; i < p.length; i++) L += Math.hypot(p[i][0] - p[i - 1][0], p[i][1] - p[i - 1][1]); return L; };
    function subP(p, f) {   // the first f of a polyline
      const L = plen(p) * f; if (L <= 0) return null;
      const out = [p[0]]; let d = 0;
      for (let i = 1; i < p.length; i++) {
        const s = Math.hypot(p[i][0] - p[i - 1][0], p[i][1] - p[i - 1][1]);
        if (d + s >= L) { out.push(lerp2(p[i - 1], p[i], (L - d) / (s || 1))); return out; }
        d += s; out.push(p[i]);
      }
      return out;
    }
    const tip = (p, f) => { const s = subP(p, f); return s ? s[s.length - 1] : p[0]; };
    function line(g, polys) { g.beginPath(); for (const p of polys) { if (!p || p.length < 2) continue; g.moveTo(p[0][0], p[0][1]); for (let i = 1; i < p.length; i++) g.lineTo(p[i][0], p[i][1]); } }
    function strokeP(g, polys, w, col, a) { if (a <= .003 || !polys.length) return; line(g, polys); g.lineWidth = w; g.strokeStyle = css(col, a); g.stroke(); }
    function clipTo(g, p) { g.beginPath(); g.moveTo(p[0][0], p[0][1]); for (let i = 1; i < p.length; i++) g.lineTo(p[i][0], p[i][1]); g.closePath(); g.clip(); }
    const place = (polys, O, sx, sy = sx) => polys.map(p => p.map(q => [O[0] + q[0] * sx, O[1] + q[1] * sy]));
    // two pens share the strokes, each tracing its own in turn: what f of the way has drawn, and where the pens are
    function pens(polys, f) {
      f = c01(f);
      const L = polys.map(plen), ids = [[], []], tot = [0, 0], drawn = [], heads = [];
      polys.forEach((p, i) => { const j = tot[0] <= tot[1] ? 0 : 1; ids[j].push(i); tot[j] += L[i]; });
      ids.forEach((list, j) => {
        let rest = f * tot[j], head = null;
        for (const i of list) {
          if (rest <= 0) break;
          if (rest >= L[i]) { drawn.push(polys[i]); head = polys[i][polys[i].length - 1]; rest -= L[i]; }
          else { const s = subP(polys[i], rest / L[i]); if (s) { drawn.push(s); head = s[s.length - 1]; } rest = 0; }
        }
        if (head && f < 1) heads.push(head);
      });
      return { drawn, heads };
    }
    function dots(polys, sp) { const out = []; for (const p of polys) { const n = Math.max(1, Math.round(plen(p) / sp)); for (let i = 0; i <= n; i++) out.push(tip(p, i / n)); } return out; }
    function coneTo(g, L, C, r, a0, a1, col) {
      const dx = C[0] - L[0], dy = C[1] - L[1], d = Math.hypot(dx, dy); if (d < 2 || a0 <= .003) return;
      const nx = -dy / d * r, ny = dx / d * r, gr = g.createLinearGradient(L[0], L[1], C[0], C[1]);
      gr.addColorStop(0, css(col, a0)); gr.addColorStop(1, css(col, a1));
      g.fillStyle = gr; g.beginPath(); g.moveTo(L[0], L[1]); g.lineTo(C[0] + nx, C[1] + ny); g.lineTo(C[0] - nx, C[1] - ny); g.closePath(); g.fill();
    }
    function disc(g, O, r, col, a) {
      if (a <= .003) return;
      const gr = g.createRadialGradient(O[0], O[1], 0, O[0], O[1], r); gr.addColorStop(0, css(col, a)); gr.addColorStop(1, css(col, 0));
      g.fillStyle = gr; g.beginPath(); g.arc(O[0], O[1], r, 0, TAU); g.fill();
    }
    function penHeads(g, lens, heads, k) {
      for (const h of heads) {
        if (lens) strokeP(g, [[lens, h]], .55 * k, SIGN, .38);
        halo(g, h[0], h[1], 4 * k, SIGN, .45);
        g.fillStyle = css(pale(SIGN, .5), .9); g.beginPath(); g.arc(h[0], h[1], .85 * k, 0, TAU); g.fill();
      }
    }

    // ---------- the signs: strokes in a box from -1 to 1, y down; a provisional set ----------
    const GLYPH = (() => {
      const G = {};
      G.cold = [];
      for (let i = 0; i < 3; i++) { const a = Math.PI / 2 + i * Math.PI / 3; G.cold.push([[-Math.cos(a), -Math.sin(a)], [Math.cos(a), Math.sin(a)]]); }
      for (let i = 0; i < 6; i++) { const a = Math.PI / 2 + i * Math.PI / 3, P = [Math.cos(a) * .56, Math.sin(a) * .56], b = q => [P[0] + Math.cos(q) * .32, P[1] + Math.sin(q) * .32]; G.cold.push([b(a - .9), P, b(a + .9)]); }
      G.heat = [-.55, 0, .55].map((x0, j) => { const p = []; for (let i = 0; i <= 16; i++) { const y = .72 - i / 16 * 1.66; p.push([x0 + .16 * Math.sin((.72 - y) * 5 + j * .6), y]); } return p; });
      G.heat.push([[-.95, .95], [.95, .95]]);
      G.air = [circ(-.18, .3, .62), [[-.64, .62], [.28, .62]], circ(.56, -.5, .2, 14), circ(.3, -.86, .1, 10)];
      const C = [-.42, .42], rock = [];
      [.36, .3, .38, .29, .35, .31, .37].forEach((r, i) => { const a = i / 7 * TAU + .3; rock.push([C[0] + Math.cos(a) * r, C[1] + Math.sin(a) * r]); });
      rock.push(rock[0].slice());
      const d = [.7071, -.7071], n = [.7071, .7071];
      G.meteor = [rock, ...[[-.24, .75], [0, 1.05], [.24, .7]].map(([o, L]) => { const s = [C[0] + d[0] * .44 + n[0] * o, C[1] + d[1] * .44 + n[1] * o]; return [s, [s[0] + d[0] * L, s[1] + d[1] * L]]; })];
      G.lightning = [[[.3, -1], [-.34, .08], [.14, .08], [-.28, 1]], [[-.28, 1], [-.27, .66]], [[-.28, 1], [-.02, .8]]];
      const lid = s => { const p = []; for (let i = 0; i <= 16; i++) { const x = -1 + i / 8; p.push([x, s * .56 * (1 - x * x)]); } return p; };
      G.hunted = [lid(-1), lid(1), circ(0, 0, .27, 16), circ(0, 0, .07, 8)];
      G.curious = [arcP(0, -.44, .4, .4, Math.PI - .15, TAU + 1.05, 18).concat([[0, .1], [0, .4]]), circ(0, .8, .08, 8)];
      return G;
    })();
    // the readout's digits, in a box .62 wide and 1 tall
    const DIG = {
      '0': [arcP(.31, .5, .29, .48, 0, TAU, 22)],
      '1': [[[.12, .2], [.36, 0], [.36, 1]]],
      '2': [arcP(.31, .3, .28, .27, Math.PI - .2, TAU + .45, 14).concat([[.02, 1], [.62, 1]])],
      '3': [arcP(.3, .27, .26, .25, Math.PI + .6, 2.5 * Math.PI, 12), arcP(.3, .76, .28, .25, -Math.PI / 2, Math.PI - .55, 12)],
      '4': [[[.46, 1], [.46, 0], [0, .68], [.62, .68]]],
      '5': [[[.58, 0], [.1, 0], [.07, .46]], arcP(.3, .68, .3, .3, -2.4, 2.5, 14)],
      '6': [[[.52, .02], [.24, .22], [.05, .62]], arcP(.31, .7, .27, .29, 0, TAU, 16)],
      '7': [[[0, 0], [.62, 0], [.22, 1]]],
      '8': [arcP(.31, .26, .23, .24, 0, TAU, 14), arcP(.31, .73, .28, .27, 0, TAU, 16)],
      '9': [arcP(.31, .3, .27, .29, 0, TAU, 16), [[.58, .36], [.46, .72], [.2, 1]]],
      '/': [[[.46, 0], [.06, 1]]],
    };
    const ADV = { '/': .62 }, advOf = ch => ADV[ch] || .84;
    const DH = 12, DSMALL = .7;   // a digit's height at ×1, page px; the maximum's size of the value's
    // the readout's text: the value, then «/max» smaller on the same foot, centred on O
    function layout(val, O, k) {
      const H = DH * k, h = H * DSMALL, a = String(val), b = '/' + MAX, wOf = (s, z) => [...s].reduce((v, ch) => v + advOf(ch), 0) * z - .22 * z;
      const wa = wOf(a, H), gap = .3 * H, wb = wOf(b, h), x0 = O[0] - (wa + gap + wb) / 2, items = [];
      let x = x0;
      for (const ch of a) { items.push({ ch, x, y: O[1] - H / 2, s: H, part: 'val' }); x += advOf(ch) * H; }
      x = x0 + wa + gap;
      for (const ch of b) { items.push({ ch, x, y: O[1] + H / 2 - h, s: h, part: 'rest' }); x += advOf(ch) * h; }
      return { items, box: [x0, O[1] - H / 2, x0 + wa + gap + wb, O[1] + H / 2] };
    }
    const polysOf = (lay, part) => { const out = []; for (const it of lay.items) if (it.part === part) for (const p of DIG[it.ch]) out.push(p.map(q => [it.x + q[0] * it.s, it.y + q[1] * it.s])); return out; };
    const DOTI = {}, DOTD = {};
    for (const id of ICONS) DOTI[id] = dots(GLYPH[id], .15);
    for (const ch in DIG) DOTD[ch] = dots(DIG[ch], .2);
    function udots(val, O, k) { const out = []; for (const it of layout(val, O, k).items) for (const q of DOTD[it.ch]) out.push({ p: [it.x + q[0] * it.s, it.y + q[1] * it.s], part: it.part }); return out; }

    // ---------- ink: a line as the game's pen draws it, wandering a little, swelling, lifting off at its ends ----------
    function inked(polys, step, wob, seed) {
      const r = rng(seed);
      return polys.map(p => {
        const n = Math.max(3, Math.round(plen(p) / step)), ph = r() * 6.283, ph2 = r() * 6.283, pts = [], w = [];
        for (let i = 0; i <= n; i++) {
          const u = i / n, q = tip(p, u), tp = Math.min(1, u / .14, (1 - u) / .14);
          pts.push([q[0] + wob * Math.sin(i * .8 + ph), q[1] + wob * Math.cos(i * .65 + ph2)]);
          w.push((.75 + .35 * Math.sin(u * 5 + ph)) * (.25 + .75 * tp));
        }
        return { pts, w, L: plen(pts) };
      });
    }
    // one pen up to f of the whole line, through O at s page px a unit; where the pen is while it draws
    function drawInk(g, ink, O, s, f, col, a, base) {
      if (a <= .003 || f <= 0) return null;
      let left = ink.reduce((v, q) => v + q.L, 0) * Math.min(1, f), head = null;
      g.strokeStyle = css(col, a);
      for (const q of ink) {
        for (let i = 1; i < q.pts.length && left > 0; i++) {
          const A = q.pts[i - 1], B = q.pts[i], d = Math.hypot(B[0] - A[0], B[1] - A[1]), e = Math.min(1, left / (d || 1));
          const P = [O[0] + A[0] * s, O[1] + A[1] * s], Q = [O[0] + lerp(A[0], B[0], e) * s, O[1] + lerp(A[1], B[1], e) * s];
          g.lineWidth = base * (q.w[i - 1] + q.w[i]) / 2; g.beginPath(); g.moveTo(P[0], P[1]); g.lineTo(Q[0], Q[1]); g.stroke();
          left -= d; head = Q;
        }
        if (left <= 0) break;
      }
      return f < 1 ? head : null;
    }
    const INKI = {}, INKD = {}, WB = {}, WB2 = {};
    ICONS.forEach((id, i) => { INKI[id] = inked(GLYPH[id], .06, .022, 11 + i); WB[id] = mk(ell(1.2, 1.04, 13), 300 + i, .34); WB2[id] = mk(ell(.66, .52, 11, .16, .22), 400 + i, .24); });
    Object.keys(DIG).forEach((ch, i) => { INKD[ch] = inked(DIG[ch], .08, .02, 50 + i); });
    const STRIKE = inked([[[0, 0], [1, -.08]]], .05, .01, 9);
    // a watercolour wash in light: a pool with a brighter tide line, a darker heart
    function wash(g, s1, s2, C, sx, sy, col, a) {
      if (a <= .003) return;
      g.save(); g.translate(C[0], C[1]); g.scale(sx, sy);
      trace(g, s1); g.fillStyle = css(col, .15 * a); g.fill(); g.lineWidth = 1.3 / Math.min(sx, sy); g.strokeStyle = css(col, .3 * a); g.stroke();
      trace(g, s2); g.fillStyle = css(col, .08 * a); g.fill();
      g.restore();
    }
    function inkText(g, lay, part, f, col, a, base) {
      const its = lay.items.filter(it => it.part === part), n = its.length;
      its.forEach((it, i) => drawInk(g, INKD[it.ch], [it.x, it.y], it.s, c01(f * n - i), col, a, base));
    }

    // ---------- the wreck: a torn hull plate on the deck, left of the hero ----------
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
    const WINK = inked([WRECK.OL], 3, .9, 4040)[0], WBW = mk(ell(1, 1, 14), 777, .3), WBW2 = mk(ell(.6, .5, 11, -.1, .15), 778, .25);
    const M0 = udots(VAL[0], READ_AT, 1).length, OLD = Array.from({ length: M0 }, (_, m) => tip(WRECK.OL, m / M0));
    const SPOT_R = 34, S0 = [WRECK.box[0] + 52, WRECK.C[1] + 8], S1 = [WRECK.box[2] - 52, WRECK.C[1] + 8];
    function drawWreck(c) {
      const W = WRECK, b = W.box;
      c.fillStyle = 'rgba(0,0,0,.45)'; c.beginPath(); c.ellipse(W.C[0], FLOOR_Y + 1.5, (b[2] - b[0]) / 2 + 10, 4, 0, 0, TAU); c.fill();
      c.lineCap = 'round'; c.lineJoin = 'round';
      line(c, [W.rib]); c.strokeStyle = INK; c.lineWidth = 7; c.stroke(); c.strokeStyle = '#3b3f46'; c.lineWidth = 4.4; c.stroke();
      c.save(); c.translate(-.8, -1.2); line(c, [W.rib]); c.strokeStyle = 'rgba(122,129,138,.55)'; c.lineWidth = 1.2; c.stroke(); c.restore();
      c.save(); trace(c, W.s); c.clip();
      c.fillStyle = '#24272c'; c.fillRect(b[0] - 4, b[1] - 4, b[2] - b[0] + 8, b[3] - b[1] + 8);
      for (const [dy, col] of [[-5, '#353940'], [-15, '#40444c']]) { c.save(); c.translate(0, dy); c.fillStyle = col; trace(c, W.s); c.fill(); c.restore(); }
      line(c, W.seams); c.lineWidth = 1.6; c.strokeStyle = '#25282d'; c.stroke();
      c.save(); c.translate(0, 1.6); line(c, W.seams); c.lineWidth = .8; c.strokeStyle = 'rgba(112,120,130,.4)'; c.stroke(); c.restore();
      for (const [x, y] of W.rivets) { c.fillStyle = '#1d1f23'; c.fillRect(x - 1.2, y - .4, 2.6, 2.6); c.fillStyle = '#5d636c'; c.fillRect(x - 1.3, y - 1.3, 2.3, 2.3); }
      for (const bl of W.rust) {
        c.fillStyle = 'rgba(112,62,34,.85)'; trace(c, bl.s); c.fill();
        c.save(); c.translate(bl.x, bl.y); c.scale(.45, .45); c.translate(-bl.x, -bl.y); c.fillStyle = 'rgba(66,34,19,.9)'; trace(c, bl.s); c.fill(); c.restore();
      }
      c.save(); c.translate(W.HOLE[0], W.HOLE[1]); trace(c, W.hole); c.fillStyle = '#0a0b0e'; c.fill(); ink(c, W.hole, 0, 1.4); c.restore();
      line(c, W.cracks); c.lineWidth = 1; c.strokeStyle = INK; c.stroke();
      c.save(); c.translate(0, 2); trace(c, W.s); c.strokeStyle = 'rgba(128,136,146,.45)'; c.lineWidth = 2; c.stroke(); c.restore();
      c.restore();
      ink(c, W.s, 0, 2.2);
    }
    // the hit: a few sparks off its right shoulder
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

    // ---------- the drone's soot, as drawDrone draws it ----------
    function orbitAt(D, k, i, t) {
      const P = DR.P[i % DR.P.length], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + t * (.8 + .25 * o) * dir, rx = 10 + o * 2.2 + (P.r1 - .5) * 1.6;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, -.3 + o * 1.05), s = DS * DSC * k;
      return [D[0] + q[0] * s, D[1] + q[1] * s];
    }
    function mote(c, g, p, s, light, heat) {
      const e = s * .25;
      c.fillStyle = '#1a1b1f'; c.fillRect(p[0] - s / 2 - e, p[1] - s / 2 - e, s + 2 * e, s + 2 * e);
      c.fillStyle = light ? '#606369' : '#4c4f55'; c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
      if (heat > .02) { g.fillStyle = css(pale(EMBER, .25 * Math.min(1, heat)), .95 * Math.min(1, heat)); g.fillRect(p[0] - s / 2, p[1] - s / 2, s, s); }
    }
    const lensOf = (D, A, k) => [D[0] + Math.cos(A) * 2.2 * DS * DSC * k, D[1] + Math.sin(A) * 2.2 * DS * DSC * k];

    // ---------- the candidates: think (a sign by the drone), sweep (the scan over the wreck), readout (left / max) ----------
    // X: { id, ph in|hold|out, e 0..1 in the phase, a, D drone, O centre, R half size, k scale, lens, t }
    // Y: { u s into the scan, W the wreck, lens, t }; Rd: { ph, e, a, val, prev, hk 0..1 since the hit, O, k, lens?, scene, D?, t }
    const shownOf = Rd => Rd.hk < .2 ? Rd.prev : Rd.val;
    const cands = [
      {
        id: 'pen', name: 'Перо', tin: .4, tout: .3, lines: true,
        idea: 'Язык окон дрона, как «Перо» у окон машин: голубой свет, два пера-луча из линзы чертят знак линия за линией, потом проступает мягкий фон; знак держится с лёгким гулом и строками, к нему тянутся два слабых луча; гаснет, стираясь обратно. Скан: перо обводит обломок, по нему один раз проходит строка; табличка открывается, как окно: перья обводят рамку, проступают цифры.',
        time: 'Знак чертится за 0,4 с · держится 1,5 с · стирается за 0,3 с · скан: обводка 1 с, табличка за 0,4 с, при ударе число вспыхивает.',
        think(g, c, X) {
          const k = X.k, P = place(GLYPH[X.id], X.O, X.R);
          const f = X.ph === 'in' ? sm(c01(X.e / .85)) : X.ph === 'out' ? 1 - sm(X.e) : 1;
          const gnd = X.ph === 'in' ? c01((X.e - .45) / .55) : X.ph === 'out' ? 1 - X.e : 1, bz = X.ph === 'hold' ? 1 - .15 * noise(X.t, 3) : 1;
          disc(g, X.O, X.R * 1.5, SIGN, .1 * gnd);
          const tr = pens(P, f);
          strokeP(g, tr.drawn, 3.4 * k, SIGN, .12 * bz);
          strokeP(g, tr.drawn, 1.25 * k, SIGN, .88 * bz);
          penHeads(g, X.lens, tr.heads, k);
          if (X.ph === 'hold' && X.lens) for (const s of [-1, 1]) strokeP(g, [[X.lens, [X.O[0] + s * X.R * .8, X.O[1] + X.R * .9]]], .5 * k, SIGN, .1 * bz);
        },
        sweep(g, c, Y) {
          const u = Y.u, OL = Y.W.OL;
          if (u < SC.sweep[0] || u >= SC.gone[1]) return;
          const f = sm(c01((u - .35) / 1)), keep = u < SC.show[0] ? 1 : u < SC.gone[0] ? .3 : .3 * (1 - c01((u - SC.gone[0]) / .4));
          const s = subP(OL, f);
          if (s) { strokeP(g, [s], 1.1, SIGN, .6 * keep); if (f < 1) penHeads(g, Y.lens, [s[s.length - 1]], 1); }
          const q = c01((u - .9) / .65), b = Y.W.box;
          if (q > 0 && q < 1) { g.save(); clipTo(g, OL); const y = lerp(b[1], b[3], q); strokeP(g, [[[b[0], y], [b[2], y]]], 1, SIGN, .35 * Math.sin(q * Math.PI)); g.restore(); }
        },
        readout(g, c, Rd) {
          const k = Rd.k, lay = layout(shownOf(Rd), Rd.O, k), b = lay.box, px = 7 * k, py = 5 * k;
          const C = [[b[0] - px, b[1] - py], [b[2] + px, b[1] - py], [b[2] + px, b[3] + py], [b[0] - px, b[3] + py]];
          const from = Rd.lens || [b[2] + 40, b[1] - 60]; let n = 0;
          C.forEach((p, i) => { if (Math.hypot(p[0] - from[0], p[1] - from[1]) < Math.hypot(C[n][0] - from[0], C[n][1] - from[1])) n = i; });
          const halves = [[C[n], C[(n + 1) % 4], C[(n + 2) % 4]], [C[n], C[(n + 3) % 4], C[(n + 2) % 4]]];
          const f = Rd.ph === 'in' ? sm(c01(Rd.e / .6)) : Rd.ph === 'out' ? 1 - sm(c01((Rd.e - .3) / .7)) : 1;
          const fill = Rd.ph === 'in' ? c01((Rd.e - .5) / .5) : Rd.ph === 'out' ? 1 - c01(Rd.e / .4) : 1;
          const bz = Rd.ph === 'hold' ? 1 - .12 * noise(Rd.t, 5) : 1, flare = Rd.hk < 1 ? 1 - Rd.hk : 0, drop = Rd.hk < .25 ? .35 + .65 * Math.abs(Math.cos(Rd.hk * 40)) : 1;
          rr(g, C[0][0], C[0][1], C[1][0] - C[0][0], C[2][1] - C[0][1], 1.5 * k); g.fillStyle = css(SIGN, .07 * fill); g.fill();
          const tr = pens(halves, f);
          strokeP(g, tr.drawn, .8 * k, SIGN, .7 * bz);
          penHeads(g, Rd.lens, tr.heads, k);
          if (Rd.ph === 'hold' && Rd.lens) { const m = [C[n], C[(n + 1) % 4], C[(n + 3) % 4]], o = Math.hypot(m[1][0] - from[0], m[1][1] - from[1]) < Math.hypot(m[2][0] - from[0], m[2][1] - from[1]) ? m[1] : m[2]; strokeP(g, [[Rd.lens, C[n]], [Rd.lens, o]], .5 * k, SIGN, .1 * bz); }
          const V = polysOf(lay, 'val'), col = mix3(SIGN, pale(SIGN, .6), flare);
          strokeP(g, V, 3 * k, SIGN, .12 * fill * bz * drop);
          strokeP(g, V, 1.2 * k, col, .92 * fill * bz * drop);
          strokeP(g, polysOf(lay, 'rest'), .85 * k, SIGN, .55 * fill * bz);
        },
      },
      {
        id: 'soot', name: 'Угли', tin: .5, tout: .45,
        idea: 'Знак из самого дрона: сажа срывается с его орбит и собирается в рисунок, в нём тлеют угли; потом осыпается обратно в облако. Не свет, а вещество — как его тело. Скан: струйка сажи стекает на обломок и бежит по его краю, оставляя тлеющий след; след поднимается и складывается в цифры, при ударе число разлетается и собирается заново.',
        time: 'Сажа собирается за 0,5 с · держится 1,5 с · осыпается за 0,45 с · скан: бег по краю 0,85 с, цифры за 0,4 с.',
        think(g, c, X) {
          const k = X.k, U = DOTI[X.id], s0 = 1.7 * k;
          strokeP(g, place(GLYPH[X.id], X.O, X.R), 3 * k, EMBER, .06 * X.a);
          U.forEach((u, i) => {
            const P = [X.O[0] + u[0] * X.R + .3 * k * Math.sin(X.t * 2.3 + i), X.O[1] + u[1] * X.R + .3 * k * Math.cos(X.t * 1.9 + i * 1.7)];
            let p = P, heat = hsh(i + 3) < .5 ? .4 + .35 * Math.sin(X.t * 9 + i * 2.1) : .08;
            if (X.ph !== 'hold') {
              const S = orbitAt(X.D, k, i * 7 + 3, X.t), ctl = [lerp(S[0], P[0], .5) + (hsh(i + 1) - .5) * 14 * k, Math.min(S[1], P[1]) - 6 * k];
              if (X.ph === 'in') { const q = sm(c01((X.e - hsh(i) * .45) / .55)); p = bez(S, ctl, P, q); heat *= q; }
              else { const q = sm(c01((X.e - hsh(i + 50) * .4) / .6)); p = bez(P, ctl, S, q); heat *= 1 - q; }
            }
            mote(c, g, p, s0 * (.8 + .4 * hsh(i + 9)), hsh(i + 7) < .5, heat);
            if (heat > .3 && hsh(i + 13) < .18) halo(g, p[0], p[1], 3.2 * k, EMBER, .22 * heat);
          });
        },
        sweep(g, c, Y) {
          const u = Y.u, OL = Y.W.OL; if (u < SC.sweep[0] || u >= 1.9) return;
          const F = c01((u - .65) / .85), s0 = 1.7;
          if (u < SC.show[0]) OLD.forEach((p, m) => { const f = m / M0; if (F < f || F <= 0) return; mote(c, g, p, s0 * (.8 + .4 * hsh(m + 3)), hsh(m + 7) < .5, .15 + .85 * Math.exp(-(F - f) * 8)); });
          for (let j = 0; j < 18; j++) {
            const d0 = .35 + j * .015, S = orbitAt(Y.D, 1, j * 11 + 5, Y.t);
            let p;
            if (u < d0) continue;
            if (u < d0 + .28) { const q = sm((u - d0) / .28), E = OL[0]; p = bez(S, [lerp(S[0], E[0], .5), Math.min(S[1], E[1]) - 24], E, q); }
            else if (u < SC.show[0]) p = tip(OL, Math.max(1e-4, F - j * .02));
            else { const q = sm(c01((u - SC.show[0]) / .35)), E = tip(OL, Math.max(1e-4, 1 - j * .02)); if (q >= 1) continue; p = bez(E, [lerp(E[0], S[0], .5), Math.min(S[1], E[1]) - 30], S, q); }
            mote(c, g, p, 1.8 * (.8 + .4 * hsh(j + 31)), j % 2 === 0, hsh(j + 17) < .4 ? .7 : .1);
          }
        },
        readout(g, c, Rd) {
          const k = Rd.k, s0 = 1.6 * k, t = Rd.t, home = Rd.scene ? Rd.D : [Rd.O[0], Rd.O[1] + 60 * k];
          udots(Rd.val, Rd.O, k).forEach((d, m) => {
            let p = [d.p[0] + .25 * k * Math.sin(t * 2.1 + m), d.p[1] + .25 * k * Math.cos(t * 1.7 + m * 1.3)];
            let heat = d.part === 'val' ? .45 + .3 * Math.sin(t * 9 + m * 2.3) : .14 + .1 * Math.sin(t * 7 + m);
            if (Rd.ph === 'in') {
              const q = sm(c01((Rd.e - hsh(m) * .4) / .6)), src = Rd.scene && m < M0 ? OLD[m] : [Rd.O[0] + (hsh(m + 5) - .5) * 70 * k, Rd.O[1] + 40 * k];
              p = bez(src, [lerp(src[0], p[0], .5), Math.min(src[1], p[1]) - 10 * k], p, q); heat *= .5 + .5 * q;
            } else if (Rd.ph === 'out') { const q = sm(c01((Rd.e - hsh(m + 2) * .35) / .65)); p = bez(p, [lerp(p[0], home[0], .5), Math.min(p[1], home[1]) - 20 * k], home, q); heat *= 1 - q; }
            if (d.part === 'val' && Rd.hk < 1) {
              const q = sm(c01((Rd.hk - .2) / .6)); if (q <= 0) return;
              const a = hsh(m + 11) * TAU; p = lerp2([p[0] + Math.cos(a) * 12 * k, p[1] + Math.sin(a) * 12 * k], p, q); heat += .8 * (1 - Rd.hk);
            }
            mote(c, g, p, s0 * (.8 + .4 * hsh(m + 3)), hsh(m + 7) < .5, heat);
          });
          if (Rd.hk < 1) udots(Rd.prev, Rd.O, k).forEach((d, m) => {
            const q = sm(c01(Rd.hk / .4)); if (d.part !== 'val' || q >= 1) return;
            const a = hsh(m + 11) * TAU, p = [d.p[0] + Math.cos(a) * 16 * k * q, d.p[1] + Math.sin(a) * 16 * k * q + 10 * k * q * q];
            mote(c, g, p, s0 * (1 - .4 * q), hsh(m + 7) < .5, .9 * (1 - q));
          });
        },
      },
      {
        id: 'ink', name: 'Тушь', tin: .45, tout: .45,
        idea: 'Знак как рисунок самой игры, только светом: пятно акварели цвета смысла (холод — синий, жара — рыжий, воздух — бирюза, опасность — красный) и поверх живая линия пера, неровная, с нажимом и отрывами. Не мерцает и не гудит; гаснет, как высыхает: сперва линия, потом пятно. Скан: перо обводит обломок, его заливает пятно цвета прочности; цифры написаны от руки, при ударе старое число зачёркивается.',
        time: 'Пятно расплывается, перо пишет за 0,45 с · держится 1,5 с · высыхает за 0,45 с · скан: обводка 1 с, надпись за 0,4 с.',
        mean: { cold: [110, 160, 225], heat: [230, 120, 60], air: [100, 200, 190], meteor: [205, 120, 70], lightning: [225, 200, 90], hunted: [210, 62, 56], curious: [150, 190, 120] },
        think(g, c, X) {
          const k = X.k, col = this.mean[X.id];
          let ws = 1, wa = 1, ia = 1, f = 1;
          if (X.ph === 'in') { ws = lerp(.55, 1, sm(c01(X.e / .5))); wa = sm(c01(X.e / .4)); f = sm(c01((X.e - .15) / .85)); }
          else if (X.ph === 'out') { ia = 1 - sm(c01(X.e / .6)); ws = 1 + .12 * sm(X.e); wa = 1 - sm(c01((X.e - .2) / .8)); }
          else ws = 1 + .02 * Math.sin(X.t * 1.3);
          wash(g, WB[X.id], WB2[X.id], X.O, X.R * ws, X.R * ws, col, wa);
          const h = drawInk(g, INKI[X.id], X.O, X.R, f, CREAM, .9 * ia, 1.5 * k);
          if (h) { g.fillStyle = css(CREAM, .9); g.beginPath(); g.arc(h[0], h[1], .9 * k, 0, TAU); g.fill(); }
        },
        sweep(g, c, Y) {
          const u = Y.u, W = Y.W, b = W.box; if (u < SC.sweep[0] || u >= SC.gone[1]) return;
          const f = sm(c01((u - .35) / 1)), keep = u < SC.show[0] ? 1 : u < SC.gone[0] ? .45 : .45 * (1 - c01((u - SC.gone[0]) / .4));
          const wa = sm(c01((u - 1) / .5)) * keep, val = u < SC.hit ? VAL[0] : VAL[1], col = u < SC.hit ? fracCol(VAL[0]) : mix3(fracCol(VAL[0]), fracCol(val), sm(c01((u - SC.hit) / .6)));
          if (wa > .003) { g.save(); clipTo(g, W.OL); wash(g, WBW, WBW2, W.C, (b[2] - b[0]) * .46, (b[3] - b[1]) * .6, col, .9 * wa); g.restore(); }
          const h = drawInk(g, [WINK], [0, 0], 1, f, CREAM, .75 * keep, 1.5);
          if (h) { g.fillStyle = css(CREAM, .9); g.beginPath(); g.arc(h[0], h[1], 1, 0, TAU); g.fill(); }
        },
        readout(g, c, Rd) {
          const k = Rd.k, lay = layout(Rd.val, Rd.O, k), b = lay.box;
          let wa = 1, ws = 1, f = 1, ia = 1;
          if (Rd.ph === 'in') { wa = sm(c01(Rd.e / .4)); ws = lerp(.6, 1, sm(c01(Rd.e / .5))); f = sm(c01((Rd.e - .25) / .75)); }
          else if (Rd.ph === 'out') { ia = 1 - sm(c01(Rd.e / .6)); ws = 1 + .1 * sm(Rd.e); wa = 1 - sm(c01((Rd.e - .2) / .8)); }
          const col = Rd.hk < 1 ? mix3(fracCol(Rd.prev), fracCol(Rd.val), sm(Rd.hk)) : fracCol(Rd.val);
          wash(g, WBW, WBW2, Rd.O, ((b[2] - b[0]) / 2 + 12 * k) * ws, ((b[3] - b[1]) / 2 + 9 * k) * ws, col, .85 * wa);
          if (Rd.hk < 1) {   // the old number struck through, then the new one written in its place
            const old = layout(Rd.prev, Rd.O, k), oa = 1 - sm(c01((Rd.hk - .35) / .2)), vEnd = old.items.filter(it => it.part === 'val').pop();
            const x0 = old.items[0].x - 2 * k, x1 = vEnd.x + .62 * vEnd.s + 2 * k;
            inkText(g, old, 'val', 1, CREAM, .92 * oa, 1.4 * k);
            drawInk(g, STRIKE, [x0, Rd.O[1] + .5 * k], x1 - x0, c01(Rd.hk / .35), CREAM, .95 * oa, 1.4 * k);
            inkText(g, lay, 'val', sm(c01((Rd.hk - .5) / .5)), CREAM, .92, 1.4 * k);
          } else inkText(g, lay, 'val', f, CREAM, .92 * ia, 1.4 * k);
          inkText(g, lay, 'rest', f, CREAM, .6 * ia, 1 * k);
        },
      },
      {
        id: 'lens', name: 'Линза', tin: .25, tout: .2,
        idea: 'Знак в овале, как в линзе дрона: открывается из щели, как зрачок, и так же закрывается; к нему от линзы тянется тонкий конус. Цвет говорит, насколько срочно: янтарь — предупреждение, красный — опасность, голубой — настроение; линза дрона светится тем же цветом. Скан: к обломку сходятся три кольца, как наводка на резкость, и одно остаётся; цифры в капсуле, её цвет — по прочности.',
        time: 'Открывается из щели за 0,25 с · держится 1,5 с · закрывается за 0,2 с · скан: наводка 1 с, капсула за 0,4 с, при ударе вздрагивает и меняет цвет.',
        cls: { cold: AMBER, heat: AMBER, air: AMBER, meteor: RED, lightning: RED, hunted: RED, curious: SIGN },
        think(g, c, X) {
          const k = X.k, col = this.cls[X.id], open = X.ph === 'in' ? sm(X.e) : X.ph === 'out' ? 1 - sm(X.e) : 1, oy = Math.max(.06, open), a = X.a;
          const rx = X.R, ry = X.R * 1.25 * oy;
          if (X.lens) { coneTo(g, X.lens, [X.O[0], X.O[1] + ry * .9], X.R * .45, .3 * a, .05 * a, col); g.fillStyle = css(col, .8 * a); g.beginPath(); g.ellipse(X.lens[0], X.lens[1], 1.3 * k, 1.3 * k, 0, 0, TAU); g.fill(); halo(g, X.lens[0], X.lens[1], 5 * k, col, .35 * a); }
          g.fillStyle = css(col, .14 * Math.min(1, a * 1.5)); g.beginPath(); g.ellipse(X.O[0], X.O[1], rx, ry, 0, 0, TAU); g.fill();
          g.lineWidth = 1.1 * k; g.strokeStyle = css(col, .85 * Math.min(1, a * 1.5)); g.stroke();
          g.lineWidth = .5 * k; g.strokeStyle = css(col, .3 * a); g.beginPath(); g.ellipse(X.O[0], X.O[1], rx * .84, ry * .86, 0, 0, TAU); g.stroke();
          if (open > .45) {
            const ga = c01((open - .45) / .4);
            g.save(); g.beginPath(); g.ellipse(X.O[0], X.O[1], rx, ry, 0, 0, TAU); g.clip();
            const P = place(GLYPH[X.id], X.O, X.R * .66, X.R * .66 * oy);
            strokeP(g, P, 2.8 * k, col, .14 * ga); strokeP(g, P, 1.2 * k, pale(col, .25), .92 * ga);
            g.restore();
          }
          const h = X.t * .9; g.lineWidth = 1.1 * k; g.strokeStyle = css(pale(col, .45), .35 * a); g.beginPath(); g.ellipse(X.O[0], X.O[1], rx, ry, 0, h, h + .6); g.stroke();
        },
        sweep(g, c, Y) {
          const u = Y.u, W = Y.W, b = W.box, hx = (b[2] - b[0]) / 2, hy = (b[3] - b[1]) / 2;
          if (u >= SC.gone[1]) return;
          const tint = c01(u / .3) * (1 - c01((u - SC.gone[0]) / .4));
          if (Y.lens) { g.fillStyle = css(TEAL, .8 * tint); g.beginPath(); g.arc(Y.lens[0], Y.lens[1], 1.3, 0, TAU); g.fill(); halo(g, Y.lens[0], Y.lens[1], 5, TEAL, .35 * tint); }
          if (u < SC.sweep[0]) return;
          const cone = sm(c01((u - .35) / .25)) * (u < 1.45 ? 1 : lerp(1, .35, c01((u - 1.45) / .3))) * (1 - c01((u - SC.gone[0]) / .4));
          coneTo(g, Y.lens, W.C, 30, .16 * cone, .03 * cone, TEAL);
          for (let j = 0; j < 3; j++) {
            const q = c01((u - .45 - j * .25) / .55); if (q <= 0 || q >= 1) continue;
            const s = lerp(1.7, 1.02, sm(q)); g.lineWidth = 1; g.strokeStyle = css(TEAL, .5 * Math.sin(q * Math.PI)); g.beginPath(); g.ellipse(W.C[0], W.C[1], hx * 1.18 * s, hy * 1.3 * s, 0, 0, TAU); g.stroke();
          }
          const lock = c01((u - 1.4) / .2) * (1 - c01((u - SC.gone[0]) / .4));
          if (lock > 0) {
            g.lineWidth = .9; g.strokeStyle = css(TEAL, .22 * lock); g.beginPath(); g.ellipse(W.C[0], W.C[1], hx * 1.2, hy * 1.33, 0, 0, TAU); g.stroke();
            g.strokeStyle = css(TEAL, .5 * lock); g.lineWidth = 1.2; g.beginPath();
            for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const x = W.C[0] + dx * hx * 1.2, y = W.C[1] + dy * hy * 1.33; g.moveTo(x - dx * 6, y - dy * 6); g.lineTo(x + dx * 3, y + dy * 3); }
            g.stroke();
          }
        },
        readout(g, c, Rd) {
          const k = Rd.k, lay = layout(shownOf(Rd), Rd.O, k), b = lay.box, w = b[2] - b[0] + 16 * k, h = b[3] - b[1] + 10 * k;
          const open = Rd.ph === 'in' ? sm(Rd.e) : Rd.ph === 'out' ? 1 - sm(Rd.e) : 1, oy = Math.max(.06, open), pulse = Rd.hk < 1 ? (1 - Rd.hk) * Math.sin(Rd.hk * Math.PI) : 0;
          const col = Rd.hk < 1 ? mix3(fracCol(Rd.prev), fracCol(Rd.val), sm(c01(Rd.hk / .5))) : fracCol(Rd.val), sc = 1 + .08 * pulse, a = Math.min(1, Rd.a * 1.5);
          const W = w * sc, H = h * oy * sc, x0 = Rd.O[0] - W / 2, y0 = Rd.O[1] - H / 2, r = Math.min(W, H) / 2 - .01;
          if (Rd.lens) coneTo(g, Rd.lens, [Rd.O[0], Rd.O[1] + H / 2], 6 * k, .25 * a, .05 * a, col);
          rr(g, x0, y0, W, H, r); g.fillStyle = css(col, .13 * a); g.fill(); g.lineWidth = 1.1 * k; g.strokeStyle = css(col, (.85 + .15 * pulse) * a); g.stroke();
          if (H > 5 * k) { rr(g, x0 + 2.2 * k, y0 + 2.2 * k, W - 4.4 * k, H - 4.4 * k, Math.max(.5, r - 2.2 * k)); g.lineWidth = .5 * k; g.strokeStyle = css(col, .3 * a); g.stroke(); }
          if (open > .45) {
            const ga = c01((open - .45) / .4), V = polysOf(lay, 'val');
            g.save(); rr(g, x0, y0, W, H, r); g.clip(); g.translate(Rd.O[0], Rd.O[1]); g.scale(sc, oy * sc); g.translate(-Rd.O[0], -Rd.O[1]);
            strokeP(g, V, 2.8 * k, col, .14 * ga); strokeP(g, V, 1.2 * k, pale(col, .25 + .5 * pulse), .95 * ga);
            strokeP(g, polysOf(lay, 'rest'), .85 * k, col, .6 * ga);
            g.restore();
          }
        },
      },
      {
        id: 'lamp', name: 'Фонарь', tin: .3, tout: .3,
        idea: 'Не голограмма, а фонарь дрона: тёплое пятно света над ним, а знак — тень в пятне, как через трафарет. Самый тихий и тёплый: никакого голубого, только лампа, которой дрон и так светит. Скан: луч фонаря проходит по обломку, как прожектор, и поднимается над ним пятном, в котором цифры — тенью; при ударе фонарь мигает.',
        time: 'Пятно выходит из линзы за 0,3 с · держится 1,5 с, чуть дрожа · уходит в линзу за 0,3 с · скан: луч по обломку 0,85 с, пятно с цифрами за 0,4 с.',
        think(g, c, X) {
          const k = X.k, open = X.ph === 'in' ? sm(X.e) : X.ph === 'out' ? 1 - sm(X.e) : 1, fl = 1 - .07 * noise(X.t, 9);
          const C = X.lens ? lerp2(X.lens, X.O, open) : X.O, r = X.R * 1.35 * lerp(.25, 1, open), L = open * fl;
          if (X.lens) coneTo(g, X.lens, C, r * .8, .2 * L, .05 * L, LAMP);
          pool(g, C, r, r, L);
          const cut = c01((open - .35) / .65);
          if (cut > 0) { g.globalCompositeOperation = 'destination-out'; strokeP(g, place(GLYPH[X.id], C, X.R * .78 * r / (X.R * 1.35)), 2.3 * k, [0, 0, 0], .95 * cut); g.globalCompositeOperation = 'lighter'; }
        },
        sweep(g, c, Y) {
          const u = Y.u; if (u < SC.sweep[0] || u >= SC.show[0]) return;
          const a = sm(c01((u - .35) / .25)), P = lerp2(S0, S1, sm(c01((u - .6) / .85))), fl = 1 - .06 * noise(Y.t, 5);
          if (Y.lens) coneTo(g, Y.lens, P, SPOT_R * .85, .18 * a * fl, .05 * a * fl, LAMP);
          g.save(); clipTo(g, Y.W.OL); disc(g, P, SPOT_R, LAMP, .34 * a * fl); g.restore();
          disc(g, P, SPOT_R * 1.3, LAMP, .06 * a * fl);
        },
        readout(g, c, Rd) {
          const k = Rd.k, lay = layout(shownOf(Rd), Rd.O, k), b = lay.box, rx = (b[2] - b[0]) / 2 + 14 * k, ry = (b[3] - b[1]) / 2 + 9 * k;
          let C = Rd.O, sx = rx, sy = ry, lit = 1, cut = 1;
          if (Rd.ph === 'in') { const e = sm(Rd.e), src = Rd.scene ? S1 : [Rd.O[0], Rd.O[1] + 40 * k]; C = lerp2(src, Rd.O, e); sx = lerp(SPOT_R * k, rx, e); sy = lerp(SPOT_R * k, ry, e); cut = c01((Rd.e - .5) / .5); }
          if (Rd.ph === 'out') { const e = sm(Rd.e), to = Rd.lens || Rd.O; C = lerp2(Rd.O, to, e); sx = rx * (1 - .9 * e); sy = ry * (1 - .9 * e); lit = 1 - e; cut = 1 - c01(Rd.e / .5); }
          const dip = Rd.hk < 1 ? .75 * Math.max(0, Math.sin(Rd.hk * Math.PI * 4)) * (1 - Rd.hk) : 0;
          lit *= (1 - dip) * (1 - .06 * noise(Rd.t, 7));
          if (Rd.lens) coneTo(g, Rd.lens, C, Math.min(sx, sy) * .9, .2 * lit, .05 * lit, LAMP);
          pool(g, C, sx, sy, lit);
          if (cut > 0) {
            g.globalCompositeOperation = 'destination-out';
            strokeP(g, polysOf(lay, 'val'), 2 * k, [0, 0, 0], .95 * cut); strokeP(g, polysOf(lay, 'rest'), 1.4 * k, [0, 0, 0], .8 * cut);
            g.globalCompositeOperation = 'lighter';
          }
        },
      },
    ];
    // the lamp's pool: warm, flat in the middle, soft at its edge, a faint rim
    function pool(g, C, sx, sy, L) {
      if (L <= .003 || sx < .5 || sy < .5) return;
      g.save(); g.translate(C[0], C[1]); g.scale(sx, sy);
      const gr = g.createRadialGradient(0, 0, 0, 0, 0, 1); gr.addColorStop(0, css(LAMP, .42 * L)); gr.addColorStop(.7, css(LAMP, .32 * L)); gr.addColorStop(.92, css(LAMP, .14 * L)); gr.addColorStop(1, css(LAMP, 0));
      g.fillStyle = gr; g.beginPath(); g.arc(0, 0, 1, 0, TAU); g.fill(); g.restore();
      g.lineWidth = .8; g.strokeStyle = css(LAMP, .16 * L); g.beginPath(); g.ellipse(C[0], C[1], sx * .95, sy * .95, 0, 0, TAU); g.stroke();
    }

    // ---------- the clock ----------
    function planOf(mode) {
      const ids = mode === 'thoughts' ? ICONS : mode === 'scan' ? ['scan'] : ORDER;
      let t = 0; return ids.map(id => { const q = { id, t0: t, len: id === 'scan' ? SCAN : SLOT }; t += q.len; return q; });
    }
    function nowOf(T) {
      const P = planOf(OPT.show), L = P[P.length - 1].t0 + P[P.length - 1].len, u = ((T.time - T.t0) % L + L) % L;
      const q = P.find(p => u >= p.t0 && u < p.t0 + p.len) || P[0];
      return { id: q.id, u: Math.max(0, u - q.t0) };
    }
    function thoughtOf(cand, id, u) {
      if (u < cand.tin) return { id, ph: 'in', e: u / cand.tin, a: u / cand.tin };
      if (u < cand.tin + HOLD) return { id, ph: 'hold', e: (u - cand.tin) / HOLD, a: 1 };
      if (u < cand.tin + HOLD + cand.tout) { const e = (u - cand.tin - HOLD) / cand.tout; return { id, ph: 'out', e, a: 1 - e }; }
      return null;
    }
    function readOf(u) {
      if (u < SC.show[0] || u >= SC.gone[1]) return null;
      const val = u < SC.hit ? VAL[0] : VAL[1], hk = u < SC.hit ? 1 : c01((u - SC.hit) / .6), base = { val, prev: VAL[0], hk };
      if (u < SC.show[1]) { const e = (u - SC.show[0]) / (SC.show[1] - SC.show[0]); return { ...base, ph: 'in', e, a: e }; }
      if (u < SC.gone[0]) return { ...base, ph: 'hold', e: 0, a: 1 };
      const e = (u - SC.gone[0]) / (SC.gone[1] - SC.gone[0]); return { ...base, ph: 'out', e, a: 1 - e };
    }
    const CUR_REST = [560, 190], CUR_AT = [WRECK.C[0] + 14, WRECK.C[1] - 6];
    function cursorOf(st) {
      if (!st.scan || st.u >= SC.gone[1]) return null;
      const u = st.u;
      if (u < SC.arrive) return { p: lerp2(CUR_REST, CUR_AT, sm(u / SC.arrive)), a: c01(u / .12) };
      if (u < SC.gone[0]) return { p: CUR_AT, a: 1 };
      return { p: lerp2(CUR_AT, CUR_REST, sm((u - SC.gone[0]) / .4)), a: 1 - c01((u - SC.gone[0] - .2) / .2) };
    }

    // ---------- the light buffer: everything a candidate lights, added onto the picture; «Перо» with the holograms' scanlines ----------
    function lightStart(T) {
      const g = T.hbg; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.hb.width, T.hb.height);
      g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalCompositeOperation = 'lighter'; g.lineCap = 'round'; g.lineJoin = 'round';
      return g;
    }
    function lightEnd(T, lines) {
      const g = T.hbg;
      if (lines) { g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'destination-out'; g.fillStyle = T.pat; g.fillRect(0, 0, T.hb.width, T.hb.height); }
      g.globalCompositeOperation = 'source-over';
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0); c.restore();
    }
    // the loupe: the drone and its sign ×2.5, or the readout ×2 while the scan shows it
    const LP = { x: 786, y: 12, w: 200, h: 218 };
    function loupe(T) {
      if (!OPT.loupe || !T.st) return false;
      const c = T.c, st = T.st, cand = T.cand, h = T.me, t = T.time + h.seed;
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.fillStyle = '#05070b'; c.fill(); c.strokeStyle = 'rgba(140,150,165,.35)'; c.lineWidth = 1; c.stroke();
      c.save(); rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip();
      const D = [LP.x + LP.w / 2, LP.y + 170 + Math.sin(T.time * 1.7 + h.seed) * 1.5 * LOUPE_K];
      const g = lightStart(T);
      if (st.th) {
        const A = -Math.PI / 2, k = LOUPE_K;
        drawDrone(c, D[0], D[1], A, 0, t, DSC * k);
        cand.think(g, c, { ...st.th, D, O: [D[0] + OFF[0] * k, D[1] + OFF[1] * k], R: RI * k, k, lens: lensOf(D, A, k), t });
      } else if (st.rd) cand.readout(g, c, { ...st.rd, O: [LP.x + LP.w / 2, LP.y + LP.h / 2], k: LOUPE_RK, scene: false, t });
      else drawDrone(c, D[0], D[1], Math.atan2(WRECK.C[1] - h.d.p[1], WRECK.C[0] - h.d.p[0]), 0, t, DSC * LOUPE_K);
      lightEnd(T, cand.lines);
      c.restore();
      c.font = '11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(200,205,214,.55)';
      c.fillText(st.rd ? 'лупа ×2' : 'лупа ×2,5', LP.x + 8, LP.y + LP.h - 8);
      return true;
    }

    return {
      cands, expose: { LOOP, SC, READ_AT, BOX: WRECK.box, M0 },
      init(T) { T.me.face = -1; T.t0 = 0; T.showWas = OPT.show; T.st = null; },
      onScale(T) {
        const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S);
        T.hb = cv; T.hbg = cv.getContext('2d');
        const p = document.createElement('canvas'); p.width = 1; p.height = 2; const pg = p.getContext('2d'); pg.fillStyle = 'rgba(0,0,0,.35)'; pg.fillRect(0, 1, 1, 1);
        T.pat = T.hbg.createPattern(p, 'repeat');
      },
      // the hero stands left of centre, facing the wreck; the loop says what the drone shows; the head follows the scan
      step(T, dt) {
        const me = T.me; me.x = HERO_X;
        if (T.showWas !== OPT.show) { T.showWas = OPT.show; T.t0 = T.time; }
        const n = nowOf(T), st = { id: n.id, u: n.u, scan: n.id === 'scan', th: null, rd: null };
        if (st.scan) st.rd = readOf(n.u); else st.th = thoughtOf(T.cand, n.id, n.u);
        T.st = st;
        if (!T.real) {
          const look = st.scan && n.u < SC.gone[1] ? (st.rd ? READ_AT : WRECK.C) : null, nk = toS(me, NECK());
          turnHead(me, look ? headTurn((look[0] - nk[0]) * me.face, look[1] - nk[1]) : 0, dt);
        }
      },
      aim(T, h) {
        const st = T.st; if (!st) return null;
        if (st.th) return [h.d.p[0] + OFF[0], h.d.p[1] + OFF[1]];
        if (st.scan && st.u >= SC.sweep[0] && st.u < SC.gone[1]) return st.rd ? READ_AT : WRECK.C;
        return null;
      },
      back(T) { drawBack(T); drawWreck(T.c); if (T.st && T.st.scan) sparks(T.c, T.st.u); },
      overDrone(T, h) {
        const st = T.st, cand = T.cand, c = T.c, D = h.d.p.slice(), lensP = lensAt(h.d), t = T.time + h.seed;
        const dm = T.dm = { id: st.id, u: st.u, scan: st.scan, th: null, rd: null, loupe: false };
        const g = lightStart(T);
        if (st.th) {
          const X = { ...st.th, D, O: [D[0] + OFF[0], D[1] + OFF[1]], R: RI, k: 1, lens: lensP, t };
          cand.think(g, c, X); dm.th = { ph: X.ph, O: X.O, D };
        }
        if (st.scan) {
          cand.sweep(g, c, { u: st.u, W: WRECK, lens: lensP, t, D });
          if (st.rd) { cand.readout(g, c, { ...st.rd, O: READ_AT, k: 1, lens: lensP, scene: true, D, t }); dm.rd = { ph: st.rd.ph, val: st.rd.val, O: READ_AT }; }
        }
        lightEnd(T, cand.lines);
      },
      screen(T) {
        const cur = T.st && cursorOf(T.st);
        if (cur && cur.a > .003) { T.c.globalAlpha = cur.a; drawCursor(T.c, cur.p[0], cur.p[1]); T.c.globalAlpha = 1; }
        if (T.dm) T.dm.loupe = loupe(T);
      },
      hud(T) {
        const st = T.st; if (!st) return '';
        if (!st.scan) return st.th ? `мысль: ${SAY[st.id]}` : 'мысль погасла';
        if (st.u < SC.sweep[0]) return 'скан: указатель идёт на обломок';
        if (st.u < SC.show[0]) return 'скан: дрон читает обломок';
        if (st.rd) return `скан: прочность ${st.rd.val} из ${MAX}${st.u >= SC.hit && st.u < SC.hit + .8 ? ' · удар' : ''}`;
        return 'скан: указатель ушёл';
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    hold: [['const SLOT = 2.5, HOLD = 1.5', 'const SLOT = 2.5, HOLD = .7']],
    far: [['OFF = [0, -44]', 'OFF = [-130, -44]']],
    value: [['const val = u < SC.hit ? VAL[0] : VAL[1], hk', 'const val = VAL[0], hk']],
    skip: [["'scan', 'air', 'meteor', 'lightning', 'hunted'", "'scan', 'air', 'meteor', 'lightning'"]],
  },

  // one whole loop: every sign in order, each held 1.5 s by the drone, the scan between with its readout over the wreck's top,
  // the value before and after the hit; then only thoughts, only the scan, the loupe off, slow, a pause, a real pointer; a pick
  check(K) {
    const X = K.X, SEQ = ['cold', 'heat', 'scan', 'air', 'meteor', 'lightning', 'hunted', 'curious'], SIGNS = SEQ.filter(s => s !== 'scan'), HOLD_S = 1.5;
    const L = Math.round(X.LOOP * 60), A = L + 30, B = A + 700, C = B + 420, E = C + 460;
    K.at(A, () => K.seg('showMode', 1));
    K.at(B, () => K.seg('showMode', 2));
    K.at(C, () => { K.seg('showMode', 0); K.seg('loupeMode', 1); K.seg('speedMode', 1); });
    K.at(C + 200, () => { K.seg('speedMode', 0); K.seg('playMode', 1); });
    K.at(C + 260, () => { K.seg('playMode', 0); K.seg('loupeMode', 0); K.ptrAll([300, 120]); });
    K.at(C + 330, () => K.ptrAll(null));
    K.at(E - 30, () => K.click(3, '.pick'));
    let st = '';
    K.at(E - 10, () => { st = K.byId.status.textContent; K.byId.copy.listeners.click[0](); });
    const per = K.tiles.map(() => ({ seq: [], hold: {}, vals: new Set(), near: 0, thOnly: 0, scOnly: 0, lOn: 0, lOff: 0, paused: 0, lastT: -1 }));
    const topMid = (X.BOX[0] + X.BOX[2]) / 2;
    K.run(E, (t, i) => {
      const q = per[K.tiles.indexOf(t)], d = t.dm; if (!d) return;
      const id = t.cand.id;
      if (i < L - 1) {
        if (q.seq[q.seq.length - 1] !== d.id) q.seq.push(d.id);
        if (d.th && d.th.ph === 'hold') { const h = q.hold[d.id] || (q.hold[d.id] = [t.time, t.time]); h[1] = t.time; }
      }
      if (d.th && d.th.ph === 'hold') {
        const D = d.th.D, O = d.th.O, far = Math.hypot(O[0] - D[0], O[1] - D[1]);
        if (far > 60 || O[1] > D[1] - 20) K.fail(`${id}: ${d.id} at ${O.map(Math.round)}, the drone at ${D.map(Math.round)}, frame ${i}`); else q.near++;
      }
      if (d.rd && d.rd.ph === 'hold') {
        const want = d.u < X.SC.hit ? 340 : 230;
        if (d.rd.val !== want) K.fail(`${id}: the readout shows ${d.rd.val} at ${d.u.toFixed(2)} s, want ${want}`); else q.vals.add(d.rd.val);
        if (Math.abs(d.rd.O[0] - topMid) > .01 || d.rd.O[1] >= X.BOX[1]) K.fail(`${id}: the readout at ${d.rd.O} is not over the wreck's top ${topMid}, ${X.BOX[1]}`);
      }
      if (i > A + 1 && i < B) { if (d.scan) K.fail(`${id}: a scan with only thoughts on, frame ${i}`); else if (d.th) q.thOnly++; }
      if (i > B + 1 && i < C) { if (d.th) K.fail(`${id}: a thought with only the scan on, frame ${i}`); else if (d.rd) q.scOnly++; }
      if (i < C) { if (d.loupe) q.lOn++; } else if (i > C + 1 && i < C + 260) { if (d.loupe) K.fail(`${id}: the loupe drawn while off, frame ${i}`); else q.lOff++; }
      if (i > C + 201 && i < C + 259) { if (q.lastT >= 0 && t.time !== q.lastT) K.fail(`${id}: time ran while paused`); q.paused++; }
      q.lastT = t.time;
    });
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id, holds = SIGNS.map(s => q.hold[s] ? q.hold[s][1] - q.hold[s][0] + 1 / 60 : 0);
      console.log(`${id}: order ${q.seq.join(' ')} | held ${holds.map(v => v.toFixed(3)).join(' ')} s | values ${[...q.vals].join(' ')} | near ${q.near} only-thoughts ${q.thOnly} only-scan ${q.scOnly} loupe ${q.lOn}/${q.lOff} paused ${q.paused}`);
      need(q.seq.slice(0, SEQ.length).join() === SEQ.join(), `${id}: the signs and the scan in order`);
      need(holds.every(v => Math.abs(v - HOLD_S) <= 1.01 / 60), `${id}: every sign held ${HOLD_S} s`);
      need(q.vals.has(340) && q.vals.has(230), `${id}: the readout before and after the hit`);
      need(q.near > 300 && q.thOnly > 300 && q.scOnly > 100, `${id}: signs by the drone; each mode alone`);
      need(q.lOn > 100 && q.lOff > 30 && q.paused > 30, `${id}: the loupe on and off; the pause held`);
    });
    const doc = K.picked(), fb = K.byId.fallback.value, cand = K.tiles[3].cand;
    console.log('status after the pick:', st, '| pick doc:', JSON.stringify(doc), '| copy text:', fb);
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    need(doc && doc.pick === cand.id, 'the pick is in the db doc');
    need(st.includes(`«${cand.name}»`), 'the status names the pick');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the copy text names the pick');
    return ok;
  },
};
