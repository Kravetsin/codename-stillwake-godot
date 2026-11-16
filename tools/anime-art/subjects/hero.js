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
/** The hanging hand from outside: the back of it, four fingers curled under and shaded, the thumb a dark sliver at the front edge, behind the fingers. */
const hand = (a, S) => {
  a.shape('M3.5 7.9C4.1 7.8 4.7 8.2 4.8 8.9C4.9 9.5 4.6 10.1 4.1 10.1C3.7 10 3.4 9.5 3.3 8.9Z', { ...S, base: S.shade, lite: S.shade, shade: S.deep }, { outer: false, shade: 'M3 9.3L5.2 9.3L5.2 10.6L3 10.6Z' });
  a.shape('M1.2 7.3L3.9 7.3C4.1 7.9 4.1 8.6 4 9.3C4.2 9.8 4.1 10.4 3.8 10.8C3.2 11.2 2.4 11.2 1.8 10.9C1.3 10.6 1 10 .9 9.4C.8 8.6 .9 7.9 1.2 7.3Z', S, { shade: 'M0 9.5L5 9.3L5 12L0 12Z', lines: ['M1 9.4L4 9.3', 'M1.7 9.5L1.8 10.9', 'M2.5 9.45L2.6 11.1', 'M3.3 9.4L3.35 11', 'M1.8 7.6L1.9 8.8', 'M2.8 7.6L2.9 8.8'] });
};
/** The same hand, five digits, in a work glove: a little larger, a seam across the knuckles, the thumb again at the front and behind. */
const glove = (a, S) => {
  a.shape('M3.7 7.7C4.4 7.6 5.1 8 5.2 8.8C5.3 9.5 4.9 10.1 4.4 10.1C3.9 10 3.6 9.5 3.5 8.9Z', { ...S, base: S.shade, lite: S.shade, shade: S.deep }, { outer: false, shade: 'M3 9.2L5.6 9.2L5.6 10.6L3 10.6Z' });
  a.shape('M.9 7L4.1 7C4.4 7.8 4.4 8.6 4.3 9.3C4.5 9.8 4.4 10.5 4 10.9C3.3 11.4 2.2 11.4 1.6 11C1 10.7 .7 10 .6 9.4C.5 8.4 .6 7.6 .9 7Z', S, { shade: 'M0 9.6L5.4 9.4L5.4 12L0 12Z', lines: ['M.7 9.5L4.3 9.3', 'M1.5 9.6L1.6 11', 'M2.45 9.55L2.55 11.25', 'M3.4 9.45L3.45 11.1'] });
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
const BLADE = { base: '#b4bcc8', shade: '#6c7486', deep: '#474c68', lite: '#d8dee6', spec: '#f0f2f4' };
const RUST = '#6e4630', SLOT = '#14172c';
// machines.js loads after this file: its kit is read when a picture is drawn.
const STEEL = () => window.AnimeKit.STEEL, COPPER = () => window.AnimeKit.COPPER;
// The harpoon, the user's pick of five drawn references («Нынешний, перерисованный»), shipped raw (refs/harpoon.png) with its harpoon
// mended out: a wooden grip, a steel frame with the spring seen through a long window, a canvas-wrapped bottle under it in two
// clamps. The harpoon is drawn here, so the gun shows empty, loaded, and the same harpoon in flight.
/** The harpoon's pictures: the gun 26 × 11, loaded 34 × 11 (the harpoon out past the muzzle); its picture's box fitted along 26. */
const HARPOON_W = 26, HARPOON_H = 11, HARPOON_LOADED_W = 34;
/** Where the picture lies in the harpoon's pictures: its scale and offset, and the reference's pixels mapped into them. */
const harpoonFit = () => {
  const [x0, y0, x1, y1] = TRACED.harpoon.fit.box, s = (HARPOON_W - 1) / (x1 - x0 + 1);
  const at = [.5 - x0 * s, (HARPOON_H - (y1 - y0 + 1) * s) / 2 - y0 * s];
  return { s, at, X: (x) => x * s + at[0], Y: (y) => y * s + at[1] };
};
/** The harpoon's shaft from x0 to its point at tip on the line y, and its head: two tiers of barbs swept back. */
const harpoonRod = (a, x0, tip, y) => {
  const h = 1.25, t = .32;
  a.shape(rr(x0, y - t, tip - 3.6 - x0, 2 * t, .15), STEEL(), { lite: band(x0, y - t, tip - 3.6, y - .08) });
  const half = (sy) => [[tip, y], [tip - 2.1, y + sy * h], [tip - 1.6, y + sy * t], [tip - 2.3, y + sy * t], [tip - 4.1, y + sy * h * .95], [tip - 3.6, y + sy * t], [tip - 3.8, y + sy * t]];
  const top = half(-1), bottom = half(1).reverse(), pts = top.concat(bottom.slice(0, -1));
  a.shape(poly(...pts.flat()), BLADE, { lite: poly(tip, y, tip - 2.1, y - h, tip - 1.6, y - t, tip - 3.8, y - t, tip - 3.8, y), shade: poly(tip, y, tip - 3.8, y, tip - 3.8, y + t, tip - 1.6, y + t, tip - 2.1, y + h) });
};
const harpoon = (loaded) => (a) => {
  const { s, at, X, Y } = harpoonFit();
  if (loaded) harpoonRod(a, X(1100), X(1500), Y(370));
  Traced.raw('harpoon', s, at)(a);
};
// The harpoon in flight (RodLook): the eye the line ties to, tied with copper; the shaft; the head the gun shows loaded.
const rod = (a) => {
  harpoonRod(a, 3.2, 23.6, 2.5);
  a.shape(ell(2.4, 2.5, 1.5, 1.4), COPPER(), { lite: 'M1.2 1.6C1.8 1.1 3 1.1 3.6 1.6L3.2 2.2L1.6 2.2Z', shade: 'M.8 2.9L4 2.9L4 3.9L.8 3.9Z' });
  a.flat(ell(2.3, 2.5, .55, .5), SLOT);
  a.line('M4.2 1.9L4.4 3.1', { w: 1 });
};
// The grappling gun, the user's pick of five drawn references («Моторная лебёдка»), shipped raw (refs/grapple.png) with its grapnel cut
// out: a black winch body with worn yellow edges, a drum of black rope seen through a round window, a crank arm on its hub, a
// green lamp, a rubber grip. The grapnel is cut alone (refs/grapnel.png): ready, it sits at the muzzle; in flight its shank and
// eye are drawn here.
/** The grappling gun's pictures: the gun 26 × 15, loaded 32 × 15 (the grapnel out past the muzzle); its picture's box fitted along 26. */
const GRAPPLE_W = 26, GRAPPLE_H = 15, GRAPPLE_LOADED_W = 32;
const grappleFit = () => {
  const [x0, y0, x1, y1] = TRACED.grapple.fit.box, s = (GRAPPLE_W - 1) / (x1 - x0 + 1);
  const at = [.5 - x0 * s, (GRAPPLE_H - (y1 - y0 + 1) * s) / 2 - y0 * s];
  return { s, at, X: (x) => x * s + at[0], Y: (y) => y * s + at[1] };
};
/** Where refs/grapnel.png was cut from refs/grapple.png: its pixel (0, 0) is the gun's (1243, 240). */
const GRAPNEL_AT = [1243, 240];
const grapnelAt = (a, s, x, y) => Traced.raw('grapnel', s, [x, y])(a);
const grapple = (loaded) => (a) => {
  const { s, at } = grappleFit();
  if (loaded) grapnelAt(a, s, at[0] + GRAPNEL_AT[0] * s, at[1] + GRAPNEL_AT[1] * s);
  Traced.raw('grapple', s, at)(a);
};
// The grapnel in flight (RodLook.Grapple): the eye the line ties to, its shank, the raw crown at the gun's own scale.
/** The grapnel's picture in flight, 10 × 9: the eye at the left, the crown's nose at the right. */
const HOOK_W = 10, HOOK_H = 9, HOOK_SHANK = 4.2;
const hookFit = () => {
  const { s } = grappleFit(), [, y0, , y1] = TRACED.grapnel.fit.box;
  return { s, at: [HOOK_SHANK, (HOOK_H - (y1 - y0 + 1) * s) / 2 - y0 * s] };
};
const grapnel = (a) => {
  const { s, at } = hookFit(), y = at[1] + 210 * s;
  a.shape(rr(2.2, y - .4, HOOK_SHANK - 1.6, .8, .2), STEEL(), { lite: band(2.3, y - .4, HOOK_SHANK, y - .1) });
  a.shape(ell(1.6, y, 1.2, 1.1), STEEL(), { lite: band(.4, y - 1.1, 2.8, y - .5), shade: band(.4, y + .4, 2.8, y + 1.1) });
  a.flat(ell(1.55, y, .45, .42), SLOT);
  grapnelAt(a, s, at[0], at[1]);
};
// The nailer, the user's pick of five drawn references («Клепальщик»), shipped raw (refs/nailer.png) with its rivet mended out: a heavy
// steel riveter, a ribbed collar and a cone nose, a lever trigger, a black rubber grip, a red air bottle clamped under the barrel.
// Ready, the nail's point shows at the nose, drawn here.
/** The nailer's pictures, 26 × 12: its picture's box fitted along 23, the nail out past the nose. */
const NAILER_W = 26, NAILER_H = 12;
const nailerFit = () => {
  const [x0, y0, x1, y1] = TRACED.nailer.fit.box, s = 22.5 / (x1 - x0 + 1);
  const at = [.5 - x0 * s, (NAILER_H - (y1 - y0 + 1) * s) / 2 - y0 * s];
  return { s, at, X: (x) => x * s + at[0], Y: (y) => y * s + at[1] };
};
const nailer = (loaded) => (a) => {
  const { s, at, X, Y } = nailerFit(), y = Y(345);
  if (loaded) {
    const nose = X(1433);
    a.shape(poly(X(1380), y - .4, nose + 1.3, y - .4, nose + 2.4, y, nose + 1.3, y + .4, X(1380), y + .4), BLADE, { lite: band(X(1380), y - .4, nose + 1.6, y - .1) });
  }
  Traced.raw('nailer', s, at)(a);
};
// The railgun, the user's pick of five drawn references («Катушечный из хлама»), shipped raw (refs/railgun.png): a home-made coil gun, a
// steel tube through five copper coils, cables looping to a green battery box at the rear, a rubber pad behind it, a loop handle
// under it held as a pistol grip, an upright fore-grip under the tube.
/** The railgun's pictures, 40 × 15: its picture's box fitted along 40. */
const RAIL_W = 40, RAIL_H = 15;
const railFit = () => {
  const [x0, y0, x1, y1] = TRACED.railgun.fit.box, s = (RAIL_W - 1) / (x1 - x0 + 1);
  const at = [.5 - x0 * s, (RAIL_H - (y1 - y0 + 1) * s) / 2 - y0 * s];
  return { s, at, X: (x) => x * s + at[0], Y: (y) => y * s + at[1] };
};
/** The five coils along the tube, in the reference's pixels: each from x0 to x1. */
const COILS = [[475, 555], [603, 675], [725, 801], [848, 922], [971, 1049]];
const railgun = (a) => {
  const { s, at } = railFit();
  Traced.raw('railgun', s, at)(a);
};
// What lights as it charges, alone: each coil glows cold blue, its windings still seen, a pale core down its middle; the lamp burns.
const railgunCharged = (a) => {
  const { X, Y } = railFit();
  for (const [x0, x1] of COILS) {
    const coil = rr(X(x0) + .1, Y(330), X(x1) - X(x0) - .2, Y(456) - Y(330), .45), w = (x1 - x0) / 5;
    a.flat(coil, '#7cd4f4');
    for (let i = 1; i < 5; i++) a.flat(band(X(x0 + i * w) - .07, Y(330), X(x0 + i * w) + .07, Y(456)), '#3e9ccc', { clip: coil });
    const m = X((x0 + x1) / 2);
    a.flat(rr(m - .3, Y(352), .6, Y(434) - Y(352), .3), '#efe9da');
  }
  a.flat(ell(X(432), Y(477), .55, .7), '#c8a8ff');
  a.flat(ell(X(432), Y(474), .25, .3), '#f4ecff');
};
// The screwdriver, the user's pick of five drawn references («Нынешняя, перерисованная»), shipped raw (refs/screwdriver.png): a
// ribbed amber handle, a steel bolster, a rusted shaft and its flat tip. Fitted along 9 px and drawn 1.4 times as thick, so the
// handle fills the fist as the drawn one did. Twisted, f 0..2, a light streak runs down the handle a third of its height a frame.
/** The screwdriver's pictures, 10 × 3; its handle's box in the reference's pixels; how much thicker than the reference it is drawn. */
const SCREW_W = 10, SCREW_H = 3, SCREW_HANDLE = [70, 412, 700, 632], SCREW_THICK = 1.4, AMBER_LIT = '#f6d48e';
/** Where a raw subject lies in a picture W × H, fitted along W - 1 and drawn k times as thick: its draw and the reference's pixels mapped in. */
const rawThick = (name, W, H, k) => {
  const [x0, y0, x1, y1] = TRACED[name].fit.box, s = (W - 1) / (x1 - x0 + 1), sy = s * k;
  const at = [.5 - x0 * s, (H - (y1 - y0 + 1) * sy) / 2 - y0 * sy], { at: [rx, ry], size: [rw, rh] } = TRACED[name].raw;
  return { X: (x) => x * s + at[0], Y: (y) => y * sy + at[1], draw: (a) => a.image(Traced.picture(name), at[0] + rx * s, at[1] + ry * sy, rw * s, rh * sy) };
};
/** A light streak across a turning part [x0, y0, x1, y1] (picture units), at the f-th third of its height: it reads as the part rolling. */
const streak = (a, [x0, y0, x1, y1], f, c, r) => {
  const h = y1 - y0, y = y0 + h * (f + .5) / 3;
  a.flat(band(x0, y - h * .07, x1, y + h * .07), c, { clip: rr(x0, y0, x1 - x0, h, r) });
};
const driver = (f) => (a) => {
  const { X, Y, draw } = rawThick('screwdriver', SCREW_W, SCREW_H, SCREW_THICK);
  draw(a);
  const [x0, y0, x1, y1] = SCREW_HANDLE;
  streak(a, [X(x0), Y(y0), X(x1), Y(y1)], f, AMBER_LIT, (Y(y1) - Y(y0)) * .45);
};

// The power driver, the user's pick of five drawn references («Нынешний, перерисованный»), shipped raw (refs/powerDriver.png): a short
// pistol-grip driver in worn grey-green enamel, a lamp on its rear end, a ribbed rubber grip, a battery slab under it with three charge
// lights, a knurled chuck and a hex bit. A state is { f: 0..2, the third of a turn the chuck and bit show; mode: 'idle' | 'run' | 'flat' }:
// idle, the lamp dark and the lights steady; running, all bright and a streak on the chuck and bit; flat, turned by the wrist, the first
// light red. In the reference's pixels: the lamp, the lights' middles, the chuck's and the bit's boxes.
const DRIVER_W = 14, DRIVER_H = 11, DRIVER_LAMP = [210, 222, 17], DRIVER_LIGHTS = [[535, 908], [585, 908], [630, 908]];
const DRIVER_CHUCK = [1064, 122, 1208, 338], DRIVER_BIT = [1268, 194, 1420, 258], STEEL_LIT = '#e4e6ea';
// Full saturation only for what works or warns: the running lamp is the game's own (HeldLook.LampColour), the flat one red.
const ON = '#9dff8a', OFF = '#1f2a23', CHARGED = '#4f9a58', WARN = '#ff4a36', WARN_DIM = '#7a2620';
const powerDriver = (st) => (a) => {
  const { s, at, X, Y } = Traced.fit('powerDriver', DRIVER_W, DRIVER_H);
  Traced.raw('powerDriver', s, at)(a);
  if (st.mode !== 'idle') for (const [x0, y0, x1, y1] of [DRIVER_CHUCK, DRIVER_BIT]) streak(a, [X(x0), Y(y0), X(x1), Y(y1)], st.f, STEEL_LIT, .05);
  const [lx, ly, lr] = DRIVER_LAMP, lamp = st.mode === 'run' ? ON : st.mode === 'flat' ? WARN_DIM : OFF;
  a.glow(ell(X(lx), Y(ly), Math.max(lr * s, .34)), lamp, st.mode === 'run' ? { core: ell(X(lx), Y(ly), .14) } : {});
  // Charged: three steady lights; running: bright; flat: dark, the first warning red.
  DRIVER_LIGHTS.forEach(([x, y], i) => a.glow(rr(X(x) - .2, Y(y) - .17, .4, .34, .06), st.mode === 'flat' ? (i === 0 ? WARN : OFF) : st.mode === 'run' ? ON : CHARGED));
};

// The hero's pictures: lit from above, drawn Px pixels a world pixel; a rig's part has Margin round it.
const LIGHT = [0, -1], Margin = 3, Px = 4;
const part = (p) => bake({ ...p, margin: Margin, light: LIGHT });
const BOXES = { torso: ['b_torso', 12, 15, 6, 0], upperArm: ['b_upper', 5, 9, 2.5, 2.5], forearm: ['b_fore', 5, 10, 2.5, 2.9], thigh: ['b_thigh', 6, 12, 3, 2.5], shin: ['b_shin', 9, 11, 3, 2.18] };
const SKIN_PAINTS = new Set([SK.base, SK.shade, SK.deep, SK.lite, '#7e4c40']);
/** What a part's draw reaches: only its skin, or only what is not, so skin can take a look's tint under cloth that stays as drawn. */
const only = (a, skin) => ({
  shape(d, m, o) { if (!!m.skin === skin) a.shape(d, m, o); },
  flat(d, c, o) { if (SKIN_PAINTS.has(c) === skin) a.flat(d, c, o); },
  line(d, o) { if (!skin) a.line(d, o); },
  tube(d, w, m, o) { if (!skin) a.tube(d, w, m, o); },
});

/** The tracker's light, white for Modulate to tint: hard in the middle, a soft ring so it carries far off. On the strap's joint and margin. */
function trackerLight() {
  const c = canvas((9 + 2 * Margin) * Px, (11 + 2 * Margin) * Px), x = c.getContext('2d'), p = new Path2D(poly(.9, 4.3, 6.1, 4.3, 6.1, 5.7, .9, 5.7));
  x.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px);
  x.lineJoin = 'round'; x.strokeStyle = 'rgba(255,255,255,.22)'; x.lineWidth = .9; x.stroke(p);
  x.fillStyle = '#fff'; x.fill(p);
  return c;
}

// ---------- The bell's kit, from the heavy helmet first drawn here (2026-11-03-the-heavy-helmet; now traced, helmets.js): its steel,
// brass and rubber, rust, a corrugated hose and a dark port, for the catchers' gear and the regen suit. ----------
const BST = { base: '#7f8796', shade: '#59606f', deep: '#3d4250', lite: '#a5acb8', spec: '#d6dbe2' };
const BELL_BRASS = { base: '#a5874c', shade: '#6e5636', deep: '#4c3b2a', lite: '#cdb57c', spec: '#efe0b0' };
const BELL_RUBBER = { base: '#2e2c2a', shade: '#1d1c1b', deep: '#121212', lite: '#45423e' };
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

const UNLIT = '#14100e';
const rivet = (a, x, y, r = .3) => { a.flat(ell(x, y, r), '#262a34'); a.flat(ell(x - r * .3, y - r * .35, r * .4), '#8e96a2'); };
const torsoDeep = 'M-1 12.4C3 13.4 9 13.4 13 12.2L13 16L-1 16Z';
/** A rotated rectangle, for a band across a cable. */
const across = (x, y, ang, w, h) => { const c = Math.cos(ang), s = Math.sin(ang); return poly(...[[-w / 2, -h / 2], [w / 2, -h / 2], [w / 2, h / 2], [-w / 2, h / 2]].flatMap(([u, v]) => [x + u * c - v * s, y + u * s + v * c])); };
/** A light drawn straight on a part's canvas, at its margin: a soft halo, then its core. */
function lightPicture(w, h, draw) {
  const c = canvas((w + 2 * Margin) * Px, (h + 2 * Margin) * Px), x = c.getContext('2d');
  x.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px);
  x.lineJoin = 'round'; x.lineCap = 'round';
  const paint = (d, core, halo, fill = true, width = .9) => {
    const p = new Path2D(d);
    x.globalAlpha = .3; x.strokeStyle = halo; x.lineWidth = fill ? .9 : width + .8; x.stroke(p);
    if (fill) { x.fillStyle = halo; x.fill(p); }
    x.globalAlpha = 1;
    if (fill) { x.fillStyle = core; x.fill(p); } else { x.strokeStyle = core; x.lineWidth = width; x.stroke(p); }
  };
  draw(paint);
  return c;
}

Subjects.hero = function () {
  // The basic suit's own parts are painted raw (suits.js); `faded` stays an outfit and the suits' cut.
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
  // The tools. A state differs from its plain picture only by what changes: baked alike and, where the sizes differ, cropped.
  // A held tool's icon (icons/<its picture>): the same drawing at many pixels a world pixel, so an icon fitted from it stays sharp.
  const icon = (name, pic) => out(`icons/${name}`, bake({ light: LIGHT, ...pic, px: Math.ceil(Anime.ICON_LONG / pic.w) }));
  out('hero_harpoon', plain(HARPOON_W, HARPOON_H, harpoon(false)));
  icon('hero_harpoon', { w: HARPOON_W, h: HARPOON_H, draw: harpoon(false) });
  out('hero_harpoon_loaded', plain(HARPOON_LOADED_W, HARPOON_H, harpoon(true)));
  out('hero_harpoon_rod', plain(24, 5, rod));
  out('hero_grapple', plain(GRAPPLE_W, GRAPPLE_H, grapple(false)));
  icon('hero_grapple', { w: GRAPPLE_W, h: GRAPPLE_H, draw: grapple(false) });
  out('hero_grapple_loaded', plain(GRAPPLE_LOADED_W, GRAPPLE_H, grapple(true)));
  out('hero_grapple_hook', plain(HOOK_W, HOOK_H, grapnel));
  out('hero_nailer', plain(NAILER_W, NAILER_H, nailer(false)));
  icon('hero_nailer', { w: NAILER_W, h: NAILER_H, draw: nailer(false) });
  out('hero_nailer_loaded', plain(NAILER_W, NAILER_H, nailer(true)));
  out('hero_railgun', plain(RAIL_W, RAIL_H, railgun));
  icon('hero_railgun', { w: RAIL_W, h: RAIL_H, draw: railgun });
  out('hero_railgun_charged', plain(RAIL_W, RAIL_H, railgunCharged));
  // The builders, raw: the screwdriver's twist and the power driver's run and flat, each a strip of three frames of its picture's size.
  const turns = [0, 1, 2].map((t) => plain(SCREW_W, SCREW_H, driver(t)));
  const twist = canvas(3 * SCREW_W * Px, SCREW_H * Px);
  turns.forEach((t, i) => twist.getContext('2d').drawImage(t, i * SCREW_W * Px, 0));
  out('hero_screwdriver', twist);
  out('hero_screwdriver_held', turns[0]);
  icon('hero_screwdriver_held', { w: SCREW_W, h: SCREW_H, draw: driver(0) });
  const strip = (mode) => {
    const c = canvas(3 * DRIVER_W * Px, DRIVER_H * Px);
    [0, 1, 2].forEach((f) => c.getContext('2d').drawImage(plain(DRIVER_W, DRIVER_H, powerDriver({ f, mode })), f * DRIVER_W * Px, 0));
    return c;
  };
  out('hero_powerDriver_held', plain(DRIVER_W, DRIVER_H, powerDriver({ f: 0, mode: 'idle' })));
  icon('hero_powerDriver_held', { w: DRIVER_W, h: DRIVER_H, draw: powerDriver({ f: 0, mode: 'idle' }) });
  out('hero_powerDriver', strip('run'));
  out('hero_powerDriver_flat', strip('flat'));
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

/** A helmet's picture (cut's, part's) kept only within d, with its outline rim wide, in the box's pixels: how a helmet is cut into the part that turns and its collar (helmets-fit-the-suit). */
function keep(c, d, rim = 0) {
  const mask = canvas(c.width, c.height), m = mask.getContext('2d'), p = new Path2D(d);
  m.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px);
  m.fill(p);
  if (rim) { m.lineWidth = rim; m.lineJoin = 'round'; m.stroke(p); }
  const x = c.getContext('2d');
  x.globalCompositeOperation = 'destination-in';
  x.drawImage(mask, 0, 0);
  x.globalCompositeOperation = 'source-over';
  return c;
}

/**
 * A part's picture c cut to a piece of its drawing (helmets-fit-the-suit): kept within the paths keep, less the paths drop, with
 * the paths back in it again; each path filled on its own, so their windings never cut holes, and stroked rim wide to take its ink.
 */
function within(c, { keep = [], drop = [], back = [] }, rim = 0) {
  const mask = canvas(c.width, c.height), m = mask.getContext('2d');
  m.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px);
  (m.lineWidth = rim), (m.lineJoin = 'round');
  const paint = (ds) => ds.forEach((d) => { const p = new Path2D(d); m.fill(p); if (rim) m.stroke(p); });
  paint(keep);
  m.globalCompositeOperation = 'destination-out';
  paint(drop);
  m.globalCompositeOperation = 'source-over';
  paint(back);
  const x = c.getContext('2d');
  x.globalCompositeOperation = 'destination-in';
  x.drawImage(mask, 0, 0);
  x.globalCompositeOperation = 'source-over';
  return c;
}

// The kit, for a subject drawing more gear on the hero's boxes (catchergear.js).
window.HeroKit = { limb, wedge, folds, mat, glove, boot, BACK_TORSO, GL, band, BST, BELL_BRASS, BELL_RUBBER, rustAt, hose, port, UNLIT, rivet, torsoDeep, part, lightPicture, cut, keep, within };
})();

