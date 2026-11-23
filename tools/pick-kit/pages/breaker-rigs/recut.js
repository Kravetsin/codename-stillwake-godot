'use strict';
// node recut.js <breakers3 dir>: every picture at the game's 4 px per world px of its rig; node sheet.js shows them all
const { spawnSync } = require('child_process');
const fs = require('fs'), path = require('path');
for (const f of ['rig.js', 'triad.rig.js', 'rigs.js']) require('./' + f);
const root = process.argv[2], cutsOf = id => JSON.parse(fs.readFileSync(path.join(root, 'cut', id, 'cuts.json'), 'utf8'));
for (const id of Object.keys(RIGS)) {
  const rig = RIGS[id], names = Object.keys(cutsOf(id)), k = {};
  for (const p of rig.parts) k[p.pic] = p.k || 1;
  for (const [alt, base] of [['shut', 'open'], ['hook', 'claw'], ['poddead', 'pod'], ['reardead', 'rear'], ['tipdead', 'tip']]) if (k[base] && !k[alt]) k[alt] = k[base];
  const r = spawnSync(process.execPath, [path.join(__dirname, 'partcut.js'), path.join(root, 'gen', id, 'parts.png'), path.join(root, 'cut', id), id === 'triad' ? 3 : 4, 2, rig.scale * 4, ...names.map(n => `${n}@${k[n] || 1}`)], { encoding: 'utf8' });
  if (r.status) throw new Error(id + r.stderr);
  const cuts = cutsOf(id), state = [{}, { sleep: 1 }, { grip: 1 }, { dead: .5 }];
  for (const o of state) for (const p of BreakerRig.posed(BreakerRig.ready(rig), BreakerRig.rest(o))) if (!cuts[p.pic]) console.log('NO CUT', id, p.pic);
}
console.log('recut', Object.keys(RIGS).join(' '));