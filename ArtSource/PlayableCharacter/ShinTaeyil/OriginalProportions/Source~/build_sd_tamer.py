"""Reference-authored SD playable boy. Blender Z-up / -Y facing.
Own character geometry; shares only mesh and export utilities with asset tools.
"""
from pathlib import Path
exec(Path('C:/Jerry/Unity Project/J2/Tools/Blender/individual_rebuild.py').read_text(encoding='utf-8'))
PALETTE.update(TamerSkin=(.96,.65,.40),TamerFace=(1,.75,.52),TamerBlue=(.025,.30,.62),TamerBlueLight=(.07,.49,.83),TamerYellow=(1,.66,.045),TamerBrown=(.28,.12,.055),TamerHair=(.24,.083,.018),TamerHairLight=(.32,.12,.027),TamerLens=(.015,.38,.60),TamerLensLight=(.12,.77,.94),TamerIris=(.32,.13,.045))
FOLDER=PROJECT/'Assets/Resources/PlayableCharacter/ShinTaeyil'

class Tamer(Individual):
    def animate(self):
        self.scene.render.fps=30;self.scene.frame_start=1;self.scene.frame_end=150
        for frame,p in [(1,0),(13,1),(25,0),(37,-1),(49,0)]:
            self.key(frame,{'Head':(p*1.5,0,p*1.5),'UpperArm.L':(0,0,p*2),'UpperArm.R':(0,0,-p*2)},{'Body':(0,p*.009,0)})
        for frame,p in [(60,0),(66,1),(72,0),(78,-1),(84,0)]:
            self.key(frame,{'Thigh.L':(p*24,0,0),'Thigh.R':(-p*24,0,0),'Shin.L':(-max(0,p)*22,0,0),'Shin.R':(-max(0,-p)*22,0,0),'UpperArm.L':(-p*20,0,0),'UpperArm.R':(p*20,0,0),'Head':(0,0,-p*2)},{'Body':(0,abs(p)*.025,0)})
        self.key(90);self.key(95,{'UpperArm.R':(-75,0,-10),'Forearm.R':(-40,0,0),'Head':(-8,0,-8)})
        self.key(100,{'UpperArm.R':(-115,0,12),'Forearm.R':(5,0,0),'Head':(-5,0,4),'Body':(5,0,0)})
        self.key(104,{'UpperArm.R':(-90,0,15),'Forearm.R':(-10,0,0)});self.key(108)
        self.key(120);self.key(128,{'Body':(15,0,0),'Head':(18,0,0),'Thigh.L':(-30,0,-10),'Thigh.R':(-30,0,10),'Shin.L':(40,0,0),'Shin.R':(40,0,0)},{'Root':(0,-.16,0)})
        for frame in [140,150]:self.key(frame,{'Root':(82,0,8),'Head':(12,0,0),'UpperArm.L':(-20,0,-25),'UpperArm.R':(-20,0,25)},{'Root':(0,.30,0)})
        self.rig.animation_data.action.name=self.name+'_AllClips'
        for name,f in [('Idle',1),('Walk',60),('Attack',90),('Death',120)]:self.scene.timeline_markers.new(name,frame=f)

def build_tamer(non_sd=False):
    folder=PROJECT/'ArtSource/PlayableCharacter/ShinTaeyil/OriginalProportions' if non_sd else FOLDER
    name='ShinTaeyil_OriginalProportions' if non_sd else 'ShinTaeyil'
    (folder/'Source~').mkdir(parents=True,exist_ok=True);b=Tamer(name)
    b.bone('Body',(0,0,.72));b.bone('Head',(0,0,1.10),'Body')
    b.loft('Blue loose short sleeved shirt',[(.61,0,.20,.135),(.67,0,.22,.15),(.83,0,.19,.13),(.99,0,.22,.13),(1.065,0,.09,.075)],'TamerBlue','Body',64,7)
    b.loft('Yellow shirt hem',[(.60,0,.204,.14),(.64,0,.22,.15),(.68,0,.215,.147)],'TamerYellow','Body',48,3)
    b.tube('Yellow collar', [(-.08,-.045,1.04),(0,-.08,1.025),(.08,-.045,1.04)],[.016]*3,'TamerYellow','Body',12)
    b.tube('Neck',[(0,0,1.015),(0,0,1.13)],[.073,.078],'TamerSkin','Head',28)
    face=[(1.04,-.025,.10,.10),(1.10,-.035,.225,.18),(1.27,-.01,.32,.26),(1.47,.005,.335,.275),(1.62,.012,.255,.23),(1.69,.02,.10,.10)]
    b.loft('SD rounded cheeks and chin',face,'TamerFace','Head',96,9);sf=profile_surface(face)
    for s in [-1,1]:
        b.oval('Rounded ear',(s*.321,.012,1.32),(.057,.043,.08),'TamerSkin','Head',28,18)
        b.oval('Ear inner fold',(s*.343,-.018,1.32),(.025,.014,.044),'TamerFace','Head',24,16)
        eye=bezier_outline([(s*.056,1.395),(s*.092,1.437),(s*.164,1.445),(s*.223,1.411),(s*.208,1.325),(s*.15,1.300),(s*.091,1.327)])
        b.patch('Expressive almond eye contour',eye,(s*.143,1.371),sf,'TamerHair','Head',.003)
        b.patch('Eye white',[(s*.143+(x-s*.143)*.89,1.371+(z-1.371)*.87) for x,z in eye],(s*.143,1.371),sf,'White','Head',.004)
        b.pigment_disc('Warm brown iris',s*.142,1.365,.041,.060,sf,'TamerIris','Head',.005)
        b.pigment_disc('Dark pupil',s*.142,1.366,.022,.042,sf,'TamerHair','Head',.006)
        b.pigment_disc('Eye light',s*.142-.014,1.397,.015,.016,sf,'White','Head',.007)
        b.stroke('Confident eyebrow',[(s*.068,1.475),(s*.134,1.492),(s*.213,1.467)],sf,'TamerHair','Head',.009,.007)
    b.tube('Small sculpted nose',[(0,-.250,1.338),(0,-.285,1.293),(0,-.281,1.283)],[.018,.026,.012],'TamerFace','Head',20)
    smile=bezier_outline([(-.085,1.206),(-.02,1.186),(.048,1.195),(.094,1.223),(.067,1.164),(0,1.147),(-.064,1.167)])
    b.patch('Cheerful mouth',smile,(0,1.18),sf,'Mouth','Head',.004)
    b.patch('Smile teeth',[(-.069,1.202),(.073,1.214),(.050,1.185),(-.046,1.18)],(0,1.195),sf,'Ivory','Head',.006,10)
    b.pigment_disc('Tongue',.015,1.166,.035,.009,sf,'Tongue','Head',.006)
    # The reference has a broad windswept crown, not upright cone spikes.
    cap=b.oval('Continuous swept crown',(0,.065,1.60),(.37,.30,.235),'TamerHair','Head',64,40)
    bm=bmesh.new();bm.from_mesh(cap.data)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().y<-.09 and f.calc_center_median().z<-.015],context='FACES');bm.to_mesh(cap.data);bm.free()
    def lock(label,points,widths,depth=.04,color='TamerHair'):
        # Closed lenticular cross sections: a broad surface, a raised ridge and a sharp tip.
        ps=[Vector(q) for q in points];vs=[];fs=[];N=24;K=12
        for i in range(N+1):
            t=i/N;c=(1-t)**2*ps[0]+2*(1-t)*t*ps[1]+t*t*ps[2]
            axis=((ps[1]-ps[0])*(1-t)+(ps[2]-ps[1])*t).normalized()
            side=axis.cross(Vector((0,-1,0))).normalized()
            w=((1-t)**2*widths[0]+2*(1-t)*t*widths[1]+t*t*.0003)*min(1,.12+t*10)
            for j in range(K):
                ang=j*math.tau/K
                vs.append(c+side*w*math.cos(ang)+Vector((0,depth*math.sin(ang)*(1-t)**.6,0)))
        for i in range(N):
            for j in range(K):
                k=i*K+j;n=i*K+(j+1)%K;fs.append((k,n,n+K,k+K))
        fs.extend([tuple(reversed(range(K))),tuple(N*K+j for j in range(K))])
        b.mesh(label,vs,fs,color,'Head')
    # Asymmetrical, overlapping lateral fans follow the supplied animated silhouette.
    fans=[
        ((.16,.02,1.73),(-.12,-.015,1.90),(-.62,.015,1.87),(.12,.14)),
        ((.08,.06,1.76),(-.20,.025,1.84),(-.72,.065,1.78),(.14,.17)),
        ((-.11,.07,1.68),(-.38,.02,1.76),(-.68,.04,1.64),(.14,.17)),
        ((-.19,.08,1.59),(-.39,.015,1.65),(-.61,.015,1.49),(.13,.12)),
        ((-.25,.10,1.54),(-.38,.08,1.54),(-.49,.02,1.36),(.10,.10)),
        ((.04,.075,1.74),(.35,.06,1.81),(.64,.04,1.73),(.13,.14)),
        ((.21,.10,1.67),(.43,.06,1.71),(.69,.08,1.61),(.12,.14)),
        ((.26,.08,1.59),(.42,.05,1.61),(.58,.03,1.46),(.11,.11)),
        ((.25,.13,1.53),(.36,.10,1.52),(.43,.03,1.34),(.09,.09)),
        ((.08,.07,1.77),(.10,.05,1.86),(-.02,.07,1.95),(.08,.07)),
        ((.13,.07,1.77),(.21,.05,1.85),(.18,.09,1.94),(.07,.06)),
        ((-.05,.16,1.74),(-.28,.35,1.79),(-.51,.44,1.76),(.13,.13)),
        ((.15,.18,1.68),(.40,.35,1.70),(.51,.47,1.61),(.13,.12)),
        ((0,.23,1.64),(-.04,.46,1.64),(-.18,.56,1.47),(.16,.14)),
        ((-.16,.20,1.56),(-.30,.36,1.53),(-.40,.42,1.36),(.10,.10)),
        ((.17,.20,1.54),(.30,.36,1.52),(.37,.42,1.34),(.10,.10))]
    for i,(a,m,e,w) in enumerate(fans):
        a=(a[0],a[1]+.13,a[2]-.035)
        lock('Swept crown layer %02d'%i,[a,m,e],w,.048,'TamerHairLight' if i in (0,5,9) else 'TamerHair')
        if i<9:
            a=Vector(a);m=Vector(m);e=Vector(e)
            lock('Fine split tip %02d'%i,[a+Vector((0,-.025,.015)),m+Vector((0,-.02,.034)),e+Vector((.035 if e.x>0 else -.035,-.01,.04))],[w[0]*.43,w[1]*.32],.014)
    for i,(a,m,e,w) in enumerate([
        ((.18,-.085,1.78),(-.02,-.22,1.83),(-.46,-.19,1.80),(.10,.105)),
        ((.16,-.13,1.75),(-.10,-.26,1.78),(-.49,-.22,1.70),(.10,.12)),
        ((.15,-.17,1.71),(-.06,-.28,1.72),(-.35,-.25,1.61),(.07,.085)),
        ((.18,-.10,1.76),(.29,-.20,1.75),(.47,-.17,1.68),(.08,.07))]):
        lock('Forehead swept overlapping surface %02d'%i,[a,m,e],w,.023,'TamerHairLight' if i==0 else 'TamerHair')
    # Forehead locks lie in front of the band and taper between the eyes.
    for i,(a,m,e,w) in enumerate([
        ((-.085,-.276,1.60),(-.04,-.292,1.49),(.035,-.293,1.36),(.065,.058)),
        ((.025,-.282,1.58),(.074,-.304,1.47),(.104,-.283,1.32),(.055,.046)),
        ((-.14,-.249,1.58),(-.20,-.258,1.47),(-.205,-.208,1.31),(.045,.048)),
        ((.22,-.211,1.56),(.26,-.205,1.42),(.25,-.153,1.28),(.046,.047)),
        ((-.28,-.13,1.50),(-.31,-.115,1.38),(-.29,-.055,1.23),(.049,.038))]):
        a=(a[0],a[1]+.080,a[2])
        lock('Separated forehead fringe %02d'%i,[a,m,e],w,.023)
    # Blue band and round gold-framed goggles rest on the forehead.
    bandpts=[(.342*math.cos(i*math.tau/64),.016+.285*math.sin(i*math.tau/64),1.575) for i in range(65)]
    b.tube('Blue headband',bandpts,[.037]*65,'TamerBlue','Head',12,flat=.7,sub=1)
    for s in [-1,1]:
        x=s*.134;z=1.615;y=-.282
        ring=[(x+.114*math.cos(i*math.tau/48),y,z+.082*math.sin(i*math.tau/48)-s*.022*math.cos(i*math.tau/48)) for i in range(49)]
        b.tube('Ivory goggle rim',ring,[.014]*49,'Ivory','Head',12,sub=1)
        b.oval('Deep blue goggle lens',(x,y+.004,z),(.103,.024,.071),'TamerLens','Head',40,28)
        b.tube('Cyan lens reflected streak',[(x-.049,y-.023,z+.044),(x-.022,y-.025,z+.059),(x+.013,y-.023,z+.064)],[.008]*3,'TamerLensLight','Head',10)
    b.tube('Goggle bridge',[(-.020,-.261,1.615),(0,-.272,1.628),(.020,-.261,1.615)],[.012]*3,'Ivory','Head',12)
    # Cargo shorts, short limbs, ribbed socks, and chunky reference sneakers.
    b.loft('Brown shorts waist',[(.49,0,.17,.13),(.60,0,.205,.143),(.63,0,.20,.14)],'TamerBrown','Body',48,4)
    for s in [-1,1]:
        side='L' if s<0 else 'R';th=b.bone('Thigh.'+side,(s*.11,0,.56),'Body');sh=b.bone('Shin.'+side,(s*.13,0,.34),th);ft=b.bone('Foot.'+side,(s*.14,0,.13),sh)
        b.tube('Cargo short leg',[(s*.115,0,.58),(s*.135,.015,.44)],[.116,.12],'TamerBrown',th,24,flat=.85,sub=4)
        b.tube('Short hem fold',[(s*.13,.012,.445),(s*.13,.012,.47)],[.124,.124],'Leather',th,24,flat=.85,sub=1)
        b.oval('Cargo side pocket',(s*.228,.012,.52),(.032,.075,.065),'Leather',th,20,12)
        b.tube('Cargo pocket flap',[(s*.25,-.045,.55),(s*.26,.015,.552),(s*.25,.065,.55)],[.009]*3,'TamerBrown',th,8)
        b.tube('Bare knee',[(s*.13,0,.445),(s*.13,0,.31)],[.066,.064],'TamerSkin',sh,28)
        b.tube('White sock',[(s*.135,0,.16),(s*.13,0,.30)],[.073,.079],'White',sh,28)
        for j in range(4):band(b,'Sock folded rib',(s*.13,0,.24+j*.019),(s*.13,0,.25+j*.019),.083,'SilverLight',sh)
        b.oval('Sneaker white upper',(s*.14,-.060,.10),(.112,.19,.091),'White',ft,40,24)
        b.oval('Sneaker yellow outsole',(s*.14,-.056,.035),(.116,.196,.031),'TamerYellow',ft,40,20)
        b.oval('Sneaker blue toe panel',(s*.14,-.180,.096),(.081,.068,.039),'TamerBlue',ft,32,20)
        b.oval('Sneaker high blue heel',(s*.14,.065,.125),(.09,.055,.087),'TamerBlue',ft,28,20)
        for j in range(3):b.tube('White sneaker lace',[(s*.14-.049,-.10+j*.037,.170),(s*.14+.049,-.11+j*.037,.171)],[.008,.008],'White',ft,10)
        up=b.bone('UpperArm.'+side,(s*.20,0,.97),'Body');fore=b.bone('Forearm.'+side,(s*.31,-.01,.82),up);hand=b.bone('Hand.'+side,(s*.36,-.04,.70),fore)
        b.tube('Blue short sleeve',[(s*.19,0,.995),(s*.27,-.005,.91)],[.097,.092],'TamerBlue',up,28)
        b.tube('Yellow sleeve binding',[(s*.258,-.005,.925),(s*.279,-.007,.903)],[.095,.095],'TamerYellow',up,24,sub=2)
        b.tube('Exposed upper arm',[(s*.28,-.005,.91),(s*.31,-.01,.82)],[.060,.052],'TamerSkin',up,28)
        b.tube('Short forearm',[(s*.31,-.01,.82),(s*.36,-.04,.70)],[.053,.046],'TamerSkin',fore,28)
        band(b,'White glove cuff',(s*.355,-.038,.72),(s*.366,-.045,.685),.060,'SilverLight',hand)
        b.oval('White glove palm',(s*.373,-.045,.646),(.064,.044,.069),'White',hand,32,20)
        for j in range(4):
            x=s*.373+(j-1.5)*.031;b.tube('Glove finger',[(x,-.045,.63),(x,-.058,.581-(.008 if j in [1,2] else 0)),(x,-.067,.575-(.008 if j in [1,2] else 0))],[.018,.016,.006],'White',hand,16)
        b.tube('Glove thumb',[(s*.32,-.045,.665),(s*.301,-.073,.631),(s*.317,-.08,.609)],[.025,.023,.011],'White',hand,18)
    if non_sd:
        # Re-proportion body and skeleton together: longer legs/torso, smaller head,
        # while shoes retain their height. No Unity destination for this study.
        def pos(v,head=False):
            x,y,z=v
            if head:return Vector((x*.72,y*.72,1.94+(z-1.10)*.72))
            zz=z if z<=.16 else .16+(z-.16)*(1.25-.16)/(.61-.16) if z<.61 else 1.25+(z-.61)*(1.94-1.25)/(.49)
            return Vector((x*.94,y*.94,zz))
        for ob in b.parts:
            ishead=ob.vertex_groups.get('Head') is not None
            inv=ob.matrix_world.inverted()
            for v in ob.data.vertices:v.co=inv@pos(ob.matrix_world@v.co,ishead)
        b.bones={n:(pos(Vector(p),n=='Head'),parent) for n,(p,parent) in b.bones.items()}
    report=b.finalize(folder);report.update(name=name,clips=[{'name':n,'firstFrame':a,'lastFrame':z,'loop':loop} for n,a,z,loop in [('Idle',1,49,True),('Walk',60,84,True),('Attack',90,108,False),('Death',120,150,False)]])
    for m in report['materials']:m['vertexColor']=True
    (folder/'Source~'/'import.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (folder/'Source~'/'build_sd_tamer.py').write_text(Path('C:/Jerry/Unity Project/J2/Tools/Blender/build_sd_tamer.py').read_text(encoding='utf-8'),encoding='utf-8')
    return {'vertices':report['vertices'],'triangles':report['triangles'],'bones':report['bones'],'folder':str(folder)}
