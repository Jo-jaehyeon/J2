from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8')
s=s.replace('import bpy, math, random, json','import bpy, math, random, json, sys\nsys.path.insert(0,"C:/Jerry/UnityProject/J2/Tools/Map")\nfrom natural_trees import TREE_PALETTE, add_tree')
s=s.replace('mats={}','palette.update(TREE_PALETTE)\nmats={}')
a=s.index('def tree(');b=s.index('for side in [-1,1]:',a)
s=s[:a]+'''def tree(x,y,h):
    add_tree(scene,stage,mats,x,y,h+1,int((x+100)*1000+y*10))
''' +s[b:]
s=s.replace("['MP_Bark','MP_Leaf','MP_LeafLight','MP_LeafDark','MP_Rock']", "['MP_Bark','MP_BarkLight','MP_BarkDark','MP_LeafSun','MP_LeafFresh','MP_LeafDeep','MP_LeafMid','MP_Leaf','MP_LeafLight','MP_LeafDark','MP_Rock']")
s=s.replace("['MP_Bark','MP_Leaf','MP_LeafLight','MP_LeafDark']", "['MP_Bark','MP_BarkLight','MP_BarkDark','MP_LeafSun','MP_LeafFresh','MP_LeafDeep','MP_LeafMid','MP_Leaf','MP_LeafLight','MP_LeafDark']")
s=s.replace('abs(v.co.x)<1.6 and v.co.y>9.8','abs(v.co.x)<2.2 and v.co.y>9.5')
s=s.replace('math.pi/2 if idx in [3,4]', '-math.pi/2 if idx in [3,4]')
s=s.replace('DragonEyeMultiplayerThroughBattlefieldsFinal.blend','DragonEyeMultiplayerLandscape.blend')
p.write_text(s,encoding='utf-8')
