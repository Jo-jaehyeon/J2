var changed = new System.Collections.Generic.List<string>();
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (!path.EndsWith(".prefab")) continue;
    var text = System.IO.File.ReadAllText(path);
    if (!text.Contains("81ea17db834bd194f88e9fff107c2969") && !text.Contains("c876c0fe0aa986c48b26d66d7b95ab6a")) continue;
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        foreach (var component in root.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
        {
            if (component == null) continue;
            var name = component.GetType().FullName;
            if (name == "DigitalArena.PlayableCharacterMotor")
            {
                if (component.GetComponent<J2.Creatures.Player>() == null)
                    component.gameObject.AddComponent<J2.Creatures.Player>();
                UnityEngine.Object.DestroyImmediate(component);
            }
            else if (name == "DigitalArena.ArenaUnitAnimation")
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        changed.Add(path);
    }
    finally
    {
        UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    }
}
UnityEditor.AssetDatabase.SaveAssets();
return "Migrated " + changed.Count + " prefabs: " + string.Join("\n", changed);
