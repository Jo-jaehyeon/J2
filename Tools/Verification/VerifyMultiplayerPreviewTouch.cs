var game=UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
object Get(string n)=>game.GetType().GetField(n,flags).GetValue(game);
void Call(string n)=>game.GetType().GetMethod(n,flags).Invoke(game,null);
var prior=UnityEngine.InputSystem.Touchscreen.current;UnityEngine.InputSystem.Touchscreen device=null;
try {
 Call("SelectMultiplayer");var preview=(J2.MultiplayerMap.MultiplayerMapPreview)Get("multiplayerPreview");
 var arena=preview.Layout.Arenas[J2.MultiplayerMap.MultiplayerMapPreview.StartArena];
 var target=arena.TransformPoint(new UnityEngine.Vector3(-2,.25f,-5));var p=preview.ViewCamera.WorldToScreenPoint(target);var screen=new UnityEngine.Vector2(p.x,p.y);
 device=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=91,phase=UnityEngine.InputSystem.TouchPhase.Began,position=screen});UnityEngine.InputSystem.InputSystem.Update();preview.Tick(.01f,v=>false);
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=91,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=screen});UnityEngine.InputSystem.InputSystem.Update();preview.Tick(.01f,v=>false);
 if(!preview.IsMoving)throw new System.Exception("Touch did not set destination");preview.Advance(10);
 if(UnityEngine.Vector3.Distance(target,preview.Player.position)>.02f)throw new System.Exception("Touch destination mismatch");
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=92,phase=UnityEngine.InputSystem.TouchPhase.Began,position=screen});UnityEngine.InputSystem.InputSystem.Update();preview.Tick(.01f,v=>true);
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=92,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=screen});UnityEngine.InputSystem.InputSystem.Update();preview.Tick(.01f,v=>false);
 if(preview.IsMoving)throw new System.Exception("UI-started touch leaked to ground");
 Call("ExitMultiplayerPreview");Call("StartSinglePlayer");
 if(Get("rules")==null||Get("world")==null)throw new System.Exception("Single player failed after preview");
 System.IO.File.AppendAllText("Logs/MultiplayerPreviewValidation.txt","PASS: touch tap moves to ground; UI-started touch ignored; single-player start works after preview exit.\n");
 return "Touch movement, UI input isolation and single-player entry after preview: passed";
} finally {
 if(Get("multiplayerPreview")!=null)Call("ExitMultiplayerPreview");
 if(device!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(device);if(prior!=null)prior.MakeCurrent();
}
