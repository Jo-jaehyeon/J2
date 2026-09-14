// Run in Play mode. Verifies the Input System adapter that replaces OnGUI mouse events.
var game = UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
if (game == null) throw new Exception("Enter Play mode first");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = game.GetType();
object Get(string name) => type.GetField(name, flags).GetValue(game);
void Set(string name, object value) => type.GetField(name, flags).SetValue(game, value);
object Call(string name, params object[] args) => type.GetMethod(name, flags).Invoke(game, args);
void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
Call("ResetRun"); Call("UpdateUiMetrics");
Set("shopOpen", false); Set("shopProgress", 0f); Set("remaining", 10000f);
Set("touchFinger", -1); Set("suppressMouseUntilFrame", -1);
var rules = (DigitalArena.ArenaRules)Get("rules");
var world = (DigitalArena.ArenaWorld3D)Get("world");
rules.Pieces.Add(new DigitalArena.ArenaRules.Piece { Id = 9101, Tier = 0, BenchSlot = 4 });
world.Rebuild(rules, null);
Vector2 Ui(Vector3 point) => (Vector2)Call("UiPoint", world.Project(point));
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
var previousBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
var previousInput = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
try
{
    void Send(Vector2 ui, bool left = false, bool right = false)
    {
        var point = (Vector2)Call("ScreenPoint", ui); point.y = Screen.height - point.y;
        var delta = point - mouse.position.ReadValue();
        var state = new UnityEngine.InputSystem.LowLevel.MouseState { position = point, delta = delta };
        if (left) state = state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
        if (right) state = state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, state);
        UnityEngine.InputSystem.InputSystem.Update();
        Call("UpdateMouseInput");
    }
    var start = Ui(world.Units.Single(u => u.Id == 9101).Root.position + Vector3.up * .5f);
    var drop = Ui(DigitalArena.ArenaWorld3D.CellPosition(20));
    Send(start, true); Send(drop, true); Send(drop);
    Check(rules.Pieces.Single(p => p.Id == 9101).Cell == 20, "Mouse adapter drag placement failed");
    start = Ui(world.Units.Single(u => u.Id == 9101).Root.position + Vector3.up * .5f);
    Send(start, right: true); Send(start);
    Check(Get("selectedUnit") != null, "Right click did not show details");
    Set("lastBoardClickTime", -1f);
    Send(start, true); Send(new Vector2(700, 850), true); Send(new Vector2(700, 850));
    Check(rules.Pieces.All(p => p.Id != 9101), "Mouse adapter sale failed");
    rules.Pieces.Add(new DigitalArena.ArenaRules.Piece { Id = 9102, Tier = 0, BenchSlot = 4 }); world.Rebuild(rules, null);
    start = Ui(world.Units.Single(u => u.Id == 9102).Root.position + Vector3.up * .5f);
    Set("lastBoardClickTime", -1f);
    Send(start, true); Send(start); Send(start, true); Send(start);
    Check(rules.Pieces.Single(p => p.Id == 9102).Cell >= 0, "Double click did not auto deploy");
    Set("shopOpen", true);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
    UnityEngine.InputSystem.InputSystem.Update(); Call("UpdateMouseInput");
    Check(!(bool)Get("shopOpen") && (int)Get("dragId") == -1, "Escape did not close shop and cancel drag");
    return "PASS: Input System mouse drag placement, right-click details, sale, double-click auto deployment, Escape cancellation.";
}
finally
{
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = previousBackground;
    UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = previousInput;
    Call("CancelDrag");
}
