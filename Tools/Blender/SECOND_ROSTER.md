# Second reference roster — 2026-09-13

Created and integrated 15 units from 56 reference PNGs in `J2_AssetImage/ready`.
The source images were moved to `confirm` after import and runtime verification.
Per-asset `Source~/reference-confirmation.jsonl` records original paths, hashes and destinations.

| Grade | Units |
|---|---|
| Baby | Tsunomon, Tanemon, Pagumon |
| Rookie | Gabumon, Palmon, DemiDevimon |
| Champion | Garurumon, Togemon, Devimon |
| Ultimate | WereGarurumon, Lillymon, Myotismon |
| Mega | MetalGarurumon, Rosemon, VenomMyotismon |

Each unit is under `Assets/Resources/Digimon/<Grade>/<Name>` with an FBX, prefab,
four Legacy clips, external materials and an independent native Blender scene in
`Source~`. Display names and gameplay values are preserved by the importer.

`next_roster.py` contains the new recipes. It uses `roster_models.py` for rigging
and export infrastructure, with smooth shading and continuous surface colors.
No fixed low-poly cap is applied. Meshes range from 20,244 to 130,032 triangles.
`inspect_next.py` creates temporary Blender inspection scenes, not image deliverables.

The shared Solid shader has an opt-in `_UseVertexColor` property, defaulting to
zero for existing materials. The manifest-driven importer sets it only for
materials whose palette declares `vertexColor`. This preserves existing assets'
color behavior while allowing smooth painted markings on the new models.

Verified:
- Individual import checks: mesh/weights, finite animated bounds, size and ground,
  four clips, loop endpoints, material mapping and resource path.
- Runtime checks passed for all 25 available assets: Idle startup, Walk, Attack,
  Special/Hit fallback and Death duration.
- Surface checks passed for all 15 new assets: FBX retains vertex colors, palette
  switches are enabled, and Solid shader compiles.
- Internal front/oblique/rear inspections led to corrections to ear profiles,
  garment overlap, collar shape, arm length and color mapping.

Unity was returned to Edit mode. User scenes were not saved or replaced.
Visual fidelity still requires the user's artistic assessment; automated checks
verify technical integration, not an exact match to the reference artwork.

For revisions, call `build_next(name, revision=True)` only for the requested unit,
then `DigimonBlenderAssetBuilder.BuildNamed(name)` and the relevant validators.
References are now in `confirm`; the current generator expects ready inputs, so
resolve the saved reference-confirmation record rather than silently inventing
missing inputs or moving the whole reference directory.
