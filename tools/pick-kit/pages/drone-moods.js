// The light drone's thoughts and moods in its shipped «Нити» look: a coal core, three orbit threads with sparks, every
// shape a thread outline with sparks running along it. Five glyph languages; the four alerts in each, eight moods to judge,
// each mood with how the drone moves. An art-pick page on tools/pick-kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/drone-moods.js <out-dir> [--break <name>]
const MOOD_NAMES = [['curious', 'Любопытство'], ['find', 'Радость находки'], ['bored', 'Скука'], ['fear', 'Страх'], ['sleep', 'Сон'], ['love', 'Верность'], ['confused', 'Растерянность'], ['pride', 'Гордость']];

module.exports = {
  title: 'Мысли дрона',
  heading: 'Мысли и настроения дрона в «Нитях»: тревоги и чувства',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Дрон «Нити»: угольное ядро, три орбиты-нити с искрами. Мысль — фигура из нити, по которой бегут искры: она встаёт над дроном, держится 1,5 с и сматывается обратно в орбиты. Здесь пять разных <b>языков знаков</b> внутри «Нитей». В каждом показаны четыре решённые <b>тревоги</b> — метеорит, молния, холод, жара — и восемь <b>настроений</b> на пробу: у каждого свой знак и своё движение дрона (наклон, подскок, дрожь, глаз).</p>
<p class="lead">Круг: покой → четыре тревоги → восемь настроений, по 2,6 с на каждую мысль. <b>Отметь во второй строке, какие настроения нужны</b>, и нажми «Выбрать этот» под языком, который нравится: отметки сохраняются вместе с выбором (и меняются после). Масштаб игры: клетка 8 px мира, вид ×4, герой ~42 px мира, знак ~11 px мира; в лупе справа вверху дрон ×2,5.</p>
<p class="miss">Не показано: звук; что видят друзья; отрисовка Godot (свет и цвета выйдут чуть иначе); что вызывает каждую мысль и как часто она приходит; две мысли сразу; движения дрона одинаковы во всех языках — сравнивай знаки.</p>`,
  toggles: [
    { id: 'showMode', bar: 1, label: 'Показ:', key: 'show', opts: [['весь круг', 'all'], ['только тревоги', 'alerts'], ['только настроения', 'moods']] },
    { id: 'loupeMode', bar: 1, label: 'Лупа:', key: 'loupe', opts: [['есть', true], ['нет', false]] },
    ...MOOD_NAMES.map(([key, name]) => ({ id: 'mood_' + key, label: name + ':', key, opts: [['не нужно', false], ['нужно', true]], saved: true, short: name.toLowerCase() })),
  ],
  pick: { doc: 'picks/droneMoods', subject: 'Мысли дрона', what: ['язык знаков', 'языком'] },

  page() {
    const TAU = Math.PI * 2;
    // ---------- the loop: idle, the four alerts, the eight moods; a thought holds 1.5 s ----------
    const SLOT = 2.6, HOLD = 1.5, IDLE = 1.5;
    const ALERTS = ['meteor', 'lightning', 'cold', 'heat'];
    const MOODS = ['curious', 'find', 'bored', 'fear', 'sleep', 'love', 'confused', 'pride'];
    const ORDER = ['idle', ...ALERTS, ...MOODS];
    const LOOP = IDLE + (ORDER.length - 1) * SLOT;
    const OFF = [0, -50], RI = 22, LOUPE_K = 2.5, HERO_X = 520, N = 120, LOOT = [404, FLOOR_Y - 3];
    const RED = [255, 58, 40];
    const SAY = {
      idle: 'покой: искры бегут по трём орбитам',
      meteor: 'тревога: летит метеорит', lightning: 'тревога: молния рядом', cold: 'тревога: холод', heat: 'тревога: жара',
      curious: 'настроение: любопытство · наклон набок, глаз в сторону',
      find: 'настроение: радость находки · подпрыгивает, смотрит на находку, орбиты быстрее',
      bored: 'настроение: скука · опускается, лениво плывёт, орбиты медленнее',
      fear: 'настроение: страх · сжимается, прячется за голову героя, дрожит',
      sleep: 'настроение: сон · оседает, орбиты почти стоят, глаз гаснет',
      love: 'настроение: верность · прижимается к шлему, смотрит на героя',
      confused: 'настроение: растерянность · качается, глаз мечется, орбиты сбиваются',
      pride: 'настроение: гордость · поднимается, раздувается, глаз вверх',
    };

    const hsh = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const arcP = (cx, cy, rx, ry, a0, a1, n = 18) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + (a1 - a0) * i / n; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; };
    const circ = (cx, cy, r, n = 22) => arcP(cx, cy, r, r, 0, TAU, n);
    const star = (cx, cy, R, r, n) => { const p = []; for (let i = 0; i <= 2 * n; i++) { const a = -Math.PI / 2 + i * Math.PI / n, q = i % 2 ? r : R; p.push([cx + Math.cos(a) * q, cy + Math.sin(a) * q]); } return p; };
    const heart = (cx, cy, s, n = 36) => { const p = []; for (let i = 0; i <= n; i++) { const a = i / n * TAU; p.push([cx + s * 16 * Math.pow(Math.sin(a), 3) / 17, cy - s * (13 * Math.cos(a) - 5 * Math.cos(2 * a) - 2 * Math.cos(3 * a) - Math.cos(4 * a)) / 17 - .15 * s]); } return p; };
    const spiral = (cx, cy, R, turns, dir, a0 = 0, n = 30) => { const p = []; for (let i = 0; i <= n; i++) { const a = a0 + dir * turns * TAU * i / n; p.push([cx + Math.cos(a) * R * i / n, cy + Math.sin(a) * R * i / n]); } return p; };
    const wave = (x0, x1, y, amp, cyc, n = 20) => { const p = []; for (let i = 0; i <= n; i++) p.push([lerp(x0, x1, i / n), y + amp * Math.sin(i / n * cyc * TAU)]); return p; };

    // ---------- paths: polylines walked by a share of their length; a frame places them on screen ----------
    function mkPath(polys) {
      const seg = []; let L = 0;
      for (const p of polys) for (let i = 1; i < p.length; i++) { const a = p[i - 1], b = p[i], d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 1e-6) continue; seg.push([a[0], a[1], b[0], b[1], d, L]); L += d; }
      return { seg, L, polys };
    }
    function onPath(P, s) {
      const d = (((s % 1) + 1) % 1) * P.L; let lo = 0, hi = P.seg.length - 1;
      while (lo < hi) { const m = (lo + hi + 1) >> 1; if (P.seg[m][5] <= d) lo = m; else hi = m - 1; }
      const q = P.seg[lo], e = c01((d - q[5]) / q[4]);
      return [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e, (q[2] - q[0]) / q[4], (q[3] - q[1]) / q[4]];
    }
    const frameOf = (O, sc) => ({ O, sc });
    const toW = (F, q) => [F.O[0] + q[0] * F.sc, F.O[1] + q[1] * F.sc, q[2], q[3]];
    function strokePolys(g, F, polys, w, col, a) {
      if (a <= .003) return;
      g.beginPath();
      for (const p of polys) p.forEach((q, j) => { const r = toW(F, q); if (j) g.lineTo(r[0], r[1]); else g.moveTo(r[0], r[1]); });
      g.lineWidth = w; g.strokeStyle = css(col, a); g.stroke();
    }
    // the first r of a one-line path, as a thread pulled out that far
    function strokePart(g, sh, r, w, col, a) {
      if (a <= .003 || r <= 0) return;
      const P = sh.P, lim = r * P.L; let first = true;
      g.beginPath();
      for (const q of P.seg) {
        if (q[5] >= lim) break;
        const e = Math.min(1, (lim - q[5]) / q[4]), A = toW(sh.F, [q[0], q[1]]), B = toW(sh.F, [q[0] + (q[2] - q[0]) * e, q[1] + (q[3] - q[1]) * e]);
        if (first) { g.moveTo(A[0], A[1]); first = false; }
        g.lineTo(B[0], B[1]);
      }
      g.lineWidth = w; g.strokeStyle = css(col, a); g.stroke();
    }
    function line2(g, a, b, w, col, al) { if (al <= .003) return; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.lineWidth = w; g.strokeStyle = css(col, al); g.stroke(); }

    // ---------- the pictures (box -1..1, y down): the alerts as decided, the moods to judge ----------
    const PICT = (() => {
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
      G.heat = [-.55, 0, .55].map((x0, j) => { const p = []; for (let i = 0; i <= 16; i++) { const y = .72 - i / 16 * 1.66; p.push([x0 + .16 * Math.sin((.72 - y) * 5 + j * .6), y]); } return p; });
      G.heat.push([[-.95, .95], [.95, .95]]);
      G.curious = [arcP(0, -.44, .4, .4, Math.PI - .15, TAU + 1.05, 18).concat([[0, .1], [0, .4]]), circ(0, .8, .08, 8)];
      G.find = [star(-.15, .12, .88, .3, 4), star(.66, -.66, .3, .1, 4)];
      G.bored = [-.6, 0, .6].map(x => circ(x, .2, .16, 10));
      const lid = s => { const p = []; for (let i = 0; i <= 16; i++) { const x = -1 + i / 8; p.push([x, s * .56 * (1 - x * x)]); } return p; };
      G.fear = [lid(-1), lid(1), circ(0, 0, .27, 16), circ(0, 0, .07, 8)];
      G.sleep = [[[-.95, -.7], [.15, -.7], [-.95, .4], [.15, .4]], [[.35, .1], [.95, .1], [.35, .75], [.95, .75]]];
      G.love = [heart(0, 0, 1)];
      G.confused = [(() => { const p = []; for (let i = 0; i <= 60; i++) { const s = i / 60 * 3 * TAU; p.push([-.7 + i / 60 * 1.4 - .28 * Math.sin(s), .1 - .42 * Math.cos(s)]); } return p; })()];
      G.pride = [[[-.9, .6], [-.95, -.45], [-.45, .05], [0, -.72], [.45, .05], [.95, -.45], [.9, .6], [-.9, .6]], [[-.9, .32], [.9, .32]]];
      return G;
    })();
    // a face of threads, for the moods of «Лицо»: two eyes and a mouth
    const FACE = (() => {
      const E = [-.45, .45], F = {};
      F.curious = [circ(-.45, -.15, .15, 12), circ(.45, -.2, .25, 16), arcP(.45, -.62, .3, .12, Math.PI + .35, TAU - .35, 10), circ(.08, .52, .13, 10)];
      F.find = [...E.map(x => [[x - .27, -.05], [x, -.38], [x + .27, -.05]]), arcP(0, .12, .62, .5, .2, Math.PI - .2, 16)];
      F.bored = [...E.map(x => [[x - .26, -.2], [x + .26, -.2]]), ...E.map(x => arcP(x, -.2, .26, .17, 0, Math.PI, 10)), [[-.35, .55], [.42, .5]]];
      F.fear = [...E.map(x => circ(x, -.2, .29, 16)), ...E.map(x => circ(x, -.2, .06, 8)), wave(-.5, .5, .58, .08, 3)];
      F.sleep = [...E.map(x => arcP(x, -.28, .26, .16, .15, Math.PI - .15, 10)), circ(0, .5, .11, 10), [[.55, -.98], [.92, -.98], [.55, -.62], [.92, -.62]]];
      F.love = [...E.map(x => heart(x, -.2, .26, 20)), arcP(0, .22, .42, .3, .3, Math.PI - .3, 12)];
      F.confused = [spiral(-.45, -.2, .3, 2, 1), spiral(.45, -.2, .3, 2, -1), [[-.45, .5], [-.27, .38], [-.09, .56], [.09, .38], [.27, .56], [.45, .44]]];
      F.pride = [...E.map(x => arcP(x, -.12, .26, .15, Math.PI + .15, TAU - .15, 10)), [[-.75, -.6], [-.2, -.5]], [[.2, -.5], [.75, -.6]], arcP(.12, .28, .42, .22, .25, Math.PI - .9, 12)];
      return F;
    })();
    // abstract signs for «Знаки»: no pictures, a shape that moves the way the thought feels
    const SIGNS = {
      meteor: t => {
        const o = ((t * 1.4) % 1) * .35, dash = off => { const C = [.15 + .7071 * off, -.15 + .7071 * off], a = -.55 + o, b = a + .4; return [[C[0] - .7071 * a, C[1] + .7071 * a], [C[0] - .7071 * b, C[1] + .7071 * b]]; };
        return [[[.85, -.85], [-.5, .5]], [[-.5, .05], [-.5, .5], [-.05, .5]], dash(.42), dash(-.42)];
      },
      lightning: t => { const k = .6 + .4 * Math.abs(Math.sin(t * 9)); return [[[-1, .05], [-.45, .05], [-.25, -.85 * k], [.05, .85 * k], [.3, -.45 * k], [.45, .05], [1, .05]]]; },
      cold: t => {
        const a0 = t * .6, hex = (r, a) => { const p = []; for (let i = 0; i <= 6; i++) { const q = a + i * TAU / 6; p.push([Math.cos(q) * r, Math.sin(q) * r]); } return p; }, out = [hex(.85, a0), hex(.22, -a0)];
        for (let i = 0; i < 6; i++) { const q = a0 + i * TAU / 6; out.push([[Math.cos(q) * .85, Math.sin(q) * .85], [Math.cos(q) * .45, Math.sin(q) * .45]]); }
        return out;
      },
      heat: t => [0, 1, 2].map(j => { const y = .75 - ((t * .7 + j / 3) % 1) * 1.5; return [[-.65, y + .28], [0, y - .12], [.65, y + .28]]; }),
      curious: t => [spiral(0, 0, .95, 2.2, 1, t * 1.2, 40)],
      find: t => { const r1 = .32 + .6 * (.7 + .3 * Math.sin(t * 7)), out = [circ(0, 0, .14, 10)]; for (let i = 0; i < 8; i++) { const a = i / 8 * TAU; out.push([[Math.cos(a) * .32, Math.sin(a) * .32], [Math.cos(a) * r1, Math.sin(a) * r1]]); } return out; },
      bored: t => { const p = [], a = .12 * Math.sin(t * 1.1); for (let i = 0; i <= 16; i++) { const x = -.9 + i / 16 * 1.8; p.push(rot(x, -.2 + .45 * (1 - x * x), a)); } return [p, circ(0, .62, .08, 8)]; },
      fear: t => { const p = []; for (let i = 0; i <= 16; i++) { const a = i / 16 * TAU, r = (i % 2 ? .45 : .8) * (.9 + .1 * Math.sin(t * 2)) + .04 * Math.sin(t * 43 + i * 2.7); p.push([Math.cos(a) * r, Math.sin(a) * r]); } p[16] = p[0].slice(); return [p]; },
      sleep: t => [arcP(0, .1, .85, .25 + .15 * (.5 + .5 * Math.sin(t * 1.5)), 0, TAU, 24), [[-.6, .65], [.6, .65]]],
      love: t => { const b = 1 + .08 * Math.pow(Math.max(0, Math.sin(t * 7)), 8); return [circ(-.33, 0, .52 * b, 20), circ(.33, 0, .52 * b, 20)]; },
      confused: t => { const p = []; for (let i = 0; i <= 90; i++) { const s = i / 90 * TAU; p.push([.85 * Math.sin(3 * s + t * .8), .8 * Math.sin(2 * s)]); } return [p]; },
      pride: () => [[[-.32, .75], [0, -1], [.32, .75], [-.32, .75]], [[-.85, .75], [.85, .75]], [[-.55, -.35], [-.8, -.55]], [[.55, -.35], [.8, -.55]]],
    };
    // «Штрихи»: a picture cut into short threads with gaps between
    function chop(polys, len = .5, gap = .12) {
      const out = [];
      for (const p of polys) {
        const P = mkPath([p]); if (!P.L) continue;
        const n = Math.max(1, Math.round(P.L / len)), step = P.L / n;
        for (let j = 0; j < n; j++) {
          const a = (j * step + gap * .5) / P.L, b = ((j + 1) * step - gap * .5) / P.L, pts = [];
          for (let m = 0; m <= 6; m++) { const q = onPath(P, lerp(a, b, m / 6)); pts.push([q[0], q[1]]); }
          out.push(pts);
        }
      }
      return out;
    }
    // «Клубок»: one line out of the drone's outer orbit through the whole picture, its pieces joined by the thread
    function yarn(polys) {
      const ps = polys.map(p => p.slice()), out = ps.shift();
      while (ps.length) {
        const e = out[out.length - 1]; let best = 0, rev = false, bd = 1e9;
        ps.forEach((p, j) => { const a = Math.hypot(p[0][0] - e[0], p[0][1] - e[1]), b = Math.hypot(p[p.length - 1][0] - e[0], p[p.length - 1][1] - e[1]); if (a < bd) { bd = a; best = j; rev = false; } if (b < bd) { bd = b; best = j; rev = true; } });
        const p = ps.splice(best, 1)[0]; out.push(...(rev ? p.reverse() : p));
      }
      const S = [.35, -OFF[1] / RI - .12], tail = [];
      for (let i = 0; i < 10; i++) tail.push(bez(S, [1.25, 1.2], out[0], i / 10));
      return [tail.concat(out)];
    }

    // ---------- how the drone moves with a mood (page px at ×1); f is how far into it ----------
    const MOTION = {
      curious: (f, t) => ({ dx: -5 * f, dy: -2 * f, tilt: .45 * f, sc: 1, eye: [-1, -.6], pace: 1 }),
      find: (f, t) => ({ dx: 0, dy: -Math.abs(Math.sin(t * 7)) * 7 * f, tilt: .1 * Math.sin(t * 7) * f, sc: 1 + .08 * f, eye: 'loot', pace: 1 + 1.4 * f, glow: .3 * f }),
      bored: (f, t) => ({ dx: Math.sin(t * .9) * 5 * f, dy: 6 * f, tilt: .18 * Math.sin(t * .7) * f, sc: 1, eye: [Math.sin(t * .6), 1.2], pace: 1 - .65 * f, dim: .3 * f }),
      fear: (f, t) => ({ dx: 9 * f + Math.sin(t * 47) * 1.2 * f, dy: 12 * f + Math.cos(t * 41) * .8 * f, tilt: 0, sc: 1 - .25 * f, eye: [-1, -.2], pace: 1 + 1.6 * f }),
      sleep: (f, t) => ({ dx: 0, dy: 10 * f + Math.sin(t * 1.6) * 1.5 * f, tilt: -.25 * f, sc: 1 - .06 * f + .03 * Math.sin(t * 1.6) * f, eye: [0, 1], pace: 1 - .85 * f, dim: .75 * f }),
      love: (f, t) => ({ dx: -22 * f, dy: 7 * f + Math.sin(t * 3) * 1.5 * f, tilt: -.2 * f, sc: 1, eye: 'hero', pace: 1 - .3 * f, glow: .2 * f }),
      confused: (f, t) => { const a = Math.floor(t * 2.5) * 2.4; return { dx: Math.sin(t * 2.3) * 3 * f, dy: 0, tilt: .35 * Math.sin(t * 3.1) * f, sc: 1, eye: [Math.cos(a), Math.sin(a)], pace: 1 + Math.sin(t * 1.7) * .8 * f }; },
      pride: (f, t) => ({ dx: 0, dy: -9 * f, tilt: 0, sc: 1 + .14 * f, eye: [-.3, -1], pace: 1 + .3 * f, glow: .25 * f }),
    };
    const STILL = { dx: 0, dy: 0, tilt: 0, sc: 1, eye: null, pace: 1, dim: 0, glow: 0 };
    const moveOf = st => {
      if (st.kind !== 'mood') return STILL;
      const m = MOTION[st.id](st.env, st.u);
      return { ...STILL, ...m, dim: m.dim || 0, glow: m.glow || 0 };
    };

    // ---------- the drone: orbits, sparks, the coal ----------
    const P170 = DR.P, NP = DR.P.length;
    function orbit(i, X) {
      const P = P170[i % NP], o = i % 3, dir = o % 2 ? -1 : 1, th = P.ph + X.op * (.8 + .25 * o) * dir * 2.2, rx = (6.3 + o * 1.4 + (P.r1 - .5)) * 1.05 * X.k, tl = -.3 + o * 1.05 + X.tilt;
      const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tl), v = rot(-Math.sin(th) * dir, Math.cos(th) * .3 * dir, tl), vl = Math.hypot(v[0], v[1]) || 1;
      return [X.D[0] + q[0], X.D[1] + q[1], v[0] / vl, v[1] / vl, Math.sin(th)];
    }
    function coal(c, g, X) {
      const x = X.D[0], y = X.D[1], k = X.k, s = DS * DSC * k, A = X.A, fl = (.75 + .25 * Math.sin(X.t * 7.3)) * (1 - .7 * X.dim);
      halo(g, x, y, 8 * k, X.hot, .35 * (1 - .5 * X.dim) + .25 * X.glow);
      c.save(); c.translate(x, y); c.scale(s, s); paint(c, DR.core, A, DARK, .8, 2.6); ink(c, DR.core, A, 1.7); c.restore();
      g.strokeStyle = css(X.hot, .5 * fl); g.lineWidth = .45 * k; g.beginPath();
      for (const [a, b] of [[-.6, 2.4], [.9, 3.6], [2.2, 4.8]]) { g.moveTo(x + Math.cos(A + a) * .6 * k, y + Math.sin(A + a) * .6 * k); g.lineTo(x + Math.cos(A + b * .5) * 2.6 * k, y + Math.sin(A + b * .5) * 2.6 * k); }
      g.stroke();
      const ex = x + Math.cos(A) * 2.2 * s, ey = y + Math.sin(A) * 2.2 * s;
      g.fillStyle = css(pale(X.hot, .5), .9 * fl); g.beginPath(); g.arc(ex, ey, 1.2 * k * (1 - .5 * X.dim), 0, TAU); g.fill();
      halo(g, ex, ey, 4 * k, X.hot, .5 * fl);
    }
    // a spark: a soot speck, its glow with a short tail behind it
    function mote(c, g, p, s, heat, X) {
      const k = X.k, h = Math.min(1, heat), dx = p[2] || 0, dy = p[3] || 0;
      c.fillStyle = '#26272b'; c.fillRect(p[0] - s / 2, p[1] - s / 2, s, s);
      if (h <= .02) return;
      for (let m = 0; m < 3; m++) { const b = m * 1.3 * k; g.fillStyle = css(pale(X.hot, .3 * (1 - m / 3)), (.95 - m * .3) * h); g.fillRect(p[0] - dx * b - s / 2, p[1] - dy * b - s / 2, s, s); }
    }
    function fly(S, E, q, i, X) {
      const e = sm(q), p = [lerp(S[0], E[0], e), lerp(S[1], E[1], e)], dx = E[0] - S[0], dy = E[1] - S[1], d = Math.hypot(dx, dy) || 1;
      if (i % 4 === 0) line2(X.g, S, p, .35 * X.k, X.hot, .07);
      return [p[0], p[1], dx / d, dy / d];
    }
    const idleHeat = (i, X) => (hsh(i + 3) < .5 ? .55 + .3 * Math.sin(X.t * 11 + i * 7) : .2) * (1 - .6 * X.dim);

    // ---------- the candidates: a glyph language each ----------
    // glyph(id, t): polys; live: rebuilt each frame; place(sh, s, i, X): spark i at s of the shape; thread(g, sh, X, form);
    // heat(i, X, sh); alert: how alerts glow ('red' or 'white'); direct: sparks run out along the thread, no flight
    const flow = (vA, vM) => function (sh, s, i, X) { const v = sh.alert ? vA : vM, Lw = sh.P.L * sh.F.sc; return toW(sh.F, onPath(sh.P, s + X.t * v * X.kb / Lw)); };
    const softHeat = (i, X, sh) => sh.alert ? .75 + .25 * Math.sin(X.t * 13 + i) : .42 + .18 * Math.sin(X.t * 4 + i * 1.7);
    const plainThread = (g, sh, X, form) => { const a = sh.alert ? 1 : .7; strokePolys(g, sh.F, sh.P.polys, 1.6 * X.kb, X.hot, .06 * form * a); strokePolys(g, sh.F, sh.P.polys, .45 * X.kb, X.hot, .3 * form * a); };
    const cands = [
      {
        id: 'outline', name: 'Контур', tin: .4, tout: .4, alert: 'red',
        idea: 'Как сейчас в «Нитях»: знак — контур-картинка из нитей, по которому течёт ток искр. Тревоги раскаляются докрасна и бегут быстро; настроения тлеют мягко и текут вдвое медленнее. Опорный вариант для сравнения.',
        time: 'Нити протягиваются за 0,4 с · держится 1,5 с · сматываются за 0,4 с · ток искр: тревога 42 px/с, настроение 18 px/с · краснота тревоги пульсирует 2,4 раза в секунду.',
        glyph: id => PICT[id],
        place: flow(42, 18), heat: softHeat, thread: plainThread,
      },
      {
        id: 'strokes', name: 'Штрихи', tin: .35, tout: .35, alert: 'white',
        idea: 'Знак нарисован короткими штрихами с просветами, как пером. По каждому штриху бежит своя искра и начинает сначала. Тревога — штрихи вспыхивают добела и мигают вразнобой, как искрящий провод; настроение — штрихи ровно тлеют, искры идут шагом.',
        time: 'Штрихи встают за 0,35 с · держится 1,5 с · гаснут за 0,35 с · искра пробегает штрих: тревога за 0,55 с, настроение за 2 с · мигание тревоги ~14 раз в секунду.',
        glyph: id => chop(PICT[id]), parts: true,
        place(sh, s, i, X) { const np = sh.parts.length, j = Math.min(np - 1, Math.floor(s * np)), v = sh.alert ? 1.8 : .5; return toW(sh.F, onPath(sh.parts[j], s * np - j + X.t * v)); },
        heat: (i, X, sh) => sh.alert ? (Math.sin(X.t * 30 + i * 2.1) > 0 ? 1 : .35) : .4 + .15 * Math.sin(X.t * 3 + i),
        thread(g, sh, X, form) {
          sh.P.polys.forEach((p, j) => {
            const a = sh.alert ? .18 + .55 * Math.pow(.5 + .5 * Math.sin(X.t * 14 + j * 1.7), 4) : .22;
            strokePolys(g, sh.F, [p], 1.5 * X.kb, X.hot, .25 * a * form); strokePolys(g, sh.F, [p], .5 * X.kb, X.hot, a * form);
          });
        },
      },
      {
        id: 'face', name: 'Лицо', tin: .4, tout: .4, alert: 'red',
        idea: 'Настроение — не картинка, а лицо из нитей над дроном: два глаза и рот. Улыбка, глаза-домики, глаза-спирали, сердечки, сонные дуги — эмоцию читают по лицу, как у человека. Тревоги — обычные контур-значки, раскалённые докрасна, чтобы их нельзя было спутать с настроением.',
        time: 'Лицо проступает за 0,4 с · держится 1,5 с · сматывается за 0,4 с · ток искр по лицу 16 px/с · тревога — как в «Контуре»: 42 px/с, красная.',
        glyph: id => MOODS.includes(id) ? FACE[id] : PICT[id],
        place: flow(42, 16), heat: softHeat, thread: plainThread,
      },
      {
        id: 'yarn', name: 'Клубок', tin: .5, tout: .45, alert: 'red', direct: true,
        idea: 'Знак — одна нить, которую дрон вытягивает из своей внешней орбиты: орбита гаснет, нить ползёт вверх и рисует знак одним росчерком, части связаны тонкой перемычкой. Уходя, нить сматывается обратно в орбиту. Тревоги — нить красная и тянется рывком.',
        time: 'Нить вытягивается за 0,5 с · держится 1,5 с · сматывается за 0,45 с · по натянутой нити бегут искры: тревога 50 px/с, настроение 20 px/с.',
        glyph: id => yarn(PICT[id]),
        place(sh, s, i, X) { const v = sh.alert ? 50 : 20, Lw = sh.P.L * sh.F.sc, r = sh.reveal; return toW(sh.F, onPath(sh.P, Math.min(.9999, (((s + X.t * v * X.kb / Lw) % 1) + 1) % 1 * r))); },
        heat: softHeat,
        thread(g, sh, X, form) {
          const a = sh.alert ? 1 : .7;
          strokePart(g, sh, sh.reveal, 1.6 * X.kb, X.hot, .07 * a); strokePart(g, sh, sh.reveal, .45 * X.kb, X.hot, .32 * a);
          const tip = toW(sh.F, onPath(sh.P, Math.min(.9999, sh.reveal)));
          if (sh.ph !== 'hold') halo(g, tip[0], tip[1], 3 * X.kb, pale(X.hot, .4), .7);
        },
      },
      {
        id: 'signs', name: 'Знаки', tin: .4, tout: .4, alert: 'red', live: true,
        idea: 'Не картинки, а живые абстрактные знаки: любопытство — раскручивающаяся спираль, радость — лучи-вспышка, скука — провисшая нить, страх — дрожащая колючка, сон — дышащий овал, верность — два сцепленных кольца, растерянность — путаница, гордость — шпиль. Тревоги — тоже знаки (стрела вниз, разряд, кристалл, поднимающийся жар), красные, в пульсирующем кольце.',
        time: 'Знак встаёт за 0,4 с · держится 1,5 с · сматывается за 0,4 с · знаки всё время движутся · ток искр: тревога 42 px/с, настроение 18 px/с · кольцо тревоги пульсирует 3 раза в секунду.',
        glyph: (id, t) => SIGNS[id](t),
        place: flow(42, 18), heat: softHeat,
        thread(g, sh, X, form) {
          plainThread(g, sh, X, form);
          if (sh.alert) { const r = (1.25 + .1 * Math.sin(X.t * TAU * 3)) * sh.F.sc; g.lineWidth = .4 * X.kb; g.strokeStyle = css(X.hot, .22 * form); g.beginPath(); g.arc(sh.F.O[0], sh.F.O[1], r, 0, TAU); g.stroke(); }
        },
      },
    ];

    // ---------- the clock ----------
    function planOf(mode) {
      const ids = mode === 'alerts' ? ALERTS : mode === 'moods' ? MOODS : ORDER;
      let t = 0; return ids.map(id => { const q = { id, t0: t, len: id === 'idle' ? IDLE : SLOT }; t += q.len; return q; });
    }
    function thoughtOf(cand, u) {
      if (u < cand.tin) return { ph: 'in', e: u / cand.tin };
      if (u < cand.tin + HOLD) return { ph: 'hold', e: (u - cand.tin) / HOLD };
      if (u < cand.tin + HOLD + cand.tout) return { ph: 'out', e: (u - cand.tin - HOLD) / cand.tout };
      return null;
    }
    const formOf = b => !b ? 0 : b.ph === 'in' ? sm(b.e) : b.ph === 'out' ? 1 - sm(b.e) : 1;
    function stateOf(T) {
      const P = planOf(OPT.show), L = P[P.length - 1].t0 + P[P.length - 1].len, u = ((T.time - T.t0) % L + L) % L;
      const q = P.find(p => u >= p.t0 && u < p.t0 + p.len) || P[0];
      const st = { id: q.id, u: Math.max(0, u - q.t0), kind: ALERTS.includes(q.id) ? 'alert' : MOODS.includes(q.id) ? 'mood' : 'idle', body: null, alarm: 0, env: 0 };
      if (st.kind !== 'idle') {
        st.body = thoughtOf(T.cand, st.u);
        st.body = st.body && { id: st.id, ...st.body };
        st.env = sm(c01(st.u / .5)) * sm(c01((SLOT - .15 - st.u) / .5));
      }
      // an alert glows red-hot (or white) and pulses while its thought is up
      if (st.kind === 'alert' && st.body) st.alarm = formOf(st.body) * (.6 + .4 * Math.sin(st.u * TAU * 2.4));
      return st;
    }

    // ---------- the body: orbit sparks, a share flying into the thought and back ----------
    const CH = {}, GL = {};
    function chosen(n, share) {
      const key = n + ':' + share; if (CH[key]) return CH[key];
      const rank = []; let m = 0;
      for (let i = 0; i < n; i++) rank.push(hsh(i * 3 + 1) < share ? m++ : -1);
      return (CH[key] = { rank, m });
    }
    function glyphOf(cand, id, t) {
      const key = cand.id + ':' + id;
      if (!cand.live && GL[key]) return GL[key];
      const polys = cand.glyph(id, t), P = mkPath(polys), G = { P, parts: cand.parts ? polys.map(p => mkPath([p])).filter(q => q.L > 0) : null };
      if (!cand.live) GL[key] = G;
      return G;
    }
    function shapeOf(cand, st, X) {
      const b = st.body; if (!b) return null;
      const G = glyphOf(cand, b.id, X.t);
      if (!G.P.L) return null;
      const F = frameOf([X.D[0] + OFF[0] * X.kb, X.D[1] + OFF[1] * X.kb], RI * X.kb);
      return { P: G.P, parts: G.parts, F, ph: b.ph, e: b.e, share: .7, alert: st.kind === 'alert', id: b.id, reveal: formOf(b) };
    }
    function mkX(c, g, D, A, kb, t, op, mo, st, cand) {
      const a = st.kind === 'alert' ? st.alarm : 0;
      const hot = a <= 0 ? EMBER : cand.alert === 'white' ? pale(EMBER, .55 * Math.min(1, a * 1.4)) : mix3(EMBER, RED, Math.min(1, a * 1.4));
      return { c, g, D, A, kb, k: kb * mo.sc, t, op, tilt: mo.tilt, dim: mo.dim, glow: mo.glow, hot, red: a };
    }
    function drawBody(c, g, cand, X, st) {
      const sh = shapeOf(cand, st, X), pick = sh ? chosen(N, sh.share) : null, list = [], form = formOf(st.body);
      let placed = 0;
      for (let i = 0; i < N; i++) {
        const I = orbit(i, X);
        let p = I, z = I[4], heat = idleHeat(i, X);
        const j = pick ? pick.rank[i] : -1;
        if (j >= 0) {
          let q = 1, out = false;
          if (!cand.direct) {
            const lag = hsh(i + 50);
            if (sh.ph === 'in') q = sm(c01((sh.e - lag * .45) / .55));
            else if (sh.ph === 'out') { q = 1 - sm(c01((sh.e - lag * .4) / .6)); out = true; }
          }
          if (q > 0) {
            const S = cand.place(sh, (j + .5) / pick.m, i, X);
            p = q >= 1 ? S : out ? fly(S, I, 1 - q, i, X) : fly(I, S, q, i, X);
            z = q > .5 ? 1 : z;
            heat = cand.heat(i, X, sh) + .3 * Math.sin(q * Math.PI);
            if (q >= 1) placed++;
          }
        }
        list.push([p, z, heat]);
      }
      // the three orbit threads; «Клубок» pulls its outer one out into the thought
      g.lineWidth = .4 * X.k;
      for (let o = 0; o < 3; o++) {
        const a = .12 * (1 - .5 * X.dim) * (cand.direct && o === 2 ? 1 - form : 1); if (a <= .003) continue;
        const rx = (6.3 + o * 1.4) * 1.05 * X.k; g.strokeStyle = css(X.hot, a); g.beginPath(); g.ellipse(X.D[0], X.D[1], rx, rx * .3, -.3 + o * 1.05 + X.tilt, 0, TAU); g.stroke();
      }
      if (sh) cand.thread(g, sh, X, form);
      const sz = .95 * X.k;
      for (const e of list) if (e[1] < 0) mote(c, g, e[0], sz, e[2], X);
      coal(c, g, X);
      for (const e of list) if (e[1] >= 0) mote(c, g, e[0], sz, e[2], X);
      return { sh, placed };
    }

    // ---------- the world: the deck, a scrap of loot, what each alert warns of ----------
    function loot(c, glint) {
      const [x, y] = LOOT;
      c.fillStyle = 'rgba(0,0,0,.45)'; c.beginPath(); c.ellipse(x, FLOOR_Y + 1.5, 13, 2.5, 0, 0, TAU); c.fill();
      c.save(); c.translate(x, y); c.rotate(-.18);
      c.fillStyle = INK; c.fillRect(-11, -6, 22, 9); c.fillStyle = '#5d636c'; c.fillRect(-10, -5, 20, 7); c.fillStyle = '#80868f'; c.fillRect(-8, -4, 9, 2);
      c.restore();
      if (glint > .01) { halo(c, x - 3, y - 4, 14, [255, 236, 200], .6 * glint); c.save(); c.globalCompositeOperation = 'lighter'; c.strokeStyle = css([255, 240, 210], .8 * glint); c.lineWidth = 1; c.beginPath(); c.moveTo(x - 9, y - 4); c.lineTo(x + 3, y - 4); c.moveTo(x - 3, y - 10); c.lineTo(x - 3, y + 2); c.stroke(); c.restore(); }
    }
    function alertWorld(c, st) {
      const u = st.u, a = st.env; if (a <= .003) return;
      if (st.id === 'meteor') {
        if (u > 1.9) return;
        const e = u / 1.9, H = [470 - 340 * e, 26 + 120 * e], Tl = [H[0] + 70, H[1] - 26], f = 1 - c01((e - .8) / .2);
        const gr = c.createLinearGradient(Tl[0], Tl[1], H[0], H[1]); gr.addColorStop(0, 'rgba(255,190,120,0)'); gr.addColorStop(1, css([255, 222, 176], .85 * f));
        c.save(); c.lineCap = 'round'; c.strokeStyle = gr; c.lineWidth = 2; c.beginPath(); c.moveTo(Tl[0], Tl[1]); c.lineTo(H[0], H[1]); c.stroke(); c.restore();
        halo(c, H[0], H[1], 9, [255, 200, 140], .5 * f);
      } else if (st.id === 'lightning') {
        const fl = Math.max(0, 1 - Math.abs(u - .5) / .12) + .6 * Math.max(0, 1 - Math.abs(u - .78) / .08);
        if (fl > .01) {
          c.fillStyle = css([190, 210, 255], .16 * fl); c.fillRect(0, 0, LW, FLOOR_Y);
          c.save(); c.globalCompositeOperation = 'lighter'; c.strokeStyle = css([220, 232, 255], .9 * Math.min(1, fl)); c.lineWidth = 1.6; c.lineJoin = 'round'; c.beginPath();
          [[176, 0], [160, 70], [182, 120], [150, 210], [170, 260], [138, 360], [150, FLOOR_Y]].forEach((p, i) => i ? c.lineTo(p[0], p[1]) : c.moveTo(p[0], p[1])); c.stroke(); c.restore();
        }
      } else if (st.id === 'cold') {
        const gr = c.createRadialGradient(LW / 2, LH / 2, 120, LW / 2, LH / 2, 620); gr.addColorStop(0, 'rgba(150,200,255,0)'); gr.addColorStop(1, css([150, 200, 255], .22 * a));
        c.fillStyle = gr; c.fillRect(0, 0, LW, LH);
        for (let i = 0; i < 40; i++) { const x = (hsh(i) * LW + Math.sin(u * 1.3 + i) * 12) % LW, y = (hsh(i + 9) * FLOOR_Y + u * (24 + 20 * hsh(i + 4))) % FLOOR_Y; c.fillStyle = css([225, 238, 255], .7 * a); c.fillRect(x, y, 1.6, 1.6); }
      } else if (st.id === 'heat') {
        const gr = c.createLinearGradient(0, FLOOR_Y, 0, FLOOR_Y - 220); gr.addColorStop(0, css([255, 110, 40], .2 * a)); gr.addColorStop(1, 'rgba(255,110,40,0)');
        c.fillStyle = gr; c.fillRect(0, FLOOR_Y - 220, LW, 220);
        c.save(); c.strokeStyle = css([255, 170, 110], .18 * a); c.lineWidth = 1;
        for (let j = 0; j < 7; j++) { const x0 = 80 + j * 130, y0 = FLOOR_Y - 10 - ((u * 40 + j * 23) % 120); c.beginPath(); for (let i = 0; i <= 10; i++) { const y = y0 - i * 6; i ? c.lineTo(x0 + 4 * Math.sin(y * .15 + u * 6), y) : c.moveTo(x0 + 4 * Math.sin(y * .15 + u * 6), y); } c.stroke(); }
        c.restore();
      }
    }

    // ---------- the light buffer and the loupe ----------
    function lightStart(T) {
      const g = T.hbg; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = 1; g.globalCompositeOperation = 'source-over'; g.clearRect(0, 0, T.hb.width, T.hb.height);
      g.setTransform(T.S, 0, 0, T.S, 0, 0); g.globalCompositeOperation = 'lighter'; g.lineCap = 'round'; g.lineJoin = 'round';
      return g;
    }
    function lightEnd(T) {
      const g = T.hbg; g.globalCompositeOperation = 'source-over';
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(T.hb, 0, 0); c.restore();
    }
    const LP = { x: 770, y: 12, w: 216, h: 262 };
    function loupe(T) {
      if (!OPT.loupe || !T.st) return false;
      const c = T.c, h = T.me, k = LOUPE_K, m = T.mo;
      rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.fillStyle = '#05070b'; c.fill(); c.strokeStyle = 'rgba(140,150,165,.35)'; c.lineWidth = 1; c.stroke();
      c.save(); rr(c, LP.x, LP.y, LP.w, LP.h, 8); c.clip();
      const D = [LP.x + LP.w / 2 + m.dx * k, LP.y + 190 + m.dy * k + Math.sin(T.time * 1.7 + h.seed) * 1.5 * k];
      const g = lightStart(T);
      drawBody(c, g, T.cand, mkX(c, g, D, h.d.ang, k, T.time + h.seed, T.op, m, T.st, T.cand), T.st);
      lightEnd(T);
      c.restore();
      c.font = '11px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'left'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(200,205,214,.55)';
      c.fillText('лупа ×2,5', LP.x + 8, LP.y + LP.h - 8);
      return true;
    }
    const droneAt = (T, h) => [h.d.p[0] + T.mo.dx, h.d.p[1] + T.mo.dy];

    return {
      cands, expose: { LOOP, SLOT, HOLD, ALERTS, MOODS, ORDER },
      init(T) { T.me.face = -1; T.t0 = 0; T.showWas = OPT.show; T.st = null; T.mo = STILL; T.op = 0; },
      onScale(T) { const cv = document.createElement('canvas'); cv.width = Math.ceil(LW * T.S); cv.height = Math.ceil(LH * T.S); T.hb = cv; T.hbg = cv.getContext('2d'); },
      step(T, dt) {
        T.me.x = HERO_X;
        if (T.showWas !== OPT.show) { T.showWas = OPT.show; T.t0 = T.time; }
        T.st = stateOf(T); T.mo = moveOf(T.st);
        T.op += dt * T.mo.pace;
      },
      // the eye: up at its thought, or where the mood looks
      aim(T, h) {
        const st = T.st, m = T.mo; if (!st) return null;
        const D = droneAt(T, h);
        if (m.eye === 'hero') return toS(h, HELM);
        if (m.eye === 'loot') return LOOT;
        if (m.eye) return [D[0] + m.eye[0] * 50, D[1] + m.eye[1] * 50];
        if (st.body) return [D[0] + OFF[0], D[1] + OFF[1]];
        return null;
      },
      ownDrone: () => true,
      back(T) {
        drawBack(T);
        const st = T.st, h = T.me; if (!st) return;
        if (st.kind === 'alert') alertWorld(T.c, st);
        loot(T.c, st.id === 'find' ? st.env * (.6 + .4 * Math.sin(st.u * 9)) : 0);
        const D = droneAt(T, h);
        lampOn(T.c, D[0], D[1], h.x, h.y, h.face, .6 * (1 - .7 * T.mo.dim), 2.6);
      },
      overDrone(T, h) {
        const st = T.st, c = T.c, m = T.mo;
        const g = lightStart(T), X = mkX(c, g, droneAt(T, h), h.d.ang, 1, T.time + h.seed, T.op, m, st, T.cand);
        const r = drawBody(c, g, T.cand, X, st);
        lightEnd(T);
        T.dm = { id: st.id, kind: st.kind, u: st.u, red: X.red, mo: m, th: r.sh ? { ph: r.sh.ph, O: r.sh.F.O, D: X.D, placed: r.placed } : null, loupe: false };
      },
      screen(T) { if (T.dm) T.dm.loupe = loupe(T); },
      hud(T) {
        const st = T.st; if (!st) return '';
        if (st.kind === 'idle') return SAY.idle;
        return st.body ? SAY[st.id] : 'мысль смоталась в орбиты';
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    hold: [['const SLOT = 2.6, HOLD = 1.5, IDLE = 1.5;', 'const SLOT = 2.6, HOLD = .7, IDLE = 1.5;']],
    far: [['const OFF = [0, -50]', 'const OFF = [-130, -50]']],
    noheat: [['      heat: t => [0, 1, 2]', '      heat: t => [] || [0, 1, 2]']],
    unsaved: [['"key":"love","opts":[["не нужно",false],["нужно",true]],"saved":true', '"key":"love","opts":[["не нужно",false],["нужно",true]],"saved":false']],
    still: [["love: (f, t) => ({ dx: -22 * f, dy: 7 * f + Math.sin(t * 3) * 1.5 * f, tilt: -.2 * f, sc: 1, eye: 'hero', pace: 1 - .3 * f, glow: .2 * f }),", 'love: (f, t) => ({ dx: 0, dy: 0, tilt: 0, sc: 1, eye: null, pace: 1 }),']],
    redmood: [["const a = st.kind === 'alert' ? st.alarm : 0;", "const a = st.kind === 'idle' ? 0 : .8;"]],
  },

  // one whole loop; then only alerts, only moods; the loupe off, slow, a pause, a real pointer; a pick, moods ticked and unticked
  check(K) {
    const X = K.X, HOLD_S = 1.5;
    const L = Math.round(X.LOOP * 60), A = L + 30, B = A + Math.round(X.ALERTS.length * X.SLOT * 60) + 20, C = B + 320, E = C + 360;
    K.at(A, () => K.seg('showMode', 1));
    K.at(B, () => K.seg('showMode', 2));
    K.at(C, () => { K.seg('showMode', 0); K.seg('loupeMode', 1); K.seg('speedMode', 1); });
    K.at(C + 120, () => { K.seg('speedMode', 0); K.seg('playMode', 1); });
    K.at(C + 180, () => { K.seg('playMode', 0); K.seg('loupeMode', 0); K.ptrAll([300, 120]); });
    K.at(C + 240, () => K.ptrAll(null));
    const docs = [], sts = [];
    let fb = '';
    K.at(E - 60, () => K.click(2, '.pick'));
    K.at(E - 55, () => { docs.push(K.picked()); sts.push(K.byId.status.textContent); });
    K.at(E - 50, () => { K.seg('mood_curious', 1); K.seg('mood_love', 1); K.seg('mood_sleep', 1); });
    K.at(E - 45, () => { docs.push(K.picked()); sts.push(K.byId.status.textContent); });
    K.at(E - 40, () => K.seg('mood_love', 0));
    K.at(E - 35, () => { docs.push(K.picked()); sts.push(K.byId.status.textContent); K.byId.copy.listeners.click[0](); fb = K.byId.fallback.value; });
    const per = K.tiles.map(() => ({ seq: [], hold: {}, placed: {}, moved: {}, near: 0, redA: 0, redM: 0, al: 0, mo: 0, lOn: 0, lOff: 0, paused: 0, lastT: -1 }));
    K.run(E, (t, i) => {
      const q = per[K.tiles.indexOf(t)], d = t.dm; if (!d) return;
      const id = t.cand.id;
      if (i < L - 1) {
        if (q.seq[q.seq.length - 1] !== d.id) q.seq.push(d.id);
        if (d.th && d.th.ph === 'hold') {
          const h = q.hold[d.id] || (q.hold[d.id] = [t.time, t.time]); h[1] = t.time;
          q.placed[d.id] = Math.max(q.placed[d.id] || 0, d.th.placed);
        }
        if (d.kind === 'mood') { const m = d.mo, mag = Math.hypot(m.dx, m.dy) + Math.abs(m.tilt) * 20 + Math.abs(m.sc - 1) * 40 + Math.abs(m.pace - 1) * 4 + m.dim * 8; q.moved[d.id] = Math.max(q.moved[d.id] || 0, mag); }
      }
      if (d.th && d.th.ph === 'hold') {
        const Dp = d.th.D, O = d.th.O;
        if (Math.abs(O[0] - Dp[0]) > 12 || O[1] > Dp[1] - 30 || O[1] < Dp[1] - 70) K.fail(`${id}: ${d.id} at ${O.map(Math.round)}, the drone at ${Dp.map(Math.round)}, frame ${i}`); else q.near++;
      }
      if (d.red > 0) { if (d.kind === 'alert') q.redA++; else { q.redM++; K.fail(`${id}: hot at ${d.id} (${d.kind}), frame ${i}`); } }
      if (i > A + 1 && i < B) { if (d.kind !== 'alert') K.fail(`${id}: ${d.id} with only alerts on, frame ${i}`); else q.al++; }
      if (i > B + 1 && i < C) { if (d.kind !== 'mood') K.fail(`${id}: ${d.id} with only moods on, frame ${i}`); else q.mo++; }
      if (i < C) { if (d.loupe) q.lOn++; } else if (i > C + 1 && i < C + 180) { if (d.loupe) K.fail(`${id}: the loupe drawn while off, frame ${i}`); else q.lOff++; }
      if (i > C + 121 && i < C + 179) { if (q.lastT >= 0 && t.time !== q.lastT) K.fail(`${id}: time ran while paused`); q.paused++; }
      q.lastT = t.time;
    });
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    const THOUGHTS = [...X.ALERTS, ...X.MOODS];
    K.tiles.forEach((t, j) => {
      const q = per[j], id = t.cand.id, held = THOUGHTS.map(s => q.hold[s] ? q.hold[s][1] - q.hold[s][0] + 1 / 60 : 0);
      console.log(`${id}: order ${q.seq.join(' ')}`);
      console.log(`  held ${held.map(v => v.toFixed(3)).join(' ')} s | sparks on ${THOUGHTS.map(s => q.placed[s] || 0).join(' ')} | moved ${X.MOODS.map(s => (q.moved[s] || 0).toFixed(1)).join(' ')} | near ${q.near} hot ${q.redA}/${q.redM} | only alerts ${q.al} moods ${q.mo} | loupe ${q.lOn}/${q.lOff} paused ${q.paused}`);
      need(q.seq.slice(0, X.ORDER.length).join() === X.ORDER.join(), `${id}: every thought in order`);
      need(held.every(v => Math.abs(v - HOLD_S) <= 1.01 / 60), `${id}: every thought held ${HOLD_S} s by the drone`);
      need(X.ALERTS.every(s => (q.placed[s] || 0) >= 40), `${id}: all four alerts drawn in sparks`);
      need(X.MOODS.every(s => (q.placed[s] || 0) >= 40), `${id}: every mood drawn in sparks`);
      need(X.MOODS.every(s => (q.moved[s] || 0) >= 4), `${id}: every mood moves the drone`);
      need(q.redA > 100 && q.redM === 0, `${id}: alerts glow hot, moods never`);
      need(q.near > 1000 && q.al > 400 && q.mo > 200, `${id}: thoughts by the drone; alerts alone, moods alone`);
      need(q.lOn > 100 && q.lOff > 30 && q.paused > 30, `${id}: the loupe on and off; the pause held`);
    });
    const cand = K.tiles[2].cand, M = X.MOODS, want = [[], ['curious', 'love', 'sleep'], ['curious', 'sleep']];
    console.log('docs:', docs.map(d => JSON.stringify(d)).join(' | '));
    console.log('status:', sts.join(' | '), '| copy:', fb);
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    want.forEach((on, n) => need(docs[n] && docs[n].pick === cand.id && M.every(m => docs[n][m] === on.includes(m)), `pick ${n}: the moods ${on.join(', ') || 'none'} saved with the pick`));
    need(sts[1].includes(`«${cand.name}»`) && sts[1].includes('верность: нужно') && sts[2].includes('верность: не нужно') && sts[2].includes('сон: нужно'), 'the status names the pick and its moods');
    need(fb.includes(`(${cand.id})`) && fb.includes('любопытство: нужно') && fb.includes('страх: не нужно'), 'the copy text names the pick and its moods');
    return ok;
  },
};
