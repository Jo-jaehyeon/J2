import bpy,json
from pathlib import Path
src=Path('C:/Jerry/UnityProject/J2/Assets/Resources/Map/Source~')
work=next(s for s in bpy.data.scenes if any(o.name.startswith('Railway_Geometry') for o in s.objects))
preview=sorted([s for s in bpy.data.scenes if s.name.startswith('J2_Multiplayer_Overview')],key=lambda s:s.name)[-1]
for m in set(o.data.materials[0] for o in work.objects if o.type=='MESH'):
    m.use_nodes=True;m.node_tree.nodes.clear();bs=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled');out=m.node_tree.nodes.new('ShaderNodeOutputMaterial');m.node_tree.links.new(bs.outputs['BSDF'],out.inputs['Surface']);bs.inputs['Base Color'].default_value=m.diffuse_color;bs.inputs['Roughness'].default_value=.8
w=preview.world;w.use_nodes=True;w.node_tree.nodes.clear();bg=w.node_tree.nodes.new('ShaderNodeBackground');out=w.node_tree.nodes.new('ShaderNodeOutputWorld');w.node_tree.links.new(bg.outputs[0],out.inputs[0]);bg.inputs[0].default_value=(.55,.73,.8,1);bg.inputs[1].default_value=.6
for o in work.objects:
    if o.type=='MESH' and o.name.startswith('Lake_Geometry') and ('Mountain' in o.name or 'Water' in o.name):
        for v in o.data.vertices:v.co.x*=1.20;v.co.y*=1.20
preview.camera.data.ortho_scale=188
bpy.context.window.scene=work
bpy.ops.object.select_all(action='DESELECT')
lake=next(o for o in work.objects if o.type=='EMPTY' and o.name.startswith('Lake_Geometry'))
lake.select_set(True)
for o in lake.children:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(src.parent/'LakeEnvironment.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
bpy.data.libraries.write(str(src/'DragonEyeMultiplayerFinal.blend'),{work,preview},fake_user=True)
bpy.context.window.scene=bpy.data.scenes['Scene']
print('Final colored source saved')
