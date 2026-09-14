import bpy
from mathutils import Vector
scene=sorted([s for s in bpy.data.scenes if s.name.startswith('J2_Multiplayer_Overview')],key=lambda s:s.name)[-1]
bpy.context.window.scene=scene
cam=scene.camera;cam.data.type='PERSP';cam.data.lens=28;cam.location=(18,-150,50);cam.rotation_euler=(Vector((0,20,20))-cam.location).to_track_quat('-Z','Y').to_euler()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
scene.render.resolution_x=1600;scene.render.resolution_y=900
bpy.ops.wm.save_as_mainfile(filepath='C:/Jerry/UnityProject/J2/Assets/Resources/Map/Source~/DragonEyeMultiplayer.blend',check_existing=False)
