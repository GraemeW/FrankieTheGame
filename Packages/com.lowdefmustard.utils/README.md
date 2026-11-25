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

- **`LazyValue<T>` / `ReInitLazyValue<T>`** — Deferred-initialization wrapper
  - `LazyValue<T>` runs its initializer delegate once, on first access
  - `ReInitLazyValue<T>` re-runs the initializer whenever the cached value is `null`, even after the first init (useful for values that can be destroyed/unloaded, e.g. Unity objects)
- **`CircularBuffer<T>`** — Fixed-size FIFO buffer backed by a `LinkedList<T>`; oldest entry drops off once capacity is reached
- **`EnumLookup<TEnum, TValue>` / `EnumLookupBase<TValue>`** (`Runtime/DataStructuresExtensions/EnumLookup`) — Dictionary-like lookup keyed by a specific enum type, exposed through a non-generic-friendly abstract base so callers can hold a reference without knowing the enum type
- **`EnumKeyedCollection<TEnum, TData>` / `IEnumKeyedCollection`** (`Runtime/DataStructuresExtensions/EnumKeyedCollection`) — Serializable alternative to a `Dictionary<TEnum, TData>` field (which Unity can't serialize natively), guaranteeing exactly one `TData` entry per `TEnum` value 
  - Backed by a `List<Entry>` of explicit `(TEnum key, TData value)` pairs with a lazily-built `Dictionary<TEnum, TData>` read cache 
  - Paired with the `[EnumKeyedCollection]` attribute/drawer below, which calls `SyncEntriesToEnum()` to force-enumerate every member in the inspector
- **`EnumExtensions.NextClamped<T>()`** — Returns the next value in an enum's declaration order, clamped to the last value (no wraparound)
- **`ListExtensions.Shuffle<T>()`** — In-place Fisher–Yates shuffle for any `IList<T>`
- **`ApproximateFloatComparer`** — `IEqualityComparer<float>` that treats floats as equal within a configurable tolerance (default `0.005`); handles `NaN`/`±Infinity`
- **`StableHash`** — Deterministic FNV-1a string hash (`GetOffsetBasis()` seed, chained `Add(hash, value)`), identical across sessions, platforms and runtimes; use it for any persisted hash, since `System.HashCode` is seeded per domain and `string.GetHashCode` is randomized on CoreCLR
- **`SmartVector2.CheckDistance(...)`** — Squared-distance threshold check between two `Vector2`s (avoids a `sqrt`), with an overload that also returns the squared delta
- **`SerializableVector2` / `SerializableVector3`** — Plain serializable float-based mirrors of Unity's vector types, for contexts where you need `[Serializable]` fields without the `Vector2`/`Vector3` overhead
- **`SerializablePolygon`** — Serializable list of `Vector2` points
- **`ChoiceActionPair`** — Simple `(string choice, Action action)` struct pairing a label with a callback (e.g. for building dialogue/menu choices)
- **`IStandardGraphNode`** — Interface for node-graph elements (`ScriptableObject`, position getter/setter) used by the `StandardNodeDragManipulator` editor tool below

### Custom Attribute Drawers (`Runtime/CustomAttributesDrawers` + `Editor/CustomAttributesDrawers`)

- **`[ReadOnly]`** — Marks an inspector field as visible but non-editable
- **`[RestrictedEnum(params int[] hiddenValues)]`** — Hides specific enum values from the inspector popup for a field, without removing them from the underlying enum
  - Honours a `PropertyField`'s label override, and lines up with the inspector's other fields
  - Attribute arguments must be constants:  to hide a list built at runtime, subclass it (e.g. Frankie's `SkillStatAttribute` / `ModifiableStatAttribute`)
- **`[ProbabilityWeight(min)]`** — On the `int` weight of an `IObjectProbabilityPair<T>` element in a list or array: shows each element's share of the total weight beside it (e.g. `25%`)
  - An empty value keeps its share (shown in italics), matching `TryGetRandom`:  rolls that land on it pick nothing
    - Each element is read as its pair (`boxedValue`), so "empty" is the roll's own rule (`ProbabilityPairExtensions.IsEmpty`:  null or destroyed)
    - `isEmptyValueSkipped: true` flags empty entries as `never picked` instead ~ e.g. for owners that filter them out before rolling
  - `min` replaces `[Min]` (a field only gets one drawer); the field is a bound `IntegerField`, so prefab overrides behave as normal
  - The drawer builds a `ProbabilityWeightField` (`Editor/CustomAttributesDrawers`), which custom editors can use directly without a label
- **`[EnumKeyedCollection]`** — Applied to a field typed as an `EnumKeyedCollection<TEnum, TData>` (via `IEnumKeyedCollection`)
  - Before rendering, the drawer calls `SyncEntriesToEnum()` on the boxed value to reconcile the backing list against the enum's current members (adding missing keys, dropping orphaned keys, sorting), and writes that back into the `SerializedProperty`
  - this process ensures no enum value can be left silently unset

### Predicates (`Runtime/Predicates`)

A small serializable boolean-expression system for data-driven conditions (e.g. quest/dialogue gating):

- **`Predicate`** — Abstract `ScriptableObject` base; concrete predicates are authored as assets
- **`IPredicateEvaluator`** — Implemented by whatever runtime system knows how to evaluate a given `Predicate`, returning a nullable bool
- **`Condition`** — Serializable AND-of-ORs (conjunction of disjunctions) over `Predicate` references, each optionally negated. `Condition.Check(evaluators)` runs every predicate against all supplied evaluators

### Probability (`Runtime/Probability`)

- **`IObjectProbabilityPair<out T>`** — Interface for a `value` + relative integer `weight` pair (covariant ~ a list of derived-value pairs rolls as its base type)
- **`ProbabilityPairExtensions.TryGetRandom(...)`** — Weighted-random pick over a collection of `IObjectProbabilityPair<T>`, or several picks without repeats (`TryGetRandom(count, out results)`)
  - an entry with a null or destroyed value keeps its weight:  landing on it picks nothing (returns `false`) ~ filter empties out first to never land on them
  - `TryGetRandom(count, ...)` never fills a pick entirely with an empty value (i.e. landing on one empty takes all other empties out of the draw)
  - returns `false` when nothing is usable

### Addressables (`Runtime/Addressables` + `Editor/Addressables`)

- **`IAddressablesCache`** — Marker interface for the type that owns an Addressables-backed cache (label == class name by default)
  - Contract:  one public static `IAddressablesCacheLifecycle` property, backed by an `[AutoStaticsCleanup]` `AddressablesLookupCache<T>` field
  - Note:  since we don't have static abstract members until ≥C# 11 (Unity 7 / CoreCLR) ~ `AddressablesCacheOwners` finds the cache by reflection, and tests enforce the contract on every owner (TBU pending)
- **`AddressablesLookupCache<T>`** — The cache itself:  every asset under one label, keyed by ID (+ an optional secondary key, e.g. `Zone` by scene name)
  - On Cache Building:
    - `StartBuild()` loads async (e.g. behind a splash screen)
    - lookups (`Get` / `GetBySecondaryKey` / `GetAll`) complete synchronously on demand
  - Notable Behaviour:
    - logs duplicate IDs + the build result (`AddressablesCacheLog`)
    - an asset with a blank ID warns + is skipped, while the rest still load
    - will never block during an Addressables scene load (`AddressablesSceneLoadState`) ~ `WaitForCompletion` during a scene load deadlocks the scene load in builds, so log an error instead
  - `Seed(...)` replaces the lookups directly for tests
- **`IAddressablesCacheLifecycle`** — external (outside cache) visibility ~ `StartBuild`, `BuildIfEmpty`, `IsReady`, `Release`, `Reset` (release + drop the lookups so the next lookup rebuilds)
- **`AddressablesCacheOwners`** (editor) — Finds `IAddressablesCache` owners (`GetOwnerTypes`) and their caches (`GetCache`)
- **`AddressablesCacheInvalidator`** (editor, `AssetPostprocessor`) — Keeps caches current in edit mode ~ they'd otherwise only rebuild on a domain reload
  - an imported, moved or deleted asset resets the cache owned by its type (specifically, the type that introduces `IAddressablesCache`, e.g. `InventoryItem` for a `KeyItem`)
  - skipped in Play mode ~ `[AutoStaticsCleanup]` already resets the caches on entry
- **`DeletedAssetTypes`** (editor, `Editor/AssetManagement`) — implements `AssetModificationProcessor` to record the main asset types of assets deleted inside the editor just before deletion 
  - `GetTypes(deletedPaths, out hasUnrecordedAssetDeletion)` lets postprocessors react to deletions by type
  - records only live until the next editor update ~ the deletion's postprocessors run synchronously within the delete
- **`AddressablesCacheLog`** — Shared build/duplicate cache log messages

### Asset Script Swap (`Editor/AssetManagement`)

- **`AssetScriptSwapper.SwapScriptInPlace<TTarget>(source)`** — Rewrites a persistent object's own `m_Script` to `TTarget`'s script (same file, same GUID/fileID), then force-reimports it
  - Works on `ScriptableObject` assets and `MonoBehaviour`s on prefab assets && throws for scene/non-persistent objects or a `ScriptableObject` <-> `MonoBehaviour` swap
  - Fields carry over by name ~ unmatched fields take their defaults and leftover YAML is dropped on the next save, so it's safest when `TTarget` derives from the source type
  - The in-memory `source` / other already-loaded objects can hold stale references until reloaded (must reload from the path)

### Serialized Property Extensions (`Editor/SerializedProperties`)

- **`property.FindParentProperty()`** — The owning property via `SerializedProperty.Parent()` && `null` for a top-level field
  - A list element's parent is the list's `.Array` node, which works as the list (`arraySize`, `GetArrayElementAtIndex`)
- **`property.FindSiblingProperty(name)`** — The field next to this one in the same owner (top-level fields look up on the `SerializedObject`)
- **`property.TryFindOwningArray(out array, out index, out relativePath)`** — The innermost list holding a field, its element index (matched w/ `SerializedProperty.EqualContents`) and the field's path inside the element (`inner.weight`)
  - returns false outside a list or for the element itself
- **`property.IsStringArray()`** — True for a `string[]` / `List<string>` property (not a `string` field itself)

### Serialized Field Utility (`Editor/SerializedProperties`)

Reflection mirror of Unity's serialization rules ~ for scanning types without a `SerializedObject` (e.g. which asset types can hold a given field)

- **`SerializedFieldUtility.GetSerializedFields(type)`** — The fields Unity serializes on a type, incl. private `[SerializeField]`s of base classes (stops at `UnityEngine.Object`)
- **`SerializedFieldUtility.IsSerialized(field)`** — Public or `[SerializeField]` / `[SerializeReference]` && not static, readonly, const or `[NonSerialized]`
- **`SerializedFieldUtility.GetCollectionElementType(fieldType)`** — `T[]` / `List<T>` -> `T`;  any other type as-is
- **`SerializedFieldUtility.IsInlineSerializable(type)`** — A `[Serializable]` class / struct stored inside its owner
  - false for primitives, enums, strings, `UnityEngine.Object` references, abstract types and core library types (e.g. `Dictionary`)

### Editor State Check (`Runtime/EditorStateCheck.cs`)

- **`EditorStateCheck.IsStandardEditorState(GameObject)`** — Guards editor-only callbacks (e.g. `OnValidate`) against firing during play mode transitions, domain reload/compilation, prefab editing, preview scenes, or on persistent (non-scene) assets

### Standard Editor Tools (`Editor/StandardEditorTools`)

Reusable UI Toolkit building blocks for custom editor windows (node/graph canvases, theming, inline editing):

- **`StandardBrownTheme`** (+ `StandardBrownTheme.uss`) — Warm brown theme for an editor window: a palette of `--theme-*` USS variables, plus restyled built-in controls (fields, dropdowns, buttons, toggles, sliders, scrollbars, lists/trees, toolbars)
  - `StandardBrownTheme.Apply(root, styleSheets...)` adds the stylesheet(s) and the theme class to a window's root
  - Add `StandardBrownTheme.darkClass` to a section (e.g. a side panel) for a darker variant ~ shared element classes (`canvasClass`, `panelClass`, `panelHeaderClass`, `groupHeaderClass`, `dimClass`, `rowClass`, `dropTargetClass`) cover common window parts
  - Windows reference the stylesheet through a serialized `StyleSheet` field set as their script's default reference (Inspector → script → Default References)
    - use `GetDefaultReference(window, fieldName)` to read it back for windows serialized in the editor layout before the reference existed
  - Palette is also exposed as `Color`s (`accent`, `darkAccent`, `text`, ...) for code that can't read USS variables
- **`StandardBackgroundLayer`** — `VisualElement` that renders an infinite dot or line grid background (`StandardBackgroundType`)
- **`StandardCanvasPanManipulator`** — Left/middle mouse-drag panning for a canvas element
- **`StandardCanvasZoomManipulator`** — Scroll-wheel zoom (clamped 0.25×–2×) that zooms toward the cursor
- **`StandardNodeDragManipulator`** — Drag-to-move for graph nodes implementing `IStandardGraphNode`; clicks below a movement threshold select the node's backing asset instead of dragging
- **`StandardObjectDragManipulator`** — Starts an editor drag (`DragAndDrop`) of an asset from any element once the pointer moves past a small threshold; clicks below it pass through (e.g. a row's ping)
- **`StandardObjectDropManipulator<T>`** — Accepts a dragged single `T` onto an element, e.g. to fill a text field with an asset's name
  - `canAccept` refuses some `T`s (rejected cursor, never accepted then discarded); drags without any `T` pass through to the element's own handling
  - Optional `highlightClass` (default none) is added to the element while an acceptable drag hovers it, e.g. `StandardBrownTheme.dropTargetClass` in a Standard Brown window
- **`AutocompleteDropdown`** — Suggestion list shown under a `TextField` while typing; `Attach(field, options)` hooks up any number of fields to one shared dropdown
  - Pass the full set of options:  matching is case-insensitive, listing options that start with the typed text first && then options containing it
  - Add it to the window root:  it positions itself in that space, so it stays unscaled over zoomed canvases.
- **`AssetNameLabel`** — Label showing an asset's name ~ double-click swaps it for a text field that renames the asset file
- **`UndoChangeSnapshot`** — Lets a window skip undo / redo that didn't touch what it shows (`Undo.undoRedoPerformed` otherwise fires for every undo in the editor)
  - Critical Usage Notes:
    - Ensure to `Capture(objects)` after each rebuild (and after the window's own edits)
    - Watch the owner of any objects whose creation / deletion is undoable (its list changes too) ~ typically track child objects + prepend owner itself
  - `HasChanged()` in the undo handler is true when any captured object's dirty count moved or the object was destroyed
  - pending `Undo.RecordObject` edits are flushed into the baseline on capture

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

| Assembly                             | Root Namespace                       | Platform                |
|--------------------------------------|--------------------------------------|-------------------------|
| `LowDefMustard.Utils.Tests.Editor`   | `LowDefMustard.Utils.Tests.Editor`   | Editor                  |
| `LowDefMustard.Utils.Tests`          | `LowDefMustard.Utils.Tests`          | Unrestricted (PlayMode) |

Shared Test Doubles - live in `Tests/Runtime/TestDoubles/` (namespace `LowDefMustard.Utils.Tests`):
*  `TestPredicate`
* `FixedResultEvaluator`
* `FixedResultEvaluatorBehaviour`
* `PerPredicateEvaluator`
* `SwapSourceAsset` / `SwapTargetAsset`, `SwapSourceBehaviour` / `SwapTargetBehaviour`: base/derived pairs for `AssetScriptSwapper`
* `TestProbabilityPair<T>`: in-memory `IObjectProbabilityPair<T>` ~ `ListOf((value, weight)...)` builds a pair list

_Note that MonoBehaviour/ScriptableObject test doubles must live in their standalone as-named script (Unity only creates a usable script asset when class name matches file name)._

Shared Test Builders live in `Tests/Runtime/TestBuilders/` (same namespace):
* `ConditionBuilder`
* `EvaluatorBuilder`
* `TestObjectTracker`:
  * `Create<T>(name = null)`: a tracked ScriptableObject
  * `CreateGameObject(name, components...)` / `CreateComponent<T>(name)`: a tracked GameObject (or a new one's added component)
  * `Track(obj)` adopts any other `UnityEngine.Object` (e.g. a `new GameObject(...)`)
  * `DestroyAll()` in TearDown cleans them up (skipping any already destroyed)

Test utilities (`AssetTestUtility`, `SerializedFieldInjector`, `HeadlessEditorWindow`) live in `Tests/Runtime/TestUtilities/` (note: many pragma'd w/ `#if UNITY_EDITOR`):
* `TempAssetFolder`: creates a disposable `Assets/_TEMP_<Name>_SafeToDelete` folder for edit mode tests
  * deletes with file operations + `AssetDatabase.Refresh` by default ~ so e.g. `ILocalizable` assets don't trigger `LocalizationDeletionHandler` and wipe real localization entries
  * pass `deleteViaAssetDatabase: true` only when a test means to exercise deletion handlers
  * `Path(fileName)` / `CreateSubfolder(name)`: paths inside the folder
  * `CreateAsset<T>(assetName, subfolderName)`: creates a ScriptableObject saved as `<assetName>.asset` (subfolder optional, made w/ `CreateSubfolder` first)
* `SceneTestUtility`: saved empty scenes for edit mode tests
  * `CreateSavedScene(folder, sceneName, mode, populate)` (or `CreateSavedScene(scenePath, ...)`): `Single` replaces the open scenes, `Additive` needs a saved active scene first and hands the active slot back
    * `populate` runs before the save ~ e.g. to add objects saved with the scene
  * `ResetToEmptyScene()`: discards open scenes for a fresh empty one ~ call before disposing the folder
  * `CreatePreviewScene()` / `ClosePreviewScene(scene)`: unsaved preview scene ~ editor-only hooks
  * `CreateGameObjectInScene(scene, name)` / `CreateComponentInScene<T>(scene, name)`: any loaded scene ~ moves the object in before adding the component to ensure its hooks already see the target scene
* `TestWait.Until(condition, timeoutSeconds)`: yields updates until a condition holds or the timeout passes
  * it only times out ~ must be followed with the appropriate test assertion
* `SerializedFieldInjector.Set(target, fieldName, value)` / `SetArray(target, fieldName, values...)`: sets a private `[SerializeField]` object reference (or reference array/list) as the inspector would
  * `SetValue(target, fieldName, value)`: sets a whole `[Serializable]` plain-class/struct field via `boxedValue`
* `HeadlessEditorWindow`: a shown, off-screen `EditorWindow` so UI Toolkit events actually dispatch
  * `CreateOffscreenWindow()` / `ShowOffscreen(window)`: plain host window, or any `EditorWindow` subclass
  * `WaitUntilSettled(window)`: yield it before the first event in test (waits/asserts panel resolved)
  * `Settle()`: two ticks
  * `Close(window)`: releases any mouse capture && closes it (use in TearDown)
  * `SendMouseDown` / `SendMouseDrag` / `SendMouseUp` / `SendClick`: synthetic events ~ send down → drag → up gesture in one tick (i.e. since mouse capture is global editor state between ticks)
  * `SendSubmit`: presses a `Button` via `NavigationSubmitEvent` (no pointer position needed)
  * `BeginObjectDrag(objects...)` + `SendDragUpdated` / `SendDragPerform` / `SendDragLeave`: synthetic editor drags (`DragAndDrop.objectReferences` set without a live drag) ~ `EndObjectDrag()` in TearDown
  * `DropObjects(target, objects...)`: whole drop in one call (begin → updated → perform)
* `AddressablesTestCatalog.IgnoreUnlessAssetDatabaseCatalog()`: allows us to skip a test that may compare project assets with what Addressables loads, when on a built catalogue (i.e. Addressables -> Play Mode Script:  Use Existing Build)
  * TearDowns must tolerate a skipped SetUp (null fields)
* `TestAssemblyCheck.IsTestAssemblyType(type)`: true for types in a test assembly (`.Tests` / `.Tests.Editor` naming, or an NUnit reference)

### Coverage at a glance

| Category                       | Types | Tested | Notes                                                                                                      |
|--------------------------------|:-----:|:------:|------------------------------------------------------------------------------------------------------------|
| Data Structures & Extensions   | 17    |   16   | `IStandardGraphNode` has no concrete implementation in this package to test against                        |
| Custom Attribute Drawers       | 4     |   4†   | `[RestrictedEnum(...)]`'s live interaction and prefab-override visuals aren't covered                      |
| Predicates                     | 3     |   3*   | `Predicate`/`IPredicateEvaluator` only exercised indirectly                                                |
| Probability                    | 2     |   2*   | `IObjectProbabilityPair<T>` only exercised indirectly                                                      |
| Addressables                   | 2     |   1    | `AddressablesCacheInvalidator` covered (mapping, reset lookup, every real owner resettable)                |
| Asset Script Swap              | 1     |   1    | `ConfirmInPlace` is a modal dialogue only                                                                  |
| Serialized Property Extensions | 1     |   1    |                                                                                                            |
| Editor State Check             | 1     |   1†   | Partial — 6 of 8 internal guard clauses are reachable                                                      |
| Standard Editor Tools          | 10    |   10†  | `StandardBackgroundLayer`'s pixels aren't reachable; `StandardBrownTheme.uss` colours spot-checked         |
| Sprite Animation Generator     | 8     |   6    | `OverrideDirectionLookup` and the full `Generate()` pipeline aren't covered                                |

\* Interface covered only via a test double standing in for a real implementation, not a concrete type of its own

† Partial coverage — see the relevant row(s) in **Detail by type** below for what is / isn't reachable

### Detail by type

| Type                                                   | Tested?  | Test file(s)                                                  | Notes                                                                                                                                                |
|--------------------------------------------------------|----------|---------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LazyValue<T>`                                         | Yes      | `LazyValueTests.cs`                                           |                                                                                                                                                      |
| `ReInitLazyValue<T>`                                   | Yes      | `LazyValueTests.cs`, `LazyValueLifecycleTests.cs`             | Play Mode coverage exercises it against a real destroyed `UnityEngine.Object` reference                                                              |
| `CircularBuffer<T>`                                    | Yes      | `CircularBufferTests.cs`                                      |                                                                                                                                                      |
| `EnumLookup<TEnum, TValue>` / `EnumLookupBase<TValue>` | Yes      | `EnumLookupTests.cs`                                          |                                                                                                                                                      |
| `EnumKeyedCollection<TEnum, TData>`                    | Yes      | `EnumKeyedCollectionTests.cs`                                 | `SyncEntriesToEnum`'s orphaned-entry removal isn't covered — awkward to reach without reflection                                                     |
| `EnumExtensions.NextClamped<T>()`                      | Yes      | `EnumExtensionsTests.cs`                                      |                                                                                                                                                      |
| `ListExtensions.Shuffle<T>()`                          | Yes      | `ListExtensionsTests.cs`                                      |                                                                                                                                                      |
| `ApproximateFloatComparer`                             | Yes      | `ApproximateFloatComparerTests.cs`                            |                                                                                                                                                      |
| `StableHash`                                           | Yes      | `StableHashTests.cs`                                          |                                                                                                                                                      |
| `SmartVector2.CheckDistance(...)`                      | Yes      | `SmartVector2Tests.cs`                                        |                                                                                                                                                      |
| `SerializableVector2` / `SerializableVector3`          | Yes      | `SerializableDataTests.cs`                                    |                                                                                                                                                      |
| `SerializablePolygon`                                  | Yes      | `SerializableDataTests.cs`                                    |                                                                                                                                                      |
| `ChoiceActionPair`                                     | Yes      | `ChoiceActionPairTests.cs`                                    |                                                                                                                                                      |
| `IStandardGraphNode`                                   | No       | —                                                             | No concrete implementation                                                                                                                           |
| `[ReadOnly]`                                           | Yes      | `ReadOnlyDrawerTests.cs`                                      |                                                                                                                                                      |
| `[ProbabilityWeight]`                                  | Partial  | `ProbabilityWeightDrawerTests.cs`                             | Prefab override bar / context menu come from the bound `IntegerField` (not checked here)                                                             |
| `[RestrictedEnum(...)]`                                | Partial  | `RestrictedEnumDrawerTests.cs`                                | `RegisterValueChangedCallback`/`TrackPropertyValue` write-back & prefab-override not covered (requires UI Toolkit panel or prefab instance)          |
| `[EnumKeyedCollection]` drawer                         | Yes      | `EnumKeyedCollectionDrawerTests.cs`                           |                                                                                                                                                      |
| `Predicate`                                            | Indirect | `ConditionTests.cs` (via `TestPredicate` stand-in)            | Abstract with no members of its own to test directly                                                                                                 |
| `IPredicateEvaluator`                                  | Indirect | `ConditionTests.cs` (via test-double evaluators)              | No concrete implementation                                                                                                                           |
| `Condition` / `Disjunction` / `PredicateWrapper`       | Yes      | `ConditionTests.cs`                                           | See `Runtime/Predicates/README.md` for the full CNF/evaluation semantics this suite covers                                                           |
| `IObjectProbabilityPair<T>`                            | Indirect | `ProbabilityPairExtensionsTests.cs` (via a test double)       | No concrete implementation; covariance checked                                                                                                       |
| `ProbabilityPairExtensions.TryGetRandom(...)`          | Yes      | `ProbabilityPairExtensionsTests.cs`                           |                                                                                                                                                      |
| `IAddressablesCache`                                   | Indirect | `AddressablesCacheInvalidatorTests.cs`                        | Marker ~ contract is checked on every owner outside test assemblies                                                                                  |
| `AddressablesLookupCache<T>`                           | Partial  | `AddressablesLookupCacheTests.cs`                             | Load (`StartBuild` / `BuildIfEmpty` / `IsReady`) is covered by consumers' cache tests                                                                |
| `AddressablesCacheOwners`                              | Yes      | `AddressablesCacheInvalidatorTests.cs`                        |                                                                                                                                                      |
| `AddressablesCacheInvalidator`                         | Partial  | `AddressablesCacheInvalidatorTests.cs`                        | `OnPostprocessAllAssets` needs real imports ~ verified live (quest / key item reimports reset only their own cache)                                  |
| `AssetScriptSwapper`                                   | Yes      | `AssetScriptSwapperTests.cs`                                  | `ConfirmInPlace` (modal dialogue) skipped (not interesting)                                                                                          |
| `SerializedFieldUtility`                               | Partial  | `SerializedFieldUtilityTests.cs`                              | Unity's own edge cases (generic `[Serializable]` instances, depth limit) not mirrored                                                                |
| `SerializedPropertyExtensions`                         | Yes      | `SerializedPropertyExtensionsTests.cs`                        |                                                                                                                                                      |
| `EditorStateCheck.IsStandardEditorState(...)`          | Partial  | `EditorStateCheckTests.cs`, `EditorStateCheckRuntimeTests.cs` | `isCompiling`/`isUpdating` and the play-mode transition window aren't reliably testable                                                              |
| `StandardBackgroundLayer`                              | Partial  | `StandardBackgroundLayerTests.cs`                             | The `Painter2D` drawing isn't testable — `MeshGenerationContext` has no public constructor                                                           |
| `StandardCanvasPanManipulator`                         | Yes      | `StandardCanvasPanManipulatorTests.cs`                        |                                                                                                                                                      |
| `StandardCanvasZoomManipulator`                        | Yes      | `StandardCanvasZoomManipulatorTests.cs`                       |                                                                                                                                                      |
| `StandardNodeDragManipulator`                          | Yes      | `StandardNodeDragManipulatorTests.cs`                         |                                                                                                                                                      |
| `StandardObjectDragManipulator`                        | Partial  | `StandardObjectDragManipulatorTests.cs`                       | `DragAndDrop.StartDrag` begins a real editor drag, so it's checked by hand                                                                           |
| `StandardObjectDropManipulator<T>`                     | Yes      | `StandardObjectDropManipulatorTests.cs`                       | Drags are synthesized (`DragAndDrop.objectReferences` set without a live drag)                                                                       |
| `StandardBrownTheme`                                   | Partial  | `StandardBrownThemeTests.cs`                                  | USS colours spot-checked only (placeholder text, via resolved style in a shown window)                                                               |
| `AutocompleteDropdown`                                 | Partial  | `AutocompleteDropdownTests.cs`                                | Delayed hide on focus-out and on-screen positioning skipped                                                                                          |
| `AssetNameLabel`                                       | Yes      | `AssetNameLabelTests.cs`                                      | Focus-out is simulated (headless windows don't hold focus)                                                                                           |
| `UndoChangeSnapshot`                                   | Yes      | `UndoChangeSnapshotTests.cs`                                  | Asset deletion goes through the same path as a destroyed object                                                                                      |
| `SpriteAnimationGenerator` (bits below)                |          |                                                               |                                                                                                                                                      | 
| ^-`.ClassifyAction(...)`                               | Yes      | `ClassifyActionTests.cs`                                      |                                                                                                                                                      |
| ^-`.OverrideConfiguration.IsOverrideMatch(...)`        | Yes      | `OverrideConfigurationIsOverrideMatchTests.cs`                |                                                                                                                                                      |
| ^-`.Generate()` / `ProcessNextBuildStep()`             | No       | —                                                             | Out of scope (requires sprite asset files on disk matching the filename patterns, pumping the deferred `EditorApplication.update` build queue. etc.) |
| ^-`AnimationBuildLog`                                  | Yes      | `AnimationBuildLogTests.cs`                                   |                                                                                                                                                      |
| ^-`{x}AnimationData`                                   | Yes      | `AnimationDataTests.cs`                                       |                                                                                                                                                      |
| ^-`OverrideDirectionLookup`                            | No       | —                                                             | Out of scope (requires `AnimatorController` + `AnimatorOverrideController` pair to construct meaningfully                                            |


## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
