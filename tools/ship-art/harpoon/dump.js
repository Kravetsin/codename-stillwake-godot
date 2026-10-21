const P = require('../weapons/png.js');
for (const f of process.argv.slice(2)) {
  const im = P.read(f); const pal = new Map(); const chars = '@abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&*+=?';
  let out = '';
  for (let y = 0; y < im.h; y++) { let l = ''; for (let x = 0; x < im.w; x++) { const o = (y*im.w+x)*4; if (!im.data[o+3]) { l += '.'; continue; } const k = '#'+[0,1,2].map(i=>im.data[o+i].toString(16).padStart(2,'0')).join('') + (im.data[o+3]<255?'/'+im.data[o+3]:''); if (!pal.has(k)) pal.set(k, chars[pal.size]); l += pal.get(k); } out += l + '\n'; }
  console.log(f.split('/').pop(), im.w + 'x' + im.h); console.log(out); console.log([...pal].map(([k,v])=>v+'='+k).join(' '));
}
