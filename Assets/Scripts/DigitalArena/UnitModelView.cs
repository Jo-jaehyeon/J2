using System.Collections.Generic;
using UnityEngine;
namespace DigitalArena
{
    // Models/materials only; no map generation, AI or round lifecycle.
    public sealed class UnitModelView : MonoBehaviour
    {
        const int WorldLayer=29;
        Shader shader;
        readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
        static Color C(uint v)=>new Color32((byte)(v>>16),(byte)(v>>8),(byte)v,255);
        void OnDestroy(){foreach(var m in materials.Values)if(m!=null)Destroy(m);}
        public void BuildSpawnModel(DigimonData data, bool enemy)
        {
            shader = Resources.Load<Shader>("ArenaSolid");
            BuildUnitModel(transform, data, enemy);
        }

        void BuildUnitModel(Transform root, DigimonData data, bool enemy)
        {
            if (!string.IsNullOrEmpty(data.prefabPath))
            {
                var prefab = Resources.Load<GameObject>(data.prefabPath);

                if (prefab != null)
                {
                    var model = Instantiate(prefab, root, false);

                    foreach (var t in model.GetComponentsInChildren<Transform>(true))
                    {
                        t.gameObject.layer = WorldLayer;
                    }

                    return;
                }

                Debug.LogWarning("Missing Digimon prefab: " + data.prefabPath);
            }

            if (data.placeholder != PlaceholderModel.Default)
            {
                var shape = data.placeholder == PlaceholderModel.BlueCube ? PrimitiveType.Cube : data.placeholder == PlaceholderModel.GreenSphere ? PrimitiveType.Sphere : PrimitiveType.Capsule;

                Color color = data.placeholder == PlaceholderModel.BlueCube ? C(0x64B5F6) : data.placeholder == PlaceholderModel.GreenSphere ? C(0x82C866) : C(0xAD78D0);

                float size = 0.65f + 0.15f * Mathf.Clamp(data.modelTier, 1, 5);

                float height = shape == PrimitiveType.Capsule ? size * 1.4f : size;

                Shape(root, shape, new Vector3(0, height / 2, 0), new Vector3(size, shape == PrimitiveType.Capsule ? height / 2 : height, size), color);

                return;
            }

            BuildModel(root, data.modelTier, enemy);
        }

        void BuildModel(Transform root, int tier, bool enemy)
        {
            Color body = enemy ? C(0x9876B9) : C(0xF3A134), pale = enemy ? C(0xD8C7E8) : C(0xFFE098), steel = C(0xA5C3CC), dark = C(0x27313B);

            Shape(root, PrimitiveType.Sphere, new Vector3(0, -0.06f, 0), new Vector3(1.15f, 0.04f, 0.9f), C(0x283B3C));

            if (tier < 2)
            {
                body = enemy ? body : C(0xF1A1B3);
                Shape(root, PrimitiveType.Sphere, new Vector3(0, 0.48f, 0), new Vector3(0.95f, 0.8f, 0.85f), body);

                for (int side = -1; side <= 1; side += 2)
                {
                    Shape(root, PrimitiveType.Capsule, new Vector3(side * 0.3f, 1.04f, -0.02f), new Vector3(0.13f, 0.39f, 0.15f), body).localRotation = Quaternion.Euler(0, 0, side * -13);
                }

                if (enemy)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Shape(root, PrimitiveType.Capsule, new Vector3((i - 1.5f) * 0.25f, 0.12f, 0.2f), new Vector3(0.12f, 0.22f, 0.16f), body).localRotation = Quaternion.Euler(65, 0, 0);
                    }
                }

                Eyes(root, 0.58f, 0.37f, 0.2f, enemy ? C(0xDF638E) : C(0x2F735B));

                return;
            }

            if (enemy && tier >= 5)
            {
                body = C(0x454455);
            }

            if (enemy && tier == 3)
            {
                Shape(root, PrimitiveType.Sphere, new Vector3(0, 0.85f, 0), new Vector3(1, 1.7f, 0.8f), body);

                for (int i = 0; i < 3; i++)
                {
                    Shape(root, PrimitiveType.Cube, new Vector3(0, 0.45f + i * 0.4f, 0), new Vector3(1.08f, 0.12f, 0.86f), dark);
                }

                Eyes(root, 1, 0.4f, 0.22f, C(0xEB5879));

                return;
            }

            Shape(root, PrimitiveType.Sphere, new Vector3(0, 0.7f, 0), new Vector3(0.82f, 1.05f, 0.7f), body);
            Shape(root, PrimitiveType.Sphere, new Vector3(0, 0.65f, 0.23f), new Vector3(0.58f, 0.65f, 0.4f), pale);
            Shape(root, PrimitiveType.Cube, new Vector3(0, 1.28f, 0.12f), new Vector3(0.87f, 0.68f, 0.7f), enemy ? pale : body);

            if (!enemy)
            {
                Shape(root, PrimitiveType.Cube, new Vector3(0, 1.13f, 0.51f), new Vector3(0.72f, 0.3f, 0.5f), body);
            }

            Eyes(root, 1.4f, 0.48f, 0.23f, enemy ? C(0xE84979) : C(0x377C65));

            for (int side = -1; side <= 1; side += 2)
            {
                Shape(root, PrimitiveType.Capsule, new Vector3(side * 0.53f, 0.7f, 0.07f), new Vector3(0.23f, enemy ? 0.48f : 0.27f, 0.25f), body).localRotation = Quaternion.Euler(25, 0, side * 18);
                Shape(root, PrimitiveType.Cube, new Vector3(side * 0.29f, 0.14f, 0.2f), new Vector3(0.34f, 0.25f, 0.65f), body);

                for (int claw = 0; claw < 2; claw++)
                {
                    Shape(root, PrimitiveType.Cube, new Vector3(side * 0.29f + (claw - 0.5f) * 0.14f, 0.15f, 0.53f), new Vector3(0.08f, 0.1f, 0.19f), pale);
                }
            }

            Shape(root, PrimitiveType.Capsule, new Vector3(0, 0.38f, -0.55f), new Vector3(0.22f, 0.4f, 0.24f), body).localRotation = Quaternion.Euler(65, 0, 0);

            if (tier >= 3)
            {
                Shape(root, PrimitiveType.Cube, new Vector3(0, 1.58f, 0.08f), new Vector3(0.94f, 0.22f, 0.76f), enemy ? dark : C(0x856348));

                for (int side = -1; side <= 1; side += 2)
                {
                    Shape(root, PrimitiveType.Capsule, new Vector3(side * 0.38f, 1.85f, 0.1f), new Vector3(0.13f, 0.28f, 0.13f), pale).localRotation = Quaternion.Euler(0, 0, -side * 18);
                }
            }

            if (tier >= 4)
            {
                Shape(root, PrimitiveType.Cube, new Vector3(-0.27f, 1.32f, 0.1f), new Vector3(0.52f, 0.71f, 0.77f), steel);
                Shape(root, PrimitiveType.Cube, new Vector3(0.62f, 0.72f, 0.2f), new Vector3(0.38f, 0.48f, 0.52f), steel);

                for (int side = -1; side <= 1; side += 2)
                {
                    Shape(root, PrimitiveType.Cube, new Vector3(side * 0.71f, 1.17f, -0.38f), new Vector3(0.72f, 0.92f, 0.13f), tier >= 5 ? pale : C(0x9C709A)).localRotation = Quaternion.Euler(0, side * 25, side * -25);
                }
            }

            if (tier >= 5)
            {
                Shape(root, PrimitiveType.Cube, new Vector3(0, 0.87f, 0.37f), new Vector3(0.72f, 0.35f, 0.21f), enemy ? C(0xD85883) : C(0xE3C15A));

                for (int side = -1; side <= 1; side += 2)
                {
                    Shape(root, PrimitiveType.Cube, new Vector3(side * 0.64f, 0.66f, 0.18f), new Vector3(0.35f, 0.45f, 0.5f), steel);

                    for (int i = 0; i < 2; i++)
                    {
                        Shape(root, PrimitiveType.Cube, new Vector3(side * 0.64f + (i - 0.5f) * 0.18f, 0.52f, 0.54f), new Vector3(0.09f, 0.1f, 0.6f), pale);
                    }
                }
            }

            if (enemy && tier == 6)
            {
                root.localScale = Vector3.one * 1.2f;
            }
        }

        void Eyes(Transform root, float y, float z, float spacing, Color iris)
        {
            for (int i = 0; i < (spacing == 0 ? 1 : 2); i++)
            {
                float x = spacing == 0 ? 0 : (i == 0 ? -spacing : spacing);

                Shape(root, PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(0.22f, 0.2f, 0.09f), Color.white);
                Shape(root, PrimitiveType.Cube, new Vector3(x, y, z + 0.05f), new Vector3(0.09f, 0.13f, 0.05f), iris);
            }
        }

        Transform Shape(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);

            go.layer = WorldLayer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var collider = go.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            if (!materials.TryGetValue(color, out var material))
            {
                material = new Material(shader);
                material.color = color;
                materials.Add(color, material);
            }

            go.GetComponent<Renderer>().sharedMaterial = material;

            return go.transform;
        }
    }
}
