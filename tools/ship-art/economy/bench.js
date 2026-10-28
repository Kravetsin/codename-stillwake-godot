// The workbench candidates: a start station of raw salvage, unpowered, 4x2 cells (32x16 px; with a rise, 32x32, the
// bench in the bottom 16 rows as the arc bench's). Each is a rest picture and four work pictures, overlays of what
// moves: a hacksaw working a bar on the top, filings falling off it.
const { Cv, C, HULL, RUST, WOOD, RED, HAZARD } = require('./lib.js');

// A hull plate torn from a wreck, laid as the top: rows y..y+5, its left end ragged, a patch welded on.
function plateTop(c, x0, x1, y) {
  c.lit(x0, y, x1, y + 5, HULL);
  c.clear(x0, y); c.clear(x0, y + 1); c.set(x0, y + 2, C.line); c.set(x0 + 1, y + 1, C.line); c.set(x0 + 1, y + 2, HULL[0]);
  for (const x of [x0 + 4, x1 - 4]) { c.set(x, y + 3, C.hi); c.set(x + 1, y + 4, HULL[3]); }
  c.rect(x0 + 18, y + 2, x0 + 20, y + 4, RUST[2]); c.set(x0 + 19, y + 2, RUST[1]); c.set(x0 + 20, y + 4, RUST[3]); c.set(x0 + 19, y + 4, RUST[2]);
  // the patch, a darker offcut with a bead of weld along its edges
  c.rect(x0 + 7, y + 2, x0 + 13, y + 4, C.dark);
  c.rect(x0 + 7, y + 2, x0 + 13, y + 2, C.mid2);
  for (let x = x0 + 7; x <= x0 + 13; x += 2) c.set(x, y + 1, C.weld);
  c.set(x0 + 6, y + 3, C.weld); c.set(x0 + 14, y + 3, C.weld);
}

// A plank, rows y..y+2, with nail heads.
function plank(c, x0, x1, y, nails = []) {
  c.box(x0, y, x1, y + 2, WOOD[1]);
  for (let x = x0 + 2; x < x1 - 1; x += 5) c.set(x, y + 1, WOOD[2]);
  for (const x of nails) c.set(x, y + 1, C.hi);
}

// An I-beam offcut standing as a leg, x0..x0+4, from y0 to the floor.
function beamLeg(c, x0, y0, y1) {
  c.box(x0, y0, x0 + 4, y1, C.mid);
  c.rect(x0 + 1, y0 + 1, x0 + 3, y0 + 1, C.steel);
  c.rect(x0 + 2, y0 + 2, x0 + 2, y1 - 2, C.dark2);
  c.rect(x0 + 1, y0 + 2, x0 + 1, y1 - 2, C.steel);
  c.rect(x0 + 1, y1 - 1, x0 + 3, y1 - 1, C.dark);
  c.set(x0 + 2, y0 + 3, C.deep);
}

// A pipe leaning from (x0, y0) down to y1, a column to the right every `lean` rows.
function pipeLeg(c, x0, y0, y1, lean) {
  for (let y = y0; y <= y1; y++) {
    const x = x0 + Math.floor((y - y0) / lean);
    c.set(x, y, C.line); c.set(x + 1, y, C.steel); c.set(x + 2, y, C.mid2); c.set(x + 3, y, C.line);
  }
}

// A brick of rust under a short leg.
function shim(c, x0, x1, y) { c.rect(x0, y, x1, y + 1, C.line); c.rect(x0 + 1, y, x1 - 1, y, RUST[1]); c.rect(x0 + 1, y + 1, x1 - 1, y + 1, RUST[2]); }

const weld = (c, pts) => { for (const [x, y] of pts) c.set(x, y, C.weld); };

// Loose tools, drawn on their own canvas and outlined.
function hammer(p, x, y) { p.rect(x, y, x + 6, y, WOOD[1]); p.set(x, y, WOOD[2]); p.rect(x + 7, y - 1, x + 8, y + 1, C.mid); p.rect(x + 7, y - 1, x + 7, y + 1, C.hi); p.set(x + 9, y - 1, C.mid); }
// a tin of bolts
function tin(p, x, y) { p.rect(x, y, x + 3, y + 2, RUST[1]); p.rect(x, y, x + 3, y, RUST[0]); p.set(x + 1, y - 1, C.hi); p.set(x + 2, y - 1, C.mid); }

// The bench of A and B: a hull plate on an I-beam and a leaning pipe on a brick, a plank shelf, a rebar brace.
function plateBench(c) {
  beamLeg(c, 3, 10, 15);
  pipeLeg(c, 24, 10, 13, 2);
  shim(c, 24, 30, 14);
  plank(c, 7, 25, 11, [9, 23]);
  for (let x = 8; x <= 23; x++) c.set(x, 15 - Math.floor((x - 8) / 8), C.dark2);
  plateTop(c, 1, 30, 4);
  weld(c, [[4, 10], [7, 10], [25, 10], [27, 10]]);
}

// The work: a bar on the top at row `top` - 1, a hacksaw on it shifted back and forth, filings off its right end.
function sawing(w, h, top, x, f) {
  const c = new Cv(w, h), p = new Cv(w, h);
  const s = [0, 2, 4, 2][f];
  p.rect(x, top - 1, x + 7, top - 1, C.steel); p.set(x, top - 1, C.hi);
  const b = x - 2 + s;
  p.rect(b, top - 2, b + 8, top - 2, C.hi);
  for (let k = b; k <= b + 8; k += 2) p.set(k, top - 2, C.mid2);
  p.rect(b, top - 4, b + 8, top - 4, C.dark2); p.set(b, top - 3, C.dark2); p.set(b + 8, top - 3, C.dark2);
  p.rect(b + 9, top - 4, b + 10, top - 2, RED[1]); p.set(b + 9, top - 4, RED[0]);
  p.outline();
  c.over(p);
  for (const [k, [dx, dy]] of [[0, 1], [1, 3], [0, 5], [1, 2]].entries()) {
    const y = top + ((dy + f * 2 + k) % 7);
    c.set(x + 8 + dx, y, 0xdfe4ea, 210 - (y - top) * 18);
  }
  return c;
}

const D = [];

D.push({
  id: 'plate', name: 'Лист на ломе', rise: 0,
  what: 'Кусок обшивки с приваренной латкой на ноге из двутавра и косой трубе на кирпиче ржавчины, полка из доски, распорка из арматуры; на листе банка болтов и молоток',
  rest() {
    const c = new Cv(32, 16), p = new Cv(32, 16);
    plateBench(c);
    tin(p, 2, 1); hammer(p, 22, 2);
    return c.over(p.outline());
  },
  work: (f) => sawing(32, 16, 4, 9, f),
});

D.push({
  id: 'board', name: 'С инструментальной стенкой', rise: 2,
  what: 'Тот же верстак, над ним тёмный щит из обшивки на уголках, на крючках пила, молоток, ключ и клещи; встаёт на 2 клетки выше футпринта, как тиски дугового верстака',
  rest() {
    const c = new Cv(32, 32), p = new Cv(32, 32), b = new Cv(32, 16), t = new Cv(32, 16);
    // The backboard on two angle posts, behind the bench.
    for (const x of [2, 27]) { c.box(x, 1, x + 2, 21, C.dark); c.rect(x + 1, 2, x + 1, 20, C.mid2); c.set(x + 1, 3, C.hi); }
    c.box(4, 3, 27, 20, 0x2c3440);
    c.rect(5, 4, 26, 4, 0x3a4655); c.rect(16, 5, 16, 19, 0x232a33);
    c.set(24, 18, RUST[3]); c.set(25, 18, RUST[2]); c.set(25, 19, RUST[3]);
    // A handsaw by its handle, teeth to the right.
    p.rect(6, 5, 9, 7, WOOD[1]); p.rect(6, 5, 9, 5, WOOD[0]); p.set(8, 6, C.line);
    for (let y = 8; y <= 16; y++) { const w = y < 11 ? 4 : y < 14 ? 3 : 2; p.rect(6, y, 5 + w, y, C.steel); p.set(6, y, C.hi); if (y % 2 === 0) p.set(5 + w, y, C.mid2); }
    // A hammer hung by its head.
    p.rect(11, 6, 15, 7, C.mid); p.rect(11, 6, 15, 6, C.hi); p.set(15, 7, C.dark);
    p.rect(13, 8, 13, 16, WOOD[1]); p.set(13, 16, WOOD[2]);
    // A wrench, open jaw up, a ring below.
    p.set(18, 6, C.steel); p.set(20, 6, C.mid); p.rect(18, 7, 20, 7, C.mid);
    p.rect(19, 8, 19, 14, C.mid); p.set(19, 8, C.steel);
    p.rect(18, 15, 20, 17, C.mid); p.set(19, 16, 0x2c3440); p.set(18, 15, C.steel);
    // Pliers with red grips.
    p.rect(23, 6, 24, 7, C.mid); p.set(23, 6, C.steel);
    p.rect(22, 8, 22, 16, RED[1]); p.rect(25, 8, 25, 16, RED[1]); p.set(22, 8, RED[0]); p.set(25, 8, RED[0]);
    p.set(23, 8, C.mid); p.set(24, 8, C.mid);
    c.over(p.outline());
    plateBench(b);
    tin(t, 2, 1);
    b.over(t.outline());
    return c.over(b, 0, 16);
  },
  work: (f) => new Cv(32, 32).over(sawing(32, 16, 4, 9, f), 0, 16),
});

D.push({
  id: 'crate', name: 'Доски на ящике', rise: 0,
  what: 'Толстая доска с латкой из стали на грузовом ящике слева и крестовине из труб справа, перемотанной проволокой; красная струбцина на краю, молоток на доске',
  rest() {
    const c = new Cv(32, 16), p = new Cv(32, 16);
    // The crate, its slats and its stencil.
    c.lit(1, 8, 13, 15, HULL);
    c.rect(2, 11, 12, 11, HULL[2]); c.rect(4, 12, 4, 14, HULL[2]); c.rect(10, 12, 10, 14, HULL[2]);
    c.rect(6, 12, 8, 13, HAZARD[1]); c.rect(6, 12, 8, 12, HAZARD[0]);
    // The crossed pipes, wound with wire where they cross.
    for (let k = 0; k <= 7; k++) for (const [x, y] of [[18 + k, 8 + k], [26 - k, 8 + k]]) { c.set(x, y, C.line); c.set(x + 1, y, C.steel); c.set(x + 2, y, C.mid2); c.set(x + 3, y, C.line); }
    c.rect(21, 11, 25, 12, RUST[1]); c.rect(21, 11, 25, 11, RUST[0]); c.set(23, 12, RUST[2]);
    // The top: a thick plank, a steel patch nailed across it.
    c.box(0, 3, 31, 7, WOOD[1]);
    c.rect(1, 4, 30, 4, WOOD[0]); c.rect(1, 6, 30, 6, WOOD[2]);
    for (const x of [5, 13, 26]) c.set(x, 5, WOOD[3]);
    c.box(15, 3, 22, 7, C.mid); c.rect(16, 4, 21, 4, C.steel); c.rect(16, 6, 21, 6, C.dark);
    for (const x of [16, 21]) c.set(x, 5, C.hi);
    for (const x of [2, 29]) c.set(x, 5, C.hi);
    // A clamp gripping the left edge.
    p.rect(1, 1, 3, 1, RED[1]); p.rect(1, 2, 1, 9, RED[1]); p.rect(1, 9, 3, 9, RED[1]); p.set(1, 1, RED[0]); p.rect(3, 10, 3, 11, C.mid);
    hammer(p, 4, 1);
    c.over(p.outline());
    return c;
  },
  work: (f) => sawing(32, 16, 3, 16, f),
});

module.exports = { D };
