# Low Def Mustard Zones

Zone/scene graph data model, Addressables-backed scene transitions and fading, and two editor tools: a single-zone node-graph editor (`ZoneEditor`) and a cross-scene visual world-map builder (`MultiZoneViewer`).

- **Package name:** `com.lowdefmustard.zones`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.unity.localization` 1.5.12
  - `com.unity.addressables` 2.9.1
  - `com.unity.ugui` 2.5.0
  - `com.lowdefmustard.utils` 0.1.0
  - `com.lowdefmustard.localization` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.zones": "file:../path/to/com.lowdefmustard.zones"` in your project's `manifest.json`. `com.lowdefmustard.utils`, `com.lowdefmustard.localization`, and Unity's Localization/Addressables packages must also be present.

## Assembly Structure

| Assembly | Root Namespace | Platform | References |
|---|---|---|---|
| `LowDefMustard.Zones` | `LowDefMustard.Zones` | Runtime | `LowDefMustard.Utils`, `LowDefMustard.Localization`, `Unity.Addressables`, `Unity.ResourceManager`, `Unity.Localization`, `UnityEngine.UI` |
| `LowDefMustard.Zones.Editor` | `LowDefMustard.Zones.Editor` | Editor only | `LowDefMustard.Zones`, `LowDefMustard.Utils`, `LowDefMustard.Utils.Editor`, `LowDefMustard.Localization`, `Unity.Localization` |

## Contents

### Core Data Model (`Runtime`)

- **`SceneReference`** — Serializable struct pairing a `SceneAsset` reference (editor-only) with a plain `sceneName`/`scenePath`, so a build can resolve scenes by name even where a live `SceneAsset` reference isn't available.
- **`Zone`** (`ScriptableObject`, `Zone/New Zone`) — One `Zone` asset per scene: a `SceneReference`, display name (`[SimpleLocalizedString]`), zone audio, and a tree of `ZoneNode`s. 
  - Implements `IAddressablesCache` (Addressables-backed static lookup by name or by scene reference, same convention as `com.lowdefmustard.utils`) and `ILocalizable` (aggregates its own and every child node's localization entries). 
  - Editor-only methods (`#if UNITY_EDITOR`) manage node/group creation, deletion, ID renaming (with matching localization-key rename), and parent/child relations — all routed through `Undo`.
- **`ZoneNode`** (`ScriptableObject`, implements `IStandardGraphNode` from `com.lowdefmustard.utils`) — A node within a zone's graph: localized display name, a `condition` (a `com.lowdefmustard.utils` `Condition`/`Predicate` gate), a `rect` for editor canvas position, and two distinct relations:
  - **`children`** — in-zone tree structure (a list of sibling `ZoneNode` IDs within the *same* `Zone`), drawn as edges by `ZoneEditor`.
  - **`externalZoneLinkToZoneNode`** — an optional link to a node in a *different* `Zone` (e.g. a door/warp destination in another scene), drawn as edges by `MultiZoneViewer`. 
  - Localization keys follow `Zone.{zoneName}.Node.{nodeID}`, kept in sync automatically as IDs/zone names change.
- **`ZoneNodeGroup`** — Editor-only visual grouping box within a zone's node graph (not a gameplay concept) — a named `Rect` that auto-recomputes its bounds to encapsulate whichever `ZoneNode`s are dragged into/out of it.
- **`ZoneHandlerBase`** — `MonoBehaviour` placed on an actual in-scene `GameObject`, bridging a `ZoneNode` (editor-only graph data) to a real warp/entry position (`warpTransform`, falling back to the handler's own transform). `MultiZoneViewer` crawls ZoneHandlers to find cross-zone links and plot node positions on the map.

### Transitions (`Runtime/Transitions`)

- **`FaderBase<T>`** (abstract, `T : struct, Enum`) — Base for a project's screen-fade/transition-type `MonoBehaviour`, found via a `"Fader"` tag and exposed through static entry points (`StartStandardFade`, `StartBlipFade`, `StartSceneLoadFade`, `StartQuickSceneLoadFade`) so any code can trigger a fade without holding a reference. 
  - `T` is the project's own transition-type enum (e.g. distinguishing a plain UI fade from a scene-load fade), alowing for per-type fade timing/behaviour 
  - Scene-load fades apply an extra `zoneFadeTimerMultiplier` and call abstract `TriggerSave`/`TriggerLoad` hooks around the load — intended to be wired to a save system such as `com.lowdefmustard.saving`.
  - Alpha crossfading is the default visual, but `IsSkipFade`/`TransitionUsesStandaloneFadeControl` + `TriggerStandaloneFadeIn`/`Out`/`Cleanup` allow for a subclass to substitute a fully custom visual for specific transition types.
- **`FaderEventTriggers<T>`** — Struct of optional callbacks (`onFadeIn`, `onFadePeak`, `onFadeOut`, `onFadeComplete`) a caller can supply to hook specific moments of a fade.
- **`SceneLoader`** — `MonoBehaviour` singleton (found via a `"SceneLoader"` tag) mapping a `SceneQueueType` to one of several preconfigured `Zone`s (`splashScreen`, `startScreen`, `namingScreen`, `newGame`, `gameOverScreen`, `gameWinScreen`) and performing the actual scene load — either directly, or routed through a subscribed `FaderBase<T>` via the `sceneLoadFadeProvider` hook. 
  - Tracks current/last `Zone` statically and raises `leavingZone`/`zoneUpdated` events. 
  - `DemoZoneOverrideProvider` lets a consuming project redirect "New Game" loads (e.g. for a demo build) without changing `SceneLoader` itself.
- **`SceneQueueData`** / **`SceneQueueType`** — Plain data (delay, completion callback, whether to use the fader) and the enum of loadable scene categories, for `SceneLoader.QueueScene`.

### Editor Tooling — Shared (`Editor`)

- **`SceneReferencePropertyDrawer`** — Standard drag-and-drop `SceneAsset` field for `SceneReference` (with an "X" to clear), matching Unity's typical scene-reference inspector pattern.
- **`ZoneTools.OpenSceneAndAct`** — Prompts to save any modified scenes, opens the target `Zone`'s scene, then invokes a callback once open. 
  - Used by `ZoneNodeView`'s "select ref" button to locate and select the matching `ZoneHandlerBase` GameObject in-scene.

### Zone Editor (`Editor/ZoneEditor`)

**Menu:** `Window > Zone Editor` (also opens automatically via double-click on a `Zone` asset). A hand-rolled UI Toolkit node-graph canvas for editing a single zone's node tree — not Unity's built-in `GraphView`, kept consistent with this project's other Rect-based custom editors.

- **`ZoneEditor`** (`EditorWindow`) — Tracks Unity's `Selection` to follow the active `Zone`/`ZoneNode`; on first opening a `Zone` asset, localizes its standard entries and creates a root node if missing (deliberately *not* done in `Zone.Awake()` — see Design Notes).
- **`ZoneGraphView`** — The canvas itself: background grid, group layer, edges layer, and node layer, with pan/zoom via `com.lowdefmustard.utils`' `StandardCanvasPanManipulator`/`StandardCanvasZoomManipulator`. Handles node creation/deletion, parent↔child linking (click "link" on a node, then click a target to toggle the relation), group placement, and `FocusOnNode` re-centering (used when selecting a `ZoneNode` asset in the Project window).
- **`ZoneEdgesLayer`** — Draws a green→blue gradient bezier curve between each parent/child `ZoneNode` pair via `Painter2D`.
- **`ZoneNodeView`** — Draggable node card (`StandardNodeDragManipulator`) showing an editable node ID, a "select ref" button (jumps to and selects the matching `ZoneHandlerBase` in-scene via `ZoneTools`), link/unlink toggle, and add/delete-child buttons. Body color shifts distinctly when the node has an `externalZoneLinkToZoneNode` set.
- **`ZoneNodeGroupView`** / **`ZoneNodeGroupDragManipulator`** — Draggable grouping box with a double-click-to-rename header; dragging the group also drags every node view it currently contains.

### Multi-Zone Viewer (`Editor/MultiZoneViewer`)

**Menu:** `Tools > Multi-Zone Viewer`. A second, independent EditorWindow that assembles a stitched, pannable/zoomable **world map** out of top-down camera snapshots of each scene, with cross-zone links drawn between them — effectively an auto-generated overworld map built from your actual scene content.

- **Capture pipeline (`CaptureAllZones`)** — For each viable scene: opens it, computes world bounds (`Tilemap`-aware if any exist, else falls back to `Renderer` bounds), positions `Camera.main` orthographically to frame those bounds, renders to an offscreen `RenderTexture`, and saves the result as a PNG under a `MultiZoneViewer/` folder alongside the project. Resulting tiles are laid out left-to-right/top-to-bottom on the map. Snapshot resolution scales with world size (`worldToSnapshotScalingFactor`), clamped between configurable min/max pixel dimensions.
- **Scene traversal** — Either crawls outward from a chosen root `Zone` following `ZoneHandlerBase`/`externalZoneLinkToZoneNode` links (`useZoneHandlerCrawl`, via `ZoneHandlerConduit`), or falls back to every scene in the active Build Profile.
- **`MultiZoneView`** (`ScriptableObject` asset) — Persists the captured map: one `ZoneViewData` per zone (snapshot path, map position/dimensions) plus per-zone `ZoneNodeData` (each node's *relative* (0–1) position within its zone, and an optional cross-zone link target). Recapture is incremental — `keepExistingPositions`/`keepExistingDimensions` toggles preserve manual map layout across re-captures.
- **Node dots + linking** — Each `ZoneHandlerBase`'s position is plotted as a small circular "dot" at its relative position within its zone's tile. Dragging a dot far enough onto another zone's dot creates or toggles a cross-zone link, written straight back to the underlying `ZoneNode.externalZoneLinkToZoneNode` (`TryLinkZoneNodes`/`TryClearZoneNodeLink`) — so links can be authored visually here as an alternative to the single-zone `ZoneEditor`. `ResyncZoneNodeLinks` reconciles stored link data against the live node graph after scene edits, without a full recapture.
- **Manipulators** — `MultiZonePanManipulator` (middle-mouse or Alt+drag canvas pan), `MultiZoneDragManipulator` (drag a zone tile to reposition it on the map; click vs. drag disambiguated by a movement threshold, with `Undo` support), `ZoneNodeLinkManipulator` (drag a node dot to start/update/complete a cross-zone link).

## Design Notes

- `ZoneEditor` and `MultiZoneViewer` are two intentionally separate UI Toolkit canvases rather than one editor with two view modes — each has a different coordinate space (a single zone's local node layout vs. a world map stitched from multiple scenes) and different edge semantics (`children` vs. `externalZoneLinkToZoneNode`).
- `Zone.Awake()` deliberately does **not** auto-create a root node; a code comment explains that doing so in `Awake()` races with Unity's ScriptableObject serialization in the editor (the node gets created, then destroyed, before serialization completes). Root-node creation happens in `ZoneEditor.OnOpenAsset` instead.
- `MultiZoneViewer`'s capture pipeline requires a `Camera.main`-taggable camera to exist in every scene it crawls, and only computes bounds from `Tilemap`/`Renderer` components — a scene with neither falls back to a placeholder 10×10 snapshot.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
