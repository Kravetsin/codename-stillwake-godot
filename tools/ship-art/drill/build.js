// Turns art.js's drawings into outlined RGBA images, as ../harpoon-art/build.js does: every spin frame of a
// held sprite trimmed to one box, the box of all its frames with their outlines, so they swap in place;
// the points moved with it. Icons stay on their 16x16 slot.
const { O, D } = require('./art.js');
const hex = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];

function outlined(px) {
  const has = (x, y) => px.has(`${x},${y}`), out = new Map(px);
  for (const [x, y] of px.values())
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]])
      if (!has(x + dx, y + dy)) out.set(`${x + dx},${y + dy}`, [x + dx, y + dy, '@']);
  return out;
}

function box(all) {
  let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
  for (const [x, y] of all.values()) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
  return [x0, y0, x1, y1];
}

function image(all, pal, [x0, y0, x1, y1]) {
  const w = x1 - x0 + 1, h = y1 - y0 + 1, data = Buffer.alloc(w * h * 4);
  for (const [x, y, c] of all.values()) {
    if (x < x0 || y < y0 || x > x1 || y > y1) { console.warn(`off the box: ${x},${y}`); continue; }
    const col = c === '@' ? O : pal[c];
    if (!col) throw new Error(`no colour for ${c}`);
    const o = ((y - y0) * w + (x - x0)) * 4, [r, g, b] = hex(col);
    data[o] = r; data[o + 1] = g; data[o + 2] = b; data[o + 3] = 255;
  }
  return { w, h, data, dx: -x0, dy: -y0 };
}

const out = {};
for (const [id, d] of Object.entries(D)) {
  const frames = [...Array(d.frames).keys()].map(k => outlined(d.held(k).px));
  const boxes = frames.map(box);
  const b = [0, 1, 2, 3].map(i => (i < 2 ? Math.min : Math.max)(...boxes.map(x => x[i])));
  const ims = frames.map(f => image(f, d.pal, b));
  const move = ([x, y]) => [x + ims[0].dx, y + ims[0].dy];
  out[id] = {
    ...d, heldIm: ims[0], spinIms: ims.slice(1), icon: image(outlined(d.icon().px), d.pal, [0, 0, 15, 15]),
    grip: move(d.grip), chuck: move(d.chuck), tip: move(d.tip), plug: move(d.plug),
  };
}
module.exports = out;
