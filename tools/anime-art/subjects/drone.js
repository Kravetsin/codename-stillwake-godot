// The light drone (drone-swarm), the user's pick «Рой»: an iron core holding ten scrap shards by its field. The pick page's own pen
// and paint, its small swarm (build(0)), drawn straight (no finish), 4 hi-res pixels a world pixel as there. drone_core: the core
// and its rust, one picture (it is round), CoreBox world px, its middle at the box's. drone_shards: a row per shard, each at Turns
// angles, ShardBox world px each, its middle at the box's. Where the shards fly, the lens, the field's threads and the glow are the
// game's (DroneSwarm.cs, LightDrone.cs), since they move or light. items/lightDrone: the swarm idle, its lamp on.
(function () {
'use strict';
const Turns = 8, CoreBox = 4, ShardBox = 4, K = 4;
const INK = '#16171b', LAMP = [255, 212, 150];
const STEEL = ['#8b9199', '#61666e', '#3b3f46'], DARK = ['#5b6067', '#42464c', '#2a2d32'], ORANGE = ['#a36c3b', '#7a512f', '#4c331f'];
const OLIVE = ['#6b745f', '#4e5546', '#30352c'], BRONZE = ['#8d7a5a', '#685a43', '#41382a'];
const css = (c, a = 1) => `rgba(${c[0] | 0},${c[1] | 0},${c[2] | 0},${a})`;
const pale = (c, k) => [c[0] + (255 - c[0]) * k, c[1] + (255 - c[1]) * k, c[2] + (255 - c[2]) * k];
function rng(seed) { let s = (seed * 2654435761) >>> 0 || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }
function ell(rx, ry, n) { const p = []; for (let i = 0; i < n; i++) { const a = i / n * Math.PI * 2; p.push([Math.cos(a) * rx, Math.sin(a) * ry]); } return p; }
const rot = (x, y, a) => [x * Math.cos(a) - y * Math.sin(a), x * Math.sin(a) + y * Math.cos(a)];
// A polygon's edges sampled densely, so the pen keeps its corners sharp.
function polyPts(v) { const p = []; for (let i = 0; i < v.length; i++) { const a = v[i], b = v[(i + 1) % v.length]; for (const t of [0, .2, .5, .8]) p.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); } return p; }

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
function halo(c, x, y, r, rgb, a) {
  if (a <= .005) return;
  c.save(); c.globalCompositeOperation = 'lighter';
  const g = c.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, css(rgb, a)); g.addColorStop(1, css(rgb, 0));
  c.fillStyle = g; c.beginPath(); c.arc(x, y, r, 0, 6.283); c.fill(); c.restore();
}
function grainOf(c) {
  const g = document.createElement('canvas'); g.width = g.height = 64;
  const x = g.getContext('2d'), r = rng(7);
  for (let i = 0; i < 420; i++) { const d = r() < .62; x.fillStyle = d ? `rgba(0,0,0,${.06 + r() * .16})` : `rgba(255,255,255,${.03 + r() * .07})`; x.fillRect(r() * 64, r() * 64, 1 + (r() < .2), 1); }
  x.strokeStyle = 'rgba(0,0,0,.1)'; x.lineWidth = .8;
  for (let i = 0; i < 4; i++) { x.beginPath(); x.arc(r() * 64, r() * 64, 6 + r() * 14, r() * 6, r() * 6 + 1.6); x.stroke(); }
  return c.createPattern(g, 'repeat');
}
function shardShape(r, sz) {
  const n = 4 + ((r() * 3) | 0), v = [];
  for (let i = 0; i < n; i++) { const a = (i + (r() - .5) * .5) / n * 6.283, rr = sz * (.6 + r() * .45); v.push([Math.cos(a) * rr * 1.3, Math.sin(a) * rr * .8]); }
  return mk(polyPts(v), (r() * 1e6) | 0, .2);
}
function piece(c, x, y, a, sh, col, lw = 1.1, rb = null) {
  c.save(); c.translate(x, y);
  paint(c, sh, a, col, .5, 1.6, .45);
  if (rb) { c.save(); c.rotate(a); trace(c, sh); c.clip(); rust(c, rb); c.restore(); }
  ink(c, sh, a, lw);
  c.restore();
}

// The pick's small swarm, drawn from its seeds in its order: each shard's phase and pace are DroneSwarm.cs's Shards.
const swarm = (() => {
  const r = rng(101), pal = [STEEL, ORANGE, DARK, STEEL, OLIVE, BRONZE], sh = [];
  for (let i = 0; i < 10; i++) {
    const s = shardShape(r, 3.3 + r() * 1.5);
    s.col = pal[(r() * pal.length) | 0]; s.rb = r() < .45 ? blobAt(r, (r() - .5) * 2, (r() - .5) * 1.5, 1.1) : null;
    s.ph = r() * 6.283; s.rr = r(); sh.push(s);
  }
  return { sh, core: mk(ell(5.4, 5.4, 22), 111, .3), crust: blobAt(rng(120), -2, 2.5, 1.5) };
})();

function core(c) {
  paint(c, swarm.core, 0, DARK, .8, 2.6);
  rust(c, swarm.crust);
  ink(c, swarm.core, 0, 1.7);
}

// The pick's idle shape at <t> seconds, canvas px: shard <s>'s place, turn and side (behind the core below 0).
function idle(s, t) {
  const th = s.ph + t * (.45 + s.rr * .45), rr = 12 + s.rr * 6, p = rot(Math.cos(th) * rr, Math.sin(th) * rr * .5, -.25);
  return [p[0], p[1] + Math.sin(t * 1.3 + s.ph) * 1.5, s.ph + t * (.8 + s.rr), Math.sin(th)];
}

Subjects.drone = function () {
  const cs = CoreBox * K, coreSheet = document.createElement('canvas');
  [coreSheet.width, coreSheet.height] = [cs, cs];
  const cc = coreSheet.getContext('2d');
  cc.grain = grainOf(cc);
  cc.translate(cs / 2, cs / 2); core(cc);
  out('drone_core', coreSheet);

  const side = ShardBox * K, sheet = document.createElement('canvas');
  [sheet.width, sheet.height] = [side * Turns, side * swarm.sh.length];
  const c = sheet.getContext('2d');
  c.grain = grainOf(c);
  swarm.sh.forEach((s, i) => {
    const far = Math.max(...s.p.map(q => Math.hypot(q[0], q[1])));
    if (far + 1.4 > side / 2) throw new Error(`Shard ${i} reaches ${far.toFixed(1)} px, past its box's ${side / 2}`);
    for (let k = 0; k < Turns; k++) piece(c, side * k + side / 2, side * i + side / 2, k / Turns * Math.PI * 2, s, s.col, 1.1, s.rb);
  });
  out('drone_shards', sheet);

  // The card: the swarm idle facing right, its lamp on, grown to fill the icon's 64 px.
  const icon = document.createElement('canvas');
  [icon.width, icon.height] = [64, 64];
  const g = icon.getContext('2d');
  g.grain = grainOf(g);
  g.translate(32, 32); g.scale(1.25, 1.25);
  const placed = swarm.sh.map(s => [s, idle(s, 2)]);
  for (const [s, q] of placed) if (q[3] < 0) piece(g, q[0], q[1], q[2], s, s.col, 1.1, s.rb);
  core(g);
  g.save(); g.translate(2.2, 0); g.scale(.82, .82);
  g.fillStyle = DARK[2]; g.beginPath(); g.ellipse(0, 0, 3.1, 4.4, 0, 0, 6.283); g.fill();
  g.strokeStyle = INK; g.lineWidth = 1.3; g.stroke();
  g.fillStyle = css(pale(LAMP, .3)); g.beginPath(); g.ellipse(.3, 0, 2.1, 3.3, 0, 0, 6.283); g.fill();
  g.fillStyle = css(pale(LAMP, .8)); g.beginPath(); g.ellipse(.8, 0, .9, 1.7, 0, 0, 6.283); g.fill();
  g.restore();
  halo(g, 2.2, 0, 14 * .82, LAMP, .55);
  for (const [s, q] of placed) if (q[3] >= 0) piece(g, q[0], q[1], q[2], s, s.col, 1.1, s.rb);
  out('items/lightDrone', icon);
};
})();
