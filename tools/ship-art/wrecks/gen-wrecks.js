// Wreck kinds for Stillwake, drawn with debris-art/gen.js's own Art. node tools/ship-art/wrecks/gen-wrecks.js [outDir, default wrecks/out]; install.js copies them into game/art
// Writes wreck_<id>.png (1x), wreck_<id>_d1/_d2.png (the crack filter at 1/3 and 2/3 lost) and manifest.json.
const fs = require('fs'), path = require('path'), zlib = require('zlib');
const { readPng, png: pngW } = require('./pngio');
const src = fs.readFileSync(path.join(__dirname, '..', 'debris-v1', 'gen.js'), 'utf8');
const lib = src.slice(src.indexOf('// ---------- PNG'), src.indexOf('const pieces ='));
const { Art, M, make } = new Function('require', 'process', 'zlib', lib + '; return { Art, M, make, png, hex };')(require, { argv: [] }, zlib);
const out = process.argv[2] || path.join(__dirname, 'out'); fs.mkdirSync(out, { recursive: true });
const GAME = path.join(__dirname, '..', '..', '..', 'game', 'art');

// Palettes the wrecks add: light, base, dark, deep.
Object.assign(M, {
  o2:    ['#7cc0f0', '#3f8fd6', '#2c69a8', '#1d4878'],   // the canister's blue
  white: ['#e6ecf2', '#c2ccd6', '#939fad', '#677280'],
  lead:  ['#6e7580', '#525862', '#3b4048', '#292d33'],   // heavy shielding, darker than hull steel
  pcb:   ['#6fbf73', '#3f8f4f', '#2c6a3a', '#1c4626'],
  cont:  ['#7d9470', '#5d7553', '#45583e', '#30402c'],   // container paint: green, apart from the wood and the blue hull
});
const LED = { green: '#7dff8a', amber: '#f2b84b', red: '#ff5a4a' };

const art = {};

// ---------------- cargo container: a long corrugated box, its side torn open on planks stacked inside ----------------
const inPoly = (pts, x, y) => { let r = false; for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) { const [xi, yi] = pts[i], [xj, yj] = pts[j]; if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) r = !r; } return r; };
art.cargoContainer = make(62, 30, 201, (a, o) => {
  // the box, its far end bent and ragged
  a.poly([[o, o + 1], [o + 1, o], [o + 52, o], [o + 56, o + 2], [o + 54, o + 6], [o + 58, o + 9], [o + 55, o + 14], [o + 59, o + 18],
    [o + 55, o + 23], [o + 57, o + 28], [o + 54, o + 30], [o + 1, o + 30], [o, o + 29]], 'cont');
  a.rect(o, o, o + 53, o + 1, 'dark'); a.rect(o, o + 28, o + 54, o + 29, 'dark');
  // planks sliding out of the torn end
  for (const [y, x1] of [[9, 61], [13, 60], [19, 62]]) a.rect(o + 52, o + y, o + x1, o + y + 1, 'wood');
  a.tear(.05);
  a.shade(.04);
  // corrugation: a light and a dark column every four pixels
  for (let x = 3; x < 56; x += 4) for (let y = 2; y < 28; y++) {
    if (a.get(o + x, o + y) === 'cont') a.paint(o + x, o + y, M.cont[0]);
    if (a.get(o + x + 1, o + y) === 'cont') a.paint(o + x + 1, o + y, M.cont[2]);
  }
  // the hole torn in its side: dark inside, planks stacked in it, the torn metal's lit and shadowed rims
  const hole = [[o + 33, o + 4], [o + 40, o + 3], [o + 44, o + 6], [o + 50, o + 4], [o + 55, o + 8], [o + 54, o + 15], [o + 56, o + 21],
    [o + 51, o + 26], [o + 44, o + 24], [o + 38, o + 27], [o + 34, o + 22], [o + 36, o + 16], [o + 31, o + 11]];
  const inH = (x, y) => inPoly(hole, x + .5, y + .5);
  for (let y = 0; y < a.h; y++) for (let x = 0; x < a.w; x++) {
    if (!a.get(x, y) || !inH(x, y)) continue;
    const row = (y - o - 6) % 4, plank = y > o + 5 && y < o + 25;
    a.paint(x, y, !plank ? '#1b1f26' : row === 0 ? M.wood[0] : row === 3 ? '#1b1f26' : row === 2 ? M.wood[2] : M.wood[1]);
    if (plank && row === 1 && (x * 7 + y) % 9 === 0) a.paint(x, y, M.wood[3]);
  }
  for (let y = 0; y < a.h; y++) for (let x = 0; x < a.w; x++) {
    if (!a.get(x, y) || inH(x, y)) continue;
    if (inH(x + 1, y) || inH(x, y + 1)) a.paint(x, y, M.cont[0]);      // the lip catching light, up and left of the hole
    else if (inH(x - 1, y) || inH(x, y - 1)) a.paint(x, y, M.cont[3]); // the far lip in shadow
  }
  // the door end: two locking bars with handles; corner castings
  for (const x of [4, 9]) { for (let y = 3; y < 27; y++) a.paint(o + x, o + y, M.steel[y % 6 ? 1 : 0]); a.paint(o + x + 1, o + 14, M.steel[0]); a.paint(o + x + 2, o + 14, M.steel[2]); }
  for (const [x, y] of [[1, 0], [1, 28], [51, 0], [52, 28]]) { a.paint(o + x, o + y, M.dark[3]); a.paint(o + x + 1, o + y, M.dark[3]); }
  // stencil half scraped off, rust, the planks' ends
  for (const [x, y] of [[15, 6], [16, 6], [17, 6], [15, 7], [15, 8], [16, 8], [17, 8], [20, 6], [20, 7], [20, 8], [21, 8], [22, 6], [22, 7], [22, 8], [25, 6], [26, 6], [26, 7], [26, 8]]) a.paint(o + x, o + y, M.white[2]);
  for (const [y, x1] of [[9, 61], [13, 60], [19, 62]]) { a.paint(o + x1, o + y, M.wood[3]); a.paint(o + x1, o + y + 1, M.wood[3]); }
  a.spots(7, 'rust', 3);
});

// ---------------- machinery block: an engine housing with its big gear and a piston torn out ----------------
art.machineBlock = make(46, 30, 301, (a, o) => {
  // the gear first, half hidden by the housing
  const gx = o + 31, gy = o + 11;
  a.ring(gx, gy, 3, 8.6, 'steel');
  for (let k = 0; k < 12; k++) { const t = k * Math.PI / 6 + .2, cx = gx + Math.cos(t) * 9.6, cy = gy + Math.sin(t) * 9.6; a.rect(Math.round(cx - .6), Math.round(cy - .6), Math.round(cx + .6), Math.round(cy + .6), 'steel'); }
  a.disc(gx, gy, 3, 'grime');
  // the housing: a dark block with a chamfered top and a broken right side
  a.poly([[o, o + 6], [o + 5, o + 1], [o + 24, o + 1], [o + 26, o + 6], [o + 25, o + 13], [o + 29, o + 17], [o + 28, o + 30], [o, o + 30]], 'dark');
  // a piston and its cylinder, pushed out on the right
  a.rect(o + 26, o + 20, o + 37, o + 26, 'grime'); a.rect(o + 37, o + 22, o + 43, o + 24, 'steel'); a.rect(o + 42, o + 20, o + 45, o + 26, 'steel');
  a.tear(.08);
  a.shade(.05);
  a.spots(5, 'rust', 2);
  // housing details: a vent grille, bolts, a seam, a hazard stripe
  for (let y = 5; y < 14; y += 2) for (let x = 4; x < 15; x++) { a.paint(o + x, o + y, M.dark[3]); a.paint(o + x, o + y + 1, M.dark[0]); }
  for (const [x, y] of [[2, 4], [21, 4], [2, 27], [22, 27], [18, 17], [8, 17]]) a.rivet(o + x, o + y);
  a.seamH(o + 1, o + 25, o + 15, 'dark');
  for (let y = 22; y < 26; y++) for (let x = 2; x < 22; x++) if (((x + y) >> 1) % 2 === 0) a.paint(o + x, o + y, M.yellow[1]);
  for (let x = 27; x < 37; x += 3) a.seamV(o + x, o + 21, o + 25, 'grime');
  // the gear's hub and spokes' bolts, a lit rim where light catches the teeth
  a.disc(gx, gy, 1.2, 'dark'); a.paint(gx - 1, gy - 1, M.steel[0]);
  for (let k = 0; k < 4; k++) { const t = k * Math.PI / 2 + .6; a.paint(gx + Math.cos(t) * 5.5, gy + Math.sin(t) * 5.5, M.steel[3]); }
  // a cable ripped out of the top of the housing
  a.wire([[o + 12, o + 1], [o + 11, o - 1], [o + 8, o - 1]], '#d0463a');
});

// ---------------- cable rack: an electronics cabinet, boards pulled half out, cables spilling ----------------
art.cableRack = make(30, 30, 401, (a, o) => {
  a.rect(o + 1, o, o + 19, o + 29, 'dark');
  a.erase(o + 19, o); a.erase(o + 18, o); a.erase(o + 19, o + 1);
  // a board slid half out to the right
  a.rect(o + 18, o + 12, o + 26, o + 16, 'pcb');
  a.shade(.03);
  // rack units: faces with slots and LEDs
  const units = [[3, 'paint'], [8, 'grime'], [17, 'paint'], [22, 'grime']];
  for (const [y, mat] of units) {
    for (let yy = y; yy < y + 4; yy++) for (let x = 3; x < 18; x++) a.paint(o + x, o + yy, M[mat][yy === y ? 0 : yy === y + 3 ? 2 : 1]);
    a.paint(o + 4, o + y + 1, M.steel[0]); a.paint(o + 16, o + y + 1, M.steel[0]);
    for (let x = 7; x < 14; x++) a.paint(o + x, o + y + 2, M.dark[3]);
  }
  // the empty bay the board came out of
  for (let y = 12; y < 17; y++) for (let x = 3; x < 18; x++) a.paint(o + x, o + y, '#1b1f26');
  a.paint(o + 14, o + 4, LED.green); a.paint(o + 12, o + 4, LED.green); a.paint(o + 14, o + 9, LED.amber); a.paint(o + 14, o + 18, LED.red);
  // the board's chips and traces
  for (const x of [20, 23]) { a.paint(o + x, o + 13, '#1b1d22'); a.paint(o + x + 1, o + 13, '#1b1d22'); a.paint(o + x, o + 14, '#1b1d22'); a.paint(o + x + 1, o + 14, '#1b1d22'); }
  for (let x = 19; x < 27; x += 2) a.paint(o + x, o + 16, M.yellow[0]);
  // rails down the sides with their holes
  for (let y = 1; y < 29; y += 3) { a.paint(o + 2, o + y, M.dark[3]); a.paint(o + 18, o + y, M.dark[3]); }
  // cables spilling out of the bottom and the bay, looping off to the right
  a.wire([[o + 17, o + 27], [o + 21, o + 28], [o + 25, o + 26], [o + 27, o + 22], [o + 29, o + 24]], '#d0463a');
  a.wire([[o + 17, o + 26], [o + 20, o + 24], [o + 24, o + 23], [o + 26, o + 19], [o + 28, o + 18]], '#e0b030');
  a.wire([[o + 18, o + 28], [o + 22, o + 30], [o + 27, o + 29], [o + 30, o + 27]], '#3a7bd0');
  a.wire([[o + 19, o + 10], [o + 23, o + 9], [o + 26, o + 6], [o + 29, o + 7]], M.copper[1]);
  a.wire([[o + 19, o + 7], [o + 22, o + 5], [o + 24, o + 2], [o + 27, o + 3]], '#d0463a');
  a.spots(3, 'grime', 2);
});

// ---------------- oxygen tanks: three blue cylinders in a strap frame, the middle one dented ----------------
art.tankCluster = make(30, 30, 501, (a, o) => {
  // the frame's base plate and its two straps
  a.rect(o, o + 26, o + 29, o + 29, 'dark');
  const tanks = [[1, 5, 8], [11, 2, 8], [21, 8, 8]];   // x, top, width
  for (const [x, top, w] of tanks) {
    a.rect(o + x, o + top + 1, o + x + w - 1, o + 26, 'o2');
    a.rect(o + x + 1, o + top, o + x + w - 2, o + top, 'o2');
    a.rect(o + x + 3, o + top - 1, o + x + 4, o + top - 1, 'steel'); a.rect(o + x + 2, o + top - 2, o + x + 5, o + top - 2, 'steel');
  }
  a.rect(o, o + 12, o + 29, o + 13, 'dark'); a.rect(o, o + 21, o + 29, o + 22, 'dark');
  // the third tank's valve wheel, knocked sideways
  a.ring(o + 27.5, o + 5, .8, 2.2, 'orange');
  a.shade(.03);
  // a lit stripe down each tank, the canister's white label, rivets on the straps
  for (const [x, top, w] of tanks) {
    for (let y = top + 1; y < 26; y++) if (a.get(o + x + 2, o + y) === 'o2') a.paint(o + x + 2, o + y, M.o2[0]);
    for (let y = top + 1; y < 26; y++) if (a.get(o + x + 6, o + y) === 'o2') a.paint(o + x + 6, o + y, M.o2[2]);
    for (let yy = 16; yy < 20; yy++) for (let xx = x + 2; xx < x + w - 2; xx++) a.paint(o + xx, o + yy, M.white[yy === 16 ? 0 : 1]);
    a.paint(o + x + 3, o + 18, M.o2[1]); a.paint(o + x + 4, o + 18, M.o2[1]);
  }
  for (let x = 2; x < 29; x += 5) { a.rivet(o + x, o + 12); a.rivet(o + x, o + 21); }
  for (let x = 1; x < 29; x += 3) a.paint(o + x, o + 27, M.dark[3]);
  // a dent in the middle tank
  for (const [x, y] of [[14, 6], [15, 7], [14, 7], [15, 8], [16, 8]]) a.paint(o + x, o + y, M.o2[3]);
  for (const [x, y] of [[13, 6], [14, 5], [15, 6]]) a.paint(o + x, o + y, M.o2[2]);
  a.spots(3, 'grime', 2);
});

// ---------------- reactor shielding: a heavy layered slab, huge bolts, a corner torn off showing its strata ----------------
art.shieldSlab = make(62, 30, 601, (a, o) => {
  a.poly([[o + 5, o], [o + 44, o], [o + 47, o + 3], [o + 51, o + 2], [o + 53, o + 6], [o + 57, o + 7], [o + 59, o + 11], [o + 62, o + 12], [o + 62, o + 27], [o + 59, o + 30], [o + 8, o + 30], [o + 5, o + 26], [o + 2, o + 24],
    [o + 4, o + 20], [o, o + 17], [o + 3, o + 13], [o + 1, o + 9], [o + 4, o + 5], [o + 2, o + 2]], 'lead');
  a.shade(.06);
  // the slab's thickness: a lit top bevel, a dark foot
  for (let x = 0; x < a.w; x++) for (let y = 0; y < a.h; y++) if (a.get(x, y)) { if (!a.get(x, y - 1)) { a.paint(x, y, M.lead[0]); a.paint(x, y + 1, M.steel[2]); } }
  // the strata at the torn edges: steel, lead, rust layers, three pixels deep
  const strata = ['steel', 'lead', 'rust2', 'lead'];
  const torn = (x, y) => (x < o + 9) || (x > o + 44 && y < o + 13);
  for (let y = 0; y < a.h; y++) for (let x = 0; x < a.w; x++) {
    if (!a.get(x, y) || !torn(x, y)) continue;
    let d = 9; for (const [dx, dy] of [[-1, 0], [1, 0], [0, -1], [-2, 0], [2, 0], [0, -2], [-3, 0], [0, -3]]) if (!a.get(x + dx, y + dy)) d = Math.min(d, Math.abs(dx) + Math.abs(dy));
    if (d > 3) continue;
    const layer = x < o + 9 ? (y >> 1) : (x >> 1);
    a.paint(x, y, M[strata[layer % strata.length]][d === 1 ? 0 : d === 2 ? 1 : 2]);
  }
  // plate seams
  a.seamV(o + 22, o + 2, o + 25, 'lead'); a.seamV(o + 42, o + 2, o + 25, 'lead');
  // a hazard band along the bottom
  for (let y = 26; y < 30; y++) for (let x = 6; x < 61; x++) if (a.get(o + x, o + y)) a.paint(o + x, o + y, ((x + y) >> 2) % 2 ? M.yellow[1] : '#23272e');
  a.seamH(o + 6, o + 60, o + 25, 'lead');
  // huge bolts: 3x3 heads
  for (const x of [12, 32, 52]) for (const y of [5, 19]) {
    if (x === 52 && y === 5) continue;
    const c = [[0, 0, 0], [0, 1, 1], [1, 1, 2], [1, 2, 3]];
    for (let dy = 0; dy < 3; dy++) for (let dx = 0; dx < 3; dx++) a.paint(o + x + dx, o + y + dy, M.steel[[0, 1, 1, 2, 3][dx + dy]]);
  }
  // a warning plate with the trefoil, drawn by hand
  const tre = ['............',
               '.XXX....XXX.',
               '.XXXX..XXXX.',
               '..XXX..XXX..',
               '....X..X....',
               '.....XX.....',
               '............',
               '.....XX.....',
               '....XXXX....',
               '...XXXXXX...'];
  for (let y = 0; y < 11; y++) for (let x = 0; x < 13; x++) a.paint(o + 26 + x, o + 7 + y, M.yellow[y === 0 || x === 0 ? 0 : y === 10 || x === 12 ? 2 : 1]);
  tre.forEach((row, y) => [...row].forEach((ch, x) => { if (ch === 'X') a.paint(o + 26 + x, o + 8 + y, '#23272e'); }));
  // scorch and pits
  a.spots(7, 'lead', 3);
  for (const [x, y] of [[48, 16], [49, 17], [15, 13], [56, 20], [55, 21]]) a.paint(o + x, o + y, M.lead[3]);
});

// ---------------- the crack filter, ported from game/render/Cracks.cs ----------------
const hash = x => { x >>>= 0; x ^= x >>> 16; x = Math.imul(x, 0x7feb352d) >>> 0; x ^= x >>> 15; x = Math.imul(x, 0x846ca68b) >>> 0; x ^= x >>> 16; return x >>> 0; };
const rnd = (seed, n) => (hash((Math.imul(seed, 0x9e3779b9) ^ hash(n)) >>> 0) & 0xffff) / 65535;
function crackMask(w, h, crack, seed) {
  const dark = new Uint8Array(w * h), lit = new Uint8Array(w * h); if (crack <= 0) return { dark, lit };
  const hit = [w * (0.3 + 0.4 * rnd(seed, 1)), h * (0.3 + 0.4 * rnd(seed, 2))];
  const longest = Math.max(w, h) * 0.5, start = rnd(seed, 3) * 6.2831853;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { const dx = x + .5 - hit[0], dy = y + .5 - hit[1]; if (dx * dx + dy * dy <= crack * crack * 4) dark[y * w + x] = 1; }
  for (let i = 0; i < 7; i++) {
    const grown = Math.min(1, Math.max(0, (crack - i * 0.11) / 0.3)); if (grown <= 0) break;
    const n = i * 16 + 8; let angle = start + i * 2.3999632 + (rnd(seed, n) - .5) * .5;
    const steps = Math.trunc(longest * (0.45 + 0.55 * rnd(seed, n + 1)) * grown);
    let at = [...hit]; if (i >= 3) { at[0] += Math.cos(angle + 2) * longest * .35 * rnd(seed, n + 2); at[1] += Math.sin(angle + 2) * longest * .35 * rnd(seed, n + 2); }
    for (let s = 0; s < 40 && s < steps; s++) {
      const px = Math.floor(at[0]), py = Math.floor(at[1]);
      if (px >= 0 && py >= 0 && px < w && py < h) dark[py * w + px] = 1;
      if (px >= 0 && py + 1 >= 0 && px < w && py + 1 < h) lit[(py + 1) * w + px] = 1;
      angle += (rnd(seed, n + 3 + s) - .5) * .9; at[0] += Math.cos(angle); at[1] += Math.sin(angle);
    }
  }
  return { dark, lit };
}
function cracked(img, crack, seed) {
  const { w, h } = img, px = Buffer.from(img.px), { dark, lit } = crackMask(w, h, crack, seed);
  for (let i = 0; i < w * h; i++) {
    if (!px[i * 4 + 3]) continue; const c = [px[i * 4], px[i * 4 + 1], px[i * 4 + 2]].map(v => v / 255);
    let r = c;
    if (dark[i]) r = c.map((v, k) => v * .15 + [0.08, 0.09, 0.12][k] * .85);
    else if (lit[i]) r = c.map(v => Math.min(1, v * 1.35 + .04));
    for (let k = 0; k < 3; k++) px[i * 4 + k] = Math.round(r[k] * 255);
  }
  return { w, h, px };
}

// ---------------- the future circuit's item icon, 16x16 ----------------
const circuit = new Art(16, 16, 9);
{ const a = circuit; a.rect(2, 4, 13, 11, 'pcb'); a.shade(0);
  for (let x = 3; x < 13; x += 2) { a.put(x, 3, 'yellow'); a.put(x, 12, 'yellow'); }
  a.shade(0); for (let x = 3; x < 13; x += 2) { a.paint(x, 3, M.yellow[0]); a.paint(x, 12, M.yellow[1]); }
  a.rect(5, 6, 8, 9, 'pcb'); for (let y = 6; y <= 9; y++) for (let x = 5; x <= 8; x++) a.paint(x, y, y === 6 ? '#3a3f48' : '#23272e');
  a.paint(10, 6, '#23272e'); a.paint(11, 6, '#23272e'); a.paint(10, 9, M.copper[0]); a.paint(11, 9, M.copper[0]); a.paint(3, 7, M.yellow[0]); a.paint(3, 8, M.yellow[0]); }
fs.writeFileSync(path.join(out, 'item_circuit.png'), pngW(16, 16, circuit.rgba()));

// ---------------- write ----------------
const ids = ['hullSection', 'cargoContainer', 'machineBlock', 'cableRack', 'tankCluster', 'shieldSlab'];
const seeds = { hullSection: 7, cargoContainer: 21, machineBlock: 5, cableRack: 13, tankCluster: 34, shieldSlab: 3 };
const manifest = [];
for (const id of ids) {
  const img = id === 'hullSection' ? readPng(GAME + '/debris/section_01.png') : { w: art[id].w, h: art[id].h, px: art[id].rgba() };
  const files = [0, 1 / 3, 2 / 3].map((c, k) => { const f = `wreck_${id}${k ? '_d' + k : ''}.png`; fs.writeFileSync(path.join(out, f), pngW(img.w, img.h, c ? cracked(img, c, seeds[id]).px : img.px)); return f; });
  manifest.push({ id, w: img.w, h: img.h, files });
}
fs.writeFileSync(path.join(out, 'manifest.json'), JSON.stringify(manifest, null, 1));
console.log(manifest.map(m => `${m.id} ${m.w}x${m.h}`).join(', '));
