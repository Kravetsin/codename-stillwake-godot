// The game's name on the title screen, «Пепел» as a loop: CASTOFF turns to dust and gathers again, forever.
// An art-pick page on tools/pick-kit (its README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/title-logo.js <out-dir> [--fonts <dir>] [--break seam|pop|fast|menu|short]
// The font is an OFL file from github.com/google/fonts, by default in fonts/oswald/ beside the out dir, with its OFL.txt.
module.exports = {
  title: 'Название',
  heading: 'Название: «Пепел» по кругу',
  lead: `
<p class="lead">Название игры на заставке, кадр 1280×720 как экран игры («Крупно 1:1» — в полный размер): CASTOFF шрифтом Oswald, высота заглавных 88 px, слева над меню; за ним сцена «Против солнца» в мягком свете. Вид — выбранный «Пепел»: бледные холодные буквы с зерном, пыль тех же тонов, у солнца она теплеет.</p>
<p class="lead">Теперь распад — круг без конца: всё слово рассыпается в пыль, пыль собирается обратно в слово, и снова. Большую часть круга (~10 из 19,5 с) слово целое и читается. Четыре дубля отличаются тем, как пыль уходит и как возвращается; сборка кончается ровно целым словом, без скачка на стыке. «Ещё раз» под плиткой запускает распад заново.</p>
<p class="miss">Не показано: звук. В игре выбранный дубль нарисует код этой страницы и выгрузит картинками (слово и его пыль), а Godot будет их только двигать, так что сглаживание выйдет чуть иначе. Меню — макет системным шрифтом; героя нет.</p>`,
  tile: [1280, 720],
  kitBars: ['speedMode', 'playMode'],
  toggles: [],
  fonts: { Oswald: 'oswald/Oswald[wght].ttf' },
  pick: { doc: 'picks/titleLogo', subject: 'Название', what: ['надпись', 'надписью'] },

  page() {
    const TAU = Math.PI * 2, WORD = 'CASTOFF';
    const unit = (x, y) => { const l = Math.hypot(x, y) || 1; return [x / l, y / l]; };
    const SPACE = '#03050a', STAR = [223, 232, 245], SUN = [840, 290], CORE_R = 6.5;
    const WARM = [255, 210, 156], DUST = [214, 222, 232];
    const X0 = 84, BASE = 250;                          // the name's left ink edge and its baseline
    const MENU_X = 84, MENU_Y = 336, MENU_DY = 38;      // the menu mock under the name
    const MENU = [MENU_X - 28, MENU_Y - 30, MENU_X + 256, MENU_Y + 4 * MENU_DY + 12];
    // the loop, seconds: whole until D0, dissolving for DIS, apart, gathering from G0 for GAT, whole again to CYCLE
    const D0 = 7.5, DIS = 3.5, G0 = 13.5, GAT = 3.5, GE = G0 + GAT, CYCLE = 19.5;
    const SOFT = .3;                                    // a pixel fades out or in over this long
    const TE_LO = D0 + SOFT + .05, TE_HI = D0 + DIS - .15, TG_LO = G0 + .05, TG_HI = GE - SOFT - .05;
    const PACE = 1;                                     // a scale on every drift
    const FADE_IN = 1.2;                                // the name's first appearance, once the font is in
    // Oswald: units per em and cap height, the cap height drawn, the tracking, and per letter [advance, ink xMin, ink xMax]
    const F = { fam: 'Oswald', upm: 1000, cap: 810, px: 88, track: 9, g: { C: [515, 48, 470], A: [492, 19, 473], S: [473, 39, 448], T: [413, 15, 398], O: [539, 48, 491], F: [391, 63, 376] } };
    const L = (() => {
      const size = F.px * F.upm / F.cap, k = size / F.upm, xs = [], ink = [];
      let x = X0 - F.g.C[1] * k;
      for (const ch of WORD) { const g = F.g[ch]; xs.push(x); ink.push([x + g[1] * k, x + g[2] * k]); x += g[0] * k + F.track; }
      const top = BASE - F.px, right = ink[6][1];
      return { xs, ink, top, right, cap: F.px, mid: BASE - F.px / 2, font: `${size.toFixed(2)}px "${F.fam}"`, WB: [X0 - 40, top - 40, right - X0 + 80, F.px + 80] };
    })();
    const XL = L.ink[0][0], WW = L.right - XL, CX = XL + WW / 2, nx = x => c01((x - XL) / WW);

    // ---------- small tools ----------
    const canvas = (w, h) => { const v = document.createElement('canvas'); v.width = Math.max(1, Math.round(w)); v.height = Math.max(1, Math.round(h)); return { v, g: v.getContext('2d') }; };
    const smoother = u => u * u * u * (u * (u * 6 - 15) + 10);
    const hash = (x, y, s) => { let h = (x * 374761393 + y * 668265263 + s * 1442695041) | 0; h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
    function vnoise(x, y, s) {
      const xi = Math.floor(x), yi = Math.floor(y), fx = x - xi, fy = y - yi, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
      const a = hash(xi, yi, s), b = hash(xi + 1, yi, s), c = hash(xi, yi + 1, s), d = hash(xi + 1, yi + 1, s);
      return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
    }
    const fbm = (x, y, s) => (vnoise(x, y, s) + .5 * vnoise(2 * x, 2 * y, s + 1) + .25 * vnoise(4 * x, 4 * y, s + 2)) / 1.75;
    // the sun's reach: how much of its warm light a point gets, as the soft sun's falloff
    const REACH = [[0, 1], [.15, .75], [.35, .4], [.6, .15], [1, .03]], REACH_R = 900;
    function reach(p) {
      const u = Math.min(1, Math.hypot(p[0] - SUN[0], p[1] - SUN[1]) / REACH_R);
      for (let i = 1; i < REACH.length; i++) if (u <= REACH[i][0]) { const [a, va] = REACH[i - 1], [b, vb] = REACH[i]; return va + (vb - va) * (u - a) / (b - a); }
      return REACH[REACH.length - 1][1];
    }
    function sunMask(g) {   // g in page px: keeps what the sun reaches
      const r = g.createRadialGradient(SUN[0], SUN[1], 0, SUN[0], SUN[1], REACH_R);
      for (const [u, a] of REACH) r.addColorStop(u, `rgba(0,0,0,${a})`);
      g.globalCompositeOperation = 'destination-in'; g.fillStyle = r; g.fillRect(-100, -100, LW + 200, LH + 200); g.globalCompositeOperation = 'source-over';
    }
    function tint(src, col, a = 1) { const b = canvas(src.width, src.height); b.g.drawImage(src, 0, 0); b.g.globalCompositeOperation = 'source-in'; b.g.fillStyle = css(col, a); b.g.fillRect(0, 0, b.v.width, b.v.height); b.g.globalCompositeOperation = 'source-over'; return b; }
    function edgeOf(src, dx, dy, col, a = 1) { const b = tint(src, col, a); b.g.globalCompositeOperation = 'destination-out'; b.g.drawImage(src, dx, dy); b.g.globalCompositeOperation = 'source-over'; return b; }
    function letters(g, s, fill) {
      g.setTransform(s, 0, 0, s, -L.WB[0] * s, -L.WB[1] * s);
      g.font = L.font; g.textAlign = 'left'; g.textBaseline = 'alphabetic'; g.fillStyle = fill;
      for (let i = 0; i < WORD.length; i++) g.fillText(WORD[i], L.xs[i], BASE);
    }
    // the dust's drift away from its letter: a wind that picks up over tr s, a slow curl on top
    function drift(g, s, tr) {
      const r = s - tr * (1 - Math.exp(-s / tr)), k = sm(s / 2);
      return [g.ox + g.vx * r + g.A * Math.sin(TAU * s / g.P) * k, g.oy + g.vy * r + g.A * .6 * (1 - Math.cos(TAU * s / (g.P * 1.3))) * k];
    }
    // «Отражение»'s clock: forward to the middle of the apart time, a soft stop, then back the same way; whole again at GE
    const MID = (D0 + GE) / 2, EPS = 1.2;
    const mirrorT = t => { const d = Math.abs(t - MID); return MID - (d >= EPS ? d : (d * d + EPS * EPS) / (2 * EPS)); };

    // ---------- the plume: a steady flow toward the sun with some lift, curled by layered sines (curl noise) ----------
    const W_SUN = 60 * PACE, LIFT = .3, VMAX = 112 * PACE, DT = .05, SETTLE = .4;   // a grain is home SETTLE s before it lands, then melts into its pixel
    const CURL = [[1 / 70, 1 / 110, 9, .3], [-1 / 95, 1 / 60, 7, 1.9], [1 / 150, -1 / 85, 12, 4.1], [1 / 38, 1 / 50, 4, 2.7]];   // [kx, ky per px, px/s, phase]
    function curl(x, y) {
      let vx = 0, vy = 0;
      for (const [kx, ky, A, q] of CURL) { const c = A * Math.cos(TAU * (kx * x + ky * y) + q), l = Math.hypot(kx, ky); vx += c * ky / l; vy -= c * kx / l; }
      return [vx, vy];
    }
    // the wisps the dust gathers into: wavy lines rising a little toward the sun, uneven in width and share
    const WISPS = [[-34, -.10, 9, 70, .3, 3, .34], [-8, -.16, 14, 95, 2.1, 7, .26], [18, -.05, 7, 55, 4, 2.5, .22], [42, -.12, 11, 120, 1.2, 10, .18]];   // [y from the word's middle, rise per px, wave px, wave length px, phase, half-width px, share]
    const wispY = (j, x) => { const [b, k, A, l, q] = WISPS[j]; return L.mid + b + k * (x - X0) + A * Math.sin(x / l + q); };
    const wispDY = (j, x) => { const [, k, A, l, q] = WISPS[j]; return k + A / l * Math.cos(x / l + q); };
    function pickWisp(g, R) {
      const w = WISPS.map(([, , , , , , p], j) => p * Math.exp(-(((g.oy - wispY(j, g.ox)) / 40) ** 2))), sum = w.reduce((p, q) => p + q, 0);
      let u = R() * sum, j = 0; while (j < 3 && u > w[j]) u -= w[j++];
      g.wisp = j; g.off = WISPS[j][5] * (R() + R() + R() - 1.5) * 1.15;
    }
    // a grain's path out of its letter, sampled every DT s: its own speed k toward the sun, picking up over the first second, gathering into its wisp
    function plume(g, len) {
      const n = Math.ceil(len / DT) + 1, P = new Float32Array(2 * n);
      const v = (x, y, s) => {
        const d = unit(SUN[0] - x, SUN[1] - y), c = curl(x, y), r = sm(s / 1.1), w = g.k * W_SUN * (.3 + 1.4 * c01((x - 80) / 450)), j = g.wisp, la = g.la;
        let vx = r * (w * d[0] + .5 * c[0]), vy = r * ((1 - la) * (w * (d[1] - LIFT) + c[1]) + la * (wispDY(j, x) * vx + .4 * c[1])) + 1.8 * la * sm(s / 1.2) * (wispY(j, x) + g.off - y);
        const m = Math.hypot(vx, vy); if (m > VMAX) { vx *= VMAX / m; vy *= VMAX / m; } return [vx, vy];
      };
      let x = g.ox, y = g.oy;
      for (let i = 0; i < n; i++) {
        P[2 * i] = x; P[2 * i + 1] = y;
        const s = i * DT, a = v(x, y, s), b = v(x + a[0] * DT / 2, y + a[1] * DT / 2, s + DT / 2);
        x += b[0] * DT; y += b[1] * DT;
      }
      return P;
    }
    const along = (P, s) => { const n = P.length / 2 - 1, u = Math.max(0, Math.min(n, s / DT)), i = Math.min(n - 1, Math.floor(u)), f = u - i; return [lerp(P[2 * i], P[2 * i + 2], f), lerp(P[2 * i + 1], P[2 * i + 3], f)]; };
    const fadeFar = x => 1 - sm((x - 560) / 220);   // the plume's far end thins out before the sun
    const plumeSpeed = R => .45 + 1.35 * Math.pow(R(), 1.6);
    const plumeGrain = (g, R) => { g.k = plumeSpeed(R); g.la = R() < .15 ? .2 * R() : .7 + .3 * R(); pickWisp(g, R); };   // speed (most slow, a few fast), its wisp, how closely it keeps to it
    // the wind take's clock along a grain's own path: out for half its trip, a soft turn, then back along the same wisp
    function windClock(g, t) {
      const q = t - g.tm, e = g.eps, b = e / g.rho;
      if (q <= -e) return g.f + q;
      if (q >= b) return Math.max(0, g.f - g.rho * q);
      const H = b + e, u = (q + e) / H, u2 = u * u, u3 = u2 * u;
      return g.f - e * (2 * u3 - 3 * u2 + 1) + H * (u3 - 2 * u2 + u) - e * (-2 * u3 + 3 * u2) - g.rho * H * (u3 - u2);
    }
    // the tide take's stream: a ribbon along a wavy line that spreads as it nears the word, traced back in time from each grain's home
    const yc = (x, j) => L.mid - 20 + (j - 2) * 14 + 10 * Math.sin(x / (50 + 13 * j) + 1.7 * j), SPREAD = .75, ARR_MAX = 5.8;   // five strands
    function stream(g) {
      const pts = [g.ox, g.oy];
      let x = g.ox, y = g.oy, tau = 0;
      while (x > -30 && tau < ARR_MAX - 1e-9) {
        const sp = sm((tau + DT / 2) / 1.4), c = curl(x, y);
        let vx = sp * (g.U + .4 * c[0]), vy = sp * (SPREAD * (y - yc(x, g.strand)) + .4 * c[1]); const m = Math.hypot(vx, vy); if (m > VMAX) { vx *= VMAX / m; vy *= VMAX / m; }
        x -= vx * DT; y -= vy * DT;
        tau += DT; pts.push(x, y);
      }
      return Float32Array.from(pts);
    }

    // ---------- the takes: when each pixel goes (u) and comes back (v), 0..1 across their windows; how its dust moves ----------
    // a grain: { ox, oy, td (leaves), tl (lands: back home, its pixel whole), a0, r, … }; at(g, t) gives [x, y, alpha] or null when home
    const home = (g, t) => t >= g.tl ? null : [g.ox, g.oy, g.a0 * sm((g.tl - t) / SETTLE)];
    const TAKES = {
      // a front left to right; the dust streams toward the sun in long wisps, then the plume is drawn back along them, landing left to right
      wind: {
        cap: 3500, u: (x, y) => .82 * nx(x) + .18 * fbm(x / 40, y / 5, 3), v: (x, y) => .85 * nx(x) + .15 * vnoise(x / 5, y / 5, 4),
        mk: (g, R) => {
          const trip = g.tl - SETTLE - g.td;
          plumeGrain(g, R); g.f = .5 * trip; g.rho = g.f / (trip - g.f); g.tm = g.td + g.f; g.eps = Math.min(.5, .4 * g.f); g.P = plume(g, g.f + .6);
        },
        at(g, t) {
          if (t <= g.td || t >= g.tl) return null;
          if (t >= g.tl - SETTLE) return home(g, t);
          const p = along(g.P, windClock(g, t));
          return [p[0], p[1], g.a0 * sm((t - g.td) / .45) * fadeFar(p[0])];
        },
      },
      // the whole word loosens at once into a cloud of its own shape, breathes and drifts a little, then tightens back
      breath: {
        cap: 4500, u: (x, y) => .6 * fbm(x / 24, y / 24, 5), v: (x, y) => .6 * fbm(x / 24, y / 24, 6),
        mk: (g, R) => { g.A = 2 + 3 * R(); g.P = 2.5 + 2.5 * R(); g.P2 = 3 + 2.5 * R(); g.q = TAU * R(); g.q2 = TAU * R(); },
        at(g, t) {
          if (t <= g.td || t >= g.tl) return null;
          const env = sm((t - g.td) / 1.6) * sm((g.tl - SETTLE - t) / 1.4), k = .07 + .03 * Math.sin(TAU * (t - D0) / 4.5);
          const dx = k * (g.ox - CX) + g.A * Math.sin(TAU * t / g.P + g.q) + 14 * PACE, dy = k * (g.oy - L.mid) + g.A * .7 * Math.sin(TAU * t / g.P2 + g.q2) - 6 * PACE;
          return [g.ox + env * dx, g.oy + env * dy, g.a0 * sm((t - g.td) / .45) * sm((g.tl - t) / .4)];
        },
      },
      // crumbling from the right edge into a plume that thins out toward the sun; new dust streams in from the left as a ribbon and builds left to right
      tide: {
        cap: 2800, twin: true, u: (x, y) => .8 * (1 - nx(x)) + .2 * vnoise(x / 3, y / 3, 7), v: (x, y) => .85 * nx(x) + .15 * vnoise(x / 4, y / 4, 8),
        mk: (g, R) => { if (g.out) { plumeGrain(g, R); g.P = plume(g, 4.2); } else { g.U = PACE * (72 + 24 * R()); g.strand = Math.floor(R() * 5); g.S = stream(g); g.tmax = (g.S.length / 2 - 1) * DT; } },
        at(g, t) {
          if (g.out) {
            const s = t - g.td; if (s <= 0 || s > 4.1) return null;
            const p = along(g.P, s);
            return [p[0], p[1], g.a0 * sm(s / .45) * Math.min(1 - sm((s - 1.4) / 2.6), fadeFar(p[0]))];
          }
          if (t >= g.tl - SETTLE) return home(g, t);
          const tau = g.tl - SETTLE - t; if (tau >= g.tmax) return null;
          const p = along(g.S, tau);
          return [p[0], p[1], g.a0 * c01((p[0] - 14) / 120) * sm((g.tmax - tau) / .8)];
        },
      },
      // the first ash forward, then played back: the dust slows, stops for a breath and returns by the same path
      mirror: {
        cap: 3200, mirror: true, u: (x, y) => .8 * (1 - nx(x)) + .2 * vnoise(x / 3, y / 3, 7), v: null,
        mk: (g, R) => { g.vx = PACE * (8 + 10 * R()); g.vy = -PACE * (1 + 3 * R()); g.A = .8 + 2 * R(); g.P = 5 + 4 * R(); },
        at(g, t) {
          const s = mirrorT(t) - g.td; if (s <= 0) return null;
          const f = drift(g, s, .8);
          return [f[0], f[1], g.a0 * sm(s / .45) * (.35 + .65 * Math.exp(-s / 6))];
        },
      },
    };
    const teOf = (k, x, y) => lerp(TE_LO, TE_HI, c01(TAKES[k].u(x, y)));
    const tgOf = (k, x, y) => TAKES[k].v ? lerp(TG_LO, TG_HI, c01(TAKES[k].v(x, y))) : 0;
    // how much of a pixel shows at loop time t
    const pixel = (k, te, tg, t) => TAKES[k].mirror ? c01((te - mirrorT(t)) / SOFT) : t < (te + tg) / 2 ? c01((te - t) / SOFT) : c01((t - tg) / SOFT);

    // the dust, once the font is in: a grain per sampled pixel of the word (two for the tide take: one leaving, one arriving).
    // Grains differ: mostly 1 px, some 1.6, a few 2.6 px flakes; brightness from a quarter to full; a few glint warm near the sun.
    function geometry(k) {
      const T = TAKES[k], R = rng(37), W = Math.ceil(L.WB[2]), H = Math.ceil(L.WB[3]), b = canvas(W, H);
      letters(b.g, 1, '#fff');
      const ya = Math.floor(L.top - 14 - L.WB[1]), yb = Math.ceil(BASE + 14 - L.WB[1]), im = b.g.getImageData(0, ya, W, yb - ya), list = [];
      for (let y = 0; y < im.height; y++) for (let x = 0; x < im.width; x++) {
        const a = im.data[(y * im.width + x) * 4 + 3]; if (a < 90) continue;
        list.push([L.WB[0] + x + .5, L.WB[1] + ya + y + .5, a / 255]);
      }
      const keep = Math.min(1, T.cap / Math.max(1, list.length)), grains = [];
      for (const [x, y, a] of list) {
        if (R() > keep) continue;
        const te = teOf(k, x, y), tg = tgOf(k, x, y), size = R(), r = size < .07 ? 1.3 : size < .3 ? .8 : .5;
        const mk = () => ({ ox: x + (R() - .5) * .8, oy: y + (R() - .5) * .8, td: te - .15 + .1 * R(), tl: tg + SOFT, a0: a * (.25 + .75 * Math.pow(R(), .8)) * (r > 1 ? .7 : 1), r, glint: false }), mkG = () => { const g = mk(); if (R() < .06) { g.glint = true; g.a0 = Math.max(g.a0, .85 * a); } return g; };
        if (T.twin) { const o = { ...mkG(), out: true }, i = mkG(); T.mk(o, R); T.mk(i, R); grains.push(o, i); }
        else { const g = mkG(); T.mk(g, R); grains.push(g); }
      }
      const n = grains.length, f = () => new Float64Array(n);
      return { grains, M: { n, x: f(), y: f(), a: f(), ox: f(), oy: f(), r: f() } };
    }

    // ---------- the word's pictures at the tile's scale: «Пепел»'s look ----------
    function grainTex() {   // watercolour grain: fine specks over soft blots, tiling
      const N = 96, b = canvas(N, N), im = b.g.createImageData(N, N), r = rng(31);
      const blot = (x, y) => .5 + .25 * Math.sin(TAU * (x * 2 + y) / N + 1.3) * Math.sin(TAU * (y * 3 - x) / N + .4) + .25 * Math.sin(TAU * (x + y * 2) / N * 2 + 2.1);
      for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
        const i = (y * N + x) * 4, dark = r() < .55, a = dark ? r() * 26 + blot(x, y) * 22 : r() * 14;
        im.data[i] = im.data[i + 1] = im.data[i + 2] = dark ? 0 : 255; im.data[i + 3] = Math.floor(a);
      }
      b.g.putImageData(im, 0, 0);
      return b.v;
    }
    const GRAIN_TEX = grainTex();
    function wordBuffers(T) {
      const S = T.S, b = canvas(L.WB[2] * S, L.WB[3] * S), g = b.g;
      letters(g, S, '#fff');
      const fill = g.createLinearGradient(0, L.top, 0, BASE); fill.addColorStop(0, css([224, 229, 235])); fill.addColorStop(1, css([174, 184, 197]));
      g.globalCompositeOperation = 'source-in'; g.fillStyle = fill; g.fillRect(L.WB[0], L.WB[1], L.WB[2], L.WB[3]);
      g.globalCompositeOperation = 'source-atop'; g.setTransform(1, 0, 0, 1, 0, 0); g.globalAlpha = .5; g.fillStyle = g.createPattern(GRAIN_TEX, 'repeat'); g.fillRect(0, 0, b.v.width, b.v.height);
      g.globalAlpha = 1; g.globalCompositeOperation = 'source-over';
      // the warm lit edge facing the sun, as far as the sun reaches, baked in
      const l = unit(SUN[0] - (L.right - 60), SUN[1] - L.mid), rim = edgeOf(b.v, -l[0] * 1.6 * S, -l[1] * 1.6 * S, WARM);
      rim.g.setTransform(S, 0, 0, S, -L.WB[0] * S, -L.WB[1] * S); sunMask(rim.g); rim.g.setTransform(1, 0, 0, 1, 0, 0);
      g.drawImage(rim.v, 0, 0);
      // every inked pixel with the times it goes and comes back
      const w = b.v.width, h = b.v.height, base = g.getImageData(0, 0, w, h), idx = [], te = [], tg = [], k = T.cand.kind;
      let total = 0;
      for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
        const n = (y * w + x) * 4; if (base.data[n + 3] < 3) continue;
        const px = L.WB[0] + (x + .5) / S, py = L.WB[1] + (y + .5) / S;
        idx.push(n); te.push(teOf(k, px, py)); tg.push(tgOf(k, px, py)); total += base.data[n + 3];
      }
      const reg = canvas(w, h);
      return { S, word: b, E: { reg, base: base.data, idx: Int32Array.from(idx), te: Float32Array.from(te), tg: Float32Array.from(tg), total, out: reg.g.createImageData(w, h), at: null, missing: 0 } };
    }
    // the word at loop time t, pixel by pixel; how much of its ink is missing
    function erodeTo(E, k, t) {
      if (E.at === t) return;
      E.at = t;
      const d = E.out.data, b = E.base, idx = E.idx;
      let miss = 0;
      for (let i = 0; i < idx.length; i++) { const n = idx[i], f = pixel(k, E.te[i], E.tg[i], t); d[n] = b[n]; d[n + 1] = b[n + 1]; d[n + 2] = b[n + 2]; d[n + 3] = b[n + 3] * f; miss += b[n + 3] * (1 - f); }
      E.reg.g.putImageData(E.out, 0, 0);
      E.missing = miss / Math.max(1, E.total);
    }
    // whole when every pixel of the word shows, by the loop's windows alone
    const wholeAt = (k, t) => TAKES[k].mirror ? mirrorT(t) <= D0 : t < D0 || t >= GE;

    // ---------- the backdrop: «Против солнца» in soft light, as on the title (title-tether.js, C) ----------
    function starLayer(S, seed, n, big, a0, a1, bright, glints) {
      const b = canvas(LW * S, LH * S), r = rng(seed), g = b.g, put = (x, y, sz, a) => { g.fillStyle = css(STAR, a); g.fillRect(x, y, sz, sz); };
      for (let i = 0; i < n; i++) { const sz = r() < big ? 2 : 1, u = r(), a = a0 + u * u * (a1 - a0); put(Math.floor(r() * (b.v.width - 2)), Math.floor(r() * (b.v.height - 2)), sz, a); }
      for (let i = 0; i < bright; i++) {
        const x = Math.floor(r() * (b.v.width - 20)) + 10, y = Math.floor(r() * (b.v.height - 20)) + 10;
        put(x, y, 2, .95);
        if (i < glints) { const l = Math.round(3.5 * S); g.fillStyle = css(STAR, .32); g.fillRect(x - l, y, 2 * l + 2, 1); g.fillRect(x, y - l, 1, 2 * l + 2); g.fillStyle = css(STAR, .5); g.fillRect(x - 1, y, 4, 1); g.fillRect(x, y - 1, 1, 4); }
      }
      return b.v;
    }
    const disc = (g, c, r) => { g.beginPath(); g.arc(c[0], c[1], r, 0, TAU); };
    function sun(S) {
      const b = canvas(LW * S, LH * S), g = b.g;
      g.setTransform(S, 0, 0, S, 0, 0);
      const f = g.createRadialGradient(SUN[0], SUN[1], 0, SUN[0], SUN[1], 300);
      [[0, .95], [.025, .6], [.08, .24], [.24, .08], [.55, .02], [1, 0]].forEach(([u, a]) => f.addColorStop(u, css([255, 228, 190], a)));
      disc(g, SUN, 300); g.fillStyle = f; g.fill();
      disc(g, SUN, CORE_R); g.fillStyle = css([255, 247, 230]); g.fill();
      return b.v;
    }
    const backdrop = S => ({ S, far: starLayer(S, 7, 330, 0, .08, .45, 0, 0), near: starLayer(S, 9, 80, .2, .25, .8, 12, 4), sun: sun(S) });

    // ---------- the menu's mock ----------
    const ITEMS = ['Новая игра', 'Миры', 'Играть вместе', 'Настройки', 'Выйти'];
    function spaced(c, s, x, y, sp) { for (const ch of s) { c.fillText(ch, x, y); x += c.measureText(ch).width + sp; } }
    function drawMenu(T) {
      const c = T.c;
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.font = '300 21px system-ui,"Segoe UI",sans-serif';
      ITEMS.forEach((s, i) => { c.fillStyle = css([218, 214, 204], i ? .55 : .88); spaced(c, s, MENU_X, MENU_Y + i * MENU_DY, .7); });
      c.fillStyle = css([218, 214, 204], .6); c.fillRect(MENU_X - 20, MENU_Y - 7, 10, 1);
    }

    // ---------- the name at loop time t ----------
    const PAL = [0, 1, 2, 3, 4, 5, 6, 7].map(w => css([lerp(DUST[0], WARM[0], w / 7), lerp(DUST[1], WARM[1], w / 7), lerp(DUST[2], WARM[2], w / 7)]));
    function drawName(T) {
      const c = T.c, S = T.S, st = T.st, G = T.G, B = T.B, E = B.E, k = T.cand.kind, t = st.t, M = G.M, wa = sm(st.age / FADE_IN), take = TAKES[k];
      c.setTransform(S, 0, 0, S, 0, 0); c.globalAlpha = wa; c.globalCompositeOperation = 'source-over';
      const whole = wholeAt(k, t);
      if (whole) { E.missing = 0; E.at = null; c.drawImage(B.word.v, L.WB[0], L.WB[1], B.word.v.width / S, B.word.v.height / S); }
      else { erodeTo(E, k, t); c.drawImage(E.reg.v, L.WB[0], L.WB[1], E.reg.v.width / S, E.reg.v.height / S); }
      let last = -1, seen = 0;
      G.grains.forEach((g, m) => {
        const p = whole ? null : take.at(g, t);
        M.ox[m] = g.ox; M.oy[m] = g.oy; M.r[m] = g.r;
        if (!p) { M.x[m] = g.ox; M.y[m] = g.oy; M.a[m] = 0; return; }
        const near = reach(p), a = wa * p[2]; M.x[m] = p[0]; M.y[m] = p[1]; M.a[m] = a;
        if (a < .004) return;
        seen++;
        const w = Math.min(7, Math.floor(near * 7) + (g.glint ? Math.round(near * 9) : 0));   // dust near the sun warms; a few grains glint
        if (w !== last) { last = w; c.fillStyle = PAL[w]; }
        c.globalAlpha = a; c.fillRect(p[0] - g.r, p[1] - g.r, 2 * g.r, 2 * g.r);
      });
      st.M = M; st.missing = E.missing; st.seen = seen; st.whole = whole && seen === 0;
      c.globalAlpha = 1;
    }

    // ---------- the tile ----------
    let fontsOK = !(document.fonts && document.fonts.load);
    if (!fontsOK) {
      const done = () => { fontsOK = true; tiles.forEach(T => { T.G = null; T.B = null; T.st.t = 0; T.st.age = 0; }); dirtyAll(); };
      Promise.all(PAGE.fonts.map(f => document.fonts.load(`80px "${f}"`, WORD))).then(done, done);
    }
    function scene(T, dt) {
      const S = T.S, c = T.c, st = T.st;
      if (fontsOK) { st.t += dt; st.age += dt; if (st.t >= CYCLE) st.t -= CYCLE; }
      if (!T.BG || T.BG.S !== S) T.BG = backdrop(S);
      c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      c.fillStyle = SPACE; c.fillRect(0, 0, T.cv.width, T.cv.height);
      const W = T.BG.far.width;
      for (const [buf, v] of [[T.BG.far, 2.2], [T.BG.near, 4.5]]) { const o = ((T.time * v * S) % W + W) % W; c.drawImage(buf, -o, 0); c.drawImage(buf, W - o, 0); }
      c.drawImage(T.BG.sun, 0, 0);
      if (fontsOK) {
        if (!T.G) T.G = geometry(T.cand.kind);
        if (!T.B || T.B.S !== S) T.B = wordBuffers(T);
        drawName(T);
      }
      drawMenu(T);
      c.setTransform(S, 0, 0, S, 0, 0); c.globalAlpha = 1;
      if (OPT.slow) { c.font = '13px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'right'; c.fillStyle = 'rgba(214,190,140,.85)'; c.fillText('×¼', LW - 20, LH - 16); c.textAlign = 'left'; }
    }

    const FONT = 'Шрифт Oswald (SIL OFL 1.1), вид «Пепла».';
    const cands = [
      {
        id: 'wind', name: 'Ветер', kind: 'wind',
        idea: `${FONT} Порыв идёт по слову слева направо, буквы рассыпаются, и пыль уносит к солнцу несколькими длинными струями, чуть вверх: быстрые зёрна уходят далеко вперёд, хвост редеет и тает, не долетая до солнца. Потом шлейф втягивается обратно по тем же струям, и слово собирается слева направо.`,
        time: 'Цикл 19,5 с: целое ~10 с; распад 3,5 с; зёрна от ~10 до 112 px/с, разгоняясь к солнцу; каждое ~3 с туда, мягкий разворот и ~3 с назад по своему следу; сборка 3,5 с.',
      },
      {
        id: 'breath', name: 'Дыхание', kind: 'breath',
        idea: `${FONT} Слово разом рыхлеет в облако пыли той же формы — оно дышит, чуть расширяясь и сжимаясь, и немного сносится к солнцу, — потом стягивается обратно в буквы. Спокойно, всегда рядом, читается даже врозь.`,
        time: 'Цикл 19,5 с: целое ~10 с; рыхлеет за ~1–2 с; облако раздувается на 7–10 % (дыхание за 4,5 с) и сносится на ~15 px; стягивается за ~1–2 с к концу сборки.',
      },
      {
        id: 'tide', name: 'Прилив', kind: 'tide',
        idea: `${FONT} Слово осыпается с правого края, пыль уходит к солнцу и тает. Обратно приходит не она, а новая пыль — тонкими струями слева, из-за кадра, уже на ходу, — и строит слово слева направо: как бесконечное течение, не перемотка.`,
        time: 'Цикл 19,5 с: целое ~10 с; осыпание 3,5 с, уходящая пыль струями до 112 px/с и тает за ~4 с; новая пыль летит 72–96 px/с до ~6 с и ложится слева направо за 3,5 с.',
      },
      {
        id: 'mirror', name: 'Отражение', kind: 'mirror',
        idea: `${FONT} Для сравнения: прежний «Пепел» — слово осыпается с правого края, ветер несёт пыль вправо, — а потом то же самое в точной обратной записи: пыль замедляется, на миг замирает и тем же путём садится обратно.`,
        time: 'Цикл 19,5 с: целое ~10 с; распад 3,5 с, пыль 8–18 px/с; замирание ~1 с посередине; обратная сборка 3,5 с.',
      },
    ];
    return {
      cands,
      init(T) {
        T.st = { t: 0, age: 0, replays: 0, M: null, missing: 0, seen: 0, whole: true };
        const b = document.createElement('button'); b.type = 'button'; b.className = 'replay'; b.textContent = 'Ещё раз';
        b.addEventListener('click', () => { T.st.t = D0 - 1.5; T.st.replays++; T.dirty = true; });
        T.sec.querySelector('.acts').appendChild(b); T.replayBtn = b;
      },
      scene,
      expose: { CYCLE, D0, LW, LH, SUN, MENU },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    seam: [['CYCLE = 19.5;', 'CYCLE = 16;']],                                                     // the loop starts again mid-gather
    pop: [[': c01((t - tg) / SOFT);', ': (t >= GE - .05 ? 1 : 0);']],                            // the word snaps back at once
    fast: [['const PACE = 1;', 'const PACE = 6;']],                                               // the dust no longer drifts
    menu: [['MENU_Y = 336', 'MENU_Y = 296']],                                                     // the menu sits where the dust passes
    short: [['const D0 = 7.5,', 'const D0 = 3,']],                                                // the word is whole too briefly to read
  },

  check(K) {
    const X = K.X, C = Math.round(X.CYCLE * 60), END = 2 * C + 820, EDGE = 8, SUN_GAP = 20;
    // px/s; alpha a second, away from home; whole s a loop; share of the ink a second
    const LIM = { v: 130, fade: 1.2, whole: 8, pop: 3 };
    K.at(C + 150, () => K.seg('speedMode', 1)); K.at(C + 260, () => K.seg('speedMode', 0));
    K.at(C + 300, () => K.seg('playMode', 1)); K.at(C + 330, () => K.seg('playMode', 0));
    K.at(2 * C + 60, () => K.tiles.forEach(t => t.replayBtn.listeners.click[0]()));
    K.at(END - 30, () => K.click(1, '.pick'));
    const my0 = X.MENU[1], clamp = (v, a, z) => v < a ? a : v > z ? z : v;
    const info = K.tiles.map(() => ({ prev: null, acc: 0, clean: true, loops: [], apart: 0, maxMiss: 0, far: 0, top: { v: 0, fade: 0, pop: 0 }, seenMax: 0 }));
    K.run(END, (T, f) => {
      const I = info[K.tiles.indexOf(T)], st = T.st, M = st.M, id = T.cand.id;
      if (!M) { K.fail(`${id}: no name drawn, frame ${f}`); return; }
      I.maxMiss = Math.max(I.maxMiss, st.missing); I.seenMax = Math.max(I.seenMax, st.seen);
      for (let m = 0; m < M.n; m++) {
        if (M.a[m] <= .01) continue;
        const x = M.x[m], y = M.y[m], r = M.r[m];
        I.far = Math.max(I.far, Math.hypot(x - M.ox[m], y - M.oy[m]));
        if (x - r < EDGE || y - r < EDGE || x + r > X.LW - EDGE || y + r > X.LH - EDGE) { K.fail(`${id}: dust ${m} at ${x.toFixed(0)},${y.toFixed(0)} leaves the frame, frame ${f}`); break; }
        if (y + r > my0) { K.fail(`${id}: dust ${m} at ${x.toFixed(0)},${y.toFixed(0)} drifts into the menu's band below ${my0}, frame ${f}`); break; }
        if (Math.hypot(x - X.SUN[0], y - X.SUN[1]) < r + SUN_GAP) { K.fail(`${id}: dust ${m} covers the sun, frame ${f}`); break; }
      }
      const p = I.prev;
      if (p && p.rep === st.replays) {
        const dt = st.t - p.t;
        if (dt < 0) {   // the seam: whole on both sides, and the loop it closes was readable long enough
          if (!p.whole || !st.whole) K.fail(`${id}: not whole at the loop's seam (missing ${(100 * Math.max(p.missing, st.missing)).toFixed(1)} %, ${Math.max(p.seen, st.seen)} grains in the air), frame ${f}`);
          if (I.clean) I.loops.push(I.acc);
          I.acc = 0; I.clean = true;
        } else if (dt > 1e-6) {
          if (st.whole) I.acc += dt;
          const pop = Math.abs(st.missing - p.missing) / dt;
          I.top.pop = Math.max(I.top.pop, pop);
          if (pop > LIM.pop) K.fail(`${id}: ${(100 * Math.abs(st.missing - p.missing)).toFixed(0)} % of the word changes in one frame at ${st.t.toFixed(2)} s, frame ${f}`);
          for (let m = 0; m < M.n; m++) {
            if (p.a[m] <= .01 || M.a[m] <= .01) continue;
            const v = Math.hypot(M.x[m] - p.x[m], M.y[m] - p.y[m]) / dt, fade = (p.a[m] - M.a[m]) / dt, home = Math.hypot(M.x[m] - M.ox[m], M.y[m] - M.oy[m]);
            I.top.v = Math.max(I.top.v, v);
            if (v > LIM.v) { K.fail(`${id}: dust ${m} moves ${v.toFixed(0)} px/s, frame ${f}`); break; }
            if (home > 3) { I.top.fade = Math.max(I.top.fade, fade); if (fade > LIM.fade) { K.fail(`${id}: dust ${m} vanishes ${fade.toFixed(2)} a second, ${home.toFixed(0)} px from home, frame ${f}`); break; } }
          }
        }
      } else if (p) { I.clean = false; I.acc = 0; }   // a replay: that loop is not counted
      if (p && p.whole && !st.whole) I.apart++;
      I.prev = { t: st.t, rep: st.replays, whole: st.whole, missing: st.missing, seen: st.seen, x: Float64Array.from(M.x), y: Float64Array.from(M.y), a: Float64Array.from(M.a) };
    });
    let ok = true;
    info.forEach((I, j) => {
      const id = K.tiles[j].cand.id;
      console.log(`${id}: ${K.tiles[j].st.M.n} grains, up to ${I.seenMax} in the air; whole ${I.loops.map(s => s.toFixed(1)).join(', ')} s a loop; came apart ${I.apart} times, up to ${(100 * I.maxMiss).toFixed(0)} % of the word; farthest ${I.far.toFixed(0)} px; fastest ${I.top.v.toFixed(0)} px/s; fade away from home ${I.top.fade.toFixed(2)}/s; word change ${(100 * I.top.pop).toFixed(0)} %/s`);
      if (!I.loops.length || I.loops.some(s => s < LIM.whole)) { console.log(`   ${id}: the word must stay whole at least ${LIM.whole} s a loop`); ok = false; }
      if (I.maxMiss < .97) { console.log(`   ${id}: not all of the word turns to dust`); ok = false; }
      if (I.apart < 3) { console.log(`   ${id}: came apart ${I.apart} times, not on every loop and replay`); ok = false; }
      if (I.far < 15) { console.log(`   ${id}: the dust never leaves`); ok = false; }
    });
    const pk = K.picked();
    if (!pk || pk.pick !== K.tiles[1].cand.id) { console.log('the pick was not taken:', JSON.stringify(pk)); ok = false; }
    return ok;
  },
};
