// The outside parts of abandoned vessels, as picked on the derelict-looks mockup (docs/plans/derelict-looks-mockup.html, drawPart).
// Each stands outside a hull, against space: lit from the upper left by the star, worn and scorched, never repaired.
(function () {
'use strict';
const { ell, poly, rng } = Anime;

const INK = '#0e1018';
const STEEL = { base: '#555a63', shade: '#3b3f47', deep: '#282a31', lite: '#757b86', spec: '#a0a6ae', ink: INK };
const GUN = { base: '#474c5a', shade: '#2e3140', deep: '#1e202c', lite: '#6a7080', spec: '#a0a8b8', ink: INK };
const OLD = { base: '#61646c', shade: '#44464f', deep: '#30323a', lite: '#83858a', spec: '#a2a3a6', ink: INK };
const COPPER = { base: '#7e5a3e', shade: '#563c2c', deep: '#3a2820', lite: '#a07a54', spec: '#c49e76', ink: INK };
/** The heat tint a burn left on the bell: brass by the throat, violet-blue further out. */
const BRONZE = { base: '#8a693f', shade: '#5e4630', deep: '#3e2e22', lite: '#9c7c52', spec: '#c9a878', ink: INK };
const BRONZE2 = { base: '#6e5a3f', shade: '#4c3e2e', deep: '#322a22', lite: '#8a7454', spec: '#ab9470', ink: INK };
const VIOLET = { base: '#4f4a6b', shade: '#36334c', deep: '#252336', lite: '#625e80', spec: '#8f8aac', ink: INK };
const VIOLET2 = { base: '#433f58', shade: '#2e2b3e', deep: '#1f1d2b', lite: '#5b5674', spec: '#7f7a98', ink: INK };
const SCORCH = ['#393b41', '#2a2b30'];
const RUST = ['#6a4630', '#3f2a24'];

const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .42), '#2c2f3e'); a.flat(ell(x - .08, y - .1, .2), '#c2c8d0'); } };
/** A torn patch with a darker heart, its edge by chance: rust where a seam held water, soot where the burn licked back. */
function patch(a, x, y, rx, ry, [edge, heart], clip, seed) {
  const r = rng(seed), blob = (k) => { const p = []; for (let i = 0; i < 11; i++) { const t = (i / 11) * Math.PI * 2, q = k * (.66 + r() * .58); p.push(x + Math.cos(t) * rx * q, y + Math.sin(t) * ry * q); } return poly(p); };
  a.flat(blob(1), edge, clip ? { clip } : undefined);
  a.flat(blob(.52), heart, clip ? { clip } : undefined);
}

/** machine_vesselNozzle: the main bell of a dead vessel, 8 × 7 cells, its throat bolted to the hull on the right, its mouth on the left. */
function nozzle(a) {
  const cy = 28, N = 53, E = 5.2, H0 = 10, H1 = 26.8, RX = 4.4;
  // The mockup's profile, h = 1.25 + 2.15·t^1.7 cells, from the neck (t 0) to the mouth (t 1).
  const h = (x) => H0 + (H1 - H0) * Math.pow(Math.max(0, Math.min(1, (N - x) / (N - E))), 1.7);
  /** A strip of the bell between x0 and x1, from k0 to k1 of its half-height (−1 its top, 1 its foot). */
  const zone = (x0, x1, k0, k1) => {
    const top = [], foot = [];
    for (let i = 0; i <= 16; i++) { const x = x0 + (x1 - x0) * i / 16; top.push(x, cy + k0 * h(x)); foot.unshift(x, cy + k1 * h(x)); }
    for (let i = 0; i < foot.length; i += 2) top.push(foot[i], foot[i + 1]);
    return poly(top);
  };
  const lit = (x0, x1, m, o) => a.shape(zone(x0, x1, -1, 1), m, { outer: false, edge: false, lite: zone(x0, x1, -1.1, -.52), shade: zone(x0, x1, .24, 1.1), deep: zone(x0, x1, .68, 1.1), ...o });

  // The collar the throat bolts through, flaring to the hull; its bracket and a feed line into the hull.
  a.shape(poly(52.6, cy - 10.6, 64, cy - 12.6, 64, cy + 12.6, 52.6, cy + 10.6), GUN, {
    lite: poly(52.6, cy - 10.6, 64, cy - 12.6, 64, cy - 9.4, 52.6, cy - 8.2), shade: band(52, cy + 5, 64, cy + 13), deep: band(52, cy + 9, 64, cy + 13) });
  a.tube(`M59.4 ${cy - 15}L59.4 ${cy - 18.4}Q59.4 ${cy - 20.4} 61.4 ${cy - 20.4}L64.6 ${cy - 20.4}`, 1.5, COPPER);
  a.shape(band(56.8, cy - 15.2, 61.8, cy + 15.2), OLD, {
    lite: band(56.8, cy - 15.2, 61.8, cy - 13.8), shade: [band(60.6, cy - 15.2, 61.8, cy + 15.2), band(56.8, cy + 8, 61.8, cy + 15.2)], deep: band(56.8, cy + 13.4, 61.8, cy + 15.2),
    lines: [`M56.8 ${cy - 10.8}L61.8 ${cy - 10.8}`, `M56.8 ${cy + 10.8}L61.8 ${cy + 10.8}`] });
  rivets(a, [58.8, cy - 13], [58.8, cy + 13], [58.8, cy - 4.6], [58.8, cy + 4.6]);
  patch(a, 59.6, cy + 12.4, 1.3, .9, RUST, band(56.8, cy - 15.2, 61.8, cy + 15.2), 7);

  // The bell, its outline round the whole, then the bands the heat left on it.
  const bell = zone(E, N, -1, 1);
  a.shape(bell, STEEL, { lite: zone(E, N, -1.1, -.52), shade: zone(E, N, .24, 1.1), deep: zone(E, N, .68, 1.1) });
  // The mockup's bands, scaled from its 7.7-cell bell to this one.
  lit(40.6, 51.8, BRONZE);
  lit(35, 40.6, BRONZE2);
  lit(25.7, 35, VIOLET);
  lit(21.3, 25.7, VIOLET2);
  // The tubes it was brazed from, along its length, and the hoops round it.
  for (const k of [-.4, .5]) a.line('M' + Array.from({ length: 17 }, (_, i) => { const x = E + 4 + (N - 6 - E) * i / 16; return `${x.toFixed(2)} ${(cy + k * h(x)).toFixed(2)}`; }).join('L'), { w: .5 });
  for (const x of [13.8, 33]) a.line(`M${x} ${cy - h(x) * .98}L${x + .2} ${cy + h(x) * .98}`, { w: 1.2 });
  // The ring at the neck, against the collar.
  a.shape(poly(50.6, cy - 11.4, 54.2, cy - 11.4, 54.2, cy + 11.4, 50.6, cy + 11.4), OLD, {
    lite: band(50.6, cy - 11.4, 54.2, cy - 6.4), shade: band(50.6, cy + 3, 54.2, cy + 11.4), deep: band(50.6, cy + 8, 54.2, cy + 11.4) });
  rivets(a, [52.4, cy - 9], [52.4, cy + 9]);

  // Soot where the burn licked back over the lip, a crack or two, worn bare at the rim.
  for (const [x, y, rx, ry, s] of [[12.6, cy + 19, 5.6, 2.4, 3], [11.4, cy - 20.6, 4.6, 1.6, 5], [10.6, cy + 7, 3.4, 1.4, 11]]) patch(a, x, y, rx, ry, SCORCH, bell, s);
  a.line(`M21.6 ${cy - 17.6}L23.4 ${cy - 15.4}L23 ${cy - 13.8}`);
  a.line(`M33 ${cy + 13.2}L34.4 ${cy + 14.6}`);

  // The mouth: its lip, then the dark inside, a thin light along the lit rim.
  a.shape(ell(E, cy, RX, H1), STEEL, { lite: band(E - RX - 1, cy - H1 - 1, E + RX + 1, cy - H1 * .45), shade: .9 });
  a.flat(ell(E + .6, cy + .3, RX - 1.5, H1 - 1.6), '#121419');
  a.flat(ell(E + 1.4, cy + 1, RX - 3, H1 - 6.4), '#0b0c10');
  a.line(`M${E - 1.6} ${cy - H1 + 2.4}Q${E - 2.6} ${cy - 12} ${E - 2.4} ${cy + 2}`, { c: '#7d838c', w: .8 });
}

// The mockup's part metals, light to dark, and the paint of what is not bare metal.
const LIGHT = { base: '#69717c', shade: '#4e555f', deep: '#3a4048', lite: '#868d97', spec: '#b0b6be', ink: INK };
const MID = { base: '#4e5560', shade: '#3a4048', deep: '#2a2e35', lite: '#666d78', spec: '#9aa1aa', ink: INK };
const DARK = { base: '#3c424b', shade: '#2c3138', deep: '#1f2328', lite: '#525963', spec: '#8a9099', ink: INK };
const BLOCK = { base: '#7f868f', shade: '#5d636b', deep: '#43484f', lite: '#9aa1a9', spec: '#c0c5cb', ink: INK };
const PANEL = { base: '#a9ada9', shade: '#868b87', deep: '#656a66', lite: '#c4c7c3', spec: '#eceeea', ink: INK };
const CELLS = { base: '#26335a', shade: '#1b2542', deep: '#121a30', lite: '#34447a', spec: '#7f93c8', ink: INK };
const TANK = { base: '#a4977a', shade: '#7a6f58', deep: '#574f3f', lite: '#bcb092', spec: '#d8cdb0', ink: INK };
const COLLAR = { base: '#6f757d', shade: '#52575e', deep: '#3b3f45', lite: '#878d95', spec: '#a8adb4', ink: INK };
const CONE = { base: '#3a3e45', shade: '#2a2d33', deep: '#1c1e22', lite: '#4c5058', spec: '#80858d', ink: INK };
/** The red beacon's glass and the strobe's, dark: the game's light is what shines through them. */
const RED = { base: '#5a201c', shade: '#3e1512', deep: '#2a0e0c', lite: '#7a3029', spec: '#c27a6e', ink: INK };
const CLEAR = { base: '#8e9498', shade: '#6a7074', deep: '#4c5155', lite: '#b4babd', spec: '#e6eaec', ink: INK };
const OCHRE = '#b8843a';
/** A solid lit from the upper left: its top a lit strip, its foot in shade. */
const lit = (a, x0, y0, x1, y1, m, o) => a.shape(band(x0, y0, x1, y1), m, { lite: band(x0, y0, x1, y0 + Math.min(1.6, (y1 - y0) * .3)), shade: band(x0, y1 - Math.min(2.4, (y1 - y0) * .35), x1, y1), ...o });
/** A thruster's bell, its throat at (x, y), opening toward (dx, dy) over its length. */
function cone(a, x, y, dx, dy, half) {
  const nx = -dy, ny = dx;
  a.shape(poly(x + nx * half * .4, y + ny * half * .4, x + dx + nx * half, y + dy + ny * half, x + dx - nx * half, y + dy - ny * half, x - nx * half * .4, y - ny * half * .4), CONE, { shade: .8 });
}

/** machine_vesselRcs: a quad of manoeuvring thrusters, 3 × 2 cells, a bell each way: it stands so at every corner of the hull. */
function rcs(a) {
  cone(a, 8.4, 4, 0, -3.4, 1.8); cone(a, 15.6, 4, 0, -3.4, 1.8);
  cone(a, 8.4, 12, 0, 3.4, 1.8); cone(a, 15.6, 12, 0, 3.4, 1.8);
  cone(a, 4, 8, -3.6, 0, 2.2); cone(a, 20, 8, 3.6, 0, 2.2);
  lit(a, 4, 4, 20, 12, BLOCK);
  a.flat(band(4.6, 7.2, 19.4, 8.6), OCHRE);
  rivets(a, [6, 10.4], [18, 10.4]);
  patch(a, 17, 5.6, 1.6, .8, RUST, band(4, 4, 20, 12), 13);
}

/** machine_vesselMast, machine_vesselMastTall: an aerial on a foot plate, three cross-bars, a guy wire, the red beacon's glass on top. */
function mast(H) {
  return (a) => {
    const ax = 8, top = 9, foot = H - 4.8;
    a.line(`M9.6 ${top + 10}L15.2 ${top + 2}`, { c: '#7d848d', w: .8 });
    lit(a, 6.4, top, 9.6, foot, LIGHT, { lite: band(6.4, top, 7.4, foot), shade: band(8.6, top, 9.6, foot) });
    for (let k = 1; k <= 3; k++) {
      const y = H - (H - top) * k / 4, half = 6.2 - k * .8;
      a.tube(`M${ax - half} ${y}L${ax + half} ${y}`, 1.1, MID);
    }
    lit(a, 1, foot, 15, H, MID);
    rivets(a, [3.4, H - 2.2], [12.6, H - 2.2]);
    lit(a, 4, top - 3.6, 12, top + 1, DARK);
    a.shape(ell(ax, top - 4.6, 2.6, 2.2), RED, { lite: ell(ax - .9, top - 5.5, .9, .6), shade: .8 });
    patch(a, 7.8, foot - 6, .8, 1.6, RUST, band(6.4, top, 9.6, foot), 17);
  };
}

/** machine_vesselDish: the comms dish on its post, a shell like a raised umbrella, its feed under it. */
function dish(a) {
  const dx = 17, base = 40, cx = dx + 3.2, cy = 25;
  lit(a, dx - 1.6, cy, dx + 1.6, base - 3, MID);
  lit(a, dx - 6, base - 3, dx + 6, base, MID);
  a.tube(`M${dx} ${cy}L${cx} ${cy - 11.6}`, 1, MID);
  a.shape(ell(cx, cy - 6, 1.8, 1.5), DARK, { shade: .6 });
  const shell = [];
  for (let i = 0; i <= 16; i++) { const t = -Math.PI * .95 + i / 16 * Math.PI * .9; shell.push(cx + Math.cos(t) * 18.4, cy + Math.sin(t) * 16.6); }
  for (let i = 16; i >= 0; i--) { const t = -Math.PI * .95 + i / 16 * Math.PI * .9; shell.push(cx + Math.cos(t) * 15.4, cy + 1.2 + Math.sin(t) * 13.4); }
  const d = poly(shell);
  a.shape(d, PANEL, { shade: 1.1, deep: .4 });
  for (const t of [-.72, -.5, -.28]) { const u = Math.PI * t; a.line(`M${cx + Math.cos(u) * 15.6} ${cy + 1.2 + Math.sin(u) * 13.6}L${cx + Math.cos(u) * 18.2} ${cy + Math.sin(u) * 16.4}`, { w: .6 }); }
  patch(a, cx - 9, cy - 11, 2.4, 1, RUST, d, 19);
}

/** machine_vesselSolar: a panel of cells on two legs and a brace, a glint across its glass. */
function solar(a) {
  const py = 6, ph = 22, base = 48;
  for (const x of [19, 66]) { lit(a, x, py + ph, x + 3.6, base - 2.4, MID); lit(a, x - 2.4, base - 2.4, x + 6, base, DARK); }
  a.tube(`M21 ${base - 3}L67 ${py + ph + 2}`, 1, MID);
  const d = band(.6, py, 87.4, py + ph);
  a.shape(d, CELLS, { lite: band(.6, py, 87.4, py + 1.4), shade: band(.6, py + ph - 2.4, 87.4, py + ph) });
  for (let x = 8; x < 88; x += 8) a.line(`M${x} ${py + .6}L${x} ${py + ph - .6}`, { c: '#3d4c7c', w: .6 });
  a.line(`M1 ${py + ph / 2}L87 ${py + ph / 2}`, { c: '#3d4c7c', w: .6 });
  a.flat(poly(48, py + 1.4, 58, py + 1.4, 54, py + ph - 1, 44, py + ph - 1), 'rgba(160,190,240,.16)', { clip: d });
  patch(a, 6, py + ph - 3, 3, 1.4, SCORCH, d, 23);
}

/** machine_vesselTank: a tank slung under the belly on two straps, the livery's band round it. */
function tank(a) {
  const y0 = 4, h = 20;
  for (const x of [24, 99]) lit(a, x, 0, x + 4.8, y0 + 2, MID);
  const d = Anime.rr(.6, y0, 126.8, h - .6, (h - .6) / 2);
  a.shape(d, TANK, { lite: band(0, y0, 128, y0 + 3), shade: 1.6, deep: .5 });
  for (const x of [23.2, 98.2]) a.flat(band(x, y0, x + 6.4, y0 + h), '#5c6068', { clip: d });
  a.flat(band(51.2, y0, 56, y0 + h), OCHRE, { clip: d });
  rivets(a, [26.4, y0 + 10], [101.4, y0 + 10]);
  patch(a, 82, y0 + 15, 4, 1.6, RUST, d, 29);
  patch(a, 12, y0 + 5, 2.6, 1.2, RUST, d, 31);
}

/** machine_vesselRadiator: a radiator wing on three legs, fins down its face, a bar across. */
function radiator(a) {
  const py = 3, ph = 26, base = 48;
  for (const x of [12, 62.4, 112]) { lit(a, x, py + ph, x + 3.6, base - 2.4, MID); lit(a, x - 2, base - 2.4, x + 5.6, base, DARK); }
  a.tube(`M13.6 ${base - 3}L64 ${py + ph + 1}`, .9, MID);
  a.tube(`M113.6 ${base - 3}L64 ${py + ph + 1}`, .9, MID);
  const d = band(.6, py, 127.4, py + ph);
  a.shape(d, PANEL, { lite: band(.6, py, 127.4, py + 1.6), shade: band(.6, py + ph - 3, 127.4, py + ph), deep: band(.6, py + ph - 1.2, 127.4, py + ph) });
  for (let x = 5.6; x < 127; x += 6) a.line(`M${x} ${py + 2.8}L${x} ${py + ph - 2.8}`, { c: 'rgba(80,86,90,.55)', w: .6 });
  a.flat(band(.6, py + ph * .48, 127.4, py + ph * .48 + 1.4), '#9da3a3');
  patch(a, 30, py + ph - 4, 3.4, 1.4, SCORCH, d, 37);
  patch(a, 96, py + 6, 2.4, 1.2, RUST, d, 41);
}

/** machine_vesselCollar: a docking collar, its ring on a body of hull, hazard chevrons round its lip. */
function collar(a) {
  lit(a, 6.4, 9.2, 41.6, 24, COLLAR);
  rivets(a, [10, 18], [38, 18]);
  const d = band(.6, 2, 47.4, 10);
  a.shape(d, LIGHT, { lite: band(.6, 2, 47.4, 3.4) });
  DebrisKit.hazard(a, .6, 6, 46.8, 3.2, { step: 4.4, clip: d });
  patch(a, 40, 21, 1.6, 1, RUST, band(6.4, 9.2, 41.6, 24), 43);
}

/** machine_vesselTruss: a segment of the keel truss hung on two posts, two chords laced between them, the container clamps under it. */
function truss(a) {
  // Its posts in columns 2 and 8 (Vessels.TrussPosts), where pylons carry them on up to a shallower hull.
  for (const x of [17.8, 65.8]) lit(a, x, 0, x + 4.4, 9, MID);
  for (let x = 0; x < 88; x += 7.2) a.tube(`M${x} ${16}L${x + 7.2} ${10}`, .9, DARK, { spec: false });
  lit(a, 0, 8, 88, 10.6, LIGHT);
  lit(a, 0, 15, 88, 17.6, MID);
  for (const x of [15.2, 74]) lit(a, x, 17.6, x + 4.8, 24, DARK);
  rivets(a, [20, 9.3], [68, 9.3]);
  patch(a, 44, 16.3, 2.4, .8, RUST, band(0, 15, 88, 17.6), 47);
}

/** machine_vesselPylon: a row of a truss post carried up to the hull, a cell high, its column the post's. */
function pylon(a) {
  lit(a, 1.8, -.4, 6.2, 8.4, MID, { lite: band(1.8, -.4, 3, 8.4), shade: band(5, -.4, 6.2, 8.4) });
  a.line('M2.4 4L5.6 4', { w: .5 });
}

/** machine_vesselBoom: the sensor boom off the bow's tip, its mount, a rod, the strobe's head at its end. */
function boom(a) {
  lit(a, 0, 6.2, 52, 9.4, LIGHT);
  lit(a, 6, 3.2, 13, 12.4, DARK);
  lit(a, 49, 2, 61, 12, MID);
  a.shape(ell(61.4, 7, 2, 3), CLEAR, { lite: ell(60.8, 5.6, .8, .9), shade: .6 });
  rivets(a, [9.5, 5.8]);
}

// ---- The skin (derelict-looks, the mockup's drawStatic): a vessel's hull in neutral greys, which the game tints by its livery
// (VesselLooks.HullGrey and the rest): plates, ribs, the bow's slopes, the stripe, the ink round it, the letters of its name.
const grey = (v) => '#' + Math.round(v).toString(16).padStart(2, '0').repeat(3);
const PLATE = { base: grey(196), lite: grey(236), shade: grey(137), deep: grey(92), spec: grey(240), ink: INK };
const RIB = { base: grey(170), lite: grey(214), shade: grey(120), deep: grey(82), spec: grey(226), ink: INK };
const STRIPE = { base: grey(222), lite: grey(242), shade: grey(186), deep: grey(160), spec: grey(250), ink: INK };
/** Drawn bright: a livery's tint darkens it to the mockup's rust. */
const SKIN_RUST = ['#b27a52', '#7a4c34'];
const SEAM = grey(98), PIT = grey(56), INK_HEAVY = '#0b0e13';
const GLASS_PANE = { base: '#7d9c9f', shade: '#62828a', deep: '#4e6c74', lite: '#b0cccb', spec: '#e4f0ee', ink: '#10202a' };
/** The letters a vessel's name and code are painted in (VesselLooks.Glyphs), in this order. */
const GLYPHS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-';

/** Pictures of one size laid in a sheet, `across` a row. */
function sheet(pics, across) {
  const w = pics[0].width, h = pics[0].height, c = Anime.canvas(w * across, h * Math.ceil(pics.length / across)), x = c.getContext('2d');
  pics.forEach((p, i) => x.drawImage(p, (i % across) * w, Math.floor(i / across) * h));
  return c;
}

/**
 * A row of an 8-cell panel: its seam at the left, lit along its top (role 0), plain (1) or in shade over space (2); bare (variant
 * 0), its cells rusted (1) or pitted (2). Each cell's wear stays in it, for a cell takes its variant alone.
 */
const plates = (role, variant) => (a) => {
  const r = rng(0x5e1 + role * 7 + variant * 31);
  a.shape(band(-2, 0, 66, 8), PLATE, { outer: false, edge: false,
    shade: role === 2 ? band(-2, 0, 66, 8) : undefined,
    deep: role === 2 ? band(-2, 5.4, 66, 8) : undefined });
  // Its top catching the star: a lighter band, brightest at the very edge (the mockup's solid, tl .45).
  if (role === 0) { a.flat(band(-2, 0, 66, 3.4), grey(222)); a.flat(band(-2, 0, 66, .9), grey(238)); }
  a.flat(band(-.36, 0, .36, 8), SEAM);
  a.flat(band(.36, 0, .9, 8), role === 2 ? grey(160) : grey(226));
  const ry = role === 0 ? 4.4 : role === 2 ? 3.6 : null;
  if (ry !== null) for (const x of [2.8, 61.2]) { a.flat(ell(x + .25, ry + .35, .8), grey(role === 2 ? 60 : 96)); a.flat(ell(x, ry, .68), grey(role === 2 ? 172 : 232)); }
  for (let c = 0; c < 8; c++) {
    const cell = band(c * 8 + .7, .5, c * 8 + 7.3, 7.5);
    if (variant === 1) {
      const x = c === 0 ? 3.8 : c === 7 ? 60.2 : c * 8 + 2.5 + r() * 3, y = role === 0 ? 3.4 + r() * 2 : role === 2 ? 3 + r() * 2.6 : 2 + r() * 4;
      patch(a, x, y, 1.5 + r() * .9, 1.1 + r() * .6, SKIN_RUST, cell, 100 + c + role * 9);
    } else if (variant === 2) {
      for (let k = 0, n = 2 + Math.floor(r() * 2); k < n; k++) {
        const x = c * 8 + 1.4 + r() * 5.2, y = 1.2 + r() * 5.6;
        a.flat(ell(x, y, .5, .45), PIT);
        a.flat(ell(x + .1, y + .55, .35, .18), grey(226));
      }
    } else if (role === 0 && r() < .35) {
      const x = c * 8 + 1 + r() * 3, y = 1.2 + r() * 2.2;
      a.line(`M${x} ${y}L${x + 2 + r() * 2.4} ${y + (r() - .5) * .8}`, { c: grey(238), w: .8 });
    }
  }
};

/** A row of a rib, two cells: a strap over the plates lit on its left, in shade on its right, a rivet a cell. */
const ribRow = (role) => (a) => {
  a.shape(band(-1, 0, 17, 8), RIB, { outer: false, edge: false,
    lite: role === 0 ? band(-1, 0, 17, 3.4) : undefined,
    shade: role === 2 ? band(-1, 0, 17, 8) : undefined,
    deep: role === 2 ? band(-1, 5.4, 17, 8) : undefined });
  a.flat(band(.4, 0, 1.4, 8), RIB.lite);
  a.flat(band(14.4, 0, 15.6, 8), RIB.shade);
  for (const x of [0, 15.6]) a.flat(band(x, 0, x + .4, 8), grey(48));
  for (const x of [4.6, 11.4]) { a.flat(ell(x + .2, 4.3, .6), grey(76)); a.flat(ell(x, 4, .55), grey(218)); }
};

/** A cell of the livery stripe, a band and its pin line under it; scuffed ones lose paint at its edges. */
const stripeCell = (scuff) => (a) => {
  const r = rng(0x57a + scuff * 13);
  a.shape(band(-1, 3.2, 9, 6.8), STRIPE, { outer: false, edge: false, lite: band(-1, 3.2, 9, 3.9), shade: band(-1, 6.1, 9, 6.8) });
  a.flat(band(-1, 7.3, 9, 7.9), STRIPE.base);
  for (let k = 0; k < scuff * 2; k++) a.cut(ell(1 + r() * 6, r() < .5 ? 3.3 : 6.7, .7 + r() * 1.1, .5 + r() * .6));
};

/**
 * A bow's slope over `run` cells, one row: the hull below the line from its top-left to its bottom-right (above it, mirrored, for
 * the bottom's), lit along the line on top, in shade under; the heavy ink on the line. Glass: the cockpit's pane instead.
 */
function bowSlope(run, bottom, glass) {
  const W = 8 * run, f = (y) => (bottom ? 8 - y : y);
  const P = (...xy) => poly(xy.map((v, i) => (i % 2 ? f(v) : v)));
  const t = 2.25 * Math.hypot(1, 8 / W), along = (k) => P(0, 0, W, 8, W - (k * W) / 8, 8, 0, k);
  const draw = (a) => {
    const hull = P(0, 0, W, 8, 0, 8);
    if (glass) a.shape(hull, GLASS_PANE, { outer: false, edge: false, lite: P(W * .25, 2, W * .25 + 2, 2, W * .25 - 2, 8, W * .25 - 4, 8) });
    else a.shape(hull, PLATE, { outer: false, edge: false, lite: bottom ? undefined : along(3.4), shade: bottom ? along(4.6) : undefined, deep: bottom ? along(t + 1) : undefined });
  };
  const c = Anime.bake({ w: W, h: 8, draw }), x = c.getContext('2d'), S = c.width / W;
  if (glass) { x.globalCompositeOperation = 'destination-out'; x.fillStyle = 'rgba(0,0,0,.41)'; x.fillRect(0, 0, c.width, c.height); x.globalCompositeOperation = 'source-over'; }
  // The line: heavy, swelling a little along it.
  x.fillStyle = INK_HEAVY;
  x.beginPath();
  const n = 10 * run;
  for (let i = 0; i <= n; i++) { const px = (W * i) / n; x.lineTo(px * S, f((8 * i) / n) * S); }
  for (let i = n; i >= 0; i--) { const px = (W * i) / n, k = t * (1 + .16 * Math.sin((Math.PI * 2 * i) / 10)); x.lineTo(px * S, f(Math.min(8, (8 * i) / n + k)) * S); }
  x.closePath(); x.fill();
  return c;
}

/** The ink along one side of a hull cell (up 1, right 2, down 4, left 8): heavy where it meets space, light where it meets a room. */
function inkSide(heavy, side) {
  const c = Anime.canvas(32, 32), x = c.getContext('2d'), n = 16, t0 = heavy ? 9 : 2.6;
  x.translate(16, 16); x.rotate({ 1: 0, 2: Math.PI / 2, 4: Math.PI, 8: -Math.PI / 2 }[side]); x.translate(-16, -16);
  x.fillStyle = heavy ? INK_HEAVY : 'rgba(20,23,29,.7)';
  x.beginPath(); x.moveTo(0, 0);
  // Swelling and thinning a little, the same at both ends so cells join.
  for (let i = 0; i <= n; i++) x.lineTo((i * 32) / n, t0 * (1 + .07 * Math.sin((Math.PI * 2 * i) / n) + .05 * Math.sin((Math.PI * 6 * i) / n + (heavy ? 0 : 1.3))));
  x.lineTo(32, 0); x.closePath(); x.fill();
  return c;
}

/** The painted letters, a slot of w × h logical pixels each in GLYPHS' order, their capitals cap tall: worn, specks of paint gone. */
function letters(w, h, cap) {
  const S = 4, c = Anime.canvas(GLYPHS.length * w * S, h * S), x = c.getContext('2d');
  x.fillStyle = grey(232); x.textAlign = 'center'; x.textBaseline = 'alphabetic';
  x.font = `700 ${(cap * S) / .7}px Bahnschrift, "Arial Narrow", Consolas, sans-serif`;
  [...GLYPHS].forEach((ch, i) => {
    const k = Math.min(1, ((w - 1) * S) / x.measureText(ch).width);
    x.save(); x.translate((i + .5) * w * S, ((h + cap) * S) / 2); x.scale(k, 1); x.fillText(ch, 0, 0); x.restore();
  });
  const r = rng(0xa11 + w);
  x.globalCompositeOperation = 'destination-out';
  for (let k = 0; k < (c.width * c.height) / 80; k++) { x.fillStyle = `rgba(0,0,0,${.45 + r() * .55})`; x.fillRect(r() * c.width, r() * c.height, 1 + r() * 2.5, 1 + r() * 2); }
  return c;
}

function skin() {
  const bake = (w, draw) => Anime.bake({ w, h: 8, tile: true, draw });
  out('vessel_hull', sheet([0, 1, 2].flatMap((role) => [0, 1, 2].map((v) => bake(64, plates(role, v)))), 1));
  out('vessel_rib', sheet([0, 1, 2].map((role) => bake(16, ribRow(role))), 1));
  out('vessel_stripe', sheet([0, 1, 2, 3].map((s) => bake(8, stripeCell(s))), 4));
  for (const run of [1, 2]) for (const bottom of [false, true]) for (const glass of [false, true])
    out(`vessel_bow_${run}_${bottom ? 'bottom' : 'top'}${glass ? '_glass' : ''}`, bowSlope(run, bottom, glass));
  for (const heavy of [true, false]) for (const side of [1, 2, 4, 8]) out(`vessel_ink_${heavy ? 'heavy' : 'light'}_${side}`, inkSide(heavy, side));
  out('vessel_letters', letters(7, 12, 8));
  out('vessel_letters_small', letters(6, 10, 6.4));
}

Subjects.vessel = function () {
  skin();
  out('machine_vesselNozzle', Anime.bake({ w: 64, h: 56, draw: nozzle }));
  out('machine_vesselRcs', Anime.bake({ w: 24, h: 16, draw: rcs }));
  out('machine_vesselMast', Anime.bake({ w: 16, h: 64, draw: mast(64) }));
  out('machine_vesselMastTall', Anime.bake({ w: 16, h: 80, draw: mast(80) }));
  out('machine_vesselDish', Anime.bake({ w: 40, h: 40, draw: dish }));
  out('machine_vesselSolar', Anime.bake({ w: 88, h: 48, draw: solar }));
  out('machine_vesselTank', Anime.bake({ w: 128, h: 24, draw: tank }));
  out('machine_vesselRadiator', Anime.bake({ w: 128, h: 48, draw: radiator }));
  out('machine_vesselCollar', Anime.bake({ w: 48, h: 24, draw: collar }));
  out('machine_vesselTruss', Anime.bake({ w: 88, h: 24, draw: truss }));
  out('machine_vesselPylon', Anime.bake({ w: 8, h: 8, draw: pylon }));
  out('machine_vesselBoom', Anime.bake({ w: 64, h: 16, draw: boom }));
};
})();
