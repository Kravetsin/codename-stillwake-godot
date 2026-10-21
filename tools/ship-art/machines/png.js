const fs = require('fs'), zlib = require('zlib');
function crc32(buf){let c,crc=0xffffffff;for(let n=0;n<buf.length;n++){c=(crc^buf[n])&0xff;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;crc=(crc>>>8)^c;}return (crc^0xffffffff)>>>0;}
function chunk(type,data){const len=Buffer.alloc(4);len.writeUInt32BE(data.length);const td=Buffer.concat([Buffer.from(type),data]);const crc=Buffer.alloc(4);crc.writeUInt32BE(crc32(td));return Buffer.concat([len,td,crc]);}
exports.write=function(path,w,h,px){ // px: array of [r,g,b,a] rows*w
  const raw=Buffer.alloc((w*4+1)*h);for(let y=0;y<h;y++){raw[y*(w*4+1)]=0;for(let x=0;x<w;x++){const p=px[y*w+x]||[0,0,0,0];raw.set(p,y*(w*4+1)+1+x*4);}}
  const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;ihdr[10]=0;ihdr[11]=0;ihdr[12]=0;
  fs.writeFileSync(path,Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',zlib.deflateSync(raw)),chunk('IEND',Buffer.alloc(0))]));
};
exports.read=function(path){const b=fs.readFileSync(path);let o=8,w,h,ct,bd,idat=[],pal=null,trns=null;
  while(o<b.length){const len=b.readUInt32BE(o),type=b.toString('ascii',o+4,o+8),d=b.slice(o+8,o+8+len);o+=12+len;
    if(type==='IHDR'){w=d.readUInt32BE(0);h=d.readUInt32BE(4);bd=d[8];ct=d[9];}else if(type==='IDAT')idat.push(d);else if(type==='PLTE')pal=d;else if(type==='tRNS')trns=d;}
  const raw=zlib.inflateSync(Buffer.concat(idat));const ch={0:1,2:3,3:1,4:2,6:4}[ct];if(bd!==8)throw new Error('bd '+bd+' ct '+ct+' '+path);
  const bpp=ch,stride=w*bpp;const out=Buffer.alloc(stride*h);let prev=Buffer.alloc(stride);
  for(let y=0;y<h;y++){const f=raw[y*(stride+1)];const line=raw.slice(y*(stride+1)+1,(y+1)*(stride+1));const cur=Buffer.alloc(stride);
    for(let i=0;i<stride;i++){const a=i>=bpp?cur[i-bpp]:0,up=prev[i],c=i>=bpp?prev[i-bpp]:0;let v=line[i];
      if(f===1)v+=a;else if(f===2)v+=up;else if(f===3)v+=(a+up)>>1;else if(f===4){const p=a+up-c,pa=Math.abs(p-a),pb=Math.abs(p-up),pc=Math.abs(p-c);v+=pa<=pb&&pa<=pc?a:pb<=pc?up:c;}
      cur[i]=v&255;}cur.copy(out,y*stride);prev=cur;}
  const px=[];for(let i=0;i<w*h;i++){if(ct===6)px.push([...out.slice(i*4,i*4+4)]);else if(ct===2)px.push([...out.slice(i*3,i*3+3),255]);else if(ct===3){const k=out[i];px.push([pal[k*3],pal[k*3+1],pal[k*3+2],trns&&k<trns.length?trns[k]:255]);}else if(ct===4)px.push([out[i*2],out[i*2],out[i*2],out[i*2+1]]);else px.push([out[i],out[i],out[i],255]);}
  return {w,h,px};};
// upscale for viewing
exports.zoom=function(src,dst,s,bg=[24,26,32,255]){const {w,h,px}=exports.read(src);const o=[];for(let y=0;y<h*s;y++)for(let x=0;x<w*s;x++){const p=px[Math.floor(y/s)*w+Math.floor(x/s)];const a=p[3]/255;o.push([Math.round(p[0]*a+bg[0]*(1-a)),Math.round(p[1]*a+bg[1]*(1-a)),Math.round(p[2]*a+bg[2]*(1-a)),255]);}exports.write(dst,w*s,h*s,o);};
// The older API the earlier scripts use: encode(path, w, h, rgbaBuffer) and decode(path) -> {w, h, rgba}.
exports.encode=function(path,w,h,rgba){const px=[];for(let i=0;i<w*h;i++)px.push([rgba[i*4],rgba[i*4+1],rgba[i*4+2],rgba[i*4+3]]);exports.write(path,w,h,px);};
exports.decode=function(path){const {w,h,px}=exports.read(path);const rgba=Buffer.alloc(w*h*4);px.forEach((p,i)=>rgba.set(p,i*4));return {w,h,rgba};};
