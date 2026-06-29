# /reference — READ-ONLY

`WixxMovement.gd` is the validated GDScript movement grey-box (SPEC §14), tested
on a real Ardwiino guitar. Per CLAUDE.md it is the **source of proven feel and
tuning numbers** and is **READ-ONLY**:

- Port constants and behaviour *from* it (M2 onward).
- Do **not** build on it or copy its hand-rolled floor physics wholesale — the
  real game uses `CharacterBody2D` with real collision and data-driven input.
- A `.gdignore` keeps Godot from importing this folder, so the prototype script
  never loads into the C# project.
