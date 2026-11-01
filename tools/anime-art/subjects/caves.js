// A cave's corner: stone as cracked cel blocks (with soil and the ice vein on the same generator), its edges, tops and details; green moss; ice crystals.
(function () {
'use strict';
const { ell, poly, rng, bake, canvas } = Anime;

const GROUNDS = {
  stone: { back: ['#181a22', '#1b1d26', '#16181f'], tones: [
    { base: '#565c6a', shade: '#3d424f', lite: '#6c7280', spec: '#868c98', ink: '#0f111b' },
    { base: '#4e5462', shade: '#373c48', lite: '#646a78', spec: '#7c8290', ink: '#0f111b' },
    { base: '#5e6472', shade: '#434855', lite: '#747a88', spec: '#8e94a0', ink: '#0f111b' },
  ], cells: 26 },
  soil: { back: ['#1e171a', '#221a1d', '#1b1417'], tones: [
    { base: '#59443a', shade: '#3e2e2c', lite: '#6e5646', spec: '#866c58', ink: '#140c0e' },
    { base: '#4f3c33', shade: '#372826', lite: '#644c3e', spec: '#7c6250', ink: '#140c0e' },
    { base: '#63493b', shade: '#46322c', lite: '#786050', spec: '#907460', ink: '#140c0e' },
  ], cells: 40,
  // Stones in the earth, each placed: x, y, half width, half height.
  stones: [[14, 9, 2.2, 1.4], [37, 22, 1.4, 1], [58, 7, 1.6, 1.1], [71, 33, 3, 1.9], [96, 14, 1.3, .9], [117, 27, 2, 1.3], [24, 44, 2.6, 1.6], [49, 55, 1.4, 1], [88, 49, 2, 1.2], [108, 58, 1.5, 1]],
  grass: [
    { base: '#4a7a38', shade: '#2f5530', lite: '#6a9a48', spec: '#6a9a48', ink: '#10200f' },
    { base: '#3f6e34', shade: '#2a4d2c', lite: '#5d9042', spec: '#5d9042', ink: '#10200f' },
    { base: '#86804a', shade: '#5c5834', lite: '#a09858', spec: '#a09858', ink: '#1a160c' },
  ] },
};
const VEIN = { base: '#6cb4e4', shade: '#3c74b4', lite: '#b4e4fa', spec: '#ffffff', ink: '#0f1c34' };

/** Points on the sheet's torus, and each one's cell as a polygon: a Voronoi of half-planes, the neighbours' copies round it included so it tiles. */
function cellsOf(W, H, n, seed) {
  const r = rng(seed), pts = [];
  // Jittered grid: blocks of about one size, some merged by leaving a point out.
  const across = Math.round(Math.sqrt((n * W) / H)), down = Math.round(n / across);
  for (let j = 0; j < down; j++) for (let i = 0; i < across; i++) {
    if (r() < .1) continue;
    pts.push([(i + .15 + r() * .7) * W / across, (j + .15 + r() * .7) * H / down]);
  }
  return pts.map((p, k) => {
    let cell = [[p[0] - 40, p[1] - 40], [p[0] + 40, p[1] - 40], [p[0] + 40, p[1] + 40], [p[0] - 40, p[1] + 40]];
    pts.forEach((q0, m) => {
      for (const dx of [-W, 0, W]) for (const dy of [-H, 0, H]) {
        if (m === k && !dx && !dy) continue;
        const q = [q0[0] + dx, q0[1] + dy], mx = (p[0] + q[0]) / 2, my = (p[1] + q[1]) / 2, nx = q[0] - p[0], ny = q[1] - p[1];
        if (nx * nx + ny * ny > 40 * 40 * 4) continue;
        const out = [];
        for (let i = 0; i < cell.length; i++) {
          const a = cell[i], b = cell[(i + 1) % cell.length], da = (a[0] - mx) * nx + (a[1] - my) * ny, db = (b[0] - mx) * nx + (b[1] - my) * ny;
          if (da <= 0) out.push(a);
          if (da * db < 0) { const t = da / (da - db); out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
        }
        cell = out;
      }
    });
    return cell;
  });
}
const flatPoly = (cell, dx, dy, k) => {
  const cx = cell.reduce((s, p) => s + p[0], 0) / cell.length, cy = cell.reduce((s, p) => s + p[1], 0) / cell.length;
  return poly(cell.flatMap((p) => [cx + (p[0] - cx) * k + dx, cy + (p[1] - cy) * k + dy]));
};

/** A sheet of ground: large masses of close tone, each with a hard shadow along its foot, and only some of the cracks between them drawn, so it reads as one body and not as paving. */
function groundSheet(look, seed, dim) {
  return { w: 128, h: 64, tile: true, K: 2, draw(a) {
    const r = rng(seed ^ 0x13), cells = cellsOf(128, 64, dim ? 34 : look.cells, seed), ink = look.tones[0].ink;
    for (const cell of cells) {
      const t = look.tones[Math.floor(r() * look.tones.length)];
      if (dim) {
        // The wall behind: dark and quiet, so what the hero can stand on reads first. One tone a facet, no lines.
        const k = r();
        a.flat(flatPoly(cell, 0, 0, 1.02), look.back[Math.floor(k * look.back.length)]);
        continue;
      }
      a.shape(flatPoly(cell, 0, 0, 1.03), t, { outer: false, edge: false, shade: 1.1 });
    }
    if (dim) return;
    for (const cell of cells) for (let i = 0; i < cell.length; i++) {
      const p = cell[i], q = cell[(i + 1) % cell.length];
      if (r() < .3) a.line(`M${p[0]} ${p[1]}L${q[0]} ${q[1]}`, { w: 2.4, c: ink });
    }
    for (const [x, y, w, h] of look.stones || []) lump(a, x, y, w, h, GROUNDS.stone.tones[Math.floor(r() * 3)], r);
  } };
}

/** Ice flecks over a wall of stone, in a few clusters, as the vein's sheet draws them. */
function veined(base) {
  return { ...base, draw(a) {
    base.draw(a);
    const r = rng(0x1ce);
    for (const [cx, cy, n] of [[30, 18, 14], [84, 40, 18], [112, 12, 9], [58, 54, 10]])
      for (let i = 0; i < n; i++) {
        const x = cx + (r() - .5) * 18, y = cy + (r() - .5) * 13, s = .9 + r() * 1.3;
        a.shape(poly(x, y - s, x + s * .8, y, x, y + s, x - s * .8, y), VEIN, { outer: false, shade: poly(x, y - s, x + s * .8, y, x, y + s), spec: poly(x - .3, y - s * .5, x, y - s * .7, x, y) });
      }
  } };
}

/** A pebble of the ground: a chipped lump with a shadow crescent. */
const lump = (a, cx, cy, w, h, m, r) => {
  const pts = [];
  for (let i = 0; i < 7; i++) { const t = (i / 7) * Math.PI * 2 + r() * .3, k = .78 + r() * .22; pts.push(cx + Math.cos(t) * w * k, cy + Math.sin(t) * h * k); }
  a.shape(poly(pts), { base: m.base, shade: m.shade, lite: m.lite, spec: m.spec, ink: m.ink }, { shade: .5, lite: poly(cx - w * .6, cy - h * .9, cx + w * .2, cy - h * .9, cx - w * .1, cy - h * .2, cx - w * .8, cy) });
};

/** One of a ground's living-edge columns, 16 × 64: lumps behind the hero on its top half, chips before it below, each halved cell a world of its own so no lump is cut. */
function topOf(look, v, front, half) {
  const r = rng(0x70 + v * 7 + (front ? 13 : 0) + half * 3);
  return bake({ w: 8, h: 32, draw(a) {
    const m = look.tones[Math.floor(r() * look.tones.length)];
    if (front) { if (r() < .35) lump(a, 2 + r() * 4, 17 + r() * .4, .7 + r() * .5, .5 + r() * .2, m, r); }
    else if (!look.grass && r() < .75) lump(a, 3.7 + r() * .6, 15.7 + r() * .3, 1.5 + r() * 2.2, .8 + r() * .7, m, r);
    if (look.grass) turf(a, look.grass, front, r);
  } });
}

/** The living edge of grown ground, one half column of it, its ends level so columns join: behind the hero a band of turf with thick tufts on it; before him turf lying over the block's top, its ragged lip hanging down the soil, and a few short blades. */
function turf(a, grass, front, r) {
  const [lit, deep] = grass, wavy = (y0, spread, flip) => {
    const pts = [];
    for (let k = 0; k <= 8; k++) pts.push(k, y0 + (k === 0 || k === 8 ? 0 : flip * r() * spread));
    return pts;
  };
  const blade = (x, root, h, lean, t) => {
    const tip = Math.min(7.7, Math.max(.3, x + lean)), mid = x + (tip - x) * .35;
    a.shape(`M${x - .42} ${root}Q${mid - .3} ${root - h * .55} ${tip} ${root - h}Q${mid + .32} ${root - h * .5} ${x + .42} ${root}Z`, t, { outer: false, edge: false, shade: `M${x} ${root}Q${mid + .05} ${root - h * .5} ${tip} ${root - h}L${x + .6} ${root}Z` });
  };
  if (front) {
    const lip = wavy(18, 2, 1), under = [];
    for (let i = lip.length - 2; i >= 0; i -= 2) under.push(lip[i], lip[i + 1]);
    a.shape(poly(-.2, 15.7, 8.2, 15.7, ...under), deep, { outer: false, edge: false, lite: poly(-.2, 15.7, 8.2, 15.7, 8.2, 16.9, -.2, 16.9) });
    a.line('M' + lip.join(' ').replace(/(\S+ \S+) /g, '$1L'), { w: 1.8, c: deep.ink });
    for (let i = 0, n = 2 + Math.floor(r() * 3); i < n; i++) blade(.8 + r() * 6.4, 16.4, 1.6 + r() * 2, (r() - .5) * 1.4, grass[Math.floor(r() * 2)]);
    return;
  }
  const crown = wavy(14.9, .9, -1);
  for (let i = 0, n = 6 + Math.floor(r() * 4); i < n; i++) blade(.5 + r() * 7, 15.4, 3 + r() * 4.2, (r() - .5) * 2.2, deep);
  a.shape(poly(...crown, 8.2, 16.4, -.2, 16.4), lit, { outer: false, edge: false, shade: poly(-.2, 15.6, 8.2, 15.6, 8.2, 16.4, -.2, 16.4) });
  for (let i = 0, n = 5 + Math.floor(r() * 4); i < n; i++) blade(.5 + r() * 7, 15.6, 2 + r() * 3.4, (r() - .5) * 1.8, r() < .12 ? grass[2] : lit);
}

/** A ground's details over its cells: pebbles and cracks; features a boulder, a long crack and a heap. */
function detailOf(look, k) {
  const m = look.tones[0], r = rng(0xde + k * 11);
  const side = k < 8 ? 16 : k < 10 ? 32 : 48;
  return bake({ w: side, h: side, draw(a) {
    if (k < 8) {
      if (k % 4 === 2) a.line(`M${3 + r() * 2} ${6 + r() * 3}L${6 + r() * 2} ${8 + r() * 3}L${8 + r() * 2} ${6 + r() * 2}L${12 + r()} ${8 + r() * 3}`, { w: 2.6 });
      else lump(a, 5 + r() * 6, 6 + r() * 6, 1.4 + r() * 1.2, 1 + r() * .9, look.tones[k % look.tones.length], r);
    } else if (k === 8) {
      lump(a, 16, 16, 9, 7.5, look.tones[look.tones.length - 1], r);
      a.line('M12 12L16 15L14 20M16 15L21 13', { w: 2.4 });
    } else if (k === 9) {
      a.line('M4 8L9 12L13 10L19 17L23 16L28 24', { w: 3.6, c: m.ink });
    } else {
      for (let i = 0; i < 9; i++) lump(a, 8 + i * 3.6 + r(), 32 - Math.abs(i - 4) * .7 - r() * 4, 2.6 + r() * 1.4, 1.7 + r(), look.tones[i % look.tones.length], r);
    }
  } });
}

// Moss: a solid band along its face at the rows the view stretches, tufts off it.
const MOSS = { base: '#4f7a3c', shade: '#2f5232', lite: '#7aa350', spec: '#b4cc7c', ink: '#0d2a18' };
const BAND = { top: [4, 5], bottom: [19, 21], left: [4, 5], right: [19, 20] };

function mossPicture(face, v) {
  const r = rng(0x305 + v * 31 + face.length * 7);
  const horizontal = face === 'top' || face === 'bottom';
  return { w: 24, h: 24, draw(a) {
    // Written as along (x) and across (y) a top face, turned to the others below.
    const at = (al, ac) => {
      if (face === 'top') return [al, ac];
      if (face === 'bottom') return [al, 24 - ac + (BAND.bottom[0] + BAND.bottom[1] - 24)];
      if (face === 'left') return [ac, al];
      return [24 - ac + (BAND.right[0] + BAND.right[1] - 24), al];
    };
    const path = (pts) => poly(pts.flatMap((p) => at(p[0], p[1])));
    const [b0, b1] = face === 'bottom' ? [4, 6] : [4, 5];
    // Cushions along the band, of uneven height, a few blades off them: a comb of even blades reads as grass.
    const cushions = 4 + Math.floor(r() * 3);
    for (let i = 0; i < cushions; i++) {
      const al = 5.5 + (i + r() * .7) * (13 / cushions), w = 1.3 + r() * 1.5, tall = .9 + r() * (face === 'bottom' ? 2 : 1.4), pts = [];
      for (let k = 0; k <= 6; k++) pts.push([al - w + (2 * w * k) / 6, b0 + .2 - (k === 0 || k === 6 ? 0 : tall * Math.sin((k / 6) * Math.PI) * (.8 + r() * .35))]);
      pts.push([al + w, b0 + .4], [al - w, b0 + .4]);
      a.shape(path(pts), MOSS, { lite: path(pts.slice(0, 4).concat([[al, b0 + .1], [al - w, b0 + .1]])) });
      if (r() < .5) a.flat(ell(...at(al - w * .3, b0 - tall * .6), .3), MOSS.spec);
    }
    a.shape(path([[4, b0], [20, b0], [20, b1], [4, b1]]), MOSS, { outer: false, edge: false });
    a.flat(path([[4, b0 + (b1 - b0) * .7], [20, b0 + (b1 - b0) * .7], [20, b1], [4, b1]]), MOSS.shade);
    a.flat(path([[4, b0 - .2], [20, b0 - .2], [20, b0 + .15], [4, b0 + .15]]), MOSS.lite);
  } };
}

/** A soft glow from a picture, as the game's bloom picture: blurred, fading before its edge. */
function glowOf(c, tint, reach, keep) {
  const o = canvas(c.width, c.height), x = o.getContext('2d');
  x.filter = `blur(${reach}px)`;
  x.drawImage(c, 0, 0);
  x.filter = 'none';
  x.globalCompositeOperation = 'source-in';
  x.fillStyle = tint;
  x.fillRect(0, 0, o.width, o.height);
  const f = canvas(c.width, c.height), y = f.getContext('2d');
  y.globalAlpha = keep;
  y.drawImage(o, 0, 0);
  y.globalAlpha = 1;
  // Clear to nothing at the box: cut off there, a bloom draws its rectangle.
  y.globalCompositeOperation = 'destination-in';
  const edge = 8, g = y.createLinearGradient(0, 0, c.width, 0);
  g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(edge / c.width, '#000'); g.addColorStop(1 - edge / c.width, '#000'); g.addColorStop(1, 'rgba(0,0,0,0)');
  y.fillStyle = g; y.fillRect(0, 0, c.width, c.height);
  const h = y.createLinearGradient(0, 0, 0, c.height);
  h.addColorStop(0, 'rgba(0,0,0,0)'); h.addColorStop(edge / c.height, '#000'); h.addColorStop(1 - edge / c.height, '#000'); h.addColorStop(1, 'rgba(0,0,0,0)');
  y.fillStyle = h; y.fillRect(0, 0, c.width, c.height);
  return f;
}

// Crystals: prisms with facets, fitted to the box the logical picture used, its base row kept where the planting looks for it.
const ICE = { base: '#7cc0f0', shade: '#3a78c4', deep: '#2a5494', lite: '#c0ecff', spec: '#ffffff', ink: '#0c1a38' };
/** A shard from its foot (x, y) up to its tip: width, length, lean; lit face toward the light. */
function shard(a, x, y, w, len, lean, hang) {
  const s = hang ? -1 : 1;
  const tipX = x + lean, tipY = y - s * len, shoulderY = y - s * len * .68;
  const lx = x - w / 2 + lean * .62, rx = x + w / 2 + lean * .62, ridge = x - w * .05 + lean * .7;
  const d = poly(x - w / 2, y, lx, shoulderY, tipX, tipY, rx, shoulderY, x + w / 2, y);
  a.shape(d, ICE, {
    lite: poly(x - w / 2, y, lx, shoulderY, tipX, tipY, ridge, shoulderY + s * len * .08, x - w * .08, y),
    shade: poly(ridge, shoulderY + s * len * .08, tipX, tipY, rx, shoulderY, x + w / 2, y, x + w * .15, y),
    deep: poly(x + w * .28, y, rx - w * .15, shoulderY + s * len * .2, rx, shoulderY, x + w / 2, y),
    spec: poly(lx + w * .14, shoulderY, tipX - w * .08, tipY + s * len * .14, tipX - w * .02, tipY + s * len * .3, lx + w * .24, shoulderY + s * len * .32),
    lines: [`M${lx} ${shoulderY}L${ridge} ${shoulderY + s * len * .08}L${rx} ${shoulderY}`, `M${ridge} ${shoulderY + s * len * .08}L${x - w * .03} ${y}`],
  });
}
const CLUSTERS = [
  [[.5, 1, .95, 0]],
  [[.14, .6, .85, -.55], [.5, .95, 1, .02], [.84, .55, .7, .25]],
  [[.26, .85, 1, -.08], [.66, .6, .78, .35]],
];
function crystal(name, variant, hang) {
  const box = LOGICAL[name];
  const w = box.x1 - box.x0 + 1, h = box.y1 - box.y0 + 1;
  return { w: box.w, h: box.h, draw(a) {
    const foot = hang ? box.y0 : box.y1 + 1;
    const shards = CLUSTERS[variant % CLUSTERS.length].slice().sort((p, q) => q[1] - p[1]);
    for (const [fx, fh, fw, lean] of shards) {
      const width = Math.max(3.4, w * (shards.length === 1 ? .62 : .36) * fw);
      shard(a, box.x0 + fx * w, foot, width, h * fh - .45, lean * h * .5, hang);
    }
  } };
}

Subjects.caves = function () {
  const stone = groundSheet(GROUNDS.stone, 0x5701, false);
  out('wall_stone', bake(stone));
  out('wall_iceVein', bake(veined(stone)));
  out('back_stone', bake(groundSheet(GROUNDS.stone, 0x5702, true)));
  out('back_iceVein', bake(groundSheet(GROUNDS.stone, 0x5702, true)));
  out('wall_soil', bake(groundSheet(GROUNDS.soil, 0x5703, false)));
  out('back_soil', bake(groundSheet(GROUNDS.soil, 0x5704, true)));
  const edges = {
    stone: { lite: '#7c8290', shade: '#2f3244', ink: '#0f111b' }, iceVein: { lite: '#7c8290', shade: '#2f3244', ink: '#0f111b' }, soil: { lite: '#7c6654', shade: '#33242a', ink: '#140c0e' },
  };
  for (const [name, look] of Object.entries(edges)) for (let open = 1; open < 16; open++) out(`wall-edge_${name}_${open}`, AnimeEdge(open, look));
  for (const [name, look] of [['stone', GROUNDS.stone], ['iceVein', GROUNDS.stone], ['soil', GROUNDS.soil]]) {
    for (let v = 0; v < 4; v++) for (const front of [false, true]) for (let half = 0; half < 2; half++) out(`top_${name}_${v}_${front ? 'front' : 'back'}_${half}`, topOf(look, v, front, half));
    for (let k = 0; k < 11; k++) out(`detail_${name}_${k}`, detailOf(look, k));
  }
  for (const face of ['top', 'bottom', 'left', 'right']) for (let v = 0; v < 4; v++) {
    const c = bake(mossPicture(face, v));
    out(`caves/mossGreen_${face}_${v}`, c);
    out(`caves/mossGreen_${face}_${v}_glow`, glowOf(c, '#7ca85a', 3, .2));
  }
  for (let v = 0; v < 3; v++) {
    for (const [family, hang] of [['crystalIce', false], ['crystalIceHang', true]]) {
      const c = bake(crystal(`${family}_${v}`, v, hang));
      out(`caves/${family}_${v}`, c);
      out(`caves/${family}_${v}_glow`, glowOf(c, '#a0dcff', 6, .8));
    }
  }
};
})();
