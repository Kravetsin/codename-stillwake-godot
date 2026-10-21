const fs=require('fs');const A=process.argv[2];const list=process.argv.slice(3);
let h='<html><body style="background:#2a2f38;margin:0;padding:8px;font:10px monospace;color:#ccc;display:flex;flex-wrap:wrap;gap:10px;align-items:flex-end">';
for(const f of list){const b=fs.readFileSync(A+'/'+f).toString('base64');h+=`<div><img src="data:image/png;base64,${b}" style="image-rendering:pixelated;zoom:3"><br>${f}</div>`;}
fs.writeFileSync('sheet.html',h+'</body></html>');
