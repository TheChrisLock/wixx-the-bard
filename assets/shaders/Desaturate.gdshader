shader_type canvas_item;

// Full-screen desaturation for the spell performance (SPEC §5.2/§8 — "the scene's
// saturation drops while the chart is on screen; the world greys back, the highway
// stays vivid"). The overlay ColorRect reads the composited screen below it and
// outputs it greyed by `amount`; the highway HUD draws on a higher CanvasLayer and
// is never touched. `amount` is driven from authoritative gameplay state (rule 7).

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_nearest;
uniform float amount : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec4 src = texture(screen_tex, SCREEN_UV);
	float luma = dot(src.rgb, vec3(0.299, 0.587, 0.114));
	vec3 greyed = mix(src.rgb, vec3(luma), amount);
	COLOR = vec4(greyed, 1.0);
}
