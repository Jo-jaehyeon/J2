if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play mode before deleting scene");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path=="Assets/Scenes/SinglePlayer.unity")
{
    if(scene.isDirty)throw new System.Exception("Unsaved single-player scene edits");
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
}
UnityEditor.EditorBuildSettings.scenes=new[]{new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",true),new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/DragonEyeLake.unity",true)};
if(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/SinglePlayer.unity")!=null && !UnityEditor.AssetDatabase.DeleteAsset("Assets/Scenes/SinglePlayer.unity"))throw new System.Exception("Single-player scene deletion failed");
UnityEditor.AssetDatabase.SaveAssets();
return "SinglePlayer scene removed; build scenes are MainMenu and DragonEyeLake.";
