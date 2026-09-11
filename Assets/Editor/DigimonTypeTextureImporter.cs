using UnityEditor;
using UnityEngine;
public sealed class DigimonTypeTextureImporter : AssetPostprocessor
{
    public override uint GetVersion()=>2;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/UI/DigimonTypes/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.isReadable=true;
    }
}
