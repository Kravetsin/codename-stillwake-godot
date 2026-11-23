// One beat of the drone meeting's vision, restaged on the «Укол» pick: threads dart from behind the giant core, each from its
// own ring's orbit, prick the hero's raised palm, and the blood running back along each thread winds it into its ring. The eye
// stays dead until the look. The void, the core, the blood and the hero are drone-vision.js's; the thread is drone-embers.js's
// «Нити». An art-pick page on the pick kit (README.md).
// node tools/pick-kit/build.js tools/pick-kit/pages/meeting-thread.js <out-dir> [--break <name>]
module.exports = {
  title: 'Нити колец',
  heading: 'Видение ядра: нити из-за ядра колют руку и становятся кольцами',
  window: false,
  kitBars: ['speedMode', 'playMode'],
  lead: `
<p class="lead">Второй круг по выбранному «Уколу». Нити больше не выходят из глаза: глаз мёртв до самого конца и открывается только для взгляда. Каждая нить вылетает <b>из-за ядра</b>, со своей орбиты: её начало спрятано за ядром, как дальняя сторона кольца. Нить жалом бьёт в поднятую ладонь — сноп искр, рука дёргается, нить держит, набухает капля. Кровь бежит <b>по нити назад</b>, и нить, сматываясь, <b>сама становится кольцом</b> вокруг ядра. Кровь никогда не летит сама по себе. Потом, как сейчас: кровь чернеет, глаз открывается, они смотрят друг на друга.</p>
<p class="lead">Четыре способа: по очереди, разом, развилкой, косой. У всех «Укол»: наезд на ладонь, укол, отъезд. Показ по умолчанию — до открытия глаза; «только укол» крутит сам миг и первое кольцо. Внизу картинки — секунды от нажатия E и что сейчас происходит.</p>
<p class="miss">Не показано: звук; начало видения (ядро в обломках, осыпание) и всё после взгляда — они не меняются, только сдвигаются на разницу в длине; отрисовка Godot (свет и цвета выйдут чуть иначе); что видят друзья.</p>`,
  toggles: [{ id: 'spanMode', bar: 1, label: 'Показ:', key: 'span', opts: [['до открытия глаза', 'eye'], ['только укол и первое кольцо', 'beat']] }],
  pick: { doc: 'picks/meetingThreadRings', subject: 'Нити колец', what: ['вариант', 'вариантом'] },

  page() {
    const TAU = Math.PI * 2;
    // today's vision (drone-vision.js, Meeting.Times): ring starts, the scene's end, the hand's reach, the blood turning black
    const TODAY = { F: [5.8, 7.7, 9.6], end: 24.8, reach: [4.8, 5.6], black: 11.6, lens: [12.6, 14.0] };
    const W0 = 4.8;   // the void is whole from here
    const VC = [360, 266], VR = 27, KV = .62, HV = { foot: [790, 345], s: .4, a: -.1 }, LEVEL = .55, MID = [500, 270];
    const SIL = 5.4;   // the core's outline (DR.core), its units
    const BL = [84, 10, 14], BROWN = [34, 12, 9], TH_RED = [226, 70, 52];
    const BLK = [22, 21, 24], BLK_L = [120, 118, 126], BLK_D = [6, 6, 8];
    const BV = [138, 18, 22], BV_L = [196, 64, 60], BV_D = [44, 5, 8];
    const ss = (u, a, b) => sm((u - a) / (b - a)), bump = (u, a, b, d) => ss(u, a, b) * (1 - ss(u, b, d));
    const mix3 = (a, b, k) => [lerp(a[0], b[0], k), lerp(a[1], b[1], k), lerp(a[2], b[2], k)];
    const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
    const vs = (a, b) => [a[0] - b[0], a[1] - b[1]], vm = (a, k) => [a[0] * k, a[1] * k];
    const unit = a => { const l = Math.hypot(a[0], a[1]) || 1; return [a[0] / l, a[1] / l]; };
    const cubic = (a, b, c, d, s) => { const k = 1 - s; return [k * k * k * a[0] + 3 * k * k * s * b[0] + 3 * k * s * s * c[0] + s * s * s * d[0], k * k * k * a[1] + 3 * k * k * s * b[1] + 3 * k * s * s * c[1] + s * s * s * d[1]]; };
    const fmt = v => v.toFixed(2).replace(/0$/, '').replace(/\.0$/, '').replace('.', ',');

    // ---------- pen and paint at any size (drone-vision.js) ----------
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
    function devGrain(g, C, a) {
      if (!g.gg || a <= .003) return;
      const S = g.S; g.save(); g.setTransform(1, 0, 0, 1, C[0] * S, C[1] * S); g.globalAlpha = a; g.fillStyle = g.gg; g.fillRect(-C[0] * S - 2, -C[1] * S - 2, LW * S + 4, LH * S + 4); g.restore();
    }

    // ---------- the giant core (drone-vision.js) ----------
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
      c.save(); c.globalCompositeOperation = big > .5 ? 'source-over' : 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
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
      if (big > 0) {
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

    // ---------- the rings: three tilted orbits; each thread leaves its own where the orbit comes out from behind the core ----------
    const SPIN_K = 7, wO = o => .8 + .25 * o, TILT = o => -.3 + o * 1.05;
    const ringPt = (o, th, rx, k) => rot(Math.cos(th) * rx * k, Math.sin(th) * rx * .3 * k, TILT(o));
    const ringP = (o, th) => ringPt(o, th, 10 + o * 2.2, KV);
    // per ring: the exit th, on the far half just outside the outline, heading most toward the hero; the thread runs there along
    // the orbit (sense s) from th0, deep behind the core
    const EXIT = [0, 1, 2].map(o => {
      let best = null;
      for (const s of [1, -1]) for (let j = 0; j < 628; j++) {
        const th = j / 100 - Math.PI, p = ringP(o, th), r = Math.hypot(p[0], p[1]);
        if (Math.sin(th) >= 0 || r < SIL + .3 || r > SIL + 1.2) continue;
        let th0 = null; for (let D = .9; D < 2.6; D += .05) { const a = th - s * D, q = ringP(o, a); if (Math.sin(a) < 0 && Math.hypot(q[0], q[1]) < SIL - 1.1) { th0 = a; break; } }
        if (th0 === null) continue;
        const q = ringP(o, th + s * .01), tl = Math.hypot(q[0] - p[0], q[1] - p[1]) || 1, sc = (q[0] - p[0]) / tl + .15 * p[0] / 6;
        if (!best || sc > best.sc) best = { th, s, th0, sc };
      }
      return best;
    });
    // a ring clock: ring o takes blood from F[o]; the blood turns black, the rings slow and the eye opens as long after the
    // last ring begins as today (11.6 after 9.6)
    function clockOf(F) {
      const b0 = Math.max(...F) + (TODAY.black - TODAY.F[2]);
      const TL = { F, black: [b0, b0 + 2.3], spin: [b0, b0 + 2], lens: [b0 + 1, b0 + 2.4], dizzy: [F[0] - .3, F[0] + .7, b0 + 2, b0 + 3] };
      const VMAX = TAU * SPIN_K / (TL.spin[0] + (TL.spin[1] - TL.spin[0]) / 2);
      const spinS = u => { const D = TL.spin[1] - TL.spin[0]; if (u <= TL.spin[0]) return VMAX * Math.max(0, u); const x = Math.min(u, TL.spin[1]) - TL.spin[0]; return VMAX * (TL.spin[0] + x - x * x / (2 * D)); };
      const phiO = (o, u) => wO(o) * (u + 1.3) + spinS(u);
      const angV = (o, u) => wO(o) + VMAX * c01(1 - (u - TL.spin[0]) / (TL.spin[1] - TL.spin[0]));
      const BLOOD = DR.P.map((P, i) => {
        const o = i % 3, dir = o % 2 ? -1 : 1, Fo = F[o], f0 = phiO(o, Fo);
        const want = f0 + (((dir * (EXIT[o].th - P.ph) - f0) % TAU) + TAU) % TAU;
        let a = Fo, b = Fo + 4; for (let k = 0; k < 44; k++) { const m = (a + b) / 2; if (phiO(o, m) < want) a = m; else b = m; }
        return { i, o, dir, P, rx: 10 + o * 2.2, jit: (P.r1 - .5) * 1.6, t: b, hot: P.hot || P.r1 > .86, kb0: TL.black[0] + o * .25 + P.r1 * .45 };
      });
      const RN = [0, 1, 2].map(o => BLOOD.filter(d => d.o === o).length);
      const FE = [0, 1, 2].map(o => Math.max(...BLOOD.filter(d => d.o === o).map(d => d.t)));
      const LEADS = [0, 1, 2].map(o => BLOOD.filter(d => d.o === o).reduce((a, b) => b.t < a.t ? b : a));
      return { TL, phiO, angV, BLOOD, RN, FE, LEADS, b0 };
    }
    const toPg = (X, q) => [X.core.C[0] + q[0] * X.core.R, X.core.C[1] + q[1] * X.core.R];
    // the angles ring o's ember line covers: its thread's own arc, then on as far as its blood has wound, the whole orbit at last
    function ringLine(o, X) {
      const K = X.K, E = EXIT[o], lead = K.LEADS[o];
      if (X.v < lead.t) return [];
      const w = Math.min(TAU, K.phiO(o, X.v) - K.phiO(o, lead.t));
      if (w >= TAU - 1e-6) return [[0, TAU]];
      const dir = o % 2 ? -1 : 1;
      return [E.s > 0 ? [E.th0, E.th] : [E.th, E.th0], dir > 0 ? [E.th, E.th + w] : [E.th - w, E.th]];
    }
    function linePts(o, X, iv, stp) {
      const out = [];
      for (const [a0, a1] of iv) { const n = Math.max(2, Math.ceil((a1 - a0) / stp)), run = []; for (let j = 0; j <= n; j++) { const a = lerp(a0, a1, j / n); run.push({ p: toPg(X, ringP(o, a)), z: Math.sin(a) }); } out.push(run); }
      return out;
    }
    function ringLines(c, X, front) {
      const w = Math.sqrt(X.cam.z);
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
      for (let o = 0; o < 3; o++) {
        const runs = linePts(o, X, X.lines[o], .03); if (!runs.length) continue;
        c.beginPath();
        for (const run of runs) { let open = false; for (let j = 1; j < run.length; j++) { const keep = (run[j].z + run[j - 1].z >= 0) === front; if (!keep) { open = false; continue; } if (!open) { c.moveTo(run[j - 1].p[0], run[j - 1].p[1]); open = true; } c.lineTo(run[j].p[0], run[j].p[1]); } }
        c.strokeStyle = css(EMBER, .09); c.lineWidth = 2.8 * w; c.stroke();
        c.strokeStyle = css(pale(EMBER, .35), .5); c.lineWidth = .6 * w; c.stroke();
      }
      c.restore();
    }

    // ---------- the blood: drawDrone's 170 motes, each running back along its ring's thread, then winding on its ring ----------
    const ON = .85;   // a drop's share of its run on the thread; the rest it eases onto its own place in the ring's band
    function mkPoly(pts) { const cum = [0]; for (let j = 1; j < pts.length; j++) cum.push(cum[j - 1] + dist(pts[j], pts[j - 1])); return { pts, cum, L: cum[cum.length - 1] || 1 }; }
    function atS(g, s) {
      const d = c01(s) * g.L; let i = 1; while (i < g.pts.length - 1 && g.cum[i] < d) i++;
      return lerp2(g.pts[i - 1], g.pts[i], c01((d - g.cum[i - 1]) / ((g.cum[i] - g.cum[i - 1]) || 1)));
    }
    function projS(g, p) {
      let best = { d: 1e9, s: 0 };
      for (let j = 1; j < g.pts.length; j++) {
        const a = g.pts[j - 1], b = g.pts[j], dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy || 1, e = c01(((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / l2);
        const d = Math.hypot(a[0] + dx * e - p[0], a[1] + dy * e - p[1]);
        if (d < best.d) best = { d, s: (g.cum[j - 1] + e * Math.sqrt(l2)) / g.L };
      }
      return best;
    }
    function dropAt(d, X) {
      const v = X.v, trav = X.B.trav, t0 = d.t - trav; if (v < t0) return null;
      const { R } = X.core, K = X.K, r = d.P.sz * Math.max(.5, 1.6 * Math.sqrt(R / VR)), rx = d.rx + d.jit * .2;
      const ringAt = a => toPg(X, ringPt(d.o, a, rx, KV));
      if (v < d.t) {
        const e = (v - t0) / trav, rt = X.route[d.o], st = 3 / rt.L, j = ss(e, ON, 1), off = vs(ringAt(EXIT[d.o].th), rt.pts[rt.pts.length - 1]);
        const at = s => { const p = atS(rt, s); return [p[0] + off[0] * j, p[1] + off[1] * j]; };
        return { p: at(e), tl: [at(e - 2.4 * st), at(e - 1.2 * st)], z: 1, r: r * .8, on: e < ON };
      }
      const th = d.P.ph + d.dir * K.phiO(d.o, v), D = clamp(.06 * K.angV(d.o, v), .08, .3);
      return { p: ringAt(th), tl: [ringAt(th - d.dir * D), ringAt(th - d.dir * D * .5)], z: Math.sin(th), r, ring: true };
    }
    const blackOf = (d, v) => ss(v, d.kb0, d.kb0 + 1.2);
    function dropLook(d, X) {
      const kb = blackOf(d, X.v), h = .45 + .4 * Math.sin(X.t * 11 + d.P.ph * 7), keep = d.P.hot || d.hot ? 1 : 0;
      return { f: mix3(BV, BLK, kb), rim: mix3(BV_D, BLK_D, kb), sh: mix3(BV_L, BLK_L, kb), shA: .8, lit: .32 * kb, emb: kb * keep * h };
    }
    function drawDrops(c, X, front) {
      c.save(); c.lineCap = 'round'; c.lineJoin = 'round';
      X.K.BLOOD.forEach((d, i) => {
        const q = X.drops[i]; if (!q || (q.z >= 0) !== front) return;
        const L = dropLook(d, X), w = q.r * 1.8, pts = [q.tl[0], q.tl[1], q.p];
        strokePts(c, pts); c.strokeStyle = css(L.rim, .9); c.lineWidth = w + .8; c.stroke();
        strokePts(c, pts); c.strokeStyle = css(L.f); c.lineWidth = w; c.stroke();
        c.beginPath(); c.arc(q.p[0], q.p[1], q.r + .4, 0, TAU); c.fillStyle = css(L.rim, .9); c.fill();
        c.beginPath(); c.arc(q.p[0], q.p[1], q.r, 0, TAU); c.fillStyle = css(L.f); c.fill();
        if (L.lit > .01 && q.r > 1.2) { c.save(); c.translate(-.35 * q.r, -.45 * q.r); strokePts(c, pts); c.strokeStyle = css([176, 184, 196], L.lit); c.lineWidth = .9; c.stroke(); c.restore(); }
        if (L.shA > .02 && q.r > 1.2) { c.beginPath(); c.arc(q.p[0] - q.r * .3, q.p[1] - q.r * .35, q.r * .38, 0, TAU); c.fillStyle = css(L.sh, L.shA); c.fill(); }
        if (L.emb > .01) {
          c.save(); c.globalCompositeOperation = 'lighter'; c.beginPath(); c.arc(q.p[0], q.p[1], q.r * .9, 0, TAU); c.fillStyle = css(pale(EMBER, .25), .9 * L.emb); c.fill(); c.restore();
          halo(c, q.p[0], q.p[1], q.r * 3.4, EMBER, .3 * L.emb);
        }
      });
      c.restore();
    }

    // ---------- the threads («Нити»): from behind the core along the orbit, out past the outline, off to the hand ----------
    const NA = 14, NT = 30;
    function threadGeo(o, X, end, wave, seed, via) {
      const E = EXIT[o], arc = j => toPg(X, ringP(o, lerp(E.th0, E.th, j / NA))), pts = [];
      for (let j = 0; j <= NA; j++) pts.push(arc(j));
      const ex = pts[NA], tn = unit(vs(toPg(X, ringP(o, E.th + E.s * .02)), ex)), ou = unit(vs(ex, X.core.C));
      const dOut = unit([tn[0] * .6 + ou[0] * .8, tn[1] * .6 + ou[1] * .8]);
      const leg = (a, b, n, from, wv) => {
        const L0 = dist(a, b) || 1, p1 = [a[0] + from[0] * L0 * .35, a[1] + from[1] * L0 * .35], p2 = lerp2(a, b, .72), nx = -(b[1] - a[1]) / L0, ny = (b[0] - a[0]) / L0;
        for (let j = 1; j <= n; j++) { const s = j / n, p = cubic(a, p1, p2, b, s), w = wv(s); pts.push([p[0] + nx * w, p[1] + ny * w]); }
      };
      const quiver = s => wave.a * Math.sin(Math.PI * s) * Math.sin(s * wave.k + X.t * wave.w + seed);
      if (!via) leg(ex, end, NT, dOut, quiver);
      else {   // the braid: to the knot, then twisted about the others down the cord
        leg(ex, via.M, 12, dOut, () => 0);
        const cd = unit(vs(end, via.M));
        leg(via.M, end, NT, cd, s => via.tw * Math.sin(Math.PI * s) * Math.sin(s * 13 + via.ph + X.t * 1.3) + quiver(s));
      }
      const g = mkPoly(pts); g.ia = NA; g.split = g.cum[NA] / g.L; return g;
    }
    function threadsOf(X) {
      const { cand, B, u, v, K } = X, kind = cand.kind, lastFE = Math.max(...K.FE), out = [];
      for (let o = 0; o < 3; o++) {
        const [a, b] = B.out[o], touch = B.touch[o], waits = touch > b + .01, latch = kind === 'fork' && o > 0;
        if (latch && !X.latch) { const g0 = out[0].geo; X.latchS = g0.split + .82 * (1 - g0.split); X.latch = atS(g0, X.latchS); }
        const target = latch ? X.latch : X.palm;
        const end = waits ? lerp2(X.wait[o], target, ss(u, touch - .12, touch)) : target;
        const quiver = (waits ? 3.2 * bump(u, b - .05, b, b + .5) : 0) + 1.4 * bump(u, touch - .02, touch, touch + .4);
        const via = kind === 'braid' ? { M: X.knot, tw: 2.4 * X.cam.z, ph: o * TAU / 3 } : null;
        const geo = threadGeo(o, X, end, { a: quiver * X.cam.z, k: 8, w: 45 }, o * 1.7 + 2, via);
        const e = c01((u - a) / (b - a)), fe = kind === 'fork' && o === 0 ? lastFE : K.FE[o];
        out.push({ o, geo, ease: 1 - (1 - e) * (1 - e) * (1 - e), rel: ss(v, fe + .05, fe + .55), latch, target, seed: o * 3.1 });
      }
      const Lmax = Math.max(...out.map(q => q.geo.L));
      for (const q of out) {
        // a braid's strands keep their tips together; the others dart each along its own length
        const f0 = kind === 'braid' ? Math.max(0, q.geo.L - (1 - q.ease) * Lmax) / q.geo.L : q.ease;
        q.f = f0 - Math.max(0, f0 - q.geo.split) * q.rel;   // letting go, the tail winds back onto the orbit
        q.tip = atS(q.geo, q.f);
      }
      for (const q of out) q.held = q.rel === 0 && q.f >= 1 - 1e-9 && dist(q.tip, q.target) < .6 && (!q.latch || out[0].held);
      return out;
    }
    function drawThread(c, q, X, a, b) {
      const b2 = Math.min(b, q.f); if (b2 <= a + 1e-4) return;
      const w = Math.sqrt(X.cam.z), pts = sub(q.geo.pts, a, b2); if (pts.length < 2) return;
      const col = mix3(EMBER, TH_RED, .6 * q.red);
      c.save(); c.globalCompositeOperation = 'lighter'; c.lineCap = 'round'; c.lineJoin = 'round';
      strokePts(c, pts); c.strokeStyle = css(col, .1); c.lineWidth = 3.6 * w; c.stroke();
      strokePts(c, pts); c.strokeStyle = css(col, .26); c.lineWidth = 1.4 * w; c.stroke();
      strokePts(c, pts); c.strokeStyle = css(pale(col, .45), .85); c.lineWidth = .55 * w; c.stroke();
      // sparks run out to the tip while it reaches, back toward the core once the blood runs
      const L = q.geo.L, gap = 24 * w, n = Math.ceil(L / gap) + 1, dir = q.held ? -1 : 1, off = ((dir * X.t * 62 * w + q.seed * 9) % gap + gap) % gap, sz = 1.2 * w;
      for (let i = 0; i < n; i++) {
        const dd = i * gap + off; if (dd < a * L || dd > b2 * L) continue;
        const s = dd / L, p = atS(q.geo, s), p2 = atS(q.geo, s + dir * 2 / L), hd = dist(p, p2) || 1, hx = (p2[0] - p[0]) / hd, hy = (p2[1] - p[1]) / hd;
        const h = .75 + .25 * Math.sin(X.t * 13 + i * 2.1);
        for (let m = 0; m < 3; m++) { const bb = m * 1.5 * w; c.fillStyle = css(pale(EMBER, .3 * (1 - m / 3)), (.95 - m * .3) * h); c.fillRect(p[0] - hx * bb - sz / 2, p[1] - hy * bb - sz / 2, sz, sz); }
      }
      c.restore();
      if (b >= 1 && !q.held && q.f > q.geo.split + .01) { c.save(); c.globalCompositeOperation = 'lighter'; c.fillStyle = css(pale(EMBER, .6), .95); c.fillRect(q.tip[0] - sz * .8, q.tip[1] - sz * .8, sz * 1.6, sz * 1.6); c.restore(); halo(c, q.tip[0], q.tip[1], 7 * w, EMBER, .55); }
    }
    // the prick: a burst of sparks off the palm (or off the thread a latch bites), and the drop welling in the wound
    const RAND = (() => { const r = rng(515), out = []; for (let i = 0; i < 12; i++) out.push([r(), r()]); return out; })();
    function pricks(c, X) {
      const w = Math.sqrt(X.cam.z), u = X.u;
      for (const [t, amp, at] of X.B.prick) {
        const e = c01((u - t) / .45); if (e <= 0 || e >= 1) continue;
        const P = at === 'latch' ? X.latch || X.palm : X.palm, back = Math.atan2(X.core.C[1] - P[1], X.core.C[0] - P[0]), n = amp > .1 ? 10 : 6;
        c.save(); c.globalCompositeOperation = 'lighter';
        RAND.slice(0, n).forEach(([r1, r2], j) => {
          const a = back + Math.PI + (j / (n - 1) - .5) * 2.6 + (r1 - .5) * .4, d = (5 + 14 * sm(e) * (.6 + r2)) * X.cam.z * .45 * (amp > .1 ? 1 : .6), s = 1.3 * w;
          c.fillStyle = css(pale(EMBER, .4), .95 * (1 - e)); c.fillRect(P[0] + Math.cos(a) * d - s / 2, P[1] + Math.sin(a) * d - s / 2, s, s);
        });
        c.restore();
        halo(c, P[0], P[1], 13 * w, pale(EMBER, .3), .75 * bump(u, t - .05, t, t + .55) * (amp > .1 ? 1 : .5));
      }
      const lastFE = Math.max(...X.K.FE), r = (1 + 1.6 * ss(u, X.B.well[0], X.B.well[1])) * w * (1 - ss(X.v, lastFE + .3, lastFE + 1.2));
      if (u >= X.B.well[0] && r > .2) { c.beginPath(); c.arc(X.palm[0], X.palm[1] + .6 * w, r + .4, 0, TAU); c.fillStyle = css(BV_D, .9); c.fill(); c.beginPath(); c.arc(X.palm[0], X.palm[1] + .6 * w, r, 0, TAU); c.fillStyle = css(BV); c.fill(); }
    }

    // ---------- the hero: the kit's rig, weightless before the giant (drone-vision.js) ----------
    const HB = newHero('me', 0, 1.3);
    function heroPose(st) { HB.face = st.face; HB.kn = st.kn; HB.lean = st.lean; HB.rk = st.rk; HB.rp = st.rp; HB.look = st.look; HB.air = false; HB.amp = 0; HB.ph = 0; HB.e = 0; st.P = poseOf(HB); return st; }
    function heroDraw(g, st) { HB.face = st.face; HB.look = st.look; HB.P = st.P; HB.x = 0; HB.y = 0; g.save(); g.translate(st.foot[0], st.foot[1]); g.rotate(st.a); g.scale(st.s, st.s); drawHero(g, HB); g.restore(); }
    const toPage = (st, p) => { const q = rot(st.s * st.face * Z * p[0], st.s * Z * p[1], st.a); return [st.foot[0] + q[0], st.foot[1] + q[1]]; };
    function localOf(st, p) { const q = rot(p[0] - st.foot[0], p[1] - st.foot[1], -st.a); return [q[0] / (st.s * st.face * Z), q[1] / (st.s * Z)]; }
    const lookAt = (st, p) => { const l = localOf(st, p); return clamp(headTurn(l[0] - .3, l[1] + RIG.Neck), -HEAD.Up, HEAD.Down); };

    // ---------- the camera: the void is on its own screen layer, so it may frame closer than play ----------
    const camP = (cam, p) => [cam.s[0] + (p[0] - cam.f[0]) * cam.z, cam.s[1] + (p[1] - cam.f[1]) * cam.z];
    function camAt(keys, u) {
      let i = 0; while (i < keys.length - 1 && u >= keys[i + 1][0]) i++;
      const a = keys[i], b = keys[Math.min(i + 1, keys.length - 1)], e = b === a ? 0 : ss(u, a[0], b[0]);
      return { z: Math.exp(lerp(Math.log(a[1]), Math.log(b[1]), e)), f: lerp2(a[2], b[2], e), s: lerp2(a[3], b[3], e) };
    }
    function heroAt(u, cam, rk, lean, p) {
      const s = HV.s;
      const st = { foot: camP(cam, [HV.foot[0] + Math.sin(u * .5) * 5 * s, HV.foot[1] + Math.cos(u * .37) * 7 * s]), s: s * cam.z, a: HV.a - .22 + .06 * Math.sin(u * .41), face: -1, kn: .1 + .05 * Math.sin(u * .7), lean: -.18 + .05 * Math.sin(u * .53) + lean, rk, look: 0 };
      const loc = localOf(st, p), sh = [.4, -RIG.Shoulder], dx = loc[0] - sh[0], dy = loc[1] - sh[1], L = Math.hypot(dx, dy) || 1;
      st.rp = [sh[0] + dx / L * 40, sh[1] + dy / L * 40];
      return heroPose(st);
    }
    const PALM0 = (() => { const st = heroAt(7, { z: 1, f: MID, s: MID }, 1, 0, VC); return toPage(st, st.P.armF[2]); })();

    // ---------- the void ----------
    const VSTARS = (() => { const r = rng(77), s = []; for (let i = 0; i < 300; i++) { const b = r(); s.push([r() * LW, r() * LH, b < .06 ? 1.8 : b < .3 ? 1.1 : .7, .18 + r() * .55, r() < .15]); } return s; })();
    function sky(g) {
      const gr = g.createLinearGradient(0, 0, 0, LH); gr.addColorStop(0, '#020307'); gr.addColorStop(1, '#05070c'); g.fillStyle = gr; g.fillRect(0, 0, LW, LH);
      for (const [x, y, sz, al, warm] of VSTARS) { g.fillStyle = warm ? `rgba(240,226,206,${al.toFixed(3)})` : `rgba(206,218,238,${al.toFixed(3)})`; g.fillRect(x, y, sz, sz); }
    }
    // the core's face: dull while it reaches, its cracks red while it drinks; the eye opens only as the blood turns
    function coreLook(X) {
      const v = X.v, TL = X.K.TL, drink = ss(v, TL.F[0], Math.max(...X.K.FE)), kb = ss(v, TL.black[0], TL.black[1]);
      return {
        lens: { on: X.lensOn, glow: .35 + .5 * bump(v, TL.lens[0], TL.lens[1], TL.lens[1] + .6) },
        crack: [drink, mix3([160, 26, 22], EMBER, kb), .5],
        soak: .3 * drink * (1 - kb),
      };
    }
    function voidGlow(c, X) {
      const v = X.v, TL = X.K.TL, { C, R } = X.core, kb = ss(v, TL.black[0], TL.black[1]), drink = ss(v, TL.F[0], Math.max(...X.K.FE)) * (1 - kb);
      halo(c, C[0], C[1], (5.4 * R + 20) * 1.15, [150, 20, 18], .35 * drink * .3);
      halo(c, C[0], C[1], (5.4 * R + 20) * 1.1, EMBER, .25 * kb * .25);
    }
    function voidScene(c, X) {
      sky(c);
      ringLines(c, X, false);
      for (const q of X.th) drawThread(c, q, X, 0, q.geo.split);
      drawDrops(c, X, false);
      const { C, R, A } = X.core;
      gCore(c, C, R, A, coreLook(X));
      voidGlow(c, X);
      ringLines(c, X, true);
      heroDraw(c, X.hv);
      for (const q of X.th) drawThread(c, q, X, q.geo.split, 1);
      drawDrops(c, X, true);
      pricks(c, X);
    }

    // ---------- the screen: the dizziness, the grade, the loop's dip (drone-vision.js) ----------
    function tunnel(p, t) {
      const blur = sm(p / .9) * (1 + .12 * Math.sin(t * (2.4 - Math.min(p, 1)))), gh = Math.sin(t * 1.9) * (14 / 480) * sm((p - .1) / .7);
      return { blur: c01(blur), dark: .62 * Math.pow(Math.min(p, 1.3), 1.2), sway: Math.sin(t * .8) * .028 * Math.min(p, 1.2), gx: gh, gy: gh * .25, swim: .006 * sm((p - .2) / .8) };
    }
    function copyOut(T) { const b = T.fxg; b.setTransform(1, 0, 0, 1, 0, 0); b.globalAlpha = 1; b.filter = 'none'; b.globalCompositeOperation = 'copy'; b.drawImage(T.cv, 0, 0); b.globalCompositeOperation = 'source-over'; }
    function dizzy(T, X) {
      const c = T.c, W = T.cv.width, H = T.cv.height, S = T.S, p = X.p;
      if (p <= .004) return;
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
    function grade(T) {
      const c = T.c, W = T.cv.width, H = T.cv.height;
      copyOut(T);
      c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'copy';
      c.filter = 'saturate(0.55) contrast(1.08) brightness(0.96)';
      c.drawImage(T.fx, 0, 0); c.filter = 'none';
      c.globalCompositeOperation = 'multiply'; c.fillStyle = css([212, 222, 228], 1); c.fillRect(0, 0, W, H);
      c.globalCompositeOperation = 'source-over';
      const R = Math.hypot(W, H) * .58, v = c.createRadialGradient(W / 2, H / 2, R * .35, W / 2, H / 2, R);
      v.addColorStop(0, 'rgba(3,4,7,0)'); v.addColorStop(1, css([3, 4, 7], .5)); c.fillStyle = v; c.fillRect(0, 0, W, H);
      c.restore();
    }
    function dip(T, X) {
      const d = X.w > X.LP - .3 ? (X.w - (X.LP - .3)) / .3 : X.w < .3 ? 1 - X.w / .3 : 0; if (d <= 0) return;
      const c = T.c; c.save(); c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.fillStyle = css([5, 7, 11], sm(d)); c.fillRect(0, 0, T.cv.width, T.cv.height); c.restore();
    }

    // ---------- the frame ----------
    function frameState(T) {
      const cand = T.cand, B = cand.B, K = cand.K, TL = K.TL, D = B.blood - TL.F[0] + B.trav;   // the first drop leaves the palm at B.blood
      const W1 = (OPT.span === 'eye' ? TL.lens[1] + .5 : TL.F[0] + 2) + D, LP = W1 - W0 + .3;
      const w = ((T.time % LP) + LP) % LP, u = W0 + w, v = u - D;
      const X = { T, cand, B, K, D, u, v, w, LP, W1, t: T.time };
      const cam = X.cam = camAt(cand.cam, u), C = camP(cam, VC), R = VR * cam.z;
      // the hand rises once, flinches at each prick, and sinks once every thread has let go
      const lastFE = Math.max(...K.FE);
      let rk = ss(u, B.hand[0], B.hand[1]), lean = 0;
      for (const [t, amp] of B.prick) { const j = bump(u, t, t + .06, t + .55); rk -= amp * j; lean -= .25 * amp * j; }
      rk *= 1 - .6 * ss(v, lastFE + .3, lastFE + 1.5);
      const hv = heroAt(u, cam, rk, lean, C);
      const helm = toPage(hv, hv.P.helm), A = Math.atan2(helm[1] - C[1], helm[0] - C[0]);
      X.core = { C, R, A };
      X.lens = [C[0] + Math.cos(A) * 2.2 * R, C[1] + Math.sin(A) * 2.2 * R];
      X.palm = toPage(hv, hv.P.armF[2]);
      const full = heroAt(u, cam, 1, lean, C), pf = toPage(full, full.P.armF[2]);
      X.knot = [C[0] + unit(vs(pf, C))[0] * 7.9 * R, C[1] + unit(vs(pf, C))[1] * 7.9 * R];
      // each thread waits a hand's breadth short of the reached palm, on its own side
      X.wait = [0, 1, 2].map(o => { const from = cand.kind === 'braid' ? X.knot : toPg(X, ringP(o, EXIT[o].th)), d = unit(vs(from, pf)); return [pf[0] + d[0] * 9 * cam.z, pf[1] + d[1] * 9 * cam.z]; });
      X.th = threadsOf(X);
      for (const q of X.th) q.red = ss(v, K.LEADS[q.o].t - B.trav, K.LEADS[q.o].t - B.trav + .8) * (1 - q.rel);
      // the way each ring's blood runs: back along its own thread from the palm (through the first thread to the fork) to its exit
      X.route = X.th.map(q => {
        let pts = q.geo.pts.slice(q.geo.ia).reverse();
        if (q.latch) pts = sub(X.th[0].geo.pts, X.latchS, 1).reverse().concat(pts.slice(1));
        return mkPoly(pts);
      });
      X.routeHeld = X.th.map(q => q.held);
      // the hero watches the first thread come, then looks back to the core
      const watch = ss(u, B.out[0][0] + .2, B.out[0][0] + .8) * (1 - ss(u, B.touch[0] + .2, B.touch[0] + .9));
      hv.look = lerp(lookAt(hv, C), lookAt(hv, X.th[0].tip), watch); heroPose(hv);
      X.hv = hv;
      X.lines = [0, 1, 2].map(o => ringLine(o, X));
      X.drops = K.BLOOD.map(d => dropAt(d, X));
      X.p = LEVEL * ss(v, TL.dizzy[0], TL.dizzy[1]) * (1 - ss(v, TL.dizzy[2], TL.dizzy[3]));
      X.lensOn = ss(v, TL.lens[0], TL.lens[1]);
      return X;
    }
    // what the check measures on a frame: the threads' roots, the drops against their routes and rings, the rings' lines
    function stats(X) {
      const { C, R } = X.core, K = X.K;
      const orb = [0, 1, 2].map(o => { const p = []; for (let j = 0; j <= 240; j++) p.push(toPg(X, ringP(o, j / 240 * TAU))); return mkPoly(p); });
      const angleOn = (o, p) => { let b = 1e9, a = 0; for (let j = 0; j < 720; j++) { const q = toPg(X, ringP(o, j / 720 * TAU)), d = dist(p, q); if (d < b) { b = d; a = j / 720 * TAU; } } return a; };
      const lines = [0, 1, 2].map(o => linePts(o, X, X.lines[o], .06).map(run => mkPoly(run.map(q => q.p))));
      const th = X.th.map(q => {
        const root = q.geo.pts[0], o = q.o, ls = lines[o];
        const toLine = p => ls.length ? Math.min(...ls.map(g => projS(g, p).d)) : 0;
        return {
          f: q.f, out: q.f > q.geo.split + 1e-6, held: q.held, rootOrb: projS(orb[o], root).d, rootSin: Math.sin(angleOn(o, root)), rootR: dist(root, C) / R, rootLens: dist(root, X.lens) / R,
          lineOrb: ls.length ? Math.max(...ls.map(g => Math.max(...g.pts.filter((_, j) => j % 3 === 0).map(p => projS(orb[o], p).d)))) : 0,
          lineRoot: ls.length ? toLine(root) : 0, arcOff: ls.length && q.f > 0 ? Math.max(...q.geo.pts.slice(0, q.geo.ia + 1).map(toLine)) : 0, toLine,
        };
      });
      const s = { th, on: [0, 0, 0], ringed: [0, 0, 0], offRoute: 0, lead: [null, null, null], ringOff: -1e9, black: 0 };
      K.BLOOD.forEach((d, i) => {
        const q = X.drops[i]; s.black += blackOf(d, X.v) / K.BLOOD.length; if (!q) return;
        if (q.ring) { s.ringed[d.o]++; s.ringOff = Math.max(s.ringOff, th[d.o].toLine(q.p) - (Math.abs(d.jit) * .2 * KV * R + 1.5)); }
        if (q.on) { s.on[d.o]++; const p = projS(X.route[d.o], q.p); s.offRoute = Math.max(s.offRoute, p.d); if (d === K.LEADS[d.o]) s.lead[d.o] = p.s; }
      });
      return s;
    }

    // ---------- the candidates: seconds from E; blood is when the first drop leaves the palm, trav its run to the ring ----------
    const PUSH = [[5.45, 1, MID, MID], [6.5, 2.6, PALM0, [600, 285]]];
    const cands = [
      {
        id: 'oneByOne', name: 'По очереди', kind: 'seq',
        idea: 'Ритм как сейчас — кольцо за кольцом. Из-за ядра, с невидимой стороны первой орбиты, жалом вылетает нить и дрожит у руки; камера наезжает на ладонь. Герой поднимает руку — укол, искры, рука дёргается, нить держит. Набухает капля, кровь бежит по нити назад, и нить, сматываясь, становится первым кольцом. Камера отъезжает: из-за ядра, со своего места, бьёт вторая нить — в ту же ранку, — за ней третья.',
        frame: 'наезд на ладонь ×2,6 к 6,5 с, отъезд к ×1,05 с 7,4 до 8,4 с — до второй нити',
        F: [5.8, 7.7, 9.6],
        B: { out: [[5.0, 5.55], [8.55, 9.0], [10.45, 10.9]], hand: [5.9, 6.75], touch: [6.75, 9.0, 10.9], prick: [[6.75, .3, 'palm'], [9.0, .14, 'palm'], [10.9, .14, 'palm']], well: [6.95, 7.45], blood: 7.45, trav: .8 },
        cam: [...PUSH, [7.4, 2.6, PALM0, [600, 285]], [8.4, 1.05, [530, 285], [500, 280]]],
        ev: [[5.0, 'из-за ядра вылетает первая нить и дрожит у руки'], [5.9, 'герой поднимает руку'], [6.75, 'укол: искры, рука дёргается'], [6.95, 'набухает капля'], [8.55, 'из-за ядра бьёт вторая нить'], [9.0, 'вторая — в ту же ранку'], [10.45, 'третья нить'], [10.9, 'третья — в ранку']],
      },
      {
        id: 'allAtOnce', name: 'Разом', kind: 'all',
        idea: 'Три нити выходят из-за ядра с трёх мест — справа сверху, справа снизу и сверху — и окружают руку с трёх сторон. Рука поднимается, все три колют разом: один сноп искр. Кровь бежит по трём нитям сразу, и три кольца наматываются вместе — короче, чем сейчас.',
        frame: 'наезд на ладонь ×2,6 к 6,5 с, отъезд к ×1,12 с 7,9 до 9,1 с',
        F: [5.8, 5.8, 5.8],
        B: { out: [[5.0, 5.55], [5.08, 5.62], [5.16, 5.7]], hand: [5.9, 6.75], touch: [6.75, 6.75, 6.75], prick: [[6.75, .34, 'palm']], well: [6.95, 7.45], blood: 7.45, trav: .8 },
        cam: [...PUSH, [7.9, 2.6, PALM0, [600, 285]], [9.1, 1.12, [540, 285], [500, 280]]],
        ev: [[5.0, 'из-за ядра выходят три нити и окружают руку'], [5.9, 'герой поднимает руку'], [6.75, 'укол тремя разом: искры, рука дёргается'], [6.95, 'набухает капля']],
      },
      {
        id: 'fork', name: 'Развилка', kind: 'fork',
        idea: 'Колет одна нить, как в «Уколе». Пока набухает капля, две другие выходят из-за ядра со своих мест и вцепляются в первую у самой ладони — две маленькие вспышки. Кровь бежит по первой нити и у развилки делится на три: каждая нить уносит свою долю за ядро и наматывает своё кольцо, все три вместе.',
        frame: 'наезд на ладонь ×2,6 к 6,5 с (развилка видна крупно), отъезд к ×1,12 с 7,9 до 9,1 с',
        F: [5.8, 5.8, 5.8],
        B: { out: [[5.0, 5.55], [6.85, 7.2], [6.95, 7.3]], hand: [5.9, 6.75], touch: [6.75, 7.2, 7.3], prick: [[6.75, .3, 'palm'], [7.2, .04, 'latch'], [7.3, .04, 'latch']], well: [6.95, 7.45], blood: 7.45, trav: .8 },
        cam: [...PUSH, [7.9, 2.6, PALM0, [600, 285]], [9.1, 1.12, [540, 285], [500, 280]]],
        ev: [[5.0, 'из-за ядра вылетает нить и дрожит у руки'], [5.9, 'герой поднимает руку'], [6.75, 'укол: искры, рука дёргается'], [6.85, 'две нити выходят из-за ядра'], [7.2, 'вцепляются в первую у ладони']],
      },
      {
        id: 'braid', name: 'Коса', kind: 'braid',
        idea: 'Три нити выходят из-за ядра каждая со своей орбиты, у края ядра сплетаются в одну косу и бьют в руку одним жалом. Кровь бежит по косе и у ядра расходится по прядям — по очереди, как сейчас: первое кольцо, второе, третье. Каждая прядь отпускает руку и сматывается в своё кольцо, когда оно полно; коса расплетается.',
        frame: 'наезд на ладонь ×2,6 к 6,5 с, отъезд к ×1,12 с 7,9 до 9,1 с',
        F: [5.8, 7.7, 9.6],
        B: { out: [[5.0, 5.6], [5.0, 5.6], [5.0, 5.6]], hand: [5.9, 6.75], touch: [6.75, 6.75, 6.75], prick: [[6.75, .32, 'palm']], well: [6.95, 7.45], blood: 7.45, trav: .8 },
        cam: [...PUSH, [7.9, 2.6, PALM0, [600, 285]], [9.1, 1.12, [540, 285], [500, 280]]],
        ev: [[5.0, 'из-за ядра выходят три нити и сплетаются в косу'], [5.9, 'герой поднимает руку'], [6.75, 'укол косой: искры, рука дёргается'], [6.95, 'набухает капля']],
      },
    ];
    for (const c of cands) {
      c.K = clockOf(c.F);
      const B = c.B, K = c.K, D = B.blood - c.F[0] + B.trav, sh = K.b0 + D - TODAY.black, uniq = [...new Set(B.touch)];
      const outs = B.out.map(o => `${fmt(o[0])}–${fmt(o[1])}`), rings = [0, 1, 2].map(o => `${fmt(c.F[o] + D)}–${fmt(K.FE[o] + D)}`);
      c.time = `Нити ${[...new Set(outs)].join(', ')} с · рука ${fmt(B.hand[0])}–${fmt(B.hand[1])} с (сейчас ${fmt(TODAY.reach[0])}–${fmt(TODAY.reach[1])}) · укол ${uniq.map(fmt).join(', ')} с · кровь по нити с ${fmt(B.blood)} с, капля бежит ${fmt(B.trav)} с · кольца ${rings.join(', ')} с (сейчас с ${TODAY.F.map(fmt).join(', ')}) · глаз ${fmt(K.TL.lens[0] + D)}–${fmt(K.TL.lens[1] + D)} с (сейчас ${fmt(TODAY.lens[0])}–${fmt(TODAY.lens[1])}) · сцена ${fmt(TODAY.end + sh)} с вместо ${fmt(TODAY.end)} (${sh >= 0 ? '+' : '−'}${fmt(Math.abs(sh))}) · кадр: ${c.frame}.`;
    }

    return {
      cands, expose: { W0, stats, TAU },
      init(T) { T.time = 0; T.heroes = []; T.span = OPT.span; dress(T.c, T.S); T.X = frameState(T); },
      onScale(T) {
        const mk_ = (w, h) => { const cv = document.createElement('canvas'); cv.width = w; cv.height = h; return cv; };
        dress(T.c, T.S);
        T.fx = mk_(T.cv.width, T.cv.height); T.fxg = T.fx.getContext('2d');
        T.fs = mk_(Math.ceil(T.cv.width / 4), Math.ceil(T.cv.height / 4)); T.fsg = T.fs.getContext('2d');
      },
      step(T) {
        if (T.span !== OPT.span) { T.span = OPT.span; T.time = 0; }   // another span starts from the void again
        const X = T.X = frameState(T);
        T.dm = {
          u: X.u, v: X.v, W1: X.W1, span: OPT.span, D: X.D, rk: X.hv.rk, lens: X.lensOn,
          nums: [X.palm[0], X.palm[1], X.core.C[0], X.core.C[1], X.core.R, X.core.A, X.p, X.hv.foot[0], X.hv.foot[1], X.hv.s, X.cam.z, ...X.th.map(q => q.tip[0])],
        };
      },
      back(T) { voidScene(T.c, T.X); },
      screen(T) { const X = T.X; dizzy(T, X); grade(T); dip(T, X); },
      hud(T) {
        const X = T.X, D = X.D, TL = X.K.TL, lastFE = Math.max(...X.K.FE);
        const ev = [[W0, 'пустота: ядро тёмное, глаз мёртв, герой без веса'], ...X.cand.ev, [X.B.blood, 'кровь бежит по нити назад, за ядро'], [TL.F[0] + D, 'нить сматывается в кольцо'],
          [lastFE + .05 + D, 'нити отпускают руку, рука опускается'], [TL.black[0] + D, 'кровь чернеет'], [TL.lens[0] + D, 'глаз открывается: взгляд']].sort((a, b) => a[0] - b[0]);
        let say = ev[0][1]; for (const q of ev) if (X.u >= q[0]) say = q[1];
        return `${fmt(X.u)} с · ${say}${X.p > .05 ? ' · голова кружится' : ''}`;
      },
    };
  },

  // build.js --break <name>: each breaks one rule the check holds
  breaks: {
    handFirst: [['hand: [5.9, 6.75], touch: [6.75, 9.0, 10.9]', 'hand: [4.0, 6.75], touch: [6.75, 9.0, 10.9]']],
    handDown: [['hand: [5.9, 6.75], touch: [6.75, 6.75, 6.75], prick: [[6.75, .34', 'hand: [5.9, 9.75], touch: [6.75, 6.75, 6.75], prick: [[6.75, .34']],
    latchLate: [['out: [[5.0, 5.55], [6.85, 7.2], [6.95, 7.3]]', 'out: [[5.0, 5.55], [6.85, 7.2], [7.6, 7.9]]']],
    seqLate: [['[10.45, 10.9]], hand', '[11.45, 11.9]], hand']],
    offRoute: [['return { p: at(e), tl: [at(e - 2.4 * st), at(e - 1.2 * st)], z: 1, r: r * .8, on: e < ON };', 'return { p: lerp2(X.palm, rt.pts[rt.pts.length - 1], e), tl: [at(e - 2.4 * st), at(e - 1.2 * st)], z: 1, r: r * .8, on: e < ON };']],
    backward: [['return { p: at(e), tl:', 'return { p: at(1 - e), tl:']],
    noRun: [['const ON = .85;', 'const ON = 0;']],
    letGo: [['rel: ss(v, fe + .05, fe + .55)', 'rel: ss(v, fe - 2, fe - 1.5)']],
    keepOut: [['rel: ss(v, fe + .05, fe + .55)', 'rel: 0']],
    eyeRoot: [['for (let j = 0; j <= NA; j++) pts.push(arc(j));', 'for (let j = 0; j <= NA; j++) pts.push(j ? arc(j) : X.lens.slice());']],
    frontRoot: [['const a = th - s * D, q = ringP(o, a); if (Math.sin(a) < 0', 'const a = th + s * D, q = ringP(o, a); if (Math.sin(a) > -9']],
    wrongOrbit: [['run.push({ p: toPg(X, ringP(o, a)), z: Math.sin(a) });', 'run.push({ p: toPg(X, ringP((o + 1) % 3, a)), z: Math.sin(a) });']],
    noWind: [['const w = Math.min(TAU, K.phiO(o, X.v) - K.phiO(o, lead.t));', 'const w = Math.min(.6, K.phiO(o, X.v) - K.phiO(o, lead.t));']],
    rings: [["F: [5.8, 7.7, 9.6],\n        B: { out: [[5.0, 5.6]", "F: [5.8, 6.3, 6.8],\n        B: { out: [[5.0, 5.6]"]],
    eye: [['X.lensOn = ss(v, TL.lens[0], TL.lens[1]);', 'X.lensOn = 1;']],
    nan: [['VR = 27, KV', 'VR = -27, KV']],
  },

  // Every candidate, every loop (one to the eye with ×¼ and a pause inside, then two short): per thread, it comes out from
  // behind the core on its own orbit (never at the eye), then (the hand up) touches, then its blood runs back along it, then
  // its ring takes that blood; no blood on a thread unless it holds; drops on their routes, moving toward the core; each
  // ring's line on its own orbit, through its thread's root and arc, under its blood; rings in turn where they are
  // sequential; every thread let go and the eye opened only after; numbers finite; pick and copy.
  check(K) {
    const X = K.X, FLIP = 1060, N = FLIP + 720, EV = ['out', 'touch', 'blood', 'ring'];
    K.at(300, () => K.seg('speedMode', 1)); K.at(540, () => K.seg('speedMode', 0));
    K.at(600, () => K.seg('playMode', 1)); K.at(660, () => K.seg('playMode', 0));
    K.at(FLIP, () => K.seg('spanMode', 1));
    K.at(N - 60, () => K.click(3, '.pick'));
    let st = '';
    K.at(N - 20, () => { st = K.byId.status.textContent; K.byId.copy.listeners.click[0](); });
    const S = K.tiles.map(() => ({ loops: { beat: 0, eye: 0 }, cur: null, lines: [] }));
    const close = (s, j) => {
      const L = s.cur, id = K.tiles[j].cand.id; if (!L) return;
      if (L.u0 > X.W0 + .1 || L.uMax < L.W1 - .05) return;   // cut by the span's switch or the run's end
      if (!(L.hand > L.first)) K.fail(`${id}: the hand rose at ${L.hand} before any thread came out at ${L.first}`);
      const line = [];
      for (let o = 0; o < 3; o++) {
        const t = EV.map(k => L.ev[o][k]);
        if (L.span === 'eye' && t.some(v => v === undefined)) { K.fail(`${id}: thread ${o + 1} without ${EV.filter(k => L.ev[o][k] === undefined).join(', ')}`); continue; }
        if (L.span === 'beat' && o === 0 && t.some(v => v === undefined)) K.fail(`${id}: the first thread without its whole beat`);
        for (let k = 1; k < 4; k++) if (t[k] !== undefined && !(t[k] > t[k - 1])) K.fail(`${id}: thread ${o + 1} ${EV[k]} at ${t[k]} not after ${EV[k - 1]} at ${t[k - 1]}`);
        if (t[0] !== undefined) line.push(`${o + 1}: ` + EV.map((k, i) => t[i] === undefined ? '-' : t[i].toFixed(2)).join('<'));
      }
      s.loops[L.span]++;
      if (L.span === 'eye' || s.lines.length < 2) s.lines.push(`${L.span} hand ${L.hand === undefined ? '-' : L.hand.toFixed(2)} | ${line.join(' | ')}`);
    };
    let paused = 0, lastT = -1;
    K.run(N, (t, i) => {
      const j = K.tiles.indexOf(t), s = S[j], d = t.dm, id = t.cand.id, Cn = t.cand.K;
      if (d.nums.some(v => !isFinite(v))) K.fail(`${id}: a number not finite, frame ${i}`);
      if (j === 0) { if (!K.OPT.playing) { if (t.time !== lastT && lastT >= 0) K.fail('time ran while paused'); paused++; } lastT = t.time; }
      if (!K.OPT.playing) return;
      if (!s.cur || d.u < s.cur.uMax - .5 || d.span !== s.cur.span) { close(s, j); s.cur = { span: d.span, u0: d.u, uMax: d.u, W1: d.W1, ev: [{}, {}, {}], first: undefined, hand: undefined, lead: [-1, -1, -1] }; }
      const L = s.cur; L.uMax = Math.max(L.uMax, d.u);
      const m = X.stats(t.X), at = `${d.u.toFixed(2)} s`, ALL = Cn.RN[0] + Cn.RN[1] + Cn.RN[2];
      if (d.rk > .05 && L.hand === undefined) L.hand = d.u;
      for (let o = 0; o < 3; o++) {
        const q = m.th[o], E = L.ev[o], seen = (k, c) => { if (c && E[k] === undefined) E[k] = d.u; };
        if (q.f > 0 && L.first === undefined) L.first = d.u;
        seen('out', q.f > 0); seen('blood', m.on[o] > 0); seen('ring', m.ringed[o] > 0);
        if (q.held && E.touch === undefined) { E.touch = d.u; if (d.rk < .85) K.fail(`${id}: thread ${o + 1} touched at ${at} with the hand ${d.rk.toFixed(2)} up`); }
        // the root: behind the core on its own orbit, never at the eye
        if (q.f > 0 && (q.rootOrb > .8 || q.rootSin >= 0 || q.rootR > 5.0 || q.rootLens < 1)) K.fail(`${id}: thread ${o + 1}'s root at ${at}: ${q.rootOrb.toFixed(1)} px off its orbit, depth ${q.rootSin.toFixed(2)}, ${q.rootR.toFixed(2)} units out, ${q.rootLens.toFixed(2)} from the eye`);
        // its blood: only while it holds, on its route, running toward the core, onto its ring only after it ran on the thread
        if (m.on[o] > 0 && !q.held) K.fail(`${id}: blood on thread ${o + 1} at ${at} while it does not hold`);
        if (m.ringed[o] > 0 && E.blood === undefined) K.fail(`${id}: ring ${o + 1} at ${at} before blood ran on its thread`);
        if (m.lead[o] !== null) { if (m.lead[o] < L.lead[o] - 1e-6) K.fail(`${id}: ring ${o + 1}'s first drop ran back toward the palm at ${at}`); L.lead[o] = m.lead[o]; }
        // its ring's line: on its own orbit, through the thread's root and along its arc
        if (q.lineOrb > .8) K.fail(`${id}: ring ${o + 1}'s line ${q.lineOrb.toFixed(1)} px off its orbit at ${at}`);
        if (q.lineRoot > .8 || q.arcOff > .8) K.fail(`${id}: ring ${o + 1}'s line leaves its thread at ${at}: root ${q.lineRoot.toFixed(1)} px, arc ${q.arcOff.toFixed(1)} px`);
      }
      if (m.offRoute > 1.2) K.fail(`${id}: a drop ${m.offRoute.toFixed(1)} px off its thread at ${at}`);
      if (m.ringOff > 0) K.fail(`${id}: a ring's blood ${m.ringOff.toFixed(1)} px off its line at ${at}`);
      if (t.cand.kind === 'seq' || t.cand.kind === 'braid') {
        if (m.ringed[1] > 0 && m.ringed[0] < Cn.RN[0]) K.fail(`${id}: ring 2 began at ${at} with ring 1 at ${m.ringed[0]}/${Cn.RN[0]}`);
        if (m.ringed[2] > 0 && m.ringed[1] < Cn.RN[1]) K.fail(`${id}: ring 3 began at ${at} with ring 2 at ${m.ringed[1]}/${Cn.RN[1]}`);
      }
      const n = m.ringed[0] + m.ringed[1] + m.ringed[2], tails = m.th.some(q => q.out);
      if (d.lens > 0 && (n < ALL || m.black < .2 || tails)) K.fail(`${id}: the eye lit at ${at}, rings ${n}/${ALL}, black ${m.black.toFixed(2)}, a thread out ${tails}`);
    });
    S.forEach((s, j) => close(s, j));
    const doc = K.picked(), fb = K.byId.fallback.value, cand = K.tiles[3].cand;
    S.forEach((s, j) => console.log(`${K.tiles[j].cand.id} (+${K.tiles[j].dm.D.toFixed(2)} s): loops ${s.loops.eye} to the eye, ${s.loops.beat} short | ${s.lines.join(' || ')}`));
    console.log('paused frames', paused, '| pick doc:', JSON.stringify(doc), '| copy text:', fb);
    let ok = true;
    const need = (c, m) => { if (!c) { ok = false; console.log('  RULE', m); } };
    need(K.tiles.length === 4, 'four candidates');
    need(S.every(s => s.loops.eye >= 1 && s.loops.beat >= 2), 'every candidate: a whole loop to the eye and two short ones');
    need(paused > 30, 'the pause held');
    need(this.pick.doc.split('/').length % 2 === 0, 'the db doc path has an even number of segments');
    need(doc && doc.pick === cand.id, 'the pick is in the db doc');
    need(st.includes(`«${cand.name}»`), 'the status names the pick');
    need(fb.includes(`(${cand.id})`) && fb.includes(cand.name), 'the copy text names the pick');
    return ok;
  },
};
