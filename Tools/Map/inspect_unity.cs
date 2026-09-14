UnityEditor.AssetDatabase.Refresh();
var p = "Assets/Resources/Map/Battlefield.fbx";
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
var renderers = asset.GetComponentsInChildren<UnityEngine.Renderer>();
var bounds = renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
UnityEngine.Debug.Log("MAP_IMPORT " + asset.name + " bounds=" + bounds + " meshCount="+renderers.Length+" materials="+string.Join(",", System.Linq.Enumerable.Select(renderers,r=>r.sharedMaterial.name)));
UnityEngine.Debug.Log("SCENE " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" playing="+UnityEditor.EditorApplication.isPlaying);
