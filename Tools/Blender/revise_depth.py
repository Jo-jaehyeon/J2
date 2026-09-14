from pathlib import Path
exec(compile(Path(r'C:/Jerry/Unity Project/J2/Tools/Blender/revise_silhouettes.py').read_text(encoding='utf-8'),'revise_silhouettes.py','exec'))

class RestoredAgumon(RevisedBuilder):
    def oval(self,name,*args,**kwargs):
        if name=='Belly':return None
        return super().oval(name,*args,**kwargs)

def deep_face(b):
    lean_diaboromon(b)
    for ob in list(b.parts):
        if ob.vertex_groups[0].name=='Head' and not ob.name.startswith('GoldenMane'):
            b.parts.remove(ob);bpy.data.objects.remove(ob,do_unlink=True)
    # Solid cranial shell with an actual side profile and projecting muzzle.
    rings=[(1.49,-.76,.025,.045),(1.38,-.77,.09,.115),(1.27,-.79,.15,.145),
           (1.16,-.80,.173,.15),(1.08,-.81,.155,.15)]
    vs=[];fs=[];count=16
    for z,y,w,d in rings:
        for i in range(count):
            a=math.tau*i/count;vs.append((w*math.cos(a),y+d*math.sin(a),z))
    for j in range(len(rings)-1):
        for i in range(count):
            a=j*count+i;c=j*count+(i+1)%count;fs.append((a,c,c+count,a+count))
    fs += [tuple(reversed(range(count))),tuple((len(rings)-1)*count+i for i in range(count))]
    b.mesh('VolumetricCranialShell',vs,fs,'DarkArmor','Head')
    b.mesh('ProjectingUpperMuzzle',[(-.14,-.945,1.135),(.14,-.945,1.135),(-.13,-1.095,1.075),(.13,-1.095,1.075),
        (-.115,-1.095,1.045),(.115,-1.095,1.045),(-.125,-.88,1.04),(.125,-.88,1.04)],
        [(0,1,3,2),(2,3,5,4),(0,2,4,6),(1,7,5,3),(6,4,5,7),(0,6,7,1)],'DarkArmorEdge','Head')
    b.mesh('RecessedOpenMouth',[(-.105,-.97,1.043),(.105,-.97,1.043),(.093,-.945,.93),(-.093,-.945,.93),
        (-.09,-.89,1.03),(.09,-.89,1.03),(.08,-.89,.94),(-.08,-.89,.94)],
        [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'Black','Head')
    # Angular mandible wraps under the recessed mouth, with an extended pointed chin.
    for s in [-1,1]:
        b.mesh('DeepCheekPlate',[(s*.153,-.84,1.16),(s*.17,-.92,1.10),(s*.14,-1.035,.95),(s*.105,-.86,.94),
            (s*.115,-.82,1.11)],[(0,1,2,3),(0,4,3),(1,0,4,3,2)],'DarkArmor','Head')
        b.tube('MandibleSide',[(s*.13,-.84,1.02),(s*.135,-.97,.91),(s*.075,-1.10,.88),(0,-1.17,.865)],
            [.032,.036,.027,.001],'DarkArmorEdge','Head',6)
        b.tube('TempleHorn',[(s*.15,-.76,1.24),(s*.29,-.74,1.25),(s*.38,-.70,1.29)],[.037,.028,.001],'DarkArmorEdge','Head',8)
        b.eye('InsetDemonEye',(s*.128,-.962,1.20),.046,'Green','Head',s*.60)
        b.tube('RaisedOrbitalBrow',[(s*.07,-.966,1.24),(s*.12,-.968,1.27),(s*.167,-.924,1.26)],
            [.018,.022,.011],'DarkArmor','Head',7)
        for i in range(3):
            x=s*(.022+i*.032)
            b.tube('UpperMouthTooth',[(x,-1.084,1.046),(x,-1.082,1.016)],[.010,.001],'Ivory','Head',6)
        b.tube('ForeheadEngraving',[(s*.047,-.90,1.37),(s*.055,-.94,1.30),(s*.083,-.965,1.28)],
            [.003]*3,'Rust','Head',5)
    b.mesh('LowerJawFloor',[(-.11,-.88,.93),(.11,-.88,.93),(.095,-1.06,.90),(0,-1.17,.865),(-.095,-1.06,.90)],
        [(0,1,2,3,4)],'DarkArmorEdge','Head')

def revision4(name):
    korean,costFolder,*_=RECIPES[name]
    folder=PROJECT/'Assets/Resources/Digimon'/costFolder/name
    manifest=json.loads((folder/'Source~/manifest.json').read_text(encoding='utf-8'))
    archive=folder/'Source~/BeforeDepthRevision';archive.mkdir(exist_ok=True)
    native=folder/'Source~'/(name+'.blend')
    if not (archive/native.name).exists():shutil.copy2(native,archive/native.name)
    b=RestoredAgumon(name) if name=='Agumon' else RevisedBuilder(name)
    if name=='Agumon':dinosaur(b,name)
    else:deep_face(b)
    report=b.finalize(folder);manifest.update(report);manifest['revision']='head-rollback-and-volumetric-face-4'
    (folder/'Source~/manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    shutil.copy2(PROJECT/'Tools/Blender/revise_depth.py',folder/'Source~/revise_depth.py')
    return {'name':name,'triangles':report['triangles']}
