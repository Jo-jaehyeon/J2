from pathlib import Path
import shutil
root=Path('C:/Jerry/UnityProject/J2');src=root/'Assets/Resources/Map/Source~'
backup=src/'Iterations'/'BeforeThroughBattlefields';backup.mkdir(parents=True,exist_ok=True)
for name in ['rail-route.json','DragonEyeMultiplayerFinal.blend','UnityOverview.png','Overview.png','Battlefield.png']:
    if (src/name).exists() and not (backup/name).exists():shutil.copy2(src/name,backup/name)
p=root/'Tools/Map/build_multiplayer_map.py';s=p.read_text(encoding='utf-8')
a=s.index('# One shared square loop.');b=s.index('# Consolidate static geometry',a)
s=s[:a]+'''# Shared square loop crosses arena centers. Side arenas rotate ninety degrees.
parent=rail
route=[]
radius=.65
for cx,cy,start in [(40-radius,40-radius,0),(-40+radius,40-radius,90),(-40+radius,-40+radius,180),(40-radius,-40+radius,270)]:
    for i in range(13):
        angle=math.radians(start+i*90/12)
        route.append(Vector((cx+radius*math.cos(angle),cy+radius*math.sin(angle),.285)))
path=[]
for i,a in enumerate(route):
    b=route[(i+1)%len(route)];d=b-a;n=max(1,math.ceil(d.length/.65))
    for j in range(n):path.append(a+d*j/n)
# Batch authored rail boxes into three meshes; no boarding decks remain.
rail_batches={}
def rail_box(p,scale,angle,mat):
    vs,fs=rail_batches.setdefault(mat,([],[]));base=len(vs);ca,sa=math.cos(angle),math.sin(angle)
    for z in [-.5,.5]:
        for y in [-.5,.5]:
            for x in [-.5,.5]:
                dx,dy=x*scale[0],y*scale[1];vs.append((p.x+dx*ca-dy*sa,p.y+dx*sa+dy*ca,p.z+z*scale[2]))
    for face in [(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)]:fs.append(tuple(base+j for j in face))
for i,a in enumerate(path):
    b=path[(i+1)%len(path)];d=b-a;mid=(a+b)/2;angle=math.atan2(d.y,d.x);normal=Vector((-math.sin(angle),math.cos(angle),0))
    rail_box(mid+Vector((0,0,-.22)),(d.length+.025,1.55,.2),angle,'Ballast')
    rail_box(a+Vector((0,0,-.105)),(.19,1.32,.12),angle,'Sleeper')
    for side in [-1,1]:rail_box(mid+normal*side*.48,(d.length+.035,.085,.12),angle,'Rail')
for mat,(vs,fs) in rail_batches.items():
    me=bpy.data.meshes.new('ThroughRail_'+mat);me.from_pydata(vs,[],fs);me.materials.append(mats[mat]);o=bpy.data.objects.new('ThroughRail_'+mat,me);scene.collection.objects.link(o);o.parent=rail
(SRC/'rail-route.json').write_text(json.dumps({'points':[[round(v.x,5),.325,round(v.y,5)] for v in path],'closed':True,'trainCount':1,'routing':'square through all eight battlefields; arenas 4 and 5 rotate 90 degrees','extent':40,'cornerRadius':radius,'cornerCellHalfOffset':.85},indent=2))
''' +s[b:]
a=s.index('for rt,filename in');
s=s[:a]+'''# Corner variant retains 64 cells and 20 bench slots, adding room for the perpendicular rail leg.
corner=root('CornerBattlefield_Geometry')
for original_mesh in stage.children:
    o=original_mesh.copy();o.data=original_mesh.data.copy();scene.collection.objects.link(o);o.parent=corner;o.name=original_mesh.name.replace('Battlefield','CornerBattlefield')
    name=o.data.materials[0].name.split('.')[0]
    if name in ['MP_Grass','MP_GrassLight','MP_EnemyGrass','MP_EnemyGrassLight','MP_Bench','MP_EnemyBench']:
        for v in o.data.vertices:v.co.x+=.85 if v.co.x>0 else -.85
    # Move the rear-center tree off the new vertical railway opening.
    if name in ['MP_Bark','MP_Leaf','MP_LeafLight','MP_LeafDark']:
        for v in o.data.vertices:
            if abs(v.co.x)<1.6 and v.co.y>9.8:v.co.x+=2
''' +s[a:]
s=s.replace("(stage,'Battlefield'),","(stage,'Battlefield'),(corner,'CornerBattlefield'),")
s=s.replace('for rt in [stage]:', 'for rt in [corner if idx in [0,2,5,7] else stage]:')
s=s.replace("o.location=ob.matrix_world.translation+Vector((x+(4 if rt==tram else 0),y,0))", "o.location=ob.matrix_world.translation+Vector((x,y,0));o.rotation_euler.z=math.pi/2 if idx in [3,4] else 0")
s=s.replace('Vector((0,-24,0))','Vector((0,-40,.325))')
s=s.replace("SRC/'DragonEyeMultiplayerGenerated.blend'", "SRC/'DragonEyeMultiplayerThroughBattlefields.blend'")
s=s.replace('for r in [stage,tram,lake,rail]', 'for r in [stage,corner,tram,lake,rail]')
p.write_text(s,encoding='utf-8')
# Build a corner prefab with matching shifted gameplay anchors, then use it at slots 0/2/5/7.
p=root/'Tools/Map/build_prefabs.cs';s=p.read_text(encoding='utf-8-sig');mark='var root=new UnityEngine.GameObject("DragonEyeMultiplayer");'
s=s.replace(mark,'''var cornerArena=UnityEngine.Object.Instantiate(arenaPrefab);cornerArena.name="CornerBattlefield";
UnityEngine.Object.DestroyImmediate(cornerArena.transform.Find("Battlefield Visual").gameObject);
visual("CornerBattlefield",cornerArena.transform);
foreach(var groupName in new[]{"AllyCells","EnemyCells","AllyBench","EnemyBench"})foreach(UnityEngine.Transform t in cornerArena.transform.Find(groupName)) {
    var p=t.localPosition;p.x+=p.x>0?.85f:-.85f;t.localPosition=p;
}
var cornerPrefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(cornerArena,basePath+"/CornerBattlefield.prefab");UnityEngine.Object.DestroyImmediate(cornerArena);
'''+mark)
s=s.replace('InstantiatePrefab(arenaPrefab);a.name=', 'InstantiatePrefab((slot==0||slot==2||slot==5||slot==7)?cornerPrefab:arenaPrefab);a.name=')
s=s.replace('layout.Arenas[slot]=a.transform;', 'if(slot==3||slot==4)a.transform.localRotation=UnityEngine.Quaternion.Euler(0,90,0);\n    layout.Arenas[slot]=a.transform;')
p.write_text(s,encoding='utf-8')
