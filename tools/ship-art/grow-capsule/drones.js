// Placeholder art for drones (0062): the drone bay, 2x2 cells, and the drone's item icon.
const { encode, scale } = require('./png.js');
const out = process.argv[2], preview = process.argv[3];
const hex = h => [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16), 255];
function canvas(w, h) { return { w, h, data: Buffer.alloc(w * h * 4) }; }
function set(img, x, y, c) { if (x < 0 || y < 0 || x >= img.w || y >= img.h) return; img.data.set(hex(c), (y * img.w + x) * 4); }
function rect(img, x0, y0, x1, y1, c) { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) set(img, x, y, c); }
function frame(img, x0, y0, x1, y1, c) { for (let x = x0; x <= x1; x++) { set(img, x, y0, c); set(img, x, y1, c); } for (let y = y0; y <= y1; y++) { set(img, x0, y, c); set(img, x1, y, c); } }
const O = '1a1e25', K = '10161d', D = '2a2f3a', B = '2a3d4f', M = '3a536a', L = '5f86a8', H = '9fbfda', P = '243241';

// The bay: a steel cabinet of four docks, two by two, each with its landing pad along its floor; a mast on its roof.
const bay = canvas(32, 32);
rect(bay, 1, 5, 30, 31, O);
rect(bay, 2, 6, 29, 30, M);
rect(bay, 2, 6, 29, 7, L);           // the roof's lit edge
for (let x = 4; x <= 27; x += 6) set(bay, x, 6, H);
rect(bay, 2, 30, 29, 30, B);         // its foot
rect(bay, 15, 8, 16, 29, D);         // the spine between the docks
rect(bay, 2, 18, 29, 19, D);         // the shelf between the rows
for (const [x0, y0] of [[3, 8], [17, 8], [3, 20], [17, 20]]) {
  frame(bay, x0, y0, x0 + 11, y0 + 9, O);
  rect(bay, x0 + 1, y0 + 1, x0 + 10, y0 + 8, K);
  rect(bay, x0 + 1, y0 + 1, x0 + 10, y0 + 1, B);   // the dock's lintel
  rect(bay, x0 + 2, y0 + 7, x0 + 9, y0 + 8, P);    // its pad, lit by the client where a drone docks
}
rect(bay, 15, 1, 16, 4, D);          // the mast
set(bay, 15, 0, L); set(bay, 16, 0, L);
rect(bay, 13, 4, 18, 4, O);
encode(`${out}/machine_droneBay.png`, bay);

// The drone: a light body with a dark visor and a sky lens, two rotors on arms.
const drone = canvas(16, 16);
const R = 'b8c6d4', W = 'dce6f0', G = '8fa0b2', S = '66778a', Y = '60a5fa';
rect(drone, 0, 3, 6, 5, O); rect(drone, 9, 3, 15, 5, O);
rect(drone, 1, 4, 5, 4, R); rect(drone, 10, 4, 14, 4, R);
set(drone, 1, 4, W); set(drone, 14, 4, W);
rect(drone, 2, 5, 4, 7, O); rect(drone, 11, 5, 13, 7, O);
set(drone, 3, 6, S); set(drone, 12, 6, S);
rect(drone, 2, 7, 13, 13, O);
rect(drone, 3, 8, 12, 12, G);
rect(drone, 3, 8, 12, 8, W);
rect(drone, 4, 9, 11, 9, R);
rect(drone, 5, 10, 10, 11, K);
set(drone, 9, 10, Y); set(drone, 8, 10, Y);
rect(drone, 3, 12, 12, 12, S);
rect(drone, 5, 13, 10, 14, O); rect(drone, 6, 13, 9, 13, S);
encode(`${out}/items/drone.png`, drone);

if (preview) {
  encode(`${preview}/preview_bay.png`, scale(bay, 10, [20, 24, 32]));
  encode(`${preview}/preview_drone.png`, scale(drone, 12, [20, 24, 32]));
}
