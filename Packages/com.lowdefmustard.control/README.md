# Low Def Mustard Control

Movement, input-control, and pathfinding systems: a walkable-region navmesh generator for 2D colliders, A* pathfinding over that mesh, top-down movement/warp handling, generic controller/input-receiver plumbing, and patrol paths.

- **Package name:** `com.lowdefmustard.control`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.lowdefmustard.utils` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.control": "file:../path/to/com.lowdefmustard.control"` in your project's `manifest.json`. The `com.lowdefmustard.utils` package must also be present.

## Assembly Structure

| Assembly | Root Namespace | Platform | References |
|---|---|---|---|
| `LowDefMustard.Control` | `LowDefMustard.Control` | Runtime | `LowDefMustard.Utils` |
| `LowDefMustard.Control.Editor` | `LowDefMustard.Control.Editor` | Editor only | `LowDefMustard.Control` |

## Contents

### Controller / Input Receiver (`Runtime/BaseController.cs`, `Runtime/ControllerInputType.cs`, `Runtime/InputReceiver`)

A generic framework for routing directional/action input through a stack of receivers (e.g. player movement, menus, dialogue) without those receivers needing to know about each other:

- **`ControllerInputType`** — `DefaultNone`, four `Navigate*` directions, `Execute`, `Cancel`, `Option`, `Escape`.
- **`BaseController`** — Abstract `MonoBehaviour`. Maintains a stack of active `IInputReceiver`s; only the most recently enabled receiver gets live input, with earlier ones auto-suspended and restored as receivers enable/disable/exit. 
  - Polls periodically (`listenerPollingInterval`) and queues self-destruction if it ends up with no receivers and no alternates (`HasAlternateReceiversActive`/`ShouldDestroyForNoReceivers`, both overridable). 
  - Also exposes:
    - `ParseDirectionalInput` / `NavigationVectorToInputType` — converts a `Vector2` into a `ControllerInputType`, picking the dominant axis.
    - `TryInputTypeToNavigationVector` — inverse conversion.
    - `VerifyUnique()` — singleton guard, intended for use in `Awake`.
- **`IInputReceiver`** — Contract a receiver to plug into a `BaseController` (get an input handler delegate, subscribe to receiver-modified events, accept active/inactive toggling, bind to a controller).
- **`ActiveInputReceiver`** — Internal bookkeeping wrapper pairing a receiver with its disable callback and enabled state.
- **`ReceiverModifiedType`** / **`ReceiverModifiedData`** — Event enum/payload a receiver uses to notify its controller of state changes (`ClientEnable`, `ClientDisable`, `ClientExit`, `ClearDisableCallbacks`, `WritingStateChanged`, `ItemSelected`).

### Movement (`Runtime/Movement`)

- **`MovementStyle`** — `Walk` (physics-driven, continuous) or `Warp` (instant teleport, with configurable pre/post delays).
- **`MovementConfiguration`** — `ScriptableObject` (`Characters/New Movement Configuration`) holding per-character movement tuning (speed, movement style, pathfinding on/off, warp delays, target-history length) and the actual move-execution logic for both input-driven and target-driven movement.
- **`Mover`** — Abstract `MonoBehaviour` base (`RequireComponent(PathFinder)`) for any entity that moves toward a coordinate or a target `GameObject`. 
  - Handles rigidbody-based movement, look-direction/animator parameter updates (`Speed`, `xLook`, `yLook` animator floats), pixel-perfect position rounding, target-history smoothing via `CircularBuffer<Vector2>`, warp delay coroutines, and delegates the actual pathfinding decision to `PathFinder`/`MovementConfiguration`. 
  - Concrete subclasses supply `SelfInitializeRigidBody`, `GetCurrentSpeed`, and `UpdateAnimatorParameters`.

#### MoveMesh (`Runtime/Movement/MoveMesh`)

Runtime navmesh generation for 2D scenes, driven entirely by existing colliders — no external navmesh baking step:

- **`MoveMesh`** (`RequireComponent(BoxCollider2D)`) — Scans child `CompositeCollider2D`s to find enclosed (walkable) regions, rasterizes them plus any additional obstacle colliders (`Box`/`Circle`/`Capsule`/`Polygon`) supplied via `additionalColliderSources` into a cell grid, erodes the walkable area inward by `walkabilityErosionRadius`, bakes per-cell traversal costs (higher near edges, via `edgeCostPenalty`/`edgeCostFalloff`), and traces simplified region outlines (Ramer–Douglas–Peucker) for editor gizmo/debug drawing. 
  - Exposes `WorldToCell`/`CellToWorld` grid conversions, an eroded-grid cache keyed by entity size, and lives on a dedicated `MoveMesh` physics layer for fast lookups (`GetMoveMeshLayerMask`).
- **`WalkabilityGrid`** — Serializable cell grid (columns/rows/cell size/origin, walkable bool per cell, non-serialized traversal-cost array) baked by `MoveMesh` and consumed by `PathFinder`.
- **`Editor/MoveMesh/MoveMeshEditor`** — Custom inspector to invoke `MoveMesh.RunDetection`/`ClearData` with an editor progress bar and undo support.

#### Pathfinding (`Runtime/Movement/PathFinding`)

- **`PathFinder`** — `MonoBehaviour`. Locates the `MoveMesh` beneath the entity via an overlap circle, sizes itself from an attached `Circle`/`CapsuleCollider2D` (or a fallback size), and runs an 8-directional A* search over the mesh's eroded/cost-weighted grid (binary min-heap open set, pooled `AStarNode`s to avoid per-search allocation, diagonal-move corner-clipping prevention, and string-pulling to straighten the resulting path). 
  - Also supports `FindBestReachablePosition` — the closest walkable cell to a target within a travel-distance budget, used for warp-style movement. 
  - Remembers the last viable target briefly (`lastTargetMemorySeconds`) so a target that temporarily leaves the grid doesn't immediately fail pathing.
- **`AStarNode`** — Pooled node record (grid coords, costs, parent) for the A* search.
- **`PathFindingCheckType`** — `Skip` / `Check` / `ForceCheck`, controlling whether `Mover`/`PathFinder` should re-run pathfinding this call or reuse the cached path.

### Patrol (`Runtime/Movement/Patrol`)

- **`PatrolPath`** — Ordered array of `PatrolPathWaypoint`s with looping and ping-pong ("`returnToFirstWaypoint`" off) support - `GetNextIndex` drives traversal order. 
  - Draws waypoint spheres/connecting lines as gizmos (dimmer when unselected, start waypoint colored distinctly), matching the intended patrol direction including ping-pong reversal.
- **`PatrolPathWaypoint`** — A single waypoint with a `WaypointType` (`Move` or `Warp`).
- **`WaypointType`** — `Move` / `Warp`.

## Design Notes

- `Mover` has a hard `RequireComponent(PathFinder)` dependency, even when `usingPathFinding` is disabled on the `MovementConfiguration`. This is intentional: every `Mover` gets pathfinding capability out of the box, so callers never need to remember to add `PathFinder` separately or worry about it being missed.
- `MoveMesh` regeneration is editor-triggered (via `MoveMeshEditor`'s "Run Detection" button) and not automatic — obstacle layout changes require manually re-running detection.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
