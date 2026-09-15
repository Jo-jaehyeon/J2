using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using DigitalArena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Imports and checks the playable character independently of the Digimon roster.</summary>
public static class PlayableCharacterAssetBuilder
{
    [Serializable]
    public class Palette
    {

        public string	name;
        public float[]	color;
        public bool		vertexColor;
    }

    [Serializable]
    public class ClipSpec
    {

        public string	name;
        public int		firstFrame, lastFrame;
        public bool		loop;
    }

    [Serializable]
    public class Manifest
    {

        public Palette[]	materials;
        public ClipSpec[]	clips;

        public string	name;
        public float	targetHeight = 1.8f, maxWidth = 2f, maxDepth = 2f;
        public int		triangleBudget;
    }

    public static string[] Manifests() => Directory.GetFiles("Assets/Resources/PlayableCharacter", "import.json", SearchOption.AllDirectories).Where(p => new DirectoryInfo(Path.GetDirectoryName(p)).Name == "Source~").OrderBy(p => p).ToArray();

    static string Folder(Manifest m) => "Assets/Resources/PlayableCharacter/" + m.name;

    static string Resource(Manifest m) => "PlayableCharacter/" + m.name + "/" + m.name;

    public static string BuildAvailable()
    {
        if (EditorApplication.isPlaying)
        {
            throw new Exception("Import requires Edit mode.");
        }

        var names = new List<string>();

        foreach (var path in Manifests())
        {
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));

            Build(m);
            names.Add(m.name);
        }

        return string.Join(", ", names);
    }

    public static string BuildNamed(string name)
    {
        if (EditorApplication.isPlaying)
        {
            throw new Exception("Import requires Edit mode.");
        }

        var manifest = Manifests().Select(p => JsonUtility.FromJson<Manifest>(File.ReadAllText(p))).Single(m => m.name == name);

        Build(manifest);

        return name + ": imported and verified";
    }

    static void Build(Manifest m)
    {
        string folder = Folder(m), modelPath = folder + "/" + m.name + "_Model.fbx";

        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);

        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);

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

        importer.clipAnimations = m.clips.Select(c => new ModelImporterClipAnimation { name = c.name, takeName = take, firstFrame = c.firstFrame, lastFrame = c.lastFrame, loopTime = c.loop, loopPose = c.loop, wrapMode = c.loop ? WrapMode.Loop : WrapMode.ClampForever, lockRootRotation = true, lockRootHeightY = true, lockRootPositionXZ = true }).ToArray();

        var shader = Shader.Find("DigitalArena/Solid");

        if (shader == null)
        {
            throw new Exception("Missing J2 shader");
        }

        foreach (var source in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
        {
            var palette = m.materials.Single(p => p.name == source.name);

            string matPath = folder + "/" + source.name.Split('.')[0] + ".mat";

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.shader = shader;
            mat.color = new Color(palette.color[0], palette.color[1], palette.color[2], palette.color[3]);
            mat.SetFloat("_UseVertexColor", palette.vertexColor ? 1f : 0f);
            EditorUtility.SetDirty(mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), mat);
        }

        importer.SaveAndReimport();

        var scene = EditorSceneManager.NewPreviewScene();

        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), scene);

        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var imported = root;

            imported.name = "Model";

            if (imported.GetComponent<Animation>() != null)
            {
                UnityEngine.Object.DestroyImmediate(imported.GetComponent<Animation>());
            }

            root = new GameObject(m.name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

            var offset = new GameObject("VisualOffset");

            offset.transform.SetParent(root.transform, false);
            imported.transform.SetParent(offset.transform, false);

            var anim = root.AddComponent<Animation>();

            foreach (var state in anim.Cast<AnimationState>().ToArray())
            {
                anim.RemoveClip(state.name);
            }

            foreach (var spec in m.clips)
            {
                var source = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Single(c => c.name == spec.name);

                string clipPath = folder + "/" + spec.name + ".anim";

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

                if (clip == null)
                {
                    clip = UnityEngine.Object.Instantiate(source);
                    AssetDatabase.CreateAsset(clip, clipPath);
                }
                else
                {
                    EditorUtility.CopySerialized(source, clip);
                }

                clip.name = spec.name;
                clip.legacy = true;
                clip.wrapMode = spec.loop ? WrapMode.Loop : WrapMode.ClampForever;

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);

                    AnimationUtility.SetEditorCurve(clip, binding, null);

                    var prefixed = binding;

                    prefixed.path = "VisualOffset/Model" + (binding.path.Length == 0 ? "" : "/" + binding.path);
                    AnimationUtility.SetEditorCurve(clip, prefixed, curve);
                }

                EditorUtility.SetDirty(clip);
                anim.AddClip(clip, spec.name);
            }

            anim.clip = anim.GetClip("Idle");
            anim.playAutomatically = true;
            anim.cullingType = AnimationCullingType.AlwaysAnimate;
            anim.clip.SampleAnimation(root, 0);

            var bounds = Bounds(root);

            float scale = Mathf.Min(m.targetHeight / bounds.size.y, m.maxWidth / bounds.size.x, m.maxDepth / bounds.size.z);

            root.transform.localScale *= scale;
            // Offset the skeleton, keeping the prefab anchor at ground level for placement.
            bounds = Bounds(root);

            foreach (Transform child in root.transform)
            {
                child.position -= new Vector3(bounds.center.x, bounds.min.y, 0);
            }

            if (root.GetComponent<PlayableCharacterMotor>() == null)
            {
                root.AddComponent<PlayableCharacterMotor>();
            }

            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.updateWhenOffscreen = true;
            }

            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = 30;
            }

            PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + m.name + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }

        Validate(m);
        AssetDatabase.SaveAssets();
    }

    public static string ValidateAll()
    {
        return string.Join("\n", Manifests().Select(p => Validate(JsonUtility.FromJson<Manifest>(File.ReadAllText(p)))));
    }

    static string Validate(Manifest m)
    {
        var prefab = Resources.Load<GameObject>(Resource(m));

        if (prefab == null || !AssetDatabase.GetAssetPath(prefab).EndsWith(".prefab"))
        {
            throw new Exception("Prefab resource: " + m.name);
        }

        var scene = EditorSceneManager.NewPreviewScene();

        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);

        try
        {
            if (root.GetComponent<PlayableCharacterMotor>() == null)
            {
                throw new Exception("Missing driver");
            }

            var anim = root.GetComponent<Animation>();

            var bones = root.GetComponentsInChildren<Transform>();

            foreach (var spec in m.clips)
            {
                var clip = anim.GetClip(spec.name);

                if (clip == null || !clip.legacy || Mathf.Abs(clip.length - (spec.lastFrame - spec.firstFrame) / 30f) > .04f)
                {
                    throw new Exception("Clip duration: " + m.name + "/" + spec.name);
                }

                clip.SampleAnimation(root, 0);

                var rotations = bones.Select(b => b.localRotation).ToArray();

                var positions = bones.Select(b => b.localPosition).ToArray();

                clip.SampleAnimation(root, clip.length * .26f);

                if (!bones.Where((b, i) => Quaternion.Angle(rotations[i], b.localRotation) > .1f || Vector3.Distance(positions[i], b.localPosition) > .001f).Any())
                {
                    throw new Exception("No motion: " + m.name + "/" + spec.name);
                }

                for (int i = 0; i <= 8; i++)
                {
                    clip.SampleAnimation(root, clip.length * i / 8);

                    var bounds = Bounds(root);

                    if (float.IsNaN(bounds.size.sqrMagnitude) || bounds.size.magnitude > 10)
                    {
                        throw new Exception("Invalid animation bounds");
                    }
                }

                if (spec.loop && bones.Where((b, i) => Quaternion.Angle(rotations[i], b.localRotation) > .2f || Vector3.Distance(positions[i], b.localPosition) > .002f).Any())
                {
                    throw new Exception("Loop seam: " + m.name + "/" + spec.name);
                }
            }

            anim.GetClip("Idle").SampleAnimation(root, 0);

            int triangles = 0;

            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (r.sharedMaterials.Any(mat => mat == null || mat.shader.name != "DigitalArena/Solid"))
                {
                    throw new Exception("Material missing");
                }

                if (r.bones.Any(b => b == null))
                {
                    throw new Exception("Missing bone");
                }

                if (r.sharedMesh.boneWeights.Any(w => Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) > .001f))
                {
                    throw new Exception("Invalid skin weights");
                }

                triangles += r.sharedMesh.triangles.Length / 3;
            }

            var idle = Bounds(root);

            if (triangles == 0 || (m.triangleBudget > 0 && triangles > m.triangleBudget) || idle.size.y > m.targetHeight + .025f || idle.size.x > m.maxWidth + .025f || idle.size.z > m.maxDepth + .025f || Mathf.Abs(idle.min.y) > .03f)
            {
                throw new Exception("Size/ground/budget: " + m.name + " " + idle);
            }

            string report = m.name + ": PASS; triangles=" + triangles + "; size=" + idle.size + "; 4 clips, loop seams, skin weights, materials verified";

            File.WriteAllText(Folder(m) + "/Source~/validation.txt", report);

            return report;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static Bounds Bounds(GameObject root)
    {
        var result = new Bounds();

        bool initialized = false;

        foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh = r.sharedMesh;

            var vertices = mesh.vertices;

            var weights = mesh.boneWeights;

            var matrices = r.bones.Select((bone, i) => bone.localToWorldMatrix * mesh.bindposes[i]).ToArray();

            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];

                var v = vertices[i];

                var p = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;

                if (!initialized)
                {
                    result = new Bounds(p, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(p);
                }
            }
        }

        return result;
    }
}
