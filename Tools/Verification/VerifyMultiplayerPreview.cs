var game=UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
if(game==null)throw new System.Exception("Game did not boot");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
object Get(string name)=>game.GetType().GetField(name,flags).GetValue(game);
void Call(string name)=>game.GetType().GetMethod(name,flags).Invoke(game,null);
var doc=System.Linq.Enumerable.Single(game.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>(),d=>d.name=="Arena UI (UI Toolkit)");
var results=new System.Text.StringBuilder();int checks=0;
void Check(bool ok,string description){if(!ok)throw new System.Exception(description);checks++;results.AppendLine("PASS: "+description);}
void Click(string name){Call("LateUpdate");var button=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement,name);Check(button!=null&&button.enabledInHierarchy,"Button available: "+name);using(var evt=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}}
var priorKeyboard=UnityEngine.InputSystem.Keyboard.current;var priorMouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.Keyboard keyboard=null;UnityEngine.InputSystem.Mouse mouse=null;
try {
    Check((bool)Get("mainMenu"),"Starts at main menu");
    var existingRules=Get("rules");
    Click("multi");
    var preview=(J2.MultiplayerMap.MultiplayerMapPreview)Get("multiplayerPreview");
    Check(preview!=null&&!(bool)Get("mainMenu"),"Multiplayer UI button enters map immediately");
    Check(preview.Layout.Arenas.Length==8&&preview.Player!=null,"Eight arenas and one controllable character loaded");
    Check(Get("rules")==existingRules&&Get("battle")==null,"Preview creates no round or battle");
    Check(preview.ViewCamera.enabled&&!((UnityEngine.Camera)Get("menuCamera")).enabled,"Preview camera replaces menu camera");
    Call("LateUpdate");
    var shop=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement,"toggleShop");
    Check(shop==null||shop.style.display.value==UnityEngine.UIElements.DisplayStyle.None,"No shop UI in preview");
    var arena=preview.Layout.Arenas[J2.MultiplayerMap.MultiplayerMapPreview.StartArena];
    var before=preview.Player.position;
    keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
    foreach(var key in new[]{UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.A,UnityEngine.InputSystem.Key.S,UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.UpArrow,UnityEngine.InputSystem.Key.DownArrow,UnityEngine.InputSystem.Key.LeftArrow,UnityEngine.InputSystem.Key.RightArrow}) {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));UnityEngine.InputSystem.InputSystem.Update();
        preview.Tick(.25f,p=>false);
        Check(UnityEngine.Vector3.Distance(before,preview.Player.position)<.001f,key+" does not move character");
    }
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());UnityEngine.InputSystem.InputSystem.Update();
    mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
    var wanted=arena.TransformPoint(new UnityEngine.Vector3(3,.25f,-6));var screen=preview.ViewCamera.WorldToScreenPoint(wanted);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(screen.x,screen.y),buttons=2});UnityEngine.InputSystem.InputSystem.Update();
    preview.Tick(.05f,p=>false);Check(preview.IsMoving,"Right-click sets a ground destination");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState());UnityEngine.InputSystem.InputSystem.Update();
    preview.Advance(10f);Check(UnityEngine.Vector3.Distance(preview.Player.position,wanted)<.02f,"Character reaches clicked world position");
    Check(!preview.MoveToWorldPoint(arena.TransformPoint(new UnityEngine.Vector3(40,.25f,0))),"Water/outside-island destination rejected");
    preview.MoveToWorldPoint(arena.TransformPoint(new UnityEngine.Vector3(0,.25f,0)));preview.CancelMovement();Check(!preview.IsMoving,"Focus-loss movement cancellation supported");
    for(int i=0;i<60;i++)Call("Update");
    Check(Get("rules")==existingRules&&Get("battle")==null,"Game loop stays outside round and combat processing");
    var rt=new UnityEngine.RenderTexture(1280,800,24);var previous=UnityEngine.RenderTexture.active;
    preview.ViewCamera.targetTexture=rt;preview.ViewCamera.Render();UnityEngine.RenderTexture.active=rt;var image=new UnityEngine.Texture2D(1280,800,UnityEngine.TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,1280,800),0,0);image.Apply();System.IO.File.WriteAllBytes("Logs/MultiplayerPreview.png",UnityEngine.ImageConversion.EncodeToPNG(image));preview.ViewCamera.targetTexture=null;UnityEngine.RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);
    Click("exitMultiplayerPreview");
    Check(Get("multiplayerPreview")==null&&(bool)Get("mainMenu"),"Return button restores main menu");
    Check(((UnityEngine.Camera)Get("menuCamera")).enabled,"Original menu camera restored");
    Click("multi");Check(Get("multiplayerPreview")!=null,"Re-entering preview works");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));UnityEngine.InputSystem.InputSystem.Update();Call("Update");
    Check(Get("multiplayerPreview")==null&&(bool)Get("mainMenu"),"Escape returns to menu");
    results.AppendLine("Checks: "+checks);System.IO.File.WriteAllText("Logs/MultiplayerPreviewValidation.txt",results.ToString());return results.ToString();
} finally {
    if(Get("multiplayerPreview")!=null)Call("ExitMultiplayerPreview");
    if(keyboard!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    if(mouse!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    if(priorKeyboard!=null)priorKeyboard.MakeCurrent();if(priorMouse!=null)priorMouse.MakeCurrent();
    Call("LateUpdate");
}
