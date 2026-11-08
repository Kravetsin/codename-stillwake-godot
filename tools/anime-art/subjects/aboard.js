// A vessel's salvage as it stood in service: each debris piece of debris.js whole and level on the deck, its parts, paint and wear kept,
// its tears, burns and loose ends gone. Drawn with debris.js's kit, read when this runs: aboard sorts before debris.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;

Subjects.aboard = function () {
  const { HULL, PLATE, OLD, GUN, SOOT, PLANK, PAINT, COPPER, ARMY, BLUE, CREAM, STAINED, STRIPE, WIRES,
    band, plate, well, rv, rusted, hazard, wire, port, boards, grain, bell, neck, block, vent, strut, boxCrate, planksStack, bottle, wall, door, shelf, tag } = window.DebrisKit;
  const BOARD = { base: '#3a6a48', shade: '#284e38', deep: '#1a3628', lite: '#588a60', spec: '#a0c8a0', ink: '#0c120e' };
  /** A box lit along its top, shaded along its foot and far side, its line round it. */
  function box(a, x, y, w, h, m, o) {
    o = o || {};
    const l = o.l ?? Math.min(1, h * .14), b = o.b ?? Math.min(1.4, h * .16), e = o.e ?? Math.min(1.2, w * .1), d = o.r ? rr(x, y, w, h, o.r) : band(x, y, x + w, y + h);
    a.shape(d, m, { lite: o.lite ?? band(x, y, x + w, y + l + .4), shade: o.shade ?? [band(x, y + h - b - .2, x + w, y + h), band(x + w - e - .3, y, x + w, y + h)], deep: o.deep, lines: o.lines, outer: o.outer });
    return d;
  }
  const inner = (a, x, y, w, h, m, o) => box(a, x, y, w, h, m, { outer: false, ...o });
  /** A plank seen along its edge: lit top, dark foot, a nail near each end. */
  function plank(a, x, y, w, h, o) {
    a.shape(band(x, y, x + w, y + h), PLANK, { lite: band(x, y, x + w, y + h * .32), shade: band(x, y + h * .68, x + w, y + h), outer: o?.outer });
    a.flat(band(x, y + h * .1, x + .6, y + h * .9), PLANK.lite);
    a.flat(ell(x + 1.6, y + h * .5, .28), PLANK.deep); a.flat(ell(x + w - 1.6, y + h * .5, .28), PLANK.deep);
  }
  const ironCorner = (a, x, y, u, v) => a.shape(poly(x, y, x + u * 3.2, y, x + u * 3.2, y + v * 1.4, x + u * 1.4, y + v * 1.6, x + u * 1.4, y + v * 3.2, x, y + v * 3.2), OLD, { outer: false, lite: band(x - 2, y - 2, x + 4, y + (v > 0 ? .5 : -1)) });

  /** crate_01: the board crate, its three boards and iron corners. */
  function crate(a) {
    const d = plate(a, [[.5, .5], [23.5, .5], [23.5, 16], [.5, 16]], PLANK, { seed: 31, l: .8 });
    boards(a, d, 1, 1.4, 22, 13.8, 3, PLANK, [0, 1, 0]);
    grain(a, 'M3.4 3.4L8.4 3.6', 'M15.6 3.8L19.6 3.6', 'M11 8.6L17 8.4', 'M5 13.2L10 13.4');
    for (const [x, y, u, v] of [[.5, .5, 1, 1], [23.5, .5, -1, 1], [.5, 16, 1, -1], [23.5, 16, -1, -1]]) ironCorner(a, x, y, u, v);
    rv(a, [1.6, 1.6], [22.4, 1.6], [1.6, 14.9], [22.4, 14.9]);
    rusted(a, 21.4, 13.6, .7, .5, d);
  }

  /** pod_01: the yellow capsule on its belt, its four legs down to the deck. */
  function pod(a) {
    for (const x of [2.6, 6, 9.4, 12.6]) {
      a.shape(poly(x - .9, 11, x + .9, 11, x + .6, 14.8, x - .6, 14.8), GUN, { lite: band(x - 1, 11, x - .3, 15), shade: band(x + .2, 11, x + 1, 15) });
      a.shape(rr(x - 1.15, 14.6, 2.3, 1.4, .3), OLD, { lite: band(x - 1.2, 14.6, x + 1.2, 15.1) });
    }
    plate(a, [[1.2, 10.4], [14.8, 10.4], [14.8, 13.4], [1.2, 13.4]], PLATE, { seed: 41, l: .7 });
    const d = plate(a, [[8, .9], [9.6, 2.6], [12, 6.6], [14.8, 10.6], [1.2, 10.6], [4, 6.6], [6.4, 2.6]], PAINT, { seed: 42, lite: poly(8, .9, 6.4, 2.6, 4, 6.6, 1.2, 10.6, 3.6, 10.6, 6, 6.6, 8.2, 3), shade: poly(9.6, 2.6, 12, 6.6, 14.8, 10.6, 10.6, 10.6, 9.4, 6) });
    a.shape(poly(6.4, 5.2, 9.6, 5.2, 10.4, 7, 9.4, 8.6, 6.6, 8.6, 5.6, 7), GUN, { outer: false, deep: band(5, 5, 11, 6) });
    a.flat(poly(6.6, 6.6, 7.4, 6.2, 7.6, 6.6, 6.8, 7.4), GUN.lite);
    rusted(a, 3.6, 9.6, .7, .5, d); rv(a, [4, 12], [8, 12], [12, 12]);
  }

  /** tanks_01: three bottles strapped in their frame on the skid; drawn on debris's rows, moved down by the shift. */
  function tanks(a) {
    const tank = (x, y, w, bot) => {
      a.shape(band(x + w * .3, y - 2.6, x + w * .7, y + .4), PLATE, { lite: band(x + w * .3, y - 2.6, x + w * .7, y - 1.8), shade: band(x + w * .56, y - 2.6, x + w * .7, y + .4) });
      return plate(a, [[x + 1.8, y], [x + w - 1.8, y], [x + w, y + 2], [x + w, bot], [x, bot], [x, y + 2]], BLUE, { lite: band(x + 1.2, y, x + 2.8, bot), shade: [band(x + w - 2.2, y, x + w, bot)] });
    };
    const a1 = tank(2, 5.4, 8.4, 28); tank(12, 2.4, 8.4, 28); tank(22, 8.6, 8.4, 28);
    rusted(a, 4.6, 8, .7, .7, a1);
    a.shape(ell(28.2, 5.4, 2.6), STAINED, { shade: .5, lite: ell(27.8, 4.9, 1.8) }); a.flat(ell(28.2, 5.4, 1), SOOT.deep);
    a.tube('M27 8L27 6.6', 1, PLATE, { spec: false });
    for (const x of [3.6, 13.6, 23.6]) { a.flat(band(x, 16.4, x + 5, 21.4), CREAM.base); a.flat(band(x, 20.6, x + 5, 21.4), CREAM.shade); a.flat(band(x + 1.2, 17.8, x + 3.8, 19), BLUE.base); }
    for (const y of [12.4, 22]) a.shape(band(.4, y, 31.6, y + 1.8), OLD, { lite: band(0, y, 32, y + .5), shade: band(0, y + 1.2, 32, y + 1.8) });
    rv(a, [1.4, 13.2], [11.2, 13.2], [21.2, 13.2], [30.6, 13.2], [1.4, 22.8], [11.2, 22.8], [21.2, 22.8], [30.6, 22.8]);
    plate(a, [[1, 27.6], [31, 27.6], [31, 31.4], [1, 31.4]], GUN, { l: .9, b: 1.2, e: 1 });
    for (const x of [4, 9, 14, 19, 24, 28]) a.flat(rr(x, 29.4, 1.6, .9, .2), SOOT.deep);
  }

  /** rack_01: the cabinet of drawers, its board seated, its cables bundled down its side into a duct on the deck. */
  function rack(a) {
    const C = (c) => ({ base: c, shade: c, lite: c });
    for (const [y, c, x, w] of [[6, WIRES.red, 29.4, .8], [10.8, WIRES.orange, 28.3, .8], [15.8, WIRES.yellow, 27.2, .9], [20.8, WIRES.red, 26.1, .8], [25, WIRES.blue, 25, .9]]) {
      a.tube(`M21.6 ${y}L${x - 1.2} ${y}Q${x} ${y} ${x} ${y + 1.2}L${x} 30`, w, C(c), { spec: false });
    }
    for (const [x0, y0] of [[27.6, 12.6], [25.4, 22.6]]) inner(a, x0, y0, 30.2 - x0, 1.2, OLD, { l: .3, b: .3, e: .3 });
    box(a, 23.6, 28.2, 7.8, 3.8, OLD, { l: .6, b: .8, e: .7 });
    a.flat(band(24.6, 29.6, 30.4, 30.4), SOOT.deep);
    plate(a, [[1.4, .6], [22.6, .6], [22.6, 32], [1.4, 32]], GUN, { l: 1.4, b: 1.6, e: .8 });
    a.flat(band(2.4, .6, 3.2, 32), GUN.lite);
    const drawer = (y, m, leds) => {
      inner(a, 4.4, y, 16.4, 4.2, m, { l: 1, b: 1.2, e: .6 });
      a.flat(band(9.6, y + 2, 16.6, y + 2.8), SOOT.deep);
      a.flat(ell(5.6, y + 1.4, .4), PLATE.lite);
      for (const [x, c] of leds) a.flat(band(x, y + .9, x + .9, y + 1.7), c);
    };
    drawer(4.4, HULL, [[16.2, '#7cc070'], [17.8, '#7cc070']]);
    drawer(9.2, OLD, [[17.6, '#c89838']]);
    well(a, 4.4, 14, 16.4, 4.6);
    a.shape(band(5.4, 14.6, 18.8, 18.2), BOARD, { outer: false, lite: band(5.4, 14.6, 18.8, 15.1), shade: band(5.4, 17.6, 18.8, 18.2) });
    for (const x of [6.6, 9.6, 12.6]) a.flat(band(x, 15.4, x + 2, 17), SOOT.base);
    for (const x of [15.4, 16.6, 17.8]) a.flat(ell(x, 16.2, .32), STRIPE[0]);
    inner(a, 18.8, 14.6, 1.4, 3.6, OLD, { l: .3, b: .4, e: .3 });
    drawer(19, HULL, [[17.4, '#b84030']]);
    drawer(23.8, OLD, []);
    a.flat(band(4.4, 29, 20.8, 31), SOOT.base); a.flat(band(4.4, 29, 20.8, 29.5), SOOT.deep);
    rusted(a, 3, 2.4, .8, .8); rv(a, [3, 8.4], [3, 18], [3, 27.6]);
  }

  /** section_01: the hull section whole, every panel and port of its dark band, the pale armour along its foot. */
  function section(a) {
    const d = plate(a, [[.6, 2], [3.6, .4], [44.4, .4], [47.4, 2], [47.4, 32], [.6, 32]], HULL, { seed: 83, l: 1, b: .1, e: .1 });
    for (const [x0, y0, x1, y1] of [[14.6, 1, 15.6, 19], [27.6, 1, 28.4, 19], [40.6, 1, 41.4, 19], [15, 10.6, 48, 11.4]]) a.flat(band(x0, y0, x1, y1), HULL.deep, { clip: d });
    a.flat(poly(17, 2, 26, 2, 26, 7, 21, 9.4, 17, 9), HULL.lite, { clip: d });
    a.flat(band(0, 19.2, 48, 23.4), SOOT.base, { clip: d });
    for (const x of [5, 11.4, 17.8, 24.2, 30.6, 37, 43.4]) { a.flat(ell(x, 21.2, 1.4, 1), SOOT.deep, { clip: d }); a.flat(poly(x - 1, 21.6, x + 1, 21.6, x + .6, 22.2, x - .6, 22.2), OLD.lite, { clip: d }); }
    a.flat(band(0, 23.4, 48, 32), PLATE.base, { clip: d }); a.flat(band(0, 23.4, 48, 24.4), PLATE.lite, { clip: d }); a.flat(band(0, 30.4, 48, 32), PLATE.shade, { clip: d });
    for (const x of [19.6, 37.6]) a.flat(band(x, 24, x + .8, 30.4), PLATE.deep, { clip: d });
    hazard(a, 24.6, 25.2, 10.6, 3.4, { step: 4, bare: true, clip: band(24.6, 25.2, 35.2, 28.6) });
    rusted(a, 4, 16.6, 1.2, .9, d); rusted(a, 45.4, 27.6, .9, .9, d);
    port(a, 7.4, 9.4, 6.6);
    a.shape(band(32.6, 3.4, 36, 6.8), GUN, { outer: false, lite: band(32.6, 3.4, 36, 3.8) }); a.flat(band(33.6, 4.4, 35, 5.8), SOOT.deep);
    rv(a, [17.4, 3], [24.6, 3], [30.6, 3], [38.4, 3], [43.6, 3], [17.6, 13.4], [25.4, 13.4], [17.6, 17.4], [25.4, 17.4], [30.6, 13.4], [38.4, 13.4], [43.6, 13.4], [43.6, 17.4],
      [4, 26.4], [9.6, 26.6], [15, 26.4], [40.6, 26.4], [45, 26.4]);
  }

  /** machine_01: the block, its saw whole, its piston home in its housing on a foot; on debris's rows, moved down by the shift. */
  function machine(a) {
    wire(a, WIRES.red, 'M9.6 3L10 .8L14 1.2L14.4 3', .8);
    const teeth = []; for (let i = 0; i < 22; i++) { const t = (i / 22) * Math.PI * 2 - .3, k = i % 2 ? 7.6 : 10; teeth.push(32 + Math.cos(t) * k, 11.6 + Math.sin(t) * k); }
    a.shape(poly(teeth), PLATE, { lite: poly(23, 12, 26, 5, 32, 3, 30, 8, 26, 13), shade: poly(40, 8, 43, 16, 36, 23, 29, 23, 33, 17, 37, 14) });
    a.flat(ell(32, 11.6, 3.6), PLATE.shade); a.flat(ell(31.6, 11.2, 2.6), '#807868'); a.flat(ell(31.2, 10.8, .9), PLATE.lite);
    const d = plate(a, [[2.6, 3], [26, 3], [28, 5], [28, 31.4], [1, 31.4], [1, 4.6]], GUN, { l: 1.2, b: 1.6, e: .8 });
    vent(a, 5, 6, 11.4, 9, 5);
    a.flat(band(1, 16.4, 28, 17.2), SOOT.deep, { clip: d }); a.flat(band(1, 17.2, 28, 17.6), GUN.lite, { clip: d });
    hazard(a, 3.4, 22.6, 20, 4, { step: 4.4, bare: true });
    rusted(a, 22, 7, 1, .8, d); rusted(a, 5, 29.4, .9, .6, d);
    rv(a, [3, 4.8], [19.6, 5], [3, 15], [19.6, 14], [26, 21], [4, 29.6], [14, 29.6], [24, 29.6], [20, 19.4]);
    for (const x of [29.4, 34.6]) a.shape(rr(x, 27.6, 2.8, 3.8, .3), GUN, { lite: band(x, 27.6, x + 1, 31.4), shade: band(x + 1.9, 27.6, x + 2.8, 31.4) });
    a.tube('M37 24L42.4 24', 2.2, PLATE);
    plate(a, [[42.4, 21], [45.8, 21.4], [45.8, 26.8], [42.4, 27.2]], PLATE, { l: .8, b: 1, e: .6 });
    plate(a, [[27.4, 20.4], [38, 20.4], [38, 28.4], [27.4, 28.4]], OLD, { l: .8, b: 1, e: .8 });
    for (const x of [29, 31.4, 33.8, 36.2]) { a.flat(band(x, 21.4, x + 1, 27.4), SOOT.base); a.flat(band(x + 1, 21.4, x + 1.3, 27.4), OLD.lite); }
  }

  /** The shell of a corrugated container: castings at its feet, ribs, corner posts, its stencil; top at y. */
  function containerShell(a, y) {
    for (const x of [1, 57.6]) box(a, x, 29, 5.4, 3, GUN, { l: .5, b: .8, e: .8 });
    const d = box(a, 1, y, 62, 29.4 - y, ARMY, { l: .8, b: 1.6, e: 1.2 });
    for (let x = 4.6; x < 58; x += 2.6) { a.flat(band(x, y + 1.6, x + .9, 27.4), ARMY.shade, { clip: d }); a.flat(band(x + .9, y + 1.6, x + 1.3, 27.4), ARMY.lite, { clip: d }); }
    for (const x of [1.2, 60.2]) { a.flat(band(x, y + .8, x + 2.6, 28.8), ARMY.shade, { clip: d }); a.flat(band(x, y + .8, x + .6, 28.8), ARMY.lite, { clip: d }); }
    a.flat(band(1, 27.4, 63, 28), ARMY.deep, { clip: d });
    a.line(`M3.8 ${y + .8}L3.8 28`, { w: 1.1 }); a.line(`M60.2 ${y + .8}L60.2 28`, { w: 1.1 });
    rv(a, [2.5, y + 2.2], [2.5, 26.2], [61.5, y + 2.2], [61.5, 26.2]);
    return d;
  }
  const stencil = (a, x, y, clip) => { for (const [u, v, w, h] of [[0, 0, 1.4, 6], [2.6, 0, 1.4, 6], [6, 0, 4.4, 1.2], [6, 4.8, 3, 1.2], [12, 0, 1.4, 6]]) a.flat(band(x + u, y + v, x + u + w, y + v + h), '#a6ae94', { clip }); };
  /** container_01, the picked design (salvage picks, A): shut tight, a bundle of planks lashed on its roof with two straps. */
  function container(a) {
    const d = containerShell(a, 9);
    stencil(a, 14, 14.6, d);
    rusted(a, 3, 27.6, 1.4, 1, d); rusted(a, 61.4, 12, 1, 1.2, d); rusted(a, 38, 28, 1.6, .8, d);
    for (const x of [12, 46]) box(a, x, 7.6, 3.4, 1.6, OLD, { l: .3, b: .4, e: .4 });
    plank(a, 6.6, 5.6, 49, 2); plank(a, 9.4, 3.6, 48.4, 2); plank(a, 7.4, 1.6, 44.6, 2);
    for (const x of [20, 42]) {
      a.shape(band(x, 1.1, x + 1.3, 9.2), { ...PAINT, base: STRIPE[0] }, { outer: false, lite: band(x, 1.1, x + .4, 9.2), shade: band(x + .9, 1.1, x + 1.3, 9.2) });
      box(a, x - .5, 4.2, 2.3, 2.2, OLD, { l: .4, b: .5, e: .5 });
    }
  }

  /** slab_01: the armour slab standing on its foot, its edges straight, every bolt in; on debris's rows, moved down by the shift. */
  function slab(a) {
    const d = plate(a, [[1.4, .6], [46, .6], [62.4, 12], [62.4, 30.4], [1.4, 30.4]], SOOT, { seed: 84, lite: band(0, 0, 64, 1), shade: band(0, 23.6, 64, 26) });
    a.flat(band(4.6, 2, 22.6, 24), GUN.shade, { clip: d });
    a.flat(band(24.4, 2, 42.6, 24), GUN.shade, { clip: d });
    a.flat(poly(44.4, 2, 45.6, 2, 60.6, 13, 60.6, 24, 44.4, 24), GUN.shade, { clip: d });
    for (const [x, y] of [[9, 8], [15, 12], [11, 19], [19, 19], [6.6, 14], [18, 6], [28, 4], [39, 21], [50, 9], [54, 19], [47, 15], [58, 21], [49, 21]]) a.flat(poly(x, y, x + .8, y - .4, x + 1.2, y + .3, x + .3, y + .7), GUN.base, { clip: d });
    a.flat(band(0, 24, 64, 25), GUN.lite, { clip: d });
    hazard(a, 3.4, 26, 58, 3.2, { step: 6, bare: true, clip: band(3.4, 26, 61, 29.2) });
    for (const [x, y] of [[8, 5.6], [19, 5.6], [8, 20], [19, 20], [28.4, 21.4], [38.6, 21.4], [48.4, 5.6], [48.4, 20.6], [57.4, 20.6]]) a.shape(poly(x - 1.4, y - 1, x + .6, y - 1.6, x + 1.6, y, x + .8, y + 1.5, x - 1, y + 1.4), PLATE, { outer: false, shade: band(x, y - 2, x + 2, y + 2), lite: band(x - 2, y - 2, x + 2, y - .8) });
    a.shape(band(27, 7, 40, 18), PAINT, { lite: band(27, 7, 40, 7.8), shade: band(27, 16.8, 40, 18) });
    a.flat(poly(29, 8.6, 33.4, 11.6, 38, 8.6, 38, 10.4, 35.2, 12.4, 37, 16.4, 30, 16.4, 31.8, 12.4, 29, 10.4), SOOT.base);
    for (const [x, y, rx, ry] of [[5.4, 12.6, 1.1, .8], [58.6, 15.4, 1, .8], [41.6, 3, 1, .7]]) rusted(a, x, y, rx, ry, d);
    a.line('M22.6 2L22.6 24', { w: 1.5 }); a.line('M24.4 2L24.4 24', { w: 1.1 }); a.line('M42.6 2L42.6 24', { w: 1.5 }); a.line('M44.4 2L44.4 24', { w: 1.1 });
  }

  /** engine_01: the block on its mounts, its fan whole behind a guard, the bell on a saddle, its wires run down to a box; on debris's rows, moved down by the shift. */
  function engine(a) {
    a.tube('M18 10L18 4.2L51.4 4.2L51.4 16', 2.2, COPPER);
    for (const x of [24, 36]) a.flat(band(x, 3, x + 1, 9), OLD.shade);
    strut(a, 'M8 15L4.6 15.6L4.6 22.4L8 23');
    for (const [y, c, x] of [[34, WIRES.red, 5.6], [36, WIRES.blue, 4.4], [38, WIRES.yellow, 3.2]]) wire(a, c, `M9 ${y}L${x + 1} ${y}Q${x} ${y} ${x} ${y + 1}L${x} 43`);
    box(a, 1, 42.4, 7.4, 4.6, OLD, { l: .6, b: .9, e: .8 }); a.flat(band(2.2, 44.6, 7, 45.2), SOOT.deep);
    a.shape(poly(62.6, 33, 68.6, 36, 68.6, 47, 62.6, 47), GUN, { lite: band(62.6, 30, 63.8, 47), shade: band(67.2, 30, 68.6, 47) });
    box(a, 60.8, 45, 9.6, 2, OLD, { l: .5, b: .6, e: .6 });
    bell(a, 57, 78, 24, 8.6, 22.6);
    neck(a, 51, 24, 8.8, 1, 11.6);
    for (const x of [11, 38.6]) box(a, x, 41, 5.4, 6, GUN, { l: .6, b: 1, e: .9 });
    block(a, 8, 9.6, 39, 32);
    // The fan in its housing, whole, behind its guard.
    const cx = 18.2, cy = 23.8, r = 6.2;
    a.shape(band(11.4, 15.6, 25, 32), SOOT, { outer: false, deep: band(11.4, 15.6, 25, 16.8), shade: band(11.4, 15.6, 12.2, 32) });
    for (let i = 0; i < 6; i++) {
      const t = .4 + (i / 6) * Math.PI * 2, at = (u, k) => [cx + Math.cos(t + u) * r * k, cy + Math.sin(t + u) * r * k];
      a.flat(poly(...at(0, .24), ...at(0, 1), ...at(.62, .94), ...at(.62, .3)), OLD.base);
      a.flat(poly(...at(0, .24), ...at(0, 1), ...at(.16, 1), ...at(.1, .26)), OLD.lite);
    }
    a.flat(ell(cx, cy, 1.8), PLATE.base); a.flat(ell(cx - .3, cy - .3, .8), PLATE.lite);
    a.line(`M11.8 ${cy}L24.6 ${cy}`, { w: 1, c: OLD.lite }); a.line(`M${cx} 16.4L${cx} 31.6`, { w: 1, c: OLD.lite });
    a.line(ell(cx, cy, r + .4), { w: 1, c: OLD.lite });
    vent(a, 30, 15.8, 13.6, 9.2, 5);
    a.shape(band(31.6, 27, 41, 33), PAINT, { outer: false, lite: band(31.6, 27, 41, 27.6), shade: band(31.6, 31.8, 41, 33) });
    a.flat(band(34.6, 28.8, 38, 31.2), SOOT.deep); a.flat(ell(36.3, 30, .5), PAINT.lite);
    rusted(a, 45, 39.6, .9, .6);
  }

  /** hold_01: the hold's wall whole, its door, and its cargo bay framed in hazard paint, the same load on its shelf. */
  function hold(a) {
    wall(a, [[1.4, 3], [94.6, 3], [94.6, 48], [1.4, 48]], 71, 1.4, 3, 94.6, 48, [36, 44]);
    door(a, 7, 9, 19, 33.4, { port: [12, 16.6] });
    tag(a, 29.6, 12.6);
    rusted(a, 4.2, 41.6, 1, .7);
    a.flat(band(49, 11, 90, 43), SOOT.base); a.flat(band(49, 11, 90, 14), SOOT.deep);
    hazard(a, 49, 11, 41, 1.8, { step: 3.6 }); hazard(a, 49, 41.2, 41, 1.8, { step: 3.6 });
    for (const x of [49, 88.2]) { a.flat(band(x, 12.8, x + 1.8, 41.2), SOOT.base); for (let v = 13.4; v < 39; v += 3.6) a.flat(poly(x, v + 1.8, x + 1.8, v, x + 1.8, v + 1.4, x, v + 3.2), STRIPE[0], { clip: band(x, 12.8, x + 1.8, 41.2) }); }
    a.line(band(49, 11, 90, 43), { w: 1.4 });
    boxCrate(a, 54.6, 14.4, 11, 11.6); boxCrate(a, 66.6, 17.4, 8.4, 8.6);
    bottle(a, 76.4, 15.6, 3.2, 10.4); bottle(a, 81, 18.2, 3.2, 7.8);
    shelf(a, 50.8, 88.2, 26);
    boxCrate(a, 54.4, 28.6, 9.6, 12.6);
    planksStack(a, 66, 30.4, 14, 10.8, 5);
    boxCrate(a, 80.6, 32.4, 6.6, 8.8);
  }

  const pics = { Crate: [24, 16, crate], Pod: [16, 16, pod], Tanks: [32, 32, tanks, [0, .6]], Rack: [32, 32, rack], Section: [48, 32, section], Machine: [48, 32, machine, [0, .6]],
    Container: [64, 32, container], Slab: [64, 32, slab, [0, 1.6]], Engine: [80, 48, engine, [0, 1]], Hold: [96, 48, hold] };
  for (const [n, [w, h, draw, shift]] of Object.entries(pics)) out(`machine_salvage${n}`, bake({ w, h, draw, shift }));
};
})();
