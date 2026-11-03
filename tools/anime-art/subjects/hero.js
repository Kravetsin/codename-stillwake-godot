// The hero: one body and one head on the game's joints, four sets of clothes and one suit over them (2026-11-02-hero-clothes).
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;

// Materials. The light falls from straight above, so a part looks the same mirrored: a hero's facing never moves it.
const G = { base: '#3f4656', shade: '#262a44', deep: '#191c32', lite: '#67718a', spec: '#c4d2e4', rim: '#5aa6c8', line: '#14172c' };
const PK = { base: '#7c8699', shade: '#474e6c', deep: '#2f3350', lite: '#aab4c6', spec: '#ffffff', rim: '#7ec8e0', line: '#262b46' };
const SH = { base: '#8f99a8', shade: '#565e7c', deep: '#3a3f5e', lite: '#c4ccd8', spec: '#ffffff', rim: '#7ec8e0', line: '#262b46' };
const GL = { base: '#1a2028', shade: '#10141b', deep: '#0a0d12', lite: '#2c3844' };
// Skin's base stays what LookTint.SkinPaint expects; the flag lets a part's skin be baked apart from its cloth.
const SK = { base: '#f0c49a', shade: '#b98a6e', deep: '#8a5f50', lite: '#fbe0c0', skin: true };
const HAIR = { base: '#7a4f30', shade: '#573439', deep: '#40263a', lite: '#a4744a' };

/** An arm's or leg's body: round at both joints, so nothing gaps when it turns. */
const limb = (w0, w1, cx, y0, y1) => {
  const a = w0 / 2, b = w1 / 2;
  return `M${cx - a} ${y0}C${cx - a} ${y0 - a * 1.3} ${cx + a} ${y0 - a * 1.3} ${cx + a} ${y0}L${cx + b} ${y1}C${cx + b} ${y1 + b * 1.3} ${cx - b} ${y1 + b * 1.3} ${cx - b} ${y1}Z`;
};
/** The hard shadow under a fold of cloth: a wedge hanging from the crease. */
const wedge = (x0, y0, x1, y1, d = .8) => { const mx = (x0 + x1) / 2, my = (y0 + y1) / 2; return `M${x0} ${y0}Q${mx} ${my + d * .15} ${x1} ${y1}Q${mx} ${my + d} ${x0} ${y0}Z`; };
const crease = (x0, y0, x1, y1) => `M${x0} ${y0}Q${(x0 + x1) / 2} ${(y0 + y1) / 2 + .12} ${x1} ${y1}`;
const folds = (...list) => ({ shade: list.map((f) => wedge(...f)), lines: list.map((f) => crease(f[0], f[1], f[2], f[3])) });
/** Cloth let in over a hole: a skewed piece, its stitches showing. */
const patch = (a, x, y, w, h, mat) => {
  a.shape(poly(x, y + .1, x + w, y, x + w + .1, y + h, x + .1, y + h + .1), mat, { outer: false, shade: poly(x - 1, y + h - .35, x + w + 1, y + h - .35, x + w + 1, y + h + 1, x - 1, y + h + 1) });
  let s = '';
  for (let i = x + .3; i < x + w; i += .8) s += `M${i} ${y - .12}L${i + .1} ${y + .28}M${i} ${y + h - .2}L${i + .1} ${y + h + .22}`;
  a.line(s, { w: .9 });
};
const taped = (a, x, y, w, c) => { a.flat(poly(x, y, x + w, y - .1, x + w, y + .8, x, y + .9), c); a.line(`M${x + .5} ${y}L${x + .7} ${y + .9}M${x + w - .6} ${y}L${x + w - .4} ${y + .85}`, { w: .8 }); };
const mat = (base, shade, deep, lite) => ({ base, shade, deep, lite });

// ---------- the head, the skull 9.3 pixels tall in its 12 pixel box, the neck still at the joint ----------
const LID = '#1c1214', IRIS = '#231816', DULL = '#b0a594', BROW = '#2c1c20', LIP = '#5e3228';
const CRANIUM = 'M4.2 8.6C3.4 7.6 3.1 6.6 3.2 5.6C3.3 3.4 5 2.2 7.2 2.2C9.4 2.2 10.8 3.4 11 5.2';
const SKULL = CRANIUM + 'L11 5.8L11.3 6.4L11.8 7.8L11.2 8.1L11.3 9.2L11.4 9.8L11.1 10.8C10.6 11.6 9.6 11.9 8.8 11.7C7.6 11.5 6.6 11 5.8 10.2C5.2 9.8 4.6 9.2 4.2 8.6Z';
const headPic = { w: 13, h: 12, draw(a) {
  a.shape(SKULL, SK, { shade: 'M2 9.1L6.2 8.9L8.4 10.5L13 10.2L13 13.5L2 13.5Z', lite: 'M5 2.7C6.4 2.2 8.4 2.2 9.6 2.9L9.2 3.5L5.4 3.5Z' });
  a.shape(ell(4.7, 6.9, .7, 1), SK, { shade: .3, lines: ['M4.6 6.3C5 6.6 5 7.2 4.7 7.6'] });
} };
// Faces: small eyes in dark sockets, no shine in them. The near eye at 8.6, 6.3.
const faceHollow = { w: 13, h: 12, draw(a) {
  a.flat('M7.2 5.9C8 5.4 9.6 5.4 10.1 5.9L10 7C9.4 7.4 8 7.4 7.4 7Z', SK.shade);
  a.flat('M7.9 6.4C8.3 6.1 9.1 6.1 9.5 6.35C9.1 6.7 8.3 6.7 7.9 6.4Z', DULL);
  a.flat(ell(8.85, 6.38, .3, .28), IRIS);
  a.line('M7.7 6.35C8.2 5.95 9.2 5.95 9.7 6.3', { w: 1.6, c: LID });
  a.line('M7.1 5.2C8 4.85 9.2 4.9 10.2 5.3', { w: 2.6, c: BROW });
  a.flat('M10.3 6.3C10.5 6.15 10.8 6.15 10.95 6.3C10.8 6.5 10.5 6.5 10.3 6.3Z', LID);
  a.line('M10.4 5.3L11 5.35', { w: 1.6, c: BROW });
  a.line('M10.5 8.1L11 8.2', { w: 1.2, c: SK.deep });
  a.flat('M6.8 8C7.4 8.4 8 9.2 8.2 10C7.6 9.8 7 9.2 6.6 8.6Z', SK.shade);
  a.line('M9.2 9.7C9.8 9.85 10.4 9.8 11.1 9.6', { w: 1.8, c: LIP });
  a.line('M8.9 9.1C8.7 9.5 8.8 10 9 10.3', { w: 1, c: SK.deep });
} };
// Screwing up its eyes: two dark slits, one brow split by an old cut, the jaw set.
const faceSquint = { w: 13, h: 12, draw(a) {
  a.flat('M7.3 5.8C8 5.5 9.7 5.5 10.2 5.9L10 6.9C9.2 7.2 8 7.2 7.5 6.9Z', SK.shade);
  a.flat('M7.8 6.45C8.4 6.2 9.2 6.2 9.8 6.4C9.2 6.7 8.4 6.75 7.8 6.45Z', LID);
  a.line('M7.3 5.1C8.2 4.9 9.2 5 10.3 5.5', { w: 3, c: BROW });
  a.flat('M8.5 4.5L8.9 4.5L8.6 5.5L8.2 5.5Z', SK.lite);
  a.flat('M10.3 6.35C10.5 6.2 10.8 6.2 10.95 6.35C10.8 6.5 10.5 6.5 10.3 6.35Z', LID);
  a.line('M10.5 8.1L11 8.2', { w: 1.2, c: SK.deep });
  a.line('M9.3 9.75L11.1 9.7', { w: 2.2, c: LIP });
  a.line('M7.4 8.6C7.8 9.2 8 9.8 8.1 10.4', { w: 1.2, c: SK.deep });
} };
// No sleep in days: the eye open and flat, a dark bag under it, the mouth's corner dropped.
const faceSleepless = { w: 13, h: 12, draw(a) {
  a.flat('M7.3 6.6C7.9 7.5 9.5 7.6 10.1 6.9C10.1 7.7 9.3 8.1 8.5 8C7.8 7.9 7.4 7.3 7.3 6.6Z', SK.deep);
  a.flat('M7.8 6.2C8.2 5.8 9.1 5.8 9.5 6.2C9.1 6.7 8.2 6.7 7.8 6.2Z', DULL);
  a.flat(ell(8.75, 6.2, .3, .32), IRIS);
  a.line('M7.6 6.15C8.1 5.65 9.2 5.65 9.7 6.15', { w: 1.3, c: LID });
  a.line('M7.3 4.9C8.2 4.7 9.2 4.9 10.2 5.5', { w: 2.2, c: BROW });
  a.flat('M10.3 6.2C10.5 6 10.8 6 10.95 6.2C10.8 6.45 10.5 6.45 10.3 6.2Z', LID);
  a.line('M10.5 8.1L11 8.2', { w: 1.2, c: SK.deep });
  a.line('M9.1 10C9.7 9.75 10.4 9.7 11.1 9.8', { w: 1.8, c: LIP });
} };
// Lids that have come down: the iris half under a heavy fold, a straight brow, a mouth set in a line.
const faceHooded = { w: 13, h: 12, draw(a) {
  a.flat('M7.3 5.9C8 5.4 9.6 5.4 10.1 5.9L10 7C9.4 7.4 8 7.4 7.4 7Z', SK.shade);
  a.flat('M7.9 6.5C8.3 6.25 9.1 6.25 9.5 6.45C9.1 6.75 8.3 6.8 7.9 6.5Z', DULL);
  a.flat(ell(8.85, 6.48, .28, .24), IRIS);
  a.flat('M7.6 6.35C8.2 5.9 9.2 5.9 9.8 6.3C9.2 6.15 8.2 6.15 7.7 6.55Z', LID);
  a.line('M7.1 5.1C8 4.85 9.2 4.85 10.3 5.15', { w: 3, c: BROW });
  a.flat('M10.3 6.35C10.5 6.2 10.8 6.2 10.95 6.35C10.8 6.5 10.5 6.5 10.3 6.35Z', LID);
  a.line('M10.5 8.1L11 8.2', { w: 1.2, c: SK.deep });
  a.line('M9.2 9.75L11.1 9.65', { w: 1.8, c: LIP });
} };
// A woman past caring how she looks: thin arched brows, the lashes still dark, a burn healed pink across the cheek.
const faceBurned = { w: 13, h: 12, draw(a) {
  a.flat('M6.2 7.6C6.8 7.3 7.6 7.5 8 8C7.9 8.6 7.3 9 6.7 9C6.3 8.7 6 8.1 6.2 7.6Z', '#c98a7c');
  a.flat('M7.8 6.3C8.2 5.8 9.1 5.8 9.5 6.3C9.1 6.8 8.2 6.8 7.8 6.3Z', DULL);
  a.flat(ell(8.8, 6.25, .32, .36), IRIS);
  a.flat('M7.5 6.3C8 5.5 9.3 5.5 9.8 6.1C9.2 5.85 8.2 5.9 7.7 6.45Z', LID);
  a.line('M7.3 5C8 4.5 9.2 4.5 10.1 5', { w: 1.4, c: BROW });
  a.flat('M10.3 6.25C10.5 6.05 10.8 6.05 10.95 6.25C10.8 6.45 10.5 6.45 10.3 6.25Z', LID);
  a.line('M10.5 8.1L11 8.2', { w: 1.2, c: SK.deep });
  a.line('M9.5 9.7C10 9.8 10.5 9.8 11.1 9.65', { w: 1.3, c: LIP });
} };

// ---------- hair and beards, over the same skull ----------
const hairOf = (draw) => ({ w: 16, h: 18, shift: [2, 2], draw });
// Shaved: a dark nap on the scalp, no outline, a nick healed white.
const hairStubble = hairOf((a) => {
  a.shape('M4.4 8.2C3.6 7.4 3 6.6 3.05 5.6C3.2 3.2 5 2.05 7.2 2.05C8.8 2.05 10 2.7 10.6 3.7C9.2 3.3 7.6 3.5 6.6 4.2C5.8 4.8 5.6 5.6 5.4 6.6C5.2 7.4 4.9 7.9 4.4 8.2Z', HAIR, { outer: false, edge: false });
  a.line('M4 3.8L3.7 4.3M5.4 3L5.1 3.5M6.9 2.5L6.6 3M8.6 2.7L8.3 3.2M3.9 5.6L3.6 6.1M4.9 4.8L4.6 5.3', { w: .9, c: HAIR.deep });
  a.flat('M7.6 2.6L8.4 3.3L8.2 3.5L7.5 2.8Z', '#c8ae96');
});
// Cropped with a blunt blade: short, close to the skull, the front uneven.
const hairCrop = hairOf((a) => {
  a.shape('M4.2 8.4C3.2 7.6 2.7 6.4 2.8 5.2C3 2.9 5 1.6 7.3 1.6C9.6 1.6 11.2 2.9 11.3 4.8L10.6 4.4L10.2 3.9L9.2 4.1L8.4 3.7L7.4 4.2L6.6 4L6 5L5.6 6.2L5.2 7.4Z', HAIR, {
    shade: 'M2 5.6L6 4.6L6 9L2 9Z', lite: 'M4.6 2.2C6.2 1.6 8.4 1.6 9.8 2.3L9.4 2.8L5 2.9Z', lines: ['M6.6 4C6.4 3.2 6.4 2.4 6.6 1.9', 'M8.4 3.7C8.4 3 8.6 2.4 8.8 2', 'M4.6 6.6C4 5.4 4 4 4.4 3'] });
});
// Grown out and never combed: clumps down the back of the neck, the fringe in points, the ear under it.
const hairMatted = hairOf((a) => {
  a.shape('M2.6 4.8C2.8 2.6 5 1.2 7.4 1.2C9.8 1.2 11.6 2.6 11.6 4.8L11 5.6L10.6 4.6L9.8 5.2L9.2 4.2L8.2 4.8L7.4 4L6.6 5L6 4.8L5.6 6.6L5.2 8L5.2 9.6L4.4 11L4 10L3.4 11.2L3 10L2.4 10.4L2.4 8.8L1.9 8L2.4 6.6Z', HAIR, {
    shade: 'M1 7.6L6 7.2L6 12L1 12Z', deep: 'M1 10L6 9.6L6 12L1 12Z', lite: 'M4.6 1.9C6 1.2 8.6 1.2 10 2L9.4 2.6L5 2.7Z',
    lines: ['M7.4 4C7.2 3 7.4 2.2 7.8 1.6', 'M9.2 4.2C9.2 3.2 9.4 2.6 9.8 2.1', 'M4.6 10.6C4.4 8.6 4.2 6 4.6 3.4', 'M3.2 10.6C3 8.8 3 7 3.4 4.6', 'M6 4.8C5.8 3.8 5.8 3 6 2.2'] });
});
// A rag tied round the skull, knotted behind; what the hold's dust does not get into.
const hairRag = hairOf((a) => {
  a.shape('M3 5.4C3.1 3.2 5 1.8 7.3 1.8C9.6 1.8 11.1 3.2 11.2 5L11.1 5.2C9 4.6 6 4.8 4.8 6L3.2 6.4Z', HAIR, { shade: 'M2 5.2L6 4.6L12 4.6L12 6L2 7Z', lite: 'M5 2.4C6.4 1.8 8.4 1.8 9.6 2.5L9.2 3L5.2 3.1Z', lines: ['M4.2 3.6C6 3.2 8.6 3.2 10.4 3.8', 'M3.4 5C5 4.2 7.4 4 9.2 4.2'] });
  a.shape('M2.9 5.6L1.6 5.2L1.2 6.6L2.2 6.4L2 7.8L3.2 6.6Z', HAIR, { shade: .35 });
});
// Going thin: only the sides and back left, the crown bare.
const hairThin = hairOf((a) => {
  a.shape('M4.2 8.4C3.2 7.6 2.9 6.6 3 5.4C3.1 4.4 3.6 3.6 4.4 3L5.2 4.4L5.6 6L5.2 7.6Z', HAIR, { outer: false, shade: 'M2 6.6L6 6.4L6 9L2 9Z', lines: ['M4.4 7.4C4 6.2 4 5 4.4 3.8'] });
});
// Pulled back and tied at the nape: a tail hanging clear behind, a strip of cord round it, its end frayed.
const hairTail = hairOf((a) => {
  a.shape('M2.8 6.4C1.8 6.6 1.2 7.6 1.2 8.8C1.2 10.4 1.5 12 2.2 13.4L2.7 12.8L3.1 13.8L3.6 12.9L4 13.2C4.5 11.8 4.8 10 4.6 8.4C4.4 7.2 3.8 6.2 2.8 6.4Z', HAIR, { shade: 'M1 10.6L5 10.6L5 14L1 14Z', lines: ['M2.4 7.4C2.3 9.2 2.5 11 3 12.6', 'M3.6 7.6C3.9 9.2 3.9 10.8 3.7 12.2'] });
  a.shape('M1.2 7.5C2 7 3.6 7 4.6 7.5L4.6 8.5C3.6 8.1 2 8.1 1.2 8.5Z', G, { outer: false });
  a.shape('M4.2 8.4C3.2 7.6 2.8 6.4 2.9 5.2C3.1 2.9 5 1.7 7.3 1.7C9.6 1.7 11.1 2.9 11.2 4.6C9.6 3.7 7.6 3.7 6.4 4.4C5.6 5 5.4 6.2 5.2 7.2Z', HAIR, { shade: 'M2 5.8L6 5.2L6 9L2 9Z', lite: 'M4.6 2.3C6.2 1.7 8.4 1.7 9.8 2.4L9.4 2.9L5 3Z', lines: ['M10.6 4C8.6 2.8 6 2.6 3.8 3.6', 'M8.4 3.8C6.8 3.2 5 3.6 3.4 5'] });
});
const beardJaw = hairOf((a) => {
  a.shape('M5.2 8.6L6.4 8.7C6.8 9.6 7.7 10.2 8.8 10.2C9.6 10.3 10.6 10.2 11.3 10L11.6 10.3C11.3 11.2 10.5 12 9.4 12.2C8 12.3 6.4 11.6 5.6 10.6C5.2 10 5 9.2 5.2 8.6Z', HAIR, { shade: 'M4 11L12.5 11L12.5 13L4 13Z', lines: ['M6 9.4L6.4 10.8', 'M7.4 10.2L7.6 11.6', 'M8.9 10.5L9 11.9', 'M10.3 10.4L10.2 11.6'] });
  a.shape('M9.2 9.2C10 8.8 10.9 8.8 11.4 9L11.3 9.45C10.6 9.3 9.9 9.35 9.3 9.6Z', HAIR, { outer: false });
});
// The moustache grown down into it, the chin under a mass of it.
const beardFull = hairOf((a) => {
  a.shape('M4.8 8.2L6.2 8.4C6.6 9.6 7.6 10.2 8.8 10.2L11.3 10L11.8 10.4C11.6 11.6 10.8 12.6 9.6 12.9C8 13.2 6.2 12.4 5.4 11C4.8 10.2 4.6 9.2 4.8 8.2Z', HAIR, { shade: 'M4 11.4L12.5 11.4L12.5 13.5L4 13.5Z', lines: ['M5.8 9.2L6.2 11.2', 'M7.2 10.2L7.5 12.4', 'M8.8 10.5L8.9 12.7', 'M10.3 10.4L10.2 12.2'] });
  a.shape('M9.1 9.1C10 8.7 10.9 8.7 11.5 8.9L11.4 9.5C10.6 9.3 9.9 9.35 9.2 9.65Z', HAIR, { outer: false });
});

// ---------- hands and feet ----------
/** The hanging hand from outside: the back of it, the fingers curled under and shaded, the thumb a dark sliver at the front edge, behind the fingers. */
const hand = (a, S) => {
  a.shape('M3.5 7.9C4.1 7.8 4.7 8.2 4.8 8.9C4.9 9.5 4.6 10.1 4.1 10.1C3.7 10 3.4 9.5 3.3 8.9Z', { ...S, base: S.shade, lite: S.shade, shade: S.deep }, { outer: false, shade: 'M3 9.3L5.2 9.3L5.2 10.6L3 10.6Z' });
  a.shape('M1.2 7.3L3.9 7.3C4.1 7.9 4.1 8.6 4 9.3C4.2 9.8 4.1 10.4 3.8 10.8C3.2 11.2 2.4 11.2 1.8 10.9C1.3 10.6 1 10 .9 9.4C.8 8.6 .9 7.9 1.2 7.3Z', S, { shade: 'M0 9.5L5 9.3L5 12L0 12Z', lines: ['M1 9.4L4 9.3', 'M2 9.5L2.1 11', 'M3 9.4L3.1 11.1', 'M1.8 7.6L1.9 8.8', 'M2.8 7.6L2.9 8.8'] });
};
/** The same hand in a work glove: a little larger, a seam across the knuckles, the thumb again at the front and behind. */
const glove = (a, S) => {
  a.shape('M3.7 7.7C4.4 7.6 5.1 8 5.2 8.8C5.3 9.5 4.9 10.1 4.4 10.1C3.9 10 3.6 9.5 3.5 8.9Z', { ...S, base: S.shade, lite: S.shade, shade: S.deep }, { outer: false, shade: 'M3 9.2L5.6 9.2L5.6 10.6L3 10.6Z' });
  a.shape('M.9 7L4.1 7C4.4 7.8 4.4 8.6 4.3 9.3C4.5 9.8 4.4 10.5 4 10.9C3.3 11.4 2.2 11.4 1.6 11C1 10.7 .7 10 .6 9.4C.5 8.4 .6 7.6 .9 7Z', S, { shade: 'M0 9.6L5.4 9.4L5.4 12L0 12Z', lines: ['M.7 9.5L4.3 9.3', 'M1.9 9.6L2 11.1', 'M3.1 9.5L3.2 11.2'] });
};
const SOLE = '#191716';
/** A boot flat on the floor, the sole at 11, as every shin stands. `top`: where the leg goes in. */
const boot = (a, M, top, lines, toe = 8.6) => {
  a.shape(`M.4 ${top}L5.6 ${top}L5.8 ${top + 1.2}C${toe - 1.6} ${top + 1.4} ${toe - .4} 8.4 ${toe} 9.2C${toe + .2} 9.6 ${toe + .2} 10.2 ${toe} 10.9L-.1 10.9C-.3 9.6 0 8 .4 ${top}Z`, M, { shade: 'M-1 9.3L10 9.3L10 12L-1 12Z', lines });
  a.flat(poly(-.2, 10.1, toe + .15, 10.1, toe + .05, 10.95, -.15, 10.95), SOLE);
};

// ---------- the suit's pieces: drab, the rig's boxes, nothing shining ----------
const BACK_TORSO = 'M3.2 .5C4.4 0 7.8 0 8.9 .5C10.3 1 11 2 11 3.6L10.6 8.4C10.5 10 10.8 11.6 10.6 13.2C10.1 14.6 9 15.1 7.6 15.1L4.4 15.1C3 15.1 1.9 14.6 1.4 13.2C1.2 11.6 1.5 10 1.4 8.4L1 3.6C1 2 1.7 1 3.2 .5Z';
const suitParts = (c) => {
  const M = c.M, P = c.P, mend = c.mend || {};
  return {
    s_torso(a) {
      a.shape(rr(c.ringX ?? 3.6, -1.1, 5, 2.1, .5), P, { shade: .4 });
      a.shape(c.torso || BACK_TORSO, M, { deep: 'M-1 12.4C3 13.4 9 13.4 13 12.2L13 16L-1 16Z', ...folds([2, 6.6, 5.4, 7.6, .9], [10, 5.2, 7, 6.6, .9], [2.2, 11, 5, 11.8, .7], [9.8, 11, 7, 11.8, .7]) });
      a.shape('M1.5 8.6C4 9.2 8 9.2 10.6 8.6L10.6 10C8 10.6 4 10.6 1.5 10Z', P, { outer: false, shade: .4, lines: ['M4 9L4 10.4', 'M8 9L8 10.4'] });
      if (mend.torso) mend.torso(a);
    },
    s_upper(a) {
      a.shape(limb(4, 3.6, 2.5, 2.5, 8.6), M, folds([1, 5.2, 3.6, 6, .8], [.8, 7, 4, 7.6, .7]));
      a.shape('M.4 2.4C.4 .9 1.3 .1 2.5 .1C3.7 .1 4.6 .9 4.6 2.4C4.6 3.2 4.4 3.8 4 4.1C3 4.5 2 4.5 1 4.1C.6 3.8 .4 3.2 .4 2.4Z', P, { outer: false, shade: .5 });
      if (mend.upper) mend.upper(a);
    },
    s_fore(a) {
      a.shape(limb(3.6, 3.2, 2.5, 2.9, 7), M, folds([1, 4.4, 3.8, 5.2, .7]));
      glove(a, c.G || P);
      a.shape('M.6 5.9L4.4 5.9L4.5 7.3C3.4 7.7 1.6 7.7 .5 7.3Z', P, { outer: false, shade: .35 });
      if (mend.fore) mend.fore(a);
    },
    s_thigh(a) {
      a.shape(limb(5, 4, 3, 2.5, 11.3), M, { deep: 'M-1 11.4L7 11.4L7 15L-1 15Z', ...folds([1.2, 3.8, 4.6, 3.2, .7], [1, 9.2, 4.8, 10, .9]) });
      if (mend.thigh) mend.thigh(a);
    },
    s_shin(a) {
      a.shape(limb(4.2, 3.8, 3, 2.18, 6.6), M, folds([1.2, 3.4, 4.8, 3.8, .6]));
      boot(a, c.B || P, c.bootTop ?? 6.3, ['M4 6.8L4.5 9', 'M5.2 6.9L5.6 9'], c.toe);
      a.shape('M1 -.2C2 -.7 4 -.7 5 -.2C5.4 .6 5.4 2.2 4.9 3C3.9 3.4 2.1 3.4 1.1 3C.6 2.2 .6 .6 1 -.2Z', P, { outer: false, shade: .45 });
      if (mend.shin) mend.shin(a);
    },
  };
};
const neck = (a, sk, x0, x1) => a.shape(`M${x0 + .1} -2.6L${x1 - .1} -2.6L${x1 + .2} 1L${x0 - .2} 1Z`, sk, { outer: false, shade: `M${x0 - 1} -1.4L${x1 + 1} -1.4L${x1 + 1} 1.2L${x0 - 1} 1.2Z`, lines: [`M${x0 + .9} -2.2L${x0 + 1.1} .6`] });
const glassRim = (a, d, F) => a.shape(d, F, { outer: false, shade: .3 });

// ---------- Каторжник: faded rust issue coverall, a stencilled number ----------
const ROBE = mat('#96603f', '#6c4231', '#4a2e28', '#ab7a58');
const ROBE2 = mat('#845238', '#5e3a2c', '#422a25', '#976a4c');
const K_TORSO = 'M3.6 .5C4.6 .1 7.6 .1 8.6 .5C9.8 .9 10.4 1.8 10.4 3.2L10 8C9.9 9.6 10.1 11.4 10 13C9.6 14.4 8.6 15 7.4 15L4.6 15C3.4 15 2.4 14.4 2 13C1.9 11.4 2.1 9.6 2 8L1.6 3.2C1.6 1.8 2.2 .9 3.6 .5Z';
const stencil = (a, x, y) => {
  a.flat(rr(x, y, 2.5, 1.9, .1), '#a87c5c');
  a.line(`M${x + .3} ${y + .3}L${x + .3} ${y + 1}L${x + .85} ${y + 1}M${x + .85} ${y + .3}L${x + .85} ${y + 1.6}`, { w: 1.7, c: '#2a1a18' });
  a.line(`M${x + 1.3} ${y + .45}L${x + 1.55} ${y + .3}L${x + 1.55} ${y + 1.6}`, { w: 1.7, c: '#2a1a18' });
  a.line(`M${x + 2} ${y + .3}L${x + 2.3} ${y + .3}L${x + 2.05} ${y + 1.6}`, { w: 1.7, c: '#2a1a18' });
};
const katorzhnik = {
    b_torso(a) {
      neck(a, SK, 5, 7.6);
      a.shape(K_TORSO, ROBE, { deep: 'M0 12.2C3 13.2 9 13.2 12 12L12 16L0 16Z', shade: ['M.8 3L2.6 3.6L3 7.6L1 8Z', ...folds([2.4, 6.6, 6, 7.6, .9], [9.6, 8.4, 6.6, 9.6, .9], [2.4, 10.6, 5.2, 11.4, .7]).shade], lines: ['M7.2 .8L7.2 11.4', 'M2 9.4C4.6 9.9 7.6 9.9 10 9.4', crease(2.4, 6.6, 6, 7.6), crease(9.6, 8.4, 6.6, 9.6)] });
      a.shape('M4.4 .1L6.2 1.5L7.8 .1L8.6 .5L7.6 2.4L6.2 1.9L4.8 2.4L3.6 .5Z', ROBE2, { outer: false, shade: .3 });
      stencil(a, 7.6, 3);
      a.flat('M3 12.4L5 12.2L5.2 13.6L3.4 13.8Z', ROBE.deep);
    },
    b_upper(a) { a.shape(limb(3.6, 3.2, 2.5, 2.5, 8.6), ROBE, folds([.9, 3.2, 3.8, 3.8, .6], [1, 6.8, 4, 7.4, .8])); },
    b_fore(a) {
      a.shape(limb(2.5, 2.2, 2.5, 5.5, 7.4), SK, { shade: .4 });
      hand(a, SK);
      a.shape(limb(3.2, 2.9, 2.5, 2.9, 6.2), ROBE, folds([1, 3.8, 3.8, 4.5, .7]));
      a.shape('M.9 5.8L4.1 5.8L4.2 7L.8 7Z', ROBE2, { outer: false, shade: .3 });
    },
    b_thigh(a) { a.shape(limb(4.4, 3.4, 3, 2.5, 11.3), ROBE, { deep: 'M-1 11.6L7 11.6L7 15L-1 15Z', ...folds([1.2, 3.8, 4.6, 3.2, .7], [1.2, 9.2, 4.6, 10, .9], [4.4, 6, 2.2, 6.8, .6]) }); },
    b_shin(a) {
      a.shape(limb(3.7, 3.4, 3, 2.18, 6.8), ROBE, folds([1.2, 3.2, 4.8, 3.8, .7]));
      boot(a, mat('#4d4a44', '#34322e', '#242220', '#5f5c54'), 6.5, ['M1 7.8L5.6 7.8', 'M4 6.8L4.4 7.8']);
    },
  };

// ---------- Списанный: filthy grey-olive overall, sleeves torn off, rags on the hands, a brand on the bare arm ----------
const OLIVE = mat('#6b6b53', '#4a4b3c', '#33342c', '#80806a');
const RAG = mat('#8a8270', '#60594c', '#443f38', '#a09884');
const S_TORSO = 'M3.8 .5L4.2 .9L5 .3C6.2 .1 7.4 .1 8.3 .4L8.8 .9L9.3 .7C9.9 1.1 10.2 1.9 10.2 3L9.8 8C9.7 9.6 9.9 11.4 9.8 13C9.5 14.3 8.5 15 7.4 15L4.6 15C3.5 15 2.5 14.3 2.2 13C2.1 11.4 2.3 9.6 2.2 8L1.8 3C1.8 2 2.2 1.2 2.9 .9Z';
const rags = (a, y0, y1) => {
  const d = `M.9 ${y0}L4.1 ${y0 - .2}L3.9 ${y1 - .2}L1.1 ${y1}Z`;
  let lines = '';
  for (let y = y0 + .5; y < y1; y += .9) lines += `M.9 ${y + .4}L4 ${y - .2}`;
  a.shape(d, RAG, { shade: `M0 ${y1 - .9}L5 ${y1 - 1}L5 ${y1 + 1}L0 ${y1 + 1}Z`, lines: [lines] });
};
const spisanny = {
    b_torso(a) {
      neck(a, SK, 5, 7.6);
      a.shape(S_TORSO, OLIVE, { deep: 'M0 12.4C3 13.2 9 13.2 12 12.2L12 16L0 16Z', shade: ['M1 2.6L2.8 3.2L3.2 7.4L1 7.8Z', ...folds([2.6, 6.4, 5.8, 7.4, .9], [9.4, 7.4, 6.6, 8.6, .9]).shade], lines: ['M7.4 .9L7.4 8.4', crease(2.6, 6.4, 5.8, 7.4), crease(9.4, 7.4, 6.6, 8.6)] });
      // Dirt where hands wipe and where it sat.
      a.flat('M7.6 5.4C8.4 5 9.4 5.2 9.6 6C9.6 6.8 8.8 7.2 8 7C7.6 6.6 7.4 6 7.6 5.4Z', OLIVE.shade);
      a.flat('M3.2 12L5.4 11.8L5.8 13.4L3.6 13.8Z', '#2b2c25');
      // The rope it is tied with, frayed at the knot.
      a.tube('M2.2 9.2C4.6 9.8 7.6 9.8 9.8 9.2', .45, RAG, { spec: false });
      a.shape(ell(8.4, 9.7, .5, .45), RAG, { outer: false });
      a.line('M8.4 10L8.2 11.6M8.7 10L8.9 11.3', { w: 1.4, c: RAG.base });
    },
    b_upper(a) {
      a.shape(limb(2.9, 2.6, 2.5, 2.8, 8.6), SK, { shade: 'M0 8L5 8L5 11L0 11Z', lines: ['M1.6 3.2L1.8 7.8'] });
      // The brand, an old burn: a bar in a ring.
      a.flat(ell(2.7, 6, 1, .95), SK.deep);
      a.flat(ell(2.7, 6, .55, .5), SK.base);
      a.flat(poly(2.45, 5.2, 2.95, 5.2, 2.95, 6.8, 2.45, 6.8), '#7e4c40');
      // The sleeve's stump, torn off.
      a.shape('M.3 2.4C.3 .9 1.3 .1 2.5 .1C3.7 .1 4.7 .9 4.7 2.4L4.6 3.9L4 3.3L3.5 4.3L2.8 3.5L2.2 4.5L1.6 3.6L1 4.2L.4 3.6Z', OLIVE, { shade: 'M0 3L5 3L5 5L0 5Z' });
      a.line('M1.6 3.6L1.5 5M3.5 4.3L3.6 5.2', { w: 1.2, c: RAG.shade });
    },
    b_fore(a) {
      a.shape(limb(2.7, 2.4, 2.5, 2.9, 7.2), SK, { shade: .4 });
      rags(a, 4.4, 7.6);
      hand(a, SK);
      // The wrap carried over the back of the hand, the fingers out of it.
      a.shape('M.9 7.2L4.1 7.2L4.1 9L.9 9.3Z', RAG, { outer: false, shade: 'M0 8.5L5 8.4L5 10L0 10Z', lines: ['M.9 8.2L4 7.5', 'M.9 9L4 8.3'] });
      a.line('M1 4.6L-.2 5.6M-.2 5.6L.2 6.4', { w: 1.6, c: RAG.shade });
    },
    b_thigh(a) {
      a.shape(limb(4.2, 3.2, 3, 2.5, 11.3), OLIVE, { deep: 'M-1 11.6L7 11.6L7 15L-1 15Z', ...folds([1.2, 3.8, 4.6, 3.2, .7], [1.2, 9.2, 4.6, 10, .9]) });
      a.flat('M1.6 5.2C2.4 4.8 3.8 5 4 5.8C4 6.8 3 7.2 2.2 7C1.6 6.6 1.4 5.8 1.6 5.2Z', OLIVE.shade);
      a.flat('M3.2 8.4L4.2 8.2L4 9L3.3 9.1Z', '#25261f');
      a.line('M3.2 8.4L4.1 9', { w: 1 });
    },
    b_shin(a) {
      a.shape(limb(3.4, 3.2, 3, 2.18, 5.4), OLIVE, folds([1.2, 3, 4.8, 3.6, .7]));
      a.shape(limb(3.5, 3.5, 3, 5.4, 6.8), RAG, { lines: ['M1.2 6.2L4.8 5.2', 'M1.2 7.2L4.8 6.2'] });
      boot(a, mat('#514537', '#382f27', '#26201c', '#63563f'), 6.6, ['M1 7.4L5.6 8.4', 'M1 8.4L6 9.4', 'M5.4 8.6L7.4 9.8', 'M6.6 9.4L7.9 9.8'], 8.2);
    },
  };

// ---------- Трюмный: a heavy waxed apron and rubber gauntlets over a coverall ----------
const COVER = mat('#57574f', '#3d3d39', '#2a2a28', '#6c6c62');
const WAX = mat('#3b2f29', '#261e1b', '#171211', '#55463b');
const RUBBER = mat('#2c2a28', '#1b1a1a', '#111112', '#42403c');
const T_TORSO = 'M3 1.4C3.8 .5 6.2 .1 7.6 .2C9 .3 10 1 10.3 2.4L10.4 8C10.4 9.6 10.2 11.4 10 13C9.6 14.4 8.6 15 7.4 15L4.2 15C3 15 2 14.2 1.8 13C1.8 11.4 1.8 9.6 1.6 8L.9 4.6C.7 3.4 1.6 2.2 3 1.4Z';
const T_APRON = 'M6.6 1.8L10 1.6L10.9 5L11.4 10L11.6 15L5.4 15L5.4 10L5.8 5Z';
const apronMarks = (a) => {
  a.flat('M7 7.2C7.8 6.8 9 7 9.4 7.8C9.4 8.6 8.6 9 7.8 8.8C7.2 8.6 6.9 7.9 7 7.2Z', WAX.deep);
  a.flat('M8.4 11.6L10.2 11.4L10.4 12.6L8.8 12.8Z', '#4b3a2c');
  a.line('M6.6 4.6L10.5 4.2', { w: 1.1 });
};
const tryumny = {
    b_torso(a) {
      neck(a, SK, 5, 7.6);
      a.shape(T_TORSO, COVER, { deep: 'M0 12.2C3 13.2 9 13.2 12 12L12 16L0 16Z', shade: ['M.4 3.6L2.6 3L3.2 8L1 8.4Z'], lines: ['M2.8 2.4C4 1.6 5 1.4 6 1.4', 'M1.8 9C3 9.4 4.4 9.5 5.6 9.4'] });
      // The hump of the back, in one fold under the collar.
      a.shape('M3 1.4C4 .5 5.4 .2 6.6 .4L6 1.6C5 1.5 3.8 2 3 2.6Z', COVER, { outer: false, shade: .3 });
      a.tube('M6.8 2L5.4 .4', .4, WAX, { spec: false });
      a.shape(T_APRON, WAX, { deep: 'M4 12.6L12 12.4L12 16L4 16Z', shade: 'M5 5L7 4.6L6.6 15L5 15Z', lines: ['M6.6 2.2L10 2'] });
      apronMarks(a);
      a.line('M2 8.8L5.6 9', { w: 1.6, c: WAX.deep });
    },
    b_upper(a) { a.shape(limb(3.9, 3.5, 2.5, 2.5, 8.6), COVER, folds([.9, 3.4, 3.9, 4, .7], [.9, 6.8, 4.1, 7.4, .8])); },
    b_fore(a) {
      // A rubber gauntlet to the elbow, its cuff standing off the arm.
      glove(a, RUBBER);
      a.shape('M.8 4.4L4.2 4.4L4 7.4L1 7.4Z', RUBBER, { shade: 'M3 4L5 4L5 8L3.4 8Z', lines: ['M1.4 6L3.8 6'] });
      a.shape('M.1 2.4L4.9 2.4L4.5 4.8C3.4 5.2 1.6 5.2 .5 4.8Z', RUBBER, { deep: 'M0 4.2L5 4.2L5 5.4L0 5.4Z', lines: ['M.4 3.2L4.6 3.2'] });
    },
    b_thigh(a) {
      a.shape(limb(4.6, 3.6, 3, 2.5, 11.3), COVER, { deep: 'M-1 11.6L7 11.6L7 15L-1 15Z', ...folds([1.2, 9.2, 4.6, 10, .9]) });
      a.shape('M2.6 1L5.5 1L5.7 9.8L2.9 10Z', WAX, { outer: false, shade: 'M2 8.2L6 8L6 11L2 11Z', lines: ['M3.2 5L5.6 4.8'] });
      a.flat('M3.4 6.2L4.8 6L5 7.2L3.6 7.4Z', '#4b3a2c');
    },
    b_shin(a) {
      a.shape(limb(3.8, 3.6, 3, 2.18, 6.4), COVER, folds([1.2, 3, 4.8, 3.6, .7]));
      boot(a, RUBBER, 6, ['M.4 6.8L5.6 6.8', 'M1 8.6L4.8 8.4'], 9);
    },
  };

// ---------- Выцветший: the yellow gone to dull mustard and grime, patched. Its cut is every hero's suit ----------
const MUST = mat('#a38749', '#745c38', '#503f2d', '#b89e66');
const MUST2 = mat('#8f7640', '#655132', '#463729', '#a48b58');
const CLOTH = mat('#4a4438', '#322e27', '#23201c', '#5c5548');
const GLASS_SEEN = 0.62;
const GLASS = 'M6.6 4.4C8.4 3.9 10.8 4 12.2 4.7C12.5 6 12.4 7.6 12 8.6C10.4 9.1 8.2 9.1 6.9 8.4C6.3 7.2 6.2 5.6 6.6 4.4Z';
const V_TORSO = 'M3.2 .5C4.4 0 7.6 0 8.8 .5C10.2 1 10.8 2.1 10.8 3.6L10.4 8.2C10.3 9.8 10.6 11.4 10.4 13C10 14.4 9 15 7.6 15L4.4 15C3 15 2 14.4 1.6 13C1.4 11.4 1.7 9.8 1.6 8.2L1.2 3.6C1.2 2.1 1.8 1 3.2 .5Z';
const faded = ((M, M2, Sk, Pad) => ({
  ...suitParts({ M: M2, P: Pad, torso: V_TORSO, mend: {
    torso(a) { patch(a, 7.8, 2.6, 2.3, 2.2, M); taped(a, 2.4, 5.8, 3, '#2f2c28'); a.flat('M3 12.2L5.4 12L5.6 13.4L3.4 13.6Z', M.deep); },
    upper(a) { patch(a, 1, 4.8, 2.8, 1.8, M); },
    thigh(a) { taped(a, 1, 6.4, 4, '#2f2c28'); a.line('M3.6 3.6L4.4 4.6M4.4 3.6L3.6 4.6', { w: 1.2 }); },
    shin(a) { a.line('M1.6 4L4.4 4.2', { w: 1.2 }); },
  } }),
  s_pack(a) {
    a.shape('M2 3L5.6 3L5.6 3.8L2 3.8Z', Pad, { outer: false });
    a.shape('M2 8.6L5.6 8.6L5.6 9.4L2 9.4Z', Pad, { outer: false });
    a.shape(rr(1.4, -1, 2.2, 1.5, .3), Pad, { shade: .3 });
    a.shape('M.7 1.2C.7 .4 1.3 0 2 0L2.9 0C3.7 0 4.3 .4 4.3 1.2L4.3 10.8C4.3 11.6 3.7 12 2.9 12L2 12C1.3 12 .7 11.6 .7 10.8Z', mat('#6e6c64', '#4b4a44', '#35342f', '#807e74'), { deep: 'M0 10L5 10L5 13L0 13Z', lines: ['M.7 2.6C1.8 3 3.2 3 4.3 2.6', 'M.7 9.5C1.8 9.9 3.2 9.9 4.3 9.5'] });
    a.flat('M1.2 5L2 4.8L2.8 5.6L1.8 6.2Z', '#4a4842');
    taped(a, .7, 6.4, 3.6, '#2f2c28');
  },
  s_head(a) {
    a.shape('M7 1C10 .9 12.3 2.8 12.6 5.8C12.8 8.2 12.2 10.4 10.8 11.4C9.4 12.2 5.2 12.2 3.8 11.5C2.2 10.6 1.4 8.4 1.5 6C1.7 3 3.8 1.1 7 1Z', M2, { shade: 'M1 8.8L14 8.8L14 13L1 13Z', deep: 'M1 10.6L14 10.6L14 13L1 13Z', lines: ['M5.4 1.2C4 3.6 3.8 7.6 4.6 11.8', 'M1.8 8.4C2.4 8.9 3.2 9.2 4.3 9.3', 'M8 2.4L9.8 3', 'M9.6 1.8L10.2 2.8'] });
    a.flat(poly(1.8, 5, 4, 4.7, 4.2, 6, 1.9, 6.3), '#2f2c28');
    a.line('M2 5.2L2.2 6.2M3.8 4.9L4 5.9', { w: .9 });
    a.line('M2.1 7L3.9 6.8M2.2 7.7L4 7.5', { w: 1.1, c: '#26231f' });
    glassRim(a, 'M6 3.7C8.2 3.1 11 3.2 12.7 4.1C13 5.8 12.9 7.8 12.4 9.1C10.6 9.8 8 9.8 6.4 8.9C5.6 7.4 5.5 5.2 6 3.7Z', Pad);
    a.shape(GLASS, GL, { outer: false, lite: 'M6.6 4.4C8.4 3.9 10.8 4 12.2 4.7L12.3 5.2C10.6 4.6 8.4 4.5 6.5 5Z' });
  },
  b_torso(a) {
    neck(a, Sk, 5, 7.6);
    a.shape(V_TORSO, M, { deep: 'M0 12.2C3 13.2 9 13.2 12 12L12 16L0 16Z', shade: ['M.6 3L2.4 3.6L2.8 7.8L.8 8.2Z', ...folds([2.4, 6.4, 5.8, 7.2, .8], [9.8, 6.8, 7.2, 7.9, .8]).shade], lines: ['M7.2 .8L7.2 8.4', crease(2.4, 6.4, 5.8, 7.2), crease(9.8, 6.8, 7.2, 7.9)] });
    a.line('M8 3.2L10.1 3.2L10 5.4L8.2 5.4Z', { w: 1.2 });
    patch(a, 2.8, 2.6, 1.8, 1.8, M2);
    a.flat('M3.4 12.2L5.6 12L5.8 13.4L3.6 13.6Z', M.deep);
    a.shape('M1.6 8.6C4 9.1 8 9.1 10.4 8.6L10.4 9.8C8 10.3 4 10.3 1.6 9.8Z', CLOTH, { outer: false, shade: .3 });
    a.flat(rr(7.2, 8.6, 1.5, 1.4, .1), '#3d3a34');
    a.shape('M4.4 .1L6.2 1.5L7.8 .1L8.6 .5L7.6 2.4L6.2 1.9L4.8 2.4L3.6 .5Z', M2, { outer: false, shade: .3 });
  },
  b_upper(a) { a.shape(limb(3.8, 3.4, 2.5, 2.5, 8.6), M, folds([.9, 3.2, 3.9, 3.8, .6], [.9, 6.8, 4.1, 7.4, .8])); },
  b_fore(a) {
    a.shape(limb(2.7, 2.4, 2.5, 3.8, 7.4), Sk, { shade: .35 });
    hand(a, Sk);
    a.shape(limb(3.5, 3.2, 2.5, 2.9, 4), M, folds([1, 3.4, 3.8, 3.9, .5]));
    a.shape('M.8 4L4.2 4L4.3 5.4C3.4 5.8 1.6 5.8 .7 5.4Z', M2, { outer: false, shade: .4, lines: ['M1 4.5L4 4.5'] });
  },
  b_thigh(a) {
    a.shape(limb(4.4, 3.4, 3, 2.5, 11.3), M, { deep: 'M-1 11.6L7 11.6L7 15L-1 15Z', ...folds([1.2, 3.8, 4.6, 3.2, .7], [1.2, 9.2, 4.6, 10, .9]) });
    patch(a, 2, 5.4, 2.2, 2.2, M2);
  },
  b_shin(a) {
    a.shape(limb(3.7, 3.4, 3, 2.18, 6.8), M, folds([1.2, 3.2, 4.8, 3.8, .7]));
    boot(a, mat('#57453a', '#3b2f28', '#29211d', '#6a5647'), 6.4, ['M4 6.9L4.5 8.8', 'M5 7L5.5 8.8', 'M1 9.2L4 9.2']);
  },
}))(MUST, MUST2, SK, CLOTH);

// The clothes a look picks among, in Look.Outfit's order; their limbs differ a little, in the same boxes on the same joints.
const OUTFITS = [katorzhnik, spisanny, tryumny, faded];

// ---------- the tracker on the near shin: a strap with its box; its light a picture of its own, tinted and unlit ----------
const PLASTIC = { base: '#1d1e21', shade: '#121316', deep: '#0b0c0e', lite: '#2e2f33' };
const trackerStrap = { w: 9, h: 11, jx: 3, jy: 2.18, draw(a) {
  a.shape('M.8 4L5.2 3.9L5.2 6L.8 6.1Z', PLASTIC, { shade: 'M0 5.6L6 5.5L6 7L0 7Z' });
  a.shape(rr(3.8, 3.6, 2.6, 2.8, .4), PLASTIC, { lines: ['M4 3.8L6.2 3.8'] });
} };

// ---------- what the hero wears ----------
const CANVAS = { base: '#9a7350', shade: '#6d4e39', deep: '#4b3429', lite: '#b8926a', spec: '#d4bb94', rim: '#b8926a' };
const LINING = { base: '#4d7570', shade: '#365753', deep: '#26403f', lite: '#739c94', spec: '#a4c4b8', rim: '#739c94' };
const FUR = { base: '#c6b99c', shade: '#8f8269', deep: '#655a4b', lite: '#e0d6bf', spec: '#f0e9d6', rim: '#e0d6bf' };
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
// The cape's cut: its hem is frayed, the same on both sides, for the two are one sheet.
const HEM = 'M-1 -1L15 -1L15 29.1L13.3 29.7L12.8 29.1L11 29.8L9.2 29.5L8.8 28.8L8.2 29.6L6 29.9L4.2 29.5L3.6 29L2.8 29.8L1 29.5L-1 29.9Z';
const capeOutside = (a) => {
  a.shape(HEM, CANVAS, {
    // The front panel takes the light from the yoke down; the skirt falls away into the dark by the hem.
    lite: ['M3 -1L14 -1L14 2.4C10 3 6.6 3 5.4 2.6L5.4 12L3 12.4Z'],
    shade: ['M5.7 9C6.2 14 5.6 22 6.2 30L7.3 30C7.1 22 7.5 14 6.7 9Z', 'M9.9 6.5C10.5 12 9.8 22 10.4 30L11.5 30C11.3 22 11.7 12 10.8 6.5Z', 'M8 18C8.3 22 7.9 26 8.2 30L9 30C8.9 26 9.2 22 8.8 18Z', band(13.2, -1, 15, 31)],
    deep: ['M-1 28.2L6 27.9L9 28.3L15 27.9L15 31L-1 31Z'],
    lines: ['M6.2 9C6.6 14 6 22 6.6 29', 'M10.4 6.5C11 12 10.2 22 10.8 28.8', 'M3.2 2.8C7 3.3 10 3.2 13.6 2.6', 'M8.4 18C8.7 22 8.3 25 8.5 28'] });
  // The turned-back front edge shows its lining, a seam between.
  a.shape('M-1 -1L2.4 -1C2.6 9 2.2 20 2.5 29.4L1 29.5L-1 29.9Z', LINING, { outer: false, lite: band(-1, -1, 1.2, 12), shade: band(1.5, -1, 3, 30), deep: band(-1, 28.3, 3, 31) });
  a.line('M2.7 -1C2.9 9 2.5 20 2.8 29.4', { w: 2.4 });
  // A patch let in long ago, its stitches showing; a tear at the hem never mended.
  a.shape('M7.6 14.2L11.4 13.8L11.7 17.4L7.9 17.8Z', { ...CANVAS, base: '#83614a', lite: '#9c7a5c', shade: '#5f4434' }, { outer: false, lite: 'M7.6 14.2L11.4 13.8L11.4 14.5L7.7 14.9Z' });
  a.line('M7.8 14.1L8.1 14.5M8.9 14L9.1 14.4M10 13.9L10.2 14.3M11.3 14.4L11 14.6M11.5 15.8L11.2 15.9M11.5 17.1L11.2 17.1M9.8 17.5L9.9 17.8M8.2 17.6L8.3 17.9', { w: .9 });
  a.cut('M12.2 29.2L12.6 26.8L13 29.2Z');
  a.line('M12.5 26.8C12.4 26.2 12.4 25.8 12.6 25.4', { w: .9 });
  a.flat('M4.2 22.4L4.9 22.2L5 22.9L4.3 23Z', CANVAS.shade);
};
const capeLining = (a) => {
  a.shape(HEM, LINING, {
    lite: ['M3 -1L13 -1L13 2.6C10 3 8 3 6 2.8L6 11.6L3.4 12L3 2.8L-1 2.8L-1 -1Z'],
    shade: ['M6.2 11C6.6 16 6 24 6.5 30L7.5 30C7.3 24 7.8 16 7.1 11Z', 'M10.2 7C10.8 13 10.1 23 10.7 30L11.7 30C11.5 23 12 13 11.1 7Z', 'M8.1 20C8.4 24 8 27 8.3 30L9 30C8.9 27 9.3 24 8.9 20Z', band(-1, 3, .4, 31), band(13, -1, 15, 31)],
    deep: ['M-1 28.2L6 27.9L9 28.3L15 27.9L15 31L-1 31Z'],
    lines: ['M6.7 11C7.1 16 6.5 24 7 29', 'M10.7 7C11.3 13 10.6 23 11.2 28.8', 'M3 2.9C7 3.2 10 3.1 13 2.7'] });
  a.cut('M12.2 29.2L12.6 26.8L13 29.2Z');
  a.line('M3 12.2C4 12.1 5 12 6 11.8', { w: .9, c: '#26403f' });
};
const capeHood = (a) => {
  const hood = 'M.5 1.4C2 .4 4 .1 5.4 .2C7 .3 8.6 .7 9.5 1.5C9.8 3.6 9.6 5.4 8.8 6.8C8 8 6.6 9.2 5 9.7C3.6 9.2 2 8 1.3 6.8C.5 5.4 .2 3.4 .5 1.4Z';
  a.shape(hood, CANVAS, { lite: 'M4 .2C6 .1 8 .6 9.3 1.5L9.2 4C7.4 3 5.6 2.6 4.4 2.8Z', shade: ['M-1 3L2 3.6C2.4 5.6 3.4 7.4 5 9L5 11L-1 11Z'], deep: 'M0 8L10 7.4L10 11L0 11Z', rim: .26,
    lines: ['M2.8 1C2.4 3.6 3 6 4.6 8', 'M6.6 3.2C6.8 5 6.4 6.8 5.4 8.4', 'M.8 2C2.6 1.5 4.4 1.4 6.2 1.5'] });
  a.line('M7.2 7.4L7.5 7.9M8 6.6L8.4 7', { w: .9 });
};
const capeCollar = (a) => {
  // Rough pelt: a body laid along the neck, then each lock in by hand, longest toward the back.
  a.shape('M.4 3.2L1.2 2.2L1.4 1L2.8 1.2L3.4 .3L4.8 .9L5.8 .2L7 .9L8.2 .3L9.2 1L10.2 1.1L11.2 1.8L12.4 2.2L13.8 3L14.8 3.8L13.6 4.2L12.6 5L10.8 4.8L9.6 5.6L8 5.2L6.6 5.8L5 5.2L3.4 5.6L2.2 4.8L.9 4.8L1.2 4Z', FUR, { shade: 'M0 4L15 4L15 6L0 6Z', rim: .2 });
  // Tufts lying toward the back, each pointed, no line round them: the pen goes only along the strands.
  for (const d of ['M3.4 1.2L5 1.6L4.2 2.2L5 3L3.2 2.8L1.8 3.8L2.2 2.4Z', 'M6.6 .9L8 1.4L7.4 2L8.2 2.8L6.4 2.6L5.2 3.4L5.4 2Z', 'M9.6 1.5L11.2 2.2L10.4 2.6L11.2 3.4L9.4 3.1L8.4 3.8L8.6 2.6Z', 'M5 3.2L6.4 3.6L5.8 4.1L6.4 4.8L4.8 4.6L3.6 5.2L3.9 4Z', 'M8.2 3.3L9.6 3.8L9 4.2L9.6 4.9L7.9 4.7L6.8 5.3L7.1 4.2Z'])
    a.flat(d, FUR.lite);
  a.line('M4.6 1.7C3.8 2 3 2.6 2.4 3.4M4.4 2.6C3.8 2.8 3.2 3.2 2.8 3.6M7.6 1.4C6.8 1.8 6.2 2.2 5.6 3M7.6 2.6C7 2.7 6.4 3 6 3.3', { w: 1, c: '#655a4b' });
  a.line('M10.8 2.3C10 2.6 9.4 3 8.8 3.6M10.8 3.3C10.2 3.3 9.6 3.4 9.2 3.7M6 3.7C5.2 4 4.6 4.4 4 5M9.2 3.9C8.4 4.2 7.8 4.6 7.2 5.1', { w: 1, c: '#655a4b' });
  a.line('M1.6 4.4L2 4.7M3.2 4.6L3.4 5M5 4.6L5.2 5M7.8 4.7L8 5.1M9.6 4.4L9.8 4.9M11.2 4.2L11.3 4.6M12.6 3.6L13 3.9', { w: .9, c: '#655a4b' });
};
// The jetpack: a blue fuel bottle in a dark frame, two straps, a valve on top and a nozzle below (Figure.NozzleAt).
const BOTTLE = { base: '#2f6fa4', shade: '#1f4a78', deep: '#173a60', lite: '#5a9cca', spec: '#a4d0ea', rim: '#5a9cca' };
const jetpack = (a) => {
  a.shape(rr(2, .2, 2, 1.6, .3), SH, { lite: band(2.2, .3, 3.8, .8), shade: band(2, 1.2, 4, 1.8) });
  a.shape(rr(1.6, 12.2, 2.8, .9, .2), G, { outer: false });
  a.shape('M2 13L4 13L4.5 13.9L1.5 13.9Z', G, { lite: band(2, 13, 4, 13.3) });
  a.shape(rr(.3, 1.5, 5.4, 11, .9), G, { lite: band(.8, 1.6, 5.2, 2.3), shade: band(.3, 11.6, 5.7, 12.5), rim: .22 });
  a.shape(rr(1.2, 2.5, 3.6, 9, .5), BOTTLE, { shade: [band(1, 2, 1.8, 12), band(4, 2, 5, 12)], lite: band(1.8, 2.5, 3, 11), spec: band(2.2, 3, 2.6, 4.6), deep: band(1, 10.2, 5, 12), outer: false });
  for (const y of [4.8, 8.7]) a.shape(rr(0, y, 6, 1.1, .2), PK, { lite: band(0, y, 6, y + .4), shade: band(0, y + .75, 6, y + 1.1) });
  a.flat(ell(1, 5.35, .22), G.deep); a.flat(ell(5, 9.25, .22), G.deep);
  a.flat('M3.6 6.6L4.3 6.4L4.4 7.4L3.9 7.8Z', BOTTLE.shade);
};
// The torch clamped to the brow; its end is the lens the figure draws again unlit (HeadLampArt.Lens).
const headLamp = (a) => {
  a.shape(rr(1.6, 2.8, 2.4, 1.1, .2), G, { shade: band(1.6, 3.4, 4, 3.9) });
  a.shape(rr(.1, .5, 5, 2.6, .5), PK, { lite: band(.4, .6, 4.8, 1.2), spec: band(1, .75, 3.6, 1), shade: band(.1, 2.3, 5.1, 3.1) });
  a.shape(rr(.1, .4, 1, 2.8, .3), G, { outer: false });
  a.line('M3.8 .6L3.8 3', { w: .9 });
  a.shape(rr(4.8, .4, 1.1, 2.8, .2), G, { outer: false });
  a.glow(rr(5, .9, .8, 2, .1), '#f0be5a');
};
// The planner, seen near edge on: a screen along its top, the slab of its case below, a lit stud.
const sheet = (a) => {
  a.shape(rr(.2, .2, 11.6, 4.6, .5), G, { lite: band(.4, .2, 11.6, .6), rim: .2 });
  a.flat(rr(.8, .4, 10.4, 1.8, .15), '#0e1428');
  a.shape(rr(.4, 2.2, 11.2, 2.2, .3), PK, { lite: band(.6, 2.2, 11.4, 2.7), shade: band(.4, 3.6, 11.6, 4.4), outer: false, lines: ['M.8 3.6L11.2 3.6'] });
  a.glow(rr(9.8, 2.5, 1, .8, .1), '#f0a838');
  a.flat(ell(1.2, 3, .22), G.deep); a.flat('M4.6 4.1L6.6 4.1L6.4 4.6L4.8 4.6Z', '#262a44');
};
// The screen alone, drawn unlit over the planner: columns of data, hand placed, brightest where it was last touched.
const sheetScreen = (a) => {
  a.flat(rr(.9, .45, 10.2, 1.7, .12), '#14607e');
  for (const [x, w, y, h, c] of [[1.2, 1, .7, 1.2, '#2ca4d8'], [2.5, 1.4, .7, .6, '#7cd4f4'], [2.6, .9, 1.4, .5, '#2ca4d8'], [4.2, .8, .7, 1.2, '#7cd4f4'], [5.3, 1.3, 1.1, .8, '#2ca4d8'], [5.4, .7, .6, .4, '#d8f0ff'], [6.9, 1, .7, .7, '#7cd4f4'], [7.1, .7, 1.5, .4, '#2ca4d8'], [8.3, 1.2, .9, 1, '#2ca4d8'], [9.8, 1, .7, .6, '#7cd4f4'], [10, .7, 1.4, .5, '#2ca4d8']])
    a.flat(band(x, y, x + w, y + h), c);
};

// ---------- the tools in the hand: the axe's and the drill's kit (zfittings), lit from above as the rest of the hero ----------
const WOOD = { base: '#7c5a40', shade: '#54392e', deep: '#3c2a24', lite: '#a07a56', spec: '#c6a47c' };
const ROPE = { base: '#bda06c', shade: '#826644', deep: '#5c4a34', lite: '#d6bd8c', spec: '#ecdcb4' };
const BRASS = { base: '#b08c48', shade: '#7c5e30', deep: '#574126', lite: '#d4b470', spec: '#f0dca4' };
const BLADE = { base: '#b4bcc8', shade: '#6c7486', deep: '#474c68', lite: '#d8dee6', spec: '#f0f2f4' };
const RUST = '#6e4630', SLOT = '#14172c';
// machines.js loads after this file: its kit is read when a picture is drawn.
const STEEL = () => window.AnimeKit.STEEL, GUN = () => window.AnimeKit.GUN, DARK = () => window.AnimeKit.DARK, COPPER = () => window.AnimeKit.COPPER;
const stud = (a, x, y, r = .42) => a.shape(ell(x, y, r), BLADE, { outer: false, edge: false, shade: band(x - r, y + .05, x + r, y + r), lines: [`M${x - r * .7} ${y + r * .3}L${x + r * .7} ${y - r * .3}`] });
// The plank stock the harpoon and the nailer share: a pistol's grip sawn from one board, a turn of tape, a steel foot.
const stock = (a) => {
  a.shape('M.5 3.3C.5 2.5 1.1 2 2 2L7.8 2L7.8 5.5L6.4 5.7C6.1 7 6.2 8.6 6.4 9.9L2.3 9.9C2.6 8.6 2.8 7 2.6 5.7L1.1 5.6C.7 5 .5 4.1 .5 3.3Z', WOOD, {
    lite: 'M1.2 2.1L7.6 2.1L7.6 2.9L1 3Z', shade: ['M.4 4.6L2.8 4.6L2.8 6L.4 6Z', 'M5.2 5.8L6.6 5.8L6.6 10L5.2 10Z'], lines: ['M1.6 3.6L6 3.5', 'M3.2 4.5L7.4 4.4', 'M3.6 6.2C3.5 7.4 3.5 8.2 3.6 9.6', 'M4.6 6.4C4.7 7.4 4.6 8.4 4.7 9.2'] });
  for (const y of [6.3, 7.1, 7.9]) a.line(`M2.8 ${y}L6.2 ${y + .2}`, { w: 1.1, c: '#3c2a24' });
  a.shape('M2.2 9.7L6.5 9.7L6.6 10.8L2.1 10.8Z', GUN(), { lite: band(2.2, 9.7, 6.5, 10.1), shade: band(2.1, 10.4, 6.6, 10.8) });
  stud(a, 6.9, 3.6);
};
const harpoon = (loaded) => (a) => {
  stock(a);
  // The tube: a frame with the spring seen through a long window, the barrel's slot before it.
  a.shape(rr(7, 1.2, 18.4, 4.8, .4), GUN(), { lite: band(7.2, 1.3, 25.2, 2), shade: band(7, 5.2, 25.4, 6), rim: .22 });
  a.flat(rr(8, 2.2, 9.4, 2.7, .2), SLOT);
  for (const x of [8.6, 9.8, 11, 12.2, 13.4, 14.6, 15.8]) { a.flat(poly(x, 2.2, x + .75, 2.2, x + .35, 4.9, x - .4, 4.9), '#a7afbc'); a.flat(poly(x, 2.2, x + .3, 2.2, x - .1, 4.9, x - .4, 4.9), '#d4dae2'); }
  a.shape(rr(17.6, 1.7, 7, 3.6, .3), STEEL(), { outer: false, lite: band(17.8, 1.8, 24.4, 2.3), spec: band(19, 1.9, 22, 2.15), shade: band(17.6, 4.6, 24.6, 5.3), lines: ['M17.7 1.8L17.7 5.2'] });
  a.flat(band(17.8, 3.05, 25, 3.95), SLOT);
  a.shape(rr(24.5, 1.3, 1.2, 4.4, .3), GUN(), { lite: band(24.6, 1.4, 25.6, 2), shade: band(24.5, 4.8, 25.7, 5.7) });
  a.flat(band(24.5, 3.05, 25.7, 3.95), SLOT);
  stud(a, 8, 5.5); stud(a, 17.1, 1.8);
  a.flat('M16.8 5.3L18.2 5.2L18.4 5.9L17.8 6L17 5.9Z', RUST);
  // The spool of line slung under it, on a stalk: wound by hand, the turns lying crooked.
  a.shape(rr(19.6, 5.7, 1.6, 1.8, .2), GUN(), { shade: band(19.6, 6.8, 21.2, 7.5) });
  a.shape(rr(17.2, 7.2, 6.4, 3, 1.1), ROPE, { lite: band(17.6, 7.3, 23.2, 8), shade: band(17.2, 9.3, 23.6, 10.2), lines: ['M18.2 7.3L18.8 10.1', 'M19.2 7.3L19.6 10.2', 'M20.2 7.2L20.8 10.2', 'M21.3 7.3L21.5 10.1', 'M22.3 7.3L22.8 10'] });
  a.shape(rr(16.6, 7.5, 1, 2.4, .25), DARK(), { lite: band(16.7, 7.6, 17.5, 8) });
  a.shape(rr(23.2, 7.5, 1, 2.4, .25), DARK(), { lite: band(23.3, 7.6, 24.1, 8) });
  if (!loaded) return;
  // Ready: the rod seated in the slot, its head out past the muzzle.
  a.flat(band(17.8, 3.15, 26, 3.85), '#c4ccd8');
  a.flat(band(17.8, 3.15, 26, 3.38), '#efe9da');
  a.shape('M25.8 2.6L27.2 2.6L26.8 1.7L29.8 3.5L26.8 5.3L27.2 4.4L25.8 4.4Z', BLADE, { lite: 'M25.8 2.6L27.2 2.6L26.8 1.7L28.8 2.9L25.8 3.1Z', spec: band(26.4, 2.8, 28, 3.1), shade: 'M25.8 3.7L29.4 3.5L26.8 5.3L27.2 4.4L25.8 4.4Z' });
};
// The rod in flight: the eye the line ties to, tied with copper; a long shaft; barbs (RodLook).
const rod = (a) => {
  a.shape(rr(2.6, 2, 19, 1, .2), STEEL(), { lite: band(2.8, 2, 21.4, 2.4), shade: band(2.6, 2.65, 21.6, 3) });
  a.shape(ell(2.4, 2.5, 1.7, 1.6), COPPER(), { lite: 'M1.2 1.5C1.8 1 3 1 3.6 1.5L3.2 2.2L1.6 2.2Z', shade: 'M.6 3L4.2 3L4.2 4.2L.6 4.2Z' });
  a.flat(ell(2.3, 2.5, .6, .55), SLOT);
  a.line('M4.2 1.7L4.4 3.3M4.8 1.9L5 3.1', { w: 1 });
  a.shape('M19.8 1.6L21.4 1.6L21 .7L23.8 2.5L21 4.3L21.4 3.4L19.8 3.4Z', BLADE, { lite: 'M19.8 1.6L21.4 1.6L21 .7L23 1.9L19.8 2.1Z', spec: band(20.4, 1.8, 22, 2.1), shade: 'M19.8 2.7L23.4 2.5L21 4.3L21.4 3.4L19.8 3.4Z' });
};
const nailer = (loaded) => (a) => {
  stock(a);
  // Under the pipe, a spring in a brass sleeve, and the tin the nails come from, its label peeling.
  a.shape(rr(9.2, 5.6, 6.2, 3.6, .3), BLADE, { lite: band(9.5, 5.7, 15.2, 6.2), shade: band(9.2, 8.3, 15.4, 9.2), lines: ['M9.3 8.3L15.3 8.3'] });
  a.flat('M10 6.2L11.1 6.2L11 8.2L10.3 8.4L10 7.8Z', '#a03a2c');
  a.line('M13.3 6.4C13.7 6.9 13.7 7.5 13.4 8', { w: .9 });
  a.shape(rr(7.2, 4.3, 10.2, 1.6, .5), BRASS, { lite: band(7.6, 4.35, 17, 4.8), shade: band(7.2, 5.3, 17.4, 5.9) });
  for (const x of [8.2, 9.3, 10.4, 11.5, 12.6, 13.7, 14.8, 15.9]) a.line(`M${x} 4.4L${x - .3} 5.8`, { w: .9 });
  // The pipe, drilled along its top for the gas and bound twice round with copper wire.
  a.shape(rr(7, 1, 18.2, 3.4, .5), GUN(), { lite: band(7.3, 1.1, 24.9, 1.7), shade: band(7, 3.7, 25.2, 4.4), rim: .22 });
  a.shape(rr(21.4, 1.2, 3.8, 3, .3), STEEL(), { outer: false, lite: band(21.6, 1.3, 25, 1.8), spec: band(22, 1.45, 24, 1.65), shade: band(21.4, 3.5, 25.2, 4.2) });
  for (const x of [9.6, 11, 12.2, 15.6, 17, 18.4, 19.8]) a.flat(rr(x, 2, .7, .9, .15), SLOT);
  for (const x of [7.3, 13.4]) { a.shape(rr(x, .8, 1.6, 3.8, .2), COPPER(), { lite: band(x, .9, x + 1.6, 1.4), shade: band(x, 3.8, x + 1.6, 4.6), outer: false }); a.line(`M${x + .5} .9L${x + .3} 4.5M${x + 1.1} .9L${x + .9} 4.5`, { w: .8 }); }
  a.flat(band(24.6, 2.1, 25.2, 2.9), SLOT);
  a.flat('M18.6 3.7L19.8 3.6L20 4.3L19 4.4Z', RUST);
  if (!loaded) return;
  a.flat(band(23.4, 2.15, 24.9, 2.85), '#c4ccd8');
  a.flat('M24.9 2.15L25.9 2.5L24.9 2.85Z', '#efe9da');
};
// The railgun: a squat housing with its red band, three capacitor cans in copper straps on it, the arc shard let into its back, two short heavy rails clamped twice.
const RAILRED = { base: '#a63c2e', shade: '#742a28', deep: '#541e20', lite: '#c8604a', spec: '#e8a890' };
const CANS = [[5.7, .15], [8.7, 0], [11.7, -.15]];
const SHARD = 'M2.5 3.1L3.6 3.3L3.8 4.4L3 5L2.3 4.3Z';
const railgun = (a) => {
  // Under it: the frame behind the grip, the grip, a foot before it.
  a.shape('M1.2 9L5.4 9L5.4 10L3.2 10L3.2 12.2L5.4 12.2L5.4 13.2L1.2 13.2Z', GUN(), { lite: band(1.3, 9.1, 3, 13), shade: band(1.2, 12.5, 5.4, 13.2) });
  a.shape('M5.4 12.2L10.8 12.2L10.8 13.2L5.4 13.2Z', GUN(), { shade: band(5.4, 12.8, 10.8, 13.2) });
  a.shape('M10.4 9L14 9L14.2 13.6L10.8 13.6Z', DARK(), { lite: band(10.6, 9.1, 11.6, 13.4), lines: ['M10.8 10.4L14 10.4', 'M10.8 11.4L14.1 11.4', 'M10.8 12.4L14.2 12.4'] });
  a.shape(rr(20.2, 8.8, 2, 4.4, .3), GUN(), { lite: band(20.4, 8.9, 21, 13), shade: band(20.2, 12.3, 22.2, 13.2) });
  a.shape('M14.6 9L15.8 9L15.8 10.4L15 11L14.6 10.6Z', RAILRED);
  // The rails first, the housing's end over them: the upper, the gap the nail runs in, the lower, the clamps.
  a.shape(rr(16, 3.9, 22.8, 2.2, .3), STEEL(), { lite: band(16.2, 4, 38.6, 4.6), spec: band(33, 4.15, 38.2, 4.45), shade: band(16, 5.4, 38.8, 6.1) });
  a.shape(rr(16, 7, 22.8, 2, .3), GUN(), { lite: band(16.2, 7.1, 38.6, 7.6), shade: band(16, 8.4, 38.8, 9) });
  a.flat(band(16, 6.1, 38.6, 7), SLOT);
  for (const x of [22.2, 29.4]) { a.shape(rr(x, 3.4, 1.5, 6.1, .25), DARK(), { lite: band(x + .1, 3.5, x + 1.4, 4.1), shade: band(x, 8.8, x + 1.5, 9.5) }); stud(a, x + .75, 4.5, .36); stud(a, x + .75, 8.4, .36); }
  a.flat('M36.4 5.6L37.6 5.5L37.8 6L36.6 6.1Z', RUST);
  a.shape(rr(1, 3.8, 16, 5.6, .6), GUN(), { lite: band(1.4, 3.9, 16.6, 4.7), spec: band(5, 4.05, 14, 4.35), shade: band(1, 8.6, 17, 9.4), rim: .22, lines: ['M16.2 3.9L16.2 9.3'] });
  a.shape(rr(4.2, 5.9, 11.8, 2.1, .2), RAILRED, { outer: false, lite: band(4.4, 5.9, 15.8, 6.4), shade: band(4.2, 7.4, 16, 8) });
  a.flat('M9.6 7.1L11.4 7L11.6 8L9.8 8Z', RAILRED.deep);
  a.flat('M13.6 5.9L14.6 5.9L14.4 6.5Z', '#868fa2');
  stud(a, 2, 8.4, .36); stud(a, 15.2, 8.7, .36);
  // The shard in its socket at the rear.
  a.shape(rr(1.6, 2.8, 2.8, 2.6, .3), DARK(), { lite: band(1.8, 2.9, 4.2, 3.3) });
  a.flat(SHARD, '#8878c8');
  a.flat('M2.7 3.3L3.2 3.4L3.1 4L2.7 4Z', '#c8c0ec');
  // The cans, each its own: one standing crooked, one strapped twice, one dented.
  for (const [x, t] of CANS) {
    a.shape(poly(x + t, .3, x + 2.3 + t, .3, x + 2.4, 4, x, 4), GUN(), { lite: band(x, .4, x + 2.6, .9), shade: band(x + 1.7, 0, x + 3, 4.2) });
    a.flat(poly(x + .2 + t * .8, 1.3, x + 2.2 + t * .8, 1.3, x + 2.2 + t * .6, 2.5, x + .2 + t * .6, 2.5), SLOT);
    a.shape(band(x - .05, 2.7, x + 2.45, 3.5), COPPER(), { outer: false, lite: band(x, 2.7, x + 2.4, 3), shade: band(x, 3.2, x + 2.4, 3.5) });
  }
  a.shape(band(8.65, .85, 11.15, 1.25), COPPER(), { outer: false, lite: band(8.7, .85, 11.1, 1) });
  a.line('M13.2 .6C13.6 1 13.6 1.4 13.3 1.7', { w: .9 });
};
// What lights as it charges, alone: each can's window, the shard and the gap between the rails, where the plain picture has them dark.
const railgunCharged = (a) => {
  for (const [x, t] of CANS) { a.flat(poly(x + .2 + t * .8, 1.3, x + 2.2 + t * .8, 1.3, x + 2.2 + t * .6, 2.5, x + .2 + t * .6, 2.5), '#2ca4d8'); a.flat(poly(x + .5 + t * .8, 1.5, x + 1.5 + t * .8, 1.5, x + 1.5 + t * .6, 2.3, x + .5 + t * .6, 2.3), '#a8ecff'); }
  a.flat(SHARD, '#d8f0ff');
  for (const [x0, x1] of [[17, 22.1], [23.8, 29.3], [31, 35.8], [36.6, 38.4]]) { a.flat(band(x0, 6.15, x1, 6.95), '#7cd4f4'); a.flat(band(x0 + .3, 6.35, x1 - .3, 6.75), '#efe9da'); }
};
// The screwdriver: a ribbed handle and a steel shaft; the ribs roll on three steps as the wrist turns, all else stands still.
const HANDLE = { base: '#c48e32', shade: '#855726', deep: '#5e3a20', lite: '#e0b868', spec: '#f4e0a8' };
const driver = (step) => (a) => {
  a.shape(rr(.4, .3, 7.4, 4.4, .9), HANDLE, { lite: band(.8, .4, 7.4, 1.4), spec: band(1.4, .75, 6.4, 1.05), shade: band(.4, 3.3, 7.8, 4.7), rim: .22 });
  for (const x of [1.6, 3.6, 5.6]) { const r = x + step * .66; a.flat(poly(r, .4, r + .7, .4, r + .7, 4.6, r, 4.6), HANDLE.shade); a.line(`M${r + .75} .5L${r + .75} 4.5`, { w: .9 }); }
  a.shape(rr(7.5, 1, 1.4, 3, .25), PK, { lite: band(7.6, 1.05, 8.8, 1.6), shade: band(7.5, 3.3, 8.9, 4) });
  a.shape('M8.8 1.5L15.2 1.5L16.5 2.2L16.5 2.8L15.2 3.5L8.8 3.5Z', BLADE, { lite: band(9, 1.55, 15, 2.1), spec: band(10.2, 1.7, 14, 1.95), shade: band(8.8, 2.9, 16.6, 3.5) });
  a.flat('M11.6 3.05L12.8 3L12.9 3.45L11.7 3.45Z', RUST);
};

const LIGHT = [0, -1], Margin = 3, Px = 4;
const part = (p) => bake({ ...p, margin: Margin, light: LIGHT });
const SUIT = { torso: 's_torso', pack: 's_pack', upperArm: 's_upper', forearm: 's_fore', thigh: 's_thigh', shin: 's_shin' };
const BOXES = { torso: ['b_torso', 12, 15, 6, 0], upperArm: ['b_upper', 5, 9, 2.5, 2.5], forearm: ['b_fore', 5, 10, 2.5, 2.9], thigh: ['b_thigh', 6, 12, 3, 2.5], shin: ['b_shin', 9, 11, 3, 2.18] };
const SKIN_PAINTS = new Set([SK.base, SK.shade, SK.deep, SK.lite, '#7e4c40']);
/** What a part's draw reaches: only its skin, or only what is not, so skin can take a look's tint under cloth that stays as drawn. */
const only = (a, skin) => ({
  shape(d, m, o) { if (!!m.skin === skin) a.shape(d, m, o); },
  flat(d, c, o) { if (SKIN_PAINTS.has(c) === skin) a.flat(d, c, o); },
  line(d, o) { if (!skin) a.line(d, o); },
  tube(d, w, m, o) { if (!skin) a.tube(d, w, m, o); },
});
const suitPart = (id) => ({ w: { s_pack: 4, s_torso: 12, s_upper: 5, s_fore: 5, s_thigh: 6, s_shin: 9, s_head: 13 }[id], h: { s_pack: 12, s_torso: 15, s_upper: 9, s_fore: 10, s_thigh: 12, s_shin: 11, s_head: 12 }[id], draw: faded[id] });

/** The tracker's light, white for Modulate to tint: hard in the middle, a soft ring so it carries far off. On the strap's joint and margin. */
function trackerLight() {
  const c = canvas((9 + 2 * Margin) * Px, (11 + 2 * Margin) * Px), x = c.getContext('2d'), p = new Path2D(poly(.9, 4.3, 6.1, 4.3, 6.1, 5.7, .9, 5.7));
  x.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px);
  x.lineJoin = 'round'; x.strokeStyle = 'rgba(255,255,255,.22)'; x.lineWidth = .9; x.stroke(p);
  x.fillStyle = '#fff'; x.fill(p);
  return c;
}

// ---------- The heavy helmet, as picked (2026-11-03-the-heavy-helmet): a diver's steel bell, a barred port in front, a small one at
// the temple, bolted to a breastplate, its hose from the nape to the pack. In the basic helmet's box and joint, facing +x. ----------
const BST = { base: '#7f8796', shade: '#59606f', deep: '#3d4250', lite: '#a5acb8', spec: '#d6dbe2' };
const BELL_BRASS = { base: '#a5874c', shade: '#6e5636', deep: '#4c3b2a', lite: '#cdb57c', spec: '#efe0b0' };
const BELL_RUBBER = { base: '#2e2c2a', shade: '#1d1c1b', deep: '#121212', lite: '#45423e' };
const BELL_PORT = ell(10.1, 6.2, 2.15, 2.25), BELL_SIDE = ell(5.4, 5.6, 1.15, 1.25);
const BELL_GLASS = BELL_PORT + BELL_SIDE, BELL_BARS = [rr(9.05, 3.7, .6, 5, .2), rr(10.95, 3.75, .6, 4.9, .2)];
const sq = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const fx = (n) => +n.toFixed(2);
const rustAt = (a, x, y, s) => {
  a.flat(poly(x - s, y - .2 * s, x - .3 * s, y - .7 * s, x + .5 * s, y - .5 * s, x + s, y + .1 * s, x + .4 * s, y + .45 * s, x - .5 * s, y + .4 * s), '#6e4630');
  a.flat(poly(x - .4 * s, y - .1 * s, x + .1 * s, y - .3 * s, x + .4 * s, y + .05 * s, x, y + .3 * s), '#3f2a24');
};
/** A corrugated hose on a quadratic curve, its ribs across it. */
const hose = (a, p, w, ribs) => {
  a.tube(`M${p[0]} ${p[1]}Q${p[2]} ${p[3]} ${p[4]} ${p[5]}`, w, BELL_RUBBER, { spec: false });
  const at = (t) => { const s = 1 - t; return [s * s * p[0] + 2 * s * t * p[2] + t * t * p[4], s * s * p[1] + 2 * s * t * p[3] + t * t * p[5]]; };
  let d = '';
  for (let i = 1; i < ribs; i++) {
    const A = at(i / ribs), B = at(i / ribs + .01), dx = B[0] - A[0], dy = B[1] - A[1], l = Math.hypot(dx, dy) || 1, nx = (-dy / l) * w * .42, ny = (dx / l) * w * .42;
    d += `M${fx(A[0] + nx)} ${fx(A[1] + ny)}L${fx(A[0] - nx)} ${fx(A[1] - ny)}`;
  }
  a.line(d, { w: .8 });
};
const port = (a, d, lite) => a.shape(d, GL, { outer: false, lite });
function heavyHelmet(a) {
  hose(a, [-.3, 7.4, -1.9, 11, -.5, 14.6], 1.15, 7);
  a.shape(rr(2.4, -.6, 1.7, 1.3, .3), BELL_BRASS, { shade: .35 });
  a.shape(ell(6.4, 5.4, 6.1, 6), BST, { shade: .9, deep: 'M-1 9.6C2 11 10 11 14 9.6L14 12L-1 12Z', lite: 'M1.6 2.6C3 .3 9.4 -.3 11.3 2.2L10.4 2.6C8.4 1.1 4.4 1.1 2.6 3Z',
    lines: ['M3.6 -.1C2 3 2 8 3.6 11', 'M8.3 -.2C8 .4 8 .8 8.2 1.2', 'M2 6.6L2.6 7'] });
  a.shape(rr(-1, 5.8, 1.8, 2, .4), BELL_BRASS, { shade: .4, lines: ['M-.1 5.9L-.1 7.7'] });
  a.shape(ell(5.4, 5.6, 1.7, 1.8), BELL_BRASS, { shade: .35 });
  port(a, BELL_SIDE, 'M4.4 4.8C5 4.3 5.9 4.3 6.4 4.8L6.3 5.1C5.8 4.8 5 4.8 4.5 5.1Z');
  a.shape(ell(10.1, 6.2, 2.95, 3.05), BELL_BRASS, { shade: .45, lines: ['M7.6 4.6L7.9 4.9', 'M12.6 7.9L12.3 8.1'] });
  port(a, BELL_PORT, 'M8.3 4.9C9.3 4 11 4 11.9 4.9L11.8 5.4C10.9 4.6 9.3 4.6 8.4 5.4Z');
  for (const x of [9.05, 10.95]) a.shape(rr(x, 3.7, .6, 5, .2), BST, { outer: false, shade: .3, lite: sq(x, 3.7, x + .25, 8.7) });
  a.shape(rr(2.2, 9.4, 8.6, 1.3, .3), BELL_BRASS, { shade: .35 });
  a.shape('M-.8 13.8C-.6 11.6 1.4 10.4 3.4 10.2L9.6 10.2C11.6 10.4 13.6 11.6 13.8 13.8L13.6 14.9L-.6 14.9Z', BST,
    { lite: 'M.6 11.4C1.4 10.7 2.4 10.4 3.4 10.3L9.6 10.3C10.6 10.4 11.6 10.7 12.4 11.4Z', deep: sq(-1, 14, 14, 15), lines: ['M6.2 10.4L6.2 14.6'] });
  for (const x of [1, 3.8, 8.6, 11.4]) { a.shape(rr(x - .5, 11.3, 1, .9, .2), BELL_BRASS, { outer: false, shade: .3 }); a.line(`M${x - .7} 11.2L${x - .2} 11.5M${x + .7} 11.2L${x + .2} 11.5`, { w: .8 }); }
  rustAt(a, 11.9, 12.7, .7); rustAt(a, 2.6, 9.2, .5);
  a.line('M4.6 2.4L5.3 2.9M5.3 2.9L5.9 2.7', { w: .9 });
}

Subjects.hero = function () {
  for (const [name, id] of Object.entries(SUIT)) out('hero_' + name, part(suitPart(id)));
  OUTFITS.forEach((clothes, n) => {
    for (const [name, [id, w, h]] of Object.entries(BOXES)) {
      const draw = clothes[id];
      out(`hero_outfit_${n}_${name}`, part({ w, h, draw: (a) => draw(only(a, false)) }));
      let bare = false;
      draw({ shape(d, m) { bare = bare || !!m.skin; }, flat() {}, line() {}, tube() {} });
      if (bare) out(`hero_outfit_${n}_${name}_skin`, part({ w, h, draw: (a) => draw(only(a, true)) }));
    }
  });
  out('hero_head', part(headPic));
  [faceHollow, faceSquint, faceSleepless, faceHooded, faceBurned].forEach((p, i) => out(`hero_face_${i}`, part(p)));
  [hairCrop, hairStubble, hairMatted, hairRag, hairThin, hairTail].forEach((p, i) => out(`hero_hair_${i}`, part(p)));
  // A look's beard 1 wears the first picture, 2 the second; beard 0 hides the sprite.
  [beardJaw, beardFull].forEach((p, i) => out(`hero_beard_${i}`, part(p)));
  out('hero_tracker', part(trackerStrap));
  out('hero_tracker_light', trackerLight());
  // Worn and carried: exactly the pixel picture's size, drawn where it stood; only the sheet is a rig's part, with its margin.
  const plain = (w, h, draw) => bake({ w, h, draw, light: LIGHT });
  out('hero_sheet', part({ w: 12, h: 5, draw: sheet }));
  out('hero_sheet_screen', plain(12, 5, sheetScreen));
  out('hero_jetpack', plain(6, 14, jetpack));
  out('hero_headLamp', plain(6, 4, headLamp));
  out('hero_cape', plain(14, 30, capeOutside));
  out('hero_cape_lining', plain(14, 30, capeLining));
  out('hero_cape_hood', plain(10, 10, capeHood));
  out('hero_cape_collar', plain(15, 6, capeCollar));
  // The tools. A state differs from its plain picture only by what changes: baked alike and, where the sizes differ, cropped.
  const crop = (c, w, h) => { const o = canvas(w * Px, h * Px); o.getContext('2d').drawImage(c, 0, 0); return o; };
  out('hero_harpoon', crop(plain(30, 11, harpoon(false)), 26, 11));
  out('hero_harpoon_loaded', plain(30, 11, harpoon(true)));
  out('hero_harpoon_rod', plain(24, 5, rod));
  out('hero_nailer', plain(26, 11, nailer(false)));
  out('hero_nailer_loaded', plain(26, 11, nailer(true)));
  out('hero_railgun', plain(40, 14, railgun));
  out('hero_railgun_charged', plain(40, 14, railgunCharged));
  const turns = [0, 1, 2].map((t) => plain(17, 5, driver(t)));
  const twist = canvas(51 * Px, 5 * Px);
  turns.forEach((t, i) => twist.getContext('2d').drawImage(t, i * 17 * Px, 0));
  out('hero_screwdriver', twist);
  out('hero_screwdriver_held', turns[0]);
  // The helmet whole, for its card: shrunk a little into the pixel picture's box, which its round shell overhangs.
  out('hero_helmet', bake({ ...suitPart('s_head'), units: 1.12, shift: [.35, .75], light: LIGHT }));
  const [shell, visor] = cut(faded.s_head, GLASS, []);
  out('hero_helmet_shell', shell);
  out('hero_visor', visor);
  const [heavyShell, heavyVisor] = cut(heavyHelmet, BELL_GLASS, BELL_BARS);
  out('hero_heavyHelmet_shell', heavyShell);
  out('hero_heavyHelmet_visor', heavyVisor);
  // Its card: the bell fitted into the 16 px square by the box its hose and breastplate reach (picked with its look).
  const [x0, y0, x1, y1] = [-1.8, -1, 13.8, 14.9], w = x1 - x0, h = y1 - y0, u = Math.max(w, h) / 15.4;
  out('items/heavyHelmet', bake({ w: 16, h: 16, light: [-0.6, -0.8], units: u, shift: [(16 * u - w) / 2 - x0, (16 * u - h) / 2 - y0],
    W: { outer: .9 * u, edge: .26 * u, panel: .14 * u }, draw: heavyHelmet }));
};

/** A helmet's two pictures: the shell with its glass taken out (what crosses the glass, <paramref name="bars"/>, kept), and the glass alone at GLASS_SEEN over the face. */
function cut(draw, glassD, bars) {
  const helmet = part({ w: 13, h: 12, draw });
  const mask = canvas(helmet.width, helmet.height), m = mask.getContext('2d');
  m.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px);
  m.fill(new Path2D(glassD));
  m.globalCompositeOperation = 'destination-out';
  for (const bar of bars) m.fill(new Path2D(bar));
  const shell = canvas(helmet.width, helmet.height), sx = shell.getContext('2d');
  sx.drawImage(helmet, 0, 0);
  sx.globalCompositeOperation = 'destination-out';
  sx.drawImage(mask, 0, 0);
  const visor = canvas(helmet.width, helmet.height), vx = visor.getContext('2d');
  vx.globalAlpha = GLASS_SEEN;
  vx.drawImage(helmet, 0, 0);
  vx.globalAlpha = 1;
  vx.globalCompositeOperation = 'destination-in';
  vx.drawImage(mask, 0, 0);
  return [shell, visor];
}

})();

