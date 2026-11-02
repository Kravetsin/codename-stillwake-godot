// A door one piece long (door_*_0), and the names of the links a door's pieces are never drawn by: room.js draws the three pieces of a longer door; these share its construction, so they are one door.
(function () {
'use strict';
const { canvas } = Anime;

const DOOR = { left: 3, panel: 4, panelEnd: 12, right: 13, top: 3, foot: 15 };
const ORANGE = { base: '#d97f3a', shade: '#a9542a', lite: '#f6b06a', spec: '#ffe4b8', ink: '#2a140c' };
const JAMB = { base: '#5a3a26', shade: '#3d2519', lite: '#7a5236', spec: '#7a5236', ink: '#160c08' };

/** A panel by its ends: window at its head, stripes at its foot. Hard rectangles at the final size, as room.js: the shut picture differs from the open one exactly by this. */
function panelOf(head, foot) {
  const c = canvas(64, 64), x = c.getContext('2d');
  const from = head ? DOOR.top : 0, to = foot ? DOOR.foot : 16;
  x.setTransform(4, 0, 0, 4, 0, 0);
  const rect = (a, b, y0, y1, col) => { x.fillStyle = col; x.fillRect(a, y0, b - a, y1 - y0); };
  rect(DOOR.panel, DOOR.panelEnd, from, to, ORANGE.base);
  rect(DOOR.panel, DOOR.panel + 1.25, from, to, ORANGE.lite);
  rect(DOOR.panel + .25, DOOR.panel + .75, from, to, ORANGE.spec);
  rect(DOOR.panelEnd - 1.75, DOOR.panelEnd, from, to, ORANGE.shade);
  x.fillStyle = ORANGE.ink;
  x.fillRect(DOOR.panel + 1.25, from, .25, to - from);
  x.fillRect(DOOR.panelEnd - 1.75, from, .25, to - from);
  rect(DOOR.panel, DOOR.panelEnd, 9, 9.5, ORANGE.ink);
  rect(DOOR.panel, DOOR.panelEnd, 9.5, 10, ORANGE.lite);
  rect(DOOR.panel, DOOR.panelEnd, 8.25, 9, ORANGE.shade);
  if (head) {
    rect(5.75, 10.25, 3.75, 7.75, '#1a1c28');
    rect(6.25, 9.75, 4.25, 7.25, '#2c4a70');
    x.fillStyle = '#9ad2f0';
    x.beginPath(); x.moveTo(6.25, 7.25); x.lineTo(6.25, 5.75); x.lineTo(7.75, 4.25); x.lineTo(9, 4.25); x.closePath(); x.fill();
    x.fillStyle = '#e4f6ff';
    x.beginPath(); x.moveTo(6.5, 5.75); x.lineTo(7.75, 4.5); x.lineTo(8.25, 4.5); x.lineTo(6.5, 6.25); x.closePath(); x.fill();
  }
  if (foot) {
    x.save();
    x.beginPath(); x.rect(DOOR.panel, 11.75, DOOR.panelEnd - DOOR.panel, 3.25); x.clip();
    rect(DOOR.panel, DOOR.panelEnd, 11.75, 15, '#f0c43c');
    x.fillStyle = '#1a1820';
    for (let k = -2; k < 12; k += 2.5) { x.beginPath(); x.moveTo(DOOR.panel + k, 15); x.lineTo(DOOR.panel + k + 1.25, 15); x.lineTo(DOOR.panel + k + 4.5, 11.75); x.lineTo(DOOR.panel + k + 3.25, 11.75); x.closePath(); x.fill(); }
    x.restore();
    rect(DOOR.panel, DOOR.panelEnd, 11.5, 11.75, ORANGE.ink);
  }
  return c;
}

/** The frame, the same shut and open: jambs, the header with its lamps at a head, the sill at a foot. */
function frameOf(head, foot) {
  const c = canvas(64, 64), x = c.getContext('2d');
  x.setTransform(4, 0, 0, 4, 0, 0);
  const rect = (a, b, y0, y1, col) => { x.fillStyle = col; x.fillRect(a, y0, b - a, y1 - y0); };
  for (const [a, b] of [[DOOR.left - .75, DOOR.panel], [DOOR.panelEnd, DOOR.right + .75]]) {
    rect(a, b, 0, 16, JAMB.ink);
    rect(a + .5, b - .25, 0, 16, JAMB.base);
  }
  rect(DOOR.left - .25, DOOR.left + .25, 0, 16, JAMB.lite);
  rect(DOOR.panelEnd + .25, DOOR.panelEnd + .75, 0, 16, JAMB.lite);
  if (head) {
    rect(DOOR.left - .75, DOOR.right + .75, 0, DOOR.top, JAMB.ink);
    rect(DOOR.left - .25, DOOR.right + .25, .5, DOOR.top - .25, JAMB.base);
    rect(DOOR.left - .25, DOOR.right + .25, .5, 1, JAMB.lite);
    for (const lx of [5.5, 9]) { rect(lx, lx + 1.5, 1.25, 2.25, '#f0c43c'); rect(lx + .25, lx + .75, 1.25, 1.5, '#fff0b0'); }
  }
  if (foot) {
    rect(DOOR.left - .75, DOOR.right + .75, DOOR.foot, 16, JAMB.ink);
    rect(DOOR.left - .25, DOOR.right + .25, DOOR.foot + .25, 15.75, JAMB.shade);
  }
  return c;
}

function doorPicture(head, foot, shut) {
  const c = canvas(64, 64), x = c.getContext('2d');
  if (shut) x.drawImage(panelOf(head, foot), 0, 0);
  x.drawImage(frameOf(head, foot), 0, 0);
  return c;
}

Subjects.doors = function () {
  // Links: up 1, right 2, down 4, left 8. Only up and down shape a door; room.js writes 1, 4 and 5, the rest are the same four pictures, as in the logical set.
  for (let links = 0; links < 16; links++) {
    if (links === 1 || links === 4 || links === 5) continue;
    const head = !(links & 1), foot = !(links & 4);
    out(`door_closed_${links}`, doorPicture(head, foot, true));
    out(`door_open_${links}`, doorPicture(head, foot, false));
  }
};
})();
