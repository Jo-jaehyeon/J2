import bpy,json
from pathlib import Path
src=Path('C:/Jerry/UnityProject/J2/Assets/Resources/Map/Source~')
work=next(s for s in bpy.data.scenes if any(o.name.startswith('Railway_Geometry') for o in s.objects))
preview=sorted([s for s in bpy.data.scenes if s.name.startswith('J2_Multiplayer_Overview')],key=lambda s:s.name)[-1]
bpy.data.libraries.write(str(src/'DragonEyeMultiplayerRing.blend'),{work,preview},fake_user=True)
report={'battlefields':8,'trains':1,'route':'one closed square loop','center':'water only','meshes':sum(o.type=='MESH' for o in work.objects),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in work.objects if o.type=='MESH')}
(src/'geometry-report.json').write_text(json.dumps(report,indent=2))
bpy.context.window.scene=bpy.data.scenes['Scene']
print(report)
