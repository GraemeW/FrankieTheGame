using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils;

namespace Utils
{
    public class DefaultSpriteImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null || !importer.importSettingsMissing) { return; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameProperties.GetArtPixelsPerUnit(FindGameProperties());
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

        private static GameProperties FindGameProperties()
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(GameProperties)}"))
            {
                var gameProperties = AssetDatabase.LoadAssetAtPath<GameProperties>(AssetDatabase.GUIDToAssetPath(guid));
                if (gameProperties != null) { return gameProperties; }
            }
            return null;
        }
    }
}
