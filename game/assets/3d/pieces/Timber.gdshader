shader_type spatial;
// Coordinates are in board lengths along/across each plank, including the diagonal braces.
uniform vec4 honey : source_color = vec4(0.72,0.44,0.23,1);
uniform vec4 chestnut : source_color = vec4(0.39,0.19,0.085,1);
void fragment() {
    vec2 p = UV;
    float wander = 0.48*sin(8.0*p.x + sin(p.y*6.0)) + 0.22*sin(17.0*p.x+p.y*4.0);
    float g = p.y*94.0 + wander + 0.44*sin(p.y*29.0+p.x*3.0);
    float ring = 0.5+0.5*sin(g);
    float thin = smoothstep(0.82,0.99,ring) * (0.5+0.5*sin(p.x*12.0+g*0.08));
    float shade = clamp(0.38+0.40*COLOR.r+0.13*sin(g*0.33)-0.16*thin,0.,1.);
    ALBEDO = mix(chestnut.rgb,honey.rgb,shade);
    ROUGHNESS=0.64+0.10*thin;
}
