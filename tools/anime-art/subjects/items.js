// The hotbar's items: round 3's motor, transceiver, accumulator and drone; the crystal and the ore drawn again; iron, scrap, biomass and the circuit new.
(function () {
'use strict';
const { ell, rr, poly, star, bake } = Anime;

// ---------- materials: shadows shift hue instead of going black ----------
const M = {
  steel: { base: '#b9c2ce', shade: '#6d7798', deep: '#474e72', lite: '#e4e9f0', spec: '#ffffff', rim: '#86d2e8', line: '#3a4163' },
  gun: { base: '#646d7d', shade: '#3b4060', deep: '#272b47', lite: '#8e99ad', spec: '#e6eefa', rim: '#5fb2d0', line: '#1f2240' },
  dark: { base: '#3a404e', shade: '#242842', deep: '#171a30', lite: '#5a6276', spec: '#cdd8e8', rim: '#4f9cbc', line: '#12142a' },
  copper: { base: '#e0914f', shade: '#a44a3c', deep: '#6f2b34', lite: '#fbc98f', spec: '#fff2d6', rim: '#ffb678', line: '#5c2420' },
  blue: { base: '#6f88cf', shade: '#3f4a9a', deep: '#2b2f70', lite: '#a9bff4', spec: '#ffffff', rim: '#a5d6ff', line: '#23286a' },
  glass: { base: '#2e2458', shade: '#191040', deep: '#100a2c', lite: '#4c3e8c', spec: '#d6c9ff', rim: '#7d5eff', line: '#120c2c' },
  red: { base: '#ff5a48', shade: '#b02a3a', deep: '#7a1a30', lite: '#ffa494', spec: '#ffffff', rim: '#ff9a70', line: '#5e1420' },
  plate: { base: '#d39258', shade: '#8f5238', lite: '#f0c08c', spec: '#fff2d6', rim: '#e8a870', line: '#4e2a18' },
  ore: { base: '#bb6c53', shade: '#7a3b42', deep: '#54263a', lite: '#de9f8a', spec: '#ffe2d4', rim: '#e88a60', line: '#431b22' },
  metal: { base: '#c8d0da', shade: '#7f8aa8', lite: '#f0f4f8', spec: '#ffffff', rim: '#c8d0da', line: '#3a4163' },
  amber: { base: '#ffbe5a', shade: '#d0663a', deep: '#a03e38', lite: '#ffe9a8', spec: '#ffffff', rim: '#ff9a50', line: '#7a3216' },
  pink: { base: '#ffa8a8', shade: '#c4507a', deep: '#8e2e62', lite: '#ffe0dc', spec: '#ffffff', rim: '#ff80a0', line: '#74203f' },
  frost: { base: '#d8f2ff', shade: '#7fa8e8', deep: '#5574c4', lite: '#ffffff', spec: '#ffffff', rim: '#a8d8ff', line: '#28508e' },
  slab: { base: '#c9c5bb', shade: '#6d6a78', deep: '#474556', lite: '#ebe7dd', spec: '#ffffff', rim: '#9ab0c4', line: '#2c2a36' },
  blur: { base: '#8ea2b8', shade: '#8ea2b8', lite: '#c8d6e4', spec: '#e8f2fa', rim: '#8ea2b8', line: '#2a3050' },
};

/** Bolts and rivets: a small round head with a lit corner. */
function bolt(a, x, y, r, m) {
  a.shape(ell(x, y, r), m || M.steel, { outer: false, shade: r * 0.45, spec: ell(x - r * 0.3, y - r * 0.35, r * 0.32) });
}

// ---------- the pictures, in units of a tenth of a game pixel; light from the upper left ----------
const PICS = [
  { id: 'motor', name: 'Мотор',
    draw(a) {
      // The base plate with its two feet, in front of nothing, behind the barrel.
      a.shape('M22 118L120 114L136 130L136 146Q136 152 130 152L18 152Q12 152 12 146L12 130Z', M.gun, {
        lite: 'M12 130L136 130L120 114L22 118Z', deep: 'M12 144L136 144L136 154L12 154Z', rim: 2,
        lines: ['M60 130L60 152', 'M88 130L88 152'] });
      bolt(a, 26, 138, 5); bolt(a, 122, 138, 5);
      a.flat(poly(64, 134, 84, 134, 84, 142, 64, 142), '#e0a030', { only: 'cel' });
      a.line('M66 134L72 142M72 134L78 142M78 134L84 142', { c: '#171a30', w: 1.2, only: 'cel' });
      // Terminal box on top, with its cable gland.
      a.shape('M50 34L50 16Q50 10 56 10L90 10Q94 10 97 12L106 20Q108 22 108 26L108 36Z', M.gun, {
        lite: 'M50 16Q50 10 56 10L90 10Q94 10 97 12L106 20L60 20Q54 20 50 24Z', deep: 'M96 20L110 20L110 38L96 38Z',
        lines: ['M60 20L60 34', 'M96 20L96 34'], rim: 1.5 });
      bolt(a, 69, 28, 3.4, M.copper); bolt(a, 82, 28, 3.4, M.copper);
      // The barrel: a finned cylinder coming toward us on the right.
      const barrel = 'M24 34L106 28C124 28 136 50 136 78C136 106 124 128 106 128L24 122C12 120 5 100 5 78C5 56 12 36 24 34Z';
      const fins = [];
      for (const x of [18, 28, 38, 48, 58, 68, 78, 88, 98]) fins.push(`M${x} ${35 - (x - 18) * 0.06}C${x - 11} 52 ${x - 11} 104 ${x} ${121 + (x - 18) * 0.06}L${x + 3.6} ${121.3 + (x - 18) * 0.06}C${x - 7} 104 ${x - 7} 52 ${x + 3.6} ${35 - (x - 18) * 0.06}Z`);
      a.shape(barrel, M.steel, {
        shade: ['M2 86C30 100 72 108 106 106C118 105 128 98 140 86L140 132L2 132Z', ...fins],
        deep: 'M2 110C30 118 72 124 106 122C118 121 126 116 140 106L140 132L2 132Z',
        lite: 'M12 52C36 44 74 40 104 38L104 30L20 34Z',
        spec: ['M14 50C36 44 54 41 76 39L76 44C54 46 38 49 16 56Z', 'M84 38L100 37L100 42L84 43Z'],
        rim: 2.4 });
      // The cut-away window: copper windings either side of a blue rotor.
      a.shape('M34 56L92 53Q98 53 98 59L98 96Q98 102 92 102L34 100Q28 100 28 94L28 62Q28 56 34 56Z', M.dark, { deep: 'M26 86L100 88L100 104L26 104Z', outer: false });
      const windings = (x0, x1, top) => {
        const d = `M${x0 + 3} ${top}L${x1 - 3} ${top - 0.8}Q${x1} ${top - 0.8} ${x1} ${top + 3}L${x1} ${top + 32}Q${x1} ${top + 36} ${x1 - 3} ${top + 36}L${x0 + 3} ${top + 36.6}Q${x0} ${top + 36.6} ${x0} ${top + 33}L${x0} ${top + 4}Q${x0} ${top} ${x0 + 3} ${top}Z`;
        const ls = [];
        for (let x = x0 + 3; x < x1 - 1; x += 3.4) ls.push(`M${x} ${top - 1}C${x + 2.4} ${top + 10} ${x + 2.4} ${top + 26} ${x} ${top + 37}`);
        a.shape(d, M.copper, { shade: `M${x0 - 1} ${top + 25}L${x1 + 1} ${top + 25}L${x1 + 1} ${top + 40}L${x0 - 1} ${top + 40}Z`, lite: `M${x0} ${top - 1}L${x1} ${top - 1}L${x1} ${top + 6}L${x0} ${top + 7}Z`, spec: `M${x0 + 2} ${top + 2}L${x1 - 2} ${top + 1.6}L${x1 - 2} ${top + 4}L${x0 + 2} ${top + 4.4}Z`, lines: ls, outer: false });
      };
      windings(33, 53, 60); windings(73, 93, 59);
      a.shape(ell(63, 78, 9, 17), M.blue, { shade: 3, lite: ell(60.5, 72, 4, 7), spec: ell(60, 70, 1.7, 3.4), outer: false });
      a.shape(ell(63, 78, 3, 5), M.dark, { outer: false });
      // End shield, bolted, then the shaft.
      a.shape(ell(112, 78, 24, 50), M.steel, {
        shade: 'M92 78C100 104 118 116 140 108L140 134L88 134Z', lite: 'M90 62C96 40 110 30 124 30L100 26L88 50Z',
        spec: 'M100 42C106 34 112 31 118 31L116 35C110 36 106 40 103 46Z', rim: 2.4 });
      a.shape(ell(114, 78, 15, 33), M.gun, { shade: 2.4, outer: false });
      for (const [x, y] of [[112, 37], [130, 62], [130, 94], [112, 119], [97, 94], [97, 62]]) bolt(a, x, y, 2.8);
      a.shape('M116 66L150 62C155 62 158 70 158 78C158 86 155 94 150 94L116 90Z', M.steel, {
        shade: 'M114 82L158 82L158 96L114 96Z', spec: 'M120 70L150 66L150 70L120 74Z', rim: 1.6, lines: ['M134 64L134 92'] });
      a.shape(ell(152, 78, 5.4, 15.4), M.gun, { lite: ell(151, 75, 3, 9), outer: false });
    } },

  { id: 'transceiver', name: 'Приёмопередатчик',
    draw(a) {
      // Signal first, behind the whip.
      a.tube('M112 4C104 12 104 26 112 34', 4, M.blue, { bare: true, spec: false });
      a.tube('M152 4C160 12 160 26 152 34', 4, M.blue, { bare: true, spec: false });
      a.tube('M101 0C91 14 91 30 101 42', 3, M.blue, { bare: true, spec: false, only: 'cel' });
      a.glow('M100 2C90 14 90 30 100 40L102 38C94 28 94 16 102 4Z', '#7fc4f0', { r: 5, only: 'soft' });
      // Carrying handle.
      a.tube('M30 62C30 44 36 38 52 38L74 38C86 38 92 44 92 56', 6, M.gun);
      // The case: top, side, front, chamfered.
      a.shape('M16 70L44 50Q48 46 54 46L136 44Q148 44 146 50L112 68Q108 72 102 72L20 74Q12 74 16 70Z', M.gun, { lite: 'M16 70L44 50Q48 46 54 46L136 44L104 62L24 66Z', outer: true });
      a.shape('M108 72L142 50Q148 46 148 54L148 126Q148 132 142 136L112 154Q106 156 106 150L106 76Q106 74 108 72Z', M.dark, {
        deep: 'M104 120L150 96L150 158L104 158Z', rim: 2.2,
        lines: ['M114 86L140 68', 'M114 94L140 76', 'M114 102L140 84', 'M114 110L140 92', 'M114 118L140 100'] });
      a.shape('M18 72L100 70Q108 70 108 78L108 148Q108 156 100 156L18 156Q10 156 10 148L10 80Q10 72 18 72Z', M.gun, {
        shade: 'M8 136L110 136L110 158L8 158Z', lite: 'M12 80Q12 74 18 74L100 72Q106 72 106 78L106 80Z', spec: 'M16 77L60 76L60 78L16 79Z', rim: 2 });
      for (const [x, y] of [[17, 80], [101, 78], [17, 149], [101, 149]]) bolt(a, x, y, 2.6, M.steel);
      // The dial: a recessed copper scale with a red needle.
      a.shape(rr(19, 84, 58, 44, 5), M.dark, { outer: false, deep: 'M17 118L79 118L79 130L17 130Z' });
      a.shape('M24 88L72 88Q73 88 73 90L73 112Q60 104 48 104Q36 104 23 112L23 90Q23 88 24 88Z', M.copper, {
        lite: 'M23 89L73 89L73 94L23 94Z', shade: 'M23 104L73 104L73 114L23 114Z', outer: false,
        lines: ['M30 98L32 94', 'M38 94L39 90.5', 'M48 92L48 89', 'M58 94L57 90.5', 'M66 98L64 94'] });
      a.line('M30 106C38 100 58 100 66 106', { w: 0.8 });
      a.flat(poly(47, 108, 50, 108, 64, 91, 63, 90), '#ff5a48');
      a.flat(ell(48.5, 112, 4), '#23273a');
      a.flat(rr(23, 116, 50, 8, 1.6), '#17192c');
      a.flat(rr(26, 118, 16, 4, 1), '#7fe0a0', { only: 'cel' });
      a.glow(rr(26, 118, 16, 4, 1), '#7fe0a0', { r: 4, only: 'soft' });
      // The big tuning knob, its ticks, a small one below.
      a.shape(ell(92, 98, 12.5), M.steel, { shade: 'M78 100C84 108 94 112 106 104L106 114L78 114Z', spec: 'M84 90C87 87 91 86 95 86L94 89C91 89 88 91 86 93Z', rim: 1.6 });
      a.shape(ell(92, 98, 7), M.gun, { shade: 2, outer: false });
      a.line('M92 98L96.5 93', { w: 1.5, c: '#ffffff' });
      a.shape(ell(92, 124, 6), M.gun, { shade: 2, spec: ell(90, 122, 1.5) });
      // Lamps and a stencil.
      a.glow(ell(24, 144, 3.6), '#7fe0a0', { r: 5 });
      a.glow(ell(36, 144, 3.6), '#ffc850', { r: 5 });
      a.line('M50 141L72 141M50 147L68 147', { c: '#aeb8ca', w: 1.1 });
      // Whip antenna, sectioned, with its red tip.
      a.shape('M116 58L132 56L132 62Q124 66 116 63Z', M.gun, { shade: 1.5, outer: true });
      a.tube('M124 58L129 26', 6, M.steel);
      a.tube('M129 26L132 8', 3.6, M.steel);
      a.shape(ell(132.5, 8, 6.6), M.red, { shade: 2.2, spec: ell(130.4, 5.6, 1.9), rim: 1.2 });
      a.glow(ell(132.5, 8, 2), '#ffd0c0', { r: 9, only: 'soft' });
    } },

  { id: 'accumulator', name: 'Аккумулятор',
    draw(a) {
      // Lower cap, glass, charge, cage rods, upper cap, terminal: nearest last.
      a.shape('M24 118L136 118L136 134C136 148 110 156 80 156C50 156 24 148 24 134Z', M.copper, {
        shade: 'M104 118L138 118L138 158L92 158C100 148 104 134 104 118Z', deep: 'M22 138C40 150 120 150 138 138L138 160L22 160Z',
        spec: 'M34 126L46 126L46 142C40 140 36 138 34 136Z', rim: 2 });
      a.shape('M31 42L129 42L129 122C129 132 106 138 80 138C54 138 31 132 31 122Z', M.glass, {
        shade: 'M92 40L132 40L132 140L80 140C88 110 92 70 92 40Z', lite: 'M31 40L46 40C43 70 43 104 47 132L31 126Z',
        spec: ['M36 50L41 50C39 72 39 94 41 116L36 114C34 92 34 72 36 50Z', 'M114 56L119 58L119 84L114 84Z'], rim: 2.4 });
      a.flat('M38 118C50 128 110 128 122 118L122 122C110 134 50 134 38 122Z', '#4a3ba0', { clip: 'M31 42L129 42L129 122C129 132 106 138 80 138C54 138 31 132 31 122Z' });
      // The charge, the only thing that shines.
      a.glow('M90 50L58 92L77 92L66 130L104 80L84 80L100 50Z', '#b493ff', { r: 8, core: 'M86 60L70 86L84 86L78 112L94 86L78 86L88 60Z' });
      a.line('M90 50L58 92L77 92L66 130L104 80L84 80L100 50', { c: '#5a3cc0', w: 0.9, only: 'cel' });
      // Cage rods hold the caps together.
      for (const x of [40, 120]) a.tube(`M${x} 48L${x} 128`, 5.4, M.steel);
      a.shape('M22 28L138 28L138 46C138 60 110 68 80 68C50 68 22 60 22 46Z', M.copper, {
        shade: 'M104 40L140 28L140 70L92 70C100 60 104 50 104 40Z', deep: 'M20 52C40 62 120 62 140 52L140 72L20 72Z',
        spec: 'M32 44L46 48L46 60C40 58 36 56 32 52Z', rim: 2,
        lines: ['M22 40C42 52 118 52 138 40'] });
      a.shape(ell(80, 28, 58, 16), M.copper, { lite: 'M22 28C30 14 130 14 138 28C120 18 40 18 22 28Z', shade: 'M60 26C70 30 100 32 130 24L140 34L60 46Z', spec: 'M34 25C46 18 62 16 76 15L74 18C60 19 46 22 38 28Z', outer: false });
      for (const [x, y] of [[36, 29], [62, 38], [98, 38], [124, 29]]) bolt(a, x, y, 3, M.plate);
      a.shape('M60 8L100 8L100 24C100 30 92 34 80 34C68 34 60 30 60 24Z', M.steel, { shade: 'M86 6L102 6L102 36L80 36C84 28 86 16 86 6Z', spec: 'M66 12L72 12L72 28L66 26Z', rim: 1.6 });
      a.shape(ell(80, 8, 20, 6.4), M.steel, { lite: ell(78, 7, 15, 4), outer: false });
      a.flat(ell(80, 8, 9, 2.6), '#474e72');
      a.flat(star(148, 16, 9), '#e4d6ff');
      a.flat(star(10, 92, 7), '#e4d6ff');
      a.flat(star(150, 70, 5), '#b493ff');
    } },

  { id: 'drone', name: 'Дрон',
    draw(a) {
      // Far legs and skid, then the near ones after the body.
      a.tube('M96 112C104 124 108 132 112 140', 5, M.dark, { spec: false });
      a.tube('M100 142L148 142', 5, M.dark, { spec: false });
      // Rotor arms behind the mast, then discs seen edge on.
      a.tube('M72 50L28 26', 6, M.gun);
      a.tube('M88 50L134 24', 6, M.gun);
      a.shape('M66 44C66 34 72 30 80 30C88 30 94 34 94 44L94 56L66 56Z', M.gun, { shade: 2.4, spec: 'M71 38C72 35 75 34 78 34L78 37C76 37 74 38 73 40Z', rim: 1.4 });
      // Tail fin and stabiliser first: they give the body a front and a back.
      a.shape('M28 66L8 42Q6 38 12 38L24 40L50 56Z', M.gun, { shade: 2.2, spec: 'M14 42L22 43L34 54L28 54Z', rim: 1.4 });
      // The body: an upper shell over a dark belly, a blunt nose to the right.
      const shell = 'M14 86C16 66 40 46 80 45C112 44 142 56 152 80L150 90C118 100 52 102 14 92Z';
      a.shape('M16 88C50 100 118 98 150 86C150 100 134 116 96 118C58 120 18 114 16 88Z', M.dark, {
        deep: 'M14 106C50 118 110 116 150 98L150 124L14 124Z', rim: 2.2, lines: ['M40 96L44 114', 'M84 98L84 118'] });
      a.shape(shell, M.steel, {
        shade: 'M12 82C42 90 104 92 154 76L154 100L12 100Z',
        lite: 'M24 70C38 54 60 47 84 47C106 47 126 54 138 64C110 54 60 54 24 70Z',
        spec: ['M34 66C48 56 62 52 78 51L78 55C62 56 50 60 40 68Z', 'M88 51L110 54L108 57L88 55Z'],
        rim: 2,
        lines: ['M58 47C52 62 52 80 54 96', 'M100 46C108 58 112 76 110 94', 'M112 76C124 74 136 70 148 66'] });
      // Vents, a hatch with a stencil, a stripe along the shell.
      for (const x of [24, 32, 40]) a.flat(poly(x, 72, x + 4, 69, x + 6, 86, x + 2, 86), '#2b2f50');
      a.flat(poly(62, 58, 98, 60, 98, 67, 62, 65), '#3f6fb8');
      a.flat(poly(62, 69, 80, 70, 80, 73, 62, 72), '#17192c', { only: 'cel' });
      a.flat(poly(84, 70, 96, 70, 96, 73, 84, 73), '#17192c', { only: 'cel' });
      bolt(a, 60, 90, 2.4); bolt(a, 100, 89, 2.4);
      // Camera: a dark ball in a yoke under the nose.
      a.shape('M104 92C112 84 136 84 146 92L148 108C140 120 112 120 104 108Z', M.gun, { shade: 2.6, outer: true });
      a.shape(ell(127, 102, 17), M.dark, { deep: 'M108 106C116 116 136 118 146 108L146 122L108 122Z', spec: 'M116 92C118 89 122 87 126 87L125 90C122 90 120 92 119 95Z', rim: 1.8 });
      a.shape(ell(131, 102, 10), M.blue, { shade: 2.6, outer: false });
      a.glow(ell(132, 102, 6), '#3fa6d8', { r: 6, core: ell(129.6, 99, 2.4) });
      // Near leg and skid.
      a.tube('M46 114C42 126 36 136 32 144', 6, M.gun, { spec: false });
      a.tube('M8 146L60 146', 6, M.dark);
      bolt(a, 46, 114, 4);
      // Rotor discs: a spinning blur and one blade caught in it.
      for (const [x, y, w] of [[28, 22, 28], [134, 20, 26]]) {
        a.shape(ell(x, y, w, 6.4), M.blur, { lite: ell(x - 4, y - 1.6, w * 0.7, 3), outer: true });
        a.shape(poly(x - w * 0.9, y - 1, x - 3, y - 3, x + w * 0.9, y + 1, x + 3, y + 3), M.gun, { outer: false });
        a.shape(ell(x, y, 3.6, 2.4), M.dark, { outer: false });
      }
    } },

];

// Crystal: prisms with slanted faces meeting off-centre at their tips, no collar, leaning apart from one stone.
PICS.push({ id: 'crystal', draw(a) {
  a.shape('M10 128L38 118L122 116L150 124L156 136L144 154L22 156L4 140Z', M.slab, {
    lite: 'M10 128L38 118L122 116L150 124L156 136L4 140Z', deep: 'M4 148L156 146L156 158L4 158Z', shade: 'M104 138L156 136L144 154L92 156Z', rim: 2,
    lines: ['M42 140L50 156', 'M112 138L120 154'] });
  // Pink at the back, leaning left: a broad low prism.
  a.shape('M14 42L38 30L58 72L60 128L24 130L6 84Z', M.pink, {
    lite: 'M14 42L38 30L40 72L24 88L6 84Z', shade: 'M38 30L58 72L60 128L44 130L40 72Z', deep: 'M52 100L60 96L60 128L50 130Z', spec: 'M15 50L28 42L26 62L13 66Z',
    lines: ['M14 42L40 72L38 30', 'M40 72L44 130', 'M6 84L40 72'] });
  // Frost, behind on the right, two points.
  a.shape('M102 128L104 76L124 50L148 80L152 110L136 128Z', M.frost, {
    lite: 'M104 76L124 50L122 88L106 104Z', shade: 'M124 50L148 80L152 110L136 128L124 128L122 88Z', deep: 'M142 104L152 110L136 128L128 128Z', spec: 'M110 78L120 62L118 82L110 92Z',
    lines: ['M104 76L122 88L148 80', 'M124 50L122 88L124 128'] });
  // Amber in front, the largest, lightly tilted right; its tip is off its axis.
  a.shape('M54 130L46 64L60 34L92 4L108 46L116 84L108 130Z', M.amber, {
    lite: 'M60 34L92 4L78 58L66 132L54 130L46 64Z', shade: 'M92 4L108 46L116 84L108 130L84 130L78 58Z', deep: 'M100 92L116 84L108 130L92 130Z',
    spec: ['M62 40L82 18L74 50L60 62Z', 'M58 74L64 72L60 112L56 112Z'],
    lines: ['M46 64L78 58L108 46', 'M60 34L78 58L92 4', 'M78 58L84 130', 'M66 132L78 58'] });
  // Chips at the foot.
  a.shape('M30 134L36 116L48 134Z', M.frost, { shade: 'M36 116L48 134L40 134Z' });
  a.shape('M118 134L126 118L136 134Z', M.pink, { shade: 'M126 118L136 134L128 134Z' });
  a.flat(star(134, 34, 10), '#ffffff');
  a.flat(star(30, 16, 6), '#ffe0dc');
} });

// Ore: one big split boulder, a slab leaning behind it, one small chip in front: three builds, not three copies.
PICS.push({ id: 'ironOre', draw(a) {
  a.shape('M84 54L92 24L118 8L146 24L154 60L140 90L104 96L84 78Z', M.ore, {
    lite: 'M92 24L118 8L124 34L100 54L84 54Z', shade: 'M118 8L146 24L154 60L128 66L124 34Z', deep: 'M154 60L140 90L104 96L112 74L128 66Z',
    lines: ['M124 34L100 54L112 74L128 66L124 34', 'M112 74L104 96'] });
  a.shape('M6 110L16 72L46 44L84 40L112 64L124 104L106 142L60 154L20 146Z', M.ore, {
    lite: 'M16 72L46 44L84 40L70 74L34 92L6 110Z', shade: 'M84 40L112 64L124 104L92 112L70 74Z', deep: 'M124 104L106 142L60 154L66 126L92 112Z',
    spec: ['M22 74L44 54L50 58L28 80Z'], rim: 2.2, lines: ['M70 74L34 92L66 126L92 112L70 74', 'M34 92L6 110', 'M66 126L60 154', 'M84 40L70 74'] });
  a.shape('M112 132L124 112L144 110L156 128L150 150L122 156Z', M.ore, {
    lite: 'M124 112L144 110L138 124L118 128Z', shade: 'M144 110L156 128L150 150L136 140L138 124Z', lines: ['M138 124L118 128', 'M138 124L136 140'] });
  const vein = (d, shade, spec) => a.shape(d, M.metal, { shade, spec, outer: false });
  vein('M28 104L44 88L52 96L68 80L82 86L68 98L76 106L60 118L52 108L40 122L30 120Z', 'M52 96L82 86L68 98L76 106L60 118L52 108L40 122L30 120L44 104Z', 'M44 88L52 96L46 98Z');
  vein('M84 124L96 116L102 124L92 134Z', 'M96 116L102 124L92 134Z', 'M84 124L96 116L92 122Z');
  vein('M108 42L124 26L134 36L126 50L114 54Z', 'M134 36L126 50L114 54L124 38Z', 'M124 26L134 36L126 33Z');
  vein('M128 132L138 124L144 134L134 142Z', 'M138 124L144 134L134 142Z', 'M128 132L138 124L134 130Z');
  a.flat(star(48, 92, 9), '#ffffff');
  a.flat(star(126, 28, 7), '#ffffff');
} });

// Iron: two cast ingots stacked, their tops bevelled.
const INGOT = { base: '#a8b2c0', shade: '#616c8c', deep: '#434a6c', lite: '#d8e0ea', spec: '#ffffff', rim: '#86d2e8', line: '#3a4163' };
const ingot = (a, x, y) => {
  a.shape(`M${x} ${y + 44}L${x + 18} ${y}L${x + 88} ${y}L${x + 106} ${y + 44}L${x + 92} ${y + 58}L${x + 12} ${y + 58}Z`, INGOT, {
    lite: `M${x + 18} ${y}L${x + 88} ${y}L${x + 80} ${y + 22}L${x + 26} ${y + 22}Z`,
    shade: `M${x + 88} ${y}L${x + 106} ${y + 44}L${x + 92} ${y + 58}L${x + 80} ${y + 22}Z`,
    deep: `M${x} ${y + 44}L${x + 106} ${y + 44}L${x + 92} ${y + 58}L${x + 12} ${y + 58}Z`,
    spec: `M${x + 24} ${y + 5}L${x + 62} ${y + 5}L${x + 56} ${y + 12}L${x + 21} ${y + 12}Z`,
    lines: [`M${x + 26} ${y + 22}L${x + 80} ${y + 22}`, `M${x + 26} ${y + 22}L${x} ${y + 44}`, `M${x + 80} ${y + 22}L${x + 106} ${y + 44}`] });
};
PICS.push({ id: 'iron', draw(a) {
  ingot(a, 2, 88);
  ingot(a, 52, 88);
  ingot(a, 28, 36);
  a.flat(star(118, 34, 9), '#ffffff');
} });

// Scrap: torn plate, a bent bar and a bolt in a heap.
PICS.push({ id: 'scrap', draw(a) {
  a.tube('M18 110C40 74 60 66 96 60', 11, M.gun, { spec: false });
  a.shape('M40 74L64 40L104 26L132 58L120 76L136 96L104 118L84 100L58 112L46 96Z', M.steel, {
    lite: 'M64 40L104 26L100 56L68 66L40 74Z', shade: 'M104 26L132 58L120 76L136 96L104 118L100 56Z', deep: 'M136 96L104 118L84 100L106 90Z', spec: 'M70 44L96 34L92 42L68 52Z',
    lines: ['M68 66L100 56', 'M100 56L106 90', 'M68 66L58 112'] });
  a.shape(ell(70, 78, 4), M.dark, { outer: false }); a.shape(ell(94, 72, 4), M.dark, { outer: false });
  a.shape('M4 138L14 110L52 96L88 112L100 146L62 156L18 154Z', M.gun, {
    lite: 'M14 110L52 96L54 118L22 128L4 138Z', shade: 'M52 96L88 112L100 146L62 156L54 118Z', deep: 'M4 138L22 128L34 156L18 154Z', spec: 'M20 112L48 102L46 108L22 118Z', lines: ['M22 128L54 118L62 156'] });
  a.shape('M104 136L116 112L146 118L156 140L140 156L112 154Z', M.copper, {
    lite: 'M116 112L146 118L136 130L110 130Z', shade: 'M146 118L156 140L140 156L136 130Z', lines: ['M110 130L136 130', 'M136 130L140 156'] });
} });

// Biomass: a wet clump of leaves and stalks.
const LEAF = { base: '#62b04a', shade: '#2e7a4a', deep: '#1c5444', lite: '#a4dc66', spec: '#e2f8a8', rim: '#c4e870', line: '#123a2a', ink: '#0c1f18' };
const leaf = (a, d, shade, lite, line) => a.shape(d, LEAF, { shade, lite, lines: line });
PICS.push({ id: 'biomass', draw(a) {
  leaf(a, 'M80 8C110 16 128 40 124 70C104 78 74 64 68 40C66 28 70 16 80 8Z', 'M124 70C104 78 74 64 68 40C84 56 104 60 124 70Z', 'M80 8C92 12 104 24 108 38L82 26Z', 'M80 12C84 32 98 50 118 66');
  leaf(a, 'M24 40C50 28 76 40 86 66C74 86 42 88 26 70C18 60 18 48 24 40Z', 'M86 66C74 86 42 88 26 70C46 76 66 74 86 66Z', 'M24 40C40 34 58 40 66 50L36 54Z', 'M26 44C44 52 66 58 82 66');
  leaf(a, 'M70 80C100 64 136 74 150 104C136 128 100 136 78 116C70 108 66 92 70 80Z', 'M150 104C136 128 100 136 78 116C104 118 128 112 150 104Z', 'M70 80C90 72 112 76 124 88L84 96Z', 'M72 84C96 92 124 98 146 104');
  leaf(a, 'M10 98C34 76 66 82 82 108C74 134 42 150 18 134C8 126 4 110 10 98Z', 'M82 108C74 134 42 150 18 134C42 136 64 124 82 108Z', 'M10 98C26 88 44 88 56 96L22 110Z', 'M12 102C34 108 58 108 78 108');
  a.tube('M60 150C66 130 74 118 82 104', 5, { base: '#c4a060', shade: '#8a6a40', spec: '#f0d8a0' }, { spec: false });
  a.flat(ell(52, 100, 3), '#e2f8a8');
  a.flat(star(118, 50, 7), '#ffffff');
} });

// A circuit: a green board with traces, a chip and pins.
PICS.push({ id: 'circuit', draw(a) {
  const B = { base: '#2e8a68', shade: '#1a5a54', deep: '#123e40', lite: '#58bc88', spec: '#c8f4d0', rim: '#58bc88', line: '#0c2a2c' };
  a.shape('M10 32L150 22L154 128L14 140Z', B, {
    lite: 'M10 32L150 22L150 34L11 44Z', deep: 'M13 128L154 116L154 128L14 140Z', shade: 'M136 24L150 22L154 128L140 129Z' });
  for (const [d, w] of [['M24 60L50 60L62 72', 1], ['M24 96L44 96L58 84', 1], ['M108 56L134 54', 1], ['M108 92L136 90', 1], ['M86 118L86 130', 1], ['M64 118L64 132', 1]])
    a.tube(d, 3.4, { base: '#e8c060', shade: '#e8c060', spec: '#fff2c0' }, { bare: true, spec: false });
  a.shape(rr(58, 50, 52, 66, 3), M.dark, { lite: rr(60, 52, 48, 12, 2), spec: rr(64, 55, 18, 4, 1), shade: 'M100 50L110 50L110 116L100 116Z' });
  for (let i = 0; i < 4; i++) {
    a.flat(rr(52, 58 + i * 14, 7, 5, 1), '#d8dde6');
    a.flat(rr(109, 56 + i * 14, 7, 5, 1), '#d8dde6');
  }
  a.flat(ell(70, 104, 3), '#7fe0a0');
  a.shape(ell(30, 118, 8), M.copper, { lite: ell(28, 115, 4), shade: .22 });
  a.shape(rr(126, 104, 16, 16, 2), M.steel, { shade: .3 });
} });

// The one line, in these paths' units: a tenth of the 18 px the 64 px picture spans (a margin of one round the 16), so a world pixel is 11.25 of them.
Subjects.items = function () {
  for (const p of PICS) out('items/' + p.id, bake({ w: 16, h: 16, margin: 1, units: 10, px: 3.5555555, K: 4, light: [-0.6, -0.8],
    W: { outer: 0.9 * 11.25, edge: 0.26 * 11.25, panel: 0.14 * 11.25 }, draw: p.draw }));
};

})();
