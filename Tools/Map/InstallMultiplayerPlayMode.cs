if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Exit Play mode before package installation");
var search = UnityEditor.PackageManager.Client.Search("com.unity.multiplayer.playmode");
var deadline = UnityEditor.EditorApplication.timeSinceStartup + 120;
UnityEditor.EditorApplication.CallbackFunction poll = null;
poll = () => {
    if (!search.IsCompleted && UnityEditor.EditorApplication.timeSinceStartup < deadline) return;
    UnityEditor.EditorApplication.update -= poll;
    if (!search.IsCompleted || search.Status != UnityEditor.PackageManager.StatusCode.Success) {
        System.IO.File.WriteAllText("Logs/MultiplayerPlayModeInstall.txt","SEARCH FAILED: " + search.Error?.message);return;
    }
    System.IO.File.WriteAllText("Logs/MultiplayerPlayModeInstall.txt","Installing compatible Multiplayer Play Mode...");
    var request = UnityEditor.PackageManager.Client.Add("com.unity.multiplayer.playmode");
    var installDeadline = UnityEditor.EditorApplication.timeSinceStartup + 600;
    UnityEditor.EditorApplication.CallbackFunction check = null;
    check = () => {
        if (!request.IsCompleted && UnityEditor.EditorApplication.timeSinceStartup < installDeadline) return;
        UnityEditor.EditorApplication.update -= check;
        System.IO.File.WriteAllText("Logs/MultiplayerPlayModeInstall.txt", request.IsCompleted && request.Status == UnityEditor.PackageManager.StatusCode.Success ? "INSTALLED: " + request.Result.name + "@" + request.Result.version : "INSTALL FAILED: " + request.Error?.message);
    };
    UnityEditor.EditorApplication.update += check;
};
UnityEditor.EditorApplication.update += poll;
return "Package search and installation started";
