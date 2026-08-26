# Low Def Mustard Utils

General-purpose data structures, editor tooling, and attribute drawers shared across Low Def Mustard Games projects.

- **Package name:** `com.lowdefmustard.utils`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** None

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.utils": "file:../path/to/com.lowdefmustard.utils"` in your project's `manifest.json`.

## Assembly Structure

| Assembly                     | Root Namespace               | Platform    | References            |
|------------------------------|------------------------------|-------------|-----------------------|
| `LowDefMustard.Utils`        | `LowDefMustard.Utils`        | Runtime     | —                     |
| `LowDefMustard.Utils.Editor` | `LowDefMustard.Utils.Editor` | Editor only | `LowDefMustard.Utils` |

## Contents

### Data Structures & Extensions (`Runtime/DataStructuresExtensions`)

- **`LazyValue<T>` / `ReInitLazyValue<T>`** — Deferred-initialization wrapper. 
  - `LazyValue<T>` runs its initializer delegate once, on first access. 
  - `ReInitLazyValue<T>` re-runs the initializer whenever the cached value is `null`, even after the first init (useful for values that can be destroyed/unloaded, e.g. Unity objects).
- **`CircularBuffer<T>`** — Fixed-size FIFO buffer backed by a `LinkedList<T>`; oldest entry drops off once capacity is reached.
- **`EnumLookup<TEnum, TValue>` / `EnumLookupBase<TValue>`** (`Runtime/DataStructuresExtensions/EnumLookup`) — Dictionary-like lookup keyed by a specific enum type, exposed through a non-generic-friendly abstract base so callers can hold a reference without knowing the enum type.
- **`EnumKeyedCollection<TEnum, TData>` / `IEnumKeyedCollection`** (`Runtime/DataStructuresExtensions/EnumKeyedCollection`) — Serializable alternative to a `Dictionary<TEnum, TData>` field (which Unity can't serialize natively), guaranteeing exactly one `TData` entry per `TEnum` value. 
  - Backed by a `List<Entry>` of explicit `(TEnum key, TData value)` pairs with a lazily-built `Dictionary<TEnum, TData>` read cache. 
  - Paired with the `[EnumKeyedCollection]` attribute/drawer below, which calls `SyncEntriesToEnum()` to force-enumerate every member in the inspector.
- **`EnumExtensions.NextClamped<T>()`** — Returns the next value in an enum's declaration order, clamped to the last value (no wraparound).
- **`ListExtensions.Shuffle<T>()`** — In-place Fisher–Yates shuffle for any `IList<T>`.
- **`ApproximateFloatComparer`** — `IEqualityComparer<float>` that treats floats as equal within a configurable tolerance (default `0.005`); handles `NaN`/`±Infinity`.
- **`SmartVector2.CheckDistance(...)`** — Squared-distance threshold check between two `Vector2`s (avoids a `sqrt`), with an overload that also returns the squared delta.
- **`SerializableVector2` / `SerializableVector3`** — Plain serializable float-based mirrors of Unity's vector types, for contexts where you need `[Serializable]` fields without the `Vector2`/`Vector3` overhead.
- **`SerializablePolygon`** — Serializable list of `Vector2` points.
- **`ChoiceActionPair`** — Simple `(string choice, Action action)` struct pairing a label with a callback (e.g. for building dialogue/menu choices).
- **`IStandardGraphNode`** — Interface for node-graph elements (`ScriptableObject`, position getter/setter) used by the `StandardNodeDragManipulator` editor tool below.

### Custom Attribute Drawers (`Runtime/CustomAttributesDrawers` + `Editor/CustomAttributesDrawers`)

- **`[ReadOnly]`** — Marks an inspector field as visible but non-editable.
- **`[RestrictedEnum(params int[] hiddenValues)]`** — Hides specific enum values from the inspector popup for a field, without removing them from the underlying enum.
- **`[EnumKeyedCollection]`** — Applied to a field typed as an `EnumKeyedCollection<TEnum, TData>` (via `IEnumKeyedCollection`). 
  - Before rendering, the drawer calls `SyncEntriesToEnum()` on the boxed value to reconcile the backing list against the enum's current members (adding missing keys, dropping orphaned keys, sorting), and writes that back into the `SerializedProperty` — so no enum value can be left silently unset.

### Predicates (`Runtime/Predicates`)

A small serializable boolean-expression system for data-driven conditions (e.g. quest/dialogue gating):

- **`Predicate`** — Abstract `ScriptableObject` base; concrete predicates are authored as assets.
- **`IPredicateEvaluator`** — Implemented by whatever runtime system knows how to evaluate a given `Predicate`, returning a nullable bool.
- **`Condition`** — Serializable AND-of-ORs (conjunction of disjunctions) over `Predicate` references, each optionally negated. `Condition.Check(evaluators)` runs every predicate against all supplied evaluators.

### Probability (`Runtime/Probability`)

- **`IObjectProbabilityPair<T>`** — Interface for an object + integer-weight pair.
- **`ProbabilityPairOperation<T>.GetRandomObject(...)`** — Weighted-random selection over a collection of `IObjectProbabilityPair<T>`.

### Addressables (`Runtime/Addressables`)

- **`IAddressablesCache`** — Convention interface for Addressables-backed caches.

### Editor State Check (`Runtime/EditorStateCheck.cs`)

- **`EditorStateCheck.IsStandardEditorState(GameObject)`** — Guards editor-only callbacks (e.g. `OnValidate`) against firing during play mode transitions, domain reload/compilation, prefab editing, preview scenes, or on persistent (non-scene) assets.

### Standard Editor Tools (`Editor/StandardEditorTools`)

Reusable UI Toolkit building blocks for custom node/graph editor windows:

- **`StandardBackgroundLayer`** — `VisualElement` that renders an infinite dot or line grid background (`StandardBackgroundType`).
- **`StandardCanvasPanManipulator`** — Left/middle mouse-drag panning for a canvas element.
- **`StandardCanvasZoomManipulator`** — Scroll-wheel zoom (clamped 0.25×–2×) that zooms toward the cursor.
- **`StandardNodeDragManipulator`** — Drag-to-move for graph nodes implementing `IStandardGraphNode`; clicks below a movement threshold select the node's backing asset instead of dragging.

### Sprite Animation Generator (`Editor/SpriteAnimationGenerator`)

**Menu:** `Tools > Sprite Animation Generator`

An `EditorWindow` that batch-generates `AnimationClip` assets from a folder of sprite sheets, matching a `[CharacterName] - [Action] - [Frame#].png` filename pattern.

- Detects movement directions (`Down`/`Front`, `Up`/`Back`, `Left`, `Right`, and the four diagonals) via configurable aliasing.
- For every recognized direction, also generates an **Idle** clip — using dedicated `Idle` frames if present, falling back to the `Down` frames, falling back to a single frame.
- Generates one **StandStill** clip per character — using `Static` frames if present, otherwise the character's first `Down` frame.
- Optionally links generated clips into an `AnimatorOverrideController` slot, matching by direction/idle/stand-still naming conventions (`OverrideDirectionLookup`, `OverrideConfiguration`).
- Unrecognized action tokens are passed through as-is and flagged in the log so naming issues in source art are easy to spot.
- Frame rate is read from a reference clip and independently configurable for idle/stand-still clips.

## Design Notes

- The Predicates system intentionally avoids generics at the `Predicate` base level to keep evaluator wiring simple; concrete predicate types are expected to define their own typed `Evaluate` overloads.
- `EnumKeyedCollection<TEnum, TData>` stores an explicit key per entry (rather than relying on list position) specifically so the guaranteed-complete, force-enumerated inspector UX doesn't come at the cost of silent data corruption if an enum gets reordered or extended in the middle.
- `EnumKeyedCollectionDrawer`'s reconciliation (`SyncEntriesToEnum`) assumes a plain (non-`[Flags]`) enum, matching the one-entry-per-member model. Using it with a `[Flags]` enum isn't guarded against and isn't a supported configuration.

## Tests

Tests use Unity's built-in Test Framework (Edit Mode + Play Mode, via NUnit), run manually through the Test Runner.

| Assembly                             | Root Namespace                       | Platform                     |
|--------------------------------------|--------------------------------------|------------------------------|
| `LowDefMustard.Utils.Tests.Editor`   | `LowDefMustard.Utils.Tests.Editor`   | Editor                       |
| `LowDefMustard.Utils.Tests.PlayMode` | `LowDefMustard.Utils.Tests.PlayMode` | All (Editor-run in practice) |

### Coverage at a glance

| Category                       | Types | Tested | Notes                                                                                 |
|--------------------------------|:-----:|:------:|---------------------------------------------------------------------------------------|
| Data Structures & Extensions   | 16    |   15   | `IStandardGraphNode` has no concrete implementation in this package to test against   |
| Custom Attribute Drawers       | 3     |   3†   | `[RestrictedEnum(...)]`'s live interaction and prefab-override visuals aren't covered |
| Predicates                     | 3     |   3*   | `Predicate`/`IPredicateEvaluator` only exercised indirectly                           |
| Probability                    | 2     |   2*   | `IObjectProbabilityPair<T>` only exercised indirectly                                 |
| Addressables                   | 1     |   0    | `IAddressablesCache` has no concrete implementation in this package to test against   |
| Editor State Check             | 1     |   1†   | Partial — 6 of 8 internal guard clauses are reachable                                 |
| Standard Editor Tools          | 4     |   4†   | `StandardBackgroundLayer`'s actual pixel rendering isn't reachable                    |
| Sprite Animation Generator     | 8     |   6    | `OverrideDirectionLookup` and the full `Generate()` pipeline aren't covered           |

\* Interface covered only via a test double standing in for a real implementation, not a concrete type of its own

† Partial coverage — see the relevant row(s) in **Detail by type** below for what is / isn't reachable

### Detail by type

| Type                                                   | Tested?  | Test file(s)                                                   | Notes                                                                                                                                                |
|--------------------------------------------------------|----------|----------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LazyValue<T>`                                         | Yes      | `LazyValueTests.cs`                                            |                                                                                                                                                      |
| `ReInitLazyValue<T>`                                   | Yes      | `LazyValueTests.cs`, `LazyValueLifecycleTests.cs`              | Play Mode coverage exercises it against a real destroyed `UnityEngine.Object` reference                                                              |
| `CircularBuffer<T>`                                    | Yes      | `CircularBufferTests.cs`                                       |                                                                                                                                                      |
| `EnumLookup<TEnum, TValue>` / `EnumLookupBase<TValue>` | Yes      | `EnumLookupTests.cs`                                           |                                                                                                                                                      |
| `EnumKeyedCollection<TEnum, TData>`                    | Yes      | `EnumKeyedCollectionTests.cs`                                  | `SyncEntriesToEnum`'s orphaned-entry removal isn't covered — awkward to reach without reflection                                                     |
| `EnumExtensions.NextClamped<T>()`                      | Yes      | `EnumExtensionsTests.cs`                                       |                                                                                                                                                      |
| `ListExtensions.Shuffle<T>()`                          | Yes      | `ListExtensionsTests.cs`                                       |                                                                                                                                                      |
| `ApproximateFloatComparer`                             | Yes      | `ApproximateFloatComparerTests.cs`                             |                                                                                                                                                      |
| `SmartVector2.CheckDistance(...)`                      | Yes      | `SmartVector2Tests.cs`                                         |                                                                                                                                                      |
| `SerializableVector2` / `SerializableVector3`          | Yes      | `SerializableDataTests.cs`                                     |                                                                                                                                                      |
| `SerializablePolygon`                                  | Yes      | `SerializableDataTests.cs`                                     |                                                                                                                                                      |
| `ChoiceActionPair`                                     | Yes      | `ChoiceActionPairTests.cs`                                     |                                                                                                                                                      |
| `IStandardGraphNode`                                   | No       | —                                                              | No concrete implementation                                                                                                                           |
| `[ReadOnly]`                                           | Yes      | `ReadOnlyDrawerTests.cs`                                       |                                                                                                                                                      |
| `[RestrictedEnum(...)]`                                | Partial  | `RestrictedEnumDrawerTests.cs`                                 | `RegisterValueChangedCallback`/`TrackPropertyValue` write-back & prefab-override not covered (requires UI Toolkit panel or prefab instance) |
| `[EnumKeyedCollection]` drawer                         | Yes      | `EnumKeyedCollectionDrawerTests.cs`                            |                                                                                                                                                      |
| `Predicate`                                            | Indirect | `ConditionTests.cs` (via `TestPredicate` stand-in)             | Abstract with no members of its own to test directly                                                                                                 |
| `IPredicateEvaluator`                                  | Indirect | `ConditionTests.cs` (via test-double evaluators)               | No concrete implementation                                                                                                                           |
| `Condition` / `Disjunction` / `PredicateWrapper`       | Yes      | `ConditionTests.cs`                                            | See `Runtime/Predicates/README.md` for the full CNF/evaluation semantics this suite covers                                                           |
| `IObjectProbabilityPair<T>`                            | Indirect | `ProbabilityPairOperationTests.cs` (via a test double)         | No concrete implementation                                                                                                                           |
| `ProbabilityPairOperation<T>.GetRandomObject(...)`     | Yes      | `ProbabilityPairOperationTests.cs`                             |                                                                                                                                                      |
| `IAddressablesCache`                                   | No       | —                                                              | No concrete implementation                                                                                                                           |
| `EditorStateCheck.IsStandardEditorState(...)`          | Partial  | `EditorStateCheckTests.cs`, `EditorStateCheckPlayModeTests.cs` | 6 of 8 internal guards covered; `isCompiling`/`isUpdating` and the play-mode transition window aren't reliably testable                              |
| `StandardBackgroundLayer`                              | Partial  | `StandardBackgroundLayerTests.cs`                              | The `Painter2D` drawing isn't testable — `MeshGenerationContext` has no public constructor                                                           |
| `StandardCanvasPanManipulator`                         | Yes      | `StandardCanvasPanManipulatorTests.cs`                         |                                                                                                                                                      |
| `StandardCanvasZoomManipulator`                        | Yes      | `StandardCanvasZoomManipulatorTests.cs`                        |                                                                                                                                                      |
| `StandardNodeDragManipulator`                          | Yes      | `StandardNodeDragManipulatorTests.cs`                          |                                                                                                                                                      |
| `SpriteAnimationGenerator` (bits below)                |          |                                                                |                                                                                                                                                      | 
| ^-`.ClassifyAction(...)`                               | Yes      | `ClassifyActionTests.cs`                                       | 
| ^-`.OverrideConfiguration.IsOverrideMatch(...)`        | Yes      | `OverrideConfigurationIsOverrideMatchTests.cs`                 |                                                                                                                                                      |
| ^-`.Generate()` / `ProcessNextBuildStep()`             | No       | —                                                              | Out of scope (requires sprite asset files on disk matching the filename patterns, pumping the deferred `EditorApplication.update` build queue. etc.) |
| ^-`AnimationBuildLog`                                  | Yes      | `AnimationBuildLogTests.cs`                                    |                                                                                                                                                      |
| ^-`{x}AnimationData`                                   | Yes      | `AnimationDataTests.cs`                                        |                                                                                                                                                      |
| ^-`OverrideDirectionLookup`                            | No       | —                                                              | Out of scope (requires `AnimatorController` + `AnimatorOverrideController` pair to construct meaningfully                                            |


## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
