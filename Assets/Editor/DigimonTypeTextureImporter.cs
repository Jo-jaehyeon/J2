using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class DigimonTypeTextureImporter : AssetPostprocessor
{
    public override uint GetVersion() => 3;

    void OnPreprocessTexture()
    {
        bool star = assetPath == "Assets/Resources/UI/RankStar.png";

        if (!star && !assetPath.StartsWith("Assets/Resources/UI/DigimonTypes/"))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;

        if (star)
        {
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 128;
        }

        importer.isReadable = true;
    }
}
