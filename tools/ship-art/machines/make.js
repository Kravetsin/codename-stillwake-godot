// Placeholder pixel art for the drill and the asteroid: raw RGBA written here, PNG made by ffmpeg.
const fs = require('fs');
const out = process.argv[2];
const hex = h => [parseInt(h.slice(0,2),16), parseInt(h.slice(2,4),16), parseInt(h.slice(4,6),16), 255];
function write(name, w, h, px) {
  const b = Buffer.alloc(w*h*4);
  for (let i=0;i<w*h;i++) { const c = px[i]; if (c) { b[i*4]=c[0]; b[i*4+1]=c[1]; b[i*4+2]=c[2]; b[i*4+3]=c[3]; } }
  fs.writeFileSync(`${out}/${name}_${w}x${h}.raw`, b);
}
function fromAscii(name, rows, pal) {
  const w = Math.max(...rows.map(r=>r.length)), h = rows.length;
  const px = [];
  for (const r of rows) for (let x=0;x<w;x++) { const ch = r[x] || '.'; px.push(ch==='.'?null:hex(pal[ch])); }
  write(name, w, h, px);
}
const P = { a:'14171e', Y:'f0b648', y:'d08f2a', o:'8f5a1c', k:'5a3a14', m:'8a94a2', M:'5f6874', l:'c9ced6', w:'f4f7fa', g:'454c57', d:'30353d', r:'b04030' };
const drill = [
  '..aaaaaaaaaaa.........',
  '.aYYYYYYYYYYYa........',
  'aYyyyyyyyyyyyYaa......',
  'aYykykykyyyyyMlMaaaa..',
  'aYyyyyyyyyyyyMlMlllwa.',
  'aoyyyyyyyyyyyMmMmmmlwa',
  'aooooooooooooMmMaaaaa.',
  '.aaaaagggggaaMMMa.....',
  '.....aggggarraaa......',
  '.....agddga.ra........',
  '....agggga..aa........',
  '....adddda............',
  '....aaaaaa............',
];
fromAscii('hero_drill', drill, P);
const spin = drill.slice();
spin[4] = 'aYyyyyyyyyyyyMmMlwlla.';
spin[5] = 'aoyyyyyyyyyyyMlMmlmmwa';
fromAscii('hero_drill_spin', spin, P);
const icon = [
  '................',
  '................',
  '................',
  '..aaaaaaaa......',
  '.aYYYYYYYYaa....',
  'aYykykyyyYMMaa..',
  'aYyyyyyyyYMlllwa',
  'aoooooooooMmmmwa',
  '.aaaggggaaMMaaa.',
  '....aggga.ra....',
  '....aggga.aa....',
  '...agggga.......',
  '...addddа.......'.replace('а','a'),
  '...aaaaaa.......',
  '................',
  '................',
];
fromAscii('drill', icon, P);

// The asteroid: a lumpy disc lit from the top left, craters and flecks, in the picked rock's colours.
let seed = 7;
const rnd = () => (seed = (seed * 1103515245 + 12345) >>> 0) / 4294967296;
function asteroid(name, n, s0) {
  seed = s0;
  const W = n, cx = (n-1)/2, cy = (n-1)/2, R = n/2 - 1.5;
  const bumps = Array.from({length: 7}, () => [rnd()*Math.PI*2, (rnd()-0.5)*0.22*R]);
  const radius = t => R + bumps.reduce((s,[p,a]) => s + a*Math.cos(t*2 + p) * 0.5 + a*Math.cos(t*3 - p)*0.25, 0);
  const inside = (x,y) => { const dx=x-cx, dy=y-cy; return Math.hypot(dx,dy) <= radius(Math.atan2(dy,dx)); };
  const craters = Array.from({length: 4}, () => { const t=rnd()*Math.PI*2, d=rnd()*R*0.6; return [cx+Math.cos(t)*d, cy+Math.sin(t)*d, 1.5+rnd()*2.2]; });
  const C = { out:hex('14171e'), hi:hex('aaa69c'), mid:hex('837f76'), lo:hex('5f5c55'), dk:hex('423f3a'), glint:hex('fbfdff'), gold:hex('f1d27a'), gold2:hex('957626'), rust:hex('a65a2a') };
  const px = [];
  for (let y=0;y<W;y++) for (let x=0;x<W;x++) {
    if (!inside(x,y)) { px.push(null); continue; }
    const edge = !inside(x-1,y) || !inside(x+1,y) || !inside(x,y-1) || !inside(x,y+1);
    if (edge) { px.push(C.out); continue; }
    const dx=(x-cx)/R, dy=(y-cy)/R;
    let light = -(dx*0.65 + dy*0.75) + (rnd()-0.5)*0.22;
    for (const [qx,qy,qr] of craters) {
      const d = Math.hypot(x-qx, y-qy);
      if (d < qr) light = (x-qx + y-qy) > 0 ? light + 0.7 : light - 0.9;
      else if (d < qr+0.8 && (x-qx + y-qy) < 0) light += 0.4;
    }
    const c = light > 0.55 ? C.hi : light > -0.1 ? C.mid : light > -0.6 ? C.lo : C.dk;
    const fleck = rnd();
    px.push(fleck < 0.012 ? C.gold : fleck < 0.02 ? C.gold2 : fleck < 0.026 ? C.rust : c);
  }
  write(name, W, W, px);
}
asteroid('asteroid_01', 32, 11);
asteroid('asteroid_02', 32, 29);
asteroid('asteroid_03', 32, 47);
asteroid('asteroid', 16, 11);
