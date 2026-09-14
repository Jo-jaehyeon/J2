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

def painted(b,ob,color_function):
    a=ob.data.color_attributes.get('ReferenceColor') or ob.data.color_attributes.new(name='ReferenceColor',type='FLOAT_COLOR',domain='CORNER')
    for loop in ob.data.loops:a.data[loop.index].color=(*color_function(ob.matrix_world@ob.data.vertices[loop.vertex_index].co),1)

def fur_blade(b,name,start,mid,end,width,bone):
    ob=b.leaf(name,[start,mid,end],[width*.35,width,.001],'Fur',bone,False,.018)
    axis=Vector(end)-Vector(start);length=axis.length
    painted(b,ob,lambda p:PALETTE['WolfBlue'] if (p-Vector(start)).dot(axis.normalized())>length*.75 else PALETTE['Fur'])

def garurumon_individual(b):
    b.bone('Body',(0,.02,.85));b.bone('Head',(0,-.63,.83),'Body');b.bone('Tail',(0,.65,.95),'Body')
    body=b.loft('Garurumon sloping chest and tucked belly',[(.42,-.20,.12,.28),(.58,-.13,.24,.53),(.78,.04,.29,.68),(.97,.05,.27,.65),(1.06,.055,.15,.51),(1.075,.05,.02,.12)],'Fur','Body',96,8)
    def stripes(p):
        nearest=min(abs(p.y-y-.048*math.sin(p.z*13+abs(p.x)*3)) for y in [-.40,-.12,.18,.49])
        width=.047*max(.2,(p.z-.43)/.60)
        return PALETTE['WolfBlue'] if nearest<width else PALETTE['Fur']
    painted(b,body,stripes)
    skull=[(.59,-.69,.09,.12),(.69,-.71,.21,.22),(.82,-.66,.245,.24),(.94,-.62,.19,.19),(1.015,-.58,.08,.08)]
    b.loft('Garurumon low broad wolf skull',skull,'Fur','Head',72,8);sf=profile_surface(skull)
    b.tube('Garurumon long upper snout',[(0,-.70,.78),(0,-.96,.745),(0,-1.16,.70)],[.17,.135,.105],'Fur','Head',36,flat=.64,sub=8)
    b.tube('Garurumon substantial lower jaw',[(0,-.71,.67),(0,-.96,.585),(0,-1.14,.61)],[.13,.105,.085],'Fur','Head',32,flat=.50)
    b.oval('Garurumon black nose',(0,-1.19,.718),(.086,.037,.055),'Black','Head',32,20)
    b.oval('Garurumon mouth interior',(0,-.94,.651),(.126,.20,.040),'Mouth','Head',40,24)
    for s in [-1,1]:
        b.patch('Garurumon blue eye mask',[(s*.075,.92),(s*.185,.94),(s*.229,.86),(s*.18,.825),(s*.10,.85)],(s*.16,.88),sf,'WolfBlue','Head',.003)
        eye=bezier_outline([(s*.11,.874),(s*.15,.906),(s*.20,.91),(s*.212,.87),(s*.18,.85),(s*.14,.853)])
        b.patch('Garurumon almond sclera',eye,(s*.167,.88),sf,'Ivory','Head',.004)
        b.pigment_disc('Garurumon amber iris',s*.171,.881,.021,.028,sf,'Gold','Head',.005)
        b.pigment_disc('Garurumon slit pupil',s*.171,.882,.009,.020,sf,'Black','Head',.006)
        for i in range(5):
            y=-.79-i*.076;x=s*(.126-i*.005)
            b.tube('Garurumon upper carnassial',[(x,y,.700-i*.006),(x,y-.004,.64-i*.002)],[.021,.001],'Ivory','Head',12,sub=2)
            b.tube('Garurumon lower tooth',[(x*.85,y,.604),(x*.85,y,.636)],[.014,.001],'Ivory','Head',10,sub=2)
        fur_blade(b,'Garurumon long blue tipped ear',(s*.10,-.56,.96),(s*.19,-.48,1.19),(s*.28,-.43,1.39),.075,'Head')
        for j in range(4):fur_blade(b,'Garurumon swept cheek fur',(s*.18,-.57,.87-j*.048),(s*.31,-.52,.83-j*.055),(s*.40,-.45,.69-j*.043),.060,'Head')
        for y in [-.36,.49]:
            for j in range(5):fur_blade(b,'Garurumon rising shoulder hip mane',(s*.23,y+(j-2)*.054,.95),(s*(.31+j*.025),y+.07,1.17),(s*(.34+j*.047),y+.13,1.34+(j%2)*.12),.063,'Body')
        for front in [True,False]:
            y=-.42 if front else .51;bone=b.bone('Leg'+str(front)+str(s),(s*.23,y,.90),'Body');knee=b.bone('Knee'+str(front)+str(s),(s*.31,y+(.06 if front else -.21),.49),bone)
            pts=[(s*.23,y,.93),(s*.30,y+(.07 if front else -.21),.65),(s*.32,y+(.11 if front else -.25),.44),(s*.34,y+(-.14 if front else .07),.20),(s*.34,y-.27,.095)]
            leg=b.tube('Garurumon crouching articulated leg',pts,[.15,.125,.09,.073,.12],'Fur',bone,32,sub=7)
            painted(b,leg,lambda p:PALETTE['WolfBlue'] if math.sin(p.z*23+p.y*5)>.78 else PALETTE['Fur'])
            group=leg.vertex_groups.new(name=knee)
            for v in leg.data.vertices:
                w=max(0,min(1,(.55-v.co.z)/.16));group.add([v.index],w,'REPLACE');leg.vertex_groups[bone].add([v.index],1-w,'REPLACE')
            b.oval('Garurumon oversized padded paw',(s*.34,y-.29,.103),(.165,.23,.10),'Fur',knee,44,28)
            for j in range(3):b.tube('Garurumon wide magenta claw',[(s*.34+(j-1)*.093,y-.44,.09),(s*.34+(j-1)*.102,y-.56,.035)],[.046,.001],'Purple',knee,16,flat=.7)
    tail=b.tube('Garurumon long rising tail',[(0,.61,.98),(0,.97,1.04),(0,1.31,1.25),(0,1.52,1.58),(0,1.54,1.92)],[.07,.054,.045,.095,.001],'Fur','Tail',28,sub=8)
    painted(b,tail,lambda p:PALETTE['WolfBlue'] if math.sin(p.z*20+p.y*4)>.4 else PALETTE['Fur'])

def metalgarurumon_individual(b):
    b.bone('Body',(0,.05,.72));b.bone('Head',(0,-.60,.83),'Body');b.bone('Tail',(0,.60,.87),'Body')
    b.loft('MetalGarurumon ribbed low chassis',[(.46,0,.16,.40),(.62,.035,.25,.60),(.82,.05,.28,.62),(.96,.05,.20,.50),(1.0,.05,.04,.24)],'SteelDark','Body',64,7)
    for j in range(8):
        y=.10+j*.048;b.tube('MetalGarurumon flexible belly rib',[(-.205,y,.64),(0,y,.51),(.205,y,.64)],[.022]*3,'Silver','Body',12)
    b.loft('MetalGarurumon golden underbody plate',[(.47,-.28,.12,.17),(.53,-.26,.22,.23),(.69,-.27,.22,.22)],'Gold','Body',32,4)
    back=b.loft('MetalGarurumon exposed white back',[(.83,-.08,.21,.43),(.98,-.06,.19,.37),(1.02,-.04,.035,.15)],'Fur','Body',64,6)
    painted(b,back,lambda p:PALETTE['WolfBlue'] if abs(math.sin(p.y*15+p.x*2))>.94 else PALETTE['Fur'])
    skull=[(.61,-.67,.10,.15),(.71,-.70,.20,.22),(.84,-.63,.24,.23),(.98,-.57,.20,.19),(1.045,-.53,.06,.08)]
    b.loft('MetalGarurumon helmet base',skull,'Suit','Head',56,7);sf=profile_surface(skull)
    b.tube('MetalGarurumon armored long muzzle',[(0,-.75,.77),(0,-1.0,.72),(0,-1.17,.67)],[.15,.13,.115],'Suit','Head',16,flat=.75,sub=5)
    b.tube('MetalGarurumon pale lower jaw',[(0,-.69,.64),(0,-.96,.58),(0,-1.13,.59)],[.12,.103,.082],'Fur','Head',24,flat=.40)
    b.oval('MetalGarurumon red sensor nose',(0,-1.206,.693),(.083,.035,.049),'HairRed','Head',28,18)
    for s in [-1,1]:
        eye=[(s*.09,.874),(s*.19,.917),(s*.226,.865),(s*.17,.838)]
        b.patch('MetalGarurumon recessed eye well',eye,(s*.17,.873),sf,'Black','Head',.003)
        b.patch('MetalGarurumon red optics',[(s*.17+(x-s*.17)*.71,.873+(z-.873)*.66) for x,z in eye],(s*.17,.873),sf,'HairRed','Head',.004)
        b.pigment_disc('MetalGarurumon optic glint',s*.16,.88,.009,.022,sf,'Ivory','Head',.005)
        b.plate('MetalGarurumon rigid antenna ear',[(s*.10,1.0),(s*.235,.99),(s*.26,1.49)],-.54,.075,'Suit','Head',.01)
        b.tube('MetalGarurumon antenna silver edge',[(s*.12,-.548,1.02),(s*.26,-.548,1.49)],[.008,.001],'Silver','Head',8)
        b.patch('MetalGarurumon forehead layered plate',[(s*.02,1.02),(s*.11,1.035),(s*.205,.96),(s*.19,.92),(s*.055,.95)],(s*.10,.98),sf,'BlueSkin','Head',.007)
        for j in range(3):b.stroke('MetalGarurumon helmet seam',[(s*.05,.99-j*.058),(s*.14,.98-j*.058),(s*.225,.91-j*.050)],sf,'DarkArmor','Head',.004,.011)
        b.tube('MetalGarurumon angular cheek guard',[(s*.17,-.67,.70),(s*.29,-.60,.74),(s*.34,-.44,.88)],[.060,.073,.001],'Suit','Head',8,flat=.40)
        for front in [True,False]:
            y=-.38 if front else .48;leg=b.bone('Leg'+str(front)+str(s),(s*.23,y,.83),'Body');knee=b.bone('Knee'+str(front)+str(s),(s*.34,y+(.08 if front else -.15),.48),leg)
            pts=[(s*.23,y,.86),(s*.31,y+(.08 if front else -.15),.62),(s*.34,y+(.10 if front else -.14),.45),(s*.35,y-.17,.19)]
            b.tube('MetalGarurumon segmented armored upper leg',pts[:3],[.17,.15,.105],'Suit',leg,12,flat=.85,sub=3)
            b.tube('MetalGarurumon angular shin armor',pts[2:],[.11,.13],'Suit',knee,12,sub=3)
            for z,yy,r in [(.74,y,.13),(.43,pts[2][1],.08)]:
                b.oval('MetalGarurumon round exposed joint',(s*.40,yy,z),(.025,r,r),'BlueSkin',leg if z>.5 else knee,40,24)
                b.oval('MetalGarurumon joint inner inset',(s*.424,yy,z),(.005,r*.74,r*.74),'Suit',leg if z>.5 else knee,32,20)
            paw=b.oval('MetalGarurumon broad white paw',(s*.35,y-.24,.10),(.18,.25,.10),'Fur',knee,40,24)
            painted(b,paw,lambda p:PALETTE['WolfBlue'] if math.sin(p.y*20+p.x*4)>.85 else PALETTE['Fur'])
            for j in range(3):b.tube('MetalGarurumon orange metal toe',[(s*.35+(j-1)*.104,y-.42,.08),(s*.35+(j-1)*.11,y-.54,.035)],[.050,.001],'HairRed',knee,12)
            for j in range(3):b.oval('MetalGarurumon armor rivet',(s*.39,y-.07+j*.065,.86),(.011,.012,.012),'Silver',leg,12,8)
        b.oval('MetalGarurumon rear armor red panel',(s*.285,.54,.82),(.14,.19,.135),'HairRed','Body',24,16)
        b.tube('MetalGarurumon gold missile housing',[(s*.34,-.23,1.02),(s*.34,-.54,1.02)],[.125,.125],'Gold','Body',8,flat=.85,sub=1)
        for j in range(3):b.tube('MetalGarurumon visible missile',[(s*.34+(j-1)*.073,-.56,1.02),(s*.34+(j-1)*.073,-.72,1.02)],[.032,.001],'Silver','Body',12)
        wing=b.bone('Wing.'+('L' if s<0 else 'R'),(s*.24,-.15,.99),'Body')
        b.tube('MetalGarurumon angular gold wing spar',[(s*.25,-.13,1.00),(s*.56,.02,1.20),(s*.78,.13,1.60)],[.055,.066,.037],'Gold',wing,6,flat=.65,sub=1)
        b.plate('MetalGarurumon wide energy blade',[(s*.56,1.22),(s*.86,1.46),(s*1.15,1.94),(s*.70,1.62)],.10,.015,'Yellow',wing,.006)
        b.plate('MetalGarurumon second energy blade',[(s*.41,1.15),(s*.56,1.29),(s*.52,1.72),(s*.35,1.43)],.22,.012,'Yellow',wing,.004)
        for j in range(4):b.tube('MetalGarurumon wing hinge engraving',[(s*(.43+j*.04),-.02,1.12+j*.06),(s*(.49+j*.04),-.015,1.10+j*.06)],[.003,.003],'Leather',wing,6,sub=1)
    # Rear blade lies in the sagittal plane, and its holes are real perforations.
    blade=b.plate('MetalGarurumon curved tail knife',[(0,.0),(.1,.0),(.2,.0)],0,.01,'Gold','Tail') if False else b.mesh('MetalGarurumon tail blade',[(x,y,z) for x in [-.023,.023] for y,z in [(.61,.88),(.90,.92),(1.36,1.08),(1.24,.88),(.91,.79)]],[(0,1,2,3,4),(9,8,7,6,5)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],'Gold','Tail')
    for y,z in [(1.04,.925),(1.16,.967),(1.27,1.003)]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.023,depth=.14,location=(0,y,z),rotation=(0,math.pi/2,0));cut=bpy.context.object
        bpy.context.view_layer.objects.active=blade;mod=blade.modifiers.new('Tail blade perforation','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cut;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
    blade.vertex_groups['Tail'].add(list(range(len(blade.data.vertices))),1,'REPLACE')
    blade.data.materials.clear();blade.data.materials.append(b.mat('Gold'))
    for p in blade.data.polygons:p.material_index=0

def weregarurumon_individual(b):
    b.bone('Body',(0,0,1.12));b.bone('Head',(0,-.085,1.83),'Body');b.bone('Tail',(0,.10,1.05),'Body')
    body=b.loft('WereGarurumon broad stooping torso',[(.98,.015,.18,.14),(1.15,0,.20,.145),(1.33,-.025,.17,.12),(1.55,-.045,.30,.19),(1.71,-.05,.34,.17),(1.81,-.07,.14,.11)],'Fur','Body',80,8)
    painted(b,body,lambda p:PALETTE['WolfBlue'] if (abs(p.x)>.15 and math.sin(p.z*22+abs(p.x)*4)>.80) else PALETTE['Fur'])
    for s in [-1,1]:
        b.oval('WereGarurumon pectoral',(s*.145,-.182,1.60),(.15,.067,.10),'Fur','Body',36,24)
        for j in range(3):b.oval('WereGarurumon defined abdominal',(s*.073,-.132,1.30+j*.072),(.071,.030,.042),'Fur','Body',28,18)
    skull=[(1.69,-.12,.12,.13),(1.79,-.14,.225,.20),(1.95,-.10,.235,.225),(2.08,-.05,.15,.16),(2.14,-.03,.045,.06)]
    b.loft('WereGarurumon angular wolf head',skull,'Fur','Head',72,8);sf=profile_surface(skull)
    b.tube('WereGarurumon long square snout',[(0,-.20,1.88),(0,-.40,1.84),(0,-.56,1.80)],[.16,.14,.11],'Fur','Head',24,flat=.62)
    b.tube('WereGarurumon wide open lower jaw',[(0,-.12,1.75),(0,-.35,1.60),(0,-.51,1.62)],[.11,.10,.08],'Fur','Head',28,flat=.5)
    b.oval('WereGarurumon mouth cavity',(0,-.32,1.725),(.133,.18,.080),'Mouth','Head',36,24)
    b.oval('WereGarurumon black nose',(0,-.583,1.822),(.089,.03,.049),'Black','Head',28,18)
    b.oval('WereGarurumon visible tongue',(0,-.395,1.658),(.068,.081,.016),'Tongue','Head',28,16)
    for s in [-1,1]:
        eye=[(s*.075,1.97),(s*.125,2.015),(s*.199,2.0),(s*.215,1.939),(s*.137,1.923)]
        b.patch('WereGarurumon angular blue brow mask',eye,(s*.145,1.969),sf,'WolfBlue','Head',.003)
        inner=[(s*.145+(x-s*.145)*.68,1.96+(z-1.969)*.51) for x,z in eye]
        b.patch('WereGarurumon fierce amber eye',inner,(s*.145,1.96),sf,'Gold','Head',.004)
        b.pigment_disc('WereGarurumon vertical pupil',s*.145,1.964,.010,.022,sf,'Black','Head',.005)
        for j in range(5):
            y=-.23-j*.065;b.tube('WereGarurumon upper fang',[(s*.12,y,1.79),(s*.11,y-.013,1.71-(.045 if j in [1,3] else 0))],[.022,.001],'Ivory','Head',12)
            b.tube('WereGarurumon lower fang',[(s*.09,y,1.639),(s*.09,y,1.68)],[.015,.001],'Ivory','Head',10)
        fur_blade(b,'WereGarurumon tall tapered ear',(s*.115,.015,2.055),(s*.235,.04,2.30),(s*.31,.07,2.51),.090,'Head')
        for j in range(5):fur_blade(b,'WereGarurumon layered cheek locks',(s*.19,-.015,1.94-j*.045),(s*.33,.015,1.98-j*.055),(s*.43,.06,1.87-j*.065),.061,'Head')
        for j in range(5):fur_blade(b,'WereGarurumon swept shoulder quills',(s*.26,.04,1.74),(s*(.36+j*.022),.10+(j-2)*.06,1.91),(s*(.43+j*.035),.16+(j-2)*.075,2.05),.075,'Body')
        side='L' if s<0 else 'R';upper=b.bone('UpperArm.'+side,(s*.30,-.02,1.68),'Body');fore=b.bone('Forearm.'+side,(s*.40,-.02,1.38),upper);hand=b.bone('Hand.'+side,(s*.45,-.09,1.03),fore)
        b.tube('WereGarurumon thick upper arm',[(s*.31,-.01,1.69),(s*.38,-.015,1.54),(s*.40,-.02,1.38)],[.15,.145,.091],'Denim' if s>0 else 'Fur',upper,36)
        b.tube('WereGarurumon muscular forearm',[(s*.40,-.02,1.39),(s*.43,-.055,1.22),(s*.45,-.09,1.04)],[.10,.127,.10],'Denim' if s>0 else 'Fur',fore,36)
        b.oval('WereGarurumon large claw palm',(s*.45,-.10,.965),(.13,.095,.13),'Fur',hand,36,24)
        for j in range(4):
            x=s*.45+(j-1.5)*.068;b.tube('WereGarurumon articulated finger',[(x,-.10,.94),(x,-.15,.84),(x,-.19,.83)],[.040,.034,.022],'Fur',hand,18)
            b.tube('WereGarurumon magenta fingernail',[(x,-.18,.845),(x,-.24,.79)],[.024,.001],'Purple',hand,12)
        b.tube('WereGarurumon opposed thumb',[(s*.34,-.12,.99),(s*.31,-.18,.91)],[.056,.030],'Fur',hand,20)
        band(b,'WereGarurumon leather wrist straps',(s*.44,-.08,1.13),(s*.45,-.09,1.03),.117,'Leather',fore)
        thigh=b.bone('Thigh.'+side,(s*.14,0,1.08),'Body');shin=b.bone('Shin.'+side,(s*.22,-.105,.65),thigh);foot=b.bone('Foot.'+side,(s*.24,.02,.22),shin)
        b.tube('WereGarurumon baggy bent jean thigh',[(s*.14,0,1.08),(s*.20,-.025,.88),(s*.22,-.105,.65)],[.14,.16,.11],'Denim',thigh,36)
        b.tube('WereGarurumon digitigrade jean calf',[(s*.22,-.105,.66),(s*.23,.07,.41),(s*.24,.02,.23)],[.115,.085,.069],'Denim',shin,32)
        b.oval('WereGarurumon broad wolf foot',(s*.24,-.095,.12),(.16,.24,.11),'Fur',foot,40,28)
        for j in range(3):b.tube('WereGarurumon foot talon',[(s*.24+(j-1)*.09,-.28,.10),(s*.24+(j-1)*.10,-.39,.035)],[.046,.001],'Purple',foot,14)
        b.oval('WereGarurumon leather knee guard',(s*.22,-.209,.64),(.105,.035,.12),'Leather',shin,28,18)
        b.tube('WereGarurumon jean seam',[(s*.26,.04,1.02),(s*.34,-.015,.86),(s*.31,-.07,.67)],[.005]*3,'Silver',thigh,8)
        for j in range(3):b.tube('WereGarurumon jean knee fold',[(s*.22-.07,-.18,.76+j*.035),(s*.22,-.204,.745+j*.035),(s*.22+.075,-.17,.77+j*.035)],[.004]*3,'DarkArmor',thigh,8)
        for j in range(4):band(b,'WereGarurumon ankle wrap',(s*.24,.00,.20+j*.027),(s*.24,.00,.223+j*.027),.084,'Cream' if s<0 else 'Leather',foot)
    b.oval('WereGarurumon single heavy pauldron',(.31,0,1.70),(.20,.20,.14),'Leather','UpperArm.R',32,20)
    b.tube('WereGarurumon diagonal leather harness',[(-.24,-.175,1.79),(0,-.231,1.51),(.16,-.147,1.18)],[.048]*3,'Leather','Body',12,flat=.22)
    band(b,'WereGarurumon waist belt',(0,0,1.04),(0,0,1.14),.23,'Leather','Body')
    b.plate('WereGarurumon large buckle',[(-.05,1.045),(.05,1.045),(.05,1.135),(-.05,1.135)],-.24,.018,'Silver','Body',.007)
    skull(b,(-.19,-.175,.88),.049,'Thigh.L') if False else None
    b.oval('WereGarurumon jean skull logo',(-.20,-.171,.89),(.052,.008,.058),'Ivory','Thigh.L',24,16)
    for s in [-1,1]:b.pigment_disc('WereGarurumon skull socket',-.20+s*.018,.90,.011,.014,lambda x,z:-.180,'Black','Thigh.L',.001)
    for j in range(6):b.oval('WereGarurumon silver wrist stud',(-.45+(j-2.5)*.035,-.207,1.08),(.013,.009,.013),'Silver','Forearm.L',16,10)
    for j in range(3):b.tube('WereGarurumon knee spike',[(.22+(j-1)*.05,-.245,.64),(.22+(j-1)*.05,-.34,.68)],[.020,.001],'Silver','Shin.R',12)
    b.tube('WereGarurumon full flowing tail',[(0,.12,1.09),(0,.48,1.02),(0,.75,.83),(0,.94,.61)],[.10,.12,.08,.001],'Fur','Tail',32)

def demidevimon_individual(b):
    b.bone('Body',(0,0,.73));b.bone('Head',(0,0,.89),'Body')
    b.oval('DemiDevimon spherical bat head body',(0,0,.74),(.35,.33,.34),'BlueSkin','Body',96,64);sf=spherical_surface((0,0,.74),(.35,.33,.34))
    for s in [-1,1]:
        eye=bezier_outline([(s*.05,.855),(s*.17,.88),(s*.255,.84),(s*.235,.74),(s*.12,.73),(s*.065,.78)])
        b.patch('DemiDevimon angular dark eye socket',eye,(s*.15,.805),sf,'DarkArmor','Body',.003)
        b.pigment_disc('DemiDevimon gold eye',s*.16,.803,.065,.065,sf,'Gold','Body',.004)
        b.pigment_disc('DemiDevimon black vertical pupil',s*.16,.805,.029,.046,sf,'Black','Body',.005)
        b.pigment_disc('DemiDevimon eye glint',s*.16-.023,.831,.017,.022,sf,'White','Body',.006)
        b.tube('DemiDevimon crooked bat antenna',[(s*.22,.02,1.0),(s*.29,.01,1.21),(s*.25,.025,1.37)],[.053,.038,.001],'BlueSkin','Head',16,flat=.6)
        wing=b.bone('Wing.'+('L' if s<0 else 'R'),(s*.28,.055,.91),'Body')
        batwing(b,(s*.28,.055,.90),[(s*.39,.075,1.15),(s*.85,.12,1.23),(s*.78,.10,.88),(s*.63,.095,.68),(s*.37,.065,.69)],wing,'DarkArmor','WolfBlue')
        batwing(b,(s*.27,.07,.69),[(s*.47,.09,.75),(s*.60,.12,.47),(s*.40,.09,.41),(s*.27,.07,.57)],wing,'DarkArmor','WolfBlue')
        leg=b.bone('Leg'+str(s),(s*.13,0,.49),'Body')
        b.tube('DemiDevimon long thin pale shin',[(s*.13,0,.47),(s*.17,-.01,.22),(s*.19,-.05,.13)],[.030,.025,.032],'Pale',leg,20)
        for j in range(3):b.tube('DemiDevimon curled red talon',[(s*.19+(j-1)*.060,-.045,.13),(s*.19+(j-1)*.089,-.17,.07),(s*.19+(j-1)*.095,-.23,.02)],[.028,.019,.001],'Ruby',leg,16)
    outline=bezier_outline([(-.21,.644),(-.10,.603),(0,.592),(.10,.603),(.21,.644),(.13,.54),(0,.515),(-.13,.54)])
    b.patch('DemiDevimon curved toothy smile',outline,(0,.58),sf,'Mouth','Body',.004)
    for j in range(8):
        x=(j-3.5)*.043;z=.600+.038*(abs(x)/.16)**2
        b.patch('DemiDevimon small square smile tooth',[(x-.018,z),(x+.018,z),(x+.013,z-.037),(x-.013,z-.037)],(x,z-.019),sf,'Ivory','Body',.006,6)
    for j in range(7):
        x=-.12+j*.04;b.stroke('DemiDevimon facial stitch',[(x,.715),(x+.006,.677)],sf,'Leather','Body',.004,.006)
    b.pigment_disc('DemiDevimon forehead skull',0,.969,.060,.063,sf,'Ivory','Head',.004)
    for s in [-1,1]:b.pigment_disc('DemiDevimon skull socket',s*.025,.978,.016,.022,sf,'DarkArmor','Head',.006)
    for j in range(3):b.patch('DemiDevimon skull tooth',[(j*.022-.035,.932),(j*.022-.02,.932),(j*.022-.02,.905),(j*.022-.035,.905)],(j*.022-.027,.919),sf,'Ivory','Head',.005,3)
    for i in range(22):
        a=i*math.tau/22;b.leaf('DemiDevimon dark feather ruff',[(.24*math.cos(a),.23*math.sin(a),.54),(.33*math.cos(a),.29*math.sin(a),.46),(.36*math.cos(a),.32*math.sin(a),.38)],[.035,.055,.001],'DarkArmor','Body',False,.01)

INDIVIDUAL={'Tsunomon':tsunomon,'Tanemon':tanemon,'Pagumon':pagumon,'Gabumon':gabumon_individual,'Palmon':palmon_individual,'Togemon':togemon_individual,'Garurumon':garurumon_individual,'MetalGarurumon':metalgarurumon_individual,'WereGarurumon':weregarurumon_individual,'DemiDevimon':demidevimon_individual}

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
