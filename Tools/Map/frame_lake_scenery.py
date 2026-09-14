from pathlib import Path
for name in ['render_map.py','save_native_map.py']:
    p=Path('Tools/Map')/name;s=p.read_text(encoding='utf-8-sig').replace('cam.data.lens=32','cam.data.lens=28').replace('(28,-117,40)','(18,-150,50)').replace('Vector((0,27,13))','Vector((0,20,20))');p.write_text(s,encoding='utf-8')
s=Path('Tools/Map/render_map.py').read_text(encoding='utf-8');start=s.index("cam.data.type='PERSP'");end=s.index("cam.data.type='ORTHO'",start)
header=s[:s.index("scene.render.resolution_x=1600;scene.render.resolution_y=1200")]
Path('Tools/Map/render_scenery.py').write_text(header+s[start:end],encoding='utf-8')
