from pathlib import Path
p=Path('Tools/Map/build_multiplayer_map.py')
s=p.read_text(encoding='utf-8-sig')
s=s.replace("bs=m.node_tree.nodes.get('Principled BSDF');", "bs=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled'); output=m.node_tree.nodes.new('ShaderNodeOutputMaterial'); m.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface']);")
a=s.index("cube('Raised track bed'");b=s.index('\ndef tree',a)
s=s[:a]+"# Shared railway is authored separately after station routing is resolved.\n"+s[b:]
s=s.replace('for rt in [stage,tram]:','for rt in [stage]:')
s=s.replace('for ob in lake.children:',"for ob in tram.children:\n    o=ob.copy();o.data=ob.data;o.parent=None;preview.collection.objects.link(o);o.location=ob.matrix_world.translation+Vector((0,-24,0))\nfor ob in lake.children:")
s=s.replace("world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.73,.8,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7;", "bg=world.node_tree.nodes.new('ShaderNodeBackground'); wo=world.node_tree.nodes.new('ShaderNodeOutputWorld');world.node_tree.links.new(bg.outputs[0],wo.inputs['Surface']);bg.inputs[0].default_value=(.55,.73,.8,1);bg.inputs[1].default_value=.7;")
p.write_text(s,encoding='utf-8')
