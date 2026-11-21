// The drone's meeting as a vision, one scene in the user's order («Стена» picked): E, the core lights and rises, the wreck
// crumbles to stars while it grows, the blood winds into three rings, turns black, the eye opens; back, the rings become its
// cloud, then the plates from the debris and up to the head. An art-pick page on the pick kit (README.md).
// node pick-kit/build.js pick-kit/pages/drone-vision.js <out-dir> [--break crowd|stray|nan|eye|rings|drift|pale]
module.exports = {
  title: 'Видение ядра',
  heading: 'Пробуждение дрона: видение «Стена», одной сценой',
  window: false,
  lead: `
<p class="lead">Герой жмёт <b>E</b> у ядра в выбоине пола — и больше не двигается: начинается сцена. Интерфейс уходит, картинка гаснет и выцветает. Ядро разгорается, как после крови, и поднимается из ямы. Обломки осыпаются осколками и гаснут звёздами, а ядро растёт на месте. В пустоте они висят друг против друга, без веса: ядро тёмное, линза мёртвая, раза в четыре выше героя. Ядро берёт кровь: она летит из ладони и наматывается на него тремя кольцами, как орбиты, по одному. Когда полны все три, кровь медленно чернеет, в ней тлеют угли — и ядро открывает глаз и смотрит на героя. Потом всё обратно: ядро сжимается туда, откуда начало расти, чёрные кольца сжимаются с ним и становятся его облаком. Уже в обломках оно по одной тянет пластины по угольным дугам и поднимается к голове.</p>
<p class="lead">Огромное ядро нарисовано тем же кодом, что маленькое (drone.js): тот же контур, корка, ржавчина, линза и цвета; перо и зерно — в пикселях экрана, ничего не растянуто. Голова кружится, пока берут кровь и пока она чернеет, — на весь экран и слабее прежнего.</p>
<p class="miss">Не показано: звук; что видят друзья в кооперативе; отрисовка Godot (свет, шейдеры и цветокоррекция выйдут иначе). Интерфейс слева вверху — условный набросок, только чтобы видеть, что его нет во время сцены.</p>`,
  toggles: [],
  pick: { doc: 'picks/droneVision', subject: 'Видение ядра', what: ['сцену', 'сценой'] },

  page() {
    // ---------- the landing (Wreck.cs), as on the meeting page: page px, a cell 32 ----------
    const FL = 430, GR = 462, TAU = Math.PI * 2;
    const CORE0 = [364, 455.5], HOVER = [362, 414], HX = [420, GR], PALM = [13, -3.3], IDLE = [-9, -44];
    const BACK = HOVER;   // where the core shrinks back to: where it began to grow
    const BL = [84, 10, 14], BL_L = [138, 28, 28], BL_D = [34, 4, 7], BROWN = [34, 12, 9];
    const BLK = [22, 21, 24], BLK_L = [120, 118, 126], BLK_D = [6, 6, 8];
    const BV = [138, 18, 22], BV_L = [196, 64, 60], BV_D = [44, 5, 8];   // the blood in the vision: brighter, to read against space
    const MOTE_RGB = [[[76, 79, 85], [26, 27, 31]], [[96, 99, 105], [26, 27, 31]]];   // drawDrone's motes
    const ss = (u, a, b) => sm((u - a) / (b - a)), bump = (u, a, b, d) => ss(u, a, b) * (1 - ss(u, b, d));
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const fmt = v => v.toFixed(2).replace(/0$/, '').replace(/\.0$/, '').replace('.', ',');
    // the scene, in seconds from E's frame
    const TL = {
      press: .35, hud: [.35, .75], grade: [.35, 1.6], light: [.6, 1.5], lift: [1.3, 2.5],
      in: [2.8, 4.8], dim: [3.0, 4.4], reach: [4.8, 5.6],
      F: [5.8, 7.7, 9.6], fly: .2,
      black: [11.6, 13.9], spin: [11.6, 13.6], lens: [12.6, 14.0], dizzy: [5.5, 6.5, 13.6, 14.6],
      out: [16.8, 19.0], plates: [19.8, 1.0], plateFly: .25, rise: [23.4, 24.3], gradeOut: [23.8, 24.8], hudBack: [24.4, 24.8],
    };
    const END = TL.gradeOut[1], LOOP = END + 1;
    const LAND = i => TL.plates[0] + i * TL.plates[1];
    const DRONE_R = DS * DSC;   // the small drone beside the hero: page px a unit
    const LEVEL = .55;          // the dizziness: whole screen, lighter than the last breath
    // the vision's frame: the core about four heroes tall, the hero right of it
    const VC = [360, 266], VR = 27, KV = .62, HV = { foot: [790, 345], s: .4, a: -.1 };

    // ---------- pen and paint at any size: lines in screen px, never stretched ----------
    const cap = (x, m) => x / Math.sqrt(1 + (x / m) * (x / m));
    const pen = (R, w, m) => cap(w * R, m) / R;
    function gInk(c, s, ang, R, w, m) {
      const cs = Math.cos(ang), sn = Math.sin(ang), off = cap(1.4 * R, 2.6) / R;
      c.save(); c.rotate(ang); c.lineCap = 'round'; c.lineJoin = 'round';
      c.strokeStyle = 'rgba(214,220,226,.5)'; c.lineWidth = pen(R, 1, 1.6);
      for (const g of s.segs) {
        const nx = g[2] - s.cx, ny = g[3] - s.cy, L = Math.hypot(nx, ny) || 1;
        if ((nx * sn + ny * cs) / L < -.42) { const ix = -nx / L * off, iy = -ny / L * off; c.beginPath(); c.moveTo(g[0] + ix, g[1] + iy); c.quadraticCurveTo(g[2] + ix, g[3] + iy, g[4] + ix, g[5] + iy); c.stroke(); }
      }
      c.strokeStyle = INK; const lw = pen(R, w, m);
      for (const g of s.segs) { if (g[6] <= .05) continue; c.lineWidth = lw * g[6]; c.beginPath(); c.moveTo(g[0], g[1]); c.quadraticCurveTo(g[2], g[3], g[4], g[5]); c.stroke(); }
      c.restore();
    }
    function lyRust(c, b) {
      c.fillStyle = 'rgba(118,66,36,.85)'; trace(c, b.s); c.fill();
      c.save(); c.translate(b.x, b.y); c.scale(.45, .45); c.translate(-b.x, -b.y);
      c.fillStyle = 'rgba(70,36,20,.9)'; trace(c, b.s); c.fill(); c.restore();
    }
    function grainOf(c) {
      const g = document.createElement('canvas'); g.width = g.height = 64;
      const x = g.getContext('2d'), r = rng(7);
      for (let i = 0; i < 420; i++) { const d = r() < .62; x.fillStyle = d ? `rgba(0,0,0,${(.06 + r() * .16).toFixed(3)})` : `rgba(255,255,255,${(.03 + r() * .07).toFixed(3)})`; x.fillRect(r() * 64, r() * 64, 1 + (r() < .2), 1); }
      x.strokeStyle = 'rgba(0,0,0,.1)'; x.lineWidth = .8;
      for (let i = 0; i < 4; i++) { x.beginPath(); x.arc(r() * 64, r() * 64, 6 + r() * 14, r() * 6, r() * 6 + 1.6); x.stroke(); }
      return c.createPattern(g, 'repeat');
    }
    // the giant's watercolour, in device px: pooling with a darker tide line, grain, a few fibres; it tiles
    let PAPER = null;
    function paper() {
      if (PAPER) return PAPER;
      const N = 320, cv = document.createElement('canvas'); cv.width = cv.height = N;
      const x = cv.getContext('2d'), r = rng(4242), round = f => { for (const ox of [-N, 0, N]) for (const oy of [-N, 0, N]) { x.save(); x.translate(ox, oy); f(); x.restore(); } };
      for (let i = 0; i < 22; i++) {
        const b = blobAt(r, r() * N, r() * N, 16 + r() * 46), dark = r() < .6;
        round(() => { trace(x, b.s); x.fillStyle = dark ? 'rgba(0,0,0,.075)' : 'rgba(255,255,255,.035)'; x.fill(); x.strokeStyle = dark ? 'rgba(0,0,0,.12)' : 'rgba(255,255,255,.05)'; x.lineWidth = 1.3; x.stroke(); });
      }
      for (let i = 0; i < 5200; i++) { const d = r() < .62, px = r() * N, py = r() * N; x.fillStyle = d ? `rgba(0,0,0,${(.05 + r() * .14).toFixed(3)})` : `rgba(255,255,255,${(.025 + r() * .06).toFixed(3)})`; x.fillRect(px, py, 1 + (r() < .25), 1); }
      x.lineWidth = .8; x.strokeStyle = 'rgba(0,0,0,.08)';
      for (let i = 0; i < 26; i++) { const px = r() * N, py = r() * N, rad = 8 + r() * 30, a0 = r() * 6.283; round(() => { x.beginPath(); x.arc(px, py, rad, a0, a0 + 1.2); x.stroke(); }); }
      return PAPER = cv;
    }
    function dress(g, S) { g.S = S; g.grain = grainOf(g); g.gg = g.createPattern(paper(), 'repeat'); }
    // inside a clip: the paper, pinned to C so it moves with the thing and never scales with it
    function devGrain(g, C, a) {
      if (!g.gg || a <= .003) return;
      const S = g.S; g.save(); g.setTransform(1, 0, 0, 1, C[0] * S, C[1] * S); g.globalAlpha = a; g.fillStyle = g.gg; g.fillRect(-C[0] * S - 2, -C[1] * S - 2, LW * S + 4, LH * S + 4); g.restore();
    }

    // ---------- the core at any size: drone.js's picture, and what only the giant shows ----------
    const PITS = [[-2.9, -2.4, .34], [-1.1, -3.7, .2], [1.5, -3.2, .26], [-3.9, .2, .28], [-.9, 1.4, .17], [2.6, 2.9, .3], [.6, 4.1, .22], [-2.3, 3.5, .19], [-4.2, -1.5, .16], [3.6, -1.9, .18], [-1.7, -.6, .13], [-3.1, 2.1, .12]];
    const HAIR = [[[-3.2, -1.2], [-2.6, -.9], [-2.4, -.2]], [[1.1, -4.3], [1.5, -3.7], [2.2, -3.6]], [[-1.6, 2.2], [-1.0, 2.6], [-1.1, 3.1]], [[3.3, 1.2], [3.9, 1.6]], [[-4.6, 1.6], [-4.0, 2.2], [-4.1, 2.8]], [[-.4, -4.6], [-.1, -4.1]]];
    const RUSTE = (() => { const r = rng(121), out = []; for (let i = 0; i < 9; i++) { const a = i / 9 * 6.283 + r() * .4, d = 1.5 + r() * .7; out.push(blobAt(r, -2 + Math.cos(a) * d, 2.5 + Math.sin(a) * d * .9, .14 + r() * .18)); } return out; })();
    const CONTACT = Math.atan2(-6.7, 4);
    const CRACKS = (() => {
      const r = rng(91), out = [];
      for (let i = 0; i < 9; i++) {
        const a = r() * 6.283, d = 1 + r() * 3.4; let x = Math.cos(a) * d, y = Math.sin(a) * d, dir = r() * 6.283; const pts = [[x, y]];
        for (let j = 0; j < 3; j++) { dir += (r() - .5) * 1.4; const L = .8 + r() * 1.2; x += Math.cos(dir) * L; y += Math.sin(dir) * L; const m = Math.hypot(x, y); if (m > 4.9) { x *= 4.9 / m; y *= 4.9 / m; } pts.push([x, y]); }
        let da = Math.abs(a - CONTACT) % 6.283; if (da > Math.PI) da = 6.283 - da;
        out.push({ pts, at: .65 * da / Math.PI });
      }
      const t = rng(93);
      for (const k of out) { const p = k.pts[2], o = Math.atan2(p[1], p[0]) + (t() - .5) * 2, L = .6 + t() * .7; k.tw = [p, [p[0] + Math.cos(o) * L * .5, p[1] + Math.sin(o) * L * .5], [p[0] + Math.cos(o + .4) * L, p[1] + Math.sin(o + .4) * L]]; }
      return out;
    })();
    function strokePts(c, pts) { c.beginPath(); c.moveTo(pts[0][0], pts[0][1]); for (let i = 1; i < pts.length; i++) c.lineTo(pts[i][0], pts[i][1]); }
    function sub(pts, f0, f1) {
      const L = [0]; for (let i = 1; i < pts.length; i++) L.push(L[i - 1] + Math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]));
      const tot = L[L.length - 1] || 1, a = f0 * tot, b = f1 * tot, out = [];
      if (b <= a) return out;
      const at = d => { let i = 1; while (i < L.length - 1 && L[i] < d) i++; return lerp2(pts[i - 1], pts[i], c01((d - L[i - 1]) / ((L[i] - L[i - 1]) || 1))); };
      out.push(at(a)); for (let i = 1; i < pts.length - 1; i++) if (L[i] > a && L[i] < b) out.push(pts[i]); out.push(at(b));
      return out;
    }
    function cracks(c, R, g, col, a, big) {
      if (a <= .003 || g <= 0) return;
      c.save(); c.globalCompositeOperation = big > .5 ? 'source-over' : 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';   // added light whitens on the giant's pale skin
      const w1 = pen(R, 1.7, 7), w2 = pen(R, .55, 2.6), core = big > .5 ? col : pale(col, .35);
      for (const k of CRACKS) {
        const q = c01((g - k.at) / .35); if (q <= 0) continue;
        const pts = sub(k.pts, 0, q); if (pts.length < 2) continue;
        strokePts(c, pts); c.strokeStyle = css(col, .28 * a); c.lineWidth = w1; c.stroke();
        strokePts(c, pts); c.strokeStyle = css(core, .95 * a); c.lineWidth = w2; c.stroke();
        if (big > 0 && q > .6) { const tw = sub(k.tw, 0, c01((q - .6) / .4)); if (tw.length > 1) { strokePts(c, tw); c.strokeStyle = css(pale(col, .2), .75 * a * big); c.lineWidth = w2 * .7; c.stroke(); } }
      }
      c.restore();
    }
    function wear(c, R, A, a) {
      c.save(); c.globalAlpha = a; c.lineCap = 'round';
      for (const [x, y, r] of PITS) {
        const p = rot(x, y, A);
        c.fillStyle = 'rgba(14,15,18,.55)'; c.beginPath(); c.ellipse(p[0], p[1], r, r * .82, 0, 0, 6.283); c.fill();
        c.strokeStyle = 'rgba(205,210,218,.4)'; c.lineWidth = pen(R, .07, 1.3); c.beginPath(); c.ellipse(p[0], p[1] + r * .08, r * .86, r * .68, 0, .4, 2.75); c.stroke();
        c.strokeStyle = 'rgba(10,10,12,.6)'; c.lineWidth = pen(R, .09, 1.6); c.beginPath(); c.ellipse(p[0], p[1], r * 1.05, r * .86, 0, 3.6, 5.9); c.stroke();
      }
      c.strokeStyle = 'rgba(12,13,16,.7)'; c.lineWidth = pen(R, .08, 1.2);
      for (const h of HAIR) { const q = h.map(p => rot(p[0], p[1], A)); strokePts(c, q); c.stroke(); }
      c.restore();
    }
    function lensRings(c, R, a, lit) {
      if (a <= .003) return;
      c.save(); c.globalAlpha = a; c.lineWidth = pen(R, .12, 1.3);
      if (lit > .02) { c.strokeStyle = css(pale(SIGN, .55), .4 * lit); for (const k of [.72, .46]) { c.beginPath(); c.ellipse(.3, 0, 2.1 * k, 3.3 * k, 0, 0, 6.283); c.stroke(); } }
      c.strokeStyle = 'rgba(255,255,255,.16)'; c.lineWidth = pen(R, .16, 2.4); c.beginPath(); c.ellipse(.3, 0, 1.7, 2.8, 0, 3.55, 4.3); c.stroke();
      c.restore();
    }
    // the core at C, R page px a unit, its lens turned A; o: lens { on, glow }, crack [g, col, a], soak
    function gCore(c, C, R, A, o = {}) {
      const big = ss(R, 3, 12);
      c.save(); c.translate(C[0], C[1]); c.scale(R, R);
      c.save(); c.rotate(A); trace(c, DR.core); c.clip(); c.rotate(-A);
      c.fillStyle = DARK[2]; c.fillRect(-9, -9, 18, 18);
      c.save(); c.translate(0, -.8); c.rotate(A); c.fillStyle = DARK[1]; trace(c, DR.core); c.fill(); c.restore();
      c.save(); c.translate(0, -2.6); c.rotate(A); c.fillStyle = DARK[0]; trace(c, DR.core); c.fill(); c.restore();
      if (big < 1 && c.grain) { c.globalAlpha = .55 * (1 - big); c.fillStyle = c.grain; c.fillRect(-9, -9, 18, 18); c.globalAlpha = 1; }
      devGrain(c, C, .9 * big);
      if (big > 0) wear(c, R, A, big);
      if (o.soak > .003) { c.fillStyle = css(BL, o.soak); c.fillRect(-9, -9, 18, 18); }
      c.restore();
      c.save(); c.rotate(A); lyRust(c, DR.crust);
      if (big > 0) { c.globalAlpha = big; c.fillStyle = 'rgba(96,52,30,.8)'; for (const b of RUSTE) { trace(c, b.s); c.fill(); } c.globalAlpha = 1; }
      if (o.crack) cracks(c, R, o.crack[0], o.crack[1], o.crack[2], big);
      c.restore();
      gInk(c, DR.core, A, R, 1.7, 5.5);
      const L = o.lens || { on: 0, glow: 0 }, lx = Math.cos(A) * 2.2, ly = Math.sin(A) * 2.2;
      c.save(); c.rotate(A); c.translate(2.2, 0); c.scale(.82, .82);
      c.fillStyle = DARK[2]; c.beginPath(); c.ellipse(0, 0, 3.1, 4.4, 0, 0, 6.283); c.fill(); c.strokeStyle = INK; c.lineWidth = pen(R * .82, 1.3, 3.8); c.stroke();
      c.fillStyle = css(pale([77, 64, 45], .1)); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill();
      if (big > 0) {   // the giant's dull glass: a deeper tone below its lit top, as the paint steps
        c.save(); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.clip(); c.rotate(-A);
        c.translate(0, .9); c.rotate(A); c.fillStyle = css([52, 44, 34], .75 * big); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill(); c.restore();
      }
      if (L.on > .002) {
        c.globalAlpha = L.on;
        c.fillStyle = css(pale(SIGN, .3)); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill();
        c.fillStyle = css(pale(SIGN, .8)); c.beginPath(); c.ellipse(.8, 0, .9, 1.7, 0, 0, 6.283); c.fill();
        c.globalAlpha = 1;
      }
      lensRings(c, R * .82, big, L.on);
      c.restore();
      if (L.on > .002) halo(c, lx, ly, 14 * .82 * lerp(1, .5, big), SIGN, .55 * L.glow * L.on * lerp(1, .3, big));
      c.restore();
    }

    // ---------- the heap the core lies in (drone.js's): cold soot, left in the pit ----------
    const HEAP = (() => {
      const hr = rng(707), heap = [];
      for (let i = 0; i < 90; i++) {
        const x = (hr() * 2 - 1) * 15, top = 7.5 * (1 - (x / 15) ** 2), s = 1.3 + hr() * 1.5;
        heap.push({ x: 32 + x, y: 64 - s / 2 - hr() ** 1.6 * top, s, tn: hr(), hot: hr() < .08, ph: hr() * 6.283 });
      }
      return heap;
    })();
    function heap(c, front, a) {
      if (a <= .003) return;
      c.save(); c.globalAlpha = a;
      for (const m of HEAP) {
        if ((m.y >= 60) !== front) continue;
        const x = CORE0[0] + m.x - 32, y = CORE0[1] + m.y - 57.5;
        c.fillStyle = m.tn < .5 ? '#1a1b1f' : '#26282d'; c.fillRect(x - m.s / 2 - .35, y - m.s / 2 - .35, m.s + .7, m.s + .7);
        c.fillStyle = m.hot ? css(EMBER, .9) : m.tn < .5 ? '#4c4f55' : '#606369'; c.fillRect(x - m.s / 2, y - m.s / 2, m.s, m.s);
      }
      c.restore();
      if (front) for (const m of HEAP) if (m.hot) halo(c, CORE0[0] + m.x - 32, CORE0[1] + m.y - 57.5, 3, EMBER, .3 * a);
    }

    // ---------- the blood: drawDrone's 170 motes, each a drop that winds onto its orbit, then turns to soot ----------
    // Each ring is one of the cloud's three orbits; it spins fast while it fills, then slows to the cloud's own speed
    // by a whole number of turns, so the black rings land exactly on drawDrone's motes.
    const SPIN_K = 7, VMAX = TAU * SPIN_K / (TL.spin[0] + (TL.spin[1] - TL.spin[0]) / 2);
    const wO = o => .8 + .25 * o;
    function spinS(u) {
      const D = TL.spin[1] - TL.spin[0]; if (u <= TL.spin[0]) return VMAX * Math.max(0, u);
      const x = Math.min(u, TL.spin[1]) - TL.spin[0]; return VMAX * (TL.spin[0] + x - x * x / (2 * D));
    }
    const phiO = (o, u) => wO(o) * (u + 1.3) + spinS(u);
    const angV = (o, u) => wO(o) + VMAX * c01(1 - (u - TL.spin[0]) / (TL.spin[1] - TL.spin[0]));
    const TILT = o => -.3 + o * 1.05;
    const ringPt = (o, th, rx, k) => rot(Math.cos(th) * rx * k, Math.sin(th) * rx * .3 * k, TILT(o));
    // where each ring takes the stream: its point nearest the hero
    const ENTRY = [0, 1, 2].map(o => { let b = -1e9, at = 0; for (let j = 0; j < 629; j++) { const x = ringPt(o, j / 100, 10 + o * 2.2, 1)[0]; if (x > b) { b = x; at = j / 100; } } return at; });
    const BLOOD = DR.P.map((P, i) => {
      const o = i % 3, dir = o % 2 ? -1 : 1, F = TL.F[o], f0 = phiO(o, F);
      const want = f0 + (((dir * (ENTRY[o] - P.ph) - f0) % TAU) + TAU) % TAU;
      let a = F, b = F + 4; for (let k = 0; k < 44; k++) { const m = (a + b) / 2; if (phiO(o, m) < want) a = m; else b = m; }
      return { i, o, dir, P, rx: 10 + o * 2.2, jit: (P.r1 - .5) * 1.6, t: b, hot: P.hot || P.r1 > .86, kb0: TL.black[0] + o * .25 + P.r1 * .45 };
    });
    const RN = [0, 1, 2].map(o => BLOOD.filter(d => d.o === o).length);
    const FE = [0, 1, 2].map(o => Math.max(...BLOOD.filter(d => d.o === o).map(d => d.t)));
    const blackOf = (d, u) => ss(u, d.kb0, d.kb0 + 1.2);
    function dropAt(d, X, C, R, k) {
      const u = X.u, t0 = d.t - TL.fly; if (u < t0) return null;
      const r = d.P.sz * Math.max(.5, 1.6 * Math.sqrt(R / VR)), rx = d.rx + d.jit * X.jit;
      if (u < d.t) {   // flying from the palm to its ring, fast
        const e = (u - t0) / TL.fly, en = ringPt(d.o, ENTRY[d.o], rx, k), end = [C[0] + en[0] * R, C[1] + en[1] * R], P0 = X.palm;
        const ct = [lerp(P0[0], end[0], .45), Math.min(P0[1], end[1]) - 40 * (.5 + d.P.r1)];
        return { p: bez(P0, ct, end, e), tl: [bez(P0, ct, end, Math.max(0, e - .3)), bez(P0, ct, end, Math.max(0, e - .15))], z: 1, r: r * lerp(.55, 1, e) };
      }
      const th = d.P.ph + d.dir * phiO(d.o, u), D = clamp(.06 * angV(d.o, u), .08, .3);
      const at = a => { const q = ringPt(d.o, a, rx, k); return [C[0] + q[0] * R, C[1] + q[1] * R]; };
      return { p: at(th), tl: [at(th - d.dir * D), at(th - d.dir * D * .5)], z: Math.sin(th), r };
    }
    // red, then black with embers; while it shrinks back it takes the cloud's own greys
    function dropLook(d, X, R) {
      const kb = blackOf(d, X.u), g = X.ph === 3 ? ss(R, 4, 1.05) : 0, m = MOTE_RGB[d.P.tn < .5 ? 0 : 1];
      const h = .45 + .4 * Math.sin(X.t * 11 + d.P.ph * 7), keep = d.P.hot ? 1 : d.hot ? 1 - (X.ph === 3 ? ss(X.kout, .2, .8) : 0) : 0;
      return { f: mix3(mix3(BV, BLK, kb), m[0], g), rim: mix3(mix3(BV_D, BLK_D, kb), m[1], g), sh: mix3(BV_L, BLK_L, kb), shA: .8 * (1 - g), lit: .32 * kb * (1 - g), emb: kb * keep * h };
    }
    function bloodDrops(c, X, C, R, k, front) {
      c.save(); c.lineCap = 'round'; c.lineJoin = 'round';
      const glow = ss(R, 3, 10);
      for (const d of BLOOD) {
        const q = dropAt(d, X, C, R, k); if (!q || (q.z >= 0) !== front) continue;
        const L = dropLook(d, X, R), w = q.r * 1.8, pts = [q.tl[0], q.tl[1], q.p];
        strokePts(c, pts); c.strokeStyle = css(L.rim, .9); c.lineWidth = w + .8; c.stroke();
        strokePts(c, pts); c.strokeStyle = css(L.f); c.lineWidth = w; c.stroke();
        c.beginPath(); c.arc(q.p[0], q.p[1], q.r + .4, 0, TAU); c.fillStyle = css(L.rim, .9); c.fill();
        c.beginPath(); c.arc(q.p[0], q.p[1], q.r, 0, TAU); c.fillStyle = css(L.f); c.fill();
        if (L.lit > .01 && q.r > 1.2) { c.save(); c.translate(-.35 * q.r, -.45 * q.r); strokePts(c, pts); c.strokeStyle = css([176, 184, 196], L.lit); c.lineWidth = .9; c.stroke(); c.restore(); }
        if (L.shA > .02 && q.r > 1.2) { c.beginPath(); c.arc(q.p[0] - q.r * .3, q.p[1] - q.r * .35, q.r * .38, 0, TAU); c.fillStyle = css(L.sh, L.shA); c.fill(); }
        if (L.emb > .01) {
          c.save(); c.globalCompositeOperation = 'lighter'; c.beginPath(); c.arc(q.p[0], q.p[1], q.r * .9, 0, TAU); c.fillStyle = css(pale(EMBER, .25), .9 * L.emb); c.fill(); c.restore();
          if (glow > 0) halo(c, q.p[0], q.p[1], q.r * 3.4, EMBER, .3 * L.emb * glow);
        }
      }
      c.restore();
    }

    // ---------- the plates: from the debris right of the ship, one at a time along ember arcs ----------
    function emberArc(c, a, b, seed, al) {
      const d = Math.hypot(b[0] - a[0], b[1] - a[1]); if (d < 2 || al <= .003) return;
      const r = rng(seed), n = Math.max(4, Math.min(40, Math.round(d / 7))), nx = -(b[1] - a[1]) / d, ny = (b[0] - a[0]) / d, amp = Math.min(7, d * .08), pts = [a];
      for (let j = 1; j < n; j++) { const e = j / n, o = (r() - .5) * 2 * amp * Math.sin(e * Math.PI); pts.push([lerp(a[0], b[0], e) + nx * o, lerp(a[1], b[1], e) + ny * o]); }
      pts.push(b);
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
      strokePts(c, pts); c.strokeStyle = css(EMBER, .25 * al); c.lineWidth = 3.4; c.stroke();
      strokePts(c, pts); c.strokeStyle = css(pale(EMBER, .5), .9 * al); c.lineWidth = .9; c.stroke();
      c.restore();
    }
    const DEB = [[612, 300], [706, 196], [812, 352], [905, 250]].map(([x, y], i) => ({ x, y, a: i * 1.3 + .4, w: [.11, -.09, .13, -.12][i], ph: i * 1.9 }));
    const SCRAP = (() => {
      const r = rng(808), out = [];
      for (const [x, y, sz, col] of [[662, 404, 17, STEEL], [772, 262, 22, DARK], [872, 140, 13, RUSTY], [954, 420, 15, OLIVE], [588, 172, 10, DARK], [842, 448, 11, STEEL]])
        out.push({ x, y, col, sh: shardShape(r, sz), a: r() * 6.283, w: (r() - .5) * .16, ph: r() * 6.283 });
      return out;
    })();
    function debris(i, t) { const d = DEB[i]; return { p: [d.x + Math.sin(t * .21 + i * 1.7) * 7, d.y + Math.cos(t * .17 + i * 2.3) * 5], a: d.a + t * d.w, k: Math.cos(t * .3 + d.ph) }; }
    function platePic(c, i, q, sc = 1) { c.save(); c.translate(q.p[0], q.p[1]); c.scale(sc, sc); plate(c, { x: 0, y: 0, a: q.a }, DR.sh[i], DR.sh[i].col, q.k); c.restore(); }
    function slotAt(i, X) { const I = plateIdle(i, X.td), C = X.core.C, R = X.core.R; return { p: [C[0] + I[0] * R, C[1] + I[1] * R], a: I[2], k: I[3], z: I[4] }; }
    function plateState(i, X) {
      const L = LAND(i), t0 = L - TL.plateFly;
      if (X.ph !== 4 || X.u < t0) return { mode: 'away' };
      const S = slotAt(i, X); if (X.u >= L) return { mode: 'home', ...S };
      const e = (X.u - t0) / TL.plateFly, ee = e * e, d = debris(i, t0);
      return { mode: 'fly', e, p: lerp2(d.p, S.p, ee), a: angLerp(d.a, S.a, ee), k: lerp(d.k, S.k, ee), z: S.z };
    }
    function arcs(c, X) {
      const C = X.core.C, R = X.core.R;
      X.ps.forEach((q, i) => {
        const L = LAND(i), t0 = L - TL.plateFly, a = bump(X.u, t0 - .35, t0 - .25, L + .08);
        if (a > .003) {
          const end = q.mode === 'away' ? debris(i, X.u).p : q.p, d = Math.hypot(end[0] - C[0], end[1] - C[1]) || 1, st = [C[0] + (end[0] - C[0]) / d * Math.min(d * .9, 5.4 * R), C[1] + (end[1] - C[1]) / d * Math.min(d * .9, 5.4 * R)];
          emberArc(c, st, end, Math.floor(X.t * 30) * 7 + i * 131 + 1, a); emberArc(c, st, end, Math.floor(X.t * 30) * 11 + i * 71 + 5, .6 * a);
        }
        if (q.mode === 'home') { const f = 1 - ss(X.u, L, L + .3); if (f > .003) halo(c, q.p[0], q.p[1], 9, EMBER, .7 * f); }
      });
    }

    // ---------- the wreck ----------
    function hull(g, pts, seed) {
      g.beginPath(); pts.forEach((p, i) => i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1])); g.closePath();
      g.fillStyle = '#353940'; g.fill();
      g.save(); g.clip();
      g.fillStyle = '#2c3036'; for (let y = 14; y < LH; y += 32) g.fillRect(0, y, LW, 1.2);
      for (let x = 12; x < LW; x += 64) g.fillRect(x, 0, 1.2, LH);
      const r = rng(seed);
      for (let y = 14; y < LH; y += 32) for (let x = 12; x < LW; x += 64) { g.fillStyle = '#4a4f57'; g.fillRect(x + 5, y + 4, 1.6, 1.6); g.fillRect(x + 57, y + 4, 1.6, 1.6); if (r() < .22) { const b = blobAt(r, x + 4 + r() * 50, y + 2, 3 + r() * 3); g.fillStyle = 'rgba(96,58,34,.75)'; trace(g, b.s); g.fill(); } }
      g.restore();
      g.strokeStyle = INK; g.lineWidth = 1.6; g.lineJoin = 'round'; g.stroke();
    }
    function litEdge(g, x0, x1, y) { g.fillStyle = '#6a7078'; g.fillRect(x0, y, x1 - x0, 2.2); g.fillStyle = '#464b53'; g.fillRect(x0, y + 2.2, x1 - x0, 5); }
    function backWall(g, x0, y0, x1, y1) {
      g.fillStyle = '#14171c'; g.fillRect(x0, y0, x1 - x0, y1 - y0);
      g.fillStyle = '#1a1e24'; for (let x = x0 + 16; x < x1; x += 32) g.fillRect(x, y0, 1.5, y1 - y0);
      g.fillStyle = 'rgba(0,0,0,.4)'; g.fillRect(x0, y1 - 8, x1 - x0, 8);
    }
    function stripes(g, x0, y0, x1, y1) {
      g.save(); g.beginPath(); g.rect(x0, y0, x1 - x0, y1 - y0); g.clip();
      g.fillStyle = '#c4a02c'; g.fillRect(x0, y0, x1 - x0, y1 - y0); g.fillStyle = '#18181a';
      for (let x = x0 - (y1 - y0); x < x1; x += 14) { g.beginPath(); g.moveTo(x, y1); g.lineTo(x + 7, y1); g.lineTo(x + 7 + (y1 - y0), y0); g.lineTo(x + (y1 - y0), y0); g.closePath(); g.fill(); }
      g.restore();
    }
    function drawScene(g) {
      const sky = g.createLinearGradient(0, 0, 0, LH); sky.addColorStop(0, '#04060a'); sky.addColorStop(1, '#0a0f17'); g.fillStyle = sky; g.fillRect(0, 0, LW, LH);
      for (const [x, y, sz, a] of STARS) { const xx = x - 300; if (xx < -2 || xx > LW + 2) continue; g.fillStyle = `rgba(220,228,240,${a.toFixed(3)})`; g.fillRect(xx, y, sz, sz); }
      backWall(g, 0, 174, 76, FL); backWall(g, 140, 174, 268, FL); backWall(g, 268, 238, 332, FL);
      g.fillStyle = '#2a2e35'; g.fillRect(80, 238, 56, FL - 238); g.fillStyle = '#343941'; g.fillRect(80, 238, 56, 3); g.fillRect(106, 238, 3, FL - 238);
      g.strokeStyle = INK; g.lineWidth = 1.2; g.strokeRect(80, 238, 56, FL - 238);
      g.fillStyle = '#7a4a1c'; g.fillRect(124, 300, 4, 6); halo(g, 126, 303, 10, [255, 150, 60], .25);
      g.fillStyle = '#0c0e12'; g.fillRect(271, 238, 3, FL - 238); g.fillRect(326, 238, 3, FL - 238);
      halo(g, 204, 186, 60, [150, 175, 200], .08); g.fillStyle = '#8fa3b5'; g.fillRect(196, 176, 16, 3);
      hull(g, [[0, 0], [204, 0], [204, 46], [268, 46], [268, 78], [332, 78], [332, 238], [268, 238], [268, 174], [140, 174], [140, 238], [76, 238], [76, 174], [0, 174]], 31);
      stripes(g, 268, 238, 332, 244); g.strokeStyle = INK; g.lineWidth = 1; g.strokeRect(268, 238, 64, 6);
      hull(g, [[0, FL], [332, FL], [332, GR], [460, GR], [460, FL], [524, FL], [529, 437], [522, 446], [528, 454], [520, 463], [526, 471], [519, 482], [524, 494], [492, 497], [466, 494], [460, 506], [463, 526], [440, 523], [428, 528], [428, LH], [0, LH]], 57);
      litEdge(g, 0, 332, FL); litEdge(g, 332, 460, GR); litEdge(g, 460, 524, FL);
      g.fillStyle = '#26292f'; g.fillRect(329, FL, 3, GR - FL);
      g.fillStyle = '#41464e'; g.fillRect(460, FL + 2, 3, GR - FL - 2);
      stripes(g, 461, FL + 2.2, 521, FL + 13); g.strokeStyle = INK; g.lineWidth = 1; g.strokeRect(461, FL + 2.2, 60, 10.8);
      g.fillStyle = 'rgba(8,8,10,.55)';
      for (const [x, w, h] of [[382, 70, 2.4], [396, 46, 1.4], [410, 40, 3.2], [430, 26, 1.6]]) g.fillRect(x, GR - h, w, h);
      const r = rng(64); g.fillStyle = '#3b3e44'; for (let i = 0; i < 26; i++) { const x = 378 + r() * 80, s = .8 + r() * 1.6; g.fillRect(x, GR - s - r() * 1.5, s, s); }
    }
    function drawScrap(c, t, a = 1) {
      if (a <= .003) return;
      c.save(); c.globalAlpha = a;
      for (const s of SCRAP) { c.save(); c.translate(s.x + Math.sin(t * .13 + s.ph) * 6, s.y + Math.cos(t * .11 + s.ph) * 4); paint(c, s.sh, s.a + t * s.w, s.col, .6, 2); ink(c, s.sh, s.a + t * s.w, 1.3); c.restore(); }
      c.restore();
    }
    function debrisPlates(c, X, a = 1) {
      if (a <= .003) return;
      c.save(); c.globalAlpha = a; X.ps.forEach((q, i) => { if (q.mode === 'away') platePic(c, i, debris(i, X.u)); }); c.restore();
    }
    // E above the core, until it is pressed
    function eKey(g, u) {
      const a = 1 - ss(u, .5, .8); if (a <= .003) return;
      const lit = bump(u, TL.press - .02, TL.press + .03, TL.press + .45), x = CORE0[0] - 34, y = CORE0[1] - 58;
      rr(g, x + .5, y + .5, 24, 22, 4); g.fillStyle = css([20, 24, 30], (.6 + .3 * lit) * a); g.fill(); g.strokeStyle = css([200, 210, 222], (.3 + .65 * lit) * a); g.lineWidth = 1; g.stroke();
      g.font = '600 13px system-ui,"Segoe UI",sans-serif'; g.textAlign = 'center'; g.textBaseline = 'alphabetic'; g.fillStyle = css([226, 230, 236], (.5 + .5 * lit) * a); g.fillText('E', x + 12.5, y + 16); g.textAlign = 'left';
    }
    // the small core: its breathing glow, and the light of its cracks once it wakes
    function smallCore(c, X, fade) {
      const { C, R, A } = X.core, big = ss(R, 2, 20);
      gCore(c, C, R, A, coreLook(X));
      const br = .7 + .3 * Math.sin(X.u / 2.6 * 6.283);
      halo(c, C[0], C[1], 5.4 * R + 34, EMBER, .55 * br * fade * lerp(1, .07, big));
      halo(c, C[0], C[1], 5.4 * R + 26, EMBER, .5 * X.light * lerp(1, .1, big));
    }
    const idleAt = X => [HX[0] - Z * IDLE[0], HX[1] + Z * IDLE[1] + Math.sin(X.u * 1.7 + 1.3) * 1.5];
    const IDLE_A = Math.atan2(46, -22);

    // ---------- the hero: the kit's rig, placed, turned and scaled; one scratch body ----------
    const HB = newHero('me', 0, 1.3);
    function heroPose(st) { HB.face = st.face; HB.kn = st.kn; HB.lean = st.lean; HB.rk = st.rk; HB.rp = st.rp; HB.look = st.look; HB.air = false; HB.amp = 0; HB.ph = 0; HB.e = 0; st.P = poseOf(HB); return st; }
    function heroDraw(g, st) { HB.face = st.face; HB.look = st.look; HB.P = st.P; HB.x = 0; HB.y = 0; g.save(); g.translate(st.foot[0], st.foot[1]); g.rotate(st.a); g.scale(st.s, st.s); drawHero(g, HB); g.restore(); }
    const toPage = (st, p) => { const q = rot(st.s * st.face * Z * p[0], st.s * Z * p[1], st.a); return [st.foot[0] + q[0], st.foot[1] + q[1]]; };
    function localOf(st, p) { const q = rot(p[0] - st.foot[0], p[1] - st.foot[1], -st.a); return [q[0] / (st.s * st.face * Z), q[1] / (st.s * Z)]; }
    const lookAt = (st, p) => { const l = localOf(st, p); return clamp(headTurn(l[0] - .3, l[1] + RIG.Neck), -HEAD.Up, HEAD.Down); };
    // in the wreck: standing still from E on; back from the vision, held where it stood with a slight sway
    function wreckHero(X) {
      const u = X.u, sway = X.ph === 4 ? ss(u, TL.out[1], TL.out[1] + .8) * (1 - ss(u, TL.rise[1] - .2, TL.rise[1] + .3)) : 0;
      const st = { foot: HX.slice(), s: 1, a: 0, face: -1, kn: 0, lean: .04 * Math.sin(u * 1.35) * sway, rk: 0, rp: PALM };
      st.look = lookAt(st, X.core.C);
      return heroPose(st);
    }
    const HELMW = (() => { const st = heroPose({ foot: HX.slice(), s: 1, a: 0, face: -1, kn: 0, lean: 0, rk: 0, rp: PALM, look: 0 }); return toPage(st, st.P.helm); })();
    const AW = Math.atan2(HELMW[1] - HOVER[1], HELMW[0] - HOVER[0]);
    // weightless before the giant: knees loose, drifting; the palm held out while the blood is drawn
    function voidHero(X) {
      const u = X.u, s = HV.s;
      const st = { foot: [HV.foot[0] + Math.sin(u * .5) * 5 * s, HV.foot[1] + Math.cos(u * .37) * 7 * s], s, a: HV.a - .22 + .06 * Math.sin(u * .41), face: -1, kn: .1 + .05 * Math.sin(u * .7), lean: -.18 + .05 * Math.sin(u * .53) };
      st.rk = lerp(.15, 1, ss(u, TL.reach[0], TL.reach[1])) * (1 - .6 * ss(u, FE[2] + .3, FE[2] + 1.5));
      const loc = localOf(st, VC), sh = [.4, -RIG.Shoulder], dx = loc[0] - sh[0], dy = loc[1] - sh[1], L = Math.hypot(dx, dy) || 1;
      st.rp = [sh[0] + dx / L * 40, sh[1] + dy / L * 40];
      st.look = lookAt(st, VC);
      return heroPose(st);
    }
    const mixHero = (a, b, k) => heroPose({ foot: lerp2(a.foot, b.foot, k), s: Math.exp(lerp(Math.log(a.s), Math.log(b.s), k)), a: lerp(a.a, b.a, k), face: -1, kn: lerp(a.kn, b.kn, k), lean: lerp(a.lean, b.lean, k), rk: lerp(a.rk, b.rk, k), rp: lerp2(a.rp, b.rp, k), look: lerp(a.look, b.look, k) });

    // ---------- the vision ----------
    const VSTARS = (() => { const r = rng(77), s = []; for (let i = 0; i < 300; i++) { const b = r(); s.push([r() * LW, r() * LH, b < .06 ? 1.8 : b < .3 ? 1.1 : .7, .18 + r() * .55, r() < .15]); } return s; })();
    function sky(g) {
      const gr = g.createLinearGradient(0, 0, 0, LH); gr.addColorStop(0, '#020307'); gr.addColorStop(1, '#05070c'); g.fillStyle = gr; g.fillRect(0, 0, LW, LH);
      for (const [x, y, sz, al, warm] of VSTARS) { g.fillStyle = warm ? `rgba(240,226,206,${al.toFixed(3)})` : `rgba(206,218,238,${al.toFixed(3)})`; g.fillRect(x, y, sz, sz); }
    }
    // the core's face: lit cracks in the wreck once it wakes, dull in the void until it drinks, its eye open only as the blood turns
    function coreLook(X) {
      const u = X.u;
      if (X.ph <= 1) return { lens: { on: 0, glow: 0 }, crack: [1, EMBER, .9 * X.light] };
      const drink = ss(u, TL.F[0], FE[2]), kb = ss(u, TL.black[0], TL.black[1]);
      return {
        lens: { on: X.lensOn, glow: .35 + .5 * bump(u, TL.lens[0], TL.lens[1], TL.lens[1] + .6) },
        crack: [drink, mix3([160, 26, 22], EMBER, kb), .5 * (1 - (X.ph === 3 ? ss(X.kout, 0, .6) : 0))],
        soak: .3 * drink * (1 - kb),
      };
    }
    function voidGlow(c, X) {
      const u = X.u, { C, R } = X.core, big = ss(R, 2, 20), kb = ss(u, TL.black[0], TL.black[1]);
      const drink = ss(u, TL.F[0], FE[2]) * (1 - kb), emb = kb * (1 - (X.ph === 3 ? ss(X.kout, 0, .5) : 0));
      halo(c, C[0], C[1], (5.4 * R + 20) * 1.15, [150, 20, 18], .35 * drink * lerp(1, .3, big));
      halo(c, C[0], C[1], (5.4 * R + 20) * 1.1, EMBER, .25 * emb * lerp(1, .25, big));
    }
    function voidScene(c, X) {
      sky(c);
      const { C, R, A } = X.core;
      bloodDrops(c, X, C, R, X.k, false);
      gCore(c, C, R, A, coreLook(X));
      voidGlow(c, X);
      heroDraw(c, X.hv);
      bloodDrops(c, X, C, R, X.k, true);
    }

    // ---------- the crumble: the wreck breaking into flakes that go out as stars, cut along a jittered grid ----------
    const put = (c, T, cv, a = 1) => { if (a <= .003) return; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = Math.min(1, a); c.drawImage(cv, 0, 0); c.restore(); };
    const FLAKES = (() => {
      const r = rng(2024), nx = 40, ny = 22, dx = LW / nx, dy = LH / ny, G = [];
      for (let i = 0; i <= nx; i++) { G.push([]); for (let j = 0; j <= ny; j++) { const edge = i === 0 || j === 0 || i === nx || j === ny; G[i].push([i * dx + (edge ? 0 : (r() - .5) * dx * .6), j * dy + (edge ? 0 : (r() - .5) * dy * .6)]); } }
      const out = [], far = Math.hypot(LW, LH);
      for (let i = 0; i < nx; i++) for (let j = 0; j < ny; j++) {
        const q = [G[i][j], G[i + 1][j], G[i + 1][j + 1], G[i][j + 1]], c = [(q[0][0] + q[2][0]) / 2, (q[0][1] + q[2][1]) / 2];
        const x0 = Math.min(...q.map(p => p[0])), y0 = Math.min(...q.map(p => p[1])), x1 = Math.max(...q.map(p => p[0])), y1 = Math.max(...q.map(p => p[1]));
        const a = Math.atan2(c[1] - CORE0[1], c[0] - CORE0[0]) + (r() - .5) * .9;
        out.push({ q, c, bb: [x0, y0, x1 - x0, y1 - y0], d: Math.hypot(c[0] - CORE0[0], c[1] - CORE0[1]) / far * 1.4 + r() * .12, dir: [Math.cos(a), Math.sin(a)], go: 60 + r() * 140, spin: (r() - .5) * 3 });
      }
      return out;
    })();
    function flakes(g, T, e) {   // e: 0 whole, 1 gone to stars
      if (e <= 0) { put(g, T, T.scn); return; }
      const S = T.S, W = T.scn.width, H = T.scn.height;
      for (const f of FLAKES) {
        const q = c01((e - f.d * .45) / .55); if (q >= 1) continue;
        const qq = q * q, cx = f.c[0] + f.dir[0] * qq * f.go, cy = f.c[1] + f.dir[1] * qq * f.go, sc = 1 - .94 * sm(q);
        g.save(); g.translate(cx, cy); g.rotate(f.spin * qq); g.scale(sc, sc); g.translate(-f.c[0], -f.c[1]);
        g.beginPath(); g.moveTo(f.q[0][0], f.q[0][1]); for (let k = 1; k < 4; k++) g.lineTo(f.q[k][0], f.q[k][1]); g.closePath(); g.clip();
        const sx = clamp(f.bb[0] * S, 0, W - 1), sy = clamp(f.bb[1] * S, 0, H - 1), sw = Math.max(1, Math.min(f.bb[2] * S + 2, W - sx)), sh = Math.max(1, Math.min(f.bb[3] * S + 2, H - sy));
        g.globalAlpha = 1 - ss(q, .55, 1);
        g.drawImage(T.scn, sx, sy, sw, sh, sx / S, sy / S, sw / S, sh / S);
        g.restore();
        if (q > .35) { g.fillStyle = css([214, 224, 240], .85 * bump(q, .35, .75, 1)); g.fillRect(cx - .8, cy - .8, 1.6, 1.6); }
      }
    }
    // into the vision (k 0..1) or back out of it: the same crumble, run forward or in reverse
    function trans(c, T, X) {
      const into = X.ph === 1, k = into ? X.kin : X.kout, e = into ? k : 1 - k;
      sky(c);
      flakes(c, T, ss(e, 0, .85));
      drawScrap(c, X.u, 1 - ss(e, 0, .35));
      debrisPlates(c, X, 1 - ss(e, 0, .3));
      const hp = 1 - ss(e, .05, .45);
      heap(c, false, hp);
      if (into) smallCore(c, X, 1 - ss(e, 0, .5));
      else { const { C, R, A } = X.core; bloodDrops(c, X, C, R, X.k, false); gCore(c, C, R, A, coreLook(X)); voidGlow(c, X); }
      heap(c, true, hp);
      heroDraw(c, X.hm);
      if (!into) { const { C, R } = X.core; bloodDrops(c, X, C, R, X.k, true); }
    }
    function wreckBefore(c, T, X) {
      put(c, T, T.scn);
      drawScrap(c, X.u); debrisPlates(c, X);
      heap(c, false, 1); smallCore(c, X, 1); heap(c, true, 1);
      eKey(c, X.u);
      heroDraw(c, X.hw);
    }
    // back in the wreck: the core where it began to grow, its cloud the black rings; the plates, then up to the head
    function wreckAfter(c, T, X) {
      put(c, T, T.scn);
      drawScrap(c, X.u); debrisPlates(c, X);
      heap(c, false, 1); heap(c, true, 1);
      heroDraw(c, X.hw);
      const { C, R, A } = X.core, all = X.ps.every(q => q.mode === 'home');
      const lamp = ss(X.u, TL.rise[0] + .5, TL.rise[1]); if (lamp > .003) lampOn(c, C[0], C[1], HX[0], HX[1], -1, .6 * lamp, 2.6);
      const pl = back => X.ps.forEach((q, i) => { if (q.mode !== 'away' && (q.z < 0) === back) platePic(c, i, q, R); });
      if (!all) pl(true);
      drawDrone(c, C[0], C[1], A, 0, X.td, R / DS, !all);
      if (!all) pl(false);
      arcs(c, X);
    }

    // ---------- the screen: the dizziness (Tunnel.cs, lighter, the whole screen alike), the grade, the stand-in HUD ----------
    function tunnel(p, t) {
      const blur = sm(p / .9) * (1 + .12 * Math.sin(t * (2.4 - Math.min(p, 1)))), gh = Math.sin(t * 1.9) * (14 / 480) * sm((p - .1) / .7);
      return { blur: c01(blur), dark: .62 * Math.pow(Math.min(p, 1.3), 1.2), sway: Math.sin(t * .8) * .028 * Math.min(p, 1.2), gx: gh, gy: gh * .25, swim: .006 * sm((p - .2) / .8) };
    }
    function copyOut(T) { const b = T.fxg; b.setTransform(1, 0, 0, 1, 0, 0); b.globalAlpha = 1; b.filter = 'none'; b.globalCompositeOperation = 'copy'; b.drawImage(T.cv, 0, 0); b.globalCompositeOperation = 'source-over'; }
    function dizzy(T, X) {
      const c = T.c, W = T.cv.width, H = T.cv.height, S = T.S, p = X.p;
      T.fxOn = p > .004; if (!T.fxOn) return;
      const L = tunnel(p, X.t), cx = W / 2, cy = H / 2;
      copyOut(T);
      c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalCompositeOperation = 'source-over'; c.globalAlpha = 1;
      c.fillStyle = '#05070b'; c.fillRect(0, 0, W, H);
      const sw = [Math.sin(X.t * 1.3) * L.swim * LW * S, Math.cos(X.t * 1.1) * L.swim * LW * S * .6];
      const putF = (ox, oy, a) => { c.globalAlpha = a; c.setTransform(1, 0, 0, 1, cx + ox, cy + oy); c.rotate(L.sway); c.translate(-cx, -cy); c.drawImage(T.fx, 0, 0, W, H); };
      putF(sw[0], sw[1], 1);
      putF(sw[0] + L.gx * LW * S, sw[1] + L.gy * LW * S, .22);
      const sg = T.fsg, w4 = T.fs.width, h4 = T.fs.height;
      sg.setTransform(1, 0, 0, 1, 0, 0); sg.globalAlpha = 1; sg.globalCompositeOperation = 'copy';
      sg.filter = `blur(${(.3 + 1.0 * L.blur).toFixed(2)}px)`; sg.drawImage(T.cv, 0, 0, w4, h4); sg.filter = 'none'; sg.globalCompositeOperation = 'source-over';
      c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = .7 * L.blur; c.drawImage(T.fs, 0, 0, W, H);
      const R = Math.hypot(W, H) * .6, br = c.createRadialGradient(cx, cy, R * .15, cx, cy, R);
      br.addColorStop(0, css(BROWN, L.dark * .15)); br.addColorStop(1, css(BROWN, L.dark * .6)); c.globalAlpha = 1; c.fillStyle = br; c.fillRect(0, 0, W, H);
      c.restore();
    }
    // the cutscene's grade: colour drained, a little harder, cold, the edges dark
    function grade(T, X) {
      const G = X.grade; if (G <= .003) return;
      const c = T.c, W = T.cv.width, H = T.cv.height;
      copyOut(T);
      c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'copy';
      c.filter = `saturate(${(1 - .45 * G).toFixed(3)}) contrast(${(1 + .08 * G).toFixed(3)}) brightness(${(1 - .04 * G).toFixed(3)})`;
      c.drawImage(T.fx, 0, 0); c.filter = 'none';
      c.globalCompositeOperation = 'multiply'; c.fillStyle = css([212, 222, 228], G); c.fillRect(0, 0, W, H);
      c.globalCompositeOperation = 'source-over';
      const R = Math.hypot(W, H) * .58, v = c.createRadialGradient(W / 2, H / 2, R * .35, W / 2, H / 2, R);
      v.addColorStop(0, 'rgba(3,4,7,0)'); v.addColorStop(1, css([3, 4, 7], .5 * G)); c.fillStyle = v; c.fillRect(0, 0, W, H);
      c.restore();
    }
    // a stand-in for the game's HUD (ring dials over a long air bar), only to show it gone while the scene plays
    function hudStandIn(T, X) {
      const a = X.hudA; if (a <= .003) return;
      const c = T.c; c.save(); c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = a;
      [[.74, [214, 132, 104]], [.88, [220, 196, 128]], [.61, [150, 200, 172]]].forEach(([v, col], j) => {
        const x = 40 + j * 38, y = 34;
        c.lineWidth = 3; c.strokeStyle = 'rgba(255,255,255,.14)'; c.beginPath(); c.arc(x, y, 13, 0, TAU); c.stroke();
        c.strokeStyle = css(col, .9); c.beginPath(); c.arc(x, y, 13, -Math.PI / 2, -Math.PI / 2 + TAU * v); c.stroke();
        c.font = '600 10px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'center'; c.textBaseline = 'middle'; c.fillStyle = 'rgba(226,230,236,.85)'; c.fillText(String(Math.round(v * 100)), x, y + .5);
      });
      c.textAlign = 'left'; c.textBaseline = 'alphabetic';
      c.fillStyle = 'rgba(255,255,255,.14)'; c.fillRect(25, 56, 106, 5); c.fillStyle = css(pale(SIGN, .2), .85); c.fillRect(25, 56, 106 * .82, 5);
      c.restore();
    }
    function dip(T, X) {
      const d = X.u > LOOP - .22 ? (X.u - (LOOP - .22)) / .22 : X.u < .22 ? 1 - X.u / .22 : 0; if (d <= 0) return;
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.fillStyle = css([5, 7, 11], sm(d)); c.fillRect(0, 0, T.cv.width, T.cv.height); c.restore();
    }

    // ---------- the frame ----------
    function coreAt(X) {
      const u = X.u;
      if (X.ph === 0) return { C: lerp2(CORE0, HOVER, ss(u, TL.lift[0], TL.lift[1])), R: 1, A: 0 };
      if (X.ph === 1) { const z = sm(X.kin); return { C: lerp2(HOVER, VC, z), R: Math.exp(Math.log(VR) * z), A: angLerp(0, X.Av, z) }; }
      if (X.ph === 2) return { C: VC.slice(), R: VR, A: X.Av };
      if (X.ph === 3) { const z = sm(X.kout); return { C: lerp2(VC, BACK, z), R: Math.exp(Math.log(VR) * (1 - z)), A: angLerp(X.Av, AW, z) }; }
      const rise = ss(u, TL.rise[0], TL.rise[1]), bob = 1.2 * ss(u, TL.out[1], TL.out[1] + .6) * (1 - rise) * Math.sin(u * 1.7);
      const hov = [BACK[0], BACK[1] + bob], idle = idleAt(X);
      return { C: rise > 0 ? bez(hov, [hov[0] + 4, idle[1] + 36], idle, rise) : hov, R: lerp(1, DRONE_R, rise), A: angLerp(AW, IDLE_A, rise) };
    }
    function frameState(T) {
      const cand = T.cand, u = ((T.time % LOOP) + LOOP) % LOOP, t = T.time;
      const X = { T, cand, u, t, td: u + 1.3 };
      X.ph = u < TL.in[0] ? 0 : u < TL.in[1] ? 1 : u < TL.out[0] ? 2 : u < TL.out[1] ? 3 : 4;
      X.kin = c01((u - TL.in[0]) / (TL.in[1] - TL.in[0])); X.kout = c01((u - TL.out[0]) / (TL.out[1] - TL.out[0]));
      X.p = LEVEL * ss(u, TL.dizzy[0], TL.dizzy[1]) * (1 - ss(u, TL.dizzy[2], TL.dizzy[3]));
      X.grade = ss(u, TL.grade[0], TL.grade[1]) * (1 - ss(u, TL.gradeOut[0], TL.gradeOut[1]));
      X.hudA = c01(1 - ss(u, TL.hud[0], TL.hud[1]) + ss(u, TL.hudBack[0], TL.hudBack[1]));
      X.light = ss(u, TL.light[0], TL.light[1]) * (1 - ss(u, TL.dim[0], TL.dim[1]));
      X.lensOn = X.ph <= 1 ? 0 : X.ph === 4 ? 1 : ss(u, TL.lens[0], TL.lens[1]);
      X.k = X.ph === 3 ? lerp(KV, 1, ss(X.kout, .3, 1)) : X.ph === 4 ? 1 : KV;
      X.jit = X.ph === 3 ? lerp(.2, 1, ss(X.kout, .3, 1)) : X.ph === 4 ? 1 : .2;   // the rings a tight band; the cloud's spread as they shrink
      X.hv = voidHero(X);
      X.palm = toPage(X.hv, X.hv.P.armF[2]);
      const helm = toPage(X.hv, X.hv.P.helm); X.Av = Math.atan2(helm[1] - VC[1], helm[0] - VC[0]);
      X.core = coreAt(X);
      X.hw = wreckHero(X);
      X.ps = [0, 1, 2, 3].map(i => plateState(i, X));
      X.m = X.ph === 0 || X.ph === 4 ? 0 : X.ph === 2 ? 1 : X.ph === 1 ? sm(X.kin) : 1 - sm(X.kout);
      X.hm = X.ph === 2 ? X.hv : X.ph === 0 || X.ph === 4 ? X.hw : mixHero(X.hw, X.hv, X.m);
      return X;
    }

    const T_ = v => fmt(v);
    const cands = [{
      id: 'wallStory', name: 'Стена',
      idea: 'Одна сцена в твоём порядке. После E герой замирает, интерфейс уходит, краски гаснут. Ядро разгорается и поднимается из ямы; обломки осыпаются звёздами, а оно растёт на месте. В пустоте они друг против друга: тёмное ядро с мёртвой линзой — и герой без веса. Кровь из ладони наматывается на ядро тремя наклонными кольцами, по одному, потом чернеет с углями внутри — и ядро открывает глаз. Они долго смотрят друг на друга. Потом всё обратно: ядро сжимается туда, где начало расти, чёрные кольца становятся его облаком. В обломках оно по одной тянет пластины по угольным дугам, с паузами, и поднимается к голове.',
      time: `E ${T_(TL.press)} с · ядро разгорается ${T_(TL.light[0])}–${T_(TL.light[1])} с, поднимается из ямы ${T_(TL.lift[0])}–${T_(TL.lift[1])} с · мир осыпается звёздами, ядро растёт ${T_(TL.in[0])}–${T_(TL.in[1])} с · лицом к лицу, рука тянется ${T_(TL.reach[0])}–${T_(TL.reach[1])} с · кольца крови ${T_(TL.F[0])}–${T_(FE[0])}, ${T_(TL.F[1])}–${T_(FE[1])}, ${T_(TL.F[2])}–${T_(FE[2])} с · кровь чернеет ${T_(TL.black[0])}–${T_(TL.black[1])} с · глаз открывается ${T_(TL.lens[0])}–${T_(TL.lens[1])} с · смотрят друг на друга до ${T_(TL.out[0])} с · обратно ${T_(TL.out[0])}–${T_(TL.out[1])} с · пластины встают в ${[0, 1, 2, 3].map(i => T_(LAND(i))).join(', ')} с · к голове ${T_(TL.rise[0])}–${T_(TL.rise[1])} с · сцена кончается к ${T_(END)} с.`,
    }];

    return {
      cands, expose: { LOOP, END, TL, HX, HOVER, RN, FE, LAND },
      init(T) { T.time = .25; T.heroes = []; dress(T.c, T.S); T.X = frameState(T); },
      onScale(T) {
        const mk_ = (w, h) => { const cv = document.createElement('canvas'); cv.width = w; cv.height = h; return cv; };
        dress(T.c, T.S);
        T.scn = mk_(T.cv.width, T.cv.height); const g = T.scn.getContext('2d'); g.setTransform(T.S, 0, 0, T.S, 0, 0); drawScene(g);
        T.fx = mk_(T.cv.width, T.cv.height); T.fxg = T.fx.getContext('2d');
        T.fs = mk_(Math.ceil(T.cv.width / 4), Math.ceil(T.cv.height / 4)); T.fsg = T.fs.getContext('2d');
      },
      step(T) {
        const X = T.X = frameState(T);
        const rings = [0, 0, 0]; let black = 0;
        if (X.ph === 2 || X.ph === 3) for (const d of BLOOD) { if (X.u >= d.t) rings[d.o]++; black += blackOf(d, X.u) / BLOOD.length; }
        T.dm = {
          u: X.u, ph: X.ph, rings, black, col: dropLook(BLOOD[0], X, VR).f, lens: X.lensOn, core: { C: X.core.C.slice(), R: X.core.R },
          fly: X.ps.filter(q => q.mode === 'fly').length, homeN: X.ps.filter(q => q.mode === 'home').length, moved: X.ps.filter(q => q.mode !== 'away').length,
          hero: X.hm, hud: X.hudA, grade: X.grade, p: X.p,
          nums: [X.palm[0], X.palm[1], X.Av, X.core.C[0], X.core.C[1], X.core.R, X.core.A, X.p, X.grade, X.hm.foot[0], X.hm.foot[1], X.hm.s],
        };
      },
      back(T) {
        const c = T.c, X = T.X;
        if (X.ph === 0) wreckBefore(c, T, X);
        else if (X.ph === 4) wreckAfter(c, T, X);
        else if (X.ph === 2) voidScene(c, X);
        else trans(c, T, X);
      },
      screen(T) { const X = T.X; dizzy(T, X); grade(T, X); hudStandIn(T, X); dip(T, X); },
      hud(T) {
        const X = T.X, u = X.u;
        const ph = [[TL.press, 'смотрит на ядро'], [TL.lift[0], 'E: сцена, ядро разгорается'], [TL.in[0], 'ядро поднимается из ямы'], [TL.in[1], 'мир осыпается звёздами, ядро растёт'],
          [TL.F[0], 'пустота: лицом к лицу'], [FE[0], 'кровь: первое кольцо'], [FE[1], 'второе кольцо'], [FE[2], 'третье кольцо'], [TL.black[0], 'три кольца'],
          [TL.lens[1], 'кровь чернеет, глаз открывается'], [TL.out[0], 'смотрят друг на друга'], [TL.out[1], 'обратно: ядро сжимается, кольца — его облако'],
          [LAND(3) + .3, 'пластины из обломков, по одной'], [TL.rise[0], 'дрон собран'], [TL.rise[1], 'к голове'], [END, 'сцена кончается'], [LOOP + 1, 'покой, затем повтор']].find(q => u < q[0]);
        return `${u.toFixed(1).replace('.', ',')} с из ${fmt(END)} · ${ph[1]}${X.p > .05 ? ' · голова кружится' : ''}`;
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    crowd: [['plates: [19.8, 1.0]', 'plates: [19.8, .12]']],
    stray: [["const st = { foot: HX.slice(), s: 1, a: 0, face: -1, kn: 0, lean: .04", "const st = { foot: [HX[0] + 30, HX[1]], s: 1, a: 0, face: -1, kn: 0, lean: .04"]],
    nan: [['VC = [360, 266], VR = 27,', 'VC = [360, 266], VR = -27,']],
    eye: [['X.lensOn = X.ph <= 1 ? 0 : X.ph === 4 ? 1 : ss(u, TL.lens[0], TL.lens[1]);', 'X.lensOn = X.ph <= 1 ? 0 : 1;']],
    rings: [['F: [5.8, 7.7, 9.6]', 'F: [5.8, 6.3, 6.8]']],
    drift: [['const BACK = HOVER;', 'const BACK = [HOVER[0] + 3, HOVER[1]];']],
    pale: [['const BLK = [22, 21, 24]', 'const BLK = [84, 10, 14]']],
  },

  // two loops (the second partly at ×¼, then a pause), a pick and the copy: every phase in order; the three rings filling
  // in turn; the blood red until all three are full, then black; the eye lit only as it turns; the core back exactly where
  // it began to grow; the plates only after the return, one in flight, a beat between; the hero back where it stood, the
  // drone by its head, standing in the hold; no HUD and the grade on while the scene plays; numbers finite; pick and copy.
  check(K) {
    const X = K.X, TL = X.TL, L = Math.round(X.LOOP * 60), N = 2 * L + 300, ALL = X.RN[0] + X.RN[1] + X.RN[2];
    K.at(L + 60, () => K.seg('speedMode', 1)); K.at(L + 300, () => K.seg('speedMode', 0));
    K.at(L + 360, () => K.seg('playMode', 1)); K.at(L + 420, () => K.seg('playMode', 0));
    K.at(N - 60, () => K.click(0, '.pick'));
    let st = '';
    K.at(N - 20, () => { st = K.byId.status.textContent; K.byId.copy.listeners.click[0](); });
    const q = { order: [], full: 0, red: 0, dark: 0, lensMax: 0, fly: 0, gap: 9, lastHome: 0, arr: -1, start: null, home: 0, back: 0, held: 0, cut: 0, hudOn: 0, lastT: -1, prevPh: -1 };
    let paused = 0;
    K.run(N, (t, i) => {
      const d = t.dm;
      if (d.nums.some(v => !isFinite(v))) K.fail(`a number not finite, frame ${i}`);
      if (!K.OPT.playing) { if (t.time !== q.lastT && q.lastT >= 0) K.fail('time ran while paused'); paused++; }
      q.lastT = t.time;
      if (q.order[q.order.length - 1] !== d.ph) q.order.push(d.ph);
      // the rings in turn, the blood red while they fill, black after
      const [a, b, c] = d.rings, n = a + b + c;
      if (b > 0 && a < X.RN[0]) K.fail(`ring 2 began at ${d.u.toFixed(2)} s with ring 1 at ${a}/${X.RN[0]}`);
      if (c > 0 && b < X.RN[1]) K.fail(`ring 3 began at ${d.u.toFixed(2)} s with ring 2 at ${b}/${X.RN[1]}`);
      if (d.ph === 2 && n === ALL) q.full++;
      if (d.ph === 2 && n < ALL && d.black > 0) K.fail(`blood blackening at ${d.u.toFixed(2)} s before the rings are full`);
      if (d.ph === 2 && n === ALL && d.black === 0) { if (d.col[0] > 2 * d.col[1] && d.col[0] > 60) q.red++; else K.fail(`blood not red at ${d.u.toFixed(2)} s: ${d.col.map(Math.round)}`); }
      if (d.ph === 2 && d.u > TL.black[1] && d.u < TL.out[0]) { if (Math.max(...d.col) < 40 && d.black > .999) q.dark++; else K.fail(`blood not black at ${d.u.toFixed(2)} s: ${d.col.map(Math.round)}`); }
      // the eye: shut until the blood turns
      if (d.ph <= 3 && d.lens > 0 && (n < ALL || d.black < .2)) K.fail(`the lens lit at ${d.u.toFixed(2)} s, blood black ${d.black.toFixed(2)}, rings ${n}/${ALL}`);
      if (d.ph === 2) q.lensMax = Math.max(q.lensMax, d.lens);
      // the core: back exactly where it began to grow
      if (d.ph === 0) q.start = d.core.C.slice();
      if (d.ph === 4 && q.prevPh === 3) {
        const off = Math.hypot(d.core.C[0] - q.start[0], d.core.C[1] - q.start[1]), off2 = Math.hypot(q.start[0] - X.HOVER[0], q.start[1] - X.HOVER[1]);
        if (off > .05 || off2 > 1e-9 || Math.abs(d.core.R - 1) > 1e-9) K.fail(`the core back at ${d.u.toFixed(2)} s ${off.toFixed(2)} px from where it began to grow, size ${d.core.R}`); else q.home++;
      }
      q.prevPh = d.ph;
      // the plates: only after the return, one at a time, a beat between, all home before the rise
      if (d.ph !== 4 && d.moved > 0) K.fail(`a plate moved at ${d.u.toFixed(2)} s, before the return`);
      q.fly = Math.max(q.fly, d.fly);
      if (d.homeN === 0) { q.lastHome = 0; q.arr = -1; }
      else if (d.homeN > q.lastHome) { if (q.arr >= 0) q.gap = Math.min(q.gap, d.u - q.arr); q.arr = d.u; q.lastHome = d.homeN; }
      if (d.ph === 4 && d.u >= TL.rise[0] && d.homeN !== 4) K.fail(`rising at ${d.u.toFixed(2)} s with ${d.homeN} plates home`);
      // back in the wreck: where it stood, its size, upright frame; in the hold standing, the drone by its head
      if (d.ph === 4) {
        const h = d.hero, off = Math.hypot(h.foot[0] - X.HX[0], h.foot[1] - X.HX[1]);
        if (off > 1e-6 || h.s !== 1 || h.a !== 0) K.fail(`back at ${d.u.toFixed(2)} s the hero ${off.toFixed(1)} px off, size ${h.s}`); else q.back++;
      }
      if (d.u >= X.END + .05 && d.u < X.LOOP - .3) {
        const h = d.hero, head = [X.HX[0] + 36, X.HX[1] - 176], doff = Math.hypot(d.core.C[0] - head[0], d.core.C[1] - head[1]);
        if (h.kn !== 0 || Math.abs(h.lean) > 1e-9 || h.rk !== 0 || doff > 2) K.fail(`in the hold at ${d.u.toFixed(2)} s: kn ${h.kn}, lean ${h.lean}, drone ${doff.toFixed(1)} px off its place`); else q.held++;
      }
      // a cutscene: no HUD, the grade on
      if (d.u > TL.hud[1] && d.u < TL.hudBack[0]) { if (d.hud > 0 || d.grade < .5 && d.u > TL.grade[1] && d.u < TL.gradeOut[0]) K.fail(`at ${d.u.toFixed(2)} s HUD ${d.hud.toFixed(2)}, grade ${d.grade.toFixed(2)}`); else q.cut++; }
      if (d.u < TL.press) { if (d.hud < 1 || d.grade > 0) K.fail(`before E at ${d.u.toFixed(2)} s HUD ${d.hud}, grade ${d.grade}`); else q.hudOn++; }
    });
    const doc = K.picked(), fb = K.byId.fallback.value, cand = K.tiles[0].cand;
    console.log('status after the pick:', st);
    console.log(`phases ${q.order.join('')} | rings full ${q.full} red ${q.red} black ${q.dark} | lens max ${q.lensMax.toFixed(2)} | plates in flight at most ${q.fly}, least beat ${q.gap.toFixed(2)} s | core home ${q.home} | back ${q.back} held ${q.held} | cutscene ${q.cut} HUD before E ${q.hudOn}`);
    console.log('ring ends', X.FE.map(v => v.toFixed(2)).join(', '), '| paused frames', paused, '| pick doc:', JSON.stringify(doc), '| copy text:', fb);
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    need(K.tiles.length === 1, 'one candidate');
    need(q.order.join('').includes('012340123'), 'wreck, in, vision, out, wreck, twice');
    need(q.full > 30 && q.red > 10 && q.dark > 30, 'three rings full, red, then black');
    need(q.lensMax === 1, 'the eye opens in the vision');
    need(q.fly === 1 && q.gap >= .5 && q.gap < 9, 'one plate in flight at a time, a beat between landings');
    need(q.home >= 2, 'the core back where it began to grow, each loop');
    need(q.back > 100 && q.held > 50, 'back where it stood; standing in the hold with the drone by its head');
    need(q.cut > 300 && q.hudOn > 10, 'the HUD there before E, gone and graded while the scene plays');
    need(paused > 30, 'the pause held');
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    need(doc && doc.pick === cand.id, 'the pick is in the db doc');
    need(st.includes(`«${cand.name}»`), 'the status names the pick');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the copy text names the pick');
    return ok;
  },
};
