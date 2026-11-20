// The title screen's scene, direction «Трос»: an art-pick page on tools/pick-kit (its README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/title-tether.js <out-dir> [--break drift|fast|anchor|far]
module.exports = {
  title: 'Заставка',
  heading: 'Заставка: «Трос»',
  lead: `
<p class="lead">Сцена за главным меню, кадр 16:9 как экран игры 1280×720 («Крупно 1:1» — в полный размер). Герой в скафандре игры — поношенная охра, латунный шлем, — около 6,5 % высоты кадра — обмяк и медленно поворачивается в пустоте; трос уходит за кадр, корабля не видно. Почти весь кадр — тёмный космос: редкие звёзды разной яркости в два слоя плывут влево, несколько ярких с крошечным крестиком. Насыщенный цвет — только у крошечного красного огонька на груди.</p>
<p class="lead">«Свет: ступени» — правило стиля: серп, атмосфера и ореол солнца — несколько чистых ступеней тона с зерном внутри, края гладкие. «Свет: мягкий» — исключение на пробу: граница света и тени, атмосфера и солнце — плавным переходом. Выбор света сохраняется вместе со сценой. Левая треть спокойна под меню: «Меню: вкл» накладывает его макет — раз за цикл (~36 с) сначала одна сцена, потом название, потом по очереди пункты.</p>
<p class="miss">Не показано: звук; настоящий шрифт меню и время появления — игры, здесь только макет; отрисовка Godot (сглаживание, зерно) выйдет чуть иначе. Герой нарисован здесь заново по пропорциям фигуры игры, лицом к зрителю.</p>`,
  tile: [1280, 720],
  kitBars: ['speedMode', 'playMode'],
  toggles: [
    { id: 'lightMode', bar: 1, label: 'Свет:', short: 'свет', key: 'light', saved: true, opts: [['ступени', 'steps'], ['мягкий', 'soft']] },
    { id: 'menuMode', bar: 1, label: 'Меню:', key: 'menu', opts: [['вкл', true], ['выкл', false]] },
  ],
  pick: { doc: 'picks/titleScene', subject: 'Заставка', what: ['заставку', 'заставкой'] },

  page() {
    const TAU = Math.PI * 2, THIRD = LW / 3;
    const unit = (x, y) => { const l = Math.hypot(x, y) || 1; return [x / l, y / l]; };
    const SPACE = '#03050a', STAR = [223, 232, 245], RED = [255, 46, 34];
    const HS = 1.05;                                  // page px per world px: the hero ~46 px, 6.5 % of the frame
    const HB = 120;                                   // the hero's buffer, page px square
    const ATTACH = [4.8, 4];                          // the tether's clip on the belt
    const LAMP_AT = [2.2, -6.6];                      // the suit lamp on the chest plate
    const H = 1 / 120, ITER = 24;                     // the tether's step and its constraint passes
    // ---------- the planet of «Серп» and «Обрыв»: lit from behind it, upper right ----------
    const PL = { c: [900, 820], r: 480 }, LIT = unit(.55, -.83);
    const CRES = [[26, [40, 56, 56]], [13, [98, 118, 106]], [4.5, [196, 206, 186]]];   // the terminator's offset, px, and its tone
    // ---------- «Против солнца» ----------
    const SUN = [840, 290], HALO_R = 100, CORE_R = 6.5;
    // ---------- how each scene moves ----------
    const ANCHOR_A = [1100, -420], BASE_A = [760, 250];
    const DRIFT_B = [45, 20], TURN_B = 52, TURN_C = 46, BASE_C = [794, 334];
    const LA = Math.hypot(BASE_A[0] - ANCHOR_A[0], BASE_A[1] - ANCHOR_A[1]), UA = Math.atan2(BASE_A[1] - ANCHOR_A[1], BASE_A[0] - ANCHOR_A[0]);
    const MOTION = {
      // swinging on a taut line round its far end, rocking ±0.45 rad
      A: t => { const f = UA + .018 * Math.sin(TAU * t / 29) + .005 * Math.sin(TAU * t / 11 + .7); return { c: [ANCHOR_A[0] + Math.cos(f) * LA, ANCHOR_A[1] + Math.sin(f) * LA], a: -.45 + .45 * Math.sin(TAU * t / 31) + .1 * Math.sin(TAU * t / 12.7 + 1.3) }; },
      // free: a slow loop of drift and a whole turn
      B: t => ({ c: [790 + DRIFT_B[0] * Math.sin(TAU * t / 71), 272 + DRIFT_B[1] * Math.sin(TAU * t / 53 + 1)], a: .6 + TAU * t / TURN_B + .15 * Math.sin(TAU * t / 17) }),
      C: t => ({ c: [BASE_C[0] + 9 * Math.sin(TAU * t / 57), BASE_C[1] + 7 * Math.sin(TAU * t / 43 + 2)], a: 2.4 - TAU * t / TURN_C + .12 * Math.sin(TAU * t / 15) }),
    };
    const toPage = (m, p) => { const q = rot(HS * p[0], HS * p[1], m.a); return [m.c[0] + q[0], m.c[1] + q[1]]; };
    // light on the hero: dimmed into shade, then a lit step and a rim cut on the light's side
    const LOOK = {
      A: { shade: [10, 13, 20], dim: .5, lit: [186, 196, 190], litA: .38, litD: 3.2, rim: [232, 236, 226], rimA: .95, rimD: 1.1, glint: [200, 225, 245], rope: [92, 88, 78], ropeHi: [200, 196, 176] },
      C: { shade: [8, 8, 11], dim: .9, lit: [255, 214, 160], litA: .3, litD: 2.6, rim: [255, 236, 204], rimA: 1, rimD: 1.3, glint: [255, 226, 190], rope: [14, 15, 19], ropeHi: [255, 226, 184] },
    };
    LOOK.B = LOOK.A;
    const lightAt = (k, p) => k === 'C' ? unit(SUN[0] - p[0], SUN[1] - p[1]) : LIT;
    // the tether of each scene: points, the far end pinned off frame or free, its slack, a weak pull and a travelling sway
    const ROPE = {
      A: { n: 44, anchor: ANCHOR_A, slack: 1.008, pull: [-2, 6], sway: 3, damp: .996 },
      B: { n: 14, anchor: null, len: 100, pull: [0, 0], sway: 10, damp: .993, out: 2 },
      C: { n: 36, anchor: [1100, 960], slack: 1.025, pull: [3, 4], sway: 4, damp: .996 },
    };

    // ---------- the hero: the rig's proportions seen from the front, spread and limp ----------
    const ARM = RIG.Arm, LEG = RIG.Leg, SH = 5.2, HIP = 2.5, NECK_Y = -10;
    function limp(h, t) {
      const w = (p, a, s) => a * Math.sin(TAU * t / p + s + h.seed);
      // angles from hanging straight down, outward positive; forearms float up, knees fall apart, feet nearer
      h.arms = [[1.82 + w(15, .12, 0), 2.32 + w(12, .16, 1)], [2.02 + w(18, .12, 2), 2.38 + w(14, .15, 3)]];
      h.legs = [[.34 + w(17, .07, 4), .04 + w(13, .1, 5)], [.42 + w(19, .07, 6), .12 + w(11, .1, 7)]];
      h.tilt = .3 + w(21, .12, 8);
    }
    function joints(h) {
      const J = { arms: [], legs: [] };
      [-1, 1].forEach((s, i) => {
        const o = [s * SH, -7.6], [u, f] = h.arms[i], e = [o[0] + s * Math.sin(u) * ARM[0], o[1] + Math.cos(u) * ARM[0]], q = [e[0] + s * Math.sin(f) * ARM[1], e[1] + Math.cos(f) * ARM[1]];
        J.arms.push([o, e, q]);
        const p = [s * HIP, 4.2], [th, sn] = h.legs[i], k = [p[0] + s * Math.sin(th) * LEG[0], p[1] + Math.cos(th) * LEG[0]], ft = [k[0] + s * Math.sin(sn) * LEG[1], k[1] + Math.cos(sn) * LEG[1]];
        J.legs.push([p, k, ft, [ft[0] + s * Math.sin(sn) * 2.2, ft[1] + Math.cos(sn) * 2.2]]);
      });
      const hc = rot(0, -6.3, h.tilt);
      J.helm = [hc[0], NECK_Y + hc[1]];
      return J;
    }
    // the basic suit's tones, the middle of each of the game's own pictures (game/art/hd/hero_*.png): worn ochre, a brass shell
    const SUIT = { torso: '#765542', plate: '#817256', belt: '#473c31', arm: '#7d5b46', glove: '#5c5044', leg: '#6a5e44', boot: '#413830', pack: '#4f4436', helm: '#99825a', collar: '#44413d', visor: '#14171c' };
    function limbLine(g, pts, wd, col) {
      g.beginPath(); g.moveTo(pts[0][0], pts[0][1]); for (let i = 1; i < pts.length; i++) g.lineTo(pts[i][0], pts[i][1]);
      g.strokeStyle = INK; g.lineWidth = wd + 1.1; g.stroke(); g.strokeStyle = col; g.lineWidth = wd; g.stroke();
    }
    function drawAstro(g, h) {
      const J = h.J, S = SUIT;
      g.lineCap = 'round'; g.lineJoin = 'round';
      for (const L of J.legs) { limbLine(g, L.slice(0, 3), LEG[2] * .9, S.leg); limbLine(g, L.slice(2), 5, S.boot); }
      for (const A of J.arms) { limbLine(g, A, ARM[2] * .9, S.arm); g.beginPath(); g.arc(A[2][0], A[2][1], 2.1, 0, TAU); g.fillStyle = S.glove; g.fill(); g.strokeStyle = INK; g.lineWidth = .6; g.stroke(); }
      rr(g, -7.6, -10.8, 15.2, 11.5, 2.2); g.fillStyle = S.pack; g.fill(); g.strokeStyle = INK; g.lineWidth = .6; g.stroke();
      rr(g, -6.2, -10.4, 12.4, 16.6, 3.2); g.fillStyle = S.torso; g.fill(); g.strokeStyle = INK; g.lineWidth = .6; g.stroke();
      rr(g, -3.4, -8, 6.8, 4.4, .9); g.fillStyle = S.plate; g.fill();
      g.fillStyle = S.belt; g.fillRect(-5.6, 3.2, 11.2, 1.8);
      g.beginPath(); g.ellipse(0, NECK_Y - .4, 3.8, 1.5, 0, 0, TAU); g.fillStyle = S.collar; g.fill();
      const c = J.helm;
      g.beginPath(); g.arc(c[0], c[1], HELM_R, 0, TAU); g.fillStyle = S.helm; g.fill(); g.strokeStyle = INK; g.lineWidth = .65; g.stroke();
      g.save(); g.translate(c[0], c[1]); g.rotate(h.tilt);
      g.beginPath(); g.ellipse(0, .5, 4.5, 3.7, 0, 0, TAU); g.fillStyle = S.visor; g.fill(); g.strokeStyle = S.collar; g.lineWidth = .7; g.stroke();
      g.restore();
    }
    // the hero's outline on the page: its joints and limb widths, the helmet a circle
    function boxOf(h, m) {
      const J = h.J, b = [1e9, 1e9, -1e9, -1e9], pts = [], put = (p, r) => { const q = toPage(m, p); pts.push([q[0], q[1], r * HS]); b[0] = Math.min(b[0], q[0] - r * HS); b[1] = Math.min(b[1], q[1] - r * HS); b[2] = Math.max(b[2], q[0] + r * HS); b[3] = Math.max(b[3], q[1] + r * HS); };
      for (const L of J.arms) for (const p of L) put(p, 2.4);
      for (const L of J.legs) for (const p of L) put(p, 2.8);
      put(J.helm, HELM_R); put([0, -4], 7);
      return { b, pts };
    }

    // ---------- the tether: a verlet chain, its near end at the hero's belt ----------
    function newRope(k) {
      const R = ROPE[k], a = toPage(MOTION[k](-20), ATTACH);
      const end = R.anchor || [a[0] - R.len * .9, a[1] + R.len * .4];
      const len = R.anchor ? Math.hypot(end[0] - a[0], end[1] - a[1]) * R.slack : R.len;
      const r = { k, n: R.n, seg: len / (R.n - 1), p: [], q: [], t: -20 };
      for (let i = 0; i < R.n; i++) { const u = i / (R.n - 1), p = [lerp(a[0], end[0], u), lerp(a[1], end[1], u)]; r.p.push(p); r.q.push(p.slice()); }
      while (r.t < 0) subRope(r, r.t += H);
      return r;
    }
    function subRope(r, ts) {
      const R = ROPE[r.k], n = r.n, p = r.p, q = r.q, A = toPage(MOTION[r.k](ts), ATTACH), last = R.anchor ? n - 1 : n;
      const far = R.anchor || p[n - 1], dl = Math.hypot(far[0] - A[0], far[1] - A[1]) || 1, perp = [-(far[1] - A[1]) / dl, (far[0] - A[0]) / dl];
      for (let i = 1; i < last; i++) {
        const s = R.sway * Math.sin(TAU * ts / 9.5 - i * .22);
        let fx = R.pull[0] + perp[0] * s, fy = R.pull[1] + perp[1] * s;
        if (R.out) { const dx = p[i][0] - A[0], dy = p[i][1] - A[1], d = Math.hypot(dx, dy) || 1; fx += dx / d * R.out * i / n; fy += dy / d * R.out * i / n; }
        const vx = (p[i][0] - q[i][0]) * R.damp, vy = (p[i][1] - q[i][1]) * R.damp;
        q[i][0] = p[i][0]; q[i][1] = p[i][1];
        p[i][0] += vx + fx * H * H; p[i][1] += vy + fy * H * H;
      }
      const pin = () => { p[0][0] = A[0]; p[0][1] = A[1]; q[0][0] = A[0]; q[0][1] = A[1]; if (R.anchor) { p[n - 1][0] = R.anchor[0]; p[n - 1][1] = R.anchor[1]; } };
      const fix = (i, j, rest, k) => {
        const a = p[i], b = p[j], dx = b[0] - a[0], dy = b[1] - a[1], d = Math.hypot(dx, dy) || 1e-6;
        if (k < 1 && d > rest) return;   // the bend only pushes a fold open
        const wa = i === 0 || (R.anchor && i === n - 1) ? 0 : 1, wb = j === 0 || (R.anchor && j === n - 1) ? 0 : 1;
        if (wa + wb === 0) return;
        const m = (d - rest) / d * k / (wa + wb);
        a[0] += dx * m * wa; a[1] += dy * m * wa; b[0] -= dx * m * wb; b[1] -= dy * m * wb;
      };
      pin();
      for (let it = 0; it < ITER; it++) {
        for (let i = 0; i < n - 1; i++) fix(i, i + 1, r.seg, 1);
        for (let i = 0; i < n - 2; i++) fix(i, i + 2, r.seg * 1.94, .25);
        pin();
      }
    }
    // the chain smoothed twice (Chaikin), ends kept
    function smooth(P) {
      let a = P;
      for (let k = 0; k < 2; k++) {
        const b = [a[0]];
        for (let i = 0; i < a.length - 1; i++) { const p = a[i], q = a[i + 1]; b.push([.75 * p[0] + .25 * q[0], .75 * p[1] + .25 * q[1]], [.25 * p[0] + .75 * q[0], .25 * p[1] + .75 * q[1]]); }
        b.push(a[a.length - 1]); a = b;
      }
      return a;
    }
    function poly(c, P, from, to) { c.beginPath(); c.moveTo(P[from][0], P[from][1]); for (let i = from + 1; i <= to; i++) c.lineTo(P[i][0], P[i][1]); }
    function drawRope(T, L) {
      const c = T.c, k = T.cand.kind, P = smooth(T.st.rope.p), soft = OPT.light === 'soft';
      c.lineCap = 'round'; c.lineJoin = 'round';
      poly(c, P, 0, P.length - 1); c.strokeStyle = css(L.rope); c.lineWidth = 1.4; c.stroke();
      // the lit edge, half a px toward the light; against the sun it dims with distance, in steps or smoothly
      const hi = P.map(p => { const l = lightAt(k, p); return [p[0] + l[0] * .6, p[1] + l[1] * .6]; });
      const alpha = p => { if (k !== 'C') return .72; const d = Math.hypot(p[0] - SUN[0], p[1] - SUN[1]); return soft ? .85 * Math.exp(-d / 220) + .06 : d < 180 ? .8 : d < 340 ? .34 : .1; };
      c.lineWidth = .55;
      for (let i = 0; i < hi.length - 1; i += 4) { const j = Math.min(hi.length - 1, i + 4); poly(c, hi, i, j); c.strokeStyle = css(L.ropeHi, alpha(P[i])); c.stroke(); }
      if (k === 'B') {   // the cut end: strands splayed and curling, slowly swaying
        const e = P[P.length - 1], f = P[P.length - 4], d = Math.atan2(e[1] - f[1], e[0] - f[0]);
        for (let i = 0; i < 6; i++) {
          const a = d + (i - 2.5) * .26 + .14 * Math.sin(T.time * .55 + i * 1.7), l = 4.5 + (i * 37 % 5) * .9, cv = (i % 2 ? .5 : -.45);
          const m = [e[0] + Math.cos(a) * l * .55, e[1] + Math.sin(a) * l * .55], z = [e[0] + Math.cos(a + cv) * l, e[1] + Math.sin(a + cv) * l];
          c.beginPath(); c.moveTo(e[0], e[1]); c.quadraticCurveTo(m[0], m[1], z[0], z[1]); c.strokeStyle = css(i % 3 ? L.rope : L.ropeHi, i % 3 ? 1 : .55); c.lineWidth = .55; c.stroke();
        }
      }
    }

    // ---------- buffers: stars, the planet or the sun, the hero; made again on a new scale ----------
    const canvas = (w, h) => { const v = document.createElement('canvas'); v.width = Math.max(1, Math.round(w)); v.height = Math.max(1, Math.round(h)); return { v, g: v.getContext('2d') }; };
    // watercolour grain: fine specks over a few soft blots of pooled pigment, tiling; laid only inside a shape
    function grain(strength) {
      const N = 96, b = canvas(N, N), im = b.g.createImageData(N, N), r = rng(31);
      const blot = (x, y) => .5 + .25 * Math.sin(TAU * (x * 2 + y) / N + 1.3) * Math.sin(TAU * (y * 3 - x) / N + .4) + .25 * Math.sin(TAU * (x + y * 2) / N * 2 + 2.1);
      for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
        const i = (y * N + x) * 4, dark = r() < .55, a = (dark ? r() * 26 + blot(x, y) * 22 : r() * 14) * strength;
        im.data[i] = im.data[i + 1] = im.data[i + 2] = dark ? 0 : 255; im.data[i + 3] = Math.floor(a);
      }
      b.g.putImageData(im, 0, 0);
      return b.v;
    }
    const GRAIN = grain(1), GRAIN_SOFT = grain(.45);
    const grainOver = (b, g) => { g.save(); g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'source-atop'; g.fillStyle = g.createPattern(g === b.g && b.soft ? GRAIN_SOFT : GRAIN, 'repeat'); g.fillRect(0, 0, b.v.width, b.v.height); g.restore(); };
    // stars: most faint, some brighter, a handful bright and a few of those with a tiny four-point glint
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
    // the lit side: steps of «disc minus an offset disc», each a crisp crescent with two sharp horns; or one soft terminator
    function crescent(S, soft) {
      const b = canvas(LW * S, LH * S), g = b.g, C = PL.c, R = PL.r;
      b.soft = soft;
      g.setTransform(S, 0, 0, S, 0, 0);
      if (!soft) {
        for (const [d, col] of CRES) {
          g.save(); disc(g, C, R); g.clip();
          g.beginPath(); g.rect(-10, -10, LW + 20, LH + 20); g.arc(C[0] - LIT[0] * d, C[1] - LIT[1] * d, R, 0, TAU);
          g.fillStyle = css(col); g.fill('evenodd'); g.restore();
        }
      } else {
        disc(g, C, R); g.fillStyle = css([150, 168, 152]); g.fill();
        const o = [C[0] - LIT[0] * 30, C[1] - LIT[1] * 30], hole = g.createRadialGradient(o[0], o[1], 0, o[0], o[1], R + 8);
        hole.addColorStop(0, 'rgba(0,0,0,1)'); hole.addColorStop((R - 14) / (R + 8), 'rgba(0,0,0,1)'); hole.addColorStop(1, 'rgba(0,0,0,0)');
        g.globalCompositeOperation = 'destination-out'; disc(g, o, R + 8); g.fillStyle = hole; g.fill();
        const li = [C[0] - LIT[0] * 8, C[1] - LIT[1] * 8], edge = g.createRadialGradient(li[0], li[1], R - 14, li[0], li[1], R);
        edge.addColorStop(0, 'rgba(224,230,212,0)'); edge.addColorStop(1, 'rgba(224,230,212,.85)');
        g.globalCompositeOperation = 'source-atop'; g.fillStyle = edge; g.fillRect(0, 0, LW, LH);
        g.globalCompositeOperation = 'source-over';
      }
      // cloud streaks along the curve, only on the lit paint: [angle from the lit point, depth px, length px, width, light]
      const base = Math.atan2(LIT[1], LIT[0]);
      g.globalCompositeOperation = 'source-atop'; g.lineCap = 'round';
      for (const [th, dep, len, w, lit] of [[-.55, 6, 60, 1.6, 0], [-.32, 11, 90, 2.2, 0], [-.12, 4, 70, 1.4, 0], [.08, 9, 54, 2, 0], [.3, 5, 80, 1.6, 0], [.62, 7, 46, 1.6, 0], [-.85, 3.5, 40, 1.2, 0], [-.22, 2, 50, 1, 1], [.18, 2.4, 40, 1.1, 1], [-.66, 1.8, 30, .9, 1]]) {
        const da = len / R / 2;
        g.beginPath(); g.arc(C[0], C[1], R - dep, base + th - da, base + th + da);
        g.strokeStyle = lit ? css([214, 220, 204], .45) : css([24, 34, 36], .5); g.lineWidth = w; g.stroke();
      }
      g.globalCompositeOperation = 'source-over';
      grainOver(b, g);
      return b.v;
    }
    // the air: a thin line outside the lit limb, thinning (steps) or fading (soft) toward the horns
    function air(S, soft) {
      const b = canvas(LW * S, LH * S), g = b.g, C = PL.c, R = PL.r;
      b.soft = soft;
      g.setTransform(S, 0, 0, S, 0, 0);
      if (!soft) {
        for (const [t, reach, col, a] of [[6, 1.25, [90, 140, 190], .4], [2.2, .95, [180, 215, 236], .85]]) {
          const o = t / (1 - Math.cos(reach));   // a disc pushed toward the light, its rim t outside the limb at the lit point, meeting it reach radians away
          g.save(); disc(g, [C[0] + LIT[0] * o, C[1] + LIT[1] * o], R + t - o); g.clip();
          g.beginPath(); g.rect(-10, -10, LW + 20, LH + 20); g.arc(C[0], C[1], R, 0, TAU); g.fillStyle = css(col, a); g.fill('evenodd'); g.restore();
        }
        grainOver(b, g);
      } else {
        for (const [w, a] of [[18, .5], [5, .75]]) {
          const ring = g.createRadialGradient(C[0], C[1], R, C[0], C[1], R + w);
          ring.addColorStop(0, css([150, 196, 230], a)); ring.addColorStop(1, css([150, 196, 230], 0));
          disc(g, C, R + w); g.fillStyle = ring; g.fill();
        }
        const fade = g.createLinearGradient(C[0], C[1], C[0] + LIT[0] * R, C[1] + LIT[1] * R);
        fade.addColorStop(0, 'rgba(0,0,0,0)'); fade.addColorStop(.35, 'rgba(0,0,0,.25)'); fade.addColorStop(1, 'rgba(0,0,0,1)');
        g.globalCompositeOperation = 'destination-in'; g.fillStyle = fade; g.fillRect(0, 0, LW, LH); g.globalCompositeOperation = 'source-over';
      }
      return b.v;
    }
    // the planet: its air, the night side pure black over the stars, the crescent
    function planet(S, soft) {
      const b = canvas(LW * S, LH * S), g = b.g;
      g.drawImage(air(S, soft), 0, 0);
      g.setTransform(S, 0, 0, S, 0, 0); disc(g, PL.c, PL.r); g.fillStyle = '#000'; g.fill();
      g.setTransform(1, 0, 0, 1, 0, 0); g.drawImage(crescent(S, soft), 0, 0);
      return b.v;
    }
    // the sun: a small hard white-warm disc in one irregular halo; or a soft falloff
    function sun(S, soft) {
      const b = canvas(LW * S, LH * S), g = b.g;
      b.soft = soft;
      g.setTransform(S, 0, 0, S, 0, 0);
      if (!soft) {
        // one round warm wash, its edge barely wobbling, the light grain inside
        g.beginPath();
        for (let i = 0; i <= 360; i++) { const th = i / 360 * TAU, r = HALO_R + 2.2 * Math.sin(3 * th + 1) + 1.4 * Math.sin(5 * th + 2.3), x = SUN[0] + Math.cos(th) * r, y = SUN[1] + Math.sin(th) * r; i ? g.lineTo(x, y) : g.moveTo(x, y); }
        g.closePath(); g.fillStyle = css([255, 202, 140], .13); g.fill();
        b.soft = true; grainOver(b, g);
      } else {
        const f = g.createRadialGradient(SUN[0], SUN[1], 0, SUN[0], SUN[1], 300);
        [[0, .95], [.025, .6], [.08, .24], [.24, .08], [.55, .02], [1, 0]].forEach(([u, a]) => f.addColorStop(u, css([255, 228, 190], a)));
        disc(g, SUN, 300); g.fillStyle = f; g.fill();
      }
      disc(g, SUN, CORE_R); g.fillStyle = css([255, 247, 230]); g.fill();
      return b.v;
    }
    function buffers(T) {
      const S = T.S;
      return { S, sky: {}, far: starLayer(S, 7, 330, 0, .08, .45, 0, 0), near: starLayer(S, 9, 80, .2, .25, .8, 12, 4), hero: canvas(HB * S, HB * S), step: canvas(HB * S, HB * S) };
    }
    const skyOf = T => { const k = OPT.light; return T.B.sky[k] || (T.B.sky[k] = (T.cand.kind === 'C' ? sun : planet)(T.S, k === 'soft')); };

    // ---------- the hero on the page: shade, a lit step, a rim, the visor's glint and the lamp ----------
    function drawFigure(T, m, L) {
      const B = T.B, S = T.S, h = T.st.h, hb = B.hero, sb = B.step, w = hb.v.width, ht = hb.v.height, g = hb.g, li = lightAt(T.cand.kind, m.c);
      g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'source-over'; g.globalAlpha = 1; g.clearRect(0, 0, w, ht);
      g.setTransform(S, 0, 0, S, 0, 0); g.translate(HB / 2, HB / 2); g.rotate(m.a); g.scale(HS, HS);
      drawAstro(g, h);
      g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'source-atop'; g.fillStyle = css(L.shade, L.dim); g.fillRect(0, 0, w, ht);
      g.globalCompositeOperation = 'source-over';
      const c = T.c, at = [m.c[0] - HB / 2, m.c[1] - HB / 2];
      c.setTransform(S, 0, 0, S, 0, 0); c.drawImage(hb.v, at[0], at[1], HB, HB);
      // a step of light: the body less itself moved d toward the light
      for (const [col, a, d] of [[L.lit, L.litA, L.litD], [L.rim, L.rimA, L.rimD]]) {
        const r = sb.g;
        r.setTransform(1, 0, 0, 1, 0, 0); r.globalCompositeOperation = 'source-over'; r.clearRect(0, 0, w, ht); r.drawImage(hb.v, 0, 0);
        r.globalCompositeOperation = 'source-in'; r.fillStyle = css(col); r.fillRect(0, 0, w, ht);
        r.globalCompositeOperation = 'destination-out'; r.drawImage(hb.v, -li[0] * d * S, -li[1] * d * S);
        r.globalCompositeOperation = 'source-over';
        c.globalAlpha = a; c.drawImage(sb.v, at[0], at[1], HB, HB); c.globalAlpha = 1;
      }
      const v = toPage(m, h.J.helm), la = Math.atan2(li[1], li[0]);
      c.beginPath(); c.arc(v[0], v[1], 3.1 * HS, la - .55, la + .55); c.strokeStyle = css(L.glint, .7); c.lineWidth = .9; c.lineCap = 'round'; c.stroke();
      // the suit lamp: on a third of a second in every 3.4, a hard dot and one step of light
      const p = toPage(m, LAMP_AT), on = ((T.time + 1.1) % 3.4) < .32;
      if (on) { disc(c, p, 3); c.fillStyle = css(RED, .2); c.fill(); }
      c.fillStyle = on ? css(RED) : css([92, 22, 18], .8); c.fillRect(p[0] - .8, p[1] - .8, 1.6, 1.6);
    }

    // ---------- the menu's mock, at the left: the name, then the choices, once a loop ----------
    const ITEMS = ['Новая игра', 'Миры', 'Играть вместе', 'Настройки', 'Выйти'], MENU_L = 36;
    function spaced(c, s, x, y, sp) { for (const ch of s) { c.fillText(ch, x, y); x += c.measureText(ch).width + sp; } }
    function drawMenu(T) {
      const c = T.c, m = T.st.mt % MENU_L, out = 1 - sm((m - 33) / 1.6);
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      const an = sm((m - 2.5) / 1.8) * out;
      if (an > .002) {
        c.font = '300 50px system-ui,"Segoe UI",sans-serif'; c.fillStyle = css([226, 222, 212], .92 * an); spaced(c, 'CASTOFF', 82, 318, 11);
        c.fillStyle = css([226, 222, 212], .3 * an); c.fillRect(84, 340, 42, 1);
      }
      c.font = '300 21px system-ui,"Segoe UI",sans-serif';
      ITEMS.forEach((s, i) => {
        const a = sm((m - 4.8 - i * .35) / 1) * out; if (a <= .002) return;
        c.fillStyle = css([218, 214, 204], (i ? .58 : .9) * a); spaced(c, s, 84, 396 + i * 40, .7);
        if (!i) { c.fillStyle = css([218, 214, 204], .6 * a); c.fillRect(64, 389, 10, 1); }
      });
    }

    // ---------- the scene ----------
    function scene(T, dt) {
      const S = T.S, c = T.c, k = T.cand.kind, st = T.st, L = LOOK[k];
      if (!T.B || T.B.S !== S) T.B = buffers(T);
      if (st.menuWas !== OPT.menu) { st.menuWas = OPT.menu; st.mt = 0; }
      st.mt += dt;
      const r = st.rope;
      while (r.t + H <= T.time + 1e-9) subRope(r, r.t += H);
      const m = MOTION[k](T.time);
      limp(st.h, T.time); st.h.J = joints(st.h);
      st.m = m; const bx = boxOf(st.h, m); st.box = bx.b; st.pts = bx.pts;
      c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      c.fillStyle = SPACE; c.fillRect(0, 0, T.cv.width, T.cv.height);
      const W = T.B.far.width;
      for (const [buf, v] of [[T.B.far, 2.2], [T.B.near, 4.5]]) { const o = ((T.time * v * S) % W + W) % W; c.drawImage(buf, -o, 0); c.drawImage(buf, W - o, 0); }
      c.drawImage(skyOf(T), 0, 0);
      c.setTransform(S, 0, 0, S, 0, 0);
      drawRope(T, L);
      drawFigure(T, m, L);
      if (OPT.menu) drawMenu(T);
      c.setTransform(S, 0, 0, S, 0, 0);
      if (OPT.slow) { c.font = '13px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'right'; c.fillStyle = 'rgba(214,190,140,.85)'; c.fillText('×¼', LW - 20, LH - 16); c.textAlign = 'left'; }
    }

    const TIME_STARS = 'звёзды плывут влево двумя слоями, 2 и 4,5 px/с';
    const cands = [
      {
        id: 'serp', name: 'Серп', kind: 'A',
        idea: 'Огромная планета внизу справа, свет из-за неё сверху справа: тонкий серп — три ступени тона, толще всего посередине, с двумя острыми рогами (один виден) — и тонкая линия атмосферы вдоль освещённого края, сходящая на нет к рогам. Ночная сторона чёрная и закрывает звёзды. Герой висит на тросе; трос почти натянут, чуть провисает и уходит вверх за кадр.',
        time: `Герой покачивается на тросе ±0,45 рад за ~31 с и ходит маятником на несколько px; по тросу бежит слабая волна за ~9 с; ${TIME_STARS}.`,
      },
      {
        id: 'obryv', name: 'Обрыв', kind: 'B',
        idea: 'Та же планета, но трос перебит: за героем тянется короткий провисший обрывок, его конец растрёпан на пряди. Ничто не держит — герой дрейфует над серпом.',
        time: `Полный оборот за ~52 с; дрейф ±45 px за ~70 с; обрывок колышется и тянется следом, пряди покачиваются; ${TIME_STARS}.`,
      },
      {
        id: 'protiv-solntsa', name: 'Против солнца', kind: 'C',
        idea: 'Без планеты: маленькое далёкое солнце — твёрдый бело-тёплый диск в одном круглом тёплом ореоле (в «мягком» — плавное затухание). Герой рядом, его тёмный силуэт заходит в ореол, со стороны солнца — яркая кромка; трос — тонкая тёмная линия со светлым краем, уходит вниз за кадр.',
        time: `Полный оборот за ~46 с в другую сторону; лёгкий дрейф ±9 px; трос колышется; ${TIME_STARS}.`,
      },
    ];
    return {
      cands,
      init(T) { T.st = { h: { seed: 1.3 }, rope: newRope(T.cand.kind), mt: 0, menuWas: OPT.menu }; },
      scene,
      expose: { ROPE, THIRD, TAU, LW, LH, SUN, HALO_R, CORE_R },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    drift: [['c: [790 + DRIFT_B[0]', 'c: [440 + DRIFT_B[0]']],               // the free hero drifts into the menu's third
    fast: [['TURN_B = 52, TURN_C = 46', 'TURN_B = 4, TURN_C = 46']],           // a turn in 4 s: no longer slow
    anchor: [['const ANCHOR_A = [1100, -420]', 'const ANCHOR_A = [1100, 60]']], // the line ends in frame: the ship would show
    far: [['BASE_C = [794, 334]', 'BASE_C = [700, 420]']],                    // the hero no longer reaches into the sun's halo
  },

  check(K) {
    const X = K.X, M = 16;   // the hero keeps 16 px from every edge
    const LIM = { hero: 8, turn: .3, rope: 30 };   // page px a second, radians a second, page px a second
    K.at(900, () => K.seg('lightMode', 1)); K.at(4800, () => K.seg('lightMode', 0));
    K.at(1200, () => K.seg('speedMode', 1)); K.at(2400, () => K.seg('speedMode', 0));
    K.at(3000, () => K.seg('playMode', 1)); K.at(3050, () => K.seg('menuMode', 1)); K.at(3100, () => K.seg('menuMode', 0)); K.at(3200, () => K.seg('playMode', 0));
    K.at(3300, () => K.ptrAll([700, 300])); K.at(3400, () => K.ptrAll(null));
    K.at(6400, () => K.click(1, '.pick'));
    const per = K.tiles.map(() => ({ turned: 0, prev: null }));
    const top = { hero: 0, turn: 0, rope: 0 }, span = K.tiles.map(() => [1e9, 1e9, -1e9, -1e9]), sunGap = [1e9, -1e9];
    let frames = 0;
    K.run(6600, (t, i) => {
      const p = per[K.tiles.indexOf(t)], st = t.st, b = st.box, k = t.cand.kind, id = t.cand.id, R = st.rope.p, sp = span[K.tiles.indexOf(t)];
      frames++;
      if (b[0] < X.THIRD + 20) K.fail(`${id}: hero at x ${b[0].toFixed(1)}, in the menu's third, frame ${i}`);
      if (b[0] < M || b[1] < M || b[2] > X.LW - M || b[3] > X.LH - M) K.fail(`${id}: hero box ${b.map(v => v.toFixed(0))} leaves the frame, frame ${i}`);
      sp[0] = Math.min(sp[0], b[0]); sp[1] = Math.min(sp[1], b[1]); sp[2] = Math.max(sp[2], b[2]); sp[3] = Math.max(sp[3], b[3]);
      const out = q => q[0] < 0 || q[0] > X.LW || q[1] < 0 || q[1] > X.LH;
      if (X.ROPE[k].anchor) { if (!out(R[R.length - 1])) K.fail(`${id}: the tether ends in frame, frame ${i}`); }
      else if (R.some(out)) K.fail(`${id}: the cut tether leaves the frame, frame ${i}`);
      if (R.some(q => q[0] < X.THIRD && q[1] > 0 && q[1] < X.LH)) K.fail(`${id}: the tether crosses the menu's third, frame ${i}`);
      if (k === 'C') {   // the silhouette reaches into the halo and never covers the sun's disc
        const near = Math.min(...st.pts.map(q => Math.hypot(q[0] - X.SUN[0], q[1] - X.SUN[1]) - q[2]));
        sunGap[0] = Math.min(sunGap[0], near); sunGap[1] = Math.max(sunGap[1], near);
        if (near > X.HALO_R - 8) K.fail(`${id}: hero ${near.toFixed(0)} px from the sun, outside its halo, frame ${i}`);
        if (near < X.CORE_R + 6) K.fail(`${id}: hero covers the sun, frame ${i}`);
      }
      const now = { time: t.time, c: st.m.c.slice(), a: st.m.a, R: R.map(q => q.slice()) };
      if (p.prev && now.time - p.prev.time > 1e-6) {
        const dt = now.time - p.prev.time;
        const v = Math.hypot(now.c[0] - p.prev.c[0], now.c[1] - p.prev.c[1]) / dt, w = Math.abs(now.a - p.prev.a) / dt;
        let vr = 0; now.R.forEach((q, j) => { vr = Math.max(vr, Math.hypot(q[0] - p.prev.R[j][0], q[1] - p.prev.R[j][1]) / dt); });
        top.hero = Math.max(top.hero, v); top.turn = Math.max(top.turn, w); top.rope = Math.max(top.rope, vr);
        if (v > LIM.hero) K.fail(`${id}: hero moves ${v.toFixed(1)} px/s, frame ${i}`);
        if (w > LIM.turn) K.fail(`${id}: hero turns ${w.toFixed(2)} rad/s, frame ${i}`);
        if (vr > LIM.rope) K.fail(`${id}: tether moves ${vr.toFixed(1)} px/s, frame ${i}`);
        p.turned += now.a - p.prev.a;
      }
      p.prev = now;
    });
    const turns = per.map(p => Math.abs(p.turned) / X.TAU);
    console.log(`fastest: hero ${top.hero.toFixed(2)} px/s (limit ${LIM.hero}), turn ${top.turn.toFixed(3)} rad/s (limit ${LIM.turn}), tether ${top.rope.toFixed(1)} px/s (limit ${LIM.rope})`);
    console.log('turns over the run:', K.tiles.map((t, j) => `${t.cand.id} ${turns[j].toFixed(2)}`).join(', '), `| sim time ${K.tiles[0].time.toFixed(1)} s`);
    console.log('hero boxes:', K.tiles.map((t, j) => `${t.cand.id} ${span[j].map(v => v.toFixed(0)).join(',')}`).join(' | '));
    console.log(`C: hero's nearest edge ${sunGap[0].toFixed(0)}..${sunGap[1].toFixed(0)} px from the sun (halo ${X.HALO_R}, disc ${X.CORE_R})`);
    // «Обрыв» and «Против солнца» turn whole: at least once over the run
    return frames > 0 && turns[1] >= 1 && turns[2] >= 1;
  },
};
