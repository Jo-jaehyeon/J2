from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8')
a=s.index('corner=root(');b=s.index('for rt,filename in',a)
s=s[:a]+'''corner=root('CornerBattlefield_Geometry')
tree_materials={'MP_Bark','MP_BarkLight','MP_BarkDark','MP_LeafSun','MP_LeafFresh','MP_LeafDeep','MP_LeafMid'}
for original_mesh in stage.children:
    name=original_mesh.data.materials[0].name.split('.')[0]
    if name in tree_materials:continue
    o=original_mesh.copy();o.data=original_mesh.data.copy();scene.collection.objects.link(o);o.parent=corner;o.name=original_mesh.name.replace('Battlefield','CornerBattlefield')
    if name in ['MP_Grass','MP_GrassLight','MP_EnemyGrass','MP_EnemyGrassLight','MP_Bench','MP_EnemyBench']:
        for v in o.data.vertices:v.co.x+=3.25 if v.co.x>0 else -3.25
    if name in ['MP_Sand','MP_WetSand']:
        for v in o.data.vertices:v.co.x*=1.3;v.co.y*=1.1
    if name=='MP_Rock':
        for v in o.data.vertices:v.co.x+=3.25 if v.co.x>0 else -3.25
# Regenerate complete trees at their corrected positions; never distort joined tree vertices.
for side in [-1,1]:
    for i,y in enumerate([-8,-4,4,8]):
        x=side*(12.55+(.4 if i%2 else 0));add_tree(scene,corner,mats,x,y,3.7+i*.15,int((x+100)*1000+y*10))
for x in [-10.25,-7.25,2.2,7.25,10.25]:add_tree(scene,corner,mats,x,11.5,4.4,int((x+100)*1000+115))
for mat in mats.values():
    obs=[o for o in corner.children if o.type=='MESH' and o.data.materials[0]==mat]
    if len(obs)<2:continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs:o.select_set(True)
    bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
''' +s[b:]
p.write_text(s,encoding='utf-8')
