// The ship's room: scrap walls as riveted plates, their back wall, their edges, and a door, shut and open.
(function () {
'use strict';
const { ell, rr, poly, rng, bake, canvas } = Anime;
const SHEET_W = 128, SHEET_H = 64;

const PLATE = [
  { base: '#79818f', shade: '#565c74', lite: '#a0a8b4', spec: '#d6dce4', ink: '#10121c' },
  { base: '#6c7382', shade: '#4c516a', lite: '#8f97a6', spec: '#c8ced8', ink: '#10121c' },
  { base: '#848a94', shade: '#5e6276', lite: '#abb1ba', spec: '#e0e4ea', ink: '#10121c' },
];
const RIVET = { base: '#9aa2ae', shade: '#555b72', lite: '#c8ced8', spec: '#ffffff', ink: '#10121c' };
const RUST = { base: '#8e6044', shade: '#6a4234', lite: '#ad7c58', spec: '#c89a70', ink: '#1c1210' };

/** A row of plates whose widths fill the sheet, varied by the seed. */
function rowsOf(r, heights, lo, hi) {
  const rows = [];
  let y = 0;
  for (const h of heights) {
    const plates = [];
    let x = Math.floor(r() * lo);
    const first = x;
    while (x < SHEET_W + first) {
      let w = Math.round(lo + r() * (hi - lo));
      if (SHEET_W + first - (x + w) < lo) w = SHEET_W + first - x;
      plates.push({ x, y, w, h });
      x += w;
    }
    rows.push(plates);
    y += h;
  }
  return rows;
}

/** A plate: its lit top, its shaded foot and side in hard tones, a thin seam, rivets along it. A scrap plate is the base art of the hull. */
function plate(a, p, r, m) {
  const { x, y, w, h } = p, d = poly(x, y, x + w, y, x + w, y + h, x, y + h);
  a.shape(d, m, {
    outer: false, edge: false,
    lite: poly(x, y, x + w, y, x + w - .7, y + .8, x + .7, y + .8),
    shade: [poly(x, y + h - 1.5, x + w, y + h - 1.5, x + w, y + h, x, y + h), poly(x + w - 1.1, y, x + w, y, x + w, y + h, x + w - 1.1, y + h)],
    spec: poly(x + 1.4, y + .2, x + 1.4 + Math.min(w * .3, 8), y + .2, x + 1 + Math.min(w * .3, 8), y + .6, x + 1, y + .6),
  });
  const rivets = Math.floor(w / 6);
  for (let i = 0; i < rivets; i++) {
    const rx = x + 3 + (i * (w - 6)) / Math.max(1, rivets - 1);
    for (const ry of [y + 2.6, y + h - 3.1]) {
      if (r() < .12) continue;
      a.shape(ell(rx, ry, .55), RIVET, { outer: false, edge: false, shade: 'M' + (rx - 1) + ' ' + (ry + .1) + 'L' + (rx + 1) + ' ' + (ry + .1) + 'L' + (rx + 1) + ' ' + (ry + 1) + 'L' + (rx - 1) + ' ' + (ry + 1) + 'Z', spec: ell(rx - .15, ry - .2, .18) });
    }
  }
}

/** A crack: a pen line, broken once or twice. */
function crack(a, r, x, y, len) {
  let d = `M${x} ${y}`, cx = x, cy = y;
  const down = r() < .5;
  for (let i = 0; i < 3 + Math.floor(r() * 2); i++) {
    if (down) { cy += len / 3; cx += (r() - .5) * 2.2; } else { cx += len / 3; cy += (r() - .5) * 2.2; }
    d += `L${cx.toFixed(2)} ${cy.toFixed(2)}`;
  }
  a.line(d, { w: 3.2 });
}

/** An irregular patch round a point: lobes and bites along its edge, as rust and old paint come away. */
function tornBlotch(r, x, y, rx, ry) {
  const pts = [];
  for (let i = 0; i < 14; i++) {
    const t = (i / 14) * Math.PI * 2, k = .55 + r() * .75;
    pts.push(x + Math.cos(t) * rx * k, y + Math.sin(t) * ry * k);
  }
  return poly(...pts);
}

const seam = (a, rows) => {
  // The dark gap between plates, drawn over all of them; straight, so it tiles.
  for (const row of rows) {
    const y = row[0].y;
    a.flat(poly(-2, y - .28, SHEET_W + 2, y - .28, SHEET_W + 2, y + .28, -2, y + .28), '#10121c');
    for (const p of row) a.flat(poly(p.x - .24, p.y, p.x + .24, p.y, p.x + .24, p.y + p.h, p.x - .24, p.y + p.h), '#10121c');
  }
};

const wallScrap = {
  w: SHEET_W, h: SHEET_H, tile: true, K: 2,
  draw(a) {
    const r = rng(0x5c7a9);
    const rows = rowsOf(r, [13, 12, 15, 11, 13], 14, 34);
    for (const row of rows) for (const p of row) plate(a, p, r, PLATE[Math.floor(r() * PLATE.length)]);
    // Rust eats out from the seams, where water sat: each patch placed by hand, its edge torn, a darker heart on the seam.
    for (const [x, y, rx, ry] of [[22, 13, 9, 3], [64, 25, 6, 2.5], [101, 40, 11, 3.5], [8, 51, 7, 3], [47, 40, 5, 2], [86, 13, 6, 2.2], [120, 25, 5, 2], [33, 51, 8, 2.6], [72, 51, 4, 2]]) {
      a.flat(tornBlotch(r, x, y, rx, ry), '#8a4f2e');
      a.flat(tornBlotch(r, x, y, rx * .55, ry * .55), '#5c3220');
    }
    seam(a, rows);
    // What makes it scrap: patches bolted over the seams, a rusty one, and cracks.
    const patches = [[100, 10, 9, 9], [15, 44, 7, 9], [19, 50, 11, 8], [120, 37, 11, 8], [50, 24, 8, 6], [84, 54, 10, 7]];
    patches.forEach(([x, y, w, h], i) => {
      const m = i === 2 ? RUST : PLATE[i % 3], d = rr(x, y, w, h, .5);
      a.shape(d, m, { lite: poly(x, y, x + w, y, x + w - .5, y + .8, x + .5, y + .8), shade: [poly(x, y + h - 1, x + w, y + h - 1, x + w, y + h, x, y + h), poly(x + w - .9, y, x + w, y, x + w, y + h, x + w - .9, y + h)] });
      for (const [rx, ry] of [[x + 1.2, y + 1.3], [x + w - 1.7, y + 1.3], [x + 1.2, y + h - 1.9], [x + w - 1.7, y + h - 1.9]])
        a.shape(ell(rx, ry, .45), RIVET, { outer: false, edge: false, spec: ell(rx - .1, ry - .15, .15) });
    });
    for (const [x, y, len] of [[30, 16, 7], [71, 3, 6], [43, 42, 8], [96, 30, 9], [58, 56, 5], [112, 19, 6]]) crack(a, r, x, y, len);
    // Rust runs from a few rivets.
    for (const [x, y, h] of [[33, 16, 3], [76, 29, 4], [108, 41, 3]]) a.flat(`M${x} ${y}L${x + .8} ${y}L${x + .5} ${y + h}L${x + .25} ${y + h + .6}Z`, '#8e6044');
  },
};

const BACK = [
  { base: '#272c38', shade: '#1f2330', lite: '#2e3440', spec: '#2e3440', ink: '#0f1118' },
  { base: '#242935', shade: '#1d212d', lite: '#2b303c', spec: '#2b303c', ink: '#0f1118' },
];
const backScrap = {
  w: SHEET_W, h: SHEET_H, tile: true, K: 2,
  draw(a) {
    const r = rng(0x77c1);
    const rows = rowsOf(r, [24, 20, 20], 26, 56);
    for (const row of rows) for (const p of row) {
      const { x, y, w, h } = p, m = BACK[Math.floor(r() * 2)];
      a.shape(poly(x, y, x + w, y, x + w, y + h, x, y + h), m, { outer: false, edge: false,
        lite: poly(x, y, x + w, y, x + w, y + .6, x, y + .6),
        shade: [poly(x, y + h - 1.2, x + w, y + h - 1.2, x + w, y + h, x, y + h), poly(x + w - .8, y, x + w, y, x + w, y + h, x + w - .8, y + h)] });
    }
    for (const row of rows) {
      const y = row[0].y;
      a.flat(poly(-2, y - .22, SHEET_W + 2, y - .22, SHEET_W + 2, y + .22, -2, y + .22), '#0f1118');
      for (const p of row) a.flat(poly(p.x - .18, p.y, p.x + .18, p.y, p.x + .18, p.y + p.h, p.x - .18, p.y + p.h), '#0f1118');
    }
    for (const [x, y, w, h] of [[20, 38, 16, 11], [86, 7, 21, 13], [58, 46, 12, 8]]) {
      const m = BACK[0];
      a.shape(rr(x, y, w, h, .5), m, { outer: false, lite: poly(x, y, x + w, y, x + w, y + .6, x, y + .6), shade: [poly(x, y + h - 1, x + w, y + h - 1, x + w, y + h, x, y + h)] });
    }
  },
};

/** A wall's edge (open: up 1, right 2, down 4, left 8): an ink line on each open side, a hard lit rim inside it on top and on the light's side, a shaded one on the others. */
function edge(open, look) {
  const S = 32, c = canvas(S, S), x = c.getContext('2d'), I = 3, R = 2;
  const up = open & 1, right = open & 2, down = open & 4, left = open & 8;
  x.fillStyle = look.shade;
  if (right) x.fillRect(S - I - R, 0, R, S);
  if (down) x.fillRect(0, S - I - R, S, R);
  x.fillStyle = look.lite;
  if (left) x.fillRect(I, 0, R, S);
  if (up) x.fillRect(0, I, S, R + 1);
  x.fillStyle = look.ink;
  if (up) x.fillRect(0, 0, S, I);
  if (right) x.fillRect(S - I, 0, I, S);
  if (down) x.fillRect(0, S - I, S, I);
  if (left) x.fillRect(0, 0, I, S);
  return c;
}

/** The world pixels of a door's picture: its jambs at either side of the panel's run, the panel between. */
const DOOR = { left: 3, panel: 4, panelEnd: 12, right: 13, top: 3, foot: 15 };
const ORANGE = { base: '#d97f3a', shade: '#a9542a', lite: '#f6b06a', spec: '#ffe4b8', ink: '#2a140c' };
const JAMB = { base: '#5a3a26', shade: '#3d2519', lite: '#7a5236', spec: '#7a5236', ink: '#160c08' };
const inPx = (v) => Math.round(v * 4);

/** The panel of one piece of a door: flat rectangles and paths inside its rectangle, drawn at the final size, so its edge is hard and the picture's difference from the open one is exactly it. */
function panelOf(piece) {
  const c = canvas(64, 64), x = c.getContext('2d');
  const from = piece === 'top' ? DOOR.top : 0, to = piece === 'foot' ? DOOR.foot : 16;
  x.setTransform(4, 0, 0, 4, 0, 0);
  const rect = (a, b, y0, y1, col) => { x.fillStyle = col; x.fillRect(a, y0, b - a, y1 - y0); };
  rect(DOOR.panel, DOOR.panelEnd, from, to, ORANGE.base);
  rect(DOOR.panel, DOOR.panel + 1.25, from, to, ORANGE.lite);
  rect(DOOR.panel + .25, DOOR.panel + .75, from, to, ORANGE.spec);
  rect(DOOR.panelEnd - 1.75, DOOR.panelEnd, from, to, ORANGE.shade);
  x.fillStyle = ORANGE.ink;
  x.fillRect(DOOR.panel + 1.25, from, .25, to - from);
  x.fillRect(DOOR.panelEnd - 1.75, from, .25, to - from);
  // A seam across, the same on every piece: where the panel's plates meet.
  rect(DOOR.panel, DOOR.panelEnd, 9, 9.5, ORANGE.ink);
  rect(DOOR.panel, DOOR.panelEnd, 9.5, 10, ORANGE.lite);
  rect(DOOR.panel, DOOR.panelEnd, 8.25, 9, ORANGE.shade);
  if (piece === 'top') {
    // The window.
    rect(5.75, 10.25, 3.75, 7.75, '#1a1c28');
    rect(6.25, 9.75, 4.25, 7.25, '#2c4a70');
    x.fillStyle = '#9ad2f0';
    x.beginPath(); x.moveTo(6.25, 7.25); x.lineTo(6.25, 5.75); x.lineTo(7.75, 4.25); x.lineTo(9, 4.25); x.closePath(); x.fill();
    x.fillStyle = '#e4f6ff';
    x.beginPath(); x.moveTo(6.5, 5.75); x.lineTo(7.75, 4.5); x.lineTo(8.25, 4.5); x.lineTo(6.5, 6.25); x.closePath(); x.fill();
  }
  if (piece === 'foot') {
    // Hazard stripes at its foot.
    x.save();
    x.beginPath(); x.rect(DOOR.panel, 11.75, DOOR.panelEnd - DOOR.panel, 3.25); x.clip();
    rect(DOOR.panel, DOOR.panelEnd, 11.75, 15, '#f0c43c');
    x.fillStyle = '#1a1820';
    for (let k = -2; k < 12; k += 2.5) { x.beginPath(); x.moveTo(DOOR.panel + k, 15); x.lineTo(DOOR.panel + k + 1.25, 15); x.lineTo(DOOR.panel + k + 4.5, 11.75); x.lineTo(DOOR.panel + k + 3.25, 11.75); x.closePath(); x.fill(); }
    x.restore();
    rect(DOOR.panel, DOOR.panelEnd, 11.5, 11.75, ORANGE.ink);
  }
  return c;
}

/** The frame of one piece, the same shut and open: jambs, the header with its lamps on the top piece, the sill on the foot. */
function frameOf(piece) {
  const c = canvas(64, 64), x = c.getContext('2d');
  x.setTransform(4, 0, 0, 4, 0, 0);
  const rect = (a, b, y0, y1, col) => { x.fillStyle = col; x.fillRect(a, y0, b - a, y1 - y0); };
  const top = piece === 'top' ? DOOR.top : 0, foot = 16;
  for (const [a, b] of [[DOOR.left - .75, DOOR.panel], [DOOR.panelEnd, DOOR.right + .75]]) {
    rect(a, b, 0, foot, JAMB.ink);
    rect(a + .5, b - .25, 0, foot, JAMB.base);
  }
  rect(DOOR.left - .25, DOOR.left + .25, 0, foot, JAMB.lite);
  rect(DOOR.panelEnd + .25, DOOR.panelEnd + .75, 0, foot, JAMB.lite);
  if (piece === 'top') {
    rect(DOOR.left - .75, DOOR.right + .75, 0, top, JAMB.ink);
    rect(DOOR.left - .25, DOOR.right + .25, .5, top - .25, JAMB.base);
    rect(DOOR.left - .25, DOOR.right + .25, .5, 1, JAMB.lite);
    for (const lx of [5.5, 9]) { rect(lx, lx + 1.5, 1.25, 2.25, '#f0c43c'); rect(lx + .25, lx + .75, 1.25, 1.5, '#fff0b0'); }
  }
  if (piece === 'foot') {
    rect(DOOR.left - .75, DOOR.right + .75, DOOR.foot, 16, JAMB.ink);
    rect(DOOR.left - .25, DOOR.right + .25, DOOR.foot + .25, 15.75, JAMB.shade);
  }
  return c;
}

function doorPicture(piece, shut) {
  const c = canvas(64, 64), x = c.getContext('2d');
  if (shut) x.drawImage(panelOf(piece), 0, 0);
  x.drawImage(frameOf(piece), 0, 0);
  return c;
}

Subjects.room = function () {
  out('wall_scrap', bake(wallScrap));
  out('back_scrap', bake(backScrap));
  const look = { lite: '#b4bcc8', shade: '#3e4258', ink: '#10121c' };
  for (let open = 1; open < 16; open++) out(`wall-edge_scrap_${open}`, edge(open, look));
  // Links: up 1, down 4; a door's pieces from its top down: joined down 4, both 5, joined up 1.
  for (const [links, piece] of [[4, 'top'], [5, 'mid'], [1, 'foot']]) {
    out(`door_closed_${links}`, doorPicture(piece, true));
    out(`door_open_${links}`, doorPicture(piece, false));
  }
};
window.AnimeEdge = edge;
})();
