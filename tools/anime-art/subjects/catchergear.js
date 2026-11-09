// The catchers' gear (tracker-catchers): the four outfits of the gear page, all picked; a catcher wears one by its id. Each is drawn as
// hero.js draws the regen suit and the heavy helmet, in the basic suit's boxes and joints, with hero.js's own kit (window.HeroKit).
// Pictures: hero_<body>_<part>, the receiver's hero_<body>_upperArm_lit, and hero_<helmet>_shell, _visor and _lit (the goggles).
(function () {
'use strict';
const { ell, rr, poly } = Anime;

/** The outfits, built once the hero's kit is there: subjects load in name order, hero.js after this. */
function outfits() {
  const { limb, wedge, folds, mat, glove, boot, BACK_TORSO, GL, band, BST, BELL_BRASS, BELL_RUBBER, rustAt, hose, port, UNLIT, rivet, torsoDeep } = window.HeroKit;
  const WHITE = '#ffffff';
  /** A tall boot: its shaft straight up the shin, no wider than it, over the hero's own foot, so the foot turns at the ankle as the hero's. */
  const tallBoot = (a, M, top, lines, toe) => {
    a.shape(`M1.1 ${top}L4.9 ${top}L5 7L1 7Z`, M, { shade: .45 });
    boot(a, M, 6.3, lines, toe);
  };

  // ================= А «Следопыт»: a long oilcloth coat, its tails on the thighs; a hood over a gas mask, two round goggles, a drum filter at the chin; the receiver strapped to the near shoulder =================
  const stalker = (() => {
    const OIL = mat('#3d4034', '#2b2e25', '#1e201a', '#575a4b');
    const WRAP = mat('#5c5647', '#433f34', '#2f2c25', '#716a58');
    const LEATH = mat('#5a4231', '#3f2e23', '#2b1f19', '#725541');
    const HIDE = mat('#3a2b22', '#29201a', '#1c1612', '#4f3c2f');
    const MASK = mat('#504d48', '#383633', '#262523', '#67635d');
    const BOX_M = mat('#45473f', '#30322c', '#21221e', '#5d6055');
    const BOOT = mat('#2f2520', '#211a16', '#171210', '#45372e');
    const CAN = mat('#5b6066', '#41454b', '#2d3034', '#767b82');
    const PIP = ell(3.35, 1.7, .42);
    const GLASS_N = ell(8.9, 6.15, 1.3, 1.35), GLASS_F = ell(11.95, 6.05, .4, 1.1);
    const HOOD = 'M-.8 13.4C-.4 11 .1 9 .3 7.6C.2 4.6 1.4 2 3.4 .9C5.4 -.1 8.6 -.2 10.4 .8C11.4 1.3 12.1 1.9 12.5 2.6L11.4 3.1C10.2 3.6 9.6 5 9.4 6.8C9.2 8.8 8.8 10.8 8.6 12.4C8.9 12.9 9.4 13.4 9.4 13.9C7 14.6 2.4 14.6 -.8 13.4Z';
    const COAT = 'M2.6 .2C4.2 -.5 8 -.5 9.6 .2C11 .8 11.6 2 11.6 3.6L11.2 8.4C11.3 10.4 11.8 12.6 12.3 14.8L12.6 17.4L-.6 17.4L-.3 14.8C.3 12.6 .8 10.4 .8 8.4L.4 3.6C.4 2 1.1 .8 2.6 .2Z';
    return {
      id: 'stalker', title: 'Следопыт', glow: [214, 238, 120],
      tag: 'Длинный промасленный плащ: полы на бёдрах расходятся на шагу и в полёте. Капюшон поверх резинового противогаза, двое круглых очков в латуни, барабан фильтра под подбородком. Приёмник — коробка на ремне у плеча, огонёк сверху; ноги в обмотках, высокие сапоги.',
      suit: {
        torso(a) {
          a.shape(COAT, OIL, { deep: 'M-1 15.8C3 16.6 9 16.6 13 15.6L13 18L-1 18Z', ...folds([1.8, 6.4, 5, 7.4, .9], [10.6, 5.2, 7.8, 6.6, .9], [1.4, 13.2, 4.2, 14.2, .8], [11.2, 13.4, 8.2, 14.4, .8]) });
          a.line('M9.6 .6C9.9 4 10.1 9 10.7 17.2', { w: 1.2 });
          a.line('M2.2 12.8L1.9 17.2', { w: 1 });
          a.shape('M7.2 -1.6L10.6 -1L11.1 1.8L8.6 1.3Z', OIL, { shade: .4, lines: ['M8 -1.2L9.2 1.2'] });
          a.shape('M.7 8.8C4 9.4 8 9.4 11.3 8.8L11.3 10.4C8 11 4 11 .7 10.4Z', LEATH, { outer: false, shade: .35, lines: ['M3.4 9.2L3.4 10.6'] });
          a.shape(rr(9.1, 8.8, 1.7, 1.7, .25), BELL_BRASS, { outer: false, shade: .3 });
          a.flat(rr(9.55, 9.25, .8, .8, .1), LEATH.deep);
          a.shape('M8.6 11.9L11.5 11.6L11.8 13.1L8.9 13.4Z', OIL, { shade: .3 });
          for (const y of [2.6, 5.6]) a.flat(ell(10.1, y, .32), '#16130f');
          a.line('M3 15.6L3.3 16.8M7.6 15.8L7.4 17', { w: .9 });
          rustAt(a, 10.4, 10.2, .35);
        },
        pack(a) {
          a.shape(rr(1.4, -1.3, 1.4, 1.5, .3), BELL_BRASS, { shade: .35 });
          a.shape(rr(.5, 0, 3.2, 12, 1.4), CAN, { shade: .8, deep: band(0, 10.6, 5, 13), lite: band(1.2, .3, 1.8, 11.6) });
          for (const y of [2.2, 8.8]) a.shape(rr(-.1, y, 4.4, 1.1, .2), LEATH, { shade: .35, lines: [`M3.2 ${y + .1}L3.2 ${y + 1}`] });
          rustAt(a, 2.8, 5.4, .55); rustAt(a, 1.4, 10.4, .4);
        },
        upperArm(a) {
          a.shape(limb(4.4, 4, 2.5, 2.5, 8.6), OIL, folds([.9, 5.6, 3.8, 6.4, .8], [.8, 7.4, 4, 8, .7]));
          a.shape('M.2 2.6C.2 .9 1.2 0 2.5 0C3.8 0 4.8 .9 4.8 2.6C4.8 3.4 4.5 4 4.1 4.3C3 4.7 2 4.7 .9 4.3C.5 4 .2 3.4 .2 2.6Z', OIL, { outer: false, shade: .5 });
          a.shape('M.1 3.4L4.9 3.3L4.9 4.4L.1 4.5Z', LEATH, { outer: false, shade: .3 });
          a.tube('M1.5 1.1L1 -1.2', .32, BELL_BRASS, { spec: false });
          a.shape(rr(.9, .9, 3.2, 2.6, .4), BOX_M, { shade: .45, lite: band(1, .9, 4, 1.3), lines: ['M1.4 2.5L3 2.5', 'M1.4 2.95L3 2.95'] });
          a.shape(ell(3.35, 1.7, .62), BELL_BRASS, { outer: false, shade: .2 });
          a.flat(PIP, UNLIT);
          a.flat(rr(3.7, 3.4, .7, 1, .1), BELL_BRASS.shade);
        },
        forearm(a) {
          a.shape(limb(4, 3.6, 2.5, 2.9, 6.6), OIL, folds([1, 4.2, 3.8, 5, .7]));
          glove(a, HIDE);
          a.shape('M.1 5.1L4.9 4.9L5.1 6.9C3.6 7.4 1.4 7.4 -.1 6.9Z', OIL, { shade: .4, lines: ['M.3 6L4.8 5.9'] });
        },
        thigh(a) {
          a.shape(limb(4.6, 3.8, 3, 2.5, 11.3), WRAP, { deep: 'M-1 11.4L7 11.4L7 15L-1 15Z' });
          a.shape('M-.6 .4L6.6 .4C7 4.6 7.4 9 7.8 13.6L6.6 14.2L5.6 13.6L4.2 14.3L2.6 13.8L1.4 14.4L-.2 13.7L-1.2 14.1C-1 9 -.9 4.6 -.6 .4Z', OIL,
            { shade: 'M-2 12.4L9 12.2L9 15L-2 15Z', deep: [wedge(4.8, 1, 5.6, 12.8, 1.2)], lines: ['M2.4 1L2 13.4', 'M5 1.2L5.5 13.2', 'M6.2 9.4L6.9 10'] });
          a.line('M.4 13.2L.6 13.9M3.6 13.3L3.4 14', { w: .9 });
        },
        shin(a) {
          a.shape(limb(4, 3.6, 3, 2.18, 6.6), WRAP, { lines: ['M1 1.4L5 .6', 'M1 2.8L5 2', 'M1.1 4.2L4.9 3.4', 'M1.2 5.6L4.8 4.8'], shade: [wedge(1, 1.4, 5, .6, .5), wedge(1.1, 4.2, 4.9, 3.4, .5)] });
          boot(a, BOOT, 6.3, ['M4 6.8L4.5 9', 'M1 9.2L4 9.2'], 8.4);
        },
      },
      lit: { upperArm(paint) { paint(PIP, WHITE, WHITE); } },
      helmet(a) {
        a.shape(HOOD, OIL, { shade: .9, deep: 'M-1 12.4C2 13.4 6 13.6 10 13.2L10 15L-1 15Z', lite: 'M2 3.2C3.2 1.2 6.4 .2 9.6 .8L9.4 1.4C6.6 1 3.8 1.8 2.6 3.6Z',
          lines: ['M6.8 .2C4 2 2.6 4.6 2.4 7.6', 'M2.8 12.2L3.2 13.6', 'M6 12.6L6.2 13.8'] });
        a.shape('M7.6 3.6C9.4 3 11.6 3.1 12.8 3.9C13.3 5.4 13.3 7.4 13 8.6C13.2 9.6 13 10.8 12.2 11.6C11 12.4 9 12.4 7.8 11.8C7 10.6 6.6 8.6 6.7 6.6C6.7 5.2 7 4.2 7.6 3.6Z', MASK,
          { shade: .7, lines: ['M7.4 9.4C8.6 10 10 10.2 11.2 10'] });
        a.shape('M7.6 4.4L1.2 6.2L1.2 7.4L7.6 5.6Z', LEATH, { outer: false, shade: .25 });
        a.shape(rr(3, 5.5, 1.1, 1.3, .2), BELL_BRASS, { outer: false, shade: .3 });
        a.flat(ell(9.4, 10, .9, .7), MASK.deep);
        a.line('M8.8 9.8L10 9.8M8.9 10.25L9.9 10.25', { w: .9, c: '#0e0c0b' });
        a.shape(rr(11.2, 9.2, 2.5, 2.8, .7), BST, { shade: .6, lines: ['M12 9.3L12 11.9', 'M12.8 9.3L12.8 11.9'] });
        a.shape(ell(13.6, 10.6, .55, 1.35), BST, { shade: .3 });
        a.flat(ell(13.65, 10.6, .25, .7), '#262a34');
        a.shape(ell(8.9, 6.15, 1.85, 1.9), BELL_BRASS, { shade: .45 });
        port(a, GLASS_N, 'M7.8 5.4C8.4 4.9 9.4 4.9 10 5.4L9.9 5.7C9.3 5.3 8.5 5.3 7.9 5.7Z');
        a.shape(ell(11.95, 6.05, .72, 1.55), BELL_BRASS, { shade: .4 });
        port(a, GLASS_F);
        a.flat(rr(10.6, 5.7, .7, .6, .1), BELL_BRASS.shade);
        rustAt(a, 12.3, 11.6, .35);
      },
      glass: GLASS_N + GLASS_F, bars: [],
      helmetLit(paint) { paint(GLASS_N, WHITE, WHITE); paint(GLASS_F, WHITE, WHITE); },
    };
  })();

  // ================= Б «Упряжь»: a black rubber flight suit in an oxblood jump harness; a leather flight cap, big brass goggles, a rubber snout and its hose to the chest; a brass receiver bell on the shoulder =================
  const harness = (() => {
    const RUB = mat('#2f3135', '#222327', '#17181b', '#474a50');
    const OX = mat('#6a2e25', '#4b201b', '#331613', '#844036');
    const CAP = mat('#4d3a2c', '#36291f', '#251c16', '#67503e');
    const JACK = mat('#24201e', '#1a1716', '#121010', '#3b3532');
    const PIP = ell(3.65, .55, .36);
    const GLASS_N = ell(9.2, 5.8, 1.55, 1.6), GLASS_F = ell(12.4, 5.7, .45, 1.2);
    return {
      id: 'harness', title: 'Упряжь', glow: [255, 176, 80],
      tag: 'Чёрный резиновый лётный костюм в бордовой подвесной упряжи: ремни через грудь и спину, широкий пояс, петли на бёдрах, нож у бедра. Кожаный лётный шлем, большие латунные очки, резиновое рыло с фильтром и шланг к груди. Приёмник — латунный колокол на плече.',
      suit: {
        torso(a) {
          a.shape(BACK_TORSO, RUB, { deep: torsoDeep, ...folds([2.4, 6.6, 5.4, 7.6, .8], [10, 6.4, 7.6, 7.6, .8], [2.2, 11.6, 5, 12.4, .7]) });
          a.shape(rr(3.2, -1.5, 5.6, 2.5, .8), RUB, { shade: .45, lines: ['M4.4 -1.3L4.4 .8', 'M5.6 -1.3L5.6 .8', 'M6.8 -1.3L6.8 .8', 'M8 -1.3L8 .8'] });
          a.shape('M8.8 0L10.3 .1L10.8 9L9.3 9Z', OX, { outer: false, shade: .25, lines: ['M9.2 .4L9.6 8.8'] });
          a.shape('M1.7 .4L3.3 .1L3 9L1.3 9Z', OX, { outer: false, shade: .25, lines: ['M2.9 .4L2.6 8.8'] });
          a.shape('M.8 8.6C4 9.2 8 9.2 11.2 8.6L11.2 10.7C8 11.3 4 11.3 .8 10.7Z', OX, { shade: .35, lines: ['M1.2 9.3C4 9.8 8 9.8 10.8 9.3', 'M1.2 10.2C4 10.7 8 10.7 10.8 10.2'] });
          a.shape(ell(9.95, 5, 1.1, 1.1), BELL_BRASS, { shade: .4 });
          a.shape(ell(9.95, 5, .52, .52), BELL_BRASS, { outer: false, shade: .2, lines: ['M9.6 5L10.3 5'] });
          a.tube(ell(10.5, 11.4, .55, .6), .26, BELL_BRASS, { spec: false });
          a.flat(rr(1.4, 9.3, .9, .9, .15), BELL_BRASS.shade);
          a.line('M3.6 3.2L4.4 3.6M2.2 12.6L3.4 13', { w: .9 });
        },
        pack(a) {
          a.shape(rr(1.5, -1.2, 1.2, 1.4, .3), BELL_BRASS, { shade: .35 });
          a.shape(rr(.4, .1, 3.6, 11.6, 1.5), RUB, { shade: .8, deep: band(0, 10.4, 5, 12.2), lines: ['M.5 6L3.9 6'] });
          for (const y of [2, 8.4]) { a.shape(rr(-.2, y, 4.4, 1.2, .2), OX, { shade: .3 }); a.flat(rr(2.9, y + .15, .8, .9, .1), BELL_BRASS.base); }
          a.line('M1.4 4L2.2 4.4', { w: .9 });
        },
        upperArm(a) {
          a.shape(limb(4, 3.6, 2.5, 2.5, 8.6), RUB, folds([1, 5.4, 3.8, 6.2, .7], [.9, 7.2, 4, 7.8, .7]));
          a.shape('M.4 1.4C1.4 .8 3.6 .8 4.6 1.4L4.6 2.6C3.6 2.2 1.4 2.2 .4 2.6Z', OX, { outer: false, shade: .3 });
          a.shape('M1.1 1.7C1.1 .2 1.8 -.7 2.6 -.7C3.4 -.7 4.1 .2 4.1 1.7Z', BELL_BRASS, { shade: .35, lite: 'M1.6 .4C1.9 -.3 2.3 -.5 2.6 -.5L2.6 -.1C2.3 -.1 2 .2 1.8 .6Z', lines: ['M1.2 .95L4 .95', 'M1.15 1.35L4.05 1.35'] });
          a.flat(PIP, UNLIT);
          a.flat(rr(2.2, -1.1, .8, .5, .1), BELL_BRASS.deep);
        },
        forearm(a) {
          a.shape(limb(3.6, 3.2, 2.5, 2.9, 6.6), RUB, folds([1, 3.2, 3.8, 3.8, .5]));
          a.shape('M.3 3.4L4.7 3.2L4.8 6.2L.2 6.4Z', OX, { shade: .35, lines: ['M.3 4.4L4.7 4.2', 'M.3 5.4L4.7 5.2'] });
          a.flat(rr(3.7, 4.05, .7, .5, .1), BELL_BRASS.base); a.flat(rr(3.7, 5.05, .7, .5, .1), BELL_BRASS.base);
          glove(a, RUB);
          a.shape('M.4 6.2L4.6 6.1L4.9 7.6C3.4 8 1.6 8 .1 7.6Z', RUB, { shade: .35 });
        },
        thigh(a) {
          a.shape(limb(4.6, 3.8, 3, 2.5, 11.3), RUB, { deep: 'M-1 11.4L7 11.4L7 15L-1 15Z', ...folds([1.2, 9.2, 4.6, 10, .8]) });
          a.shape('M-.2 2L6.2 3.4L6.2 4.7L-.2 3.3Z', OX, { shade: .3 });
          a.shape(rr(4, 2.9, 1.3, 1.5, .25), BELL_BRASS, { outer: false, shade: .3 });
          a.shape(rr(3.5, 4.2, 1.4, 1.8, .3), JACK, { shade: .3 });
          a.shape('M3.4 5.8L5 6L4.7 10.6L4 11.2L3.5 10.4Z', OX, { shade: .4, lines: ['M4.2 6.2L4.1 10.4'] });
        },
        shin(a) {
          a.shape(limb(4, 3.6, 3, 2.18, 6.6), RUB, folds([1.2, 2.6, 4.8, 3, .5]));
          tallBoot(a, JACK, 3.8, ['M4 6.8L4.5 9', 'M1 9.2L4 9.2']);
          a.shape(poly(1.1, 4.7, 4.9, 4.6, 4.9, 5.2, 1.1, 5.3), OX, { outer: false, shade: .2 });
          a.flat(rr(3.9, 4.55, .7, .8, .1), BELL_BRASS.base);
        },
      },
      lit: { upperArm(paint) { paint(PIP, WHITE, WHITE); } },
      helmet(a) {
        hose(a, [11.6, 11.4, 10.6, 13.8, 8.2, 14.8], .95, 6);
        a.shape('M1.2 9.6C.6 7.6 .8 5 1.8 3.4C3 1.4 5.4 .4 7.8 .5C10 .6 11.6 1.8 12 3.6L12.1 4.6L9.6 4.4C8.2 4.4 7 5 6.6 6.2L6.4 9.4C6.2 10.8 6.6 12 7 12.8L3.4 13C2.4 12.2 1.6 11 1.2 9.6Z', CAP,
          { shade: .8, deep: 'M0 11.4L9 11.4L9 14L0 14Z', lite: 'M2.6 2.8C4 1.2 6.6 .6 9 .9L8.8 1.5C6.6 1.3 4.4 1.8 3.2 3.2Z', lines: ['M7.6 .6C6.6 3 6.2 4.6 6.4 6', 'M2.4 3.4C3.6 5 3.8 7 3.4 9.4'] });
        a.shape('M3.6 6C4.8 5.6 6.4 5.8 7 6.6L7.2 10.6C6.6 11.6 5 11.8 3.8 11.2C3.2 9.6 3.2 7.4 3.6 6Z', CAP, { shade: .45, lines: ['M4.2 6.6L4.3 10.6', 'M3.9 6.4C4.8 6.1 6 6.2 6.6 6.8'] });
        a.shape('M7.2 7.1C8.6 7.5 10.6 7.5 12.2 7.1C12.9 8.2 13.1 9.6 12.8 10.8C12.2 11.8 10.6 12.3 9.2 12.2C8.2 12.1 7.4 11.6 7 11C6.8 9.8 6.8 8.2 7.2 7.1Z', RUB, { shade: .6 });
        a.shape('M6.8 10.2L10.6 11.5L10.4 12.3L6.6 11.1Z', CAP, { outer: false, shade: .2 });
        a.shape(rr(5.8, 9.8, 1.2, 1.4, .2), BELL_BRASS, { outer: false, shade: .3 });
        a.shape('M11.4 8.1L13.8 8.6L14.2 11.3L11.6 11.8Z', RUB, { shade: .5, lines: ['M12.2 8.3L12.4 11.6', 'M13 8.45L13.2 11.5'] });
        a.shape(ell(14.1, 9.95, .6, 1.5), BELL_BRASS, { shade: .4 });
        for (const y of [9.3, 9.95, 10.6]) a.flat(ell(14.15, y, .16), '#262a34');
        a.shape('M1.1 5L7 4.5L7 6.3L1.1 6.9Z', RUB, { outer: false, shade: .3 });
        a.shape(ell(9.2, 5.8, 2.2, 2.25), BELL_BRASS, { shade: .5, lite: 'M7.4 4.6C8 3.8 9 3.5 9.8 3.6L9.7 4C8.9 4 8.1 4.3 7.7 4.9Z' });
        port(a, GLASS_N, 'M7.9 5C8.6 4.3 9.8 4.3 10.5 5L10.4 5.4C9.7 4.8 8.7 4.8 8 5.4Z');
        a.flat(rr(11.2, 5.3, .8, .8, .15), BELL_BRASS.shade);
        a.shape(ell(12.4, 5.7, .8, 1.75), BELL_BRASS, { shade: .45 });
        port(a, GLASS_F);
        rustAt(a, 2.6, 10.6, .4);
      },
      glass: GLASS_N + GLASS_F, bars: [],
      helmetLit(paint) { paint(GLASS_N, WHITE, WHITE); paint(GLASS_F, WHITE, WHITE); },
    };
  })();

  // ================= В «Латы»: blackened hull plates riveted over a padded jack; a welded helm with a brow, a slit, two lenses deep in it and a grille for a jaw; a chalk stripe; the receiver a lamp in the pauldron =================
  const plates = (() => {
    const PLATE = mat('#4b5058', '#34383f', '#24272d', '#6a7079');
    const LAME = mat('#565b63', '#3c4047', '#2a2d33', '#757b84');
    const JACK = mat('#423b33', '#2e2a24', '#201d19', '#5a5249');
    const STRAP = mat('#3b2c22', '#2a1f18', '#1d1611', '#523e30');
    const CHALK = '#aaa38f';
    const PIP = ell(2.5, .6, .55, .52);
    const GLASS_N = ell(9.1, 6.15, .8, .78), GLASS_F = ell(11.9, 6.1, .42, .68);
    const HELM = 'M.8 11.2C.4 8 .5 5 1.2 3C2.2 1 4.4 .2 7 .1C9.6 0 11.6 .6 12.6 2.2L13.2 4.6L13.2 12.4L9.4 12.8C6.6 13.2 3.6 13 .8 12.6Z';
    const BREAST = 'M5.2 .6C7.4 .1 9.6 .3 10.8 1.2C11.6 3 11.6 6.4 11 8.6C9.4 9.2 7.2 9.2 5.4 8.8C5 6 4.9 3 5.2 .6Z';
    const quilted = (x0, x1, y0, y1, s) => { const l = []; for (let x = x0; x <= x1; x += s) l.push(`M${x} ${y0}L${x + .1} ${y1}`); return l; };
    return {
      id: 'plates', title: 'Латы', glow: [255, 92, 60],
      tag: 'Обшивка, снятая с корпусов, воронёная и склёпанная в латы поверх стёганого поддоспешника: нагрудник, юбка из пластин, набедренники, наколенники, поножи и большой наплечник. Сварной шлем с козырьком, узкой щелью и двумя линзами в глубине, решётка вместо челюсти, меловая полоса. Приёмник — фонарь в наплечнике.',
      suit: {
        torso(a) {
          a.shape(BACK_TORSO, JACK, { deep: torsoDeep, lines: quilted(2, 10.4, 1, 14.6, 1.4) });
          a.shape('M.9 1.2C2 .4 3.6 .2 4.6 .5L4.6 8.6C3.2 8.9 1.8 8.8 1 8.4C.7 6 .6 3.4 .9 1.2Z', PLATE, { shade: .7, lite: 'M1.2 1.2C2.2 .7 3.4 .6 4.4 .7L4.4 1.2C3.4 1.1 2.2 1.3 1.3 1.8Z' });
          a.shape(BREAST, PLATE, { shade: .9, lite: 'M5.6 .8C7.4 .4 9.4 .6 10.6 1.4L10.5 1.9C9.2 1.2 7.4 1 5.6 1.4Z', lines: ['M8.6 .5C9.6 3 9.8 6 9.4 8.9'] });
          a.flat('M5.2 3.6L11.6 1.9L11.7 3.1L5.2 4.9Z', CHALK, { clip: BREAST });
          for (const [x, y] of [[5.8, 1.2], [5.9, 8.2], [10.8, 2.2], [10.6, 8.1], [1.4, 1.6], [4.1, 8], [1.4, 7.8]]) rivet(a, x, y);
          a.shape('M1 9C4 9.6 8 9.6 11.2 9L11.5 11.4C8 12 4 12 .8 11.4Z', LAME, { shade: .45 });
          a.shape('M.6 11.1C4 11.7 8 11.7 11.7 11.1L12 13.7C8 14.3 4 14.3 .4 13.7Z', LAME, { shade: .5, deep: 'M0 13.2C4 13.8 8 13.8 12.4 13.2L12.4 15L0 15Z' });
          for (const y of [10.2, 12.4]) { rivet(a, 1.4, y); rivet(a, 10.8, y); }
          rustAt(a, 10.9, 6.4, .45); rustAt(a, 2, 12.8, .4);
        },
        pack(a) {
          a.shape(rr(1.4, -1.2, 1.4, 1.4, .2), LAME, { shade: .35 });
          a.shape(rr(.2, .1, 4, 11.8, .5), PLATE, { shade: .7, deep: band(0, 10.8, 5, 12.4), lite: band(.6, .3, 3.8, .8), lines: ['M.3 6L4.1 6'] });
          a.flat(band(.2, 2.2, 4.2, 3), CHALK);
          for (const y of [1.2, 4.6, 7.4, 10.8]) { rivet(a, .8, y, .25); rivet(a, 3.6, y, .25); }
          rustAt(a, 2.2, 8.8, .5);
        },
        upperArm(a) {
          a.shape(limb(4.2, 3.8, 2.5, 2.5, 8.6), JACK, { lines: quilted(1.1, 4, 4, 9.6, 1.2) });
          a.shape('M-.8 4.6C.8 5.6 4.2 5.6 5.8 4.6L5.6 6.4C4 7.2 1 7.2 -.6 6.4Z', LAME, { shade: .4 });
          a.shape('M-1 3C.6 3.9 4.4 3.9 6 3L5.9 4.8C4.2 5.7 .8 5.7 -.9 4.8Z', LAME, { shade: .45 });
          a.shape('M-1.4 3.2C-1.4 .4 .6 -1.4 2.5 -1.4C4.4 -1.4 6.4 .4 6.4 3.2C5 4 0 4 -1.4 3.2Z', PLATE, { shade: .7, lite: 'M-.6 1.2C0 0 1.2 -.8 2.5 -.9L2.4 -.4C1.4 -.3 .4 .4 -.1 1.4Z' });
          a.flat('M-1.4 2.3C0 2.9 5 2.9 6.4 2.3L6.4 3.1C5 3.7 0 3.7 -1.4 3.1Z', CHALK, { clip: 'M-1.4 3.2C-1.4 .4 .6 -1.4 2.5 -1.4C4.4 -1.4 6.4 .4 6.4 3.2C5 4 0 4 -1.4 3.2Z' });
          a.shape(ell(2.5, .6, .95, .9), BELL_BRASS, { shade: .35 });
          a.flat(PIP, UNLIT);
          for (const x of [-.6, 5.6]) rivet(a, x, 2.4, .26);
        },
        forearm(a) {
          a.shape(limb(3.8, 3.4, 2.5, 2.9, 6.6), JACK, { lines: quilted(1.2, 4, 1, 3, 1.2) });
          glove(a, STRAP);
          a.shape('M.8 7.1L4 7.1L4.2 8.5L.6 8.7Z', LAME, { outer: false, shade: .3 });
          a.shape('M.1 2.9L4.9 2.7L5 6.8L0 7Z', PLATE, { shade: .5, lite: band(.2, 2.8, 4.8, 3.3), lines: ['M3.4 2.9L3.5 6.8'] });
          rivet(a, .6, 3.6, .24); rivet(a, .6, 6.2, .24);
        },
        thigh(a) {
          a.shape(limb(4.8, 3.8, 3, 2.5, 11.3), JACK, { deep: 'M-1 11.4L7 11.4L7 15L-1 15Z', lines: quilted(1.6, 4.6, 8, 11.6, 1.4) });
          a.shape('M-.2 7.2C1.6 8 4.4 8 6.2 7.2L6 9.2C4.2 9.9 1.8 9.9 0 9.2Z', LAME, { shade: .45 });
          a.shape('M-.6 .6L6.6 .6L6.4 7.6C4.6 8.4 1.4 8.4 -.4 7.6Z', PLATE, { shade: .7, lite: band(-.4, .7, 6.4, 1.3), lines: ['M3.2 .8L3.1 7.8'] });
          rivet(a, .3, 1.6, .26); rivet(a, 5.7, 1.6, .26);
          a.shape(ell(3.4, 11.5, 2.3, 1.75), PLATE, { shade: .6, lite: 'M1.6 10.6C2.4 10 4.4 10 5.2 10.6L5 11C4.2 10.5 2.6 10.5 1.8 11Z' });
          rivet(a, 3.4, 11.5, .3);
        },
        shin(a) {
          a.shape(limb(4, 3.6, 3, 2.18, 6.6), JACK);
          boot(a, STRAP, 6.3, ['M1 9.2L4 9.2']);
          a.shape('M5.6 8.3C6.8 8.3 7.9 8.7 8.4 9.5L8.5 10.1L5.3 10.1Z', LAME, { outer: false, shade: .3 });
          a.shape('M1.8 .2C3.4 -.4 5.4 0 5.8 1L5.4 6.8L2.2 7C1.6 4.4 1.4 2 1.8 .2Z', PLATE, { shade: .6, lite: band(2, -.4, 5.8, .5), lines: ['M4.6 .4L4.4 6.8'] });
          rivet(a, 2.6, 1.2, .24); rivet(a, 2.8, 6, .24);
          rustAt(a, 3.6, 5.2, .35);
        },
      },
      lit: { upperArm(paint) { paint(PIP, WHITE, WHITE); } },
      helmet(a) {
        a.shape('M.6 10.8L5.4 11.6L5.2 13.6L-.4 13.2Z', LAME, { shade: .4 });
        a.shape(HELM, PLATE, { shade: .9, deep: 'M0 11.6C4 12.4 9 12.4 14 11.6L14 14L0 14Z', lite: 'M2 2.8C3.2 1.2 5.4 .5 7.4 .5L7.3 1C5.6 1.1 3.8 1.7 2.6 3.2Z', lines: ['M7.3 .3C6.8 4 6.8 8 7.2 12.8'] });
        a.flat('M4.6 .2L6.2 .1L6.4 12.9L4.8 13Z', CHALK, { clip: HELM });
        a.shape('M1.6 3.2C3 .6 6.6 -1.2 11.2 -.4L11.4 .6C7.2 -.2 4 1.2 2.4 3.6Z', LAME, { shade: .25 });
        a.flat('M7.4 5.2L13.3 4.9L13.3 7.2L7.6 7.4Z', UNLIT);
        a.shape(ell(9.1, 6.15, 1.05, 1), BELL_BRASS, { outer: false, shade: .3 });
        port(a, GLASS_N);
        a.shape(ell(11.9, 6.1, .6, .88), BELL_BRASS, { outer: false, shade: .3 });
        port(a, GLASS_F);
        a.shape('M6.8 3.8L13.7 3.4L13.8 5.1L6.9 5.4Z', LAME, { shade: .3, lite: band(6.8, 3.5, 13.7, 3.9) });
        a.shape('M8.6 7.7L13.4 7.4L13.6 10.6C12.6 11.8 10.4 12.2 8.8 11.8Z', LAME, { shade: .6 });
        for (const x of [9.6, 10.5, 11.4, 12.3]) a.flat(rr(x, 8.3, .42, 2.7, .15), UNLIT);
        for (const y of [4.2, 8.6, 11]) rivet(a, 7.6, y, .26);
        rivet(a, 13.2, 4.3, .24);
        rustAt(a, 2.2, 9, .5); rustAt(a, 12.6, 11, .4);
      },
      glass: GLASS_N + GLASS_F, bars: [],
      helmetLit(paint) { paint(GLASS_N, WHITE, WHITE); paint(GLASS_F, WHITE, WHITE); },
    };
  })();

  // ================= Г «Клюв»: a leather shoulder cape over a laced jerkin, a belt of nail cartridges, slim canvas legs, laced boots; a hood and a rubber beak mask with small goggles; the receiver a direction-finding loop on the shoulder =================
  const beak = (() => {
    const HOOD = mat('#4c3a2d', '#36291f', '#261c16', '#66513f');
    const MASK = mat('#5b5e55', '#42453d', '#2f312b', '#75796c');
    const JERK = mat('#3a3330', '#2a2523', '#1d1a18', '#504843');
    const TROU = mat('#4a4740', '#35332e', '#252420', '#615e55');
    const BELT = mat('#232020', '#181616', '#100f0f', '#383433');
    const SHELL = '#a88a4e';
    const PIP = ell(4, .5, .38);
    const GLASS_N = ell(8.8, 5.9, 1.05, 1.1), GLASS_F = ell(11.55, 5.75, .35, .9);
    const CAPE = 'M1.2 -.6C3.6 -1.6 8.4 -1.6 10.8 -.6C12.2 .4 13 2.4 13.2 4.6L13.4 7.4L12 7.9L11 7.3L9.6 8L8 7.4L6.4 8.1L4.8 7.4L3.4 8.1L2 7.4L.4 7.9L-.8 7.3L-.6 4.6C-.4 2.4 .2 .4 1.2 -.6Z';
    const SHOULDER = 'M-1 .6C-.2 -.6 1.2 -1.4 2.5 -1.4C3.8 -1.4 5.2 -.6 6 .6L6.2 5.6L5 6.1L3.8 5.6L2.4 6.2L1 5.6L-.2 6.1L-1.2 5.6Z';
    return {
      id: 'beak', title: 'Клюв', glow: [196, 226, 255],
      tag: 'Короткая кожаная пелерина на плечах поверх зашнурованной куртки, пояс-патронташ с гвоздевыми патронами, узкие брезентовые штаны и высокие шнурованные сапоги. Кожаный капюшон, резиновая маска с длинным гофрированным клювом-фильтром и маленькими круглыми очками. Приёмник — рамочная пеленгаторная антенна на плече.',
      suit: {
        torso(a) {
          a.shape('M3 .4C4.2 -.1 7.8 -.1 9 .4C10.4 .9 11 2 11 3.6L10.6 8.4C10.5 10 10.8 11.8 10.8 13.6C10.3 15 9.2 15.6 7.6 15.6L4.4 15.6C2.8 15.6 1.7 15 1.2 13.6C1.2 11.8 1.5 10 1.4 8.4L1 3.6C1 2 1.6 .9 3 .4Z', JERK, { deep: 'M-1 13C3 14 9 14 13 12.8L13 17L-1 17Z', ...folds([2.2, 11.4, 5, 12.2, .7], [10, 11.6, 7.4, 12.4, .7]) });
          a.line('M10 1.6L10.8 2.4L10 3.2L10.8 4L10 4.8L10.8 5.6L10 6.4L10.8 7.2L10 8', { w: .9 });
          a.shape('M.8 8.8C4 9.4 8 9.4 11.2 8.8L11.2 11C8 11.6 4 11.6 .8 11Z', BELT, { shade: .3 });
          for (let x = 1.6; x < 10.8; x += 1.05) { a.flat(rr(x, 8.95 + (x - 6) * (x - 6) * .012, .62, 1.5, .25), SHELL); a.flat(rr(x + .1, 9.9 + (x - 6) * (x - 6) * .012, .42, .5, .1), '#6e5636'); }
          a.shape(rr(4.6, 10.6, 2.4, 2.2, .3), BELT, { shade: .4, lines: ['M4.8 11.4L6.8 11.4'] });
          a.shape(CAPE, HOOD, { shade: .6, deep: 'M-1 6.6L14 6.6L14 9L-1 9Z', lite: 'M1.6 -.2C4 -1 8 -1 10.4 -.2L10.2 .4C8 -.3 4 -.3 1.8 .4Z',
            lines: ['M3.6 .2C3 2.4 2.6 4.8 2.6 7.4', 'M9.4 .2C10.2 2.4 10.8 4.8 11 7.2', 'M6.2 .1C6.2 2.6 6.3 5 6.4 7.8'] });
          a.line('M.6 7.6L.9 7.1M12.3 7.7L12 7.2', { w: .8 });
        },
        pack(a) {
          a.shape(rr(1.5, -1.2, 1.2, 1.4, .3), BELL_BRASS, { shade: .35 });
          a.shape(rr(.4, .1, 3.6, 11.8, 1.5), HOOD, { shade: .7, deep: band(0, 10.6, 5, 12.4), lines: ['M.6 3.6L3.8 3.6', 'M.6 4.2L3.8 4.2', 'M.6 8L3.8 8'] });
          for (const y of [1.6, 9.2]) { a.shape(rr(-.1, y, 4.2, 1.1, .2), BELT, { shade: .3 }); a.flat(rr(2.8, y + .15, .8, .8, .1), BELL_BRASS.base); }
        },
        upperArm(a) {
          a.shape(limb(4, 3.6, 2.5, 2.5, 8.6), JERK, folds([.9, 6.6, 3.9, 7.2, .7]));
          a.tube('M2.2 -.6L1.9 -1.7', .34, BELL_BRASS, { spec: false });
          a.tube(ell(1.75, -2.05, .62, .7), .26, BELL_BRASS, { spec: false });
          a.shape(SHOULDER, HOOD, { shade: .5, deep: 'M-2 5.2L7 5.2L7 7L-2 7Z', lite: 'M-.4 .4C.4 -.5 1.4 -1 2.5 -1L2.5 -.6C1.6 -.5 .6 0 -.1 .8Z', lines: ['M1.2 -1C.8 1.4 .8 3.6 1 5.6', 'M3.8 -1C4.4 1.4 4.4 3.6 4.2 5.4'] });
          a.shape(ell(4, .5, .62), BELL_BRASS, { shade: .3 });
          a.flat(PIP, UNLIT);
        },
        forearm(a) {
          a.shape(limb(3.6, 3.2, 2.5, 2.9, 6.6), JERK, folds([1, 3.4, 3.8, 3.9, .5]));
          a.shape('M.4 3.8L4.6 3.6L4.7 6.8L.3 7Z', HOOD, { shade: .35, lines: ['M2.2 3.9L2.9 4.6L2.2 5.3L2.9 6L2.2 6.7'] });
          glove(a, BELT);
        },
        thigh(a) {
          a.shape(limb(4.4, 3.4, 3, 2.5, 11.3), TROU, { deep: 'M-1 11.4L7 11.4L7 15L-1 15Z', ...folds([1.2, 4, 4.6, 3.4, .7], [1.2, 9.2, 4.6, 10, .9]) });
          a.shape('M3.4 4.4L5.6 4.6L5.4 7.4L3.4 7.2Z', TROU, { shade: .35, lines: ['M3.5 5.2L5.5 5.4'] });
          a.line('M1.8 6.4L2.4 7.2', { w: .9 });
        },
        shin(a) {
          a.shape(limb(3.8, 3.4, 3, 2.18, 6.6), TROU, folds([1.2, 2.6, 4.8, 3, .5]));
          tallBoot(a, HOOD, 3.4, ['M1 9.2L4 9.2'], 8.4);
          a.line('M4.3 3.8L4.8 4.3L4.3 4.8L4.8 5.3L4.3 5.8L4.8 6.3', { w: .8 });
        },
      },
      lit: { upperArm(paint) { paint(PIP, WHITE, WHITE); } },
      helmet(a) {
        a.shape('M1 11.6C.4 9 .4 6 1.2 3.8C2.2 1.4 4.6 .2 7.2 .2C9.6 .2 11.2 1.2 11.8 3L11.4 4.2C9.6 3.8 8 4.2 7.4 5.4L7 9.6C7 11 7.4 12.2 8 13L3.6 13.4C2.4 12.9 1.4 12.4 1 11.6Z', HOOD,
          { shade: .9, deep: 'M0 11.6L9 11.6L9 14L0 14Z', lite: 'M2.4 2.8C3.6 1.2 5.6 .6 7.6 .6L7.5 1.1C5.8 1.2 4 1.8 3 3.2Z', lines: ['M7.2 .3C4.6 1.6 3.2 4 2.8 7', 'M7.2 .3C6.4 2 6.2 3.4 6.4 4.6'] });
        a.shape('M7.4 3.8C9.2 3.4 11.2 3.6 12.4 4.4C12.9 5.6 12.9 7 12.6 8L12.8 10.8C12.2 11.8 10.4 12.3 9 12.1C8 11.9 7.4 11.3 7.1 10.6C6.8 8.6 6.8 5.6 7.4 3.8Z', MASK, { shade: .6 });
        a.shape('M7.3 4.6L4 5.6L4 6.6L7.3 5.8Z', BELT, { outer: false, shade: .2 });
        a.shape(rr(4.6, 5.2, 1, 1.2, .2), BELL_BRASS, { outer: false, shade: .3 });
        a.shape('M11.2 6.6C12.6 6.4 14 7 15.1 8.2L15.6 9C15.7 9.6 15.4 9.9 14.9 9.9C13.8 10.1 12.4 10.5 11.4 11Z', MASK,
          { shade: .55, lite: 'M11.4 6.8C12.6 6.7 13.8 7.2 14.8 8.2L14.6 8.4C13.6 7.6 12.6 7.2 11.5 7.2Z', lines: ['M12.2 6.7L12.3 10.6', 'M13 6.9L13.1 10.3', 'M13.8 7.3L13.9 10.1', 'M14.5 7.8L14.6 9.9'] });
        a.shape(ell(15.05, 9.15, .5, .78), BELL_BRASS, { shade: .35 });
        a.flat(ell(15.15, 9.2, .2, .4), '#262a34');
        a.shape(ell(8.8, 5.9, 1.55, 1.6), BELL_BRASS, { shade: .45 });
        port(a, GLASS_N, 'M7.9 5.3C8.4 4.9 9.2 4.9 9.7 5.3L9.6 5.6C9.1 5.3 8.5 5.3 8 5.6Z');
        a.shape(ell(11.55, 5.75, .58, 1.25), BELL_BRASS, { shade: .4 });
        port(a, GLASS_F);
        a.flat(ell(9.6, 10.6, .8, .6), MASK.deep);
        rustAt(a, 12.4, 10.6, .35);
      },
      glass: GLASS_N + GLASS_F, bars: [],
      helmetLit(paint) { paint(GLASS_N, WHITE, WHITE); paint(GLASS_F, WHITE, WHITE); },
    };
  })();


  return [stalker, harness, plates, beak];
}

/** Each outfit's body and helmet, as Figure wears them. */
const IDS = { stalker: ['catcherCoat', 'catcherHood'], harness: ['catcherHarness', 'catcherCap'], plates: ['catcherPlates', 'catcherHelm'], beak: ['catcherCape', 'catcherBeak'] };
const BOX = { torso: [12, 15], pack: [4, 12], upperArm: [5, 9], forearm: [5, 10], thigh: [6, 12], shin: [9, 11] };

Subjects.catchergear = function () {
  const { part, lightPicture, cut } = window.HeroKit;
  for (const c of outfits()) {
    const [body, helmet] = IDS[c.id];
    for (const [name, [w, h]] of Object.entries(BOX)) {
      out(`hero_${body}_${name}`, part({ w, h, draw: c.suit[name] }));
      if (c.lit[name]) out(`hero_${body}_${name}_lit`, lightPicture(w, h, c.lit[name]));
    }
    const [shell, visor] = cut(c.helmet, c.glass, c.bars);
    out(`hero_${helmet}_shell`, shell);
    out(`hero_${helmet}_visor`, visor);
    out(`hero_${helmet}_lit`, lightPicture(13, 12, c.helmetLit));
  }
};
})();
