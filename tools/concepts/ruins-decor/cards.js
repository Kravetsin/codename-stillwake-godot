// One card per decor piece: a slice of a room (plaster back wall, alloy floor and ceiling), the piece lit by a soft key
// light and its own glow. Writes out/cards/<id>.png and out/cards.json (size, emitters).
const fs = require('fs'), path = require('path');
const E = require('../../planet-art/caves/engine');
const { C, P, Pic, lightUp } = E;
const { Room, paint } = require('./room');
const D = require('./decor');

const OUT = path.join(__dirname, 'out', 'cards'); fs.mkdirSync(OUT, { recursive: true });
const ON = { dishes: 'table', kettle: 'stool', musicBox: 'stool', arcCandle: 'table' };
const meta = {};
for (const [id, pc] of Object.entries(D.PIECES)) {
  const base = ON[id] ? D.PIECES[ON[id]] : null;
  const w = Math.max(pc.w, base ? base.w : 0), h = pc.h + (base ? base.h : 0);
  const W = Math.max(4, Math.ceil(w / C) + 2), Hc = Math.max(3, Math.ceil(h / C) + (pc.anchor === 'wall' ? 2 : 1)) + 2;
  const room = new Room(W, Hc);
  room.fill(0, 0, W, Hc, 'alloy');
  room.carve(0, 1, W, Hc - 2, (x, y) => y === Hc - 2 ? 'alloy' : 'plaster');
  room.shapes();
  const pic = new Pic(room); paint(pic);
  const floor = (Hc - 1) * C, ceil = C, x = Math.round((W * C - w) / 2);
  let bx = x, bw = w, btop, bh = h;
  if (base) { D.put(pic, ON[id], x + Math.round((w - base.w) / 2), floor); D.put(pic, id, x + Math.round((w - pc.w) / 2), floor - base.h + (ON[id] === 'table' ? 1 : 0)); }
  else if (pc.anchor === 'floor') D.put(pic, id, x, floor);
  else if (pc.anchor === 'ceiling') D.put(pic, id, x, ceil);
  else D.put(pic, id, x, Math.round(ceil + (floor - ceil - pc.h) / 2) - 2);
  btop = pc.anchor === 'floor' || base ? floor - h : pc.anchor === 'ceiling' ? ceil : Math.round(ceil + (floor - ceil - pc.h) / 2) - 2;
  const glows = !!pc.glow;
  pic.light({ x: W * C * 0.3, y: ceil + 4, col: [0.86, 0.88, 0.95], r: Math.max(W, Hc) * 1.1, k: glows ? 0.32 : 0.75, p: 0.9, pen: 0.98 });
  const img = lightUp(pic, { amb: glows ? 0.16 : 0.34, bloom: 0.9 });
  const M = 7, cx0 = Math.max(0, bx - M), cy0 = Math.max(0, btop - M - (pc.anchor === 'ceiling' ? 4 : 0)), cx1 = Math.min(img.w, bx + bw + M), cy1 = Math.min(img.h, (pc.anchor === 'floor' || base ? floor : btop + bh) + (pc.anchor === 'floor' || base ? 4 : M));
  const cw = cx1 - cx0, ch = cy1 - cy0, crop = P.create(cw, ch);
  for (let j = 0; j < ch; j++) img.data.copy(crop.data, j * cw * 4, ((cy0 + j) * img.w + cx0) * 4, ((cy0 + j) * img.w + cx1) * 4);
  P.encode(crop, path.join(OUT, id + '.png'));
  for (const e of pic.emitters) { const sh = (pt) => Array.isArray(pt) ? [pt[0] - cx0, pt[1] - cy0] : pt; if (e.a) e.a = sh(e.a); if (e.b) e.b = sh(e.b); if (e.x != null) e.x -= cx0; if (e.y != null) e.y -= cy0; if (e.x0 != null) { e.x0 -= cx0; e.x1 -= cx0; e.y0 -= cy0; e.y1 -= cy0; } if (e.pts) e.pts = e.pts.map(sh); }
  meta[id] = { size: [cw, ch], cells: [Math.ceil(pc.w / C), Math.ceil(pc.h / C)], px: [pc.w, pc.h], anchor: pc.anchor,
    emitters: pic.emitters.map((e) => ({ ...e, col: e.col && e.col.map((v) => v <= 1 && !Number.isInteger(v) ? Math.round(v * 255) : v) })) };
}
fs.writeFileSync(path.join(__dirname, 'out', 'cards.json'), JSON.stringify(meta));
console.log(Object.keys(meta).length, 'cards');
