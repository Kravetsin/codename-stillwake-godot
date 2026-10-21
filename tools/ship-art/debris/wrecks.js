// The three derelicts, three pictures each, drawn of what they break into (ADR 0056):
// the engine block (gears, plates, scrap, wire, a rotor, a circuit), the cargo hold (planks in crates,
// plates, gears, wire, canisters) and the pod cluster (canisters, scrap, plates, wire, circuits).
const { Pic, clamp, hash } = require('./lib.js');
const { jag, tear, hole, holeShade, bar, truss, wire, gear, rotor, crate, canister, porthole } = require('./parts.js');

// ---------- engine block 80 x 48 ----------
// a nozzle bell along x from x0 (throat) to x1 (mouth), round its axis yc, flaring from h0 to h1 tall
function bell(p, x0, x1, yc, h0, h1, { heat = 6, seams = 7, torn = null, mat = 'steel' } = {}) {
  const ext = x => { const t = (x - x0) / (x1 - x0), h = h0 + (h1 - h0) * Math.pow(t, 1.6); return [Math.round(yc - h / 2), Math.round(yc + h / 2)]; };
  for (let x = x0; x <= x1; x++) {
    const [a, b] = ext(x); const m = x - x0 < heat ? 'heat' : x - x0 < heat + 2 && (x + a) % 2 ? 'heat' : mat;
    p.cyl(x, a, x, b, m, 'x', { bias: -0.05 + 0.1 * (x - x0) / (x1 - x0) });
    if (x - x0 < 2) p.cyl(x, a, x, b, 'copper', 'x');
    if (seams && (x - x0) % seams === seams - 1 && x < x1 - 2) for (let y = a + 1; y < b; y++) { p.px(x, y, m, Math.max(0, p.slotAt(x, y) - 2)); }
  }
  // the lip of the mouth
  const [a, b] = ext(x1); for (let y = a; y <= b; y++) { p.px(x1, y, mat, y < yc ? 5 : 3); p.px(x1 - 1, y, mat, 1); }
  if (torn) tear(p, torn);
  return ext;
}

function engineBody(p, x0, y0, x1, y1) {
  p.poly([[x0, y0 + 3], [x0 + 3, y0], [x1 - 3, y0], [x1, y0 + 3], [x1, y1 - 3], [x1 - 3, y1], [x0 + 3, y1], [x0, y1 - 3]], 'dsteel', 3);
  // the bevel of the chamfered box
  for (let x = x0 + 3; x <= x1 - 3; x++) { p.px(x, y0, 'dsteel', 4); p.px(x, y1, 'dsteel', 1); }
  for (let y = y0 + 3; y <= y1 - 3; y++) { p.px(x0, y, 'dsteel', 4); p.px(x1, y, 'dsteel', 1); }
  for (let i = 0; i < 3; i++) { p.px(x0 + i, y0 + 3 - i, 'dsteel', 4); p.px(x1 - i, y1 - 3 + i, 'dsteel', 1); p.px(x1 - 3 + i, y0 + i, 'dsteel', 3); p.px(x0 + i, y1 - 3 + i, 'dsteel', 2); }
}
function grille(p, x0, y0, x1, y1) { for (let y = y0; y <= y1; y += 2) { p.rect(x0, y, x1, y, 'void', 1); p.rect(x0, y + 1, x1, y + 1, 'dsteel', 4); } }
function rivets(p, x0, x1, y, step, mat) { if (p.st.chunky) return; for (let x = x0; x <= x1; x += step) p.px(x, y, mat, 5); }

const ENGINE = [
  // a whole bell, a side panel torn off over the turbine, torn from its mount on the left
  p => {
    engineBody(p, 8, 7, 46, 41);
    p.panel(9, 8, 45, 12, 'steel', { base: 3 }); rivets(p, 12, 44, 10, 6, 'steel');
    p.panel(10, 14, 26, 34, 'dsteel', { base: 3 }); p.panel(28, 14, 44, 34, 'dsteel', { base: 3 });
    grille(p, 31, 16, 41, 24); p.panel(32, 27, 40, 32, 'yellow', { base: 3 }); p.rect(35, 28, 37, 31, 'ink', 0); p.px(36, 29, 'yellow', 4);
    p.stripes(9, 36, 45, 38); p.rect(9, 39, 45, 40, 'steel', 2);
    const h = hole(p, jag([12, 16], [25, 15], 1.5, 3).concat(jag([25, 15], [24, 33], 1.5, 4), jag([24, 33], [12, 32], 1.5, 5), jag([12, 32], [12, 16], 1.5, 6)));
    rotor(p, 18, 23, 6, 'steel'); gear(p, 23, 30, 3.5, 7, 'grime'); holeShade(p, h);
    // pipe along the top into the throat
    p.cyl(18, 2, 51, 4, 'copper', 'x'); p.cyl(50, 2, 52, 14, 'copper', 'y'); p.px(50, 2, 'copper', 4);
    for (const x of [24, 36]) p.rect(x, 5, x + 1, 6, 'dsteel', 2);
    // throat collar and bell
    p.panel(47, 13, 50, 35, 'steel', { base: 3 }); rivets(p, 48, 48, 16, 1, 'steel'); p.px(48, 32, 'steel', 5);
    bell(p, 51, 77, 24, 16, 42);
    // the mount it was torn from
    p.panel(3, 11, 8, 37, 'grime', { base: 2 });
    tear(p, jag([1, 9], [5, 13], 1, 7).concat(jag([5, 13], [3, 37], 1.5, 8), [[1, 40], [0, 40], [0, 9]]));
    bar(p, [8, 17], [2, 14], 'dsteel'); bar(p, [8, 31], [2, 35], 'dsteel'); p.px(2, 13, 'dsteel', 5);
    wire(p, [[7, 38], [6, 41], [4, 44], [4, 46]], 'red'); wire(p, [[9, 41], [9, 44], [11, 46]], 'wire_y'); wire(p, [[6, 36], [3, 39], [2, 43]], 'wire_b');
  },
  // the bell broken off short, a fuel tank strapped on top, gears showing through a gash
  p => {
    engineBody(p, 6, 11, 44, 42);
    p.cyl(9, 3, 40, 9, 'orange', 'x'); for (const x of [9, 40]) p.cyl(x, 4, x, 8, 'orange', 'x', { bias: -0.15 });
    for (const x of [14, 34]) { p.rect(x, 3, x + 1, 10, 'dsteel', 2); p.px(x, 3, 'dsteel', 4); }
    p.stripes(20, 5, 27, 6, { w: 2 });
    p.panel(8, 13, 42, 17, 'steel', { base: 3 }); rivets(p, 11, 41, 15, 5, 'steel');
    // radiator fins on the left panel
    p.panel(8, 19, 22, 36, 'dsteel', { base: 3 }); for (let x = 10; x <= 20; x += 2) { p.rect(x, 21, x, 34, 'dsteel', 4); p.rect(x + 1, 21, x + 1, 34, 'dsteel', 1); }
    p.panel(24, 19, 42, 36, 'dsteel', { base: 3 });
    const h = hole(p, jag([26, 22], [40, 20], 1.5, 11).concat(jag([40, 20], [41, 34], 1.5, 12), jag([41, 34], [27, 35], 1.5, 13), jag([27, 35], [26, 22], 1, 14)));
    gear(p, 31, 26, 4.5, 9, 'steel'); gear(p, 37, 31, 3.5, 7, 'grime'); p.rect(26, 31, 33, 32, 'copper', 3); holeShade(p, h);
    wire(p, [[28, 34], [30, 37], [29, 40], [31, 43], [30, 46]], 'red'); wire(p, [[33, 35], [35, 39], [35, 45]], 'wire_y');
    p.stripes(7, 38, 43, 39); p.rect(7, 40, 43, 41, 'steel', 2);
    p.panel(45, 17, 48, 36, 'steel', { base: 3 });
    bell(p, 49, 78, 26, 15, 40, { torn: jag([79, 4], [70, 10], 2, 15, 2).concat(jag([70, 10], [62, 26], 2.5, 16, 2), jag([62, 26], [72, 44], 2.5, 17, 2), [[79, 47], [79, 4]]) });
    // a curl of the bell still hanging on below
    bar(p, [62, 36], [66, 41], 'steel'); p.px(67, 42, 'steel', 4);
    // torn mount: a stub of beam and bolts
    bar(p, [5, 20], [1, 18], 'dsteel', 3); bar(p, [5, 33], [2, 36], 'dsteel', 2); p.px(2, 44, 'grime', 3);
    tear(p, jag([0, 24], [7, 26], 1, 18).concat([[7, 29], [0, 31]]));
  },
  // two smaller bells stacked, one crumpled; cooling fins on top; seen from the other side
  p => {
    p.flip = true;
    engineBody(p, 4, 9, 42, 44);
    for (let x = 10; x <= 36; x += 3) { p.rect(x, 3, x + 1, 8, 'steel', 3); p.px(x, 3, 'steel', 5); p.px(x + 1, 8, 'steel', 1); }
    p.panel(5, 10, 41, 13, 'steel', { base: 3 }); rivets(p, 8, 40, 11, 4, 'steel');
    p.panel(6, 15, 41, 37, 'dsteel', { base: 3 });
    grille(p, 9, 17, 19, 21);
    // a diagonal gash over the gear train
    const h = hole(p, [[18, 24], [26, 18], [37, 19], [39, 25], [32, 33], [21, 34], [17, 30]].flatMap((pt, i, a) => jag(pt, a[(i + 1) % a.length], 1.2, 20 + i).slice(0, -1)));
    gear(p, 24, 28, 4.5, 9, 'steel'); gear(p, 31, 24, 3.5, 7, 'steel'); gear(p, 34, 30, 2.5, 6, 'copper'); holeShade(p, h, 1);
    p.panel(9, 25, 15, 31, 'pcb', { base: 3 }); p.px(11, 27, 'yellow', 4); p.px(13, 29, 'yellow', 4); p.rect(10, 29, 12, 29, 'pcb', 1);
    p.stripes(5, 39, 41, 40); p.rect(5, 41, 41, 42, 'steel', 2);
    p.panel(43, 11, 46, 22, 'steel', { base: 3 }); p.panel(43, 30, 46, 41, 'steel', { base: 3 });
    bell(p, 47, 76, 16, 9, 24, { seams: 6, heat: 4 });
    bell(p, 47, 70, 36, 9, 18, { seams: 6, heat: 4, torn: jag([71, 47], [64, 40], 2, 21).concat(jag([64, 40], [67, 27], 2, 22), [[71, 27]]) });
    p.cyl(43, 24, 46, 28, 'copper', 'x');
    wire(p, [[3, 24], [1, 28], [2, 33]], 'wire_b'); wire(p, [[3, 36], [2, 40], [3, 44], [2, 46]], 'red');
    tear(p, jag([0, 14], [6, 17], 1, 23).concat(jag([6, 17], [5, 30], 1, 24), [[0, 32]]));
  },
  // its casing burst open at the top on the turbine shaft, a big gear half out of a torn corner, a short wide bell
  p => {
    engineBody(p, 6, 11, 42, 41);
    p.panel(8, 22, 40, 35, 'dsteel', { base: 3 }); grille(p, 26, 25, 37, 31);
    p.panel(10, 25, 22, 32, 'steel', { base: 3, rivets: 1, inset: 1 });
    p.rect(13, 27, 19, 30, 'yellow', 3); p.rect(13, 27, 19, 27, 'yellow', 4); p.rect(15, 28, 17, 29, 'ink', 0);
    // the top burst open: the shaft and its blade rows standing in the dark
    const h = hole(p, jag([9, 13], [39, 13], 2, 81).concat([[39, 20], [9, 20]]));
    p.cyl(10, 15, 38, 17, 'steel', 'x'); for (let x = 12; x <= 36; x += 3) { p.rect(x, 13, x, 19, 'steel', 4); p.px(x + 1, 19, 'steel', 1); }
    holeShade(p, h, 1);
    tear(p, jag([4, 0], [12, 12], 2, 82).concat(jag([12, 12], [26, 10], 2.5, 83), jag([26, 10], [44, 13], 2, 84), [[46, 0]]));
    bar(p, [12, 13], [9, 7], 'dsteel'); bar(p, [30, 12], [33, 6], 'dsteel'); p.px(33, 5, 'dsteel', 5);
    // a corner torn away round a gear
    tear(p, [[0, 33], [11, 33], [13, 37], [12, 47], [0, 47]]);
    gear(p, 10, 39, 6.5, 12, 'steel'); p.sphere(10, 39, 3, 'steel', { bias: 0.25 }); p.ell(10, 39, 1.2, 1.2, 'void', 0);
    p.stripes(14, 37, 41, 38); p.rect(14, 39, 41, 40, 'steel', 2);
    p.panel(43, 14, 46, 37, 'steel', { base: 3 }); p.px(44, 17, 'steel', 5); p.px(44, 34, 'steel', 5);
    bell(p, 47, 72, 26, 18, 34, { seams: 5, heat: 5 });
    p.cyl(18, 42, 44, 43, 'copper', 'x'); p.cyl(44, 38, 45, 43, 'copper', 'y');
    wire(p, [[24, 41], [26, 44], [24, 46]], 'wire_y'); wire(p, [[34, 41], [33, 44], [35, 46]], 'red');
  },
];

// ---------- cargo hold 96 x 48 ----------
// a ribbed hull segment: skin, ribs every 8, rails top and bottom
function holdShell(p, x0, y0, x1, y1, { rib = 8 } = {}) {
  p.panel(x0, y0, x1, y1, 'paint', { base: 3 });
  p.panel(x0, y0, x1, y0 + 3, 'steel', { base: 3 }); p.panel(x0, y1 - 3, x1, y1, 'steel', { base: 3 });
  for (let x = x0 + 4; x < x1 - 2; x += rib) { p.rect(x, y0 + 4, x, y1 - 4, 'paint', 4); p.rect(x + 1, y0 + 4, x + 1, y1 - 4, 'paint', 2); p.px(x, y0 + 2, 'steel', 5); p.px(x, y1 - 2, 'steel', 5); }
  for (let x = x0 + 2; x < x1; x += 4) { p.px(x, y0 + 1, 'steel', 4); }
}
function door(p, x0, y0, x1, y1) {
  p.stripes(x0, y0, x1, y1, { w: 2 });
  p.panel(x0 + 2, y0 + 2, x1 - 2, y1 - 2, 'steel', { base: 3, rivets: 1, inset: 1 });
  const mx = Math.round((x0 + x1) / 2); p.seamV(mx, y0 + 3, y1 - 3, 'steel', 1);
  porthole(p, mx - 4, y0 + 8, 2); p.rect(mx + 2, y0 + 14, mx + 3, y0 + 17, 'dsteel', 2); p.px(mx + 2, y0 + 14, 'dsteel', 4);
}
function shelf(p, x0, x1, y) { p.rect(x0, y, x1, y, 'dsteel', 4); p.rect(x0, y + 1, x1, y + 1, 'dsteel', 1); }
function planks(p, x0, y0, x1, n) { for (let i = 0; i < n; i++) { const y = y0 + i * 2, d = i % 2 ? 1 : 0; p.rect(x0 + d, y, x1 - (1 - d), y, 'wood', 4); p.rect(x0 + d, y + 1, x1 - (1 - d), y + 1, 'wood', 2); } }
function grate(p, h) { if (p.st.chunky) return; for (const [x, y] of h.cells) if (p.matAt(x, y) === 'void' && x % 3 === 0 && y % 3 === 0) p.px(x, y, 'void', 4); }

const HOLD = [
  // a door on the left, the far end torn open on its crates, ribs bent out of the tear
  p => {
    holdShell(p, 2, 4, 90, 43);
    door(p, 7, 8, 25, 39);
    p.rect(30, 11, 33, 11, 'yellow', 3); p.rect(30, 13, 31, 13, 'yellow', 3);
    const h = hole(p, jag([52, 9], [92, 8], 2, 31).concat([[92, 39]], jag([92, 39], [55, 39], 2, 32), jag([55, 39], [50, 24], 2.5, 33), jag([50, 24], [52, 9], 2.5, 34)));
    grate(p, h);
    shelf(p, 53, 88, 24); crate(p, 55, 13, 11, 11); crate(p, 67, 16, 8, 8); canister(p, 76, 13, 11); canister(p, 81, 15, 9);
    crate(p, 55, 27, 9, 11); planks(p, 66, 29, 82, 5); crate(p, 79, 28, 9, 10);
    holeShade(p, h);
    tear(p, jag([94, 0], [82, 6], 2, 35).concat(jag([82, 6], [87, 25], 3, 36), jag([87, 25], [80, 47], 3, 37), [[95, 47], [95, 0]]));
    for (const [x, y0, y1] of [[84, 4, 13], [76, 37, 45]]) { bar(p, [x, y0], [x + 5, y1 - (y1 - y0) / 2], 'steel'); }
    bar(p, [78, 6], [86, 3], 'steel'); bar(p, [74, 41], [82, 45], 'steel');
    wire(p, [[60, 40], [62, 43], [61, 46]], 'wire_y');
  },
  // a gash down its middle, a crate sliding out; the door at the far end hangs open
  p => {
    holdShell(p, 3, 9, 92, 44, { rib: 7 });
    const h = hole(p, jag([30, 12], [60, 11], 2.5, 41).concat(jag([60, 11], [64, 26], 2, 42), jag([64, 26], [58, 40], 2, 43), jag([58, 40], [33, 40], 2, 44), jag([33, 40], [29, 25], 2, 45), jag([29, 25], [30, 12], 2, 46)));
    grate(p, h);
    shelf(p, 30, 63, 27); crate(p, 34, 17, 10, 10); crate(p, 46, 19, 8, 8); canister(p, 56, 17, 10, 'blue'); canister(p, 60, 19, 8, 'blue');
    planks(p, 32, 30, 50, 5); crate(p, 52, 29, 10, 10);
    holeShade(p, h);
    // a crate drifting out of the tear, tilted on its corner
    p.poly([[38, 3], [45, 1.5], [48, 7], [41, 10]], 'wood', 3);
    p.line(38, 3, 45, 1, 'wood', 4); p.line(45, 1, 48, 7, 'wood', 4); p.line(41, 10, 48, 7, 'wood', 1); p.line(38, 3, 41, 10, 'wood', 2); p.line(40, 4, 46, 7, 'wood', 2);
    // the door at the right end, swung out of its frame
    p.stripes(72, 13, 88, 39);
    p.rect(74, 15, 86, 37, 'void', 1); for (let y = 16; y < 37; y += 3) p.rect(75, y, 85, y, 'void', 3);
    p.poly([[78, 14], [91, 17], [91, 40], [78, 38]], 'steel', 3); p.line(78, 14, 91, 17, 'steel', 4); p.line(78, 14, 78, 38, 'steel', 4); p.line(91, 17, 91, 40, 'steel', 1); p.line(78, 38, 91, 40, 'steel', 1);
    porthole(p, 84, 21, 2); p.rect(87, 27, 88, 30, 'dsteel', 2);
    p.rect(8, 15, 11, 15, 'yellow', 3); p.rect(8, 17, 9, 17, 'yellow', 3);
    tear(p, jag([0, 6], [6, 12], 1.5, 47).concat(jag([6, 12], [4, 40], 1.5, 48), [[0, 44]]));
    wire(p, [[44, 38], [45, 42], [43, 45], [44, 46]], 'red');
  },
  // its roof torn away: crates and a container standing up out of the open top; the other way round
  p => {
    p.flip = true;
    holdShell(p, 3, 8, 92, 44);
    door(p, 8, 12, 24, 40);
    const h = hole(p, [[30, 9], [93, 9], [93, 40], [34, 40]].flatMap((pt, i, a) => (i === 0 ? jag(pt, a[1], 0, 1) : i === 3 ? jag(pt, a[0], 2.5, 51) : [pt])));
    grate(p, h);
    shelf(p, 35, 90, 30); planks(p, 37, 32, 60, 4); crate(p, 62, 32, 9, 8); crate(p, 72, 32, 9, 8); canister(p, 84, 31, 9);
    holeShade(p, h);
    // the roof's torn edge
    tear(p, [[29, 0], [95, 0], [95, 16]].concat(jag([95, 16], [70, 14], 2.5, 52), jag([70, 14], [45, 17], 2.5, 53), jag([45, 17], [31, 11], 2, 54)));
    // what stands up out of the open top: a container, crates stacked, one tilted
    p.panel(40, 5, 57, 29, 'green', { base: 3 }); for (let x = 43; x < 56; x += 3) p.seamV(x, 7, 27, 'green', 2); p.rect(40, 5, 57, 6, 'green', 4);
    p.rect(44, 10, 49, 10, 'wood', 3); p.rect(44, 12, 47, 12, 'wood', 3);
    crate(p, 62, 15, 10, 10); crate(p, 73, 17, 8, 8); crate(p, 63, 7, 7, 7);
    p.poly([[78, 6], [85, 4], [87, 11], [80, 13]], 'wood', 3); p.line(78, 6, 85, 4, 'wood', 4); p.line(80, 13, 87, 11, 'wood', 1); p.line(79, 8, 86, 8, 'wood', 2);
    // ribs sticking up where the roof went
    bar(p, [35, 14], [33, 6], 'paint'); bar(p, [91, 18], [92, 9], 'paint'); p.px(92, 8, 'paint', 5);
    wire(p, [[60, 42], [62, 45], [61, 46]], 'wire_b');
  },
  // both cargo doors slid open on the stacked crates, canisters strapped on its back, the left end torn off
  p => {
    holdShell(p, 3, 9, 92, 44);
    p.cyl(52, 3, 70, 7, 'blue', 'x'); p.cyl(72, 4, 86, 8, 'blue', 'x'); p.rect(60, 3, 60, 8, 'dsteel', 2); p.rect(79, 4, 79, 8, 'dsteel', 2);
    p.rect(55, 5, 57, 5, 'white', 4); p.rect(75, 6, 77, 6, 'white', 4);
    p.stripes(34, 12, 62, 41);
    const h = hole(p, [[36, 14], [60, 14], [60, 39], [36, 39]]);
    grate(p, h);
    crate(p, 37, 28, 11, 11); crate(p, 49, 30, 10, 9); crate(p, 39, 19, 9, 9); planks(p, 49, 22, 59, 4); canister(p, 55, 15, 6);
    holeShade(p, h);
    // the door slid aside over the ribs
    p.panel(64, 12, 84, 41, 'steel', { base: 3, rivets: 1, inset: 2 }); p.seamV(74, 14, 39, 'steel', 1); p.rect(66, 24, 67, 29, 'dsteel', 2);
    porthole(p, 88, 18, 2); p.rect(87, 25, 90, 25, 'yellow', 3); p.rect(87, 27, 88, 27, 'yellow', 3);
    tear(p, jag([0, 4], [10, 10], 2, 91).concat(jag([10, 10], [16, 26], 3, 92), jag([16, 26], [9, 47], 3, 93), [[0, 47]]));
    bar(p, [14, 14], [8, 8], 'steel'); bar(p, [13, 36], [6, 41], 'steel'); bar(p, [17, 25], [11, 24], 'paint');
    wire(p, [[15, 30], [12, 33], [12, 37], [9, 40]], 'red'); wire(p, [[16, 18], [12, 20], [10, 24]], 'wire_y');
  },
];

// ---------- pod cluster 64 x 64 ----------
// a lifepod: a capsule along its axis (angle), a heat shield on its far end, an orange band, a porthole,
// a docking collar on its near end where it hung from the frame
function pod(p, cx, cy, r, { band = 'orange', hull = 'white', angle = 0, port = true, shield = 'dsteel', collar = true } = {}) {
  const rx = r * 1.2, ry = r * 0.88, ca = Math.cos(angle), sa = Math.sin(angle);
  if (collar) { const ex = cx - ca * (rx + 0.3), ey = cy - sa * (rx + 0.3); p.sphere(ex, ey, 3.4, 'steel', { gain: 0.8 }); p.ell(ex - ca * 1.2, ey - sa * 1.2, 1.4, 1.4, 'dsteel', 2); }
  const box = Math.ceil(rx + 1);
  for (let y = Math.floor(cy - box); y <= cy + box; y++) for (let x = Math.floor(cx - box); x <= cx + box; x++) {
    const dx = x + 0.5 - cx, dy = y + 0.5 - cy, u = (dx * ca + dy * sa) / rx, v = (-dx * sa + dy * ca) / ry, q = u * u + v * v;
    if (q > 1) continue;
    const nz = Math.sqrt(1 - q); let nx = u * ca - v * sa, ny = u * sa + v * ca; const X = p.flip ? -nx : nx;
    const d = X * -0.46 + ny * -0.58 + nz * 0.67;
    let m = hull, bias = 0.05;
    if (u > 0.66) { m = shield; bias = 0.02; } else if (u > 0.05 && u < 0.25) m = band;
    p.px(x, y, m, p.q(Math.max(0, Math.min(1, 0.12 + 0.88 * d + bias))));
    if (Math.abs(u - 0.66) < 0.07 && q < 0.97) p.px(x, y, shield, 1);
  }
  if (port) { const px = cx + ca * -0.3 * rx - sa * -0.42 * ry, py = cy + sa * -0.3 * rx + ca * -0.42 * ry; porthole(p, px, py, Math.max(1.5, r * 0.2)); }
}
function hub(p, x0, y0, x1, y1) {
  p.panel(x0, y0, x1, y1, 'dsteel', { base: 3, rivets: 1, inset: 1 });
  const h = hole(p, [[x0 + 3, y0 + 3], [x1 - 2, y0 + 3], [x1 - 2, y1 - 2], [x0 + 3, y1 - 2]]);
  for (let x = x0 + 4; x <= x1 - 6; x += 5) canister(p, x, y0 + 4, y1 - y0 - 6);
  holeShade(p, h, 1);
}

const PODS = [
  // four pods round a hub of canisters, one arm torn and its pod gone
  p => {
    truss(p, [32, 32], [14, 14], 'dsteel', 5); truss(p, [32, 32], [50, 15], 'dsteel', 5); truss(p, [32, 32], [16, 50], 'dsteel', 5); truss(p, [32, 32], [52, 50], 'dsteel', 5);
    tear(p, jag([62, 63], [46, 48], 1.5, 61).concat([[60, 38], [63, 40]]));
    hub(p, 22, 23, 41, 41);
    pod(p, 14, 14, 10, { angle: 3.93 }); pod(p, 50, 14, 9, { band: 'white', hull: 'orange', angle: 5.5 });
    pod(p, 14, 50, 9, { angle: 2.36 });
    // the empty clamp where the fourth hung
    bar(p, [47, 47], [53, 46], 'steel'); bar(p, [47, 47], [49, 53], 'steel'); p.px(54, 45, 'steel', 5);
    wire(p, [[44, 44], [48, 52], [50, 57], [54, 60]], 'red'); wire(p, [[42, 45], [44, 51], [43, 56], [46, 61]], 'wire_y');
  },
  // three pods along a broken spine, a canister rack between; the spine sheared at the bottom
  p => {
    truss(p, [32, 3], [32, 61], 'dsteel', 7);
    tear(p, jag([20, 52], [44, 50], 2.5, 71).concat([[44, 63], [20, 63]]));
    bar(p, [29, 50], [27, 57], 'dsteel'); bar(p, [35, 50], [38, 56], 'dsteel'); p.px(38, 57, 'dsteel', 4);
    pod(p, 16, 13, 10, { angle: 3.14 }); pod(p, 48, 29, 10, { angle: 0.12 }); pod(p, 16, 44, 10, { band: 'red', angle: 3.4 });
    p.panel(26, 18, 38, 24, 'dsteel', { base: 3 }); canister(p, 28, 17, 7); canister(p, 33, 17, 7);
    wire(p, [[30, 51], [28, 55], [30, 59], [28, 62]], 'red'); wire(p, [[34, 51], [36, 56], [34, 61]], 'wire_b');
    p.panel(26, 33, 38, 37, 'pcb', { base: 3 }); p.px(28, 35, 'yellow', 4); p.px(31, 35, 'yellow', 4); p.px(35, 34, 'lamp', 3);
  },
  // a ring girder holding three pods and an empty cradle; the other way round
  p => {
    p.flip = true;
    for (let a = 0; a < 6.28; a += 0.02) {
      if (a > 4.3 && a < 5.3) continue; // the broken arc
      for (let rr = 18; rr <= 21; rr++) { const x = Math.floor(32 + Math.cos(a) * rr), y = Math.floor(32 + Math.sin(a) * rr); p.px(x, y, 'dsteel', rr === 18 ? 1 : rr === 21 ? (Math.sin(a) < 0 ? 4 : 2) : 3); }
    }
    for (let a = 0; a < 6.28; a += 0.45) if (!(a > 4.2 && a < 5.4)) { const x = Math.floor(32 + Math.cos(a) * 19.5), y = Math.floor(32 + Math.sin(a) * 19.5); p.px(x, y, 'dsteel', 5); }
    truss(p, [32, 32], [32, 14], 'dsteel', 5);
    bar(p, [39, 16], [43, 9], 'dsteel'); bar(p, [47, 22], [53, 19], 'dsteel'); p.px(43, 8, 'dsteel', 5);
    p.panel(26, 26, 38, 38, 'dsteel', { base: 3, rivets: 1, inset: 1 }); p.ell(32, 32, 3, 3, 'void', 1); p.px(31, 31, 'blue', 4); canister(p, 30, 29, 7);
    pod(p, 14, 16, 9, { angle: 3.75 }); pod(p, 14, 48, 9, { angle: 2.55, hull: 'orange', band: 'white' }); pod(p, 48, 48, 10, { angle: 0.8 });
    wire(p, [[45, 10], [49, 6], [53, 5], [57, 2]], 'wire_y'); wire(p, [[50, 20], [55, 21], [58, 25], [61, 24]], 'red');
  },
  // a big pod and two small ones on a spine broken in two, a bundle of canisters strapped to it
  p => {
    truss(p, [32, 3], [32, 38], 'dsteel', 7); truss(p, [37, 46], [37, 61], 'dsteel', 7);
    tear(p, jag([20, 38], [44, 36], 2, 96).concat([[44, 40], [20, 41]]));
    bar(p, [30, 38], [28, 42], 'dsteel'); bar(p, [35, 38], [38, 42], 'dsteel'); bar(p, [35, 46], [33, 43], 'dsteel'); p.px(28, 43, 'dsteel', 5);
    wire(p, [[31, 38], [30, 41], [33, 44], [35, 47]], 'red'); wire(p, [[34, 38], [36, 41], [35, 44], [39, 46]], 'wire_y');
    pod(p, 15, 15, 11, { angle: 3.14 });
    p.panel(36, 20, 52, 31, 'dsteel', { base: 2 });
    for (const x of [38, 42, 46]) canister(p, x, 21, 10);
    p.rect(36, 25, 51, 25, 'orange', 3); p.rect(36, 26, 51, 26, 'orange', 1);
    pod(p, 52, 10, 7, { angle: 0.15, hull: 'orange', band: 'white' });
    pod(p, 53, 54, 7, { angle: 0.3 });
    p.panel(25, 26, 30, 33, 'pcb', { base: 3 }); p.px(27, 28, 'yellow', 4); p.px(27, 31, 'lamp', 3);
    wire(p, [[24, 50], [27, 53], [25, 57], [27, 61]], 'wire_b');
  },
];

const WRECKS = {
  engineBlock: { look: 'engine', w: 80, h: 48, list: ENGINE },
  cargoHold: { look: 'hold', w: 96, h: 48, list: HOLD },
  podCluster: { look: 'pods', w: 64, h: 64, list: PODS },
};

function drawWreck(style, kind, i) {
  const K = WRECKS[kind], p = new Pic(K.w, K.h, style, { seed: 5 + i });
  p.name = `${kind}#${i + 1}`;
  K.list[i](p);
  p.despeck();
  if (style.wreckRim) p.rim(null, { k: 5, min: 2, skip: ['red', 'wire_y', 'wire_b', 'ink', 'void', 'glass', 'lamp'] });
  if (style.inkSeams) p.inkSeams(['red', 'wire_y', 'wire_b', 'glass', 'lamp', 'heat', 'copper']);
  return p;
}

module.exports = { WRECKS, drawWreck };
