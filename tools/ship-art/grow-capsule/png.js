const fs = require('fs'), zlib = require('zlib');
function crc32(buf){let c,crc=0xffffffff;for(let n=0;n<buf.length;n++){c=(crc^buf[n])&0xff;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;crc=(crc>>>8)^c;}return (crc^0xffffffff)>>>0;}
function decode(file){
  const b=fs.readFileSync(file); let p=8, w,h,depth,ct, idat=[], pal=null, trns=null;
  while(p<b.length){const len=b.readUInt32BE(p), type=b.toString('ascii',p+4,p+8), d=b.slice(p+8,p+8+len);
    if(type==='IHDR'){w=d.readUInt32BE(0);h=d.readUInt32BE(4);depth=d[8];ct=d[9];}
    else if(type==='PLTE')pal=d; else if(type==='tRNS')trns=d; else if(type==='IDAT')idat.push(d);
    p+=12+len;}
  if(depth!==8) throw new Error('depth '+depth+' ct '+ct);
  const raw=zlib.inflateSync(Buffer.concat(idat)); const bpp={0:1,2:3,3:1,4:2,6:4}[ct]; const stride=w*bpp;
  const out=Buffer.alloc(w*h*4); let prev=Buffer.alloc(stride);
  for(let y=0;y<h;y++){const f=raw[y*(stride+1)]; const line=Buffer.from(raw.slice(y*(stride+1)+1,(y+1)*(stride+1)));
    for(let i=0;i<stride;i++){const a=i>=bpp?line[i-bpp]:0, up=prev[i], c=i>=bpp?prev[i-bpp]:0; let v=line[i];
      if(f===1)v+=a; else if(f===2)v+=up; else if(f===3)v+=(a+up)>>1; else if(f===4){const pp=a+up-c,pa=Math.abs(pp-a),pb=Math.abs(pp-up),pc=Math.abs(pp-c);v+=(pa<=pb&&pa<=pc)?a:(pb<=pc?up:c);} line[i]=v&255;}
    for(let x=0;x<w;x++){let r,g,bb,al=255;
      if(ct===6){[r,g,bb,al]=line.slice(x*4,x*4+4);} else if(ct===2){[r,g,bb]=line.slice(x*3,x*3+3);} else if(ct===3){const i=line[x];r=pal[i*3];g=pal[i*3+1];bb=pal[i*3+2];al=trns&&i<trns.length?trns[i]:255;} else if(ct===0){r=g=bb=line[x];} else if(ct===4){r=g=bb=line[x*2];al=line[x*2+1];}
      out.set([r,g,bb,al],(y*w+x)*4);}
    prev=line;}
  return {w,h,data:out};
}
function encode(file,img){
  const {w,h,data}=img; const raw=Buffer.alloc((w*4+1)*h);
  for(let y=0;y<h;y++){raw[y*(w*4+1)]=0; data.copy(raw,y*(w*4+1)+1,y*w*4,(y+1)*w*4);}
  const chunk=(t,d)=>{const len=Buffer.alloc(4);len.writeUInt32BE(d.length);const td=Buffer.concat([Buffer.from(t,'ascii'),d]);const c=Buffer.alloc(4);c.writeUInt32BE(crc32(td));return Buffer.concat([len,td,c]);};
  const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(w,0);ihdr.writeUInt32BE(h,4);ihdr[8]=8;ihdr[9]=6;ihdr[10]=0;ihdr[11]=0;ihdr[12]=0;
  fs.writeFileSync(file,Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',zlib.deflateSync(raw)),chunk('IEND',Buffer.alloc(0))]));
}
function scale(img,k,bg){const {w,h,data}=img;const o=Buffer.alloc(w*k*h*k*4);for(let y=0;y<h*k;y++)for(let x=0;x<w*k;x++){const s=((y/k|0)*w+(x/k|0))*4;let px=[data[s],data[s+1],data[s+2],data[s+3]];if(bg&&px[3]<255){const a=px[3]/255;px=[0,1,2].map(i=>Math.round(px[i]*a+bg[i]*(1-a)));px.push(255);}o.set(px,(y*w*k+x)*4);}return {w:w*k,h:h*k,data:o};}
module.exports={decode,encode,scale};
