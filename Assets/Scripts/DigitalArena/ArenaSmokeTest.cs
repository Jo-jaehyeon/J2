using System.Collections.Generic;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DigitalArena
{
    // Opt-in player integration test. Normal games never instantiate this component.
    public sealed class ArenaSmokeTest : MonoBehaviour
    {

        const BindingFlags	Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        DigitalArenaGame	game;

        string	output;
        int		assertions;
        bool	failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Environment.GetCommandLineArgs().Any(a => a == "-arena-smoke" || a == "-arena-stars-smoke"))
            {
                Application.runInBackground = true;
                new GameObject("3D Runtime Verification").AddComponent<ArenaSmokeTest>();
            }
        }

        T Field<T>(string name) => (T)typeof(DigitalArenaGame).GetField(name, Flags).GetValue(game);

        void Set(string name, object value) => typeof(DigitalArenaGame).GetField(name, Flags).SetValue(game, value);

        void Call(string name) => typeof(DigitalArenaGame).GetMethod(name, Flags).Invoke(game, null);

        void SetAccountFixture()
        {
            // Explicit opt-in smoke test only; no runtime fake-login endpoint or release bypass.
            var client = Field<ArenaAccountClient>("account");

            typeof(ArenaAccountClient).GetProperty("Profile").GetSetMethod(true).Invoke(client, new object[] { new ArenaAccountProfile { playerId = "smoke-only", nickname = "검증테이머", mmr = 1000, ownedCharacterIds = new string[0] } });
        }

        void Pointer(EventType type, Vector2 screen, int clicks = 1, int button = 0)
        {
            Vector2 mouse = (screen - Field<Vector2>("uiOffset")) / Field<float>("uiScale");

            typeof(DigitalArenaGame).GetMethod("HandlePointer", Flags).Invoke(game, new object[] { type, button, clicks, mouse });
        }

        void Drag(ArenaWorld3D world, int id, Vector3 target)
        {
            var unit = world.Units.Find(u => u.Id == id);

            Pointer(EventType.MouseDown, world.Project(unit.Root.position + Vector3.up * 0.6f), 1, 0);
            Pointer(EventType.MouseDrag, world.Project(target), 1, 0);
            Pointer(EventType.MouseUp, world.Project(target), 1, 0);
        }

        void Check(bool condition, string description)
        {
            if (!condition)
            {
                failed = true;
                Debug.LogError("ARENA SMOKE FAILED: " + description);
                Application.Quit(1);

                throw new Exception(description);
            }

            assertions++;
        }

        IEnumerator Capture(string name)
        {
            yield return null;

            string path = Path.Combine(output, name + ".png");

            var camera = Field<ArenaWorld3D>("world").Camera;

            var target = new RenderTexture(1440, 900, 24);

            var previous = RenderTexture.active;

            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;

            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);

            image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            Destroy(target);
            Destroy(image);
            Check(File.Exists(path), "captured " + name);
        }

        IEnumerator CaptureStarUi(string name)
        {
            // Opt in only with a visible player: hidden Windows players skip GUI rendering.
            if (!Environment.GetCommandLineArgs().Contains("-arena-stars-capture"))
            {
                yield break;
            }

            yield return new WaitForEndOfFrame();

            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();

            Check(screenshot.GetPixels32().Any(c => c.r > 20 || c.g > 20 || c.b > 20), "UI capture is not blank");
            File.WriteAllBytes(Path.Combine(output, name + ".png"), screenshot.EncodeToPNG());
            Destroy(screenshot);
        }

        IEnumerator StarSmoke()
        {
            SetAccountFixture();
            Call("StartSinglePlayer");

            var original = Field<ArenaRules>("rules");

            foreach (var data in original.Catalog.allies.Concat(original.Catalog.enemies))
            {
                if (!string.IsNullOrEmpty(data.prefabPath))
                {
                    Check(Resources.Load<GameObject>(data.prefabPath) != null, "migrated prefab " + data.id);
                }
            }

            var star = Resources.Load<Texture2D>("UI/RankStar");

            Check(star != null && star.GetPixel(0, 0).a < .01f, "generated star has transparent background");

            var catalog = original.Catalog.Copy();

            catalog.allies[0].role = DigimonRole.Marksman;

            var balance = original.Balance.Copy();

            foreach (var level in balance.levels)
            {
                level.weights = new[]
                {
                    100,
                    0,
                    0,
                    0,
                    0
                };
            }

            var rules = new ArenaRules(12, balance, catalog);

            rules.BeginRound();
            Set("rules", rules);
            Set("remaining", 999f);
            Set("shopOpen", false);
            Set("shopProgress", 0f);

            var world = Field<ArenaWorld3D>("world");

            foreach (int targetStars in new[]
            {
                1,
                2,
                3
            }

            )
            {
                while (!rules.Pieces.Any(p => p.Stars == targetStars))
                {
                    for (int slot = 0; slot < 5 && !rules.Pieces.Any(p => p.Stars == targetStars); slot++)
                    {
                        rules.Offers[slot] = 0;
                        rules.Buy(slot);
                    }

                    if (!rules.Pieces.Any(p => p.Stars == targetStars))
                    {
                        rules.StartBattle();
                        rules.ResolveBattle(true, 0);
                        rules.BeginRound();
                    }
                }

                world.Rebuild(rules, null);

                var piece = rules.Pieces.First(p => p.Stars == targetStars);

                Set("selectedUnit", world.Units.First(u => u.Id == piece.Id));
                Check(piece.Tier == 0 && piece.InvestedGold == ArenaRules.OriginalCopies(targetStars), "same species and investment at star " + targetStars);

                yield return CaptureStarUi("stars-" + targetStars + "-preparation");
            }

            rules.StartBattle();

            var battle = new ArenaBattle(rules);

            Set("battle", battle);
            Set("help", true);
            world.Rebuild(rules, battle);

            var unit = world.Units.First(u => !u.Enemy && u.Fighter != null && u.Fighter.Stars == 3);

            Set("selectedUnit", unit);
            Set("help", false);
            Check(unit.Data.role == DigimonRole.Marksman && unit.Fighter.Stars == 3, "combat view retains role and three stars");

            yield return CaptureStarUi("stars-3-combat");
        }

        IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs/Arena3D"));
            Directory.CreateDirectory(output);

            yield return null;
            yield return null;

            game = FindAnyObjectByType<DigitalArenaGame>();

            if (Environment.GetCommandLineArgs().Contains("-arena-stars-smoke"))
            {
                yield return StarSmoke();

                Debug.Log("ARENA STARS SMOKE PASSED: " + assertions + " assertions");
                Application.Quit(0);

                yield break;
            }

            Check(game != null, "game bootstraps");
            Check(Field<bool>("mainMenu") && Field<ArenaRules>("rules") == null, "boot stays on main menu without round income");
            Check(Resources.Load<Texture2D>("UI/DigiTacticsMainBackground") != null, "generated main background bundled");
            Call("StartSinglePlayer");
            Call("SelectMultiplayer");
            Check(Field<bool>("mainMenu") && !Field<bool>("multiplayerNotice") && Field<ArenaRules>("rules") == null, "unauthenticated player cannot start either mode");
            SetAccountFixture();
            Call("SelectMultiplayer");

            yield return new WaitForSeconds(.25f);

            Check(Field<bool>("mainMenu") && Field<bool>("multiplayerNotice") && Field<ArenaRules>("rules") == null, "multiplayer TODO leaves game unstarted");
            Call("StartSinglePlayer");
            Check(!Field<bool>("mainMenu"), "single selection starts game");
            Check(Resources.Load<Shader>("UI/DigimonTypeBadge").isSupported, "type badge texture shader supported");

            string iconOutput = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs/TypeIcons"));

            Directory.CreateDirectory(iconOutput);

            foreach (DigimonType type in Enum.GetValues(typeof(DigimonType)))
            {
                Check(DigimonBadge.Source(type) != null, "provided type PNG loaded " + type);

                var badge = DigimonBadge.Create(type, DigimonElement.Fire);

                Check(badge.GetPixel(0, 0).a < .01f && badge.GetPixel(32, 32).a > .99f, "badge transparency " + type);

                int minX = 64, maxX = -1, minY = 64, maxY = -1;

                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        var pixel = badge.GetPixel(x, y);

                        if (pixel.r < .7f || pixel.g > .5f || pixel.b > .5f)
                        {
                            continue;
                        }

                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                Check(maxX >= minX && Mathf.Abs((minX + maxX) * .5f - 31.5f) <= 1.5f && Mathf.Abs((minY + maxY) * .5f - 31.5f) <= 1.5f, "visible symbol centered " + type);
                File.WriteAllBytes(Path.Combine(iconOutput, type + ".png"), badge.EncodeToPNG());
                Destroy(badge);
            }

            Color? fixedBackground = null, fixedBorder = null;

            foreach (DigimonElement element in Enum.GetValues(typeof(DigimonElement)))
            {
                var badge = DigimonBadge.Create(DigimonType.Vaccine, element);

                var background = badge.GetPixel(32, 5);

                var border = badge.GetPixel(32, 1);

                if (fixedBackground.HasValue)
                {
                    Check(background == fixedBackground.Value && border == fixedBorder.Value, "frame independent of element " + element);
                }

                fixedBackground = background;
                fixedBorder = border;

                var expected = DigimonBadge.Colors[(int)element];

                Check(badge.GetPixels().Count(p => p.a > .99f && Mathf.Abs(p.r - expected.r) + Mathf.Abs(p.g - expected.g) + Mathf.Abs(p.b - expected.b) < .09f) > 20, "symbol uses element color " + element);
                File.WriteAllBytes(Path.Combine(iconOutput, "Vaccine_" + element + ".png"), badge.EncodeToPNG());
                Destroy(badge);
            }

            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);

            Set("uiScale", scale);
            Set("uiOffset", new Vector2((Screen.width - 1440 * scale) / 2, (Screen.height - 900 * scale) / 2));

            var rules = Field<ArenaRules>("rules");

            var world = Field<ArenaWorld3D>("world");

            Check(rules.Gold == 2, "first round starts with two gold");
            Check(Resources.Load<TextAsset>("DigimonCatalog") != null, "catalog bundled in player");
            Check(rules.Catalog.Validate() == null, "loaded catalog valid");
            Check(Mathf.Abs(world.Units.First(u => u.Enemy).Data.HP - rules.Catalog.enemies[rules.Catalog.EnemyAt(rules.Stage)].HP * .58f) < .01f, "enemy preview applies selected stage stats");
            Check(world.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Enemy bench ")) == 10, "ten enemy bench squares");
            Check(world.Camera.enabled && !world.Camera.orthographic, "perspective 3D camera");
            Check(Resources.Load<Shader>("ArenaSolid").isSupported, "3D shader supported in player");
            Check(rules.Preparing && Field<bool>("shopOpen"), "round opens selection drawer");

            yield return new WaitForSeconds(0.4f);
            yield return Capture("01-round-start");

            rules.Offers[0] = 0;
            Check(rules.Buy(0), "buy baby unit");
            Set("shopOpen", false);
            world.Rebuild(rules, null);

            yield return new WaitForSeconds(0.4f);

            var piece = rules.Pieces[0];

            var unit = world.Units.Find(u => u.Id == piece.Id);

            Check(world.Pick(world.Project(unit.Root.position + Vector3.up * 0.6f)) == piece.Id, "3D bench picking");

            for (int cell = 0; cell < ArenaRules.BoardSize; cell++)
            {
                var position = ArenaWorld3D.CellPosition(cell);

                Check(world.GroundPoint(world.Project(position), out var ground) && ArenaWorld3D.DropCell(ground) == cell, "project and drop cell " + cell);
            }

            Vector2 benchHit = world.Project(unit.Root.position + Vector3.up * 0.6f);

            Pointer(EventType.MouseDown, benchHit);
            Pointer(EventType.MouseUp, benchHit);
            Check(piece.Cell == -1, "single click does not place piece");
            Check(Field<ArenaWorld3D.UnitView>("selectedUnit") == null, "left click does not open details");
            Pointer(EventType.MouseDown, benchHit, 1, 1);
            Check(Field<ArenaWorld3D.UnitView>("selectedUnit") == unit, "right click opens unit details");
            Pointer(EventType.MouseDown, benchHit, 1, 1);
            Check(Field<ArenaWorld3D.UnitView>("selectedUnit") == null, "right click again closes unit details");

            var tamer = Field<PlayableCharacterMotor>("playableCharacter");

            Check(tamer != null, "existing tamer prefab loaded");

            var destination = ArenaWorld3D.CellPosition(20);

            Pointer(EventType.MouseDown, world.Project(destination), 1, 1);
            Check((new Vector2(tamer.Destination.x, tamer.Destination.z) - new Vector2(destination.x, destination.z)).sqrMagnitude < .01f, "right click ground moves tamer");

            var savedDestination = tamer.Destination;

            Pointer(EventType.MouseDown, world.Project(ArenaWorld3D.CellPosition(21)), 1, 0);
            Check(tamer.Destination == savedDestination, "left ground click does not move tamer");
            Pointer(EventType.MouseDown, benchHit, 2);
            Pointer(EventType.MouseUp, benchHit, 2);
            Check(piece.Cell >= 0, "double click input deploys piece");

            yield return Capture("02-deployed-3d");

            Drag(world, piece.Id, ArenaWorld3D.CellPosition(31));
            Check(piece.Cell == 31, "drag input manually places piece");
            Drag(world, piece.Id, ArenaWorld3D.Position(0, 6));
            Check(piece.Cell == 31, "enemy drop cancels placement");
            Drag(world, piece.Id, ArenaWorld3D.BenchPosition(0));
            Check(piece.Cell == -1, "drag input returns piece to bench");
            Drag(world, piece.Id, ArenaWorld3D.BenchPosition(7));
            Check(piece.BenchSlot == 7, "drag rearranges bench slot");
            Check(piece.Cell < 0, "leave piece on bench for timeout deployment");
            world.Rebuild(rules, null);
            Set("remaining", 0.01f);

            yield return new WaitForSeconds(0.2f);

            var battle = Field<ArenaBattle>("battle");

            float trainCenter = rules.Train.Center;

            Check(!rules.Preparing && battle != null && !Field<bool>("shopOpen"), "timer automatically starts battle and closes drawer");
            Check(piece.Cell >= 0 && piece.BenchSlot == -1 && battle.Fighters.Any(f => !f.Enemy), "timeout deploys bench piece before combat fighters are created");

            var enemy = world.Units.First(u => u.Enemy);

            Pointer(EventType.MouseDown, world.Project(enemy.Root.position + Vector3.up * .6f), 1, 1);
            Check(Field<ArenaWorld3D.UnitView>("selectedUnit") == enemy, "combat enemy inspection");

            float deadline = Time.realtimeSinceStartup + 35;

            while (!battle.Finished && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Check(battle.Finished && rules.LastBattleXp == 2, "combat awards two XP including level-up carry");
            Check(rules.Train.Center == trainCenter, "train stays still during combat");

            while (rules.Round == 1 && Time.realtimeSinceStartup < deadline + 6)
            {
                yield return null;
            }

            Check(rules.Round == 2 && Field<bool>("shopOpen"), "next round reopens drawer");
            Call("BeginBattle");
            rules.ResolveBattle(false, 100);
            Check(rules.Health == 0, "lethal damage ends game");
            Call("ResetRun");
            rules = Field<ArenaRules>("rules");
            Check(rules.Health == 100 && rules.Round == 1 && rules.Pieces.Count == 0, "restart resets state");
            Set("shopOpen", false);
            Set("shopProgress", 0f);
            rules.Offers[0] = 0;
            Check(rules.Buy(0), "purchase for sale input");
            world.Rebuild(rules, null);

            var salePiece = rules.Pieces.Single();

            Vector2 saleScreen = new Vector2(720, 850) * Field<float>("uiScale") + Field<Vector2>("uiOffset");

            void BeginSaleDrag()
            {
                var view = world.Units.Find(u => u.Id == salePiece.Id);

                Pointer(EventType.MouseDown, world.Project(view.Root.position + Vector3.up * .6f), 1, 0);
                Pointer(EventType.MouseDrag, saleScreen, 1, 0);
            }

            BeginSaleDrag();
            Check(Field<bool>("saleHover"), "bottom drag reveals sale UI");
            Pointer(EventType.MouseDrag, world.Project(ArenaWorld3D.BenchPosition(7)), 1, 0);
            Check(!Field<bool>("saleHover"), "leaving bottom cancels sale preview");
            Pointer(EventType.MouseUp, world.Project(ArenaWorld3D.BenchPosition(7)), 1, 0);
            Check(rules.Pieces.Count == 1 && rules.Gold == 1, "cancelled sale keeps unit and gold");
            BeginSaleDrag();
            Pointer(EventType.MouseUp, saleScreen, 1, 0);
            Check(rules.Pieces.Count == 0 && rules.Gold == 2 && !Field<bool>("saleHover"), "bench drop sells once");
            rules.Offers[1] = 0;
            Check(rules.Buy(1), "purchase for field sale");
            salePiece = rules.Pieces.Single();
            rules.AutoDeploy(salePiece.Id);
            world.Rebuild(rules, null);
            BeginSaleDrag();
            Pointer(EventType.MouseUp, saleScreen, 1, 0);
            Check(rules.Pieces.Count == 0 && rules.Gold == 2, "field drop sells unit");

            var expanded = rules.Catalog.Copy();

            var extra = expanded.allies[0].Copy();

            extra.id = "smoke_added";
            extra.name = "Added unit";
            extra.evolvesTo = "";
            extra.cost = 1;
            expanded.allies = expanded.allies.Concat(new[] { extra }).ToArray();

            foreach (var row in expanded.enemies)
            {
                row.startStage = 2;
            }

            var extraEnemy = expanded.enemies[0].Copy();

            extraEnemy.id = "smoke_enemy";
            extraEnemy.name = "Added enemy";
            extraEnemy.startStage = 1;
            expanded.enemies = expanded.enemies.Concat(new[] { extraEnemy }).ToArray();

            var expandedRules = new ArenaRules(1, null, expanded);

            expandedRules.BeginRound();
            expandedRules.Offers[0] = expanded.allies.Length - 1;
            Check(expandedRules.Buy(0) && expandedRules.AutoDeploy(expandedRules.Pieces[0].Id), "added unit purchased and deployed");

            var portraits = world.CreatePortraits(expanded);

            var creepPortraits = world.CreatePortraits(expanded, true);

            Check(portraits.Length == expanded.allies.Length && creepPortraits.Length == expanded.enemies.Length, "portraits expand with registered units");

            foreach (var texture in portraits.Concat(creepPortraits))
            {
                Destroy(texture);
            }

            world.Rebuild(expandedRules, null);
            Check(world.Units.Any(u => u.Root.name == extra.name) && world.Units.Any(u => u.Root.name == extraEnemy.name), "new ally and creep models instantiated");

            yield return Capture("03-expanded-catalog");

            expandedRules.StartBattle();

            var expandedBattle = new ArenaBattle(expandedRules);

            Check(expandedBattle.Fighters.Any(f => f.Data.id == extra.id) && expandedBattle.Fighters.Any(f => f.Data.id == extraEnemy.id), "added data participates in combat");
            world.Rebuild(rules, null);

            var tsumemon = Resources.Load<GameObject>("Digimon/1코스트/Tsumemon/Tsumemon");

            Check(tsumemon != null, "Tsumemon prefab bundled");

            var model = Instantiate(tsumemon);

            model.transform.position = new Vector3(1000, 0, 0);

            var motion = model.GetComponent<ArenaUnitAnimation>();

            var clips = model.GetComponent<Animation>();

            Check(motion != null && clips.GetClipCount() == 6, "Tsumemon six animation states");

            foreach (string clip in new[]
            {
                "Idle",
                "Walk",
                "Attack",
                "Special",
                "Hit",
                "Death"
            }

            )
            {
                clips.Stop();
                clips.Play(clip);
                clips[clip].time = clips[clip].length * .5f;
                clips.Sample();
                Check(clips.GetClip(clip).length > 0, "animation clip samples " + clip);
            }

            clips.Stop();
            clips.Play("Walk");
            clips["Walk"].time = .3f;
            clips.Sample();
            Check(model.transform.Find("Rig").localPosition.y > .05f, "walk animation changes body pose");

            var animationFighter = new ArenaBattle.Fighter
            {
                Hp = 100
            };

            motion.Pose(false, animationFighter, .01f);
            animationFighter.AttackCount++;
            motion.Pose(false, animationFighter, .01f);
            Check(clips.IsPlaying("Attack"), "battle attack triggers animation");
            animationFighter.UsedSkill = true;
            animationFighter.AttackCount++;
            motion.Pose(false, animationFighter, .01f);
            Check(clips.IsPlaying("Special"), "SP attack triggers special animation");
            animationFighter.HitCount++;
            motion.Pose(false, animationFighter, .01f);
            Check(clips.IsPlaying("Hit"), "damage triggers hit animation");
            animationFighter.Hp = 0;
            motion.Pose(false, animationFighter, .2f);
            Check(!motion.DeathComplete && clips.IsPlaying("Death"), "death remains visible during animation");
            motion.Pose(false, animationFighter, .7f);
            Check(motion.DeathComplete, "death animation completes before removal");
            Destroy(model);
            Debug.Log("ARENA 3D SMOKE PASSED: " + assertions + " checks");
            Application.Quit(failed ? 1 : 0);
        }

        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                failed = true;
                Application.Quit(1);
            }
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }
    }
}
#endif

