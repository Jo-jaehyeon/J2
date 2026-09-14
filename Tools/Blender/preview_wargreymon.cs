var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root = UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("Digimon/워그레이몬/WarGreymon"));
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var cameraObject = new UnityEngine.GameObject("WarGreymonPreviewCamera");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera = cameraObject.AddComponent<UnityEngine.Camera>();
camera.scene = scene;
camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
camera.backgroundColor = new UnityEngine.Color(.11f,.14f,.19f);
camera.orthographic = true; camera.orthographicSize = 1.35f;
camera.transform.position = new UnityEngine.Vector3(3,1.8f,5);
camera.transform.LookAt(new UnityEngine.Vector3(0,1.05f,0));
var rt = new UnityEngine.RenderTexture(720,850,24);
camera.targetTexture = rt;
var animation = root.GetComponent<UnityEngine.Animation>();
var report = new System.Text.StringBuilder();
try {
    foreach(string name in new[]{"Idle","Walk","Attack","Death"}) {
        var clip = animation.GetClip(name);
        clip.SampleAnimation(root,name=="Idle"?0:name=="Death"?clip.length:clip.length*.5f);
        var renderer=root.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
        var mesh=new UnityEngine.Mesh();renderer.BakeMesh(mesh);
        report.AppendLine(name+": "+clip.length+"s; root scale="+root.transform.localScale+"; local bounds="+mesh.bounds);
        UnityEngine.Object.DestroyImmediate(mesh);
        camera.Render();
        var previous = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active=rt;
        var image=new UnityEngine.Texture2D(720,850,UnityEngine.TextureFormat.RGB24,false);
        image.ReadPixels(new UnityEngine.Rect(0,0,720,850),0,0);image.Apply();
        System.IO.File.WriteAllBytes("Logs/WarGreymon/unity-"+name+".png",image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.RenderTexture.active=previous;
    }
    System.IO.File.WriteAllText("Logs/WarGreymon/unity-validation.txt",report.ToString());
} finally {
    camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
return report.ToString();
