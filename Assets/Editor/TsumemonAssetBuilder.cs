using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DigitalArena;
using UnityEditor;
using UnityEngine;

public static class TsumemonAssetBuilder
{

    const string	Folder = "Assets/Resources/Digimon/Tsumemon";
    static int		meshId;

    [MenuItem("Digital Arena/Build Tsumemon Assets")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        meshId = 0;

        var bodyMat = Material("Skin", new Color(.52f, .58f, .75f));

        var clawMat = Material("Claws", new Color(.77f, .78f, .72f));

        var black = Material("EyeSocket", new Color(.045f, .045f, .06f));

        var red = Material("Iris", new Color(.57f, .17f, .12f));

        var white = Material("Glint", Color.white);

        var root = new GameObject("Tsumemon");

        var rig = Node(root.transform, "Rig", Vector3.zero);

        var body = Node(rig, "Body", new Vector3(0, .87f, 0));

        Ellipsoid(body, "BodyMesh", Vector3.zero, new Vector3(.63f, .66f, .54f), 12, 8, bodyMat);
        Ellipsoid(body, "EyeSocket", new Vector3(0, .08f, .465f), new Vector3(.355f, .375f, .12f), 16, 10, black);
        Ellipsoid(body, "Iris", new Vector3(0, .09f, .555f), new Vector3(.265f, .29f, .09f), 20, 12, red);
        Ellipsoid(body, "Pupil", new Vector3(0, .07f, .637f), new Vector3(.13f, .16f, .035f), 16, 10, black);
        Ellipsoid(body, "Highlight", new Vector3(-.09f, .24f, .637f), new Vector3(.075f, .074f, .032f), 12, 8, white);
        Ellipsoid(body, "HighlightSmall", new Vector3(.12f, .015f, .644f), new Vector3(.033f, .041f, .019f), 10, 6, white);

        for (int side = -1; side <= 1; side += 2)
        {
            var antenna = Node(body, side < 0 ? "AntennaL" : "AntennaR", new Vector3(side * .23f, .55f, -.02f));

            Tube(antenna, "Blade", new[] { Vector3.zero, new Vector3(side * .12f, .39f, 0), new Vector3(side * .36f, .75f, -.045f), new Vector3(side * .65f, 1.02f, -.09f) }, new[] { .036f, .075f, .085f, .002f }, bodyMat, .4f);
        }

        float[] angles =
        {
            0,
            -65,
            65,
            -130,
            130
        };

        for (int i = 0; i < 5; i++)
        {
            float a = angles[i] * Mathf.Deg2Rad;

            var axis = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));

            var leg = Node(rig, "Leg" + i, axis * .37f + Vector3.up * .5f);

            leg.localRotation = Quaternion.Euler(0, angles[i], 0);
            Tube(leg, "LegMesh", new[] { Vector3.zero, new Vector3(0, -.03f, .31f), new Vector3(0, -.19f, .52f), new Vector3(0, -.35f, .58f) }, new[] { .20f, .29f, .23f, .115f }, bodyMat, .85f);
            Tube(leg, "Claw", new[] { new Vector3(0, -.30f, .58f), new Vector3(0, -.43f, .64f), new Vector3(0, -.49f, .72f) }, new[] { .11f, .075f, .002f }, clawMat, .8f);
        }

        var animation = root.AddComponent<Animation>();

        animation.playAutomatically = true;

        foreach (string name in new[]
        {
            "Idle",
            "Walk",
            "Attack",
            "Special",
            "Hit",
            "Death"
        }

        )
        {
            var clip = new AnimationClip
            {
                name = name,
                legacy = true,
                frameRate = 30,
                wrapMode = name == "Idle" || name == "Walk" ? WrapMode.Loop : WrapMode.ClampForever
            };

            float length = name == "Death" ? .85f : name == "Idle" ? 1.6f : name == "Walk" ? .6f : .45f;

            Curve(clip, "Rig", "localPosition.y", length, 0, name == "Walk" ? .09f : name == "Death" ? -.3f : .025f, name == "Death" ? -.43f : 0);
            Curve(clip, "Rig/Body", "localEulerAnglesRaw.x", length, 0, name == "Attack" ? 24 : name == "Special" ? -22 : name == "Hit" ? -16 : 0, name == "Death" ? 65 : 0);
            Curve(clip, "Rig", "localScale.y", length, 1, name == "Special" ? 1.12f : name == "Hit" ? .88f : 1, name == "Death" ? .35f : 1);
            Curve(clip, "Rig", "localPosition.z", length, 0, name == "Attack" ? .25f : name == "Special" ? .15f : 0, 0);
            Curve(clip, "Rig/Body/AntennaL", "localEulerAnglesRaw.z", length, 0, name == "Special" ? -24 : 7, 0);
            Curve(clip, "Rig/Body/AntennaR", "localEulerAnglesRaw.z", length, 0, name == "Special" ? 24 : -7, 0);

            for (int i = 0; i < 5; i++)
            {
                float swing = name == "Walk" ? (i % 2 == 0 ? 22 : -22) : name == "Attack" && i < 3 ? -30 : name == "Special" ? -18 : 3;

                Curve(clip, "Rig/Leg" + i, "localEulerAnglesRaw.x", length, 0, swing, name == "Death" ? 35 : 0);
            }

            string clipPath = Folder + "/" + name + ".anim";

            Save(clip, clipPath);
            animation.AddClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath), name);
        }

        animation.clip = animation.GetClip("Idle");
        root.AddComponent<ArenaUnitAnimation>();
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Tsumemon.prefab");

        var catalogPath = "Assets/Resources/DigimonCatalog.json";

        var catalog = JsonUtility.FromJson<DigimonCatalog>(File.ReadAllText(catalogPath));

        var row = catalog.enemies.FirstOrDefault(d => d.id == "enemy_1" || d.name == "츠메몬");

        if (row == null)
        {
            throw new Exception("Tsumemon entry missing");
        }

        row.prefabPath = "Digimon/Tsumemon/Tsumemon";
        File.WriteAllText(catalogPath, JsonUtility.ToJson(catalog, true));
        AssetDatabase.ImportAsset(catalogPath);
        AssetDatabase.SaveAssets();
        Render(root);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("TSUMEMON ASSETS PASSED: " + meshId + " meshes, 6 animation clips");
    }

    static void Save(UnityEngine.Object obj, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(obj, path);
        }
        else
        {
            EditorUtility.CopySerialized(obj, existing);
            UnityEngine.Object.DestroyImmediate(obj);
            EditorUtility.SetDirty(existing);
        }
    }

    static Material Material(string name, Color color)
    {
        var mat = new Material(Shader.Find("DigitalArena/Solid"))
        {
            name = name,
            color = color
        };

        string path = Folder + "/" + name + ".mat";

        Save(mat, path);

        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    static Transform Node(Transform parent, string name, Vector3 position)
    {
        var t = new GameObject(name).transform;

        t.SetParent(parent, false);
        t.localPosition = position;

        return t;
    }

    static void MeshObject(Transform parent, string name, List<Vector3> vertices, List<int> indices, Material material)
    {
        var flat = new Vector3[indices.Count];

        for (int i = 0; i < flat.Length; i++)
        {
            flat[i] = vertices[indices[i]];
        }

        var mesh = new Mesh
        {
            name = name,
            vertices = flat,
            triangles = Enumerable.Range(0, flat.Length).ToArray()
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = Folder + "/Mesh" + (meshId++) + "_" + name + ".asset";

        Save(mesh, path);

        var t = Node(parent, name, Vector3.zero);

        t.gameObject.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    static void Ellipsoid(Transform parent, string name, Vector3 center, Vector3 size, int longitude, int latitude, Material material)
    {
        var vertices = new List<Vector3>();

        var triangles = new List<int>();

        for (int y = 0; y <= latitude; y++)
        {
            for (int x = 0; x <= longitude; x++)
            {
                float v = Mathf.PI * y / latitude, u = Mathf.PI * 2 * x / longitude;

                vertices.Add(center + Vector3.Scale(size, new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u))));
            }
        }

        for (int y = 0; y < latitude; y++)
        {
            for (int x = 0; x < longitude; x++)
            {
                int a = y * (longitude + 1) + x, b = a + longitude + 1;

                triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
        }

        MeshObject(parent, name, vertices, triangles, material);
    }

    static void Tube(Transform parent, string name, Vector3[] points, float[] radii, Material material, float thickness)
    {
        var vertices = new List<Vector3>();

        var triangles = new List<int>();

        const int sides = 8;

        for (int p = 0; p < points.Length; p++)
        {
            Vector3 direction = (points[Mathf.Min(points.Length - 1, p + 1)] - points[Mathf.Max(0, p - 1)]).normalized;

            Vector3 right = Vector3.Cross(direction, Vector3.forward).normalized;

            if (right.sqrMagnitude < .01f)
            {
                right = Vector3.right;
            }

            Vector3 up = Vector3.Cross(direction, right).normalized;

            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;

                vertices.Add(points[p] + radii[p] * (right * Mathf.Cos(angle) + up * Mathf.Sin(angle) * thickness));
            }
        }

        for (int p = 0; p < points.Length - 1; p++)
        {
            for (int i = 0; i < sides; i++)
            {
                int a = p * sides + i, b = p * sides + (i + 1) % sides;

                triangles.AddRange(new[] { a, b, a + sides, b, b + sides, a + sides });
            }
        }

        for (int i = 1; i < sides - 1; i++)
        {
            triangles.AddRange(new[] { 0, i + 1, i });

            int a = (points.Length - 1) * sides;

            triangles.AddRange(new[] { a, a + i, a + i + 1 });
        }

        MeshObject(parent, name, vertices, triangles, material);
    }

    static void Curve(AnimationClip clip, string path, string property, float duration, float start, float middle, float end)
    {
        clip.SetCurve(path, typeof(Transform), property, new AnimationCurve(new Keyframe(0, start), new Keyframe(duration * .5f, middle), new Keyframe(duration, end)));
    }

    static void Render(GameObject root)
    {
        Directory.CreateDirectory("Logs/Tsumemon");

        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            t.gameObject.layer = 29;
        }

        var cameraObject = new GameObject("Asset preview camera");

        var cam = cameraObject.AddComponent<Camera>();

        cam.enabled = false;
        cam.cullingMask = 1 << 29;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.13f, .18f, .23f);
        cam.orthographic = true;
        cam.orthographicSize = 1.55f;

        var target = new RenderTexture(768, 768, 24);

        cam.targetTexture = target;

        var prior = RenderTexture.active;

        foreach (var view in new[]
        {
            "Front",
            "Side",
            "Back",
            "ThreeQuarter"
        }

        )
        {
            cam.transform.position = view == "Front" ? new Vector3(0, 1.4f, 5) : view == "Side" ? new Vector3(5, 1.4f, 0) : view == "Back" ? new Vector3(0, 1.4f, -5) : new Vector3(3, 2.5f, 5);
            cam.transform.LookAt(new Vector3(0, 1.15f, 0));
            cam.Render();
            RenderTexture.active = target;

            var texture = new Texture2D(768, 768, TextureFormat.RGB24, false);

            texture.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Logs/Tsumemon/" + view + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        RenderTexture.active = prior;
        cam.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
