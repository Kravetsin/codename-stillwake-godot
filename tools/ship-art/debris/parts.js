// Ship parts for the derelicts, drawn into a Pic in design space (mirrored pictures stay lit from the left).
const { hash, clamp } = require('./lib.js');

// a jagged run from a to b: teeth of up to amp pixels either side, laid by the seed
function jag([x0, y0], [x1, y1], amp, seed, step = 2) {
  const len = Math.hypot(x1 - x0, y1 - y0), n = Math.max(1, Math.round(len / step)), nx = -(y1 - y0) / len, ny = (x1 - x0) / len, out = [];
  for (let i = 0; i <= n; i++) {
    const t = i / n, o = i === 0 || i === n ? 0 : (hash(i, seed, 77) - 0.5) * 2 * amp * (i % 2 ? 1 : 0.4);
    out.push([x0 + (x1 - x0) * t + nx * o, y0 + (y1 - y0) * t + ny * o]);
  }
  return out;
}

// tear a polygon off: everything in it goes, and the metal left along the cut shows its torn edge
function tear(p, pts, { lit = 4 } = {}) {
  const gone = p.polyPts(pts).filter(([x, y]) => p.has(x, y));
  for (const [x, y] of gone) p.clear(x, y);
  const set = new Set(gone.map(([x, y]) => x + ',' + y)), seen = new Set();
  for (const [x, y] of gone) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    const X = x + dx, Y = y + dy, key = X + ',' + Y; if (set.has(key) || seen.has(key) || !p.has(X, Y)) continue; seen.add(key);
    const m = p.matAt(X, Y); if (m === 'ink' || m === 'void') continue;
    p.px(X, Y, m, hash(X, Y, 5) < 0.3 ? lit : 1);
  }
}

// a hole into the dark inside: the cut edge lit on its lower and right sides, shadowed on the upper and left
function hole(p, pts, { mat = 'void' } = {}) {
  const cells = p.polyPts(pts); const set = new Set(cells.map(([x, y]) => x + ',' + y));
  for (const [x, y] of cells) p.px(x, y, mat, 2 + (hash(x, y, 9) < 0.12 ? 1 : 0));
  const rim = [];
  for (const [x, y] of cells) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    const X = x + dx, Y = y + dy; if (set.has(X + ',' + Y) || !p.has(X, Y)) continue;
    const m = p.matAt(X, Y); if (m === mat || m === 'ink') continue;
    // the hole lies up or left of this pixel (output space): its cut face looks at the light
    const ox = p.flip ? -dx : dx; rim.push([X, Y, m, (ox > 0 || dy > 0) ? 4 : 1]);
  }
  for (const [x, y, m, k] of rim) p.px(x, y, m, k);
  return { cells, set };
}
// shade what lies in a hole: the rows and columns just inside its upper and left rim fall into shadow
function holeShade(p, h, depth = 2) {
  for (const [x, y] of h.cells) {
    let near = false;
    for (let d = 1; d <= depth && !near; d++) if (!h.set.has(x + ',' + (y - d)) || !h.set.has((p.flip ? x + d : x - d) + ',' + y)) near = true;
    if (near && p.matAt(x, y) !== 'void') p.px(x, y, p.matAt(x, y), Math.max(0, p.slotAt(x, y) - 2));
    else if (near) p.px(x, y, 'void', 0);
  }
}

// a bar of thickness t from a to b: lit on its upper side
function bar(p, [x0, y0], [x1, y1], mat, t = 2) {
  const pts = p.line(x0, y0, x1, y1), vert = Math.abs(y1 - y0) > Math.abs(x1 - x0);
  for (const [x, y] of pts) for (let i = 0; i < t; i++) {
    const X = vert ? x + i : x, Y = vert ? y : y + i, k = t === 1 ? 3 : i === 0 ? 4 : i === t - 1 ? 2 : 3;
    p.px(X, Y, mat, vert ? (p.flip ? (i === t - 1 ? 4 : i === 0 ? 2 : 3) : k) : k);
  }
}
// a truss: two rails and zigzag braces between them, from a to b, w wide
function truss(p, [x0, y0], [x1, y1], mat, w = 6) {
  const len = Math.hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / len, uy = (y1 - y0) / len, nx = -uy, ny = ux, h = (w - 1) / 2;
  const n = Math.max(1, Math.round(len / (w - 2)));
  for (let i = 0; i < n; i++) {
    const a = [x0 + ux * len * i / n, y0 + uy * len * i / n], b = [x0 + ux * len * (i + 1) / n, y0 + uy * len * (i + 1) / n];
    const s1 = i % 2 ? h - 1 : -(h - 1); p.line(a[0] + nx * s1, a[1] + ny * s1, b[0] - nx * s1, b[1] - ny * s1, mat, 3);
  }
  // rails, two pixels each: the side toward the light lit
  const lit = s => (nx * s * (p.flip ? 1 : -1) * 0.62 + ny * s * -0.78 > 0);
  for (const s of [-h, h]) for (const o of [0, s < 0 ? 1 : -1]) {
    const k = o === 0 ? (lit(s) ? 4 : 1) : 3;
    p.line(x0 + nx * (s + o), y0 + ny * (s + o), x1 + nx * (s + o), y1 + ny * (s + o), mat, k);
  }
}
function wire(p, pts, mat) { const line = p.path(pts); line.forEach(([x, y], i) => p.px(x, y, mat, i % 4 === 1 ? 4 : 3)); }

function gear(p, cx, cy, r, teeth, mat) {
  p.sphere(cx, cy, r - 1, mat, { gain: 0.6, bias: 0.05 });
  for (let i = 0; i < teeth; i++) {
    const a = (i / teeth) * Math.PI * 2 + 0.3, x = cx + Math.cos(a) * (r - 0.2), y = cy + Math.sin(a) * (r - 0.2);
    p.px(Math.floor(x), Math.floor(y), mat, Math.cos(a) * -0.62 + Math.sin(a) * -0.78 > 0 ? 4 : 2);
  }
  p.ell(cx, cy, Math.max(1, r * 0.32), Math.max(1, r * 0.32), 'void', 0);
  if (r >= 5) p.px(cx - 1 - Math.floor(r * 0.45), cy - Math.floor(r * 0.45), mat, 5);
}
// a turbine seen face on: a ring and blades round a hub
function rotor(p, cx, cy, r, mat) {
  p.ell(cx, cy, r, r, mat, 1);
  for (let i = 0; i < 9; i++) {
    const a = i / 9 * Math.PI * 2; for (let t = 1.5; t < r; t += 0.5) {
      const b = a + t * 0.12; p.px(Math.floor(cx + Math.cos(b) * t), Math.floor(cy + Math.sin(b) * t), mat, Math.sin(b) < 0 ? 4 : 3);
    }
  }
  p.ell(cx, cy, 1.6, 1.6, mat, 5);
}
function crate(p, x, y, w, h, { mat = 'wood', brace = true } = {}) {
  p.panel(x, y, x + w - 1, y + h - 1, mat, { base: 3 });
  for (let j = y + 3; j < y + h - 2; j += 3) p.seamH(x + 1, x + w - 2, j, mat, 2);
  if (brace && w >= 6 && h >= 6) { p.line(x + 1, y + 1, x + w - 2, y + h - 2, mat, 4); p.line(x + 1, y + h - 2, x + w - 2, y + 1, mat, 2); }
  p.rect(x, y, x + w - 1, y, mat, 4); p.rect(x, y + h - 1, x + w - 1, y + h - 1, mat, 1);
}
function canister(p, x, y, h, mat = 'blue') {
  p.cyl(x, y + 1, x + 3, y + h - 1, mat, 'y'); p.rect(x + 1, y, x + 2, y, 'steel', 4);
  p.rect(x, y + Math.floor(h / 2), x + 3, y + Math.floor(h / 2), 'white', 4);
}
function porthole(p, cx, cy, r) {
  p.ell(cx, cy, r + 1, r + 1, 'steel', 2);
  p.ell(cx, cy, r, r, 'glass', (x, y, dx, dy) => (dx + dy < -0.5 ? 4 : dx + dy < 0.4 ? 3 : 2));
  p.px(Math.floor(cx - r * 0.4), Math.floor(cy - r * 0.4), 'glass', 5);
}

module.exports = { jag, tear, hole, holeShade, bar, truss, wire, gear, rotor, crate, canister, porthole };
