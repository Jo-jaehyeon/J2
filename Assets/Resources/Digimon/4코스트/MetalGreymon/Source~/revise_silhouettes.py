"""Third revision: continuous dinosaur skulls and reference-proportioned Diaboromon."""
from pathlib import Path
exec(compile(Path(r'C:/Jerry/Unity Project/J2/Tools/Blender/revise_roster.py').read_text(encoding='utf-8'),'revise_roster.py','exec'))

def loft(b,name,sections,color,bone,sides=16):
    # Longitudinal cross-sections: y, half-width, lower edge, upper edge.
    vs=[];fs=[]
    for y,w,low,high in sections:
        for i in range(sides):
            a=math.tau*i/sides
            vs.append((w*math.cos(a),y,(low+high)/2+(high-low)/2*math.sin(a)))
    for j in range(len(sections)-1):
        for i in range(sides):
            a=j*sides+i;c=j*sides+(i+1)%sides;fs.append((a,c,c+sides,a+sides))
    fs+=[tuple(reversed(range(sides))),tuple((len(sections)-1)*sides+i for i in range(sides))]
    return b.mesh(name,vs,fs,color,bone)

class SkullBuilder(RevisedBuilder):
    def oval(self,name,*args,**kwargs):
        if name in ['Cranium','LongMuzzle','SkullHelmet','HelmetSnout']:return None
        if self.name=='Agumon' and name=='LowerJaw':
            return loft(self,'TaperedLowerJaw',[(.02,.20,1.18,1.32),(-.24,.30,1.17,1.29),(-.52,.23,1.17,1.25),(-.74,.15,1.23,1.28)],'SkinGold','Jaw')
        return super().oval(name,*args,**kwargs)
    def detailed_eye(self,name,pos,size,iris,bone,outward=0,pupil=True):
        if self.name!='Agumon':
            side=-1 if pos[0]<0 else 1
            pos=(side*.377,-.145,2.07);outward=side*1.8;size=.108
        return detailed_eye(self,name,pos,size,iris,bone,outward,pupil)

def skull_dinosaur(b):
    dinosaur(b,b.name)
    if b.name=='Agumon':
        # Rounded crown, full cheeks and a gently sloping nose which narrows in front view.
        loft(b,'SculptedAgumonHead',[(.30,.13,1.36,1.87),(.22,.28,1.30,1.98),(.06,.38,1.27,2.005),
            (-.12,.40,1.27,1.97),(-.28,.36,1.27,1.85),(-.45,.295,1.265,1.72),
            (-.62,.24,1.27,1.62),(-.76,.17,1.285,1.55),(-.81,.10,1.32,1.48)],'SkinGold','Head',20)
        # Thin cheek creases follow the surface rather than separating the head into boxes.
        for s in [-1,1]:
            b.tube('SmileCrease',[(s*.29,-.32,1.31),(s*.32,-.26,1.35),(s*.345,-.22,1.42)],[.007,.008,.003],'HornBrown','Head',5)
    else:
        shell='Silver' if b.name=='MetalGreymon' else 'HornBrown'
        loft(b,'ContinuousHelmet',[(.30,.12,1.84,2.22),(.20,.30,1.77,2.36),(.03,.42,1.74,2.39),
            (-.17,.415,1.73,2.31),(-.36,.37,1.70,2.19),(-.56,.32,1.68,2.09),(-.77,.25,1.72,2.01),(-.83,.15,1.76,1.98)],shell,'Head',16)
        # Internal flesh stays wholly inside the cap; the separate animated jaw remains exposed.
        for s in [-1,1]:
            b.tube('HelmetSeam',[(s*.12,.14,2.36),(s*.23,-.20,2.25),(s*.20,-.62,2.055)],[.007]*3,'SteelDark' if b.name=='MetalGreymon' else 'Black','Head',5)

def lean_diaboromon(b):
    # Retain only the revised small face; rebuild the body and all limb pivots.
    diaboromon(b)
    for ob in list(b.parts):
        if ob.vertex_groups[0].name!='Head':
            b.parts.remove(ob);bpy.data.objects.remove(ob,do_unlink=True)
        else:
            for vertex in ob.data.vertices:
                p=ob.matrix_world@vertex.co;p.y-=.48;p.z-=.40;vertex.co=ob.matrix_world.inverted()@p
    b.bones={}
    b.bone('Root',(0,0,0),None);b.bone('Body',(0,.35,.90));b.bone('Head',(0,-.76,1.15),'Body');b.bone('Tail',(0,.77,.78),'Body')
    b.tube('LeanSpinalTorso',[(0,-.46,1.35),(0,-.12,1.16),(0,.29,.94),(0,.68,.82)],[.20,.155,.105,.13],'Tendon','Body',10,sub=3)
    for s in [-1,1]:
        b.tube('WaistTendon',[(s*.13,-.16,1.11),(s*.085,.25,.84),(s*.105,.62,.81)],[.025,.02,.028],'TendonLight','Body',6,sub=3)
        upper='UpperArm.'+('L' if s<0 else 'R');hand='Hand.'+('L' if s<0 else 'R')
        thigh='Thigh.'+('L' if s<0 else 'R');shin='Shin.'+('L' if s<0 else 'R');foot='Foot.'+('L' if s<0 else 'R')
        b.bone(upper,(s*.31,-.38,1.30),'Body');b.bone(hand,(s*.64,-.75,.20),upper)
        shoulder=[(s*.13,-.58,1.60),(s*.40,-.64,1.57),(s*.83,-.27,1.27),(s*.68,-.70,1.13),(s*.26,-.81,1.15),(s*.27,.11,1.42),(s*.66,.08,1.21)]
        b.mesh('SlopingShoulderCarapace',shoulder,[(0,1,2,3,4),(0,5,6,2,1),(2,6,5,0,4,3)],'DarkArmor',upper)
        pts=[(s*.32,-.35,1.28),(s*.49,-.13,.91),(s*.63,-.13,.61),(s*.65,-.76,.20)]
        b.tube('LongLeanForelimb',pts,[.074,.062,.058,.055],'Tendon',upper,8,sub=3)
        for offset in [-.035,0,.035]:b.tube('VisibleArmTendon',[(x+offset,y-.025,z) for x,y,z in pts],[.012]*4,'TendonLight',upper,5,sub=3)
        # Broad flattened gauntlets and long talons support the front of the body.
        b.mesh('LongFlatGauntlet',[(s*.48,-.72,.25),(s*.78,-.72,.25),(s*.91,-1.17,.115),(s*.40,-1.22,.09),
              (s*.48,-.72,.12),(s*.78,-.72,.12),(s*.91,-1.17,.07),(s*.40,-1.22,.055)],
              [(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'DarkArmor',hand)
        for i in range(4):
            x=s*.65+(i-1.5)*.12
            b.tube('LongFrontTalon',[(x,-1.11,.105),(x+s*.03,-1.41,.065),(x+s*.045,-1.67,.023)],[.049,.034,.001],'Rust',hand,5,sub=2)
        b.bone(thigh,(s*.16,.63,.84),'Body');b.bone(shin,(s*.36,.19,.46),thigh);b.bone(foot,(s*.35,.68,.13),shin)
        b.tube('SlenderFoldedThigh',[(s*.16,.63,.84),(s*.27,.38,.64),(s*.36,.19,.46)],[.085,.10,.067],'Tendon',thigh,9,sub=3)
        b.tube('SlimRearShin',[(s*.36,.19,.46),(s*.35,.58,.25),(s*.35,.68,.12)],[.065,.05,.068],'TendonLight',shin,8,sub=3)
        for i in range(3):
            x=s*.35+(i-1)*.085
            b.tube('RearFootTalon',[(x,.66,.12),(x+s*.02,.47,.065),(x+s*.04,.26,.023)],[.039,.025,.001],'Rust',foot,5,sub=2)
    # Four distinct dorsal spines span the neck, shoulders, mid-back and pelvis.
    for i,(y,z,h) in enumerate([(-.52,1.53,.86),(-.16,1.43,1.18),(.28,1.15,.76),(.70,.92,.46)]):
        b.oval('DorsalArmorSegment',(0,y,z-.07),(.25-i*.025,.19,.105),'DarkArmor','Body',10,6)
        b.tube('NeckToPelvisSpine',[(0,y,z),(0,y-.03,z+h*.56),(0,y-.28,z+h)],[.085-i*.009,.061-i*.008,.001],'Rust','Body',5)
        if i>0:
            for s in [-1,1]:b.tube('SideRibPlate',[(s*.13,y-.07,z),(s*.28,y,z-.09),(s*.26,y+.05,z-.25)],[.055,.053,.025],'DarkArmorEdge','Body',5)
    b.tube('FineTail',[(0,.72,.81),(0,1.02,.67),(0,1.15,.53)],[.10,.055,.001],'Tendon','Tail',7)

def revision3(name):
    korean,costFolder,height,width,depth,kind=RECIPES[name]
    folder=PROJECT/'Assets/Resources/Digimon'/costFolder/name
    prior=json.loads((folder/'Source~/manifest.json').read_text(encoding='utf-8'))
    backup=folder/'Source~/BeforeSilhouetteRevision';backup.mkdir(exist_ok=True)
    for f in [folder/'Source~'/(name+'.blend'),folder/'Source~/manifest.json']:
        if not (backup/f.name).exists():shutil.copy2(f,backup/f.name)
    b=RevisedBuilder(name) if name=='Diaboromon' else SkullBuilder(name)
    if name=='Diaboromon':lean_diaboromon(b)
    else:skull_dinosaur(b)
    report=b.finalize(folder)
    prior.update(report);prior['revision']='continuous-skull-and-lean-demon-3'
    (folder/'Source~/manifest.json').write_text(json.dumps(prior,ensure_ascii=False,indent=2),encoding='utf-8')
    shutil.copy2(PROJECT/'Tools/Blender/revise_silhouettes.py',folder/'Source~/revise_silhouettes.py')
    return {'name':name,'triangles':report['triangles']}
