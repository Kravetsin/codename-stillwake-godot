// node dump.js name... : prints a game picture as chars plus its palette
const {read}=require('./png.js');const A=require('path').join(__dirname, '..', '..', '..', '..', 'game', 'art') + '/';
const chars='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
for(const n of process.argv.slice(2)){const im=read(A+n+'.png');const k={};let rows=[];for(let y=0;y<im.h;y++){let r='';for(let x=0;x<im.w;x++){const i=(y*im.w+x)*4;if(im.data[i+3]<128){r+='.';continue;}const h='#'+[0,1,2].map(q=>im.data[i+q].toString(16).padStart(2,'0')).join('');if(!(h in k))k[h]=chars[Object.keys(k).length%chars.length];r+=k[h];}rows.push(r);}
console.log(n);console.log(rows.join('\n'));console.log(JSON.stringify(k));}
