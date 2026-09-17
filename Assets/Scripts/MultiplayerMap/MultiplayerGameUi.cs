using System;
using System.Collections.Generic;
using DigitalArena;
using UnityEngine;

namespace J2.MultiplayerMap
{
    // Presentation and user intent only. Values change exclusively through ApplyServerState.
    public sealed class MultiplayerGameUi : IDisposable
    {
        readonly ArenaGameUi ui;
        readonly MultiplayerSceneController owner;
        readonly Dictionary<string, Texture2D> pictures = new Dictionary<string, Texture2D>();
        MultiplayerUiState state = new MultiplayerUiState();
        bool shopOpen, economyOpen, help, detailsOpen = true;
        float shopProgress;
        public event Action<int,int> PurchaseRequested;
        public event Action RerollRequested, BuyXpRequested, RestartRequested;
        public event Action<int> PlayerViewRequested, UnitDetailsRequested, SellRequested, AutoDeployRequested;
        public event Action<int,Vector3> PlacementRequested;
        public int Gold => state.gold;
        public int OfferCount => state.offers.Length;
        public int? DragEntityId { get; set; }
        static readonly Color Panel=C(0x16292E),Edge=C(0x556A62),Ink=C(0xF4EEDB),Muted=C(0xA7B8AF),Mint=C(0x7BE5BC),GoldColor=C(0xF3CF80),Pink=C(0xEF91A1);
        static Color C(uint v)=>new Color32((byte)(v>>16),(byte)(v>>8),(byte)v,255);
        Rect ShopRect=>new Rect(310,Mathf.Lerp(910,660,Mathf.SmoothStep(0,1,shopProgress)),920,185);
        public MultiplayerGameUi(Transform parent,Font font,MultiplayerSceneController controller){ui=new ArenaGameUi(parent,font);owner=controller;}
        public void ApplyServerState(MultiplayerUiState value)
        {
            if(value==null || value.offers==null || value.offers.Length!=5) throw new ArgumentException("Server shop must contain exactly five slots (null for empty).");
            state=JsonUtility.FromJson<MultiplayerUiState>(JsonUtility.ToJson(value));
            state.players=state.players??Array.Empty<PlayerRowView>();state.units=state.units??Array.Empty<UnitLabelView>();
            state.costProbabilities=state.costProbabilities??Array.Empty<string>();
            detailsOpen=true;
        }
        public void ApplyShopOffers(ShopOfferView[] offers)
        {
            if(offers==null || offers.Length!=5)throw new ArgumentException("Exactly five shop slots required.");
            var next=JsonUtility.FromJson<MultiplayerUiState>(JsonUtility.ToJson(state));next.offers=offers;ApplyServerState(next);shopOpen=true;
        }
        public void Buy(int slot)
        {
            if(slot<0||slot>=5)return;var offer=state.offers[slot];
            if(offer!=null&&offer.canBuy&&!state.gameOver)PurchaseRequested?.Invoke(slot,offer.offerId);
        }
        public void Reroll(){if(state.canReroll&&!state.gameOver)RerollRequested?.Invoke();}
        public void BuyXp(){if(state.canBuyXp&&!state.gameOver)BuyXpRequested?.Invoke();}
        public void RequestDetails(int id){detailsOpen=true;UnitDetailsRequested?.Invoke(id);}
        public void RequestPlacement(int id,Vector3 world){PlacementRequested?.Invoke(id,world);}
        public void RequestSell(int id){SellRequested?.Invoke(id);}
        public void RequestAutoDeploy(int id){AutoDeployRequested?.Invoke(id);}
        public void ShowShop(){shopOpen=true;}
        public bool OverUi(Vector2 p)
        {
            return help||state.gameOver||state.showResult||p.y<110||new Rect(1210,155,230,590).Contains(p)
                ||new Rect(24,economyOpen?463:805,250,economyOpen?405:63).Contains(p)
                ||new Rect(1250,746,166,122).Contains(p)
                ||(detailsOpen&&state.details!=null&&new Rect(24,268,250,452).Contains(p))
                ||(shopProgress>0&&new Rect(ShopRect.x,ShopRect.y-32,ShopRect.width,ShopRect.height+32).Contains(p));
        }
        public void Draw(float scale,Vector2 offset,float dt)
        {
            shopProgress=Mathf.MoveTowards(shopProgress,shopOpen?1:0,Mathf.Max(0,dt)*5);
            ui.Begin(scale,offset);ui.Enabled=!(help||state.gameOver||state.showResult);
            Header();PlayerList();BottomControls();UnitDetails();UnitLabels(scale,offset);
            if(shopProgress>0)Shop();
            if(DragEntityId.HasValue){Box(new Rect(0,790,1440,110));Text(new Rect(24,810,1392,45),"여기에 놓아 판매 요청",24,GoldColor,TextAnchor.MiddleCenter);}
            ui.Enabled=true;
            ui.Button("returnMainMenu",new Rect(1216,24,200,48),"메인 화면으로",Panel,owner.ReturnToMenu,!DigitalArena.GameSceneFlow.IsLoading);
            if(help)Help();else if(state.gameOver)GameOver();else if(state.showResult)Result();
            ui.End();
        }
        void Header()
        {
            Box(new Rect(470,18,500,74));Text(new Rect(492,30,250,37),state.stage??"서버 데이터 대기",21,Ink);
            Text(new Rect(742,30,205,37),(state.phase??"")+"  "+Mathf.CeilToInt(state.remainingSeconds)+"초",20,Mint,TextAnchor.MiddleRight);
            Bar(new Rect(492,77,456,3),state.remainingSeconds,state.phaseDuration,Mint);
        }
        void PlayerList()
        {
            Text(new Rect(1216,166,200,25),"플레이어",14,Muted);
            for(int i=0;i<state.players.Length&&i<8;i++)
            {
                var row=state.players[i];if(row==null)continue;var r=new Rect(1216,198+i*64,200,59);Box(r);Picture(new Rect(r.x+6,r.y+5,44,44),row.portraitPath);
                Text(new Rect(r.x+56,r.y+3,140,25),row.name,15,row.isLocal?Mint:Ink);Text(new Rect(r.x+56,r.y+27,140,23),row.health+" / "+row.maxHealth,14,Ink);
                ui.Button("player"+row.playerId,r,"",Color.clear,()=>{owner.World.SetViewedArena(row.arenaIndex);PlayerViewRequested?.Invoke(row.playerId);},transparent:true);
            }
            Text(new Rect(1216,720,200,24),"배치 "+state.deployed+" / "+state.capacity,15,Muted);
        }
        void BottomControls()
        {
            Box(new Rect(24,805,250,63));Text(new Rect(38,811,210,23),state.gold+" G   ·   Lv. "+state.level,21,GoldColor);
            Text(new Rect(38,840,210,23),state.xp+" / "+state.nextLevelXp+" XP",14,Ink);
            ui.Button("economy",new Rect(24,805,250,63),"",Color.clear,()=>economyOpen=!economyOpen,transparent:true);
            if(economyOpen){Box(new Rect(24,463,250,330));Text(new Rect(38,480,222,170),state.economyDescription??"",15,Ink);ui.Button("buyXp",new Rect(38,720,222,50),state.xpPrice+" G · "+state.xpAmount+" XP 구매",Panel,BuyXp,state.canBuyXp);}
            ui.Button("toggleShop",new Rect(1250,805,166,63),shopOpen?"기물 선택 닫기":"기물 선택 열기",Panel,()=>shopOpen=!shopOpen);
            ui.Button("guide",new Rect(1250,746,166,45),"플레이 가이드",Panel,()=>help=true);
            Text(new Rect(330,862,900,24),state.notice??"",13,Ink,TextAnchor.MiddleCenter);
        }
        void Shop()
        {
            var p=ShopRect;Box(p);Box(new Rect(p.x,p.y-32,p.width,30));Text(new Rect(p.x+14,p.y-29,220,24),"코스트별 등장 확률",14,Ink);
            for(int i=0;i<5;i++)Text(new Rect(p.x+245+i*132,p.y-29,132,24),i<state.costProbabilities.Length?state.costProbabilities[i]:"",13,Rarity(i));
            ui.Button("reroll",new Rect(p.x+16,p.y+8,150,29),"리롤 · "+state.rerollPrice+"골드",C(0x304C43),Reroll,state.canReroll);
            ui.Button("closeShop",new Rect(p.xMax-61,p.y+8,43,27),"닫기",Panel,()=>shopOpen=false);
            for(int i=0;i<5;i++)
            {
                int slot=i;var offer=state.offers[i];var card=new Rect(p.x+16+i*179,p.y+46,171,114);Box(card);
                if(offer==null)continue;
                ui.Button("offer"+i,card,"",C(0x243C3F),()=>Buy(slot),offer.canBuy,tooltip:offer.description);
                Picture(new Rect(card.x+4,card.y+5,163,83),offer.portraitPath);
                Text(new Rect(card.x+8,card.y+87,124,22),offer.name,13,Ink);Text(new Rect(card.x+132,card.y+87,36,22),offer.price+"G",14,GoldColor);Frame(card,Rarity(offer.rarity),3);
            }
        }
        void UnitDetails()
        {
            var d=state.details;if(!detailsOpen||d==null)return;
            Box(new Rect(24,268,250,452));ui.Button("closeDetails",new Rect(223,302,48,36),"×",Panel,()=>detailsOpen=false);
            Text(new Rect(38,276,190,50),d.name+"  "+d.stars+"성",18,Ink);Picture(new Rect(38,339,84,84),d.portraitPath);
            Text(new Rect(130,350,137,70),d.type+" · "+d.element+"\n"+d.role,13,Ink);
            Bar(new Rect(38,435,222,22),d.health,d.maxHealth,Mint);Text(new Rect(38,435,222,22),"HP "+d.health+" / "+d.maxHealth,13,Ink,TextAnchor.MiddleCenter);
            Bar(new Rect(38,463,222,22),d.sp,d.maxSp,C(0x2D7CAC));Text(new Rect(38,463,222,22),"SP "+d.sp+" / "+d.maxSp,13,Ink,TextAnchor.MiddleCenter);
            Text(new Rect(38,503,222,90),"ATK "+d.attack+"  DEF "+d.defense+"\nINT "+d.intelligence+"  SPD "+d.speed+"\n사거리 "+d.range,15,Ink);
            Text(new Rect(38,592,222,64),d.description,13,Muted);
            ui.Button("deployUnit",new Rect(38,672,100,32),"자동 배치",Panel,()=>RequestAutoDeploy(d.entityId),d.canDeploy);
            ui.Button("sellUnit",new Rect(150,672,100,32),"판매",Panel,()=>RequestSell(d.entityId),d.canSell);
        }
        void UnitLabels(float scale,Vector2 offset)
        {
            foreach(var d in state.units)
            {
                if(d==null||!owner.Spawner.TryGetEntity(d.entityId,out var entity))continue;
                var p=owner.World.ViewCamera.WorldToScreenPoint(entity.transform.position+Vector3.up*2);if(p.z<=0)continue;
                var q=(new Vector2(p.x,Screen.height-p.y)-offset)/scale;
                Bar(new Rect(q.x-26,q.y-6,52,5),d.health,d.maxHealth,d.enemy?Pink:Mint);Bar(new Rect(q.x-26,q.y+1,52,3),d.sp,d.maxSp,C(0x2D7CAC));
                Text(new Rect(q.x-30,q.y-25,60,18),new string('★',Mathf.Clamp(d.stars,0,3)),12,GoldColor,TextAnchor.MiddleCenter);
            }
        }
        void Result(){Box(new Rect(495,346,450,150));Text(new Rect(515,359,410,48),state.resultTitle,30,Ink,TextAnchor.MiddleCenter);Text(new Rect(515,416,410, 60),state.resultDescription,16,Muted,TextAnchor.MiddleCenter);}
        void GameOver(){ui.Fill(new Rect(0,0,1440,900),new Color(0,0,0,.8f));Result();ui.Button("restart",new Rect(500,540,440,60),"다시 시작 요청",Panel,()=>RestartRequested?.Invoke(),state.canRestart);}
        void Help(){ui.Fill(new Rect(0,0,1440,900),new Color(0,0,0,.8f));Box(new Rect(300,160,840,580));Text(new Rect(340,185,760,48),"전장 가이드",29,Mint);Text(new Rect(340,260,760,280),"빈 바닥 우클릭 / 터치: 캐릭터 이동\n기물 우클릭: 정보 요청\n기물 드래그: 배치 / 하단으로 드래그: 판매 요청\n상점 카드 클릭: 구매 요청\n플레이어 목록 클릭: 해당 전장 보기",20,Ink);ui.Button("continue",new Rect(340,671,760,44),"닫기",Panel,()=>help=false);}
        static Color Rarity(int tier)=>tier==0?Muted:tier==1?Mint:tier==2?C(0x7ABDE1):tier==3?C(0xBAA0DA):GoldColor;
        void Picture(Rect r,string path){if(string.IsNullOrEmpty(path))return;if(!pictures.TryGetValue(path,out var texture)){texture=Resources.Load<Texture2D>(path);pictures[path]=texture;}if(texture!=null)ui.Picture(r,texture,ScaleMode.ScaleToFit);}
        void Text(Rect r,string text,int size,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)=>ui.Text(r,text??"",size,color,false,alignment);
        void Bar(Rect r,float value,float max,Color color){ui.Fill(r,Edge);ui.Fill(new Rect(r.x,r.y,r.width*(max>0?Mathf.Clamp01(value/max):0),r.height),color);}
        void Box(Rect r){ui.Fill(r,Panel);Frame(r,Edge,1);}
        void Frame(Rect r,Color c,float w){ui.Fill(new Rect(r.x,r.y,r.width,w),c);ui.Fill(new Rect(r.x,r.yMax-w,r.width,w),c);ui.Fill(new Rect(r.x,r.y,w,r.height),c);ui.Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        public void Dispose()=>ui.Dispose();
    }
}
