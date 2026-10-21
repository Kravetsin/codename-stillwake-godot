const E=require('./engine.js');const {P,MAPS,Grid,stepNow,stepNew}=E;
function hero(w,c,r){return {x:c*1000+500,y:(r+1)*1000,vx:0,vy:0,dx:0,dy:0,climbing:false,facing:1};}
function arc(fn,map,hold,label){const w=new Grid(MAPS[map]);let h=hero(w,5,map==='planet'?10:6);const y0=h.y;let minY=h.y,t=0,land=null;
 for(let i=0;i<200;i++){const inp={x:0,y:0,jump:i===0,jumpHeld:i<hold,sprint:false};h=fn(w,h,inp);minY=Math.min(minY,h.y);if(i>2&&h.y===y0&&h.vy===0&&land===null)land=i+1;}
 console.log(label.padEnd(28),'rise',(y0-minY),'cells',((y0-minY)/1000).toFixed(2),'airtime steps',land,'=',Math.round(land*1000/60),'ms');}
arc(stepNow,'flat',99,'now aboard');
arc(stepNew,'flat',99,'new aboard hold');
arc(stepNew,'flat',1,'new aboard tap');
arc(stepNew,'flat',8,'new aboard 8 steps');
arc(stepNow,'planet',99,'now planet');
arc(stepNew,'planet',99,'new planet hold');
arc(stepNew,'planet',1,'new planet tap');
