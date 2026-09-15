var session = new J2.Networking.ServerSession();
void Check(bool ok,string message) { if(!ok) throw new System.Exception(message); }
session.SetSessionId(42);
Check(!session.TryConsumeMatchResult(out _,out _),"Entered before response");
System.Threading.Tasks.Task.Run(()=>J2.Networking.BasicPacketHandler.Handle_S_FindMatch(session,new J2.Protocol.S_FindMatch { GameId=88 })).GetAwaiter().GetResult();
Check(session.TryConsumeMatchResult(out var owner,out var gameId)&&owner==42&&gameId==88,"S_FindMatch did not publish response");
Check(!session.TryConsumeMatchResult(out _,out _),"Response consumed twice");
J2.Networking.BasicPacketHandler.Handle_S_FindMatch(session,new J2.Protocol.S_FindMatch { GameId=89 });
session.ClearMatchResult();
Check(!session.TryConsumeMatchResult(out _,out _),"Old result survived new request");
J2.Networking.BasicPacketHandler.Handle_S_FindMatch(session,new J2.Protocol.S_FindMatch { GameId=90 });
session.OnDisconnected(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,9900));
Check(!session.TryConsumeMatchResult(out _,out _),"Disconnected result survived");
var source=System.IO.File.ReadAllText("Assets/Scripts/DigitalArena/DigitalArenaGame.cs");
var select=source.Substring(source.IndexOf("void SelectMultiplayer()"));select=select.Substring(0,select.IndexOf("void ResetRun()"));
Check(!select.Contains("EnterMultiplayerPreview"),"Request send still starts scene transition");
System.IO.File.WriteAllText("Logs/MatchResponseValidation.txt","PASS: no result before S_FindMatch; worker-thread packet handler publishes session/game; consume once; clear stale response before request; disconnect clears pending response; SelectMultiplayer no longer transitions on send.\n");
return "PASS: response storage, worker-thread reception, one-time consumption, stale/disconnected response clearing, no transition on send.";
