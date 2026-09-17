var root = new UnityEngine.GameObject("Player input verification");
var world = root.AddComponent<J2.MultiplayerMap.MultiplayerMapPreview>();
var font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Arial" }, 18);
J2.MultiplayerMap.MultiplayerGameUi hud = null;
void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
try
{
    world.Initialize(UnityEngine.Resources.Load<UnityEngine.GameObject>("Map/DragonEyeMultiplayer"), 6);
    var spawner = root.AddComponent<J2.MultiplayerMap.MultiplayerEntitySpawner>();
    spawner.Initialize(world.ViewedArena);
    var control = root.AddComponent<J2.Creatures.PlayerController>();
    control.Initialize(world.ViewCamera);
    var owner = root.AddComponent<J2.MultiplayerMap.MultiplayerSceneController>();
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    owner.GetType().GetField("<Spawner>k__BackingField", flags).SetValue(owner, spawner);
    owner.GetType().GetField("<PlayerController>k__BackingField", flags).SetValue(owner, control);
    hud = new J2.MultiplayerMap.MultiplayerGameUi(root.transform, font, owner);
    int exits = 0, details = 0, sold = 0, placements = 0, deployed = 0;
    control.ConfigureInteraction(spawner, hud, () => world.ViewedArena, () => exits++);
    hud.UnitDetailsRequested += id => details = id;
    hud.SellRequested += id => sold = id;
    hud.PlacementRequested += (id, point) => placements = id;
    hud.AutoDeployRequested += id => deployed = id;
    var table = UnityEngine.Resources.Load<J2.Spawning.SpawnTypeTable>("SpawnTypeTable");
    var type = table.entries.Find(e => e.kind == J2.Spawning.SpawnKind.Player);
    owner.SetLocalEntityId(801);
    owner.HandleSpawn(new J2.Protocol.S_Spawn { EntityId = 802, SpawnTypeId = type.id, X = 6 });
    Check(control.Player == null, "Remote spawn bound to input");
    owner.HandleSpawn(new J2.Protocol.S_Spawn { EntityId = 801, SpawnTypeId = type.id });
    Check(control.Player != null && control.Player.EntityId == 801, "Own spawn did not bind");
    owner.HandleSpawn(new J2.Protocol.S_Spawn { EntityId = 803, SpawnTypeId = type.id, X = -6 });
    Check(control.Player.EntityId == 801, "Remote spawn stole input");
    float scale = UnityEngine.Mathf.Min(UnityEngine.Screen.width / 1440f, UnityEngine.Screen.height / 900f);
    var offset = new UnityEngine.Vector2((UnityEngine.Screen.width - 1440 * scale) / 2, (UnityEngine.Screen.height - 900 * scale) / 2);
    var target = world.ViewedArena.TransformPoint(new UnityEngine.Vector3(2, .25f, -4));
    var screen = (UnityEngine.Vector2)world.ViewCamera.WorldToScreenPoint(target);
    var previousDestination = control.Player.Destination;
    Check(control.TryGetMoveWorldPoint(screen, out var converted) && UnityEngine.Vector3.Distance(converted, target) < .01f, "World coordinate conversion failed");
    control.PointAt(screen, p => false);
    Check(control.Player.Destination == previousDestination, "Click changed local destination before server response");
    var original = control.Player.transform.position;
    // Exercise the same gesture endpoints with explicit selection so model bounds cannot affect this test.
    var pressed = control.GetType().GetField("pressed", flags);
    var dragging = control.GetType().GetField("dragging", flags);
    var end = control.GetType().GetMethod("End", flags);
    pressed.SetValue(control, (int?)802); dragging.SetValue(control, false);
    end.Invoke(control, new object[] { screen, scale, offset, true, 0 });
    Check(details == 802, "Touch details lost");
    pressed.SetValue(control, (int?)802); dragging.SetValue(control, true);
    end.Invoke(control, new object[] { screen, scale, offset, false, 0 });
    Check(placements == 802, "Placement callback lost");
    var sellScreen = new UnityEngine.Vector2(offset.x + 720 * scale, UnityEngine.Screen.height - offset.y - 850 * scale);
    pressed.SetValue(control, (int?)802); dragging.SetValue(control, true);
    end.Invoke(control, new object[] { sellScreen, scale, offset, false, 0 });
    Check(sold == 802, "Sale callback lost");
    pressed.SetValue(control, (int?)802); dragging.SetValue(control, false);
    end.Invoke(control, new object[] { screen, scale, offset, false, 2 });
    Check(deployed == 802, "Double click callback lost");
    Check(control.Player.transform.position == original, "Input directly moved transform");
    control.CancelMovement();
    Check(!control.Player.IsMoving && hud.DragEntityId == null, "Cancellation failed");
    return "PASS: own EntityId auto-binding, remote isolation, ground input handler, touch details, placement/sale/double-click callbacks, gesture cancellation; input leaves transform updates to Creature.";
}
finally
{
    hud?.Dispose();
    world.Shutdown();
    UnityEngine.Object.DestroyImmediate(root);
    UnityEngine.Object.DestroyImmediate(font);
}

