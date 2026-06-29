# Wixx The Bard — Game Design Spec (v0.8)

**One-line pitch:** A momentum-driven retro platformer where a small bard with a magic lute is controlled entirely with a Guitar Hero controller, and his most powerful spells are mini-songs you have to actually play.

**Status:** Concept / pre-prototype. This is a thinking document, not a contract. Everything here is provisional until the movement prototype proves out.

**v0.2 changes:** added tilt super-jump, whammy crouch/slide, the strum-to-run race mechanic, and the Music Run level archetype (§4). The full instrument is now mapped.

**v0.3 changes:** added the tar / quicksand struggle hazard (§4.4), which reuses strum-to-run as a cheap, droppable Exploration-level mechanic.

**v0.4 changes:** locked eight open decisions (§13) — tilt cooldown, whammy slide, global time-slow + desaturation, Music Run launch-ramp-then-auto-forward with flick lane-change, anytime radial loadout, dual-format bosses, and a v1 Accessibility Mode.

**v0.5 changes:** elevated controller remapping to a first-class v1 feature (§10) sharing the A/V-latency Options screen, supporting arbitrary guitars with axis rest/direction auto-detect — driven by real findings from a test guitar where nothing about the layout was guessable.

**v0.6 changes:** movement scheme **locked to Hold (Scheme B)** after hands-on testing with a real guitar — momentum-flick was cut. The single biggest open question is now answered. Flagged Music Run's flick-to-change-lane to revisit for the same reason.

**v0.7 changes:** captured the movement-prototyping pass (tested on real hardware) — tar hazard reworked to a validated vertical climb-and-breach loop (§4.4), strum-to-run feel confirmed, tilt super-jump implementation locked (self-calibrate, level-fire, ~5×, cooldown), and a new **Prototype findings & implementation notes** section (§14) so the real build inherits the hard-won input and engineering lessons. Only the story remains open.

**v0.8 changes:** story **locked to The Silent Empire** (§7) — music outlawed, last bard, lute imbued by The Great Song. Art direction (§8) reframed: pursue *distinctive* (a **colour-is-mechanical** identity drawn from the story) over "truly unique," with production deferred until after the M6 fun gate. Audio flavour marked provisional (§9). No open *design* questions remain.

---

## 1. Design Pillars

1. **The instrument is the game.** The Guitar Hero controller isn't a control scheme bolted onto a platformer — it shapes the genre. The game leans into *flow and momentum* (Sonic-leaning) rather than pixel-precise positioning (Mario), because that's what the hardware and the musical theme both want.
2. **Mastery means learning to play.** Every special attack is a fixed little song. Getting good at an ability = learning its chart by muscle memory. Skill is literal musicianship.
3. **The whole instrument has a job.** Frets, strum, whammy, and tilt all drive verbs. Nothing on the controller is dead weight — picking it up should feel like the game was built around the hardware, because it was.
4. **Mega Drive silhouette, modern polish.** Chunky pixel art with palette discipline, but smooth scrolling, parallax, more animation frames, and modern lighting where it serves the mood.
5. **Diegetic music.** Music isn't a backing track — it's the verb. Abilities have leitmotifs, bosses speak in phrases, and the world's audio responds to play.
6. **Small, weird, and finished.** Built for fun and for Steam, solo-scoped. A great vertical slice beats a sprawling unfinished epic.

---

## 2. The Hook & Control Scheme

The whole game is played on a 5-fret guitar peripheral. Hardware available to map:

- **5 frets:** Green, Red, Yellow, Blue, Orange
- **Strum bar:** up / down (spring-centred — flick, don't hold)
- **Whammy bar:** analog (spring-centred)
- **Tilt sensor:** binary-ish (tilt past threshold)
- **D-pad + Start/Select** on the body (menu navigation, fallback movement)

### 2.1 The strum is mode-dependent (read this first)

The strum bar is the busiest input on the controller — it has **three** jobs, and the whole control design hinges on never asking it to do two at once. They're separated by context:

| Context | Strum does |
|---|---|
| **Exploration levels** | Sets movement direction (see schemes below) |
| **Music Run levels** | Alternate up/down strums = run cadence (§4.2) |
| **Tar / quicksand** | Rapid alternate strums = struggle free (§4.4) |
| **Spell performance** (any archetype) | Hits notes on the highway, in time-slow (§5.2) |

These never overlap — the game is always in exactly one of these modes — so the strum is never double-booked in practice. (Note that the last three are all the same underlying verb — *alternate strumming for effort* — in different costumes.)

### 2.2 Movement schemes (Exploration levels)

Your original idea — *hold strum up = left, hold strum down = right* — is thematically perfect but I expected it to fight the hardware. It doesn't. **Tested on a real guitar, Hold (Scheme B) won decisively; momentum-flick felt "way too weird."** The schemes are kept below for the record, but the decision is made.

**Scheme B — Hold strum (your original) — CHOSEN ✓.**
Hold strum up = move left, hold strum down = move right; release to stop.
- Verdict: felt natural and correct with the real strum in hand. The flick alternative read as strange and floaty by comparison.
- Watch-item: spring-loaded hold *could* fatigue over long sessions — keep an eye on it in extended play, but it was not a problem in the feel test.

**Scheme A — Momentum flick (rejected).**
Right hand flicks the strum to set run direction; Wixx carries momentum like Sonic.
- Looked good on paper (no continuous holding, Sonic-like flow) but felt wrong in practice — too weird, too loose. Cut.

**Scheme C — D-pad movement (unused fallback).**
Movement on the body d-pad; strum + frets reserved entirely for actions and the rhythm layer.
- Pros: reliable, precise, frees the strum completely.
- Cons: "I'm just using a d-pad" — sheds most of the novelty. Safe fallback only.

### 2.3 Full instrument map

| Input | Verb | Notes |
|---|---|---|
| **Tap Green** | Jump | Tap vs. hold = variable jump height. Essential for platformer feel. |
| **Hold Red** | Sprint / build speed | Sonic-style charge; ties into momentum. |
| **Yellow** | Lute swing (melee) | Snappy, low-commitment basic attack. |
| **Blue** | Special attack slot 1 | Triggers the spell performance (see §5). |
| **Orange** | Special attack slot 2 | Triggers the spell performance. |
| **Strum** | Move / run / play | Mode-dependent (see §2.1). |
| **Whammy (hold)** | Crouch → slide | See §2.4. |
| **Tilt up** | Super jump | See §2.4. |

Two-handed ergonomics work *for* us: strum + whammy + tilt live on the right side / body (motion), frets on the left (actions) — exactly how a guitar is held.

### 2.4 Traversal & defensive verbs (new)

**Tilt → Super Jump.** Tilt the neck up to launch Wixx high into the air — you physically *raise the lute* to rocket up. Thematically the strongest of the new verbs (it's the Star Power gesture, repurposed). *Prototyped & validated — feels great at roughly 5× a normal jump.* Design + implementation notes:
- The tilt sensor is coarse and its axis **rest value and polarity vary per guitar**, so the input must **self-calibrate** (learn the rest value at runtime, fire on deflection *either* direction) and be **forgiving** — no precision expected. See the implementation findings in §14.
- **Fire on tilt *level* + cooldown, not on a clean rising edge** — edge-detection missed inputs and made it feel unreliable; level + cooldown is dependable.
- It must be **gated**, or it trivialises platforming. **Locked: a cooldown** — after a super-jump the lute recharges before it fires again (the glow dims and refills). A deliberate traversal tool, not a spammable skip button. (At ~5× height it can clear the screen, so super-jump zones imply a camera / taller level design.)

**Whammy → Crouch / Slide.** Hold the whammy to crouch (duck under projectiles, drop through one-way platforms). Because whammy is analog, depth can map to crouch depth. Crouch **while carrying momentum** = a **slide**: pass under low hazards, slide into enemies for a knockdown. Free extra verb out of one input. Whammy springs back, so "hold to stay low, release to stand" is natural and self-correcting.

> Note: inputs are **mode-contextual**. In normal play whammy = crouch; inside a spell performance the whammy instead handles sustained/bent notes (§5.3). Same physical input, different meaning per mode — consistent with how the strum already works.

---

## 3. Core Gameplay Loop

1. Move through a side-scrolling level with momentum (run, jump, sprint, crouch/slide, super-jump).
2. Dispatch basic enemies with the **Yellow** lute swing.
3. Hit a tougher enemy / obstacle → trigger an equipped **special attack** (Blue/Orange) → enter the spell performance.
4. Perform the song correctly → spell fires + cooldown begins. Fail → cooldown only, no spell.
5. Reach the level boss → **Rock Off** (versus rhythm duel).
6. Clear level → unlock new abilities / story beat → re-equip loadout.

Levels come in two archetypes (§4) that share this loop but flip the strum's role.

---

## 4. Level Archetypes (new)

### 4.1 Exploration levels (the default)
Standard momentum platforming using the §2.2 directional strum. Explore, fight, solve light traversal, find secrets (great use for the tilt super-jump — hidden high ledges). This is the bulk of the game and the home of the vertical slice.

### 4.2 Music Run levels (the showcase)
Inspired directly by the rhythm-platforming setpieces in *Rayman Legends* — auto-forward levels where running, jumping gaps, and killing enemies all land **on the beat** of a track. These are the game's signature spectacle levels and the natural home for the **strum-to-run race mechanic**:

- **Launch ramp (strum-to-run):** the level *starts* with a strum-to-run sprint — alternate up/down strums to drive Wixx's legs (left foot / right foot), exactly like alternate picking, to build up to top speed. A little skill-check intro that earns your momentum.
- **Then auto-forward:** once you're up to speed the level takes over forward motion (on-rails), freeing the strum from running duty. The launch ramp is the only place you actively strum to move.
- **Flick = change lane:** during the auto-forward stretch, flick the strum up/down to switch between vertical lanes — dodge hazards, grab collectibles, line up enemy hits. (The directional flick gets repurposed for lanes now that forward speed is automatic — no conflict, since the strum-to-run ramp is already done by this point.)
- **Beat-timed actions:** jumps (Green), swings (Yellow), and super-jumps (tilt) are charted to the music. Hit them in time and the level *sings* — the track layers in as you nail beats, drops out when you miss (the *Rayman* "the level is the song" feel).
- **Failure is soft and flowy**, not a hard stop — a missed beat costs momentum/style, not necessarily a life, to keep the run feeling like a performance.

**Scope warning:** these levels are *expensive*. Each is hand-authored and synced beat-by-beat to a specific composed track — closer to choreographing a music video than building a platforming level. Treat them as **rare showcase setpieces (≈ one per world / biome)**, not the staple. Building the game *around* them would blow solo scope apart.

### 4.3 How the two relate
Exploration is the verbs; Music Run is the verbs performed *to music* with direction taken off your hands. A player learns the moveset in Exploration, then a Music Run level asks them to perform it. Bosses (§6) are effectively a third, combat-flavoured rhythm context.

### 4.4 Tar / quicksand (struggle hazard) — *prototyped & validated*
A localised hazard that reuses the strum-to-run verb without needing a whole authored Music Run level — cheap to build, droppable anywhere in an Exploration level. **Built and tested on real hardware; the loop below feels good.**

- **Enter:** stepping onto tar plunges Wixx in to a set depth. The strum is taken over for struggling the instant he's in it (same mode-swap discipline as everywhere else); directional control is suspended.
- **The struggle is vertical.** A constant downward pull sinks him; **rapid alternate up/down strumming** climbs him back toward the surface (and nudges him forward). Clean alternation is what counts — single-direction mashing barely helps — which ties the hazard to "play it properly," not "spam the bar." This validated the core **strum-to-run feel**: alternation reads as *struggling*, not as a fiddly input gate.
- **Breach = leap out.** The moment he climbs back to the surface he **launches up and forward out of the pit** — a real jump with enough airtime to *re-point* (swap which strum direction you hold) so you don't immediately fall back in. The leap firing on surface-breach (not on reaching a horizontal edge) is what makes it feel like escaping rather than trudging.
- **Fail state:** full submersion = **death**, respawn at the entry edge. Unforgiving on purpose.
- **Tuning that worked:** plunge-in depth must be a few kicks deep (or you breach instantly and the hazard does nothing); the exit leap needs real height *and* forward carry; breaching deep inside a wide pit can drop you back in, which reads as a fair "keep fighting" loop rather than a bug.

> Confirmed as the right *first* home for strum-to-run: the feel is proven here, so Music Run levels can now be built on a verb that's known to work.

---

## 5. Ability System & Spell Performance

### 5.1 Loadout
- Two active slots, mapped to **Blue** and **Orange**.
- Unlock more abilities through progression; **swap the equipped two anytime via a quick radial menu** (held button brings up the wheel mid-level — no need to reach a checkpoint or pause screen). Encourages experimenting with loadouts on the fly.

### 5.2 The performance (the signature moment)
Triggering a special attack drops the world into a **global time-slow** (~3s) — not a hard pause. Everything keeps moving, but enemies and hazards creep along *really* slowly, so there's lingering tension without you having to platform and play notes at once. To pull the eye onto the note chart, the scene's **saturation drops** while the chart is on screen — the world greys back, the highway and lute glow stay vivid. This both reads clearly and reinforces the "the music is taking over" moment. The strum remaps to "play" mode for the duration.

- Notes scroll **right → left** across the screen (mirrored Guitar Hero).
- You strum + fret notes in time over ~3 seconds.
- The notes **play the ability's tune** as you hit them — diegetic, satisfying, memorable.
- **Fixed charts:** each ability always uses the same song, so mastery = learning to play it.

### 5.3 Difficulty tiers
Stronger abilities demand harder music: single notes → two-note chords → faster runs → sustained/bent notes (held frets + **whammy**) → full chord progressions. Difficulty of the chart gates the power of the spell.

### 5.4 Resolution
- **Success** → spell fires **and** cooldown begins.
- **Fail** → cooldown begins, spell does **not** fire. (Real risk/reward; punishes spamming.)
- **Stretch — Perfect performance:** all notes clean → empowered version of the attack (bigger AoE, extra damage, etc.). Adds a skill ceiling for experts.
- Cooldown visualised as the lute "recharging" its glow.

---

## 6. Boss Battles — "Rock Off"

Bosses are rhythm duels that climax the guitar conceit. A fight uses **both** formats as escalating phases — call-and-response to learn the boss, then a simultaneous highway to go on the offensive:

**Phase type A — Call & response.** Boss plays a phrase; you must repeat/counter it on the note highway. Teaches the boss's musical "tells." Miss lets the boss land a hit.

**Phase type B — Simultaneous highway.** A continuous note stream; landing notes damages the boss, missed notes open you to boss attacks. This is where you do real damage.

**Structure:** a fight alternates and escalates between the two — e.g. learn a phrase in call-and-response, then survive a highway onslaught, then a harder phrase, then a faster highway, into a final flurry. Call-and-response sets the rhythm vocabulary; the highway phases cash it in. Each boss = a distinct musical identity; phases shift the music; defeat = the boss's stolen song returns to the world.

---

## 7. Story — *locked*

**Premise:** The **Silent Empire** has outlawed music. **Wixx is the last bard.** His
lute has been imbued with magical energy by **The Great Song**, and he fights back
against the Empire one region at a time — restoring music (and, see §8, colour and life)
to a silenced world. (A fusion of the old Directions 2 + 3: the outlawed-music tyrant
*and* the Great-Song embodiment.)

- **Antagonists:** the Silent Empire's enforcers. Bosses are Empire champions; defeating
  one in a Rock Off returns that region's song.
- **Why it justifies the mechanic:** the whole game is an act of defiant music-making.
  Every verb — strum, swing, spell, super-jump — is Wixx playing in a world that banned it.
- **Tone:** warm, light, a little melancholic under the silence. Delivered through
  environment, animation, and music — minimal cutscenes, no exposition dumps. Names for
  abilities, regions, and bosses can now draw on this world (M4/M5).

Wants kept: simple, justifies the music mechanic, environmental storytelling.

---

## 8. Art Direction

**Stance: don't chase a "truly unique" art style — the guitar mechanic is already the
game's unprecedented hook. Aim for *distinctive and consistent* (recognisable as this
game in one screenshot), not unprecedented. And defer art *production* until after the
M6 fun gate — placeholder/grey-box through the slice, exactly as the prototype proved
works. Hold the direction below loosely *now* (it touches shaders and level design); paint
nothing yet.**

- **The identity, drawn from the story: colour is mechanical.** The Silent Empire drains
  the world of music, so drain it of **colour** too — a desaturated, greyed world that
  floods back to colour and life as Wixx plays. Regions re-saturate as you clear them; the
  lute glows; the spell-performance desaturation (§5.2) is the same language; Music Run
  levels visibly "sing." Colour-restoration isn't novel in itself (de Blob, Okami, Hue) —
  what's distinctive *here* is that it's driven by **music, via a guitar, in a rhythm
  platformer**. That fusion is the identity, and it's inseparable from the mechanic rather
  than bolted on, which is what makes it durable.
- **Reference point:** Sega Mega Drive / Genesis *silhouette* (chunky sprites, palette
  discipline), not its literal limits.
- **"Modern take":** more colours than a real Genesis (~61–64 on-screen from a 512 palette),
  smooth multi-layer parallax, extra animation frames, modern lighting/particles, optional
  CRT toggle — and the saturation system above as the signature layer.
- **Internal render resolution:** ~320×224 (NTSC Genesis-native) or a clean multiple,
  integer-scaled.
- **Comparables to study:** *Sonic Mania* (modern-retro Genesis gold standard),
  *Freedom Planet*, *The Messenger*, *Shovel Knight* (philosophy), *Rayman Legends* (Music
  Run levels).
- **Wixx:** small blonde boy, lute on back; readable, expressive run/jump/swing/slide
  animations; the lute glows/pulses during spells and lights up on super-jump.

---

## 9. Audio Direction

Music is the mechanic, so audio is a top-tier concern, not polish.

- **Flavour (provisional — not locked):** rolling with FM-synth / chiptune nods to the
  Genesis YM2612, optionally hybridised with modern instrumentation. Open to change; the
  composition approach (adaptive, diegetic, learnable motifs) matters more than the timbre,
  and the flavour can be decided properly when scoring actually starts.
- **Adaptive & diegetic:** ability tunes, boss phrases, world audio that responds to play. Music Run levels are *composed first*, level-designed second.
- **Latency is critical.** Rhythm games live and die on timing. USB guitar input lag + audio output lag + display lag all stack. An **A/V calibration screen** (offset adjustment) is mandatory, not optional — and doubly so for Music Run levels and strum-to-run.
- Each ability gets a short, distinct, learnable motif. These double as the player's "spellbook" by ear.

---

## 10. Tech Stack

**Recommendation: Godot 4 (C#).**
- You've already shipped 3D/C# work in Godot 4 (Gear Heart); the learning cost is low.
- Godot 4's 2D pipeline is best-in-class for exactly this kind of pixel platformer.
- Free, open source, good pixel workflow, straightforward Steam export.
- **Steam integration:** via the GodotSteam community integration / GDExtension. *Confirm current Godot 4.x support status before relying on it.*

**Alternative: MonoGame / FNA (C#).** Maximum control, Celeste/Stardew lineage, but you build all the plumbing yourself. Only worth it if hand-rolling the engine is part of the fun. For a solo "ship it" target, Godot wins on velocity.

### Input pipeline (the highest-risk dependency)
- There is **no single "Guitar Hero controller."** PS3, Xbox 360 (Xplorer / wireless), GH Live (USB dongle), Rock Band, and DIY boards (Ardwiino/Santroller) all enumerate differently over HID/XInput, often needing adapters on PC. *Confirmed in practice:* a test Ardwiino guitar exposed frets on arbitrary button indices (0,1,3,2,9), the strum as two **buttons** (not an axis), whammy as an **axis resting at −1.0**, and tilt as a separate axis — none of it guessable in advance.
- Godot reads these via its joystick/gamepad API (SDL2 under the hood — the same backend Clone Hero uses, so anything those see, Godot sees). But the layout is per-device and frequently **unmapped**, so raw button/axis indices must be read directly.
- **Player-facing Controls remapping is a first-class feature, shipped in v1, living in the same Options screen as the A/V latency calibration (§9).** It is not optional polish — without it, only identical hardware to the dev's would work. Requirements:
  - Rebind every verb (move/jump/sprint/swing/specials/crouch/super-jump) by *pressing the control* — capture whatever button or axis fires, exactly like the prototype's binder.
  - **Auto-detect axis rest value and direction**, because analog controls (whammy, some strums/tilts) rest at −1, +1, or 0 depending on the board. The binder must learn "engaged = moved away from rest," not assume a polarity.
  - Sensible **presets** for the common controllers (360/PS3/GH Live/Rock Band) so most players never open the binder, with full manual rebinding as the fallback for everything else.
- Study how **Clone Hero** and **YARG** handle guitar input and their controller databases — that's the proven path.

---

## 11. Key Technical Risks (de-risk in this order)

1. **Guitar → Godot input.** Spike this *first*, before art or levels. Read frets + strum + whammy + tilt reliably, with a remap/calibration screen. If this isn't solid, nothing else matters.
2. **Movement feel.** Grey-box Scheme A (and B as a control) with a real guitar in hand. Decide by feel, not on paper.
3. **Strum-to-run feel.** Separate spike: does alternate-strumming-to-run feel energising or just tiring? Prove it in the **tar hazard (§4.4)** first — it's the cheapest possible test bed — before authoring any Music Run levels on top of it.
4. **Rhythm latency.** Build the A/V offset calibration early; validate the note-timing windows feel fair on real hardware.
5. **The performance mode.** Prove the time-slow special-attack loop is fun in a vacuum (one ability, one chart) before authoring a library of abilities.
6. **Music Run authoring cost.** Build *one* short Music Run level end-to-end before committing to more — they're the most expensive content in the game.
7. **Steam export + controller compatibility** on a clean machine, early enough to catch surprises.

---

## 12. Scope — Vertical Slice First

Build the smallest thing that proves the game is fun:

**Vertical slice (the only thing that matters initially):**
- One biome, a few minutes of **Exploration** level.
- Movement + jump + sprint + Yellow lute swing + whammy crouch/slide + tilt super-jump.
- **One** special ability with its spell performance (one fixed chart).
- **One** Rock Off boss.
- Guitar input + an Options screen housing **both** control remapping (press-to-bind, any guitar) **and** A/V latency calibration.

If the slice is fun → expand abilities, biomes, bosses, story. If it isn't → the controller idea needs rework, and you've spent days, not months, finding out.

**Phase 2 (after the slice lands):** ability library + anytime-radial loadout swapping, multiple biomes, full boss roster, story delivery, **one** Music Run level as a proof of the archetype, Steam page/achievements.

**Accessibility Mode (committed for v1):** a standard gamepad fallback so players without a guitar can play. Mapped sensibly (stick/d-pad to move, face buttons for jump/sprint/attack/specials, bumpers or stick-flick for the rhythm inputs). **Clearly labelled in-game as "Accessibility Mode — the guitar is the intended way to play"** so it reads as an accommodation, not the default experience. In scope for v1, not the vertical slice.

**Phase 3:** more Music Run setpieces (budget permitting), additional worlds, polish.

> Music Run levels deliberately sit *outside* the vertical slice — they're high-risk, high-cost showcase content, and the core game has to be proven fun on Exploration levels first.

---

## 13. Decisions & Open Questions

### Locked decisions
- **Tilt super-jump:** gated by a **cooldown** (lute recharges before it fires again).
- **Whammy:** **crouch**; crouching while running becomes a **slide**.
- **Spell time-slow:** **global** slow-creep (enemies barely move, not paused) + **saturation drop** while the chart shows, to pull focus onto the highway.
- **Music Run movement:** **strum-to-run launch ramp** to build speed at the start, then **pure auto-forward**, with **flick = change vertical lane** during the auto stretch.
- **Loadout swapping:** **anytime, via a radial menu** (no checkpoint/pause needed).
- **Boss format:** **both** — call-and-response phases *and* simultaneous-highway phases, alternating and escalating within a fight.
- **Accessibility:** **standard gamepad fallback shipped in v1**, clearly labelled as Accessibility Mode and not the intended way to play.
- **Tar hazard:** pull is straight down; full submersion = death.
- **Control remapping:** in-game, press-to-bind, in the **same Options screen as A/V latency**; must support arbitrary guitars (auto-detect axis rest/direction) with presets for common controllers. Shipped in v1.
- **Movement (Exploration):** **Hold (Scheme B)** — direction held on the strum. Hardware-tested; momentum-flick felt wrong and was cut.
- **Strum-to-run feel:** **validated** via the tar hazard (§4.4) — alternate-strumming reads as *struggling*, not a fiddly input gate. The verb works; Music Run levels can be built on it.
- **Story:** **locked — The Silent Empire** (§7). Music is outlawed; Wixx is the last bard; his lute is imbued by The Great Song; he fights back, restoring music and colour region by region.
- **Art identity:** **distinctive, not "unique"** — colour-is-mechanical (§8), driven by the music/guitar. *Production deferred until after the M6 fun gate.*

### Still open / deferred
- **Audio flavour:** rolling with FM-synth/chiptune provisionally; decide for real when scoring starts (§9).
- **Music Run lane-change via flick (§4.2):** revisit — flicking felt wrong for *core* movement, so a discrete flick-to-snap-lane may also feel off. Prototype before committing; d-pad or hold-to-lane may be better.
---

## 14. Prototype findings & implementation notes

Captured from the movement prototyping pass (browser grey-box → Godot, tested on a real Ardwiino guitar). These are the things that will save pain when this becomes the actual `CharacterBody2D` slice.

### Input / hardware (the big lessons)
- **The browser Gamepad API is a dead end for guitars.** Chrome/Firefox filter out the guitar HID class — a test guitar was invisible to the browser and to neutral web gamepad testers, while Clone Hero, Steam and Windows `joy.cpl` all read it fine. Don't prototype guitar input in a browser.
- **Godot reads guitars natively** (SDL2 backend — the same one Clone Hero uses). Anything those tools see, Godot sees, often as an **UNMAPPED** joypad — so read **raw button/axis indices** directly rather than relying on the standard gamepad mapping.
- **Nothing about a guitar's layout is guessable.** The test unit reported: frets on buttons **0,1,3,2,9**; strum as **two buttons (11/12), not an axis**; whammy as an **axis resting at −1.0**; tilt as an **axis with an unknown rest value/polarity**. Every guitar will differ — hence the mandatory press-to-bind remapper (§10).
- **Analog inputs must self-calibrate.** Learn each axis's **rest value at runtime** and treat "engaged" as *deflection away from rest in either direction* — never assume a polarity or a zero rest. This is what made tilt work; it's the rule for the remapper too.

### Movement / feel (validated)
- **Hold beat Flick decisively in the hand**, against the on-paper prediction — the only reliable judge of feel is the real instrument. Lock nothing about feel from reasoning alone.
- **Tar struggle (strum-to-run) feels good** as a vertical climb-and-breach loop (§4.4). Plunge-in depth must be a few kicks deep; the surface-breach leap needs height + forward carry.
- **Super-jump feels right at ~5× a normal jump**, cooldown-gated, fired on tilt *level* not edge.

### Engineering footguns (don't reintroduce in the real build)
- **The variable-jump-height "cut" (reduce upward velocity when the jump button is released early) must NOT be applied to forced launches** — super-jump and the tar-exit leap aren't triggered by the jump button, so a naïve cut silently chops them to a fraction of their height. Exempt any non-button-driven launch from the cut (the prototype uses a short "launch" timer that suppresses the cut and softens gravity).
- **Fire tilt/whammy-style actions on level + cooldown, not on a clean input edge** — edge-detection on a coarse analog sensor drops inputs and reads as unreliable.
- **Per-frame tuning constants assume a fixed 60 Hz step.** The prototype runs physics in `_physics_process` (fixed tick) so the browser-tuned numbers port 1:1; if the real build goes delta-based, the constants need rescaling.
