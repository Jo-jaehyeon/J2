import bpy
from mathutils import Vector
from pathlib import Path
scene=sorted([s for s in bpy.data.scenes if s.name.startswith('J2_Multiplayer_Overview')],key=lambda s:s.name)[-1]
bpy.context.window.scene=scene;cam=scene.camera
out=Path('C:/Jerry/UnityProject/J2/Assets/Resources/Map/Source~')
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_percentage=100
scene.render.resolution_x=1600;scene.render.resolution_y=1200
cam.data.type='ORTHO';cam.data.ortho_scale=245;cam.location=(80,-180,205);cam.rotation_euler=(Vector((0,16,8))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'Overview.png');bpy.ops.render.render(write_still=True,scene=scene.name)
cam.data.type='PERSP';cam.data.lens=28;cam.location=(18,-150,50);cam.rotation_euler=(Vector((0,20,20))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1600;scene.render.resolution_y=900
scene.render.filepath=str(out/'LakeScenery.png');bpy.ops.render.render(write_still=True,scene=scene.name)
cam.data.type='ORTHO';cam.data.ortho_scale=35;cam.location=(20,-67,28);cam.rotation_euler=(Vector((0,-40,0))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1400;scene.render.resolution_y=1050
scene.render.filepath=str(out/'Battlefield.png');bpy.ops.render.render(write_still=True,scene=scene.name)
