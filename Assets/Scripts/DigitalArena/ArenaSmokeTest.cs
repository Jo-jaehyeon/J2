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
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        DigitalArenaGame game;
        string output;
        int assertions;
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Environment.GetCommandLineArgs().Contains("-arena-smoke"))
            {
                Application.runInBackground = true;
                new GameObject("3D Runtime Verification").AddComponent<ArenaSmokeTest>();
            }
        }
        T Field<T>(string name) => (T)typeof(DigitalArenaGame).GetField(name,Flags).GetValue(game);
        void Set(string name,object value) => typeof(DigitalArenaGame).GetField(name,Flags).SetValue(game,value);
        void Call(string name) => typeof(DigitalArenaGame).GetMethod(name,Flags).Invoke(game,null);
        void Pointer(EventType type, Vector2 screen, int clicks=1, int button=0)
        {
            Vector2 mouse=(screen-Field<Vector2>("uiOffset"))/Field<float>("uiScale");
            typeof(DigitalArenaGame).GetMethod("HandlePointer",Flags).Invoke(game,new object[] { type,button,clicks,mouse });
        }
        void Drag(ArenaWorld3D world,int id,Vector3 target)
        {
            var unit=world.Units.Find(u=>u.Id==id);
            Pointer(EventType.MouseDown,world.Project(unit.Root.position+Vector3.up*0.6f),1,1);
            Pointer(EventType.MouseDrag,world.Project(target),1,1);
            Pointer(EventType.MouseUp,world.Project(target),1,1);
        }
        void Check(bool condition,string description)
        {
            if (!condition) { failed=true; Debug.LogError("ARENA SMOKE FAILED: "+description); Application.Quit(1); throw new Exception(description); }
            assertions++;
        }
        IEnumerator Capture(string name)
        {
            yield return null;
            string path=Path.Combine(output,name+".png");
            var camera=Field<ArenaWorld3D>("world").Camera;
            var target=new RenderTexture(1440,900,24);
            var previous=RenderTexture.active;
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1440,900),0,0); image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous;
            target.Release(); Destroy(target); Destroy(image);
            Check(File.Exists(path),"captured "+name);
        }
        IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Logs/Arena3D"));
            Directory.CreateDirectory(output);
            yield return null; yield return null;
            game=FindAnyObjectByType<DigitalArenaGame>();
            Check(game!=null,"game bootstraps");
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            Set("uiScale",scale);
            Set("uiOffset",new Vector2((Screen.width-1440*scale)/2,(Screen.height-900*scale)/2));
            var rules=Field<ArenaRules>("rules"); var world=Field<ArenaWorld3D>("world");
            Check(rules.Gold==8,"five starting gold plus three income");
            Check(Resources.Load<TextAsset>("DigimonCatalog")!=null,"catalog bundled in player");
            Check(rules.Catalog.Validate()==null,"loaded catalog valid");
            Check(Mathf.Abs(world.Units.First(u=>u.Enemy).Data.HP-rules.Catalog.enemies[0].HP*.58f)<.01f,"enemy preview applies stage stats");
            Check(world.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Enemy bench "))==10,"ten enemy bench squares");
            Check(world.Camera.enabled && !world.Camera.orthographic,"perspective 3D camera");
            Check(Resources.Load<Shader>("ArenaSolid").isSupported,"3D shader supported in player");
            Check(rules.Preparing && Field<bool>("shopOpen"),"round opens selection drawer");
            yield return new WaitForSeconds(0.4f);
            yield return Capture("01-round-start");
            rules.Offers[0]=2; Check(rules.Buy(0),"buy Agumon");
            Set("shopOpen",false); world.Rebuild(rules,null);
            yield return new WaitForSeconds(0.4f);
            var piece=rules.Pieces[0];
            var unit=world.Units.Find(u=>u.Id==piece.Id);
            Check(world.Pick(world.Project(unit.Root.position+Vector3.up*0.6f))==piece.Id,"3D bench picking");
            for(int cell=0;cell<ArenaRules.BoardSize;cell++)
            {
                var position=ArenaWorld3D.CellPosition(cell);
                Check(world.GroundPoint(world.Project(position),out var ground) && ArenaWorld3D.DropCell(ground)==cell,"project and drop cell "+cell);
            }
            Vector2 benchHit=world.Project(unit.Root.position+Vector3.up*0.6f);
            Pointer(EventType.MouseDown,benchHit); Pointer(EventType.MouseUp,benchHit);
            Check(piece.Cell==-1,"single click does not place piece");
            Check(Field<ArenaWorld3D.UnitView>("selectedUnit")==unit,"left click opens unit details");
            Pointer(EventType.MouseDown,benchHit,2); Pointer(EventType.MouseUp,benchHit,2);
            Check(piece.Cell>=0,"double click input deploys piece");
            yield return Capture("02-deployed-3d");
            Drag(world,piece.Id,ArenaWorld3D.CellPosition(31));
            Check(piece.Cell==31,"drag input manually places piece");
            Drag(world,piece.Id,ArenaWorld3D.Position(0,6));
            Check(piece.Cell==31,"enemy drop cancels placement");
            Drag(world,piece.Id,ArenaWorld3D.BenchPosition(0));
            Check(piece.Cell==-1,"drag input returns piece to bench");
            Drag(world,piece.Id,ArenaWorld3D.BenchPosition(7));
            Check(piece.BenchSlot==7,"drag rearranges bench slot");
            Check(rules.AutoDeploy(piece.Id),"redeploy for battle"); world.Rebuild(rules,null);
            Set("remaining",0.01f);
            yield return new WaitForSeconds(0.2f);
            var battle=Field<ArenaBattle>("battle");
            float trainCenter=rules.Train.Center;
            Check(!rules.Preparing && battle!=null && !Field<bool>("shopOpen"),"timer automatically starts battle and closes drawer");
            var enemy=world.Units.First(u=>u.Enemy);
            Pointer(EventType.MouseDown,world.Project(enemy.Root.position+Vector3.up*.6f));
            Check(Field<ArenaWorld3D.UnitView>("selectedUnit")==enemy,"combat enemy inspection");
            float deadline=Time.realtimeSinceStartup+35;
            while(!battle.Finished && Time.realtimeSinceStartup<deadline) yield return null;
            Check(battle.Finished && rules.LastBattleXp==2,"combat awards two XP including level-up carry");
            Check(rules.Train.Center==trainCenter,"train stays still during combat");
            while(rules.Round==1 && Time.realtimeSinceStartup<deadline+6) yield return null;
            Check(rules.Round==2 && Field<bool>("shopOpen"),"next round reopens drawer");
            Call("BeginBattle"); rules.ResolveBattle(false,100);
            Check(rules.Health==0,"lethal damage ends game");
            Call("ResetRun"); rules=Field<ArenaRules>("rules");
            Check(rules.Health==100 && rules.Round==1 && rules.Pieces.Count==0,"restart resets state");
            var expanded=rules.Catalog.Copy();
            var extra=expanded.allies[0].Copy();extra.id="smoke_added";extra.name="Added unit";extra.evolvesTo="";extra.cost=1;
            expanded.allies=expanded.allies.Concat(new[]{extra}).ToArray();
            foreach(var row in expanded.enemies) row.startStage=2;
            var extraEnemy=expanded.enemies[0].Copy();extraEnemy.id="smoke_enemy";extraEnemy.name="Added enemy";extraEnemy.startStage=1;
            expanded.enemies=expanded.enemies.Concat(new[]{extraEnemy}).ToArray();
            var expandedRules=new ArenaRules(1,null,expanded);expandedRules.BeginRound();expandedRules.Offers[0]=expanded.allies.Length-1;
            Check(expandedRules.Buy(0)&&expandedRules.AutoDeploy(expandedRules.Pieces[0].Id),"added unit purchased and deployed");
            var portraits=world.CreatePortraits(expanded);var creepPortraits=world.CreatePortraits(expanded,true);
            Check(portraits.Length==expanded.allies.Length&&creepPortraits.Length==expanded.enemies.Length,"portraits expand with registered units");
            foreach(var texture in portraits.Concat(creepPortraits)) Destroy(texture);
            world.Rebuild(expandedRules,null);
            Check(world.Units.Any(u=>u.Root.name==extra.name)&&world.Units.Any(u=>u.Root.name==extraEnemy.name),"new ally and creep models instantiated");
            yield return Capture("03-expanded-catalog");
            expandedRules.StartBattle();var expandedBattle=new ArenaBattle(expandedRules);
            Check(expandedBattle.Fighters.Any(f=>f.Data.id==extra.id)&&expandedBattle.Fighters.Any(f=>f.Data.id==extraEnemy.id),"added data participates in combat");
            world.Rebuild(rules,null);
            var tsumemon=Resources.Load<GameObject>("Digimon/Tsumemon/Tsumemon");
            Check(tsumemon!=null,"Tsumemon prefab bundled");
            var model=Instantiate(tsumemon);model.transform.position=new Vector3(1000,0,0);
            var motion=model.GetComponent<ArenaUnitAnimation>();var clips=model.GetComponent<Animation>();
            Check(motion!=null&&clips.GetClipCount()==6,"Tsumemon six animation states");
            foreach(string clip in new[]{"Idle","Walk","Attack","Special","Hit","Death"})
            {
                clips.Stop();clips.Play(clip);clips[clip].time=clips[clip].length*.5f;clips.Sample();
                Check(clips.GetClip(clip).length>0,"animation clip samples "+clip);
            }
            clips.Stop();clips.Play("Walk");clips["Walk"].time=.3f;clips.Sample();
            Check(model.transform.Find("Rig").localPosition.y>.05f,"walk animation changes body pose");
            var animationFighter=new ArenaBattle.Fighter {Hp=100};
            motion.Pose(false,animationFighter,.01f);animationFighter.AttackCount++;motion.Pose(false,animationFighter,.01f);
            Check(clips.IsPlaying("Attack"),"battle attack triggers animation");
            animationFighter.UsedSkill=true;animationFighter.AttackCount++;motion.Pose(false,animationFighter,.01f);
            Check(clips.IsPlaying("Special"),"SP attack triggers special animation");
            animationFighter.HitCount++;motion.Pose(false,animationFighter,.01f);Check(clips.IsPlaying("Hit"),"damage triggers hit animation");
            animationFighter.Hp=0;motion.Pose(false,animationFighter,.2f);Check(!motion.DeathComplete&&clips.IsPlaying("Death"),"death remains visible during animation");
            motion.Pose(false,animationFighter,.7f);Check(motion.DeathComplete,"death animation completes before removal");
            Destroy(model);
            Debug.Log("ARENA 3D SMOKE PASSED: "+assertions+" checks");
            Application.Quit(failed?1:0);
        }
        void OnLog(string message,string stack,LogType type)
        {
            if(type==LogType.Exception || type==LogType.Error) { failed=true; Application.Quit(1); }
        }
        void OnDestroy() { Application.logMessageReceived-=OnLog; }
    }
}
#endif
