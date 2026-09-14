// Run with `unity command eval_file --file Tools/Verification/VerifyMobile.cs` in Play mode.
var g = UnityEngine.Object.FindAnyObjectByType<DigitalArena.DigitalArenaGame>();
if (g == null) throw new Exception("Enter Play mode first");
var type = g.GetType();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
object Get(string n) => type.GetField(n, flags).GetValue(g);
void Set(string n, object v) => type.GetField(n, flags).SetValue(g, v);
object Call(string n, params object[] args) => type.GetMethod(n, flags).Invoke(g, args);
void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
void Begin(Vector2 p) { Set("touchFinger", 100); Call("BeginTouch", p); }
void End(Vector2 p) { Call("EndTouch", p); Set("touchFinger", -1); }
Call("ResetRun");
Call("UpdateUiMetrics");
Set("shopOpen", false); Set("shopProgress", 0f); Set("remaining", 10000f);
var rules = (DigitalArena.ArenaRules)Get("rules");
var world = (DigitalArena.ArenaWorld3D)Get("world");
var motor = (DigitalArena.PlayableCharacterMotor)Get("playableCharacter");
Vector2 Ui(Vector3 p) => (Vector2)Call("UiPoint", world.Project(p));
rules.Pieces.Add(new DigitalArena.ArenaRules.Piece { Id = 9001, Tier = 0, BenchSlot = 4 });
world.Rebuild(rules, null);
var unit = world.Units.Single(u => u.Id == 9001);
var start = Ui(unit.Root.position + Vector3.up * .5f);
Begin(start); End(start + new Vector2(2, 0));
Check(Get("selectedUnit") == unit, "Tap with finger jitter did not show details");
Check((int)Get("dragId") == -1, "Tap started a drag");
Begin(start); End(start);
Check(Get("selectedUnit") == null, "Second tap did not close details");
var drop = Ui(DigitalArena.ArenaWorld3D.CellPosition(20));
Begin(start); Call("MoveTouch", drop); End(drop);
Check(rules.Pieces.Single(p => p.Id == 9001).Cell == 20, "Touch placement failed");
unit = world.Units.Single(u => u.Id == 9001);
start = Ui(unit.Root.position + Vector3.up * .5f);
Begin(start); Call("MoveTouch", new Vector2(700, 850)); Call("OnApplicationPause", true); End(new Vector2(700, 850));
Check(rules.Pieces.Any(p => p.Id == 9001) && (int)Get("dragId") == -1, "Pause did not cancel sale");
var before = motor.Destination;
Begin(new Vector2(1300, 225)); End(new Vector2(700, 500));
Check(motor.Destination == before, "UI gesture leaked into movement");
Vector2 empty = default; bool found = false;
for (int y = 350; y < 650 && !found; y += 25)
for (int x = 350; x < 1150 && !found; x += 25)
{
    var p = new Vector2(x, y); var screen = (Vector2)Call("ScreenPoint", p);
    if (!(bool)Call("OverUi", p) && world.PickUnit(screen) == null && world.GroundPoint(screen, out var ground) && DigitalArena.PlayableCharacterMotor.InBattlefield(ground)) { empty = p; found = true; }
}
Check(found, "No visible empty battlefield point");
Begin(empty); End(empty);
Check(motor.Destination != before, "Empty ground tap did not move tamer");
before = motor.Destination;
Call("ViewPlayer", true); Begin(empty); End(empty);
Check(motor.Destination == before, "Opponent view allowed movement");
Call("ViewPlayer", false);
unit = world.Units.Single(u => u.Id == 9001); start = Ui(unit.Root.position + Vector3.up * .5f);
Begin(start); Call("MoveTouch", drop + new Vector2(80, 0)); Call("BeginBattle"); End(new Vector2(700, 850));
Check(rules.Pieces.Any(p => p.Id == 9001), "Phase change failed to cancel drag");
unit = world.Units.First(u => u.Enemy && u.Root.gameObject.activeSelf);
start = Ui(unit.Root.position + Vector3.up * .5f);
Begin(start); End(start);
Check(Get("selectedUnit") == unit, "Enemy battle unit touch details failed");
Call("ResetRun"); Set("shopOpen", false); Set("shopProgress", 0f);
rules = (DigitalArena.ArenaRules)Get("rules");
rules.Pieces.Add(new DigitalArena.ArenaRules.Piece { Id = 9002, Tier = 0, BenchSlot = 4 }); world.Rebuild(rules, null);
unit = world.Units.Single(u => u.Id == 9002); start = Ui(unit.Root.position + Vector3.up * .5f);
int gold = rules.Gold;
Begin(start); Call("MoveTouch", new Vector2(700, 850)); End(new Vector2(700, 850));
Check(rules.Pieces.All(p => p.Id != 9002) && rules.Gold > gold, "Touch sale failed");
// Board touch handling leaves UI taps to UI Toolkit's pointer events.
Begin(new Vector2(1300, 820)); End(new Vector2(1300, 820));
Check(!(bool)Get("shopOpen"), "Board touch handler also activated a UI control");
Set("remaining", 10000f);
return "PASS: tap/jitter/details toggle, drag placement, pause cancellation, UI isolation, ground movement, opponent view, battle transition cancellation, enemy battle details, sale, native UI event ownership.";
