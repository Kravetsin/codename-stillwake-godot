// The sealing walls: glass, mica and hide, three candidates each; airtight and fragile, so each must read apart from the
// armour (scrap, iron) and from the others by look alone.
const { Sheet, W, H, mod, hex, mix, shade, noise, ramp } = require('./lib.js');

// ---- glass: frosted, matte, never a window -------------------------------------------------------------------------
/** How much of what lies behind a pane of glass it hides, of 255: stars show through, a room's light does not. */
const PANE_ALPHA = 150;
const FROST = [0x86a6ab, 0x95b4b8, 0xa3c1c3, 0xb0cbcc, 0xbdd5d4].map(hex);

/** Frosted panes in thin steel frames, 32x16, a bolt where frames cross; the panes half see-through, so stars show behind. */
function panes(r, back) {
  const s = new Sheet(), n = noise(r, 16, 8), f = noise(r, 4, 4), pw = back ? 64 : 32, ph = back ? 32 : 16;
  const frame = hex(0x4c6670), lip = hex(0x6d8a93), bolt = hex(0x9fb3b8), tint = Array.from({ length: (W / pw) * (H / ph) }, () => (r() - 0.5) * 0.18);
  s.each((x, y) => {
    const px = mod(x, pw), py = mod(y, ph), t = tint[Math.floor(y / ph) * (W / pw) + Math.floor(x / pw)];
    if (px === 0 || py === 0) return frame;
    if (py === 1) return lip;
    // Matte frost: a slow haze, a fine grain, a little darker toward the pane's foot.
    return [...ramp(FROST, 0.55 + t + (n(x, y) - 0.5) * 0.5 + (f(x, y) - 0.5) * 0.25 - (py / ph) * 0.18, x, y, 0.9), PANE_ALPHA];
  });
  for (let y = 0; y < H; y += ph) for (let x = 0; x < W; x += pw) { s.set(x, y, bolt); }
  return s;
}

const BLOCK = [0x7d9ea6, 0x8eafb5, 0x9fbfc3, 0xb1cfd0, 0xc3dcdb].map(hex);

/** Glass blocks, a cell each, set in pale mortar: pillowed, a pressed ring inside, no two quite alike. */
function blocks(r, back) {
  const s = new Sheet(), n = noise(r, 8, 8), mortar = hex(0x6f7f84), mortarHi = hex(0x8c9a9e);
  const tint = Array.from({ length: (W / 8) * (H / 8) }, () => (r() - 0.5) * 0.16);
  s.each((x, y) => {
    const px = mod(x, 8), py = mod(y, 8), t = tint[Math.floor(y / 8) * (W / 8) + Math.floor(x / 8)];
    if (px === 7 || py === 7) return back ? mortar : (px === 7 && py === 7 ? mortar : py === 7 ? mortarHi : mortar);
    const dx = Math.abs(px - 3), dy = Math.abs(py - 3), d = Math.max(dx, dy);
    if (d === 2 && !back) return BLOCK[1];
    const pillow = d === 3 ? -0.18 : d <= 1 ? 0.12 : 0;
    return ramp(BLOCK, 0.55 + t + pillow + (n(x, y) - 0.5) * 0.3 - (py - px) * 0.015, x, y, 0.6);
  });
  return s;
}

const OPAL = [0xa79cb3, 0xb7adc2, 0xc6bdd0, 0xd4cddc, 0xe0dbe6].map(hex);

/** Milky opal tiles, 16x16 laid in running bond, thin pale frames, a slow cloud through each. */
function opal(r, back) {
  const s = new Sheet(), n = noise(r, 16, 16), m = noise(r, 32, 16), frame = hex(0x6c6479), lip = hex(0x8b8398);
  const tw = back ? 32 : 16, th = back ? 32 : 16;
  s.each((x, y) => {
    const row = Math.floor(y / th), ox = row % 2 ? tw / 2 : 0, px = mod(x + ox, tw), py = mod(y, th);
    if (px === 0 || py === 0) return frame;
    if (px === 1 || py === 1) return lip;
    const cloud = Math.sin((x + y * 0.5) / 5 + m(x, y) * 6) * 0.12;
    return ramp(OPAL, 0.5 + (n(x, y) - 0.5) * 0.55 + cloud, x, y, 0.8);
  });
  return s;
}

// ---- mica: flakes, silvery to amber ---------------------------------------------------------------------------------
const FLAKES = [
  [0xd2cbb5, 0xbab29c, 0x9a937f], // silver
  [0xcdb88a, 0xb39d6e, 0x8f7a51], // champagne
  [0xc3995a, 0xa67c43, 0x7f5d30], // amber
  [0xa89a7c, 0x8f8266, 0x6f6450], // smoke
].map((p) => p.map(hex));

/** Overlapping flakes, wider than tall, each with a lit cleavage edge and a shadow on the flake below it. */
function flakes(r, back) {
  const s = new Sheet().each(() => FLAKES[1][2]), glint = hex(0xf1ead3), k = back ? 1.6 : 1;
  for (let f = 0; f < (back ? 70 : 200); f++) {
    const cx = r() * W, cy = r() * H, rx = (6 + r() * 8) * k, ry = (1.6 + r() * 1.8) * k, p = FLAKES[r() < 0.45 ? 0 : r() < 0.6 ? 1 : r() < 0.75 ? 2 : 3];
    const wob = Array.from({ length: 8 }, () => 0.8 + r() * 0.35), tilt = (r() - 0.5) * 0.25;
    const inside = (x, y) => {
      const dx = x - cx, dy = y - cy - dx * tilt, a = Math.atan2(dy / ry, dx / rx), w = wob[mod(Math.round((a / Math.PI + 1) * 4), 8)];
      return (dx / rx) ** 2 + (dy / ry) ** 2 <= w * w;
    };
    for (let y = Math.floor(cy - ry * 2); y <= cy + ry * 2; y++) for (let x = Math.floor(cx - rx * 1.5); x <= cx + rx * 1.5; x++) {
      if (!inside(x, y)) continue;
      const top = !inside(x, y - 1) || !inside(x - 1, y), foot = !inside(x, y + 1);
      s.set(x, y, top ? p[0] : foot ? p[2] : p[1]);
      if (!top && !foot && !back && r() < 0.025) s.set(x, y, glint);
    }
  }
  return s;
}

const AMBER = [0x9a6428, 0xb07834, 0xc48d43, 0xd6a456, 0xe4bb70].map(hex);

/**
 * Amber mica panes in a riveted iron frame, 32x16, as a stove's window but blind: cloudy where the sheets lie
 * thicker, crossed by the straight bright steps where a sheet split, flecked silver.
 */
function framed(r, back) {
  const s = new Sheet(), n = noise(r, 8, 8), m = noise(r, 16, 16), pw = back ? 64 : 32, ph = back ? 32 : 16;
  const frame = hex(0x34312e), frameHi = hex(0x5a5650), rivet = hex(0xa9a49a), fleck = hex(0xeee4c8), step = hex(0xedcf8e);
  s.each((x, y) => {
    const px = mod(x, pw), py = mod(y, ph);
    if (px <= 1 || py <= 1) return px === 1 && py > 1 ? frameHi : py === 1 && px > 1 ? frameHi : frame;
    return ramp(AMBER, 0.5 + (n(x, y) - 0.5) * 0.45 + (m(x, y) - 0.5) * 0.5, x, y, 0.5);
  });
  for (let k = 0; k < (back ? 6 : 26); k++) {
    // A step: a short straight edge, lit above and shadowed below, kept inside its pane.
    const x0 = Math.floor(r() * W), y0 = Math.floor(r() * H), len = 4 + Math.floor(r() * 9), dy = r() < 0.5 ? 0 : r() < 0.5 ? 1 : -1;
    for (let i = 0; i < len; i++) {
      const x = x0 + i, y = y0 + Math.floor((i * dy) / 3);
      if (mod(x, pw) <= 2 || mod(y, ph) <= 2 || mod(y + 1, ph) <= 1) continue;
      s.set(x, y, step); s.set(x, y + 1, AMBER[0]);
    }
  }
  for (let y = 0; y < H; y += ph) for (let x = 0; x < W; x += pw) for (const [dx, dy] of [[0, 0], [pw / 2, 0], [0, ph / 2]]) s.set(x + dx, y + dy, rivet);
  for (let k = 0; k < (back ? 0 : 50); k++) { const x = Math.floor(r() * W), y = Math.floor(r() * H); if (mod(x, pw) > 2 && mod(y, ph) > 2) s.set(x, y, fleck); }
  return s;
}

const SILVER = [0x7f7b6e, 0x969283, 0xaca796, 0xc1bca9, 0xd5d0bc].map(hex);
const SILVER_AMBER = [0x9a8156, 0xb3995f];

/** Silver mica cleaved in layers, a book of sheets on its side, stepped where one sheet meets the next every 32 px. */
function laminate(r, back) {
  const s = new Sheet(), wave = noise(r, 32, 64), bands = [], sw = back ? 64 : 32;
  for (let y = 0; y < H;) { const t = 1 + Math.floor(r() * 3); bands.push({ y, t, c: r() < 0.18 ? hex(SILVER_AMBER[Math.floor(r() * 2)]) : SILVER[1 + Math.floor(r() * 4)] }); y += t; }
  const offs = Array.from({ length: W / sw }, () => Math.floor(r() * 6));
  s.each((x, y) => {
    const sheet = Math.floor(x / sw), yy = mod(y + offs[sheet] + Math.round((wave(x, 0) - 0.5) * 3), H), px = mod(x, sw);
    let b = bands.length - 1; while (bands[b].y > yy) b--;
    const band = bands[b], c = band.c;
    if (px === 0) return SILVER[0];
    if (yy === band.y) return mix(c, hex(0xe8e3d0), 0.35); // the lit lip of each layer
    if (yy === band.y + band.t - 1 && band.t > 1) return shade(c, 0.85);
    return c;
  });
  return s;
}

// ---- hide: stretched membrane, seams sealed with fat, lashed ----------------------------------------------------------
const LEATHER = [0x9b6a4a, 0x8a5d40, 0xa57656, 0x7e553b, 0x946349].map(hex);
const FAT = hex(0xddcc95), FAT_SHADE = hex(0xb5a36e), CORD = hex(0x3b2a1f);

/** Patches of hide of every tone, their seams beaded with fat and stitched across. */
function patchwork(r, back) {
  const s = new Sheet(), seeds = [], n = noise(r, 8, 8), k = back ? 2 : 1;
  for (let i = 0; i < (back ? 7 : 20); i++) seeds.push({ x: r() * W, y: r() * H, c: LEATHER[i % LEATHER.length], sx: 0.8 + r() * 0.5 });
  const owner = (x, y) => {
    let best = 0, bd = Infinity;
    seeds.forEach((p, i) => { const dx = Math.min(Math.abs(x - p.x), W - Math.abs(x - p.x)) * p.sx, dy = Math.min(Math.abs(y - p.y), H - Math.abs(y - p.y)) * 1.4, d = dx * dx + dy * dy; if (d < bd) { bd = d; best = i; } });
    return best;
  };
  const o = new Sheet().each((x, y) => [owner(x, y), 0, 0]), id = (x, y) => o.get(x, y)[0];
  s.each((x, y) => {
    const me = id(x, y), seam = id(x + 1, y) !== me || id(x, y + 1) !== me;
    if (seam) return (x * 3 + y * 5) % (4 * k) === 0 && !back ? CORD : FAT;
    if (id(x, y - 1) !== me || id(x - 1, y) !== me) return FAT_SHADE;
    const near = id(x + 2, y) !== me || id(x, y + 2) !== me;
    const c = seeds[me].c, v = n(x, y);
    if (!back && r() < 0.02) return shade(c, 0.78); // a pore
    return shade(c, (near ? 0.86 : 1) * (0.94 + v * 0.12));
  });
  return s;
}

const RAWHIDE = [0x8e6f62, 0xa0806f, 0xb1917d, 0xc1a28c, 0xceb19a].map(hex);

/** Rawhide drum-tight on a frame of poles, 32x32: laced to the poles, lashed where they cross. */
function drum(r, back) {
  const s = new Sheet(), n = noise(r, 16, 16), pw = back ? 64 : 32, ph = 32;
  const pole = hex(0x4d3a2c), poleHi = hex(0x6e5640), lace = hex(0x3a281b), laceHi = hex(0xd6c48e);
  s.each((x, y) => {
    const px = mod(x, pw), py = mod(y, ph);
    if (px <= 1 || py <= 1) return px === 0 || py === 0 ? pole : poleHi;
    // Stretched: lit in the middle, falling off toward the poles; the lacing pulls the rim in.
    const cx = (px - pw / 2) / (pw / 2), cy = (py - ph / 2) / (ph / 2), d = Math.max(Math.abs(cx), Math.abs(cy));
    if (!back && (px === 3 || py === 3 || px === pw - 2 || py === ph - 2) && mod(px + py, 4) === 0) return lace;
    if (!back && (px === 2 || py === 2 || px === pw - 1 || py === ph - 1) && mod(px + py, 4) === 1) return laceHi;
    return ramp(RAWHIDE, 0.75 - d * d * 0.5 + (n(x, y) - 0.5) * 0.3, x, y, 0.6);
  });
  for (let y = 0; y < H; y += ph) for (let x = 0; x < W; x += pw) for (const [dx, dy, c] of [[-1, -1, lace], [1, 1, lace], [1, -1, laceHi], [-1, 1, laceHi], [0, 0, lace]]) s.set(x + dx, y + dy, c);
  return s;
}

const BEAST = [0x5b5f50, 0x666b5a, 0x727765, 0x7f8470, 0x8c917c].map(hex);

/** A beast's pebbled hide, olive-grey, in long strips sealed with fat along their seams and stitched. */
function beast(r, back) {
  const s = new Sheet(), n = noise(r, 16, 8), sh = back ? 32 : 16, offs = Array.from({ length: H / sh }, () => Math.floor(r() * W));
  const pebble = Array.from({ length: W * H }, () => r());
  s.each((x, y) => {
    const strip = Math.floor(y / sh), py = mod(y, sh), xx = mod(x + offs[strip], W);
    const joint = mod(xx, 48) === 0;
    if (py === sh - 1) return mod(x, 3) === 0 && !back ? CORD : FAT;
    if (py === 0 || joint) return py === 0 ? FAT_SHADE : FAT;
    // Pebbles: a lit pixel over a shadowed one, on a jittered grid.
    const v = 0.5 + (n(x, y) - 0.5) * 0.45 - (py / sh) * 0.15;
    if (!back && mod(xx, 3) === 1 && mod(py, 3) === 1 && pebble[y * W + x] < 0.6) return ramp(BEAST, v + 0.3, x, y, 0);
    if (!back && mod(xx, 3) === 1 && mod(py, 3) === 2 && pebble[(y - 1) * W + x] < 0.6) return ramp(BEAST, v - 0.25, x, y, 0);
    return ramp(BEAST, v, x, y, 0.5);
  });
  return s;
}

module.exports = { panes, blocks, opal, flakes, framed, laminate, patchwork, drum, beast };
