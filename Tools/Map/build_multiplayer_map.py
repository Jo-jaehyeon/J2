"""Author isolated multiplayer map assets in Blender via MCP. Does not touch existing scenes."""
import bpy, math, random, json, sys
sys.path.insert(0,"C:/Jerry/UnityProject/J2/Tools/Map")
from natural_trees import TREE_PALETTE, add_tree
import importlib, lake_landscape
importlib.reload(lake_landscape)
from lake_landscape import ENV_PALETTE, build_environment
from pathlib import Path
from mathutils import Vector
OUT=Path('C:/Jerry/UnityProject/J2/Assets/Resources/Map')
SRC=OUT/'Source~'; SRC.mkdir(parents=True,exist_ok=True)
random.seed(82)
original=bpy.context.window.scene
scene=bpy.data.scenes.new('J2_Multiplayer_AssetWorkshop')
bpy.context.window.scene=scene
palette={'Sand': 'D7C494','WetSand':'A39B79','Grass':'A3B88C','GrassLight':'B5C397','EnemyGrass':'B8BB94','EnemyGrassLight':'C8CCA6','Bench':'63988E','EnemyBench':'A18C8C','Rock':'829991','Bark':'6E7651','Leaf':'478D70','LeafLight':'6AA17A','LeafDark':'397762','Steel':'617C80','Rail':'ADC1BD','Sleeper':'766852','Ballast':'939580','Cream':'E8E3BD','TramGreen':'398968','Glass':'7CB8CA','Dark':'344D53','Light':'FFE5A2','Water':'53B9CD','Foam':'B0E0D8','Mountain':'78A68F'}
palette.update(TREE_PALETTE)
palette.update(ENV_PALETTE)
mats={}
for n,h in palette.items():
    c=tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
    m=bpy.data.materials.new('MP_'+n);m.diffuse_color=(*c,1);m.use_nodes=True;m.node_tree.nodes.clear()
    bs=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled'); output=m.node_tree.nodes.new('ShaderNodeOutputMaterial'); m.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface']);bs.inputs['Base Color'].default_value=(*(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c),1);bs.inputs['Roughness'].default_value=.8
    mats[n]=m

def root(n):
    o=bpy.data.objects.new(n,None);scene.collection.objects.link(o);return o
stage=root('Battlefield_Geometry');tram=root('Tram_Geometry');rail=root('Railway_Geometry')
parent=stage

def finish(o,n,p,s,mat):
    o.name=n;o.location=p;o.scale=s;o.parent=parent;o.data.materials.append(mats[mat]);return o

def cube(n,p,s,mat,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o=finish(bpy.context.object,n,p,s,mat)
    if bevel:
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        m=o.modifiers.new('Soft crafted edges','BEVEL');m.width=bevel;m.segments=2
        bpy.ops.object.modifier_apply(modifier=m.name)
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o

def oval(n,p,s,mat,sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1)
    return finish(bpy.context.object,n,p,s,mat)

def beam(n,a,b,r,mat):
    a,b=Vector(a),Vector(b);d=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=d.length)
    o=finish(bpy.context.object,n,(a+b)/2,(1,1,1),mat);o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return o

def island(n,rings,mat):
    N=64;vs=[]
    for rx,ry,z in rings:
        for i in range(N):
            a=i*math.tau/N; wob=1+.018*math.sin(5*a)+.013*math.cos(9*a)
            vs.append((rx*math.cos(a)*wob,ry*math.sin(a)*wob,z))
    fs=[tuple(range(N-1,-1,-1))]
    for j in range(len(rings)-1):
        for i in range(N):
            a=j*N+i;b=j*N+(i+1)%N;fs.append((a,b,b+N,a+N))
    fs.append(tuple(range((len(rings)-1)*N,len(rings)*N)))
    me=bpy.data.meshes.new(n);me.from_pydata(vs,[],fs);me.materials.append(mats[mat]);o=bpy.data.objects.new(n,me);scene.collection.objects.link(o);o.parent=parent

island('Shoreline',[(12.3,14,-.85),(12.8,14.5,-.55),(12.2,14,-.3)],'WetSand')
island('Sandy island',[(12.2,14,-.45),(11.7,13.5,-.1),(10.8,12.6,0)],'Sand')
for row in range(9):
    if row==4:continue
    for col in range(8):
        mat=('Grass' if (row+col)%2==0 else 'GrassLight') if row<4 else ('EnemyGrass' if (row+col)%2==0 else 'EnemyGrassLight')
        cube('Tile_%d_%d'%(col,row),((col-3.5)*1.7,(row-4)*1.7,.035),(1.62,1.62,.21),mat,.035)
for side in [-1,1]:
    for i in range(10):
        x=(i-4.5)*1.35;y=side*9
        cube('Bench', (x,y,.08),(1.28,1.28,.24),'Bench' if side<0 else 'EnemyBench',.045)
        cube('Bench inset',(x,y,.207),(.88,.88,.02),'Grass' if side<0 else 'EnemyGrass',.025)
# Shared railway is authored separately after station routing is resolved.

def tree(x,y,h):
    add_tree(scene,stage,mats,x,y,h+1,int((x+100)*1000+y*10))
for side in [-1,1]:
    for i,y in enumerate([-8,-4,4,8]):
        tree(side*(9.3+(.4 if i%2 else 0)),y,2.7+i*.15)
        oval('Shore rock',(side*8.4,y+.8,.25),(.55,.42,.35),'Rock')
for x in [-7,-4,0,4,7]:tree(x,11.5,3.2+random.random()*.7)
# Submerged tower legs and cross braces, deliberately outside the playable cells.
for side in [-1,1]:
    x=side*14.6;y=4.3;h=5
    def tp(u,v,z):return (x+u+side*z*.12,y+v,z-.75)
    for u in [-.65,.65]:
        for v in [-.45,.45]:beam('Tower leg',tp(u,v,0),tp(u*.4,v*.4,h),.065,'Steel')
    for z in [0,1,2,3]:
        w=.65*(1-z*.12);wn=.65*(1-(z+1)*.12)
        for v in [-.4,.4]:
            beam('Tower brace',tp(-w,v,z),tp(wn,v,z+1),.035,'Steel')
            beam('Tower brace',tp(w,v,z),tp(-wn,v,z+1),.035,'Steel')
    for z in [3.4,4.5]:
        beam('Tower cross arm',tp(-1.4,0,z),tp(1.4,0,z),.07,'Steel')
        for u in [-1.2,1.2]:
            for k in range(3):oval('Ceramic insulator',tp(u,0,z-.15-k*.13),(.12,.12,.055),'Cream',1)
    for i in range(4):cube('Water glint',(x+(i-1.5)*.8,y-1.7+i*.5,-.69),(1.3,.05,.012),'Foam')
# Independent movable tram, origin at rail center and ground level.
parent=tram
cube('Cream cabin',(0,0,.95),(4.9,1.32,1.3),'Cream',.10)
cube('Green skirt',(0,0,.48),(5,1.37,.38),'TramGreen',.05)
cube('Roof',(0,0,1.68),(5.15,1.49,.22),'Steel',.07)
for side in [-1,1]:
    cube('Green waist',(0,side*.68,.78),(4.95,.04,.09),'TramGreen')
    for i in range(7):cube('Window',(-1.95+i*.65,side*.669,1.24),(.48,.025,.56),'Glass',.035)
    for x in [-1.65,1.65]:
        o=beam('Wheel',(x,side*.45,.26),(x,side*.65,.26),.24,'Dark')
    cube('End glass',(side*2.46,0,1.21),(.035,.95,.55),'Glass',.02)
    for y in [-.43,.43]:oval('Headlight',(side*2.5,y,.69),(.05,.1,.1),'Light')
    beam('Pantograph',(side*.48,0,1.78),(0,0,2.15),.035,'Steel')
cube('Pantograph contact',(0,0,2.18),(.8,.36,.055),'Steel')
# Continuous enclosing shore, distant mountains and two waterfall cascades.
lake=build_environment(scene,mats)
# Shared square loop crosses arena centers. Side arenas rotate ninety degrees.
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
(SRC/'rail-route.json').write_text(json.dumps({'points':[[round(v.x,5),.325,round(v.y,5)] for v in path],'closed':True,'trainCount':1,'routing':'square through all eight battlefields; arenas 4 and 5 rotate 90 degrees','extent':40,'cornerRadius':radius,'cornerCellHalfOffset':3.25},indent=2))
# Consolidate static geometry by material to keep eight instances economical.
for rt in [stage,tram,lake,rail]:
    for mat in mats.values():
        obs=[o for o in scene.objects if o.type=='MESH' and o.parent==rt and o.data.materials[0]==mat]
        if not obs:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in obs:o.select_set(True)
        bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
        o=bpy.context.object;o.name=rt.name+'_'+mat.name
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
# Corner variant retains 64 cells and 20 bench slots, adding room for the perpendicular rail leg.
corner=root('CornerBattlefield_Geometry')
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
for rt,filename in [(stage,'Battlefield'),(corner,'CornerBattlefield'),(tram,'LakeTram'),(lake,'LakeEnvironment'),(rail,'SquareRailway')]:
    bpy.ops.object.select_all(action='DESELECT')
    rt.select_set(True)
    for o in rt.children:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(filename+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
# Separate preview scene with linked geometry and no changes to the original Blender scene.
preview=bpy.data.scenes.new('J2_Multiplayer_Overview');bpy.context.window.scene=preview
positions=[(-40,40),(0,40),(40,40),(-40,0),(40,0),(-40,-40),(0,-40),(40,-40)]
for idx,(x,y) in enumerate(positions):
    for rt in [corner if idx in [0,2,5,7] else stage]:
        for ob in rt.children:
            o=ob.copy();o.data=ob.data;o.parent=None;preview.collection.objects.link(o);o.location=ob.matrix_world.translation+Vector((x,y,0));o.rotation_euler.z=-math.pi/2 if idx in [3,4] else 0
for ob in tram.children:
    o=ob.copy();o.data=ob.data;o.parent=None;preview.collection.objects.link(o);o.location=ob.matrix_world.translation+Vector((0,-40,.325))
for ob in list(lake.children)+list(rail.children):
    o=ob.copy();o.parent=None;preview.collection.objects.link(o);o.matrix_world=ob.matrix_world.copy()
world=bpy.data.worlds.new('Lake daylight');world.use_nodes=True;world.node_tree.nodes.clear();bg=world.node_tree.nodes.new('ShaderNodeBackground'); wo=world.node_tree.nodes.new('ShaderNodeOutputWorld');world.node_tree.links.new(bg.outputs[0],wo.inputs['Surface']);bg.inputs[0].default_value=(.55,.73,.8,1);bg.inputs[1].default_value=.7;preview.world=world
ld=bpy.data.lights.new('Sun','SUN');ld.energy=2;lo=bpy.data.objects.new('Sun',ld);preview.collection.objects.link(lo);lo.rotation_euler=(.45,-.5,-.5)
cd=bpy.data.cameras.new('Overview');cam=bpy.data.objects.new('Overview',cd);preview.collection.objects.link(cam);cam.location=(80,-125,160);cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=188;preview.camera=cam
preview.render.engine='CYCLES';preview.cycles.samples=24;preview.cycles.use_denoising=True;preview.render.resolution_x=1400;preview.render.resolution_y=1400;preview.render.resolution_percentage=100
preview.view_settings.view_transform='AgX'
bpy.data.libraries.write(str(SRC/'DragonEyeMultiplayerLake.blend'),{scene,preview},fake_user=True)
(OUT/'Source~'/'palette.json').write_text(json.dumps(palette,indent=2))
report={'battlefields':8,'grid':'3x3 center empty','spacing':40,'tilesPerSide':32,'benchesPerSide':10,'cellSpacing':1.7,'positions':positions,'meshObjects':{r.name:len(r.children) for r in [stage,corner,tram,lake,rail]},'triangles':{r.name:sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in r.children) for r in [stage,corner,tram,lake,rail]}}
(SRC/'geometry-report.json').write_text(json.dumps(report,indent=2))
bpy.context.window.scene=original
print(json.dumps(report))
