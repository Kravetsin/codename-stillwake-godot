// Composing the capsule as the game does: base, then plant (lit), then the lamp (unlit) and its Glow.
const path = require('path');
const { Pic, img, blit, glow, shade } = require('./lib.js');
const ART = require('path').join(__dirname, '..', '..', '..', '..', 'game', 'art');

function current() {
  const L = n => Pic.load(path.join(ART, n + '.png'));
  return { base: L('machine_growCapsule'), work: L('growCapsule_work_0'), plants: [0, 1, 2, 3].map(i => L('growCapsule_plant_' + i)), light: [16, 9] };
}

// stage -1: empty. on: lamp lit; the room is dimmed a little and the pink Glow added, as a hint of the game's light
function frame(c, stage, on, { room = 0.78 } = {}) {
  let im = img(c.base.w, c.base.h);
  blit(im, c.base, 0, 0);
  if (stage >= 0) blit(im, c.plants[stage], 0, 0);
  if (on) {
    im = shade(im, room);
    glow(im, c.light[0] + 0.5, c.light[1] + 0.5, 24, '#ff7ad1', 0.5);
    blit(im, c.work, 0, 0);
  } else im = shade(im, room);
  return im;
}
module.exports = { current, frame, ART };
