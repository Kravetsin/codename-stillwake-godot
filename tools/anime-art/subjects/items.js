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
// Sulfur (the user's pick, 2026-11-08: bloom): a crumbly pale yellow lump, pores in its crust, on a sliver of the stone it grew on.
const SULFUR = { base: '#d8c66c', shade: '#aa9c58', deep: '#7c7244', lite: '#eee2a4', spec: '#f4ecc0', ink: '#29251a' };
const SULFUR_STONE = { base: '#5e6472', shade: '#434855', deep: '#2f3340', lite: '#747a88', spec: '#8e94a0', ink: '#0f111b' };
PICS.push({ id: 'sulfur', draw(a) {
  a.shape('M6 122L28 108L62 116L74 138L56 154L20 150Z', SULFUR_STONE, { lite: 'M6 122L28 108L62 116L40 124Z', shade: 'M62 116L74 138L56 154L46 132Z', lines: ['M40 124L46 132'] });
  a.shape('M18 108C14 88 28 72 48 70C56 52 78 44 98 50C118 44 142 58 144 80C156 90 156 114 142 126C130 142 100 146 78 142C52 148 24 134 18 108Z', SULFUR, {
    lite: 'M18 108C14 88 28 72 48 70C56 52 78 44 98 50C86 62 70 70 56 84C42 92 28 100 18 108Z',
    shade: 'M144 80C156 90 156 114 142 126C130 142 100 146 78 142C96 132 120 120 130 100Z',
    deep: 'M142 126C130 142 100 146 78 142C100 140 124 132 136 120Z',
    lines: ['M60 96L72 106L68 120', 'M104 78L112 92', 'M120 110L110 124'] });
  // Pores in the crust, a lit lip on the far side of each.
  for (const [x, y, rx] of [[82, 88, 7], [118, 104, 5], [52, 112, 4], [96, 124, 4]]) {
    a.flat(ell(x, y, rx, rx * .7), SULFUR.deep);
    a.line(`M${x - rx * .7} ${y + rx * .5}C${x} ${y + rx * .9} ${x + rx * .6} ${y + rx * .6} ${x + rx} ${y}`, { c: SULFUR.lite, w: 1.3 });
  }
  for (const [x, y, s] of [[40, 86, 2.6], [70, 64, 2.2], [108, 62, 2.4], [132, 88, 2], [88, 106, 2.2], [62, 132, 2]]) a.flat(ell(x, y, s), SULFUR.spec);
  a.shape('M124 146L132 138L142 142L138 152Z', SULFUR, { shade: 'M132 138L142 142L138 152Z' });
  a.shape('M144 136L150 132L154 138L148 142Z', SULFUR, { shade: 'M150 132L154 138L148 142Z' });
} });

// Opal (the user's pick, 2026-11-08: ammonite): a fossil shell turned to opal, its chambers flecked with every band's play-of-colour, a
// shard of grey matrix still on it. The deep caches are the same shell in the rock (cavelife.js).
const OPAL = { base: '#8fb4d8', shade: '#5a72b0', deep: '#3a4682', lite: '#d4ecff', spec: '#ffffff', rim: '#a8d8ff', ink: '#1c2248' };
const OPAL_MATRIX = { base: '#6c7280', shade: '#474c58', deep: '#30333c', lite: '#9298a6', spec: '#e0e4ea', ink: '#14161c' };
const OPAL_FLECKS = ['#8cd2ff', '#9cf07a', '#ffbe5a', '#ff7040', '#c0a4ff'];
PICS.push({ id: 'opal', draw(a) {
  const pp = (list) => poly(list.flat());
  a.shape(pp([[18, 104], [34, 76], [62, 62], [70, 98], [56, 136], [26, 134]]), OPAL_MATRIX, { shade: 6, lite: pp([[18, 104], [34, 76], [40, 90], [26, 110]]) });
  const C = [84, 80], R = (t) => 5.4 * Math.exp(0.205 * t), on = (t, r) => [C[0] + Math.cos(t + 0.7) * r, C[1] + Math.sin(t + 0.7) * r], N = 12, dt = (Math.PI * 2) / N;
  a.shape(ell(C[0], C[1], R(Math.PI * 2) + 2), OPAL, { outer: false, shade: 3 });
  // The outer whorl's chambers, each from this turn's edge to the last's, a fleck of colour in each.
  for (let k = 0; k < N; k++) {
    const t0 = Math.PI * 2 + k * dt, o = [], i = [];
    for (let j = 0; j <= 3; j++) { const t = t0 + (j / 3) * dt; o.push(on(t, R(t))); i.unshift(on(t, R(t - Math.PI * 2))); }
    a.shape(pp(o.concat(i)), OPAL, { shade: 3 });
    const tm = t0 + dt / 2, [x, y] = on(tm, (R(tm) + R(tm - Math.PI * 2)) / 2), w = (R(tm) - R(tm - Math.PI * 2)) * 0.28;
    a.glow(ell(x, y, w, w * 0.8), OPAL_FLECKS[k % 5]);
  }
  const spiral = [];
  for (let t = 0.3; t <= Math.PI * 2 + 0.01; t += 0.3) spiral.push(on(t, R(t)));
  a.line('M' + spiral.map((p) => p.map((n) => n.toFixed(1)).join(' ')).join('L'), { w: 1.4, c: OPAL.ink });
  a.flat(star(58, 38, 9), '#ffffff');
} });

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

// The rest of the items. Paths in the logical picture's pixels, so each stands where its pixel picture stood. Icons must tell apart at 32 px: one object, its own colour.
function more() {
const { STEEL, GUN, DARK, COPPER, band, box, inner, cyl, rivets, hazard } = window.AnimeKit;
const P = {};
const STONE = { base: '#77756f', shade: '#54535a', deep: '#3a3a44', lite: '#9b978c', spec: '#bdb8aa' };
const COAL = { base: '#4b4c55', shade: '#33343f', deep: '#23242e', lite: '#6b6d78', spec: '#9a9ca6' };
const TAN = { base: '#8a7a5e', shade: '#5f5348', deep: '#433b38', lite: '#ab9a78', spec: '#cdbd98' };
const COPPERY = { base: '#b9743f', shade: '#7c4530', deep: '#55302c', lite: '#dc9c62', spec: '#f4c896' };
const BRASS = { base: '#a98a4c', shade: '#6e5436', deep: '#4c3a2c', lite: '#d4b878', spec: '#f6e6b4' };
const WOOD = { base: '#8a6242', shade: '#5c3e30', deep: '#402a26', lite: '#ad8258', spec: '#cca678' };
const PALE = { base: '#a9b8c6', shade: '#73859c', deep: '#526279', lite: '#d0dae2', spec: '#eef2f4' };
const ICE = { base: '#b4d6e8', shade: '#7ba4c8', deep: '#5a80ac', lite: '#dcecf4', spec: '#f4fafc' };
const MILK = { base: '#c9c6cc', shade: '#8f8ca0', deep: '#6a6880', lite: '#e8e6e8', spec: '#fbfafa' };
const SOIL = { base: '#5f4532', shade: '#402e26', deep: '#2b201d', lite: '#7e5f44', spec: '#9c7c5c' };
const SPROUT = { base: '#5f9048', shade: '#3a6438', deep: '#28482c', lite: '#88b860', spec: '#b8d888' };

/** A pit in rock: dark on the side the light comes from, its lip caught on the far side. */
const pit = (a, x, y, r, m) => {
  a.shape(ell(x, y, r, r * .82), { base: m.shade, shade: m.deep, deep: m.deep, lite: m.lite }, { outer: false, edge: false, deep: ell(x - r * .25, y - r * .22, r * .72, r * .58) });
  a.line(`M${x - r * .7} ${y + r * .62}C${x - r * .1} ${y + r * 1.02} ${x + r * .7} ${y + r * .8} ${x + r} ${y + r * .05}`, { c: m.lite, w: 1.3 });
};
/** A speck of another colour in the rock. */
const fleck = (a, x, y, r, c) => a.flat(ell(x, y, r, r * .8), c);

// ---------- rock ----------
P.asteroid = (a) => {
  a.shape('M6.4 1.2L9.8 1.6L12 3.4L12.6 6.2L13.6 8.6L12.8 11.8L10.4 14.2L6.8 15L4 13.4L3 10.6L2 8L3 4.2Z', STONE, {
    shade: 'M12.6 6.2L13.6 8.6L12.8 11.8L10.4 14.2L6.8 15L4 13.4L8 12.6L10.6 9.6L11 6Z', deep: 'M12.8 11.8L10.4 14.2L6.8 15L4 13.4L8 13.6L11 12Z', lite: 'M3 4.2L6.4 1.2L9.8 1.6L8 3.6L4.4 6.4L2.4 7.6Z' });
  pit(a, 6.4, 5, 1.7, STONE); pit(a, 9.6, 9.4, 2.1, STONE); pit(a, 5, 10.6, 1.1, STONE);
  a.line('M10.4 3.4L9.4 5.2L10.6 6', { w: 1 });
  fleck(a, 3.9, 7.6, .4, '#8e5c3c');
};
P.asteroidLarge = (a) => {
  a.shape('M3 3.8L7.4 2.6L11.6 3L14.6 5.2L15.6 8.6L14.2 11.6L10.4 13.4L5.4 13.4L1.8 11.6L.4 8.2Z', COAL, {
    shade: 'M15.6 8.6L14.2 11.6L10.4 13.4L5.4 13.4L1.8 11.6L6 11.4L11 10.4Z', deep: 'M14.2 11.6L10.4 13.4L5.4 13.4L8 12.2Z', lite: 'M.4 8.2L3 3.8L7.4 2.6L11.6 3L7 4.6L3.4 6.2Z' });
  pit(a, 5.2, 7.2, 2.5, COAL); pit(a, 11, 7.6, 1.6, COAL); pit(a, 8.6, 11, .9, COAL);
  a.line('M8.4 4.2L9.2 6L8.2 7.6', { w: 1.1 });
};
P.asteroidOre = (a) => {
  a.shape('M5 1.6L9 .8L12.4 2.2L14.8 5.4L15.2 9.4L13.4 12.8L10 15L5.8 15L2.6 13L.8 9.4L1.4 5Z', COAL, {
    shade: 'M15.2 9.4L13.4 12.8L10 15L5.8 15L2.6 13L7 13L11.6 11Z', deep: 'M13.4 12.8L10 15L5.8 15L8.6 13.8Z', lite: 'M1.4 5L5 1.6L9 .8L7.4 3L3.6 5.4L1.2 8Z' });
  pit(a, 5.4, 9.8, 1.5, COAL); pit(a, 11.2, 6.2, 1.2, COAL);
  // The ore: bright metal broken out of the rock, its facets catching the light.
  for (const [x, y, s] of [[4.6, 4.2, 1.1], [8.2, 3.2, .7], [9, 8, 1.3], [12.4, 10, 1], [6.8, 12.8, .8], [2.8, 7.2, .6]]) {
    a.shape(poly(x - s, y, x - s * .1, y - s, x + s, y - s * .2, x + s * .3, y + s), STEEL, { outer: false, shade: poly(x + s, y - s * .2, x + s * .3, y + s, x - s * .1, y), lite: poly(x - s, y, x - s * .1, y - s, x, y) });
  }
};
P.asteroidRich = (a) => {
  a.shape('M2.6 5L6 3L10.4 2.8L13.8 4.6L15.4 7.8L14.6 10.8L11 12.8L6.4 13.4L2.6 12.2L.8 9Z', TAN, {
    shade: 'M15.4 7.8L14.6 10.8L11 12.8L6.4 13.4L2.6 12.2L7 11.6L11.6 9.6Z', deep: 'M14.6 10.8L11 12.8L6.4 13.4L9 12Z', lite: 'M.8 9L2.6 5L6 3L10.4 2.8L6.4 4.8L3 7Z' });
  pit(a, 7.4, 5.6, 1.5, TAN); pit(a, 12, 6.8, 1, TAN);
  // The vein: gold in a forking crack, as the ore rock's in space.
  a.flat('M1.6 9.8L4 9.2L5.8 8.2L7.6 9.6L10 9L12.6 10L13 10.6L10 9.8L8 10.6L6.2 12.4L5.6 12.2L6.6 10.2L5.6 9.2L4 10.2L1.8 10.6Z', '#d4a838');
  a.flat('M3.6 9.6L5.6 8.6L7 9.8L5.6 9.6L4 10Z', '#f0d878');
  a.line('M5.6 8.2L5 6.6', { c: '#8a6a24', w: 1.4 });
  a.line('M1.6 10.6L4 10.4L5.6 9.8L6.6 10.6L8 10.8L10 10L12.6 10.8', { w: 1 });
};
P.asteroidSmall = (a) => {
  a.shape('M4.4 4.4L8 3L11.4 3.4L13.6 5.8L14.2 8.6L12.4 11.4L8.6 13L4.8 12.6L2.2 10.4L2 7Z', STONE, {
    shade: 'M14.2 8.6L12.4 11.4L8.6 13L4.8 12.6L2.2 10.4L6.4 11L10.6 9.6Z', deep: 'M12.4 11.4L8.6 13L4.8 12.6L8 11.8Z', lite: 'M2 7L4.4 4.4L8 3L11.4 3.4L7.6 5L4 6.6Z' });
  pit(a, 9.4, 6.6, 1.9, STONE);
  a.line('M4.4 7.2L6 8.6L5.2 10.2', { w: 1 });
  fleck(a, 4.6, 5.8, .45, '#8e5c3c'); fleck(a, 12, 8.8, .4, '#8e5c3c');
};
P.rock = (a) => {
  a.shape('M5 1.8L9.6 1.2L13 3.2L15 7L14.4 11L11.4 14.2L6.8 15L3 13.4L1 9.6L1.6 5Z', STONE, {
    shade: 'M15 7L14.4 11L11.4 14.2L6.8 15L3 13.4L7.4 13L11.6 10.6L12.8 7Z', deep: 'M14.4 11L11.4 14.2L6.8 15L9 13.4L12.4 11.4Z', lite: 'M1.6 5L5 1.8L9.6 1.2L7.4 3.2L3.6 5.2L1.2 8.4Z',
    lines: ['M6 3.6L5 6.4L7 8.6L10.2 8L11.6 5', 'M7 8.6L6 12', 'M5 6.4L2.2 7.4'] });
  fleck(a, 12.4, 5.2, .4, '#a07a3c'); fleck(a, 4, 11, .45, '#a07a3c'); fleck(a, 9.2, 12, .35, '#a07a3c');
};
P.gravel = (a) => {
  const stone = (d, sh, m) => a.shape(d, m || STONE, { shade: sh });
  stone('M7 5.2L9.6 4.6L10.8 6.6L9.8 8.6L7.2 8.8L6 7Z', 'M10.8 6.6L9.8 8.6L7.2 8.8L9 7Z');
  stone('M2.8 8.4L5.4 7.6L6.8 9.4L6.4 11.8L3.4 12.4L1.8 10.4Z', 'M6.8 9.4L6.4 11.8L3.4 12.4L5 10.4Z', COAL);
  stone('M10 8L12.8 7.8L14.6 10L13.6 12.4L10.8 12.6L9.2 10.4Z', 'M14.6 10L13.6 12.4L10.8 12.6L12.4 10.6Z', TAN);
  stone('M5.8 10.6L8.8 9.8L10.4 11.8L9.6 14L6.4 14.4L5 12.6Z', 'M10.4 11.8L9.6 14L6.4 14.4L8.4 12.4Z');
  stone('M.8 12L2.8 11.4L4 13L3 14.6L1 14.4Z', 'M4 13L3 14.6L1 14.4L2.6 13.4Z', TAN);
  stone('M11 12.4L13.2 12L15.2 13L14.4 14.8L11.6 14.8Z', 'M15.2 13L14.4 14.8L11.6 14.8L13.4 13.6Z', COAL);
  stone('M4.4 13.2L6.4 12.8L7.2 14.2L6 15L4.2 14.8Z', 'M7.2 14.2L6 15L4.2 14.8L5.8 14Z');
};
P.magnetite = (a) => {
  a.shape('M2.6 5.4L5.6 2.6L10 2L13.4 3.8L15.4 7.4L14.4 11L11 13.4L6 14L2.4 12.4L.8 9Z', COAL, {
    shade: 'M15.4 7.4L14.4 11L11 13.4L6 14L8 12L12 9.8Z', deep: 'M14.4 11L11 13.4L6 14L9.6 12.4Z', lite: 'M.8 9L2.6 5.4L5.6 2.6L10 2L6.4 4.2L3.4 7Z' });
  // A blue-black lustre, and the seam still hot: what works, at full strength.
  a.shape('M5 4.6L8.2 3.8L9.8 5.2L7.4 6.6L5 6.2Z', { base: '#4a5688', shade: '#333c68', lite: '#7c88b8' }, { outer: false, edge: false, lite: 'M5 4.6L8.2 3.8L8 4.8L5.4 5.4Z' });
  a.flat('M2.4 11.2L5 10.6L6.2 9L8.2 9.6L10 7.4L12.6 6.2L13.6 4.8L14 5.2L13.2 7.4L10.8 8.8L9 11L7 10.8L5.8 12L3 12.2Z', '#c24a22');
  a.flat('M3.8 11.4L5.4 11L6.6 9.8L8.4 10.2L10.2 8.2L12.6 7L11 8.2L9 10.2L7 10L5.8 11.2Z', '#f0a040');
  a.line('M2.4 12.2L5.8 12.4L7 11.2L9 11.4L11 9.2L13.4 7.8', { w: 1 });
};
P.silicon = (a) => {
  a.shape('M1.4 13.6L4 9.6L6.4 5.6L8.4 3.4L11 6.4L13 9.4L15 13.6L12.4 14.2L4 14.2Z', { base: '#8d96a8', shade: '#5c6480', deep: '#454c68', lite: '#c4cad6', spec: '#e8ecf2' }, {
    lite: 'M1.4 13.6L4 9.6L6.4 5.6L8.4 3.4L7.4 8L5 11.4L3.4 14Z', shade: 'M8.4 3.4L11 6.4L13 9.4L15 13.6L12.4 14.2L10 10L8.8 7Z', deep: 'M15 13.6L12.4 14.2L9.4 14.2L10.6 11.6L12.8 12Z',
    lines: ['M8.4 3.4L7.4 8L5 11.4', 'M7.4 8L10 10L12.4 14.2', 'M10 10L6.6 14.2'] });
  fleck(a, 8.6, 7.6, .55, '#a48cd0'); fleck(a, 6.4, 11.8, .35, '#a48cd0');
};
P.quartz = (a) => {
  a.shape('M.8 12.4L10.8 2.4L14.6 1.6L15.2 5L5 14.8L2 15.2Z', MILK, {
    lite: 'M.8 12.4L10.8 2.4L12.4 4L2.6 14Z', shade: 'M14.6 1.6L15.2 5L5 14.8L3.6 13.2L13 3.6Z', deep: 'M15.2 5L5 14.8L4.4 14.2L14.6 4.4Z', spec: 'M3 11.8L9.8 5L10.4 5.6L3.6 12.4Z',
    lines: ['M10.8 2.4L12.8 4L15.2 5', 'M12.8 4L14.6 1.6', 'M2.6 14L12.8 4'] });
};
P.mica = (a) => {
  const leaf = (y, l, r, m) => a.shape(`M${l} ${y + .4}L${l + 1.4} ${y - .7}L${r - .6} ${y - .9}L${r} ${y + .2}L${r - 1} ${y + 1}L${l + .8} ${y + 1.2}Z`, m, {
    lite: `M${l + 1} ${y - .6}L${r - .6} ${y - .8}L${r - .8} ${y}L${l + 1} ${y + .2}Z`, shade: `M${l} ${y + .7}L${r} ${y + .5}L${r - 1} ${y + 1.1}L${l + .8} ${y + 1.3}Z` });
  const FLAKE = { base: '#a59eae', shade: '#6c6480', deep: '#514a66', lite: '#d8d4dc', spec: '#f4f2f4' };
  const DUSK = { base: '#8a7e96', shade: '#5a506c', deep: '#433b58', lite: '#bcb4c6', spec: '#e8e4ec' };
  leaf(12.4, 3.4, 12, DUSK); leaf(10.2, 1.6, 13.6, FLAKE); leaf(8, 2.6, 14.4, DUSK); leaf(5.8, 1.2, 12.6, FLAKE); leaf(3.6, 3.8, 13.2, DUSK);
  a.line('M4 3.2L9 3', { c: '#f4f2f4', w: 1.2 }); a.line('M3 5.2L8 5', { c: '#f4f2f4', w: 1.2 });
};
P.ice = (a) => {
  a.shape('M1.2 8L3.6 3.8L7.8 1.8L12.4 2.8L15 6L14.8 10.4L11.8 13.6L6.6 14.6L2.6 12.6Z', ICE, {
    lite: 'M1.2 8L3.6 3.8L7.8 1.8L12.4 2.8L9 6.6L5 7.4Z', shade: 'M9 6.6L12.4 2.8L15 6L14.8 10.4L11.8 13.6L9.6 10.4Z', deep: 'M14.8 10.4L11.8 13.6L10 14L11 11Z', spec: 'M4.4 5L7.6 3.2L8.4 3.6L5 5.8Z',
    lines: ['M5 7.4L9 6.6L12.4 2.8', 'M9 6.6L9.6 10.4L11.8 13.6', 'M5 7.4L2.6 12.6', 'M9.6 10.4L6.6 14.6'] });
  a.line('M6 9L7 11', { c: '#5a80ac', w: 1 });
};
P.glass = (a) => {
  const PANE = { base: '#8fb8c6', shade: '#68909e', deep: '#527a8a', lite: '#c0dce2', spec: '#eaf6f6' };
  a.shape('M2.8 3L13.6 2.8L13.8 13.8L3 14Z', PANE, {
    lite: 'M2.8 3L13.6 2.8L13.6 3.8L2.8 4Z', shade: 'M12.8 2.8L13.8 2.8L13.8 13.8L12.8 13.8Z', deep: 'M3 13L13.8 12.8L13.8 13.8L3 14Z',
    spec: ['M5 11L10.6 4.6L11.8 4.6L6 11.2Z', 'M8 11.2L11.8 7L11.8 8.2L9 11.2Z'] });
  a.line('M3.6 10.6L4.8 12.2', { c: '#3f5f6c', w: 1 });
};
P.soil = (a) => {
  // A burlap sack, its mouth rolled, the dirt heaped in it.
  const SACK = { base: '#927a52', shade: '#65533c', deep: '#493c30', lite: '#b09a6c', spec: '#c8b488' };
  a.shape('M3 5.4L13 5.4L14.2 8L14.6 12.6L13 14.8L3 14.8L1.4 12.6L1.8 8Z', SACK, {
    lite: 'M2 7L5 7L4.2 13L1.6 12Z', shade: 'M11 6L13 5.4L14.2 8L14.6 12.6L13 14.8L10.6 14.8Z', deep: 'M1.4 13L14.6 13L13 14.8L3 14.8Z',
    lines: ['M5 7L4.6 14.6', 'M8 7L8 14.8', 'M11 7L11.4 14.6', 'M2 9.6L14.2 9.6', 'M1.6 12L14.4 12'] });
  a.shape('M2.8 5.6L4.4 3L7 1.6L10 1.8L12.4 3.4L13.4 5.6L10.6 6.8L5 6.8Z', SOIL, {
    lite: 'M4.4 3L7 1.6L9 2L6 3.8L3.6 5Z', shade: 'M12.4 3.4L13.4 5.6L10.6 6.8L8 6.8L10 4.4Z', lines: ['M6.4 4.6L8 5.4'] });
  a.tube('M2.6 6.2C5 7.4 11 7.4 13.4 6.2', 1, SACK, { spec: false });
};
P.soilClod = (a) => {
  a.tube('M4.4 11.8L3.6 14.4', .55, TAN, { spec: false, bare: true }); a.tube('M8.4 13L8.6 15.2', .55, TAN, { spec: false, bare: true }); a.tube('M11.6 12L12.8 14.2', .55, TAN, { spec: false, bare: true });
  a.shape('M.8 10L2.6 7.4L6 6.4L9.6 6.6L13 7.4L15.2 9.8L14.4 12L10.8 13.4L5 13.4L1.8 12.4Z', SOIL, {
    lite: 'M.8 10L2.6 7.4L6 6.4L9.6 6.6L6.4 8L3 9.6Z', shade: 'M15.2 9.8L14.4 12L10.8 13.4L5 13.4L1.8 12.4L6 11.8L11 11Z', deep: 'M14.4 12L10.8 13.4L5 13.4L9 12.6Z', lines: ['M6 9.4L7.4 10.6', 'M10.8 8.8L10 10.4'] });
  fleck(a, 3.6, 9.8, .45, '#8a8478'); fleck(a, 12.6, 9.6, .5, '#8a8478');
  a.tube('M8.2 7.4C8.2 5.6 8 4.6 8.2 3.2', .7, SPROUT, { spec: false });
  a.shape('M8.2 3.8C6.8 2.2 5 1.8 3.8 2.4C4.6 4 6.4 4.8 8.2 3.8Z', SPROUT, { shade: 'M8.2 3.8C6.4 4.8 4.6 4 3.8 2.4C5.4 3.2 6.8 3.6 8.2 3.8Z' });
  a.shape('M8.2 3.4C9.4 1.8 11.2 1.2 12.8 1.8C12 3.6 10 4.4 8.2 3.4Z', SPROUT, { shade: 'M8.2 3.4C10 4.4 12 3.6 12.8 1.8C11.2 2.8 9.6 3.2 8.2 3.4Z' });
};
P.copperOre = (a) => {
  const ORE = { base: '#c0743c', shade: '#864a2e', deep: '#5e3228', lite: '#e09c60', spec: '#f4c088' };
  // A broken lump of native copper, all arms: flat faces meeting at hard edges, as the iron ore's, bright where they were knocked.
  a.shape('M2.4 4.2L5 3.4L6.6 4.6L8 2.2L10.8 2.8L11.4 5.2L14.2 5L15 7.4L13.4 8.8L15 10.8L13.2 12.4L11.2 11.8L10.4 14.2L7.8 14.4L6.8 12.6L4.6 14L3 12.4L3.6 10.6L1.2 9.8L1.4 7.2L3 6.6Z', ORE, {
    lite: 'M2.4 4.2L5 3.4L6.6 4.6L8 2.2L10.8 2.8L8.6 5.6L5 6.4L3 6.6Z', shade: 'M11.4 5.2L14.2 5L15 7.4L13.4 8.8L15 10.8L13.2 12.4L11.2 11.8L10.4 8.8Z', deep: 'M10.4 8.8L11.2 11.8L10.4 14.2L7.8 14.4L6.8 12.6L8.2 10.4Z',
    lines: ['M5 6.4L8.6 5.6L10.4 8.8L8.2 10.4L4.8 9.8L5 6.4', 'M8.6 5.6L10.8 2.8', 'M10.4 8.8L13.4 8.8', 'M4.8 9.8L3.6 10.6', 'M8.2 10.4L6.8 12.6', 'M5 6.4L3 6.6', 'M4.8 9.8L1.2 9.8'] });
  a.flat('M5.8 6.8L8.2 6.2L8.8 7.4L6.2 8Z', '#f4c088'); a.flat('M3.2 4.6L4.8 4.2L4.4 5.2L3.2 5.6Z', '#f4c088'); a.flat('M12.2 6L13.8 5.8L13.4 6.8L12.2 7Z', '#e0a468');
  a.shape('M12 10.4L14 10.8L13.2 12L12 11.6Z', STONE, { outer: false, shade: 'M14 10.8L13.2 12L12 11.6Z' });
};

/** A cog's outline of n square teeth, r to their tips. */
const cog = (cx, cy, r, n) => {
  const p = [];
  for (let i = 0; i < n; i++) {
    const t = i / n * Math.PI * 2 - .12, w = Math.PI / n * .5, at = (q, s) => [cx + Math.cos(t + s) * q, cy + Math.sin(t + s) * q];
    p.push(...at(r * .68, -w * 1.5), ...at(r, -w), ...at(r, w), ...at(r * .68, w * 1.5));
  }
  return poly(...p);
};

// ---------- worked metal ----------
P.copper = (a) => {
  const BRIGHT = { base: '#d08a4c', shade: '#a05e38', deep: '#70402e', lite: '#f0b47c', spec: '#fbdcae' };
  a.shape('M1.2 10.6L5.8 5.2L13.6 4.4L15.2 7.2L10.8 13L2.6 13.4Z', BRIGHT, {
    lite: 'M2.6 10.2L6.4 5.8L13.2 5L9.6 9.6Z', shade: 'M1.2 10.6L2.6 10.2L9.6 9.6L10.8 13L2.6 13.4Z', deep: 'M13.6 4.4L15.2 7.2L10.8 13L9.6 9.6Z', spec: 'M6.6 7L10.4 6.2L8.4 8.4L5.4 8.6Z',
    lines: ['M2.6 10.2L9.6 9.6L13.6 4.4', 'M9.6 9.6L10.8 13'] });
};
P.plate = (a) => {
  a.shape('M2 2.2L14 2L14.2 14L2.2 14.2Z', STEEL, {
    lite: 'M2 2.2L14 2L13.4 3L2.8 3.2Z', shade: 'M13.2 2.6L14 2L14.2 14L13.2 13.4Z', deep: 'M2.8 13.2L13.2 13.2L14.2 14L2.2 14.2Z', spec: ['M6 10.6L10.4 5.6L11 6L6.6 11Z'] });
  rivets(a, [3.8, 3.8], [12.4, 3.8], [3.8, 12.4], [12.4, 12.4]);
  a.line('M5 5.6L6.4 6.8', { w: 1 });
};
P.alloyPlate = (a) => {
  const ALLOY = { base: '#7f9ca0', shade: '#577278', deep: '#3e5560', lite: '#aac6c6', spec: '#d6ecec' };
  a.shape('M2 4L10.6 3.4L14 6.6L14.2 12.6L2.4 13Z', ALLOY, {
    lite: 'M2 4L10.6 3.4L11.4 4.4L2.8 5Z', shade: 'M10.6 3.4L14 6.6L14.2 12.6L13 12.6L13 7L10.4 4.4Z', deep: 'M2.8 12L13 11.8L14.2 12.6L2.4 13Z' });
  a.shape('M4 6.6L11.6 6.4L11.8 10.4L4.2 10.6Z', { base: '#3f5560', shade: '#2d3f4a', deep: '#22303c', lite: '#577278' }, { outer: false, deep: 'M4 6.6L11.6 6.4L11.6 7.2L4 7.4Z' });
  a.flat(rr(5.4, 8.2, 4.8, .9, .2), '#58b4a8');
  a.flat(rr(5.6, 8.3, 1.6, .4, .1), '#a8e4d8');
  rivets(a, [3.2, 11.8]);
};
P.alloyScrap = (a) => {
  const ALLOY = { base: '#7f9ca0', shade: '#577278', deep: '#3e5560', lite: '#aac6c6', spec: '#d6ecec' };
  // The same alloy torn out in a larger sheet, its slot still lit, its right side bitten off.
  a.shape('M1 3.2L8 2.4L13.4 2.8L15 4.8L13.6 6.2L14.8 8.2L12.8 9.4L13.2 11.8L10.6 12.2L9 14.2L1.4 14Z', ALLOY, {
    lite: 'M1 3.2L8 2.4L13.4 2.8L12.8 3.8L2 4.4Z', shade: 'M13.4 2.8L15 4.8L13.6 6.2L14.8 8.2L12.8 9.4L13.2 11.8L10.6 12.2L11.6 9L12 4Z', deep: 'M1.4 13L10 12.6L9 14.2L1.4 14Z' });
  a.shape('M3 5.6L10.6 5.2L10.8 11.2L3.2 11.4Z', { base: '#3f5560', shade: '#2d3f4a', deep: '#22303c', lite: '#577278' }, { outer: false, deep: 'M3 5.6L10.6 5.2L10.6 6.2L3 6.6Z' });
  a.flat(rr(4.2, 7.6, 5.2, 2, .3), '#3fa89c');
  a.flat(rr(5.2, 8.2, 3, .8, .2), '#c8f0e4');
  a.line('M11 10.6L12.4 9.2M11.4 5L12.6 6', { w: 1.1 });
  a.flat('M1.6 12.2L2.8 12L3.4 13.4L1.8 13.6Z', '#7c5038');
};
P.hullPlate = (a) => {
  // A torn wedge of hull, as the hull debris in space: pale paint, a corner left, holes where it was riveted.
  const HULL = { base: '#8fa0b2', shade: '#62728a', deep: '#465368', lite: '#b8c6d2', spec: '#dfe6ea' };
  a.shape('M1 1.6L14.8 1L13.4 3.4L14 4.6L11.4 6.4L11 8.2L8.4 9L7.6 11.2L5.4 11.8L4.6 14.2L2.6 14.6L1.2 15.2Z', HULL, {
    lite: 'M1 1.6L14.8 1L14 2L2 2.8Z', shade: 'M1 12L5 11.4L4.6 14.2L1.2 15.2Z', deep: 'M11.4 6.4L11 8.2L8.4 9L7.6 11.2L5.4 11.8L6.6 9.6L9 7.6Z',
    lines: ['M3 2.6L3 13.4', 'M3 4.6L13 3.6'] });
  for (const [x, y] of [[5, 3], [7.4, 3], [9.8, 2.8], [12, 2.6], [4.8, 5.6], [7, 5.4], [9.4, 5], [4.6, 8], [6.8, 7.6], [4.4, 10.2]]) a.flat(ell(x, y, .38), '#2c3344');
  a.flat('M8 5.8L9.6 6.4L9.2 7.6L7.8 7.2Z', '#7c5038'); a.flat('M3.4 11.8L4.6 11.6L4.4 13L3.4 13.2Z', '#7c5038');
};
P.frame = (a) => {
  // The square of box section, its cross of struts behind, copper gussets in the corners.
  a.tube('M3.4 3.4L12.6 12.6', 1.5, GUN, { spec: false }); a.tube('M12.6 3.4L3.4 12.6', 1.5, STEEL);
  a.shape('M1 1L15 1L15 15L1 15ZM3 3L3 13L13 13L13 3Z', GUN, {
    lite: 'M1 1L15 1L14 2L2 2Z', shade: 'M14 1L15 1L15 15L14 15Z', deep: 'M1 14L15 14L15 15L1 15Z' });
  for (const [x, y, u, v] of [[3, 3, 1, 1], [13, 3, -1, 1], [3, 13, 1, -1], [13, 13, -1, -1]]) a.shape(poly(x, y, x + u * 2.2, y, x, y + v * 2.2), COPPERY, { outer: false, lite: poly(x, y, x + u * 1.4, y, x, y + v * 1.4) });
  rivets(a, [2, 2], [14, 2], [2, 14], [14, 14]);
};
P.gear = (a) => {
  a.shape(cog(8, 8, 7.4, 8), GUN, { lite: ell(7, 6.6, 5.2, 4.6), shade: 'M1 10L4 11.8L8 12.8L12 11.8L15 8.6L16 16L0 16Z', deep: 'M4 13L8 13.8L12 12.4L13 16L3 16Z' });
  a.shape(ell(8, 8, 4.4), { ...GUN, base: '#6c7484' }, { outer: false, shade: .5 });
  a.shape(ell(8, 8, 2.5), DARK, { deep: ell(7.7, 7.6, 1.9), edge: true, outer: false });
  a.line('M4.6 5.6C5.4 4.8 6.2 4.4 7 4.2', { c: '#dfe7f2', w: 1.2 });
};
P.nail = (a) => {
  // A collated strip: five nails through a red paper band, as they feed a nailer.
  for (const x of [2.8, 5.2, 7.6, 10, 12.4]) {
    a.shape(poly(x - .55, 4, x + .55, 4, x + .45, 11.6, x, 13.4, x - .45, 11.6), STEEL, { shade: poly(x + .1, 4, x + .55, 4, x + .45, 11.6, x, 13.4) });
    a.shape(rr(x - 1.05, 3.1, 2.1, 1.1, .3), STEEL, { lite: band(x - 1, 3.1, x + 1, 3.6), shade: band(x - 1, 3.8, x + 1, 4.2) });
  }
  a.shape('M1 6.2L15 6.2L15 8.2L1 8.2Z', { base: '#a84034', shade: '#742a2c', deep: '#561e24', lite: '#d0624c', spec: '#eaa088' }, { lite: 'M1 6.2L15 6.2L15 6.8L1 6.8Z', shade: 'M1 7.6L15 7.6L15 8.2L1 8.2Z' });
};
P.tube = (a) => {
  // A length of pipe, its open end toward us at the foot, the far end cut.
  a.shape('M1.4 10.6L10.4 2L14.6 1.8L14.8 6L5.8 14.6Z', STEEL, {
    lite: 'M2.2 9.8L10.8 2.2L12.8 3.6L4 11.6Z', shade: 'M14.6 1.8L14.8 6L5.8 14.6L4.4 13.2Z', deep: 'M14.8 6L5.8 14.6L5 13.8L14.4 4.6Z', spec: 'M3.6 9.8L10.4 3.6L11 4.2L4.2 10.4Z' });
  a.line('M11.4 2.2L13.8 5.2', { w: 1.1 });
  a.shape(ell(3.4, 12.8, 2.5, 2.5), GUN, { shade: .5 });
  a.shape(ell(3.4, 12.8, 1.5, 1.5), DARK, { outer: false, deep: ell(3, 12.4, 1.1) });
};
P.wire = (a) => {
  // A spool: dark flanges, copper turns, the end hanging free.
  a.tube('M12.4 12.4L13.4 14.6L15.2 14.8', .8, COPPERY, { spec: false });
  a.shape('M4 4.6L12 4.4L12 12.6L4 12.6Z', COPPERY, { lite: 'M4 4.8L12 4.6L12 6L4 6.2Z', shade: 'M10.2 4.6L12 4.4L12 12.6L10.2 12.6Z', deep: 'M4 11.4L12 11.4L12 12.6L4 12.6Z',
    lines: ['M5.2 4.6L5.4 12.6', 'M6.4 4.6L6.6 12.6', 'M7.6 4.6L7.8 12.6', 'M8.8 4.6L9 12.6', 'M10 4.6L10.2 12.6', 'M11.2 4.6L11.4 12.6'] });
  cyl(a, 1.8, 1.6, 2.4, 13.4, GUN, { r: .5 });
  cyl(a, 11.8, 1.6, 2.4, 13.4, GUN, { r: .5 });
};
P.coil = (a) => {
  // Loose hoops of copper, seen sideways, the far half of each behind the near: only wire, no core.
  const hoop = (x) => [`M${x} 3C${x - 3} 3 ${x - 3} 13 ${x} 13`, `M${x} 3C${x + 2} 3 ${x + 2} 13 ${x} 13`];
  const xs = [4.2, 7.2, 10.2, 13];
  for (const x of xs) a.tube(hoop(x)[1], .8, { ...COPPERY, base: '#7c4530', shade: '#55302c' }, { spec: false });
  a.tube('M1 15.2L2.4 13', .9, COPPERY, { spec: false });
  for (const x of xs) a.tube(hoop(x)[0], 1, COPPERY);
  a.tube('M13 3L15.4 .8', .9, COPPERY, { spec: false });
};
P.rotor = (a) => {
  // A drum of windings on its shaft, spun on the two steel ends.
  a.shape('M.8 7.2L4 6.8L12 6.8L15.2 7.2L15.2 9L12 9.4L4 9.4L.8 9Z', STEEL, { lite: 'M.8 7.2L15.2 7.2L15.2 7.8L.8 7.8Z', shade: 'M.8 8.5L15.2 8.5L15.2 9.2L.8 9.2Z' });
  a.shape('M4.4 2L11.6 2L12.2 3L12.2 13.2L11.6 14.2L4.4 14.2L3.8 13.2L3.8 3Z', COPPERY, {
    lite: 'M4.4 2L6.8 2L6.8 14.2L4.4 14.2Z', shade: 'M10 2L11.6 2L12.2 3L12.2 13.2L11.6 14.2L10 14.2Z', deep: 'M3.8 12.6L12.2 12.6L11.6 14.2L4.4 14.2Z',
    lines: ['M3.8 3.4L12.2 3.4', 'M3.8 4.8L12.2 4.8', 'M3.8 6.2L12.2 6.2', 'M3.8 7.6L12.2 7.6', 'M3.8 9L12.2 9', 'M3.8 10.4L12.2 10.4', 'M3.8 11.8L12.2 11.8'] });
  a.shape(rr(3.2, 1.6, 1.1, 13, .3), GUN, { lite: band(3.2, 1.6, 3.7, 14.6) }); a.shape(rr(11.7, 1.6, 1.1, 13, .3), GUN, { shade: band(12.3, 1.6, 12.8, 14.6) });
};
P.nozzle = (a) => {
  // The bell, wide to the left, its throat and flange to the right, as the engine's in the world.
  a.shape(rr(12, 2.8, 2.2, 10.6, .4), GUN, { lite: band(12, 2.8, 12.6, 13.4), shade: band(13.5, 2.8, 14.2, 13.4) });
  a.shape('M9.4 5.4L12.2 5.6L12.2 10.6L9.4 10.8Z', DARK, { lite: 'M9.4 5.4L12.2 5.6L12.2 6.4L9.4 6.4Z' });
  a.shape('M2.4 1.4L10 5L10 11.2L2.4 14.8C1.2 11 1.2 5.4 2.4 1.4Z', { base: '#6c7484', shade: '#3e4258', deep: '#262a42', lite: '#949cae', spec: '#dfe7f2' }, {
    lite: 'M2.4 1.4L10 5L10 6.4L3.4 3.8Z', shade: 'M10 8.8L10 11.2L2.4 14.8L3 12.6Z', lines: ['M6.4 3.4C5.8 6 5.8 10 6.4 12.8'] });
  a.shape(ell(2.5, 8.1, 1.3, 6.7), DARK, { deep: ell(2.3, 7.6, .8, 5.4) });
  a.flat('M7 10.6L8.2 10.2L8.4 11L7.2 11.6Z', '#6c3a30');
};
P.igniter = (a) => {
  // A flint in a wooden block, striking two sparks: the only thing here that burns.
  a.shape('M3.4 12L12.6 11.8L13 15L3 15.2Z', WOOD, { lite: 'M3.4 12L12.6 11.8L12.6 12.6L3.4 12.8Z', shade: 'M11.6 11.8L12.6 11.8L13 15L11.6 15Z', deep: 'M3 14.2L13 14.2L13 15L3 15.2Z', lines: ['M5 13.4L8 13.2'] });
  a.shape('M7 12.4L5.4 7.4L8 2.4L10.6 7.4L9 12.4Z', { base: '#6c9ec0', shade: '#44709c', deep: '#325880', lite: '#a4cce0', spec: '#dcf0f6' }, {
    lite: 'M7 12.4L5.4 7.4L8 2.4L8 7.8Z', shade: 'M8 2.4L10.6 7.4L9 12.4L8 12.4L8 7.8Z', spec: 'M6.4 7.4L7.8 4.4L7.8 7.2Z', lines: ['M5.4 7.4L8 7.8L10.6 7.4', 'M8 7.8L8 12.4'] });
  a.flat(star(3.8, 3, 1.5), '#f0b83c');
  a.flat(star(12.6, 2, 1.7), '#f0b83c');
};
P.scrapChunk = (a) => {
  // A rusted lump of plate, a cog still bolted on it, a bar sticking out at the foot.
  const RUST = { base: '#8a5234', shade: '#5e3628', deep: '#402622', lite: '#ad6c44', spec: '#c88c60' };
  a.tube('M9.6 11.4L14.6 14.8', 1.5, RUST, { spec: false });
  a.shape('M1 6L3.8 3.2L8 2.8L11 5.4L12 8.8L10 12.4L6.4 13.6L3 12.4L1 9.4Z', RUST, {
    lite: 'M1 6L3.8 3.2L8 2.8L6 5L2.6 7.4Z', shade: 'M11 5.4L12 8.8L10 12.4L6.4 13.6L7.4 9.6L8.8 6Z', deep: 'M3 12.4L6.4 13.6L10 12.4L8 11.2L4.4 11Z',
    lines: ['M2.6 7.4L6 5L8.8 6', 'M6 5L5.4 9.4L7.4 9.6'] });
  a.line('M3 8.6L4.4 8.2L4 10', { w: 1.2 });
  a.shape(cog(11, 4.4, 3.5, 7), GUN, { lite: ell(10.4, 3.6, 2.2), shade: .5 });
  a.shape(ell(11, 4.4, 1.3), DARK, { outer: false, deep: ell(10.8, 4.2, .9) });
};
P.planks = (a) => {
  const plank = (y, l, r) => {
    a.shape(`M${l} ${y}L${r} ${y - .2}L${r + .3} ${y + 3.2}L${l + .2} ${y + 3.4}Z`, WOOD, {
      lite: `M${l} ${y}L${r} ${y - .2}L${r} ${y + .7}L${l} ${y + .9}Z`, shade: `M${l} ${y + 2.5}L${r + .3} ${y + 2.4}L${r + .3} ${y + 3.2}L${l + .2} ${y + 3.4}Z`,
      lines: [`M${l + 1.4} ${y + 1.6}L${l + 5} ${y + 1.4}`, `M${l + 6.4} ${y + 1.9}L${r - 1} ${y + 1.6}`] });
    a.shape(ell(r - .8, y + 1.5, .55, 1.1), { ...WOOD, base: '#c8aa7c', shade: '#ad8258' }, { outer: false, edge: true });
  };
  plank(2.6, 2.2, 13.6); plank(6.2, .8, 12.4); plank(9.8, 2.8, 15);
  a.flat(ell(3.2, 7.6, .35), '#c8ccd2'); a.flat(ell(4.4, 11.2, .35), '#c8ccd2'); a.flat(ell(4, 4, .35), '#c8ccd2');
};
P.arcShard = (a) => {
  // A dark crystal gripped in a copper collar on three legs, its spine lit: the arc.
  const NIGHT = { base: '#3e2f6c', shade: '#281e50', deep: '#191238', lite: '#5e4a94', spec: '#b8a4e8' };
  a.tube('M6 10.6L2.2 14.8', .9, COPPERY, { spec: false }); a.tube('M10 10.6L13.8 14.8', .9, COPPERY, { spec: false });
  a.shape('M6.4 13.8L7 9.8L5.8 6L7.6 1.2L9.8 5.6L9.6 9.8L10 13.4L8.2 15Z', NIGHT, {
    lite: 'M5.8 6L7.6 1.2L7.8 6.2L7 9.8Z', shade: 'M7.6 1.2L9.8 5.6L9.6 9.8L10 13.4L8.4 13.8L8.4 7Z', lines: ['M7.8 6.2L5.8 6', 'M7.8 6.2L9.8 5.6', 'M7.6 10L8.4 15'] });
  a.flat('M7.8 2.6L8.4 5.4L7.8 6.6L8.6 8.6L7.6 8.8L7.4 6.6L7.2 5.2Z', '#c8a8ff');
  a.flat('M7.7 3.8L8 5.4L7.7 6.6L7.5 5.2Z', '#f0e8ff');
  a.shape(rr(3.6, 9.2, 8.8, 1.7, .4), COPPERY, { lite: band(3.6, 9.2, 12.4, 9.7), shade: band(3.6, 10.3, 12.4, 10.9) });
  a.flat(poly(11.8, 1.8, 13, 1.2, 12.6, 2.4), '#c8a8ff'); a.flat(poly(2.6, 5.2, 3.8, 4.8, 3.2, 6), '#c8a8ff');
};

// ---------- devices ----------
const SUIT = { base: '#c8923c', shade: '#8c5c30', deep: '#623e28', lite: '#e4b860', spec: '#f6dc98' };
const SCREEN = { base: '#141c2a', shade: '#0e1420', deep: '#0a0e18', lite: '#22304a', spec: '#6a8cb0' };
const CAN = { base: '#3f78b0', shade: '#2a5488', deep: '#1c3c68', lite: '#70a4d0', spec: '#c4e0f0' };
const battery = (full) => (a) => {
  a.shape(rr(5.6, .9, 4.8, 1.7, .3), STEEL, { lite: band(5.6, .9, 10.4, 1.4), shade: band(9.4, .9, 10.4, 2.6) });
  box(a, 2.4, 2.2, 11.2, 12.8, GUN, { r: .7, spec: true });
  inner(a, 3.4, 3.2, 1, 10.8, STEEL, { s: .3, b: .4, l: .3 }); inner(a, 11.6, 3.2, 1, 10.8, STEEL, { s: .3, b: .4, l: .3 });
  a.shape(rr(5, 4.4, 6, 8.6, .3), DARK, { outer: false, deep: band(5, 4.4, 11, 5.2) });
  // Only the bars change when it runs flat.
  for (const y of [5.6, 7.9, 10.2]) {
    a.flat(rr(6, y, 4, 1.5, .15), full ? '#6cc05c' : '#2c3238');
    a.flat(band(6.3, y + .15, 8.2, y + .55), full ? '#c0e88c' : '#3a4048');
  }
};
P.battery = battery(true);
P.battery_empty = battery(false);
P.biofuel = (a) => {
  const OLIVE = { base: '#747c34', shade: '#4e5628', deep: '#383e20', lite: '#9ca048', spec: '#c8cc70' };
  // A jerrycan: the handle cut in its shoulder, the cap to the right, the pressed X on its side.
  a.shape(rr(10.6, .6, 2.6, 2.6, .4), BRASS, { lite: band(10.6, .6, 13.2, 1.2), shade: band(12.4, .6, 13.2, 3.2) });
  a.shape('M3 3.4L4.6 1.8L10.8 1.8L13.4 3.4L14 5L14 13.8L13 15L3 15L2 13.8L2 4.6Z', OLIVE, {
    lite: 'M2 4.6L3 3.4L4.6 1.8L10.8 1.8L10 3L3.4 3.8L3 14.6L2 13.8Z', shade: 'M12.6 3L14 5L14 13.8L13 15L11.8 15L11.8 4Z', deep: 'M2.4 14L14 14L13 15L3 15Z',
    lines: ['M3.4 4.4L12.4 4.4', 'M3.4 13.4L12.4 13.4'] });
  a.flat(rr(5.2, 2.5, 3.2, 1, .3), '#2a2c1c');
  a.tube('M4.6 6L11.2 12.4', 1.3, { base: '#5c6228', shade: '#424820', spec: '#9ca048' }, { bare: true, spec: false });
  a.tube('M11.2 6L4.6 12.4', 1.3, { base: '#5c6228', shade: '#424820', spec: '#9ca048' }, { bare: true, spec: false });
  a.flat(ell(7.9, 9.2, 1.5, 1.5), '#d09830');
  a.flat(ell(7.6, 8.8, .6, .6), '#f0d068');
};
P.canister = (a) => {
  // The oxygen bottle: the world's tanks, its valve up top, a white label.
  a.shape(rr(6.6, 1.8, 2.8, 2.2, .3), GUN, { lite: band(6.6, 1.8, 9.4, 2.3), shade: band(8.7, 1.8, 9.4, 4) });
  a.shape('M4.6 5.4L6 3.8L10 3.8L11.4 5.4L11.8 7L11.8 13.6L10.8 14.8L5.2 14.8L4.2 13.6L4.2 7Z', CAN, {
    lite: 'M4.2 7L4.6 5.4L6 3.8L7.2 3.8L6.4 6L6.4 14.8L5.2 14.8L4.2 13.6Z', shade: 'M10 3.8L11.4 5.4L11.8 7L11.8 13.6L10.8 14.8L9.6 14.8L9.8 6Z', deep: 'M4.4 13.8L11.8 13.8L10.8 14.8L5.2 14.8Z', spec: 'M5.2 6.4L5.8 5.4L5.8 11L5.2 11Z' });
  a.shape(rr(5.6, 8, 4.8, 3, .2), { base: '#d4d4cc', shade: '#9a9c9c', deep: '#7a7c80', lite: '#ecece6' }, { outer: false, shade: band(9.6, 8, 10.4, 11) });
  a.flat(band(6.6, 9, 9.2, 9.9), '#2a5488');
};
const BOTTLE = { base: '#8db4c2', shade: '#56808f', deep: '#3d5f6e', lite: '#cfe6ee', spec: '#f4fbfd' };
const WATERED = { base: '#2c8ca8', shade: '#1f6f80', deep: '#165a6a', lite: '#7fd8e6', spec: '#d6f4fa' };
P.water = (a) => {
  // A glass bottle of melted ice (water): a copper cap, the water in it, a white drop on its side.
  a.shape(rr(6.2, .8, 3.6, 1.8, .3), COPPERY, { lite: band(6.2, .8, 9.8, 1.3), shade: band(9, .8, 9.8, 2.6) });
  a.shape(rr(6.8, 2.4, 2.4, 2.6, .2), BOTTLE, { outer: false, lite: band(6.8, 2.4, 7.5, 5), shade: band(8.5, 2.4, 9.2, 5) });
  a.shape('M5.6 5.2L6.8 4.6L9.2 4.6L10.4 5.2L12 6.8L12 14L11 15.2L5 15.2L4 14L4 6.8Z', BOTTLE, {
    lite: 'M4 6.8L5.6 5.2L6.8 4.6L6.2 6.4L5.4 15.2L5 15.2L4 14Z', shade: 'M10.4 5.2L12 6.8L12 14L11 15.2L10 15.2L10.6 6.6Z' });
  a.shape('M4.6 8.4L11.4 8.4L11.4 13.8L10.6 14.6L5.4 14.6L4.6 13.8Z', WATERED, {
    outer: false, lite: band(4.6, 8.4, 11.4, 9.1), shade: band(10.2, 8.4, 11.4, 14.6), deep: band(4.6, 13.6, 11.4, 14.6) });
  a.flat('M8 9.6C8 9.6 6.6 11.4 6.6 12.2C6.6 13 7.2 13.5 8 13.5C8.8 13.5 9.4 13 9.4 12.2C9.4 11.4 8 9.6 8 9.6Z', '#eef8fb');
  a.flat(ell(7.4, 12.2, .35, .5), '#9ad8e8');
  a.flat(band(4.9, 6.4, 5.4, 12.6), '#e4f4f8');
};
P.display = (a) => {
  a.shape('M5.6 12.8L10.4 12.8L10.8 14.2L12 15.2L4 15.2L5.2 14.2Z', GUN, { lite: 'M4 14.6L12 14.6L12 15.2L4 15.2Z', shade: 'M9.4 12.8L10.4 12.8L10.8 14.2L12 15.2L9.4 15.2Z' });
  box(a, .9, 1.4, 14.2, 11.8, GUN, { r: .5, spec: true });
  a.shape(rr(2.2, 2.7, 11.6, 8.8, .3), SCREEN, { outer: false, deep: band(2.2, 2.7, 13.8, 3.4) });
  a.flat(rr(3.4, 4.2, 6.6, .9, .15), '#6cb4e0');
  a.flat(rr(3.4, 6, 8.8, .8, .15), '#3e7ca8');
  a.flat(rr(3.4, 7.7, 5, .8, .15), '#3e7ca8');
  a.flat(rr(3.4, 9.4, 3.4, .8, .15), '#2a5a80');
  a.flat(ell(13.6, 12.4, .3), '#6cc05c');
};
const NIGHTCELL = { base: '#2a2c6c', shade: '#1c1c50', deep: '#14123a', lite: '#4a48a0', spec: '#a0a0e0' };
P.photoPanel = (a) => {
  a.shape(rr(3.6, 12.6, 2.2, 2.4, .2), COPPERY, { shade: band(5, 12.6, 5.8, 15) }); a.shape(rr(10.2, 12.6, 2.2, 2.4, .2), COPPERY, { shade: band(11.6, 12.6, 12.4, 15) });
  box(a, 1, 1.6, 14, 11.2, GUN, { r: .3 });
  a.shape(band(2.2, 2.8, 13.8, 11.6), NIGHTCELL, { outer: false, deep: 'M2.2 2.8L8 2.8L2.2 8Z', shade: 'M13.8 5L13.8 11.6L7.4 11.6Z',
    lines: ['M5 2.8L5 11.6', 'M8 2.8L8 11.6', 'M11 2.8L11 11.6', 'M2.2 5.8L13.8 5.8', 'M2.2 8.8L13.8 8.8'] });
  // The light it drinks: a hard gold stroke across it.
  a.flat('M4.4 11.6L11.2 2.8L13 2.8L6.2 11.6Z', '#c8a040', { clip: band(2.2, 2.8, 13.8, 11.6) });
  a.flat('M5.2 11.6L11.9 2.8L12.3 2.8L5.6 11.6Z', '#f0dc90', { clip: band(2.2, 2.8, 13.8, 11.6) });
};
P.solarCell = (a) => {
  const CELLS = { base: '#27468c', shade: '#1b3268', deep: '#12224c', lite: '#3f68b4', spec: '#a0c4f4' };
  a.shape(rr(7, 13.4, 2.4, 2, .2), COPPERY, { shade: band(8.6, 13.4, 9.4, 15.4) });
  a.shape(rr(1.2, 1.4, 13.6, 12.4, .3), CELLS, { shade: 'M14.8 6L14.8 13.8L6 13.8Z', deep: 'M14.8 11L14.8 13.8L11 13.8Z', lite: 'M1.2 1.4L6.4 1.4L1.2 6.4Z' });
  // The silver grid, one bus across and one down, thin fingers between.
  for (const x of [4.4, 11.4]) a.flat(band(x, 1.6, x + .25, 13.6), '#8ea2c0');
  for (const y of [4.2, 10.8]) a.flat(band(1.4, y, 14.6, y + .25), '#8ea2c0');
  a.flat(band(7.6, 1.6, 8.4, 13.6), '#c0ccd8'); a.flat(band(1.4, 7.2, 14.6, 8), '#c0ccd8');
  a.flat(poly(3, 3, 4, 2.4, 3.6, 3.6), '#e6eef4'); a.flat(poly(9.4, 9, 10.8, 8.6, 10, 10), '#e6eef4');
};
P.processor = (a) => {
  // The chip: copper legs on four sides, a dark body, one lit die.
  for (const t of [4.2, 7, 9.8]) {
    a.shape(rr(t - .05, .9, 1.3, 2.6, .2), COPPERY, { shade: band(t + .6, .9, t + 1.3, 3.5) }); a.shape(rr(t - .05, 12.5, 1.3, 2.6, .2), COPPERY, { shade: band(t + .6, 12.5, t + 1.3, 15.1) });
    a.shape(rr(.9, t - .05, 2.6, 1.3, .2), COPPERY, { shade: band(.9, t + .6, 3.5, t + 1.3) }); a.shape(rr(12.5, t - .05, 2.6, 1.3, .2), COPPERY, { shade: band(12.5, t + .6, 15.1, t + 1.3) });
  }
  box(a, 2.8, 2.8, 10.4, 10.4, DARK, { r: .5 });
  a.shape(rr(5.2, 5.2, 5.6, 5.6, .3), GUN, { outer: false, lite: band(5.2, 5.2, 10.8, 5.8), shade: band(5.2, 10, 10.8, 10.8) });
  a.flat(rr(6.6, 6.6, 2.8, 2.8, .2), '#2c7a98');
  a.flat(rr(7, 7, 1.2, 1.1, .1), '#90dcec');
  a.flat(ell(3.9, 12.1, .35), '#58b070');
};
P.arcLamp = (a) => {
  // The world's arc lamp hanging by its ring, lit: the one bright thing here.
  a.tube('M6.2 2.4C6.2 .8 9.8 .8 9.8 2.4', .7, GUN, { spec: false });
  a.shape(rr(5.2, 2.2, 5.6, 1.7, .4), GUN, { lite: band(5.2, 2.2, 10.8, 2.7), shade: band(9.8, 2.2, 10.8, 3.9) });
  a.shape(rr(5.2, 12.8, 5.6, 2.2, .4), GUN, { lite: band(5.2, 12.8, 10.8, 13.3), shade: band(9.8, 12.8, 10.8, 15) });
  a.shape('M5.4 3.8L10.6 3.8L11.8 5.6L11.8 11.2L10.6 13L5.4 13L4.2 11.2L4.2 5.6Z', { base: '#6ea8d8', shade: '#3e78b0', deep: '#2c5c94', lite: '#a8d0ec', spec: '#e8f4fa' }, {
    shade: 'M10.8 4.2L11.8 5.6L11.8 11.2L10.8 12.6Z', lite: 'M5.2 4.2L6.4 3.8L5.4 13L4.2 11.2L4.2 5.6Z' });
  a.glow(ell(8, 8.4, 2.5, 3), '#d4ecf8', { core: ell(8, 8.2, 1.4, 1.9) });
  for (const x of [4.6, 11.4]) a.tube(`M${x} 4.4L${x} 12.4`, .6, DARK, { spec: false });
  a.tube('M8 3.8L8 5', .5, DARK, { spec: false }); a.tube('M8 11.8L8 13', .5, DARK, { spec: false });
};
P.arcModule = (a) => {
  // A dark casing with the arc's coil across its window, two violet prongs above and four gold pins below.
  for (const x of [5.2, 10.6]) { a.flat(band(x, .8, x + .8, 3), '#a48cd8'); a.flat(band(x + .15, .8, x + .4, 1.8), '#e4d8ff'); }
  for (const x of [2.8, 5.6, 9.2, 12]) { a.flat(band(x, 12.6, x + 1.2, 15), '#b8983c'); a.flat(band(x, 12.6, x + .5, 15), '#e0c868'); }
  box(a, 1, 2.8, 14, 10, GUN, { r: .6, spec: true });
  a.shape(rr(2.6, 4.6, 10.8, 6, .3), DARK, { outer: false, deep: band(2.6, 4.6, 13.4, 5.3) });
  a.flat(band(3.2, 6.3, 12.8, 8.9), '#c88446');
  // The turns of the coil, hard dark bars on the copper.
  for (const x of [3.8, 5.4, 7, 8.6, 10.2, 11.8]) a.flat(poly(x, 6.3, x + .55, 6.3, x + .95, 8.9, x + .4, 8.9), '#4c2830');
  a.flat(band(3.2, 6.3, 12.8, 6.7), '#ecb882');
  a.flat(ell(13.4, 11.7, .35), '#a48cd8');
};
P.helmetLamp = (a) => {
  // The lamp clipped to the helmet: a short barrel, its lit lens at the right, its bracket and two feet.
  a.tube('M7.6 10L5.2 13.2', .9, DARK, { spec: false }); a.tube('M8.2 10L10.6 13.2', .9, DARK, { spec: false });
  a.shape(rr(3.2, 12.9, 3.2, 1.3, .3), GUN, {}); a.shape(rr(9.4, 12.9, 3.2, 1.3, .3), GUN, {});
  a.shape('M3.2 5.8L13 5L13 9.6L3.2 9.4C2.6 8.4 2.6 6.8 3.2 5.8Z', STEEL, { lite: 'M3.2 5.8L13 5L13 6.2L3.2 6.8Z', shade: 'M3.2 8.4L13 8.4L13 9.6L3.2 9.4Z', spec: 'M4.6 6.4L9.6 6.1L9.6 6.6L4.6 6.9Z', lines: ['M5.2 5.6L5.2 9.4', 'M6.4 5.6L6.4 9.4'] });
  a.shape(rr(12.6, 3.9, 1.8, 6.8, .4), GUN, { shade: band(13.8, 3.9, 14.4, 10.7), lite: band(12.6, 3.9, 13.1, 10.7) });
  a.glow(rr(14.1, 4.3, 1.7, 6, .4), '#e8b83c', { core: rr(14.4, 4.9, .8, 3.6, .3) });
};
P.deckDrive = (a) => {
  // The ship's deck drive: a housing, its board on top, its checkered core, its shaft and amber tip.
  a.shape('M1.8 4.2L3 2.6L9 2.6L10 4.2Z', { base: '#2e7a56', shade: '#1a5040', deep: '#123a30', lite: '#54ba74', spec: '#c8f4c0' }, { lite: 'M3 2.6L9 2.6L9.4 3.2L2.6 3.2Z' });
  for (const x of [3.2, 5, 6.8]) a.flat(band(x, 3.2, x + 1, 4), '#c8a840');
  a.shape('M10 8.8L13 8.8L13 11L10 11Z', STEEL, { lite: band(10, 8.8, 13, 9.4), shade: band(10, 10.4, 13, 11) });
  a.shape(rr(12.8, 8.4, 2.6, 3, .4), BRASS, { lite: band(12.8, 8.4, 15.4, 9), shade: band(14.6, 8.4, 15.4, 11.4) });
  box(a, 1, 4.2, 9.6, 8.8, STEEL, { r: .5, spec: true });
  a.shape(band(2.2, 6, 6.8, 12), COPPERY, { outer: false, lines: ['M2.2 7.5L6.8 7.5', 'M2.2 9L6.8 9', 'M2.2 10.5L6.8 10.5', 'M3.7 6L3.7 12', 'M5.3 6L5.3 12'], shade: band(5.8, 6, 6.8, 12), lite: band(2.2, 6, 6.8, 6.6) });
  a.flat(ell(8.6, 6.4, .4), '#6cc05c');
  rivets(a, [8.6, 11.8]);
};
P.fuelPump = (a) => {
  // A squat barrel of a pump, a brass band round it, its two pipes standing up.
  for (const x of [3.6, 10.2]) {
    a.shape(rr(x, 1.2, 2.2, 6, .3), GUN, { lite: band(x, 1.2, x + .7, 7.2), shade: band(x + 1.5, 1.2, x + 2.2, 7.2) });
    a.shape(rr(x - .4, .8, 3, 1.2, .3), DARK, { lite: band(x - .4, .8, x + 2.6, 1.2) });
  }
  a.shape('M2.6 6.8L13.4 6.8L14.8 8.6L14.8 12.8L13.4 14.8L2.6 14.8L1.2 12.8L1.2 8.6Z', STEEL, {
    lite: 'M1.2 8.6L2.6 6.8L13.4 6.8L14 7.6L2.4 8.6L1.2 9.6Z', shade: 'M11.8 6.8L13.4 6.8L14.8 8.6L14.8 12.8L13.4 14.8L11.8 14.8Z', deep: 'M1.6 13.4L14.4 13.4L13.4 14.8L2.6 14.8Z' });
  a.shape(band(6.2, 6.8, 9.8, 14.8), COPPERY, { lite: band(6.2, 6.8, 7.4, 14.8), shade: band(8.8, 6.8, 9.8, 14.8), lines: ['M7 6.8L7 14.8', 'M9 6.8L9 14.8'] });
  a.line('M3 9.6L5.2 9.6M3 11.6L5.2 11.6', { w: 1 });
};
P.engineBlock = (a) => {
  // The ship's engine shrunk to a block: the housing, the fan seen through it, the cone at its back.
  a.shape('M9.4 5.4L15.2 2.4L15.4 13.6L9.4 10.8Z', { base: '#4a5262', shade: '#2e3248', deep: '#20233a', lite: '#6e7688', spec: '#b0bac8' }, {
    lite: 'M9.4 5.4L15.2 2.4L15.2 4L9.4 6.8Z', shade: 'M9.4 9.4L15.4 11L15.4 13.6L9.4 10.8Z', lines: ['M11.6 4.4L11.6 11.8', 'M13.4 3.4L13.4 12.8'] });
  a.shape(rr(.6, 1.8, 9.8, 12.8, .5), STEEL, { lite: band(.6, 1.8, 10.4, 2.6), shade: band(9.4, 1.8, 10.4, 14.6), deep: band(.6, 13.4, 10.4, 14.6) });
  a.shape(band(1.4, 3, 9.6, 5), DARK, { outer: false, lines: ['M1.4 3.6L9.6 3.6', 'M1.4 4.3L9.6 4.3'] });
  a.shape(ell(5.4, 9, 3.6), DARK, { outer: true, deep: ell(5, 8.5, 2.8) });
  a.shape(cog(5.4, 9, 3, 6), GUN, { outer: false, lite: ell(4.8, 8.2, 2) });
  a.shape(ell(5.4, 9, 1), STEEL, { outer: false, shade: .25 });
  hazard(a, 1.4, 13.2, 7.8, .9);
};
P.jetpack = (a) => {
  // The hero's two blue bottles, strapped, their nozzles under.
  for (const x of [1.8, 9.2]) {
    a.shape(rr(x + 1, .9, 3, 1.7, .3), GUN, { lite: band(x + 1, .9, x + 4, 1.4) });
    a.shape(rr(x + .9, 12.8, 3.2, 2.2, .3), DARK, { lite: band(x + .9, 12.8, x + 4.1, 13.3), deep: band(x + .9, 14.3, x + 4.1, 15) });
    a.shape(rr(x, 2.2, 5, 11, 1.3), CAN, { lite: band(x + .7, 2.2, x + 2, 13.2), spec: band(x + 1, 3.4, x + 1.6, 9.8), shade: band(x + 3.5, 2.2, x + 5, 13.2), deep: band(x, 12.2, x + 5, 13.2) });
  }
  for (const y of [5, 9.8]) a.shape(rr(.7, y, 14.6, 1.5, .2), GUN, { lite: band(.7, y, 15.3, y + .5), shade: band(.7, y + 1, 15.3, y + 1.5) });
  rivets(a, [8, 5.7], [8, 10.5]);
};
P.helmet = (a) => {
  // The hero's helmet, as he wears it: an amber shell, a dark band over the brow, the wide grey-rimmed visor, a box at the ear, the collar under.
  a.shape(rr(3, 13, 10, 2.1, .5), GUN, { lite: band(3, 13, 13, 13.5), shade: band(3, 14.4, 13, 15.1) });
  a.shape('M1.2 8.4C1 4.4 4 1 8.4 1C12.6 1 15.2 4 15.2 8C15.2 11 13.8 13.2 11.6 14L5.2 14C2.8 13.4 1.4 11.2 1.2 8.4Z', SUIT, {
    lite: 'M1.6 6.4C2.2 3.4 4.8 1.2 8.4 1.2C5.6 2.4 3.6 4 3 6.6Z', deep: 'M1.8 11C2.6 12.6 3.8 13.6 5.2 14L11.6 14C12.4 13.6 13 13.2 13.6 12.6C10 13 5 13 1.8 11Z', shade: 'M2.2 9.6L6 12.8L12 12.8L5 12.4Z' });
  a.shape('M3.8 3.4C5.4 2 7 1.4 8.8 1.4C10.8 1.4 12.6 2.2 13.8 3.8L12.8 4.6C10.6 3.4 7 3.2 4.6 4.4Z', DARK, { outer: false, edge: true, lite: 'M6 2.2L10 2L10 2.6L6 2.8Z' });
  a.shape('M5.6 5.2C8.4 4.4 12 4.6 14.8 5.8C15.4 7.6 15.2 9.8 14 11.6C11.4 12.2 8 12.2 6 11.4C5 9.6 4.8 7 5.6 5.2Z', { base: '#7c828c', shade: '#54586a', deep: '#3a3e4c', lite: '#a0a6ae', spec: '#d0d4d8' }, { outer: false, edge: true });
  a.shape('M6.4 6C8.8 5.4 11.8 5.6 14.2 6.6C14.6 8 14.4 9.6 13.6 10.8C11.2 11.4 8.6 11.4 6.8 10.8C6 9.2 5.8 7.4 6.4 6Z', { base: '#171d2a', shade: '#10141f', deep: '#0b0e16', lite: '#2a3650', spec: '#a8c4dc' }, { outer: false,
    lite: 'M6.4 6C8.8 5.4 11.8 5.6 14.2 6.6L14.3 7.2C11.6 6.4 8.6 6.4 6.3 7Z', deep: 'M6.6 10C9 10.8 11.6 10.6 13.8 10L13.6 10.8C11.2 11.4 8.6 11.4 6.8 10.8Z' });
  a.flat('M8 6.6L10.4 6.4L9.8 7.4L7.8 7.6Z', '#6c8ca8');
  a.shape(rr(2.2, 6.8, 2.2, 3.4, .4), DARK, { lite: band(2.2, 6.8, 4.4, 7.3) });
};
P.suit = (a) => {
  // The suit laid out flat: amber, the belt and straps grey, the collar's ring, red-brown toward the hem as the hero wears it.
  a.shape('M4.6 2.2L11.4 2.2L14.6 4L15.4 11.6L13 12.4L12.2 7.2L12 14.4L9 15.2L8 11.6L7 15.2L4 14.4L3.8 7.2L3 12.4L.6 11.6L1.4 4Z', SUIT, {
    lite: 'M4.6 2.2L8 2.2L7 5L4 6L3.6 7L3 12.4L2 12L2.4 4.2Z', shade: 'M11.4 2.2L14.6 4L15.4 11.6L14 12L13.4 5.4L12.2 7.2L12 14.4L10.6 14.8L10.8 3.6Z', deep: 'M4 13.4L7.4 13.4L7 15.2L4 14.4ZM8.6 13.4L12 13.4L12 14.4L9 15.2Z',
    lines: ['M8 3.4L8 11.6', 'M3.8 7.2L3.2 4.4', 'M12.2 7.2L12.8 4.4'] });
  a.shape(rr(3.8, 9, 8.4, 1.3, .15), GUN, { lite: band(3.8, 9, 12.2, 9.4), shade: band(3.8, 9.9, 12.2, 10.3) });
  a.flat(rr(7.1, 8.8, 1.8, 1.7, .2), '#c0c8d0');
  a.shape(ell(8, 2.6, 2.7, 1.3), DARK, { outer: false, deep: ell(8, 2.8, 2, .8) });
  a.tube('M5.6 3.4L5.2 9', .8, GUN, { spec: false, bare: true }); a.tube('M10.4 3.4L10.8 9', .8, GUN, { spec: false, bare: true });
  a.flat(rr(5.5, 5.4, 1.3, 1, .15), '#c0c8d0');
};
P.cape = (a) => {
  // The cloak bundled on itself, tied at the top: sand canvas in a few long folds, its teal lining turned out at both corners.
  const CANVAS = { base: '#a8895e', shade: '#755c44', deep: '#544234', lite: '#c8ac80', spec: '#e0cca4' };
  const LINING = { base: '#4a7470', shade: '#325450', deep: '#243e3c', lite: '#70989a', spec: '#a8c8c8' };
  a.shape('M8 1.8C11 1.8 12.8 3.6 13.4 6.4C14 9 14.8 11 15.2 13L13.6 14.8L2.4 14.8L.8 13C1.2 11 2 9 2.6 6.4C3.2 3.6 5 1.8 8 1.8Z', CANVAS, {
    lite: 'M8 1.8C5.4 1.8 3.8 3.4 3 6C2.4 8.8 1.8 10.8 1.4 12.6L4.4 13.4C4.6 9.6 5.4 5 8 1.8Z', shade: 'M10.4 2.6C12 3.4 13 4.8 13.4 6.4C14 9 14.8 11 15.2 13L13.6 14.8L11.8 14.8C12 10 11.4 6 10.4 2.6Z', deep: 'M3 13.6L13.4 13.6L13.6 14.8L2.4 14.8Z',
    lines: ['M6.6 3.2C5.2 6.4 4.4 10 4.4 13.6', 'M8.8 3.2C8.2 6.8 8 10.4 8.2 14.4', 'M10.4 3.2C11.4 6.6 11.8 10.4 11.8 14'] });
  a.shape('M.8 13L3.6 13.2L4.4 14.8L2.4 14.8Z', LINING, { outer: false, shade: 'M2 14L4 14L4.4 14.8L2.4 14.8Z' });
  a.shape('M15.2 13L12.4 13.2L11.6 14.8L13.6 14.8Z', LINING, { outer: false, shade: 'M12 14L14 14L13.6 14.8L11.6 14.8Z' });
  a.shape(rr(6.2, 1.2, 3.6, 1.3, .4), SOIL, { lite: band(6.2, 1.2, 9.8, 1.6) });
};

// ---------- what lives ----------
P.cloth = (a) => {
  // A bolt of red cloth, wound and tied with brass, its end trailing down at the right.
  const RED = { base: '#94403a', shade: '#682c30', deep: '#4c2028', lite: '#b86254', spec: '#d89078' };
  a.shape('M11 9.6L14.6 9.6L14.8 13.4L12.8 14.4L11 13.8Z', { base: '#c8b888', shade: '#94845c', deep: '#6c6048', lite: '#e4d8ac' }, { shade: 'M11 12L14.8 12L14.8 13.4L12.8 14.4L11 13.8Z' });
  a.shape('M3 3.8L14.4 3.4C15.4 5 15.4 9 14.4 10.8L3 11.2Z', RED, {
    lite: 'M3 3.8L14.4 3.4L14.8 5L3 5.4Z', shade: 'M3 8.8L15 8.4L14.4 10.8L3 11.2Z', lines: ['M10.8 3.6L10.8 11', 'M12.4 3.6C13 6 13 8.6 12.4 11'] });
  a.shape(band(6.8, 3.6, 8.8, 11), BRASS, { outer: false, lite: band(6.8, 3.6, 7.5, 11), shade: band(8.2, 3.6, 8.8, 11) });
  a.shape(ell(3, 7.5, 2.2, 3.8), RED, { shade: .5, lines: ['M3 5.4C2 6 2 9 3 9.6', 'M3.4 6.4C2.8 7 2.8 8 3.4 8.6'] });
};
P.hide = (a) => {
  // A skin stretched to dry on crossed poles: its four legs tied out, the belly's seam down the middle.
  const SKIN = { base: '#8f6c48', shade: '#644a36', deep: '#47362c', lite: '#ae8a60', spec: '#c8a880' };
  a.tube('M1 1.2L14.8 14.8', .9, WOOD, { spec: false }); a.tube('M15 1.2L1.2 14.8', .9, WOOD, { spec: false });
  a.shape('M2 2.2L5 4.4L8 3.6L11 4.4L14 2.2L12.6 6L13 10L14 13.8L11 11.8L8 12.4L5 11.8L2 13.8L3 10L3.4 6Z', SKIN, {
    lite: 'M2 2.2L5 4.4L8 3.6L7 7L4.8 9L3.2 8Z', shade: 'M11 4.4L14 2.2L12.6 6L13 10L14 13.8L11 11.8L10.4 7Z', deep: 'M5 11.8L8 12.4L11 11.8L10.4 10.8L5.4 10.8Z',
    lines: ['M8 3.6L7.8 7L8.2 12.4', 'M5.2 6.4L6.4 7.2', 'M10.4 8.6L9.4 9.4', 'M5.6 9.6L6.4 9'] });
  a.flat('M9.6 5.6L10.8 5.8L10.4 7L9.4 6.8Z', '#47362c');
};
P.fat = (a) => {
  // A lump of rendered fat: pale, greasy, a streak of meat still in it.
  const TALLOW = { base: '#d4c8a4', shade: '#a0916e', deep: '#786c54', lite: '#ece4c8', spec: '#faf6e6' };
  a.shape('M3 5L6.4 2.2L10.4 2L13.4 4.4L14.8 8.2L13.6 12L10 14.4L5.6 14.4L2.4 12.2L1.4 8.4Z', TALLOW, {
    lite: 'M1.4 8.4L3 5L6.4 2.2L10.4 2L7 4.4L4.4 7L2.6 9.4Z', shade: 'M14.8 8.2L13.6 12L10 14.4L5.6 14.4L8.6 12.6L12 9.6L12.8 6.8Z', deep: 'M13.6 12L10 14.4L7 14.4L11 12.4Z', spec: 'M5 5L7.4 3.4L8 3.8L5.6 5.6Z',
    lines: ['M9 5C10.2 6 10.4 7.4 10 8.4'] });
  a.flat('M9.8 8.4L12.4 7L12.2 8.2L10.2 9.8Z', '#a86a5c'); a.flat('M4.4 11L5.8 10L5.8 11L4.6 11.8Z', '#a86a5c');
};
P.seed = (a) => {
  // A swollen seed splitting at its crown, a shoot up; its small twin beside it.
  const HUSK = { base: '#a0703c', shade: '#6e4a2e', deep: '#4c3426', lite: '#c8964e', spec: '#e4bc78' };
  a.shape('M12.2 9.8C13.8 9.8 15 11.4 15 13C15 14.4 13.6 15.2 12.2 15.2C10.8 15.2 9.6 14.4 9.6 13C9.6 11.4 10.8 9.8 12.2 9.8Z', HUSK, { shade: .5, lines: ['M12.2 10.4C11.6 11.8 11.6 13.4 12.2 14.8'] });
  a.shape('M6 5.2C8.6 5.2 11 7.6 11 10.8C11 13.6 8.8 15 6 15C3.2 15 1 13.6 1 10.8C1 7.6 3.4 5.2 6 5.2Z', HUSK, {
    lite: 'M1.4 9.4C2 7 4 5.4 6 5.4C4.6 7 3.4 9 3 11.4L1.2 11.4Z', shade: 'M8 5.8C9.8 7 11 8.8 11 10.8C11 13.6 8.8 15 6 15C4.8 15 3.8 14.8 3 14.2C6.4 14.2 9 11 8 5.8Z', spec: 'M2.6 8.8C3 8 3.6 7.4 4.2 7L3.8 8.4L3.2 10Z',
    lines: ['M6 5.6C5 8 4.8 11 5.6 14.6', 'M6.6 5.6C7.8 8 8 11 7.4 14.4'] });
  a.tube('M6 5.6C6 4.4 6.2 3.8 6.6 3', .7, SPROUT, { spec: false });
  a.shape('M6.4 3.4C5.4 1.8 3.6 1.2 2 1.8C2.8 3.4 4.6 4.2 6.4 3.4Z', SPROUT, { shade: 'M6.4 3.4C4.6 4.2 2.8 3.4 2 1.8C3.4 2.6 5 3 6.4 3.4Z' });
  a.shape('M6.6 3.2C7.8 1.6 9.8 1 11.4 1.6C10.6 3.2 8.6 4 6.6 3.2Z', SPROUT, { shade: 'M6.6 3.2C8.6 4 10.6 3.2 11.4 1.6C9.8 2.4 8.2 2.8 6.6 3.2Z' });
};
P.spore = (a) => {
  // Three spore sacs, wet and thin-skinned: a colour nothing else here has.
  const SAC = { base: '#8a68c0', shade: '#5a4090', deep: '#3e2c6c', lite: '#b898e0', spec: '#ecdcfa' };
  const sac = (x, y, r) => {
    a.shape(ell(x, y, r), SAC, { shade: r * .28, deep: `M${x - r} ${y + r * .55}C${x - r * .3} ${y + r * .95} ${x + r * .6} ${y + r * .8} ${x + r} ${y + r * .2}L${x + r} ${y + r}L${x - r} ${y + r}Z`, lite: ell(x - r * .25, y - r * .28, r * .55), spec: ell(x - r * .36, y - r * .42, r * .2) });
    a.line(`M${x + r * .1} ${y - r * .1}C${x + r * .4} ${y + r * .1} ${x + r * .4} ${y + r * .4} ${x + r * .2} ${y + r * .6}`, { c: '#3e2c6c', w: 1 });
  };
  sac(3.6, 3.6, 2.6); sac(12.8, 3.8, 2.1); sac(8.4, 10.6, 4.5);
};

// ---------- what drifts: the debris of space in its small picture ----------
P.cargoCrate = (a) => {
  // The crate in space: boards in a dark steel frame, a brass plate on its front.
  a.shape(rr(.9, 2.1, 14.2, 10, .5), GUN, { lite: band(.9, 2.1, 15.1, 2.9), shade: band(14.2, 2.1, 15.1, 12.1), deep: band(.9, 11.3, 15.1, 12.1) });
  a.shape(band(2.2, 3.3, 13.8, 10.9), WOOD, { outer: false, deep: band(2.2, 3.3, 13.8, 3.8), shade: band(12.8, 3.3, 13.8, 10.9), lines: ['M2.2 5.8L13.8 5.6', 'M2.2 8.4L13.8 8.2', 'M4.8 3.4L4.6 5.7', 'M3.6 5.8L3.4 8.4', 'M12 8.3L11.8 10.9'] });
  a.shape(rr(5.6, 5.4, 4.8, 3.2, .2), { base: '#c8a040', shade: '#8e7028', deep: '#685428', lite: '#e8c868', spec: '#f8e8a8' }, { lite: band(5.6, 5.4, 10.4, 6), shade: band(9.7, 5.4, 10.4, 8.6) });
  rivets(a, [1.6, 2.9], [14.4, 2.9], [1.6, 11.3], [14.4, 11.3]);
};
P.cargoHold = (a) => {
  // A section of the hold: a steel frame with a hazard edge, crates still lashed inside.
  a.shape(rr(.7, 1.6, 14.6, 13, .4), STEEL, { lite: band(.7, 1.6, 15.3, 2.4), shade: band(14.4, 1.6, 15.3, 14.6), deep: band(.7, 13.7, 15.3, 14.6) });
  a.shape(band(2.8, 3.2, 13.6, 13), DARK, { outer: false, deep: band(2.8, 3.2, 13.6, 4.2) });
  rivets(a, [1.7, 3], [1.7, 5.6], [1.7, 8.2], [1.7, 10.8], [1.7, 13.2]);
  a.shape(rr(4, 4.6, 5.4, 4, .15), WOOD, { lite: band(4, 4.6, 9.4, 5), shade: band(8.6, 4.6, 9.4, 8.6), lines: ['M4 6.4L9.4 6.4', 'M6.6 4.6L6.6 6.4'] });
  a.shape(rr(4, 8.8, 9, 4.2, .15), WOOD, { lite: band(4, 8.8, 13, 9.2), shade: band(12, 8.8, 13, 13), lines: ['M4 10.9L13 10.9', 'M7 8.8L7 10.9', 'M10 10.9L10 13'] });
  a.shape(rr(9.8, 5.8, 3, 2.9, .15), WOOD, { shade: band(12, 5.8, 12.8, 8.7), lines: ['M9.8 7.2L12.8 7.2'] });
  rivets(a, [14.2, 3], [14.2, 13.2]);
};
P.cargoContainer = (a) => {
  // A green corrugated box, its door end stove in, broken boards hanging out to the right.
  const OLIVE = { base: '#536e4e', shade: '#38503c', deep: '#28382e', lite: '#769270', spec: '#a8c0a0' };
  for (const [y, x] of [[4.8, 14.8], [7, 15.4], [9.4, 14.6], [11.4, 15]]) a.shape(`M9 ${y}L${x} ${y - .4}L${x - .5} ${y + 1.1}L9 ${y + 1.4}Z`, WOOD, { shade: `M9 ${y + 1}L${x - .5} ${y + .7}L${x - .5} ${y + 1.1}L9 ${y + 1.4}Z`, lite: `M9 ${y}L${x} ${y - .4}L${x} ${y}L9 ${y + .4}Z` });
  a.shape('M.8 2.8L11.6 2.4L10.8 4.8L11.8 6.6L10.6 8.8L11.6 10.8L10.4 13L.8 13.6Z', OLIVE, {
    lite: 'M.8 2.8L11.6 2.4L11 3.4L.8 3.8Z', deep: 'M.8 12.6L10.8 12.2L10.4 13L.8 13.6Z',
    lines: ['M2.4 3L2.4 13.4', 'M4 3L4 13.4', 'M5.6 3L5.6 13.2', 'M7.2 3L7.2 13.2', 'M8.8 3L8.8 13.2'] });
  a.flat(band(1.5, 5.2, 7, 5.9), '#c8c0a8'); a.flat(band(1.5, 6.4, 4.4, 7), '#c8c0a8');
  a.flat('M8.4 8.6L10 8.2L9.8 10.4L8.6 10.6Z', '#7c5038');
};
P.cableRack = (a) => {
  // A server rack, its cables torn out in colours to the right.
  a.tube('M9 4.4C12 3 13 5 15 3.4', .8, { base: '#a84034', shade: '#742a2c', spec: '#d0624c' }, { spec: false });
  a.tube('M9 7.6C11.6 6.4 12.6 8.8 15.2 7.4', .8, { base: '#4e8e50', shade: '#34663c', spec: '#78b470' }, { spec: false });
  a.tube('M9 10.6C11 10.4 12.4 12.4 15 11', .8, { base: '#c0983c', shade: '#8c6c2c', spec: '#e0c060' }, { spec: false });
  a.tube('M9 13C11 12.4 12 14.8 14.8 14.2', .8, { base: '#3e68a8', shade: '#2a4a80', spec: '#6c94cc' }, { spec: false });
  a.shape(rr(.8, 1, 9.6, 14.2, .3), GUN, { lite: band(.8, 1, 10.4, 1.7), shade: band(9.6, 1, 10.4, 15.2), deep: band(.8, 14.4, 10.4, 15.2) });
  for (const [y, c] of [[2.2, '#6cc05c'], [5.4, '#e0a040'], [8.6, '#6cc05c'], [11.8, '#48505e']]) {
    a.shape(rr(1.9, y, 7.4, 2.5, .15), STEEL, { outer: false, lite: band(1.9, y, 9.3, y + .5), shade: band(1.9, y + 1.8, 9.3, y + 2.5) });
    a.flat(band(2.6, y + .9, 6.2, y + 1.6), '#23273a');
    a.flat(ell(7.8, y + 1.2, .4), c);
  }
};
P.hullSection = (a) => {
  // A whole chunk of hull: a porthole still lit, plates and rivets, a hazard strip, wires torn at the right.
  a.tube('M12.4 8L15 9.6', .55, { base: '#a84034', shade: '#742a2c' }, { spec: false, bare: true });
  a.tube('M12.6 10L15.2 11.6', .55, { base: '#c0983c', shade: '#8c6c2c' }, { spec: false, bare: true });
  a.shape('M.8 2.4L10.6 2L13.2 5L12.4 7.2L13.6 9.2L12.6 11.4L13 14L.8 14.2Z', STEEL, {
    lite: 'M.8 2.4L10.6 2L11 3L.8 3.4Z', shade: 'M10.6 2L13.2 5L12.4 7.2L13.6 9.2L12.6 11.4L13 14L11.2 14L11 3.4Z',
    lines: ['M6.6 2.4L6.6 11', 'M.8 11L13 11', 'M6.6 6.4L12.6 6.4'] });
  a.shape(ell(3.7, 6.4, 2.2), GUN, {});
  a.shape(ell(3.7, 6.4, 1.5), { base: '#3f78a8', shade: '#2a5488', deep: '#1c3c68', lite: '#8cc4e0' }, { outer: false, lite: ell(3.3, 6, .8) });
  rivets(a, [8, 3.6], [10, 3.6], [8, 8], [10.6, 8], [8, 10], [10.6, 10]);
  a.shape(band(.8, 11, 13, 14.2), GUN, { outer: false, lite: band(.8, 11, 13, 11.4) });
  hazard(a, 1.4, 12, 7, 1.4);
  a.flat('M10 12L11.2 11.8L11.4 13.2L10.2 13.4Z', '#7c5038');
};
P.machineBlock = (a) => {
  // A wrecked machine: its grey case and vents, the big toothed wheel at its side, a hazard strip along its foot.
  a.tube('M12 11.6L15.2 11.6', 1.2, GUN, { spec: false });
  a.shape(rr(.8, 2.6, 10.4, 11.2, .4), STEEL, { lite: band(.8, 2.6, 11.2, 3.4), shade: band(10.4, 2.6, 11.2, 13.8), deep: band(.8, 12.9, 11.2, 13.8) });
  a.shape(rr(2, 4, 4.8, 3.6, .2), GUN, { outer: false, lines: ['M2 4.9L6.8 4.9', 'M2 5.8L6.8 5.8', 'M2 6.7L6.8 6.7'], lite: band(2, 4, 6.8, 4.4) });
  hazard(a, 1.6, 10.8, 6.6, 1.6);
  a.shape(cog(10.6, 7, 4.8, 9), GUN, { lite: ell(9.8, 6, 3), shade: .5 });
  a.shape(ell(10.6, 7, 1.7), DARK, { outer: false, deep: ell(10.3, 6.7, 1.2) });
  a.flat(ell(10.6, 7, .5), '#a7afbc');
  a.tube('M2.4 2.6C2.4 1.2 4 1 5 2', .5, { base: '#a84034', shade: '#742a2c' }, { spec: false, bare: true });
};
P.podCluster = (a) => {
  // Escape pods on their strut, no longer flying: scuffed white hulls with an orange nose, a slit of a port, not an eye.
  const HULLW = { base: '#b4b6b6', shade: '#7c8088', deep: '#5c6068', lite: '#d4d4d0', spec: '#ececea' };
  const CAP = { base: '#b0662c', shade: '#7c4426', deep: '#58301e', lite: '#d08c48', spec: '#ecbc80' };
  const pod = (x, y, r, tilt) => {
    const c = Math.cos(tilt), s = Math.sin(tilt), at = (u, v) => [x + u * c - v * s, y + u * s + v * c];
    a.shape(ell(x, y, r, r), HULLW, { shade: r * .26, lite: ell(x - r * .3, y - r * .35, r * .5) });
    a.shape(poly(...at(r * .42, -r * .9), ...at(r * 1.02, -r * .4), ...at(r * 1.02, r * .4), ...at(r * .42, r * .9)), CAP, { outer: false, edge: true, shade: poly(...at(r * .42, r * .3), ...at(r * 1.02, r * .1), ...at(r * 1.02, r * .4), ...at(r * .42, r * .9)) });
    a.flat(poly(...at(-r * .5, -r * .34), ...at(r * .1, -r * .4), ...at(r * .1, -r * .06), ...at(-r * .5, -r * .02)), '#1d2c40');
    a.line(`M${at(-r * .86, r * .46).join(' ')}L${at(r * .36, r * .86).join(' ')}`, { w: 1 });
  };
  a.tube('M5 5L8 8L5.4 12', 1.2, DARK, { spec: false });
  a.tube('M8 8L10 8', 1.2, DARK, { spec: false });
  pod(4.6, 4.2, 3.5, -.5);
  pod(5.6, 12.2, 2.9, .4);
  pod(11.8, 7.8, 3.5, .1);
};
P.shieldSlab = (a) => {
  // The shielded slab: a thick dark block, its yellow warning plate and hazard foot, the only bright things on it.
  const LEAD = { base: '#54565e', shade: '#393b46', deep: '#292a34', lite: '#74767e', spec: '#a8aab0' };
  a.shape('M.8 3.8L2 2.4L13.2 2.6L15.2 4.4L14.6 7L15.2 9.4L14.8 13.6L1 13.8L.8 10.6L1.4 7.6Z', LEAD, {
    lite: 'M2 2.4L13.2 2.6L14 3.4L1.6 3.4Z', shade: 'M12.8 3L15.2 4.4L14.6 7L15.2 9.4L14.8 13.6L13 13.6Z', deep: 'M1 11.2L14.8 11.2L14.8 13.6L1 13.8Z', lines: ['M4.6 3L4.6 11', 'M11.4 3L11.4 11'] });
  a.shape(rr(5.8, 3.8, 4.4, 5.8, .2), { base: '#c8a030', shade: '#8e7028', deep: '#685428', lite: '#e4c45c', spec: '#f4e49c' }, { lite: band(5.8, 3.8, 10.2, 4.3), shade: band(9.5, 3.8, 10.2, 9.6) });
  a.flat(poly(8, 6.2, 7, 4.5, 9, 4.5), '#1a1820'); a.flat(poly(7.5, 7, 6, 7.1, 6.8, 8.8), '#1a1820'); a.flat(poly(8.5, 7, 10, 7.1, 9.2, 8.8), '#1a1820'); a.flat(ell(8, 6.8, .4), '#1a1820');
  hazard(a, 1.6, 11.6, 12.8, 1.5);
  rivets(a, [2.8, 4.4], [2.8, 9.6], [13, 4.8], [13, 9.6]);
};
P.supplyPod = (a) => {
  // A supply drop: the ribbed amber dome and its bolted hatch, sat on its grey saucer.
  a.shape('M1.4 10.6L14.6 10.6L13 12.6L11.6 13.2L11.6 14.6L4.4 14.6L4.4 13.2L3 12.6Z', GUN, { lite: 'M1.4 10.6L14.6 10.6L14 11.2L2 11.2Z', shade: 'M10.6 11.2L14.6 10.6L13 12.6L11.6 13.2L11.6 14.6L10.2 14.6Z', deep: 'M4.4 13.8L11.6 13.8L11.6 14.6L4.4 14.6Z' });
  a.shape('M2.8 10.8C2.8 6.8 4.8 2.4 8 1.8C11.2 2.4 13.2 6.8 13.2 10.8Z', SUIT, {
    lite: 'M3 10C3.4 7 5.2 3.2 8 2.2C6.8 4.6 5.8 7.6 5.6 10.8L2.8 10.8Z', shade: 'M10.4 3.8C12 5.6 13.2 8.2 13.2 10.8L10.8 10.8C10.8 8.4 10.8 6 10.4 3.8Z', deep: 'M2.8 9.8L13.2 9.8L13.2 10.8L2.8 10.8Z', lines: ['M4 9.8L12 9.8'] });
  a.shape(rr(6, 5.4, 4, 3.4, .3), GUN, { lite: band(6, 5.4, 10, 5.8), shade: band(9.3, 5.4, 10, 8.8) });
  a.flat(band(6.7, 6.2, 9.3, 8), '#1a1c28');
  a.flat(band(7.8, 6.2, 8.2, 8), '#5c6474');
};
P.tankCluster = (a) => {
  // The tanks in space: three blue bottles in a steel rack, one valve painted red-brown.
  const tank = (x, y, w, h) => {
    a.shape(rr(x + w * .32, y - 1.1, w * .36, 1.4, .2), GUN, { lite: band(x + w * .32, y - 1.1, x + w * .68, y - .7) });
    a.shape(rr(x, y, w, h, w * .3), CAN, { lite: band(x + w * .14, y, x + w * .4, y + h), spec: band(x + w * .2, y + 1, x + w * .32, y + h * .5), shade: band(x + w * .68, y, x + w, y + h), deep: band(x, y + h - .8, x + w, y + h) });
  };
  tank(1, 5, 4.2, 9); tank(10.8, 5.8, 4.2, 8.2); tank(5.6, 2.4, 4.8, 11.6);
  a.tube('M9 1.2L11.4 1.2', .8, { base: '#a0603a', shade: '#70402e', spec: '#d09060' }, { spec: false });
  a.shape(rr(.5, 8.4, 15, 1.4, .15), STEEL, { lite: band(.5, 8.4, 15.5, 8.8), shade: band(.5, 9.3, 15.5, 9.8) });
  a.shape(rr(.5, 13.4, 15, 1.7, .2), GUN, { lite: band(.5, 13.4, 15.5, 13.9), shade: band(.5, 14.5, 15.5, 15.1) });
  a.flat(rr(6.8, 10.4, 2.4, 1.6, .1), '#d4d4cc');
};
P.ration = (a) => {
  // A bar of pressed ration in waxed paper, torn back off its end; a red-brown band and an inked stamp.
  const PAPER = { base: '#988a64', shade: '#6a6048', deep: '#4c4636', lite: '#bcae84', spec: '#d8cca4' };
  const BAR = { base: '#6c4a34', shade: '#4a3226', deep: '#33221c', lite: '#8c6444', spec: '#a8805a' };
  const RUST = { base: '#8a3e30', shade: '#5e2a26', deep: '#401c1c', lite: '#a85a44' };
  const at = (s, t) => [1.9 + s * .78 + t * .62, 12.3 - s * .62 + t * .78];
  const q = (...st) => poly(st.flatMap(([s, t]) => at(s, t)));
  const l = (...st) => 'M' + st.map(([s, t]) => at(s, t).map((v) => v.toFixed(2)).join(' ')).join('L');
  a.shape(q([10.4, -1.9], [15, -1.9], [15, 1.9], [10.4, 1.9]), BAR, {
    lite: q([10.4, -1.9], [15, -1.9], [15, -1.2], [10.4, -1.2]), shade: q([10.4, 1.1], [15, 1.1], [15, 1.9], [10.4, 1.9]), lines: [l([12.8, -1.9], [12.8, 1.9]), l([13.4, -.4], [14.2, .2])] });
  a.shape(q([0, -2.2], [11.4, -2.2], [10.8, -1.3], [11.6, -.3], [10.9, .7], [11.5, 1.5], [11, 2.2], [0, 2.2]), PAPER, {
    lite: q([0, -2.2], [11.4, -2.2], [11, -1.5], [0, -1.5]), shade: q([0, 1.3], [11.4, 1.3], [11.4, 2.2], [0, 2.2]),
    lines: [l([0, -2.2], [1.5, 0], [0, 2.2]), l([6.4, -2], [6.8, 2])] });
  a.shape(q([3, -2.2], [5.6, -2.2], [5.6, 2.2], [3, 2.2]), RUST, { outer: false, lite: q([3, -2.2], [5.6, -2.2], [5.6, -1.5], [3, -1.5]), shade: q([3, 1.3], [5.6, 1.3], [5.6, 2.2], [3, 2.2]) });
  a.line(l([7.6, -1.1], [9.8, -1.1], [9.8, .9], [7.6, .9], [7.6, -1.1]), { c: '#3c2a24', w: 1.2 });
  a.flat(poly(at(8.2, -.4).concat(at(9.2, -.4), at(9.2, .3), at(8.2, .3))), '#4a3428');
  // The torn flap, lifting off the bar.
  a.shape(q([11.4, -2.2], [12.8, -3], [12.2, -1.9], [11.1, -1.4]), PAPER, { shade: .25 });
};
P.cannedMeat = (a) => {
  // A tall tin of stewed meat: an oxblood label with a cream band, the tin's beads above and below, rust at the foot.
  const TIN = { base: '#a6acb6', shade: '#686e80', deep: '#464a5c', lite: '#d2d6dc', spec: '#f4f4f0' };
  const LABEL = { base: '#8c3a32', shade: '#62282a', deep: '#441c20', lite: '#ac5444' };
  const CREAM = { base: '#d0c4a6', shade: '#9a9076', deep: '#746c5a', lite: '#ece2c6' };
  a.shape('M3.4 3.8L12.6 3.8L12.6 13.6C12.6 15.4 3.4 15.4 3.4 13.6Z', TIN, {
    lite: band(4.4, 3.8, 6.2, 15.4), spec: band(4.9, 4.6, 5.4, 5.6), shade: band(10.6, 3.8, 12.6, 15.6), deep: 'M3.4 14.2C5 15 11 15 12.6 14.2L12.6 15.6L3.4 15.6Z',
    lines: ['M3.4 5C5 5.9 11 5.9 12.6 5', 'M3.4 13C5 13.9 11 13.9 12.6 13'] });
  a.shape('M3.4 6C5 7 11 7 12.6 6L12.6 12C11 13 5 13 3.4 12Z', LABEL, { outer: false, lite: band(4.4, 5.4, 6.2, 13.2), shade: band(10.6, 5.4, 12.6, 13.2) });
  a.shape('M3.4 7.8C5 8.8 11 8.8 12.6 7.8L12.6 10.2C11 11.2 5 11.2 3.4 10.2Z', CREAM, { outer: false, lite: band(4.4, 7.4, 6.2, 11.4), shade: band(10.6, 7.4, 12.6, 11.4) });
  a.flat(ell(7.9, 9.5, 1.7, .95), '#6c3428');
  a.line('M6.8 9.3C7.4 8.9 8.4 8.9 9.1 9.4', { c: '#e4d6b4', w: 1.1 });
  a.shape(ell(8, 3.8, 4.6, 1.4), TIN, { lite: ell(7.2, 3.5, 3.2, .8), shade: 'M10 3.2L12.6 3.6L12.4 4.6L10 4.8Z', lines: ['M4.6 3.9C5.6 4.6 10.4 4.6 11.4 3.9'] });
  // Rust where water sat on the bottom rim; a crease in the label.
  a.flat('M10.4 14.5C11.1 14.2 11.8 13.9 12.2 13.5L12.3 14.1C11.8 14.6 11.1 14.9 10.6 14.9Z', '#7a4a32');
  a.flat(ell(11.4, 14.2, .35, .25), '#4e2c22');
  a.line('M10.6 6.8L11.2 7.6', { w: 1 });
};
// Never an item: the pod paste a hero is printed with, drawn for its meal slot.
P.podPaste = (a) => {
  // A squeeze tube from the pod, crimped and dented where it was pressed, the pod's teal band; a ribbon of grey paste from its nozzle.
  const TUBE = { base: '#c2c6c4', shade: '#868c92', deep: '#5e646c', lite: '#e4e6e2', spec: '#f6f6f2' };
  const TEAL = { base: '#4b8f96', shade: '#2d606e', deep: '#20465a', lite: '#7cc0c0' };
  const PASTE = { base: '#8e908a', shade: '#62645e', deep: '#474844', lite: '#b4b6ae', spec: '#dcdcd4' };
  const at = (s, t) => [2.2 + s * .707 + t * .707, 13.8 - s * .707 + t * .707];
  const q = (...st) => poly(st.flatMap(([s, t]) => at(s, t)));
  const l = (...st) => 'M' + st.map(([s, t]) => at(s, t).map((v) => v.toFixed(2)).join(' ')).join('L');
  a.shape(q([1.8, -3], [9.6, -2.2], [10.8, -1.2], [11.4, -.8], [11.4, .8], [10.8, 1.2], [9.6, 2.2], [1.8, 3]), TUBE, {
    lite: q([1.8, -3], [9.6, -2.2], [10.8, -1.2], [10.4, -.8], [9.4, -1.5], [1.8, -2.1]), shade: q([1.8, 2], [9.4, 1.4], [10.8, 1.2], [9.6, 2.2], [1.8, 3]),
    lines: [l([3.8, -1.2], [5, .2], [4.2, 1.8]), l([7.4, -1.6], [8.2, -.2])] });
  a.shape(q([6, -2.47], [7.4, -2.33], [7.4, 2.33], [6, 2.47]), TEAL, { outer: false, lite: q([6, -2.47], [7.4, -2.33], [7.4, -1.8], [6, -1.9]) });
  a.shape(q([-.2, -3.3], [1.9, -3.3], [1.9, 3.3], [-.2, 3.3]), TUBE, { lite: q([-.2, -3.3], [1.9, -3.3], [1.9, -2.6], [-.2, -2.6]), shade: q([-.2, 2.5], [1.9, 2.5], [1.9, 3.3], [-.2, 3.3]),
    lines: [-2.2, -1.1, 0, 1.1, 2.2].map((t) => l([.2, t], [1.5, t])) });
  a.shape(q([11.3, -.8], [12.8, -.8], [12.8, .8], [11.3, .8]), STEEL, { shade: q([11.3, .3], [12.8, .3], [12.8, .8], [11.3, .8]), lines: [l([12, -.8], [12, .8])] });
  a.tube('M11.3 4.7C12.3 3.5 13.9 3.2 14.4 4.3C14.8 5.3 13.6 6 12.9 5.3', 1.3, PASTE);
};
// A loose picture in the art folder, no longer called by the game: the screwdriver across the square.
P['tool_screwdriver'] = (a) => {
  a.tube('M9.4 6.6L14.6 1.4', .9, STEEL);
  a.shape('M14.2 1L15.4 .6L15 1.8L14.4 1.8Z', STEEL, {});
  a.shape('M2 11.6L7.8 5.8L10.2 8.2L4.4 14C3.4 15 2.4 15 1.6 14.4C.9 13.6 1 12.6 2 11.6Z', SUIT, {
    lite: 'M2 11.6L7.8 5.8L8.6 6.6L2.8 12.4Z', shade: 'M9.4 7.4L10.2 8.2L4.4 14C3.4 15 2.4 15 1.6 14.4Z',
    lines: ['M3.4 10.2L5.8 12.6', 'M4.8 8.8L7.2 11.2', 'M6.2 7.4L8.6 9.8'] });
  a.shape('M7.4 6.2L9 4.6L11.4 7L9.8 8.6Z', GUN, { shade: .3 });
};
return P;
}

// The one line, in these paths' units: a tenth of the 18 px the 64 px picture spans (a margin of one round the 16), so a world pixel is 11.25 of them.
const first = (p) => bake({ w: 16, h: 16, margin: 1, units: 10, px: 3.5555555, K: 4, light: [-0.6, -0.8],
  W: { outer: 0.9 * 11.25, edge: 0.26 * 11.25, panel: 0.14 * 11.25 }, draw: p.draw });

Subjects.items = function () {
  for (const p of PICS) out('items/' + p.id, first(p));
  // An empty state of one of the ten: the same calls in the same order, only the part that changes goes dark.
  const pic = (id) => PICS.find((p) => p.id === id);
  out('items/accumulator_empty', first({ draw: (a) => pic('accumulator').draw({ ...a,
    glow: (d) => a.flat(d, '#2c2158'),
    line: (d, o) => a.line(d, { ...o, c: '#1d1640' }),
    flat: (d, c, o) => (c === '#e4d6ff' || c === '#b493ff' ? a.flat('M0 0', c) : a.flat(d, c, o)) }) }));
  out('items/drone_empty', first({ draw: (a) => pic('drone').draw({ ...a,
    glow: (d) => a.flat(d, '#232738'),
    shape: (d, m, o) => a.shape(d, m === M.blue ? M.dark : m, o) }) }));
  // The rest, in the logical picture's own pixels, on the ship's materials.
  for (const [id, draw] of Object.entries(more())) out(id.startsWith('tool_') ? id : 'items/' + id, bake({ w: 16, h: 16, light: [-0.6, -0.8], draw }));
};

})();
