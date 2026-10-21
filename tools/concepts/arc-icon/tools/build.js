const fs=require('fs'),p=require('path');const d=p.join(__dirname,'..');
const crops=fs.readFileSync(p.join(__dirname,'crops.txt'),'utf8').trim().split(/\r?\n/);
const icons={};for(const l of fs.readFileSync(p.join(__dirname,'icons.txt'),'utf8').trim().split(/\r?\n/)){const i=l.indexOf(':');icons[l.slice(0,i)]=l.slice(i+1);}
let h=fs.readFileSync(p.join(d,'template.html'),'utf8').replace('%%CROP1%%',crops[0]).replace('%%CROP2%%',crops[1]).replace('%%ICONS%%',JSON.stringify(icons));
fs.writeFileSync(p.join(d,'arc-icon.html'),h);
fs.writeFileSync(p.join(__dirname,'preview.html'),'<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"></head><body style="margin:0">'+h+'</body></html>');
console.log(h.length);
