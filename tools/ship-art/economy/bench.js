// The workbench candidates: a start station of raw salvage, unpowered, 4x2 cells, drawn 32x40 so the top stands at a
// hero's waist (24 px up) and rises 3 cells over the footprint, as the arc bench's vise does. Each is a rest picture and
// four work pictures of the same size, overlays of what moves: a hacksaw working a bar on the top, filings falling off it.
const { Cv, C, HULL, RUST, WOOD, RED, HAZARD } = require('./lib.js');

const W = 32, H = 40, TOP = 16;

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


// Hung tools, rows y..y+8: a handsaw by its handle, a hammer by its head, a wrench jaw up, pliers with red grips.
function hungTools(p, y) {
  p.rect(6, y, 9, y + 1, WOOD[1]); p.rect(6, y, 9, y, WOOD[0]); p.set(8, y + 1, C.line);
  for (let k = 0; k < 7; k++) { const w = k < 2 ? 4 : k < 5 ? 3 : 2; p.rect(6, y + 2 + k, 5 + w, y + 2 + k, C.steel); p.set(6, y + 2 + k, C.hi); if (k % 2 === 0) p.set(5 + w, y + 2 + k, C.mid2); }
  p.rect(11, y, 15, y + 1, C.mid); p.rect(11, y, 15, y, C.hi); p.set(15, y + 1, C.dark);
  p.rect(13, y + 2, 13, y + 8, WOOD[1]); p.set(13, y + 8, WOOD[2]);
  p.set(18, y, C.steel); p.set(20, y, C.mid); p.rect(18, y + 1, 20, y + 1, C.mid);
  p.rect(19, y + 2, 19, y + 6, C.mid); p.set(19, y + 2, C.steel);
  p.rect(18, y + 7, 20, y + 9, C.mid); p.set(19, y + 8, null); p.set(18, y + 7, C.steel);
  p.rect(23, y, 24, y + 1, C.mid); p.set(23, y, C.steel);
  p.rect(22, y + 2, 22, y + 8, RED[1]); p.rect(25, y + 2, 25, y + 8, RED[1]); p.set(22, y + 2, RED[0]); p.set(25, y + 2, RED[0]);
  p.set(23, y + 2, C.mid); p.set(24, y + 2, C.mid);
}

// A coil of wire, its centre at (x, y).
function coil(p, x, y) {
  for (const [dx, dy] of [[-1, -2], [0, -2], [1, -2], [-2, -1], [2, -1], [-2, 0], [2, 0], [-2, 1], [2, 1], [-1, 2], [0, 2], [1, 2]]) p.set(x + dx, y + dy, RUST[0]);
  p.set(x - 1, y - 2, 0xe8b07a); p.set(x - 2, y - 1, 0xe8b07a); p.set(x + 2, y + 1, RUST[2]); p.set(x + 1, y + 2, RUST[2]);
}

// A pipe 3 px wide from (x0, y0) to (x1, y1), steel between two lines.
function pipe(c, x0, y0, x1, y1) {
  for (let y = y0; y <= y1; y++) { const x = x0 + Math.round(((y - y0) * (x1 - x0)) / Math.max(1, y1 - y0)); c.set(x, y, C.line); c.set(x + 1, y, C.steel); c.set(x + 2, y, C.mid2); c.set(x + 3, y, C.line); }
}

// The tool board on two angle posts, behind the bench, its tools hung.
function toolBoard(c) {
  const p = new Cv(W, H);
  for (const x of [2, 27]) { c.box(x, 0, x + 2, TOP + 2, C.dark); c.rect(x + 1, 1, x + 1, TOP + 1, C.mid2); c.set(x + 1, 2, C.hi); }
  c.box(4, 1, 27, 14, 0x2c3440);
  c.rect(5, 2, 26, 2, 0x3a4655); c.rect(16, 3, 16, 13, 0x232a33);
  c.set(24, 12, RUST[3]); c.set(25, 12, RUST[2]); c.set(25, 13, RUST[3]);
  hungTools(p, 3);
  c.over(p.outline());
}

// The base: a locker torn from a wreck, on feet, the left door shut, the right one torn off.
function locker(c) {
  for (const x of [3, 25]) c.box(x, H - 3, x + 3, H - 1, C.dark2);
  c.lit(2, TOP + 6, 29, H - 3, HULL);
  c.lit(3, TOP + 7, 15, H - 4, HULL);
  c.rect(4, TOP + 12, 14, TOP + 13, HAZARD[1]); for (let x = 5; x <= 14; x += 3) c.set(x, TOP + 13, C.line);
  c.rect(13, TOP + 9, 13, TOP + 10, C.hi);
  c.rect(17, TOP + 7, 28, H - 4, C.deep);
  c.rect(17, TOP + 13, 28, TOP + 13, C.mid); c.rect(17, TOP + 14, 28, TOP + 14, C.dark2);
  for (const y of [TOP + 8, H - 6]) { c.set(16, y, C.mid); c.set(16, y + 1, C.dark); }
  tin(c, 19, TOP + 10); c.set(24, TOP + 12, C.mid);
  for (const [x, h] of [[19, 5], [22, 7], [25, 4]]) { c.rect(x, H - 4 - h, x + 1, H - 5, C.mid2); c.rect(x, H - 4 - h, x + 1, H - 4 - h, C.steel); }
}

const D = [];

D.push({
  id: 'tall', name: 'Высокий, со стенкой', rise: 3,
  what: 'Выбранный верстак, поднятый до пояса героя: лист обшивки на двутавре и косой трубе, полка внизу с банкой болтов и мотком проволоки, над листом щит с пилой, молотком, ключом и клещами',
  rest() {
    const c = new Cv(W, H), p = new Cv(W, H), t = new Cv(W, H);
    toolBoard(c);
    beamLeg(c, 3, TOP + 6, H - 1);
    pipeLeg(c, 24, TOP + 6, H - 3, 5);
    shim(c, 25, 31, H - 2);
    plank(c, 7, 25, 31, [9, 23]);
    for (let x = 8; x <= 23; x++) c.set(x, 39 - Math.floor((x - 8) / 3), C.dark2);
    for (let x = 8; x <= 23; x++) c.set(x, TOP + 6 + Math.floor((x - 8) / 4), C.dark2);
    plateTop(c, 1, 30, TOP);
    weld(c, [[4, TOP + 6], [7, TOP + 6], [25, TOP + 6], [27, TOP + 6], [7, 32], [25, 32]]);
    tin(t, 2, TOP - 3); tin(t, 10, 28); coil(t, 19, 28);
    return c.over(t.outline());
  },
  work: (f) => sawing(W, H, TOP, 9, f),
});

D.push({
  id: 'locker', name: 'Шкаф из обшивки', rise: 3,
  what: 'Низ из вырванного шкафчика: левая дверца с жёлтой полосой, правая сорвана, внутри полка с банкой и обрезки труб; сверху лист с латкой, над ним перфорированный щит с молотком, ключом, отвёртками и мотком',
  rest() {
    const c = new Cv(W, H), p = new Cv(W, H), t = new Cv(W, H);
    // The pegboard, its holes, two straps up out of the picture.
    for (const x of [6, 25]) c.rect(x, 0, x, 1, C.dark2);
    c.box(3, 1, 28, 13, 0x3a4655);
    for (let y = 3; y <= 11; y += 2) for (let x = 5; x <= 26; x += 3) c.set(x, y, 0x232a33);
    hammer(p, 5, 4);
    p.rect(17, 3, 17, 5, RED[1]); p.set(17, 3, RED[0]); p.rect(17, 6, 17, 10, C.steel);
    p.rect(19, 3, 19, 5, HAZARD[1]); p.set(19, 3, HAZARD[0]); p.rect(19, 6, 19, 9, C.steel);
    p.rect(21, 3, 23, 4, C.mid); p.set(22, 3, null); p.rect(22, 5, 22, 10, C.mid); p.set(22, 5, C.steel); p.rect(21, 11, 23, 11, C.mid);
    coil(p, 10, 9);
    c.over(p.outline());
    locker(c);
    plateTop(c, 1, 30, TOP);
    tin(t, 26, TOP - 3);
    return c.over(t.outline());
  },
  work: (f) => sawing(W, H, TOP, 9, f),
});

D.push({
  id: 'trestle', name: 'На козлах, с рейкой', rise: 3,
  what: 'Лист обшивки на двух козлах из труб, перемотанных проволокой, под ним половина бочки с ломом; над листом труба-рейка на стойках, на крюках молоток, пила, ключ, клещи и моток',
  rest() {
    const c = new Cv(W, H), p = new Cv(W, H);
    // The rail on two posts, the tools on hooks below it.
    for (const x of [1, 27]) { c.box(x, 1, x + 3, TOP + 1, C.dark); c.rect(x + 1, 2, x + 1, TOP, C.mid2); }
    c.rect(1, 2, 30, 4, C.line); c.rect(2, 3, 29, 3, C.steel); c.set(2, 3, C.hi);
    for (const x of [7, 13, 19, 23, 27]) c.set(x, 5, C.dark2);
    p.rect(6, 6, 9, 7, WOOD[1]); p.rect(6, 6, 9, 6, WOOD[0]); p.set(8, 7, C.line);
    for (let k = 0; k < 5; k++) { const w = k < 2 ? 4 : 3; p.rect(6, 8 + k, 5 + w, 8 + k, C.steel); p.set(6, 8 + k, C.hi); if (k % 2 === 0) p.set(5 + w, 8 + k, C.mid2); }
    p.rect(11, 6, 15, 7, C.mid); p.rect(11, 6, 15, 6, C.hi); p.rect(13, 8, 13, 12, WOOD[1]);
    p.rect(18, 6, 20, 7, C.mid); p.set(19, 6, null); p.rect(19, 8, 19, 11, C.mid); p.set(19, 8, C.steel);
    p.rect(22, 6, 24, 6, C.mid); p.rect(22, 7, 22, 11, RED[1]); p.rect(24, 7, 24, 11, RED[1]); p.set(22, 7, RED[0]);
    coil(p, 27, 8);
    c.over(p.outline());
    // The half barrel of scrap between the trestles.
    c.lit(11, H - 8, 20, H - 1, RUST);
    c.rect(12, H - 5, 19, H - 5, RUST[3]);
    for (const [x, y, k] of [[12, H - 10, C.mid], [14, H - 11, C.steel], [17, H - 10, HULL[1]], [18, H - 12, C.mid2]]) { c.rect(x, y, x + 1, H - 8, k); }
    // The trestles: two crossed pipes each, wound with wire where they cross.
    pipe(c, 1, TOP + 6, 7, H - 1); pipe(c, 7, TOP + 6, 1, H - 1);
    pipe(c, 21, TOP + 6, 27, H - 1); pipe(c, 27, TOP + 6, 21, H - 1);
    for (const x of [3, 23]) { c.rect(x, TOP + 14, x + 3, TOP + 16, RUST[1]); c.rect(x, TOP + 14, x + 3, TOP + 14, RUST[0]); }
    plateTop(c, 1, 30, TOP);
    weld(c, [[3, TOP + 6], [8, TOP + 6], [23, TOP + 6], [28, TOP + 6]]);
    return c;
  },
  work: (f) => sawing(W, H, TOP, 9, f),
});

D.unshift({
  id: 'boardLocker', name: 'Стенка над шкафом', rise: 3,
  what: 'Щит с пилой, молотком, ключом и клещами из «Высокого», под листом шкафчик из «Шкафа»: левая дверца с жёлтой полосой, правая сорвана, внутри полка с банкой и обрезки труб',
  rest() {
    const c = new Cv(W, H), t = new Cv(W, H);
    toolBoard(c);
    locker(c);
    plateTop(c, 1, 30, TOP);
    tin(t, 2, TOP - 3);
    return c.over(t.outline());
  },
  work: (f) => sawing(W, H, TOP, 9, f),
});

module.exports = { D };
