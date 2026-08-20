using UnityEditor;
using UnityEngine;

namespace Utils.ImportDefaults
{
    public class DefaultSpriteImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null || !importer.importSettingsMissing) { return; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 16;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0f); // Bottom centre pivot
            importer.SetTextureSettings(settings);
        }
    }
}
