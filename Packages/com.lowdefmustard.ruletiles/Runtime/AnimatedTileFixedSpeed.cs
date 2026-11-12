using UnityEngine;
using UnityEngine.Tilemaps;

namespace LowDefMustard.RuleTiles
{
    [CreateAssetMenu(fileName = "New Fixed Speed Animated Tile", menuName = "CustomRuleTiles/New Fixed Speed Animated Tile", order = 25)]
    public class AnimatedTileFixedSpeed : AnimatedTile
    {
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            if (!base.GetTileAnimationData(position, tilemap, ref tileAnimationData)) { return false; }

            tileAnimationData.animationSpeed = TileAnimationSpeed.Resolve(m_MinSpeed, m_MaxSpeed);
            return true;
        }
    }
}
