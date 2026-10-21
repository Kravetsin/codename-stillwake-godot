const fs=require('fs');
const art={};for(const f of fs.readdirSync('art')) if(f.endsWith('.png')) art[f.replace('.png','')]='data:image/png;base64,'+fs.readFileSync('art/'+f).toString('base64');
const strip=s=>s.replace(/if \(typeof module[\s\S]*$/,'');
const code=['engine.js','render.js','zphys.js','zfig.js','zapp.js'].map(f=>strip(fs.readFileSync(f,'utf8'))).join('\n');
let page=fs.readFileSync('page.html','utf8');
page=page.replace('/*ART*/',()=>JSON.stringify(art)).replace('/*CODE*/',()=>code);
fs.writeFileSync('index.html',page);
fs.writeFileSync('local.html','<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"></head><body>'+page+'</body></html>');
console.log('ok',(page.length/1024).toFixed(0)+'KB');
