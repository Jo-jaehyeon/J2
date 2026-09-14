var messages=new System.Collections.Generic.List<string>();
foreach(var path in DigimonBlenderAssetBuilder.Manifests())
{
    var m=UnityEngine.JsonUtility.FromJson<DigimonBlenderAssetBuilder.Manifest>(System.IO.File.ReadAllText(path));
    if(!m.materials.Any(p=>p.vertexColor))continue;
    string resource="Digimon/"+m.grade+"/"+m.name+"/"+m.name;
    var prefab=UnityEngine.Resources.Load<UnityEngine.GameObject>(resource);
    foreach(var renderer in prefab.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        var mesh=renderer.sharedMesh;
        if(mesh.colors.Length!=mesh.vertexCount)throw new System.Exception("Lost FBX vertex colors: "+m.name);
        if(!mesh.colors.Any(c=>c.r<.8f||c.g<.8f||c.b<.8f))throw new System.Exception("Unpainted white mesh: "+m.name);
        if(renderer.sharedMaterials.Any(mat=>mat.GetFloat("_UseVertexColor")<.99f))throw new System.Exception("Color material mapping: "+m.name);
    }
    var report=m.name+": PASS; FBX vertex colors retained and material switch enabled";
    System.IO.File.WriteAllText(System.IO.Path.GetDirectoryName(path)+"/surface-validation.txt",report);
    messages.Add(report);
}
if(UnityEditor.ShaderUtil.ShaderHasError(UnityEngine.Shader.Find("DigitalArena/Solid")))throw new System.Exception("Shader compile failed");
return string.Join("\n",messages);
