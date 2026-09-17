var session = new J2.Networking.ServerSession();
J2.Networking.BasicPacketHandler.Handle_S_EnterGame(session, new J2.Protocol.S_EnterGame { SessionId = 23, ObjectId = 901 });
if (session.SessionId != 23 || session.LocalEntityId != 901)
{
    throw new System.Exception("EnterGame did not preserve distinct ownership ID");
}
session.OnDisconnected(null);
if (session.LocalEntityId != 0)
{
    throw new System.Exception("Ownership survived disconnect");
}
var controller = UnityEngine.Object.FindAnyObjectByType<J2.MultiplayerMap.MultiplayerSceneController>();
var network = J2.Networking.NetworkManager.Instance;
if (controller == null || network == null || network.LocalEntityId <= 0)
{
    throw new System.Exception("Enter multiplayer through the connected main menu before running this test");
}
if (!controller.Spawner.TryGetEntity(network.LocalEntityId, out var local) || controller.PlayerController.Player != local.GetComponent<J2.Creatures.Player>())
{
    throw new System.Exception("Actual server spawn was not automatically bound to local input");
}
var world = controller.World;
var player = controller.PlayerController.Player;
var before = player.transform.position;
var target = player.transform.parent.TransformPoint(new UnityEngine.Vector3(2, .25f, -4));
var screen = world.ViewCamera.WorldToScreenPoint(target);
var pointAt = controller.PlayerController.GetType().GetMethod("PointAt");
try
{
    pointAt.Invoke(controller.PlayerController, new object[] { new UnityEngine.Vector2(screen.x, screen.y), (System.Func<UnityEngine.Vector2, bool>)(_ => false) });
    if (player.Destination != before) throw new System.Exception("Click changed destination without server response");
    player.SetDestination(target); // Simulate future server destination application.
    player.Tick(10);
    if (UnityEngine.Vector3.Distance(player.transform.position, target) > .01f)
    {
        throw new System.Exception("Owned server entity did not move after ground input");
    }
    foreach (var pair in controller.Spawner.Entities)
    {
        if (pair.Key != network.LocalEntityId && pair.Value.transform == player.transform)
        {
            throw new System.Exception("A remote entity acquired local control");
        }
    }
    return "PASS: server EnterGame -> LocalEntityId -> actual Spawn -> automatic control binding -> coordinate-only click and externally applied movement; disconnect clears ownership. EntityId=" + network.LocalEntityId;
}
finally
{
    player.transform.position = before;
    controller.PlayerController.CancelMovement();
}
