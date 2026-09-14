"""Explicit user-authorized face/anatomy revision. Run inside Blender MCP.

Preserves Unity GUIDs and archives the prior native source. Uses ready closeups;
reference movement happens only after Unity verification, outside this script.
"""
from pathlib import Path
exec(compile((Path(r'C:/Jerry/Unity Project/J2/Tools/Blender/roster_models.py')).read_text(encoding='utf-8'),'roster_models.py','exec'))
import shutil

def detailed_eye(self,name,pos,size,iris,bone,outward=0,pupil=True):
    if self.name=='Agumon':
        side=-1 if pos[0]<0 else 1
        pos=(side*.393,-.10,pos[2]);outward=side*2.8;size*=.85
    n=Vector((outward,-1,0)).normalized();u=Vector((-n.y,n.x,0));v=Vector((0,0,1));p=Vector(pos)
    side=-1 if pos[0]<0 else 1
    species=self.name
    p+=n*({'Greymon':.055,'MetalGreymon':.055,'Koromon':.025}.get(species,0))
    if species=='Tsumemon':
        outline=[(math.cos(a),math.sin(a)) for a in [math.tau*i/24 for i in range(24)]]
        outline[12]=(-1.15,0)
    elif species=='Keramon':
        outline=[(-.95,-.83),(-.82,.25),(-.45,.95),(.35,1.05),(.90,.55),(.92,-.45),(.37,-1.02),(-.35,-1.00)]
    elif species=='Koromon':
        outline=[(-.86,-.66),(-.80,.26),(-.40,.90),(.12,1),(.64,.65),(.85,-.18),(.91,-.73),(.05,-.70)]
    elif species=='Agumon':
        outline=[(-1.0,-.57),(-.98,.23),(-.51,.85),(.14,.94),(.72,.54),(.89,-.35),(.46,-.78),(-.25,-.85)]
    elif species=='Greymon':
        outline=[(-1.12,-.25),(-.59,.51),(.26,.68),(1.02,.26),(.69,-.46),(-.20,-.60)]
    elif species=='MetalGreymon':
        outline=[(-1.09,-.56),(-1.02,.36),(-.23,.75),(.83,.65),(1.04,-.10),(.33,-.64)]
    elif species in ['Infermon','Chrysalimon']:
        outline=[(-1.24,-.68),(-.80,.30),(1.13,.75),(.65,-.36),(-.05,-.66)]
    else:
        outline=[(-.55,-.8),(-.65,.40),(-.08,.96),(.57,.44),(.61,-.5),(.10,-.84)]
    if species not in ['Tsumemon','Keramon']:outline=[(x*side,z) for x,z in outline]
    def patch(label,xy,color,depth):
        verts=[p+u*(x*size)+v*(z*size)+n*depth for x,z in xy]
        area=sum(xy[i][0]*xy[(i+1)%len(xy)][1]-xy[(i+1)%len(xy)][0]*xy[i][1] for i in range(len(xy)))
        face=tuple(range(len(verts)))
        if area<0:face=tuple(reversed(face))
        count=len(verts);verts += [v-n*.002 for v in verts]
        faces=[face,tuple(i+count for i in reversed(face))]
        faces += [(face[i],face[(i+1)%count],face[(i+1)%count]+count,face[i]+count) for i in range(count)]
        return self.mesh(name+label,verts,faces,color,bone)
    rim='SteelDark' if species=='MetalGreymon' else 'PurpleLight' if species=='Chrysalimon' else 'Silver' if species=='Infermon' else 'HornBrown' if species=='Greymon' else 'Black'
    patch('ShapedRim',[(x*1.12,z*1.12) for x,z in outline],rim,.003)
    patch('Aperture',outline,'Ruby' if species=='Koromon' else 'Ivory' if species=='Agumon' else 'Black',.008)
    def disk(label,cx,cz,rx,rz,color,depth):
        patch(label,[(cx+rx*math.cos(math.tau*i/24),cz+rz*math.sin(math.tau*i/24)) for i in range(24)],color,depth)
    if species=='Keramon':
        disk('ThinGreenRing',0,0,.79,.91,'Green',.013)
        disk('VeryLargeBlackPupil',0,0,.68,.79,'Black',.017)
    elif species=='Tsumemon':
        disk('CopperIris',0,0,.80,.82,'Ruby',.013)
        disk('InnerIris',0,-.08,.58,.59,'Rust',.015)
        disk('RoundPupil',0,0,.40,.40,'Black',.020)
    else:
        rx=.69 if species=='Agumon' else .52 if species not in ['Diaboromon','Chrysalimon'] else .36
        rz=.78 if species=='Agumon' else .63 if species=='Koromon' else .43
        disk('Iris',0,0,rx,rz,iris,.013)
        if species in ['Infermon','Diaboromon']:
            disk('GoldCenter',0,0,rx*.68,rz*.70,'Yellow',.017)
        disk('Pupil',0,0,rx*.48,rz*.60,'Black',.022)
    glint=.18 if species in ['Tsumemon','Koromon','Agumon'] else .11
    disk('Highlight',-.22,.31,glint,glint,'White',.026)
    if species=='Tsumemon':disk('SecondaryHighlight',.41,-.22,.12,.12,'White',.027)
    # Raised brows are integrated skin/armor, not identical floating circular eyeballs.
    if species in ['Agumon','Greymon','MetalGreymon','Chrysalimon']:
        color={'Agumon':'SkinGold','Greymon':'HornBrown','MetalGreymon':'Silver','Chrysalimon':'Purple'}[species]
        pts=[p+u*(x*size)+v*(z*size)+n*.027 for x,z in outline[:4]]
        self.tube(name+'UpperLid',pts,[size*.09]*len(pts),color,bone,5)

def koromon_mouth(self):
    # A single curved mouth opening follows the blob, with tongue inside and four fangs.
    def surf(x,z,offset=0):
        return (x,-.49*math.sqrt(max(.025,1-(x/.60)**2-((z-.48)/.47)**2))-offset,z)
    vertices=[];faces=[]
    for i in range(17):
        x=-.385+i*.77/16;edge=abs(x)/.385
        top=.35+.015*edge;bottom=.18+.155*edge**3
        for j in range(5):vertices.append(surf(x,bottom+(top-bottom)*j/4,.045))
    for i in range(16):
        for j in range(4):a=i*5+j;faces.append((a,a+5,a+6,a+1))
    ob=self.mesh('CleanMouthOpening',vertices,faces,'Mouth','Body')
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('Mouth thickness','SOLIDIFY');mod.thickness=.003
    bpy.ops.object.modifier_apply(modifier=mod.name)
    tongue=[surf(0,.252,.058)]+[surf(.16*math.cos(math.tau*i/20),.252+.044*math.sin(math.tau*i/20),.058) for i in range(20)]
    ob=self.mesh('TongueInsideMouth',tongue,[(0,i+1,(i+1)%20+1) for i in range(20)],'PinkShadow','Body')
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('Tongue thickness','SOLIDIFY');mod.thickness=.002
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for x,z in [(-.32,.35),(.32,.35),(-.23,.346),(.23,.346)]:
        a=Vector(surf(x,z,.055));end=a+Vector((0,-.008,-.055 if abs(x)>.3 else -.035))
        self.tube('SmallMouthFang',[a,end],[.018,.001],'Ivory','Body',5)

class RevisedBuilder(Builder):
    detailed_eye=detailed_eye
    koromon_mouth=koromon_mouth
    def oval(self,name,*args,**kwargs):
        ob=super().oval(name,*args,**kwargs)
        if self.name in ['Agumon','Greymon','MetalGreymon'] and name in ['Cranium','LongMuzzle','LowerJaw','HelmetSnout']:
            # Superellipsoid creates a broad flat muzzle and planes around the temples.
            power=.53 if self.name=='Agumon' else .64
            extents=[max(abs(v.co[j]) for v in ob.data.vertices) for j in range(3)]
            for vert in ob.data.vertices:
                for j in range(3):
                    a=vert.co[j]/extents[j]
                    vert.co[j]=math.copysign(abs(a)**power,a)*extents[j]
        return ob

def quadruped(b):
    """Retarget rest geometry and pivots to a long, low, four-contact stance."""
    for ob in b.parts:
        group=ob.vertex_groups[0].name
        def warp(p):
            x,y,z=p
            if group.startswith(('UpperArm','Hand')):
                # Forelimbs reach forwards and down, with knuckles on the same plane as feet.
                return Vector((x,y-.48,.02+(z-.22)*.70))
            if group.startswith(('Thigh','Shin','Foot')):
                return Vector((x,y+.52,.02+(z-.03)*.66))
            if group=='Head':return Vector((x,y-.50,z*.72-.14))
            if group=='Tail':return Vector((x,y+.48,z*.68))
            return Vector((x,y+.12,(z-1.1)*.80+.75))
        inv=ob.matrix_world.inverted()
        for vert in ob.data.vertices:vert.co=inv@warp(ob.matrix_world@vert.co)
    for name,(head,parent) in list(b.bones.items()):
        x,y,z=head
        if name.startswith(('UpperArm','Hand')):p=(x,y-.48,.02+(z-.22)*.70)
        elif name.startswith(('Thigh','Shin','Foot')):p=(x,y+.52,.02+(z-.03)*.66)
        elif name=='Head':p=(x,y-.50,z*.72-.14)
        elif name=='Tail':p=(x,y+.48,z*.68)
        elif name=='Root':p=head
        else:p=(x,y+.12,(z-1.1)*.80+.75)
        b.bones[name]=(p,parent)

original_animate=RevisedBuilder.animate
def animate_revision(self):
    original_animate(self)
    if self.name!='Diaboromon':return
    # Neutral planted forehands in idle; diagonal fore/hind pairs advance in the walk.
    for f,t in [(1,0),(13,1),(25,0),(37,-1),(49,0)]:
        self.key(f,{'Head':(t*1.5,0,0),'Tail':(0,0,t*3)})
    for f,t in [(60,0),(66,1),(72,0),(78,-1),(84,0)]:
        self.key(f,{'Head':(-t*2,0,0),'Tail':(0,0,t*6)},
            {'UpperArm.L':(0,max(0,t)*.07,t*.11),'UpperArm.R':(0,max(0,-t)*.07,-t*.11),
             'Thigh.L':(0,max(0,-t)*.06,-t*.10),'Thigh.R':(0,max(0,t)*.06,t*.10)})
    self.key(90)
    self.key(95,{'Head':(-8,0,0),'UpperArm.R':(-14,0,0)},{'Hand.R':(0,.14,.02)})
    self.key(100,{'Head':(9,0,0),'UpperArm.R':(9,0,0)},{'Hand.R':(0,.05,.19)})
    self.key(104,{'Head':(3,0,0)})
    self.key(108)
    self.key(120)
    self.key(128,{'Head':(18,0,0)},{'Body':(0,-.12,0)})
    for f in [140,150]:self.key(f,{'Body':(0,0,18),'Head':(22,0,0)},{'Body':(0,-.35,0)}, {'Body':(1,.68,1)})
RevisedBuilder.animate=animate_revision

def revise(name):
    korean,costFolder,height,width,depth,kind=RECIPES[name]
    folder=PROJECT/'Assets/Resources/Digimon'/costFolder/name
    prior=json.loads((folder/'Source~/manifest.json').read_text(encoding='utf-8'))
    photos=list(REFERENCES.glob(korean+'_확대.*'))
    if len(photos)!=1:raise RuntimeError('Need unique closeup: '+korean)
    archive=folder/'Source~/BeforeFaceRevision'
    archive.mkdir(exist_ok=True)
    for file in [folder/'Source~'/(name+'.blend'),folder/'Source~/manifest.json',folder/'Source~/build_model.py']:
        target=archive/file.name
        if file.exists() and not target.exists():shutil.copy2(file,target)
    b=RevisedBuilder(name)
    if name in ['Koromon','Tsumemon']:baby(b,name=='Tsumemon')
    elif name in ['Agumon','Greymon','MetalGreymon']:dinosaur(b,name)
    elif name=='Keramon':keramon(b)
    elif name=='Chrysalimon':chrysalimon(b)
    elif name=='Infermon':infermon(b)
    else:diaboromon(b);quadruped(b)
    report=b.finalize(folder)
    report.update(name=name,korean=korean,costFolder=costFolder,targetHeight=height,maxWidth=width,maxDepth=depth,
        references=[{'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in photos],
        previousReferences=prior.get('references',[]),clips=prior['clips'],revision='face-and-anatomy-2')
    (folder/'Source~/manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    shutil.copy2(PROJECT/'Tools/Blender/roster_models.py',folder/'Source~/roster_models.py')
    shutil.copy2(PROJECT/'Tools/Blender/revise_roster.py',folder/'Source~/revise_roster.py')
    return {k:report[k] for k in ['name','triangles','bones','revision']}
