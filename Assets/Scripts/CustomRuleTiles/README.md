# Custom Rule Tiles

## Rule Tile Sibling

[RuleTileSibling](./RuleTileSibling.cs) allows different tile assets to recognize and connect with each other as if they were the same tile.  

This is a simple/standard implementation, where sibling `TileBase` tiles are defined in `siblings`.  `RuleMatch()` is overridden to treat sibling tiles the same as This during neighbor checks.

## Random Animated Tiles && Random Animated Rule Tiles

By default, Unity's smart tiles allow for A) random tiles B) animation tiles, but they do not allow for random animation tiles.  

Before jumping into this section, it is worth noting that Unity's animation tiles are serialized in a manner that is quite **heavy**.  Filling a scene with animation tiles can easily blow up a scene from a few MBs to many tens of MBs.  To this end, keep an eye on overall scene size when painting tilemaps, especially if you intend to commit the scene data to GIT.

In any case, here we discuss two approaches to enable random animation tiles.

### POR Approach:  Random Tiles from Siblings

[RuleTileRandomFromSiblings](./RuleTileRandomFromSiblings.cs) chooses a base tile (including arbitrary rule tiles) randomly from a list of siblings.

The index of the random tile is calculated by the deterministic approach:
```C#
int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, 100000f) * siblings.Count), 0, siblings.Count - 1);
```
, which uses the tile's positional data to define its selection. This allows the same index to be gleaned in the `GetTileAnimationData()` override, thus allowing for random animation rule tiles.

Note that:
* the ruleset from the tile itself is completely ignored in this approach
* a sibling `BaseTile` can be any tile -- it can include simple animation tiles, but also more complex rule tiles 
* since `RuleTileRandomFromSiblings` built on the `RuleTileSibling` base class, any instance is directly usable as a sibling for other (e.g.) non-animated tiles

### Alternative (Deprecated) Approach:  Random Animations from Tile Rules

[RuleTileRandomAnimation](./RuleTileRandomAnimation.cs) chooses a base tile randomly from the rule tile entries (i.e. ignoring the rules themselves).  This is accomplished by forcing each tiling rule's `m_Output` to `OutputSprite.Animation`.  The tile data is then populated with the values from the randomly selected rule.

This approach is less desirable because of A) additional overhead on randomized animation speeds on a per-tile/call basis, and B) reduced flexibility in allowing more complex rule tiles that may or may not contain animations as part of the random tile set.
