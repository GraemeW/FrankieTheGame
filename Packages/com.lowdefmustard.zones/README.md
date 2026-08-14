# Low Def Mustard Zones

Zone/scene graph data model, Addressables-backed scene transitions and fading, and two UI Toolkit editor tools: a single-zone node-graph editor (`ZoneEditor`) and a cross-scene visual world-map builder (`MultiZoneViewer`).

- **Package name:** `com.lowdefmustard.zones`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** `com.unity.localization` 1.5.12, `com.unity.addressables` 2.9.1, `com.unity.ugui` 2.5.0, `com.lowdefmustard.utils` 0.1.0, `com.lowdefmustard.localization` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.zones": "file:../path/to/com.lowdefmustard.zones"` in your project's `manifest.json`. `com.lowdefmustard.utils`, `com.lowdefmustard.localization`, and Unity's Localization/Addressables packages must also be present.

## Assembly Structure

| Assembly                     | Root Namespace               | Platform    | References                                                                                                                                 |
|------------------------------|------------------------------|-------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| `LowDefMustard.Zones`        | `LowDefMustard.Zones`        | Runtime     | `LowDefMustard.Utils`, `LowDefMustard.Localization`, `Unity.Addressables`, `Unity.ResourceManager`, `Unity.Localization`, `UnityEngine.UI` |
| `LowDefMustard.Zones.Editor` | `LowDefMustard.Zones.Editor` | Editor only | `LowDefMustard.Zones`, `LowDefMustard.Utils`, `LowDefMustard.Utils.Editor`, `LowDefMustard.Localization`, `Unity.Localization`             |

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
- **`ZoneHandlerBase`** — `MonoBehaviour` placed on an actual in-scene `GameObject`, bridging a `ZoneNode` (editor-only graph data) to a real warp/entry position (`warpTransform`, falling back to the handler's own transform).
  - `MultiZoneViewer` crawls `ZoneHandlerBase` entities to find cross-zone links and plot node positions on the map.

### Transitions (`Runtime/Transitions`)

- **`FaderBase<TTransitionType>`** (abstract, `TTransitionType : struct, Enum`) — Base class for a project's screen-fade/transition-type `MonoBehaviour`, found via a `"Fader"` tag and exposed through static entry points (`StartStandardFade`, `StartBlipFade`, `StartSceneLoadFade`, `StartQuickSceneLoadFade`) so any code can trigger a fade without holding a reference. 
  - `TTransitionType` is the project's own transition-type enum (e.g. distinguishing a plain UI fade from a scene-load fade), letting per-type timing/behaviour vary. 
  - Scene-load fades apply an extra `zoneFadeTimerMultiplier` and call abstract `TriggerSave`/`TriggerLoad` hooks around the load — intended to be wired to a save system such as `com.lowdefmustard.saving`. 
  - Alpha crossfading is the default visual for fading, but `IsSkipFade`/`TransitionUsesStandaloneFadeControl` + `TriggerStandaloneFadeIn`/`Out`/`Cleanup` let a subclass substitute a fully custom visual for specific transition types. 
  - Holds its scene-loader reference as the non-generic `SceneLoaderBase` (i.e. instead of the typed `SceneLoaderBase<TSceneType>`) since `FaderBase` has no reason to know a project's scene-queue enum.
- **`FaderEventTriggers<T>`** — Struct of optional callbacks (`onFadeIn`, `onFadePeak`, `onFadeOut`, `onFadeComplete`) a caller can supply to hook specific moments of a fade.
- **`SceneLoaderBase`** (non-generic, abstract) — Base layer holding everything that doesn't depend on a project's scene-queue enum.
  - Notable Details:
    - the `"SceneLoader"`-tag singleton finder (`FindSceneLoader()`)
    - the `sceneLoadFadeProvider` - i.e. an `Action<Zone, bool>` that a `FaderBase<T>` can subscribe to so a scene load can route through a fade
    - current/last `Zone` tracking with `leavingZone`/`zoneUpdated` events
    - the async scene-load coroutine (`LoadNewSceneAsync`). 
  - Splitting this out as non-generic lets `FaderBase<TTransitionType>` hold a reference to the scene loader without also needing to know the project's scene enum type.
- **`SceneLoaderBase<TSceneType>`** (abstract, `TSceneType : struct, Enum`) — Adds the project-specific scene-queue mapping on top of the non-generic `SceneLoaderBase`.
  - Notable Details:
    - an `[EnumKeyedCollection]`-drawn `ZoneSceneTypeLookup<TSceneType>` mapping each `TSceneType` value to a `Zone`
    - `QueueScene`/`StartLoadScene` to enable loading in a new scene using the aforementioned `TSceneType`
    - hooks for a concrete project subclass to classify its own enum (`IsNewGameSceneType`, `IsGameOverSceneType`, `ShouldSaveSessionOnGameOver`)
    - hooks to resolve the target `Zone` via `DemoZoneOverrideProvider`
- **`ZoneSceneTypeLookup<TSceneType>`** — Thin `EnumKeyedCollection<TSceneType, Zone>` subclass (see `com.lowdefmustard.utils`) used as `SceneLoaderBase<TSceneType>`'s scene-queue-to-`Zone` map.
- **`SceneQueueData`** — Plain data (delay, completion callback, whether to use the fader) passed to `SceneLoaderBase<TSceneType>.QueueScene`.

### Editor Tooling — Shared (`Editor`)

- **`SceneReferencePropertyDrawer`** — Standard drag-and-drop `SceneAsset` field for `SceneReference` (with an "X" to clear), matching Unity's typical scene-reference inspector pattern.
- **`ZoneTools.OpenSceneAndAct`** — Prompts to save any modified scenes, opens the target `Zone`'s scene, then invokes a callback once open. 
  - e.g. used by `ZoneNodeView`'s "select ref" button to locate and select the matching `ZoneHandlerBase` GameObject in-scene

### Zone Editor (`Editor/ZoneEditor`)

**Menu:** `Window > Zone Editor` (also opens automatically via double-click on a `Zone` asset). A UI Toolkit node-graph canvas for editing a single zone's node tree.

- **`ZoneEditor`** (`EditorWindow`) — Tracks Unity's `Selection` to follow the active `Zone`/`ZoneNode`; on first opening a `Zone` asset, localizes its standard entries and creates a root node if missing (deliberately *not* done in `Zone.Awake()` — see Design Notes).
- **`ZoneGraphView`** — The canvas itself: background grid, group layer, edges layer, and node layer, with pan/zoom via `com.lowdefmustard.utils`' `StandardCanvasPanManipulator`/`StandardCanvasZoomManipulator`. 
  - handles node creation/deletion, parent↔child linking, group placement, and `FocusOnNode` re-centering
- **`ZoneEdgesLayer`** — Draws a green→blue gradient bezier curve between each parent/child `ZoneNode` pair via `Painter2D`.
- **`ZoneNodeView`** — Draggable node card (`StandardNodeDragManipulator`) showing an editable node ID, a "select ref" button (jumps to and selects the matching `ZoneHandlerBase` in-scene via `ZoneTools`), link/unlink toggle, and add/delete-child buttons.
- **`ZoneNodeGroupView`** / **`ZoneNodeGroupDragManipulator`** — Draggable grouping box with a double-click-to-rename header; dragging the group also drags every node view it currently contains.

### Multi-Zone Viewer (`Editor/MultiZoneViewer`)

**Menu:** `Tools > Multi-Zone Viewer`. A second EditorWindow that assembles a stitched, pannable/zoomable **world map** out of top-down camera snapshots of each scene, with cross-zone links drawn between them — effectively an auto-generated overworld map built from the actual scene content.

- **Capture pipeline (`CaptureAllZones`)** — For each viable scene: opens it, computes world bounds (`Tilemap`-aware if any exist, else falls back to `Renderer` bounds), positions `Camera.main` orthographically to frame those bounds, renders to an offscreen `RenderTexture`, and saves the result as a PNG under a `MultiZoneViewer/` folder alongside the project. 
  - resulting tiles are laid out left-to-right/top-to-bottom on the map 
  - snapshot resolution scales with world size (`worldToSnapshotScalingFactor`), clamped between configurable min/max pixel dimensions
- **Scene traversal** — Either crawls outward from a chosen root `Zone` following `ZoneHandlerBase`/`externalZoneLinkToZoneNode` links (`useZoneHandlerCrawl`, via `ZoneHandlerConduit`), or falls back to every scene in the active Build Profile.
- **`MultiZoneView`** (`ScriptableObject` asset) — Persists the captured map: one `ZoneViewData` per zone (snapshot path, map position/dimensions) plus per-zone `ZoneNodeData` (each node's *relative* (0–1) position within its zone, and an optional cross-zone link target). 
  - recapture is incremental — `keepExistingPositions`/`keepExistingDimensions` toggles preserve manual map layout across re-captures
- **Node dots + linking** — Each `ZoneHandlerBase`'s position is plotted as a small circular "dot" at its relative position within its zone's tile. 
  - dragging a dot far enough onto another zone's dot creates or toggles a cross-zone link, written straight back to the underlying `ZoneNode.externalZoneLinkToZoneNode` (`TryLinkZoneNodes`/`TryClearZoneNodeLink`)
    - such that links can be authored visually here as an alternative to manually editing the Node through the Unity inspector
  - `ResyncZoneNodeLinks` reconciles stored link data against the live node graph after scene edits, without a full recapture
- **Manipulators** — `MultiZonePanManipulator` (middle-mouse or Alt+drag canvas pan), `MultiZoneDragManipulator` (drag a zone tile to reposition it on the map; click vs. drag disambiguated by a movement threshold, with `Undo` support), `ZoneNodeLinkManipulator` (drag a node dot to start/update/complete a cross-zone link).

## Design Notes

- `MultiZoneViewer`'s capture pipeline requires a `Camera.main`-taggable camera to exist in every scene it crawls, and only computes bounds from `Tilemap`/`Renderer` components — a scene with neither falls back to a placeholder 10×10 snapshot.
- `ZoneEditor` and `MultiZoneViewer` are two intentionally separate UI Toolkit canvases rather than one editor with two view modes — each has a different coordinate space (a single zone's local node layout vs. a world map stitched from multiple scenes) and different edge semantics (`children` vs. `externalZoneLinkToZoneNode`).
- `Zone.Awake()` deliberately does **not** auto-create a root node; doing so in `Awake()` races with Unity's ScriptableObject serialization in the editor (the node gets created, then destroyed, before serialization completes). Root-node creation happens in `ZoneEditor.OnOpenAsset` instead.
- `SceneLoaderBase`/`SceneLoaderBase<TSceneType>` follow the same non-generic-base-plus-generic-subclass split as `FaderBase<T>`'s scene-loader reference (see above): the members a generic-agnostic caller actually needs (`sceneLoadFadeProvider`, the tag-based finder, current/last `Zone` tracking, the load coroutine) live on the non-generic `SceneLoaderBase`, so `FaderBase<TTransitionType>` never has to become generic over a second, unrelated enum just to reach the scene loader.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
