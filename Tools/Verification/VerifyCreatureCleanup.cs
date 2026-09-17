int count = 0;
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (!path.EndsWith(".prefab")) continue;
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    foreach (var node in prefab.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0)
            throw new System.Exception("Missing script: " + path);
    }
    count++;
}
if (typeof(J2.MultiplayerMap.MultiplayerMapPreview).GetMethod("BindPlayer") != null || typeof(J2.MultiplayerMap.MultiplayerMapPreview).GetMethod("Advance") != null)
    throw new System.Exception("Map still controls players");
return "PASS: " + count + " resource prefabs contain no missing scripts; map no longer controls players.";
