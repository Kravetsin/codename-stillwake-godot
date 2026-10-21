// Turns art.js's pixel maps into outlined RGBA images, as ../weapons/build.js does: held sprites and
// rods trimmed with a one-pixel outline border (their points moved with them), icons on a 16x16 slot.
const { O, D } = require('./art.js');
const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];

function outlined(px) {
  const has = (x, y) => px.has(`${x},${y}`), out = new Map(px);
  for (const [x, y] of px.values())
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]])
      if (!has(x + dx, y + dy)) out.set(`${x + dx},${y + dy}`, [x + dx, y + dy, '@']);
  return out;
}

function image(px, pal, fixed) {
  const all = fixed ? px : outlined(px);
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

// an icon is drawn on its 16x16 slot already; its outline is added in place
const iconPx = px => outlined(px);

// the gun with its rod seated: the rod's pixels under the gun's (in the barrel) or over them (on a rail)
function loaded(d) {
  const px = new Map(d.held.px);
  for (const [x, y, c] of d.rod.px.values()) {
    const k = `${x + d.seat[0]},${y + d.seat[1]}`;
    if (d.rodOver || (!px.has(k) && x + d.seat[0] >= d.muzzle[0] - 1)) px.set(k, [x + d.seat[0], y + d.seat[1], c]);
  }
  return px;
}

const out = {};
for (const [id, d] of Object.entries(D)) {
  const held = image(d.held.px, d.pal), full = image(loaded(d), d.pal), rod = image(d.rod.px, d.pal);
  const moveH = ([x, y]) => [x + held.dx, y + held.dy];
  // the loaded sprite is the empty one grown by whatever of the rod sticks out
  const shift = [full.dx - held.dx, full.dy - held.dy];
  // the rod's eye: the middle of its ring, the pixel at (1, 1) of the rod's grid
  out[id] = {
    ...d, heldIm: held, loadedIm: full, rodIm: rod, icon: image(iconPx(d.icon.px), d.pal, 16),
    grip: moveH(d.grip), muzzle: moveH(d.muzzle), reelAt: moveH(d.reel), loadedShift: shift,
    eye: [d.eye[0] + rod.dx, d.eye[1] + rod.dy], tip: [rod.w - 1, 1.5 + rod.dy],
  };
}
module.exports = out;
