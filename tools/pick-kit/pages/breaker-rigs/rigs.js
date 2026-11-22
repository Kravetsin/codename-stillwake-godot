// The other eight bodies on the one engine. Each: its pieces, the joints on its sheet, and how its states move them.
'use strict';
(root => {
  const R = root.RIGS = root.RIGS || {}, TAU = Math.PI * 2, PI = Math.PI;
  const off = s => s.sleep > .5 || s.dead > 0;
  const asleep = s => s.sleep > .6 && !s.dead;
  const fast = s => Math.min(1, Math.hypot(s.v[0], s.v[1]));
  const shake = s => s.tell * Math.sin(s.t * 47) * .03;
  const body = (id, scale, parts, joints, move) => { R[id] = { id, scale, parts, joints, move }; };
  // an arm of two pieces, the hand bending on the elbow
  const arm = (parts, n, parent, at, a, k, J, flip) => {
    parts.push({ id: `arm${n}`, pic: 'upper', parent, at, pivot: J.root, a, k, flip });
    parts.push({ id: `hand${n}`, pic: 'open', parent: `arm${n}`, at: J.elbow, pivot: J.wrist, a: 0, k, flip });
  };
  const hands = (m, n, s, shoulder, bend) => {
    m[`arm${n}`] = { a: shoulder, hidden: asleep(s) };
    m[`hand${n}`] = { a: bend, hidden: asleep(s), pic: s.grip > .5 ? 'shut' : 'open' };
  };

  // «Прорва»: a ring, four thrusters that steer, six arms into the hole
  {
    const C = [203, 265], J = { root: [180, 769], elbow: [205, 768], wrist: [556, 771] };
    const parts = [], pods = [[101, 182, -2.45], [290, 177, -.75], [93, 341, 2.45], [301, 343, .75]];
    for (let i = 0; i < 6; i++) { const a = i / 6 * TAU + .3; arm(parts, i, 'ring', [C[0] + 90 * Math.cos(a), C[1] + 70 * Math.sin(a)], a + PI + .55, 1.35, J); }
    parts.push({ id: 'ring', pic: 'ring', at: [0, 0], pivot: C });
    pods.forEach(([x, y, a], i) => parts.push({ id: `pod${i}`, pic: 'pod', parent: 'ring', at: [x, y], pivot: [938, 257], a, k: 1.2 }));
    body('maw', 22 / 385, parts, { dead: [587, 265], poddead: [1322, 257], shut: [943, 771] }, s => {
      const m = { ring: { pic: off(s) ? 'dead' : 'ring', a: Math.sin(s.t * 1.3) * .08 + shake(s) + s.grip * .35 + s.hurt * .4 } };
      for (let i = 0; i < 6; i++) {
        const w = Math.sin(s.t * 5 + i * 1.7);
        hands(m, i, s, w * .18 + s.tell * .9 - s.grip * .6, .5 + w * .25 + s.tell * 1.1 - s.grip * .6 + s.hurt);
      }
      for (let i = 0; i < 4; i++) m[`pod${i}`] = { pic: off(s) ? 'poddead' : 'pod', a: Math.sin(s.t * 2 + i) * .12 + s.tell * (i % 2 ? -.4 : .4) + fast(s) * (i % 2 ? .35 : -.35) };
      return m;
    });
  }

  // «Латаный»: a patchwork shell over a dark body, plates trailing, two arms under the beak
  {
    const J = { root: [173, 769], elbow: [211, 768], wrist: [543, 770] }, parts = [];
    parts.push({ id: 'rear', pic: 'rear', parent: 'shell', at: [105, 225], pivot: [964, 180], a: .2 });
    arm(parts, 0, 'shell', [200, 305], 1.25, 1.7, J);
    parts.push({ id: 'under', pic: 'under', parent: 'shell', at: [190, 270], pivot: [1340, 255], k: 1 });
    arm(parts, 1, 'shell', [255, 300], .95, 1.9, J);
    parts.push({ id: 'shell', pic: 'shell', at: [0, -30], pivot: [190, 255] });
    body('patched', 22 / 350, parts, { dead: [573, 255], shut: [931, 770] }, s => {
      const w = Math.sin(s.t * 4), m = {};
      m.shell = { pic: off(s) ? 'dead' : 'shell', a: w * .04 + shake(s) - s.tell * .28 + s.grip * .16 - s.hurt * .4, y: s.sleep * 22 };
      m.rear = { a: w * .1 + fast(s) * .5 + s.tell * .6 - s.sleep * .2 };
      m.under = { hidden: asleep(s) };
      for (let i = 0; i < 2; i++) { const u = Math.sin(s.t * 5 + i * 2); hands(m, i, s, u * .2 - s.tell * 1.1 - s.grip * .7 + s.hurt * .6, -.6 + u * .25 - s.tell * .5 + s.grip * .5); }
      return m;
    });
  }

  // «Саван»: a cowl, loose plates that trail, four arms out of the dark between them
  {
    const J = { root: [505, 758], elbow: [668, 756], wrist: [855, 762] }, parts = [];
    const plates = [['long', 130, 235, .42], ['short', 170, 250, .2], ['long', 215, 255, 0], ['short', 265, 250, -.2]];
    parts.push({ id: 'plate0', pic: 'long', parent: 'cowl', at: [130, 235], pivot: [1344, 92], a: .42, k: .68 });
    parts.push({ id: 'inner', pic: 'inner', parent: 'cowl', at: [205, 200], pivot: [960, 95], a: .12, k: .8 });
    for (let i = 0; i < 4; i++) arm(parts, i, 'cowl', [240 - i * 7, 260 + i * 56], -.25 + i * .22, .42, J);
    plates.slice(1).forEach(([pic, x, y, a], i) => parts.push({ id: `plate${i + 1}`, pic, parent: 'cowl', at: [x, y], pivot: pic === 'long' ? [1344, 92] : [210, 617], a, k: pic === 'long' ? .68 : .6 }));
    parts.push({ id: 'cowl', pic: 'cowl', at: [-25, -50], pivot: [200, 270] });
    body('shroud', 22 / 420, parts, { dead: [584, 270], shut: [1240, 765] }, s => {
      const m = { cowl: { pic: off(s) ? 'dead' : 'cowl', a: Math.sin(s.t * 1.7) * .05 + shake(s) - s.tell * .15 + s.grip * .2 - s.hurt * .35 } };
      const rests = [.42, .2, 0, -.2];
      for (let i = 0; i < 4; i++) {
        const w = Math.sin(s.t * 3 - i * .8);
        m[`plate${i}`] = { a: w * .07 * (1 - s.sleep) + fast(s) * .45 + s.tell * (rests[i] * 1.2 + .15) - s.sleep * rests[i] * .75 + s.hurt * (i - 1.5) * .3 };
        hands(m, i, s, w * .18 - s.tell * .5 + s.grip * (1.5 - i) * .12, -.35 + w * .3 - s.tell * .7 + s.grip * .5);
      }
      m.inner = { hidden: asleep(s) };
      return m;
    });
  }

  // «Расщеп»: two halves on sinews round a mind, arms in the gap
  {
    const J = { root: [536, 773], elbow: [618, 768], wrist: [914, 771] }, parts = [];
    parts.push({ id: 'cable0', pic: 'cable', parent: 'mind', at: [1305, 230], pivot: [150, 766], a: -2.1, k: 1 });
    parts.push({ id: 'cable1', pic: 'cable', parent: 'mind', at: [1305, 285], pivot: [150, 766], a: 2.2, k: 1 });
    parts.push({ id: 'mind', pic: 'mind', at: [-25, 0], pivot: [1344, 256], k: 1.2 });
    for (let i = 0; i < 4; i++) arm(parts, i, 'mind', [1380, 226 + i * 21], -.33 + i * .22, .78, J);
    parts.push({ id: 'top', pic: 'top', parent: 'mind', at: [1344, 210], pivot: [175, 290] });
    parts.push({ id: 'low', pic: 'low', parent: 'mind', at: [1344, 300], pivot: [940, 250] });
    body('split', 22 / 350, parts, { dead: [559, 290], shut: [1302, 771] }, s => {
      const b = Math.sin(s.t * 3), open = s.tell * 24 + b * 4 - s.sleep * 40 - s.grip * 18, m = { mind: { a: shake(s) + s.hurt * .3 } };
      m.top = { pic: off(s) ? 'dead' : 'top', y: -open, a: -s.tell * .3 + s.grip * .12 - s.hurt * .35 - fast(s) * .05 };
      m.low = { y: open, a: s.tell * .3 - s.grip * .12 + s.hurt * .3 + fast(s) * .05 };
      for (let i = 0; i < 4; i++) { const w = Math.sin(s.t * 6 + i * 1.5); hands(m, i, s, w * .16 + s.tell * (i - 1.5) * .25, w * .3 - s.grip * (i - 1.5) * .2); }
      m.cable0 = { hidden: asleep(s) }; m.cable1 = { hidden: asleep(s) };
      return m;
    });
  }

  // «Сросток»: the engine, four vertebrae that whip, the senses and arms
  {
    const J = { root: [520, 768], elbow: [633, 768], wrist: [880, 772] }, REST = [.55, -.2, -.5, -.25], parts = [];
    parts.push({ id: 'rear', pic: 'rear', at: [-105, -40], pivot: [1042, 297], k: .85 });
    for (let i = 0; i < 4; i++) parts.push({ id: `bone${i}`, pic: 'bone', parent: i ? `bone${i - 1}` : 'rear', at: i ? [212, 768] : [1042, 297], pivot: [170, 768], a: REST[i] });
    arm(parts, 0, 'front', [185, 300], 1.35, .9, J);
    parts.push({ id: 'front', pic: 'front', parent: 'bone3', at: [212, 768], pivot: [77, 250], a: .4 });
    arm(parts, 1, 'front', [215, 310], 1, 1, J);
    body('twin', 22 / 575, parts, { dead: [461, 250], reardead: [1426, 297], shut: [1268, 772] }, s => {
      const m = { rear: { pic: off(s) ? 'reardead' : 'rear', a: shake(s) + Math.sin(s.t * 2) * .05 - s.tell * .1 + s.hurt * .25 } };
      let turned = 0;
      for (let i = 0; i < 4; i++) {
        const d = Math.sin(s.t * 4 - i) * .12 * (1 - s.sleep) + REST[i] * (s.tell * 1.3 - s.grip * .9) + s.sleep * REST[i] * 2.2 + s.hurt * (i % 2 ? .4 : -.3);
        turned += d; m[`bone${i}`] = { a: d };
      }
      m.front = { pic: off(s) ? 'dead' : 'front', a: -turned * .8 + s.sleep * .15 };
      for (let i = 0; i < 2; i++) { const w = Math.sin(s.t * 5 + i * 2); hands(m, i, s, w * .2 - s.tell * .8 - s.grip * .8, -.8 + w * .25 + s.grip * .6); }
      return m;
    });
  }

  // «Венец»: four limbs over the back, each with its thruster, short arms under
  {
    const J = { root: [561, 768], elbow: [592, 768], wrist: [932, 769] }, parts = [];
    const crown = [[88, 208, -.6, false], [137, 197, -.4, false], [186, 190, PI + .4, true], [248, 199, PI + .6, true]];
    const limb = i => {
      const [x, y, a, flip] = crown[i];
      parts.push({ id: `limb${i}`, pic: 'limb', parent: 'body', at: [x, y], pivot: [963, 326], a, k: .8, flip });
      parts.push({ id: `tip${i}`, pic: 'tip', parent: `limb${i}`, at: [990, 183], pivot: [1333, 319], k: .62, flip });
    };
    limb(1); limb(2);
    for (let i = 0; i < 3; i++) arm(parts, i, 'body', [120 + i * 65, 308], 1.25, 1.3, J);
    parts.push({ id: 'body', pic: 'body', at: [0, 65], pivot: [190, 265] });
    limb(0); limb(3);
    body('crown', 22 / 380, parts, { dead: [574, 265], tipdead: [181, 831], shut: [1316, 769] }, s => {
      const m = { body: { pic: off(s) ? 'dead' : 'body', a: Math.sin(s.t * 2) * .04 + shake(s) + s.grip * .15 - s.hurt * .35 } };
      for (let i = 0; i < 4; i++) {
        const side = i < 2 ? -1 : 1, w = Math.sin(s.t * 3 + i * 1.3) * .07 * (1 - s.sleep);
        m[`limb${i}`] = { a: w + side * (s.tell * .4 - s.sleep * .75 - s.grip * .3) - fast(s) * .3 + s.hurt * side * .3 };
        m[`tip${i}`] = { pic: off(s) ? 'tipdead' : 'tip', a: w - s.tell * .35 + s.sleep * .7 + s.grip * .3 };
      }
      for (let i = 0; i < 3; i++) { const w = Math.sin(s.t * 6 + i * 2); hands(m, i, s, w * .25 - s.grip * .9 - s.tell * .4, -.7 + w * .3 + s.grip * .5); }
      return m;
    });
  }

  // «Лик»: the plate, the hub, five chains of three that stream behind and sweep forward to grip
  {
    const SOCKETS = [[896, 172], [890, 222], [890, 271], [890, 319], [906, 363]], parts = [];
    SOCKETS.forEach((at, i) => {
      parts.push({ id: `root${i}`, pic: 'root', parent: 'hub', at, pivot: [1398, 256], a: (2 - i) * .2, k: .62 });
      parts.push({ id: `middle${i}`, pic: 'middle', parent: `root${i}`, at: [1296, 259], pivot: [234, 767], k: .8 });
      parts.push({ id: `claw${i}`, pic: 'claw', parent: `middle${i}`, at: [150, 767], pivot: [623, 762], k: .9 });
    });
    parts.push({ id: 'hub', pic: 'hub', parent: 'plate', at: [110, 255], pivot: [1000, 255] });
    parts.push({ id: 'plate', pic: 'plate', at: [105, 0], pivot: [190, 255] });
    body('visage', 22 / 510, parts, { dead: [574, 255], hook: [995, 758] }, s => {
      const m = { plate: { pic: off(s) ? 'dead' : 'plate', a: Math.sin(s.t * 1.8) * .04 + shake(s) - s.tell * .2 + s.grip * .12 + s.hurt * .35 } };
      m.hub = { x: s.sleep * 75 + s.grip * 15 };
      for (let i = 0; i < 5; i++) {
        const side = i < 2 ? 1 : i > 2 ? -1 : 0, w = Math.sin(s.t * 4 - i * .9) * (1 - s.sleep);
        const curl = side || 1;
        m[`root${i}`] = { a: w * .12 + (2 - i) * (s.tell * .3 - fast(s) * .08 - s.sleep * .12) + side * s.grip * 2.2 + s.hurt * w * .4, hidden: asleep(s) };
        m[`middle${i}`] = { a: w * .2 + curl * (s.tell * .5 - s.grip * .15), hidden: asleep(s) };
        m[`claw${i}`] = { a: w * .3 + curl * (s.tell * .4 + s.grip * .5), hidden: asleep(s), pic: s.grip > .5 ? 'hook' : 'claw' };
      }
      return m;
    });
  }

  // «Клубок»: a core wound in bands that lift off it to work
  {
    const J = { root: [540, 770], elbow: [612, 758], wrist: [927, 769] }, parts = [];
    parts.push({ id: 'long1', pic: 'long', parent: 'core', at: [340, 262], pivot: [810, 328], a: PI });
    arm(parts, 0, 'core', [295, 190], -.3, 1.35, J);
    arm(parts, 1, 'core', [295, 320], .3, 1.35, J);
    parts.push({ id: 'core', pic: 'core', at: [0, 0], pivot: [192, 255] });
    parts.push({ id: 'long0', pic: 'long', parent: 'core', at: [44, 250], pivot: [810, 328], k: .92 });
    parts.push({ id: 'short', pic: 'short', parent: 'core', at: [85, 180], pivot: [1240, 294], a: .55, k: .85 });
    parts.push({ id: 'cross', pic: 'cross', parent: 'core', at: [95, 330], pivot: [90, 800], a: .1, k: .85 });
    body('knot', 22 / 410, parts, { dead: [576, 255], shut: [1313, 769] }, s => {
      const w = Math.sin(s.t * 3), loose = s.tell + s.hurt * .7 + (w + 1) * .07 * (1 - s.sleep) - s.grip * .1;
      const m = { core: { pic: off(s) ? 'dead' : 'core', a: shake(s) + fast(s) * Math.sin(s.t * 5) * .15 + s.hurt * .3 } };
      m.long0 = { a: -loose * .65 }; m.long1 = { a: -loose * .65 };
      m.short = { a: -loose * .9 }; m.cross = { a: loose * .5 };
      for (let i = 0; i < 2; i++) { const u = Math.sin(s.t * 5 + i * 2), side = i ? 1 : -1; hands(m, i, s, u * .2 + side * s.tell * .7 - side * s.grip * .35, u * .3 + side * .3 - side * s.grip * .5); }
      return m;
    });
  }
})(typeof window === 'undefined' ? globalThis : window);
