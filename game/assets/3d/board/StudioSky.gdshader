shader_type sky;
// Broad studio cards in the reflection dome give polished facets a bright and a dark edge.
void sky() {
 vec3 d = EYEDIR;
 vec3 col = mix(vec3(.12,.075,.16),vec3(.47,.39,.53),smoothstep(-.4,.8,d.y));
 float key = smoothstep(.79,.91,dot(d,normalize(vec3(-.8,.65,1.0))));
 float rim = smoothstep(.89,.95,dot(d,normalize(vec3(.8,.08,-.4))));
 COLOR = col + key*vec3(1.45,1.21,.93) + rim*vec3(.85,.75,1.0);
}
