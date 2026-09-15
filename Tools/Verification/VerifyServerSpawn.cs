var table=UnityEngine.Resources.Load<J2.Spawning.SpawnTypeTable>("SpawnTypeTable");
var player=table.entries.Find(e=>e.key=="player_shintaeyil");
void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var root=new UnityEngine.GameObject("Spawn Verification");
try {
root.transform.position=new UnityEngine.Vector3(100,0,100);root.transform.rotation=UnityEngine.Quaternion.Euler(0,90,0);root.layer=29;
var spawner=root.AddComponent<J2.MultiplayerMap.MultiplayerEntitySpawner>();spawner.Initialize(root.transform);
var session=new J2.Networking.ServerSession();
var packet=new J2.Protocol.S_Spawn { SpawnTypeId=player.id,EntityId=101,X=2,Y=3 };
System.Threading.Tasks.Task.Run(()=>J2.Networking.InGamePacketHandler.Handle_S_Spawn(session,packet)).GetAwaiter().GetResult();
packet.X=999;
Check(session.TryDequeueSpawn(out var received)&&received.X==2,"Packet snapshot not preserved");
Check(spawner.Spawn(received),"Player spawn failed");
Check(spawner.TryGetEntity(101,out var entity),"EntityId lookup failed");
Check(UnityEngine.Vector3.Distance(entity.transform.position,root.transform.TransformPoint(new UnityEngine.Vector3(2,.25f,3)))<.001f,"Viewed arena local coordinates incorrect");
Check(entity.layer==29,"Object invisible to multiplayer camera");
Check(spawner.Spawn(received)&&spawner.Count==1,"Duplicate response duplicated object");
received.EntityId=102;received.X=20;
Check(spawner.Spawn(received)&&spawner.Count==2,"Same type needs distinct entity instances");
Check(spawner.TryGetEntity(102,out var second)&&second!=entity,"Entity lookup returned wrong instance");
var ally=table.entries.Find(e=>e.kind==J2.Spawning.SpawnKind.Ally);
if(ally!=null)Check(spawner.Spawn(new J2.Protocol.S_Spawn{SpawnTypeId=ally.id,EntityId=103,X=3,Y=4}),"Catalog model spawn failed");
session.EnqueueSpawn(received);session.OnDisconnected(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,1));Check(!session.TryDequeueSpawn(out _),"Disconnect kept old spawns");
System.IO.File.WriteAllText("Logs/ServerSpawnValidation.txt","PASS: socket-thread queue snapshot; player and catalog model creation; viewed arena local X/Z under rotated parent; multiplayer layer; EntityId lookup; duplicate suppression; same type with separate IDs; disconnect cleanup.\n");
return "PASS: server spawn queue, type mapping, viewed arena coordinates, entity registry, duplicates and disconnect cleanup.";
} finally { UnityEngine.Object.DestroyImmediate(root); }
