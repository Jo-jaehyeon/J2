"""WarGreymon, modeled from the user's front/side/back references via Blender MCP.
Rigid weighted low-poly armor rig. Blender: Z up, -Y front. Unity FBX: Y up.
Run inside Blender, never resets or deletes the user's existing scene.
"""
import bpy
import bmesh
import math
import json
from mathutils import Vector
from pathlib import Path

BASE = Path(r'C:/Jerry/Unity Project/J2/Assets/Resources/Digimon/워그레이몬')
OUT = Path(r'C:/Jerry/Unity Project/J2/Logs/WarGreymon')
BASE.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('J2_WarGreymon')
bpy.context.window.scene = scene
scene.render.fps = 30
scene.frame_start, scene.frame_end = 1, 150
parts = []

def material(name, rgb, metal=0.0):
    m = bpy.data.materials.get('WG_' + name) or bpy.data.materials.new('WG_' + name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    shader = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if shader is None:
        shader=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        output=next((n for n in m.node_tree.nodes if n.type == 'OUTPUT_MATERIAL'), None) or m.node_tree.nodes.new('ShaderNodeOutputMaterial')
        m.node_tree.links.new(shader.outputs['BSDF'],output.inputs['Surface'])
    shader.inputs['Base Color'].default_value = (*rgb, 1)
    shader.inputs['Metallic'].default_value = metal
    shader.inputs['Roughness'].default_value = .4 if metal else .66
    return m

gold = material('Gold', (.93,.54,.055), .55)
gold_light = material('GoldEdge', (1,.76,.20), .5)
gold_dark = material('GoldInset', (.49,.235,.022), .45)
silver = material('Silver', (.62,.69,.76), .65)
silver_light = material('SilverEdge', (.86,.90,.92), .55)
silver_dark = material('SilverInset', (.27,.32,.38), .6)
skin = material('OrangeSkin', (.89,.255,.032))
skin_light = material('Muscle', (1,.39,.055))
red = material('RedBindings', (.48,.025,.035))
red_light = material('RedMane', (.72,.055,.035))
dark = material('Undersuit', (.052,.045,.09))
white = material('ShieldCrest', (.94,.93,.85), .15)
green = material('EmeraldEyes', (.045,.7,.13), .2)

def finish(obj, name, mat, bone):
    obj.name = name
    if mat: obj.data.materials.append(mat)
    if bone:
        obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
        parts.append(obj)
    return obj

def mesh(name, vertices, faces, mat, bone):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    return finish(ob, name, mat, bone)

def ellipsoid(name, loc, scale, mat, bone, seg=10, rings=6, rotation=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, location=loc)
    ob = bpy.context.object
    ob.scale = scale
    if rotation: ob.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(ob, name, mat, bone)

def tube(name, points, radii, mat, bone, sides=6, flatten=1):
    verts, faces = [], []
    for i,p in enumerate(points):
        p = Vector(p)
        direction = Vector(points[min(i+1,len(points)-1)]) - Vector(points[max(0,i-1)])
        direction.normalize()
        u = direction.cross(Vector((0,1,0)))
        if u.length < .01: u = direction.cross(Vector((1,0,0)))
        u.normalize(); v = direction.cross(u).normalized()
        for j in range(sides):
            a = 2*math.pi*j/sides
            verts.append(p + radii[i]*(u*math.cos(a)+v*math.sin(a)*flatten))
    for i in range(len(points)-1):
        for j in range(sides):
            a=i*sides+j; b=i*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces += [tuple(reversed(range(sides))), tuple((len(points)-1)*sides+j for j in range(sides))]
    return mesh(name, verts, faces, mat, bone)

def plate(name, outline, y, depth, mat, bone, bevel=.02):
    # Outlines are x,z. A bevel produces a broad faceted metal edge.
    n=len(outline)
    vs=[(x,y,z) for x,z in outline]+[(x,y+depth,z) for x,z in outline]
    fs=[tuple(reversed(range(n))), tuple(n+i for i in range(n))]
    fs.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    ob=mesh(name,vs,fs,mat,bone)
    if bevel:
        bpy.context.view_layer.objects.active=ob
        mod=ob.modifiers.new('Forged edge','BEVEL'); mod.width=bevel; mod.segments=1
        mod.affect='EDGES'
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob

# Skeleton. All meshes are weighted; the character exports as a single skinned mesh.
specs=[('Root',(0,0,0),(0,0,.25),None),
       ('Hips',(0,0,1.2),(0,0,1.53),'Root'),
       ('Chest',(0,0,1.53),(0,0,2.14),'Hips'),
       ('Head',(0,0,2.14),(0,0,2.65),'Chest')]
for s,side in [(-1,'L'),(1,'R')]:
    specs += [(f'UpperArm.{side}',(s*.46,0,2.06),(s*.66,-.015,1.69),'Chest'),
              (f'Forearm.{side}',(s*.66,-.015,1.69),(s*.83,-.08,1.16),f'UpperArm.{side}'),
              (f'Thigh.{side}',(s*.235,0,1.25),(s*.32,-.025,.72),'Hips'),
              (f'Shin.{side}',(s*.32,-.025,.72),(s*.36,0,.23),f'Thigh.{side}'),
              (f'Foot.{side}',(s*.36,0,.23),(s*.36,-.33,.1),f'Shin.{side}'),
              (f'Shield.{side}',(s*.18,.2,2.04),(s*.56,.28,1.48),'Chest')]
arm=bpy.data.armatures.new('WarGreymon_Rig')
rig=bpy.data.objects.new('WarGreymon_Rig',arm);scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for name,head,tail,parent in specs:
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent: b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
rig.show_in_front=True

# Torso: orange frame, tapered silver cuirass, separate overlapping abdominal plates.
ellipsoid('TorsoUnderArmor',(0,.025,1.82),(.40,.235,.43),skin,'Chest',12,7)
plate('PectoralArmor',[(-.39,2.08),(-.23,2.17),(.23,2.17),(.39,2.08),(.34,1.86),(.18,1.8),(-.18,1.8),(-.34,1.86)],-.237,.14,silver,'Chest',.034)
for s in [-1,1]:
    plate('PectoralFacet',[(s*.045,2.125),(s*.25,2.115),(s*.345,2.035),(s*.28,1.895),(s*.05,1.875)],-.288,.021,silver_light,'Chest',.012)
for i,(z,w) in enumerate([(1.75,.265),(1.535,.23)]):
    plate('Abdomen_'+str(i),[(-w,z+.11),(w,z+.11),(w+.014,z-.07),(w-.035,z-.13),(-w+.035,z-.13),(-w-.014,z-.07)],-.222,.14,silver,'Chest' if i==0 else 'Hips',.025)
    for s in [-1,1]:
        plate('AbdominalRim',[(s*(w-.045),z+.085),(s*w,z+.055),(s*w,z-.07),(s*(w-.045),z-.10)],-.245,.012,silver_dark,'Chest' if i==0 else 'Hips',.005)
for z,w,bone in [(2.015,.082,'Chest'),(1.76,.055,'Chest'),(1.545,.047,'Hips')]:
    for s in [-1,1]:
        tube('ChestRedX',[(-w,-.315,z+s*w),(0,-.333,z),(w,-.315,z-s*w)],[.028,.032,.028],red,bone,7)
for s in [-1,1]:
    for dz in [0,.065]:
        tube('ShoulderBindings',[(s*.3,-.23,2.065-dz),(s*.38,-.27,2.075-dz),(s*.48,-.2,2.11-dz)],[.026]*3,red,'Chest',7)
ellipsoid('Pelvis',(0,0,1.25),(.29,.2,.20),dark,'Hips')
plate('Belt',[(-.30,1.42),(.30,1.42),(.27,1.29),(.10,1.28),(.07,1.22),(-.07,1.22),(-.10,1.28),(-.27,1.29)],-.245,.17,dark,'Hips',.015)
plate('Codpiece',[(-.125,1.3),(.125,1.3),(.11,1.05),(.075,.84),(-.075,.84),(-.11,1.05)],-.235,.10,silver,'Hips',.018)
plate('CodpieceRed',[(-.10,1.105),(.10,1.105),(.078,.94),(-.078,.94)],-.255,.014,red,'Hips',.005)

# Neck and dragon helmet: long angular muzzle, inset eyes, three horns, red mane.
ellipsoid('Neck',(0,0,2.22),(.13,.135,.20),dark,'Head',8,5)
for z in [2.19,2.26]:
    tube('NeckLames',[(-.105,-.08,z),(0,-.151,z-.025),(.105,-.08,z)],[.018]*3,silver_dark,'Head',5)
ellipsoid('HelmetSkull',(0,.006,2.49),(.205,.19,.255),silver,'Head',10,7)
plate('HelmetBrow',[(-.21,2.58),(-.11,2.69),(0,2.75),(.11,2.69),(.21,2.58),(.10,2.48),(0,2.51),(-.10,2.48)],-.162,.1,silver_light,'Head',.014)
mesh('DragonMuzzle',[(-.16,-.17,2.50),(.16,-.17,2.50),(-.095,-.3,2.41),(.095,-.3,2.41),(0,-.35,2.34),(0,-.16,2.33),(-.145,-.13,2.32),(.145,-.13,2.32)],[(0,1,3,2),(2,3,4),(0,2,4,6),(1,7,4,3),(6,4,5),(7,5,4),(0,6,5,7,1)],silver,'Head')
for s in [-1,1]:
    plate('EyeSocket',[(s*.07,2.52),(s*.178,2.555),(s*.16,2.485),(s*.087,2.47)],-.204,.018,dark,'Head',.002)
    plate('EmeraldEye',[(s*.095,2.517),(s*.158,2.533),(s*.145,2.498),(s*.104,2.491)],-.226,.008,green,'Head',.001)
    plate('EyeGlint',[(s*.111,2.516),(s*.12,2.519),(s*.12,2.499),(s*.111,2.499)],-.236,.003,white,'Head',0)
    ellipsoid('TempleHinge',(s*.188,.018,2.48),(.035,.087,.12),silver_dark,'Head',8,5)
    tube('SweptHelmetHorn',[(s*.15,.02,2.62),(s*.29,.018,2.68),(s*.345,.005,2.80),(s*.355,-.013,2.99)],[.074,.061,.039,.001],silver,'Head',6)
    for k in range(2):
        x=.215+k*.04
        tube('HornBand',[(s*x,.018,2.643+k*.02),(s*(x+.012),.018,2.649+k*.021)],[.072-k*.004]*2,silver_dark,'Head',6)
    for i in range(6):
        tube('ManeBlade',[(s*.10,.09,2.59-i*.04),(s*(.18+.009*i),.14+i*.02,2.53-i*.045),(s*(.21+.008*i),.23+i*.025,2.38-i*.03)],[.052,.055,.001],red_light if i%2 else red,'Head',4, .5)
    for i in range(3):
        tube('SideMane',[(s*.145,.02,2.45-i*.035),(s*.185,-.01,2.31-i*.07),(s*.19,-.035,2.20-i*.035)],[.037,.03,.001],red_light,'Head',4,.5)
tube('CentralBlade',[(0,-.1,2.59),(0,-.16,2.72),(0,-.15,2.99)],[.072,.047,.001],silver_light,'Head',4,.48)

for s,side in [(-1,'L'),(1,'R')]:
    upper,fore,thigh,shin,foot,shield=[f'{x}.{side}' for x in ['UpperArm','Forearm','Thigh','Shin','Foot','Shield']]
    # Orange upper arm and oversized gold shoulder shell.
    ellipsoid('Biceps',(s*.56,-.015,1.89),(.14,.145,.265),skin,upper,10,7, (0,s*-.36,0))
    ellipsoid('BicepsFacet',(s*.57,-.112,1.87),(.096,.058,.18),skin_light,upper,8,5,(0,s*-.36,0))
    ellipsoid('ShoulderShell',(s*.465,.0,2.09),(.258,.255,.27),gold,upper,10,7)
    tube('ShoulderRim',[(s*.28,-.13,2.0),(s*.39,-.239,1.90),(s*.56,-.218,1.875),(s*.68,-.11,1.99)],[.034]*4,gold_light,upper,6)
    tube('ShoulderSpike',[(s*.57,.045,2.25),(s*.68,.055,2.43),(s*.83,.065,2.67)],[.108,.07,.001],gold,upper,6)
    tube('SpikeRedBand',[(s*.675,.054,2.419),(s*.717,.058,2.487)],[.08,.056],red,upper,6)
    ellipsoid('ElbowJoint',(s*.66,0,1.69),(.135,.14,.13),dark,fore)
    ellipsoid('ForearmCore',(s*.745,-.02,1.43),(.14,.16,.28),skin,fore)
    # Wedge bracers, layered bevels, three independent long silver claws.
    outline=[(s*.57,1.67),(s*.61,1.87),(s*.87,1.65),(s*1.0,1.17),(s*.70,1.13)]
    plate('DramonGauntlet',outline,-.245,.32,gold,fore,.026)
    plate('GauntletFacet',[(s*.655,1.74),(s*.8,1.61),(s*.925,1.21),(s*.76,1.19)],-.283,.01,gold_light,fore,.01)
    for i in range(3):
        x=s*(.735+i*.098)
        tube('ClawHousing',[(x,-.19,1.43),(x,-.238,1.24),(x+s*.02,-.23,1.14)],[.043,.047,.042],gold,fore,5,.62)
        tube('DramonClaw',[(x+s*.02,-.23,1.16),(x+s*.057,-.28,.97),(x+s*.074,-.32,.74),(x+s*.035,-.39,.64)],[.044,.038,.023,.001],silver_light,fore,4,.72)
    # Thighs, separated armor and red calf lashings.
    ellipsoid('ThighMuscle',(s*.275,.005,1.025),(.177,.171,.30),skin,thigh,10,7,(0,s*.15,0))
    ellipsoid('Quadriceps',(s*.255,-.125,1.04),(.12,.071,.235),skin_light,thigh,8,6,(0,s*.15,0))
    plate('HipGoldRail',[(s*.27,1.34),(s*.34,1.37),(s*.50,.98),(s*.44,.94)],-.08,.13,gold,thigh,.02)
    ellipsoid('Calf',(s*.34,.023,.50),(.126,.136,.245),skin,shin,10,6)
    plate('Greave',[(s*.21,.71),(s*.43,.71),(s*.48,.27),(s*.22,.24)],-.164,.13,silver,shin,.018)
    plate('GreaveRed',[(s*.265,.62),(s*.395,.62),(s*.429,.32),(s*.282,.31)],-.189,.014,red,shin,.006)
    plate('KneeShield',[(s*.17,.76),(s*.25,.84),(s*.40,.76),(s*.48,.82),(s*.455,.62),(s*.32,.565),(s*.185,.63)],-.235,.17,silver_light,shin,.02)
    for z in [.67,.615,.345,.295]:
        for half in [0,1]:
            pts=[]
            for j in range(9):
                a=(j/8*math.pi)+(math.pi if half else 0)
                pts.append((s*.34+.146*math.cos(a),.025+.155*math.sin(a),z))
            tube('CalfRedBinding',pts,[.024]*9,red,shin,6)
    ellipsoid('BroadDinosaurFoot',(s*.36,-.125,.145),(.195,.285,.14),skin,foot,10,6)
    for i in range(3):
        x=s*.36+(i-1)*.123
        tube('ToeClaw',[(x,-.29,.16),(x,-.40,.095),(x+(i-1)*.025,-.53,.045)],[.073,.05,.001],silver_light,foot,5,.65)

    # Brave Shield: two broad bevelled gold halves, backs facing +Y.
    outline=[(s*.08,2.25),(s*.76,2.24),(s*.95,1.11),(s*.36,.78)]
    plate('BraveShield',outline,.275,.12,gold,shield,.028)
    # Inner polygon plus raised perimeter rails.
    inner=[(s*.145,2.16),(s*.696,2.15),(s*.85,1.155),(s*.39,.91)]
    plate('ShieldInset',inner,.401,.009,gold_dark,shield,.008)
    inner2=[(s*.16,2.145),(s*.681,2.137),(s*.826,1.166),(s*.401,.948)]
    plate('ShieldFace',inner2,.414,.009,gold,shield,.006)
    for j in range(4):
        p1,p2=outline[j],outline[(j+1)%4]
        tube('ShieldBrightEdge',[(p1[0],.413,p1[1]),(p2[0],.413,p2[1])],[.021]*2,gold_light,shield,4)
    # Reference crest: cropped concentric rings, surrounded by eight hollow rays.
    def inside(x,z):
        poly=inner2
        signs=[]
        for j in range(4):
            a,b=poly[j],poly[(j+1)%4]
            signs.append((b[0]-a[0])*(z-a[1])-(b[1]-a[1])*(x-a[0]))
        return all(v>=0 for v in signs) or all(v<=0 for v in signs)
    cx,cz=s*.19,1.57
    for radius,width in [(.36,.038),(.235,.03),(.112,.07)]:
        for j in range(96):
            a,b=2*math.pi*j/96,2*math.pi*(j+1)/96
            coords=[(cx+r*math.cos(t),cz+r*math.sin(t)) for r,t in [(radius-width/2,a),(radius+width/2,a),(radius+width/2,b),(radius-width/2,b)]]
            if all(inside(x,z) for x,z in coords):
                plate('CrestRing',coords,.43,.005,white,shield,0)
    for j in range(8):
        a=math.pi/2+j*math.pi/4
        radial=Vector((math.cos(a),math.sin(a))); tangent=Vector((-math.sin(a),math.cos(a)))
        center=Vector((cx,cz))+radial*.505
        tri=[center+radial*.105,center-radial*.075+tangent*.09,center-radial*.075-tangent*.09]
        if all(inside(p.x,p.y) for p in tri):
            for k in range(3):
                p,q=tri[k],tri[(k+1)%3]
                tube('CrestRay',[(p.x,.438,p.y),(q.x,.438,q.y)],[.012,.012],white,shield,4)

# Join all weighted geometry; one renderer, shared low-poly material palette.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts: ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
body=bpy.context.object;body.name='WarGreymon_Mesh'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
body.parent=rig
mod=body.modifiers.new('ArmorSkeleton','ARMATURE');mod.object=rig
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(body.data);bm.free()
for p in body.data.polygons: p.use_smooth=False

# Animate one deterministic timeline, exported and split into four Unity clips.
# Idle 1-49, Walk 60-84, Attack 90-108, Death 120-150.
for pb in rig.pose.bones: pb.rotation_mode='XYZ'
def pose(frame, rotations=None, root=(0,0,0)):
    for pb in rig.pose.bones:
        pb.rotation_euler=(0,0,0);pb.location=(0,0,0)
    rig.pose.bones['Root'].location=root
    for name,degrees in (rotations or {}).items():
        rig.pose.bones[name].rotation_euler=[math.radians(v) for v in degrees]
    for pb in rig.pose.bones:
        pb.keyframe_insert('rotation_euler',frame=frame,group=pb.name)
        pb.keyframe_insert('location',frame=frame,group=pb.name)
for f,t in [(1,0),(13,1),(25,0),(37,-1),(49,0)]:
    pose(f,{'Chest':(t*1.5,0,0),'Head':(-t,0,t),'UpperArm.L':(0,0,-t*1.7),'UpperArm.R':(0,0,t*1.7),'Shield.L':(0,t,0),'Shield.R':(0,-t,0)},(0,t*.012,0))
for f,t in [(60,0),(66,1),(72,0),(78,-1),(84,0)]:
    pose(f,{'Thigh.L':(t*24,0,0),'Thigh.R':(-t*24,0,0),'Shin.L':(max(0,-t)*24,0,0),'Shin.R':(max(0,t)*24,0,0),'Foot.L':(-t*9,0,0),'Foot.R':(t*9,0,0),'UpperArm.L':(-t*15,0,0),'UpperArm.R':(t*15,0,0),'Chest':(3,t*3,0),'Head':(-3,-t*2,0)},(0,abs(t)*.018,0))
pose(90)
pose(95,{'Chest':(-8,-14,0),'Head':(4,8,0),'UpperArm.R':(-65,-20,30),'Forearm.R':(-35,0,0),'UpperArm.L':(-15,0,-12),'Shield.R':(0,8,0)})
pose(100,{'Chest':(18,20,0),'Head':(-8,-10,0),'UpperArm.R':(36,12,-25),'Forearm.R':(-20,0,0),'UpperArm.L':(-30,0,-15),'Thigh.L':(-10,0,0),'Shin.L':(12,0,0)},(0,-.03,-.1))
pose(104,{'Chest':(8,8,0),'UpperArm.R':(15,0,-10),'Forearm.R':(-10,0,0)})
pose(108)
pose(120)
pose(128,{'Chest':(18,0,8),'Head':(20,0,0),'Thigh.L':(-25,0,0),'Thigh.R':(-25,0,0),'Shin.L':(50,0,0),'Shin.R':(50,0,0),'UpperArm.L':(15,0,15),'UpperArm.R':(15,0,-15)},(0,-.25,0))
pose(140,{'Root':(78,0,10),'Chest':(12,0,0),'Head':(12,0,0),'UpperArm.L':(-20,0,30),'UpperArm.R':(-20,0,-30),'Thigh.L':(-15,0,0),'Thigh.R':(-10,0,0),'Shin.L':(25,0,0),'Shin.R':(20,0,0)},(0,.15,0))
pose(150,{'Root':(82,0,10),'Chest':(12,0,0),'Head':(12,0,0),'UpperArm.L':(-20,0,30),'UpperArm.R':(-20,0,-30),'Thigh.L':(-15,0,0),'Thigh.R':(-10,0,0),'Shin.L':(25,0,0),'Shin.R':(20,0,0)},(0,.15,0))
rig.animation_data.action.name='WarGreymon_AllClips'
for name,start,end in [('Idle',1,49),('Walk',60,84),('Attack',90,108),('Death',120,150)]:
    scene.timeline_markers.new(name,frame=start)
scene.frame_set(1)

# Export only the requested character, never any user's other objects.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(BASE/'WarGreymon_Model.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},
    axis_forward='-Z',axis_up='Y',global_scale=1.0,apply_unit_scale=True,apply_scale_options="FBX_SCALE_ALL",add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
    mesh_smooth_type='FACE',use_mesh_modifiers=True)

# Inspection studio is in a separate collection, omitted from the FBX.
studio=bpy.data.collections.new('PreviewStudio');scene.collection.children.link(studio)
def studio_link(ob):
    for c in list(ob.users_collection): c.objects.unlink(ob)
    studio.objects.link(ob)
floor=material('StudioFloor',(.035,.05,.073))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));stage=bpy.context.object;stage.name='PreviewFloor';stage.data.materials.append(floor);studio_link(stage)
def look(ob,target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
for name,loc,power,size in [('Key',(-3,-4,6),650,4),('Fill',(4,-2,3),430,3),('Rim',(0,4,5),950,3)]:
    d=bpy.data.lights.new(name,'AREA');ob=bpy.data.objects.new(name,d);studio.objects.link(ob);ob.location=loc;d.energy=power;d.shape='DISK';d.size=size;look(ob,(0,0,1.4))
camdata=bpy.data.cameras.new('PreviewCamera');cam=bpy.data.objects.new('PreviewCamera',camdata);studio.objects.link(cam);scene.camera=cam
camdata.type='ORTHO';camdata.ortho_scale=3.6
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.render.resolution_x=850;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('WarGreymonStudio');scene.world.use_nodes=True;next(n for n in scene.world.node_tree.nodes if n.type == 'BACKGROUND').inputs[0].default_value=(.12,.15,.21,1);next(n for n in scene.world.node_tree.nodes if n.type == 'BACKGROUND').inputs[1].default_value=.4
scene.view_settings.view_transform='AgX'
cam.location=(3.6,-6,3.0);look(cam,(0,0,1.45))
scene.render.filepath=str(OUT/'three-quarter.png')
# Keep the native source in a Unity-ignored folder to avoid duplicate .blend imports.
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'Source~'/'WarGreymon.blend'))
body.data.calc_loop_triangles()
report={'vertices':len(body.data.vertices),'triangles':len(body.data.loop_triangles),'bones':len(arm.bones),'materials':len(body.data.materials),'clips':{'Idle':[1,49],'Walk':[60,84],'Attack':[90,108],'Death':[120,150]},'fbx':str(BASE/'WarGreymon_Model.fbx')}
(BASE/'Source~'/'build-report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
result=report
