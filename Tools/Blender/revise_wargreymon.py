from pathlib import Path
import shutil,hashlib,json
folder=Path(r'C:/Jerry/Unity Project/J2/Assets/Resources/Digimon/5코스트/WarGreymon')
archive=folder/'Source~/BeforeFaceRevision';archive.mkdir(exist_ok=True)
for file in [folder/'Source~/WarGreymon.blend',folder/'Source~/build_wargreymon.py']:
    if not (archive/file.name).exists():shutil.copy2(file,archive/file.name)
source=(archive/'build_wargreymon.py').read_text(encoding='utf-8')
source=source.split('# Inspection studio')[0]
source=source.replace('Digimon/워그레이몬','Digimon/5코스트/WarGreymon')
source=source.replace("[(s*.07,2.52),(s*.178,2.555),(s*.16,2.485),(s*.087,2.47)]","[(s*.065,2.512),(s*.185,2.57),(s*.169,2.494),(s*.089,2.474)]")
source=source.replace("[(s*.095,2.517),(s*.158,2.533),(s*.145,2.498),(s*.104,2.491)]","[(s*.096,2.515),(s*.163,2.544),(s*.15,2.501),(s*.105,2.49)]")
source=source.replace("    plate('EyeGlint'", "    ellipsoid('EyePupil',(s*.133,-.239,2.517),(.011,.004,.018),dark,'Head',8,5)\n    plate('EyeGlint'")
# Additional temple-facing almond eye recesses, visible in the reference's side view.
source=source.replace("    ellipsoid('TempleHinge'", "    mesh('SideEyeRecess',[(s*.194,-.105,2.535),(s*.211,-.022,2.56),(s*.209,.005,2.519),(s*.192,-.081,2.491)],[(0,1,2,3) if s>0 else (3,2,1,0)],dark,'Head')\n    ellipsoid('SideIris',(s*.213,-.035,2.532),(.007,.024,.024),green,'Head',12,7)\n    ellipsoid('SidePupil',(s*.221,-.037,2.532),(.004,.010,.014),dark,'Head',10,6)\n    ellipsoid('SideGlint',(s*.226,-.044,2.542),(.003,.005,.005),white,'Head',8,5)\n    ellipsoid('TempleHinge'")
exec(compile(source,'wargreymon_face_revision','exec'))
bpy.data.libraries.write(str(folder/'Source~/WarGreymon.blend'),{scene},fake_user=True,compress=True)
photo=Path(r'C:/Users/User/Desktop/J2_AssetImage/ready/워그레이몬_확대.png')
body.data.calc_loop_triangles()
manifest={'name':'WarGreymon','korean':'워그레이몬','costFolder':'5코스트','targetHeight':2.15,'maxWidth':3.0,'maxDepth':3.0,
 'vertices':len(body.data.vertices),'triangles':len(body.data.loop_triangles),'bones':len(arm.bones),
 'materials':[{'name':m.name,'color':list(m.diffuse_color)} for m in body.data.materials],
 'clips':[{'name':n,'firstFrame':a,'lastFrame':z,'loop':n in ['Idle','Walk']} for n,a,z in [('Idle',1,49),('Walk',60,84),('Attack',90,108),('Death',120,150)]],
 'references':[{'path':str(photo),'sha256':hashlib.sha256(photo.read_bytes()).hexdigest()}], 'revision':'face-and-anatomy-2'}
(folder/'Source~/manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(folder/'Source~/build_wargreymon.py').write_text(source,encoding='utf-8')
result={'name':'WarGreymon','triangles':manifest['triangles'],'revision':manifest['revision']}
