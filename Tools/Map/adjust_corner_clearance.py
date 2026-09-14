from pathlib import Path
r=Path('C:/Jerry/UnityProject/J2')
p=r/'Tools/Map/build_multiplayer_map.py';s=p.read_text(encoding='utf-8').replace("'cornerCellHalfOffset':.85","'cornerCellHalfOffset':3.25").replace('v.co.x+=.85 if v.co.x>0 else -.85','v.co.x+=3.25 if v.co.x>0 else -3.25')
s=s.replace("    # Move the rear-center tree", "    if name in ['MP_Sand','MP_WetSand']:\n        for v in o.data.vertices:v.co.x*=1.3\n    if name in ['MP_Bark','MP_Leaf','MP_LeafLight','MP_LeafDark','MP_Rock']:\n        for v in o.data.vertices:\n            if abs(v.co.x)>7.5:v.co.x+=3.25 if v.co.x>0 else -3.25\n    # Move the rear-center tree")
s=s.replace('DragonEyeMultiplayerThroughBattlefields.blend','DragonEyeMultiplayerThroughBattlefieldsFinal.blend');p.write_text(s,encoding='utf-8')
p=r/'Tools/Map/build_prefabs.cs';s=p.read_text(encoding='utf-8').replace('p.x>0?.85f:-.85f','p.x>0?3.25f:-3.25f');p.write_text(s,encoding='utf-8')
