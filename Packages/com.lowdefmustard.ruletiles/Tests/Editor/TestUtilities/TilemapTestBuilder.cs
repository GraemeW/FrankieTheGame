using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    internal static class TilemapTestBuilder
    {
        // Const Tunables
        private const int _spriteSize = 2;

        // Internal Methods
        internal static bool TryGetAnimationData(TileBase tile, Tilemap tilemap, out TileAnimationData tileAnimationData)
        {
            tilemap.SetTile(Vector3Int.zero, tile); // Paints the tile at the origin
            tileAnimationData = new TileAnimationData();
            return tile.GetTileAnimationData(Vector3Int.zero, new ITilemap(tilemap), ref tileAnimationData); // queries it the way the Tilemap would
        }
        
        internal static Tilemap CreateTilemap(TestObjectTracker tracker)
        {
            // Note:  grid root is tracked -> destroying it takes the child Tilemap with it
            
            GameObject grid = tracker.Track(new GameObject(nameof(Grid), typeof(Grid)));
            var tilemapObject = new GameObject(nameof(Tilemap), typeof(Tilemap));
            tilemapObject.transform.SetParent(grid.transform);
            return tilemapObject.GetComponent<Tilemap>();
        }
        
        internal static Sprite CreateSprite(TestObjectTracker tracker)
        {
            Texture2D texture = tracker.Track(new Texture2D(_spriteSize, _spriteSize));
            return tracker.Track(CreateSpriteFrom(texture));
        }
        
        internal static Sprite CreateSpriteAsset(TempAssetFolder tempFolder, string assetName)
        {
            // Note:  texture is the main asset with the sprite as a sub-asset, so both reload and get cleaned up with the temp folder
            
            var texture = new Texture2D(_spriteSize, _spriteSize);
            Sprite sprite = CreateSpriteFrom(texture);
            sprite.name = assetName;
            string path = tempFolder.Path($"{assetName}Sprite.asset");
            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.AddObjectToAsset(sprite, path);
            return sprite;
        }
        
        private static Sprite CreateSpriteFrom(Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, _spriteSize, _spriteSize), Vector2.zero);
    }
}
