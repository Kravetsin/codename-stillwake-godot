// montage.js shot.png out.png scale cols x,y,w,h [x,y,w,h ...]  -- or 'panels:N:x,y,w,h' to take that rect of each of N stacked panels
const {decode,encode,crop}=require('./png.js');const fs=require('fs');
const [f,o,s,cols,...rs]=process.argv.slice(2);const im=decode(f);const S=+s,C=+cols;
let rects=[];for(const r of rs){if(r.startsWith('panels:')){const [,n,pw,ph,rr]=r.split(':');const [x,y,w,h]=rr.split(',').map(Number);const per=Math.floor((im.w-6)/(+pw+6));for(let i=0;i<+n;i++){const px=6+(i%per)*(+pw+6),py=6+Math.floor(i/per)*(+ph+6);rects.push([px+x,py+y,w,h]);}}else rects.push(r.split(',').map(Number));}
const cw=Math.max(...rects.map(r=>r[2]))*S,ch=Math.max(...rects.map(r=>r[3]))*S,R=Math.ceil(rects.length/C);
const out={w:C*cw+(C-1)*4,h:R*ch+(R-1)*4,data:Buffer.alloc((C*cw+(C-1)*4)*(R*ch+(R-1)*4)*4,40)};
rects.forEach((r,i)=>{const c=crop(im,...r,S);const ox=(i%C)*(cw+4),oy=Math.floor(i/C)*(ch+4);for(let y=0;y<c.h;y++)c.data.copy(out.data,((oy+y)*out.w+ox)*4,y*c.w*4,(y+1)*c.w*4);});
fs.writeFileSync(o,encode(out));
