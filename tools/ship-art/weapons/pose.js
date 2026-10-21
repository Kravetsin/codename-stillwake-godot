// The hero holding a weapon, as the game's Figure builds it (Rig.cs, Figure.cs): parts placed at their
// joints in world pixels, feet at (0, 0), facing +x. Plain browser JS: the page embeds this file and
// Node evaluates it for the contact sheet.
var POSE = (function () {
  var H = 2625 / 1000 * 16, W = 950 / 1000 * 16;
  var RIG = {
    hip: H * 0.42, shoulder: H * 0.61, neck: H * 0.71, hipApart: W * 0.14, back: W * 0.62,
    arm: H * 0.145, leg: H * 0.42 * 0.5,
    // the fist's middle, from the elbow: where the hand closes, a little short of the arm's end
    fist: 4.8,
  };
  var JOINT = {
    helmet: [6.5, 12], torso: [6, 0], pack: [0, 0], upperArm: [2.5, 2.5], forearm: [2.5, 2.9],
    thigh: [3, 2.5], shin: [3, 2.18],
  };
  var rad = function (d) { return d * Math.PI / 180; };
  var from = function (p, deg, len) { return [p[0] + Math.cos(rad(deg)) * len, p[1] + Math.sin(rad(deg)) * len]; };

  // Screen angles in degrees: 0 forward (+x), 90 down. The swing turns clockwise, angles rising.
  var POSES = {
    rest: { upper: 84, fore: 48, label: 'Rest' },
    raised: { upper: -95, fore: -50, label: 'Raised' },
    strike: { upper: 35, fore: 95, label: 'Strike' },
    through: { upper: 60, fore: 120, label: 'Through' },
  };

  // A limb from its joint along two screen angles: each part's sprite hangs down (+y) unturned.
  function limb(list, at, upperDeg, lowerDeg, len, up, low, far) {
    var bend = from(at, upperDeg, len);
    list.push({ part: low, at: bend, rot: lowerDeg - 90, far: far });
    list.push({ part: up, at: at, rot: upperDeg - 90, far: far });
    return bend;
  }

  /** The draws, back to front, and where the weapon's grip, head and edges are. */
  function scene(w, pose) {
    var list = [], shoulder = [0, -RIG.shoulder];
    limb(list, shoulder, 97, 75, RIG.arm, 'upperArm', 'forearm', true);
    limb(list, [-RIG.hipApart, -RIG.hip], 94, 88, RIG.leg, 'thigh', 'shin', true);
    limb(list, [RIG.hipApart, -RIG.hip], 84, 96, RIG.leg, 'thigh', 'shin', false);
    list.push({ part: 'pack', at: [-RIG.back, -RIG.shoulder - H * 0.02], rot: 0 });
    list.push({ part: 'torso', at: [0, -RIG.neck], rot: 0 });
    list.push({ part: 'helmet', at: [0, -RIG.neck], rot: 0 });
    var elbow = from(shoulder, pose.upper, RIG.arm), fist = from(elbow, pose.fore, RIG.fist);
    var turn = pose.fore - w.hold;
    list.push({ part: 'weapon', at: fist, rot: turn, joint: w.grip });
    var front = [];
    limb(front, shoulder, pose.upper, pose.fore, RIG.arm, 'upperArm', 'forearm', false);
    list.push.apply(list, front);
    var place = function (p) {
      var dx = p[0] - w.grip[0], dy = p[1] - w.grip[1], c = Math.cos(rad(turn)), s = Math.sin(rad(turn));
      return [fist[0] + dx * c - dy * s, fist[1] + dx * s + dy * c];
    };
    return { list: list, fist: fist, elbow: elbow, shoulder: shoulder, edges: w.edge.map(place), grip: fist };
  }

  /** Each edge point's path from raised, through the strike, to the follow-through. */
  function sweep(w, steps) {
    var a = POSES.raised, b = POSES.through, paths = w.edge.map(function () { return []; });
    for (var i = 0; i <= steps; i++) {
      var t = i / steps, s = scene(w, { upper: a.upper + (b.upper - a.upper) * t, fore: a.fore + (b.fore - a.fore) * t });
      s.edges.forEach(function (p, k) { paths[k].push(p); });
    }
    return paths;
  }

  /** The farthest any edge point gets from the shoulder through the swing, in pixels. */
  function reach(w) {
    var best = 0;
    sweep(w, 90).forEach(function (path) { path.forEach(function (p) { best = Math.max(best, Math.hypot(p[0], p[1] + RIG.shoulder)); }); });
    return best;
  }

  return { RIG: RIG, JOINT: JOINT, POSES: POSES, scene: scene, sweep: sweep, reach: reach, H: H };
})();
if (typeof module !== 'undefined') module.exports = POSE;
