// Insert inside validate_map.cs's try block after `train` and `report` exist.
// Uses instantiated anchor transforms, so rotated arenas 4/5 and corner offsets
// are verified from the prefab itself. Roof occupancy is projected onto X/Z:
// its height does not exempt it from intersecting a playable tile footprint.
var sweepArenaCells = new System.Collections.Generic.List<UnityEngine.Vector2[]>[layout.Arenas.Length];
var sweepCellNames = new System.Collections.Generic.List<string>[layout.Arenas.Length];
for (int ai = 0; ai < layout.Arenas.Length; ai++)
{
    var arenaTransform = layout.Arenas[ai];
    sweepArenaCells[ai] = new System.Collections.Generic.List<UnityEngine.Vector2[]>();
    sweepCellNames[ai] = new System.Collections.Generic.List<string>();
    foreach (string groupName in new[] { "AllyCells", "EnemyCells" })
    {
        var group = arenaTransform.Find(groupName);
        if (group == null) throw new System.Exception("Missing cell group " + ai + "/" + groupName);
        foreach (UnityEngine.Transform cell in group)
        {
            var quad = new UnityEngine.Vector2[4];
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner == 0 || corner == 3 ? -.81f : .81f;
                float z = corner < 2 ? -.81f : .81f;
                var local = arenaTransform.InverseTransformPoint(cell.TransformPoint(new UnityEngine.Vector3(x, 0, z)));
                quad[corner] = new UnityEngine.Vector2(local.x, local.z);
            }
            sweepArenaCells[ai].Add(quad);
            sweepCellNames[ai].Add(groupName + "/" + cell.name);
        }
    }
}

// Separating axis test for two ordered convex quadrilaterals. Coincident edges
// without positive overlap are allowed; tolerance is 0.00001 local units.
System.Func<UnityEngine.Vector2[], UnityEngine.Vector2[], bool> sweepOverlaps = (a, b) =>
{
    float ax0 = float.PositiveInfinity, az0 = float.PositiveInfinity;
    float ax1 = float.NegativeInfinity, az1 = float.NegativeInfinity;
    float bx0 = float.PositiveInfinity, bz0 = float.PositiveInfinity;
    float bx1 = float.NegativeInfinity, bz1 = float.NegativeInfinity;
    for (int i = 0; i < 4; i++)
    {
        ax0 = UnityEngine.Mathf.Min(ax0, a[i].x); ax1 = UnityEngine.Mathf.Max(ax1, a[i].x);
        az0 = UnityEngine.Mathf.Min(az0, a[i].y); az1 = UnityEngine.Mathf.Max(az1, a[i].y);
        bx0 = UnityEngine.Mathf.Min(bx0, b[i].x); bx1 = UnityEngine.Mathf.Max(bx1, b[i].x);
        bz0 = UnityEngine.Mathf.Min(bz0, b[i].y); bz1 = UnityEngine.Mathf.Max(bz1, b[i].y);
    }
    if (ax1 <= bx0 || bx1 <= ax0 || az1 <= bz0 || bz1 <= az0) return false;
    for (int polygon = 0; polygon < 2; polygon++)
    {
        var vertices = polygon == 0 ? a : b;
        for (int i = 0; i < 4; i++)
        {
            var edge = vertices[(i + 1) % 4] - vertices[i];
            var axis = new UnityEngine.Vector2(-edge.y, edge.x).normalized;
            if (axis.sqrMagnitude < .5f) throw new System.Exception("Degenerate clearance rectangle");
            float minA = float.PositiveInfinity, maxA = float.NegativeInfinity;
            float minB = float.PositiveInfinity, maxB = float.NegativeInfinity;
            for (int v = 0; v < 4; v++)
            {
                float pa = UnityEngine.Vector2.Dot(a[v], axis);
                float pb = UnityEngine.Vector2.Dot(b[v], axis);
                minA = UnityEngine.Mathf.Min(minA, pa); maxA = UnityEngine.Mathf.Max(maxA, pa);
                minB = UnityEngine.Mathf.Min(minB, pb); maxB = UnityEngine.Mathf.Max(maxB, pb);
            }
            if (maxA <= minB + .00001f || maxB <= minA + .00001f) return false;
        }
    }
    return true;
};

int sweepSamples = 0;
int sweepSegment = 0;
var sweepRoofWorld = new UnityEngine.Vector3[4];
var sweepRoofLocal = new UnityEngine.Vector2[4];
System.Action<float> sweepCheckPose = meters =>
{
    train.SetAuthoritativeDistance(meters);
    sweepSamples++;
    for (int corner = 0; corner < 4; corner++)
    {
        float along = corner == 0 || corner == 3 ? -2.575f : 2.575f;
        float across = corner < 2 ? -.745f : .745f;
        // The model's longitudinal axis is local X, matching MultiplayerMapTrain.
        sweepRoofWorld[corner] = train.Train.TransformPoint(new UnityEngine.Vector3(along, 0, across));
    }
    for (int ai = 0; ai < layout.Arenas.Length; ai++)
    {
        var arenaTransform = layout.Arenas[ai];
        for (int corner = 0; corner < 4; corner++)
        {
            var local = arenaTransform.InverseTransformPoint(sweepRoofWorld[corner]);
            sweepRoofLocal[corner] = new UnityEngine.Vector2(local.x, local.z);
        }
        for (int ci = 0; ci < sweepArenaCells[ai].Count; ci++)
        {
            if (sweepOverlaps(sweepRoofLocal, sweepArenaCells[ai][ci]))
                throw new System.Exception("Projected train/tile overlap: arena " + ai + ", " +
                    sweepCellNames[ai][ci] + ", route segment " + sweepSegment +
                    ", distance " + meters.ToString("F5") + " m");
        }
    }
};

if (train.Route == null || train.Route.Length < 3 || train.RouteLength <= 0)
    throw new System.Exception("Missing closed train route for clearance validation");
float sweepStart = 0;
for (sweepSegment = 0; sweepSegment < train.Route.Length; sweepSegment++)
{
    var from = train.Route[sweepSegment];
    var to = train.Route[(sweepSegment + 1) % train.Route.Length];
    float segmentLength = UnityEngine.Vector3.Distance(from, to);
    if (float.IsNaN(segmentLength) || float.IsInfinity(segmentLength) || segmentLength <= .00001f)
        throw new System.Exception("Degenerate train route segment " + sweepSegment);
    int steps = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(segmentLength / .1f));
    // Check both orientations around each vertex. SetAuthoritativeDistance at an
    // exact boundary can still select the incoming segment due to its <= test.
    sweepCheckPose(sweepStart);
    sweepCheckPose(sweepStart + UnityEngine.Mathf.Min(.0001f, segmentLength * .25f));
    for (int step = 1; step <= steps; step++)
        sweepCheckPose(sweepStart + segmentLength * step / steps);
    sweepStart += segmentLength;
}
sweepCheckPose(train.RouteLength - .0001f);
sweepCheckPose(0);
report.AppendLine("PASS: projected 5.15 x 1.49 train roof clears all 512 cell footprints; " +
    sweepSamples + " poses across every route segment at <= 0.1 m spacing, including both sides of route vertices.");
