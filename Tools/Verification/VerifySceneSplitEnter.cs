void Check(bool ok,string message) { if(!ok) throw new System.Exception(message); }
Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu","MainMenu must boot first");
var game=UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
Check(game!=null,"Menu controller missing");
Check(UnityEngine.Object.FindAnyObjectByType<DigitalArena.ArenaWorld3D>()==null,"Single-player world in menu");
Check(UnityEngine.Object.FindAnyObjectByType<J2.MultiplayerMap.MultiplayerMapPreview>()==null,"Multiplayer world in menu");
UnityEditor.SessionState.SetString("SceneSplit.Account",UnityEngine.Object.FindAnyObjectByType<DigitalArena.ArenaAccountClient>().GetEntityId().ToString());
UnityEditor.SessionState.SetString("SceneSplit.Network",J2.Networking.NetworkManager.Instance.GetEntityId().ToString());
DigitalArena.GameSceneFlow.SetLocalArenaAssignment(3);
game.GetType().GetMethod("EnterMultiplayerPreview",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,null);
return "PASS: MainMenu only contains menu; actual asynchronous multiplayer scene load requested with arena 4 assignment.";
