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
  // Look 0 wears no beard; the slot still loads a picture, blank.
  out('hero_beard_0', canvas((16 + Margin * 2) * Px, (18 + Margin * 2) * Px));
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
