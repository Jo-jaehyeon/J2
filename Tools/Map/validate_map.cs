var path="Assets/Resources/Map/DragonEyeMultiplayer.prefab";
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,preview);
var report=new System.Text.StringBuilder();
try {
    var layout=root.GetComponent<J2.MultiplayerMap.MultiplayerMapLayout>();
    if(layout.Arenas.Length!=8)throw new System.Exception("Arena count");
    var occupied=new System.Collections.Generic.HashSet<UnityEngine.Vector3>();
    for(int i=0;i<8;i++) {
        var a=layout.Arenas[i];if(a.localPosition==UnityEngine.Vector3.zero||!occupied.Add(a.localPosition))throw new System.Exception("Layout");
        if(a.Find("AllyCells").childCount!=32||a.Find("EnemyCells").childCount!=32||a.Find("AllyBench").childCount!=10||a.Find("EnemyBench").childCount!=10)throw new System.Exception("Anchors");
        if(!layout.TryGetView(i,out var anchor,out var target))throw new System.Exception("Camera anchor");
        var bench=System.Linq.Enumerable.First(a.GetComponentsInChildren<UnityEngine.Renderer>(),r=>r.name.Contains("MP_Bench"));
        if(UnityEngine.Mathf.Abs(bench.bounds.center.y-.08f)>.01f || UnityEngine.Vector3.Distance(bench.bounds.center,a.TransformPoint(new UnityEngine.Vector3(0,.08f,-9)))>.01f)throw new System.Exception("FBX axis mismatch "+bench.bounds);
    }
    if(root.GetComponentsInChildren<J2.MultiplayerMap.MultiplayerMapTrain>().Length!=1 || root.transform.Find("SharedTrain")==null)throw new System.Exception("Train count");
    var train=layout.SharedTrain;float length=train.RouteLength;
    for(int i=0;i<8;i++) {
        float expected=(i==3||i==4)?90:0;
        if(UnityEngine.Quaternion.Angle(layout.Arenas[i].localRotation,UnityEngine.Quaternion.Euler(0,expected,0))>.01f)throw new System.Exception("Wrong arena orientation "+i);
        var a=layout.Arenas[i];
        // Inspect the central straight corridor end-to-end, not just the nearest route point.
        for(float x=-6;x<=6;x+=.5f) {
            var wanted=a.TransformPoint(new UnityEngine.Vector3(x,0,0));float best=float.MaxValue;
            for(int j=0;j<train.Route.Length;j++) {
                var p0=train.transform.TransformPoint(train.Route[j]);var p1=train.transform.TransformPoint(train.Route[(j+1)%train.Route.Length]);p0.y=0;p1.y=0;
                var delta=p1-p0;float t=UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(wanted-p0,delta)/delta.sqrMagnitude);best=UnityEngine.Mathf.Min(best,UnityEngine.Vector3.Distance(wanted,p0+delta*t));
            }
            // Corner slots turn through the center rather than continuing out the far edge.
            bool corner=(i==0||i==2||i==5||i==7);
            bool outgoingOutside=corner && ((a.position.x>0&&x>0)||(a.position.x<0&&x<0));
            if(!outgoingOutside && best>.3f)throw new System.Exception("Rail misses arena corridor "+i+" x="+x+" gap="+best);
        }
    }

    train.SetAuthoritativeDistance(0);var first=train.Train.position;
    for(int i=1;i<=1000;i++) {
        train.SetAuthoritativeDistance(length*i/1000);var p=train.Train.position;
        if(float.IsNaN(p.x)||UnityEngine.Mathf.Abs(p.y-.325f)>.001f||UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(p.x),UnityEngine.Mathf.Abs(p.z))>40.01f)throw new System.Exception("Train off route");
    }
    if(UnityEngine.Vector3.Distance(first,train.Train.position)>.001f)throw new System.Exception("Loop seam");
    train.SetAuthoritativeDistance(-1);var neg=train.Train.position;train.SetAuthoritativeDistance(length-1);
    if(UnityEngine.Vector3.Distance(neg,train.Train.position)>.001f)throw new System.Exception("Negative distance wrap");
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

train.SetAuthoritativeDistance(length*.6f);
    foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>())t.gameObject.layer=29;
    var lightGo=new UnityEngine.GameObject("Preview sun");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,preview);var light=lightGo.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<29;light.transform.rotation=UnityEngine.Quaternion.Euler(50,-35,0);
    var cameraGo=new UnityEngine.GameObject("Preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,preview);var cam=cameraGo.AddComponent<UnityEngine.Camera>();cam.scene=preview;cam.enabled=false;cam.cullingMask=1<<29;cam.clearFlags=UnityEngine.CameraClearFlags.SolidColor;cam.backgroundColor=new UnityEngine.Color(.60f,.80f,.84f);cam.orthographic=true;cam.orthographicSize=122;cam.farClipPlane=500;
    cam.transform.position=new UnityEngine.Vector3(65,205,-180);cam.transform.LookAt(new UnityEngine.Vector3(0,8,16));
    var rt=new UnityEngine.RenderTexture(1400,1400,24);cam.targetTexture=rt;var prev=UnityEngine.RenderTexture.active;
    try {
        cam.Render();UnityEngine.RenderTexture.active=rt;var tex=new UnityEngine.Texture2D(1400,1400,UnityEngine.TextureFormat.RGB24,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1400,1400),0,0);tex.Apply();System.IO.File.WriteAllBytes("Assets/Resources/Map/Source~/UnityOverview.png",UnityEngine.ImageConversion.EncodeToPNG(tex));UnityEngine.Object.DestroyImmediate(tex);
        cam.orthographic=false;cam.fieldOfView=60;cam.transform.position=new UnityEngine.Vector3(28,40,-117);cam.transform.LookAt(new UnityEngine.Vector3(0,13,27));
        cam.Render();UnityEngine.RenderTexture.active=rt;tex=new UnityEngine.Texture2D(1400,1400,UnityEngine.TextureFormat.RGB24,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1400,1400),0,0);tex.Apply();System.IO.File.WriteAllBytes("Assets/Resources/Map/Source~/UnityLakeScenery.png",UnityEngine.ImageConversion.EncodeToPNG(tex));UnityEngine.Object.DestroyImmediate(tex);
    } finally { cam.targetTexture=null;UnityEngine.RenderTexture.active=prev;rt.Release();UnityEngine.Object.DestroyImmediate(rt); }
    report.AppendLine("PASS: eight unique outer 3x3 arena slots; center empty.");report.AppendLine("PASS: each arena has 32+32 cells, 10+10 benches and valid camera anchors.");report.AppendLine("PASS: instantiated FBX axes and scale agree with gameplay anchor coordinates.");report.AppendLine("PASS: one shared train; 1000 route samples; closed loop; negative-distance wrapping.");report.AppendLine("PASS: square route crosses all eight arenas; side arenas 4 and 5 rotate 90 degrees.");report.AppendLine("Route length: "+length);report.AppendLine("Unity preview rendered.");
    System.IO.File.WriteAllText("Assets/Resources/Map/Source~/unity-validation.txt",report.ToString());return report.ToString();
} finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }


