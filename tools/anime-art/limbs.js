// Whole limbs (whole-limbs): a hero's arm and leg are each one picture. A subject that draws a limb in two halves has them joined
// here, once every subject has drawn, into one straight picture in the rest pose, and only that is written: `X_upperArm` over
// `X_forearm` makes `X_arm`, `X_thigh` over `X_shin` makes `X_leg`, each with its `_lit` and `_skin` beside it; a half without
// one counts as empty. Joined at a seam where both are full width: the lower from there, the upper fading out over it down to
// where it still is, so neither's rounded end shows. A raw suit's sheet has its arm and leg whole already (suits.js).
(function () {
'use strict';
/** Pixels a world pixel and the margin round a rig's part (HiRes.Scale, HiRes.FigureMargin). */
const PX = 4, MARGIN = 3;
/** The upper bones' lengths, world pixels: Rig.Arm.Upper and Rig.Leg.Upper (WholeLimbsTests ties them). */
const ARM = 6.09, LEG = 8.82;
/** Each limb: its picture, its two halves, the upper bone's length and each half's joint in its box (the whole's at the upper's). */
const LIMBS = [
  { whole: 'arm', upper: 'upperArm', lower: 'forearm', bone: ARM, upperJoint: [2.5, 2.5], lowerJoint: [2.5, 2.9] },
  { whole: 'leg', upper: 'thigh', lower: 'shin', bone: LEG, upperJoint: [3, 2.5], lowerJoint: [3, 2.18] },
];
/** The seam: how far round the joint widths are looked at, how much of the widest counts as full, and where none is found. */
const SEAM_REACH = 3, FULL = 0.85, PLAIN = [-0.5, 0.5];

/** A canvas's opaque width in world pixels, row by row, over a second canvas laid under it (a skin under clothes, SuitArt.Inked). */
function widths(c, under) {
  const rows = [], d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data;
  const u = under && under.getContext('2d').getImageData(0, 0, under.width, under.height).data;
  for (let y = 0; y < c.height; y++) {
    let n = 0;
    for (let x = 0; x < c.width; x++) {
      const i = (y * c.width + x) * 4, a = d[i + 3] / 255, b = u && x < under.width && y < under.height ? u[i + 3] / 255 : 0;
      if (1 - (1 - a) * (1 - b) > 127 / 255) n++;
    }
    rows.push(n / PX);
  }
  return rows;
}

/** How far down from the joint a picture stays full width toward its rounded end; null if it never is. */
function edge(rows, top, down) {
  const row = (along) => Math.floor((along - top) * PX);
  const first = Math.max(0, row(-SEAM_REACH)), last = Math.min(rows.length - 1, row(SEAM_REACH));
  if (first > last) return null;
  let most = 0;
  for (let r = first; r <= last; r++) most = Math.max(most, rows[r]);
  if (most <= 0) return null;
  const whole = (r) => r >= first && r <= last && rows[r] >= most * FULL;
  const step = down ? 1 : -1;
  let at = Math.min(Math.max(row(0), first), last);
  while (!whole(at) && at - step >= first && at - step <= last) at -= step;
  if (!whole(at)) return null;
  while (whole(at + step)) at += step;
  return top + (down ? at + 1 : at) / PX;
}

/** Where the halves meet, world pixels down from the joint; each half's first row `upperTop`, `lowerTop` from it. */
function seamOf(upper, upperTop, lower, lowerTop) {
  const to = edge(upper, upperTop, true), from = edge(lower, lowerTop, false);
  if (to == null || from == null) return PLAIN;
  return from <= to ? [from, to] : [(from + to) / 2, (from + to) / 2];
}

const blank = (w, h) => Object.assign(document.createElement('canvas'), { width: w, height: h });

/**
 * The two halves joined, the upper's box at the top-left: the lower `dy` rows down from where the seam starts (`cut`, a row of the
 * whole), the upper over it, whole down to `fade`, gone by `gone`, a straight line between.
 */
function join(upper, lower, dy, w, h, cut, fade, gone) {
  const c = blank(w, h), x = c.getContext('2d');
  const skip = Math.max(0, cut - dy);
  if (lower && skip < lower.height) x.drawImage(lower, 0, skip, lower.width, lower.height - skip, 0, dy + skip, lower.width, lower.height - skip);
  if (upper) {
    const u = blank(upper.width, upper.height), ux = u.getContext('2d');
    ux.drawImage(upper, 0, 0);
    const img = ux.getImageData(0, 0, u.width, u.height);
    for (let y = 0; y < u.height; y++) {
      const along = y + 0.5, keep = gone <= fade ? (along < gone ? 1 : 0) : Math.min(1, Math.max(0, (gone - along) / (gone - fade)));
      if (keep >= 1) continue;
      for (let i = y * u.width * 4 + 3, end = i + u.width * 4; i < end; i += 4) img.data[i] = Math.round(img.data[i] * keep);
    }
    ux.putImageData(img, 0, 0);
    x.drawImage(u, 0, 0);
  }
  return c;
}

window.Limbs = {
  /** Joins every pair of halves among `made` ([name, canvas], the last of a name winning) into its whole, leaving no hero's half in it. */
  join(made) {
    const byName = new Map(made);
    for (const [name] of made) for (const L of LIMBS) {
      if (!name.endsWith('_' + L.upper)) continue;
      const stem = name.slice(0, -L.upper.length), half = (part, s = '') => byName.get(stem + part + s);
      if (!half(L.lower) || byName.has(stem + L.whole)) continue;
      const upper = half(L.upper), lower = half(L.lower);
      // The lower's box down the whole's: its joint at the elbow or knee, to the nearest of the picture's pixels.
      const dy = Math.round((L.upperJoint[1] + L.bone - L.lowerJoint[1]) * PX);
      const w = Math.max(upper.width, lower.width), h = Math.ceil(Math.max(upper.height, dy + lower.height) / PX) * PX;
      // The seam from the halves as worn, a skin under clothes; their first rows from the joint, the margin included.
      const [from, to] = seamOf(widths(upper, half(L.upper, '_skin')), -(L.upperJoint[1] + MARGIN + L.bone),
        widths(lower, half(L.lower, '_skin')), -(L.lowerJoint[1] + MARGIN));
      const joint = (MARGIN + L.upperJoint[1] + L.bone) * PX, row = (along) => joint + along * PX;
      const cut = Math.round(row(from));
      for (const s of ['', '_lit', '_skin']) {
        const u = half(L.upper, s), l = half(L.lower, s);
        if (!u && !l) continue;
        const c = join(u, l, dy, w, h, cut, row(from), row(to));
        made.push([stem + L.whole + s, c]);
        byName.set(stem + L.whole + s, c);
      }
    }
    const half = /^hero_.*(upperArm|forearm|thigh|shin)(_lit|_skin)?$/;
    for (let i = made.length - 1; i >= 0; i--) if (half.test(made[i][0])) made.splice(i, 1);
  },
};
})();
