import bpy
from mathutils import Vector
s=bpy.context.scene;s.frame_set(1)
s.render.resolution_x=850;s.render.resolution_y=1000
for name,loc,target,scale in [('three-quarter',(3.6,-6,3),(0,0,1.45),3.6),('back',(0,6,1.5),(0,0,1.5),3.6)]:
 s.camera.location=loc;s.camera.rotation_euler=(Vector(target)-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=scale
 s.render.filepath='C:/Jerry/Unity Project/J2/Logs/WarGreymon/'+name+'.png';bpy.ops.render.render(write_still=True)
s.camera.location=(3.6,-6,3);s.camera.rotation_euler=(Vector((0,0,1.45))-s.camera.location).to_track_quat('-Z','Y').to_euler()
s.render.filepath='C:/Jerry/Unity Project/J2/Logs/WarGreymon/three-quarter.png'
bpy.ops.wm.save_as_mainfile(filepath=r'C:/Jerry/Unity Project/J2/Assets/Resources/Digimon/워그레이몬/Source~/WarGreymon.blend')
result={'final_preview':'Logs/WarGreymon/three-quarter.png'}
