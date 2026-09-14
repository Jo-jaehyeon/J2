from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8')
s=s.replace("stage=root('Battlefield_Geometry');tram=root('Tram_Geometry');lake=root('Lake_Geometry')", "stage=root('Battlefield_Geometry');tram=root('Tram_Geometry');lake=root('Lake_Geometry');rail=root('Railway_Geometry')")
a=s.index('# Consolidate static geometry')
new='''# One shared square loop. Rounded 3-unit corners keep the single tram turn continuous.
parent=rail
route=[]
for cx,cy,start in [(21,21,0),(-21,21,90),(-21,-21,180),(21,-21,270)]:
    for i in range(25):
        angle=math.radians(start+i*90/24)
        route.append((cx+3*math.cos(angle),cy+3*math.sin(angle),.285))
# Order is counterclockwise: east/north corner, north/west, west/south, south/east.
# Subdivide long straight sections for sleepers and train route sampling.
path=[]
for i,a in enumerate(route):
    b=route[(i+1)%len(route)];d=Vector(b)-Vector(a);n=max(1,math.ceil(d.length/.65))
    for j in range(n):path.append(Vector(a)+d*j/n)
for i,a in enumerate(path):
    b=path[(i+1)%len(path)];d=b-a;mid=(a+b)/2;angle=math.atan2(d.y,d.x);normal=Vector((-math.sin(angle),math.cos(angle),0))
    o=cube('Continuous ballast',mid+Vector((0,0,-.22)),(d.length+.025,1.55,.2),'Ballast');o.rotation_euler.z=angle
    o=cube('Loop sleeper',a+Vector((0,0,-.105)),(.19,1.32,.12),'Sleeper');o.rotation_euler.z=angle
    for side in [-1,1]:
        o=cube('Continuous rail',mid+normal*side*.48,(d.length+.035,.085,.12),'Rail');o.rotation_euler.z=angle
# Eight timber boarding links join the island edges to the same ring.
for idx,(x,y) in enumerate([(-40,40),(0,40),(40,40),(-40,0),(40,0),(-40,-40),(0,-40),(40,-40)]):
    v=Vector((x,y,0)).normalized();corner=x!=0 and y!=0
    end=Vector((x,y,.0))-v*(12 if corner else 11)
    start=Vector((math.copysign(23.12,x) if x else 0,math.copysign(23.12,y) if y else 0,0)) if corner else Vector((math.copysign(24,x) if x else 0,math.copysign(24,y) if y else 0,0))
    delta=end-start;ang=math.atan2(delta.y,delta.x)
    mid=(start+end)/2;mid.z=-.03
    o=cube('Station connector %02d'%idx,mid,(delta.length,2.1,.18),'Sleeper');o.rotation_euler.z=ang
    for j in range(int(delta.length/.4)+1):
        t=j/max(1,int(delta.length/.4));pt=start+delta*t;pt.z=.075
        o=cube('Deck plank',pt,(.32,2,.045),'Sand');o.rotation_euler.z=ang
    station=start+v*1.7;station.z=.05
    o=cube('Boarding platform',station,(3,2.4,.2),'Sand',.05);o.rotation_euler.z=ang
    beam('Station lamp pole',station+Vector((0,0,.1)),station+Vector((0,0,1.7)),.065,'Steel')
    oval('Station lantern',station+Vector((0,0,1.8)),(.18,.18,.23),'Light')
(SRC/'rail-route.json').write_text(json.dumps({'points':[[round(v.x,5),0,round(v.y,5)] for v in path],'closed':True,'trainCount':1,'routing':'square perimeter with rounded corners, boarding links outside combat cells'},indent=2))
'''
s=s[:a]+new+s[a:]
s=s.replace('for rt in [stage,tram,lake]:','for rt in [stage,tram,lake,rail]:')
s=s.replace("(lake,'LakeEnvironment')]:","(lake,'LakeEnvironment'),(rail,'SquareRailway')]:")
s=s.replace('for ob in lake.children:','for ob in list(lake.children)+list(rail.children):')
s=s.replace('for r in [stage,tram,lake]','for r in [stage,tram,lake,rail]')
p.write_text(s,encoding='utf-8')
