# Low Def Mustard Game State Modifiers

Self-maintaining linkage between `GameStateModifier` assets (project-defined game-state flag/toggle ScriptableObjects) and the in-scene handler `MonoBehaviour`s that reference them, with a custom inspector and automatic dangling-entry cleanup.

- **Package name:** `com.lowdefmustard.gamestatemodifiers`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** `com.lowdefmustard.utils` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.gamestatemodifiers": "file:../path/to/com.lowdefmustard.gamestatemodifiers"` in your project's `manifest.json`. `com.lowdefmustard.utils` must also be present.

## Assembly Structure

| Assembly                                  | Root Namespace                            | Platform    | References                         |
|-------------------------------------------|-------------------------------------------|-------------|------------------------------------|
| `LowDefMustard.GameStateModifiers`        | `LowDefMustard.GameStateModifiers`        | Runtime     | `LowDefMustard.Utils`              |
| `LowDefMustard.GameStateModifiers.Editor` | `LowDefMustard.GameStateModifiers.Editor` | Editor only | `LowDefMustard.GameStateModifiers` |

## Contents

### Core Data Model (`Runtime`)

- **`GameStateModifier`** (abstract `ScriptableObject`, `ISerializationCallbackReceiver`) — Base for a project-defined "game state flag" asset (e.g. a quest completion, a world event, a toggle) that other objects need to react to or be linked against
  - Auto-generates a stable `guid` on first serialize
  - Holds `linkedHandlers` — a `List<LinkedHandlerRecord>` reverse-index of every scene GameObject ("handler") currently referencing this modifier, maintained automatically by handlers rather than hand-authored (see `IGameStateModifierHandler` below)
    - `GameStateModifierEditor` renders this with its own custom UI in place of Unity's default list drawer
  - Editor-only static guid→asset cache (`GetGameStateModifier(guid)`), built lazily via `AssetDatabase.FindAssets`
    - misses return null without rebuilding
    - the cache is reset (`ResetGameStateModifierCache()`) when:
      - A. modifier assets are imported, moved or deleted (`GameStateModifierCacheInvalidator`, editor; deleted types via `DeletedAssetTypes` in `com.lowdefmustard.utils`, and an `.asset` deleted from outside the editor still resets)
      - B. when `OnBeforeSerialize` generates a new GUID
    - a hit on a destroyed entry rebuilds once
  - `AddOrUpdateGameStateModifierHandler` / `RemoveGameStateModifierHandler` / `CleanDanglingModifierHandlerData` — editor-only mutation API a handler calls into to keep this reverse-index in sync; the latter removes entries whose scene/object can no longer be found, or whose target `GameStateModifier` link has been severed, or that are exact duplicates
  - `ScenePathProvider` — optional static hook (`HasScenePathDelegate`) letting a consuming project supply its own zone-name → scene-path resolution (e.g. via `com.lowdefmustard.zones`'s `Zone` lookup) in place of the built-in `DefaultGetScenePath` (a plain `AssetDatabase.FindAssets($"{zoneName} t:Scene")` name search)
- **`IGameStateModifierHandler`** (interface, extends `ISerializationCallbackReceiver`) — Implemented by any `MonoBehaviour` that references one or more `GameStateModifier`s and needs that reference kept in sync with the modifier's reverse-index automatically
  - Configuration contract (see the extensive comment block at the top of the file before implementing):
      - Requires `[ExecuteInEditMode]` on the implementing class, and a call to the static `IGameStateModifierHandler.TriggerOnDestroy(this)` from its `OnDestroy()`
      - Requires explicit serialized backing fields for `handlerGUID` / `modifierListHashCheck` / `hasGameStateModifiers` / `gameStateModifierGUIDs` (default interface properties can't themselves carry `[SerializeField]` state)
      - `gameObject` is declared on the interface but deliberately left unimplemented — it is satisfied implicitly by `MonoBehaviour.gameObject`, which is why this interface can only be implemented by a `MonoBehaviour`-derived class, never a plain C# class
      - The one method an implementer must actually supply is `GetGameStateModifiers()` (which asset(s) this handler references); everything else is default-implemented
      - `OnBeforeSerialize()` (editor-only): builds a `LinkedHandlerRecord` for the handler's current scene/name/parent, hashes it together with its linked `GameStateModifier` GUIDs (`GetModifierListHashCheck` ~ skip redundant work if nothing changed), and finally reconciles
        - reconciliation involves updating/adding this handler's entry on every currently-linked `GameStateModifier` ++ cleaning its entry off any `GameStateModifier` that it *used* to reference (//no longer does)
        - note that the hash is persisted, so we use a deterministic FNV-1a over the raw characters (`LowDefMustard.Utils.StableHash`) 
      - `RecordHandlerModifications(handler)` — marks the handler dirty and, on a prefab instance, records its backing fields as prefab overrides
        - prefab override explicitly required, otherwise prefab instance are silently dropped on scene save
        - `OnBeforeSerialize()` defers this method call via `EditorApplication.delayCall` as it isn't safe to call from inside the serialization callback
        - likewise, batch tools that save the scene immediately (e.g. `ForceSerializeAllHandlers`) must call it directly after reconciling
      - `ReconcileGameStateModifiers(cleanDanglingModifierData = true)` — the body of `OnBeforeSerialize()` (hash check, then link/unlink); returns whether it reconciled
        - note that bulk tools generally pass `false` and clean every modifier once at the end instead (see `GameStateModifierHandlerLookup`)
      - `TriggerOnGizmos(this)` — optional scene-view gizmo (a star-in-circle, at the handler's `SpriteRenderer` bounds center if present) drawn when `hasGameStateModifiers` is true, for spotting linked objects visually while scene-editing
      - All reconciliation is guarded by `EditorStateCheck.IsStandardEditorState` (`com.lowdefmustard.utils`), so it doesn't fire during play mode, domain reload, prefab isolation, or on non-scene assets
- **`IGameStateModifierPredicate`** (interface) — Implemented by a `Predicate` (`com.lowdefmustard.utils`) whose evaluation depends on one or more `GameStateModifier`s ~ exposing them via `GetGameStateModifiers()`
  - `IGameStateModifierPredicate.CollectGameStateModifiers(predicates)` (static): the linked modifiers across a set of predicates (e.g. `Condition.GetPredicates()`), each once with nulls dropped ~ typically used as the body of a predicate-holding handler's `GetGameStateModifiers()`
- **`GameStateModifierHandlerLookup`** (editor-only) — Snapshot of which handler (per zone) links to which modifier GUIDs, built in one pass over the scenes
  - `GameStateModifier.CleanDanglingModifierHandlerData(removeOnSceneCheck, handlerLookup)` checks entries in the lookup's zones against it instead of opening their scene
  - entries for other zones fall back to the per-entry scene check
  - keyed internally by `ZoneHandlerKey` (zone name + handler GUID)
- **`LinkedHandlerRecord`** — Serializable struct (`zoneName`, `gameObjectName`, `parentObjectName`, `handlerGUID`, `handlerTypeName`): the modifier's record of one handler linking to it; equality is defined purely on `handlerGUID`. 
  - `handlerTypeName` is the handler's concrete class name (e.g. `AIConversant`), used to group entries in the inspector
  - one entry per handler, stored in the owning `GameStateModifier.linkedHandlers` list

### Editor Tooling (`Editor/GameStateModifierEditor.cs`, `Editor/GameStateModifierToolbar.cs`)

- **`Tools/GameStateModifiers/ForceSerializeAllHandlers`** — Opens each scene once, reconciles every handler, indexes it, saves the scene, then cleans every modifier once against the full lookup
- **`Tools/GameStateModifiers/CleanAllDanglingModifiers`** — Indexes each scene once (opened additively), then cleans every modifier against that lookup

- **`GameStateModifierEditor`** (`[CustomEditor(typeof(GameStateModifier), true)]`) — UI Toolkit custom inspector replacing the default list rendering of `linkedHandlers`
  - Linked handlers are grouped by `handlerTypeName` in one foldout per type (alphabetical, untyped entries last as "Unknown Handler Type")
    - note that entries keep their list index (sorting is visual representation only)
    - a lone group opens by default, otherwise groups start collapsed and remember their state per type for the editor session (`SessionState`)
  - includes a foldout of styled entry cards comprising: 
    - zone/object/GUID/handler-type fields
    - "Open & Select" button
    - per-entry delete button
    - "Remove Invalid Entries" button
  - Manual field editing is locked behind an explicit 🔓/🔒 toggle, with a standing warning that hand-edits here don't propagate back to the actual scene object — they're a local override, not a re-link
- **`OpenSceneAndActProvider`** — optional static hook (`Action<string, Action>`) letting a consuming project substitute its own scene-opening flow (e.g. `com.lowdefmustard.zones`'s `ZoneTools.OpenSceneAndAct`) for the built-in `DefaultOpenSceneAndAct`

## Design Notes

- `GameStateModifier` and `IGameStateModifierHandler` are almost entirely `#if UNITY_EDITOR`-gated — at runtime in a build, a `GameStateModifier` exposes only its `guid`, and a handler's serialized `hasGameStateModifiers`/`gameStateModifierGUIDs` fields (already baked in by the last editor session) remain readable, but no reconciliation logic runs
  - this is intentional: the whole system is editor-authoring/bookkeeping tooling for keeping a modifier's reverse-index accurate as scenes are edited, not something with its own runtime behavior
    - any project-specific logic that *reacts* to a `GameStateModifier` at runtime lives elsewhere, consuming the `guid`/handler data this package maintains
  - background rationale:
    - `IGameStateModifierHandler` is implemented as an interface to be attached to (runtime-compiled) MonoBehaviours, and so cannot live in .Editor namespace
    - in accordance with this, the public methods on `GameStateModifier` need to be accessible in a runtime-compiled (i.e. non-editor) namespace
    - the concession is to have the code itself in a non-editor namespace, but heavily gate behind `#if UNITY_EDITOR` pragma
    - naturally, it is up to the individual using the code to ensure they don't access editor-only methods (behind sed pragma) from ungated runtime-compiled code
- `ScenePathProvider` and `OpenSceneAndActProvider` exist so this package has no dependency on `com.lowdefmustard.zones` (or any other scene-management scheme)
  - without a provider assigned, both fall back to a plain `AssetDatabase` name search
- The configuration contract used in `IGameStateModifierHandler` mirrors `com.lowdefmustard.localization`'s `ILocalizable` — both are "keep a reverse-index in sync with a MonoBehaviour's lifecycle" contracts solving the same category of problem for different data

## Tests

| Assembly                                        | Root Namespace                                  | Platform                      | References                                                                                                                                                                  |
|-------------------------------------------------|-------------------------------------------------|-------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LowDefMustard.GameStateModifiers.Tests.Editor` | `LowDefMustard.GameStateModifiers.Tests.Editor` | Editor only                   | `LowDefMustard.GameStateModifiers`, `LowDefMustard.GameStateModifiers.Editor`, `LowDefMustard.GameStateModifiers.Tests`, `LowDefMustard.Utils`, `LowDefMustard.Utils.Tests` |
| `LowDefMustard.GameStateModifiers.Tests`        | `LowDefMustard.GameStateModifiers.Tests`        | Unrestricted (shared doubles) | `LowDefMustard.GameStateModifiers`, `LowDefMustard.Utils`                                                                                                                   |

Shared test doubles live in `Tests/Runtime/TestDoubles/` (namespace `LowDefMustard.GameStateModifiers.Tests`):
* `TestGameStateModifier`: concrete stand-in for the abstract `GameStateModifier`
* `TestGameStateModifierHandler`: `MonoBehaviour` handler returning `modifiersToReturn`, counting `GetGameStateModifiers` calls; deliberately no `[ExecuteInEditMode]`/`OnDestroy` hook
* `TestModifierPredicate`: `Predicate` linking `modifiersToReturn` (or null via `returnNull`)

### Coverage at a glance

| Category                         | Tested / Total | Notes                                                                                                                                                                                                                       |
|----------------------------------|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LinkedHandlerRecord`            | 6 / 6          | Constructor, `UpdateRecord`, `GetParentLabelStem`, `Equals`/`GetHashCode`, `Get*Ref()` helpers                                                                                                                              |
| `GameStateModifier`              | 6 / 7          | Guid generation, add/update/remove handler, dedup, partial clean-dangling (incl. lookup-backed), partial scene-path lookup                                                                                                  |
| `GameStateModifierHandlerLookup` | 1 / 1          | Zone/handler/link indexing, per-zone separation, merge, blank inputs                                                                                                                                                        |
| `ZoneHandlerKey`                 | 1 / 1          | Equality/hash on zone + handler GUID, null zone name matches empty                                                                                                                                                          |
| `IGameStateModifierHandler`      | 9 / 11         | `MakeLinkedHandlerRecord` (incl. handler type), hash-check, `ReconcileGameStateModifiers` (incl. deferred clean-up), add/remove-stale reconciliation, `OnBeforeSerialize`, `TriggerOnDestroy`, `RecordHandlerModifications` |
| `IGameStateModifierPredicate`    | 1 / 1          | `CollectGameStateModifiers` (null input, mixed/duplicate/null modifiers, null-returning predicate)                                                                                                                          |
| `GameStateModifierEditor`        | 7 / 9          | Inspector structure, edit-lock toggle, add/remove/delete buttons, entry display logic, select name-match filter, handler-type grouping                                                                                      |

### Detail by type

| Type                                | Status  | Test file(s)                                | Notes                                                                                                                                                                 |
|-------------------------------------|---------|---------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LinkedHandlerRecord`               | Yes     | `LinkedHandlerRecordTests.cs`               |                                                                                                                                                                       |
| `GameStateModifier`                 | Partial | `GameStateModifierTests.cs`                 | `GetGameStateModifier` hits + `BuildCache` skipped - needs project assets (reset-on-import verified live)                                                             |
| `GameStateModifierHandlerLookup`    | Yes     | `GameStateModifierHandlerLookupTests.cs`    |                                                                                                                                                                       |
| `ZoneHandlerKey`                    | Yes     | `ZoneHandlerKeyTests.cs`                    |                                                                                                                                                                       |
| `GameStateModifierCacheInvalidator` | Partial | `GameStateModifierCacheInvalidatorTests.cs` | `OnPostprocessAllAssets` needs imports ~ verified live                                                                                                                |
| `IGameStateModifierHandler`         | Partial | `GameStateModifierHandlerTests.cs`          | `TriggerOnGizmos` skipped - needs a live GUI/SceneView context; `RecordHandlerModifications` prefab-override path skipped - test double cannot live on a saved prefab |
| `IGameStateModifierPredicate`       | Yes     | `GameStateModifierPredicateTests.cs`        |                                                                                                                                                                       |
| `GameStateModifierEditor`           | Partial | `GameStateModifierEditorTests.cs`           | Click paths with destructive scene open / blocking dialogue skipped                                                                                                   |

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
