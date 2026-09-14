using System;
using System.Linq;
using UnityEngine;

namespace DigitalArena
{
    public sealed partial class DigitalArenaGame : MonoBehaviour
    {
        [SerializeField, Min(1)] float preparationSeconds = 30;
        [SerializeField, Min(1)] int xpPerPurchase = 4;
        ArenaRules rules;
        ArenaBattle battle;
        ArenaWorld3D world;
        PlayableCharacterMotor playableCharacter;
        bool viewingOpponent;
        Texture2D[] partners, enemyPortraits;
        readonly System.Collections.Generic.Dictionary<int,Texture2D> badges=new System.Collections.Generic.Dictionary<int,Texture2D>();
        ArenaWorld3D.UnitView selectedUnit;
        Font font;
        Texture2D mainBackground, rankStar;
        bool mainMenu=true, multiplayerNotice;
        Camera menuCamera;
        float remaining, resultTime, elapsed, shopProgress, uiScale;
        Vector2 uiOffset, mouseDown;
        int pressedId = -1, dragId = -1;
        Vector3? dragPoint;
        bool help, shopOpen, economyOpen, saleHover;
        static readonly Rect SaleRect = new Rect(0,790,1440,110);
        GUIStyle textStyle, buttonStyle;
        static readonly Color Panel = Hex(0x16292E), Edge = Hex(0x556A62), Ink = Hex(0xF4EEDB);
        static readonly Color Muted = Hex(0xA7B8AF), Mint = Hex(0x7BE5BC), Gold = Hex(0xF3CF80), Pink = Hex(0xEF91A1);
        static Color Hex(uint v) => new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        Rect ShopRect => new Rect(310, Mathf.Lerp(910, 660, Mathf.SmoothStep(0,1,shopProgress)), 920, 185);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<DigitalArenaGame>() == null)
                new GameObject("Digital Arena • 3D Single Player").AddComponent<DigitalArenaGame>();
        }
        void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 18);
            Application.targetFrameRate = 60;
            mainBackground=Resources.Load<Texture2D>("UI/DigiTacticsMainBackground");
            rankStar=Resources.Load<Texture2D>("UI/RankStar");
            menuCamera=new GameObject("Main Menu Camera").AddComponent<Camera>();
            menuCamera.transform.SetParent(transform);menuCamera.cullingMask=0;
            menuCamera.clearFlags=CameraClearFlags.SolidColor;menuCamera.backgroundColor=Hex(0x112A3A);
        }
        void StartSinglePlayer() { if(mainMenu) ResetRun(); }
        void SelectMultiplayer() { multiplayerNotice=true; } // TODO: multiplayer lobby and network session.
        void ResetRun()
        {
            mainMenu=false;multiplayerNotice=false;
            if(world==null) { world=gameObject.AddComponent<ArenaWorld3D>();world.Initialize(); }
            if(playableCharacter==null)
            {
                var prefab=Resources.Load<GameObject>("PlayableCharacter/ShinTaeyil/ShinTaeyil");
                if(prefab!=null) playableCharacter=Instantiate(prefab,world.transform).GetComponent<PlayableCharacterMotor>();
                else Debug.LogError("ShinTaeyil playable character prefab is missing.");
            }
            if(playableCharacter!=null) playableCharacter.ResetPosition(new Vector3(0,.25f,-7.9f));
            ViewPlayer(false);
            var data=Resources.Load<TextAsset>("ArenaBalance");
            var catalog=Resources.Load<TextAsset>("DigimonCatalog");
            rules = new ArenaRules(Environment.TickCount,data==null?new ArenaBalance():JsonUtility.FromJson<ArenaBalance>(data.text),catalog==null?new DigimonCatalog():JsonUtility.FromJson<DigimonCatalog>(catalog.text));
            if(partners!=null) foreach(var t in partners) Destroy(t);
            if(enemyPortraits!=null) foreach(var t in enemyPortraits) Destroy(t);
            partners=world.CreatePortraits(rules.Catalog);enemyPortraits=world.CreatePortraits(rules.Catalog,true);
            selectedUnit=null;
            battle = null; resultTime = 0; help = false; economyOpen = false; shopProgress = 0;
            CancelDrag(); NextRound();
        }
        void NextRound()
        {
            rules.BeginRound(); remaining = preparationSeconds; battle = null;
            CancelDrag(); shopOpen = true; world.Rebuild(rules, null);
        }
        void BeginBattle()
        {
            if (!rules.Preparing) return;
            CancelDrag(); rules.StartBattle(); shopOpen = false;
            battle = new ArenaBattle(rules); world.Rebuild(rules, battle);
        }
        void Update()
        {
            UpdateTouchInput();
            if(playableCharacter!=null) playableCharacter.Tick(Time.deltaTime,!mainMenu && !help && rules!=null && rules.Health>0);
            if (mainMenu || help || rules.Health <= 0) return;
            elapsed += Time.deltaTime;
            shopProgress = Mathf.MoveTowards(shopProgress, shopOpen ? 1 : 0, Time.deltaTime * 5);
            if (rules.Preparing)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0) BeginBattle();
            }
            else if (battle != null && !battle.Finished)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.1f);
                while (dt > 0 && !battle.Finished) { float step = Mathf.Min(dt, 0.025f); battle.Tick(step); dt -= step; }
                if (battle.Finished) { rules.ResolveBattle(battle.Won, battle.Survivors); resultTime = 4; }
            }
            else if (battle != null) { resultTime -= Time.deltaTime; if (resultTime <= 0) NextRound(); }
            world.Animate(elapsed, dragId, dragPoint);
        }
        void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
        void CancelDrag()
        {
            pressedId = dragId = -1; dragPoint = null; saleHover=false;
            touchCancelled = true; pendingUiTap = false;
            if (world != null) world.Highlight(-1);
        }
        void OnDestroy()
        {
            if (partners != null) foreach (var t in partners) Destroy(t);
            if(enemyPortraits!=null) foreach(var t in enemyPortraits) Destroy(t);
            foreach(var t in badges.Values) Destroy(t);
            if (font != null) Destroy(font);
        }
        void Styles()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle { font = font, richText = false, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            buttonStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
        }
        Vector2 UiPoint(Vector2 screen) => (screen-uiOffset)/uiScale;
        Vector2 ScreenPoint(Vector2 ui) => ui*uiScale+uiOffset;
        void OnGUI()
        {
            Styles();
            UpdateUiMetrics();
            if (SuppressTouchMouse && Event.current.isMouse) Event.current.Use();
            GUI.matrix = Matrix4x4.TRS(uiOffset, Quaternion.identity, new Vector3(uiScale,uiScale,1));
            if(mainMenu) { MainMenu(); FinishTouchGui(); GUI.matrix=Matrix4x4.identity;return; }
            bool modal = help || rules.Health <= 0;
            GUI.enabled = !modal;
            UnitLabels(); Header(); PlayerList(); BottomControls(); UnitDetails();
            if (shopProgress > 0) Shop();
            if(saleHover && dragId>=0) SaleOverlay();
            if (battle != null && battle.Finished && !battle.Won && rules.Health > 0) Result();
            if (!modal) HandlePlacement(Event.current);
            GUI.enabled = true;
            if (help) Help(); else if (rules.Health <= 0) GameOver();
            FinishTouchGui();
            GUI.matrix = Matrix4x4.identity;
        }
        void MainMenu()
        {
            if(mainBackground!=null) GUI.DrawTexture(new Rect(0,0,1440,900),mainBackground,ScaleMode.ScaleAndCrop);
            Label(200,165,1040,110,"DigiTactics",82,new Color(0,0,0,.55f),true,TextAnchor.MiddleCenter);
            Label(200,161,1040,110,"DigiTactics",82,Ink,true,TextAnchor.MiddleCenter);
            Label(390,277,660,34,"디지털 월드에서 시작되는 나만의 전술",20,Ink,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(402,686,310,62),"싱글 플레이",Hex(0x285B55))) StartSinglePlayer();
            if(Button(new Rect(728,686,310,62),"멀티 플레이  ·  준비 중",Hex(0x24394F))) SelectMultiplayer();
            if(multiplayerNotice) Label(350,762,740,48,"멀티 플레이는 추후 업데이트 예정입니다. (TODO)",18,Ink,false,TextAnchor.MiddleCenter);
            Label(390,831,660,24,"DIGITAL WORLD · SINGLE PLAYER PROTOTYPE",12,Ink,false,TextAnchor.MiddleCenter);
        }
        void Header()
        {
            PanelBox(new Rect(470,18,500,74));
            string phase = rules.Preparing ? "준비" : battle.Finished ? "다음 라운드" : "전투";
            float time = rules.Preparing ? remaining : battle.Finished ? resultTime : ArenaBattle.Duration-battle.Time;
            Label(492,30,250,37,"스테이지 " + rules.Stage + "  ·  " + rules.StageRound + " / 8",21,Ink,true);
            Label(751,30,195,37,phase + "   " + Mathf.CeilToInt(Mathf.Max(0,time)).ToString("00") + "초",23,Mint,true,TextAnchor.MiddleRight);
            float ratio = rules.Preparing ? remaining/preparationSeconds : battle.Finished ? resultTime/4 : (ArenaBattle.Duration-battle.Time)/ArenaBattle.Duration;
            Fill(new Rect(492,77,456,3),Edge); Fill(new Rect(492,77,456*Mathf.Clamp01(ratio),3),Mint);
        }
        void PlayerList()
        {
            Label(1216,166,200,25,"플레이어",14,Muted,true);
            PanelBox(new Rect(1216,198,200,99));
            GUI.DrawTexture(new Rect(1224,213,53,53),partners[Mathf.Min(2,partners.Length-1)]);
            Label(1284,211,118,25,"나 · 테이머",15,Ink,true);
            Label(1284,242,118,28,rules.Health + " / 100",22,Pink,true);
            Fill(new Rect(1230,280,172,4),Edge); Fill(new Rect(1230,280,172*rules.Health/100f,4),Mint);
            if(TouchButton(new Rect(1216,198,200,99)) || GUI.Button(new Rect(1216,198,200,99),GUIContent.none,GUIStyle.none)) ViewPlayer(false);
            Label(1216,308,205,27,"배치 " + rules.Pieces.Count(p=>p.Cell>=0) + " / " + rules.Level,16,Ink);
            Label(1216,337,200,22,"적: " + rules.Catalog.enemies[rules.Catalog.EnemyAt(rules.Stage)].name,13,Muted);
            if(Button(new Rect(1216,369,200,38),viewingOpponent?"상대 시점 · 선택됨":"현재 상대 시점",Panel)) ViewPlayer(true);
            if(selectedUnit!=null) return;
            Label(24,198,230,27,"용의 눈 호수",21,Ink,true);
            Label(24,236,240,45,rules.Train.Visible?"기차가 가로막는 길을 우회하세요.":"기차 재등장: "+(rules.Train.ReturnRound-rules.Round)+"라운드 후",12,Gold);
        }
        void ViewPlayer(bool opponent)
        {
            viewingOpponent=opponent; selectedUnit=null; CancelDrag();
            if(world==null) return;
            world.Camera.transform.position=new Vector3(0,21,opponent?24:-24);
            world.Camera.transform.LookAt(new Vector3(0,0,opponent?.6f:-.6f));
        }
        void BottomControls()
        {

            if (Button(new Rect(24,805,250,63),"골드 " + rules.Gold + "   ·   Lv. " + rules.Level,Hex(0x304C43))) { economyOpen=!economyOpen; selectedUnit=null; }
            if (economyOpen)
            {
                PanelBox(new Rect(24,463,250,264));
                Label(40,478,220,28,"레벨 " + rules.Level + "  ·  배치 한도 " + rules.Level,17,Ink,true);
                Label(40,515,220,22,rules.Level==ArenaRules.MaxLevel?"MAX LEVEL":rules.Xp+" / "+rules.RequiredXp+" XP",14,Mint);
                Fill(new Rect(40,545,218,5),Edge); Fill(new Rect(40,545,218*(rules.Level==ArenaRules.MaxLevel?1:rules.Xp/(float)rules.RequiredXp),5),Mint);
                if(Button(new Rect(40,566,218,45),"XP 구매 +"+xpPerPurchase+"   /   4 pt",Hex(0x304C43),rules.Preparing&&rules.Gold>=4&&rules.Level<ArenaRules.MaxLevel)) rules.BuyXp(xpPerPurchase);
                Label(40,621,220,24,"라운드 수입 +"+rules.LastIncome+" pt",13,Gold);
                Label(40,650,220,24,"다음 이자 예상 +"+rules.Interest+" pt",13,Muted);
                Label(40,678,220,24,"연승 "+rules.WinStreak+" · 승리 +1 / 3연승 +1G",13,Mint);
            }
            if(Button(new Rect(1250,805,166,63),shopOpen?"기물 선택 닫기":"기물 선택 열기",Panel)) { CancelDrag(); shopOpen=!shopOpen; }
            if(Button(new Rect(1250,746,166,45),"플레이 가이드",Panel)) { CancelDrag(); help=true; }
            Label(330,862,900,24,rules.Notice,13,Ink,false,TextAnchor.MiddleCenter);
            if(shopProgress<0.05f && rules.Preparing)
                Label(330,827,900,22,TouchControls ? "기물 탭: 정보   ·   드래그: 배치 / 교환 / 하단 판매   ·   빈 바닥 탭: 테이머 이동" : "빈 전장 우클릭: 테이머 이동   ·   기물 더블 클릭: 자동배치   ·   좌클릭 드래그: 배치 / 교환",13,Muted,false,TextAnchor.MiddleCenter);
        }
        void UnitLabels()
        {
            foreach(var unit in world.Units)
            {
                if(!unit.Root.gameObject.activeSelf || unit.Id==dragId && dragId>=0) continue;
                Vector2 p=UiPoint(world.Project(unit.Root.position+Vector3.up*(unit.Data.modelTier<2?1.15f:2.15f)));
                Fill(new Rect(p.x-26,p.y-6,52,5),Panel);
                Fill(new Rect(p.x-26,p.y-6,52*Mathf.Clamp01(unit.Health),5),unit.Enemy?Pink:Mint);
                if(unit.Fighter!=null) { Fill(new Rect(p.x-26,p.y,52,3),Panel); Fill(new Rect(p.x-26,p.y,52*unit.Fighter.Sp/unit.Fighter.Data.SP,3),Hex(0x66BFF0)); }
                Label(p.x-70,p.y-28,140,19,unit.Data.name,11,Ink,false,TextAnchor.MiddleCenter);
                if(unit.Fighter!=null && unit.Fighter.Flash>0 && unit.Fighter.Target!=null)
                {
                    Vector2 target=UiPoint(world.Project(ArenaWorld3D.Position(unit.Fighter.Target.X,unit.Fighter.Target.Y)+Vector3.up));
                    for(int i=1;i<=8;i++) { Vector2 pos=Vector2.Lerp(p,target,i/8f); Fill(new Rect(pos.x,pos.y,4,4),unit.Enemy?Pink:Gold); }
                    Label(target.x-40,target.y-30,80,23,"-"+Mathf.RoundToInt(unit.Fighter.LastDamage),16,Gold,true,TextAnchor.MiddleCenter);
                }
            }
        }
        Texture2D Badge(DigimonData d)
        {
            int key=(int)d.type*9+(int)d.element;
            if(!badges.TryGetValue(key,out var texture)) { texture=DigimonBadge.Create(d.type,d.element);badges.Add(key,texture); }
            return texture;
        }
        void UnitDetails()
        {
            if(selectedUnit==null) return;
            if(selectedUnit.Root==null||!world.Units.Contains(selectedUnit)) { selectedUnit=null;return; }
            var u=selectedUnit;var d=u.Data;
            PanelBox(new Rect(24,268,250,452));
            if(!u.Enemy)
            {
                int stars=u.Fighter!=null?u.Fighter.Stars:rules.Pieces.Find(p=>p.Id==u.Id)?.Stars??1;
                for(int i=0;i<stars;i++)
                    if(rankStar!=null) GUI.DrawTexture(new Rect(38+i*28,276,24,24),rankStar,ScaleMode.ScaleToFit,true);
                    else Label(38+i*28,276,24,24,"★",22,Gold,true);
                Label(130,276,130,24,d.cost+"코스트 · "+stars+"성",13,Gold);
            }
            if(Button(new Rect(223,302,48,36),"×",Panel)) { selectedUnit=null;return; }
            GUI.DrawTexture(new Rect(38,339,84,84),(u.Enemy?enemyPortraits:partners)[u.Tier]);
            GUI.DrawTexture(new Rect(133,346,44,44),Badge(d));
            Label(38,306,185,28,d.name,18,Ink,true);
            Label(130,395,137,24,DigimonCatalog.TypeNames[(int)d.type]+" · "+DigimonCatalog.ElementNames[(int)d.element],13,Ink);
            float hp=u.Fighter==null?d.HP:u.Fighter.Hp,sp=u.Fighter==null?0:u.Fighter.Sp;
            Fill(new Rect(38,435,222,22),Edge);Fill(new Rect(38,435,222*Mathf.Clamp01(hp/d.HP),22),Hex(0x2A825C));
            Label(38,435,222,22,"HP  "+hp.ToString("0")+" / "+d.HP.ToString("0"),13,Ink,false,TextAnchor.MiddleCenter);
            Fill(new Rect(38,463,222,22),Edge);Fill(new Rect(38,463,222*sp/d.SP,22),Hex(0x2D7CAC));
            Label(38,463,222,22,"SP  "+sp.ToString("0")+" / "+d.SP.ToString("0"),13,Ink,false,TextAnchor.MiddleCenter);
            Label(38,495,224,26,(d.attack==AttackKind.Physical?"물리형":"특수형")+" / "+DigimonCatalog.RoleName(d.role),14,Gold,true);
            Label(38,527,220,25,"ATK  "+d.ATK.ToString("0.#")+"     DEF  "+d.DEF.ToString("0.#"),15,Ink);
            Label(38,559,220,25,"INT  "+d.INT.ToString("0.#")+"     SPD  "+d.SPD.ToString("0.##"),15,Ink);
            Label(38,591,220,25,"기본 공격 사거리  "+d.range.ToString("0.##")+"칸",14,Ink);
            Label(38,625,220,24,"공격당 SP +"+d.spPerAttack+"  ·  스킬 ×"+d.skillPower,12,Mint);
            Label(38,658,222,24,"방어 피해 감소  "+(100*d.DEF/(100+d.DEF)).ToString("0.#")+"%",12,Muted);
            Label(38,686,222,23,TouchControls?"기물 탭: 조회 / 닫기 · 드래그: 배치":u.Enemy?"적 기물":"우클릭 조회 / 닫기 · 좌클릭 드래그",12,Muted);
        }
        void Shop()
        {
            Rect panel=ShopRect; PanelBox(panel);
            PanelBox(new Rect(panel.x,panel.y-32,panel.width,30));
            Label(panel.x+14,panel.y-29,220,24,"코스트별 등장 확률",14,Ink,true);
            
            for(int i=0;i<5;i++) Label(panel.x+245+i*132,panel.y-29,132,24,ArenaRules.CostNames[i]+" "+rules.Balance.levels[rules.Level-1].weights[i]+"%",13,Rarity(i));
            if(Button(new Rect(panel.x+16,panel.y+8,150,29),"리롤 · "+ArenaRules.RerollCost+"골드",Hex(0x304C43),rules.CanReroll)) rules.Reroll();
            if(Button(new Rect(panel.xMax-61,panel.y+8,43,27),"닫기",Panel)) shopOpen=false;
            for(int i=0;i<5;i++)
            {
                int tier=rules.Offers[i]; Rect card=new Rect(panel.x+16+i*179,panel.y+46,171,114);
                if(tier<0||rules.Bought[i]) { PanelBox(card);continue; }
                if(Button(card,"",Hex(0x243C3F),rules.Preparing&&rules.Gold>=ArenaRules.PurchasePrice(rules.Catalog.allies[tier])))
                {
                    if(rules.Buy(i)) { CancelDrag(); world.Rebuild(rules,null); }
                }
                if(rules.Bought[i]) { PanelBox(card);continue; }
                GUI.DrawTexture(new Rect(card.x+4,card.y+5,163,83),partners[tier],ScaleMode.ScaleToFit);
                var d=rules.Catalog.Get(tier,false);
                GUI.DrawTexture(new Rect(card.xMax-39,card.y+7,32,32),Badge(d));
                GUI.Label(new Rect(card.xMax-41,card.y+5,36,36),new GUIContent("",DigimonCatalog.TypeNames[(int)d.type]+" · "+DigimonCatalog.ElementNames[(int)d.element]));
                Label(card.x+8,card.y+87,124,22,rules.Catalog.allies[tier].name,13,Ink,true);
                Label(card.x+132,card.y+87,36,22,ArenaRules.PurchasePrice(rules.Catalog.allies[tier])+"G",14,Gold,true);
                Frame(card,Rarity(d.cost-1),3);
            }
        }
        bool OverUi(Vector2 point)
        {
            return new Rect(470,18,500,74).Contains(point) || new Rect(1216,166,200,245).Contains(point)
                || new Rect(24,economyOpen?463:805,250,economyOpen?405:63).Contains(point)
                || selectedUnit!=null && new Rect(24,268,250,452).Contains(point)
                || new Rect(1250,746,166,122).Contains(point) || shopProgress>0 && new Rect(ShopRect.x,ShopRect.y-32,ShopRect.width,ShopRect.height+32).Contains(point);
        }
        void HandlePlacement(Event e)
        {
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape) { CancelDrag(); shopOpen=false; e.Use(); return; }
            if(SuppressTouchMouse) return;
            if(HandlePointer(e.rawType==EventType.MouseUp?EventType.MouseUp:e.type,e.button,e.clickCount,e.mousePosition)) e.Use();
        }
        bool HandlePointer(EventType type,int button,int clicks,Vector2 mouse)
        {
            if(mainMenu || help || rules.Health<=0) return false;
            Vector2 screen=ScreenPoint(mouse);
            if(type==EventType.MouseDown && button==1 && !OverUi(mouse))
            {
                CancelDrag();
                var picked=world.PickUnit(screen);
                economyOpen=false;
                if(picked!=null)
                {
                    selectedUnit=selectedUnit==picked?null:picked;
                    return true;
                }
                selectedUnit=null;
                return !viewingOpponent && playableCharacter!=null && world.GroundPoint(screen,out var destination) && playableCharacter.MoveTo(destination);
            }
            else if(type==EventType.MouseDown && button==0 && rules.Preparing && !OverUi(mouse) && !viewingOpponent)
            {
                int id=world.Pick(screen);if(id<0) return false;
                if(clicks==2)
                {
                    if(rules.AutoDeploy(id)) world.Rebuild(rules,null);
                    CancelDrag();return true;
                }
                pressedId=id;mouseDown=mouse;return true;
            }            else if(type==EventType.MouseDrag && pressedId>=0 && button==0 && rules.Preparing)
            {
                if(Vector2.Distance(mouseDown,mouse)>(touchFinger>=0?TouchDragThreshold:6)) dragId=pressedId;
                saleHover=dragId>=0 && SaleRect.Contains(mouse);
                if(dragId>=0 && world.GroundPoint(screen,out var point))
                {
                    dragPoint=point;
                    world.Highlight(OverUi(mouse)?-1:ArenaWorld3D.DropCell(point));
                }
                return true;
            }
            else if(type==EventType.MouseUp && pressedId>=0 && button==0 && rules.Preparing)
            {
                if(dragId>=0 && SaleRect.Contains(mouse))
                {
                    if(rules.Sell(dragId)) { selectedUnit=null; world.Rebuild(rules,null); }
                }
                else if(dragId>=0 && !OverUi(mouse) && world.GroundPoint(screen,out var point))
                {
                    int slot=ArenaWorld3D.BenchSlot(point),cell=ArenaWorld3D.DropCell(point);
                    bool moved=slot>=0?rules.MoveToBench(dragId,slot):cell>=0&&rules.Place(dragId,cell);
                    if(moved) world.Rebuild(rules,null);
                }
                CancelDrag(); return true;
            }
            return false;
        }
        void SaleOverlay()
        {
            var piece=rules.Pieces.Find(p=>p.Id==dragId); if(piece==null) return;
            var data=rules.Catalog.allies[piece.Tier];
            PanelBox(SaleRect);
            Label(24,810,1392,40,data.name+" "+piece.Stars+"성 판매 · +"+rules.SalePrice(piece)+" 골드",24,Gold,true,TextAnchor.MiddleCenter);
            Label(24,852,1392,25,TouchControls?"손가락을 놓으면 판매 · 영역 밖으로 이동하면 취소":"좌클릭을 놓으면 판매 · 영역 밖으로 이동하면 취소",15,Ink,false,TextAnchor.MiddleCenter);
        }
        void Result()
        {
            PanelBox(new Rect(495,346,450,137));
            Label(515,359,410,48,battle.Won?"전투 승리":"전투 패배",32,battle.Won?Mint:Pink,true,TextAnchor.MiddleCenter);
            Label(515,416,410,26,rules.LastBattleXp>0?"기본 경험치 +2 XP":"최대 레벨",17,Gold,true,TextAnchor.MiddleCenter);
            Label(515,449,410,22,Mathf.CeilToInt(resultTime)+"초 후 다음 라운드",13,Muted,false,TextAnchor.MiddleCenter);
        }
        void Help()
        {
            Fill(new Rect(0,0,1440,900),new Color(0,0,0,0.8f)); PanelBox(new Rect(300,160,840,580));
            Label(340,185,760,48,"3D 전장 가이드",29,Mint,true);
            string[] lines={
                "상단은 적 전장, 하단은 내 전장입니다. 양쪽에 대기석 10칸이 있습니다.",
                TouchControls?"아군·적 기물을 탭하면 정보를 확인합니다. 빈 바닥을 탭하면 테이머가 이동합니다.":"대기석 기물을 더블 클릭하면 해당 기물만 빈 전장 칸에 자동배치합니다.",
                TouchControls?"기물을 끌어서 배치·교환하세요. 하단 판매 영역에서 손을 놓으면 판매합니다.":"기물을 좌클릭 드래그해 8×4 전장에 배치·교환하거나 대기석 안에서 옮기세요.",
                "준비 30초 후 전투가 시작됩니다. 전투 제한 시간도 30초입니다.",
                "라운드 시작 시 기물 선택 창이 올라옵니다. 우측 하단에서 다시 열 수 있습니다.",
                "골드 범위에서 원하는 후보를 각각 구매하세요. 동일 기물·동일 별 3개로 3성까지 강화합니다.",
                "골드·레벨은 좌측 하단에서 확인하고 버튼을 눌러 4pt로 "+xpPerPurchase+"XP를 구매하세요.",
                "승패와 관계없이 전투 종료 시 플레이어 기본 경험치 2XP를 받습니다.",
                "시작 체력 100. 패배 피해는 남은 크립 수 × 스테이지 × 2입니다.",
                "체력 0이면 게임 종료. 가이드를 보고 있는 동안 일시 정지됩니다."
            };
            for(int i=0;i<lines.Length;i++) Label(340,247+i*37,760,28,lines[i],15,Ink);
            if(Button(new Rect(340,671,760,44),"계속 플레이",Hex(0x304C43))) help=false;
        }
        void GameOver()
        {
            Fill(new Rect(0,0,1440,900),new Color(0,0,0,0.85f)); PanelBox(new Rect(410,240,620,420));
            Label(450,275,540,85,"패배했습니다",52,Pink,true,TextAnchor.MiddleCenter);
            Label(450,369,540,30,"플레이어 체력이 모두 소진되었습니다.",18,Ink,false,TextAnchor.MiddleCenter);
            Label(450,413,540,32,"스테이지 "+rules.Stage+" · "+rules.StageRound+"라운드 도달",22,Ink,true,TextAnchor.MiddleCenter);
            Label(450,459,540,30,"최고 레벨 "+rules.Level+"  /  수집 기물 "+rules.Pieces.Count,16,Muted,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(450,548,260,62),"게임 재시작",Hex(0x304C43))) ResetRun();
            if(Button(new Rect(730,548,260,62),"게임 종료",Hex(0x52303F))) QuitGame();
        }
        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        static Color Rarity(int tier)=>tier==0?Muted:tier==1?Mint:tier==2?Hex(0x7ABDE1):tier==3?Hex(0xBAA0DA):Gold;
        void Label(float x,float y,float w,float h,string value,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            textStyle.fontSize=size; textStyle.normal.textColor=color; textStyle.fontStyle=bold?FontStyle.Bold:FontStyle.Normal; textStyle.alignment=alignment;
            GUI.Label(new Rect(x,y,w,h),value,textStyle);
        }
        bool Button(Rect rect,string value,Color color,bool enabled=true)
        {
            bool previous=GUI.enabled; GUI.enabled=previous&&enabled;
            bool hover=rect.Contains(Event.current.mousePosition)&&GUI.enabled;
            Fill(rect,hover?Color.Lerp(color,Mint,0.15f):color); Frame(rect,hover?Mint:Edge,1);
            buttonStyle.fontSize=14; buttonStyle.normal.textColor=enabled?Ink:Muted;
            bool clicked=TouchButton(rect) | GUI.Button(rect,value,buttonStyle); GUI.enabled=previous; return clicked;
        }
        static void Fill(Rect rect,Color color) { Color previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }
        static void Frame(Rect r,Color c,float w)
        { Fill(new Rect(r.x,r.y,r.width,w),c); Fill(new Rect(r.x,r.yMax-w,r.width,w),c); Fill(new Rect(r.x,r.y,w,r.height),c); Fill(new Rect(r.xMax-w,r.y,w,r.height),c); }
        static void PanelBox(Rect rect) { Fill(rect,Panel); Frame(rect,Edge,1); }
    }
}



