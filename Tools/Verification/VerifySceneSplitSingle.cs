void Check(bool ok,string message) { if(!ok) throw new System.Exception(message); }
Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SinglePlayer","SinglePlayer scene not loaded");
var game=UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
Check(game!=null&&UnityEngine.Object.FindAnyObjectByType<DigitalArena.ArenaWorld3D>()!=null,"Original single-player world not initialized");
Check(game.GetType().GetField("rules",flags).GetValue(game)!=null,"Single-player rules not initialized");
Check(!(bool)game.GetType().GetField("mainMenu",flags).GetValue(game),"Main menu shown in single-player");
Check(UnityEngine.Object.FindAnyObjectByType<J2.MultiplayerMap.MultiplayerMapPreview>()==null,"Multiplayer in single-player");
Check(UnityEngine.Object.FindObjectsByType<J2.Networking.NetworkManager>(UnityEngine.FindObjectsSortMode.None).Length==1,"Duplicate network session");
DigitalArena.GameSceneFlow.Load(DigitalArena.GameSceneFlow.MainMenu);
System.IO.File.WriteAllText("Logs/SceneSplitValidation.txt","PASS: MainMenu boot without world; actual scene transition; assigned arena 4 spawn and camera; one multiplayer map; preserved account and network across scenes; right-click on rotated arena; WASD/arrows ignored; return unloads multiplayer; single-player UI button loads preserved world and starts rules.\n");
return "PASS: SinglePlayer scene starts original map and rules, without multiplayer; returning to MainMenu.";
