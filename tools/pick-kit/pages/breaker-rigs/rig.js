// A breaker's rig: pieces cut from a generated sheet, hung on joints. Points are in the sheet's own px.
// part: { id, pic, parent?, at: joint in the parent's sheet px (root: from the body's middle), pivot: the same joint in its
// own sheet px, a: rest turn, k?: scale, flip?: mirrored across its own axis }. Parts draw in list order.
'use strict';
(root => {
  function posed(rig, s) {
    const moves = rig.move(s), at = {}, out = [];
    const visit = p => {
      if (at[p.id]) return at[p.id];
      const m = moves[p.id] || {}, k = p.k || 1;
      let o = p.at, a = (p.a || 0) + (m.a || 0);
      if (m.x || m.y) o = [o[0] + (m.x || 0), o[1] + (m.y || 0)];
      if (p.parent) {
        const q = rig.byId[p.parent], P = visit(q), x = (o[0] - q.pivot[0]) * P.k, y = (o[1] - q.pivot[1]) * P.k * P.flip;
        o = [P.o[0] + x * Math.cos(P.a) - y * Math.sin(P.a), P.o[1] + x * Math.sin(P.a) + y * Math.cos(P.a)];
        a = P.a + a * P.flip;
      }
      return (at[p.id] = { part: p, o, a, k, flip: p.flip ? -1 : 1, pic: m.pic || p.pic, hidden: m.hidden });
    };
    for (const p of rig.parts) out.push(visit(p));
    return out;
  }
  // c stands at the body's middle in world px; res picks the cut: 'hd' (the game's) or 'hi'
  function draw(c, rig, s, images, res = 'hd') {
    let n = -1;
    for (const p of posed(rig, s)) {
      n++;
      if (p.hidden) continue;
      const box = rig.cuts[p.pic], im = images[`${rig.id}_${p.pic}_${res}`];
      if (!box || !im || !im.complete || !im.naturalWidth) continue;
      c.save();
      if (s.dead) {
        // death: every piece goes its own way, turning
        const a = n * 2.399, d = s.dead, out = Math.hypot(p.o[0], p.o[1]) > 8 ? Math.atan2(p.o[1], p.o[0]) : a;
        c.translate(Math.cos(out + .3 * Math.sin(a)) * d * (9 + n % 5 * 3), Math.sin(out + .3 * Math.sin(a)) * d * (9 + n % 4 * 3));
        c.globalAlpha *= Math.max(0, Math.min(1, (1.6 - d) / .6));
      }
      c.scale(rig.scale, rig.scale);
      c.translate(p.o[0], p.o[1]); c.rotate(p.a + (s.dead ? s.dead * Math.sin(n * 1.9) * 2.2 : 0)); c.scale(p.k, p.k * p.flip);
      // a swapped picture has the same joint at its own place on the sheet
      const joint = (rig.joints && rig.joints[p.pic]) || p.part.pivot;
      c.drawImage(im, box[0] - joint[0], box[1] - joint[1], box[2], box[3]);
      c.restore();
    }
  }
  function ready(rig) {
    rig.byId = Object.fromEntries(rig.parts.map(p => [p.id, p]));
    return rig;
  }
  // the states every body answers to, each 0..1 but t (seconds) and v (its flight along its own axes, -1..1)
  const rest = o => ({ t: 0, v: [0, 0], sleep: 0, tell: 0, grip: 0, hurt: 0, dead: 0, ...o });
  root.BreakerRig = { posed, draw, ready, rest };
})(typeof window === 'undefined' ? globalThis : window);
