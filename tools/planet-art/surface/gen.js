// node tools/planet-art/surface/gen.js [style ...]: renders the study scenes (here only s9, "Stillwake") into surface/out/
const K = require('./kit'), fs = require('fs');
const ALL = fs.readdirSync(__dirname).filter((f) => /^s\d-.*\.js$/.test(f)).map((f) => f.slice(0, -3));
const pick = process.argv.slice(2); fs.mkdirSync(__dirname + '/out', { recursive: true });
for (const id of ALL.filter((a) => !pick.length || pick.some((p) => a.includes(p)))) {
  const t = Date.now(), cv = require('./' + id)(K);
  K.P.encode(cv.toPng(1), `${__dirname}/out/${id}.png`); K.P.encode(cv.toPng(2), `${__dirname}/out/${id}@2x.png`);
  console.log(id, Date.now() - t, 'ms');
}
