shader_type spatial;
render_mode specular_schlick_ggx;
// UV.x selects the facet tint; UV.y picks out the narrow polished edges.
uniform vec4 deep : source_color;
uniform vec4 body : source_color;
uniform vec4 light_facet : source_color;
uniform vec4 glint : source_color;
varying vec3 local;
varying vec3 normal_local;
varying vec3 camera_local;
void vertex() {
    local = VERTEX;
    normal_local = NORMAL;
    camera_local = (inverse(MODEL_MATRIX) * INV_VIEW_MATRIX[3]).xyz;
}
void fragment() {
    float u = UV.x;
    vec3 color = mix(deep.rgb, body.rgb, smoothstep(0.0, 0.53, u));
    color = mix(color, light_facet.rgb, smoothstep(0.42, 0.91, u));
    color = mix(color, glint.rgb, smoothstep(0.85, 1.0, u));
    // A small virtual pavilion seen through the front faces. Analytic folds, no
    // refraction pass, extra transparent shell, or scene texture.
    vec3 eye = normalize(camera_local-local);
    vec2 lateral = normalize(vec2(eye.z+0.0001,-eye.x));
    float x = dot(local.xz,lateral) + dot(normal_local.xz,lateral)*0.068;
    float y = local.y-0.48 + 0.10*normal_local.y - 0.05*eye.y;
    vec2 q = vec2(x/0.315,y/0.46);
    float cut = abs(q.x) + abs(q.y);
    float inner = 1.0-smoothstep(0.94,1.01,cut);
    float seam = 1.0-smoothstep(0.015,0.07,abs(cut-0.95));
    float mirror = smoothstep(-0.025,0.025,abs(q.x)*0.62+q.y-0.17);
    vec3 reflected = mix(light_facet.rgb,deep.rgb,mirror*0.91);
    float sliver = 1.0-smoothstep(.01,.12,abs(q.x*0.70-q.y-.14));
    reflected = mix(reflected, glint.rgb, sliver*.42);
    color = mix(color,reflected,inner*(0.32+0.12*(1.0-u)));
    color = mix(color,glint.rgb,seam*.25 + UV.y*.52);
    float edge = pow(1.0-max(dot(normalize(NORMAL),normalize(VIEW)),0.0),3.0);
    float bottom = 1.0-smoothstep(0.0,0.38,local.y);
    ALBEDO = color;
    EMISSION = color * 0.19 + glint.rgb * (0.07*edge + 0.17*bottom + 0.055*seam);
    METALLIC = 0.09; ROUGHNESS = 0.19; SPECULAR = 0.56;
}
