// A vessel's salvage as it stood in service: each debris piece of debris.js whole and level on the deck, its parts, paint and wear kept,
// its tears, burns and loose ends gone. Drawn with debris.js's kit, read when this runs: aboard sorts before debris.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;

Subjects.aboard = function () {
  const { HULL, PLATE, OLD, GUN, SOOT, PLANK, PAINT, COPPER, ARMY, BLUE, CREAM, STAINED, STRIPE, WIRES,
    band, plate, well, rv, rusted, slats, hazard, wire, port, boards, grain, bell, neck, block, vent, strut, boxCrate, planksStack, bottle, wall, door, shelf, tag } = window.DebrisKit;
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

  // ---------- the vessel's main engine and its bridge consoles: the picked designs (engine-console picks), not debris ----------
  const VIOLET = { base: '#4c3f72', shade: '#322a50', deep: '#221d38', lite: '#6c5c98', spec: '#a498c4', ink: '#0c0d13' };
  const GLASS = { base: '#5d94ac', shade: '#3a6a84', deep: '#274a62', lite: '#8cc0d0', spec: '#d6eef2', ink: '#0c1420' };
  const TEAL = { base: '#3f7f86', shade: '#2a5a64', deep: '#1c3f4a', lite: '#62a0a2', spec: '#a8d0cc', ink: '#0c1418' };
  const SPOOL = {
    red: { base: '#a83c30', shade: '#742a22', deep: '#4e1c18', lite: '#c45a4a', spec: '#d88a7c', ink: '#160c0a' },
    yellow: { base: '#b8922e', shade: '#7e6424', deep: '#54431e', lite: '#d0ac4a', spec: '#e2c870', ink: '#16120a' },
  };
  const LIT = { green: '#8fd070', amber: '#e8a83c', red: '#d8503c' };
  const SCREEN = { base: '#1f2b2e', shade: '#172125', deep: '#10181b', lite: '#2b3b3f', spec: '#5d7c80', ink: '#0c1418' };
  const TRACE = '#5f9e88', FAINT = '#2f4a4a';
  const path = (...v) => 'M' + v.reduce((s, n, i) => s + (i % 2 ? ' ' : i ? 'L' : '') + n, '');
  /** A standing cylinder: lit down its near side, dark down its far one. */
  const cyl = (a, x, y, w, h, m, o) => a.shape(rr(x, y, w, h, o?.r ?? w * .22), m, { outer: o?.outer, lite: band(x + w * .14, y, x + w * .36, y + h), shade: band(x + w * .64, y, x + w, y + h), deep: band(x + w * .88, y, x + w, y + h) });
  /** A drum lying on its side: lit along its top, dark along its foot. */
  const drum = (a, x, y, w, h, m, o) => a.shape(rr(x, y, w, h, o?.r ?? h * .22), m, { outer: o?.outer, lite: band(x, y + h * .08, x + w, y + h * .3), shade: band(x, y + h * .64, x + w, y + h), deep: band(x, y + h * .86, x + w, y + h) });
  /** A rotor in its dark ring: n blades swept at turn, a hub. */
  function rotor(a, cx, cy, r, n, turn, m) {
    for (let i = 0; i < n; i++) {
      const t = turn + (i / n) * Math.PI * 2, c = Math.cos(t), s = Math.sin(t), c2 = Math.cos(t + .62), s2 = Math.sin(t + .62);
      a.flat(poly(cx + c * r * .22, cy + s * r * .22, cx + c * r, cy + s * r, cx + c2 * r * .94, cy + s2 * r * .94, cx + c2 * r * .3, cy + s2 * r * .3), (m || PLATE).base);
      a.flat(poly(cx + c * r * .22, cy + s * r * .22, cx + c * r, cy + s * r, cx + Math.cos(t + .16) * r, cy + Math.sin(t + .16) * r, cx + Math.cos(t + .1) * r * .24, cy + Math.sin(t + .1) * r * .24), (m || PLATE).lite);
    }
    a.flat(ell(cx, cy, r * .26), OLD.base); a.flat(ell(cx - r * .04, cy - r * .06, r * .12), OLD.lite);
  }
  const led = (a, x, y, c) => { a.flat(ell(x, y, .62), '#14131c'); a.glow(ell(x, y, .42), c); };
  /** The drive's block, whole: a chamfered box, rivets along the top, hazard paint along the foot (debris's block is torn). */
  function driveBlock(a, x, y, w, h) {
    const d = poly(x + 2, y, x + w - 2, y, x + w, y + 2, x + w, y + h - 2, x + w - 2, y + h, x + 2, y + h, x, y + h - 2, x, y + 2);
    a.shape(d, GUN, { lite: band(x, y, x + w, y + 1.6), shade: [band(x, y + h - 2, x + w, y + h), band(x + w - 1.7, y, x + w, y + h)] });
    a.shape(band(x + 1.4, y + 4, x + w - 1.4, y + h - 2.4), SOOT, { outer: false, lite: band(x + 1.4, y + 4, x + w - 1.4, y + 4.4), shade: band(x + 1.4, y + h - 4, x + w - 1.4, y + h - 2.4) });
    a.line(`M${x + 1.4} ${y + 4}L${x + w - 1.4} ${y + 4}`, { w: 1.4 });
    const top = []; for (let u = x + 4; u < x + w - 2; u += 5.6) top.push([u, y + 2.1]);
    rv(a, ...top);
    hazard(a, x + 1.6, y + h - 5.4, w - 3.2, 1.9, { step: 3.8 });
    return d;
  }
  /** A collar bolted to the stern wall at x = 0, where the drive passes through the hull. */
  function collar(a, y, h) {
    box(a, .5, y, 4.6, h, OLD, { l: 1, b: 1.2, e: 1 });
    a.line(`M2.8 ${y + .8}L2.8 ${y + h - .8}`, { w: .9 });
    const pts = []; for (let v = y + 2.2; v < y + h - 1.6; v += 4.4) pts.push([1.6, v], [4, v]);
    rv(a, ...pts);
  }
  /** A dark glass screen: a deep line under its bezel, a still picture drawn by pic, one pale sheen leaning across it. */
  function screen(a, x, y, w, h, pic) {
    const d = rr(x, y, w, h, Math.min(w, h) * .16);
    a.shape(d, SCREEN, { outer: false, deep: band(x, y, x + w, y + Math.min(.7, h * .14)), shade: band(x, y, x + Math.min(.6, w * .1), y + h) });
    if (pic) pic(x, y, w, h);
    a.flat(poly(x + w * .62, y + h * .1, x + w * .7, y + h * .1, x + w * .5, y + h * .9, x + w * .42, y + h * .9), '#2c3e42', { clip: d });
    return d;
  }
  const trace = (a) => (x, y, w, h) => {
    for (const v of [.33, .66]) a.line(`M${x + .8} ${y + h * v}L${x + w - .8} ${y + h * v}`, { w: .5, c: FAINT });
    a.line(path(x + 1, y + h * .58, x + w * .24, y + h * .58, x + w * .32, y + h * .22, x + w * .42, y + h * .84, x + w * .52, y + h * .42, x + w * .6, y + h * .58, x + w - 1, y + h * .58), { w: .9, c: TRACE });
  };
  const bars = (a) => (x, y, w, h) => {
    const n = 5, s = (w - 2) / n;
    for (let i = 0; i < n; i++) { const t = [.5, .8, .35, .65, .45][i]; a.flat(band(x + 1 + i * s + s * .2, y + h - .8 - (h - 1.8) * t, x + 1 + i * s + s * .8, y + h - .8), i === 1 ? '#4f8a76' : '#355a54'); }
  };
  const gridPic = (a) => (x, y, w, h) => {
    for (let u = x + 2; u < x + w - 1; u += 2.6) a.line(`M${u} ${y + .8}L${u} ${y + h - .8}`, { w: .45, c: FAINT });
    for (let v = y + 1.8; v < y + h - .6; v += 2.2) a.line(`M${x + .8} ${v}L${x + w - .8} ${v}`, { w: .45, c: FAINT });
    // A ship's outline on the grid, the bridge marked.
    a.line(path(x + w * .16, y + h * .62, x + w * .3, y + h * .4, x + w * .74, y + h * .4, x + w * .86, y + h * .52, x + w * .74, y + h * .68, x + w * .22, y + h * .68, x + w * .16, y + h * .62), { w: .8, c: TRACE });
    a.flat(ell(x + w * .7, y + h * .5, .45), '#8fd0a8');
  };
  const knob = (a, x, y, r, m) => a.shape(ell(x, y, r), m, { shade: r * .26, lite: ell(x - r * .26, y - r * .3, r * .5) });
  const key = (a, x, y, w, h, c) => { a.flat(rr(x, y, w, h, .25), '#14131c'); a.flat(rr(x + .12, y + .08, w - .24, h - .3, .2), c); };

  /** Main engine A: a rocket drive on two saddles. Its throat runs through a collar into the stern wall; behind the hooped chamber a turbopump block feeds it by two copper lines. */
  function mainEngineA(a) {
    box(a, 6, 44.6, 89.4, 3.4, GUN, { l: .6, b: .9, e: 1 });
    for (const x of [20, 41]) a.shape(`M${x} 44.8L${x + 11} 44.8L${x + 10.4} 36.6Q${x + 5.5} 39.6 ${x + .6} 36.6Z`, OLD, { lite: band(x, 36, x + 1.6, 45), shade: band(x + 9, 36, x + 11, 45) });
    // The feed lines, out of the pump's side and over into the chamber.
    a.tube('M64 3.2L28.4 3.2Q27 3.2 27 4.6L27 10', 2.2, COPPER);
    a.tube('M64 6.4L41.6 6.4Q40.2 6.4 40.2 7.8L40.2 10', 1.8, COPPER);
    for (const x of [48, 56]) box(a, x, 1.6, 1.6, 6.6, OLD, { l: .3, b: .4, e: .4 });
    // The turbopump block.
    driveBlock(a, 61.6, .8, 33.6, 43.8);
    a.shape(ell(72.4, 16.4, 7.2), OLD, { shade: 1.1, lite: ell(71.6, 15.6, 5.8) });
    a.flat(ell(72.4, 16.4, 5.6), SOOT.deep);
    rotor(a, 72.4, 16.4, 5, 7, .3);
    a.line(ell(72.4, 16.4, 5.8), { w: 1, c: OLD.lite });
    for (const [x, m] of [[82.4, BLUE], [87.8, STAINED]]) { cyl(a, x, 7.4, 4.4, 18.4, m); box(a, x - .4, 6.2, 5.2, 1.8, OLD, { l: .3, b: .4, e: .4 }); box(a, x - .4, 24.2, 5.2, 1.8, OLD, { l: .3, b: .4, e: .4 }); }
    vent(a, 65.2, 27.4, 13.6, 9, 5);
    a.shape(band(81.2, 28, 92, 36.4), PAINT, { outer: false, lite: band(81.2, 28, 92, 28.6), shade: band(81.2, 35.2, 92, 36.4) });
    a.shape(ell(84.6, 32.2, 2.4), OLD, { shade: .4 }); a.flat(ell(84.6, 32.2, 1.8), CREAM.base); a.line('M84.6 32.2L85.8 31', { w: .8, c: '#a83c30' });
    led(a, 89.4, 30.8, LIT.green); led(a, 89.4, 33.6, LIT.amber);
    // The throat, narrowed and widened again into the collar.
    a.shape(poly(5, 16.6, 8.4, 18.2, 11.6, 18.6, 16.4, 12.4, 16.4, 37.6, 11.6, 31.4, 8.4, 31.8, 5, 33.4), GUN, { lite: poly(5, 16.6, 8.4, 18.2, 11.6, 18.6, 16.4, 12.4, 16.4, 15, 11.6, 20.4, 5, 19.4), shade: poly(5, 30, 11.6, 29.4, 16.4, 33, 16.4, 37.6, 11.6, 31.4, 8.4, 31.8, 5, 33.4) });
    box(a, 9.2, 17.4, 2.6, 15.2, VIOLET, { l: 1.4, b: 2, e: .4 });
    // The chamber, its hoops, a dial and a warning plate between them.
    drum(a, 15, 8, 46.6, 34, GUN, { r: 7 });
    for (const x of [22, 33.2, 44.4]) { inner(a, x, 8.2, 2.6, 33.6, OLD, { l: .7, b: 1.6, e: .5 }); rv(a, [x + 1.3, 10.4], [x + 1.3, 39.6]); }
    a.shape(ell(28.6, 18, 2.6), OLD, { outer: false, shade: .4 }); a.flat(ell(28.6, 18, 1.9), CREAM.base); a.line('M28.6 18L29.8 16.6', { w: .8, c: '#a83c30' });
    a.shape(band(36.4, 13.6, 43, 18.6), PAINT, { outer: false, lite: band(36.4, 13.6, 43, 14.2), shade: band(36.4, 17.8, 43, 18.6) });
    a.flat(poly(39.7, 14.4, 41.8, 17.8, 37.6, 17.8), SOOT.deep);
    hazard(a, 25, 35.4, 7.8, 1.8, { step: 3.6 }); hazard(a, 36.2, 35.4, 7.8, 1.8, { step: 3.6 }); hazard(a, 47.4, 35.4, 10.6, 1.8, { step: 3.6 });
    box(a, 59.4, 9.4, 3.4, 31.2, OLD, { l: .8, b: 1.2, e: .6 });
    rv(a, [61.1, 12], [61.1, 25], [61.1, 38]);
    collar(a, 12.8, 24.4);
    rusted(a, 18.6, 38.4, 1, .7); rusted(a, 93, 41, .8, .6); rusted(a, 52.6, 12.4, .8, .6);
  }
  /** Main engine B: a reactor ring standing in a cradle, coils round its face and a dim violet core behind glass; a coiled magnetic nozzle runs to the wall, a bank of capacitors feeds it. */
  function mainEngineB(a) {
    box(a, 4, 44.6, 91.4, 3.4, GUN, { l: .6, b: .9, e: 1 });
    // The capacitor bank and its cables into the ring.
    box(a, 69.4, 39.6, 25.8, 5.2, OLD, { l: .6, b: .8, e: .8 });
    for (const [c, d] of [[WIRES.red, 'M72.8 9.6Q70 4.6 62.4 8.6'], [WIRES.yellow, 'M78.8 9.6Q76 2.4 58.6 5.4'], [WIRES.blue, 'M71 41Q67.6 40.6 64 37']]) wire(a, c, d, 1.1);
    for (const x of [71, 77, 83, 89]) {
      cyl(a, x, 12, 5, 28, TEAL);
      box(a, x - .3, 10.4, 5.6, 2.2, OLD, { l: .4, b: .5, e: .5 });
      box(a, x + 1.8, 8.4, 1.4, 2.2, COPPER, { l: .3, b: .4, e: .3 });
      a.flat(band(x + .6, 30, x + 4.4, 33), CREAM.base); a.flat(band(x + 1.2, 31, x + 3.2, 32), TEAL.deep);
    }
    a.tube('M71.8 8.6L93.4 8.6', 1.2, COPPER);
    // The magnetic nozzle: a tube wound with coils.
    drum(a, 4, 18.2, 24, 13.6, GUN, { r: 2 });
    for (const x of [6.6, 11.6, 16.6]) {
      box(a, x, 15.4, 3.2, 19.2, COPPER, { l: .8, b: 1.4, e: .6 });
      for (let y = 17.6; y < 33.4; y += 1.6) a.line(`M${x + .4} ${y}L${x + 2.8} ${y}`, { w: .6, c: COPPER.deep });
    }
    collar(a, 14.6, 20.8);
    // The cradle.
    for (const x of [31, 55]) a.shape(poly(x, 37, x + 6, 37, x + 7.6, 44.8, x - 1.6, 44.8), OLD, { lite: band(x - 2, 36, x + 1.4, 45), shade: band(x + 5.2, 36, x + 8, 45) });
    hazard(a, 29.6, 45.4, 34.6, 1.6, { step: 3.4 });
    // The ring.
    const cx = 46, cy = 23.6, R = 21.4;
    a.shape(ell(cx, cy, R), GUN, { shade: 2.4, deep: .9, lite: ell(cx - 1.4, cy - 1.8, R * .9) });
    for (let i = 0; i < 8; i++) {
      const t = (i / 8) * Math.PI * 2 + Math.PI / 8, at = (r, u) => [cx + Math.cos(t + u) * r, cy + Math.sin(t + u) * r];
      a.shape(poly(...at(13.4, -.22), ...at(20.2, -.17), ...at(20.2, .17), ...at(13.4, .22)), COPPER, { outer: false, lite: poly(...at(13.4, -.22), ...at(20.2, -.17), ...at(20.2, -.06), ...at(13.4, -.08)), shade: poly(...at(13.4, .1), ...at(20.2, .08), ...at(20.2, .17), ...at(13.4, .22)) });
      for (const r of [15.2, 17, 18.8]) a.line(path(...at(r, -.17), ...at(r, .17)), { w: .6, c: COPPER.deep });
    }
    a.shape(ell(cx, cy, 12.4), OLD, { outer: false, shade: 1.2, lite: ell(cx - .8, cy - 1, 11) });
    a.shape(ell(cx, cy, 10.2), SOOT, { outer: false, shade: 1.4 });
    a.flat(ell(cx, cy, 7.4), VIOLET.deep); a.flat(ell(cx - .8, cy - .8, 4.6), VIOLET.shade); a.flat(ell(cx - 1.2, cy - 1.2, 1.8), VIOLET.base);
    a.flat(poly(cx - 4.4, cy + 1.6, cx - 1.6, cy - 5, cx + .4, cy - 4.6, cx - 2.6, cy + 2.6), '#3a3454');
    const bolts = []; for (let i = 0; i < 8; i++) { const t = (i / 8) * Math.PI * 2; bolts.push([cx + Math.cos(t) * 11.3, cy + Math.sin(t) * 11.3]); }
    rv(a, ...bolts);
    box(a, cx - 3, .9, 6, 2.6, OLD, { l: .4, b: .6, e: .6 }); a.flat(ell(cx, 2.1, .7), SOOT.deep);
    rusted(a, 33, 40.6, .9, .6); rusted(a, 91.6, 42, .8, .5);
  }
  /** Main engine C: a beam drive: seven stage rings on a spine, growing from the wall, fed by a loom from a tall power cabinet with glass fuses and a knife switch. */
  function mainEngineC(a) {
    box(a, 3, 44.8, 66, 3.2, GUN, { l: .6, b: .9, e: .8 });
    // The power cabinet.
    box(a, 68, 1.2, 27.2, 46.8, GUN, { l: 1, b: 1.4, e: 1.2 });
    a.flat(band(69.4, 1.2, 70.2, 48), GUN.lite);
    vent(a, 71.4, 4.4, 20.6, 8.4, 4);
    well(a, 71, 14.8, 21.4, 14.2);
    for (const x of [72.4, 77.4, 82.4, 87.4]) {
      a.shape(rr(x, 17.4, 3.6, 9.4, 1.2), GLASS, { outer: false, lite: band(x + .5, 17.4, x + 1.3, 27), shade: band(x + 2.6, 17.4, x + 3.6, 27) });
      a.line(`M${x + 1.8} 18.6L${x + 1.8} 25.6`, { w: .8, c: '#c89838' });
      box(a, x - .3, 16, 4.2, 1.8, COPPER, { l: .3, b: .5, e: .4 }); box(a, x - .3, 26.4, 4.2, 1.8, COPPER, { l: .3, b: .5, e: .4 });
    }
    inner(a, 71.4, 31.2, 20.6, 7.6, OLD, { l: .6, b: .8, e: .6 });
    for (const x of [74.6, 78.4]) box(a, x, 32.6, 1.6, 4.8, COPPER, { l: .3, b: .4, e: .3 });
    a.tube('M76.4 35L87.4 31.6', 1, PLATE);
    a.shape(rr(86.6, 30.2, 3.2, 2.6, .8), SPOOL.red, { lite: band(86.6, 30.2, 89.8, 31) });
    hazard(a, 70.4, 41.6, 22.6, 1.8, { step: 3.6 });
    rv(a, [70.4, 3], [93.2, 3], [70.4, 46], [93.2, 46]);
    // The loom along the rail.
    for (const [c, y] of [[WIRES.red, 42], [WIRES.yellow, 43], [WIRES.blue, 44]]) wire(a, c, `M69 ${y - 6}Q66 ${y - 6} 66 ${y - 3}L66 ${y - 1}Q66 ${y} 64.4 ${y}L5 ${y}`, .8);
    // The spine and the stages.
    drum(a, 4, 19.6, 64.6, 10, GUN, { r: 1.6 });
    collar(a, 15, 19);
    const cy = 24.4, halves = [8.4, 10.6, 12.8, 15, 17, 18.8, 20.4];
    halves.forEach((hh, i) => {
      const x = 8.6 + i * 8.2, y0 = cy - hh, y1 = cy + hh;
      if (y1 < 44.4) { a.tube(`M${x + 2.2} ${y1}L${x + 2.2} 44.8`, 1.2, OLD, { spec: false }); box(a, x + .4, 43.6, 3.6, 1.4, OLD, { l: .3, b: .4, e: .4 }); }
      a.shape(rr(x, y0, 4.4, hh * 2, 1.8), OLD, { lite: band(x, y0, x + 1.2, y1), shade: band(x + 3.2, y0, x + 4.4, y1), deep: band(x + 3.9, y0, x + 4.4, y1) });
      inner(a, x + 1, y0 + 2.4, 2.4, hh * 2 - 4.8, VIOLET, { l: .5, b: .8, e: .4 });
      for (let v = y0 + 3.6; v < y1 - 3; v += 1.8) a.line(`M${x + 1.2} ${v}L${x + 3.2} ${v}`, { w: .55, c: VIOLET.deep });
      rv(a, [x + 2.2, y0 + 1.2], [x + 2.2, y1 - 1.2]);
    });
    rusted(a, 30, 46.2, .9, .5); rusted(a, 93.6, 22, .7, .6);
  }

  /** Console A: a cabinet desk: a sloped face of keys and switches, a screen hood behind it, doors and a vent below. */
  function consoleA(a) {
    box(a, 3.6, 22, 24.8, 2, SOOT, { l: .3, b: .5, e: .5 });
    box(a, 2.4, 11.6, 27.2, 10.8, GUN, { l: .6, b: 1, e: 1 });
    inner(a, 4.2, 13.6, 11, 7.2, OLD, { l: .6, b: .8, e: .6 }); slats(a, 5.4, 14.8, 8.6, 5, 3);
    inner(a, 16.8, 13.6, 11, 7.2, OLD, { l: .6, b: .8, e: .6 }); a.flat(band(18, 15.6, 18.8, 18.8), PLATE.lite);
    a.flat(band(22.6, 15, 26.4, 16.6), CREAM.base); a.flat(band(23.2, 15.6, 25.4, 16), OLD.shade);
    // The screen hood on the desk's back edge.
    box(a, 7.4, .8, 17.2, 7, GUN, { l: .6, b: .6, e: .8 });
    screen(a, 8.6, 1.9, 14.8, 4.7, trace(a));
    // The sloped face.
    a.shape(poly(1, 11.8, 31, 11.8, 29.6, 7, 2.4, 7), OLD, { lite: band(0, 7, 32, 7.8), shade: band(0, 10.6, 32, 11.8) });
    for (const [x, c] of [[4.4, '#a83c30'], [6.4, '#b8922e'], [8.4, '#a9aeb4'], [10.4, '#a9aeb4']]) key(a, x, 8.2, 1.5, 1.1, c);
    for (const x of [4.4, 6.4, 8.4, 10.4]) key(a, x, 9.8, 1.5, 1.1, '#61646c');
    well(a, 13, 8, 7, 3);
    for (const x of [14.2, 16.4, 18.6]) a.tube(`M${x} 10.2L${x + .5} 8.6`, .55, PLATE, { spec: false });
    for (const x of [21.6, 23.6]) knob(a, x, 9.4, .9, GUN);
    led(a, 27, 8.8, LIT.green); led(a, 28.4, 8.8, LIT.amber); led(a, 27, 10.4, LIT.green);
    box(a, .6, 11.2, 30.8, 1.4, GUN, { l: .3, b: .4, e: .4 });
    rusted(a, 4.4, 21.2, .6, .4);
  }
  /** Console B: a terminal: a cream monitor with a grid and a ship's outline on it, a keyboard shelf, a drive tower on a plinth. */
  function consoleB(a) {
    box(a, 6.4, 21.8, 19.2, 2.2, GUN, { l: .4, b: .6, e: .6 });
    box(a, 9, 13.8, 14, 8.4, OLD, { l: .6, b: .8, e: .8 });
    for (const y of [15.4, 17.6]) { a.flat(band(10.6, y, 18.4, y + .8), SOOT.deep); a.flat(band(10.6, y + .8, 18.4, y + 1.1), OLD.lite); }
    slats(a, 10.6, 19.6, 7.8, 1.8, 2);
    led(a, 20.4, 15.8, LIT.green); led(a, 20.4, 18, LIT.amber);
    // The shelf and its keys.
    a.shape(poly(3.6, 12.4, 28.4, 12.4, 29.4, 14.8, 2.6, 14.8), GUN, { lite: band(0, 12.4, 32, 13), shade: band(0, 14, 32, 14.8) });
    for (const [y, o] of [[12.9, 0], [13.75, .7]]) for (let x = 4.6 + o; x < 27; x += 1.55) a.flat(band(x, y, x + 1.1, y + .55), x > 22 && y > 13 ? '#b8922e' : PLATE.base);
    // The monitor.
    box(a, 5, .8, 22, 11.6, CREAM, { r: 2, l: .6, b: 1, e: 1.2 });
    a.flat(band(5.6, 11, 26.4, 11.6), CREAM.shade);
    screen(a, 6.8, 2.2, 15.4, 8.2, gridPic(a));
    knob(a, 24.4, 4, .8, GUN); knob(a, 24.4, 6.4, .8, GUN);
    led(a, 24.4, 9.2, LIT.green);
  }
  /** Console C: a helm desk on open legs: a round scope with a still sweep, three throttle levers in their quadrant, the cables down to a deck box. */
  function consoleC(a) {
    for (const [c, d] of [[WIRES.yellow, 'M13.4 15L13.4 22.6'], [WIRES.blue, 'M15 15L15 22.6'], [WIRES.red, 'M16.6 15L16.6 22.6']]) wire(a, c, d, .7);
    box(a, 11.6, 22.2, 6.8, 1.8, OLD, { l: .3, b: .5, e: .5 });
    for (const x of [2.6, 26.4]) { box(a, x, 14.6, 3, 8.4, GUN, { l: .4, b: .6, e: .7 }); box(a, x - 1, 22.6, 5, 1.4, OLD, { l: .3, b: .5, e: .5 }); }
    strut(a, 'M5.6 20.4L26.4 20.4', 1);
    box(a, 1, 12.4, 30, 3, OLD, { l: .7, b: .8, e: .8 });
    // The scope.
    box(a, 2.2, 2, 13.4, 10.6, GUN, { r: 1, l: .6, b: .8, e: .8 });
    a.shape(ell(8.9, 7.3, 4.4), OLD, { outer: false, shade: .6, lite: ell(8.6, 7, 3.8) });
    a.shape(ell(8.9, 7.3, 3.6), SCREEN, { outer: false, shade: .5 });
    for (const r of [1.2, 2.4]) a.line(ell(8.9, 7.3, r), { w: .5, c: FAINT });
    a.line('M5.4 7.3L12.4 7.3', { w: .45, c: FAINT }); a.line('M8.9 3.8L8.9 10.8', { w: .45, c: FAINT });
    a.line('M8.9 7.3L11.4 4.9', { w: .9, c: TRACE });
    a.flat(ell(7.1, 5.8, .38), '#8fd0a8'); a.flat(ell(10.6, 9.2, .3), '#4f8a76');
    rv(a, [3.4, 3.2], [14.4, 3.2], [3.4, 11.4], [14.4, 11.4]);
    // The quadrant and its levers.
    box(a, 17.4, 8.6, 12.4, 3.8, GUN, { l: .5, b: .7, e: .7 });
    a.flat(band(18.6, 9.4, 28.6, 10.2), SOOT.deep);
    for (const [x0, x1, y1, m] of [[20.4, 19.4, 3.4, SPOOL.red], [23.6, 23.6, 2.6, SPOOL.yellow], [26.8, 28, 4.4, GUN]]) {
      a.tube(`M${x0} 9.8L${x1} ${y1}`, .8, PLATE, { spec: false });
      knob(a, x1, y1, 1.15, m);
    }
    knob(a, 21, 11.4, .5, OLD); led(a, 28.4, 11.4, LIT.red);
  }
  /** Console D: a lectern on one column: a wide tilted head with two screens and a row of keys, a flexible conduit down to the deck. */
  function consoleD(a) {
    a.tube('M25.4 9.6Q27.4 12.6 27.4 16.4Q27.4 21.2 29 22.4', 1.3, OLD, { spec: false });
    box(a, 26.8, 21.8, 4.4, 2.2, GUN, { l: .3, b: .5, e: .5 });
    a.shape('M7.4 24L24.6 24L23.4 21.8Q16 20.6 8.6 21.8Z', GUN, { lite: band(7, 20.6, 25, 21.8), shade: band(7, 23.2, 25, 24) });
    cyl(a, 13, 9.6, 6, 12.4, OLD, { r: .6 });
    box(a, 12, 15.4, 8, 1.4, GUN, { l: .3, b: .4, e: .4 });
    led(a, 16, 18.6, LIT.amber);
    // The head.
    a.shape(poly(.8, 1, 31.2, 1, 29.8, 10.2, 2.2, 10.2), GUN, { lite: band(0, 1, 32, 1.9), shade: band(0, 9, 32, 10.2) });
    screen(a, 3, 2.4, 11.6, 5, trace(a));
    screen(a, 17.4, 2.4, 11.6, 5, bars(a));
    for (let x = 4; x < 28; x += 2.1) key(a, x, 8, 1.6, 1, x > 20 && x < 24 ? '#a83c30' : x < 6 ? '#b8922e' : '#83858a');
    box(a, 2, 9.8, 28, 1.3, OLD, { l: .3, b: .4, e: .4 });
    rv(a, [2.6, 1.8], [29.4, 1.8]);
  }

  const pics = { Crate: [24, 16, crate], Pod: [16, 16, pod], Tanks: [32, 32, tanks, [0, .6]], Rack: [32, 32, rack], Section: [48, 32, section], Machine: [48, 32, machine, [0, .6]],
    Container: [64, 32, container], Slab: [64, 32, slab, [0, 1.6]], Engine: [80, 48, engine, [0, 1]], Hold: [96, 48, hold] };
  for (const [n, [w, h, draw, shift]] of Object.entries(pics)) out(`machine_salvage${n}`, bake({ w, h, draw, shift }));
  // A vessel draws one of each set at random: the first keeps the bare name.
  [mainEngineA, mainEngineB, mainEngineC].forEach((draw, i) => out(`machine_salvageMainEngine${i ? i + 1 : ''}`, bake({ w: 96, h: 48, draw })));
  [consoleA, consoleB, consoleC, consoleD].forEach((draw, i) => out(`machine_salvageConsole${i ? i + 1 : ''}`, bake({ w: 32, h: 24, draw })));
};
})();
