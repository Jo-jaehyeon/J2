"""Reference-authored SD playable boy. Blender Z-up / -Y facing.
Own character geometry; shares only mesh and export utilities with asset tools.
"""
from pathlib import Path
exec(Path('C:/Jerry/Unity Project/J2/Tools/Blender/individual_rebuild.py').read_text(encoding='utf-8'))
PALETTE.update(TamerSkin=(.96,.65,.40),TamerFace=(1,.75,.52),TamerBlue=(.025,.30,.62),TamerBlueLight=(.07,.49,.83),TamerYellow=(1,.66,.045),TamerBrown=(.28,.12,.055),TamerHair=(.105,.036,.018),TamerHairLight=(.23,.075,.029),TamerLens=(.015,.38,.60),TamerLensLight=(.12,.77,.94),TamerIris=(.32,.13,.045))
FOLDER=PROJECT/'Assets/Resources/Players/SDTamer'

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
        self.rig.animation_data.action.name='SDTamer_AllClips'
        for name,f in [('Idle',1),('Walk',60),('Attack',90),('Death',120)]:self.scene.timeline_markers.new(name,frame=f)

def build_tamer():
    (FOLDER/'Source~').mkdir(parents=True,exist_ok=True);b=Tamer('SDTamer')
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
    # Hair cap extends around the back, with layered swept locks instead of a bald dome.
    cap=b.oval('Brown hair mass',(0,.056,1.55),(.354,.29,.25),'TamerHair','Head',64,40)
    # Remove front-lower cap faces that would cover face and eyebrows.
    bm=bmesh.new();bm.from_mesh(cap.data)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().y<-.10 and f.calc_center_median().z<.015],context='FACES');bm.to_mesh(cap.data);bm.free()
    for i,(start,mid,end,w) in enumerate([
        ((-.24,.07,1.62),(-.40,.10,1.74),(-.55,.16,1.78),.11),
        ((-.28,.11,1.52),(-.46,.18,1.57),(-.61,.24,1.64),.12),
        ((-.25,.15,1.45),(-.43,.26,1.44),(-.56,.34,1.50),.11),
        ((.20,.08,1.62),(.36,.17,1.73),(.50,.27,1.84),.12),
        ((.25,.15,1.54),(.45,.26,1.60),(.59,.39,1.71),.12),
        ((.20,.22,1.46),(.41,.39,1.47),(.52,.51,1.56),.11),
        ((-.11,.06,1.72),(-.17,.12,1.91),(-.20,.20,2.04),.11),
        ((.04,.05,1.74),(.11,.11,1.95),(.16,.20,2.06),.115),
        ((.16,.12,1.69),(.25,.23,1.89),(.34,.35,1.98),.10),
        ((0,.22,1.66),(.01,.41,1.76),(.05,.57,1.83),.14),
        ((-.12,.25,1.53),(-.19,.42,1.60),(-.29,.55,1.68),.12)]):
        b.tube('Swept sculpted hair lock '+str(i),[start,mid,end],[w,w*.72,.001],'TamerHairLight' if i%3==0 else 'TamerHair','Head',12,flat=.46,sub=6)
        b.tube('Hair ridge '+str(i),[Vector(start)+Vector((0,-.013,.024)),Vector(mid)+Vector((0,-.013,.024)),end],[.009,.006,.001],'TamerBrown','Head',8)
    for s in [-1,1]:
        b.tube('Pointed sideburn',[(s*.29,-.045,1.52),(s*.30,-.06,1.37),(s*.29,-.04,1.27)],[.056,.037,.001],'TamerHair','Head',12,flat=.5)
    # Blue band and round gold-framed goggles rest on the forehead.
    bandpts=[(.342*math.cos(i*math.tau/64),.016+.285*math.sin(i*math.tau/64),1.575) for i in range(65)]
    b.tube('Blue headband',bandpts,[.037]*65,'TamerBlue','Head',12,flat=.7,sub=1)
    for s in [-1,1]:
        x=s*.125;z=1.615;y=-.258
        ring=[(x+.105*math.cos(i*math.tau/48),y,z+.09*math.sin(i*math.tau/48)) for i in range(49)]
        b.tube('Golden goggle rim',ring,[.012]*49,'TamerYellow','Head',12,sub=1)
        b.oval('Deep blue goggle lens',(x,y+.004,z),(.094,.024,.079),'TamerLens','Head',40,28)
        b.tube('Cyan lens reflected streak',[(x-.049,y-.023,z+.044),(x-.022,y-.025,z+.059),(x+.013,y-.023,z+.064)],[.008]*3,'TamerLensLight','Head',10)
    b.tube('Goggle bridge',[(-.020,-.261,1.615),(0,-.272,1.628),(.020,-.261,1.615)],[.012]*3,'TamerYellow','Head',12)
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
    report=b.finalize(FOLDER);report.update(name='SDTamer',clips=[{'name':n,'firstFrame':a,'lastFrame':z,'loop':loop} for n,a,z,loop in [('Idle',1,49,True),('Walk',60,84,True),('Attack',90,108,False),('Death',120,150,False)]])
    for m in report['materials']:m['vertexColor']=True
    (FOLDER/'Source~'/'import.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (FOLDER/'Source~'/'build_sd_tamer.py').write_text(Path('C:/Jerry/Unity Project/J2/Tools/Blender/build_sd_tamer.py').read_text(encoding='utf-8'),encoding='utf-8')
    return {'vertices':report['vertices'],'triangles':report['triangles'],'bones':report['bones'],'folder':str(FOLDER)}
