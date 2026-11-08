// The tablet edge's keys as small green phosphor screens (the hologram-keys ADR), as picked: each motif a white mask the game tints and
// lights, and a soft copy of it for the glow round it. Drawn straight on a canvas, no finish: at 18 pixels it would not show.
(function () {
'use strict';
const { canvas } = Anime;
/** The motif's square and the glow's margin round it, in screen pixels; the pictures are 4 times that. */
const ICON = 18, GLOW_PAD = 6, X = 4;

// ---------- the motifs, in a 16-unit square, y down ----------
const rr = (x, y, w, h, r) => `M ${x + r} ${y} L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
const ring = (cx, cy, r) => { let d = ''; for (let k = 0; k < 48; k++) { const a = (k / 48) * Math.PI * 2; d += `${k ? 'L' : 'M'} ${cx + r * Math.cos(a)} ${cy + r * Math.sin(a)} `; } return d + 'Z'; };
const fill = (d) => ({ fill: d }), stroke = (d, w) => ({ stroke: d, w });

/**
 * Each key's parts: 'solid' the mark, 'band' a part of it at half strength, 'lamp' cut out of it.
 * The air: three strokes, the top one curling up, the lowest down. The build: three offset courses of bricks, a corner knocked off one.
 * The research: a bulb, a filament cut in it. The market: an onion bulb from a pointed neck, two inner layers paler.
 * The suit: a vest with a round lamp port on its chest. The journal: an open book, two lines a page.
 */
const MOTIFS = {
  air: {
    solid: [
      stroke('M 0.5 5.9 C 4 5.3 8 6.1 11.4 5.5 C 14.3 5.0 14.5 1.4 12.0 1.3 C 10.5 1.25 9.9 2.7 10.9 3.4', 1.95),
      stroke('M 1.6 8.7 C 5.5 8.1 10.5 9.1 15.5 8.3', 1.9),
      stroke('M 0.5 11.5 C 3.8 10.9 7.2 11.7 9.6 11.6 C 12.5 11.5 12.8 15.1 10.4 15.25 C 8.9 15.35 8.3 13.9 9.3 13.2', 1.8),
    ],
  },
  build: {
    solid: [
      fill(rr(0.4, 1.8, 7.1, 3.5, 0.55)), fill('M 8.4 1.8 L 14.6 1.8 Q 15.6 1.8 15.6 3.0 L 15.6 4.7 Q 15.6 5.3 15.0 5.3 L 8.95 5.3 Q 8.4 5.3 8.4 4.75 L 8.4 2.35 Q 8.4 1.8 8.95 1.8 Z'),
      fill(rr(0.4, 6.2, 3.1, 3.5, 0.5)), fill(rr(4.4, 6.25, 7.2, 3.5, 0.55)), fill(rr(12.5, 6.2, 3.1, 3.5, 0.5)),
      fill(rr(0.4, 10.6, 7.1, 3.5, 0.55)), fill('M 8.4 10.75 L 15.05 10.75 Q 15.6 10.75 15.6 11.3 L 15.6 13.75 Q 15.6 14.25 15.05 14.25 L 8.95 14.25 Q 8.4 14.25 8.4 13.7 L 8.4 12.2 L 9.6 10.75 Z'),
    ],
  },
  research: {
    solid: [fill('M 5.3 10.8 C 2.4 9.3 1.8 5.4 3.8 3.0 C 5.8 0.6 10.2 0.6 12.2 3.0 C 14.2 5.4 13.6 9.3 10.7 10.8 L 10.4 12.2 L 5.6 12.2 Z'),
      fill('M 5.5 12.7 L 10.5 12.7 L 10.5 14.3 L 5.5 14.3 Z'), fill('M 6.7 14.8 L 9.3 14.8 L 8.7 15.8 L 7.3 15.8 Z')],
    lamp: [fill('M 6.2 9.8 L 7 6.6 L 8 8.2 L 9 6.6 L 9.8 9.8 L 9.2 9.8 L 8.9 8.0 L 8 9.4 L 7.1 8.0 L 6.8 9.8 Z')],
  },
  market: {
    solid: [fill('M 8 0.8 Q 8.5 2.6 9.7 3.7 Q 14.9 6.5 14.3 11.0 Q 13.5 15.2 8 15.4 Q 2.5 15.2 1.7 11.0 Q 1.1 6.5 6.3 3.7 Q 7.5 2.6 8 0.8 Z')],
    band: [fill('M 8 4.6 Q 4.0 7.4 4.6 11.2 Q 5.2 14.0 8 14.4 Q 5.9 13.4 5.8 11.0 Q 5.6 7.6 8 4.6 Z'), fill('M 8 4.6 Q 12.0 7.4 11.4 11.2 Q 10.8 14.0 8 14.4 Q 10.1 13.4 10.2 11.0 Q 10.4 7.6 8 4.6 Z')],
  },
  suit: {
    solid: [fill('M 5.5 1.7 Q 8 5.3 10.5 1.7 L 12.9 2.2 Q 15.5 2.9 15.7 5.6 L 15.6 9.3 Q 14.6 9.7 13.6 9.4 L 13.25 6.9 '
      + 'Q 12.3 7.5 12.4 9.2 L 11.95 12.4 Q 12.5 14.0 12.35 15.2 Q 8 15.9 3.65 15.2 Q 3.5 14.0 4.05 12.4 L 3.6 9.2 Q 3.7 7.5 2.75 6.9 '
      + 'L 2.4 9.4 Q 1.4 9.7 0.4 9.3 L 0.3 5.6 Q 0.5 2.9 3.1 2.2 Z')],
    lamp: [fill(ring(8, 7.7, 1.95))],
  },
  journal: {
    solid: [fill('M 7.75 4.6 Q 4.8 2.7 0.6 3.3 L 0.4 13.4 Q 4.6 12.7 7.75 14.7 Z'), fill('M 8.25 4.6 Q 11.2 2.7 15.4 3.3 L 15.6 13.4 Q 11.4 12.7 8.25 14.7 Z'),
      fill('M 0.4 12.6 Q 4.6 12.1 8 14.2 Q 11.4 12.1 15.6 12.6 L 15.6 14.2 Q 11.6 13.8 8 15.6 Q 4.4 13.8 0.4 14.2 Z')],
    band: [fill('M 7.6 4.9 Q 4.8 3.3 1.4 3.9 L 1.3 12.3 Q 4.8 11.9 7.6 13.4 Z'), fill('M 8.4 4.9 Q 11.2 3.3 14.6 3.9 L 14.7 12.3 Q 11.2 11.9 8.4 13.4 Z')],
    lamp: [fill('M 2.6 6.0 Q 4.6 5.6 6.6 6.4 L 6.6 7.2 Q 4.6 6.4 2.6 6.8 Z'), fill('M 2.6 8.6 Q 4.6 8.2 6.6 9.0 L 6.6 9.8 Q 4.6 9.0 2.6 9.4 Z'),
      fill('M 9.4 6.4 Q 11.4 5.6 13.4 6.0 L 13.4 6.8 Q 11.4 6.4 9.4 7.2 Z'), fill('M 9.4 9.0 Q 11.4 8.2 13.4 8.6 L 13.4 9.4 Q 11.4 9.0 9.4 9.8 Z')],
  },
};

/** The motif in white on `c`, its square at x, y, s wide: the band at half strength, the lamp cut out. */
function mark(c, key, x, y, s) {
  const m = MOTIFS[key];
  c.save();
  c.translate(x, y); c.scale(s / 16, s / 16);
  c.lineCap = 'round'; c.lineJoin = 'round';
  c.fillStyle = c.strokeStyle = '#fff';
  for (const it of m.solid) {
    if (it.fill) c.fill(new Path2D(it.fill));
    else { c.lineWidth = it.w; c.stroke(new Path2D(it.stroke)); }
  }
  c.globalCompositeOperation = 'destination-out';
  c.globalAlpha = 0.5; for (const it of m.band || []) c.fill(new Path2D(it.fill));
  c.globalAlpha = 1; for (const it of m.lamp || []) c.fill(new Path2D(it.fill));
  c.restore();
}

Subjects.edgekeys = function () {
  for (const key of Object.keys(MOTIFS)) {
    const icon = canvas(ICON * X, ICON * X);
    mark(icon.getContext('2d'), key, 0, 0, ICON * X);
    out(`holo_${key}`, icon);
    // The glow a 5-pixel shadow casts round it, as the page drew it: the mark blurred, its margin room to spread.
    const wide = (ICON + 2 * GLOW_PAD) * X, glow = canvas(wide, wide), g = glow.getContext('2d');
    g.filter = `blur(${2.5 * X}px)`;
    g.drawImage(icon, GLOW_PAD * X, GLOW_PAD * X);
    out(`holo_${key}_glow`, glow);
  }
};
})();
