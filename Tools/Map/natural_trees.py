"""Mesh-authored broadleaf trees: tapered wood, roots, branching and individual folded leaves."""
import bpy, math, random
from mathutils import Vector
TREE_PALETTE={'BarkLight':'8A7958','BarkDark':'514D36','LeafSun':'9BAD4F','LeafFresh':'76984A','LeafDeep':'315F3D','LeafMid':'527E40'}
def add_tree(scene,parent,mats,x,y,height,seed):
    rng=random.Random(seed);batches={}
    def poly(vs,faces,mat):
        verts,fs=batches.setdefault(mat,([],[]));offset=len(verts);verts.extend(vs);fs.extend(tuple(i+offset for i in f) for f in faces)
    def wood(points,radii):
        pts=[Vector(p) for p in points];verts=[];sides=9
        for j,(p,r) in enumerate(zip(pts,radii)):
            axis=(pts[min(j+1,len(pts)-1)]-pts[max(j-1,0)]).normalized();u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u).normalized()
            for i in range(sides):
                a=i*math.tau/sides;verts.append(tuple(p+(u*math.cos(a)+v*math.sin(a))*r*(1+.1*math.sin(i*5))))
        for j in range(len(pts)-1):
            for i in range(sides):
                k=j*sides+i;kn=j*sides+(i+1)%sides
                poly([verts[t] for t in [k,kn,kn+sides,k+sides]],[(0,1,2,3)],['Bark','BarkLight','BarkDark'][i%3])
        poly(verts[-sides:],[tuple(range(sides))],'Bark')
    zscale=height/3.4
    spine=[(x,y,0),(x-.09,y+.03,.55*zscale),(x+.08,y+.1,1.25*zscale),(x+.18,y+.13,2.15*zscale),(x+.12,y+.2,2.9*zscale)]
    wood(spine,[.23,.18,.14,.085,.025])
    for i in range(6):
        a=i*math.tau/6+.1;wood([(x+math.cos(a)*.62,y+math.sin(a)*.62,.025),(x+math.cos(a)*.27,y+math.sin(a)*.27,.1),(x,y,.38)],[.025,.08,.12])
    clusters=[]
    for i in range(9):
        a=i*2.39996;r=.72+rng.uniform(-.1,.27);z=(2.25+.6*(i%3)/2)*zscale
        start=Vector((x+.05,y+.08,(1.25+i*.08)*zscale));end=Vector((x+math.cos(a)*r,y+math.sin(a)*r,z));mid=start.lerp(end,.6);mid.z-=.17
        wood([start,mid,end],[.065,.044,.013]);clusters.append((end,.6+rng.random()*.15))
        for j in [-1,1]:
            tip=end+Vector((math.cos(a+j*.8)*.4,math.sin(a+j*.8)*.4,.22));wood([end-Vector((0,0,.2)),tip],[.018,.004])
    clusters.append((Vector((x+.1,y+.2,3.15*zscale)),.62))
    for center,spread in clusters:
        for i in range(52):
            phi=rng.random()*math.tau;rad=math.sqrt(rng.random())*spread
            c=center+Vector((math.cos(phi)*rad,math.sin(phi)*rad,rng.uniform(-.26,.24)))
            angle=rng.random()*math.tau;length=rng.uniform(.17,.30);width=length*rng.uniform(.35,.55)
            axis=Vector((math.cos(angle),math.sin(angle),rng.uniform(-.5,.7))).normalized();cross=Vector((-math.sin(angle),math.cos(angle),0))
            points=[c-axis*length,c-axis*length*.25+cross*width,c+axis*length*.55+cross*width*.7,c+axis*length,c+axis*length*.55-cross*width*.7,c-axis*length*.25-cross*width,c+Vector((0,0,.035))]
            faces=[]
            for j in range(6):faces.extend([(6,j,(j+1)%6),(6,(j+1)%6,j)])
            mat=rng.choices(['LeafDeep','LeafMid','LeafFresh','LeafSun'],[2,4,4,2])[0]
            poly([tuple(p) for p in points],faces,mat)
    for mat,(vs,fs) in batches.items():
        mesh=bpy.data.meshes.new('Broadleaf_'+mat);mesh.from_pydata(vs,[],fs);mesh.materials.append(mats[mat]);mesh.update()
        obj=bpy.data.objects.new('NaturalTree_'+mat,mesh);scene.collection.objects.link(obj);obj.parent=parent
