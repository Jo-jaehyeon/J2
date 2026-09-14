"""Blender-authored enclosing lakeshore for the isolated J2 multiplayer map.

No operators, external files, or scene changes. Geometry is deterministic and
batched by material. Coordinates: XY ground plane, Z elevation, lake Z=-0.72.
"""
import math
import random
import bpy
from mathutils import Vector

ENV_PALETTE = {
    'LakeDeep': '278FA7', 'LakeBlue': '2994AA', 'LakeTeal': '36A5B5',
    'LakeShallow': '66C3B6', 'LakeGlint': 'A2DEDC',
    'ShoreWet': '8EAB96', 'ShoreSand': 'D3C79C', 'ShoreSandLight': 'E0D3AA',
    'ShoreEarth': '8B9164', 'ShoreMeadow': '83A365', 'ShoreMoss': '688657',
    'CliffStone': '93A398', 'CliffLight': 'A0ADA0', 'CliffShade': '87998F',
    'DistantStone': '81A2A1', 'DistantLight': 'A4B8B0',
    'ForestBark': '6F7356', 'ForestBarkLight': '979478',
    'ForestDeep': '416E51', 'ForestLeaf': '568551', 'ForestLight': '779B58',
    'ForestGold': '92AB63', 'WaterfallBlue': '7ACDD5',
    'WaterfallWhite': 'DEF3E7', 'WaterfallShade': '4FA9B8', 'FoamPearl': 'BFE6D7',
}

TAU = math.tau
WATER_Z = -0.72

def _noise(x, y):
    return (math.sin(x * .093 + math.cos(y * .051)) * .46
            + math.sin(y * .137 - x * .048) * .26
            + math.sin(x * .377 + y * .211) * .17
            + math.cos(x * .891 - y * .693) * .11)

def _shore(a):
    # Continuous shore outside all battlefields (minimum radius > 79 m).
    return 84.5 + 3.1 * math.sin(3*a+.8) + 1.9*math.cos(5*a-1) + .7*math.sin(11*a)

def _mix(a, b, t):
    return a + (b-a)*t

def _profile(q):
    points = [(-3,-1.1),(0,-.45),(4,.3),(9,2.0),(13.4,4.8),
              (14,13.7),(20,15.4),(23,17.0),(23.6,30.5),
              (29,32.2),(35,35.0),(45,38.0),(80,25.0)]
    for (a,h),(b,k) in zip(points, points[1:]):
        if q <= b:
            return _mix(h,k,max(0,min(1,(q-a)/(b-a))))
    return 25

FALL_ANGLES = (1.08, 2.10)

def _height(a, q):
    r = _shore(a)+q
    x, y = r*math.cos(a), r*math.sin(a)
    if q <= 0:
        return -.86 + max(-2,q)*.10
    # Lower front shore preserves an open view across the eight arenas.
    north = max(0, math.sin(a))
    h = .2 + q*.074 + _noise(x,y)*min(1,q/6)*1.7
    h += max(0,q-6)*(.07+.16*north)
    peaks = [(-70,133,38,25,23),(-33,145,58,29,29),
             (14,147,65,26,26),(58,136,42,28,24),(-105,73,17,26,24),(106,71,21,23,26)]
    for px,py,ph,sx,sy in peaks:
        g=math.exp(-(((x-px)/sx)**2+((y-py)/sy)**2)*1.5)
        h += ph*g*min(1,q/12)
    h += max(0,h-5)*(.13*_noise(x*1.8,y*1.8)+.07*math.sin(a*37+q*.43))
    # Carved stepped river corridors make every waterfall rest against land.
    for fa in FALL_ANGLES:
        center=fa+.007*math.sin(q*.21)
        delta=abs(math.atan2(math.sin(a-center),math.cos(a-center)))*r
        if delta < 13 and q < 46:
            shoulder=max(0,1-delta/13)
            h=max(h,(_profile(q)+2.5)*shoulder+h*(1-shoulder))
            if delta < 4.6:
                channel=(1-delta/4.6)**.42
                h=_mix(h,_profile(q)-.5,channel)
    return h


class _Batches:
    def __init__(self):
        self.parts={}

    def mesh(self, mat, verts, faces):
        dst_v,dst_f=self.parts.setdefault(mat,([],[]))
        offset=len(dst_v)
        dst_v.extend(verts)
        dst_f.extend(tuple(offset+i for i in f) for f in faces)

    def tube(self, mat, a, b, r0, r1=None, sides=6):
        a,b=Vector(a),Vector(b)
        axis=(b-a).normalized()
        tangent=axis.cross(Vector((0,0,1)))
        if tangent.length<.01:tangent=axis.cross(Vector((1,0,0)))
        tangent.normalize(); bitangent=axis.cross(tangent).normalized()
        verts=[]
        for p,r in [(a,r0),(b,r0 if r1 is None else r1)]:
            for i in range(sides):
                angle=i*TAU/sides
                verts.append(tuple(p+(tangent*math.cos(angle)+bitangent*math.sin(angle))*r))
        faces=[tuple(range(sides-1,-1,-1)),tuple(range(sides,2*sides))]
        faces += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
        self.mesh(mat,verts,faces)

    def ellipsoid(self, mat, p, scale, seed=0, sides=9, rings=5, rough=.13):
        rnd=random.Random(seed); phase=rnd.random()*TAU
        verts=[(p[0],p[1],p[2]+scale[2])]
        for j in range(1,rings):
            latitude=math.pi*j/rings
            for i in range(sides):
                a=TAU*i/sides
                perturb=1+rough*(.55*math.sin(a*3+phase+j*.5)+.45*rnd.uniform(-1,1))
                verts.append((p[0]+math.sin(latitude)*math.cos(a)*scale[0]*perturb,
                              p[1]+math.sin(latitude)*math.sin(a)*scale[1]*perturb,
                              p[2]+math.cos(latitude)*scale[2]*perturb))
        verts.append((p[0],p[1],p[2]-scale[2])); bottom=len(verts)-1
        faces=[]
        for i in range(sides):faces.append((0,1+i,1+(i+1)%sides))
        for j in range(rings-2):
            k=1+j*sides
            for i in range(sides):faces.append((k+i,k+sides+i,k+sides+(i+1)%sides,k+(i+1)%sides))
        k=1+(rings-2)*sides
        for i in range(sides):faces.append((k+i,bottom,k+(i+1)%sides))
        self.mesh(mat,verts,faces)

    def finish(self, scene, mats, root):
        tri_count=0
        for mat,(verts,faces) in self.parts.items():
            if not verts:continue
            mesh=bpy.data.meshes.new('LakeLandscape_'+mat)
            mesh.from_pydata(verts,[],faces);mesh.materials.append(mats[mat]);mesh.update()
            obj=bpy.data.objects.new('LakeLandscape_'+mat,mesh)
            scene.collection.objects.link(obj);obj.parent=root
            if mat.startswith(('Forest','Lake','Waterfall','Foam')):
                for polygon in mesh.polygons:polygon.use_smooth=True
            tri_count+=sum(len(f)-2 for f in faces)
        root['environment_triangles']=tri_count
        return root


def _terrain(batch):
    segments=320
    # Fine radial sampling supports natural strata and defined river ledges.
    qs=[-2,0,1.0,2.3,3.8,5.5,7.5,9.5,11.5,13.4,14,16,18,20,22,23,23.6,25,27,29,32,35,39,43,48,54,61,69,78,90]
    rings=[]
    for q in qs:
        ring=[]
        for i in range(segments):
            a=i*TAU/segments
            r=_shore(a)+q
            # Angular features continue smoothly across the complete shore.
            ring.append((r*math.cos(a),r*math.sin(a),_height(a,q)))
        rings.append(ring)
    for j in range(len(qs)-1):
        for i in range(segments):
            ni=(i+1)%segments
            vs=[rings[j][i],rings[j][ni],rings[j+1][ni],rings[j+1][i]]
            for ids in [(0,2,1),(0,3,2)]:
                tri=[vs[k] for k in ids]
                n=(Vector(tri[1])-Vector(tri[0])).cross(Vector(tri[2])-Vector(tri[0])).normalized()
                z=sum(v[2] for v in tri)/3
                if j==0:mat='ShoreWet'
                elif j<3:mat='ShoreSandLight' if (i*13+j*7)%9<3 else 'ShoreSand'
                elif z>43:mat='DistantStone'
                elif n.z<.70:mat='CliffStone'
                elif j<5:mat='ShoreEarth' if (i+j)%4==0 else 'ShoreMeadow'
                else:mat='ShoreMoss' if _noise(tri[0][0],tri[0][1])<0 else 'ShoreMeadow'
                batch.mesh(mat,tri,[(0,1,2)])
    # Sculpted outside skirt closes the annular land, hidden below the horizon.
    for i in range(segments):
        ni=(i+1)%segments;a,b=rings[-1][i],rings[-1][ni]
        batch.mesh('CliffShade',[a,b,(b[0],b[1],-8),(a[0],a[1],-8)],[(0,3,2,1)])


def _lake(batch, rnd):
    segments=240
    # A disk fitted to the enclosing shore, with shallow teal along its margin.
    ring_ratios=[0,.15,.3,.45,.6,.73,.84,.91,.955,1.015]
    for j in range(len(ring_ratios)-1):
        for i in range(segments):
            verts=[]
            for t,k in [(ring_ratios[j],i),(ring_ratios[j],i+1),(ring_ratios[j+1],i+1),(ring_ratios[j+1],i)]:
                a=k*TAU/segments;r=(_shore(a)-.4)*t
                x,y=r*math.cos(a),r*math.sin(a)
                z=WATER_Z
                verts.append((x,y,z))
            if j>=8:mat='LakeShallow'
            elif j>=6:mat='LakeTeal'
            else:mat='LakeDeep'
            batch.mesh(mat,verts,[(0,3,2)] if j==0 else [(0,3,2,1)])
    # Sparse, tapered wave crests; the center remains visually quiet for play.
    for i in range(170):
        a=rnd.random()*TAU;r=rnd.uniform(7,_shore(a)-3)
        x,y=r*math.cos(a),r*math.sin(a)
        length=rnd.uniform(.35,1.8);width=rnd.uniform(.018,.05)
        v=[]
        for k in range(7):
            t=k/6;dx=(t-.5)*length;dy=math.sin(t*math.pi)*.10
            w=width*math.sin(t*math.pi)
            v.extend([(x+dx,y+dy-w,WATER_Z+.035),(x+dx,y+dy+w,WATER_Z+.035)])
        batch.mesh('LakeGlint',v,[(2*k,2*k+2,2*k+3,2*k+1) for k in range(6)])
    # Short broken foam lace at the beach, following the natural lake outline.
    for i in range(160):
        if i%7 in (0,1,5):continue
        a0=i*TAU/160;a1=a0+TAU/220
        v=[]
        for k in range(6):
            a=_mix(a0,a1,k/5);r=_shore(a)-.85
            w=.09*math.sin(math.pi*k/5)
            v.extend([((r-w)*math.cos(a),(r-w)*math.sin(a),WATER_Z+.04),
                      ((r+w)*math.cos(a),(r+w)*math.sin(a),WATER_Z+.04)])
        batch.mesh('FoamPearl',v,[(2*k,2*k+1,2*k+3,2*k+2) for k in range(5)])


def _crags(batch, rnd):
    # Narrow, fractured limestone towers rise from broad, continuous foothills.
    for index,(x,y,height,width) in enumerate([(-75,127,23,12),(-51,137,32,14),(-28,146,39,15),(-2,151,34,12),(20,145,46,17),(44,139,32,13),(69,129,23,12)]):
        a=math.atan2(y,x);q=math.hypot(x,y)-_shore(a)
        base=_height(a,q)-5
        height=max(10,min(height,82-base))
        sides=15; levels=[(0,1.15),(.15,1),(.39,.88),(.58,.73),(.77,.54),(.93,.29),(1.0,.04)]
        vs=[]
        for level,(z,radius) in enumerate(levels):
            for k in range(sides):
                an=k*TAU/sides
                jag=1+.21*math.sin(k*2.37+index)+rnd.uniform(-.07,.07)
                dx=math.sin(level*.8+index)*width*.15
                dy=math.cos(level*.7+index)*width*.10
                vs.append((x+dx+math.cos(an)*width*radius*jag,
                           y+dy+math.sin(an)*width*.72*radius*jag,
                           base+height*z+math.sin(k*3.5+index)*height*.035*(1-z)))
        for j in range(len(levels)-1):
            for k in range(sides):
                face=(j*sides+k,j*sides+(k+1)%sides,(j+1)*sides+(k+1)%sides,(j+1)*sides+k)
                mat='DistantLight' if k%5 in (1,2) else ('DistantStone' if k%5==3 else 'CliffStone')
                batch.mesh(mat,[vs[n] for n in face],[(0,1,2),(0,2,3)])



def _tree(batch, rnd, x,y,z,h, seed):
    lean=Vector((rnd.uniform(-.45,.45),rnd.uniform(-.45,.45),0))
    base=Vector((x,y,z-.13));fork=base+Vector((0,0,h*.49))+lean*.3
    top=base+Vector((0,0,h*.88))+lean
    batch.tube('ForestBark',base,fork,h*.055,h*.037,7)
    batch.tube('ForestBarkLight',fork,top,h*.037,h*.012,6)
    # Buttress roots and irregular branching retain a convincing tree silhouette.
    for i in range(3):
        a=i*TAU/3+rnd.uniform(-.4,.4)
        end=base+Vector((math.cos(a)*h*.15,math.sin(a)*h*.15,.04))
        batch.tube('ForestBark',base+Vector((0,0,h*.14)),end,h*.03,h*.008,5)
    clumps=[]
    for i in range(6):
        a=i*2.399+seed*.71
        side=h*rnd.uniform(.13,.25);zz=h*rnd.uniform(.62,.98)
        endpoint=base+Vector((math.cos(a)*side,math.sin(a)*side,zz))+lean
        branch_start=base+Vector((0,0,h*rnd.uniform(.40,.61)))+lean*.4
        batch.tube('ForestBark',branch_start,endpoint,h*.019,h*.005,5)
        clumps.append((endpoint,h*rnd.uniform(.17,.24),a))
    clumps.append((top+Vector((0,0,h*.055)),h*.18,0))
    for i,(p,size,a) in enumerate(clumps):
        mat=['ForestLeaf','ForestLight','ForestDeep','ForestLeaf','ForestGold','ForestLeaf','ForestLight'][i]
        batch.ellipsoid(mat,p,(size*1.15,size*.94,size*.79),seed*29+i,sides=8,rings=5,rough=.23)
        # Small folded leaf sprays break up the canopy's silhouette and highlights.
        for k in range(9):
            az=rnd.random()*TAU;el=rnd.uniform(-.55,.8)
            direction=Vector((math.cos(az)*math.cos(el),math.sin(az)*math.cos(el),math.sin(el)))
            center=p+direction*size*.95
            tangent=Vector((-math.sin(az),math.cos(az),.25)).normalized()
            length=size*rnd.uniform(.16,.27);wide=length*.37
            v=[tuple(center-direction*length),tuple(center+tangent*wide),tuple(center+direction*length),tuple(center-tangent*wide+Vector((0,0,.045)))]
            batch.mesh('ForestLight' if k%3 else 'ForestGold',v,[(0,1,2),(0,2,3)])


def _vegetation(batch, rnd):
    # Continuous woods, with open shore coves and the two river mouths reserved.
    accepted=0;attempts=0
    while accepted<440 and attempts<3200:
        attempts+=1;a=rnd.random()*TAU;q=rnd.uniform(6,39)
        r=_shore(a)+q;x,y=r*math.cos(a),r*math.sin(a)
        if any(abs(math.atan2(math.sin(a-fa),math.cos(a-fa)))*r<7 for fa in FALL_ANGLES):continue
        if y<-30 and rnd.random()<.48:continue
        z=_height(a,q)
        if z>37:continue
        h=rnd.uniform(3.3,6.8)*(1.08 if y>0 else .77)
        _tree(batch,rnd,x,y,z,h,accepted+19);accepted+=1
    for i in range(115):
        a=rnd.random()*TAU;q=rnd.uniform(2,12);r=_shore(a)+q
        x,y=r*math.cos(a),r*math.sin(a);z=_height(a,q)
        if any(abs(math.atan2(math.sin(a-fa),math.cos(a-fa)))*r<4 for fa in FALL_ANGLES):continue
        sz=rnd.uniform(.45,1.2)
        batch.ellipsoid('ForestLeaf' if i%3 else 'ForestLight',(x,y,z+.35),(sz,sz*.8,sz*.55),1000+i,sides=7,rings=4,rough=.22)
    # Embedded shore boulders: larger clusters at the lake's cliff-backed edges.
    for i in range(155):
        a=rnd.random()*TAU;q=rnd.uniform(-.8,8);r=_shore(a)+q
        x,y=r*math.cos(a),r*math.sin(a);z=_height(a,q)
        sz=rnd.uniform(.45,1.7)*(1.4 if math.sin(a)>.4 else .75)
        batch.ellipsoid('CliffStone' if i%3 else 'CliffLight',(x,y,z+sz*.21),(sz,sz*.73,sz*.61),2000+i,sides=7,rings=4,rough=.24)


def _waterfalls(batch, rnd):
    qs=[35,32,29,26,24.0,23.6,23.5,23.2,23.0,22,20,18,16,14.4,14,13.9,13.6,13.4,12,10,8,6,4,2,0,-1]
    for idx,fa in enumerate(FALL_ANGLES):
        rows=[]
        for j,q in enumerate(qs):
            a=fa+.007*math.sin(q*.21)
            # Slightly toward the lake, in front of the sculpted cliff face.
            r=_shore(a)+q-.27
            z=_profile(q)+.16
            width=(1.6+.5*math.sin(q*.4+idx)+max(0,6-q)*.17)*(1.05 if idx else .85)
            row=[]
            for k in range(9):
                side=(k/8-.5)*width*2
                wave=.035*math.sin(q*2.2+k*1.7)
                row.append((r*math.cos(a)-math.sin(a)*side,r*math.sin(a)+math.cos(a)*side,z+wave))
            rows.append(row)
        for j in range(len(rows)-1):
            for k in range(8):
                mat='WaterfallWhite' if k in (2,5) else ('WaterfallBlue' if k in (1,3,4,6) else 'WaterfallShade')
                batch.mesh(mat,[rows[j][k],rows[j+1][k],rows[j+1][k+1],rows[j][k+1]],[(0,3,2,1)])
        # Thin bright streaks on the two steep vertical drops.
        for qa,qb in [(23.6,23),(14,13.4)]:
            for k in range(7):
                a=fa+.007*math.sin(qa*.21);r=_shore(a)+qa-.52
                side=(k-3)*.38;w=.035+(k%3)*.025
                top=_profile(qa)+.21;bottom=_profile(qb)+.4
                verts=[]
                for z in [top,bottom]:
                    rr=r if z==top else r-(qa-qb)
                    for ss in [side-w,side+w]:verts.append((rr*math.cos(a)-math.sin(a)*ss,rr*math.sin(a)+math.cos(a)*ss,z))
                batch.mesh('WaterfallWhite',verts,[(0,1,3,2)])
        # Broken foam rings mark impacts and the final shallow lake pool.
        for q,poolsize in [(23.0,2.2),(13.3,2.4),(-.8,3.6)]:
            a=fa+.007*math.sin(q*.21);r=_shore(a)+q-.7
            center=(r*math.cos(a),r*math.sin(a),max(WATER_Z+.045,_profile(q)+.25))
            for ring in range(3):
                for k in range(30):
                    if (k+ring)%5==0:continue
                    angle=k*TAU/30;angle2=angle+TAU/38
                    rr=poolsize*(.40+ring*.27);ww=.05+(2-ring)*.07
                    verts=[]
                    for an,rad in [(angle,rr-ww),(angle,rr+ww),(angle2,rr+ww),(angle2,rr-ww)]:
                        verts.append((center[0]+math.cos(an)*rad,center[1]+math.sin(an)*rad*.63,center[2]+.015*ring))
                    batch.mesh('FoamPearl',verts,[(0,1,2,3)])
            for k in range(10):
                dx=rnd.uniform(-1.5,1.5);dy=rnd.uniform(-.7,.7)
                batch.ellipsoid('WaterfallWhite',(center[0]+dx,center[1]+dy,center[2]+rnd.uniform(.05,.32)),(.10,.07,.10),3000+idx*99+k,sides=5,rings=3)


def build_environment(scene, mats):
    """Return a single export root containing material-batched lake landscape."""
    root=bpy.data.objects.new('Lake_Geometry',None)
    scene.collection.objects.link(root)
    root['design']='Enclosed forest lake; continuous irregular shore; north limestone mountains and two cascades'
    root['shore_minimum_radius']=min(_shore(i*TAU/2048) for i in range(2048))
    root['battlefield_clearance']='All environment land starts outside arena centers at +/-40 m'
    for key in ['LakeDeep','LakeBlue','LakeTeal','LakeShallow','LakeGlint']:
        mat=mats[key]
        if mat.use_nodes:
            bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if bs:
                bs.inputs['Roughness'].default_value=.24
                bs.inputs['Metallic'].default_value=.14
    batch=_Batches();rnd=random.Random(20914)
    _terrain(batch);_lake(batch,rnd);_crags(batch,rnd);_vegetation(batch,rnd);_waterfalls(batch,rnd)
    return batch.finish(scene,mats,root)
