# Low Def Mustard Rule Tiles

Custom rule tiles extending Unity's 2D Tilemap Extras: sibling-tile recognition across separate tile assets, and random-animation rule tiles (which Unity's built-in rule tiles don't support directly).

- **Package name:** `com.lowdefmustard.ruletiles`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.unity.2d.tilemap` 1.0.0
  - `com.unity.2d.tilemap.extras` 8.0.3

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.ruletiles": "file:../path/to/com.lowdefmustard.ruletiles"` in your project's `manifest.json`.

## Assembly Structure

| Assembly                         | Root Namespace                   | Platform    | References                                                               |
|----------------------------------|----------------------------------|-------------|--------------------------------------------------------------------------|
| `LowDefMustard.RuleTiles`        | `LowDefMustard.RuleTiles`        | Runtime     | `Unity.2D.Tilemap`, `Unity.2D.Tilemap.Extras`                            |
| `LowDefMustard.RuleTiles.Editor` | `LowDefMustard.RuleTiles.Editor` | Editor only | `Unity.2D.Tilemap`, `Unity.2D.Tilemap.Extras`, `LowDefMustard.RuleTiles` |
| `LowDefMustard.RuleTiles.Tests.Editor` | `LowDefMustard.RuleTiles.Tests.Editor` | Editor only, test-only | `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Unity.2D.Tilemap`, `Unity.2D.Tilemap.Extras`, `LowDefMustard.RuleTiles`, `LowDefMustard.RuleTiles.Editor` |

## Contents

### Rule Tile Sibling (`Runtime/RuleTileSibling.cs`)

- **`RuleTileSibling`** (`CustomRuleTiles/New Sibling Rule Tile`) — Allows separate tile assets to recognize and connect with each other as though they were the same tile. Sibling `TileBase` tiles are listed in `siblings`; `RuleMatch()` is overridden so `This`/`NotThis` neighbor checks also match against anything in that list, alongside the base rule-tile behaviour.

### Random Animated Rule Tiles (`Runtime/RuleTileRandomFromSiblings.cs`, `Runtime/RuleTileRandomAnimation.cs`, `Editor/RuleTileRandomFromSiblingsEditor.cs`)

Unity's built-in smart tiles support random tiles *or* animated tiles, but not both together. This package covers that gap, along with a caveat worth calling out up front: **Unity's animation-tile serialization is heavy** — a tilemap painted with animation tiles can bloat a scene from a few MB to tens of MB, so keep an eye on scene size (especially anything committed to Git) when painting with these.

- **`RuleTileRandomFromSiblings`** (`CustomRuleTiles/New Random From Siblings Rule Tile`) — **Current/recommended approach.** Extends `RuleTileSibling`; picks a base tile at random from `siblings` (any `TileBase`, including plain sprites, animated tiles, or other rule tiles) using a deterministic Perlin-noise hash of the cell position (`m_PerlinScale` controls how the randomness varies across positions):
  ```csharp
  int index = Mathf.Clamp(Mathf.FloorToInt(GetPerlinValue(position, m_PerlinScale, 100000f) * siblings.Count), 0, siblings.Count - 1);
  ```
  The same index is used in both `GetTileData()` and `GetTileAnimationData()`, so a given cell consistently resolves to the same sibling — enabling random *animated* tiles. The tile's own tiling ruleset is otherwise ignored; only the sibling list drives selection. Because it inherits from `RuleTileSibling`, an instance of this tile is itself directly usable as a sibling for other tiles.
  - **`Editor/RuleTileRandomFromSiblingsEditor`** — Custom inspector labeling the tile as "Random Tile from Siblings" and surfacing `m_DefaultSprite` (palette-only, unused when painting), `m_PerlinScale`, and `siblings` with explanatory tooltips.
- **`RuleTileRandomAnimation`** — **Deprecated**, kept for reference/back-compat only (no `CreateAssetMenu`, so it can no longer be created fresh from the asset menu). Instead of using sibling tiles, it force-overrides every tiling rule's output to `Animation` and picks a rule at random (same Perlin-hash approach) to source the tile/animation data.

### Rule Tile → Sibling Rule Tile Converter (`Editor/RuleTileToSiblingConverter.cs`)

- **`RuleTileToSiblingConverter`** 
  - — Editor utility under `Assets > LowDefMustard > RuleTiles > Convert to Sibling Rule Tile` (+ in the right-click context menu)
    - Select one or more plain `RuleTile` assets to create `RuleTileSibling` copy alongside each one (`<Name>Sibling.asset`)
      - Carries over the default sprite/gameObject/collider type and the full tiling-rule list, with an empty `siblings` list
    - Uses `JsonUtility.ToJson`/`FromJsonOverwrite` to transplant fields (future-proofs if `RuleTile` gains new serialized fields later)
    - Only handles the base `RuleTile` type — extend the type check if a project-specific `RuleTile<T>` subtype needs the same treatment
  - — Editor utility under `Assets > LowDefMustard > RuleTiles > Convert to Sibling Rule Tile (In-Place)` (+ in the right-click context menu)
    - Second, more dangerous option on the same selection 
    - Swaps the *existing* asset's own `m_Script` reference from `RuleTile` to `RuleTileSibling` via `SerializedObject`
      - so the asset keeps its GUID/fileID and any `Tilemap` already painted with it picks up the change immediately 
    - Gated behind an `EditorUtility.DisplayDialog` confirmation recommending a commit/stash first

## Design Notes

- `RuleTileRandomAnimation` is deprecated in favor of `RuleTileRandomFromSiblings`. 
- The rule-based approach was less desirable for two reasons: 
  - it recalculates a randomized animation speed per-tile/per-call (extra overhead)
  - it can't mix animated and non-animated (or otherwise more complex) tiles into the same random set the way a sibling list can

## Tests

Edit Mode tests only — every tested member here is a plain `ScriptableObject` or custom-editor method

`RuleTileRandomFromSiblings.GetTileData`/`GetTileAnimationData` tests compute their expected sibling index in the test, using the same public `RuleTile.GetPerlinValue(position, scale, offset)` call from the tile itself.

**Behavioural Note:** 

Both `RuleTileSibling.RuleMatch` and `RuleTileRandomFromSiblings.RuleMatch` only consult the `siblings` list when the neighboring tile is itself a `RuleTileSibling` (or subclass) instance — a plain `Tile` placed in `siblings` will never match via the sibling mechanism as a tilemap neighbor, falling through to the base rule tile's reference-equality check instead. This is intended to avoid anomalous behaviour where tile-painting follows rules in one direction and not the other due to a mis-configuration.

### Coverage at a glance

| Category                                   | Tested  | Total |
|--------------------------------------------|---------|-------|
| `RuleTileSibling` members                  | 1/1     | 1     |
| `RuleTileRandomFromSiblings` members       | 3/3     | 3     |
| `RuleTileRandomFromSiblingsEditor` members | 1/1     | 1     |
| `RuleTileRandomAnimation` members          | 0/2     | 2     |
| `RuleTileToSiblingConverter` members       | 4/4     | 4     |

### Detail by type

| Type                               | Status | Test file(s)                               | Notes                                      |
|------------------------------------|--------|--------------------------------------------|--------------------------------------------|
| `RuleTileSibling`                  | Yes    | `RuleTileSiblingTests.cs`                  |                                            |
| `RuleTileRandomFromSiblings`       | Yes    | `RuleTileRandomFromSiblingsTests.cs`       |                                            |
| `RuleTileRandomFromSiblingsEditor` | Yes    | `RuleTileRandomFromSiblingsEditorTests.cs` |                                            |
| `RuleTileToSiblingConverter`       | Yes    | `RuleTileToSiblingConverter.cs`            | `Convert`/`ConvertInPlace` menu entry points not tested directly — thin wrappers already covered by the above   | 
| `RuleTileRandomAnimation`          | No     | —                                          | Deprecated, kept for back-compat reference |

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
