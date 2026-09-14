// Run in Play mode with Game view visible. Tests the Input System -> UI Toolkit path.
var game = UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
if (game == null) throw new Exception("Enter Play mode first");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = game.GetType();
var doc = game.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>().Single(d => d.name == "Arena UI (UI Toolkit)");
System.Collections.IEnumerator Verify()
{
    type.GetMethod("ResetRun", flags).Invoke(game, null);
    type.GetField("remaining", flags).SetValue(game, 10000f);
    type.GetField("shopOpen", flags).SetValue(game, false);
    type.GetField("shopProgress", flags).SetValue(game, 0f);
    var device = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
    var previousInput = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
    var previousBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
    UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
    UnityEngine.InputSystem.InputSystem.EnableDevice(device);
    // Automated Editor evaluation runs without OS focus. Keep this override test-only.
    var runtime = typeof(UnityEngine.UIElements.UIDocument).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
    var updateMode = runtime.GetProperty("eventSystemUpdateMode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    var previousMode = updateMode.GetValue(null);
    updateMode.SetValue(null, Enum.Parse(updateMode.PropertyType, "Always"));
    void PumpUi()
    {
        var binding = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var provider = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.InputForUI.EventProvider")).First(t => t != null);
        provider.GetMethod("NotifyUpdate", binding).Invoke(null, null);
        var system = runtime.GetProperty("defaultEventSystem", binding).GetValue(null);
        var update = system.GetType().GetMethod("Update");
        update.Invoke(system, new[] { Enum.Parse(update.GetParameters()[0].ParameterType, "Always") });
    }
    try
    {
        yield return new WaitForEndOfFrame();
        var button = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement, "toggleShop");
        if (doc.rootVisualElement.panel.Pick(button.worldBound.center) != button) throw new Exception("Shop button is occluded");
        var point = button.worldBound.center * doc.panelSettings.scale;
        point.y = Screen.height - point.y;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(device, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = 71, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
        UnityEngine.InputSystem.InputSystem.Update();
        PumpUi();
        yield return new WaitForSecondsRealtime(.1f);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(device, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = 71, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
        UnityEngine.InputSystem.InputSystem.Update();
        PumpUi();
        yield return new WaitForSecondsRealtime(.1f);
        if (!(bool)type.GetField("shopOpen", flags).GetValue(game)) throw new Exception("Native touch did not open the shop exactly once");
        yield return new WaitForSecondsRealtime(.2f);
        if (!(bool)type.GetField("shopOpen", flags).GetValue(game)) throw new Exception("Touch also generated a duplicate mouse click");
        Debug.Log("ARENA TOUCH UI PASS: panel picking, native touch button, no duplicate mouse activation.");
    }
    finally
    {
        UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
        updateMode.SetValue(null, previousMode);
        UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = previousInput;
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = previousBackground;
    }
}
game.StartCoroutine(Verify());
return "Native touch UI test started; completion is logged as ARENA TOUCH UI PASS.";
