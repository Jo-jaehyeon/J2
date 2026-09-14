from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8')
s=s.replace("m.use_nodes=True\n", "m.use_nodes=True;m.node_tree.nodes.clear()\n")
s=s.replace("world.use_nodes=True;bg=", "world.use_nodes=True;world.node_tree.nodes.clear();bg=")
s=s.replace('r=65+random.uniform(-1,1)','r=(65+random.uniform(-1,1))*1.2')
s=s.replace('(137,137,.25)','(164.4,164.4,.25)')
s=s.replace("cd.ortho_scale=168", "cd.ortho_scale=188")
s=s.replace("bpy.data.libraries.write(str(SRC/'DragonEyeMultiplayer.blend'),{scene,preview},fake_user=True)", "bpy.data.libraries.write(str(SRC/'DragonEyeMultiplayerGenerated.blend'),{scene,preview},fake_user=True)")
p.write_text(s,encoding='utf-8')
