// node convert.js <trace.json> <W> <H> <out.js>: the trace's "detailed" ops moved from the reference's pixels into a W x H picture,
// the silhouette's box fitted along its length with half a pixel round it; printed as a JS array of [op, material, d, options].
const fs = require('fs');
const [file, Ws, Hs, outFile] = process.argv.slice(2), W = +Ws, H = +Hs;
const T = JSON.parse(fs.readFileSync(file, 'utf8')), [x0, y0, x1, y1] = T.fit.box;
const u = (x1 - x0 + 1) / (W - 1), dy = (H - (y1 - y0 + 1) / u) / 2;
const X = (x) => +((x - x0) / u + .5).toFixed(2), Y = (y) => +((y - y0) / u + dy).toFixed(2);
const move = (d) => d.replace(/(-?\d*\.?\d+) (-?\d*\.?\d+)/g, (m, a, b) => X(+a) + ' ' + Y(+b));
const ops = T.variants.detailed.map((o) => {
  const opt = {};
  if (o.shade) opt.shade = o.shade; if (o.deep) opt.deep = o.deep;
  if (o.lite && o.lite.length) opt.lite = o.lite.map(move);
  if (o.lines && o.lines.length) opt.lines = o.lines.map(move);
  if (o.outer === false) opt.outer = false;
  return [o.op, o.mat || null, move(o.d), opt, o.w];
});
const lines = ops.map(([op, mat, d, opt, w]) => op === 'line' ? `  ['line', null, '${d}', { w: ${w || 1} }],` : `  ['${op}', '${mat}', '${d}', ${JSON.stringify(opt).replace(/"/g, "'")}],`);
fs.writeFileSync(outFile, '[\n' + lines.join('\n') + '\n]');
console.log('units', u.toFixed(2), 'height used', ((y1 - y0 + 1) / u).toFixed(2), 'dy', dy.toFixed(2), 'ops', ops.length, 'bytes', fs.statSync(outFile).size);
