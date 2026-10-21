// Turns the pixel maps of art.js into outlined RGBA images: held sprites trimmed with a one-pixel
// outline border (grip and edge points moved with them), icons kept on their 16x16 slot.
const { O, HELD, ICON } = require('./art.js');
const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];

function outlined(px) {
  const has = (x, y) => px.has(`${x},${y}`), out = new Map(px);
  for (const [x, y] of px.values())
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]])
      if (!has(x + dx, y + dy)) out.set(`${x + dx},${y + dy}`, [x + dx, y + dy, '@']);
  return out;
}

function image(px, pal, fixed) {
  const all = outlined(px);
  let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
  for (const [x, y] of all.values()) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
  if (fixed) { x0 = 0; y0 = 0; x1 = fixed - 1; y1 = fixed - 1; }
  const w = x1 - x0 + 1, h = y1 - y0 + 1, data = Buffer.alloc(w * h * 4);
  for (const [x, y, c] of all.values()) {
    if (x < x0 || y < y0 || x > x1 || y > y1) { console.warn(`off the slot: ${x},${y}`); continue; }
    const col = c === '@' ? O : pal[c];
    if (!col) throw new Error(`no colour for ${c}`);
    const o = ((y - y0) * w + (x - x0)) * 4, [r, g, b] = hex(col);
    data[o] = r; data[o + 1] = g; data[o + 2] = b; data[o + 3] = 255;
  }
  return { w, h, data, dx: -x0, dy: -y0 };
}

const out = {};
for (const id of Object.keys(HELD)) {
  const h = HELD[id], held = image(h.px, h.pal);
  const move = ([x, y]) => [x + held.dx, y + held.dy];
  out[id] = { held, icon: image(ICON[id], h.pal, 16), grip: move(h.grip), edge: h.edge.map(move) };
}
module.exports = out;
