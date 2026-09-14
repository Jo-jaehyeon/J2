"""Reference-authored second roster. Run in the connected Blender editor.

Curved closed surfaces, character-specific facial apertures and articulated rigs.
The first roster generator is reused only for export/rig infrastructure.
"""
from pathlib import Path
_base=Path('C:/Jerry/Unity Project/J2/Tools/Blender/roster_models.py')
exec(_base.read_text(encoding='utf-8').replace('poly.use_smooth=False','poly.use_smooth=True').replace('self.animate()','self.apply_surface_colors();self.animate()'))
import random
PALETTE.update(Cream=(1,.88,.60), Fur=(.78,.86,.94), WolfBlue=(.13,.20,.53),
    Leaf=(.19,.43,.10), Plant=(.48,.69,.25), LeafLight=(.51,.70,.22),
    Peach=(.98,.78,.67), Rose=(.65,.025,.065), RoseLight=(.88,.055,.10),
    Petal=(.95,.28,.54), PetalLight=(1,.51,.70), Leather=(.21,.115,.055),
    Denim=(.12,.25,.43), Pale=(.67,.73,.86), Suit=(.20,.20,.43),
    DemonSkin=(.10,.085,.13), DemonRed=(.64,.07,.085), FurDark=(.13,.075,.12))
NEW={
 'Tsunomon':('뿔몬','1코스트',1.1,1.45,1.35,'baby'),
 'Gabumon':('파피몬','2코스트',1.65,1.55,1.7,'dinosaur'),
 'Garurumon':('가루몬','3코스트',1.65,1.75,2.3,'spider'),
 'WereGarurumon':('워가루몬','4코스트',2.15,1.9,1.9,'dinosaur'),
 'MetalGarurumon':('메탈가루몬','5코스트',1.9,2.35,2.35,'spider'),
 'Tanemon':('시드몬','1코스트',1.05,1.55,1.4,'baby'),
 'Palmon':('팔몬','2코스트',1.6,1.6,1.5,'dinosaur'),
 'Togemon':('니드몬','3코스트',1.9,1.9,1.7,'dinosaur'),
 'Lillymon':('릴리몬','4코스트',2.0,2.0,1.8,'floating'),
 'Rosemon':('로제몬','5코스트',2.15,2.0,1.8,'dinosaur'),
 'Pagumon':('퍼그몬','1코스트',1.05,1.65,1.4,'baby'),
 'DemiDevimon':('피코데블몬','2코스트',1.45,1.9,1.5,'floating'),
 'Devimon':('데블몬','3코스트',2.15,1.9,1.8,'dinosaur'),
 'Myotismon':('묘티스몬','4코스트',2.15,1.9,1.8,'dinosaur'),
 'VenomMyotismon':('베놈묘티스몬','5코스트',2.35,2.45,2.1,'dinosaur')}
RECIPES.update(NEW)

class Detailed(Builder):
    def oval(self,name,loc,scale,color,bone,seg=40,rings=28,rotate=None,stripe=None):
        ob=super().oval(name,loc,scale,color,bone,seg,rings,rotate)
        if stripe:self.markings(ob,stripe,scale)
        return ob

    def markings(self,ob,color,scale):
        attr=ob.data.color_attributes.get('ReferenceColor') or ob.data.color_attributes.new(name='ReferenceColor',type='FLOAT_COLOR',domain='CORNER')
        base=ob.data.materials[0].diffuse_color;ink=PALETTE[color]
        for loop in ob.data.loops:
            v=ob.data.vertices[loop.vertex_index].co
            x,y,z=v.x/scale[0],v.y/scale[1],v.z/scale[2]
            w=math.sin(z*12+abs(x)*3.5+y*1.8+.7*math.sin(y*7))
            t=max(0,min(1,(w-.48)/.34))*max(0,min(1,(abs(x)-.16)*12)) if y<-.25 else max(0,min(1,(w-.48)/.34))
            t=t*t*(3-2*t)
            attr.data[loop.index].color=(*(base[k]*(1-t)+ink[k]*t for k in range(3)),1)

    def apply_surface_colors(self):
        mesh=self.body.data
        attr=mesh.color_attributes.get('ReferenceColor') or mesh.color_attributes.new(name='ReferenceColor',type='FLOAT_COLOR',domain='CORNER')
        for p in mesh.polygons:
            for i in p.loop_indices:
                if attr.data[i].color[3]<.5:attr.data[i].color=mesh.materials[p.material_index].diffuse_color
        mesh.color_attributes.active_color=attr

    def tube(self,name,points,radii,color,bone,sides=16,flat=1,sub=5,stripe=None):
        return super().tube(name,points,radii,color,bone,sides,flat,sub,stripe)

    def loft(self,name,sections,color,bone,segments=48,steps=4,ridge=0):
        # sections: z, center-y, half-width, half-depth. Smooth profile, closed ends.
        sec=[]
        for i in range(len(sections)-1):
            a,b=sections[i:i+2]
            for j in range(steps):
                t=j/steps
                p0,p1,p2,p3=[sections[max(0,min(len(sections)-1,q))] for q in [i-1,i,i+1,i+2]]
                sec.append([.5*(2*p1[k]+(-p0[k]+p2[k])*t+(2*p0[k]-5*p1[k]+4*p2[k]-p3[k])*t*t+(-p0[k]+3*p1[k]-3*p2[k]+p3[k])*t*t*t) for k in range(4)])
        sec.append(sections[-1]);v=[];f=[]
        for z,y,w,d in sec:
            for j in range(segments):
                a=j*math.tau/segments;r=1+ridge*math.cos(a*12)
                v.append((math.cos(a)*w*r,y+math.sin(a)*d*r,z))
        for i in range(len(sec)-1):
            for j in range(segments):
                k=i*segments+j;n=i*segments+(j+1)%segments;f.append((k,n,n+segments,k+segments))
        f.extend([tuple(reversed(range(segments))),tuple((len(sec)-1)*segments+j for j in range(segments))])
        return self.mesh(name,v,f,color,bone)

    def petal(self,name,points,width,color,bone,vein=False,cup=.15):
        # Thick doubly-curved leaf/petal. Width follows pointed botanical outline.
        p=[Vector(q) for q in points];verts=[];faces=[];N=20;M=12
        for layer in [-1,1]:
            for i in range(N+1):
                t=i/N;c=(1-t)**2*p[0]+2*(1-t)*t*p[1]+t*t*p[2]
                axis=((p[1]-p[0])*(1-t)+(p[2]-p[1])*t).normalized()
                side=axis.cross(Vector((0,-1,0))).normalized()
                if side.length<.1:side=Vector((1,0,0))
                w=width*(math.sin(math.pi*t)**.65)*(.9+.1*math.sin(t*5))+.002
                for j in range(M+1):
                    u=j/M*2-1
                    verts.append(c+side*w*u+Vector((0,cup*u*u*math.sin(math.pi*t)+layer*.006,0)))
        stride=(N+1)*(M+1)
        for l in range(2):
            for i in range(N):
                for j in range(M):
                    k=l*stride+i*(M+1)+j;faces.append((k,k+1,k+M+2,k+M+1))
        border=list(range(M+1))+[i*(M+1)+M for i in range(1,N+1)]+[N*(M+1)+j for j in range(M-1,-1,-1)]+[i*(M+1) for i in range(N-1,0,-1)]
        for i,a in enumerate(border):
            z=border[(i+1)%len(border)];faces.append((a,z,z+stride,a+stride))
        ob=self.mesh(name,verts,faces,color,bone)
        if vein:
            self.tube(name+' midrib',[q+Vector((0,-.016,0)) for q in p],[.011,.014,.001],'LeafLight',bone,8,sub=9)
        return ob

    def almond(self,name,pos,width,height,iris,bone,slant=0):
        x,y,z=pos;N=48;v=[(x,y-.034,z)];outline=[]
        for i in range(N):
            a=i*math.tau/N;xx=math.cos(a);zz=math.sin(a)*(abs(math.sin(a))**.28)
            outline.append((x+width*xx,y+.035*abs(xx),z+height*zz+slant*xx))
        v+=outline;f=[(0,1+i,1+(i+1)%N) for i in range(N)]
        v.append((x,y+.035,z));f += [(N+1,1+(i+1)%N,1+i) for i in range(N)]
        self.mesh(name+' sclera',v,f,'Ivory',bone)
        self.tube(name+' eyelid',outline+[outline[0]],[.009]*(N+1),'Black',bone,8,sub=1)
        self.oval(name+' iris',(x,y-.048,z),(height*.69,.018,height*.81),iris,bone,32,20)
        self.oval(name+' pupil',(x,y-.064,z),(height*.32,.011,height*.57),'Black',bone,24,16)
        self.oval(name+' glint',(x-height*.2,y-.074,z+height*.34),(height*.16,.006,height*.17),'White',bone,16,10)

    def smile(self,name,pos,width,height,bone,fangs=False):
        x,y,z=pos
        self.oval(name+' cavity',(x,y,z),(width,.035,height),'Mouth',bone)
        self.tube(name+' lip',[(x-width,y-.008,z),(x,y-.039,z-height*.8),(x+width,y-.008,z)],[.01,.014,.01],'Cream' if self.name=='Tsunomon' else 'Black',bone,8)
        if fangs:
            for s in [-1,1]:self.tube('Fang',[(x+s*width*.70,y-.035,z+height*.45),(x+s*width*.64,y-.055,z-height*.30)],[width*.15,.001],'Ivory',bone,12,sub=2)

def small(b):
    n=b.name;b.bone('Body',(0,0,.43));b.bone('Head',(0,0,.65),'Body')
    color={'Tsunomon':'SkinOrange','Tanemon':'Plant','Pagumon':'BlueLight'}[n]
    ob=b.oval('Continuous body',(0,0,.44),(.51,.37,.44),color,'Body',64,48)
    for v in ob.data.vertices:v.co.z=max(v.co.z,-.41)
    if n!='Pagumon':
        verts=[];faces=[]
        for i in range(41):
            x=-.43+.86*i/40
            top=.65+.16*math.sin(abs(x)/.43*math.pi)
            bottom=.085+.14*(abs(x)/.43)**3
            for j in range(33):
                z=bottom+(top-bottom)*j/32
                y=-.37*math.sqrt(max(.015,1-(x/.51)**2-((z-.44)/.44)**2))-.006
                verts.append((x,y,z))
        for i in range(40):
            for j in range(32):
                k=i*33+j;faces.append((k,k+1,k+34,k+33))
        patch=b.mesh('Curved heart-shaped cream face',verts,faces,'Cream','Body')
        mod=patch.modifiers.new('Face marking thickness','SOLIDIFY');mod.thickness=.002
        bpy.context.view_layer.objects.active=patch;bpy.ops.object.modifier_apply(modifier=mod.name)
        for s in [-1,1]:b.almond('Red eye',(s*.215,-.376,.53),.10,.127,'Ruby','Body',s*.025)
        b.smile('Smile',(0,-.366,.27),.12,.044,'Body',n=='Tsunomon')
    if n=='Tsunomon':
        b.tube('Swept solitary horn',[(0,.035,.82),(0,.09,1.02),(0,.18,1.36),(0,.22,1.52)],[.12,.09,.035,.001],'SteelDark','Head',24)
        for i in range(11):
            a=i*math.tau/11;b.tube('Horn fur',[(.11*math.cos(a),.03+.08*math.sin(a),.86),(.17*math.cos(a),.03+.12*math.sin(a),.80)],[.038,.001],color,'Body',8,sub=2)
    elif n=='Tanemon':
        b.tube('Split stalk',[(0,0,.80),(0,0,1.02)],[.035,.026],'Leaf','Head')
        for s in [-1,1]:
            bone=b.bone('Antenna.'+str(s),(0,0,.98),'Head')
            b.petal('Curled seed leaf',[(0,0,1.0),(s*.61,.02,1.42),(s*.46,-.06,.94)],.22,'Leaf',bone,True,.14)
        for s in [-1,1]:
            for y in [-.17,.2]:
                b.oval('Root foot',(s*.34,y,.07),(.17,.17,.075),'Cream','Body')
                for j in range(3):b.tube('Root nail',[(s*.36+(j-1)*.05,y-.11,.06),(s*.36+(j-1)*.05,y-.20,.025)],[.025,.001],'Silver','Body',10,sub=2)
    else:
        for s in [-1,1]:
            b.almond('Mischievous eye',(s*.23,-.322,.62),.13,.049,'Ruby','Body',s*.065)
            # Small yellow iris, and muzzle follows the body rather than protruding balls.
            b.oval('Yellow iris',(s*.23,-.379,.62),(.019,.009,.024),'Yellow','Body',20,12)
            arm=b.bone('UpperArm.'+('L' if s<0 else 'R'),(s*.37,.02,.67),'Body')
            b.tube('Broad floppy ear arm',[(s*.36,.05,.68),(s*.62,.08,.98),(s*.87,.08,1.03),(s*1.01,.04,.88)],[.10,.15,.17,.12],color,arm,24,.40)
            for j in range(3):b.tube('Ear fingers',[(s*(.91+j*.055),.04,.91),(s*(.97+j*.07),0,.78)],[.055,.038],color,arm,16)
        b.smile('Cheek smile',(0,-.366,.31),.13,.075,'Body')
        b.oval('Tongue',(0,-.401,.282),(.065,.012,.037),'Tongue','Body')

def flower(b,center,radius,color,bone,count=7,tilt=0):
    c=Vector(center)
    b.oval('Flower heart',c,(radius*.20,.035,radius*.20),'Yellow',bone,24,16)
    for i in range(count):
        a=i*math.tau/count;d=Vector((math.cos(a),tilt,math.sin(a)))
        b.petal('Flower petal',[c,c+d*radius*.65+Vector((0,-.025,0)),c+d*radius],radius*.24,color,bone,cup=.03)

def plant(b):
    tog=b.name=='Togemon';b.bone('Body',(0,0,.7));b.bone('Head',(0,0,1.2),'Body')
    if tog:
        b.loft('Ridged cactus',[(.38,0,.25,.22),(.50,0,.43,.32),(.9,0,.50,.37),(1.4,0,.44,.33),(1.65,0,.28,.24),(1.72,0,.015,.015)],'Plant','Body',96,8,.035)
        for s in [-1,1]:
            b.oval('Eye hole',(s*.17,-.35,1.32),(.059,.025,.081),'Black','Body')
        b.oval('Mouth hole',(0,-.381,1.08),(.062,.025,.123),'Black','Body')
        random.seed(71)
        for i in range(230):
            a=random.uniform(0,math.tau);z=random.uniform(.50,1.59);w=.49*math.sqrt(max(.12,1-((z-.98)/.77)**2))
            p=Vector((math.cos(a)*w,math.sin(a)*w*.75,z))
            if p.y<-.2 and abs(p.x)<.29 and .88<z<1.48:continue
            d=Vector((math.cos(a),math.sin(a),.12))
            b.tube('Cactus thorn',[p,p+d*.05],[.009,.001],'Leaf','Body',6,sub=1)
        for i in range(11):
            a=i*math.tau/11
            b.petal('Cactus crown',[(0,0,1.68),(.14*math.cos(a),.14*math.sin(a),1.86),(.23*math.cos(a),.23*math.sin(a),1.76)],.045,'Yellow','Head',cup=.02)
    else:
        b.loft('Plant trunk',[(.35,0,.15,.14),(.53,0,.19,.15),(.86,0,.13,.12),(1.02,0,.18,.15)],'Plant','Body')
        b.oval('Plant head',(0,-.025,1.10),(.30,.245,.27),'Plant','Head')
        for s in [-1,1]:b.almond('Leaf eye',(s*.13,-.259,1.13),.085,.105,'Emerald','Head',s*.018)
        b.smile('Plant smile',(0,-.27,.99),.12,.035,'Head',True)
        for i in range(6):
            a=i*math.tau/6
            b.petal('Crown pink petal',[(0,.015,1.38),(.35*math.cos(a),.27*math.sin(a),1.51),(.41*math.cos(a),.34*math.sin(a),1.30)],.20,'Petal','Head',cup=.05)
        b.oval('Flower calyx',(0,0,1.39),(.11,.10,.055),'Yellow','Head')
        pts=[(.025*math.cos(i*.35),.025*math.sin(i*.35),1.4+i*.012) for i in range(26)]
        b.tube('Spiral orange stamen',pts,[.022]*25+[.007],'SkinOrange','Head',10,sub=1)
        for i in range(6):b.oval('Back bud',(0,.145,.47+i*.075),(.028,.035,.027),'Leaf','Body',16,12)
    for s in [-1,1]:
        side='L' if s<0 else 'R';th=b.bone('Thigh.'+side,(s*.17,0,.5),'Body');sh=b.bone('Shin.'+side,(s*.19,-.01,.25),th)
        b.tube('Root leg',[(s*.17,0,.52),(s*.20,0,.29),(s*.23,-.04,.10)],[.12 if tog else .078,.11 if tog else .073,.13 if tog else .065],'Plant',sh,24)
        for j in range(3):
            x=s*.23+(j-1)*.062;b.tube('Root toe',[(x,-.03,.09),(x+(j-1)*.04,-.21,.035)],[.055 if tog else .038,.025],'Plant',sh,16)
        arm=b.bone('UpperArm.'+side,(s*(.40 if tog else .14),0,1.02 if tog else .85),'Body')
        hand=b.bone('Hand.'+side,(s*(.68 if tog else .42),-.08,.9 if tog else .42),arm)
        if tog:
            b.tube('Bent cactus arm',[(s*.39,0,1.08),(s*.66,0,.88),(s*.72,-.08,1.1)],[.145,.14,.125],'Plant',arm,28)
            b.oval('Boxing glove',(s*.75,-.09,1.18),(.22,.22,.26),'Ruby',hand)
            b.oval('Glove thumb',(s*.58,-.23,1.10),(.085,.10,.12),'Ruby',hand)
            b.tube('Glove cuff',[(s*.71,-.06,.94),(s*.72,-.07,1.04)],[.135,.155],'Rose',hand,32,sub=2)
            b.tube('Glove seam',[(s*.62,-.275,1.10),(s*.74,-.309,1.08),(s*.87,-.275,1.11)],[.007]*3,'Rose',hand,8)
        else:
            b.tube('Drooping stalk arm',[(s*.14,0,.88),(s*.32,0,.69),(s*.43,-.05,.39)],[.07,.065,.075],'Plant',arm,24)
            for j in range(3):
                x=s*.43+(j-1)*.065
                b.tube('Purple leaf finger',[(x,-.05,.43),(x+(j-1)*.025,-.10,.30)],[.038,.04],'Purple',hand)
                b.petal('Pointed leaf finger',[(x,-.06,.34),(x+(j-1)*.05,-.09,.20),(x+(j-1)*.065,-.13,.08)],.043,'Leaf',hand,True,cup=.015)

def fur(b,center,spread,length,color,bone,count=12):
    c=Vector(center)
    for i in range(count):
        a=i*2.39996;h=(i+.5)/count
        d=Vector((math.cos(a)*spread[0],math.sin(a)*spread[1],(h-.5)*spread[2]))
        start=c+d*.7;end=c+d+Vector((d.x*.35,d.y*.25,-length))
        b.tube('Tapered fur lock',[start,(start+end)*.5+Vector((0,0,.025)),end],[length*.24,length*.18,.001],color,bone,8,flat=.65,sub=3)

def wolf_head(b,center,scale=1,metal=False,pelt=False):
    start=len(b.parts);C='Suit' if metal else 'Fur';stripe='WolfBlue'
    skull=b.loft('Sculpted wolf cranium',[(-.20,.02,.13,.12),(-.04,0,.23,.19),(.15,.03,.24,.19),(.29,.07,.17,.15),(.34,.10,.015,.02)],C,'Head',64,7)
    if not metal:b.markings(skull,stripe,(.24,.20,.34))
    b.tube('Upper muzzle',[(0,-.07,.03),(0,-.28,-.055),(0,-.43,-.10)],[.16,.13,.105],C,'Head',32,.7,7)
    b.oval('Wolf nose',(0,-.465,-.077),(.085,.045,.06),'HairRed' if metal else 'Black','Head')
    b.oval('Open mouth cavity',(0,-.28,-.162),(.125,.18,.055),'Mouth','Head')
    b.tube('Lower mandible',[(0,-.03,-.15),(0,-.25,-.232),(0,-.41,-.195)],[.115,.10,.07],C,'Head',28,.5)
    b.oval('Tongue',(0,-.32,-.198),(.055,.072,.012),'Tongue','Head')
    for s in [-1,1]:
        first=len(b.parts)
        b.almond('Predatory eye',(s*.16,-.163,.102),.080,.064 if pelt else .050,'Ruby' if pelt else 'HairRed' if metal else 'Gold','Head',s*.025)
        # Rotate the entire eye to the oblique cheek surface.
        from mathutils import Matrix
        pivot=Vector((s*.16,-.163,.102));R=Matrix.Rotation(s*.40,4,'Z')
        for ob in b.parts[first:]:ob.matrix_world=Matrix.Translation(pivot)@R@Matrix.Translation(-pivot)@ob.matrix_world
        b.tube('Heavy brow',[(s*.07,-.19,.16),(s*.17,-.155,.20),(s*.255,-.08,.20)],[.032,.046,.016],C,'Head',16)
        b.plate('Pointed triangular wolf ear',[(s*.09,.25),(s*.29,.28),(s*.31,.72)],.065,.075,C,'Head',.012)
        b.plate('Triangular blue ear inset',[(s*.135,.31),(s*.255,.33),(s*.285,.64)],.052,.006,'WolfBlue','Head',.004)
        if not metal:
            b.tube('Forehead blue wedge',[(s*.06,-.13,.29),(s*.08,-.19,.18),(s*.045,-.22,.065)],[.045,.035,.002],'WolfBlue','Head',12,flat=.18)
            for j in range(3):b.tube('Muzzle cross stripe',[(s*.015,-.20-j*.065,.092-j*.055),(s*.085,-.21-j*.065,.075-j*.055),(s*.125,-.19-j*.065,.032-j*.055)],[.018,.023,.001],'WolfBlue','Head',10,flat=.25)
        for j in range(5):
            y=-.12-j*.06
            b.tube('Upper fang',[(s*(.105-j*.004),y,-.13),(s*(.10-j*.004),y-.009,-.19-(.027 if j in [1,3] else 0))],[.019,.001],'Ivory','Head',12,sub=2)
        for j in range(4):
            y=-.14-j*.06;b.tube('Lower tooth',[(s*.084,y,-.218),(s*.084,y,-.18)],[.013,.001],'Ivory','Head',10,sub=2)
        if not metal:fur(b,(s*.22,.075,-.08),(.12,.14,.20),.19,C,'Head',16)
        else:
            b.petal('Armored cheek fin',[(s*.17,-.10,-.1),(s*.32,.04,.04),(s*.40,.13,.23)],.09,'Suit','Head',cup=.015)
            for j in range(3):b.tube('Cheek armor engraving',[(s*.24,-.03,-.11+j*.06),(s*.29,.05,-.08+j*.06)],[.006,.006],'DarkArmor','Head',6,sub=1)
    if pelt:
        b.tube('Golden forehead horn',[(0,-.02,.28),(0,-.04,.59),(0,-.10,.84)],[.07,.05,.001],'Yellow','Head',24)
        for i in range(5):b.tube('Horn growth line',[(-.043,-.06,.38+i*.07),(0,-.088,.39+i*.07),(.043,-.06,.38+i*.07)],[.005]*3,'Gold','Head',8)
    for ob in b.parts[start:]:
        ob.location=Vector(center)+ob.location*scale
        ob.scale*=scale

def quadruped(b):
    metal=b.name=='MetalGarurumon';C='Suit' if metal else 'Fur'
    b.bone('Body',(0,.1,.75));b.bone('Head',(0,-.60,1.0),'Body');b.bone('Tail',(0,.70,.83),'Body')
    body=b.loft('Continuous deep chest and lean barrel',[(.51,0,.17,.48),(.65,.06,.25,.69),(.88,.08,.30,.68),(1.08,.08,.24,.52),(1.13,.07,.035,.08)],'Fur','Body',72,7)
    b.markings(body,'WolfBlue',(.30,.70,1.13))
    wolf_head(b,(0,-.57,1.06),.80,metal)
    for s in [-1,1]:
        for front in [True,False]:
            y=-.39 if front else .49;x=s*.245
            leg=b.bone('Leg'+('Front' if front else 'Rear')+str(s),(x,y,.91),'Body')
            knee=b.bone('Knee'+str(front)+str(s),(x,y+(.04 if front else -.17),.47),leg)
            p=[(x,y,.93),(s*.31,y+(.04 if front else -.19),.58),(s*.29,y+(.11 if front else .11),.30),(s*.29,y-.04,.115)]
            limb=b.tube('Articulated wolf leg',p,[.16,.115,.075,.075],C,leg,28,sub=7)
            # Blend distal vertices through the knee; rest positions remain continuous.
            g=limb.vertex_groups.new(name=knee);upper=limb.vertex_groups[leg]
            for v in limb.data.vertices:
                w=max(0,min(1,(.63-v.co.z)/.20))
                if w:g.add([v.index],w,'REPLACE');upper.add([v.index],1-w,'REPLACE')
            if not metal:b.markings(limb,'WolfBlue',(.32,.60,1.0))
            b.oval('Broad wolf paw',(s*.30,y-.10,.105),(.145,.19,.10),'Fur',knee)
            for j in range(3):
                xx=s*.30+(j-1)*.085
                b.tube('Wolf talon',[(xx,y-.23,.09),(xx,y-.32,.035)],[.042,.001],'HairRed' if metal else 'Purple',knee,16)
            if metal:
                b.oval('Hinge',(s*.385,y,.69),(.03,.104,.104),'Silver',leg)
                b.oval('Hinge centre',(s*.41,y,.69),(.015,.055,.055),'DarkArmor',leg)
                b.tube('Armored boot',p[-2:],[.12,.11],'Suit',knee,16,sub=2)
            else:fur(b,(s*.29,y,.77),(.12,.15,.15),.17,'Fur',leg,12)
    if not metal:
        fur(b,(0,-.38,.91),(.31,.16,.26),.19,'Fur','Body',32)
        b.tube('Sweeping long tail',[(0,.61,.86),(0,1.00,.85),(0,1.28,1.12),(0,1.40,1.33)],[.09,.075,.11,.001],'Fur','Tail',24)
        fur(b,(0,1.25,1.13),(.07,.08,.17),.17,'Fur','Tail',16)
    else:
        b.loft('Golden belly armor',[(.50,0,.15,.36),(.60,0,.20,.44),(.73,0,.20,.43)],'Gold','Body',32,3)
        for s in [-1,1]:
            b.oval('Shoulder missile pod',(s*.32,-.38,1.0),(.18,.24,.17),'Gold','Body')
            for j in range(3):
                x=s*.32+(j-1)*.085;b.tube('Missile',[(x,-.54,1.02),(x,-.65,1.02)],[.037,.001],'SilverLight','Body',16)
            wing=b.bone('Wing.'+('L' if s<0 else 'R'),(s*.22,.07,1.03),'Body')
            b.tube('Wing main spar',[(s*.22,.1,1.04),(s*.69,.20,1.40),(s*1.14,.44,1.61)],[.065,.06,.024],'Gold',wing,12)
            for j in range(3):
                b.petal('Luminous gold wing blade',[(s*(.34+j*.12),.16+j*.07,1.14),(s*(.70+j*.10),.26+j*.12,1.35),(s*(1.12-j*.025),.53+j*.18,1.57-j*.15)],.09,'Yellow',wing,cup=.018)
            b.oval('Rear armor',(s*.21,.49,.98),(.18,.24,.15),'Suit','Body')
            b.plate('Red armor inset',[(s*.12,.95),(s*.32,.95),(s*.30,1.05),(s*.13,1.06)],.60,.015,'Ruby','Body',.01)
        b.petal('Golden blade tail',[(0,.64,.87),(0,.94,1.05),(0,1.3,1.16)],.15,'Gold','Tail',cup=.01)
        for i in range(3):b.oval('Blade perforation',(0,.84+i*.10,1.0+i*.05),(.029,.032,.02),'DarkArmor','Tail',20,12)

def band(b,name,a,z,r,color,bone):
    b.tube(name,[a,z],[r,r],color,bone,32,sub=1)

def gabumon(b):
    b.bone('Body',(0,0,.55));b.bone('Head',(0,-.07,1.13),'Body');b.bone('Tail',(0,.20,.45),'Body')
    b.loft('Yellow dinosaur body',[(.25,0,.12,.11),(.35,0,.24,.19),(.59,0,.27,.21),(.84,0,.22,.17),(.95,0,.12,.12)],'Yellow','Body',56,7)
    wolf_head(b,(0,-.09,1.12),.86,pelt=True)
    b.oval('Exposed yellow lower face',(0,-.275,.96),(.16,.075,.085),'Yellow','Head')
    b.oval('Turquoise belly',(0,-.198,.59),(.16,.030,.21),'Stripe','Body')
    for s in [-1,1]:
        pts=[]
        for i in range(35):
            t=i/34;a=t*math.pi*2.3;r=.075*(1-t*.8)
            pts.append((s*(.07+r*math.cos(a)),-.233,.60+r*math.sin(a)))
        b.tube('Pink belly spiral',pts,[.009]*len(pts),'PetalLight','Body',8,sub=1)
        side='L' if s<0 else 'R';leg=b.bone('Thigh.'+side,(s*.17,0,.36),'Body');b.bone('Shin.'+side,(s*.19,-.02,.18),leg)
        b.tube('Yellow leg',[(s*.17,0,.37),(s*.21,0,.20),(s*.22,-.03,.075)],[.09,.08,.105],'Yellow',leg,24)
        for j in range(3):b.tube('Ivory foot claw',[(s*.22+(j-1)*.055,-.11,.075),(s*.22+(j-1)*.065,-.23,.025)],[.032,.001],'Ivory',leg,16)
        arm=b.bone('UpperArm.'+side,(s*.19,0,.84),'Body');hand=b.bone('Hand.'+side,(s*.39,-.04,.40),arm)
        sleeve=b.tube('Hanging wolf pelt sleeve',[(s*.19,.015,.84),(s*.32,.01,.61),(s*.42,-.035,.36)],[.115,.13,.14],'Fur',arm,32)
        b.markings(sleeve,'WolfBlue',(.45,.18,1))
        fur(b,(s*.41,-.02,.43),(.11,.08,.09),.12,'Fur',hand,12)
        for j in range(3):b.tube('Pelt hand claw',[(s*.43+(j-1)*.068,-.095,.35),(s*.43+(j-1)*.085,-.18,.26)],[.033,.001],'Purple',hand,16)
    for i in range(8):
        x=-.23+i*.066;b.petal('Jagged back pelt',[(x,.13,.91),(x,.25,.66),(x*1.1,.20,.30)],.074,'Fur','Body',cup=.02)
        b.tube('Pelt back stripe',[(x,.242,.73),(x+.035,.267,.67),(x+.01,.25,.60)],[.023,.028,.001],'WolfBlue','Body',10)
    b.tube('Yellow reptile tail',[(0,.16,.41),(0,.43,.32),(0,.66,.48),(0,.75,.72)],[.115,.085,.05,.001],'Yellow','Tail',28)
    for j in range(5):b.tube('Tail ridge',[(0,.32+j*.075,.37+j*.02),(0,.35+j*.075,.47+j*.03)],[.03,.001],'Gold','Tail',10,sub=2)

def humanoid(b,color='Peach',muscle=False,legcolor=None,armcolor=None):
    # Anatomical continuous torso. Waist narrower than ribcage; elbow/knee skin blends.
    b.bone('Body',(0,0,1.10));b.bone('Head',(0,0,1.88),'Body')
    b.loft('Anatomical torso',[(.91,0,.15,.12),(1.06,0,.22,.14),(1.23,0,.15,.105),(1.43,0,.24 if muscle else .19,.135),(1.63,0,.29 if muscle else .215,.13),(1.70,0,.16,.10),(1.79,0,.066,.068)],color,'Body',64,6)
    for s in [-1,1]:
        side='L' if s<0 else 'R';th=b.bone('Thigh.'+side,(s*.13,0,1.0),'Body');sh=b.bone('Shin.'+side,(s*.15,-.025,.55),th)
        ft=b.bone('Foot.'+side,(s*.16,0,.12),sh)
        leg=b.tube('Contoured leg',[(s*.13,0,1.02),(s*.16,0,.83),(s*.15,-.02,.55),(s*.16,.025,.37),(s*.16,.015,.12)],[.12 if muscle else .085,.12 if muscle else .09,.060,.077 if muscle else .056,.043],legcolor or color,th,32,sub=7)
        g=leg.vertex_groups.new(name=sh)
        for v in leg.data.vertices:
            w=max(0,min(1,(.64-v.co.z)/.17))
            if w:g.add([v.index],w,'REPLACE');leg.vertex_groups[th].add([v.index],1-w,'REPLACE')
        b.oval('Foot',(s*.16,-.06,.075),(.065,.13,.065),legcolor or color,ft)
        arm=b.bone('UpperArm.'+side,(s*(.25 if muscle else .195),0,1.60),'Body');fore=b.bone('Forearm.'+side,(s*.34,0,1.30),arm);hand=b.bone('Hand.'+side,(s*.40,-.03,1.01),fore)
        limb=b.tube('Contoured arm',[(s*(.25 if muscle else .195),0,1.62),(s*.30,0,1.47),(s*.34,0,1.30),(s*.38,-.025,1.17),(s*.40,-.03,1.02)],[.105 if muscle else .057,.10 if muscle else .056,.043,.066 if muscle else .049,.032],armcolor or color,arm,28,sub=7)
        g=limb.vertex_groups.new(name=fore)
        for v in limb.data.vertices:
            w=max(0,min(1,(1.38-v.co.z)/.15))
            if w:g.add([v.index],w,'REPLACE');limb.vertex_groups[arm].add([v.index],1-w,'REPLACE')
        b.oval('Palm',(s*.405,-.031,.96),(.052,.036,.075),armcolor or color,hand)
        for j in range(4):
            x=s*.405+(j-1.5)*.025
            b.tube('Articulated finger',[(x,-.03,.93),(x,-.042,.87-(.015 if j in [1,2] else 0)),(x,-.065,.86-(.015 if j in [1,2] else 0))],[.014,.012,.007],armcolor or color,hand,12,sub=3)
        b.tube('Thumb',[(s*.36,-.026,.98),(s*.342,-.06,.925),(s*.36,-.075,.90)],[.021,.016,.01],armcolor or color,hand,12)

def were(b):
    humanoid(b,'Fur',True,'Denim');wolf_head(b,(0,-.02,1.91),.86)
    for s in [-1,1]:
        b.oval('Sculpted wolf pectoral',(s*.125,-.104,1.50),(.126,.054,.09),'Fur','Body')
        for j in range(3):b.oval('Wolf abdominal muscle',(s*.064,-.108,1.22+j*.07),(.063,.027,.037),'Fur','Body')
    b.bone('Tail',(0,.11,1.02),'Body');b.tube('White wolf tail',[(0,.12,1.02),(0,.40,.82),(0,.55,.57),(0,.57,.36)],[.075,.085,.065,.001],'Fur','Tail',24)
    for ob in b.parts:
        if ob.name.startswith(('Anatomical','Contoured arm','Palm')):b.markings(ob,'WolfBlue',(.43,.15,1.9))
    for s in [-1,1]:
        side='L' if s<0 else 'R';arm='UpperArm.'+side;sh='Shin.'+side;hand='Hand.'+side
        fur(b,(s*.25,0,1.64),(.10,.12,.16),.13,'Fur',arm,16)
        b.oval('Wolf paw',(s*.16,-.055,.09),(.12,.17,.08),'Fur','Foot.'+side)
        for j in range(3):b.tube('Purple paw claw',[(s*.16+(j-1)*.073,-.16,.07),(s*.16+(j-1)*.087,-.26,.015)],[.033,.001],'Purple','Foot.'+side,16)
        for j in range(4):b.tube('Purple finger claw',[(s*.405+(j-1.5)*.025,-.06,.86),(s*.405+(j-1.5)*.03,-.10,.81)],[.015,.001],'Purple',hand,12)
        b.oval('Leather knee pad',(s*.15,-.083,.55),(.08,.028,.11),'Leather',sh)
        band(b,'Ankle binding',(s*.16,.01,.20),(s*.16,.01,.28),.07,'Cream' if s<0 else 'Leather',sh)
        b.tube('Jean outer seam',[(s*.235,0,.97),(s*.245,0,.78),(s*.21,0,.60)],[.006]*3,'Silver', 'Thigh.'+side,8)
    b.tube('Diagonal leather chest strap',[(-.22,-.135,1.68),(0,-.155,1.41),(.17,-.125,1.21)],[.046]*3,'Leather','Body',12,flat=.23)
    band(b,'Belt',(0,0,1.03),(0,0,1.10),.23,'Leather','Body')
    b.plate('Belt buckle',[(-.045,1.035),(.045,1.035),(.045,1.10),(-.045,1.10)],-.234,.018,'Silver','Body',.01)
    b.oval('Single shoulder armor',(.255,0,1.61),(.145,.15,.14),'Leather','UpperArm.R')
    band(b,'Studded wrist',(-.39,-.025,1.10),(-.40,-.03,1.02),.057,'Leather','Forearm.L')
    for j in range(5):b.oval('Metal rivet',(-.41+(j-2)*.018,-.079,1.065),(.010,.007,.01),'Silver','Forearm.L',16,10)
    for j in range(3):b.tube('Knee armor spike',[(.15+(j-1)*.04,-.11,.56),(.15+(j-1)*.04,-.20,.59)],[.018,.001],'Silver','Shin.R',12)
    b.oval('Jean skull',(-.15,-.116,.78),(.045,.008,.050),'Ivory','Thigh.L')
    for s in [-1,1]:b.oval('Skull socket',(-.15+s*.017,-.125,.787),(.009,.003,.013),'Black','Thigh.L',16,10)

def humanface(b,color='Peach',mask=False,eyes=True):
    b.loft('Sculpted facial planes',[(1.79,-.008,.025,.03),(1.83,-.01,.070,.055),(1.91,0,.107,.077),(1.99,.006,.11,.08),(2.07,.012,.09,.075),(2.10,.015,.026,.029)],color,'Head',64,7)
    if eyes:
        for s in [-1,1]:
            if mask:b.petal('Angular red eye mask',[(s*.018,-.081,1.97),(s*.065,-.098,2.0),(s*.123,-.061,2.015)],.038,'Rose','Head',cup=.008)
            b.almond('Narrow expressive eye',(s*.051,-.084,1.965),.040,.022,'Gold' if mask else 'Leather','Head',s*.008)
    b.tube('Nose bridge',[(0,-.071,1.97),(0,-.102,1.92),(0,-.107,1.914)],[.014,.018,.009],color,'Head',16)
    b.tube('Upper lip',[(-.029,-.076,1.87),(0,-.084,1.865),(.029,-.076,1.87)],[.004,.006,.004],'Rose','Head',12)
    b.tube('Lower lip',[(-.024,-.079,1.863),(0,-.084,1.855),(.024,-.079,1.863)],[.003,.005,.003],'PetalLight' if color=='Peach' else 'Silver','Head',10)

def cloth(b,name,top,bottom,width,color,inner,bone,points=7):
    vs=[];fs=[];N=32;M=48
    for i in range(N+1):
        t=i/N
        for j in range(M+1):
            u=j/M*2-1;x=u*width*(.45+.55*t)
            z=top*(1-t)+bottom*t+.10*t**8*(.5+.5*math.cos(u*math.pi*points))
            y=.13+.19*t+.09*math.cos(u*math.pi*3)*t+.14*abs(u)*(1-t)
            vs.append((x,y,z))
    for i in range(N):
        for j in range(M):
            k=i*(M+1)+j;fs.append((k,k+1,k+M+2,k+M+1))
    ob=b.mesh(name,vs,fs,color,bone);ob.data.materials.append(b.mat(inner))
    mod=ob.modifiers.new('Two sided fabric','SOLIDIFY');mod.thickness=.012;mod.material_offset=1
    bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)

def rosebloom(b,center,radius,bone):
    # Nested petals wrap around a bowl; each has curled lip and overlapping ends.
    cx,cy,cz=center
    for ring in range(4):
        count=7 if ring<2 else 5;r=radius*(1-ring*.21)
        for i in range(count):
            a=i*math.tau/count+ring*.6;v=[];f=[]
            for j in range(13):
                t=j/12
                for k in range(17):
                    u=k/16;ang=a+(u-.5)*math.tau/count*1.3
                    rr=r*(.44+.56*t)+.012*math.sin(t*math.pi)
                    z=cz+ring*.034+t*.16+.02*math.sin(u*math.pi)-.028*t**8
                    v.append((cx+math.cos(ang)*rr,cy+math.sin(ang)*rr,z))
            for j in range(12):
                for k in range(16):
                    q=j*17+k;f.append((q,q+1,q+18,q+17))
            ob=b.mesh('Overlapping curled rose petal',v,f,'RoseLight' if (ring+i)%3==0 else 'Rose',bone)
            mod=ob.modifiers.new('Petal thickness','SOLIDIFY');mod.thickness=.005;bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)

def fairy(b):
    rose=b.name=='Rosemon';humanoid(b,'Peach',False,'Black' if rose else 'Peach','Rose' if rose else 'Peach')
    humanface(b,eyes=not rose)
    # Fit garment panels to the torso; exposed skin follows only reference cutouts.
    torso=next(o for o in b.parts if o.name.startswith('Anatomical torso'))
    torso.data.materials.append(b.mat('Rose' if rose else 'Petal'))
    for p in torso.data.polygons:
        z=sum(torso.data.vertices[i].co.z for i in p.vertices)/len(p.vertices)
        if 1.04<z<1.58:p.material_index=1
    if rose:
        rosebloom(b,(0,0,1.91),.24,'Head')
        for s in [-1,1]:
            b.petal('Green collar',[(0,0,1.72),(s*.17,.03,1.82),(s*.27,.10,1.97)],.09,'Leaf','Body',True,.025)
            pts=[]
            for j in range(60):
                t=j/59;a=t*math.tau*1.6
                pts.append((.175*math.cos(a),.13*math.sin(a)-.003,1.13+t*.47))
            if s<0:b.tube('Gold bodice vine',pts,[.011]*len(pts),'Gold','Body',10,sub=1)
            hand='Hand.'+('L' if s<0 else 'R')
            b.tube('Long thorn whip',[(s*.41,-.07,.91),(s*.68,-.04,.69),(s*.76,.05,.23),(s*.95,.03,.08)],[.013,.012,.008,.001],'Gold',hand,12)
            for j in range(6):b.tube('Whip thorn',[(s*(.59+j*.032),-.035,.72-j*.072),(s*(.65+j*.032),-.03,.75-j*.072)],[.012,.001],'Gold',hand,8,sub=2)
        b.oval('Purple collar gem',(0,-.125,1.72),(.06,.035,.06),'PurpleLight','Body')
        cloth(b,'Leaf cape',1.70,.41,.52,'Leaf','Cream','Body',6)
        for i in range(14):
            x=(i-6.5)*.014;b.tube('Golden ponytail',[(x,.07,2.01),(x*1.6,.18,1.79),(x*.9,.28,1.15),(x*.7,.31,.83)],[.022,.024,.018,.001],'Gold','Head',10)
        for s in [-1,1]:rosebloom(b,(s*.09,.30,1.0),.07,'Body')
    else:
        b.loft('Closed pink flower helmet',[(1.995,.015,.101,.078),(2.08,.015,.13,.105),(2.18,.015,.085,.078),(2.24,.015,.005,.006)],'Petal','Head',56,6)
        for i in range(7):
            a=i*math.tau/7
            b.petal('Pink bud helmet',[(.09*math.cos(a),.075*math.sin(a),1.96),(.15*math.cos(a),.12*math.sin(a),2.10),(.115*math.cos(a),.09*math.sin(a),2.27)],.051,'PetalLight' if i%3==0 else 'Petal','Head',cup=.008)
        b.tube('Curled golden stamen',[(0,0,2.19),(.03,0,2.32),(.08,.01,2.35),(.10,.01,2.31)],[.017,.015,.012,.004],'Yellow','Head',12)
        for i in range(7):
            a=i*math.tau/7
            b.petal('Layered petal skirt',[(.16*math.cos(a),.13*math.sin(a),1.14),(.24*math.cos(a),.18*math.sin(a),1.02),(.29*math.cos(a),.22*math.sin(a),.89)],.11,'Petal' if i%2 else 'PetalLight','Body',cup=.025)
        flower(b,(0,-.14,1.57),.072,'Yellow','Body',6)
        flower(b,(0,-.151,1.14),.08,'Yellow','Body',6)
        for s in [-1,1]:
            side='L' if s<0 else 'R';wing=b.bone('Wing.'+side,(s*.12,.10,1.60),'Body')
            b.petal('Upper leaf wing',[(s*.12,.11,1.60),(s*.57,.24,1.98),(s*.79,.28,2.29)],.14,'Leaf',wing,True,.07)
            b.petal('Lower leaf wing',[(s*.12,.15,1.57),(s*.50,.29,1.34),(s*.72,.34,.97)],.13,'Leaf',wing,True,.07)
            b.tube('Leaf glove',[(s*.37,-.019,1.23),(s*.40,-.03,1.02)],[.050,.034],'Leaf','Forearm.'+side,28)
            b.tube('Green leaf boot',[(s*.16,.015,.12),(s*.16,.025,.37),(s*.15,-.02,.57)],[.047,.059,.070],'Leaf','Shin.'+side,28)
            flower(b,(s*.16,-.16,.11),.094,'Yellow','Foot.'+side,8)
            flower(b,(s*.37,-.063,1.21),.06,'Yellow','Forearm.'+side,6)
        for i in range(7):
            x=(i-3)*.029;b.petal('Green leaf hair',[(x,.065,2.02),(x*1.6,.13,1.81),(x*1.9,.17,1.61)],.041,'Leaf','Head',True,.018)
        for ob in b.parts:
            if ob.name.startswith(('Palm','Articulated finger','Thumb')):
                ob.data.materials.clear();ob.data.materials.append(b.mat('Leaf'))
    # Fairy head proportion is larger than the adult vampire's.
    from mathutils import Matrix
    factor=1.18 if rose else 1.35;pivot=Vector((0,0,1.79))
    for ob in b.parts:
        if ob.vertex_groups.get('Head'):ob.matrix_world=Matrix.Translation(pivot)@Matrix.Scale(factor,4)@Matrix.Translation(-pivot)@ob.matrix_world

def batwing(b,root,tips,bone,rib='DemonSkin',membrane='Black'):
    root=Vector(root);boundary=[]
    for i in range(len(tips)-1):
        a,z=Vector(tips[i]),Vector(tips[i+1]);mid=(a+z)*.5;control=mid+(root-mid)*.25
        for j in range(13):
            t=j/12;boundary.append((1-t)**2*a+2*(1-t)*t*control+t*t*z)
    vs=[];fs=[];R=18;B=len(boundary)
    for j in range(R+1):
        t=.003+.997*j/R
        for q in boundary:vs.append(root*(1-t)+q*t+Vector((0,.035*math.sin(t*math.pi),0)))
    for j in range(R):
        for i in range(B-1):
            # Torn perforations in the large demons' membrane, away from finger ribs.
            torn=b.name in ['Devimon','VenomMyotismon'] and 8<j<15 and (i%13 in [5,6]) and ((i//13)%2==0)
            if not torn:
                k=j*B+i;fs.append((k,k+1,k+B+1,k+B))
    ob=b.mesh('Scalloped bat membrane',vs,fs,membrane,bone)
    mod=ob.modifiers.new('Membrane thickness','SOLIDIFY');mod.thickness=.010;bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
    for i,t in enumerate(tips):
        t=Vector(t);b.tube('Bat finger rib',[root,(root+t)*.5+Vector((0,-.025,.03)),t],[.027,.018,.003],rib,bone,12)
    b.tube('Membrane scalloped edge',boundary,[.009]*len(boundary),rib,bone,8,sub=1)

def demi(b):
    b.bone('Body',(0,0,.66));b.bone('Head',(0,0,.77),'Body')
    b.oval('Round blue bat body',(0,0,.73),(.34,.27,.32),'BlueSkin','Body',64,48)
    fur(b,(0,.04,.50),(.29,.22,.10),.12,'DarkArmor','Body',38)
    for s in [-1,1]:
        b.almond('Bat golden eye',(s*.135,-.252,.79),.091,.086,'Gold','Body',s*.025)
        b.tube('Bat head feeler',[(s*.18,.015,.98),(s*.20,.01,1.21),(s*.29,.005,1.31)],[.046,.035,.001],'BlueSkin','Head',16)
        wing=b.bone('Wing.'+('L' if s<0 else 'R'),(s*.26,.03,.84),'Body')
        batwing(b,(s*.25,.02,.84),[(s*.39,.04,1.13),(s*.83,.08,1.22),(s*.68,.055,.80),(s*.54,.03,.61),(s*.29,.02,.62)],wing,'DarkArmor','WolfBlue')
        leg=b.bone('Leg'+str(s),(s*.14,.03,.47),'Body')
        b.tube('Pale thin leg',[(s*.14,.03,.47),(s*.18,.015,.25),(s*.21,-.02,.13)],[.030,.025,.03],'Pale',leg,20)
        for j in range(3):b.tube('Red hooked foot claw',[(s*.21+(j-1)*.055,-.02,.13),(s*.21+(j-1)*.08,-.13,.07),(s*.21+(j-1)*.09,-.20,.02)],[.026,.020,.001],'Ruby',leg,16)
    b.smile('Bat grin',(0,-.267,.60),.18,.06,'Body',True)
    for j in range(6):b.tube('Grin teeth',[(-.105+j*.042,-.305,.629),(-.105+j*.042,-.31,.59)],[.018,.001],'Ivory','Body',12,sub=2)
    b.oval('Forehead skull',(0,-.227,.974),(.070,.020,.07),'Ivory','Head')
    for s in [-1,1]:b.oval('Skull black socket',(s*.026,-.249,.983),(.018,.009,.025),'Black','Head',20,14)
    for i in range(7):b.tube('Nose scar stitch',[(-.105+i*.035,-.277,.716),(-.095+i*.035,-.280,.746)],[.005,.005],'Leather','Body',8,sub=1)

def batcrest(b,pos,size,color,bone):
    x,y,z=pos
    shape=[(-1,.25),(-.7,.05),(-.55,-.25),(-.35,.02),(-.12,-.25),(0,-.46),(.12,-.25),(.35,.02),(.55,-.25),(.7,.05),(1,.25),(.53,.15),(.25,.38),(.13,.17),(-.13,.17),(-.25,.38),(-.53,.15)]
    b.plate('Bat emblem',[(x+a*size,z+c*size) for a,c in shape],y,.008,color,bone)

def skull(b,pos,size,bone):
    x,y,z=pos;b.oval('Skull emblem',(x,y,z),(size,.011,size*1.15),'Ivory',bone,24,16)
    for s in [-1,1]:b.oval('Skull eye socket',(x+s*size*.36,y-.012,z+size*.18),(size*.22,.006,size*.28),'Black',bone,16,10)
    for j in range(3):b.tube('Skull teeth',[(x+(j-1)*size*.30,y-.01,z-size*.7),(x+(j-1)*size*.30,y-.01,z-size*1.1)],[size*.12,size*.10],'Ivory',bone,8,sub=1)

def demon(b):
    myo=b.name=='Myotismon';venom=b.name=='VenomMyotismon'
    color='Suit' if myo else 'DemonRed' if venom else 'DemonSkin'
    humanoid(b,color,True,'FurDark' if venom else 'Black')
    humanface(b,'Pale',mask=myo or venom)
    if myo or venom:
        for i in range(20):
            a=i*math.tau/20
            b.tube('Layered golden hair',[(.078*math.cos(a),.069*math.sin(a),2.083),(.115*math.cos(a),.08*math.sin(a),2.0),(.13*math.cos(a),.08*math.sin(a)+.018,1.82 if myo else 1.70)],[.017,.020,.001],'Gold','Head',10)
        for s in [-1,1]:b.tube('Front hair strand',[(s*.035,-.042,2.09),(s*.06,-.081,2.02),(s*.08,-.09,1.88)],[.019,.017,.001],'Gold','Head',12)
    else:
        b.loft('Black demon hood',[(1.91,.013,.112,.065),(1.97,.018,.118,.076),(2.05,.022,.093,.070),(2.10,.018,.03,.025)],'DemonSkin','Head',56,5)
        for s in [-1,1]:b.almond('Slanted demon eye',(s*.05,-.083,1.97),.043,.012,'Ruby','Head',s*.013)
        b.smile('Demon fang grin',(0,-.081,1.884),.057,.022,'Head',True)
    if myo:
        cloth(b,'Black outside red lined cape',1.72,.12,.60,'Black','Ruby','Body',7)
        for s in [-1,1]:
            b.plate('High angular vampire collar',[(s*.09,1.72),(s*.36,1.94),(s*.35,2.23),(s*.20,2.10)],.08,.025,'Black','Body',.005)
            b.plate('Crimson angular collar lining',[(s*.105,1.75),(s*.335,1.95),(s*.331,2.19),(s*.213,2.08)],.066,.005,'Ruby','Body',.003)
            b.oval('Gold shoulder epaulet',(s*.25,-.005,1.64),(.145,.14,.055),'Gold','UpperArm.'+('L' if s<0 else 'R'))
            for j in range(3):b.oval('Gold jacket button',(s*.093,-.143,1.31+j*.10),(.022,.012,.022),'Gold','Body',20,14)
            b.tube('Jacket gold piping',[(s*.22,-.102,1.63),(s*.17,-.15,1.50),(s*.08,-.125,1.20)],[.009]*3,'Gold','Body',10)
            hand='Hand.'+('L' if s<0 else 'R');fore='Forearm.'+('L' if s<0 else 'R')
            band(b,'Emerald cuff',(s*.386,-.025,1.13),(s*.40,-.03,1.03),.052,'Emerald',fore)
            # Recolor exposed hands and fingers only.
            for ob in b.parts:
                if ob.name.startswith(('Palm','Articulated finger','Thumb')) and ob.vertex_groups.get(hand):
                    ob.data.materials.clear();ob.data.materials.append(b.mat('Pale'))
            skull(b,(s*.16,-.176,.07),.037,'Foot.'+('L' if s<0 else 'R'))
        band(b,'Black waist belt',(0,0,1.05),(0,0,1.13),.232,'Black','Body')
        b.plate('Gold belt buckle',[(-.05,1.045),(.05,1.045),(.05,1.135),(-.05,1.135)],-.236,.012,'Gold','Body',.012)
    else:
        for s in [-1,1]:
            side='L' if s<0 else 'R';wing=b.bone('Wing.'+side,(s*.20,.10,1.64),'Body')
            if venom:tips=[(s*.37,.14,2.36),(s*1.07,.22,2.55),(s*.97,.26,1.85),(s*.82,.25,1.34),(s*.47,.19,1.24)]
            else:tips=[(s*.43,.13,2.36),(s*.76,.22,2.12),(s*.77,.24,1.43),(s*.66,.25,.67),(s*.45,.20,.28),(s*.26,.13,1.20)]
            batwing(b,(s*.20,.10,1.64),tips,wing,'DemonRed' if venom else 'DemonSkin')
            if venom:batwing(b,(s*.23,.13,1.34),[(s*.54,.20,1.71),(s*.78,.29,1.31),(s*.61,.27,.87),(s*.32,.20,1.01)],wing,'DemonRed')
            b.tube('Swept demon horn',[(s*.07,.0,2.07),(s*.23,.015,2.11),(s*.36,.018,2.22 if not venom else 2.12)],[.047,.032,.001],color,'Head',20)
            hand='Hand.'+side;fore='Forearm.'+side
            band(b,'Black wrist straps',(s*.38,-.025,1.16),(s*.40,-.03,1.02),.067,'Black',fore)
            for j in range(3):b.plate('Silver wrist buckle',[(s*.40-.023,1.045+j*.035),(s*.40+.023,1.045+j*.035),(s*.40+.023,1.068+j*.035),(s*.40-.023,1.068+j*.035)],-.095,.008,'Silver',fore,.003)
            for j in range(4):
                x=s*.405+(j-1.5)*(.04 if venom else .025)
                b.tube('Long curved demon finger',[(x,-.03,.945),(x+(j-1.5)*.02,-.06,.78),(x+(j-1.5)*.023,-.115,.67),(x+(j-1.5)*.016,-.16,.70)],[.027 if venom else .016,.024 if venom else .012,.014 if venom else .009,.001],color,hand,16)
            if venom:
                b.oval('Broad red hand',(s*.405,-.03,.96),(.086,.051,.10),'DemonRed',hand)
                batcrest(b,(s*.405,-.084,.963),.055,'Gold',hand)
                for j in range(5):fur(b,(s*.16,0,.30+j*.15),(.13,.10,.12),.095,'FurDark','Shin.'+side if j<2 else 'Thigh.'+side,14)
                for j in range(6):b.tube('Purple fur stripe',[(s*.16-.09,-.089,.36+j*.105),(s*.16,-.126,.32+j*.105),(s*.16+.085,-.09,.36+j*.105)],[.015,.019,.001],'Purple','Thigh.'+side if j>2 else 'Shin.'+side,10)
                b.oval('Huge pale foot',(s*.16,-.08,.08),(.15,.20,.075),'Pale','Foot.'+side)
                for j in range(3):b.tube('Purple foot claw',[(s*.16+(j-1)*.09,-.20,.07),(s*.16+(j-1)*.11,-.31,.02)],[.035,.001],'Purple','Foot.'+side,16)
            else:
                for z in [.49,.73,1.04]:band(b,'Leather leg strap',(s*.16,0,z),(s*.16,0,z+.03),.083,'Leather','Shin.'+side if z<.6 else 'Thigh.'+side)
        if venom:
            b.oval('Abdomen shadow face',(0,-.132,1.025),(.14,.027,.12),'Black','Body')
            for s in [-1,1]:b.almond('Abdomen demon eye',(s*.067,-.163,1.055),.045,.014,'HairRed','Body',s*.016)
            b.smile('Abdomen demon mouth',(0,-.159,.971),.085,.025,'Body',True)
            for s in [-1,1]:b.oval('Red pectoral',(s*.13,-.094,1.51),(.13,.061,.095),'DemonRed','Body')
            for j in range(4):
                for s in [-1,1]:b.oval('Segmented red abdomen',(s*.065,-.104,1.17+j*.072),(.066,.032,.042),'DemonRed','Body')
            b.bone('Tail',(0,.10,1.0),'Body')
            b.tube('Silver segmented tail',[(0,.12,1.0),(0,.35,.74),(0,.55,.38),(0,.66,.10)],[.065,.055,.035,.001],'Silver','Tail',24)
            for j in range(8):b.tube('Tail armor ring',[(-.045,.20+j*.045,.88-j*.085),(0,.17+j*.045,.88-j*.085),(.045,.20+j*.045,.88-j*.085)],[.008]*3,'SteelDark','Tail',10)
        else:
            batcrest(b,(0,-.139,1.50),.19,'Ruby','Body')
            skull(b,(-.25,-.090,1.63),.045,'UpperArm.L');skull(b,(.16,-.064,.30),.034,'Shin.R')
    if not myo:
        if not venom:
            face=next(o for o in b.parts if o.name.startswith('Sculpted facial planes'))
            face.data.materials.append(b.mat('DemonSkin'))
            for p in face.data.polygons:
                if sum(face.data.vertices[i].co.z for i in p.vertices)/len(p.vertices)>1.91:p.material_index=1
        # Stretch the arms from the fixed shoulder, and match joint rest positions.
        from mathutils import Matrix
        stretch=1.43 if venom else 1.23
        for ob in b.parts:
            if any(g.name.startswith(('UpperArm.','Forearm.','Hand.')) for g in ob.vertex_groups):
                ob.matrix_world=Matrix.Translation((0,0,1.62))@Matrix.Diagonal((1,1,stretch,1))@Matrix.Translation((0,0,-1.62))@ob.matrix_world
        for n,(p,parent) in list(b.bones.items()):
            if n.startswith(('UpperArm.','Forearm.','Hand.')):b.bones[n]=((p[0],p[1],1.62+(p[2]-1.62)*stretch),parent)
    if venom:
        # Broad upper body, longer legs; preserve mesh and rig coordinate agreement.
        for ob in b.parts:
            ob.location.x*=1.45;ob.scale.x*=1.45
        b.bones={n:((p[0]*1.45,p[1],p[2]),parent) for n,(p,parent) in b.bones.items()}

def build_character(b):
    if b.name in ['Palmon','Togemon']:plant(b)
    elif b.name in ['Garurumon','MetalGarurumon']:quadruped(b)
    elif b.name=='Gabumon':gabumon(b)
    elif b.name=='WereGarurumon':were(b)
    elif b.name in ['Lillymon','Rosemon']:fairy(b)
    elif b.name=='DemiDevimon':demi(b)
    elif b.name in ['Devimon','Myotismon','VenomMyotismon']:demon(b)
    else:raise RuntimeError('Recipe pending '+b.name)

def build_next(name,revision=False):
    korean,costFolder,h,w,d,kind=NEW[name];folder=PROJECT/'Assets/Resources/Digimon'/costFolder/name
    if (folder/(name+'.prefab')).exists() and not revision:raise RuntimeError('Already integrated: '+name)
    refs=[p for p in REFERENCES.iterdir() if p.is_file() and (p.name.startswith(korean+'_') or p.name.startswith(korean+'-'))]
    for view in ['앞','옆','뒤']:
        if not any(p.stem.endswith(view) for p in refs):raise RuntimeError('Missing reference '+korean+view)
    (folder/'Source~').mkdir(parents=True,exist_ok=True)
    b=Detailed(name)
    if name in ['Tsunomon','Tanemon','Pagumon']:small(b)
    else:build_character(b)
    report=b.finalize(folder)
    for material in report['materials']:material['vertexColor']=True
    report.update(name=name,korean=korean,costFolder=costFolder,targetHeight=h,maxWidth=w,maxDepth=d,
        references=[{'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in refs],
        clips=[{'name':n,'firstFrame':a,'lastFrame':z,'loop':loop} for n,a,z,loop in [('Idle',1,49,True),('Walk',60,84,True),('Attack',90,108,False),('Death',120,150,False)]])
    (folder/'Source~'/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    (folder/'Source~'/'build_model.py').write_text(Path(__file__).read_text(encoding='utf-8')+'\nresult=build_next('+repr(name)+')\n',encoding='utf-8')
    return {k:v for k,v in report.items() if k not in ['materials','references','clips']}

result=build_next('Gabumon')
