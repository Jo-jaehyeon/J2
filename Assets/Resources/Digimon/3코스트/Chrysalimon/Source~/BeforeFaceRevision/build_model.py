"""J2 reference-authored low-poly characters. Execute inside connected Blender.

No screenshots/renders or user-scene resets. Each native source contains its own
scene, weighted mesh and 30fps animation timeline. Invoke build_asset(EnglishName).
"""
import bpy
import bmesh
import math
import json
import hashlib
from pathlib import Path
from mathutils import Vector

PROJECT = Path(r'C:/Jerry/Unity Project/J2')
REFERENCES = Path(r'C:/Users/User/Desktop/J2_AssetImage')
RECIPES = {
    'Koromon': ('코로몬', '1코스트', 1.05, 1.60, 1.5, 'baby'),
    'Agumon': ('아구몬', '2코스트', 1.6, 1.45, 1.6, 'dinosaur'),
    'Greymon': ('그레이몬', '3코스트', 2.05, 1.65, 2.2, 'dinosaur'),
    'MetalGreymon': ('메탈그레이몬', '4코스트', 2.20, 2.0, 2.2, 'dinosaur'),
    'Tsumemon': ('츠메몬', '1코스트', 1.35, 1.55, 1.55, 'crawler'),
    'Keramon': ('케라몬', '2코스트', 1.85, 1.65, 1.6, 'floating'),
    'Chrysalimon': ('크리사리몬', '3코스트', 1.95, 1.95, 1.9, 'cocoon'),
    'Infermon': ('인펠몬', '4코스트', 1.55, 2.10, 2.15, 'spider'),
    'Diaboromon': ('디아블로몬', '5코스트', 2.20, 2.10, 2.0, 'demon'),
}
PALETTE = {
    'SkinGold': (.98,.54,.055), 'SkinOrange': (.92,.34,.035),
    'Belly': (1,.68,.16), 'Pink': (.96,.56,.73), 'PinkLight': (1,.76,.86),
    'PinkShadow': (.69,.29,.43), 'BlueSkin': (.39,.46,.70),
    'BlueLight': (.58,.63,.86), 'Purple': (.34,.24,.57),
    'PurpleLight': (.50,.38,.73), 'DarkArmor': (.12,.115,.23),
    'DarkArmorEdge': (.23,.22,.36), 'Tendon': (.32,.325,.20),
    'TendonLight': (.46,.46,.30), 'Black': (.018,.02,.03),
    'Mouth': (.25,.025,.045), 'Tongue': (.7,.11,.18), 'Ivory': (.91,.91,.76),
    'Green': (.11,.66,.10), 'Emerald': (.035,.39,.22), 'Ruby': (.67,.035,.08),
    'Yellow': (.98,.78,.075), 'Gold': (.83,.66,.17),
    'Silver': (.53,.59,.63), 'SilverLight': (.80,.84,.85),
    'SteelDark': (.22,.27,.31), 'RedMetal': (.48,.11,.10),
    'Rust': (.43,.15,.095), 'HairRed': (.84,.14,.045), 'Stripe': (.07,.28,.36),
    'ElectricStripe': (.09,.19,.79), 'HornBrown': (.18,.13,.105),
    'White': (.98,.98,.96), 'ShellWhite': (.81,.82,.88),
}

class Builder:
    def __init__(self, name):
        self.name=name
        self.scene=bpy.data.scenes.new('J2_'+name)
        self.scene['j2_asset']=name
        bpy.context.window.scene=self.scene
        self.parts=[];self.bones={};self.materials={}
        self.bone('Root',(0,0,0),None)

    def mat(self, key):
        if key not in self.materials:
            mat=bpy.data.materials.new(self.name+'_'+key)
            mat.diffuse_color=(*PALETTE[key],1);mat.use_nodes=True
            bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            bsdf.inputs['Base Color'].default_value=mat.diffuse_color
            bsdf.inputs['Roughness'].default_value=.6
            bsdf.inputs['Metallic'].default_value=.4 if key in ['Silver','SilverLight','SteelDark','RedMetal'] else .0
            self.materials[key]=mat
        return self.materials[key]

    def bone(self, name, head, parent='Root'):
        self.bones[name]=(head,parent)
        return name

    def finish(self, ob, name, color, bone):
        ob.name=name;ob.data.materials.append(self.mat(color))
        group=ob.vertex_groups.new(name=bone);group.add(list(range(len(ob.data.vertices))),1,'REPLACE')
        self.parts.append(ob)
        return ob

    def mesh(self, name, vertices, faces, color, bone):
        data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
        ob=bpy.data.objects.new(name,data);self.scene.collection.objects.link(ob)
        return self.finish(ob,name,color,bone)

    def oval(self, name, loc, scale, color, bone, seg=12, rings=8, rotate=None, stripe=None):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
        ob=bpy.context.object;ob.scale=scale
        if rotate: ob.rotation_euler=rotate
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        self.finish(ob,name,color,bone)
        if stripe:
            ob.data.materials.append(self.mat(stripe))
            for poly in ob.data.polygons:
                mid=sum((ob.data.vertices[v].co for v in poly.vertices),Vector())/len(poly.vertices)
                # Surface-assigned jagged tiger markings, including the rear silhouette.
                wave=math.sin(mid.z/scale[2]*13 + abs(mid.x)/scale[0]*2.2 + mid.y/scale[1])
                if wave>.78:poly.material_index=1
        return ob

    def tube(self,name,points,radii,color,bone,sides=7,flat=1,sub=1,stripe=None):
        if sub>1:
            pnew=[];rnew=[]
            for i in range(len(points)-1):
                p0,p1,p2,p3=[Vector(points[max(0,min(len(points)-1,j))]) for j in [i-1,i,i+1,i+2]]
                for j in range(sub):
                    t=j/sub
                    pnew.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
                    rnew.append(radii[i]*(1-t)+radii[i+1]*t)
            points=pnew+[Vector(points[-1])];radii=rnew+[radii[-1]]
        vs=[];fs=[]
        for i,p in enumerate(points):
            direction=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
            u=direction.cross(Vector((0,1,0)))
            if u.length<.01:u=direction.cross(Vector((1,0,0)))
            u.normalize();v=direction.cross(u).normalized()
            for j in range(sides):
                a=math.tau*j/sides;vs.append(Vector(p)+radii[i]*(u*math.cos(a)+v*math.sin(a)*flat))
        for i in range(len(points)-1):
            for j in range(sides):
                a=i*sides+j;c=i*sides+(j+1)%sides;fs.append((a,c,c+sides,a+sides))
        fs += [tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))]
        ob=self.mesh(name,vs,fs,color,bone)
        if stripe:
            ob.data.materials.append(self.mat(stripe))
            for p in ob.data.polygons:
                if p.index//sides%4==2:p.material_index=1
        return ob

    def plate(self,name,outline,y,depth,color,bone,bevel=0):
        n=len(outline);vs=[(x,y,z) for x,z in outline]+[(x,y+depth,z) for x,z in outline]
        faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]
        faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
        ob=self.mesh(name,vs,faces,color,bone)
        if bevel:
            bpy.context.view_layer.objects.active=ob
            m=ob.modifiers.new('Plate bevel','BEVEL');m.width=bevel;m.segments=1
            bpy.ops.object.modifier_apply(modifier=m.name)
        return ob

    def eye(self, name, pos, size, iris, bone, outward=0, pupil=True):
        normal=Vector((outward,-1,0)).normalized()
        angle=math.atan2(normal.x,-normal.y)
        def layer(label,delta,sz,col):
            return self.oval(name+label,Vector(pos)+normal*delta,sz,col,bone,16,10,(0,0,angle))
        layer('Socket',0,(size*.78,size*.18,size),'Black')
        layer('Iris',size*.14,(size*.65,size*.13,size*.84),iris)
        if pupil:layer('Pupil',size*.24,(size*.33,size*.07,size*.63),'Black')
        glow=Vector(pos)+normal*(size*.34)+Vector((-size*.20,0,size*.35))
        self.oval(name+'Highlight',glow,(size*.17,size*.055,size*.18),'White',bone,8,5,(0,0,angle))

    def grin(self,name,center,width,height,bone,teeth=8,color='Mouth'):
        x,y,z=center
        outline=[(x-width,z+height*.25),(x-width*.55,z-height*.15),(x,z-height*.28),
                 (x+width*.55,z-height*.15),(x+width,z+height*.25),
                 (x+width*.82,z-height*.70),(x+width*.35,z-height),(x-width*.35,z-height),(x-width*.82,z-height*.70)]
        self.plate(name,outline,y,.025,color,bone)
        for i in range(teeth):
            t=(i+.5)/teeth;xx=x-width*.88+t*width*1.76
            zz=z-height*.15+height*.35*(abs(xx-x)/width)**2
            length=height*(.37 if i in [0,teeth-1] else .22)
            self.tube(name+'Tooth',[(xx,y-.012,zz),(xx,y-.02,zz-length)],[width/teeth*.25,.001],'Ivory',bone,5)

    def finalize(self, folder):
        arm=bpy.data.armatures.new(self.name+'_Armature');rig=bpy.data.objects.new(self.name+'_Rig',arm)
        self.scene.collection.objects.link(rig)
        bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
        bpy.ops.object.mode_set(mode='EDIT')
        for name,(head,parent) in self.bones.items():
            bone=arm.edit_bones.new(name);bone.head=head;bone.tail=Vector(head)+Vector((0,0,.15))
            if parent:bone.parent=arm.edit_bones[parent]
        bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
        for ob in self.parts:ob.select_set(True)
        bpy.context.view_layer.objects.active=self.parts[0];bpy.ops.object.join()
        body=bpy.context.object;body.name=self.name+'_Mesh'
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(body.data);bm.free()
        for poly in body.data.polygons:poly.use_smooth=False
        body.parent=rig;modifier=body.modifiers.new('Skeleton','ARMATURE');modifier.object=rig
        for bone in rig.pose.bones:bone.rotation_mode='XYZ'
        self.rig=rig;self.body=body
        self.animate()
        self.scene.frame_set(1)
        body.data.calc_loop_triangles()
        if len(body.data.loop_triangles)>18000:raise RuntimeError('Triangle budget exceeded: '+self.name)
        for v in body.data.vertices:
            if not v.groups or abs(sum(g.weight for g in v.groups)-1)>.0001:raise RuntimeError('Unweighted vertex')
        # Independent file: writing only this scene avoids copying unrelated user work.
        bpy.data.libraries.write(str(folder/'Source~'/(self.name+'.blend')),{self.scene},fake_user=True,compress=True)
        bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
        bpy.ops.export_scene.fbx(filepath=str(folder/(self.name+'_Model.fbx')),use_selection=True,
            object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',
            apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,
            bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,
            bake_anim_simplify_factor=0,mesh_smooth_type='FACE',use_mesh_modifiers=True)
        return {'vertices':len(body.data.vertices),'triangles':len(body.data.loop_triangles),
                'bones':len(self.bones),'materials':[{'name':m.name,'color':list(m.diffuse_color)} for m in body.data.materials]}

    def key(self, frame, rotations=None, positions=None, scales=None):
        for bone in self.rig.pose.bones:
            bone.rotation_euler=(0,0,0);bone.location=(0,0,0);bone.scale=(1,1,1)
        for name,angles in (rotations or {}).items():
            if name in self.bones:self.rig.pose.bones[name].rotation_euler=[math.radians(a) for a in angles]
        for name,position in (positions or {}).items():
            if name in self.bones:self.rig.pose.bones[name].location=position
        for name,scale in (scales or {}).items():
            if name in self.bones:self.rig.pose.bones[name].scale=scale
        for bone in self.rig.pose.bones:
            for path in ['rotation_euler','location','scale']:bone.keyframe_insert(path,frame=frame,group=bone.name)

    def animate(self):
        self.scene.render.fps=30;self.scene.frame_start=1;self.scene.frame_end=150
        kind=RECIPES[self.name][5]
        limb_names=[name for name in self.bones if name.startswith(('Leg','Tendril','Antenna'))]
        for frame,t in [(1,0),(13,1),(25,0),(37,-1),(49,0)]:
            rot={'Body':(t*1.8,0,t),'Head':(-t*1.5,0,0),'Tail':(0,0,t*4),'Wing.L':(0,t*3,0),'Wing.R':(0,-t*3,0)}
            for i,name in enumerate(limb_names):rot[name]=(t*2,0,t*(3 if i%2 else -3))
            self.key(frame,rot,{'Root':(0,t*.015,0)})
        for frame,t in [(60,0),(66,1),(72,0),(78,-1),(84,0)]:
            rot={'Body':(2,t*2,0),'Head':(-2,0,0),'Tail':(0,0,-t*10),
                 'Thigh.L':(t*23,0,0),'Thigh.R':(-t*23,0,0),'Shin.L':(-max(0,t)*26,0,0),'Shin.R':(-max(0,-t)*26,0,0),
                 'UpperArm.L':(-t*17,0,0),'UpperArm.R':(t*17,0,0),'Hand.L':(t*8,0,0),'Hand.R':(-t*8,0,0),
                 'Wing.L':(0,t*5,0),'Wing.R':(0,-t*5,0)}
            for i,name in enumerate(limb_names):rot[name]=(t*(16 if i%2 else -16),0,t*(10 if i%2 else -10))
            lift=abs(t)*(.10 if kind=='baby' else .04)
            self.key(frame,rot,{'Root':(0,lift,0)}, {'Body':(1+abs(t)*.04,1-abs(t)*.05,1+abs(t)*.02)} if kind=='baby' else None)
        self.key(90)
        wind={'Body':(-10,0,-8),'Head':(-12,0,0),'Jaw':(18,0,0),'UpperArm.R':(-65,0,25),'Hand.R':(-25,0,0),'Tail':(0,0,-15)}
        strike={'Body':(17,0,10),'Head':(20,0,0),'Jaw':(-3,0,0),'UpperArm.R':(25,0,-15),'UpperArm.L':(-28,0,-5),'Hand.R':(-25,0,0),'Tail':(0,0,10)}
        for i,name in enumerate(limb_names):
            wind[name]=(-18,0,(-1 if i%2 else 1)*16)
            strike[name]=(30,0,(-1 if i%2 else 1)*-15)
        self.key(95,wind,{'Root':(0,-.03,.06)})
        self.key(100,strike,{'Root':(0,.02,-.18)})
        self.key(104,{'Body':(7,0,3),'Head':(6,0,0)},{'Root':(0,0,-.07)})
        self.key(108)
        self.key(120)
        collapse={'Body':(22,0,12),'Head':(20,0,0),'Thigh.L':(-25,0,-10),'Thigh.R':(-20,0,10),'Shin.L':(40,0,0),'Shin.R':(40,0,0)}
        for i,name in enumerate(limb_names):collapse[name]=(25,0,(-1 if i%2 else 1)*35)
        self.key(128,collapse,{'Root':(0,-.12,0)})
        if kind in ['baby','crawler','cocoon','spider','floating']:
            collapse['Body']=(12,0,45);endpos=(0,-.18,0)
            endscale={'Body':(1.1,.45,1.05)}
        else:
            collapse.update({'Root':(78,0,6),'UpperArm.L':(-20,0,-30),'UpperArm.R':(-20,0,30)})
            endpos=(0,.18,0);endscale={}
        self.key(140,collapse,{'Root':endpos},endscale);self.key(150,collapse,{'Root':endpos},endscale)
        self.rig.animation_data.action.name=self.name+'_AllClips'
        for name,frame in [('Idle',1),('Walk',60),('Attack',90),('Death',120)]:self.scene.timeline_markers.new(name,frame=frame)

def baby(b, tsumemon=False):
    b.bone('Body',(0,0,.5));b.bone('Head',(0,0,.58),'Body')
    if tsumemon:
        b.oval('RoundCyclops',(0,.015,.66),(.43,.35,.48),'BlueSkin','Body',14,9)
        b.eye('SingleRedEye',(0,-.338,.78),.255,'Ruby','Body')
        for side in [-1,1]:
            name='Antenna.'+('L' if side<0 else 'R');b.bone(name,(side*.17,0,1.02),'Body')
            b.tube('LeafAntenna',[(side*.17,0,1.02),(side*.3,.005,1.4),(side*.59,.02,1.7)],[.025,.085,.001],'BlueLight',name,5,.3)
        for i,a in enumerate([0,65,-65,132,-132]):
            a=math.radians(a);axis=Vector((math.sin(a),-math.cos(a),0))
            base=axis*.29+Vector((0,0,.45));name='Leg'+str(i);b.bone(name,base,'Body')
            pts=[base,axis*.62+Vector((0,0,.43)),axis*.72+Vector((0,0,.20)),axis*.68+Vector((0,0,.10))]
            b.tube('FleshyLeg',pts,[.18,.24,.15,.07],'BlueSkin',name,8,sub=2)
            b.tube('SmallClaw',[pts[-1],axis*.77+Vector((0,0,.015))],[.079,.001],'Ivory',name,5)
    else:
        b.oval('PinkBody',(0,0,.48),(.60,.49,.47),'Pink','Body',16,10)
        b.oval('LowerLip',(0,-.412,.265),(.40,.115,.11),'PinkLight','Body')
        b.grin('Smile',(0,-.491,.36),.38,.1,'Body',6)
        for s in [-1,1]:
            b.eye('RedEye', (s*.235,-.417,.62),.132,'Ruby','Body',s*.28)
            name='Antenna.'+('L' if s<0 else 'R');b.bone(name,(s*.30,.05,.87),'Body')
            b.tube('FoldedPinkEar',[(s*.3,.05,.87),(s*.51,.04,1.00),(s*.71,.015,.84),(s*.85,.02,.64),(s*1.03,.06,.69)], [.11,.095,.085,.12,.001],'Pink',name,5,.28)
            b.tube('EarInner',[(s*.4,-.015,.93),(s*.52,-.025,.96),(s*.69,-.025,.82)],[.044,.048,.018],'PinkLight',name,4,.3)

def dinosaur(b, mode):
    advanced=mode!='Agumon';metal=mode=='MetalGreymon'
    skin='SkinOrange' if advanced else 'SkinGold'
    stripe='ElectricStripe' if metal else 'Stripe' if advanced else None
    headz=1.92 if advanced else 1.58
    b.bone('Body',(0,0,.95));b.bone('Head',(0,-.03,headz-.30),'Body');b.bone('Jaw',(0,-.12,headz-.2),'Head');b.bone('Tail',(0,.23,.68),'Body')
    b.oval('PearTorso',(0,.025,.98),(.46,.34,.66),skin,'Body',14,10,stripe=stripe)
    if not advanced:b.oval('Belly',(0,-.245,.98),(.31,.12,.46),'Belly','Body',12,8)
    else:b.oval('ShoulderMass',(0,.05,1.47),(.45,.31,.28),skin,'Body',12,8,stripe=stripe)
    b.oval('Cranium',(0,-.01,headz),(.40,.34,.41),skin,'Head',14,10,stripe=stripe)
    # Forward-projecting dinosaur snout and opening lower jaw.
    b.oval('LongMuzzle',(0,-.33,headz-.12),(.345,.43,.235),skin,'Head',12,8)
    b.oval('MouthCavity',(0,-.40,headz-.27),(.313,.345,.075),'Mouth','Head',12,6)
    b.oval('LowerJaw',(0,-.39,headz-.335),(.305,.35,.09),skin,'Jaw',12,6)
    for s in [-1,1]:
        b.eye('DinosaurEye',(s*.29,-.24,headz+.12),.145,'Ruby' if mode=='Greymon' else 'Emerald' if not metal else 'BlueLight','Head',s*.7)
        b.oval('Nostril',(s*.175,-.690,headz-.06),(.032,.017,.024),'HornBrown','Head',8,5)
        for i in range(5 if advanced else 3):
            y=-.66+i*.105
            b.tube('UpperFang',[(s*(.235+.025*math.sin(i)),y,headz-.235),(s*.215,y-.01,headz-.33)],[.036 if advanced else .025,.001],'Ivory','Head',5)
        for i in range(3):
            b.tube('LowerFang',[(s*.23,-.58+i*.12,headz-.30),(s*.218,-.58+i*.12,headz-.22)],[.026,.001],'Ivory','Jaw',5)
        upper='UpperArm.'+('L' if s<0 else 'R');hand='Hand.'+('L' if s<0 else 'R')
        b.bone(upper,(s*.37,0,1.38),'Body');b.bone(hand,(s*.57,-.22,.92 if not advanced else 1.12),upper)
        if metal and s<0:
            b.oval('CyberShoulder',(s*.46,0,1.46),(.25,.25,.27),'Silver',upper,10,7)
            b.tube('HydraulicUpperArm',[(s*.48,-.015,1.4),(s*.6,-.15,1.12)],[.13,.105],'SteelDark',upper,8)
            for off in [-.08,.08]:b.tube('ArmPiston',[(s*.48+off,-.19,1.4),(s*.6+off,-.25,1.13)],[.027,.027],'SilverLight',upper,6)
            b.oval('ElbowBearing',(s*.6,-.2,1.12),(.145,.145,.13),'SteelDark',hand,10,6)
            b.oval('CyberGauntlet',(s*.65,-.35,.98),(.22,.30,.23),'Silver',hand,10,7)
            for i in range(3):
                x=s*.65+(i-1)*.115
                b.tube('MetalTalon',[(x,-.5,.96),(x,-.72,.82),(x,-.98,.68)],[.049,.038,.001],'SilverLight',hand,5)
            for i in range(3):b.oval('CyberLight',(s*.79,-.47,.95+i*.07),(.019,.017,.025),'Yellow',hand,6,4)
        else:
            elbowz=.96 if not advanced else 1.12
            b.tube('UpperArm',[(s*.36,.02,1.37),(s*.50,-.015,1.22),(s*.57,-.18,elbowz)],[.14,.13,.085],skin,upper,9,sub=2,stripe=stripe)
            endz=.67 if not advanced else 1.04
            b.tube('Forearm',[(s*.57,-.18,elbowz),(s*.61,-.31,endz+.10),(s*.6,-.4,endz)],[.095,.125,.14],skin,hand,9,sub=2)
            for i in range(3):
                x=s*.60+(i-1)*.092
                b.tube('Finger',[(x,-.41,endz+.055),(x,-.49,endz-.06)],[.059,.055],skin,hand,7)
                b.tube('HandClaw',[(x,-.49,endz-.045),(x,-.53,endz-.21)],[.044,.001],'Ivory',hand,5)
        thigh='Thigh.'+('L' if s<0 else 'R');shin='Shin.'+('L' if s<0 else 'R');foot='Foot.'+('L' if s<0 else 'R')
        b.bone(thigh,(s*.3,.055,.83),'Body');b.bone(shin,(s*.39,-.11,.47),thigh);b.bone(foot,(s*.39,.01,.17),shin)
        b.oval('PowerThigh',(s*.345,.01,.65),(.23,.23,.30),skin,thigh,12,8,stripe=stripe)
        b.tube('Shin',[(s*.4,-.10,.5),(s*.39,.025,.28),(s*.39,-.04,.15)],[.145,.10,.15],skin,shin,9,sub=2,stripe=stripe)
        b.oval('Foot',(s*.395,-.22,.13),(.215,.34,.12),skin,foot,12,7)
        for i in range(3):
            x=s*.395+(i-1)*.125
            b.tube('FootClaw',[(x,-.43,.15),(x,-.60,.055),(x+(i-1)*.02,-.68,.018)],[.07,.046,.001],'Ivory',foot,5)
    b.tube('LongTail',[(0,.22,.69),(0,.59,.55),(0,.97,.51),(.09,1.32,.57),(.14,1.58 if advanced else 1.15,.66)],[.23,.20,.135,.068,.001],skin,'Tail',10,sub=2,stripe=stripe)
    if advanced:
        shell='Silver' if metal else 'HornBrown'
        b.oval('SkullHelmet',(0,.015,headz+.125),(.43,.32,.335),shell,'Head',12,8)
        b.oval('HelmetSnout',(0,-.39,headz-.022),(.36,.38,.215),shell,'Head',10,6)
        for s in [-1,1]:
            # Eye windows remain outside the shell; eyebrows have a heavy overhang.
            b.eye('HelmetEye',(s*.347,-.22,headz+.12),.123,'BlueLight' if metal else 'Ruby','Head',s*.8)
            b.tube('HelmetHorn',[(s*.32,.015,headz+.32),(s*.59,.04,headz+.40),(s*.72,.025,headz+.67)],[.083,.054,.001],shell,'Head',6)
            for i in range(2):
                b.tube('HornBand',[(s*(.4+i*.065),.02,headz+.346+i*.02),(s*(.43+i*.065),.024,headz+.355+i*.02)],[.077-i*.007]*2,'SteelDark','Head',6)
            b.plate('CheekGuard',[(s*.27,headz+.12),(s*.43,headz-.05),(s*.48,headz-.14),(s*.30,headz-.19)],-.035,.14,shell,'Head')
        b.tube('NoseHorn',[(0,-.55,headz+.075),(0,-.68,headz+.31),(0,-.70,headz+.58)],[.085,.051,.001],shell,'Head',5)
    if metal:
        for i in range(8):
            a=(i-3.5)*.30
            b.tube('RedMane',[(math.sin(a)*.25,.22,headz+.17),(math.sin(a)*.38,.39,headz+.25),(math.sin(a)*.47,.54,headz+.37)],[.088,.10,.001],'HairRed','Head',5,.55)
        b.plate('MetalChest',[(-.35,1.49),(.35,1.49),(.36,1.26),(.16,1.16),(-.16,1.16),(-.36,1.26)],-.31,.12,'Silver','Body',.02)
        for x in [-.16,.16]:
            b.oval('MissilePort',(x,-.39,1.31),(.09,.05,.065),'SteelDark','Body',10,6)
            b.tube('ChestCable',[(x,-.35,1.17),(x*1.25,-.36,1.11),(x*1.3,-.3,1.06)],[.022]*3,'Silver','Body',6,sub=2)
        for s in [-1,1]:
            wing='Wing.'+('L' if s<0 else 'R');b.bone(wing,(s*.24,.28,1.46),'Body')
            # Separate membrane lobes and narrow gaps recreate the torn insect-like wings.
            base=(s*.24,1.46)
            for i,(tipx,tipz) in enumerate([(1.05,2.76),(1.18,2.28),(1.26,1.82)]):
                outline=[base,(s*(tipx-.16),tipz-.12),(s*tipx,tipz),(s*(tipx-.04),tipz-.18),(s*(tipx-.2),tipz-.40),(s*.44,1.43)]
                b.plate('TornWingLobe',outline,.33,.028,'Purple',wing)
                b.tube('WingRib',[(s*.26,.31,1.47),(s*(tipx-.21),.34,tipz-.22),(s*tipx,.33,tipz)],[.037,.025,.001],'PurpleLight',wing,5)
                b.oval('WingHole',(s*(tipx-.29),.309,tipz-.29),(.044,.008,.14),'DarkArmor',wing,8,5,(0,s*-.30,0))
        b.tube('TailMechanism',[(0,.72,.67),(0,1.03,.64)],[.10,.08],'Silver','Tail',8)

def keramon(b):
    b.bone('Body',(0,0,.65));b.bone('Head',(0,0,1.56),'Body')
    b.oval('LargeHead',(0,0,1.84),(.49,.34,.36),'BlueLight','Head',16,10)
    b.grin('WideToothyGrin',(0,-.333,1.76),.43,.16,'Head',12)
    for s in [-1,1]:
        b.eye('LargeGreenEye',(s*.255,-.277,1.95),.175,'Green','Head',s*.38)
        b.tube('BrowScar',[(s*.12,-.29,2.05),(s*.19,-.295,2.13),(s*.29,-.24,2.15)],[.012]*3,'Rust','Head',5)
        anten='Antenna.'+('L' if s<0 else 'R');b.bone(anten,(s*.24,.05,2.10),'Head')
        b.tube('LongBentAntenna',[(s*.24,.05,2.10),(s*.51,.06,2.49),(s*.69,.09,2.18),(s*.90,.12,1.96),(s*.94,.16,1.36),(s*.82,.17,1.17)],[.027,.035,.027,.030,.023,.001],'BlueSkin',anten,6,sub=3)
    # Jagged golden collar and ribbed tapering tentacle body.
    for i in range(12):
        a=math.tau*i/12
        start=(.07*math.cos(a),.07*math.sin(a),1.54)
        end=(.34*math.cos(a),.25*math.sin(a),1.36+(i%2)*.06)
        b.tube('GoldenCollarRay',[start,end],[.085,.001],'Yellow','Body',4,.45)
    b.tube('NarrowRibbedTorso',[(0,0,1.45),(0,0,1.1),(.045,.015,.73),(.10,.10,.34)],[.155,.12,.075,.12],'BlueSkin','Body',12,sub=3)
    for i in range(8):
        a=math.tau*i/8
        b.tube('TorsoTendon',[(.14*math.cos(a),.14*math.sin(a),1.43),(.105*math.cos(a),.105*math.sin(a),1.05),(.045+.065*math.cos(a),.02+.065*math.sin(a),.73),(.10+.11*math.cos(a),.09+.10*math.sin(a),.37)],[.019]*4,'BlueLight','Body',5,sub=2)
        name='Leg'+str(i);b.bone(name,(.10,0,.4),'Body')
        tip=(.10+.28*math.cos(a),.03+.24*math.sin(a),.19+(i%2)*.045)
        b.tube('RootTentacle',[(.08,.04,.45),(.1+.16*math.cos(a),.08+.12*math.sin(a),.21),tip],[.042,.046,.018],'BlueSkin',name,6,sub=2)
        b.tube('GreenTentacleTip',[tip,Vector(tip)+Vector((.035*math.cos(a),.035*math.sin(a),-.03))],[.022,.001],'Green',name,5)
    for s in [-1,1]:
        arm='UpperArm.'+('L' if s<0 else 'R');hand='Hand.'+('L' if s<0 else 'R')
        b.bone(arm,(s*.13,0,1.37),'Body');b.bone(hand,(s*.60,-.12,.94),arm)
        b.tube('ThinBentArm',[(s*.13,0,1.37),(s*.40,0,.93),(s*.59,-.13,1.03)],[.059,.058,.057],'BlueSkin',arm,7)
        b.oval('HugeHand',(s*.65,-.15,.82),(.16,.10,.23),'BlueSkin',hand,10,7)
        for i in range(4):
            x=s*.65+(i-1.5)*.073
            b.tube('LongFinger',[(x,-.17,.74),(x+s*.035,-.20,.52),(x+s*.005,-.21,.28+abs(i-1.5)*.07)],[.037,.031,.014],'BlueSkin',hand,6,sub=2)
        b.tube('Thumb',[(s*.54,-.15,.87),(s*.45,-.19,.69),(s*.48,-.22,.49)],[.055,.042,.015],'BlueSkin',hand,7,sub=2)
        for radius,color,y in [(.104,'HairRed',-.244),(.077,'Green',-.26),(.05,'Yellow',-.275)]:
            b.oval('HandTarget',(s*.65,y,.91),(radius,.015,radius*1.18),color,hand,12,6)

def chrysalimon(b):
    b.bone('Body',(0,0,1.07));b.bone('Head',(0,0,1.6),'Body')
    b.oval('CocoonCore',(0,.025,1.13),(.35,.30,.83),'Purple','Body',10,8)
    for i,(z,w,h) in enumerate([(1.65,.28,.42),(1.32,.38,.43),(.98,.31,.31),(.74,.25,.25),(.55,.18,.2)]):
        bone='Head' if i==0 else 'Body'
        b.plate('OverlappingChitin',[(0,z+h*.6),(-w,z+h*.18),(-w*.86,z-h*.4),(0,z-h*.7),(w*.86,z-h*.4),(w,z+h*.18)],-.245-.018*i,.30,'PurpleLight' if i%2==0 else 'Purple',bone,.013)
    b.tube('CrimsonForeheadHorn',[(0,-.05,1.85),(0,-.10,2.12),(0,-.17,2.48)],[.087,.05,.001],'Rust','Head',5)
    for s in [-1,1]:
        b.eye('YellowEye',(s*.135,-.272,1.73),.06,'Yellow','Head',s*.15)
        for i in range(3):
            b.tube('GoldShellSpike',[(s*(.23+i*.02),0,1.74-i*.25),(s*(.43+i*.014),-.06,1.82-i*.25)],[.09,.001],'Gold','Body',5)
        for i in range(3):
            name='Tendril'+('L' if s<0 else 'R')+str(i)
            origin=(s*.25,.19,1.40-i*.19);b.bone(name,origin,'Body')
            endpoint=(s*(1.28-(i%2)*.12),-.17,1.9-i*.66)
            b.tube('LongBlackCable',[origin,(s*.73,.15,1.45-i*.22),(s*1.12,.06,1.45-i*.26),endpoint],[.025,.023,.019,.025],'Black',name,6,sub=3)
            tip=Vector(endpoint)+Vector((s*.10,-.16,-.23))
            b.tube('GoldenSpear', [endpoint,Vector(endpoint)*.35+tip*.65,tip],[.12,.07,.001],'Yellow',name,4,.30)
            b.tube('SpearSpine',[Vector(endpoint)+Vector((0,-.035,0)),tip],[.046,.001],'Purple',name,4,.3)
    b.tube('BottomGoldSting',[(0,0,.44),(0,-.02,.19)],[.069,.001],'Gold','Body',5)

def infermon(b):
    b.bone('Body',(0,0,.85));b.bone('Head',(0,-.60,.83),'Body');b.bone('Tail',(0,.60,.76),'Body')
    b.tube('ArmoredAbdomen',[(0,-.48,.89),(0,-.15,1.03),(0,.24,1.0),(0,.64,.82),(0,.96,.73)],[.26,.37,.34,.22,.19],'ShellWhite','Body',12,sub=3)
    b.tube('RedDorsalShell',[(0,-.48,1.04),(0,-.18,1.27),(0,.25,1.20),(0,.64,1.0),(0,.92,.91)],[.22,.29,.25,.19,.14],'RedMetal','Body',10,flat=.34,sub=3)
    b.oval('WhiteMask',(0,-.72,.86),(.25,.16,.29),'ShellWhite','Head',12,8)
    b.plate('MaskJaw',[(-.20,.78),(0,.65),(.20,.78),(.15,.61),(0,.53),(-.15,.61)],-.82,.08,'Silver','Head',.01)
    for s in [-1,1]:
        b.eye('MaskEye',(s*.126,-.851,.94),.073,'Green','Head',s*.15)
        b.oval('GoldenPupil',(s*.126,-.872,.94),(.025,.017,.038),'Yellow','Head',10,6)
        b.tube('MaskBrow',[(s*.04,-.84,1.05),(s*.11,-.85,1.10),(s*.20,-.78,1.13)],[.012]*3,'Rust','Head',5)
    b.tube('RedHeadSpike',[(0,-.65,1.10),(0,-.73,1.36),(0,-.83,1.61)],[.072,.044,.001],'RedMetal','Head',5)
    for y,z in [(-.13,1.26),(.23,1.2),(.64,1.0)]:b.tube('DorsalGoldSpike',[(0,y,z),(0,y+.015,z+.20)],[.083,.001],'Gold','Body',5)
    b.tube('GoldenTail',[(0,.9,.77),(0,1.17,.73),(0,1.42,.69)],[.16,.095,.001],'Gold','Tail',7)
    for s in [-1,1]:
        for i,y in enumerate([-.40,.12,.62]):
            name='Leg'+('L' if s<0 else 'R')+str(i);b.bone(name,(s*.24,y,.91),'Body')
            b.oval('RedLegSocket',(s*.28,y,.91),(.065,.135,.135),'RedMetal','Body',10,7)
            end=(s*(1.14+(i%2)*.13),y-.15,.10)
            points=[(s*.29,y,.92),(s*.71,y-.04,1.2),(s*1.10,y-.08,1.1),end]
            b.tube('CableLeg',points,[.049,.054,.050,.04],'SteelDark',name,8,sub=3)
            for off in [-.023,.023]:
                b.tube('LegTendon',[(x,y0+off,z+.022) for x,y0,z in points],[.012]*4,'Silver',name,5,sub=3)
            b.tube('RedFootCuff',[end,Vector(end)+Vector((0,0,.08))],[.105,.1],'RedMetal',name,8)
            for direction in [-1,1]:
                b.tube('GoldenFootClaw',[end,Vector(end)+Vector((s*.16,direction*.17,-.085))],[.052,.001],'Gold',name,5)

def diaboromon(b):
    b.bone('Body',(0,.12,1.10));b.bone('Head',(0,-.28,1.55),'Body');b.bone('Tail',(0,.36,.95),'Body')
    b.oval('NarrowTorso',(0,.075,1.33),(.29,.235,.55),'Tendon','Body',12,8)
    b.oval('DarkBackArmor',(0,.25,1.58),(.35,.28,.45),'DarkArmor','Body',10,7)
    for i in range(3):
        b.tube('TallBackSpine',[(0,.12+i*.19,1.92-i*.16),(0,.18+i*.2,2.22-i*.18),(0,.2+i*.24,2.78-i*.24)],[.09,.065,.001],'Rust','Body',5)
    for s in [-1,1]:
        b.plate('RibArmor',[(s*.10,1.64),(s*.30,1.55),(s*.39,1.25),(s*.22,1.12),(s*.17,1.35)],.15,.1,'DarkArmorEdge','Body')
        upper='UpperArm.'+('L' if s<0 else 'R');hand='Hand.'+('L' if s<0 else 'R')
        b.bone(upper,(s*.30,.04,1.66),'Body');b.bone(hand,(s*.79,-.21,.63),upper)
        b.oval('ShoulderBall',(s*.33,.04,1.69),(.19,.19,.19),'TendonLight',upper,10,7)
        b.plate('HugeShoulderArmor',[(s*.37,1.87),(s*.80,1.87),(s*1.04,1.70),(s*1.33,1.67),(s*1.09,1.57),(s*.89,1.48),(s*.55,1.54)],-.025,.36,'DarkArmor',upper,.018)
        for i in range(3):
            x=.55+i*.15
            b.plate('ShoulderLame',[(s*x,1.86),(s*(x+.10),1.83),(s*(x+.15),1.55),(s*(x+.06),1.57)],-.065,.06,'DarkArmorEdge',upper,.008)
        pts=[(s*.43,.0,1.60),(s*.66,.0,1.25),(s*.78,-.12,.85),(s*.8,-.22,.61)]
        b.tube('LongTendonArm',pts,[.105,.09,.064,.075],'Tendon',upper,9,sub=2)
        for offset in [-.033,0,.033]:b.tube('ArmTendonCable',[(x+offset,y-.05,z) for x,y,z in pts],[.021]*4,'TendonLight',upper,5,sub=2)
        b.plate('HandArmor',[(s*.70,.78),(s*.89,.78),(s*1.02,.38),(s*.64,.32),(s*.57,.5)],-.31,.20,'DarkArmor',hand,.01)
        for i in range(4):
            x=s*.78+(i-1.5)*.105
            b.tube('HugeRustClaw',[(x,-.40,.46),(x+s*.08,-.63,.32),(x+s*.11,-.91,.22)],[.069,.047,.001],'Rust',hand,5,sub=2)
        thigh='Thigh.'+('L' if s<0 else 'R');shin='Shin.'+('L' if s<0 else 'R');foot='Foot.'+('L' if s<0 else 'R')
        b.bone(thigh,(s*.16,.07,.95),'Body');b.bone(shin,(s*.46,-.035,.56),thigh);b.bone(foot,(s*.32,.15,.19),shin)
        b.tube('BentThigh',[(s*.17,.10,1.02),(s*.41,.02,.70),(s*.47,-.02,.55)],[.14,.16,.10],'Tendon',thigh,9,sub=2)
        b.tube('ReverseCalf',[(s*.47,-.02,.56),(s*.32,.18,.38),(s*.30,.12,.19)],[.10,.10,.065],'TendonLight',shin,9,sub=2)
        for i in range(3):
            x=s*.32+(i-1)*.09
            b.tube('LongFootTalon',[(x,.08,.15),(x+s*.05,-.13,.09),(x+s*.10,-.43,.03)],[.055,.04,.001],'Rust',foot,5,sub=2)
    b.tube('RearTail',[(0,.23,.97),(0,.52,.80),(0,.63,.66)],[.13,.07,.001],'Tendon','Tail',7)
    # Small forward mask, tiny green/yellow eyes and long golden mane.
    b.oval('PointedFace',(0,-.30,1.53),(.145,.12,.22),'DarkArmor','Head',10,7)
    b.plate('Forehead',[(-.15,1.62),(0,1.90),(.15,1.62),(.10,1.43),(-.10,1.43)],-.37,.08,'DarkArmorEdge','Head')
    b.grin('ToothedMouth',(0,-.435,1.41),.105,.074,'Head',6)
    b.plate('Chin',[(-.10,1.35),(0,1.22),(.10,1.35),(.07,1.4),(-.07,1.4)],-.37,.08,'DarkArmorEdge','Head')
    for s in [-1,1]:
        b.eye('SmallGreenEye',(s*.072,-.419,1.55),.045,'Green','Head')
        b.oval('EyeGold',(s*.072,-.43,1.55),(.012,.010,.018),'Yellow','Head',8,5)
        b.tube('SideFaceHorn',[(s*.1,-.28,1.69),(s*.27,-.22,1.68),(s*.34,-.19,1.87)],[.028,.022,.001],'DarkArmorEdge','Head',5)
        for i in range(8):
            b.tube('GoldenMane',[(s*.12,.0,1.68-i*.045),(s*(.22+i*.01),-.11,1.52-i*.038),(s*(.23+i*.01),-.18,1.21-i*.025)],[.041,.036,.001],'Gold','Head',5,.6)

def build_asset(name):
    korean,costFolder,height,width,depth,kind=RECIPES[name]
    folder=PROJECT/'Assets/Resources/Digimon'/costFolder/name
    if (folder/(name+'.prefab')).exists():
        raise RuntimeError('Existing final asset requires an explicit revision: '+name)
    reference=[]
    for view in ['앞','옆','뒤']:
        matches=list(REFERENCES.glob(korean+'_'+view+'.*'))
        if len(matches)!=1:raise RuntimeError('Missing/ambiguous reference: '+korean+'_'+view)
        reference.append({'path':str(matches[0]),'sha256':hashlib.sha256(matches[0].read_bytes()).hexdigest()})
    (folder/'Source~').mkdir(parents=True,exist_ok=True)
    b=Builder(name)
    if name in ['Koromon','Tsumemon']:baby(b,name=='Tsumemon')
    elif name in ['Agumon','Greymon','MetalGreymon']:dinosaur(b,name)
    elif name=='Keramon':keramon(b)
    elif name=='Chrysalimon':chrysalimon(b)
    elif name=='Infermon':infermon(b)
    else:diaboromon(b)
    report=b.finalize(folder)
    report.update(name=name,korean=korean,costFolder=costFolder,targetHeight=height,maxWidth=width,maxDepth=depth,
                  references=reference,clips=[{'name':n,'firstFrame':a,'lastFrame':z,'loop':loop} for n,a,z,loop in [('Idle',1,49,True),('Walk',60,84,True),('Attack',90,108,False),('Death',120,150,False)]])
    (folder/'Source~'/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    source=(PROJECT/'Tools/Blender/roster_models.py').read_text(encoding='utf-8')
    (folder/'Source~'/'build_model.py').write_text(source+'\nresult=build_asset('+repr(name)+')\n',encoding='utf-8')
    return report

result=build_asset('Chrysalimon')
