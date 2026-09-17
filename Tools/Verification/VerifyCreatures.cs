var root = new UnityEngine.GameObject("Creature validation");
void Check(bool condition, string message)
{
    if (!condition) throw new System.Exception(message);
}
try
{
    root.transform.SetPositionAndRotation(new UnityEngine.Vector3(100, 0, 100), UnityEngine.Quaternion.Euler(0, 90, 0));
    var spawner = root.AddComponent<J2.MultiplayerMap.MultiplayerEntitySpawner>();
    spawner.Initialize(root.transform);
    var table = UnityEngine.Resources.Load<J2.Spawning.SpawnTypeTable>("SpawnTypeTable");
    var playerType = table.entries.Find(e => e.kind == J2.Spawning.SpawnKind.Player);
    var unitType = table.entries.Find(e => e.kind == J2.Spawning.SpawnKind.Ally);
    Check(spawner.Spawn(new J2.Protocol.S_Spawn { EntityId = 701, SpawnTypeId = playerType.id }), "Player spawn");
    Check(spawner.Spawn(new J2.Protocol.S_Spawn { EntityId = 702, SpawnTypeId = unitType.id }), "Unit spawn");
    Check(spawner.TryGetCreature(701, out var player) && player is J2.Creatures.Player, "Player subclass");
    Check(spawner.TryGetCreature(702, out var creature) && creature is J2.Creatures.Entity, "Entity subclass");
    var unit = (J2.Creatures.Entity)creature;
    Check(player.EntityId == 701 && unit.EntityId == 702 && player.SpawnTypeId == playerType.id, "Object-owned IDs");
    var start = player.transform.position;
    var destination = start + UnityEngine.Vector3.right * 8;
    Check(spawner.SetDestination(701, destination), "Destination routing");
    Check(player.transform.position == start && player.Destination == destination, "Player teleported on destination update");
    player.Tick(.25f);
    Check(player.transform.position.x > start.x && player.transform.position.x < destination.x, "Player does not interpolate");
    player.Tick(10);
    Check(player.transform.position == destination && !player.IsMoving, "Player failed to arrive");
    unit.SetDestination(destination);
    Check(unit.transform.position == destination, "Preparation must teleport");
    spawner.SetBattleState(J2.Creatures.EntityBattleState.Combat);
    unit.SetDestination(destination + UnityEngine.Vector3.forward * 8);
    Check(unit.transform.position == destination, "Combat teleported");
    unit.Tick(.25f);
    Check(unit.transform.position.z > destination.z && unit.transform.position.z < unit.Destination.z, "Combat does not interpolate");
    unit.SetDestination(destination + UnityEngine.Vector3.left * 4);
    unit.Tick(10);
    Check(unit.transform.position == unit.Destination, "Retarget failed");
    unit.SetDestination(destination);
    spawner.SetBattleState(J2.Creatures.EntityBattleState.Preparation);
    Check(unit.transform.position == destination, "Preparation transition must finish placement");
    spawner.SetBattleState(J2.Creatures.EntityBattleState.Combat);
    Check(spawner.Spawn(new J2.Protocol.S_Spawn { EntityId = 703, SpawnTypeId = unitType.id }), "Late unit spawn");
    spawner.TryGetCreature(703, out var late);
    Check(((J2.Creatures.Entity)late).BattleState == J2.Creatures.EntityBattleState.Combat, "Late spawn lost battle state");
    var storedDestination = player.Destination;
    Check(spawner.Spawn(new J2.Protocol.S_Spawn { EntityId = 701, SpawnTypeId = playerType.id }), "Duplicate spawn");
    Check(player.Destination == storedDestination, "Duplicate reset destination");
    player.PlayDeathAnimation();
    player.SetDestination(start);
    player.Tick(10);
    Check(player.transform.position == destination && player.IsDead, "Dead player moved");
    foreach (var name in new[] { "PlayMoveAnimation", "PlayAttackAnimation", "PlayDeathAnimation" })
    {
        Check(typeof(J2.Creatures.Creature).GetMethod(name).IsVirtual, "Animation cannot be overridden: " + name);
    }
    Check(!spawner.SetDestination(-999, start), "Unknown entity accepted");
    return "PASS: real spawn subclasses and IDs; player smooth movement; unit preparation teleport/combat interpolation; retarget/state transition/late spawn; duplicate preservation; death; virtual animation API.";
}
finally
{
    UnityEngine.Object.DestroyImmediate(root);
}
