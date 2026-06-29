extends Node2D
## Wixx — Godot 4 movement prototype, wired to YOUR guitar (Ardwiino map).
##
## SETUP: New scene -> add a "Node2D" as root -> attach this script -> press F6.
##   (Plug the guitar in first. A keyboard fallback also works for quick checks.)
##
## GUITAR:  strum up/down = move (HOLD),  Green = jump,  Red = sprint (hold),
##   Yellow = lute swing,  whammy = crouch (crouch + moving = slide),  tilt = super-jump.
## KEYS:    Left/Right = move,  Space = jump,  Shift = sprint,  J = swing,
##   Down = crouch,  X = super-jump.
## TAB = toggle Hold (chosen) vs Flick (cut, kept only for comparison).   R = reset.
##
## Hold (Scheme B) is the locked movement scheme after hardware testing.
## Tilt self-calibrates its rest value, fires on level (not edge) for reliability,
##   and super-jump now floats to full height instead of being cut short.
## Tar: you sink IN, strum up to climb, and LEAP OUT the moment you breach the surface.

# ---- guitar map (straight from the inspector readings) ----
const FRET_GREEN := 0
const FRET_RED := 1
const FRET_YELLOW := 3
const STRUM_UP := 11      # move left
const STRUM_DOWN := 12    # move right
const WHAMMY_AXIS := 2    # rests near -1.0, pressing sweeps toward +1.0
const TILT_AXIS := 3      # star-power tilt (rest value learned at runtime)
const WHAMMY_ON := -0.4   # whammy counts as "pressed" above this
const TILT_DEFLECT := 0.35 # tilt fires when axis 3 moves this far from rest (lower = more sensitive)
#   ^ if tilt still won't fire, lower further. If it fires on its own, tap R while
#     holding the guitar level to re-learn the rest value.

# ---- tuning (mirrors the browser tester) ----
const GRAVITY := 0.8
const MOVE_ACCEL := 0.9
const MAX_SPEED := 4.2
const FRICTION_HOLD := 0.78
const FRICTION_FLICK := 0.94
const JUMP_V := 13.0
const SPRINT_MAX := 1.9
const SUPER_JUMP_V := 21.0    # ~5x a normal jump now that it floats; raise for even higher (may exit top of window)
const SUPER_LAUNCH := 50      # frames the super-jump floats (must cover the whole rise)
const SUPER_CD := 90          # cooldown frames (~1.5s @ 60Hz) — spec: super-jump is cooldown-gated

const TAR_SINK := 0.55
const TAR_KICK_RISE := 7.0
const TAR_KICK_FWD := 8.0
const TAR_DEATH := 58.0
const TAR_ENTRY_DEPTH := 26.0   # how deep you plunge on contact (must be > a couple of kicks, or you breach instantly)
const TAR_EXIT_JUMP := 12.0     # launch up off the surface on breach
const TAR_EXIT_FWD := 4.0       # ...and forward, away from the pit
const TAR_LAUNCH_FRAMES := 14   # frames the exit jump floats

# ---- scene geometry (set in _ready from the viewport) ----
var W := 1152.0
var H := 648.0
var GROUND_Y := 480.0
var TAR_X := 720.0
var TAR_W := 280.0

# ---- state ----
var mode := "hold"        # chosen scheme; TAB to compare against flick
var device := -1
var prev := {}
var tilt_rest := 2.0      # impossible sentinel; real rest value learned on first frame

var px := 200.0
var py := 0.0             # 0 = on ground, negative = airborne
var vx := 0.0
var vy := 0.0
var on_ground := true
var facing := 1
var run_dir := 0
var sprint_charge := 1.0
var attack_timer := 0
var crouching := false
var sliding := false
var super_cd := 0
var launch := 0           # >0 = a forced jump (tar exit / super) is floating; not subject to the cut
var in_tar := false
var tar_depth := 0.0
var tar_facing := 1
var last_strum := 0
var deaths := 0
var sank_flash := 0

var font: Font

func _ready() -> void:
	var vp := get_viewport_rect().size
	W = vp.x
	H = vp.y
	GROUND_Y = H - 150.0
	TAR_X = W * 0.6
	TAR_W = W * 0.22
	px = W * 0.18
	font = ThemeDB.fallback_font

func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_TAB:
			mode = "hold" if mode == "flick" else "flick"
		elif event.keycode == KEY_R:
			_reset()

func _reset() -> void:
	px = W * 0.18; py = 0; vx = 0; vy = 0; run_dir = 0
	in_tar = false; tar_depth = 0; last_strum = 0; super_cd = 0; launch = 0
	tilt_rest = 2.0   # re-learn the tilt rest value (hold the guitar level)

func _btn(idx: int) -> bool:
	return device >= 0 and Input.is_joy_button_pressed(device, idx)

func _axis(idx: int) -> float:
	return Input.get_joy_axis(device, idx) if device >= 0 else -1.0

func _exit_tar() -> void:
	# Breach the surface with a real jump up-and-away, so you get airtime to
	# change strum direction and not run straight back in.
	in_tar = false; tar_depth = 0; last_strum = 0
	py = 0; on_ground = false
	vy = -TAR_EXIT_JUMP
	vx = tar_facing * TAR_EXIT_FWD
	facing = tar_facing
	launch = TAR_LAUNCH_FRAMES

func _physics_process(_dt: float) -> void:
	var pads := Input.get_connected_joypads()
	device = int(pads[0]) if pads.size() > 0 else -1
	if device >= 0 and tilt_rest > 1.5:
		tilt_rest = _axis(TILT_AXIS)   # learn tilt rest on the first frame (don't tilt at launch)

	# ---- resolve actions (guitar OR keyboard) ----
	var left := _btn(STRUM_UP) or Input.is_key_pressed(KEY_LEFT)
	var right := _btn(STRUM_DOWN) or Input.is_key_pressed(KEY_RIGHT)
	var jump := _btn(FRET_GREEN) or Input.is_key_pressed(KEY_SPACE)
	var sprint := _btn(FRET_RED) or Input.is_key_pressed(KEY_SHIFT)
	var attack := _btn(FRET_YELLOW) or Input.is_key_pressed(KEY_J)
	var crouch := (device >= 0 and _axis(WHAMMY_AXIS) > WHAMMY_ON) or Input.is_key_pressed(KEY_DOWN)
	var tilt := (device >= 0 and absf(_axis(TILT_AXIS) - tilt_rest) > TILT_DEFLECT) or Input.is_key_pressed(KEY_X)

	# ---- edges ----
	var je := jump and not bool(prev.get("jump", false))
	var le := left and not bool(prev.get("left", false))
	var re := right and not bool(prev.get("right", false))
	var ae := attack and not bool(prev.get("attack", false))

	if ae and not in_tar: attack_timer = 12
	if attack_timer > 0: attack_timer -= 1
	if sank_flash > 0: sank_flash -= 1
	if super_cd > 0: super_cd -= 1
	if launch > 0: launch -= 1

	if in_tar:
		# ---- TAR STRUGGLE: alternate strum (11/12) to climb, breach the surface to leap out ----
		tar_depth += TAR_SINK
		var kd := -1 if le else (1 if re else 0)
		if kd != 0:
			if kd != last_strum:
				tar_depth = maxf(0.0, tar_depth - TAR_KICK_RISE)
				px += tar_facing * TAR_KICK_FWD
			else:
				tar_depth = maxf(0.0, tar_depth - TAR_KICK_RISE * 0.15)
			last_strum = kd
		vx = 0; vy = 0; on_ground = false
		if tar_depth >= TAR_DEATH:
			deaths += 1; sank_flash = 28
			in_tar = false; tar_depth = 0; last_strum = 0
			px = TAR_X - 40; py = 0; on_ground = true
		elif tar_depth <= 0.0:
			_exit_tar()                                   # breached the surface — leap out anywhere
		elif px >= TAR_X + TAR_W or px <= TAR_X:
			_exit_tar()                                   # safety: waded clear of the pit horizontally
	else:
		# ---- NORMAL MOVEMENT ----
		if sprint: sprint_charge = minf(SPRINT_MAX, sprint_charge + 0.03)
		else: sprint_charge = maxf(1.0, sprint_charge - 0.05)
		var max_speed := MAX_SPEED * sprint_charge

		crouching = crouch and on_ground
		sliding = crouching and absf(vx) > 1.5

		if mode == "hold":
			var dir := (1 if right else 0) - (1 if left else 0)
			if dir != 0:
				vx += dir * MOVE_ACCEL; facing = dir
			else:
				vx *= FRICTION_HOLD
			run_dir = dir
		else:
			if le: run_dir = 0 if run_dir == -1 else -1
			if re: run_dir = 0 if run_dir == 1 else 1
			if run_dir != 0:
				vx += run_dir * MOVE_ACCEL; facing = run_dir
			else:
				vx *= (0.985 if sliding else FRICTION_FLICK)
		vx = clampf(vx, -max_speed, max_speed)

		# super-jump (tilt): level-triggered + cooldown for reliability, floats to full height
		if tilt and on_ground and super_cd == 0:
			vy = -SUPER_JUMP_V; on_ground = false; super_cd = SUPER_CD; launch = SUPER_LAUNCH
		elif je and on_ground:
			vy = -JUMP_V; on_ground = false
		# a HELD Green OR an active forced launch keeps the arc alive; only a tapped/released
		# Green gets cut. This is what makes the super-jump and tar-exit reach full height.
		var jump_held := jump or launch > 0
		if not jump_held and vy < 0: vy *= 0.85
		vy += (GRAVITY * 0.6 if (jump_held and vy < 0) else GRAVITY)

		px += vx; py += vy
		var over_tar := px > TAR_X and px < TAR_X + TAR_W
		if py >= 0:
			if over_tar:
				in_tar = true; tar_depth = TAR_ENTRY_DEPTH; last_strum = 0
				tar_facing = 1 if vx >= 0 else -1
				py = 0; vy = 0; on_ground = false
			else:
				py = 0; vy = 0; on_ground = true
		if px < -20: px = W + 20
		if px > W + 20: px = -20

	prev = {"left": left, "right": right, "jump": jump, "sprint": sprint, "attack": attack, "tilt": tilt}
	queue_redraw()

func _draw() -> void:
	# bg
	draw_rect(Rect2(0, 0, W, H), Color("0e1116"), true)
	# parallax
	for i in range(8):
		var bx := fmod(i * 190.0 + 40.0, W)
		draw_rect(Rect2(bx, GROUND_Y - 90, 110, 90), Color("141b26"), true)
	# ground
	draw_rect(Rect2(0, GROUND_Y, W, H - GROUND_Y), Color("1b2230"), true)
	draw_line(Vector2(0, GROUND_Y), Vector2(W, GROUND_Y), Color("39507a"), 2.0)
	# tar base
	draw_rect(Rect2(TAR_X, GROUND_Y - 2, TAR_W, H - GROUND_Y + 2), Color("0a0d08"), true)

	var feet_x := px
	var feet_y := GROUND_Y + (tar_depth if in_tar else py)
	var body_h := 16.0 if (crouching and not in_tar) else 30.0
	var body_w := 16.0
	var head_r := 9.0

	# lute on back
	var lx := feet_x - facing * 10.0
	var ly := feet_y - body_h * 0.6
	draw_line(Vector2(lx, ly), Vector2(lx - facing * 13.0, ly - 16.0), Color("7a4a14"), 3.0)
	draw_circle(Vector2(lx, ly), 9.0, Color("b5651d"))
	# body
	draw_rect(Rect2(feet_x - body_w / 2.0, feet_y - body_h, body_w, body_h), Color("3aa0ff"), true)
	# head + hair + eye
	draw_circle(Vector2(feet_x, feet_y - body_h - head_r + 2.0), head_r, Color("ffd9a0"))
	draw_circle(Vector2(feet_x, feet_y - body_h - head_r - 1.0), head_r * 0.85, Color("ffd84d"))
	draw_circle(Vector2(feet_x + facing * 3.0, feet_y - body_h - head_r + 2.0), 1.8, Color("222222"))

	# swing arc
	if attack_timer > 0:
		var c := Color("ffe078"); c.a = attack_timer / 12.0
		var cc := Vector2(feet_x + facing * 8.0, feet_y - body_h * 0.55)
		if facing > 0:
			draw_arc(cc, 24.0, -1.1, 1.1, 16, c, 4.0)
		else:
			draw_arc(cc, 24.0, PI - 1.1, PI + 1.1, 16, c, 4.0)

	# tar goo over character + surface line
	draw_rect(Rect2(TAR_X, GROUND_Y + 2, TAR_W, H - GROUND_Y), Color(0.078, 0.125, 0.055, 0.92), true)
	draw_rect(Rect2(TAR_X, GROUND_Y - 1, TAR_W, 5), Color("2c4a1c"), true)

	# struggle UI
	if in_tar:
		var pct := clampf(tar_depth / TAR_DEATH, 0.0, 1.0)
		draw_rect(Rect2(TAR_X, GROUND_Y - 92, TAR_W, 10), Color("26344a"), true)
		draw_rect(Rect2(TAR_X, GROUND_Y - 92, TAR_W * pct, 10), Color("e5484d") if pct > 0.7 else Color("ffcf4d"), true)
		if int(Time.get_ticks_msec() / 220) % 2 == 0:
			draw_string(font, Vector2(TAR_X, GROUND_Y - 98), "ALTERNATE STRUM!", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color("ffffff"))

	# death flash
	if sank_flash > 0:
		draw_rect(Rect2(0, 0, W, H), Color(0.9, 0.28, 0.30, sank_flash / 28.0 * 0.55), true)
		draw_string(font, Vector2(W / 2.0 - 34, 70), "SANK", HORIZONTAL_ALIGNMENT_LEFT, -1, 30, Color("ffffff"))

	# HUD
	var dev_txt := ("guitar #%d" % device) if device >= 0 else "no pad — keyboard"
	draw_string(font, Vector2(16, 30), "MODE: %s   (%s)" % [mode.to_upper(), dev_txt], HORIZONTAL_ALIGNMENT_LEFT, -1, 18, Color("9fb3c8"))
	draw_string(font, Vector2(16, 54), "vx %.2f    sank x%d" % [vx, deaths], HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color("6b7c92"))
	if super_cd > 0:
		draw_string(font, Vector2(16, 78), "super-jump cooldown", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("6b7c92"))
		draw_rect(Rect2(170, 68, 120, 8), Color("26344a"), true)
		draw_rect(Rect2(170, 68, 120 * (1.0 - super_cd / float(SUPER_CD)), 8), Color("3aa0ff"), true)
	draw_string(font, Vector2(16, H - 18), "TAB hold/flick  ·  R reset  ·  strum=move  Green=jump  Red=sprint  Yellow=swing  whammy=crouch  tilt=super", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("44515f"))
