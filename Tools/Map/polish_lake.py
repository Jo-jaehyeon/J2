from pathlib import Path
p=Path('Tools/Map/lake_landscape.py');s=p.read_text(encoding='utf-8')
s=s.replace("'LakeDeep': '238CA7', 'LakeBlue': '299FB4', 'LakeTeal': '3CADBC'", "'LakeDeep': '278FA7', 'LakeBlue': '2994AA', 'LakeTeal': '36A5B5'")
s=s.replace("'CliffLight': 'B3BBA7', 'CliffShade': '728D84'", "'CliffLight': 'A0ADA0', 'CliffShade': '87998F'")
s=s.replace("elif z>43:mat='DistantLight' if n.z<.73 else 'DistantStone'", "elif z>43:mat='DistantStone'")
s=s.replace("elif n.z<.70:mat=('CliffLight','CliffStone','CliffShade')[(i+2*j)%3]", "elif n.z<.70:mat='CliffStone'")
s=s.replace("z=WATER_Z+.016*math.sin(x*.8+y*.31)*math.cos(y*.47)", "z=WATER_Z")
s=s.replace("else:mat='LakeBlue' if (i+3*j)%17<3 else 'LakeDeep'", "else:mat='LakeDeep'")
a=s.index('                # A few uneven green ledges');b=s.index('\n\n\ndef _tree',a)
s=s[:a]+s[b:]
s=s.replace('accepted<260 and attempts<1600','accepted<440 and attempts<3200')
p.write_text(s,encoding='utf-8')
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8').replace('DragonEyeMultiplayerLandscapeFinal.blend','DragonEyeMultiplayerLake.blend');p.write_text(s,encoding='utf-8')
