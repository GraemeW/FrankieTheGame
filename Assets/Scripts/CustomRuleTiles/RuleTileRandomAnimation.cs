using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "New Random Animation Rule Tile", menuName = "CustomRuleTiles/New Random Animation Rule Tile")]
public class RuleTileRandomAnimation : RuleTile<RuleTileRandomAnimation.Neighbor> {
    // Global Perlin Scale -- for pulling random animation tile
    public float m_PerlinScale = 0.5f;

    public class Neighbor : RuleTile.TilingRule.Neighbor {
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        foreach(TilingRule tempRule in m_TilingRules)
        {
            // Force override all rules as animation
            tempRule.m_Output = OutputSprite.Animation;
        }

        // Standard Initialization
        var iden = Matrix4x4.identity;
        tileData.sprite = m_DefaultSprite;
        tileData.gameObject = m_DefaultGameObject;
        tileData.colliderType = m_DefaultColliderType;
        tileData.flags = TileFlags.LockTransform;
        tileData.transform = iden;

        // Instead of iterating over rules and selecting on rule:  override to random tile from tiling rule set
        if (m_TilingRules == null || m_TilingRules.Count == 0) { return; }
        int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, 100000f) * m_TilingRules.Count), 0, m_TilingRules.Count - 1);
        TilingRule overrideRule = m_TilingRules[index];

        // Populate with override tile
        Matrix4x4 transform = iden;
        tileData.sprite = overrideRule.m_Sprites[0];
        tileData.transform = transform;
        tileData.gameObject = overrideRule.m_GameObject;
        tileData.colliderType = overrideRule.m_ColliderType;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (m_TilingRules == null || m_TilingRules.Count == 0) { return false; }

        // Instead of iterating over rules and selecting on rule:  override to random tile from tiling rule set
        int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, 100000f) * m_TilingRules.Count), 0, m_TilingRules.Count - 1);
        TilingRule overrideRule = m_TilingRules[index];
        if (overrideRule.m_Output == OutputSprite.Animation)
        {
            // Populate with override tile
            tileAnimationData.animatedSprites = overrideRule.m_Sprites;
            tileAnimationData.animationSpeed = Random.Range(overrideRule.m_MinAnimationSpeed, overrideRule.m_MaxAnimationSpeed);
            return true;
        }
        return false;
    }
}
