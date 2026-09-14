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
        b.tube('Pagumon flattened hand ear',[(s*.22,.02,.85),(s*.48,.05,.91),(s*.79,.11,.99),(s*.99,.13,1.12)],[.15,.16,.17,.09],'PaguSkin',arm,32,flat=.28,sub=8)
        for j in range(3):
            b.tube('Pagumon separated ear finger',[(s*(.82+j*.034),.11+(j-1)*.035,1.015),(s*(1.04+j*.030),.13+(j-1)*.03,1.17+(j-1)*.015),(s*(1.06+j*.030),.13+(j-1)*.03,1.18+(j-1)*.015)],[.053,.027,.002],'PaguSkin',arm,20,flat=.6)
    # Muzzle is shallow relief in the same skin color, blended into the sphere.
    for s in [-1,1]:
        outline=bezier_outline([(0,.49),(s*.18,.48),(s*.18,.405),(s*.10,.382),(0,.408)])
        b.patch('Pagumon shallow muzzle lobe',outline,(s*.08,.44),surface,'PaguSkin','Body',.004)
    b.stroke('Pagumon muzzle crease',[(-.19,.449),(-.17,.407),(-.09,.391),(0,.414),(.09,.391),(.17,.407),(.19,.449)],surface,'DarkArmorEdge','Body',.004,.006)
    tongue=bezier_outline([(-.075,.392),(0,.409),(.075,.392),(.052,.367),(0,.36),(-.052,.367)])
    b.patch('Pagumon small red tongue',tongue,(0,.381),surface,'Tongue','Body',.005)

def profile_surface(sections):
    samples=[]
    for i in range(len(sections)-1):
        p0,p1,p2,p3=[sections[max(0,min(len(sections)-1,j))] for j in [i-1,i,i+1,i+2]]
        for j in range(24):
            t=j/24;samples.append([.5*(2*p1[k]+(-p0[k]+p2[k])*t+(2*p0[k]-5*p1[k]+4*p2[k]-p3[k])*t*t+(-p0[k]+3*p1[k]-3*p2[k]+p3[k])*t*t*t) for k in range(4)])
    samples.append(sections[-1]);samples.sort(key=lambda p:p[0])
    def surface(x,z):
        import bisect
        j=max(1,min(len(samples)-1,bisect.bisect_left([p[0] for p in samples],z)))
        a,c=samples[j-1:j+1];t=max(0,min(1,(z-a[0])/max(.00001,c[0]-a[0])))
        _,y,w,d=[a[k]*(1-t)+c[k]*t for k in range(4)]
        return y-d*math.sqrt(max(.01,1-(x/max(.01,w))**2))
    return surface

def gabumon_individual(b):
    b.bone('Body',(0,0,.62));b.bone('Head',(0,0,1.15),'Body');b.bone('Tail',(0,.20,.42),'Body')
    torso=[(.24,0,.13,.12),(.39,0,.24,.21),(.65,0,.31,.25),(.87,0,.24,.21),(1.04,0,.17,.16)]
    b.loft('Gabumon plump yellow torso',torso,'Yellow','Body',72,8)
    face=[(.88,-.09,.15,.14),(.97,-.075,.26,.23),(1.12,-.01,.33,.275),(1.30,.018,.30,.26),(1.43,.035,.19,.18),(1.48,.04,.008,.01)]
    b.loft('Gabumon individually shaped pelt hood',face,'Fur','Head',88,8);sf=profile_surface(face)
    for s in [-1,1]:
        lightning=[(s*.035,1.29),(s*.075,1.41),(s*.16,1.43),(s*.23,1.35),(s*.17,1.33),(s*.11,1.25),(s*.08,1.30)]
        b.patch('Gabumon hood forehead blue marking',lightning,(s*.11,1.34),sf,'WolfBlue','Head',.003)
        b.patch('Gabumon angular cheek marking',[(s*.15,1.13),(s*.28,1.20),(s*.29,1.12),(s*.24,1.08),(s*.28,1.04),(s*.14,1.065)],(s*.23,1.11),sf,'WolfBlue','Head',.003)
        eye=bezier_outline([(s*.10,1.16),(s*.13,1.25),(s*.19,1.27),(s*.235,1.22),(s*.22,1.145),(s*.16,1.135)])
        b.patch('Gabumon dark hood eye contour',eye,(s*.17,1.20),sf,'Outline','Head',.004)
        b.patch('Gabumon ivory hood eye',[(s*.17+(x-s*.17)*.84,1.20+(z-1.20)*.84) for x,z in eye],(s*.17,1.20),sf,'White','Head',.005)
        b.pigment_disc('Gabumon red eye',s*.17,1.192,.040,.056,sf,'EyeRed','Head',.006)
        b.pigment_disc('Gabumon pupil',s*.17,1.192,.021,.040,sf,'EyeDark','Head',.007)
        b.pigment_disc('Gabumon eye reflection',s*.17-.015,1.218,.017,.016,sf,'White','Head',.008)
        # Fur ears are wide and rake outward, unlike the previous upright rabbit ears.
        b.leaf('Gabumon outward pelt ear',[(s*.19,.04,1.40),(s*.34,.07,1.62),(s*.53,.10,1.83)],[.085,.105,.001],'WolfBlue','Head',False,.018)
        b.leaf('Gabumon cream ear tuft',[(s*.21,.015,1.43),(s*.35,.042,1.61),(s*.46,.067,1.74)],[.052,.067,.001],'Fur','Head',False,.012)
        for j in range(7):b.tube('Gabumon hood fur tooth',[(s*(.13+j*.019),sf(s*(.13+j*.019),.965+j*.009)-.004,.965+j*.009),(s*(.13+j*.019),-.25,.92+j*.009)],[.018,.001],'Fur','Head',8,sub=2)
        arm=b.bone('UpperArm.'+('L' if s<0 else 'R'),(s*.24,.015,.99),'Body');hand=b.bone('Hand.'+('L' if s<0 else 'R'),(s*.45,-.02,.35),arm)
        b.tube('Gabumon heavy draped pelt arm',[(s*.24,.055,1.00),(s*.39,.025,.81),(s*.46,0,.54),(s*.44,-.06,.32)],[.145,.16,.15,.16],'Fur',arm,36,flat=.8,sub=9)
        for j in range(4):
            z=.52+j*.12;x=s*(.45-j*.022)
            b.tube('Gabumon pelt zigzag stripe',[(x-s*.12,-.11,z+.045),(x,-.153,z),(x+s*.11,-.105,z+.035)],[.025,.041,.001],'WolfBlue',arm,12,flat=.20)
        for j in range(9):
            x=s*.44+(j-4)*.028;b.tube('Gabumon wrist fur fringe',[(x,-.12,.42),(x+(j-4)*.009,-.13,.28)],[.026,.001],'Fur',hand,10,sub=3)
        for j in range(3):
            x=s*.44+(j-1)*.09;b.tube('Gabumon long magenta hand talon',[(x,-.13,.31),(x+(j-1)*.016,-.22,.21),(x+(j-1)*.02,-.24,.12)],[.047,.030,.001],'Petal',hand,20)
        leg=b.bone('Thigh.'+('L' if s<0 else 'R'),(s*.18,0,.38),'Body')
        b.tube('Gabumon stocky yellow leg',[(s*.19,0,.38),(s*.25,-.02,.19),(s*.26,-.075,.10)],[.105,.11,.12],'Yellow',leg,28)
        for j in range(3):b.tube('Gabumon white toenail',[(s*.26+(j-1)*.063,-.16,.085),(s*.26+(j-1)*.073,-.26,.028)],[.038,.001],'Ivory',leg,16)
    b.oval('Gabumon soft triangular nose',(0,-.315,1.038),(.045,.028,.034),'SteelDark','Head',24,16)
    b.tube('Gabumon yellow chin',[(0,-.12,.91),(0,-.245,.885)],[.125,.10],'Yellow','Head',24,flat=.25)
    b.tube('Gabumon segmented golden horn',[(0,-.045,1.36),(0,-.06,1.55),(0,-.08,1.73)],[.06,.039,.001],'Yellow','Head',20)
    for j in range(6):b.tube('Gabumon horn chevron ridge',[(-.045+j*.004,-.080,1.40+j*.037),(0,-.11+j*.004,1.385+j*.037),(.045-j*.004,-.080,1.40+j*.037)],[.004]*3,'Gold','Head',8)
    belly=profile_surface(torso)
    b.pigment_disc('Gabumon cyan belly marking',0,.58,.19,.19,belly,'BlueSkin','Body',.003)
    # Reference's crest: central dot, arched crown and two scalloped horizontal bands.
    b.pigment_disc('Gabumon pink crest dot',0,.647,.032,.030,belly,'PetalLight','Body',.005)
    b.stroke('Gabumon crest crown',[(-.13,.625),(-.06,.697),(0,.721),(.06,.697),(.13,.625)],belly,'PetalLight','Body',.016,.006)
    for dz in [0,-.09]:b.stroke('Gabumon crest scallop',[(-.145,.57+dz),(-.10,.54+dz),(-.05,.57+dz),(0,.54+dz),(.05,.57+dz),(.10,.54+dz),(.145,.57+dz)],belly,'PetalLight','Body',.015,.006)
    cloth(b,'Gabumon continuous back pelt',1.05,.34,.34,'Fur','Fur','Body',9)
    for j in range(5):b.tube('Gabumon back stripe',[(-.26,.25,.49+j*.095),(0,.32,.46+j*.095),(.26,.25,.49+j*.095)],[.022,.027,.001],'WolfBlue','Body',12,flat=.25)
    b.tube('Gabumon swept yellow tail',[(0,.16,.42),(0,.42,.31),(0,.71,.47),(0,.81,.70)],[.105,.11,.065,.001],'Yellow','Tail',28)

def palmon_individual(b):
    b.bone('Body',(0,0,.57));b.bone('Head',(0,0,1.03),'Body')
    torso=[(.24,0,.12,.11),(.40,0,.17,.13),(.65,0,.13,.11),(.82,0,.105,.10)]
    b.loft('Palmon tapering plant stalk torso',torso,'SeedGreen','Body',56,8)
    head=[(.80,-.03,.10,.10),(.89,-.04,.23,.20),(1.05,-.015,.29,.25),(1.23,.025,.27,.23),(1.31,.04,.16,.14)]
    b.loft('Palmon broad smiling plant head',head,'SeedGreen','Head',80,8);sf=profile_surface(head)
    for s in [-1,1]:
        b.pigment_disc('Palmon green eye outline',s*.127,1.053,.088,.085,sf,'Leaf','Head',.003)
        b.pigment_disc('Palmon emerald eye',s*.127,1.053,.079,.076,sf,'Emerald','Head',.004)
        b.pigment_disc('Palmon eye lower green',s*.127,1.021,.062,.040,sf,'Green','Head',.005)
        b.pigment_disc('Palmon dark pupil',s*.127,1.053,.037,.050,sf,'EyeDark','Head',.006)
        b.pigment_disc('Palmon eye highlight',s*.127-.027,1.083,.027,.018,sf,'White','Head',.007)
        b.pigment_disc('Palmon nostril',s*.030,.966,.010,.006,sf,'Leaf','Head',.003)
        arm=b.bone('UpperArm.'+('L' if s<0 else 'R'),(s*.11,0,.74),'Body');hand=b.bone('Hand.'+('L' if s<0 else 'R'),(s*.35,-.08,.18),arm)
        b.tube('Palmon long flattened plant arm',[(s*.12,0,.75),(s*.19,-.01,.51),(s*.25,-.045,.28),(s*.42,-.08,.17)],[.055,.056,.09,.13],'SeedGreen',arm,32,flat=.55,sub=9)
        for j in range(3):
            y=-.085+(j-1)*.060
            b.tube('Palmon purple finger base',[(s*.39,y,.19),(s*.53,y,.14)],[.055,.05],'PurpleLight',hand,20,flat=.5)
            b.leaf('Palmon long green leaf finger',[(s*.50,y,.15),(s*.67,y-.02,.13),(s*.81,y-.045,.13)],[.05,.054,.001],'Emerald',hand,True,.012)
        leg=b.bone('Thigh.'+('L' if s<0 else 'R'),(s*.09,0,.32),'Body')
        b.tube('Palmon branching root leg',[(s*.085,0,.35),(s*.11,-.04,.17),(s*.16,-.06,.055)],[.052,.065,.08],'SeedGreen',leg,28)
        for j in range(3):b.tube('Palmon spread root toe',[(s*.15+(j-1)*.046,-.06,.08),(s*.15+(j-1)*.077,-.20,.03),(s*.15+(j-1)*.09,-.255,.012)],[.038,.025,.001],'SeedGreen',leg,18)
    smile=bezier_outline([(-.19,.934),(-.09,.91),(0,.90),(.09,.91),(.19,.934),(.11,.865),(0,.85),(-.11,.865)])
    b.patch('Palmon wide curved mouth',smile,(0,.895),sf,'Mouth','Head',.004)
    for s in [-1,1]:b.patch('Palmon small tooth',[(s*.145,.923),(s*.112,.912),(s*.126,.889)],(s*.127,.91),sf,'Ivory','Head',.006,5)
    for i in range(5):
        a=i*math.tau/5+.25
        b.leaf('Palmon broad hanging pink petal',[(0,.025,1.35),(.21*math.cos(a),.19*math.sin(a)+.025,1.38),(.33*math.cos(a),.29*math.sin(a)+.025,1.25),(.34*math.cos(a),.30*math.sin(a)+.025,1.08)],[.04,.17,.13,.001],'Petal','Head',False,.025)
    for i in range(11):
        a=i*math.tau/11;b.leaf('Palmon yellow flower stamen',[(0,.025,1.35),(.07*math.cos(a),.07*math.sin(a)+.025,1.42),(.16*math.cos(a),.15*math.sin(a)+.025,1.36)],[.015,.025,.001],'Yellow','Head',False,.003)
    spiral=[(0,0,1.38),(0,0,1.58)]
    spiral.extend((.10+.10*(1-i/44)*math.cos(math.pi+i*math.tau/32),0,1.68+.10*(1-i/44)*math.sin(math.pi+i*math.tau/32)) for i in range(35))
    b.tube('Palmon orange curled shoot',spiral,[.020]*len(spiral),'SkinOrange','Head',16,sub=2)
    for i in range(7):b.oval('Palmon rear stalk bud',(0,.13,.30+i*.064),(.023,.020,.019),'Leaf','Body',16,10)

def togemon_individual(b):
    b.bone('Body',(0,0,1.0));b.bone('Head',(0,0,1.8),'Body')
    center=(0,0,1.13);radii=(.54,.43,.85)
    body=b.oval('Togemon elongated cactus body',center,radii,'LeafBright','Body',128,96)
    # Recess actual mesh vertices around the three apertures, not black eyeballs.
    for v in body.data.vertices:
        x,y,z=v.co.x,v.co.y,v.co.z+1.13
        if y<0:
            e=min(((x-.205)/.105)**2+((z-1.43)/.13)**2,((x+.205)/.105)**2+((z-1.43)/.13)**2)
            mouth=(x/.083)**2+((z-1.06)/.19)**4
            v.co.y+=.045*max(0,1-min(e,mouth))
    sf=spherical_surface(center,radii)
    for s in [-1,1]:
        b.pigment_disc('Togemon recessed eye rim',s*.205,1.43,.112,.137,sf,'Leaf','Body',.001)
        b.pigment_disc('Togemon dark eye cavity',s*.205,1.43,.088,.115,sf,'Black','Body',.0015)
    mout=bezier_outline([(-.076,1.17),(-.055,1.24),(.055,1.24),(.076,1.17),(.076,.94),(.047,.895),(-.047,.895),(-.076,.94)])
    b.patch('Togemon tall mouth rim',mout,(0,1.07),sf,'Leaf','Body',.001)
    b.patch('Togemon hollow mouth',[(x*.78,1.07+(z-1.07)*.86) for x,z in mout],(0,1.07),sf,'Black','Body',.002)
    for i in range(14):
        a=i*math.tau/14;pts=[]
        for j in range(33):
            t=.07+(math.pi-.14)*j/32;x=.541*math.sin(t)*math.cos(a);y=.431*math.sin(t)*math.sin(a);z=1.13+.85*math.cos(t)
            if y<0 and ((abs(abs(x)-.205)<.12 and abs(z-1.43)<.15) or (abs(x)<.10 and abs(z-1.07)<.20)):continue
            pts.append((x,y,z))
        # Split groove around holes instead of crossing their interior.
        for k in range(len(pts)-1):
            if (Vector(pts[k+1])-Vector(pts[k])).length<.15:b.tube('Togemon longitudinal cactus groove',pts[k:k+2],[.005,.005],'Leaf','Body',8,sub=1)
    random.seed(94)
    for i in range(125):
        a=random.uniform(0,math.tau);t=random.uniform(.25,2.83);x=.54*math.sin(t)*math.cos(a);y=.43*math.sin(t)*math.sin(a);z=1.13+.85*math.cos(t)
        if y<0 and abs(x)<.35 and .84<z<1.60:continue
        p=Vector((x,y,z));d=Vector((x,y,(z-1.13)*.4)).normalized();b.tube('Togemon tapered cactus needle',[p,p+d*.075+Vector((0,0,.017))],[.013,.001],'SeedVein','Body',8,sub=2)
    for s in [-1,1]:
        side='L' if s<0 else 'R';arm=b.bone('UpperArm.'+side,(s*.42,0,1.10),'Body');hand=b.bone('Hand.'+side,(s*.79,-.055,.62),arm)
        b.tube('Togemon diagonal cactus arm',[(s*.43,0,1.12),(s*.63,-.01,.89),(s*.79,-.06,.66)],[.14,.135,.135],'LeafBright',arm,32)
        band(b,'Togemon large glove cuff',(s*.75,-.05,.76),(s*.82,-.07,.61),.19,'Ruby',hand)
        b.oval('Togemon hanging boxing glove',(s*.88,-.07,.40),(.225,.215,.30),'Ruby',hand,48,36)
        b.tube('Togemon curved boxing thumb',[(s*.73,-.22,.54),(s*.70,-.25,.39),(s*.77,-.25,.32)],[.083,.087,.038],'Ruby',hand,24)
        b.tube('Togemon glove stitched seam',[(s*.80,-.273,.64),(s*.85,-.288,.46),(s*.84,-.27,.29)],[.004]*3,'Rose',hand,8)
        leg=b.bone('Thigh.'+side,(s*.25,0,.40),'Body')
        b.tube('Togemon short cactus shin',[(s*.25,0,.41),(s*.28,-.015,.23),(s*.31,-.04,.14)],[.12,.12,.13],'LeafBright',leg,32)
        b.oval('Togemon broad round foot',(s*.31,-.15,.125),(.205,.27,.12),'LeafBright',leg,48,28)
        for j in range(5):b.tube('Togemon foot needle',[(s*.31+(j-2)*.06,-.14,.23),(s*.31+(j-2)*.06,-.14,.29)],[.013,.001],'SeedVein',leg,8)
    for i in range(8):
        a=i*math.tau/8;b.leaf('Togemon pointed yellow crown',[(0,0,1.94),(.12*math.cos(a),.11*math.sin(a),2.10),(.24*math.cos(a),.21*math.sin(a),2.20 if i%2 else 2.08)],[.07,.065,.001],'Yellow','Head',False,.003)

INDIVIDUAL={'Tsunomon':tsunomon,'Tanemon':tanemon,'Pagumon':pagumon,'Gabumon':gabumon_individual,'Palmon':palmon_individual,'Togemon':togemon_individual}

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
