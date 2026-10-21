const {encode} = require('./png.js');
const out = process.argv[2] || require('path').join(__dirname, '..', '..', '..', 'game', 'art', 'items');
const hex = h => [parseInt(h.slice(0,2),16),parseInt(h.slice(2,4),16),parseInt(h.slice(4,6),16),255];
function draw(name, rows, pal) {
  const w = 16, h = 16, data = Buffer.alloc(w*h*4);
  rows.forEach((row, y) => { if (row.length !== 16) throw new Error(name + ' row ' + y + ' is ' + row.length);
    [...row].forEach((c, x) => { if (c === '.') return; const col = hex(pal[c]); col.forEach((v,i)=>data[(y*w+x)*4+i]=v); }); });
  if (rows.length !== 16) throw new Error(name + ' has ' + rows.length + ' rows');
  encode(`${out}/${name}.png`, w, h, data);
}
const A = '1a1e25';
// Scrap's rusty copper, iron's blue-grey: each part in the colours of what it is made of.
const scrap = { B: '8a5a3a', C: 'b07a50', D: 'a0603a', E: 'c98a55' };
const iron = { B: 'dce6f0', C: 'b8c6d4', D: '8fa0b2', E: '66778a' };
draw('wire', [
  '................',
  '................',
  '................',
  '.....AAAAAA.....',
  '....AEEEEECA....',
  '...AECAAAACDA...',
  '..AECA....ACDA..',
  '..AEA......ADA..',
  '..ACA......ABA..',
  '..ACDA....ADBA..',
  '...ADDAAAADBA...',
  '....ADDDDDBAAA..',
  '.....AAAAAAAEEA.',
  '............AAA.',
  '................',
  '................',
], { A, ...scrap });
draw('plate', [
  '................',
  '................',
  '................',
  '................',
  '..AAAAAAAAAAAA..',
  '..ABBBBBBBBBBA..',
  '..ACECCCCCCECA..',
  '..ACCCCCCCCCCA..',
  '..ADDDDDDDDDDA..',
  '..ADEDDDDDDEDA..',
  '..AFFFFFFFFFFA..',
  '..AAAAAAAAAAAA..',
  '................',
  '................',
  '................',
  '................',
], { A, ...iron, F: '66778a', E: '4a5868' });
draw('gear', [
  '................',
  '......AAAA......',
  '..AA..ABBA..AA..',
  '.ABBAABBBBAABBA.',
  '.ABCCBCCCCBCCBA.',
  '..ACCCCAACCCCA..',
  'AAACCCA..ACCCAAA',
  'ABCCCA....ACCCDA',
  'ACDDDA....ADDDEA',
  'AAADDDA..ADDDAAA',
  '..ADDDDAADDDDA..',
  '.ADDEDDDDDDEDDA.',
  '.ADEAAEDDEAAEDA.',
  '..AA..AEEA..AA..',
  '......AAAA......',
  '................',
], { A, ...iron });
draw('rotor', [
  '................',
  '................',
  '...AAAAAAAAAA...',
  '...ABWXWXWXBA...',
  '...ACWXWXWXCA...',
  '...ACXWXWXWCA...',
  'AAAACWXWXWXCAAAA',
  'ABBBCXWXWXWCBBBA',
  'ADDDDWXWXWXDDDDA',
  'AAAADXWXWXWDAAAA',
  '...ADWXWXWXDA...',
  '...ADYXYXYXDA...',
  '...AEYYYYYYEA...',
  '...AAAAAAAAAA...',
  '................',
  '................',
], { A, ...iron, W: 'c98a55', X: 'b07a50', Y: '8a5a3a' });
