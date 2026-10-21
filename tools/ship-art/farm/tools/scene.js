// The "in context" strip: a stretch of ship room, back wall and floor from the game's own tiles, the furnace and
// the smelter as the game draws them, and two capsules: one leafy and dark, one ripe with its lamp on.
const path = require('path');
const { img, blit, glow, shade, loadPng } = require('./lib.js');
const { frame, ART } = require('./compose.js');

const W = 176, H = 80, FLOOR = 66;
function room() {
  const im = img(W, H, '#1a1e27');
  const back = loadPng(path.join(ART, 'back_scrap.png'));
  for (let y = 8; y < FLOOR; y += back.h) for (let x = 0; x < W; x += back.w) blit(im, back, x, y);
  const wall = loadPng(path.join(ART, 'wall_iron.png'));
  // ceiling and floor: the iron wall's face, lit edge on the room side
  for (let x = 0; x < W; x += 64) { blit(im, wall, x, -56); blit(im, wall, x, FLOOR); }
  for (let x = 0; x < W; x++) {
    for (const [y, c] of [[7, [0x70, 0x77, 0x87]], [FLOOR, [0xb0, 0xb7, 0xc5]], [FLOOR + 1, [0x9a, 0xa3, 0xb5]], [H - 1, [0x70, 0x77, 0x87]]]) im.data.set([...c, 255], (y * W + x) * 4);
  }
  return im;
}
function scene(capsule) {
  let im = room();
  const at = (pic, x) => blit(im, pic, x, FLOOR - pic.h);
  at(loadPng(path.join(ART, 'machine_furnace.png')), 6);
  at(loadPng(path.join(ART, 'machine_smelter.png')), 42);
  at(frame(capsule, 2, false, { room: 1 }), 84);
  at(frame(capsule, 3, false, { room: 1 }), 126);
  im = shade(im, 0.72);
  // the lit capsule: its Glow over everything round it, then its lamp unlit
  glow(im, 126 + capsule.light[0] + 0.5, FLOOR - 48 + capsule.light[1] + 0.5, 30, '#ff7ad1', 0.55);
  blit(im, capsule.work, 126, FLOOR - 48);
  return im;
}
module.exports = { scene, W, H };
