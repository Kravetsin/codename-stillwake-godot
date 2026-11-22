// «Триада»: a core, three lobes, three arms between them.
'use strict';
(root => {
  const TURN = Math.PI * 2 / 3, C = [255.7, 279], parts = [];
  const round = (r, a) => [C[0] + r * Math.sin(a), C[1] - r * Math.cos(a)];
  for (let i = 0; i < 3; i++) {
    const a = i * TURN + Math.PI / 3;
    parts.push({ id: `arm${i}`, pic: 'upper', parent: 'core', at: round(70, a), pivot: [188, 773], a: a - Math.PI / 2 });
    parts.push({ id: `hand${i}`, pic: 'open', parent: `arm${i}`, at: [324, 768], pivot: [662, 775], a: 0 });
  }
  for (let i = 0; i < 3; i++) parts.push({ id: `lobe${i}`, pic: 'lobe', parent: 'core', at: round(108, i * TURN), pivot: [1272, 474], a: i * TURN, k: .9 });
  parts.push({ id: 'core', pic: 'core', at: [0, 0], pivot: C });

  function move(s) {
    const m = {}, beat = Math.sin(s.t * 3), tremble = s.tell * Math.sin(s.t * 47) * .035;
    const speed = Math.min(1, Math.hypot(s.v[0], s.v[1]));
    for (let i = 0; i < 3; i++) {
      const a = i * TURN, pull = s.sleep * 100 + s.grip * 34 - s.tell * 48 - beat * 7 * (1 - s.sleep);
      m[`lobe${i}`] = {
        a: tremble + s.hurt * [.5, -.35, .3][i] + speed * .1 * Math.sin(s.t * 7 + i * 2),
        x: -pull * Math.sin(a), y: pull * Math.cos(a),
      };
      const wave = Math.sin(s.t * 4.2 + i * 2.1) * .16;
      const bend = .95 + wave - s.tell * .8 - s.grip * .95 + s.hurt * .4;
      m[`arm${i}`] = { a: -.5 + wave * .5 + s.tell * .35 + s.grip * .5, hidden: s.sleep > .6 };
      m[`hand${i}`] = { a: bend, pic: s.grip > .5 ? 'shut' : 'open', hidden: s.sleep > .6 };
    }
    m.core = { pic: s.sleep > .5 || s.dead ? 'dead' : 'core', a: tremble };
    return m;
  }
  root.RIGS = root.RIGS || {};
  root.RIGS.triad = { id: 'triad', scale: 22 / 1040, parts, move, joints: { dead: [C[0] + 512, C[1]], shut: [1183, 775] } };
})(typeof window === 'undefined' ? globalThis : window);
