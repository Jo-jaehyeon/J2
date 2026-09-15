if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop play mode before creating scenes");
var original = "Assets/Scenes/DragonEyeLake.unity";
var single = "Assets/Scenes/SinglePlayer.unity";
var menu = "Assets/Scenes/MainMenu.unity";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(single) == null)
    if (!UnityEditor.AssetDatabase.CopyAsset(original,single)) throw new System.Exception("Could not preserve original scene");
var loaded = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (loaded.isDirty) throw new System.Exception("Current scene has unsaved changes: " + loaded.path);
var singleScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(single);
if (UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>() == null)
    new UnityEngine.GameObject("Single Player Game").AddComponent<DigitalArena.DigitalArenaGame>();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(singleScene);
var mainScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
new UnityEngine.GameObject("Main Menu").AddComponent<DigitalArena.DigitalArenaGame>();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(mainScene,menu);
var multiScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
var map = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Map/DragonEyeMultiplayer.prefab"));
var controller = new UnityEngine.GameObject("Multiplayer Game").AddComponent<J2.MultiplayerMap.MultiplayerSceneController>();
var serialized = new UnityEditor.SerializedObject(controller);
serialized.FindProperty("sceneMap").objectReferenceValue = map.GetComponent<J2.MultiplayerMap.MultiplayerMapLayout>();
serialized.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(multiScene,original);
var scenes = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene> {
    new UnityEditor.EditorBuildSettingsScene(menu,true),
    new UnityEditor.EditorBuildSettingsScene(original,true),
    new UnityEditor.EditorBuildSettingsScene(single,true)
};
foreach(var scene in UnityEditor.EditorBuildSettings.scenes)
    if(scene.path != menu && scene.path != original && scene.path != single) scenes.Add(scene);
UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(menu);
UnityEditor.AssetDatabase.SaveAssets();
return "Created MainMenu, multiplayer-only DragonEyeLake, and preserved SinglePlayer scenes. MainMenu is build entry.";
