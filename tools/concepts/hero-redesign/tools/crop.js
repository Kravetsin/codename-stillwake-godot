const {decode,encode,crop}=require('./png.js');const fs=require('fs');
const [f,o,x,y,w,h,s]=process.argv.slice(2);fs.writeFileSync(o,encode(crop(decode(f),+x,+y,+w,+h,+(s||4))));
