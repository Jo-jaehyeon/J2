import bpy, json
print(json.dumps({'file':bpy.data.filepath,'scene':bpy.context.scene.name,'objects':len(bpy.context.scene.objects),'version':bpy.app.version_string}))
