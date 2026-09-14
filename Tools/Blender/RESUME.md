# J2 asset work checkpoint — 2026-09-13

Status: COMPLETED after user requested resumption on 2026-09-13.

## Latest request: head rollback and facial depth (revision 4)

User rejected the tapered Agumon head and requested rollback. `revise_depth.py` restores the preceding rounded rectangular head through RevisedBuilder while omitting the invented Belly object. Diaboromon retains the lean revision-3 body and gets a volumetric cranial shell, projecting upper muzzle, recessed mouth, cheek plates and extended mandible. `.cursor/rules/j2-digimon-assets.mdc` now prioritizes reference fidelity and detail instead of low-poly limits for future assets. The generator/importer no longer impose a fixed triangle ceiling; optional explicit triangle budgets remain supported.

## Latest silhouette correction (revision 3)

`revise_silhouettes.py` corrects Greymon/MetalGreymon with continuous helmet meshes (the prior enlarged inner heads protruded through the unchanged outer cap). Diaboromon was rebuilt with a lean waist, narrow thighs/shins, sloping shoulder shells, and four spines spanning neck through pelvis. Agumon now has a connected tapered head with a rounded crown, narrower mouth gap, inset teeth, and a single yellow torso: the invented Belly object was removed. This follows the user's added full-body reference and explicit rejection of the contrasting abdomen.

Four revised assets were imported individually using BuildNamed and passed editor checks. Previous native files are in `Source~/BeforeSilhouetteRevision`. This revision uses the user's conversation attachments and existing confirm references; ready was empty, so no reference movement or deletion applies.

## Latest completed face/anatomy revision

User requested distinctive eyes, removal of Koromon mouth protrusions, a squarer Agumon head, Chrysalimon tendrils curving from rear to front, and quadrupedal Diaboromon. Ten ready closeups including WarGreymon were inspected and applied. Scripts: `revise_roster.py` and `revise_wargreymon.py`. All ten passed updated editor checks and Play mode controller checks. Diaboromon has four rest contacts at matching heights and diagonal fore/hind walking pairs. Prior native sources are preserved in each `Source~/BeforeFaceRevision`.

WarGreymon now uses the existing `Mega/WarGreymon` folder and the manifest-driven importer. For individual reimports call `DigimonBlenderAssetBuilder.BuildNamed(name)` to avoid Pipeline's short request timeout. `Confirm-References.ps1` checks hashes and fresh passed reports, then moves reference files to confirm without overwriting. Per-unit `reference-confirmation.jsonl` records completed moves.

Latest reference workflow: read images from `C:/Users/User/Desktop/J2_AssetImage/ready`. After creation, integration and verification succeed, move only the completed unit's images to sibling `confirm`, preserving files on name collisions. Do not delete references. This supersedes the deletion workflow in historical notes below; previously deleted images are historical actions, not instructions for future runs.

## Completion update (supersedes the paused notes below)

- All nine queued assets were generated through connected Blender MCP, saved as native .blend and FBX, and integrated into J2 catalog entries at the prescribed English grade/name paths.
- Importer: `Assets/Editor/DigimonBlenderAssetBuilder.cs`. A VisualOffset parent isolates grounding from animation curves. FBX and prefab names are distinct.
- All nine passed mesh/material/skin/size/ground checks, four clip motion and loop checks, and actual Play mode controller transition checks. Runtime script: `Tools/Blender/validate_roster_runtime.cs`.
- Per-unit reports reside in `Source~/validation.txt` and `Source~/runtime-validation.txt`. Native .blend scene/action contents were checked.
- `Tools/Verify-Arena.ps1` passed 39,517 checks. Unity returned to Edit mode.
- All 27 reference images were deleted after verification and SHA256/path checks. No deliverable preview images were generated.
- Blender has the generated unit scenes and a temporary J2_Inspection scene; original Scene preserved. Individual native sources are saved independently.
- Unity MCP initial connection became stale after refresh; Unity CLI successfully used current Pipeline port 7801. No git commit made; unrelated edits preserved.

## Historical paused checkpoint

## Completed and saved

- Earlier WarGreymon asset is integrated under `Assets/Resources/Digimon/워그레이몬` (old path). Its importer is `Assets/Editor/WarGreymonAssetBuilder.cs`. Do not rebuild or migrate without considering existing references.
- Updated standing rules: `.cursor/rules/j2-digimon-assets.mdc`.
- Read 27 reference images for nine units from the actual folder `C:/Users/User/Desktop/J2_AssetImage`.
- Wrote `Tools/Blender/roster_models.py` (33,913 bytes at checkpoint): procedural low-poly geometry, rigid bone rigs, four animation ranges, Blender source/FBX/manifest export. `python -m py_compile Tools/Blender/roster_models.py` PASSED.
- The new nine-unit generator has NOT been executed. Geometry quality, Blender execution, FBX import, animation playback and integration are NOT yet verified. No new nine-unit final assets exist.
- Reference images have NOT been deleted.

## Units queued

| English name | Korean name | Grade |
| --- | --- | --- |
| Koromon | 코로몬 | Baby |
| Agumon | 아구몬 | Rookie |
| Greymon | 그레이몬 | Champion |
| MetalGreymon | 메탈그레이몬 | Ultimate |
| Tsumemon | 츠메몬 | Baby |
| Keramon | 케라몬 | Rookie |
| Chrysalimon | 크리사리몬 | Champion |
| Infermon | 인펠몬 | Ultimate |
| Diaboromon | 디아블로몬 | Mega |

## Next steps

1. Check current files/editor state, then inspect and execute the saved generator in small batches via Blender MCP. `build_asset(name)` builds one unit. Its existing-prefab guard prevents accidental replacement.
2. Implement a reusable manifest-driven Unity importer, using the proven WarGreymon importer as a starting point. Save each unit at `Assets/Resources/Digimon/<Grade>/<EnglishName>`.
3. Import Legacy Idle/Walk/Attack/Death clips, external materials with `DigitalArena/Solid`, prefab with `ArenaUnitAnimation`; update only corresponding catalog prefab paths/placeholder values, preserving stats and other edits.
4. Validate meshes, material mappings, skin weights, size/ground alignment, animation movement/loop continuity and runtime behavior. Do not produce preview image deliverables.
5. Only after successful integration/verification delete that unit's three reference files. Match captured SHA256 hashes and resolved paths before deletion; preserve incomplete units' images.

## Working tool connections

- Project: `C:/Jerry/Unity Project/J2`; PowerShell. Full access, approval never. Do not ask permission for already authorized work or set sandbox_permissions.
- Blender 5.2.1 LTS connected through installed MCP server, despite no direct Blender tools in this app's tool inventory.
- Client helper: `Tools/Blender/mcp_execute.py`. Call with `C:/Users/User/AppData/Local/BlenderMCP/1.0.3/server/.venv/Scripts/python.exe`, passing a Python script path. It uses MCP ClientSession with `-m blmcp`, host 127.0.0.1 port 9876 and `execute_blender_code`.
- Example runner script: load/exec `roster_models.py`, then assign `result = [build_asset('Koromon')]`.
- Unity CLI ready previously: J2 port 7800, editor PID 44092, Edit mode. Recheck before use. `unity command eval` / `eval_file --file <absolute.cs> --timeout 60000 --project-path <project> --json`.
- Unity CLI skill already read: `C:/Users/User/.codex/skills/unity/unity-cli/SKILL.md`.

## Known import pitfalls

- FBX filename must be `<Name>_Model.fbx`, distinct from `<Name>.prefab`, to avoid Resources.Load ambiguity.
- FBX unit flags: `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`, forward -Z/up Y. Recalculate mesh normals before export.
- Blender localized node names: find Principled node by `node.type == 'BSDF_PRINCIPLED'`.
- Normalize actual skinned vertices using bone.localToWorldMatrix * bindpose and bone weights (see WarGreymon importer). BakeMesh/bounds previously misrepresented scale.
- Timeline frames at 30fps: Idle 1–49 loop; Walk 60–84 loop; Attack 90–108; Death 120–150. Unity Legacy animations with no compression.
- Source files belong in ignored `Source~` directory. Native source saves only generated scene dependencies via libraries.write; preserve user's other Blender scenes.

## Preserve unrelated work

Git has existing modifications to DigitalArenaValidation.cs, ArenaRules.cs, ArenaSmokeTest.cs, README.md and other work. Old `Assets/Resources/Digimon/Tsumemon` deletions predate this batch; do not restore them. DigimonCatalog.json and ArenaUnitAnimation.cs include earlier WarGreymon work; do not overwrite wholesale. No git commit made at checkpoint.
