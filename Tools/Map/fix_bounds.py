from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8');s=s.replace('bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)\nfor rt,filename','bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)\nfor rt,filename');p.write_text(s,encoding='utf-8')
