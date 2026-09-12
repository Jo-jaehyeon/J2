using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DigitalArena
{
    public sealed class ArenaWorld3D : MonoBehaviour
    {
        public sealed class UnitView
        {
            public Transform Root;
            public int Id, Tier;
            public bool Enemy;
            public Vector3 Home;
            public ArenaBattle.Fighter Fighter;
            public DigimonData Data;
            public ArenaUnitAnimation Motion;
            public float Health => Fighter == null ? 1 : Fighter.Hp / Fighter.MaxHp;
        }
        public readonly List<UnitView> Units = new List<UnitView>();
        public Camera Camera { get; private set; }
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Camera> previousCameras = new List<Camera>();
        Transform actors, highlight, tram;
        ArenaRules state;
        int trainRound;
        float trainProgress;
        Shader shader;
        bool quitting;
        const int WorldLayer = 30;
        public static Vector3 Position(float column, float row) => new Vector3((column - 3.5f) * 1.7f, 0.25f, (row - 4) * 1.7f);
        public static Vector3 CellPosition(int cell) => Position(cell % 8, 3 - cell / 8);
        public static Vector3 BenchPosition(int index) => new Vector3((index - 4.5f) * 1.35f, 0.3f, -9);
        public void Initialize()
        {
            foreach (var cam in FindObjectsByType<Camera>())
                if (cam.enabled) { previousCameras.Add(cam); cam.enabled = false; }
            shader = Resources.Load<Shader>("ArenaSolid");
            Camera = new GameObject("Arena 3D Camera").AddComponent<Camera>();
            Camera.transform.SetParent(transform);
            Camera.transform.position = new Vector3(0, 21, -24);
            Camera.transform.LookAt(new Vector3(0, 0, -0.6f));
            Camera.orthographic = false; Camera.fieldOfView = 44;
            Camera.nearClipPlane = 0.1f; Camera.farClipPlane = 120;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = C(0x9BD9E4);
            Camera.cullingMask = 1 << WorldLayer;
            actors = new GameObject("Pieces").transform; actors.SetParent(transform);
            BuildStage();
            highlight = Shape(transform, PrimitiveType.Cube, Vector3.zero, new Vector3(1.62f, 0.04f, 1.62f), C(0x61E7C0));
            highlight.name = "Drop cell highlight"; highlight.gameObject.SetActive(false);
        }
        void BuildStage()
        {
            var water=Shape(transform,PrimitiveType.Cube,new Vector3(0,-0.85f,0),new Vector3(85,0.3f,80),C(0x53B9CD));
            water.name="Dragon Eye Lake";
            Shape(transform,PrimitiveType.Cylinder,new Vector3(0,-0.6f,-1),new Vector3(24,0.6f,26),C(0xB9AC7C));
            Shape(transform,PrimitiveType.Cylinder,new Vector3(-1,-0.23f,-1),new Vector3(22,0.15f,24),C(0xDDD1A2));
            for (int row = 0; row < 9; row++) for (int col = 0; col < 8; col++)
            {
                if(row==4) continue;
                Color tile = row < 4 ? C((row + col) % 2 == 0 ? 0xA3B88Cu : 0xAEBD92u) : C((row + col) % 2 == 0 ? 0xB8BB94u : 0xC2C3A1u);
                Vector3 p = Position(col, row); p.y = 0;
                Shape(transform, PrimitiveType.Cube, p, new Vector3(1.62f,0.28f,1.62f), tile);
            }
            Shape(transform,PrimitiveType.Cube,new Vector3(0,0.02f,0),new Vector3(24,0.2f,1.5f),C(0x908C78));
            for(int i=-16;i<=16;i++) Shape(transform,PrimitiveType.Cube,new Vector3(i*0.7f,0.16f,0),new Vector3(0.18f,0.12f,1.32f),C(0x695E4B));
            for(int side=-1;side<=1;side+=2) Shape(transform,PrimitiveType.Cube,new Vector3(0,0.25f,side*0.48f),new Vector3(25,0.13f,0.08f),C(0xBAC9CA));
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = BenchPosition(i); p.y = 0.05f;
                var enemyBench=Shape(transform, PrimitiveType.Cube, new Vector3(p.x,p.y,9),new Vector3(1.28f,0.3f,1.28f),C(0x9B888B));
                enemyBench.name="Enemy bench "+i;
                Shape(transform, PrimitiveType.Cube, p, new Vector3(1.28f,0.3f,1.28f), C(i % 2 == 0 ? 0x608F87u : 0x6B9B8Fu));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for(int i=0;i<5;i++)
                {
                    Vector3 p=new Vector3(side*(9.4f+i%2),0,-7+i*3.5f);
                    Shape(transform,PrimitiveType.Sphere,p,new Vector3(1.3f,0.9f,1.2f),C(0x8B9F94));
                    if(i!=2) Tree(p+new Vector3(side*1.2f,0,0),2.4f+i*0.2f);
                }
                for(int i=0;i<4;i++)
                {
                    Vector3 p=new Vector3(side*(13+i*2),-0.6f,3+i*4);
                    Tower(p,4+i*0.5f,side*(i%2==0?7:-5));
                }
            }
            for(int i=0;i<11;i++)
            {
                Shape(transform,PrimitiveType.Sphere,new Vector3((i-5)*5,1,23+i%3*2),new Vector3(9,7+i%3*3,7),C(i%2==0?0x76AA95u:0x87B9A3u));
                Tree(new Vector3((i-5)*2.4f,0,11.5f),3+i%3*0.5f);
            }
            for(int i=0;i<26;i++)
            {
                float x=(i%2==0?-1:1)*(12+i%5*2.2f), z=-10+(i/2)*2.2f;
                Shape(transform,PrimitiveType.Cube,new Vector3(x,-0.65f,z),new Vector3(1.2f+i%3,0.015f,0.06f),C(0xB7EBE8));
            }
            tram=new GameObject("Moving green lake tram").transform; tram.SetParent(transform);
            BuildTram();
        }
        void Tree(Vector3 p,float height)
        {
            Shape(transform,PrimitiveType.Cylinder,p+Vector3.up*height/2,new Vector3(0.4f,height/2,0.4f),C(0x6A785A));
            Shape(transform,PrimitiveType.Sphere,p+Vector3.up*height,new Vector3(2.2f,1.8f,2),C(0x478D70));
            Shape(transform,PrimitiveType.Sphere,p+new Vector3(0.5f,height+0.6f,0),new Vector3(1.6f,1.5f,1.5f),C(0x64A886));
        }
        void Tower(Vector3 position,float height,float angle)
        {
            var root=new GameObject("Submerged leaning power tower").transform; root.SetParent(transform);root.position=position;root.rotation=Quaternion.Euler(0,0,angle);
            for(int side=-1;side<=1;side+=2)
            {
                Shape(root,PrimitiveType.Cube,new Vector3(side*0.34f,height/2,0),new Vector3(0.1f,height,0.1f),C(0x586F75));
                for(int i=0;i<4;i++) Shape(root,PrimitiveType.Cube,new Vector3(0,0.65f+i*height/4,0),new Vector3(0.07f,height/3,0.07f),C(0x71888B)).localRotation=Quaternion.Euler(0,0,side*37);
            }
            Shape(root,PrimitiveType.Cube,new Vector3(0,height-0.35f,0),new Vector3(1.8f,0.12f,0.12f),C(0x506970));
            Shape(root,PrimitiveType.Cube,new Vector3(0,height*0.65f,0),new Vector3(1.3f,0.1f,0.1f),C(0x506970));
        }
        void BuildTram()
        {
            Color green=C(0x3C8865),cream=C(0xE5E3B9),metal=C(0x627475),glass=C(0x78B3CB);
            Shape(tram,PrimitiveType.Cube,new Vector3(0,0.85f,0),new Vector3(4.9f,1.35f,1.32f),cream);
            Shape(tram,PrimitiveType.Cube,new Vector3(0,0.5f,0),new Vector3(5,0.35f,1.36f),green);
            Shape(tram,PrimitiveType.Cube,new Vector3(0,1.62f,0),new Vector3(5.05f,0.2f,1.45f),metal);
            Shape(tram,PrimitiveType.Cube,new Vector3(0,0.77f,0),new Vector3(5.02f,0.07f,1.38f),green);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<7;i++) Shape(tram,PrimitiveType.Cube,new Vector3(-1.95f+i*0.65f,1.2f,side*0.67f),new Vector3(0.48f,0.55f,0.025f),glass);
                for(int wheel=-1;wheel<=1;wheel+=2) Shape(tram,PrimitiveType.Cylinder,new Vector3(wheel*1.6f,0.18f,side*0.51f),new Vector3(0.48f,0.13f,0.48f),C(0x354148)).localRotation=Quaternion.Euler(90,0,0);
                Shape(tram,PrimitiveType.Cube,new Vector3(side*2.46f,1.15f,0),new Vector3(0.03f,0.68f,0.98f),glass);
                Shape(tram,PrimitiveType.Sphere,new Vector3(-2.51f,0.63f,side*0.43f),Vector3.one*0.16f,C(0xFFE9A0));
                Shape(tram,PrimitiveType.Cube,new Vector3(side*0.28f,1.98f,0),new Vector3(0.06f,0.65f,0.06f),metal).localRotation=Quaternion.Euler(0,0,side*45);
            }
            Shape(tram,PrimitiveType.Cube,new Vector3(0,2.22f,0),new Vector3(1,0.07f,0.35f),metal);
        }
        public Texture2D[] CreatePortraits(DigimonCatalog catalog,bool enemy=false)
        {
            var result=new Texture2D[(enemy?catalog.enemies:catalog.allies).Length]; var go=new GameObject("Portrait camera");var camera=go.AddComponent<Camera>();
            camera.enabled=false;camera.cullingMask=1<<WorldLayer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=C(0x243C3F);
            camera.orthographic=true;camera.orthographicSize=1.25f;camera.nearClipPlane=0.1f;camera.farClipPlane=20;
            camera.transform.position=new Vector3(1000,2.3f,4);camera.transform.LookAt(new Vector3(1000,0.95f,0));
            var target=new RenderTexture(192,192,24);camera.targetTexture=target;var previous=RenderTexture.active;
            for(int tier=0;tier<result.Length;tier++)
            {
                var root=new GameObject("Portrait model").transform;root.position=new Vector3(1000,0,0);BuildUnitModel(root,catalog.Get(tier,enemy),enemy);
                var renderers=root.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    camera.orthographicSize=Mathf.Max(.6f,Mathf.Max(bounds.extents.x,bounds.extents.y)*1.25f);
                    camera.transform.position=bounds.center+new Vector3(0,.5f,4);camera.transform.LookAt(bounds.center);
                }
                camera.Render();RenderTexture.active=target;
                var image=new Texture2D(192,192,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,192,192),0,0);image.Apply();result[tier]=image;
                root.gameObject.SetActive(false);Destroy(root.gameObject);
            }
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(go);return result;
        }
        public void Rebuild(ArenaRules rules, ArenaBattle battle)
        {
            if(state!=rules || trainRound!=rules.Round) { trainRound=rules.Round;trainProgress=0; }
            state=rules;
            foreach (var unit in Units) { unit.Root.gameObject.SetActive(false); Destroy(unit.Root.gameObject); }
            Units.Clear(); highlight.gameObject.SetActive(false);
            if (battle == null)
            {
                foreach (var piece in rules.Pieces)
                    Add(piece.Id, piece.Tier, false, piece.Cell < 0 ? BenchPosition(piece.BenchSlot) : CellPosition(piece.Cell), null);
                for (int i = 0; i < ArenaBattle.EnemyCount(rules.Stage, rules.StageRound); i++)
                    Add(-1, rules.Catalog.EnemyAt(rules.Stage,i), true, Position(i % 8, 5+i/8), null);
            }
            else
            {
                foreach (var fighter in battle.Fighters)
                    Add(-1, fighter.Tier, fighter.Enemy, Position(fighter.X,fighter.Y), fighter);
                foreach (var piece in rules.Pieces.Where(p => p.Cell < 0)) Add(piece.Id,piece.Tier,false,BenchPosition(piece.BenchSlot),null);
            }
        }
        void Add(int id, int tier, bool enemy, Vector3 position, ArenaBattle.Fighter fighter)
        {
            var root = new GameObject(state.Catalog.Get(tier,enemy).name).transform;
            root.SetParent(actors); root.position = position;
            root.localRotation = Quaternion.Euler(0, enemy ? 180 : 0, 0);
            BuildUnitModel(root,state.Catalog.Get(tier,enemy),enemy);
            Units.Add(new UnitView { Root=root,Id=id,Tier=tier,Enemy=enemy,Home=position,Fighter=fighter,Data=fighter==null?ArenaBattle.CreateData(state,tier,enemy):fighter.Data,Motion=root.GetComponentInChildren<ArenaUnitAnimation>() });
        }
        public void Animate(float time, int draggedId, Vector3? dragPoint)
        {
            if(state!=null)
            {
                trainProgress=state.Preparing?Mathf.Min(1,trainProgress+Time.deltaTime/1.2f):1;
                tram.gameObject.SetActive(state.Train.Visible || trainProgress<1 && state.Round+3==state.Train.ReturnRound);
                tram.position=Position(Mathf.Lerp(state.Train.Previous,state.Train.Center,Mathf.SmoothStep(0,1,trainProgress)),4);
            }
            foreach (var unit in Units)
            {
                bool moving=unit.Fighter!=null&&(Position(unit.Fighter.X,unit.Fighter.Y)-unit.Home).sqrMagnitude>.000001f;
                if(unit.Motion!=null) unit.Motion.Pose(moving,unit.Fighter,Time.deltaTime);
                if (unit.Fighter != null)
                {
                    unit.Root.gameObject.SetActive(unit.Fighter.Alive || unit.Motion!=null&&!unit.Motion.DeathComplete);
                    if (!unit.Fighter.Alive) continue;
                    var f = unit.Fighter;
                    Vector3 travel=Position(f.X,f.Y)-unit.Home;
                    unit.Home = Position(f.X,f.Y);
                    if(travel.sqrMagnitude>.000001f) unit.Root.rotation=Quaternion.LookRotation(travel);
                    else if (f.Target != null)
                    {
                        Vector3 direction = Position(f.Target.X,f.Target.Y)-unit.Home;
                        if (direction.sqrMagnitude > 0.001f) unit.Root.rotation = Quaternion.LookRotation(direction);
                    }
                }
                Vector3 pos = unit.Home;
                if(unit.Motion==null) pos.y += Mathf.Sin(time*3+unit.Home.x)*0.04f;
                if (unit.Motion==null && unit.Fighter != null && unit.Fighter.Flash > 0) pos += unit.Root.forward * (unit.Fighter.Flash*0.9f);
                if (unit.Id == draggedId && draggedId >= 0 && dragPoint.HasValue) pos = dragPoint.Value + Vector3.up*0.65f;
                unit.Root.position = pos;
            }
        }
        public Vector2 Project(Vector3 world)
        {
            Vector3 p = Camera.WorldToScreenPoint(world);
            return new Vector2(p.x, Screen.height-p.y);
        }
        public int Pick(Vector2 screenTopLeft) { var u=PickUnit(screenTopLeft); return u==null||u.Enemy?-1:u.Id; }
        public UnitView PickUnit(Vector2 screenTopLeft)
        {
            // Screen-space bounds deliberately include head and feet, even on small baby models.
            foreach (var unit in Units.Where(u => u.Root.gameObject.activeSelf).OrderBy(u => Vector3.Distance(Camera.transform.position,u.Root.position)))
            {
                Vector2 feet=Project(unit.Root.position), head=Project(unit.Root.position+Vector3.up*1.5f);
                float width=Mathf.Max(32, Mathf.Abs(head.y-feet.y)*0.9f);
                if (new Rect(feet.x-width/2,head.y-8,width,feet.y-head.y+16).Contains(screenTopLeft)) return unit;
            }
            return null;
        }
        public bool GroundPoint(Vector2 screenTopLeft, out Vector3 point)
        {
            Ray ray=Camera.ScreenPointToRay(new Vector3(screenTopLeft.x,Screen.height-screenTopLeft.y,0));
            var plane=new Plane(Vector3.up,new Vector3(0,0.25f,0));
            if (plane.Raycast(ray,out float distance)) { point=ray.GetPoint(distance); return true; }
            point=Vector3.zero; return false;
        }
        public static int DropCell(Vector3 point)
        {
            int col=Mathf.FloorToInt(point.x/1.7f+4), row=Mathf.FloorToInt(point.z/1.7f+4.5f);
            return col>=0 && col<8 && row>=0 && row<4 ? (3-row)*8+col : -2;
        }
        public static int BenchSlot(Vector3 point)
        {
            int slot=Mathf.FloorToInt(point.x/1.35f+5);
            return slot>=0 && slot<10 && Mathf.Abs(point.z+9)<=0.65f?slot:-1;
        }
        public void Highlight(int cell)
        {
            highlight.gameObject.SetActive(cell>=0);
            if(cell>=0) { Vector3 p=CellPosition(cell); p.y=0.18f; highlight.position=p; }
        }
        void BuildUnitModel(Transform root,DigimonData data,bool enemy)
        {
            if(!string.IsNullOrEmpty(data.prefabPath))
            {
                var prefab=Resources.Load<GameObject>(data.prefabPath);
                if(prefab!=null)
                {
                    var model=Instantiate(prefab,root,false);
                    foreach(var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=WorldLayer;
                    return;
                }
                Debug.LogWarning("Missing Digimon prefab: "+data.prefabPath);
            }
            if(data.placeholder!=PlaceholderModel.Default)
            {
                var shape=data.placeholder==PlaceholderModel.BlueCube?PrimitiveType.Cube:
                    data.placeholder==PlaceholderModel.GreenSphere?PrimitiveType.Sphere:PrimitiveType.Capsule;
                Color color=data.placeholder==PlaceholderModel.BlueCube?C(0x64B5F6):
                    data.placeholder==PlaceholderModel.GreenSphere?C(0x82C866):C(0xAD78D0);
                float size=0.65f+0.15f*Mathf.Clamp(data.modelTier,1,5);
                float height=shape==PrimitiveType.Capsule?size*1.4f:size;
                Shape(root,shape,new Vector3(0,height/2,0),new Vector3(size,shape==PrimitiveType.Capsule?height/2:height,size),color);
                return;
            }
            BuildModel(root,data.modelTier,enemy);
        }
        void BuildModel(Transform root,int tier,bool enemy)
        {
            Color body=enemy ? C(0x9876B9) : C(0xF3A134), pale=enemy ? C(0xD8C7E8) : C(0xFFE098), steel=C(0xA5C3CC), dark=C(0x27313B);
            Shape(root,PrimitiveType.Sphere,new Vector3(0,-0.06f,0),new Vector3(1.15f,0.04f,0.9f),C(0x283B3C));
            if(tier<2)
            {
                body=enemy ? body : C(0xF1A1B3);
                Shape(root,PrimitiveType.Sphere,new Vector3(0,0.48f,0),new Vector3(0.95f,0.8f,0.85f),body);
                for(int side=-1;side<=1;side+=2)
                    Shape(root,PrimitiveType.Capsule,new Vector3(side*0.3f,1.04f,-0.02f),new Vector3(0.13f,0.39f,0.15f),body).localRotation=Quaternion.Euler(0,0,side*-13);
                if(enemy) for(int i=0;i<4;i++) Shape(root,PrimitiveType.Capsule,new Vector3((i-1.5f)*0.25f,0.12f,0.2f),new Vector3(0.12f,0.22f,0.16f),body).localRotation=Quaternion.Euler(65,0,0);
                Eyes(root,0.58f,0.37f,0.2f,enemy ? C(0xDF638E):C(0x2F735B));
                return;
            }
            if(enemy && tier>=5) body=C(0x454455);
            if(enemy && tier==3)
            {
                Shape(root,PrimitiveType.Sphere,new Vector3(0,0.85f,0),new Vector3(1,1.7f,0.8f),body);
                for(int i=0;i<3;i++) Shape(root,PrimitiveType.Cube,new Vector3(0,0.45f+i*0.4f,0),new Vector3(1.08f,0.12f,0.86f),dark);
                Eyes(root,1,0.4f,0.22f,C(0xEB5879)); return;
            }
            Shape(root,PrimitiveType.Sphere,new Vector3(0,0.7f,0),new Vector3(0.82f,1.05f,0.7f),body);
            Shape(root,PrimitiveType.Sphere,new Vector3(0,0.65f,0.23f),new Vector3(0.58f,0.65f,0.4f),pale);
            Shape(root,PrimitiveType.Cube,new Vector3(0,1.28f,0.12f),new Vector3(0.87f,0.68f,0.7f),enemy?pale:body);
            if(!enemy) Shape(root,PrimitiveType.Cube,new Vector3(0,1.13f,0.51f),new Vector3(0.72f,0.3f,0.5f),body);
            Eyes(root,1.4f,0.48f,0.23f,enemy?C(0xE84979):C(0x377C65));
            for(int side=-1;side<=1;side+=2)
            {
                Shape(root,PrimitiveType.Capsule,new Vector3(side*0.53f,0.7f,0.07f),new Vector3(0.23f,enemy?0.48f:0.27f,0.25f),body).localRotation=Quaternion.Euler(25,0,side*18);
                Shape(root,PrimitiveType.Cube,new Vector3(side*0.29f,0.14f,0.2f),new Vector3(0.34f,0.25f,0.65f),body);
                for(int claw=0;claw<2;claw++) Shape(root,PrimitiveType.Cube,new Vector3(side*0.29f+(claw-0.5f)*0.14f,0.15f,0.53f),new Vector3(0.08f,0.1f,0.19f),pale);
            }
            Shape(root,PrimitiveType.Capsule,new Vector3(0,0.38f,-0.55f),new Vector3(0.22f,0.4f,0.24f),body).localRotation=Quaternion.Euler(65,0,0);
            if(tier>=3)
            {
                Shape(root,PrimitiveType.Cube,new Vector3(0,1.58f,0.08f),new Vector3(0.94f,0.22f,0.76f),enemy?dark:C(0x856348));
                for(int side=-1;side<=1;side+=2) Shape(root,PrimitiveType.Capsule,new Vector3(side*0.38f,1.85f,0.1f),new Vector3(0.13f,0.28f,0.13f),pale).localRotation=Quaternion.Euler(0,0,-side*18);
            }
            if(tier>=4)
            {
                Shape(root,PrimitiveType.Cube,new Vector3(-0.27f,1.32f,0.1f),new Vector3(0.52f,0.71f,0.77f),steel);
                Shape(root,PrimitiveType.Cube,new Vector3(0.62f,0.72f,0.2f),new Vector3(0.38f,0.48f,0.52f),steel);
                for(int side=-1;side<=1;side+=2) Shape(root,PrimitiveType.Cube,new Vector3(side*0.71f,1.17f,-0.38f),new Vector3(0.72f,0.92f,0.13f),tier>=5?pale:C(0x9C709A)).localRotation=Quaternion.Euler(0,side*25,side*-25);
            }
            if(tier>=5)
            {
                Shape(root,PrimitiveType.Cube,new Vector3(0,0.87f,0.37f),new Vector3(0.72f,0.35f,0.21f),enemy?C(0xD85883):C(0xE3C15A));
                for(int side=-1;side<=1;side+=2)
                {
                    Shape(root,PrimitiveType.Cube,new Vector3(side*0.64f,0.66f,0.18f),new Vector3(0.35f,0.45f,0.5f),steel);
                    for(int i=0;i<2;i++) Shape(root,PrimitiveType.Cube,new Vector3(side*0.64f+(i-0.5f)*0.18f,0.52f,0.54f),new Vector3(0.09f,0.1f,0.6f),pale);
                }
            }
            if(enemy && tier==6) root.localScale=Vector3.one*1.2f;
        }
        void Eyes(Transform root,float y,float z,float spacing,Color iris)
        {
            for(int i=0;i<(spacing==0?1:2);i++)
            {
                float x=spacing==0?0:(i==0?-spacing:spacing);
                Shape(root,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(0.22f,0.2f,0.09f),Color.white);
                Shape(root,PrimitiveType.Cube,new Vector3(x,y,z+0.05f),new Vector3(0.09f,0.13f,0.05f),iris);
            }
        }
        Transform Shape(Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
        {
            var go=GameObject.CreatePrimitive(type); go.layer=WorldLayer;
            go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>(); if(collider!=null) { collider.enabled=false; Destroy(collider); }
            if(!materials.TryGetValue(color,out var material)) { material=new Material(shader); material.color=color; materials.Add(color,material); }
            go.GetComponent<Renderer>().sharedMaterial=material;
            return go.transform;
        }
        static Color C(uint value)=>new Color32((byte)(value>>16),(byte)(value>>8),(byte)value,255);
        void OnApplicationQuit() { quitting=true; }
        void OnDestroy()
        {
            foreach(var material in materials.Values) Destroy(material);
            if(quitting && !Application.isEditor) return;
            foreach(var cam in previousCameras) if(cam!=null) cam.enabled=true;
        }
    }
}
