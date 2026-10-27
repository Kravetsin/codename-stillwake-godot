'use strict';
// The hero composed as Figure.cs draws it, standing: its parts recoloured by ramp swaps (SuitArt.Of), placed on the
// rig (Rig.cs) and drawn with nearest sampling as Godot draws a rotated sprite. No file access: pictures come from
// get(name), so the page runs the same code (build.js inlines it). Images are { w, h, data: RGBA bytes }.
function make(A) {
  // Rig.cs: 5250 thousandths of a cell tall, 1900 wide, 8 px a cell.
  const H = 5.25 * 8, W = 1.9 * 8;
  const Hip = H * 0.42, Shoulder = H * 0.61, Neck = H * 0.71, HipApart = W * 0.14, Back = W * 0.62;
  const Arm = H * 0.145, Leg = Hip * 0.5;
  const JOINTS = { helmet: [6.5, 12], torso: [6, 0], pack: [0, 0], upperArm: [2.5, 2.5], forearm: [2.5, 2.9], thigh: [3, 2.5], shin: [3, 2.18] };
  // Figure.cs: the far limbs in the suit darkened by this; the lamp's bulb on the helmet's brow, from the neck.
  const FAR = 0.7, LAMP_ON = [4, -9];

  const num = c => (typeof c === 'string' ? parseInt(c.slice(1, 7), 16) : c);
  // Suits.Shade: each channel times by, or toward white by -by below 0.
  function shade(c, by) {
    const ch = s => { const v = (c >> s) & 255; return Math.round(by >= 0 ? v * by : v + (255 - v) * -by); };
    return (ch(16) << 16) | (ch(8) << 8) | ch(0);
  }
  const shadesOf = c => [shade(c, -0.35), c, shade(c, 0.72), shade(c, 0.42)];
  const darker = (ramp, by) => ramp.map(c => shade(num(c), by));

  const blank = (w, h, fill) => {
    const img = { w, h, data: new Uint8ClampedArray(w * h * 4) };
    if (fill != null) for (let i = 0; i < w * h; i++) img.data.set([fill >> 16, (fill >> 8) & 255, fill & 255, 255], i * 4);
    return img;
  };

  /** A copy of img with each colour of every from ramp swapped for the same shade of its to ramp. */
  function recolour(img, swaps) {
    const map = new Map();
    for (const [from, to] of swaps) from.forEach((c, i) => map.set(num(c), num(to[i])));
    const out = { w: img.w, h: img.h, data: new Uint8ClampedArray(img.data) };
    for (let i = 0; i < img.w * img.h; i++) {
      if (!out.data[i * 4 + 3]) continue;
      const c = (out.data[i * 4] << 16) | (out.data[i * 4 + 1] << 8) | out.data[i * 4 + 2];
      const t = map.get(c);
      if (t != null) { out.data[i * 4] = t >> 16; out.data[i * 4 + 1] = (t >> 8) & 255; out.data[i * 4 + 2] = t & 255; }
    }
    return out;
  }

  const lookOf = look => ({ suit: 0, skin: A.SKIN_DRAWN, hair: 0, hairColour: A.HAIR_DRAWN, face: 0, ...look });
  const swapsOf = (look, far) => [
    [A.SUIT_DRAWN, shadesOf(far ? shade(A.SUITS[look.suit], FAR) : A.SUITS[look.suit])],
    [A.SKINS[A.SKIN_DRAWN], far ? darker(A.SKINS[look.skin], FAR) : A.SKINS[look.skin]],
    [A.HAIRS[A.HAIR_DRAWN], A.HAIRS[look.hairColour]],
  ];

  /** The head's layers alone, in the helmet's template: head, face, hair, and suited the shell and the glass. */
  function headLayers(look, get, glass = true) {
    look = lookOf(look);
    const sw = swapsOf(look, false);
    // fine: the trial head drawn twice as fine (hd.js), each picture with its own joint; its hair, bigger than the
    // helmet, is hidden under it.
    const [p, density] = look.fine ? ['hd_', 2] : ['hero_', 1];
    const names = [p + 'head', p + 'face_' + look.face];
    if (!(look.fine && look.suited && glass)) names.push(p + 'hair_' + look.hair);
    const layers = names.map(n => ({ img: recolour(get(n), sw), face: true, density, joint: get(n).joint || JOINTS.helmet }));
    if (look.suited && glass) layers.push({ img: recolour(get('hero_helmet_shell'), sw) }, { img: get('hero_visor') });
    return layers;
  }

  /**
   * The parts of a standing hero, back to front, each { img, at: [x, y] from the feet, turn, joint, face }.
   * look: { suit: 0-3, suited, skin: 0-6, hair: 0-4, hairColour: 0-5, face: 0-2, closed, bubble }; closed draws
   * today's helmet, its visor opaque; bubble the suit with the head bare, for a glass bubble drawn round it.
   */
  // Stand at rest (Pose.cs): each limb's [upper, lower] bend; a pose may also tilt the body and lift it.
  const STAND = { fa: [0, 0.18], ba: [0, 0.22], fl: [0.02, -0.04], bl: [-0.02, -0.04], tilt: 0, lift: 0 };
  const rot = ([x, y], t) => [x * Math.cos(t) - y * Math.sin(t), x * Math.sin(t) + y * Math.cos(t)];
  /** A point of the body at rest, from the feet, where the pose's tilt round the middle and its lift put it. */
  function bodied(p, pose) {
    const r = rot([p[0], p[1] + H / 2], pose.tilt || 0);
    return [r[0], r[1] - H / 2 - (pose.lift || 0)];
  }
  /** A pixel p of a picture on the head (joint and density its own) in the world, from the feet, facing +x. */
  function onHead(look, p, joint, density) {
    const pose = { ...STAND, ...look.pose };
    const v = rot([(p[0] - joint[0]) / density, (p[1] - joint[1]) / density], (look.turn || 0) + (pose.tilt || 0));
    const neck = bodied([0, -Neck], pose);
    return [neck[0] + v[0], neck[1] + v[1]];
  }

  /**
   * The parts of a hero, back to front, each { img, at: [x, y] from the feet, turn, joint, face, flip }.
   * look: { suit: 0-3, suited, skin: 0-6, hair: 0-4, hairColour: 0-5, face: 0-2, closed, bubble, fine, crown, turn,
   * pose, flip }; closed draws today's helmet, its visor opaque; bubble the suit with the head bare, for a glass bubble
   * drawn round it; crown the trial hair without what a chain draws (hd_crown_n); pose the limbs' bends, tilt and lift;
   * flip faces −x. extra: { behind, beforeHead } ops placed at the back and under the head, already in the world's way.
   */
  function figure(look, get, extra = {}) {
    look = lookOf(look);
    const pose = { ...STAND, ...look.pose };
    const ops = [...(extra.behind || [])];
    const put = (img, at, turn, joint, more) => ops.push({ img, at: bodied(at, pose), turn: turn + pose.tilt, joint, flip: look.flip, ...more });
    const part = (name, far) => recolour(get((look.suited ? 'hero_' : 'hero_jumpsuit_') + name), swapsOf(look, far));
    // Rotations run clockwise.
    const limb = (upper, lower, at, size, [u, l], far) => {
      const elbow = [at[0] + Math.sin(u) * size, at[1] + Math.cos(u) * size];
      put(part(lower, far), elbow, -(u + l), JOINTS[lower]);
      put(part(upper, far), at, -u, JOINTS[upper]);
    };
    const shoulder = [0, -Shoulder], neck = [0, -Neck];
    limb('upperArm', 'forearm', shoulder, Arm, pose.ba, true);
    limb('thigh', 'shin', [-HipApart, -Hip], Leg, pose.bl, true);
    limb('thigh', 'shin', [HipApart, -Hip], Leg, pose.fl, false);
    if (look.suited) put(part('pack', false), [-Back, -Shoulder - H * 0.02], 0, [0, 0]);
    put(part('torso', false), neck, 0, JOINTS.torso);
    ops.push(...(extra.beforeHead || []));
    if (look.closed) put(part('helmet', false), neck, 0, JOINTS.helmet);
    else {
      const layers = headLayers(look, get, !look.bubble);
      if (look.crown != null) layers[2] = { ...layers[0], img: recolour(get('hd_crown_' + look.crown), swapsOf(look, false)), joint: get('hd_crown_' + look.crown).joint };
      for (const l of layers) put(l.img, neck, look.turn || 0, l.joint || JOINTS.helmet, { face: l.face, density: l.density });
    }
    limb('upperArm', 'forearm', shoulder, Arm, pose.fa, false);
    return ops;
  }

  /**
   * Draws ops onto img with the feet at origin, sampling each part at the pixel's middle as a GPU does. light(x, y,
   * op) gives [r, g, b] multipliers for a pixel drawn at x, y in pixels of the world; none leaves colours as they are.
   * zoom: screen pixels a pixel of the world, as the camera's; an op's density: its pixels a pixel of the world (2 for
   * a picture drawn twice as fine), its joint in its own pixels. smooth averages 4×4 samples a pixel, as mipmaps would.
   */
  function draw(img, ops, origin, light, { zoom = 1, smooth = false } = {}) {
    const n = smooth ? 4 : 1;
    for (const op of ops) {
      // flip mirrors the op round the origin's column, as a hero facing −x is drawn.
      const { img: src, at, turn, joint } = op, d = op.density || 1, f = op.flip ? -1 : 1;
      const c = Math.cos(turn), s = Math.sin(turn);
      const ox = origin[0] + f * at[0] * zoom, oy = origin[1] + at[1] * zoom;
      const r = (Math.hypot(src.w, src.h) / d + 2) * zoom;
      for (let y = Math.floor(oy - r); y <= oy + r; y++) for (let x = Math.floor(ox - r); x <= ox + r; x++) {
        if (x < 0 || y < 0 || x >= img.w || y >= img.h) continue;
        let sr = 0, sg = 0, sb = 0, sa = 0;
        for (let j = 0; j < n; j++) for (let i = 0; i < n; i++) {
          const dx = f * (x + (i + 0.5) / n - ox) / zoom, dy = (y + (j + 0.5) / n - oy) / zoom;
          const u = Math.floor((c * dx + s * dy) * d + joint[0]), v = Math.floor((-s * dx + c * dy) * d + joint[1]);
          if (u < 0 || v < 0 || u >= src.w || v >= src.h) continue;
          const si = (v * src.w + u) * 4, a = src.data[si + 3] / 255;
          sr += src.data[si] * a; sg += src.data[si + 1] * a; sb += src.data[si + 2] * a; sa += a;
        }
        if (!sa) continue;
        const a = sa / (n * n), col = [sr / sa, sg / sa, sb / sa];
        const m = light ? light((x + 0.5) / zoom, (y + 0.5) / zoom, op) : [1, 1, 1];
        const di = (y * img.w + x) * 4, da = img.data[di + 3] / 255, oa = a + da * (1 - a);
        for (let k = 0; k < 3; k++) img.data[di + k] = Math.round((Math.min(255, col[k] * m[k]) * a + img.data[di + k] * da * (1 - a)) / oa);
        img.data[di + 3] = Math.round(oa * 255);
      }
    }
    return img;
  }

  /** img over base at (x, y), alpha-blended, each pixel times light(x, y) if given. */
  function over(base, img, x0, y0, light) {
    for (let y = 0; y < img.h; y++) for (let x = 0; x < img.w; x++) {
      const X = x + x0, Y = y + y0;
      if (X < 0 || Y < 0 || X >= base.w || Y >= base.h) continue;
      const si = (y * img.w + x) * 4, a = img.data[si + 3] / 255;
      if (!a) continue;
      const m = light ? light(X + 0.5, Y + 0.5) : [1, 1, 1];
      const di = (Y * base.w + X) * 4, da = base.data[di + 3] / 255, oa = a + da * (1 - a);
      for (let k = 0; k < 3; k++) base.data[di + k] = Math.round((Math.min(255, img.data[si + k] * m[k]) * a + base.data[di + k] * da * (1 - a)) / oa);
      base.data[di + 3] = Math.round(oa * 255);
    }
    return base;
  }

  /** img k times larger, each pixel a block. */
  function scale(img, k) {
    const o = blank(img.w * k, img.h * k);
    for (let y = 0; y < o.h; y++) for (let x = 0; x < o.w; x++) {
      const s = (((y / k) | 0) * img.w + ((x / k) | 0)) * 4;
      o.data.set(img.data.subarray(s, s + 4), (y * o.w + x) * 4);
    }
    return o;
  }

  return { H, W, Neck, Shoulder, Hip, JOINTS, LAMP_ON, A, STAND, shade, shadesOf, darker, blank, recolour, headLayers, figure, bodied, onHead, draw, over, scale };
}

if (typeof module !== 'undefined') module.exports = make;
