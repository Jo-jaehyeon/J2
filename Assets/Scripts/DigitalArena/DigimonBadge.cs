using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalArena
{
    // Center the source silhouette; element colors affect only the symbol, never the frame.
    public static class DigimonBadge
    {

        public static readonly Color[]	Colors =
        {
            new Color32(234, 64, 55, 255),
            new Color32(40, 114, 237, 255),
            new Color32(155, 225, 59, 255),
            new Color32(250, 219, 53, 255),
            new Color32(119, 213, 248, 255),
            new Color32(147, 92, 50, 255),
            Color.white,
            Color.black,
            Color.gray
        };

        static readonly string[]	Names =
        {
            "Vaccine",
            "Virus",
            "Data",
            "Unknown",
            "Free",
            "NoData"
        };

        public static Texture2D Source(DigimonType type) => Resources.Load<Texture2D>("UI/DigimonTypes/" + Names[(int)type]);

        public static Rect ContentBounds(Texture2D source)
        {
            var pixels = source.GetPixels();

            int left = source.width, right = -1, bottom = source.height, top = -1;

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    Color p = pixels[y * source.width + x];

                    if ((Mathf.Max(p.r, p.g) - p.b) * p.a < .1f)
                    {
                        continue;
                    }

                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }

            if (right < left)
            {
                throw new System.InvalidOperationException("No visible type symbol: " + source.name);
            }

            return new Rect(left / (float)source.width, bottom / (float)source.height, (right - left + 1) / (float)source.width, (top - bottom + 1) / (float)source.height);
        }

        public static Texture2D Create(DigimonType type, DigimonElement element)
        {
            var source = Source(type);

            if (source == null)
            {
                throw new System.InvalidOperationException("Missing type texture: " + Names[(int)type]);
            }

            var shader = Resources.Load<Shader>("UI/DigimonTypeBadge");

            var material = new Material(shader);

            material.SetColor("_ElementColor", Colors[(int)element]);

            var bounds = ContentBounds(source);

            float aspect = bounds.width * source.width / (bounds.height * source.height);

            material.SetVector("_IconRect", new Vector4(bounds.x, bounds.y, bounds.width, bounds.height));
            material.SetVector("_IconScale", new Vector4(.64f * Mathf.Min(1, aspect), .64f * Mathf.Min(1, 1 / aspect), 0, 0));

            var target = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);

            var previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, target, material);
                RenderTexture.active = target;

                var result = new Texture2D(64, 64, TextureFormat.RGBA32, false)
                {
                    name = Names[(int)type] + "_" + element,
                    filterMode = FilterMode.Bilinear
                };

                result.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                result.Apply();

                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(material);
            }
        }
    }
}
