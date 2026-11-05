// The light of a hologram on the tablet's edge (hologram-keys): added to what is under it, every other screen row dimmed as a
// tube's scanlines. With grey, a picture is reduced to its brightness, for the modulate to tint it.
shader_type canvas_item;
render_mode blend_add;

uniform float scan = 0.35;
uniform bool grey = false;

void fragment() {
	vec4 c = texture(TEXTURE, UV);
	if (grey) c.rgb = vec3(dot(c.rgb, vec3(0.3, 0.59, 0.11)) * 1.6);
	c *= COLOR;
	if (mod(floor(FRAGCOORD.y), 2.0) < 1.0) c.a *= 1.0 - scan;
	COLOR = c;
}
