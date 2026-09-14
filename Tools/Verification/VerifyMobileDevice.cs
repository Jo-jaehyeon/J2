// Run after VerifyMobile.cs in Play mode. Exercises the real Input System touch adapter.
var g = UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
var type = g.GetType();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
object Get(string n) => type.GetField(n, flags).GetValue(g);
object Call(string n, params object[] args) => type.GetMethod(n, flags).Invoke(g, args);
void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
var screen = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
try
{
    var ui = new Vector2(1300, 225);
    var point = (Vector2)Call("ScreenPoint", ui); point.y = Screen.height - point.y;
    void Send(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 p)
    {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(screen, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = id, phase = phase, position = p });
        UnityEngine.InputSystem.InputSystem.Update();
        Call("UpdateTouchInput");
    }
    Send(41, UnityEngine.InputSystem.TouchPhase.Began, point);
    Check((int)Get("touchFinger") == 41, "First touch was not captured");
    Send(42, UnityEngine.InputSystem.TouchPhase.Began, point + Vector2.right * 100);
    Check((int)Get("touchFinger") == 41, "Second finger stole gesture");
    Send(41, UnityEngine.InputSystem.TouchPhase.Canceled, point);
    Check((int)Get("touchFinger") == -1 && (int)Get("dragId") == -1, "Canceled touch retained a world gesture");
    UnityEngine.InputSystem.InputSystem.Update(); Call("UpdateTouchInput");
    Check((int)Get("touchFinger") == -1, "Held secondary finger took over");
    Send(42, UnityEngine.InputSystem.TouchPhase.Ended, point);
    Send(43, UnityEngine.InputSystem.TouchPhase.Began, point);
    Send(43, UnityEngine.InputSystem.TouchPhase.Ended, point);
    Check((int)Get("touchFinger") == -1, "Fresh UI tap retained gesture ownership");
    Check((bool)type.GetProperty("SuppressTouchMouse", flags).GetValue(g), "Touch release did not suppress synthetic mouse");
    Call("CancelDrag");
    return "PASS: Input System device events, finger ownership, multitouch isolation, cancellation, secondary-finger lockout, new tap, synthetic mouse suppression.";
}
finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(screen); }
