const fs=require('fs');
const art={};for(const f of fs.readdirSync('art')) if(f.endsWith('.png')) art[f.replace('.png','')]='data:image/png;base64,'+fs.readFileSync('art/'+f).toString('base64');
const strip=s=>s.replace(/if \(typeof module[\s\S]*$/,'');
const engine=strip(fs.readFileSync('engine.js','utf8'))+'\n'+strip(fs.readFileSync('cards.js','utf8'))+'\n'+fs.readFileSync('render.js','utf8');
let page=fs.readFileSync('page.html','utf8');
page=page.replace('/*ART*/',()=>JSON.stringify(art)).replace('/*ENGINE*/',()=>engine).replace('/*APP*/',()=>fs.readFileSync('app.js','utf8'));
fs.writeFileSync('index.html',page);
// a local copy wrapped as the artifact skeleton is, to look at in Chrome
fs.writeFileSync('local.html','<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"></head><body>'+page+'</body></html>');
console.log('ok',(page.length/1024).toFixed(0)+'KB');
