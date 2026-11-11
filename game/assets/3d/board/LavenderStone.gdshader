shader_type spatial;
uniform vec4 tint : source_color;
varying vec2 slab;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7)))*4375.5453); }
float noise2(vec2 p) {
 vec2 a=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(hash(a),hash(a+vec2(1,0)),f.x),mix(hash(a+vec2(0,1)),hash(a+1.0),f.x),f.y);
}
void vertex(){slab=(MODEL_MATRIX*vec4(VERTEX,1.0)).xz;}
void fragment(){
 vec2 p=slab*1.42;
 p+=0.18*vec2(noise2(p*4.0),noise2(p*4.2+7.0));
 vec2 cell=floor(p), f=fract(p);
 float a=10.0,b=10.0;
 for(int j=-1;j<=1;j++){for(int i=-1;i<=1;i++){
 vec2 k=vec2(float(i),float(j)), s=cell+k;
 vec2 d=k+0.18+0.64*vec2(hash(s),hash(s+2.91))-f;
 float v=dot(d,d); b=min(b,max(a,v)); a=min(a,v);
 }}
 float grain=noise2(slab*59.0);
 float vein=(1.0-smoothstep(.0,.030,b-a))*smoothstep(.26,.72,noise2(slab*1.8+2.0));
 ALBEDO=tint.rgb*(.88+.15*noise2(slab*3.5)+.08*grain-.19*vein);
 ROUGHNESS=.69+.07*grain;
}
