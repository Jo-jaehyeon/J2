var root=new UnityEngine.GameObject("Multiplayer movement validation");
var world=root.AddComponent<J2.MultiplayerMap.MultiplayerMapPreview>();
void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
try{
world.Initialize(UnityEngine.Resources.Load<UnityEngine.GameObject>("Map/DragonEyeMultiplayer"),3);
var playerObject=UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("PlayableCharacter/ShinTaeyil/ShinTaeyil"),world.ViewedArena);
var player=playerObject.GetComponent<J2.Creatures.Player>();
player.Initialize(1,1);
var control=root.AddComponent<J2.Creatures.PlayerController>();
control.Initialize(world.ViewCamera);control.BindPlayer(player);
var wanted=world.ViewedArena.TransformPoint(new UnityEngine.Vector3(2,.25f,-4));
var point=world.ViewCamera.WorldToScreenPoint(wanted);
var pick=control.GetType().GetMethod("PointAt");
pick.Invoke(control,new object[]{new UnityEngine.Vector2(point.x,point.y),(System.Func<UnityEngine.Vector2,bool>)(_=>false)});
player.Tick(10);Check(UnityEngine.Vector3.Distance(player.transform.position,wanted)<.01f,"Ground pointer movement failed");
control.CancelMovement();var before=player.transform.position;
pick.Invoke(control,new object[]{new UnityEngine.Vector2(point.x+60,point.y),(System.Func<UnityEngine.Vector2,bool>)(_=>true)});player.Tick(10);
Check(UnityEngine.Vector3.Distance(before,player.transform.position)<.001f,"UI click moved character");
Check(world.SetViewedArena(6),"Player-list camera switch failed");
Check(player.transform.parent==world.Layout.Arenas[3],"Spectating changed player assignment");
return "PASS: ground picking and movement, UI input blocking, camera switching without reassigning player.";
}finally{world.Shutdown();UnityEngine.Object.DestroyImmediate(root);}
