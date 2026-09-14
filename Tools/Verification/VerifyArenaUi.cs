// Run in Play mode. Uses in-memory gameplay fixtures; never calls account APIs.
var g = UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
if (g == null) throw new Exception("Enter Play mode first");
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = g.GetType();
object Get(string name) => type.GetField(name, flags).GetValue(g);
void Set(string name, object value) => type.GetField(name, flags).SetValue(g, value);
object Call(string name, params object[] args) => type.GetMethod(name, flags).Invoke(g, args);
int checks = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
var document = g.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>().Single(d => d.name == "Arena UI (UI Toolkit)");
UnityEngine.UIElements.Button Button(string name) => UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(document.rootVisualElement, name);
void Draw() => Call("LateUpdate");
System.Collections.IEnumerator Click(string name)
{
    yield return new WaitForEndOfFrame();
    var button = Button(name);
    Check(button != null && button.style.display.value != UnityEngine.UIElements.DisplayStyle.None, "Missing button: " + name);
    using (var evt = UnityEngine.UIElements.PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = button.worldBound.center })) { evt.target = button; button.SendEvent(evt); }
    using (var evt = UnityEngine.UIElements.PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = button.worldBound.center })) { evt.target = button; button.SendEvent(evt); }
    Draw();
}
System.Collections.IEnumerator Verify()
{
Call("ResetRun");
Set("remaining", 10000f); Set("shopProgress", 1f);
var rules = (DigitalArena.ArenaRules)Get("rules");
var world = (DigitalArena.ArenaWorld3D)Get("world");
void SetRule(string name, int value) => rules.GetType().GetProperty(name).GetSetMethod(true).Invoke(rules, new object[] { value });
SetRule("Gold", 50);
Draw();
var retained = Button("toggleShop"); Draw();
Check(ReferenceEquals(retained, Button("toggleShop")), "Button was rebuilt between frames");
int gold = rules.Gold, pieces = rules.Pieces.Count;
int price = DigitalArena.ArenaRules.PurchasePrice(rules.Catalog.allies[rules.Offers[0]]);
yield return Click("offer0");
Check(rules.Gold == gold - price && rules.Pieces.Count == pieces + 1 && rules.Bought[0], "Purchase did not target slot 0 exactly once");
Check(Button("offer0").style.display.value == UnityEngine.UIElements.DisplayStyle.None, "Purchased slot remained clickable");
gold = rules.Gold; yield return Click("reroll");
Check(rules.Gold == gold - DigitalArena.ArenaRules.RerollCost && !rules.Bought[0], "Reroll failed");
yield return Click("economy");
Check((bool)Get("economyOpen"), "Economy panel did not open");
gold = rules.Gold; yield return Click("buyXp");
Check(rules.Gold == gold - 4, "XP purchase failed");
yield return Click("guide");
Check((bool)Get("help") && !Button("reroll").enabledSelf && !Button("economy").enabledSelf, "Guide failed to disable underlying controls");
gold = rules.Gold;
using (var evt = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()) Button("reroll").SendEvent(evt);
Check(rules.Gold == gold, "Disabled control accepted a submit");
float remaining = (float)Get("remaining"); Call("Update");
Check((float)Get("remaining") == remaining, "Guide failed to pause gameplay");
yield return Click("continue"); Check(!(bool)Get("help") && Button("reroll").enabledSelf, "Guide did not resume gameplay");
yield return Click("opponentView"); Check((bool)Get("viewingOpponent"), "Opponent view failed");
yield return Click("myView"); Check(!(bool)Get("viewingOpponent"), "Own view failed");
Set("selectedUnit", world.Units.First(u => !u.Enemy)); Draw();
yield return Click("closeDetails"); Check(Get("selectedUnit") == null, "Details close failed");
yield return Click("closeShop"); Check(!(bool)Get("shopOpen"), "Shop close failed");
yield return Click("toggleShop"); Check((bool)Get("shopOpen"), "Shop reopen failed");
SetRule("Gold", 0); Draw();
Check(!Button("reroll").enabledSelf, "Unaffordable reroll enabled");
SetRule("Health", 0); Draw();
Check(!Button("toggleShop").enabledSelf && Button("restart").enabledSelf, "Game-over controls not isolated");
yield return Click("restart"); rules = (DigitalArena.ArenaRules)Get("rules");
Check(rules.Health == 100 && rules.Preparing && Button("restart").style.display.value == UnityEngine.UIElements.DisplayStyle.None, "Restart failed to reset screen and rules");
Set("remaining", 10000f);
Check(type.GetMethod("OnGUI", flags) == null, "Legacy UI renderer remains");
Debug.Log("ARENA UI PASS: " + checks + " checks — retained controls, purchase, reroll, XP, guide pause/modal isolation, views, details, shop, affordability, game over and restart.");
}
g.StartCoroutine(Verify());
return "Arena UI pointer regression started; completion is logged as ARENA UI PASS.";
