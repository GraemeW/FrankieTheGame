using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Random From Siblings Tile", menuName = "CustomRuleTiles/New Random From Siblings Rule Tile")]
public class RuleTileRandomFromSiblings : RuleTileSibling
{
    // Note:  Parent rules will be ignored, only uses sibling rules
    
    // ReSharper disable once InconsistentNaming
    public float m_PerlinScale = 0.5f;

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        var ruleTileSibling = tile as RuleTileSibling;
        if (ruleTileSibling == null) { return base.RuleMatch(neighbor, tile); }

        return neighbor switch
        {
            TilingRuleOutput.Neighbor.This => (siblings.Contains(tile) || base.RuleMatch(neighbor, tile)),
            TilingRuleOutput.Neighbor.NotThis => (!siblings.Contains(tile) && base.RuleMatch(neighbor, tile)),
            _ => base.RuleMatch(neighbor, tile)
        };
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // Standard Initialization
        var iden = Matrix4x4.identity;
        tileData.sprite = m_DefaultSprite;
        tileData.gameObject = m_DefaultGameObject;
        tileData.colliderType = m_DefaultColliderType;
        tileData.flags = TileFlags.LockTransform;
        tileData.transform = iden;

        // Override to random tile from sibling rule set
        if (siblings == null || siblings.Count == 0) { return; }
        int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, 100000f) * siblings.Count), 0, siblings.Count - 1);
        if (siblings[index] == null) { return; }

        siblings[index].GetTileData(position, tilemap, ref tileData);
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        // Override to random tile from sibling rule set
        if (siblings == null || siblings.Count == 0) { return false; }
        int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, 100000f) * siblings.Count), 0, siblings.Count - 1);
        return siblings[index] != null && siblings[index].GetTileAnimationData(position, tilemap, ref tileAnimationData);
    }
}
