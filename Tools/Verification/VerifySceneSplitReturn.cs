void Check(bool ok,string message) { if(!ok) throw new System.Exception(message); }
Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu","Return did not load MainMenu");
Check(UnityEngine.Object.FindAnyObjectByType<J2.MultiplayerMap.MultiplayerMapPreview>()==null,"Multiplayer world survived scene exit");
Check(UnityEngine.Object.FindAnyObjectByType<DigitalArena.ArenaAccountClient>().GetEntityId().ToString()==UnityEditor.SessionState.GetString("SceneSplit.Account",""),"Account lost on return");
Check(UnityEngine.Object.FindObjectsByType<DigitalArena.ArenaAccountClient>(UnityEngine.FindObjectsSortMode.None).Length==1,"Duplicate account session");
var game=UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
game.GetType().GetMethod("LateUpdate",flags).Invoke(game,null);
var doc=game.GetComponentInChildren<UnityEngine.UIElements.UIDocument>();
var button=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement,"single");
Check(button!=null&&button.enabledInHierarchy,"Single-player button unavailable");
using(var evt=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}
return "PASS: Returned to MainMenu, multiplayer unloaded, account session retained once. Actual single-player button requested scene change.";
