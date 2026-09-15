using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using DigitalArena;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the Blender-authored asset; does not regenerate geometry or animations.</summary>
public static class WarGreymonAssetBuilder
{

    public const string			Folder = "Assets/Resources/Digimon/5코스트/WarGreymon";
    const string				Model = Folder + "/WarGreymon_Model.fbx";
    static readonly string[]	Clips =
    {
        "Idle",
        "Walk",
        "Attack",
        "Death"
    };
    static readonly int[]		Starts =
    {
        1,
        60,
        90,
        120
    }, Ends =
    {
        49,
        84,
        108,
        150
    };

    [MenuItem("Digital Arena/Import WarGreymon From Blender")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play mode before importing WarGreymon.");
        }

        AssetDatabase.ImportAsset(Model, ImportAssetOptions.ForceSynchronousImport);

        var importer = (ModelImporter)AssetImporter.GetAtPath(Model);

        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = true;
        importer.globalScale = 1;

        foreach (var remap in importer.GetExternalObjectMap())
        {
            importer.RemoveRemap(remap.Key);
        }

        importer.SaveAndReimport();

        string take = importer.defaultClipAnimations.First().takeName;

        importer.clipAnimations = Clips.Select((name, i) => new ModelImporterClipAnimation { name = name, takeName = take, firstFrame = Starts[i], lastFrame = Ends[i], loopTime = i < 2, loopPose = i < 2, wrapMode = i < 2 ? WrapMode.Loop : WrapMode.ClampForever, lockRootRotation = true, lockRootHeightY = true, lockRootPositionXZ = true }).ToArray();
        importer.SaveAndReimport();
        // Match the Blender palette in J2's own shader; no runtime dependency on Blender shaders.

        var shader = Shader.Find("DigitalArena/Solid");

        if (shader == null)
        {
            throw new InvalidOperationException("J2 Solid shader is missing.");
        }

        foreach (var source in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Material>())
        {
            string path = Folder + "/" + source.name.Split('.')[0] + ".mat";

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            mat.color = source.HasProperty("_Color") ? source.color : Color.white;
            EditorUtility.SetDirty(mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), mat);
        }

        importer.SaveAndReimport();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);

        var root = (GameObject)PrefabUtility.InstantiatePrefab(model);

        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "WarGreymon";

            var animation = root.GetComponent<Animation>() ?? root.AddComponent<Animation>();

            foreach (var state in animation.Cast<AnimationState>().ToArray())
            {
                animation.RemoveClip(state.name);
            }

            foreach (string name in Clips)
            {
                var source = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Single(c => c.name == name);

                string path = Folder + "/" + name + ".anim";

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip == null)
                {
                    clip = UnityEngine.Object.Instantiate(source);
                    AssetDatabase.CreateAsset(clip, path);
                }
                else
                {
                    EditorUtility.CopySerialized(source, clip);
                }

                clip.name = name;
                clip.legacy = true;
                clip.wrapMode = name == "Idle" || name == "Walk" ? WrapMode.Loop : WrapMode.ClampForever;
                EditorUtility.SetDirty(clip);
                animation.AddClip(clip, name);
            }

            animation.clip = animation.GetClip("Idle");
            animation.playAutomatically = true;
            animation.GetClip("Idle").SampleAnimation(root, 0);
            // Imported skinned-renderer bounds can cover the entire animation timeline.

            // Normalize from actual evaluated Idle vertices, not that conservative AABB.
            var rendererForSize = root.GetComponentInChildren<SkinnedMeshRenderer>();

            var vertices = WorldVertices(rendererForSize);

            float height = vertices.Max(v => v.y) - vertices.Min(v => v.y);

            root.transform.localScale *= 2.15f / height;
            root.AddComponent<ArenaUnitAnimation>();

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.updateWhenOffscreen = true;
            }

            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/WarGreymon.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        const string table = "Assets/Resources/DigimonCatalog.json";

        var catalog = JsonUtility.FromJson<DigimonCatalog>(File.ReadAllText(table));

        var entry = catalog.allies.Single(d => d.name == "워그레이몬");

        entry.prefabPath = "Digimon/5코스트/WarGreymon/WarGreymon";
        entry.placeholder = PlaceholderModel.Default;

        if (catalog.Validate()is string error)
        {
            throw new InvalidOperationException(error);
        }

        File.WriteAllText(table, JsonUtility.ToJson(catalog, true));
        AssetDatabase.ImportAsset(table);
        AssetDatabase.SaveAssets();
        Validate();
    }

    [MenuItem("Digital Arena/Validate WarGreymon Asset")]
    public static void Validate()
    {
        var prefab = Resources.Load<GameObject>("Digimon/5코스트/WarGreymon/WarGreymon");

        if (prefab == null)
        {
            throw new Exception("WarGreymon prefab missing.");
        }

        var root = UnityEngine.Object.Instantiate(prefab);

        try
        {
            var anim = root.GetComponent<Animation>();

            if (root.GetComponent<ArenaUnitAnimation>() == null)
            {
                throw new Exception("Arena animation driver missing.");
            }

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterials.Any(m => m == null || m.shader.name != "DigitalArena/Solid"))
                {
                    throw new Exception("Invalid material.");
                }
            }

            var bones = root.GetComponentsInChildren<Transform>();

            foreach (string name in Clips)
            {
                var clip = anim.GetClip(name);

                if (clip == null || !clip.legacy || clip.length <= 0)
                {
                    throw new Exception("Invalid clip: " + name);
                }

                clip.SampleAnimation(root, 0);

                var start = bones.Select(b => b.localRotation).ToArray();

                var positions = bones.Select(b => b.localPosition).ToArray();

                clip.SampleAnimation(root, clip.length * .26f);

                bool changed = bones.Where((b, i) => Quaternion.Angle(start[i], b.localRotation) > .1f || Vector3.Distance(positions[i], b.localPosition) > .001f).Any();

                if (!changed)
                {
                    throw new Exception("Animation has no motion: " + name);
                }

                if (name == "Idle" || name == "Walk")
                {
                    clip.SampleAnimation(root, clip.length);

                    if (bones.Where((b, i) => Quaternion.Angle(start[i], b.localRotation) > .2f || Vector3.Distance(positions[i], b.localPosition) > .002f).Any())
                    {
                        throw new Exception("Loop discontinuity: " + name);
                    }
                }
            }

            var mesh = root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

            if (mesh.triangles.Length / 3 > 15000)
            {
                throw new Exception("Low-poly triangle budget exceeded.");
            }

            anim.GetClip("Idle").SampleAnimation(root, 0);

            var skinned = root.GetComponentInChildren<SkinnedMeshRenderer>();

            var world = WorldVertices(skinned);

            float height = world.Max(v => v.y) - world.Min(v => v.y);

            if (Mathf.Abs(height - 2.15f) > .02f)
            {
                throw new Exception("Incorrect model height: " + height);
            }

            Debug.Log("WARGREYMON VALIDATION PASSED: 4 animated clips, seamless loops, " + mesh.triangles.Length / 3 + " triangles.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static Vector3[] WorldVertices(SkinnedMeshRenderer renderer)
    {
        // Evaluate the skin matrices directly: BakeMesh has parent-scale differences
        // across Unity versions and is unsuitable for import-time normalization.
        var mesh = renderer.sharedMesh;

        var vertices = mesh.vertices;

        var weights = mesh.boneWeights;

        var matrices = renderer.bones.Select((bone, i) => bone.localToWorldMatrix * mesh.bindposes[i]).ToArray();

        return vertices.Select((v, i) =>
        {
            var w = weights[i];

            return matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
        }).ToArray();
    }
}
