from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8')
s=s.replace('from natural_trees import TREE_PALETTE, add_tree','from natural_trees import TREE_PALETTE, add_tree\nfrom lake_landscape import ENV_PALETTE, build_environment')
s=s.replace('palette.update(TREE_PALETTE)','palette.update(TREE_PALETTE)\npalette.update(ENV_PALETTE)')
s=s.replace(";lake=root('Lake_Geometry')",'')
a=s.index('# Shared lake, open center');b=s.index('# Shared square loop',a)
s=s[:a]+'''# Continuous enclosing shore, distant mountains and two waterfall cascades.
lake=build_environment(scene,mats)
''' +s[b:]
p.write_text(s,encoding='utf-8')
