// The light drone (light-drone), the user's pick «Диск»: a flat lens disc in a slowly turning band of marks, its face turned where it
// shines. The pick page's own pen and paint, drawn straight (no finish), 4 hi-res pixels a world pixel as there. drone_disc: its body
// (rims, face, rust, bolts, the lens's bezel) at each of Turns angles in a row, Box world px each, its middle at the box's; the band,
// its marks, the lens and its light are the game's (LightDrone.cs), since they move or light. items/lightDrone: the whole disc, lit.
(function () {
'use strict';
const Turns = 16, Box = 10, K = 4;
const INK = '#16171b', LAMP = [255, 212, 150];
const STEEL = ['#8b9199', '#61666e', '#3b3f46'], DARK = ['#5b6067', '#42464c', '#2a2d32'], FACE = ['#7d838b', '#5a5f66', '#3c4046'];
const css = (c, a = 1) => `rgba(${c[0] | 0},${c[1] | 0},${c[2] | 0},${a})`;
const pale = (c, k) => [c[0] + (255 - c[0]) * k, c[1] + (255 - c[1]) * k, c[2] + (255 - c[2]) * k];
function rng(seed) { let s = (seed * 2654435761) >>> 0 || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }
function ell(rx, ry, n) { const p = []; for (let i = 0; i < n; i++) { const a = i / n * Math.PI * 2; p.push([Math.cos(a) * rx, Math.sin(a) * ry]); } return p; }
const rot = (x, y, a) => [x * Math.cos(a) - y * Math.sin(a), x * Math.sin(a) + y * Math.cos(a)];

// A hand-drawn shape: jittered points and the pen's pressure per segment.
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
// Hard tone steps lit from straight above: the shade a world-space crescent at the bottom, whichever way the shape turns.
function paint(c, s, ang, col, k1 = 1.6, k2 = 5, grainA = .55) {
  c.save(); c.rotate(ang); trace(c, s); c.clip(); c.rotate(-ang);
  c.fillStyle = col[2]; c.fillRect(-90, -90, 180, 180);
  c.save(); c.translate(0, -k1); c.rotate(ang); c.fillStyle = col[1]; trace(c, s); c.fill(); c.restore();
  c.save(); c.translate(0, -k2); c.rotate(ang); c.fillStyle = col[0]; trace(c, s); c.fill(); c.restore();
  if (c.grain) { c.globalAlpha = grainA; c.fillStyle = c.grain; c.fillRect(-90, -90, 180, 180); }
  c.restore();
}
// A wandering pen line, heavier round the object; a thin lit edge where the outline faces up.
function ink(c, s, ang, lw, col = INK, lit = 'rgba(214,220,226,.5)') {
  const cs = Math.cos(ang), sn = Math.sin(ang);
  c.save(); c.rotate(ang); c.lineCap = 'round'; c.lineJoin = 'round';
  if (lit && s.closed) {
    c.strokeStyle = lit; c.lineWidth = 1;
    for (const g of s.segs) {
      const nx = g[2] - s.cx, ny = g[3] - s.cy, L = Math.hypot(nx, ny) || 1;
      if ((nx * sn + ny * cs) / L < -.42) {
        const ix = -nx / L * 1.4, iy = -ny / L * 1.4;
        c.beginPath(); c.moveTo(g[0] + ix, g[1] + iy); c.quadraticCurveTo(g[2] + ix, g[3] + iy, g[4] + ix, g[5] + iy); c.stroke();
      }
    }
  }
  c.strokeStyle = col;
  for (const g of s.segs) {
    if (g[6] <= .05) continue;
    c.lineWidth = lw * g[6]; c.beginPath(); c.moveTo(g[0], g[1]); c.quadraticCurveTo(g[2], g[3], g[4], g[5]); c.stroke();
  }
  c.restore();
}
function blobAt(r, x, y, sz) {
  const n = 9, p = [];
  for (let i = 0; i < n; i++) { const a = i / n * 6.283 + r() * .4, rr = sz * (.55 + .6 * r()); p.push([x + Math.cos(a) * rr, y + Math.sin(a) * rr * .8]); }
  return { s: mk(p, (r() * 1e6) | 0, .3), x, y };
}
function rust(c, b) {
  c.fillStyle = 'rgba(118,66,36,.85)'; trace(c, b.s); c.fill();
  c.save(); c.translate(b.x, b.y); c.scale(.45, .45); c.translate(-b.x, -b.y);
  c.fillStyle = 'rgba(70,36,20,.9)'; trace(c, b.s); c.fill(); c.restore();
}
function rivet(c, x, y) {
  c.fillStyle = '#25282d'; c.beginPath(); c.arc(x, y, 1.6, 0, 6.283); c.fill();
  c.fillStyle = 'rgba(190,196,202,.8)'; c.beginPath(); c.arc(x - .4, y - .6, .6, 0, 6.283); c.fill();
}
function grainOf(c) {
  const g = document.createElement('canvas'); g.width = g.height = 64;
  const x = g.getContext('2d'), r = rng(7);
  for (let i = 0; i < 420; i++) { const d = r() < .62; x.fillStyle = d ? `rgba(0,0,0,${.06 + r() * .16})` : `rgba(255,255,255,${.03 + r() * .07})`; x.fillRect(r() * 64, r() * 64, 1 + (r() < .2), 1); }
  x.strokeStyle = 'rgba(0,0,0,.1)'; x.lineWidth = .8;
  for (let i = 0; i < 4; i++) { x.beginPath(); x.arc(r() * 64, r() * 64, 6 + r() * 14, r() * 6, r() * 6 + 1.6); x.stroke(); }
  return c.createPattern(g, 'repeat');
}

const rim = mk(ell(8.6, 13.5, 36), 51, .35), face = mk(ell(6.6, 10.6, 30), 52, .3), groove = mk(ell(4.6, 7.6, 26), 53, .2);
const bez = mk(ell(3.4, 5, 18), 54, .2);
const rusts = (() => { const r = rng(55); return [blobAt(r, -3, 9, 1.6), blobAt(r, 4.5, -8.5, 1.3)]; })();
const bolts = [[0, -9.2], [0, 9.2], [-5.6, -4.6], [-5.6, 4.6]];

/** The disc's body turned <a>, at the canvas's origin: everything of the pick's but its band, lens and light. */
function body(c, a) {
  const back = rot(-2.6, 0, a);
  c.save(); c.translate(back[0], back[1]); paint(c, rim, a, DARK, 1, 3); ink(c, rim, a, 1.8); c.restore();
  paint(c, rim, a, STEEL, 1.4, 4.5);
  paint(c, face, a, FACE, 1, 3);
  c.save(); c.rotate(a); for (const b of rusts) rust(c, b); for (const b of bolts) rivet(c, b[0], b[1]); c.restore();
  ink(c, groove, a, .8, 'rgba(22,22,26,.75)', null);
  ink(c, face, a, 1.1, 'rgba(22,22,26,.85)');
  ink(c, rim, a, 2.1);
  c.save(); const lp = rot(2.6, 0, a); c.translate(lp[0], lp[1]);
  paint(c, bez, a, DARK, .8, 2); ink(c, bez, a, 1.3, INK, null);
  c.restore();
}

/** Half the band, behind the body or before it, with its marks <t> seconds on: the game draws the same (LightDrone.cs). */
function band(c, t, front) {
  const rx = 20.5, ry = 6.2, tl = -.32, a0 = front ? 0 : Math.PI, a1 = front ? Math.PI : Math.PI * 2;
  c.save(); c.lineCap = 'round';
  c.beginPath(); c.ellipse(0, 0, rx, ry, tl, a0, a1); c.strokeStyle = INK; c.lineWidth = 5; c.stroke();
  c.strokeStyle = front ? '#6c7178' : '#4d5157'; c.lineWidth = 3; c.stroke();
  if (front) { c.beginPath(); c.ellipse(0, -1, rx, ry, tl, a0 + .25, a1 - .25); c.strokeStyle = 'rgba(190,196,202,.45)'; c.lineWidth = .8; c.stroke(); }
  for (let k = 0; k < 5; k++) {
    const th = k * Math.PI * 2 / 5 + t * .45, s = Math.sin(th);
    if ((s >= 0) !== front) continue;
    const [x, y] = rot(Math.cos(th) * rx, s * ry, tl);
    c.fillStyle = front ? 'rgba(206,198,170,.9)' : 'rgba(150,144,124,.7)';
    c.fillRect(x - 1.1, y - 1.1, 2.2, 2.2);
  }
  c.restore();
}

Subjects.drone = function () {
  const side = Box * K, sheet = document.createElement('canvas');
  [sheet.width, sheet.height] = [side * Turns, side];
  const c = sheet.getContext('2d');
  c.grain = grainOf(c);
  for (let i = 0; i < Turns; i++) {
    c.save(); c.translate(side * i + side / 2, side / 2); body(c, i / Turns * Math.PI * 2); c.restore();
  }
  out('drone_disc', sheet);

  // The card: the whole disc facing right, its lamp on, grown to fill the icon's 64 px.
  const icon = document.createElement('canvas');
  [icon.width, icon.height] = [64, 64];
  const g = icon.getContext('2d');
  g.grain = grainOf(g);
  g.translate(32, 33); g.scale(1.3, 1.3);
  band(g, 1.2, false);
  body(g, 0);
  g.save(); g.translate(2.6, 0);
  g.save(); g.globalCompositeOperation = 'lighter';
  const h = g.createRadialGradient(0, 0, 0, 0, 0, 15); h.addColorStop(0, css(LAMP, .6)); h.addColorStop(1, css(LAMP, 0));
  g.fillStyle = h; g.beginPath(); g.arc(0, 0, 15, 0, 6.283); g.fill(); g.restore();
  g.fillStyle = css(pale(LAMP, .3)); g.beginPath(); g.ellipse(0, 0, 2.3, 3.7, 0, 0, 6.283); g.fill();
  g.fillStyle = css(pale(LAMP, .78)); g.beginPath(); g.ellipse(.6, 0, 1, 1.8, 0, 0, 6.283); g.fill();
  g.restore();
  band(g, 1.2, true);
  out('items/lightDrone', icon);
};
})();
