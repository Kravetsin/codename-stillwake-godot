// The shared part of an art-pick page (README.md). build.js puts it in one closure with ICON_SRC, PAGE (the page
// file's data) and the page's own function, which sees everything here by name and returns its candidates and hooks.
const tiles = [];
const dirtyAll = () => tiles.forEach(t => { t.dirty = true; });
const IMG = {};
for (const k in ICON_SRC) { const im = new Image(); im.onload = dirtyAll; im.src = ICON_SRC[k]; IMG[k] = im; }
const ready = im => !!im && im.complete && im.naturalWidth > 0;

// ---------- constants ----------
const LW = PAGE.tile ? PAGE.tile[0] : 1000, LH = PAGE.tile ? PAGE.tile[1] : 540, FLOOR_Y = 470, HOME_X = 700, MAR = 28;
const INK = '#16171b';
const STEEL = ['#8b9199', '#61666e', '#3b3f46'];
const DARK = ['#5b6067', '#42464c', '#2a2d32'];
const ORANGE = ['#a36c3b', '#7a512f', '#4c331f'];
const OLIVE = ['#6b745f', '#4e5546', '#30352c'];
const BRONZE = ['#8d7a5a', '#685a43', '#41382a'];
const RUSTY = ['#6e5646', '#4f3e33', '#30261f'];
const EMBER = [255, 138, 58], SIGN = [168, 238, 255], LAMP = [255, 236, 206];
const TONES = { bag: { light: [140, 196, 255], ground: [7, 16, 28] } };

// ---------- helpers ----------
const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
const c01 = v => v < 0 ? 0 : v > 1 ? 1 : v;
const sm = u => { u = c01(u); return u * u * (3 - 2 * u); };
const lerp = (a, b, k) => a + (b - a) * k;
const css = (c, a = 1) => `rgba(${c[0] | 0},${c[1] | 0},${c[2] | 0},${a < .001 ? 0 : a > 1 ? 1 : +a.toFixed(3)})`;
const pale = (c, k) => [c[0] + (255 - c[0]) * k, c[1] + (255 - c[1]) * k, c[2] + (255 - c[2]) * k];
const lerp2 = (a, b, k) => [a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k];
const rot = (x, y, a) => [x * Math.cos(a) - y * Math.sin(a), x * Math.sin(a) + y * Math.cos(a)];
const add = (a, b) => [a[0] + b[0], a[1] + b[1]];
function rng(seed) { let s = (seed * 2654435761) >>> 0 || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }
function angLerp(a, b, e) { let d = b - a; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI; return a + d * e; }
function bez(a, b, d, s) { const k = 1 - s; return [k * k * a[0] + 2 * k * s * b[0] + s * s * d[0], k * k * a[1] + 2 * k * s * b[1] + s * s * d[1]]; }
// affine [a,b,c,d,e,f]: x' = a x + c y + e, y' = b x + d y + f
const AF = {
  ap: (m, x, y) => [m[0] * x + m[2] * y + m[4], m[1] * x + m[3] * y + m[5]],
  inv: m => { const d = m[0] * m[3] - m[1] * m[2]; return [m[3] / d, -m[1] / d, -m[2] / d, m[0] / d, (m[2] * m[5] - m[3] * m[4]) / d, (m[1] * m[4] - m[0] * m[5]) / d]; },
};
function rr(g, x, y, w, h, r) { g.beginPath(); g.moveTo(x + r, y); g.arcTo(x + w, y, x + w, y + h, r); g.arcTo(x + w, y + h, x, y + h, r); g.arcTo(x, y + h, x, y, r); g.arcTo(x, y, x + w, y, r); g.closePath(); }
function halo(c, x, y, r, rgb, a) {
  if (a <= .005) return;
  c.save(); c.globalCompositeOperation = 'lighter';
  const g = c.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, css(rgb, a)); g.addColorStop(1, css(rgb, 0));
  c.fillStyle = g; c.beginPath(); c.arc(x, y, r, 0, 6.283); c.fill(); c.restore();
}

// ---------- the drone, «Пепел в латах» ----------
function ell(rx, ry, n, cx = 0, cy = 0) { const p = []; for (let i = 0; i < n; i++) { const a = i / n * Math.PI * 2; p.push([cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]); } return p; }
function polyPts(v) { const p = []; for (let i = 0; i < v.length; i++) { const a = v[i], b = v[(i + 1) % v.length]; for (const t of [0, .2, .5, .8]) p.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); } return p; }
function mk(pts, seed, jit = 0.6, closed = true) {
  const r = rng(seed);
  const p = pts.map(q => [q[0] + (r() - .5) * jit, q[1] + (r() - .5) * jit]);
  const w = [], ph = r() * 6.28;
  for (let i = 0; i < p.length; i++) { let v = .8 + .4 * Math.sin(i * .7 + ph) + (r() - .5) * .35; if (r() < .035) v = 0; w.push(Math.max(0, v)); }
  const s = { p, w, closed };
  let cx = 0, cy = 0; for (const q of p) { cx += q[0]; cy += q[1]; } s.cx = cx / p.length; s.cy = cy / p.length;
  const n = p.length, segs = [];
  if (closed) for (let i = 0; i < n; i++) { const a = p[(i - 1 + n) % n], b = p[i], d = p[(i + 1) % n]; segs.push([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, b[0], b[1], (b[0] + d[0]) / 2, (b[1] + d[1]) / 2, w[i]]); }
  else for (let i = 0; i < n - 1; i++) { const a = p[i], b = p[i + 1]; segs.push([a[0], a[1], (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, b[0], b[1], w[i]]); }
  s.segs = segs;
  return s;
}
function trace(c, s) {
  const p = s.p, n = p.length; c.beginPath();
  if (!s.closed) { c.moveTo(p[0][0], p[0][1]); for (let i = 1; i < n; i++) c.lineTo(p[i][0], p[i][1]); return; }
  const g = s.segs; c.moveTo(g[0][0], g[0][1]);
  for (const q of g) c.quadraticCurveTo(q[2], q[3], q[4], q[5]);
  c.closePath();
}
function paint(c, s, ang, col, k1 = 1.6, k2 = 5) {
  c.save(); c.rotate(ang); trace(c, s); c.clip(); c.rotate(-ang);
  c.fillStyle = col[2]; c.fillRect(-90, -90, 180, 180);
  c.save(); c.translate(0, -k1); c.rotate(ang); c.fillStyle = col[1]; trace(c, s); c.fill(); c.restore();
  c.save(); c.translate(0, -k2); c.rotate(ang); c.fillStyle = col[0]; trace(c, s); c.fill(); c.restore();
  c.restore();
}
function ink(c, s, ang, lw, col = INK) {
  c.save(); c.rotate(ang); c.lineCap = 'round'; c.lineJoin = 'round'; c.strokeStyle = col;
  for (const g of s.segs) { if (g[6] <= .05) continue; c.lineWidth = lw * g[6]; c.beginPath(); c.moveTo(g[0], g[1]); c.quadraticCurveTo(g[2], g[3], g[4], g[5]); c.stroke(); }
  c.restore();
}
function blobAt(r, x, y, sz) {
  const n = 9, p = [];
  for (let i = 0; i < n; i++) { const a = i / n * 6.283 + r() * .4, rr_ = sz * (.55 + .6 * r()); p.push([x + Math.cos(a) * rr_, y + Math.sin(a) * rr_ * .8]); }
  return { s: mk(p, (r() * 1e6) | 0, .3), x, y };
}
function rust(c, b) { c.fillStyle = 'rgba(118,66,36,.85)'; trace(c, b.s); c.fill(); }
function lens(c, x, y, a, rgb, glow, sc = 1) {
  c.save(); c.translate(x, y); c.rotate(a); c.scale(sc, sc);
  c.fillStyle = DARK[2]; c.beginPath(); c.ellipse(0, 0, 3.1, 4.4, 0, 0, 6.283); c.fill();
  c.strokeStyle = INK; c.lineWidth = 1.3; c.stroke();
  c.fillStyle = css(pale(rgb, .3)); c.beginPath(); c.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); c.fill();
  c.fillStyle = css(pale(rgb, .8)); c.beginPath(); c.ellipse(.8, 0, .9, 1.7, 0, 0, 6.283); c.fill();
  c.restore();
  halo(c, x, y, 14 * sc, rgb, .55 * glow);
}
function shardShape(r, sz) {
  const n = 4 + ((r() * 3) | 0), v = [];
  for (let i = 0; i < n; i++) { const a = (i + (r() - .5) * .5) / n * 6.283, rr_ = sz * (.6 + r() * .45); v.push([Math.cos(a) * rr_ * 1.3, Math.sin(a) * rr_ * .8]); }
  const s = mk(polyPts(v), (r() * 1e6) | 0, .2);
  s.len = Math.max(...s.p.map(q => Math.abs(q[0])));
  return s;
}
function flipY(sh, k) { return { p: sh.p, segs: sh.segs.map(g => [g[0], g[1] * k, g[2], g[3] * k, g[4], g[5] * k, g[6]]), closed: true, cx: sh.cx, cy: sh.cy * k }; }
function rustsOf(sh) { return !sh.rb ? [] : Array.isArray(sh.rb) ? sh.rb : [sh.rb]; }
function plate(c, q, sh, col, k) {
  const kk = Math.abs(k) < .08 ? (k < 0 ? -.08 : .08) : k, s = flipY(sh, kk);
  c.save(); c.translate(q.x, q.y);
  paint(c, s, q.a, kk > 0 ? col : RUSTY, .5, 1.6);
  if (kk > 0 && sh.rb) { c.save(); c.rotate(q.a); trace(c, s); c.clip(); for (const b of rustsOf(sh)) rust(c, b); c.restore(); }
  ink(c, s, q.a, 1.3);
  c.restore();
}
const DS = .45;    // the ash drawing's units to screen px
const DSC = 1.4;   // drawn at about 6 world px beside the hero
const DR = (() => {
  const r = rng(501), pal = [STEEL, ORANGE, OLIVE, BRONZE], sh = [];
  for (let i = 0; i < 4; i++) { const s = shardShape(r, 5.6 + r() * 1.2); s.col = pal[i]; s.rb = r() < .6 ? blobAt(r, (r() - .5) * 4, (r() - .5) * 2, 1.5) : null; s.ph = r() * 6.283; s.rr = r(); sh.push(s); }
  const r2 = rng(530), P = [];
  for (let i = 0; i < 170; i++) P.push({ ph: r2() * 6.283, r1: r2(), sz: 1.4 + r2() * 1.2, hot: r2() < 1 / 13, tn: r2() });
  return { sh, P, core: mk(ell(5.4, 5.4, 22), 111, .3), crust: blobAt(rng(120), -2, 2.5, 1.5) };
})();
// one idle plate, in the drone's units from its middle: x, y, turn, and its flip (-1..1)
function plateIdle(i, t) { const s = DR.sh[i], th = i / 4 * 6.283 + t * .35, id = rot(Math.cos(th) * 16, Math.sin(th) * 16 * .45, -.2); return [id[0], id[1] + Math.sin(t * 1.2 + i) * 1.2, th * 1.3 + i, Math.cos(t * (.3 + .4 * s.rr) + s.ph * 3), Math.sin(th)]; }
// the whole drone, as the game draws it beside the hero; fan spreads its plates, hideP leaves them out (a page drew them away)
function drawDrone(c, x, y, A, fan, t, sc = 1, hideP = false) {
  c.save(); c.translate(x, y); c.scale(DS * sc, DS * sc);
  const Q = DR.sh.map((s, i) => {
    const w = i / 3 * 2 - 1, I = plateIdle(i, t), ph = A - .15 + w * .8, H = [Math.cos(ph) * 9, Math.sin(ph) * 9, ph, 1];
    const e = sm(fan * 1.25 - i * .08), tum = I[3];
    const k = e <= 0 ? tum : e >= 1 ? 1 : Math.cos(e * 6.283) * (tum * (1 - e) + e);
    return { x: I[0] + (H[0] - I[0]) * e, y: I[1] + (H[1] - I[1]) * e, a: angLerp(I[2], H[2], e), z: e > .5 ? 1 : I[4], k, sh: s, g: e };
  });
  const M = DR.P.map((p, i) => {
    const o = i % 3, tilt = -.3 + o * 1.05, dir = o % 2 ? -1 : 1, th = p.ph + t * (.8 + .25 * o) * dir, rx = 10 + o * 2.2 + (p.r1 - .5) * 1.6;
    const q = rot(Math.cos(th) * rx, Math.sin(th) * rx * .3, tilt); return { x: q[0], y: q[1], z: Math.sin(th), p };
  });
  const motes = front => {
    for (const m of M) { if ((m.z >= 0) !== front) continue; const s = m.p.sz; c.fillStyle = '#1a1b1f'; c.fillRect(m.x - s / 2 - .4, m.y - s / 2 - .4, s + .8, s + .8); c.fillStyle = m.p.tn < .5 ? '#4c4f55' : '#606369'; c.fillRect(m.x - s / 2, m.y - s / 2, s, s); }
    c.save(); c.globalCompositeOperation = 'lighter';
    for (const m of M) { if ((m.z >= 0) !== front || !m.p.hot) continue; const h = .45 + .4 * Math.sin(t * 11 + m.p.ph * 7), s = m.p.sz; c.fillStyle = css(pale(EMBER, .25 * h), .95 * h); c.fillRect(m.x - s / 2, m.y - s / 2, s, s); }
    c.restore();
  };
  const one = q => { if (hideP) return; plate(c, q, q.sh, q.sh.col, q.k); if (q.g > .05) halo(c, q.x, q.y, 7, SIGN, .45 * q.g * fan); };
  motes(false);
  for (const q of Q) if (q.z < 0) one(q);
  paint(c, DR.core, A, DARK, .8, 2.6);
  c.save(); c.rotate(A); rust(c, DR.crust); c.restore();
  ink(c, DR.core, A, 1.7);
  lens(c, Math.cos(A) * 2.2, Math.sin(A) * 2.2, A, SIGN, .35 + .65 * fan, .82);
  motes(true);
  for (const q of Q) if (q.z >= 0) one(q);
  c.restore();
}
// where a hero's drone lens is on screen
const lensAt = d => [d.p[0] + Math.cos(d.ang) * 2.2 * DS * DSC, d.p[1] + Math.sin(d.ang) * 2.2 * DS * DSC];

// ---------- the hero, close to the game's rig (Rig.cs, Pose.cs), in world px drawn ×4 ----------
const Z = 4;   // screen px per world px: the game's zoom
const RIG = (() => { const W = 15.2, H = 42; return { W, H, Hip: H * .42, Shoulder: H * .61, Neck: H * .71, Arm: [H * .145, H * .145, W * .27], Leg: [H * .42 * .5, H * .42 * .5, W * .34], HipApart: W * .07 }; })();
const HEAD = { Up: .6, Down: .5 };   // Pose.Head: how far the head turns, up and down
const SHEET = { Length: RIG.W * .85, Ahead: .45, PerLook: .6 }, HOLD = { Arm: 1.05, PerLook: .8, Reach: .8, FarHand: .35 };
// Pose.HeadTurn: toward a point dx ahead of the neck the way the hero faces and dy below it; positive looks down
const headTurn = (dx, dy) => dx === 0 && dy === 0 ? 0 : clamp(Math.atan2(dy, Math.abs(dx)), -HEAD.Up, HEAD.Down);
const sheetTurn = look => -SHEET.Ahead + look * SHEET.PerLook;
function planHand(look) { const a = HOLD.Arm - look * HOLD.PerLook, o = HOLD.Reach * (RIG.Arm[0] + RIG.Arm[1]); return [Math.sin(a) * o, Math.cos(a) * o]; }
// Rig.Reach: the bend putting a limb's end at t from its joint; 0 hangs straight down, forward positive
function reach(t, seg, bends) {
  const a = seg[0], b = seg[1], far = Math.hypot(t[0], t[1]), d = Math.min(a + b - 1e-3, Math.max(Math.abs(a - b) + 1e-3, far));
  const toward = Math.atan2(t[0], t[1]), atJ = Math.acos(clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1)), atB = Math.acos(clamp((a * a + b * b - d * d) / (2 * a * b), -1, 1));
  return [toward - bends * atJ, bends * (Math.PI - atB)];
}
const limbPts = (o, bend, seg) => { const e = [o[0] + Math.sin(bend[0]) * seg[0], o[1] + Math.cos(bend[0]) * seg[0]], t = bend[0] + bend[1]; return [o, e, [e[0] + Math.sin(t) * seg[1], e[1] + Math.cos(t) * seg[1]]]; };
const mixBend = (a, b, k) => [angLerp(a[0], b[0], k), angLerp(a[1], b[1], k)];
const SUITS = { me: ['#938d7d', '#6f6b5f', '#4b4941', '#5d5a50', '#777366'], friend: ['#7f8a86', '#5f6966', '#3f4644', '#505957', '#68726f'] };
const NECK = () => [.3, -RIG.Neck], HELM = [1, -RIG.Neck - 6.2], EYE = [4.6, -RIG.Neck - 6.4], HELM_R = 6.4;
const rotAbout = (p, o, a) => { const q = rot(p[0] - o[0], p[1] - o[1], a); return [o[0] + q[0], o[1] + q[1]]; };
const toS = (h, p) => [h.x + h.face * Z * p[0], h.y + Z * p[1]];
// the kneel a page may bend the rig into (h.kn of the way): thigh from straight down and knee bend, front leg then back; the
// front thigh level and its shin upright, the back knee on the ground and its toes tucked under
const KNEEL = { F: [1.5, 1.5], B: [-.05, 1.7], Toe: [1.2, 2] };
// the pose: legs and arms from the gait; h.e of the way to holding the planner sheet at look h.hl (Poses.Plan). A page's own
// pose on the same joints: h.kn of the way to the kneel, the body h.lean forward about the hip, the front hand h.rk of the way
// to h.rp (world px from the feet); all three 0 unless a page sets them
function poseOf(h) {
  const kq = h.kn || 0, lean = h.lean || 0, gb = h.air ? 0 : Math.abs(Math.sin(h.ph)) * .7 * h.amp;
  const angs = s => {
    const q = h.ph + (s > 0 ? 0 : Math.PI);
    let th, kn;
    if (h.air) { th = s > 0 ? .9 : -.3; kn = s > 0 ? 1.1 : .5; } else { th = Math.sin(q) * .6 * h.amp; kn = Math.max(0, -Math.cos(q)) * 1.1 * h.amp + .06; }
    if (kq > 0) { const K = s > 0 ? KNEEL.F : KNEEL.B; th = lerp(th, K[0], kq); kn = lerp(kn, K[1], kq); }
    return [th, kn];
  };
  const LF = angs(1), LB = angs(-1);
  // kneeling lowers the hip until the front foot stands on the ground
  const drop = kq > 0 ? Math.max(0, RIG.Hip - RIG.Leg[0] * Math.cos(LF[0]) - RIG.Leg[1] * Math.cos(LF[0] - LF[1])) * Math.min(1, kq * 10) : 0;
  const bob = gb - drop, up = p => [p[0], p[1] - bob];
  const P = { bob, lean };
  P.hip = up([0, -RIG.Hip]);
  const leaned = p => lean ? rotAbout(p, P.hip, lean) : p;
  P.neck0 = up(NECK()); P.neck = leaned(P.neck0); P.sh = leaned(up([.4, -RIG.Shoulder]));
  const leg = (s, [th, kn]) => {
    const hip = [s * RIG.HipApart, -RIG.Hip];
    const k = [hip[0] + Math.sin(th) * RIG.Leg[0], hip[1] + Math.cos(th) * RIG.Leg[0]], f = [k[0] + Math.sin(th - kn) * RIG.Leg[1], k[1] + Math.cos(th - kn) * RIG.Leg[1]];
    const toe = s < 0 && kq > 0 ? lerp2([2.6, 0], KNEEL.Toe, kq) : [2.6, 0];
    return [up(hip), up(k), up(f), up([f[0] + toe[0], f[1] + toe[1]])];
  };
  P.legF = leg(1, LF); P.legB = leg(-1, LB);
  let fa, ba;
  if (h.air) { fa = [1.3, .7]; ba = [-.6, .9]; }
  else { const s = Math.sin(h.ph) * h.amp; fa = [.12 - .7 * s, .3 + .5 * h.amp]; ba = [-.06 + .7 * s, .3 + .5 * h.amp]; }
  if (h.e > 0) {   // the front hand at PlanHand, the far hand FarHand along the sheet
    const hand = planHand(h.hl), turn = sheetTurn(h.hl), al = HOLD.FarHand * SHEET.Length, far = [hand[0] + Math.cos(turn) * al, hand[1] + Math.sin(turn) * al];
    fa = mixBend(fa, reach(hand, RIG.Arm, 1), h.e); ba = mixBend(ba, reach(far, RIG.Arm, 1), h.e);
  }
  if (kq > 0) { const kn = P.legF[1]; ba = mixBend(ba, reach([kn[0] - P.sh[0], kn[1] - P.sh[1]], RIG.Arm, 1), kq); }   // the far hand on the front knee
  if (h.rk > 0 && h.rp) fa = mixBend(fa, reach([h.rp[0] - P.sh[0], h.rp[1] - P.sh[1]], RIG.Arm, 1), h.rk);
  P.armF = limbPts(P.sh, fa, RIG.Arm); P.armB = limbPts(P.sh, ba, RIG.Arm);
  P.helm = leaned(rotAbout(up(HELM), P.neck0, h.look)); P.eye = leaned(rotAbout(up(EYE), P.neck0, h.look));
  return P;
}
function limb(c, pts, w, col) {
  c.lineCap = 'round'; c.lineJoin = 'round'; c.beginPath(); c.moveTo(pts[0][0], pts[0][1]);
  for (let i = 1; i < pts.length; i++) c.lineTo(pts[i][0], pts[i][1]);
  c.strokeStyle = INK; c.lineWidth = w + .9; c.stroke(); c.strokeStyle = col; c.lineWidth = w; c.stroke();
}
function fist(c, p, col) { c.beginPath(); c.arc(p[0], p[1], 1.45, 0, 6.283); c.fillStyle = col; c.fill(); c.strokeStyle = INK; c.lineWidth = .45; c.stroke(); }
function drawHero(c, h) {
  const P = h.P, S = SUITS[h.suit];
  c.save(); c.translate(h.x, h.y); c.scale(h.face * Z, Z);
  limb(c, P.armB.slice(0, 3), RIG.Arm[2] * .9, S[3]); fist(c, P.armB[2], S[3]);
  limb(c, P.legB.slice(0, 3), RIG.Leg[2] * .9, S[3]); limb(c, P.legB.slice(2), 2.2, '#3a3833');
  const b = P.bob;
  // the body and the helmet lean about the hip (P.lean, 0 unless a page bends it)
  const leaned = () => { c.save(); if (P.lean) { c.translate(P.hip[0], P.hip[1]); c.rotate(P.lean); c.translate(-P.hip[0], -P.hip[1]); } };
  leaned();
  rr(c, -6.6, -RIG.Shoulder - 1.8 - b, 3.2, 9.5, .8); c.fillStyle = '#58564e'; c.fill(); c.strokeStyle = INK; c.lineWidth = .45; c.stroke();
  rr(c, -4, -RIG.Shoulder - 2.6 - b, 8.4, RIG.Shoulder - RIG.Hip + 4.2, 2.2); c.fillStyle = S[1]; c.fill();
  c.fillStyle = S[0]; c.fillRect(-2.6, -RIG.Shoulder - 1.6 - b, 5.6, 1.4); c.fillStyle = S[2]; c.fillRect(-3.6, -RIG.Hip - .4 - b, 7.6, 1.3);
  rr(c, -4, -RIG.Shoulder - 2.6 - b, 8.4, RIG.Shoulder - RIG.Hip + 4.2, 2.2); c.strokeStyle = INK; c.lineWidth = .5; c.stroke();
  c.restore();
  limb(c, P.legF.slice(0, 3), RIG.Leg[2] * .9, S[4]); limb(c, P.legF.slice(2), 2.2, '#45423b');
  // the helmet turns about the neck by the look, down positive
  leaned();
  c.translate(P.neck0[0], P.neck0[1]); c.rotate(h.look); c.translate(-P.neck0[0], -P.neck0[1] - b);
  c.beginPath(); c.arc(HELM[0], HELM[1], HELM_R, 0, 6.283); c.fillStyle = '#a29c8c'; c.fill(); c.strokeStyle = INK; c.lineWidth = .5; c.stroke();
  c.beginPath(); c.ellipse(HELM[0] + 3, HELM[1] - .1, 3.5, 3.1, 0, 0, 6.283); c.fillStyle = '#18212e'; c.fill();
  c.beginPath(); c.arc(HELM[0] + 3.6, HELM[1] - .9, 1.9, 3.6, 5.2); c.strokeStyle = 'rgba(170,205,235,.55)'; c.lineWidth = .35; c.stroke();
  c.restore();
  limb(c, P.armF.slice(0, 3), RIG.Arm[2], S[4]); fist(c, P.armF[2], S[4]);
  c.restore();
}
// a loop of standing, running and jumping, kept within 150 px of its middle
const HERO = (() => {
  const L = 9, dt = 1 / 240, n = Math.round(L / dt);
  const segs = [[0, 1.2, 0], [1.2, 2.6, 1], [2.6, 3.4, 0], [3.4, 6.2, -1], [6.2, 7.0, 0], [7.0, 8.4, 1], [8.4, 9.01, 0]];
  const jumps = [0.4, 1.9, 4.4, 5.5, 7.6], SPEED = 190, ACC = 1500, G = 1700, VJ = 400;
  const X = new Float32Array(n + 1), V = new Float32Array(n + 1);
  let x = 0, v = 0;
  for (let i = 0; i <= n; i++) {
    const t = i * dt, sg = segs.find(s => t >= s[0] && t < s[1]) || segs[0];
    v += clamp(sg[2] * SPEED - v, -ACC * dt, ACC * dt); x += v * dt; X[i] = x; V[i] = v;
  }
  const drift = X[n];
  let mn = 1e9, mx = -1e9;
  for (let i = 0; i <= n; i++) { X[i] -= drift * i / n; mn = Math.min(mn, X[i]); mx = Math.max(mx, X[i]); }
  const mid = (mn + mx) / 2, k = Math.min(1, 300 / (mx - mn));
  return {
    L,
    at(t) {
      t = ((t % L) + L) % L;
      const i = Math.min(n, Math.floor(t / dt));
      let y = 0, vy = 0, air = false;
      for (const j of jumps) { const u = t - j; if (u >= 0 && u < 2 * VJ / G) { y = -(VJ * u - G * u * u / 2); vy = -(VJ - G * u); air = true; } }
      return { x: (X[i] - mid) * k, v: (V[i] - drift / L) * k, y, vy, air };
    },
  };
})();
// the drone's lamp on the hero
function lampOn(c, dx, dy, fx, fy, face, L, sc) {
  if (L > .01) {
    halo(c, dx + 22 * face * sc, dy + 46 * sc, 150 * sc, LAMP, .14 * L);
    c.save(); c.globalCompositeOperation = 'lighter'; c.translate(fx + 26 * face * sc, fy); c.scale(1, .12);
    const g = c.createRadialGradient(0, 0, 0, 0, 0, 95 * sc); g.addColorStop(0, css(LAMP, .16 * L)); g.addColorStop(1, css(LAMP, 0));
    c.fillStyle = g; c.beginPath(); c.arc(0, 0, 95 * sc, 0, 6.283); c.fill(); c.restore();
  }
}

// ---------- the bag: rows 2–4, 35 slots, sort and stash keys, trash ----------
const BAG = (() => {
  const slots = [];
  for (let i = 0; i < 35; i++) slots.push({ i, x: 10 + (i % 9) * 48, y: 10 + Math.floor(i / 9) * 48, w: 44, h: 44 });
  slots.push({ i: 'trash', trash: true, x: 10 + 8 * 48, y: 10 + 3 * 48, w: 44, h: 44 });
  return { x: 24, y: 36, w: 482, h: 208, slots, keys: [{ k: 'sort', x: 442, y: 10, w: 30, h: 30 }, { k: 'stash', x: 442, y: 44, w: 30, h: 30 }] };
})();
const W = BAG.w, H = BAG.h, L0 = TONES.bag.light;
const SLOTS = BAG.slots.filter(s => !s.trash), TRASH = BAG.slots.find(s => s.trash);
// slot, item, count: build.js embeds each item's picture from game/art/hd/items
const BAG_ITEMS = [[0, 'scrap', 37], [1, 'ironOre', 12], [2, 'copper', 8], [4, 'battery', 1], [9, 'circuit', 5], [10, 'fruit', 6], [13, 'porridge', 2], [19, 'canister', 3], [27, 'crystal', 4]];
function startItems() { const bag = {}; for (const [i, k, n] of BAG_ITEMS) bag[i] = [k, n]; return { bag }; }
function glyph(g, k, x, y, col) {
  g.save(); g.strokeStyle = col; g.fillStyle = col; g.lineWidth = 1.6; g.lineCap = 'round'; g.lineJoin = 'round'; g.beginPath();
  if (k === 'sort') {
    g.moveTo(x + 7, y + 9); g.lineTo(x + 16, y + 9); g.moveTo(x + 7, y + 15); g.lineTo(x + 13.5, y + 15); g.moveTo(x + 7, y + 21); g.lineTo(x + 11, y + 21);
    g.moveTo(x + 21.5, y + 8); g.lineTo(x + 21.5, y + 22); g.moveTo(x + 18.5, y + 19); g.lineTo(x + 21.5, y + 22.5); g.lineTo(x + 24.5, y + 19);
  } else if (k === 'stash') {
    g.rect(x + 15, y + 9, 9, 12); g.moveTo(x + 6, y + 15); g.lineTo(x + 13, y + 15); g.moveTo(x + 10, y + 12); g.lineTo(x + 13, y + 15); g.lineTo(x + 10, y + 18);
  } else { g.moveTo(x + 8, y + 22); g.lineTo(x + 22, y + 8); }
  g.stroke(); g.restore();
}
function binGlyph(g, cx, cy, col) {
  g.save(); g.strokeStyle = col; g.lineWidth = 1.4; g.globalAlpha *= .8;
  g.beginPath(); g.arc(cx, cy, 15, 0, 6.283); g.stroke();
  g.beginPath(); g.moveTo(cx - 6, cy - 5); g.lineTo(cx - 5, cy + 8); g.lineTo(cx + 5, cy + 8); g.lineTo(cx + 6, cy - 5);
  g.moveTo(cx - 8, cy - 6.5); g.lineTo(cx + 8, cy - 6.5); g.moveTo(cx - 2, cy - 9); g.lineTo(cx + 2, cy - 9);
  g.moveTo(cx - 2, cy - 2); g.lineTo(cx - 2, cy + 5); g.moveTo(cx + 2, cy - 2); g.lineTo(cx + 2, cy + 5); g.stroke(); g.restore();
}
function paintBoxes(g, Wd, st) {
  for (const s of Wd.slots) {
    rr(g, s.x + .5, s.y + .5, s.w - 1, s.h - 1, st.r);
    if (st.fill) { g.fillStyle = st.fill; g.fill(); }
    if (st.edge) { g.strokeStyle = st.edge; g.lineWidth = st.ew || 1; g.stroke(); }
    if (s.trash) binGlyph(g, s.x + s.w / 2, s.y + s.h / 2, st.glyph);
  }
  for (const k of Wd.keys) {
    rr(g, k.x + .5, k.y + .5, k.w - 1, k.h - 1, 2);
    if (st.keyFill) { g.fillStyle = st.keyFill; g.fill(); }
    g.strokeStyle = st.keyEdge; g.lineWidth = 1; g.stroke(); glyph(g, k.k, k.x, k.y, st.glyph);
  }
}
// the window's static pictures, cached per tile and scale
const BOX = { fill: 'rgba(20,32,50,.78)', edge: css(L0, .36), r: 2, keyFill: 'rgba(10,16,26,.9)', keyEdge: css(L0, .6), glyph: css(pale(L0, .5)) };
const PAINT = {
  g: g => {
    const r = rng(77);
    g.fillStyle = css(TONES.bag.ground, .87); g.fillRect(0, 0, W, H);
    for (let i = 0; i < 800; i++) { const s = .6 + r() * 1.5; g.fillStyle = `rgba(0,0,0,${(.1 + r() * .22).toFixed(3)})`; g.fillRect(r() * W, r() * H, s, s); }
    for (let i = 0; i < 160; i++) { g.fillStyle = css(pale(L0, .2), .04 + r() * .07); g.fillRect(r() * W, r() * H, 1, 1); }
  },
  e: g => { rr(g, .5, .5, W - 1, H - 1, 3); g.strokeStyle = css(L0, .1); g.lineWidth = 4; g.stroke(); g.strokeStyle = css(L0, .5); g.lineWidth = 1; g.stroke(); },
  b: g => paintBoxes(g, { slots: SLOTS, keys: [] }, BOX),
  k: g => paintBoxes(g, { slots: [TRASH], keys: BAG.keys }, BOX),
};
function statOf(T, id) {
  if (T.cache[id]) return T.cache[id];
  const cv = document.createElement('canvas');
  cv.width = Math.ceil((W + 2 * MAR) * T.S); cv.height = Math.ceil((H + 2 * MAR) * T.S);
  const g = cv.getContext('2d'); g.setTransform(T.S, 0, 0, T.S, MAR * T.S, MAR * T.S);
  PAINT[id](g);
  return (T.cache[id] = cv);
}
function drawItem(T, c, s) {
  const it = T.items.bag[s.i]; if (!it) return;
  const im = IMG[it[0]];
  if (ready(im)) c.drawImage(im, s.x + 4, s.y + 4, 36, 36); else { c.fillStyle = '#6b6f78'; c.fillRect(s.x + 12, s.y + 12, 20, 20); }
  if (it[1] > 1) { c.fillStyle = 'rgba(0,0,0,.85)'; c.fillText(String(it[1]), s.x + s.w - 3, s.y + s.h - 3); c.fillStyle = '#f4f4f0'; c.fillText(String(it[1]), s.x + s.w - 4, s.y + s.h - 4); }
}
// the bag, drawn at once while open: no animation of its own
function drawWindow(T) {
  if (!T.win.on || OPT.hideWindow) return;
  const c = T.c, S = T.S, M = T.M;
  c.save(); c.setTransform(S * M[0], S * M[1], S * M[2], S * M[3], S * M[4], S * M[5]); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
  for (const id of ['g', 'e', 'k', 'b']) { const cv = statOf(T, id); c.drawImage(cv, -MAR, -MAR, cv.width / S, cv.height / S); }
  c.font = '600 11px ui-monospace,Consolas,monospace'; c.textAlign = 'right'; c.textBaseline = 'alphabetic';
  for (const s of SLOTS) drawItem(T, c, s);
  if (T.hover) { const s = T.hover.s; rr(c, s.x + .5, s.y + .5, s.w - 1, s.h - 1, 2); c.fillStyle = css(L0, .1); c.fill(); c.strokeStyle = css(pale(L0, .4), .95); c.lineWidth = 1.3; c.stroke(); }
  c.restore();
}

// ---------- the options every page has: the open schedule, the hero, speed, pause ----------
const RM = !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
// a page with window: false has no bag: no open schedule, ghost or Tab key, and its heroes stand where its step puts them
const WIN = PAGE.window !== false;
const OPT = { mode: 'sched', stand: !WIN, slow: false, playing: !RM, force: null, hideWindow: false };
function schedule() { return OPT.mode === 'often' ? { L: 1.4, wins: [[0, .7]] } : { L: 4.6, wins: [[.5, 3.3]] }; }
// whether the window is open lag seconds behind this tile's schedule (a friend's window)
function scheduledOn(T, lag = 0) {
  if (OPT.force !== null) return OPT.force;
  const sc = schedule(), tl = ((T.tl - lag) % sc.L + sc.L) % sc.L;
  return OPT.mode === 'always' || sc.wins.some(w => tl >= w[0] && tl < w[1]);
}

// ---------- a tile's world ----------
const STARS = (() => { const r = rng(5), s = []; for (let i = 0; i < 230; i++) s.push([r() * 1400, r() * 450, r() < .1 ? 1.6 : r() < .4 ? 1.1 : .7, .25 + r() * .6]); return s; })();
const REST = [930, 330];
function newHero(suit, x, seed) {
  const h = { suit, x, y: FLOOR_Y, face: 1, ph: 0, amp: 0, air: false, look: 0, hl: 0, e: 0, seed, P: null };
  h.d = { p: [0, 0], v: [0, 0], ang: Math.atan2(46, 22) };
  h.P = poseOf(h);
  return h;
}
function newState(T) {
  T.time = 0; T.tl = OPT.playing ? schedule().L - .25 : 2; T.ht = OPT.playing ? 0 : .2;
  T.hx = 0; T.hv = [0, 0]; T.hy = 0;
  T.me = newHero('me', HOME_X, 1.3); T.heroes = [T.me];
  T.vs = [0, 0]; T.lo = [0, 0]; T.lv = [0, 0]; T.calm = 1;
  T.win = { on: false };
  T.M = [1, 0, 0, 1, BAG.x, BAG.y];
  T.items = startItems(); T.held = null; T.hover = null; T.ptrIn = false;
  T.ghost = { x: REST[0], y: REST[1], step: 0, on: false, from: 0, to: 3 }; T.real = false; T.ptr = [0, 0]; T.tab = -10; T.openT = -10; T.clickAt = -10; T.clickAfter = 0;
}
function returnHeld(T) {
  const h = T.held; if (!h) return;
  const arr = T.items.bag;
  if (!arr[h.from.i]) arr[h.from.i] = h.it;
  else { const free = SLOTS.find(s => !arr[s.i]); if (free) arr[free.i] = h.it; }
  T.held = null;
}
function clickAt(T) {
  const h = T.hover, arr = T.items.bag;
  if (T.held) {
    if (h && !h.s.trash) {
      const cur = arr[h.i];
      if (cur && cur[0] === T.held.it[0]) arr[h.i] = [cur[0], cur[1] + T.held.it[1]];
      else { arr[h.i] = T.held.it; if (cur) { if (!arr[T.held.from.i]) arr[T.held.from.i] = cur; else { const free = SLOTS.find(s => !arr[s.i]); if (free) arr[free.i] = cur; } } }
      T.held = null;
    } else returnHeld(T);
  } else if (h && !h.s.trash && arr[h.i]) { T.held = { it: arr[h.i], from: { i: h.i } }; arr[h.i] = null; }
}
const slotCenter = (T, i) => { const s = SLOTS[i]; return AF.ap(T.M, s.x + s.w / 2, s.y + s.h / 2); };
const keyCenter = (T, k) => { const s = BAG.keys.find(q => q.k === k); return AF.ap(T.M, s.x + s.w / 2, s.y + s.h / 2); };
const pointer = T => T.real ? T.ptr : (T.ghost.on ? [T.ghost.x, T.ghost.y] : null);

function turnHead(h, tg, dt) {
  tg = clamp(tg, -HEAD.Up, HEAD.Down);
  if (!OPT.playing) { h.look = tg; return; }
  h.look += (tg - h.look) * Math.min(1, dt * 14); if (Math.abs(tg - h.look) < 1e-3) h.look = tg;
}
function stepHero(T, dt) {
  const me = T.me;
  if (OPT.stand) { T.hv = [0, 0]; T.hy = 0; me.air = false; }
  else { T.ht = (T.ht + dt) % HERO.L; const r = HERO.at(T.ht); T.hx = r.x; T.hv = [r.v, r.vy]; T.hy = r.y; me.air = r.air; }
  me.x = HOME_X + T.hx; me.y = FLOOR_Y + T.hy;
  const P = pointer(T);
  // Pose.FaceOf: the hero faces the pointer, half a cell either side of the neck kept
  if (P) { const nx = toS(me, NECK())[0]; if (Math.abs(P[0] - nx) > Z * 4) me.face = Math.sign(P[0] - nx); }
  else if (Math.abs(T.hv[0]) > 8) me.face = Math.sign(T.hv[0]);
  me.ph += T.hv[0] * me.face * dt / (Z * 5.5); me.amp = Math.min(1, Math.abs(T.hv[0]) / 190);
  if (P) { const n = toS(me, NECK()); turnHead(me, headTurn((P[0] - n[0]) * me.face, P[1] - n[1]), dt); }
}
// the window opens and closes on the schedule; a real pointer in it near the end holds it open
function stepTimeline(T, dt) {
  const sc = schedule(), cur = sc.wins.find(w => T.tl >= w[0] && T.tl < w[1]);
  const hold = OPT.mode === 'sched' && T.real && T.ptrIn && cur && T.tl > cur[1] - .4;
  if (!hold) T.tl = (T.tl + dt) % sc.L;
  const on = scheduledOn(T);
  if (on !== T.win.on) { T.win.on = on; if (on) T.openT = T.time; if (T.time > .05) T.tab = T.time; }
  if (T.held && !on) returnHeld(T);
}
function stepDrone(T, h, dt) {
  const d = h.d, b = toS(h, [-9, -44]), tg = [b[0], b[1] + Math.sin(T.time * 1.7 + h.seed) * 1.5], k = 45, cd = 2 * Math.sqrt(k) * .85;
  if (!OPT.playing || T.time < .02) { d.p = tg.slice(); d.v = [0, 0]; }
  else for (let j = 0; j < 2; j++) { d.v[j] += ((tg[j] - d.p[j]) * k - d.v[j] * cd) * dt; d.p[j] += d.v[j] * dt; }
  const aim = hook('aim', T, h);
  const want = aim ? Math.atan2(aim[1] - d.p[1], aim[0] - d.p[0]) : Math.atan2(46, 22 * h.face);
  d.ang = angLerp(d.ang, want, OPT.playing ? Math.min(1, dt * 14) : 1);
}
// the ghost takes a stack, puts it down, then wanders over the bag: the head follows it
function stepGhost(T, dt) {
  const g = T.ghost, bag = T.items.bag, shown = !OPT.hideWindow;
  g.on = !T.real && OPT.playing;
  if (!T.win.on || !g.on) g.step = 0;
  let tg = REST;
  if (g.on && T.win.on) {
    const u = T.time - T.openT, often = OPT.mode === 'often', tTake = often ? .2 : .3, tPut = often ? .5 : 1;
    if (g.step === 0) { g.from = [0, 3].find(i => bag[i] && bag[i][0] === 'scrap'); g.step = g.from === undefined ? 3 : 1; }
    if (g.step === 1) {
      tg = slotCenter(T, g.from);
      if (u >= tTake && T.hover && T.hover.i === g.from && !T.held) {
        clickAt(T);
        if (T.held) { T.held.ghost = true; g.to = g.from === 0 ? 3 : 0; if (bag[g.to]) g.to = SLOTS.find(s => !bag[s.i]).i; T.clickAt = T.time; T.clickAfter = u; }
        g.step = 2;
      } else if (u >= tTake && (!shown || u > tTake + .5)) { g.to = g.from === 0 ? 3 : 0; g.step = 2; }
    }
    if (g.step === 2) {
      tg = slotCenter(T, g.to);
      if (u >= tPut && (!T.held || (T.hover && T.hover.i === g.to))) { if (T.held) clickAt(T); g.step = 3; }
    }
    if (g.step === 3) tg = u < 1.7 ? slotCenter(T, 31) : u < 2.3 ? keyCenter(T, 'sort') : slotCenter(T, 22);
  }
  const k = OPT.playing ? Math.min(1, dt * 14) : 1;
  g.x += (tg[0] - g.x) * k; g.y += (tg[1] - g.y) * k;
}
function hitTest(T) {
  T.hover = null; T.ptrIn = false;
  const P = pointer(T);
  if (!P || !T.win.on || OPT.hideWindow) return;
  const q = AF.ap(AF.inv(T.M), P[0], P[1]);
  if (q[0] > -10 && q[0] < W + 10 && q[1] > -10 && q[1] < H + 10) T.ptrIn = true;
  const inS = s => q[0] >= s.x && q[0] < s.x + s.w && q[1] >= s.y && q[1] < s.y + s.h;
  for (const s of SLOTS) if (inS(s)) { T.hover = { i: s.i, s }; return; }
  if (inS(TRASH)) T.hover = { i: 'trash', s: TRASH };
}
// «Отставание»: a start, a stop or a jump swings the window up to 6 px; calmed while the pointer is in it or a stack is held
function stepMotion(T, dt) {
  const ct = (T.ptrIn || T.held) ? .15 : 1;
  T.calm += (ct - T.calm) * Math.min(1, dt * (ct < T.calm ? 22 : 5));
  for (let j = 0; j < 2; j++) T.vs[j] += (T.hv[j] - T.vs[j]) * Math.min(1, dt * 3);
  const dv = [T.hv[0] - T.vs[0], T.hv[1] - T.vs[1]];
  const tg = [clamp(-dv[0] * .04, -6, 6), clamp(-dv[1] * .015, -4.5, 4.5)];
  const k = 70, cd = 2 * Math.sqrt(k) * .55;
  for (let j = 0; j < 2; j++) { T.lv[j] += ((tg[j] - T.lo[j]) * k - T.lv[j] * cd) * dt; T.lo[j] += T.lv[j] * dt; }
  T.M = [1, 0, 0, 1, BAG.x + T.lo[0] * T.calm, BAG.y + T.lo[1] * T.calm];
}

// ---------- drawing ----------
function drawBack(T) {
  const c = T.c, S = T.S;
  c.setTransform(S, 0, 0, S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
  const g = c.createLinearGradient(0, 0, 0, FLOOR_Y); g.addColorStop(0, '#04060a'); g.addColorStop(1, '#0a0f17'); c.fillStyle = g; c.fillRect(0, 0, LW, FLOOR_Y);
  for (const [x, y, sz, a] of STARS) { const xx = x - 200; if (xx < -2 || xx > LW + 2) continue; c.fillStyle = `rgba(220,228,240,${a.toFixed(3)})`; c.fillRect(xx, y, sz, sz); }
  c.fillStyle = '#2f3138'; c.fillRect(0, FLOOR_Y, LW, LH - FLOOR_Y);
  c.fillStyle = '#4f525b'; c.fillRect(0, FLOOR_Y, LW, 3);
  c.fillStyle = 'rgba(0,0,0,.35)'; c.fillRect(0, FLOOR_Y + 3, LW, 3);
  for (let x = 0; x < LW + 32; x += 32) { c.fillStyle = 'rgba(0,0,0,.4)'; c.fillRect(x, FLOOR_Y + 6, 2, LH - FLOOR_Y - 6); c.fillStyle = 'rgba(160,165,175,.25)'; c.fillRect(x + 6, FLOOR_Y + 12, 2, 2); c.fillRect(x + 26, FLOOR_Y + 12, 2, 2); }
  c.fillStyle = 'rgba(0,0,0,.25)'; c.fillRect(0, FLOOR_Y + 36, LW, 2);
}
function drawCursor(c, x, y) {
  c.save(); c.translate(x, y); c.beginPath(); c.moveTo(0, 0); c.lineTo(0, 17); c.lineTo(4.5, 13); c.lineTo(8, 20); c.lineTo(10.5, 19); c.lineTo(7, 12.5); c.lineTo(12.5, 12.5); c.closePath();
  c.fillStyle = 'rgba(238,238,232,.9)'; c.fill(); c.strokeStyle = '#111'; c.lineWidth = 1.2; c.stroke(); c.restore();
}
const world = T => { T.c.setTransform(T.S, 0, 0, T.S, 0, 0); T.c.globalAlpha = 1; T.c.globalCompositeOperation = 'source-over'; };
// a hero with its drone and lamp; the page draws over the hero, takes the drone's plates away, draws over the drone; while
// ownDrone holds, the page draws the drone and its lamp itself
function drawFigure(T, h) {
  const c = T.c, d = h.d, own = !!hook('ownDrone', T, h);
  if (!own) { world(T); lampOn(c, d.p[0], d.p[1], h.x, h.y, h.face, .6, 2.6); }
  world(T); drawHero(c, h);
  world(T); hook('overHero', T, h);
  if (!own) { world(T); drawDrone(c, d.p[0], d.p[1], d.ang, 0, T.time + h.seed, DSC, !!hook('platesAway', T, h)); }
  world(T); hook('overDrone', T, h);
}
function frameTile(T, dt) {
  if (!OPT.playing) dt = 0; else if (OPT.slow) dt *= .25;
  T.time += dt;
  if (T.bufS !== T.S) { T.bufS = T.S; T.cache = {}; hook('onScale', T); }
  if (CFG.scene) { CFG.scene(T, dt); return; }   // a page drawing its whole scene: no floor, hero loop, drone or window
  stepHero(T, dt);
  if (WIN) stepTimeline(T, dt);
  hook('step', T, dt);
  for (const h of T.heroes) { h.P = poseOf(h); stepDrone(T, h, dt); }
  if (WIN) { hitTest(T); stepGhost(T, dt); stepMotion(T, dt); hitTest(T); }
  const c = T.c;
  c.setTransform(1, 0, 0, 1, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
  if (CFG.back) { world(T); CFG.back(T); } else drawBack(T);
  for (const h of T.heroes) drawFigure(T, h);
  drawWindow(T);
  world(T); hook('screen', T);
  world(T);
  const P = pointer(T);
  if (T.held && P && !OPT.hideWindow && ready(IMG[T.held.it[0]])) {
    c.drawImage(IMG[T.held.it[0]], P[0] - 20, P[1] - 20, 40, 40);
    if (T.held.it[1] > 1) { c.font = '600 11px ui-monospace,Consolas,monospace'; c.textAlign = 'right'; c.fillStyle = '#000'; c.fillText(String(T.held.it[1]), P[0] + 19, P[1] + 19); c.fillStyle = '#fff'; c.fillText(String(T.held.it[1]), P[0] + 18, P[1] + 18); c.textAlign = 'left'; }
  }
  if (WIN && !T.real && T.ghost.on) drawCursor(c, T.ghost.x, T.ghost.y);
  if (WIN) {
    const lit = Math.max(0, 1 - (T.time - T.tab) / .45);
    rr(c, 24.5, 494.5, 54, 26, 4); c.fillStyle = css([20, 24, 30], .6 + .3 * lit); c.fill(); c.strokeStyle = css([200, 210, 222], .3 + .65 * lit); c.lineWidth = 1; c.stroke();
    c.font = '600 13px system-ui,"Segoe UI",sans-serif'; c.textAlign = 'center'; c.textBaseline = 'alphabetic'; c.fillStyle = css([226, 230, 236], .5 + .5 * lit); c.fillText('Tab', 51.5, 512); c.textAlign = 'left';
  }
  const hud = hook('hud', T);
  if (hud) { c.font = '13px system-ui,"Segoe UI",sans-serif'; c.textBaseline = 'alphabetic'; c.fillStyle = 'rgba(214,226,240,.75)'; c.fillText(hud, WIN ? 92 : 24, 512); }
  if (OPT.slow) { c.font = '12px system-ui,"Segoe UI",sans-serif'; c.fillStyle = 'rgba(214,190,140,.85)'; c.fillText('×¼', 24, 486); }
}

// ---------- the page: a tile per candidate, the toggles, the frame loop ----------
let CFG = null, PICK = null;
const hook = (k, ...a) => CFG && CFG[k] ? CFG[k](...a) : undefined;
function makeTile(host, cand, num) {
  const sec = document.createElement('section'); sec.className = 'cand';
  sec.innerHTML = `<div class="head"><span class="name">${num} · ${cand.name}</span><span class="tag">${cand.id}</span></div>
    <p class="idea">${cand.idea}</p>
    <p class="idea time">${cand.time}</p>
    <canvas class="view" width="${LW}" height="${LH}"></canvas>
    <div class="acts"><button class="pick" type="button">Выбрать этот</button><button class="big" type="button">Крупно 1:1</button></div>`;
  host.appendChild(sec);
  const cv = sec.querySelector('canvas');
  const T = { cand, sec, cv, c: cv.getContext('2d'), S: 1, visible: true, dirty: true };
  newState(T); hook('init', T);
  T.resize = () => {
    const cssW = cv.clientWidth || 600, d = Math.min(2, window.devicePixelRatio || 1);
    const bw = Math.round(Math.max(300, Math.min(LW * 1.4, cssW * d)));
    if (bw === cv.width && T.S === bw / LW) return;
    cv.width = bw; cv.height = Math.round(LH * bw / LW); T.S = bw / LW; T.dirty = true;
  };
  const toLocal = e => { const r = cv.getBoundingClientRect(); return [(e.clientX - r.left) / (r.width || 1) * LW, (e.clientY - r.top) / (r.height || 1) * LH]; };
  const takeOver = () => { if (!T.real) { T.real = true; if (T.held && T.held.ghost) returnHeld(T); } };
  cv.addEventListener('pointermove', e => { T.ptr = toLocal(e); takeOver(); T.dirty = true; });
  cv.addEventListener('pointerleave', () => { T.real = false; T.dirty = true; });
  cv.addEventListener('pointerdown', e => { e.preventDefault(); T.ptr = toLocal(e); takeOver(); hitTest(T); clickAt(T); T.dirty = true; });
  sec.querySelector('.pick').addEventListener('click', () => choose(cand.id));
  const big = sec.querySelector('.big');
  big.addEventListener('click', () => { const on = sec.classList.toggle('wide'); big.textContent = on ? 'Обратно' : 'Крупно 1:1'; requestAnimationFrame(() => { T.resize(); }); if (on) sec.scrollIntoView({ block: 'nearest' }); });
  tiles.push(T);
  return T;
}
function segButtons(host, opts, get, set) {
  const bs = opts.map(o => {
    const b = document.createElement('button'); b.type = 'button'; b.textContent = o.n;
    b.addEventListener('click', () => { set(o.v); refresh(); dirtyAll(); });
    host.appendChild(b); return b;
  });
  const refresh = () => bs.forEach((x, i) => x.classList.toggle('on', opts[i].v === get()));
  refresh();
  return refresh;
}
const STANDARD = {
  openMode: { win: 1, key: 'mode', opts: [['Открывать и закрывать', 'sched'], ['Часто', 'often'], ['Всегда открыто', 'always']] },
  heroMode: { win: 1, key: 'stand', opts: [['Герой бегает', false], ['Герой стоит', true]] },
  speedMode: { key: 'slow', opts: [['Как в игре', false], ['Медленно ×¼', true]] },
  playMode: { key: 'playing', opts: [['Движение', true], ['Пауза', false]] },
};
const REFRESH = [];
// one toggle bar: its buttons set OPT[key]; a saved one goes into the pick and saves it again
function toggle(t) {
  const host = document.getElementById(t.id);
  REFRESH.push(segButtons(host, t.opts.map(([n, v]) => ({ n, v })), () => OPT[t.key], v => { OPT[t.key] = v; if (t.saved && PICK) choose(PICK); }));
}
let last = null;
function loop(now) {
  const dt = last == null ? 1 / 60 : clamp((now - last) / 1000, 0, 1 / 20); last = now;
  for (const t of tiles) {
    if (!t.visible) continue;
    if (!OPT.playing && !t.dirty) continue;
    t.dirty = false;
    try { frameTile(t, dt); } catch (e) { console.error(e); t.visible = false; }
  }
  requestAnimationFrame(loop);
}

// ---------- the pick: a db doc read back on load, the copy fallback ----------
const statusEl = document.getElementById('status'), copyBtn = document.getElementById('copy'), fbEl = document.getElementById('fallback');
let db = null, writing = Promise.resolve();
const status = s => { statusEl.textContent = s; };
const SAVED = () => PAGE.toggles.filter(t => t.saved);
const optText = () => SAVED().map(t => `${t.short}: ${t.opts.find(o => o[1] === OPT[t.key])[0]}`).join(', ');
const candOf = id => CFG.cands.find(c => c.id === id);
const label = id => `${CFG.cands.indexOf(candOf(id)) + 1} «${candOf(id).name}»${SAVED().length ? ', ' + optText() : ''}`;
function mark() {
  tiles.forEach(t => { const on = PICK === t.cand.id; t.sec.classList.toggle('chosen', on); t.sec.querySelector('.pick').textContent = on ? 'Выбран ✓' : 'Выбрать этот'; });
  dirtyAll();
}
// the db doc a pick of id writes: the candidate and every saved toggle
function pickDoc(id) { const doc = { pick: id }; for (const t of SAVED()) doc[t.key] = OPT[t.key]; return doc; }
function choose(id) {
  PICK = id; mark();
  const doc = pickDoc(id);
  doc.at = new Date().toISOString();
  if (!db) { status(`Выбрано: ${label(id)}. База недоступна: нажми «Скопировать выбор» и отправь текст.`); return; }
  status('Сохраняю…');
  writing = writing.then(() => db.doc(PAGE.pick.doc).set(doc)).then(
    () => status(`Сохранено: ${label(id)}.`),
    () => status('Не сохранилось: нажми «Скопировать выбор» и отправь текст.'));
}
copyBtn.addEventListener('click', () => {
  if (!PICK) { status(`Сначала выбери ${PAGE.pick.what[0]}.`); return; }
  const s = `${PAGE.pick.subject}: ${candOf(PICK).name} (${PICK})${SAVED().length ? ', ' + optText() : ''}.`;
  fbEl.value = s;
  const selectIt = () => { fbEl.hidden = false; fbEl.focus(); fbEl.select(); status('Текст выделен: скопируй его (Ctrl+C).'); };
  try {
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(s).then(() => status('Скопировано.'), selectIt);
    else selectIt();
  } catch (e) { selectIt(); }
});
async function readPick() {
  const deadline = new Promise(r => setTimeout(() => r('timeout'), 10000));
  const load = (async () => {
    const cl = window.claude;
    if (!cl || typeof cl.use !== 'function') return null;
    const d = await cl.use('db');
    if (!d) return null;
    db = d;
    return await d.doc(PAGE.pick.doc).get();
  })().catch(() => 'error');
  const r = await Promise.race([load, deadline]);
  if (!db) { status('База недоступна: выбор можно скопировать кнопкой внизу.'); return; }
  if (r === 'timeout' || r === 'error' || !r) { status('Прошлый выбор не прочитался; новый сохранится.'); return; }
  try {
    if (r.exists) {
      const d = r.data() || {};
      if (!PICK && candOf(d.pick)) {
        PICK = d.pick;
        for (const t of SAVED()) if (t.opts.some(o => o[1] === d[t.key])) OPT[t.key] = d[t.key];
        REFRESH.forEach(f => f());
      }
      mark();
      status(PICK ? `Сохранён выбор: ${label(PICK)}.` : 'Готово.');
    } else status(`Выбора пока нет: нажми «Выбрать этот» под ${PAGE.pick.what[1]}, которая нравится.`);
  } catch (e) { status('Прошлый выбор не прочитался; новый сохранится.'); }
}

// the page's function returns { cands, hooks… }; this runs it all
function startPage(cfg) {
  CFG = cfg;
  for (const t of PAGE.toggles) if (!(t.key in OPT)) OPT[t.key] = t.opts[0][1];
  const host = document.getElementById('cands');
  cfg.cands.forEach((c, i) => makeTile(host, c, i + 1));
  tiles.forEach(t => t.resize());
  if ('ResizeObserver' in window) { const ro = new ResizeObserver(() => tiles.forEach(t => t.resize())); tiles.forEach(t => ro.observe(t.cv)); }
  window.addEventListener('resize', () => tiles.forEach(t => t.resize()));
  if ('IntersectionObserver' in window) {
    const io = new IntersectionObserver(es => es.forEach(e => { const t = tiles.find(x => x.cv === e.target); if (t) { t.visible = e.isIntersecting; t.dirty = true; } }), { rootMargin: '120px' });
    tiles.forEach(t => io.observe(t.cv));
  }
  for (const id in STANDARD) if ((WIN || !STANDARD[id].win) && (!PAGE.kitBars || PAGE.kitBars.includes(id))) toggle({ id, ...STANDARD[id] });
  PAGE.toggles.forEach(toggle);
  if (window.__pickCheck) window.__pickCheck({ tiles, OPT, X: cfg.expose || {}, picked: () => PICK && pickDoc(PICK) });
  requestAnimationFrame(loop);
  readPick();
}
