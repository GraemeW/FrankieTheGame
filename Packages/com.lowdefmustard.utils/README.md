# Low Def Mustard Utils

General-purpose data structures, editor tooling, and attribute drawers shared across Low Def Mustard Games projects.

- **Package name:** `com.lowdefmustard.utils`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** None

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.utils": "file:../path/to/com.lowdefmustard.utils"` in your project's `manifest.json`.

## Assembly Structure

| Assembly | Root Namespace | Platform | References |
|---|---|---|---|
| `LowDefMustard.Utils` | `LowDefMustard.Utils` | Runtime | — |
| `LowDefMustard.Utils.Editor` | `LowDefMustard.Utils.Editor` | Editor only | `LowDefMustard.Utils` |

## Contents

### Data Structures & Extensions (`Runtime/DataStructuresExtensions`)

- **`LazyValue<T>` / `ReInitLazyValue<T>`** — Deferred-initialization wrapper. `LazyValue<T>` runs its initializer delegate once, on first access. `ReInitLazyValue<T>` re-runs the initializer whenever the cached value is `null`, even after the first init (useful for values that can be destroyed/unloaded, e.g. Unity objects).
- **`CircularBuffer<T>`** — Fixed-size FIFO buffer backed by a `LinkedList<T>`; oldest entry drops off once capacity is reached.
- **`EnumLookup<TEnum, TValue>` / `EnumLookupBase<TValue>`** — Dictionary-like lookup keyed by a specific enum type, exposed through a non-generic-friendly abstract base so callers can hold a reference without knowing the enum type.
- **`EnumExtensions.NextClamped<T>()`** — Returns the next value in an enum's declaration order, clamped to the last value (no wraparound).
- **`ListExtensions.Shuffle<T>()`** — In-place Fisher–Yates shuffle for any `IList<T>`.
- **`ApproximateFloatComparer`** — `IEqualityComparer<float>` that treats floats as equal within a configurable tolerance (default `0.005`); handles `NaN`/`±Infinity`.
- **`SmartVector2.CheckDistance(...)`** — Squared-distance threshold check between two `Vector2`s (avoids a `sqrt`), with an overload that also returns the squared delta.
- **`SerializableVector2` / `SerializableVector3`** — Plain serializable float-based mirrors of Unity's vector types, for contexts where you need `[Serializable]` fields without the `Vector2`/`Vector3` overhead or precision surprises.
- **`SerializablePolygon`** — Serializable list of `Vector2` points.
- **`ChoiceActionPair`** — Simple `(string choice, Action action)` struct pairing a label with a callback (e.g. for building dialogue/menu choices).
- **`IStandardGraphNode`** — Interface for node-graph elements (`ScriptableObject`, position getter/setter) used by the `StandardNodeDragManipulator` editor tool below.

### Custom Attribute Drawers (`Runtime/CustomAttributesDrawers` + `Editor/CustomAttributesDrawers`)

- **`[ReadOnly]`** — Marks an inspector field as visible but non-editable.
- **`[RestrictedEnum(params int[] hiddenValues)]`** — Hides specific enum values from the inspector popup for a field, without removing them from the underlying enum. Falls back to a `HelpBox` error if applied to a non-enum field.

### Predicates (`Runtime/Predicates`)

A small serializable boolean-expression system for data-driven conditions (e.g. quest/dialogue gating):

- **`Predicate`** — Abstract `ScriptableObject` base; concrete predicates are authored as assets.
- **`IPredicateEvaluator`** — Implemented by whatever runtime system knows how to evaluate a given `Predicate`, returning a nullable bool.
- **`Condition`** — Serializable AND-of-ORs (conjunction of disjunctions) over `Predicate` references, each optionally negated. `Condition.Check(evaluators)` runs every predicate against all supplied evaluators.

### Probability (`Runtime/Probability`)

- **`IObjectProbabilityPair<T>`** — Interface for an object + integer-weight pair.
- **`ProbabilityPairOperation<T>.GetRandomObject(...)`** — Weighted-random selection over a collection of `IObjectProbabilityPair<T>`.

### Addressables (`Runtime/Addressables`)

- **`IAddressablesCache`** — Convention interface for Addressables-backed caches (default label strategy: label name == implementing class name).

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
- Frame rate is read from a reference clip and independently configurable for idle/stand-still clips.

## Notes / Known Limitations

- The Predicates system intentionally avoids generics at the `Predicate` base level to keep evaluator wiring simple; concrete predicate types are expected to define their own typed `Evaluate` overloads.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
