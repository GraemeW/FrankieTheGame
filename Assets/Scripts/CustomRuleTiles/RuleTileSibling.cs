using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Sibling Rule Tile", menuName = "CustomRuleTiles/New Sibling Rule Tile")]
public class RuleTileSibling : RuleTile<RuleTileSibling.Neighbor> {
    public List<TileBase> siblings;

    // Must be declared non-abstract
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Neighbor : RuleTile.TilingRuleOutput.Neighbor
    {
    }
    
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
}
