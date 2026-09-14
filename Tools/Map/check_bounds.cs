var a=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Map/Battlefield.fbx");
var go=UnityEngine.Object.Instantiate(a); go.transform.rotation=UnityEngine.Quaternion.Euler(0,180,0);
var result=string.Join("\n", System.Linq.Enumerable.Select(go.GetComponentsInChildren<UnityEngine.Renderer>(),r=>r.name+" "+r.bounds));
UnityEngine.Object.DestroyImmediate(go); return result;
