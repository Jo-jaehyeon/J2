# ShinTaeyil

The updated SD game asset is `Assets/Resources/PlayableCharacter/ShinTaeyil/ShinTaeyil.prefab`.
Its native Blender file and supplied references are in that folder's `Source~` directory.

The non-SD proportion study is `OriginalProportions/Source~/ShinTaeyil_OriginalProportions.blend`.
It has longer legs and torso and a smaller head relative to the body, using the same revised hairstyle.
This study and its FBX stay outside Unity Assets and are not used by the game.

The earlier SD source is preserved in `Archive~/BeforeHairRevision`.
Generator: `Tools/Blender/build_sd_tamer.py`; `build_tamer()` exports SD, `build_tamer(True)` exports the non-SD study.

Gameplay: click empty ground to move ShinTaeyil. Board pieces retain selection, double-click deployment, and right-drag placement.
Clicks outside x +/-6.7, z +/-9.6 are rejected. The right player list selects local or current-opponent camera perspective.
The current single-player game has no independent remote player boards; remote-board spectating still requires that data/network feature.

Validated: four animation clips, skin weights, materials, bounds, loop seams, actual runtime rendering, screen-to-world movement,
all four movement bounds, arrival, UI isolation and view selection. Game rule validation: 39608 checks passed.
