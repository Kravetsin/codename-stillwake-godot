// The armour walls, redrawn: scrap, patched together with gaps at its seams, and iron, riveted plates. Both hold off
// meteors and leak air, against the sealing walls (seal.js). Iron's back walls are 16 px panels and grilles (backs()).
const { Sheet, W, H, mod, hex, mix, shade, noise } = require('./lib.js');

// ---- scrap --------------------------------------------------------------------------------------------------------------
const SCRAP = [0x5b6170, 0x646a78, 0x545966, 0x6a6f7a, 0x5f6370].map(hex);
const RUSTY = hex(0x6b5446), GAP = hex(0x121418), SEAM = hex(0x2e323b), HI = hex(0xa5a8b0), SH = hex(0x40444e);
const RUST = hex(0x8a5a3a), SCRATCH = hex(0x7f848f);

/** Splits a row of `total` into widths between lo and hi, the last taking what is left (never a sliver). */
function widths(r, total, lo, hi) {
  const out = [];
  for (let left = total; left > 0;) {
    let w = lo + Math.floor(r() * (hi - lo + 1));
    if (left - w < lo) w = left;
    out.push(w); left -= w;
  }
  return out;
}

/** Draws a plate's rim: lit along its top and left, a seam in shadow along its foot and right side. */
function rim(s, x0, y0, w, h, tone) {
  for (let i = 0; i < w; i++) { s.set(x0 + i, y0, mix(tone, hex(0xc8ccd4), 0.28)); s.set(x0 + i, y0 + h - 1, SEAM); }
  for (let j = 1; j < h - 1; j++) { s.set(x0, y0 + j, mix(tone, hex(0xc8ccd4), 0.12)); s.set(x0 + w - 1, y0 + j, SEAM); }
}

/** A dark slit along part of a seam: where the plates do not meet and the air gets out. */
function slit(s, r, x, y, len, across) {
  for (let i = 0; i < len; i++) {
    s.set(across ? x + i : x, across ? y : y + i, GAP);
    if (r() < 0.3) s.set(across ? x + i : x - 1, across ? y - 1 : y + i, GAP); // ragged
  }
}

/**
 * Plates of every tone in rows, riveted where they hold, slit where they do not; small patches riveted over holes, rust
 * running from a few rivets, a pit or two.
 */
function patched(r, back) {
  const s = new Sheet(), n = noise(r, 8, 8), rows = back ? [24, 16, 24] : [12, 8, 16, 12, 16];
  let y0 = 0;
  for (const h of rows) {
    let x0 = Math.floor(r() * W);
    for (const w of widths(r, W, back ? 28 : 14, back ? 56 : 36)) {
      const tone = r() < 0.12 ? RUSTY : SCRAP[Math.floor(r() * SCRAP.length)];
      for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) s.set(x0 + i, y0 + j, n(x0 + i, y0 + j) > 0.62 ? shade(tone, 0.93) : tone);
      rim(s, x0, y0, w, h, tone);
      if (!back && h >= 8 && r() < 0.75) {
        for (let i = 3; i < w - 3; i += 5) for (const j of [2, h - 3]) {
          s.set(x0 + i, y0 + j, HI); s.set(x0 + i, y0 + j + 1, SH);
          if (r() < 0.08) for (let d = 2; d < 2 + Math.floor(r() * 3); d++) s.set(x0 + i, y0 + j + d, RUST);
        }
      }
      if (r() < 0.65) { const len = 2 + Math.floor(r() * (h - 3)); slit(s, r, x0 + w - 1, y0 + Math.floor(r() * (h - len)), len, false); }
      if (r() < 0.5) { const len = 3 + Math.floor(r() * 8); slit(s, r, x0 + Math.floor(r() * (w - len)), y0 + h - 1, len, true); }
      x0 += w;
    }
    y0 += h;
  }
  for (let k = 0; k < (back ? 2 : 6); k++) {
    // A patch over a hole: smaller, its own tone, outlined, a rivet in each corner.
    const w = (6 + Math.floor(r() * 7)) * (back ? 2 : 1), h = (5 + Math.floor(r() * 4)) * (back ? 2 : 1), x = Math.floor(r() * W), y = Math.floor(r() * H);
    const tone = r() < 0.3 ? RUSTY : mix(SCRAP[Math.floor(r() * SCRAP.length)], hex(0x8a8f9a), 0.25);
    s.rect(x, y, x + w - 1, y + h - 1, tone);
    for (let i = 0; i < w; i++) { s.set(x + i, y - 1, SEAM); s.set(x + i, y + h, GAP); }
    for (let j = -1; j <= h; j++) { s.set(x - 1, y + j, SEAM); s.set(x + w, y + j, SEAM); }
    for (let i = 0; i < w; i++) s.set(x + i, y, mix(tone, hex(0xc8ccd4), 0.3));
    if (!back) for (const [dx, dy] of [[1, 1], [w - 2, 1], [1, h - 2], [w - 2, h - 2]]) s.set(x + dx, y + dy, HI);
  }
  for (let k = 0; k < (back ? 0 : 7); k++) {
    const x = Math.floor(r() * W), y = Math.floor(r() * H), len = 3 + Math.floor(r() * 4);
    for (let i = 0; i < len; i++) s.set(x + i, y, SCRATCH);
  }
  for (let k = 0; k < (back ? 0 : 3); k++) { const x = Math.floor(r() * W), y = Math.floor(r() * H); s.set(x, y, GAP); s.set(x + 1, y, SH); s.set(x, y + 1, SH); }
  return s;
}

const SHEET = [0x5e6269, 0x666a72, 0x575b63].map(hex);

/** Salvaged sheets, flat ones bolted at the corners and corrugated ones, laid in rows that never quite meet. */
function sheets(r, back) {
  const s = new Sheet(), n = noise(r, 16, 8), rust = noise(r, 8, 8), rows = back ? [32, 32] : [16, 16, 16, 16];
  let y0 = 0;
  for (const h of rows) {
    let x0 = Math.floor(r() * W);
    for (const w of widths(r, W, back ? 32 : 18, back ? 64 : 40)) {
      const tone = SHEET[Math.floor(r() * SHEET.length)], ribbed = r() < 0.45, short = r() < 0.6 ? 1 + Math.floor(r() * 2) : 0;
      for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
        const x = x0 + i, y = y0 + j;
        let c = n(x, y) > 0.6 ? shade(tone, 0.94) : tone;
        if (ribbed) c = [mix(c, hex(0xb0b4bc), 0.3), c, c, shade(c, 0.78)][mod(i, 4)];
        if (rust(x, y) > 0.78) c = mix(c, RUST, 0.3);
        s.set(x, y, c);
      }
      rim(s, x0, y0, w, h, tone);
      // Short of its neighbour: a gap down its right side, ragged where the cut was.
      for (let j = 0; j < h; j++) for (let d = 0; d < short + (r() < 0.35 ? 1 : 0); d++) s.set(x0 + w - 1 - d, y0 + j, GAP);
      if (!ribbed && !back) for (const [dx, dy] of [[2, 2], [w - 4, 2], [2, h - 3], [w - 4, h - 3]]) { s.set(x0 + dx, y0 + dy, HI); s.set(x0 + dx, y0 + dy + 1, SH); }
      if (r() < 0.4) { const len = 4 + Math.floor(r() * 10); slit(s, r, x0 + Math.floor(r() * Math.max(1, w - len)), y0 + h - 1, len, true); }
      x0 += w;
    }
    y0 += h;
  }
  return s;
}

// ---- iron ---------------------------------------------------------------------------------------------------------------
const IRON = { tones: [0x8a94a8, 0x8f99ad, 0x95a0b3].map(hex), lit: hex(0xb4bcca), shade: hex(0x6f7787), seam: hex(0x4b5262), rivet: hex(0xd3d8e0), rivetShade: hex(0x5f6676) };
const GUN = { tones: [0x7a8396, 0x808a9c, 0x868fa1].map(hex), lit: hex(0xa7afbe), shade: hex(0x646b7b), seam: hex(0x3c4250), rivet: hex(0xc4cad4), rivetShade: hex(0x525867) };

/** Plates of iron laid like bricks, 32x16, bevelled, two columns of rivets at each end. */
function riveted(r, back) {
  const s = new Sheet(), n = noise(r, 8, 4), p = IRON, pw = back ? 64 : 32, ph = back ? 32 : 16;
  const tone = Array.from({ length: (W / pw) * (H / ph) }, () => p.tones[Math.floor(r() * 3)]);
  s.each((x, y) => {
    const row = Math.floor(y / ph), ox = row % 2 ? pw / 2 : 0, px = mod(x + ox, pw), py = mod(y, ph);
    const t = tone[row * (W / pw) + Math.floor(mod(x + ox, W) / pw)];
    if (px === 0 || py === 0) return p.seam;
    if (py === 1 || px === 1) return p.lit;
    if (py === ph - 1 || px === pw - 1) return p.shade;
    if (!back && (px === 3 || px === pw - 4) && py >= 3 && py <= ph - 4 && mod(py, 3) === 0) return p.rivet;
    if (!back && (px === 3 || px === pw - 4) && py >= 4 && py <= ph - 3 && mod(py - 1, 3) === 0) return p.rivetShade;
    return n(x, y) > 0.64 ? shade(t, 0.96) : t;
  });
  return s;
}

/** Heavy square plates, 32x32, a double seam between them, a ring of rivets round each, worn bright in places. */
function heavy(r, back) {
  const s = new Sheet(), n = noise(r, 16, 16), p = GUN, side = back ? 64 : 32;
  const tone = Array.from({ length: (W / side) * (H / side) * 4 }, () => p.tones[Math.floor(r() * 3)]);
  s.each((x, y) => {
    const px = mod(x, side), py = mod(y, side), t = tone[Math.floor(y / side) * (W / side) + Math.floor(x / side)];
    if (px <= 1 || py <= 1) return px === 0 || py === 0 ? p.seam : p.shade;
    if (py === 2 || px === 2) return p.lit;
    if (py === side - 1 || px === side - 1) return p.shade;
    const ring = (px === 4 || px === side - 4 || py === 4 || py === side - 4) && px >= 4 && px <= side - 4 && py >= 4 && py <= side - 4;
    if (!back && ring && mod(px + py, 4) === 0) return p.rivet;
    if (!back && (px === 4 || px === side - 4 || py === 4 || py === side - 4) && px >= 4 && py >= 5 && px <= side - 4 && py <= side - 3 && mod(px + py - 1, 4) === 0) return p.rivetShade;
    return ramp3(t, n(x, y));
  });
  for (let k = 0; k < (back ? 0 : 8); k++) {
    const x = Math.floor(r() * W), y = Math.floor(r() * H), len = 2 + Math.floor(r() * 4);
    for (let i = 0; i < len; i++) if (mod(x + i, side) > 5 && mod(y, side) > 5 && mod(x + i, side) < side - 5 && mod(y, side) < side - 5) s.set(x + i, y, p.lit);
  }
  return s;
}
const ramp3 = (t, v) => (v > 0.66 ? mix(t, hex(0xb0b8c6), 0.12) : v < 0.3 ? shade(t, 0.95) : t);

/** Iron's 16 px back walls, a panel and a grille, cut over the 2x2 cells of a back wall's character (0104). */
function backs(style) {
  const plain = new Sheet(16, 16), dark = hex(0x2f3646), deep = hex(0x1a1e27), lip = hex(0x3d4556), riv = hex(0x5d626f), low = hex(0x252a37);
  plain.each((x, y) => {
    if (x === 15 || y === 15) return deep;
    if (x === 0 || y === 0) return lip;
    if (style === 'heavy' && x >= 3 && x <= 12 && y >= 3 && y <= 12) return x === 12 || y === 12 ? lip : x === 3 || y === 3 ? deep : low;
    return dark;
  });
  for (const [x, y] of [[2, 2], [13, 2], [2, 13], [13, 13]]) { plain.set(x, y, riv); plain.set(x, y + 1, deep); }
  const grille = new Sheet(16, 16);
  grille.c = plain.c.slice();
  if (style === 'heavy') {
    for (let y = 4; y <= 11; y++) for (let x = 4; x <= 11; x++) grille.set(x, y, mod(x + y, 4) === 0 || mod(x - y, 4) === 0 ? lip : deep);
  } else {
    for (let y = 4; y <= 11; y += 2) for (let x = 3; x <= 12; x++) { grille.set(x, y, deep); grille.set(x, y + 1, x === 3 ? deep : lip); }
  }
  return { plain, grille };
}

module.exports = { patched, sheets, riveted, heavy, backs };
