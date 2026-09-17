var root=new UnityEngine.GameObject("HUD Validation");
var font=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},18);
var owner=root.AddComponent<J2.MultiplayerMap.MultiplayerSceneController>();
var hud=new J2.MultiplayerMap.MultiplayerGameUi(root.transform,font,owner);
void Check(bool ok,string msg){if(!ok)throw new System.Exception(msg);}
try {
var state=new J2.MultiplayerMap.MultiplayerUiState{gold=50,level=3,canReroll=true,canBuyXp=true};
for(int i=0;i<5;i++)state.offers[i]=new J2.MultiplayerMap.ShopOfferView{offerId=100+i,name="서버 기물 "+i,price=i+1,canBuy=true};
hud.ApplyServerState(state);hud.ShowShop();hud.Draw(1,UnityEngine.Vector2.zero,1);
int purchases=0,lastSlot=-1,lastId=-1,rerolls=0,xp=0;
hud.PurchaseRequested+=(slot,id)=>{purchases++;lastSlot=slot;lastId=id;};hud.RerollRequested+=()=>rerolls++;hud.BuyXpRequested+=()=>xp++;
var doc=root.GetComponentInChildren<UnityEngine.UIElements.UIDocument>();
for(int i=0;i<5;i++){
var b=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement,"offer"+i);Check(b!=null,"Missing offer button");
using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
Check(lastSlot==i&&lastId==100+i,"Wrong purchase callback");}
hud.Reroll();hud.BuyXp();Check(purchases==5&&rerolls==1&&xp==1,"Callbacks not preserved");Check(hud.Gold==50&&hud.OfferCount==5,"Client mutated server economy");
state.offers[0].offerId=999;hud.Buy(0);Check(lastId==100,"State is not isolated from caller mutation");
state.offers[0].canBuy=false;hud.ApplyServerState(state);int before=purchases;hud.Buy(0);Check(purchases==before,"Unavailable offer accepted");
bool rejected=false;try{hud.ApplyShopOffers(new J2.MultiplayerMap.ShopOfferView[4]);}catch(System.ArgumentException){rejected=true;}Check(rejected,"Invalid shop length accepted");
System.IO.File.WriteAllText("Logs/MultiplayerHudValidation.txt","PASS: five actual shop buttons; exact slot/offer IDs; reroll/xp callbacks; no client economy mutation; cloned server state; unavailable offer gate; exactly five slots.\n");
return "PASS: multiplayer HUD shows server offers and only emits requests.";
} finally {hud.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(font);}
