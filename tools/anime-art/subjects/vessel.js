// The outside parts, skin, furniture and signs of abandoned vessels, as picked on the derelict-looks mockup (docs/decisions/2026-11-17-derelict-looks.md).
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

// ---- Signs of abandonment inside (derelict-looks, the mockup's damage in drawStatic): pictures, never things, on a room's cells.
// Each picture's box is its sign's cells (VesselSigns.Size); the room's ceiling is just above row 0, its floor just under the last row.
const ICE = { base: '#cfe6f0', shade: '#a9c8d6', deep: '#86a8b8', lite: '#eef8fc', spec: '#ffffff', ink: '#1a2a36' };
const CRATE = { base: '#5e5a44', shade: '#46432f', deep: '#312f21', lite: '#78735a', spec: '#a39d80', ink: INK };
const PIPE = { base: '#6b7179', shade: '#4f545b', deep: '#383c42', lite: '#878d95', spec: '#b0b5bc', ink: INK };
const FALLEN = { base: '#4a5059', shade: '#363b42', deep: '#262a30', lite: '#626973', spec: '#959ba4', ink: INK };
const HOLE = '#0b0c0f';
/** A soft blob with a torn edge, `n` points round, by chance from `seed`. */
function blobOf(x, y, rx, ry, seed, n = 11) {
  const r = rng(seed), p = [];
  for (let i = 0; i < n; i++) { const t = (i / n) * Math.PI * 2, q = .62 + r() * .5; p.push(x + Math.cos(t) * rx * q, y + Math.sin(t) * ry * q); }
  return poly(p);
}
/** A band `len` long and `thick` deep round (cx, cy), turned by `turn` radians. */
function turned(cx, cy, len, thick, turn) {
  const c = Math.cos(turn), s = Math.sin(turn), at = (u, v) => [cx + u * c - v * s, cy + u * s + v * c];
  return poly(...at(-len / 2, -thick / 2), ...at(len / 2, -thick / 2), ...at(len / 2, thick / 2), ...at(-len / 2, thick / 2));
}
/** A torn hole along the ceiling from x0 to x1, `deep` down at most. */
function torn(a, x0, x1, deep, seed) {
  const r = rng(seed), p = [x0, -1];
  for (let x = x0; x <= x1; x += 2.2) p.push(x, deep * (.45 + r() * .55));
  p.push(x1, -1);
  a.flat(poly(p), HOLE);
}

/** vessel_frost: frost on the back wall by a cold wall at its right, icicles off the ceiling, a rim of ice along the floor. 5 × 6 cells. */
function frost(a) {
  const r = rng(61);
  for (let i = 0; i < 9; i++) a.flat(blobOf(40 - 3 - r() * 24, 5 + r() * 38, 3 + r() * 5, 3 + r() * 5, 70 + i), 'rgba(196,224,238,.42)');
  for (let i = 0; i < 5; i++) a.flat(blobOf(40 - 2 - r() * 11, 8 + r() * 32, 1.6 + r() * 2.4, 1.6 + r() * 2.4, 90 + i), 'rgba(232,246,252,.6)');
  for (const [x, len] of [[37, 8.4], [33.6, 5.2], [29, 7.4], [22.4, 4], [14, 3.2]]) a.shape(poly(x - 1, 0, x + 1, 0, x + .2, len), ICE, { shade: .5 });
  const rim = [8, 48];
  for (let x = 8; x <= 40; x += 3.2) rim.push(x, 46.6 - r() * 1.2 - (x - 8) / 32 * 1.4);
  rim.push(40, 48);
  a.shape(poly(rim), ICE, { lite: band(8, 44, 40, 46.4) });
}

/** vessel_cable: a torn hole in the ceiling, a cable hanging out of it to a frayed end (VesselLooks.CableTip), a red wire beside it. 3 × 4 cells. */
function cable(a) {
  torn(a, 3, 20, 4.2, 101);
  a.shape(poly(15, 0, 20.4, 0, 19.6, 5.6, 17.4, 4.2), FALLEN, { shade: .5 });
  a.tube('M13.6 2C12.8 8 9.8 10.4 10.4 14.2', .8, { base: '#a3402c', shade: '#7a2c1e', deep: '#521c12', lite: '#c0604a', ink: INK });
  a.tube('M11 2C12.6 11 16.4 14 15 21', 1.4, { base: '#2a2d32', shade: '#1c1e22', deep: '#121417', lite: '#40444b', ink: INK });
  for (const [dx, dy] of [[-1.4, 1.8], [.2, 2.2], [1.6, 1.4]]) a.line(`M15 21L${15 + dx} ${21 + dy}`, { c: '#c49e76', w: .7 });
}

/** vessel_soot: soot over a burnt machine, climbing the back wall from the top of it, its machine's top two rows of cells from the foot. 6 × 5 cells. */
function soot(a) {
  a.flat(blobOf(24, 30, 22, 12, 111, 13), 'rgba(20,18,15,.55)');
  for (const [x, y, rx, ry, s] of [[17, 16, 7, 9, 113], [29, 12, 6, 10, 117], [23, 7, 4, 7, 119]]) a.flat(blobOf(x, y, rx, ry, s), 'rgba(20,18,15,.4)');
  a.flat(blobOf(24, 31, 13, 7, 121), 'rgba(10,9,8,.72)');
}

/** vessel_pipe: a pipe along the ceiling, down out of it and back up, burst in the middle (VesselLooks.BurstAt), its lip bent the way it sprays. 8 × 2 cells. */
function pipe(a) {
  for (const x of [13, 50]) lit(a, x, 0, x + 3, 5, DARK);
  a.tube('M3 -1L3 4.5Q3 7.5 6 7.5L28.6 7.5', 3, PIPE);
  a.tube('M35.4 7.5L58 7.5Q61 7.5 61 4.5L61 -1', 3, PIPE);
  a.shape(poly(27.6, 5.6, 29.8, 5, 30, 6.6, 29.2, 7.4, 29.8, 8.6, 28.6, 9.6, 27.6, 9.4), PIPE, { shade: .5 });
  a.shape(poly(35.4, 5.6, 34.2, 6.4, 34.8, 7.6, 34.4, 9.2, 35.6, 10.8, 37.4, 11.6, 37.6, 9.4), PIPE, { shade: .5 });
  a.flat(ell(29, 7.5, .7, 1.1), HOLE);
  a.flat(ell(35, 7.8, .7, 1.1), HOLE);
  patch(a, 44, 8.4, 2.2, .8, RUST, band(36, 6, 58, 9), 131);
}

/** vessel_fallen: a ceiling panel torn out, a hole where it hung, and the panel down on the floor, leaning. 4 × 6 cells. */
function fallen(a) {
  torn(a, 2, 26, 3.8, 141);
  a.line('M8.4 2.4L9.4 6.8L10.8 11', { c: '#3a3d43', w: .8 });
  const d = turned(14.4, 43.2, 25.6, 2.9, -.32);
  a.shape(d, FALLEN, { shade: .6 });
  a.line(`M${14.4 - 4} ${43.2 + 1.3}L${14.4 - 3.4} ${43.2 - 1.6}`, { w: .6 });
  rivets(a, [6.6, 45.6], [22, 40.8]);
  for (const [x, w] of [[24.6, 2.4], [28, 1.6]]) a.shape(band(x, 46.4, x + w, 48), FALLEN, {});
}

/** vessel_toppled: a storage crate tipped on its corner, what it held spilled along the floor beside it. 5 × 3 cells. */
function toppled(a) {
  const d = turned(14, 13.1, 22, 14, .42), c = Math.cos(.42), s = Math.sin(.42), at = (u, v) => `${14 + u * c - v * s} ${13.1 + u * s + v * c}`;
  a.shape(d, CRATE, { shade: .9, lines: [`M${at(-11, -7)}L${at(11, 7)}`, `M${at(11, -7)}L${at(-11, 7)}`] });
  const r = rng(151);
  for (let i = 0; i < 6; i++) {
    const x = 26 + r() * 12, w = 2 + r() * 2;
    a.shape(band(x, 21.8, x + w, 24), { base: ['#8b5a36', '#7d848d', '#c9a03a', '#5c7b4e'][i % 4], shade: '#2c2a26', ink: INK }, {});
  }
}


// ---- Furniture inside (derelict-looks, the mockup's drawItem): what a hero takes apart in a derelict's rooms, lit from the side by
// the game's light, worn and left, never repaired. Each picture is its machine's footprint, eight pixels a cell.
const hexRgb = (h) => [1, 3, 5].map((i) => parseInt(h.substr(i, 2), 16));
const mixed = (h, to, f) => '#' + hexRgb(h).map((v, i) => Math.round(v + (hexRgb(to)[i] - v) * f).toString(16).padStart(2, '0')).join('');
/** The mockup's palOf: a paint, lit and shaded from it. */
const palOf = (h) => ({ base: h, lite: mixed(h, '#ffffff', .26), spec: mixed(h, '#ffffff', .5), shade: mixed(h, '#000000', .3), deep: mixed(h, '#000000', .55), ink: INK });
const WARM = palOf('#5e5a44'), BUNK = palOf('#6b5c49'), SEAT = palOf('#6a4a3a'), LIFE = palOf('#56676d'), WORKS = palOf('#4a5049');
/** The lamp's housing: pale bare metal, lighter than the ship's lamp; its lens unlit, the game's light shines through it. */
const PALE = { base: '#aeb3b6', shade: '#878c90', deep: '#64696d', lite: '#cdd1d3', spec: '#eef0f1', ink: INK };
const LENS = { base: '#c9c2ad', shade: '#a49d89', deep: '#7f7969', lite: '#e2dccb', spec: '#f6f2e6', ink: INK };
/** The cargo containers' colours, a look each (Vessels.ContainerColours), the mockup's CONT; and each keel container's stencilled number. */
const CONT = ['#8a4a35', '#3d6275', '#9b7b3a', '#5b6b4a', '#767b83', '#6d4a5e'];
const NUMBERS = ['27', '41', '63', '18', '85', '52'];

/** machine_vesselLamp: a flat ceiling fixture, two cells across and a cell tall, its lens strip under a pale housing. */
function lamp(a) {
  a.shape(band(.4, -.4, 15.6, 3.8), PALE, { lite: band(.4, -.4, 15.6, 1), shade: band(.4, 2.8, 15.6, 3.8) });
  rivets(a, [2.4, 1.6], [13.6, 1.6]);
  a.shape(band(2, 3.8, 14, 5.4), LENS, { lite: band(2, 3.8, 14, 4.3), shade: .4 });
  for (const x of [5, 8, 11]) a.line(`M${x} 3.9L${x} 5.3`, { c: '#8c8676', w: .4 });
}

/** machine_vesselBunk: two berths on posts, a mattress each on its frame, a blanket thrown back on the lower. 5 × 5 cells. */
function bunk(a) {
  for (const x of [0, 37.2]) lit(a, x, 0, x + 2.8, 40, MID, { lite: band(x, 0, x + 1, 40), shade: band(x + 1.9, 0, x + 2.8, 40) });
  for (const by of [11.2, 31.2]) {
    lit(a, 2.8, by + 3.6, 37.2, by + 6.4, DARK);
    a.shape(Anime.rr(3.2, by - .4, 33.6, 4.4, 1.2), BUNK, { lite: band(3.2, by - .4, 36.8, by + .8), shade: band(3.2, by + 2.8, 36.8, by + 4) });
  }
  a.shape(Anime.rr(5, 8.6, 6, 2.6, 1), palOf('#8a8270'), { shade: .5 });
  a.shape(poly(18, 30.4, 36.4, 30.4, 36.4, 34.6, 22, 34.6), palOf('#4f5b66'), { lite: band(18, 30.4, 36.4, 31.2), lines: ['M24 30.6L26.6 34.4'] });
  patch(a, 38.6, 24, .8, 1.6, RUST, band(37.2, 0, 40, 40), 211);
}

/** machine_vesselLocker: tall and narrow, vent slats at its top, a handle, a dent in its door. 2 × 5 cells. */
function locker(a) {
  const d = band(.4, 0, 15.6, 40);
  lit(a, .4, 0, 15.6, 40, MID, { lines: ['M2 38.4L14 38.4'] });
  for (let y = 4; y < 11.2; y += 2) { a.flat(band(2.4, y, 13.6, y + .9), DARK.deep); a.flat(band(2.4, y + .9, 13.6, y + 1.3), MID.lite); }
  a.shape(band(11.2, 19.2, 12.8, 24.8), { base: '#b8b2a2', shade: '#8e897b', deep: '#6a665b', lite: '#d4cfc1', spec: '#f0ece2', ink: INK }, { shade: .4 });
  a.line('M4.4 26L6 27.6L5.4 29.4', { w: .5 });
  patch(a, 4, 35, 1.8, 1.2, RUST, d, 223);
}

/** machine_vesselTable: a top of worn board on one leg and its foot. 3 × 2 cells. */
function table(a) {
  lit(a, 10.4, 3.2, 13.6, 14.6, MID);
  lit(a, 5.6, 14, 18.4, 16, DARK);
  a.shape(band(0, 0, 24, 3.2), WARM, { lite: band(0, 0, 24, 1), shade: band(0, 2.4, 24, 3.2), lines: ['M8 .6L8 2.6', 'M16.4 .6L16.4 2.6'] });
}

/** machine_vesselSeat: a pilot's seat facing the bow, its back on the left, a cushion on a post. 2 × 3 cells. */
function seat(a) {
  lit(a, 7, 17.6, 9.8, 22.4, MID);
  lit(a, 3.6, 22.4, 13.2, 24, DARK);
  a.shape(Anime.rr(1.2, 0, 5.6, 16.4, 1.6), SEAT, { lite: band(1.2, 0, 6.8, 2), shade: band(5, 0, 6.8, 16.4) });
  a.shape(Anime.rr(1.2, 12.8, 14.4, 4.8, 1.4), SEAT, { lite: band(1.2, 12.8, 15.6, 14), shade: band(1.2, 16.4, 15.6, 17.6) });
  a.line('M3 4L5 4', { w: .5 });
  a.line('M8 13.4L12 13.2', { c: '#8e6a54', w: .6 });
}

/** machine_vesselLifeTank: a squat cylinder, its shoulders bevelled, a yellow band round it and its pipe up into the wall. 4 × 4 cells. */
function lifeTank(a) {
  a.tube('M16 4L16 1.6Q16 .8 17 .8L32 .8', 1.6, MID);
  const d = poly(1.6, 9.6, 6.4, 4, 25.6, 4, 30.4, 9.6, 30.4, 32, 1.6, 32);
  a.shape(d, LIFE, { lite: poly(1.6, 9.6, 6.4, 4, 25.6, 4, 30.4, 9.6, 30.4, 11, 1.6, 11), shade: band(24, 4, 31, 32), deep: band(28, 4, 31, 32) });
  a.flat(band(1.6, 19.2, 30.4, 21.6), '#c9b25a', { clip: d });
  a.flat(band(1.6, 21.6, 30.4, 22.2), '#8f7d3c', { clip: d });
  lit(a, 12, 25, 20, 29, DARK);
  a.flat(ell(16, 27, 1.4), '#b33a2a');
  rivets(a, [5, 14], [27, 14]);
  patch(a, 8, 30, 2.6, 1.2, RUST, d, 229);
}

/** machine_vesselConsole: a body of switches under a slanted screen (Light.ConsoleScreens), dark till the game lights it. 4 × 3 cells. */
function consoleDesk(a) {
  lit(a, 0, 8.8, 32, 24, MID, { lines: ['M1.2 19.2L30.8 19.2'] });
  a.shape(poly(2.4, 8.8, 5.6, 0, 29.6, 0, 29.6, 8.8), DARK, { lite: poly(5.6, 0, 29.6, 0, 29.6, .8, 5.3, .8), shade: .5 });
  a.flat(band(7.2, 1.6, 27.2, 7.6), '#0c1d20');
  a.flat(band(7.6, 2, 12, 2.6), '#163036');
  for (let k = 0; k < 4; k++) a.flat(band(4.8 + k * 6.4, 12, 7.6 + k * 6.4, 13.6), '#7d848d');
  for (const [x, c] of [[8, '#5a2420'], [11, '#2c4a2c'], [14, '#5a4a20']]) a.flat(ell(x, 16.4, .9), c);
  patch(a, 26, 22, 2, .9, RUST, band(0, 8.8, 32, 24), 233);
}

/** machine_vesselMachine: a machine block, a gear wheel in its face, a copper stripe, hazard paint along its foot. 6 × 4 cells. */
function machineBlock(a) {
  const d = band(0, 0, 48, 32);
  lit(a, 0, 0, 48, 32, WORKS);
  const teeth = [];
  for (let i = 0; i < 24; i++) { const t = (i / 24) * Math.PI * 2, r = i % 2 ? 7 : 8.4; teeth.push(14.4 + Math.cos(t) * r, 14.4 + Math.sin(t) * r); }
  a.shape(poly(teeth), palOf('#2f3330'), { shade: .8 });
  a.shape(ell(14.4, 14.4, 2.8), palOf('#6b7168'), { shade: .4 });
  a.shape(band(28.8, 6.4, 41.6, 10.4), COPPER, { lite: band(28.8, 6.4, 41.6, 7.4) });
  for (let y = 15; y < 24; y += 2.4) a.line(`M29 ${y}L42 ${y}`, { c: WORKS.deep, w: .5 });
  hazardFoot(a, 0, 28.4, 48, 2.4, d);
  patch(a, 40, 26, 2.6, 1.2, RUST, d, 239);
}
/** Yellow and black paint in slanting bands from x to x + w. */
function hazardFoot(a, x, y, w, h, clip) {
  a.flat(band(x, y, x + w, y + h), '#d6a12c', { clip });
  for (let u = x - h; u < x + w; u += 3.6) a.flat(poly(u, y + h, u + 1.8, y + h, u + 1.8 + h, y, u + h, y), '#17181b', { clip });
}

/** machine_vesselRack: a dark frame of cables, four colours looping down it between a top bar and a foot. 4 × 4 cells. */
function rack(a) {
  lit(a, 0, 0, 32, 32, DARK);
  const cols = ['#a63b2c', '#c9a03a', '#3b6aa0', '#7a7f86'];
  cols.forEach((c, k) => {
    const x = 4.8 + k * 6.8;
    a.tube(`M${x} 2.4Q${x + 2.4} 19.2 ${x} 29.6`, 1.4, palOf(c), { spec: false });
  });
  lit(a, 1.2, 1.2, 30.8, 3.6, MID);
  lit(a, 1.2, 28.4, 30.8, 30.8, MID);
  rivets(a, [3, 2.4], [29, 2.4]);
}

/** The corrugation, castings and wear of a cargo container w × h pixels in `col`; the keel's stencilled `num` on a plate if given. */
const container = (w, h, col, num) => (a) => {
  const pal = palOf(col), d = band(0, 0, w, h);
  a.shape(d, pal, { lite: band(0, 0, w, 2.4), shade: band(0, h - 3.6, w, h), deep: band(0, h - 1.4, w, h) });
  for (let x = 5.6; x < w - 4; x += 4.8) { a.flat(band(x, 2.8, x + 1, h - 2.8), pal.shade, { clip: d }); a.flat(band(x + 1, 2.8, x + 1.5, h - 2.8), pal.lite, { clip: d }); }
  for (const [x, y] of [[0, 0], [w - 4, 0], [0, h - 4], [w - 4, h - 4]]) a.flat(band(x, y, x + 4, y + 4), '#25272b');
  if (num) {
    a.flat(band(12, 7.2, 30, 17.6), pal.base);
    stencil(a, num, 14, 8.8, 'rgba(235,228,212,.78)');
  }
  a.flat(band(w * .7, h - 7.2, w * .7 + 7.2, h - 3.2), 'rgba(110,70,45,.55)', { clip: d });
  patch(a, w * .3, h - 4.6, 2.2, 1, RUST, d, 241 + w);
};
/** Seven-segment stencil digits, each 4.6 wide and 7.2 tall with its bridges left open, from (x, y). */
function stencil(a, text, x, y, c) {
  const SEG = { 0: 'abcdef', 1: 'bc', 2: 'abged', 3: 'abgcd', 4: 'fgbc', 5: 'afgcd', 6: 'afgedc', 7: 'abc', 8: 'abcdefg', 9: 'abcdfg' };
  const t = 1.2, W = 4.6, H = 7.2, m = H / 2;
  [...text].forEach((ch, i) => {
    const X = x + i * 7.4, seg = {
      a: band(X + .5, y, X + W - .5, y + t), g: band(X + .5, y + m - t / 2, X + W - .5, y + m + t / 2), d: band(X + .5, y + H - t, X + W - .5, y + H),
      f: band(X, y + .5, X + t, y + m - .7), b: band(X + W - t, y + .5, X + W, y + m - .7),
      e: band(X, y + m + .7, X + t, y + H - .5), c: band(X + W - t, y + m + .7, X + W, y + H - .5),
    };
    for (const s of SEG[ch]) a.flat(seg[s], c);
  });
}

function furniture() {
  out('machine_vesselLamp', Anime.bake({ w: 16, h: 8, draw: lamp }));
  out('machine_vesselBunk', Anime.bake({ w: 40, h: 40, draw: bunk }));
  out('machine_vesselLocker', Anime.bake({ w: 16, h: 40, draw: locker }));
  out('machine_vesselTable', Anime.bake({ w: 24, h: 16, draw: table }));
  out('machine_vesselSeat', Anime.bake({ w: 16, h: 24, draw: seat }));
  out('machine_vesselLifeTank', Anime.bake({ w: 32, h: 32, draw: lifeTank }));
  out('machine_vesselConsole', Anime.bake({ w: 32, h: 24, draw: consoleDesk }));
  out('machine_vesselMachine', Anime.bake({ w: 48, h: 32, draw: machineBlock }));
  out('machine_vesselRack', Anime.bake({ w: 32, h: 32, draw: rack }));
  // A colour a look, the first the kind itself (Vessels.LookKind).
  CONT.forEach((col, k) => {
    const look = k === 0 ? '' : String(k + 1);
    out('machine_vesselCargo' + look, Anime.bake({ w: 64, h: 32, draw: container(64, 32, col) }));
    out('machine_vesselContainer' + look, Anime.bake({ w: 80, h: 32, draw: container(80, 32, col, NUMBERS[k]) }));
  });
}

// ---- A liner's rooms (big-vessels, the mockup's drawItem and SHUTTLE_DRAW): the restaurant, the pool, the galley, the infirmary,
// the hangar and its five shuttles, the fuel tank; and the decor its rooms are dressed with (VesselDress.Decor), on the back wall.
const WOOD = palOf('#5e4330'), WOOD_DARK = palOf('#2f2119'), BLACK = palOf('#1d1d20'), CLOTH = palOf('#d6d1c4'), PALE_METAL = palOf('#d0d6d8');
const ORANGE = palOf('#c8722e'), SHEET = palOf('#bdbab2'), CAB = palOf('#a9aca8'), STEEL_GREY = palOf('#8a9096');
const NOZZLE = palOf('#494d55'), CANOPY = '#1b3640';
/** The mockup's CONTENTS, a capsule's look each: fuel, water, oxygen, nitrogen, coolant (Vessels.CapsuleContents). */
const CONTENTS = ['#9a5a2e', '#4f6f86', '#a7bcc6', '#7d8590', '#4f7a5a'];
/** x mirrored about a picture w wide when it faces the other way. */
const mirror = (w, flip) => (pts) => flip ? pts.map((v, i) => (i % 2 ? v : w - v)) : pts;

/** machine_vesselBar: the restaurant's bar, panelled wood under a dark top, a brass rail along its foot. 7 × 3 cells. */
function bar(a) {
  const d = band(0, 2.8, 56, 24);
  lit(a, 0, 2.8, 56, 24, WOOD, { lines: [9.6, 20.8, 32, 43.2].map((x) => `M${x} 5.6L${x} 21.6`) });
  lit(a, -1, 0, 57, 3.2, WOOD_DARK);
  a.line('M1 19.2L55 19.2', { c: '#c9a03a', w: .9 });
  patch(a, 44, 12, 2, 1, RUST, d, 301);
}

/** machine_vesselPiano: a grand piano from its side, its lid propped up, its keys out front, on two legs. 5 × 3 cells. */
function piano(a) {
  for (const x of [3.2, 35.2]) lit(a, x, 15.2, x + 1.8, 24, BLACK);
  a.line('M2.4 8L26 0', { c: '#1d1d20', w: 1.4 });
  a.line('M18 8L22 1.6', { c: '#5d646e', w: .6 });
  a.shape(poly(0, 8, 26, 8, 40, 10.4, 40, 15.2, 0, 15.2), BLACK, { lite: band(0, 8, 40, 9.2) });
  a.flat(band(-1, 9.6, 1, 11.6), '#e8e4da');
}

/** machine_vesselRoundTable: a round table under its cloth, on one pedestal and its foot. 3 × 2 cells. */
function roundTable(a) {
  lit(a, 10.8, 9, 13.2, 14.6, DARK);
  lit(a, 5.6, 14, 18.4, 16, DARK);
  a.shape(poly(2.4, 0, 21.6, 0, 24, 10.4, 0, 10.4), CLOTH, { lite: band(0, 0, 24, 1.4), shade: band(0, 8.4, 24, 10.4), lines: ['M8 1L7 10', 'M16 1L17 10'] });
}

/** machine_vesselChair, machine_vesselChair2: a dining chair, its back on the left (or mirrored), its seat and a front leg. 1 × 3 cells. */
function chair(flip) {
  return (a) => {
    const m = mirror(8, flip);
    a.shape(poly(m([0, 0, 2.8, 0, 2.8, 11.2, 8, 11.2, 8, 14, 2.8, 14, 2.8, 24, 0, 24])), WOOD, { shade: .4 });
    a.shape(poly(m([6.6, 14, 8, 14, 8, 24, 6.6, 24])), WOOD, { shade: .4 });
    a.flat(poly(m([.6, 2, 2.2, 2, 2.2, 9, .6, 9])), '#7b2e2a');
  };
}

/** machine_vesselLounger: a deck lounger, striped canvas on a frame, its back raised at the right, on two legs. 4 × 2 cells. */
function lounger(a) {
  for (const x of [2.4, 22.4]) lit(a, x, 12, x + 1.2, 16, MID);
  const d = poly(0, 8.8, 22.4, 8.8, 28.8, 0, 32, 1.6, 25.6, 12, 0, 12);
  a.shape(d, ORANGE, { shade: band(0, 10.4, 32, 12) });
  for (let x = 3.2; x < 25.6; x += 7.2) a.flat(band(x, 0, x + 3.2, 16), '#e6dfcf', { clip: d });
}

/** machine_vesselDiving: the diving board on its post at the right, the board out over the basin's far end. 5 × 8 cells. */
function diving(a) {
  lit(a, 33.6, 4.8, 37.6, 64, LIGHT);
  for (let y = 16; y < 62; y += 7.2) a.line(`M37.6 ${y}L40 ${y}`, { c: '#7d848d', w: .7 });
  a.shape(band(0, 2.4, 40, 4.8), PALE_METAL, { lite: band(0, 2.4, 40, 3.2) });
  lit(a, 30.4, 4.8, 38.4, 7.2, DARK);
}

/** machine_vesselPool: the basin half drained, tiles above an old waterline, murky water below, a ladder down its near end. 20 × 5 cells. */
function pool(a) {
  const d = band(0, 2.8, 160, 40);
  a.shape(d, palOf('#8fb6c2'), { lite: band(0, 2.8, 160, 4) });
  for (let x = 4.8; x < 160; x += 4.8) a.line(`M${x} 3.2L${x} 39`, { c: 'rgba(80,110,120,.35)', w: .35 });
  for (let y = 7.6; y < 40; y += 4.8) a.line(`M1 ${y}L159 ${y}`, { c: 'rgba(80,110,120,.35)', w: .35 });
  a.line('M1.6 10L158.4 10', { c: '#3e5250', w: .9 });
  a.flat(band(1.6, 21.6, 158.4, 38.4), 'rgba(34,82,80,.92)', { clip: d });
  a.line('M1.6 21.6L158.4 21.6', { c: '#7cb8ae', w: .9 });
  a.shape(band(-1, 0, 161, 2.8), palOf('#d8d6cc'), { lite: band(-1, 0, 161, .8) });
  for (const x of [6.4, 12.8]) a.tube(`M${x} 38L${x} 1Q${x} -1 ${x - 3.2} 0`, .9, LIGHT, { spec: false });
  for (let y = 6; y < 36; y += 5.6) a.line(`M6.4 ${y}L12.8 ${y}`, { c: '#c9ced2', w: .7 });
  patch(a, 120, 30, 6, 2, ['rgba(40,60,50,.5)', 'rgba(20,40,30,.6)'], d, 307);
}

/** machine_vesselChandelier: a chandelier on its chain, brass arms and crystal drops round lamps the game lights. 3 × 3 cells. */
function chandelier(a) {
  a.line('M12 0L12 7', { c: '#8c7a4a', w: .9 });
  a.tube('M2.4 12Q12 17.6 21.6 12', 1.1, palOf('#a8873a'), { spec: false });
  lit(a, 9.6, 6.4, 14.4, 14.4, palOf('#a8873a'));
  for (const x of [2.4, 12, 21.6]) a.shape(ell(x, 10, 1.8, 2.2), LENS, { lite: ell(x - .5, 9.2, .7, .8) });
  for (const x of [4.8, 8, 12, 16, 19.2]) a.shape(poly(x - .8, 15, x + .8, 15, x, 22), CLEAR, {});
}

/** machine_vesselStove: the galley's range, its burners on top, the oven's dark door. 4 × 3 cells. */
function stove(a) {
  lit(a, 0, 4.8, 32, 24, MID, { lines: ['M2 7.2L30 7.2'] });
  a.flat(band(3.2, 11.2, 28.8, 20), '#1f2125');
  for (const x of [6.4, 17.6, 25.6]) lit(a, x - 3.2, 2.4, x + 3.2, 4.8, DARK);
  a.line('M4 9.4L28 9.4', { c: '#b8b2a2', w: .7 });
}

/** machine_vesselCounter: a galley counter, steel doors along it. 5 × 2 cells. */
function counter(a) {
  lit(a, 0, 0, 40, 16, STEEL_GREY, { lines: [8, 20.8, 33.6].map((x) => `M${x} 4L${x} 15`) });
  a.flat(band(-.5, 0, 40.5, 1.6), '#5d646e');
}

/** machine_vesselBed: an infirmary bed, a white sheet on its frame, four short legs. 4 × 2 cells. */
function bed(a) {
  for (const x of [1.6, 28]) lit(a, x, 10.4, x + 2.4, 16, DARK);
  lit(a, 0, 7.2, 32, 10.4, MID);
  a.shape(Anime.rr(1.6, 3.2, 28.8, 4.4, 1.6), SHEET, { lite: band(1.6, 3.2, 30.4, 4.2) });
  a.shape(Anime.rr(2.4, 1.6, 7.2, 3.2, 1.4), palOf('#e3e0d8'), {});
}

/** machine_vesselMedCab: a medicine cabinet, the red cross on its door. 2 × 5 cells. */
function medCab(a) {
  lit(a, .4, 0, 15.6, 40, CAB, { lines: ['M2 20L14 20'] });
  a.flat(band(6.8, 6.4, 9.2, 15.2), '#a3332a');
  a.flat(band(3.6, 9.6, 12.4, 12), '#a3332a');
  a.flat(band(12, 24, 13.2, 29.6), '#7d848d');
}

/** machine_vesselFuelCart: a fuel cart, a rust-red tank on two wheels. 3 × 2 cells. */
function fuelCart(a) {
  lit(a, 0, 1.6, 24, 12, palOf('#8a4a35'), { lines: ['M8 2L8 11'] });
  for (const x of [4.8, 19.2]) a.shape(ell(x, 13.6, 2.4), palOf('#1f2125'), {});
}

/** machine_vesselCapsule and its looks: a horizontal tank on two saddles in its contents' paint, bands at its seams. 14 × 11 cells. */
function capsule(col) {
  return (a) => {
    const pal = palOf(col), d = Anime.rr(.8, 1.6, 110.4, 70.4, 35);
    for (const x of [20, 76]) a.shape(poly(x, 56, x + 17.6, 56, x + 22.4, 88, x - 4.8, 88), DARK, { shade: .5 });
    a.shape(d, pal, { lite: band(0, 1.6, 112, 12), shade: band(0, 54, 112, 72), deep: band(0, 64, 112, 72) });
    for (const x of [36, 72]) a.flat(band(x, 1.6, x + 4.8, 72), pal.shade, { clip: d });
    a.flat(band(0, 18, 112, 24), 'rgba(255,255,255,.08)', { clip: d });
    rivets(a, [38.4, 30], [74.4, 30]);
    patch(a, 90, 50, 6, 2.4, RUST, d, 311);
  };
}

/** The mockup's shuttle points, cells to pixels, lowered by dy so its feet stand on the picture's foot. */
const s8 = (pts, dy) => pts.map((v, i) => (i % 2 ? v * 8 + dy : v * 8));
/** machine_vesselShuttleGull: a courier, a light hull with its orange stripe on two skids. 21 × 7 cells. */
function gull(a) {
  const dy = 4;
  a.shape(poly(s8([0, 2.3, 2.2, 2.9, 2.2, 4.3, 0, 4.9], dy)), NOZZLE, {});
  for (const [x0, x1] of [[5.5, 4.8], [15, 15.7]]) a.line(`M${x0 * 8} ${5.2 * 8 + dy}L${x1 * 8} ${6.4 * 8 + dy}`, { c: '#5a6069', w: 1.6 });
  lit(a, 30, 54.4, 136, 56, DARK);
  const hull = poly(s8([2.2, 2.4, 5.2, 1.2, 12.4, .9, 16.6, 1.6, 21, 3.5, 19.8, 4.7, 13.4, 5.2, 3.2, 5.2, 2.2, 4.4], dy));
  a.shape(hull, palOf('#c9c6bd'), { lite: band(0, 0, 168, 18) });
  a.flat(band(16, 3.6 * 8 + dy, 160, 4.05 * 8 + dy), '#c8722e', { clip: hull });
  a.flat(poly(s8([12.6, 1.05, 16.2, 1.75, 18.6, 3, 13.6, 2.7], dy)), CANOPY);
}
/** machine_vesselShuttleMule: a cargo hauler, its cab forward over a yellow box, on two struts. 24 × 9 cells. */
function mule(a) {
  const dy = 4;
  for (const by of [1.6, 4]) a.shape(poly(s8([0, by, 2, by + .5, 2, by + 1.6, 0, by + 2.1], dy)), NOZZLE, {});
  for (const x of [44, 160]) lit(a, x, 6 * 8 + dy, x + 3.2, 72, DARK);
  a.shape(band(16, 1.6 * 8 + dy, 128, 2.3 * 8 + dy), MID, {});
  a.shape(band(16, 1.6 * 8 + dy, 35.2, 6.2 * 8 + dy), DARK, {});
  const box = band(36.8, 2.5 * 8 + dy, 124.8, 6.9 * 8 + dy);
  a.shape(box, palOf('#c9a03a'), { lines: Array.from({ length: 12 }, (_, i) => `M${43 + i * 6.8} ${2.9 * 8 + dy}L${43 + i * 6.8} ${6.5 * 8 + dy}`) });
  const cab = poly(s8([15.6, .6, 21.4, .6, 24, 2.6, 24, 5.6, 15.6, 5.6], dy));
  a.shape(cab, palOf('#b8b3a6'), { lite: band(124, 0, 192, 12) });
  hazardFoot(a, 124.8, 4.8 * 8 + dy, 67.2, 4.4, cab);
  a.flat(poly(s8([21.2, .9, 23.6, 2.8, 21.6, 2.8], dy)), CANOPY);
  lit(a, 36, 69.6, 176, 72, DARK);
}
/** machine_vesselShuttleBeetle: a tug, an orange dome on three legs, its claw arm forward. 16 × 9 cells. */
function beetle(a) {
  const dy = 4;
  for (const [lx, fx] of [[3.5, 2.2], [8, 8], [12.4, 13.6]]) {
    a.line(`M${lx * 8} ${5.6 * 8 + dy}L${fx * 8} ${8.1 * 8 + dy}`, { c: '#5a6069', w: 1.8 });
    lit(a, fx * 8 - 4.8, 68.4, fx * 8 + 4.8, 72, DARK);
  }
  const shell = [];
  for (let i = 0; i <= 16; i++) { const t = Math.PI + Math.PI * i / 16; shell.push(64 + Math.cos(t) * 49.6, 5.6 * 8 + dy + Math.sin(t) * 36.8); }
  const d = poly(shell);
  a.shape(d, palOf('#c8722e'), { lite: band(0, 0, 128, 14) });
  a.flat(band(12.8, 4.4 * 8 + dy, 115.2, 4.8 * 8 + dy), '#6a3f22', { clip: d });
  a.flat(ell(83.2, 2.9 * 8 + dy, 8.8, 7.2), CANOPY);
  a.tube(`M107.2 ${4.4 * 8 + dy}L121.6 ${5.4 * 8 + dy}L126.4 ${6.6 * 8 + dy}`, 1.8, LIGHT, { spec: false });
}
/** machine_vesselShuttleWhale: a lifeboat, long and pale, a row of portholes, its blue stripe. 24 × 7 cells. */
function whale(a) {
  const dy = 6;
  a.shape(poly(s8([0, 2.2, 2, 2.6, 2, 3.8, 0, 4.2], dy)), NOZZLE, {});
  a.shape(poly(s8([2.4, 1.5, 5, 1.5, 3.6, .2, 2.4, .2], dy)), MID, {});
  a.shape(poly(s8([2.4, 5, 5, 5, 3.6, 6.2, 2.4, 6.2], dy)), MID, {});
  const nose = [];
  for (let i = 0; i <= 10; i++) { const t = -Math.PI / 2 + Math.PI * i / 10; nose.push(21.6 + Math.cos(t) * 2.2, 3 + Math.sin(t) * 2.2); }
  const hull = poly(s8([2, .8, ...nose, 2, 5.2], dy));
  a.shape(hull, palOf('#d0cec6'), { lite: band(0, 0, 192, 14) });
  a.flat(band(16, 4.1 * 8 + dy, 184, 4.55 * 8 + dy), '#2d5a8a', { clip: hull });
  for (let x = 6; x < 20; x += 2) a.flat(ell(x * 8, 2.6 * 8 + dy, 3.6), x % 6 === 0 ? '#e8c27a' : CANOPY);
  lit(a, 24, 5.2 * 8 + dy, 168, 56, DARK);
}
/** machine_vesselShuttleHeron: a lander, a green hull high on three legs, its belly nozzle. 16 × 11 cells. */
function heron(a) {
  const dy = 7.2;
  for (const [lx, ly, fx] of [[4.2, 6, .8], [8, 6.4, 8], [11.8, 6, 15.2]]) {
    a.line(`M${lx * 8} ${ly * 8 + dy}L${fx * 8} ${86}`, { c: '#5a6069', w: 2 });
    lit(a, Math.max(0, fx * 8 - 5.6), 84.4, Math.min(128, fx * 8 + 5.6), 88, DARK);
  }
  a.shape(poly(s8([7, 6.4, 9, 6.4, 9.8, 8, 6.2, 8], dy)), NOZZLE, {});
  const hull = poly(s8([4, 1.4, 11.4, 1.4, 13.4, 3.2, 13.4, 6.4, 3, 6.4, 3, 2.4], dy));
  a.shape(hull, palOf('#6f7d6a'), { lite: band(0, 0, 128, 20) });
  a.flat(band(24, 5.3 * 8 + dy, 108, 5.65 * 8 + dy), '#c9b25a', { clip: hull });
  a.flat(poly(s8([11.2, 1.7, 13, 3.3, 11, 3.3], dy)), CANOPY);
  a.line(`M43.2 ${1.4 * 8 + dy}L43.2 ${dy}`, { c: '#7c838c', w: 1 });
}

/** vessel_pool_tiles: the pool hall's tiled wall, a tile that repeats. 8 × 8 cells. */
function poolTiles(a) {
  a.flat(band(0, 0, 64, 64), '#4d6b73');
  for (let x = 6.4; x < 64; x += 6.4) a.flat(band(x - .25, 0, x + .25, 64), '#3f5960');
  for (let y = 6.4; y < 64; y += 6.4) a.flat(band(0, y - .25, 64, y + .25), '#3f5960');
  a.flat(band(0, 0, 64, 1), '#6d8f98');
}
/** vessel_lifebuoy: a lifebuoy on the pool hall's wall, red and cream. 2 × 2 cells. */
function lifebuoy(a) {
  a.shape(ell(8, 8, 6.4), palOf('#c8522e'), { lite: ell(6.4, 5.6, 2.4, 1.6) });
  for (const t of [0, Math.PI / 2, Math.PI, Math.PI * 1.5]) {
    const at = (r, k) => [8 + Math.cos(t + k) * r, 8 + Math.sin(t + k) * r];
    a.flat(poly(...at(6.6, -.35), ...at(6.6, .35), ...at(3, .4), ...at(3, -.4)), '#e6dfcf');
  }
  a.cut(ell(8, 8, 3));
}
/** vessel_wainscot: the restaurant's panelled wainscot along its floor, a tile that repeats. 8 × 3 cells. */
function wainscot(a) {
  a.flat(band(0, 0, 64, 24), '#4b3324');
  a.flat(band(0, 0, 64, 2.4), '#6b4a33');
  for (let x = 2; x < 64; x += 16) { a.flat(band(x, 5.6, x + 12.8, 21), '#432d20'); a.flat(band(x, 5.6, x + 12.8, 6.2), '#3a271b'); }
}
/** vessel_bottles: two shelves of bottles over the bar. 7 × 4 cells. */
function bottles(a) {
  const cols = ['#3f6b3a', '#8a5a2a', '#cfd8d0', '#6a2f2a', '#c9a03a'], r = rng(331);
  for (const sy of [14.4, 32]) {
    lit(a, 0, sy - 1.6, 56, sy, WOOD);
    for (let x = 2.4; x < 54; x += 4.4) {
      const h = 4.8 + r() * 4, c = cols[Math.floor(r() * cols.length)];
      a.flat(band(x, sy - 1.6 - h, x + 2.6, sy - 1.6), c);
      a.flat(band(x + .8, sy - 3.6 - h, x + 1.8, sy - 1.6 - h), c);
    }
  }
}
/** vessel_pad: the hangar's landing pad along its floor, hazard paint on its edge, a tile that repeats. 8 × 1 cells. */
function pad(a) {
  a.flat(band(0, 0, 64, 8), '#2c3138');
  hazardFoot(a, 0, 0, 64, 3.2, band(0, 0, 64, 8));
}

// ---- A tanker's rooms (big-vessels, the mockup's drawItem): the pump room's pump and valves, a bay's tall tanks, the fuel column.
/** The mockup's CONTENTS by name, a stencil on each tank: fuel, water, oxygen, nitrogen, coolant. */
const CONTENT_IDS = ['FUEL', 'H2O', 'O2', 'N2', 'COOLANT'];
const PUMP_BODY = palOf('#4f5a62');

/** A handwheel at (x, y) of radius r in `col`: its rim, four spokes and the hub. */
function wheel(a, x, y, r, col) {
  a.shape(ell(x, y, r), palOf(col), { shade: .45 });
  a.flat(ell(x, y, r * .62), '#1f2125');
  for (const [dx, dy] of [[1, 0], [0, 1]]) a.line(`M${x - dx * r * .62} ${y - dy * r * .62}L${x + dx * r * .62} ${y + dy * r * .62}`, { c: col, w: .7 });
  a.flat(ell(x, y, r * .22), col);
}

/** machine_vesselPump: a pump housing on the floor, its pipe up out of it, its red handwheel, chevrons along its foot. 4 × 4 cells. */
function pump(a) {
  a.tube('M8 0L8 9', 2.2, PIPE, { spec: false });
  const d = band(0, 8, 32, 32);
  lit(a, 0, 8, 32, 32, PUMP_BODY);
  hazardFoot(a, 0, 28.8, 32, 3.2, d);
  wheel(a, 16, 19.2, 6.4, '#a33a2a');
  rivets(a, [3, 11], [29, 11]);
  patch(a, 25, 24, 2.4, 1.2, RUST, d, 401);
}

/** machine_vesselValves: the pump room's wall panel, three pipes down it, a handwheel on each, red, brass, red. 3 × 4 cells. */
function valves(a) {
  ['#5d646e', '#7a5a3e', '#5d646e'].forEach((c, k) => a.tube(`M${4 + k * 8} 0L${4 + k * 8} 32`, 1.8, palOf(c), { spec: false }));
  ['#a33a2a', '#c9a03a', '#a33a2a'].forEach((c, k) => wheel(a, 4 + k * 8, 8 + k * 7.2, 3.4, c));
}

/** A stencilled word on a baked picture `c` (4 px a world pixel), centred on (x, y), `size` tall, at most `wide` across. */
function stencilled(c, text, x, y, size, wide) {
  const S = 4, g = c.getContext('2d');
  g.save();
  g.fillStyle = 'rgba(235,228,212,.78)'; g.textAlign = 'center'; g.textBaseline = 'middle';
  g.font = `700 ${size * S}px Bahnschrift, "Arial Narrow", Consolas, sans-serif`;
  const k = Math.min(1, (wide * S) / g.measureText(text).width);
  g.translate(x * S, y * S); g.scale(k, 1); g.fillText(text, 0, 0);
  g.restore();
  return c;
}

/**
 * machine_vesselTankTall and its looks, machine_vesselFuelColumn3 and 4: an upright tank w × h pixels in its contents' paint, its
 * domed top with a flange where its pipe left, bands round it at its seams, a pale streak down its lit side.
 */
const tallTank = (w, h, col) => (a) => {
  const pal = palOf(col), dh = Math.min(24, w * .3), top = 3.2;
  const pts = [];
  for (let i = 0; i <= 12; i++) { const t = Math.PI + (Math.PI * i) / 12; pts.push(w / 2 + Math.cos(t) * w / 2, top + dh + Math.sin(t) * dh); }
  const d = poly(...pts, w, h, 0, h);
  lit(a, w / 2 - 4, 0, w / 2 + 4, top + 1, MID);
  a.shape(d, pal, { lite: band(0, 0, w * .12, h), shade: band(w * .78, 0, w, h), deep: band(0, h - 3.2, w, h) });
  for (let y = top + h * .3; y < h - 8; y += h * .28) a.flat(band(0, y, w, y + 3.6), pal.shade, { clip: d });
  a.flat(band(w * .18, top, w * .28, h), 'rgba(255,255,255,.08)', { clip: d });
  rivets(a, [w * .12, h - 6], [w * .88, h - 6]);
  patch(a, w * .7, h * .62, 3, 1.6, RUST, d, 409 + w + h);
};

// ---- The big classes' outsides (big-vessels, the mockup's drawPart): a liner's fin and pods, a tanker's manifold, domes and spheres,
// the bay doors under a hangar. What the livery paints (the fin's band and emblem, the doors' leaves) is drawn grey and tinted by the game.
const FIN = palOf('#b4b6b1'), POD = palOf('#cfcac0'), SPHERE = palOf('#b4ad9f'), MANIFOLD = palOf('#7a5a3e');

/** machine_vesselFin: the liner's fin over its service block, swept back, a radiator's slats along its foot. 13 × 10 cells. */
function fin(a) {
  const d = poly(0, 80, 32, 3.2, 68.8, 0, 104, 80);
  a.shape(d, FIN, { lite: poly(0, 80, 32, 3.2, 36, 3, 6, 80), shade: poly(68.8, 0, 104, 80, 92, 80, 64, 4), deep: band(0, 77, 104, 80) });
  for (let x = 16; x < 96; x += 5.6) a.line(`M${x} 57L${x} 77`, { c: 'rgba(60,64,70,.45)', w: .6 });
  a.line('M12 56L98 56', { w: .6 });
  rivets(a, [44, 20], [60, 20], [30, 50], [76, 50]);
  patch(a, 84, 66, 3.4, 1.6, RUST, d, 501);
}

/** vessel_emblem: the owner's band and ring on the fin, in the stripe's grey (the livery tints it), the fin showing through the ring. 13 × 10 cells. */
function emblem(a) {
  const S = { base: grey(222), lite: grey(242), shade: grey(186), deep: grey(160), spec: grey(250), ink: INK };
  a.shape(poly(17.6, 40, 88, 40, 92, 49.6, 12.8, 49.6), S, { shade: .5 });
  a.shape(ell(59.2, 22.4, 10.4), S, { lite: ell(56, 18.4, 4.8, 3.2), shade: .8 });
  a.cut(ell(59.2, 22.4, 4.8));
}

/** The arms a pod hangs from, out of its cradle's rail at x, leaning out by `lean`. */
function arms(a, x, foot, lean) {
  for (const [ax, k] of [[x + 8, -lean], [x + 32, lean]]) a.tube(`M${ax} ${foot}L${ax + k * 9.6} ${foot - 15}`, 2.6, MID, { spec: false });
}
/** machine_vesselPod: an escape pod in its cradle, cream with an orange band and a porthole. 5 × 4 cells. */
function pod(a) {
  arms(a, 0, 30, 0);
  const d = Anime.rr(1.6, 5.6, 36.8, 20, 10);
  a.shape(d, POD, { lite: band(0, 5.6, 40, 10), shade: band(0, 20, 40, 26) });
  a.flat(band(12.8, 5.6, 16.8, 26), '#c8722e', { clip: d });
  a.flat(ell(28, 15.6, 3.6), CANOPY);
  a.flat(ell(27, 14.4, 1.2, .8), 'rgba(220,240,240,.5)');
  lit(a, 3.2, 28.4, 36.8, 32, DARK);
  patch(a, 33, 22, 1.6, 1, RUST, d, 503);
}
/** machine_vesselCradle: a pod's cradle, its pod gone, the arms flung open, hazard paint along its rail. 5 × 3 cells. */
function cradle(a) {
  arms(a, 0, 22, .6);
  lit(a, 3.2, 20.4, 36.8, 24, DARK);
  hazardFoot(a, 3.2, 20.4, 33.6, 1.6, band(3.2, 20.4, 36.8, 22));
}

/** machine_vesselManifold: a segment of the dorsal manifold, a brown pipe over a pale one, clamps across both, its strut seats under. 12 × 2 cells. */
function manifold(a) {
  lit(a, 0, 1, 96, 5.4, MANIFOLD);
  lit(a, 0, 6.6, 96, 10.6, LIGHT);
  for (let x = 12; x < 96; x += 24) lit(a, x, 0, x + 2, 11.6, CONE);
  // Seats over the struts, in columns 2 and 8 (Vessels.Fits.ManifoldPosts).
  for (const x of [16, 64]) lit(a, x, 10.6, x + 8, 16, DARK);
  patch(a, 52, 3, 2.4, 1, RUST, band(0, 1, 96, 5.4), 505);
}
/** machine_vesselRiser: a length of the riser down from the manifold to a bay. 1 × 1 cell. */
function riser(a) {
  lit(a, 1.6, 0, 6.4, 8, LIGHT, { lite: band(1.6, 0, 2.6, 8), shade: band(5.4, 0, 6.4, 8) });
}
/** machine_vesselRiser2: the riser's top over the manifold, its valve's red wheel. 1 × 1 cell. */
function riserValve(a) {
  lit(a, 2.4, 4, 5.6, 8, LIGHT);
  wheel(a, 4, 4, 3.6, '#a33a2a');
}
/** machine_vesselDome: a tank bay's dome hatch on the hull, its wheel on top. 4 × 2 cells. */
function dome(a) {
  const pts = [];
  for (let i = 0; i <= 12; i++) { const t = Math.PI + (Math.PI * i) / 12; pts.push(16 + Math.cos(t) * 15.2, 16 + Math.sin(t) * 10.4); }
  const d = poly(pts);
  a.shape(d, palOf('#5f5a52'), { lite: poly(1, 16, 4, 9, 12, 6, 16, 5.6, 16, 8, 6, 16), shade: band(22, 0, 32, 16) });
  wheel(a, 16, 9.6, 3.2, '#3a3e45');
  lit(a, 0, 14.4, 32, 16, DARK);
}
/** machine_vesselSphere: a spherical tank slung under the keel on two struts, an ochre band round its middle. 10 × 11 cells. */
function sphere(a) {
  for (const [x0, x1] of [[16, 22], [64, 58]]) a.tube(`M${x0} 0L${x1} 18`, 2.4, MID, { spec: false });
  lit(a, 12, 0, 68, 2.4, DARK);
  const d = ell(40, 48, 38.4);
  a.shape(d, SPHERE, { lite: ell(28, 30, 16, 12), shade: [band(0, 62, 80, 88), band(62, 10, 80, 88)], deep: band(0, 78, 80, 88) });
  a.flat(band(0, 45.6, 80, 50.4), OCHRE, { clip: d });
  a.line('M4 40Q40 30 76 40', { w: .6 });
  rivets(a, [12, 48], [68, 48]);
  patch(a, 56, 70, 4, 2, RUST, d, 507);
}

/** vessel_bay_door: a hangar's bay doors in the bottom armour, a panel of a leaf in the hull's grey (the livery tints it), a tile that repeats. 8 × 2 cells. */
function bayDoor(a) {
  const D = { base: grey(162), lite: grey(196), shade: grey(122), deep: grey(92), spec: grey(214), ink: INK };
  lit(a, 0, 0, 64, 16, D, { lines: ['M0.6 0L0.6 16', 'M0 8L64 8'] });
  for (const x of [16, 48]) a.line(`M${x} 2L${x} 14`, { c: grey(120), w: .6 });
  rivets(a, [4, 4], [60, 4], [4, 12], [60, 12]);
}
/** vessel_bay_door_left and _right: the doors' ends, hazard paint where the leaves part from the hull. 2 × 2 cells. */
const bayDoorEnd = (right) => (a) => {
  hazardFoot(a, 0, 9.6, 16, 6.4, band(0, 9.6, 16, 16));
  a.line(right ? 'M15.4 0L15.4 16' : 'M0.6 0L0.6 16', { w: 1 });
};

function outsides() {
  out('machine_vesselFin', Anime.bake({ w: 104, h: 80, draw: fin }));
  out('vessel_emblem', Anime.bake({ w: 104, h: 80, draw: emblem }));
  out('machine_vesselPod', Anime.bake({ w: 40, h: 32, draw: pod }));
  out('machine_vesselCradle', Anime.bake({ w: 40, h: 24, draw: cradle }));
  out('machine_vesselManifold', Anime.bake({ w: 96, h: 16, draw: manifold }));
  out('machine_vesselRiser', Anime.bake({ w: 8, h: 8, draw: riser }));
  out('machine_vesselRiser2', Anime.bake({ w: 8, h: 8, draw: riserValve }));
  out('machine_vesselDome', Anime.bake({ w: 32, h: 16, draw: dome }));
  out('machine_vesselSphere', Anime.bake({ w: 80, h: 88, draw: sphere }));
  out('vessel_bay_door', Anime.bake({ w: 64, h: 16, tile: true, draw: bayDoor }));
  out('vessel_bay_door_left', Anime.bake({ w: 16, h: 16, draw: bayDoorEnd(false) }));
  out('vessel_bay_door_right', Anime.bake({ w: 16, h: 16, draw: bayDoorEnd(true) }));
}

function tanker() {
  out('machine_vesselPump', Anime.bake({ w: 32, h: 32, draw: pump }));
  out('machine_vesselValves', Anime.bake({ w: 24, h: 32, draw: valves }));
  // A look a content but fuel, the capsule's after its first (Vessels.TankTallContents).
  for (let k = 1; k < CONTENTS.length; k++)
    out('machine_vesselTankTall' + (k === 1 ? '' : String(k)), stencilled(Anime.bake({ w: 64, h: 88, draw: tallTank(64, 88, CONTENTS[k]) }), CONTENT_IDS[k], 32, 50, 9, 52));
  for (const [decks, h] of [[3, 152], [4, 208]])
    out('machine_vesselFuelColumn' + decks, stencilled(Anime.bake({ w: 80, h, draw: tallTank(80, h, CONTENTS[0]) }), CONTENT_IDS[0], 40, h * .5, 16, 64));
}

function liner() {
  out('machine_vesselBar', Anime.bake({ w: 56, h: 24, draw: bar }));
  out('machine_vesselPiano', Anime.bake({ w: 40, h: 24, draw: piano }));
  out('machine_vesselRoundTable', Anime.bake({ w: 24, h: 16, draw: roundTable }));
  out('machine_vesselChair', Anime.bake({ w: 8, h: 24, draw: chair(false) }));
  out('machine_vesselChair2', Anime.bake({ w: 8, h: 24, draw: chair(true) }));
  out('machine_vesselLounger', Anime.bake({ w: 32, h: 16, draw: lounger }));
  out('machine_vesselDiving', Anime.bake({ w: 40, h: 64, draw: diving }));
  out('machine_vesselPool', Anime.bake({ w: 160, h: 40, draw: pool }));
  out('machine_vesselChandelier', Anime.bake({ w: 24, h: 24, draw: chandelier }));
  out('machine_vesselStove', Anime.bake({ w: 32, h: 24, draw: stove }));
  out('machine_vesselCounter', Anime.bake({ w: 40, h: 16, draw: counter }));
  out('machine_vesselBed', Anime.bake({ w: 32, h: 16, draw: bed }));
  out('machine_vesselMedCab', Anime.bake({ w: 16, h: 40, draw: medCab }));
  out('machine_vesselFuelCart', Anime.bake({ w: 24, h: 16, draw: fuelCart }));
  CONTENTS.forEach((col, k) => out('machine_vesselCapsule' + (k === 0 ? '' : String(k + 1)), Anime.bake({ w: 112, h: 88, draw: capsule(col) })));
  out('machine_vesselShuttleGull', Anime.bake({ w: 168, h: 56, draw: gull }));
  out('machine_vesselShuttleMule', Anime.bake({ w: 192, h: 72, draw: mule }));
  out('machine_vesselShuttleBeetle', Anime.bake({ w: 128, h: 72, draw: beetle }));
  out('machine_vesselShuttleWhale', Anime.bake({ w: 192, h: 56, draw: whale }));
  out('machine_vesselShuttleHeron', Anime.bake({ w: 128, h: 88, draw: heron }));
  out('vessel_pool_tiles', Anime.bake({ w: 64, h: 64, tile: true, draw: poolTiles }));
  out('vessel_lifebuoy', Anime.bake({ w: 16, h: 16, draw: lifebuoy }));
  out('vessel_wainscot', Anime.bake({ w: 64, h: 24, tile: true, draw: wainscot }));
  out('vessel_bottles', Anime.bake({ w: 56, h: 32, draw: bottles }));
  out('vessel_pad', Anime.bake({ w: 64, h: 8, tile: true, draw: pad }));
}

Subjects.vessel = function () {
  skin();
  furniture();
  liner();
  tanker();
  outsides();
  out('vessel_frost', Anime.bake({ w: 40, h: 48, draw: frost }));
  out('vessel_cable', Anime.bake({ w: 24, h: 32, draw: cable }));
  out('vessel_soot', Anime.bake({ w: 48, h: 40, draw: soot }));
  out('vessel_pipe', Anime.bake({ w: 64, h: 16, draw: pipe }));
  out('vessel_fallen', Anime.bake({ w: 32, h: 48, draw: fallen }));
  out('vessel_toppled', Anime.bake({ w: 40, h: 24, draw: toppled }));
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
