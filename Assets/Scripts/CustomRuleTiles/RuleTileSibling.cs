using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Sibling Rule Tile", menuName = "CustomRuleTiles/New Sibling Rule Tile")]
public class RuleTileSibling : RuleTile<RuleTileSibling.Neighbor> {
    public List<TileBase> siblings;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int Sibling = 3;
    }
    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        RuleTileSibling ruleTileSibling = tile as RuleTileSibling;
        if (ruleTileSibling == null) { return base.RuleMatch(neighbor, tile); }

        switch (neighbor)
        {
            case Neighbor.This:
                return (siblings.Contains(tile)
                    || base.RuleMatch(neighbor, tile));
            case Neighbor.NotThis:
                return (!siblings.Contains(tile)
                    && base.RuleMatch(neighbor, tile));
        }
        return base.RuleMatch(neighbor, tile);
    }
}
