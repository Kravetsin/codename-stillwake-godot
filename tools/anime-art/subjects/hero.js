// The hero: round 3's cel parts on the game's joints, the bare head split into the pictures the rig draws (Figure.PictureOf).
(function () {
'use strict';
const { ell, rr, poly, bake, canvas } = Anime;

// Materials: shadows shift hue. The light falls from straight above, so a part looks the same mirrored: a hero's facing never moves it (the world's light does not turn either).
const Y = { base: '#e6a735', shade: '#b85f2c', deep: '#84382c', lite: '#f9d678', spec: '#fff4c6', rim: '#ff8f52', line: '#6e3416' };
const G = { base: '#3f4656', shade: '#262a44', deep: '#191c32', lite: '#67718a', spec: '#c4d2e4', rim: '#5aa6c8', line: '#14172c' };
const PK = { base: '#7c8699', shade: '#474e6c', deep: '#2f3350', lite: '#aab4c6', spec: '#ffffff', rim: '#7ec8e0', line: '#262b46' };
const GL = { base: '#18243a', shade: '#0e1428', deep: '#0a0e20', lite: '#38628c', spec: '#d8f0ff', rim: '#5ad0ff', line: '#0b1020' };
const SK = { base: '#f0c49a', shade: '#d18a6e', deep: '#b0655c', lite: '#fbe0c0', spec: '#ffffff', rim: '#ff9f7a', line: '#7a4030' };
const HR = { base: '#5f3e28', shade: '#46292e', deep: '#331c2c', lite: '#8c6040', spec: '#d8a878', rim: '#a86a40', line: '#20121c' };
const CROWN = 'M1.3 2.3C2.4 .9 4.6 .5 7.8 .6';
const DARK_RING = { base: '#0f111b', shade: '#0f111b', deep: '#0f111b', lite: '#0f111b', spec: '#0f111b', rim: '#0f111b', ink: '#0f111b' };
const SH = { base: '#8f99a8', shade: '#565e7c', deep: '#3a3f5e', lite: '#c4ccd8', spec: '#ffffff', rim: '#7ec8e0', line: '#262b46' };

/** A limb lit from above reads round by a lit middle between two shaded sides. */
const sidesOf = (cx, half) => [poly(cx - half - 4, -2, cx - half, -2, cx - half, 16, cx - half - 4, 16), poly(cx + half, -2, cx + half + 4, -2, cx + half + 4, 16, cx + half, 16)];
const middle = (cx, half, y0, y1) => poly(cx - half, y0, cx + half, y0, cx + half * .8, y1, cx - half * .8, y1);
/** The body of an arm or leg: round at both joints so nothing gaps when it turns. */
const limbPath = (w0, w1, cx, y0, y1) => {
  const a = w0 / 2, b = w1 / 2;
  return `M${cx - a} ${y0}C${cx - a} ${y0 - a * 1.3} ${cx + a} ${y0 - a * 1.3} ${cx + a} ${y0}L${cx + b} ${y1}C${cx + b} ${y1 + b * 1.3} ${cx - b} ${y1 + b * 1.3} ${cx - b} ${y1}Z`;
};

// ---------- the parts, in game pixels, each with its joint where the rig puts it ----------
const FIST = 'M.1 6.8L4.9 6.8C5.5 7.4 5.6 8.6 5.3 9.6C5 10.6 3.8 11.2 2.6 11.2C1.4 11.2 .2 10.6 -.1 9.6C-.4 8.6 -.3 7.4 .1 6.8Z';
const suitedLeg = (a) => {
  a.shape(limbPath(5.8, 4.8, 3, 2.5, 11.3), Y, { shade: sidesOf(3, 1.75), lite: middle(3, .85, 0, 9), spec: middle(3, .3, 1.4, 6.6), rim: .3, lines: ['M1.6 5.4C2.6 5.9 4.2 5.9 5.4 5.4'] });
};
const PARTS = [
  { id: 's_pack', w: 4, h: 12, jx: 0, jy: 0, draw(a) {
    a.tube('M2.2 -.6C2.2 -2.4 3.8 -3 5.6 -2.4', .75, G, { spec: false });
    a.shape(rr(1.1, -1.3, 2.2, 1.7, .3), G, { lite: rr(1.4, -1.2, 1.6, .5, .1) });
    a.shape(rr(1, 11.4, 2.4, 1.4, .3), G, { shade: .3 });
    a.shape('M.2 1.4C.2 .4 1 0 1.8 0L2.6 0C3.5 0 4.2 .4 4.2 1.4L4.2 10.7C4.2 11.7 3.5 12 2.6 12L1.8 12C1 12 .2 11.7 .2 10.7Z', PK, {
      shade: sidesOf(2.2, 1.2), deep: 'M-1 10L5 10L5 13L-1 13Z', lite: middle(2.2, .7, 0, 10), spec: [middle(2.2, .25, 1, 3.6), middle(2.2, .25, 7.2, 8.4)], rim: .25,
      lines: ['M.2 2.6C1.4 3 3 3 4.2 2.6', 'M.2 9.5C1.4 9.9 3 9.9 4.2 9.5'] });
    a.shape(rr(-.1, 4.4, 4.6, 1.8, .3), G, { lite: rr(0, 4.4, 4.4, .5, .1), shade: .3 });
    a.shape(ell(2.2, 5.3, 1.75), DARK_RING, { outer: false });

    a.flat(poly(.5, 7.2, 2.4, 7.2, 2.4, 8.8, .5, 8.8), '#e0a030', { only: 'cel' });
  } },
  { id: 's_torso', w: 12, h: 15, jx: 6, jy: 0, draw(a) {
    a.shape(rr(3, -1, 6.4, 2.4, .9), G, { lite: rr(4.2, -.9, 4, .8, .3), shade: .4 });
    const t = 'M3 .5C4.6 -.1 7.6 -.1 9.2 .5C11 1.1 12 2.4 12 4.2L11.4 8.6C11.4 10 11.8 11.6 11.6 13.2C11 14.6 9.8 15.2 8 15.2L4 15.2C2.2 15.2 1 14.6 .4 13.2C.2 11.6 .6 10 .6 8.6L0 4.2C0 2.4 1 1.1 3 .5Z';
    a.shape(t, Y, {
      shade: ['M-1 0L2.2 0C1.6 3 1.8 6 2.2 8.4C2.6 11 3 13.4 3.4 16L-1 16Z', 'M13 0L9.8 0C10.4 3 10.2 6 9.8 8.4C9.4 11 9 13.4 8.6 16L13 16Z'], deep: 'M-1 11.6C3 13 9 13.2 13 11.4L13 16L-1 16Z', lite: 'M3.2 .3C4.8 -.1 7.4 -.1 8.8 .3L8.4 3.8L3.6 3.8Z',
      spec: 'M4.2 .7L7.8 .7L7.6 1.2L4.4 1.2Z', rim: .28,
      lines: ['M2.8 .8C2.2 3.4 2.4 6 3 8.2', 'M6.6 1C6.8 3.4 6.8 5.8 6.6 8.2', 'M.6 11.3C3.6 12 8.4 12 11.5 11.3'] });
    // The chest pad with a decal, then the belt and its buckle.
    a.shape('M7.4 2.2L10.8 2.8L10.6 6.8L7.4 7.2C7.8 5.6 7.8 3.8 7.4 2.2Z', G, { lite: 'M7.4 2.2L10.8 2.8L10.7 3.7L7.6 3.2Z', shade: 'M7.4 6L10.6 5.8L10.6 6.8L7.4 7.2Z', outer: false });
    a.shape(ell(9.1, 4.7, 1.75), DARK_RING, { outer: false });


    a.shape('M.3 8.3C3.6 9 8.4 9 11.7 8.3L11.8 10.2C8.4 10.9 3.6 10.9 .2 10.2Z', G, { lite: 'M.3 8.3C3.6 9 8.4 9 11.7 8.3L11.8 9C8.4 9.7 3.6 9.7 .2 9Z', outer: false });
    a.shape(rr(8.4, 8.2, 2, 2.4, .3), SH, { spec: rr(8.7, 8.5, .7, .5, .1), shade: .3, outer: false });
    a.line('M3 8.8L3 10.3', { w: 1 });
  } },
  { id: 's_upper', w: 5, h: 9, jx: 2.5, jy: 2.5, draw(a) {
    a.shape(limbPath(4.6, 4.2, 2.5, 2.5, 8.6), Y, { shade: sidesOf(2.5, 1.3), lite: middle(2.5, .7, 1, 7), spec: middle(2.5, .25, 3.4, 6.6), rim: .26, lines: ['M.5 6.4C1.6 6.8 3.4 6.8 4.5 6.4'] });
    a.shape('M-.1 2.4C-.1 .6 1 -.3 2.5 -.3C4 -.3 5.1 .6 5.1 2.4C5.1 3.4 4.8 4.2 4.3 4.5C3.2 5 1.8 5 .7 4.5C.2 4.2 -.1 3.4 -.1 2.4Z', G, {
      lite: 'M.8 .9C1.6 -.1 3.4 -.1 4.2 .9L3.6 1.8L1.4 1.8Z', spec: 'M1.7 .4L3.3 .4L3.1 .8L1.9 .8Z', shade: .5, rim: .2, outer: false });
  } },
  { id: 's_fore', w: 5, h: 10, jx: 2.5, jy: 2.9, draw(a) {
    a.shape(limbPath(4.2, 3.8, 2.5, 2.9, 7), Y, { shade: sidesOf(2.5, 1.2), lite: middle(2.5, .65, .6, 6), spec: middle(2.5, .22, 2.2, 5), rim: .24 });
    a.shape(FIST, G, { lite: 'M.6 6.8L4.4 6.8L4.2 8.2L.8 8.2Z', spec: 'M1.6 7.1L3.4 7.1L3.3 7.6L1.7 7.6Z', shade: 'M-1 10L6 10L6 12L-1 12Z', rim: .24, lines: ['M1.2 9.7L4.4 9.7', 'M2.2 9.8L2.2 11', 'M3.4 9.8L3.4 11'] });
    a.shape('M-.1 6L5.1 6L5.3 7.8C3.6 8.3 1.4 8.3 -.3 7.8Z', G, { lite: 'M0 6L5 6L5.1 6.7L-.1 6.7Z', outer: false });
  } },
  { id: 's_thigh', w: 6, h: 12, jx: 3, jy: 2.5, draw: suitedLeg },
  { id: 's_shin', w: 9, h: 11, jx: 3, jy: 2.18, draw(a) {
    a.shape(limbPath(5.2, 4.8, 3, 2.18, 6.6), Y, { shade: sidesOf(3, 1.55), lite: middle(3, .8, 0, 6), rim: .26 });
    // The boot: a real toe, a heel, a sole flat on the floor.
    a.shape('M.1 6.2L5.9 6.2L6.2 7.3C7.6 7.5 8.8 8 9.2 9.1C9.4 9.6 9.4 10.2 9.2 10.9L-.1 10.9C-.3 9.2 -.2 7.6 .1 6.2Z', G, {
      lite: 'M.4 6.2L5.8 6.2L6 7.3C7.4 7.5 8.4 7.9 8.9 8.5L.3 8.2Z', spec: 'M1.2 6.6L4.8 6.6L4.8 7L1.2 7Z', shade: 'M-1 9.4L10 9.4L10 12L-1 12Z', rim: .24, lines: ['M4 6.3L4.6 8.9', 'M5.4 6.4L6 9'] });
    a.flat(poly(-.2, 10, 9.4, 10, 9.3, 10.95, -.15, 10.95), '#14172c');
    // Knee pad over the joint.
    a.shape('M.8 -.2C1.8 -.8 4.2 -.8 5.2 -.2C5.8 .6 5.8 3 5.1 3.9C4 4.4 2 4.4 .9 3.9C.2 3 .2 .6 .8 -.2Z', G, { lite: 'M1 -.2C2 -.7 4 -.7 5 -.2L4.6 1.4L1.4 1.4Z', spec: 'M2 -.3L4 -.3L3.8 .2L2.2 .2Z', shade: .45, rim: .22, outer: false });
  } },
  { id: 's_head', w: 13, h: 12, jx: 6.5, jy: 12, draw(a) {
    const shell = 'M6.6 -.4C10 -.6 13 1.6 13.5 5.4C13.8 8.2 13 10.6 11.2 11.7C9.6 12.5 4.6 12.6 3 11.8C1 10.8 -.3 8.4 -.2 5.4C0 2.2 2.8 -.2 6.6 -.4Z';
    a.shape(shell, Y, {
      shade: 'M-1 7.6C2 10 4.6 10.6 6.6 10.6C8.6 10.6 11.2 10 14.2 7.6L14.2 13.4L-1 13.4Z', deep: 'M-1 10C3 11.8 10 11.8 14.2 10L14.2 13.4L-1 13.4Z',
      lite: 'M2.4 3.2C3.8 .6 9.4 .6 10.8 3.2C9 2 4.2 2 2.4 3.2Z',
      spec: 'M4.2 1C5.6 .4 7.6 .4 9 1C7.6 1.1 5.6 1.1 4.2 1Z', rim: .32,
      lines: ['M5.2 -.3C3.6 2.4 3.2 7 4.2 12', 'M.4 8.2C1.2 8.8 2.2 9.2 3.5 9.4'] });
    // The vent: three slats low on the back of the helmet.
    a.shape('M.9 5.2L3 4.9L3.2 8.2L1.2 8.2Z', G, { deep: .3, outer: false });
    a.line('M1.1 6L3 5.8M1.2 6.8L3.1 6.6M1.2 7.5L3.1 7.3', { w: 1.1, c: '#5a6276' });
    // The player's mark: its socket here, the light itself a picture of its own over it.
    a.shape(ell(2.8, 3.8, 1.75), DARK_RING, { outer: false });
    a.tube(CROWN, 2.2, DARK_RING, { spec: false });
    // The visor: a dark frame round the glass, a reflection cut across it.
    a.shape('M6 3.4C8.4 2.6 11.6 2.8 13.5 3.8C13.9 5.6 13.8 7.6 13.2 9C11.2 9.8 8 9.8 6.3 8.8C5.5 7.2 5.4 5 6 3.4Z', G, { lite: 'M6.4 3.2C8.6 2.7 11.6 2.9 13.4 3.8L13.5 4.4C11.6 3.6 8.8 3.4 6.4 3.9Z', outer: false });
    const glass = 'M6.8 4.1C8.8 3.5 11.4 3.6 13 4.4C13.3 5.8 13.2 7.4 12.8 8.5C11 9.1 8.4 9.1 7 8.3C6.4 7 6.3 5.4 6.8 4.1Z';
    a.shape(glass, GL, { shade: 'M6 7L14 7L14 10L6 10Z', lite: 'M6.4 3.4L13.4 3.4L13.4 4.9L6.4 4.9Z', rim: .22, outer: false });
    a.glow('M7.6 4.2L12.2 4.2L12 4.8L7.4 4.8Z', '#d8f0ff', { r: .9 });


    // Lamp on the brow.
    a.shape(rr(8.6, 1.3, 2.2, 1.1, .3), G, { outer: false });
    a.glow(rr(9, 1.5, 1.4, .6, .2), '#fff0c0', { r: .9 });
  } },
  { id: 'b_torso', w: 12, h: 15, jx: 6, jy: 0, draw(a) {
    a.shape('M4.6 -1.4L8 -1.4L8.2 1.2L4.4 1.2Z', SK, { shade: 'M3 -2L6 -2L6 .4L3 .4Z', outer: false });
    const t = 'M3 .4C4.2 0 7.8 0 9.2 .5C10.6 1 11.4 2.2 11.4 3.8L11 8.4C11 10 11.4 11.6 11.2 13C10.8 14.4 9.6 15.1 8 15.1L4 15.1C2.4 15.1 1.2 14.4 .8 13C.6 11.6 1 10 1 8.4L.6 3.8C.6 2.2 1.6 .8 3 .4Z';
    a.shape(t, Y, {
      shade: ['M0 0L2.2 0C1.8 3 1.8 6 2.3 8.4C2.8 11 3 13.4 3.4 16L0 16Z', 'M12 0L9.8 0C10.2 3 10.2 6 9.7 8.4C9.2 11 9 13.4 8.6 16L12 16Z'], deep: 'M0 11.8C3 13 9 13 12 11.6L12 16L0 16Z', lite: 'M3.2 .4C4.6 0 7.4 0 8.8 .4L8.4 3.4L3.6 3.4Z',
      rim: .26,
      lines: ['M7.2 .6L7.2 8.2', 'M1 11.4C3.8 12 8.2 12 11 11.4', 'M8 3.4L10.2 3.4L10 5.8L8.2 5.8Z'] });
    a.shape(ell(9.1, 4.6, 1.7), DARK_RING, { outer: false });
    // The open collar.
    a.shape('M4.2 .2L6.2 1.8L7.4 .2L8.6 .6L7.4 3L6.2 2.4L4.8 3.2L3.2 .6Z', Y, { lite: 'M4.2 .2L6.2 1.8L7.4 .2L8.6 .6L8.2 1.4L6.2 2.4L4 1.2Z', outer: false });
    a.shape('M.9 8.4C3.8 9 8.2 9 11.1 8.4L11.2 9.7C8.2 10.3 3.8 10.3 .9 9.7Z', G, { lite: 'M.9 8.4C3.8 9 8.2 9 11.1 8.4L11.1 8.9C8.2 9.5 3.8 9.5 .9 8.9Z', outer: false });
    a.shape(rr(7, 8.3, 1.8, 1.8, .2), SH, { spec: rr(7.3, 8.5, .6, .4, .1), outer: false });
  } },
  { id: 'b_upper', w: 5, h: 9, jx: 2.5, jy: 2.5, draw(a) {
    a.shape(limbPath(4.4, 4, 2.5, 2.3, 8.6), Y, { shade: sidesOf(2.5, 1.25), lite: middle(2.5, .7, 0, 7), spec: middle(2.5, .25, 1, 5), rim: .26, lines: ['M.4 2.4C1.2 1.2 2.6 .6 4 .9'] });
  } },
  { id: 'b_fore', w: 5, h: 10, jx: 2.5, jy: 2.9, draw(a) {
    // Bare forearm and a real hand: fingers curled over, the thumb on top.
    a.shape(limbPath(3.4, 3.4, 2.5, 3.6, 7.4), SK, { shade: sidesOf(2.5, 1), lite: middle(2.5, .55, 2, 7), rim: .2 });
    a.shape('M.5 7.2C1.4 6.8 3.8 6.8 4.6 7.3C5.2 8 5.2 9.4 4.8 10.3C4.2 11 3 11.4 2 11.2C1 11 .4 10.3 .3 9.4C.2 8.6 .2 7.8 .5 7.2Z', SK, {
      shade: 'M-1 9.6L6 9.6L6 12L-1 12Z', deep: 'M-1 10.8L6 10.8L6 12L-1 12Z', lite: 'M1 7L4.2 7L4 8.4L1.2 8.4Z', rim: .2,
      lines: ['M1.8 9.6L1.9 11', 'M2.9 9.7L3 11.2', 'M3.9 9.5L4 10.9'] });
    a.shape('M3.2 8.2C3.8 7.8 5 7.9 5.4 8.5C5.6 9 5.2 9.5 4.6 9.5C4 9.5 3.4 9.1 3.2 8.2Z', SK, { lite: .0001, shade: .25, outer: false });
    // The sleeve rolled to the elbow, with its turned cuff.
    a.shape(limbPath(4.2, 3.9, 2.5, 2.9, 4), Y, { shade: sidesOf(2.5, 1.2), lite: middle(2.5, .65, .6, 4), rim: .22 });
    a.shape('M.2 4L4.8 4L5 5.6C3.6 6.1 1.4 6.1 0 5.6Z', Y, { lite: 'M.2 4L4.8 4L4.9 4.7L.1 4.7Z', outer: false });
  } },
  { id: 'b_thigh', w: 6, h: 12, jx: 3, jy: 2.5, draw: suitedLeg },
  { id: 'b_shin', w: 9, h: 11, jx: 3, jy: 2.18, draw(a) {
    a.shape(limbPath(5.2, 4.8, 3, 2.18, 6.8), Y, { shade: sidesOf(3, 1.55), lite: middle(3, .8, 0, 6), spec: middle(3, .28, 1.2, 4.8), rim: .26, lines: ['M.5 6.6C1.8 7 4.2 7 5.5 6.6'] });
    // A work shoe with a white toe cap and a dark sole.
    a.shape('M.4 6.8L5.6 6.8L6 8C7.6 8.2 8.8 8.8 9.2 9.6C9.4 10 9.4 10.4 9.2 10.9L-.1 10.9C-.3 9.4 0 8 .4 6.8Z', SH, {
      lite: 'M.5 6.8L5.6 6.8L6 8C7.4 8.2 8.4 8.6 8.9 9.1L.3 8.8Z', spec: 'M1.4 7.2L4.8 7.2L4.8 7.6L1.4 7.6Z', shade: 'M-1 9.6L10 9.6L10 12L-1 12Z', rim: .22, lines: ['M4 7.2L4.6 9', 'M5 7.2L5.5 9'] });
    a.flat(poly(-.2, 10.1, 9.4, 10.1, 9.3, 10.95, -.15, 10.95), '#14172c');
  } },
];

// The bare head, as the rig layers it: head (skull and ear), face, then hair over them with a hole where the skull is.
const HAIR = { base: '#7a4f30', shade: '#573439', deep: '#40263a', lite: '#a4744a', spec: '#dcb184', rim: '#b98050', ink: '#1c1018' };
const SKULL = 'M2 5.4C2 2.2 4.6 .8 7.4 .8C10.6 .8 12.9 2.8 13 5.8C13.1 7.4 12.8 8.6 12.1 9.7C11.4 10.8 10.2 12 8.9 12.2C7.4 12.4 5.4 11.4 4.2 10.4C2.8 9.2 2 7.6 2 5.4Z';
const FRINGE = 'M1.2 8C.4 4.6 2 .4 6.4 -.8C9.8 -1.6 13.2 -.4 13.8 3C14 4 13.8 5 13.4 5.6C13 4.6 12.4 3.8 11.6 3.3C11.7 4.2 11.3 5 10.6 5.4C10.6 4.4 10.2 3.6 9.5 3.1C9.2 4.2 8.4 5 7.2 5.4C7.5 4.4 7.3 3.6 6.9 3.1C6.2 4.2 5.2 4.8 4.6 6.2C4.4 7 3.6 7.2 3.2 7C2.8 7.8 2 8.4 1.2 8Z';
const headPic = { w: 13, h: 12, draw(a) {
  a.shape(SKULL, SK, { shade: 'M1 9L14 9L14 13L1 13Z', lite: 'M4 1.8C6 .9 9.4 .9 11 1.8L10.4 3.4L4.6 3.4Z' });
  a.shape(ell(3.7, 7, 1, 1.4), SK, { shade: .3, lines: ['M3.6 6.2C4.1 6.6 4.1 7.4 3.7 7.8'] });
} };
const facePic = { w: 13, h: 12, draw(a) {
  // The near eye: white, a big iris with light low in it, a heavy upper lash, one hard highlight.
  a.flat('M6.8 5.4C7.6 4.4 9.6 4.3 10.2 5.4L10.1 8.2C9.6 9.1 7.8 9.1 7.2 8.2Z', '#fdf8f4');
  a.flat(ell(8.85, 6.9, 1.25, 1.95), '#3e2a24');
  a.flat('M7.75 7.6C8.2 8.8 9.5 8.8 9.95 7.6C9.6 8.1 8.2 8.1 7.75 7.6Z', '#c9894a');
  a.flat(ell(8.85, 7.5, .95, 1), '#7a4c2c');
  a.flat(ell(8.95, 6.6, .62, .95), '#14101a');
  a.flat(ell(8.35, 5.95, .46, .56), '#ffffff');
  a.flat(ell(9.45, 7.9, .2), '#ffffff');
  a.flat('M6.3 5.9C7 4.2 9.8 3.8 10.7 5.3C10.8 5.6 10.6 5.7 10.4 5.5C9.6 4.8 7.8 4.9 7 6C6.8 6.2 6.2 6.2 6.3 5.9Z', '#201420');
  a.line('M7.4 3.6C8.2 3 9.6 3 10.4 3.5', { w: 2.2, c: '#3a2232' });
  // The far eye, foreshortened at the edge of the face.
  a.flat('M11.2 5.5C11.6 4.9 12.3 5 12.5 5.6L12.4 7.8C12.1 8.5 11.5 8.5 11.3 7.9Z', '#fdf8f4');
  a.flat(ell(11.95, 6.8, .62, 1.3), '#3e2a24');
  a.flat(ell(12, 7.3, .46, .7), '#7a4c2c');
  a.flat(ell(11.75, 6, .24, .32), '#ffffff');
  a.flat('M11 5.6C11.4 4.5 12.4 4.5 12.8 5.2C12.9 5.5 12.7 5.5 12.5 5.4C12 5 11.6 5.1 11.3 5.7C11.2 5.9 10.9 5.9 11 5.6Z', '#201420');
  a.line('M11.3 3.8C11.8 3.5 12.4 3.5 12.8 3.8', { w: 1.8, c: '#3a2232' });
  // Nose and mouth, as little as anime needs.
  a.line('M11.2 8.5L11.5 9', { w: 1.4, c: '#b0655c' });
  a.line('M9.8 10.4C10.3 10.7 10.9 10.7 11.3 10.3', { w: 1.8, c: '#7a4030' });
} };
// Hair in the head's coordinates, moved by the two world pixels its picture is larger each way.
const hairPic = { w: 16, h: 18, shift: [2, 2], draw(a) {
  // The back mass is big flat locks now, not a dark lump: lit top, a hard shadow only low behind the neck, strands drawn in lighter ink.
  a.shape('M1 2.6C2.6 -.6 6.6 -1.8 9.6 -1C12.4 -.2 14 2 14 5C14 6.4 13.4 7.4 12.8 8L4 11.2C3.6 12 2.6 12.6 1.6 12.2C1.8 11.6 1.8 11.2 1.6 10.8C.8 11 0 10.6 -.5 9.8C0 9.4 .1 9 0 8.5C-.6 8.2 -1.1 7.7 -1.3 7C-.9 6.7 -.7 6.2 -.8 5.6C-.8 4.6 -.1 3.4 1 2.6Z', HAIR, {
    shade: 'M-2 8.4C0 9 2 9 3.4 8.2L4.4 13L-2 13Z', lite: 'M1.4 2.4C3 -.8 8 -1.6 11 -.8C8.8 -.4 3.6 .2 1.4 2.4Z', rim: .3,
    lines: ['M.2 8.3C.8 6.8 1.2 5.2 1.2 3.4', 'M1.6 10.6C2 8.8 2.4 7 2.2 5'] });
  a.cut(SKULL, { grow: .9 });
  a.cut(ell(3.7, 7, 1, 1.4), { grow: .9 });
  // Fringe: separate locks over the brow, each with its own point, a band of shine across them.
  a.shape(FRINGE, HAIR, {
    shade: 'M0 5.6L14 5.6L14 8.6L0 8.6Z', lite: 'M3.4 -.2C6 -1.2 10 -1.2 12 .2C10 -.2 6 -.2 3.4 .8C3 1 3 0 3.4 -.2Z', rim: .26,
    lines: ['M6.9 3.1C7 2 6.6 1 6 .2', 'M9.5 3.1C9.8 2 9.6 1 9 .2', 'M11.6 3.3C11.8 2.4 11.6 1.4 11.2 .8'] });
  a.flat('M3.2 3.2L4.6 2.2L5.2 2.8L6.4 1.8L7 2.5L8.4 1.7L9 2.3L10.4 1.8L11 2.4L12.2 2.2L12.3 2.8L10.8 3L10.2 2.8L8.8 3.2L8.2 2.8L6.8 3.4L6.2 2.9L5 3.7L4.4 3.2L3.4 4Z', '#c0905c', { clip: FRINGE });
  a.flat('M5.4 2.7L6.2 2.1L6.4 2.5L5.6 3Z', '#ecc89c', { clip: FRINGE });
  a.flat('M8.6 2.2L9.6 2L9.7 2.4L8.8 2.6Z', '#ecc89c', { clip: FRINGE });
} };
// The helmet is drawn whole, then its glass cut out into the visor's own picture, translucent, so the face shows through it as it does in the game.
const GLASS = 'M6.8 4.1C8.8 3.5 11.4 3.6 13 4.4C13.3 5.8 13.2 7.4 12.8 8.5C11 9.1 8.4 9.1 7 8.3C6.4 7 6.3 5.4 6.8 4.1Z';
const GLASS_SEEN = 0.62;

// ---------- the look editor: faces on the same skull, each a person and tired ----------
const WHITE = '#ddd3c2', IRIS = '#2c1e1c', LID = '#201420', BROW = '#3a2232', CREASE = '#b0655c', LIP = '#7a4030';
const farEye = (a, y) => {
  a.flat(`M11.5 ${y}C11.8 ${y - .3} 12.4 ${y - .3} 12.7 ${y}C12.4 ${y + .45} 11.8 ${y + .45} 11.5 ${y}Z`, WHITE);
  a.flat(ell(12.2, y, .3, .32), IRIS);
  a.flat(`M11.3 ${y - .05}C11.7 ${y - .55} 12.5 ${y - .55} 12.9 ${y - .1}C12.5 ${y - .3} 11.8 ${y - .3} 11.3 ${y - .05}Z`, LID);
};
const nose = (a, x = 11.4) => a.line(`M${x} 7.3C${x + .3} 7.9 ${x + .5} 8.4 ${x + .4} 8.8C${x + .1} 9 ${x - .3} 8.9 ${x - .4} 8.7`, { w: 1.4, c: CREASE });
// Lids that have come down: little white, the iris half under a heavy fold, a straight brow, a mouth set in a line.
const faceHooded = { w: 13, h: 12, draw(a) {
  a.flat('M7.3 6.6C7.9 6.1 9.5 6 10.1 6.5C9.6 7.1 8 7.2 7.3 6.6Z', WHITE);
  a.flat(ell(9, 6.45, .56, .52), IRIS);
  a.flat('M7 6.4C7.8 5.6 9.6 5.5 10.4 6.3C9.6 6.05 8 6.05 7.3 6.65Z', LID);
  a.flat('M7.2 5.5C8 5.05 9.6 5.05 10.3 5.55C9.6 5.4 8 5.4 7.2 5.75Z', CREASE);
  a.line('M6.7 4.6C7.8 4.25 9.4 4.25 10.7 4.7', { w: 3, c: BROW });
  farEye(a, 6.4);
  a.line('M11.4 4.8C11.9 4.6 12.4 4.6 12.9 4.8', { w: 2.2, c: BROW });
  nose(a);
  a.line('M9.2 10.3C10 10.45 10.9 10.4 11.8 10.2', { w: 1.8, c: LIP });
  a.line('M8.8 9.1C8.5 9.6 8.5 10.2 8.7 10.7', { w: 1.1, c: CREASE });
} };
// A woman past caring how she looks: thin arched brows, the lashes still dark, old paint worn off the lips, a burn healed pink across the cheek.
const faceBurned = { w: 13, h: 12, draw(a) {
  a.flat('M5.9 7.5C6.6 7.2 7.5 7.4 8 7.9C7.9 8.5 7.3 9 6.5 9C6 8.7 5.7 8 5.9 7.5Z', '#c98a7c');
  a.flat('M6.3 7.8C6.8 7.7 7.2 7.8 7.5 8.1C7.2 8.4 6.8 8.5 6.4 8.4Z', '#b0706a');
  a.flat('M7.3 6.3C7.8 5.5 9.4 5.4 10 6.2C9.6 7 8 7.1 7.3 6.3Z', WHITE);
  a.flat(ell(8.95, 6.15, .62, .68), IRIS);
  a.flat(ell(8.75, 5.92, .14, .14), '#efe9da');
  a.flat('M6.9 6.3C7.5 5.1 9.6 5 10.4 6C9.6 5.6 8 5.7 7.3 6.45L6.7 6.7Z', LID);
  a.line('M6.9 4.5C7.6 3.7 9.2 3.6 10.3 4.3', { w: 1.5, c: BROW });
  farEye(a, 6.1);
  a.line('M11.4 4.3C11.8 4 12.4 4 12.9 4.3', { w: 1.3, c: BROW });
  nose(a);
  a.flat('M10 10.1C10.5 9.9 11 9.95 11.5 10.1C11.1 10.6 10.4 10.65 10 10.1Z', '#8e4a40');
  a.line('M9.8 10.1C10.4 10.2 11 10.2 11.7 10', { w: 1.2, c: LIP });
} };
// Screwing up its eyes at something bright: both slits of dark, one brow hauled up, the teeth set, an old nick through that brow.
const faceSquint = { w: 13, h: 12, draw(a) {
  a.flat('M7.4 6.5C8.2 6.15 9.6 6.15 10.4 6.4C9.7 6.9 8 6.95 7.4 6.5Z', LID);
  a.flat('M9 6.35L9.9 6.35L9.8 6.6L9.1 6.65Z', IRIS);
  a.line('M7 6C7.8 5.5 9.8 5.5 10.7 5.9', { w: 1.2, c: CREASE });
  a.line('M7.2 7.2C8 7.5 9.2 7.5 10 7.2', { w: 1, c: CREASE });
  a.line('M7.2 3.3C8 2.6 9.6 2.6 10.5 3.3', { w: 2.6, c: BROW });
  a.flat('M8.6 2.5L9 2.5L8.7 3.6L8.3 3.6Z', '#e0ae88');
  a.flat('M11.5 6.3C11.9 6.1 12.4 6.1 12.8 6.25C12.4 6.6 11.8 6.6 11.5 6.3Z', LID);
  a.line('M11.4 4.7C11.9 4.4 12.4 4.5 12.9 4.8', { w: 2, c: BROW });
  nose(a);
  a.flat('M9.8 9.8L11.8 9.7C11.8 10.3 11.5 10.7 11 10.7C10.4 10.7 9.9 10.4 9.8 9.8Z', '#3a1c1c');
  a.flat('M10 9.85L11.6 9.75L11.5 10.2L10.1 10.25Z', '#d8ccb8');
  a.line('M10.5 9.85L10.5 10.2M11 9.8L11 10.2', { w: .8, c: '#6a5a50' });
  a.line('M9.3 9.2C9.2 9.7 9.3 10.1 9.5 10.4', { w: 1.1, c: CREASE });
} };
// No sleep in days: eyes wide and flat, a dark bag under each, brows sliding down at the outside, the mouth's corner dropped.
const faceSleepless = { w: 13, h: 12, draw(a) {
  a.flat('M7 7.1C7.6 7.8 9.2 8 10.2 7.3C10.4 8.1 9.7 8.7 8.6 8.7C7.7 8.7 7.1 8 7 7.1Z', '#c0846c');
  a.flat('M7.6 7.6C8.2 8 9.1 8.1 9.8 7.8C9.4 8.3 8 8.3 7.6 7.6Z', '#a56a5a');
  a.flat('M7.5 6.5C7.9 5.9 9.3 5.85 9.8 6.45C9.5 7 8 7.1 7.5 6.5Z', WHITE);
  a.flat(ell(8.85, 6.42, .42, .46), IRIS);
  a.line('M7.3 6.4C7.9 5.6 9.5 5.5 10 6.3', { w: 1.5, c: LID });
  a.line('M7.3 4.2C8.2 4 9.4 4.2 10.5 4.9', { w: 2.2, c: BROW });
  farEye(a, 6.3);
  a.flat('M11.4 7.1C11.8 7.5 12.4 7.5 12.8 7.1C12.7 7.8 11.6 7.8 11.4 7.1Z', '#c0846c');
  a.line('M11.4 4.7C11.9 4.6 12.4 4.8 12.8 5.1', { w: 1.8, c: BROW });
  nose(a);
  a.line('M9.9 10.5C10.4 10.25 11 10.2 11.6 10.3', { w: 1.8, c: LIP });
} };

// ---------- the look editor: hair, each cut in the head's coordinates over the same skull ----------
const hairOf = (draw) => ({ w: 16, h: 18, shift: [2, 2], draw });
const cutHead = (a) => { a.cut(SKULL, { grow: .9 }); a.cut(ell(3.7, 7, 1, 1.4), { grow: .9 }); };
const shine = (a, clip, d, ...dabs) => { a.flat(d, '#c0905c', { clip }); for (const dab of dabs) a.flat(dab, '#ecc89c', { clip }); };
// Cropped with a blunt knife: the short back and a crown of uneven tufts standing up.
const SHAGGY = 'M1.4 7.6C.7 6.2 .5 4.8 1 3.4L.7 2.6L1.9 2.4L2.3 1.1L3.6 1.2L4.2 .1L5.6 .2L6.6 -.7L7.8 -.1L9.2 -.7L10.2 .1L11.8 0L12.2 1.1L13.6 1.7L13.4 3L13.9 4.2L13.2 4.8C13.2 5.4 13 5.8 12.7 6.2L11.9 4.4L11.2 5.4L10.3 3.9L9.3 5.2L8.5 3.7L7.4 5L6.8 3.7L5.6 5.4L5 4.2L4.2 6.2L3.2 5.6L2.8 7.4Z';
const hairShaggy = hairOf((a) => {
  a.shape('M1.2 3C2.6 .4 6 -.8 9 -.4C12 0 14 2 14 5L12.8 8L4.4 10.2L3.4 10.6L3 10L2 10.9L1.5 10.1L.6 10.2L.3 9.2L-.4 8.6L-.2 7.6L-.7 6.8L-.3 5.8L-.6 4.8C-.2 4.2 .4 3.6 1.2 3Z', HAIR, {
    shade: 'M-2 8C0 8.8 2 8.8 3.6 8L4.4 13L-2 13Z', lite: 'M1.6 2.6C3 .4 7 -.6 10 -.2C8 .2 3.8 .8 1.6 2.6Z', rim: .3, lines: ['M.4 8.4C.9 7 1.2 5.4 1.2 3.8', 'M2 10C2.3 8.4 2.5 6.8 2.3 5.2'] });
  cutHead(a);
  a.shape(SHAGGY, HAIR, { shade: 'M0 5L14 5L14 8.6L0 8.6Z', lite: 'M3 1.4L6 .8L7 0L8 .5L9.4 .1L10.2 1L7 1.8L3.4 2.6Z', rim: .26,
    lines: ['M6.8 3.6C6.9 2.4 6.8 1 6.7 -.4', 'M9.3 3.8C9.4 2.4 9.4 1 9.3 -.4', 'M4.6 4.2C4.4 2.8 4.3 1.2 4.3 .3', 'M11.9 4C12 3 12 1.8 11.9 .4'] });
  shine(a, SHAGGY, 'M4.6 2.6L6 2L6.4 2.6L7.6 2L8.2 2.6L9.6 2L10 2.6L8.4 3.1L7.4 2.9L6 3.4Z', 'M6.2 2.4L7 2.1L7.1 2.4L6.4 2.7Z');
});
// Grown out and let be: one long sheet down the back to the shoulders, swept over from a parting.
const SWEPT = 'M1.4 7.4C.4 4.4 2 .4 6.4 -.8C9.8 -1.6 13.2 -.4 13.9 3C14.1 4.2 13.9 5.4 13.4 6.2C12.6 4.8 11.4 3.8 9.8 3.4C9.2 4.4 7.6 4.6 6.6 4.2C5.6 5 4.8 6.2 4.4 7.2C3.6 7.6 2.2 7.8 1.4 7.4Z';
const hairLong = hairOf((a) => {
  a.shape('M1 2.6C2.6 -.6 6.6 -1.8 9.6 -1C12.4 -.2 14 2 14 5C14 6.4 13.4 7.4 12.8 8L5 9.6C4.9 11.6 4.6 13.4 3.8 15.2C3 15.7 2.2 15.3 1.5 15.7C.7 15.4 -.3 15.2 -1 14.4C-1.3 11.4 -1.5 8.4 -1 5.6C-.8 4.4 0 3.4 1 2.6Z', HAIR, {
    shade: 'M-2 9C-.6 10 2 10 5.4 9.2L5.4 16L-2 16Z', deep: 'M-2 13.4C-.4 14 2 14 4.6 13.4L4.6 16L-2 16Z', lite: 'M1.4 2.4C3 -.8 8 -1.6 11 -.8C8.8 -.4 3.6 .2 1.4 2.4Z', rim: .3,
    lines: ['M.2 5C-.2 8 -.2 11.4 .2 14.8', 'M1.6 4.6C1.2 8 1.4 11.6 1.7 15', 'M3 6C3 9 3.1 12 3 14.8'] });
  cutHead(a);
  a.shape(SWEPT, HAIR, { shade: 'M0 5.2L14 5.2L14 8.6L0 8.6Z', lite: 'M3.4 -.2C6 -1.2 10 -1.2 12 .2C10 -.2 6 -.2 3.4 .8C3 1 3 0 3.4 -.2Z', rim: .26,
    lines: ['M9.8 3.4C8.6 2.4 7.6 1 7 -.6', 'M6.6 4.2C6.2 3 5.8 1.6 5.6 .2', 'M4.4 6.8C3.8 5.2 3.4 3.4 3.6 1.4', 'M11.6 3.8C11.4 2.8 11 1.6 10.4 .6'] });
  shine(a, SWEPT, 'M3.4 3.6L5.2 2.6L7.2 2.2L9.4 2.2L11.4 2.6L12.2 3L10.8 3L9 2.8L7.2 3L5.4 3.4L3.6 4.4Z', 'M6.2 2.6L7.4 2.4L7.5 2.7L6.4 2.9Z');
});
// Tied up out of the way: drawn flat back over the skull into a knot at the crown, bound with cord.
const SLICKED = 'M1.2 7.8C.4 4.6 2 .4 6.4 -.8C9.8 -1.6 13 -.4 13.7 2.8C13.8 3.6 13.7 4.2 13.4 4.8C12.2 3.6 10.2 3 8 3.2C6.4 3.4 5 4.2 4.4 5.6C4.2 6.4 3.8 7 3.2 7C2.8 7.8 2 8.2 1.2 7.8Z';
const hairKnot = hairOf((a) => {
  a.shape('M1 2.6C2.6 -.6 6.6 -1.8 9.6 -1C12.4 -.2 14 2 14 5L12.8 8L4 10.8C3.2 11.2 2.2 11.2 1.5 10.6C.6 10.2 0 9.4 -.2 8.4C-.5 6.8 -.2 4.4 1 2.6Z', HAIR, {
    shade: 'M-2 8C0 8.6 2 8.6 3.6 8L4.4 13L-2 13Z', lite: 'M1.4 2.4C3 -.8 8 -1.6 11 -.8C8.8 -.4 3.6 .2 1.4 2.4Z', rim: .3, lines: ['M.6 8.8C.9 7 1.2 5.2 1.6 3.6', 'M1.8 10.2C2 8.4 2.4 6.8 2.4 5'] });
  cutHead(a);
  a.shape(SLICKED, HAIR, { shade: 'M0 5.6L14 5.6L14 8.6L0 8.6Z', lite: 'M3.4 -.2C6 -1.2 10 -1.2 12 .2C10 -.2 6 -.2 3.4 .8C3 1 3 0 3.4 -.2Z', rim: .26,
    lines: ['M13 3.6C10.4 1.6 6.4 .8 2.6 1', 'M10.6 3.1C8 1.8 5.4 1.4 2.4 2.2', 'M7.6 3.3C5.8 2.6 4 2.8 2.2 3.8'] });
  shine(a, SLICKED, 'M4.2 2L6.4 1.5L8.6 1.7L10.4 2.3L8.6 2.2L6.4 2.1L4.4 2.6Z');
  // The knot on the crown behind the skull, pulled tight, bound at its foot.
  a.shape('M-1 -.6C0 -1.4 1.6 -1.3 2.4 -.4C3.2 .4 3.1 1.6 2.4 2.3C1.5 3 0 2.9 -.8 2.1C-1.4 1.4 -1.5 .2 -1 -.6Z', HAIR, { shade: 'M-1.8 1.5L3.6 1.5L3.6 3.6L-1.8 3.6Z', lite: 'M-.4 -.9C.6 -1.2 1.6 -1 2.1 -.4L1.4 .1C.8 -.4 .2 -.4 -.4 -.2Z', rim: .24, lines: ['M-.8 .4C.2 -.2 1.4 -.2 2.5 .4', 'M-.8 1.4C.2 .8 1.6 .8 2.6 1.3'] });
  a.shape('M2 2.2L3.2 1.2L3.8 1.9L2.6 3Z', G, { outer: false, lite: 'M2 2.2L3.2 1.2L3.4 1.5L2.2 2.5Z' });
});
// Pulled back and tied at the nape: a tail hanging clear behind, a strip of cord round it, its end frayed.
const hairTail = hairOf((a) => {
  a.shape('M-.4 5.4C-1.4 5.8 -2 6.8 -2 8.2C-2 10 -1.6 11.8 -.8 13.2L-.2 12.6L.2 13.8L.8 12.8L1.2 13.2C1.8 11.6 2.2 9.6 2 7.6C1.8 6.4 .8 5.2 -.4 5.4Z', HAIR, { shade: 'M-2.4 10L3 10L3 14L-2.4 14Z', lite: 'M-1 6C-.4 5.6 .4 5.6 .8 6.2L0 7.6L-1 7.6Z', rim: .24, lines: ['M-.8 6.6C-1 8.6 -.8 10.6 -.3 12.4', 'M.6 7C.8 8.8 .8 10.6 .6 12.2'] });
  a.shape('M1 2.6C2.6 -.6 6.6 -1.8 9.6 -1C12.4 -.2 14 2 14 5L12.8 8L4 10.2C3.2 10.4 2.2 9.8 1.5 9C.8 8.4 .3 7.6 .2 6.8C.2 5 .2 3.8 1 2.6Z', HAIR, {
    shade: 'M-1 8C.6 8.4 2 8.2 3.4 7.8L4.4 12L-1 12Z', lite: 'M1.4 2.4C3 -.8 8 -1.6 11 -.8C8.8 -.4 3.6 .2 1.4 2.4Z', rim: .3, lines: ['M1 7.6C1.2 6 1.4 4.6 2 3.2'] });
  cutHead(a);
  a.shape('M-1.2 6.5C-.3 6 .7 6 1.3 6.6L1.3 7.8C.6 7.4 -.4 7.4 -1.3 7.8Z', G, { outer: false, lite: 'M-1.2 6.5C-.3 6 .7 6 1.3 6.6L1.3 7C.6 6.5 -.4 6.5 -1.3 7Z' });
  a.shape(SWEPT, HAIR, { shade: 'M0 5.2L14 5.2L14 8.6L0 8.6Z', lite: 'M3.4 -.2C6 -1.2 10 -1.2 12 .2C10 -.2 6 -.2 3.4 .8C3 1 3 0 3.4 -.2Z', rim: .26,
    lines: ['M13.4 5.8C11.6 3 8 1.2 3.4 1.4', 'M9.8 3.4C7.8 2.2 5.6 2 3 3.2', 'M6.6 4.2C5.2 3.6 3.8 4 2.4 5.2'] });
  shine(a, SWEPT, 'M4 2.4L6.2 1.8L8.6 1.9L10.6 2.6L8.6 2.5L6.2 2.4L4.2 3Z');
});
// Shaved and grown back a little: a dark nap on the back and crown, no fringe, the forehead bare and the hairline going.
const NAP = { ...HAIR, base: '#573439', shade: '#40263a', deep: '#2e1c2c', lite: '#6e4a44' };
const hairCropped = hairOf((a) => {
  const cap = 'M1.5 8.2C.8 5 2 1.6 5 .1C7 -.8 9.2 -.8 10.8 .2C11.6 .7 11.2 1.5 10.2 1.6C9 1.7 8.4 2.2 7.8 2.9C6.8 3 6 3.5 5.4 4.4C5 5.2 4.6 6.2 4.2 7C4.2 7.8 3.9 8.3 3.2 8.2C2.8 8.8 2 8.8 1.5 8.2Z';
  a.shape(cap, NAP, { shade: 'M0 6L5 6L5 9L0 9Z', lite: 'M3.4 1.6C5 .4 7.4 -.2 9.4 .3C7.4 .5 5 1.2 3.6 2.4Z', rim: .2 });
  for (const [x, y] of [[3, 2.8], [4.4, 2], [5.8, 1.2], [7.4, .8], [9, .6], [2.4, 4.4], [3.8, 3.8], [5, 3], [6.4, 2.2], [2.2, 6], [3.4, 5.4], [2.6, 7.2]]) a.line(`M${x} ${y}L${x - .3} ${y + .5}`, { w: .9, c: '#7c5444' });
});

// ---------- beards: over the jaw, the mouth left clear. A look's beard 1 wears the first ----------
const beardJaw = hairOf((a) => {
  a.shape('M3.8 8.6L5.5 8.5C5.9 9.4 7.2 10.1 8.8 10.7C9.8 11 10.8 11 11.6 10.7L11.9 10.9C11.4 11.5 10.2 12.2 8.9 12.4C7.2 12.5 5.2 11.6 4.1 10.5C3.7 9.9 3.6 9.2 3.8 8.6Z', HAIR, {
    shade: 'M3 11L12 11L12 13L3 13Z', lite: 'M3.9 8.7L5.4 8.6L5.6 9.3L4 9.5Z', rim: .2, lines: ['M4.8 9.2L5.2 10.8', 'M6.2 10.2L6.4 11.6', 'M7.8 10.8L7.9 12', 'M9.4 11.1L9.3 12.1'] });
});
const beardFull = hairOf((a) => {
  a.shape('M3.9 7L5.4 7.1C5.5 8.5 6.2 9.6 7.4 10.2C8.2 10.7 8.8 11 9.4 11L11.6 11C11.9 10.6 11.9 9.6 12.4 8.9L12.9 9.2C13.1 10.4 12.6 11.6 11.6 12.6C10.8 13.5 9.2 13.9 7.8 13.5C6 13 4.5 12 3.8 10.4C3.4 9.2 3.4 8 3.9 7Z', HAIR, {
    shade: 'M3 11.4L14 11.4L14 14.5L3 14.5Z', deep: 'M3 12.9L14 12.9L14 14.5L3 14.5Z', lite: 'M4 7.1L5.3 7.2L5.5 8.4L4 8.6Z', rim: .24,
    lines: ['M4.6 8.2L5 10.8', 'M6 10L6.4 12.6', 'M7.6 11L7.8 13.2', 'M9.2 11.4L9.3 13.5', 'M10.8 11.2L10.6 13'] });
  // The moustache over the mouth's corner, grown down into it.
  a.shape('M9.4 9.5C10.2 9.1 11.2 9.1 12.2 9.3L11.9 9.9C11 9.7 10.2 9.8 9.6 10.1Z', HAIR, { outer: false, lite: 'M9.6 9.45C10.4 9.15 11.2 9.15 12 9.3L11.9 9.5C11 9.4 10.2 9.45 9.6 9.7Z' });
});

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

const OF = { torso: 's_torso', pack: 's_pack', upperArm: 's_upper', forearm: 's_fore', thigh: 's_thigh', shin: 's_shin',
  jumpsuit_torso: 'b_torso', jumpsuit_upperArm: 'b_upper', jumpsuit_forearm: 'b_fore', jumpsuit_thigh: 'b_thigh', jumpsuit_shin: 'b_shin' };
const LIGHT = [0, -1], Margin = 3, Px = 4;
const part = (p) => bake({ ...p, margin: Margin, light: LIGHT });

/** The player's mark, white for Modulate to tint: a hard light in the middle, and a soft ring so it carries at a far zoom. 8 world pixels across. */
function stripe() {
  const c = canvas(64, 64), x = c.getContext('2d');
  // Helmet coordinates, the picture centred on (5, 2) of the shell.
  x.setTransform(Px, 0, 0, Px, (8 - 5) * Px, (8 - 2) * Px);
  x.lineCap = 'round'; x.strokeStyle = 'rgba(255,255,255,.22)'; x.lineWidth = 3; x.stroke(new Path2D(CROWN));
  x.strokeStyle = '#fff'; x.lineWidth = 1.3; x.stroke(new Path2D(CROWN));
  return c;
}

function mark() {
  const S = 32, c = canvas(S, S), x = c.getContext('2d'), m = S / 2;
  const g = x.createRadialGradient(m, m, 5, m, m, m);
  g.addColorStop(0, 'rgba(255,255,255,.55)'); g.addColorStop(.5, 'rgba(255,255,255,.14)'); g.addColorStop(1, 'rgba(255,255,255,0)');
  x.fillStyle = g; x.fillRect(0, 0, S, S);
  x.fillStyle = '#fff'; x.beginPath(); x.arc(m, m, 5.2, 0, Math.PI * 2); x.fill();
  return c;
}

Subjects.hero = function () {
  for (const [name, id] of Object.entries(OF)) out('hero_' + name, part(PARTS.find((p) => p.id === id)));
  out('hero_head', part(headPic));
  out('hero_face_0', part(facePic));
  out('hero_hair_0', part(hairPic));
  // A look's beard 1 wears the first picture, 2 the second; beard 0 hides the sprite.
  out('hero_beard_0', part(beardJaw));
  out('hero_beard_1', part(beardFull));
  [faceHooded, faceBurned, faceSquint, faceSleepless].forEach((p, i) => out(`hero_face_${i + 1}`, part(p)));
  [hairShaggy, hairLong, hairKnot, hairTail, hairCropped].forEach((p, i) => out(`hero_hair_${i + 1}`, part(p)));
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
  out('hero_helmet', bake({ ...PARTS.find((p) => p.id === 's_head'), units: 1.12, shift: [.35, .75], light: LIGHT }));
  const helmet = part(PARTS.find((p) => p.id === 's_head'));
  const glass = new Path2D(GLASS), place = (x) => { x.setTransform(Px, 0, 0, Px, Margin * Px, Margin * Px); };
  const shell = canvas(helmet.width, helmet.height), sx = shell.getContext('2d');
  sx.drawImage(helmet, 0, 0);
  sx.globalCompositeOperation = 'destination-out';
  place(sx); sx.fill(glass);
  out('hero_helmet_shell', shell);
  out('hero_mark', mark());
  out('hero_mark_stripe', stripe());
  const visor = canvas(helmet.width, helmet.height), vx = visor.getContext('2d');
  vx.globalAlpha = GLASS_SEEN;
  vx.drawImage(helmet, 0, 0);
  vx.globalAlpha = 1;
  vx.globalCompositeOperation = 'destination-in';
  place(vx); vx.fill(glass);
  out('hero_visor', visor);
};

})();
