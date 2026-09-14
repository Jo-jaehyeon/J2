UnityEditor.AssetDatabase.Refresh();
var basePath = "Assets/Resources/Map";
System.IO.Directory.CreateDirectory(basePath+"/Materials");
var palette = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(basePath+"/Source~/palette.json"));
var materials = new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
foreach(var item in palette.Properties()) {
    UnityEngine.Color color; UnityEngine.ColorUtility.TryParseHtmlString("#"+(string)item.Value,out color);
    var path=basePath+"/Materials/MP_"+item.Name+".mat";
    var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if(mat==null) { mat=new UnityEngine.Material(UnityEngine.Shader.Find("Standard")); UnityEditor.AssetDatabase.CreateAsset(mat,path); }
    mat.color=color;mat.SetFloat("_Glossiness",item.Name.Contains("Water")?.65f:.16f);UnityEditor.EditorUtility.SetDirty(mat);materials[item.Name]=mat;
}
System.Func<string,UnityEngine.Transform,UnityEngine.GameObject> visual=(name,parent)=>{
    var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(basePath+"/"+name+".fbx");
    if(asset==null)throw new System.Exception("Missing FBX "+name);
    var correction=new UnityEngine.GameObject(name+" Visual");correction.transform.SetParent(parent,false);correction.transform.localRotation=UnityEngine.Quaternion.Euler(0,180,0);
    var go=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset);go.transform.SetParent(correction.transform,false);
    foreach(var r in go.GetComponentsInChildren<UnityEngine.Renderer>()){
        var slots=r.sharedMaterials;
        for(int i=0;i<slots.Length;i++) { var key=slots[i].name.Substring(3).Split('.')[0]; slots[i]=materials[key]; }
        r.sharedMaterials=slots;
    }
    return correction;
};
System.Func<string,UnityEngine.Transform,UnityEngine.Vector3,UnityEngine.Transform> marker=(name,parent,p)=>{
    var t=new UnityEngine.GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;
};
var arena=new UnityEngine.GameObject("Battlefield");
visual("Battlefield",arena.transform);
for(int side=0;side<2;side++) {
    var cells=marker(side==0?"AllyCells":"EnemyCells",arena.transform,UnityEngine.Vector3.zero);
    for(int r=0;r<4;r++)for(int c=0;c<8;c++)marker("Cell_"+(r*8+c).ToString("D2"),cells,new UnityEngine.Vector3((c-3.5f)*1.7f,.25f,(side==0?-1-r:1+r)*1.7f));
    var bench=marker(side==0?"AllyBench":"EnemyBench",arena.transform,UnityEngine.Vector3.zero);
    for(int i=0;i<10;i++)marker("Slot_"+i.ToString("D2"),bench,new UnityEngine.Vector3((i-4.5f)*1.35f,.3f,side==0?-9:9));
}
var anchor=marker("CameraAnchor",arena.transform,new UnityEngine.Vector3(0,21,-24));
var target=marker("CameraTarget",arena.transform,new UnityEngine.Vector3(0,0,-.6f));anchor.LookAt(target);
var arenaPrefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(arena,basePath+"/Battlefield.prefab");UnityEngine.Object.DestroyImmediate(arena);
var cornerArena=UnityEngine.Object.Instantiate(arenaPrefab);cornerArena.name="CornerBattlefield";
UnityEngine.Object.DestroyImmediate(cornerArena.transform.Find("Battlefield Visual").gameObject);
visual("CornerBattlefield",cornerArena.transform);
foreach(var groupName in new[]{"AllyCells","EnemyCells","AllyBench","EnemyBench"})foreach(UnityEngine.Transform t in cornerArena.transform.Find(groupName)) {
    var p=t.localPosition;p.x+=p.x>0?3.25f:-3.25f;t.localPosition=p;
}
var cornerPrefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(cornerArena,basePath+"/CornerBattlefield.prefab");UnityEngine.Object.DestroyImmediate(cornerArena);
var root=new UnityEngine.GameObject("DragonEyeMultiplayer");
var layout=root.AddComponent<J2.MultiplayerMap.MultiplayerMapLayout>();
var arenas=marker("Arenas",root.transform,UnityEngine.Vector3.zero);
int slot=0;
for(int row=1;row>=-1;row--)for(int col=-1;col<=1;col++) {
    if(row==0&&col==0)continue;
    var a=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab((slot==0||slot==2||slot==5||slot==7)?cornerPrefab:arenaPrefab);a.name="Arena_"+slot.ToString("D2");a.transform.SetParent(arenas,false);a.transform.localPosition=new UnityEngine.Vector3(col*40,0,row*40);
    if(slot==3||slot==4)a.transform.localRotation=UnityEngine.Quaternion.Euler(0,90,0);
    layout.Arenas[slot]=a.transform;layout.CameraAnchors[slot]=a.transform.Find("CameraAnchor");layout.CameraTargets[slot]=a.transform.Find("CameraTarget");slot++;
}
visual("LakeEnvironment",root.transform);visual("SquareRailway",root.transform);
var trainRoot=marker("SharedTrain",root.transform,UnityEngine.Vector3.zero);visual("LakeTram",trainRoot);
var driver=root.AddComponent<J2.MultiplayerMap.MultiplayerMapTrain>();driver.Train=trainRoot;
var route=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(basePath+"/Source~/rail-route.json"));
var points=(Newtonsoft.Json.Linq.JArray)route["points"];driver.Route=new UnityEngine.Vector3[points.Count];
for(int i=0;i<points.Count;i++)driver.Route[i]=new UnityEngine.Vector3((float)points[i][0],(float)points[i][1],(float)points[i][2]);
layout.SharedTrain=driver;driver.RebuildRoute();
var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,basePath+"/DragonEyeMultiplayer.prefab");
var result="Created "+UnityEditor.AssetDatabase.GetAssetPath(prefab)+"; arenas="+slot+"; train=1; routeMeters="+driver.RouteLength;
UnityEngine.Object.DestroyImmediate(root);UnityEditor.AssetDatabase.SaveAssets();return result;
