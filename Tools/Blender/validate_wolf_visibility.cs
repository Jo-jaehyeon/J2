if(!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var catalog=UnityEngine.JsonUtility.FromJson<DigitalArena.DigimonCatalog>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("DigimonCatalog").text);
var reports=new System.Collections.Generic.List<string>();
foreach(var name in new[]{"가루몬","메탈가루몬"})
{
    var unit=catalog.allies.Single(d=>d.name==name);
    var prefab=UnityEngine.Resources.Load<UnityEngine.GameObject>(unit.prefabPath);
    if(prefab==null)throw new System.Exception("Missing prefab: "+name);
    var instance=UnityEngine.Object.Instantiate(prefab);
    var cameraObject=new UnityEngine.GameObject("WolfVisibilityProbe");
    var target=new UnityEngine.RenderTexture(128,128,24);
    var pixels=new UnityEngine.Texture2D(128,128,UnityEngine.TextureFormat.RGB24,false);
    var previous=UnityEngine.RenderTexture.active;
    try
    {
        instance.transform.position=new UnityEngine.Vector3(1000,0,1000);
        foreach(var t in instance.GetComponentsInChildren<UnityEngine.Transform>(true))t.gameObject.layer=31;
        foreach(var r in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
            if(r.sharedMesh==null||r.sharedMesh.vertexCount==0||r.bones.Any(b=>b==null)||r.sharedMaterials.Any(m=>m==null))throw new System.Exception("Broken renderer: "+name);
        var camera=cameraObject.AddComponent<UnityEngine.Camera>();
        camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
        camera.backgroundColor=UnityEngine.Color.magenta;camera.orthographic=true;camera.orthographicSize=1.5f;
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.targetTexture=target;
        camera.transform.position=new UnityEngine.Vector3(1000,1,996);
        var anim=instance.GetComponent<UnityEngine.Animation>();
        var counts=new System.Collections.Generic.List<string>();
        foreach(var clipName in new[]{"Idle","Walk","Attack","Death"})
        {
            var clip=anim.GetClip(clipName);if(clip==null)throw new System.Exception("Missing clip: "+clipName);
            clip.SampleAnimation(instance,clipName=="Idle"?0:.25f);
            camera.Render();UnityEngine.RenderTexture.active=target;
            pixels.ReadPixels(new UnityEngine.Rect(0,0,128,128),0,0);pixels.Apply();
            int count=pixels.GetPixels().Count(c=>c.g>.12f||c.r<.85f||c.b<.85f);
            if(count<20)throw new System.Exception("Model not rendered: "+name+" / "+clipName);
            counts.Add(clipName+"="+count+" visible pixels");
        }
        reports.Add(name+": PASS; "+string.Join(", ",counts));
    }
    finally
    {
        UnityEngine.RenderTexture.active=previous;
        cameraObject.GetComponent<UnityEngine.Camera>().targetTexture=null;
        UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(instance);
        target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);
    }
}
return string.Join("\n",reports);
