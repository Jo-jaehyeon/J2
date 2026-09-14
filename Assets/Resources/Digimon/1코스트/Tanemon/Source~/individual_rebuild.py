"""Individual reference revisions. Only geometry/export utilities are shared.
No generic character body, head, or eye assemblies from the rejected batch.
"""
from pathlib import Path
_utility=Path('C:/Jerry/Unity Project/J2/Tools/Blender/next_roster.py')
exec(_utility.read_text(encoding='utf-8'))
from mathutils import Matrix
PALETTE.update(SeedGreen=(.66,.77,.23),SeedCream=(.98,.91,.58),SeedVein=(.39,.46,.12),
    TsunoOrange=(1,.53,.045),TsunoWhite=(1,.96,.80),PaguSkin=(.55,.61,.78),
    EyeRed=(.60,.08,.075),EyeDark=(.20,.035,.035),EyeLight=(.85,.22,.17),
    Outline=(.24,.20,.19),Horn=(.28,.35,.39),LeafBright=(.63,.77,.20))

class Individual(Detailed):
    def patch(self,name,outline,center,surface,color,bone,offset=.002,rings=28):
        N=len(outline);cx,cz=center;v=[(cx,surface(cx,cz)-offset,cz)];f=[]
        for j in range(1,rings+1):
            t=j/rings
            for x,z in outline:
                xx=cx*(1-t)+x*t;zz=cz*(1-t)+z*t;v.append((xx,surface(xx,zz)-offset,zz))
        for i in range(N):f.append((0,1+i,1+(i+1)%N))
        for j in range(rings-1):
            for i in range(N):
                k=1+j*N+i;n=1+j*N+(i+1)%N;f.append((k,n,n+N,k+N))
        ob=self.mesh(name,v,f,color,bone)
        # Thin solid backing makes normal recalculation and FBX double-sidedness robust.
        m=ob.modifiers.new('Conformal pigment backing','SOLIDIFY');m.thickness=.0005
        bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=m.name)
        return ob

    def pigment_disc(self,name,x,z,rx,rz,surface,color,bone,offset=.003,tilt=0):
        outline=[(x+rx*math.cos(a),z+rz*math.sin(a)+tilt*math.cos(a)) for a in [i*math.tau/64 for i in range(64)]]
        return self.patch(name,outline,(x,z),surface,color,bone,offset,12)

    def stroke(self,name,points,surface,color,bone,width=.006,offset=.004):
        return self.tube(name,[(x,surface(x,z)-offset,z) for x,z in points],[width]*len(points),color,bone,10,sub=4)

    def leaf(self,name,path,widths,color,bone,veins=True,cup=.045):
        ps=[];ws=[]
        for i in range(len(path)-1):
            p0,p1,p2,p3=[Vector(path[max(0,min(len(path)-1,k))]) for k in [i-1,i,i+1,i+2]]
            for j in range(8):
                t=j/8;ps.append(.5*(2*p1+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t));ws.append(widths[i]*(1-t)+widths[i+1]*t)
        ps.append(Vector(path[-1]));ws.append(widths[-1]);v=[];f=[];M=12
        for i,p in enumerate(ps):
            axis=(ps[min(i+1,len(ps)-1)]-ps[max(i-1,0)]).normalized();side=axis.cross(Vector((0,-1,0))).normalized()
            for j in range(M+1):
                u=j/M*2-1;v.append(p+side*ws[i]*u+Vector((0,cup*u*u,0)))
        for i in range(len(ps)-1):
            for j in range(M):
                k=i*(M+1)+j;f.append((k,k+1,k+M+2,k+M+1))
        ob=self.mesh(name,v,f,color,bone);m=ob.modifiers.new('Leaf thickness','SOLIDIFY');m.thickness=.012;bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=m.name)
        if veins:
            self.tube(name+' central vein',[p+Vector((0,-.012,0)) for p in ps],[.007]*(len(ps)-1)+[.001],'SeedVein',bone,8,sub=1)
            for i in range(6,len(ps)-4,5):
                axis=(ps[i+1]-ps[i-1]).normalized();side=axis.cross(Vector((0,-1,0))).normalized()
                for s in [-1,1]:self.tube(name+' lateral vein',[ps[i]+Vector((0,-.014,0)),ps[min(i+3,len(ps)-1)]+side*ws[i]*s*.80+Vector((0,.015,0))],[.003,.001],'SeedVein',bone,6,sub=2)
        return ob

def spherical_surface(center,radii):
    cx,cy,cz=center;rx,ry,rz=radii
    return lambda x,z:cy-ry*math.sqrt(max(.012,1-((x-cx)/rx)**2-((z-cz)/rz)**2))

def bezier_outline(points,steps=8):
    out=[];N=len(points)
    for i in range(N):
        p0,p1,p2,p3=[Vector(points[k%N]) for k in [i-1,i,i+1,i+2]]
        for j in range(steps):
            t=j/steps;out.append(.5*(2*p1+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
    return [(p.x,p.y) for p in out]

def tsunomon(b):
    b.bone('Body',(0,0,.50));b.bone('Head',(0,0,.88),'Body')
    body=b.oval('Tsunomon spherical orange body',(0,0,.51),(.50,.49,.50),'TsunoOrange','Body',96,64)
    for v in body.data.vertices:v.co.z=max(v.co.z,-.488)
    surface=spherical_surface((0,0,.51),(.50,.49,.50))
    contour=bezier_outline([(-.44,.38),(-.37,.50),(-.35,.68),(-.25,.79),(-.15,.77),(0,.60),(.15,.77),(.25,.79),(.35,.68),(.37,.50),(.44,.38),(.32,.20),(0,.145),(-.32,.20)])
    b.patch('Continuous white face marking',contour,(0,.44),surface,'TsunoWhite','Body')
    for s in [-1,1]:
        cx=s*.212;cz=.573
        # Sloping egg-shaped eye opening, painted on the spherical skin.
        outline=bezier_outline([(cx-s*.075,cz+.07),(cx-s*.028,cz+.115),(cx+s*.037,cz+.093),(cx+s*.076,cz+.015),(cx+s*.052,cz-.075),(cx-s*.054,cz-.073)])
        b.patch('Tsunomon eye contour',outline,(cx,cz),surface,'Outline','Body',.003)
        inner=[(cx+(x-cx)*.89,cz+(z-cz)*.88) for x,z in outline]
        b.patch('Tsunomon white sclera',inner,(cx,cz),surface,'White','Body',.004)
        b.pigment_disc('Tsunomon crimson iris',cx+s*.004,cz-.017,.052,.071,surface,'EyeRed','Body',.005)
        b.pigment_disc('Tsunomon lower iris light',cx,cz-.043,.043,.033,surface,'EyeLight','Body',.006)
        b.pigment_disc('Tsunomon dark pupil',cx,cz-.01,.027,.040,surface,'EyeDark','Body',.007)
        b.pigment_disc('Tsunomon eye reflection',cx-.024,cz+.036,.025,.020,surface,'White','Body',.008)
        b.stroke('Tsunomon upper eyelash',[(cx-s*.071,cz+.078),(cx,cz+.11),(cx+s*.064,cz+.045)],surface,'Outline','Body',.004,.006)
    mouth=bezier_outline([(-.12,.287),(-.06,.269),(0,.279),(.06,.269),(.12,.287),(.075,.228),(0,.217),(-.075,.228)])
    b.patch('Tsunomon small mouth',mouth,(0,.251),surface,'Mouth','Body',.004)
    b.pigment_disc('Tsunomon tongue',0,.237,.057,.017,surface,'EyeLight','Body',.006)
    for s in [-1,1]:b.patch('Tsunomon tiny fang',[(s*.08,.28),(s*.045,.269),(s*.067,.234)],(s*.064,.261),surface,'White','Body',.008,4)
    b.tube('Tsunomon curved steel horn',[(0,.06,.97),(0,.11,1.18),(0,.17,1.45),(0,.18,1.68)],[.12,.088,.047,.001],'Horn','Head',24,sub=8)
    for i in range(9):
        a=i*math.tau/9;b.tube('Tsunomon horn root tooth',[(.10*math.cos(a),.065+.095*math.sin(a),1.01),(.11*math.cos(a),.065+.10*math.sin(a),.93)],[.029,.001],'Horn','Head',8,sub=2)

def tanemon(b):
    b.bone('Body',(0,0,.58));b.bone('Head',(0,.015,1.0),'Body')
    b.oval('Tanemon spherical seed',(0,0,.58),(.47,.465,.49),'SeedGreen','Body',96,64)
    surface=spherical_surface((0,0,.58),(.47,.465,.49))
    outline=bezier_outline([(-.42,.53),(-.37,.72),(-.25,.85),(-.13,.82),(0,.70),(.13,.82),(.25,.85),(.37,.72),(.42,.53),(.33,.30),(0,.21),(-.33,.30)])
    b.patch('Tanemon cream heart mask',outline,(0,.54),surface,'SeedCream','Body')
    for s in [-1,1]:
        x=s*.21;z=.61
        b.pigment_disc('Tanemon integrated red eye',x,z,.095,.133,surface,'EyeRed','Body',.003,tilt=s*.036)
        b.pigment_disc('Tanemon lower iris gradient',x,z-.058,.067,.055,surface,'EyeLight','Body',.004,tilt=s*.018)
        b.pigment_disc('Tanemon deep pupil',x+s*.006,z-.012,.046,.066,surface,'EyeDark','Body',.005,tilt=s*.015)
        b.pigment_disc('Tanemon large shine',x-.030,z+.059,.029,.029,surface,'White','Body',.006)
        b.pigment_disc('Tanemon small shine',x+.031,z-.042,.013,.012,surface,'PinkLight','Body',.006)
    b.stroke('Tanemon w smile',[(-.14,.374),(-.10,.348),(-.04,.343),(0,.365),(.04,.343),(.10,.348),(.14,.374)],surface,'SeedVein','Body',.004,.004)
    for s in [-1,1]:
        for y in [-.23,.24]:
            root=b.bone('Leg'+str(s)+str(y),(s*.28,y,.25),'Body')
            b.tube('Tanemon soft root foot',[(s*.26,y,.25),(s*.35,y-.045,.16),(s*.40,y-.09,.10)],[.09,.10,.11],'SeedCream',root,24,flat=.60)
            for j in range(3):b.tube('Tanemon grey root tip',[(s*.40+(j-1)*.062,y-.17,.09),(s*.40+(j-1)*.074,y-.245,.055)],[.032,.001],'Silver',root,12,sub=3)
        leaf=b.bone('Antenna.'+str(s),(0,0,1.0),'Head')
        b.leaf('Tanemon recurved seed leaf',[(0,0,1.0),(s*.13,.015,1.29),(s*.32,.025,1.47),(s*.53,.01,1.39),(s*.57,-.03,1.17),(s*.49,-.065,1.08)],[.045,.09,.15,.16,.10,.001],'LeafBright',leaf,True,.045)

def pagumon(b):
    b.bone('Body',(0,0,.50));b.bone('Head',(0,0,.80),'Body')
    # Nearly equal radii, with only the reference's slight lower cheek fullness.
    body=b.oval('Pagumon round body',(0,0,.52),(.48,.47,.51),'PaguSkin','Body',96,64)
    for v in body.data.vertices:
        if v.co.z<0:v.co.x*=1+.025*(-v.co.z/.51)
    surface=spherical_surface((0,0,.52),(.48,.47,.51))
    for s in [-1,1]:
        eye=bezier_outline([(s*.085,.68),(s*.15,.80),(s*.22,.82),(s*.30,.766),(s*.22,.763),(s*.15,.725)])
        b.patch('Pagumon wicked eye outline',eye,(s*.191,.767),surface,'Outline','Body',.003)
        eye2=[(s*.191+(x-s*.191)*.87,.767+(z-.767)*.85) for x,z in eye]
        b.patch('Pagumon coral eye aperture',eye2,(s*.191,.767),surface,'EyeLight','Body',.004)
        b.pigment_disc('Pagumon tiny gold iris',s*.174,.770,.022,.027,surface,'Yellow','Body',.005)
        b.pigment_disc('Pagumon tiny pupil',s*.174,.770,.010,.016,surface,'EyeDark','Body',.006)
        arm=b.bone('UpperArm.'+('L' if s<0 else 'R'),(s*.30,.04,.90),'Body')
        # Flattened broad hand-ear lies outward, not a raised tubular handle.
        b.tube('Pagumon flattened hand ear',[(s*.29,.03,.92),(s*.56,.07,.91),(s*.79,.11,.99),(s*.99,.13,1.12)],[.13,.16,.17,.09],'PaguSkin',arm,32,flat=.28,sub=8)
        for j in range(3):
            b.tube('Pagumon separated ear finger',[(s*(.82+j*.034),.11+(j-1)*.035,1.015),(s*(1.04+j*.030),.13+(j-1)*.03,1.17+(j-1)*.015)],[.053,.027],'PaguSkin',arm,20,flat=.6)
    # Muzzle is shallow relief in the same skin color, blended into the sphere.
    for s in [-1,1]:
        outline=bezier_outline([(0,.49),(s*.18,.48),(s*.18,.405),(s*.10,.382),(0,.408)])
        b.patch('Pagumon shallow muzzle lobe',outline,(s*.08,.44),surface,'PaguSkin','Body',.004)
    b.stroke('Pagumon muzzle crease',[(-.19,.449),(-.17,.407),(-.09,.391),(0,.414),(.09,.391),(.17,.407),(.19,.449)],surface,'DarkArmorEdge','Body',.004,.006)
    tongue=bezier_outline([(-.075,.392),(0,.409),(.075,.392),(.052,.367),(0,.36),(-.052,.367)])
    b.patch('Pagumon small red tongue',tongue,(0,.381),surface,'Tongue','Body',.005)

INDIVIDUAL={'Tsunomon':tsunomon,'Tanemon':tanemon,'Pagumon':pagumon}

def rebuild(name):
    korean,costFolder,h,w,d,_=NEW[name];folder=PROJECT/'Assets/Resources/Digimon'/costFolder/name
    old=json.loads((folder/'Source~'/'manifest.json').read_text(encoding='utf-8'))
    refs=[]
    for r in old['references']:
        candidate=Path('C:/Users/User/Desktop/J2_AssetImage/confirm')/Path(r['path']).name
        if not candidate.exists() or hashlib.sha256(candidate.read_bytes()).hexdigest()!=r['sha256']:raise RuntimeError('Reference hash mismatch '+str(candidate))
        refs.append({'path':str(candidate),'sha256':r['sha256']})
    b=Individual(name);INDIVIDUAL[name](b);report=b.finalize(folder)
    for m in report['materials']:m['vertexColor']=True
    report.update(name=name,korean=korean,costFolder=costFolder,targetHeight=h,maxWidth=w,maxDepth=d,clips=old['clips'],references=refs,revision='individual-reference-rebuild')
    (folder/'Source~'/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    (folder/'Source~'/'individual_rebuild.py').write_text(Path('C:/Jerry/Unity Project/J2/Tools/Blender/individual_rebuild.py').read_text(encoding='utf-8'),encoding='utf-8')
    return {'name':name,'triangles':report['triangles'],'bones':report['bones']}
