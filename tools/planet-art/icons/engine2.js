// 16 px item icons in the game's style: 1 px outline #14171e on the outside, light from the top left.
const P = require('../png');
const OUT = '14171e';
const R = {
  cu: ['fbd9b0', 'f7bd84', 'd9864a', 'a65a2a', '6f3818'],
  rk: ['c9c5bb', 'aaa69c', '837f76', '5f5c55', '423f3a'],
  ic: ['ffffff', 'e8f8ff', 'b8e2f8', '7fb8e6', '4f86c0'],
  am: ['fff2c0', 'ffbe5a', 'e1933f', 'c87028', '914e1c'],
  ro: ['ffe0dc', 'ffa8a8', 'f07a86', 'c4506a', '84304a'],
  vi: ['f6e8ff', 'd2b8ff', 'b48cff', '8862de', '51369b'],
  vd: ['8862de', '6a46c8', '51369b', '2c1e58', '1b1332'],
  mi: ['ffffff', 'e4e2ec', 'b9b4c8', '8c86a0', '5e5872'],
  mb: ['fbf0d4', 'e6d2a4', 'c4a878', '957a52', '64503a'],
  mg: ['9aa4bc', '6a7389', '474d60', '30343f', '1d2029'],
  st: ['e4e9ef', 'aab4c0', '7a8593', '535c69', '383e49'],
  ds: ['a3acb8', '8a93a0', '646d7a', '474e59', '30353e'],
  al: ['a6c2c6', '6e8a93', '4e6772', '384c57', '26353e'],
  tl: ['d8fff4', '96ffe6', '4aa8a0', '2f7f7a', '245c5c'],
  rd: ['d8705a', 'c45c48', '9c3a36', '74262c', '4a1820'],
  oc: ['ecd088', 'cca048', 'a07a2c', '6a4a18', '3a2810'],
  in: ['7a88d0', '5b68ae', '4b589e', '333c76', '22284e'],
  bl: ['f4feff', 'c4ecff', '70c6ff', '3a7ad8', '26384a'],
  em: ['fff0b0', 'ffc060', 'ff8a30', 'c83a18', '7a1e0c'],
  hb: ['c0cad8', '96a0b0', '747e90', '5a6474', '3e4656'],
};
// Default legend: lowercase letters are materials shaded by their edges; uppercase and digits are set per icon.
const BASE = { c: 'cu', r: 'rk', i: 'ic', a: 'am', p: 'ro', v: 'vi', u: 'vd', m: 'mi', y: 'mb', g: 'mg', s: 'st', d: 'ds', l: 'al', t: 'tl', e: 'rd', o: 'oc', n: 'in', b: 'bl', f: 'em', h: 'hb', W: '#ffffff', K: '#' + OUT };
function parse(v) {
  if (v[0] === '#') return { hex: v.slice(1), mat: v };
  const m = v.match(/^([a-z]{2})(\d)?$/); if (!m) throw new Error('legend ' + v);
  return { mat: m[1], tone: m[2] != null ? +m[2] : null };
}
function draw(rows, legend = {}) {
  const L = { ...BASE, ...legend };
  if (rows.length !== 16) throw new Error('rows ' + rows.length + ' ' + rows[0]);
  const cell = [];
  for (let y = 0; y < 16; y++) { const r = rows[y].padEnd(16, '.'); if (r.length > 16) throw new Error('wide row ' + y + ' ' + r);
    for (let x = 0; x < 16; x++) { const ch = r[x]; if (ch === '.') { cell.push(null); continue; } if (ch === '#') { cell.push({ hex: OUT, mat: '#', ch }); continue; }
      if (!L[ch]) throw new Error('no legend ' + ch); cell.push({ ...parse(L[ch]), ch }); } }
  const at = (x, y) => (x < 0 || y < 0 || x > 15 || y > 15 ? null : cell[y * 16 + x]);
  const same = (x, y, me) => { const n = at(x, y); return n && n.mat === me.mat && (n.tone != null || n.ch === me.ch); };
  const img = P.create(16, 16);
  const put = (x, y, hex) => { const o = (y * 16 + x) * 4; img.data[o] = parseInt(hex.slice(0, 2), 16); img.data[o + 1] = parseInt(hex.slice(2, 4), 16); img.data[o + 2] = parseInt(hex.slice(4, 6), 16); img.data[o + 3] = 255; };
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) {
    const c = at(x, y);
    if (!c) { if (at(x - 1, y) || at(x + 1, y) || at(x, y - 1) || at(x, y + 1)) put(x, y, OUT); continue; }
    if (c.hex) { put(x, y, c.hex); continue; }
    let t = c.tone;
    if (t == null) { const up = !same(x, y - 1, c), dn = !same(x, y + 1, c), lf = !same(x - 1, y, c), rt = !same(x + 1, y, c);
      t = up ? (lf ? 0 : 0) : dn ? (rt ? 4 : 3) : lf ? 1 : rt ? 3 : 2; }
    put(x, y, R[c.mat][t]);
  }
  return img;
}
function recolor(file, map) { const im = P.decode(file); for (let i = 0; i < 256; i++) { const h = im.data.slice(i * 4, i * 4 + 3).toString('hex'); if (im.data[i * 4 + 3] && map[h]) { const n = map[h]; im.data[i * 4] = parseInt(n.slice(0, 2), 16); im.data[i * 4 + 1] = parseInt(n.slice(2, 4), 16); im.data[i * 4 + 2] = parseInt(n.slice(4, 6), 16); } } return im; }
module.exports = { draw, recolor, R, OUT, P };
