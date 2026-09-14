import bpy
from mathutils import Quaternion,Euler
def inspect(names,angle=0):
    sc=bpy.data.scenes.new('J2_InternalInspection');bpy.context.window.scene=sc
    for i,n in enumerate(names):
        src=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith(n+'_Mesh')][-1]
        ob=bpy.data.objects.new(n,src.data.copy());sc.collection.objects.link(ob)
        ob.location=((i-(len(names)-1)/2)*2.5,0,0)
        ob.rotation_euler.z=angle
    for a in bpy.context.screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.shading.color_type='MATERIAL';a.spaces.active.overlay.show_overlays=False
            a.spaces.active.region_3d.view_rotation=Quaternion((1,0,0),1.5707963)
            a.spaces.active.region_3d.view_distance=5.7 if len(names)<=2 else 8
            a.spaces.active.region_3d.view_location=(0,0,1.12)
    return {'inspecting':names}
