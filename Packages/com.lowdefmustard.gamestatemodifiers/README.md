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
  - Holds `gameStateModifierHandlerData` — a `List<ZoneToGameObjectLinkData>` reverse-index of every scene GameObject ("handler") currently referencing this modifier, maintained automatically by handlers rather than hand-authored (see `IGameStateModifierHandler` below); `GameStateModifierEditor` renders this with its own custom UI in place of Unity's default list drawer
  - Editor-only static guid→asset cache (`GetGameStateModifier(guid)`), built via `AssetDatabase.FindAssets` and rebuilt on a lookup miss in case it's gone stale (e.g. after a new asset is created mid-session)
  - `AddOrUpdateGameStateModifierHandler` / `RemoveGameStateModifierHandler` / `CleanDanglingModifierHandlerData` — the editor-only mutation API a handler calls into to keep this reverse-index in sync; the latter removes entries whose scene/object can no longer be found, or whose target `GameStateModifier` link has been severed, or that are exact duplicates
  - `ScenePathProvider` — optional static hook (`HasScenePathDelegate`) letting a consuming project supply its own zone-name → scene-path resolution (e.g. via `com.lowdefmustard.zones`'s `Zone` lookup) in place of the built-in `DefaultGetScenePath` (a plain `AssetDatabase.FindAssets($"{zoneName} t:Scene")` name search)
- **`IGameStateModifierHandler`** (interface, extends `ISerializationCallbackReceiver`) — Implemented by any `MonoBehaviour` that references one or more `GameStateModifier`s and needs that reference kept in sync with the modifier's reverse-index automatically
  - Configuration contract (see the extensive comment block at the top of the file before implementing):
      - Requires `[ExecuteInEditMode]` on the implementing class, and a call to the static `IGameStateModifierHandler.TriggerOnDestroy(this)` from its `OnDestroy()`
      - Requires explicit serialized backing fields for `handlerGUID` / `modifierListHashCheck` / `hasGameStateModifiers` / `gameStateModifierGUIDs` (default interface properties can't themselves carry `[SerializeField]` state)
      - `gameObject` is declared on the interface but deliberately left unimplemented — it is satisfied implicitly by `MonoBehaviour.gameObject`, which is why this interface can only be implemented by a `MonoBehaviour`-derived class, never a plain C# class
      - The one method an implementer must actually supply is `GetGameStateModifiers()` (which asset(s) this handler references); everything else is default-implemented
      - `OnBeforeSerialize()` (editor-only): builds a `ZoneToGameObjectLinkData` for the handler's current scene/name/parent, hashes it together with its linked `GameStateModifier` GUIDs (`GetModifierListHashCheck`) to skip redundant work when nothing's changed, then reconciles — updates/adds this handler's entry on every currently-linked `GameStateModifier`, and cleans its entry off any `GameStateModifier` it *used* to reference but no longer does
      - `TriggerOnGizmos(this)` — optional scene-view gizmo (a star-in-circle, at the handler's `SpriteRenderer` bounds center if present) drawn when `hasGameStateModifiers` is true, for spotting linked objects visually while scene-editing
      - All reconciliation is guarded by `EditorStateCheck.IsStandardEditorState` (`com.lowdefmustard.utils`), so it doesn't fire during play mode, domain reload, prefab isolation, or on non-scene assets
- **`ZoneToGameObjectLinkData`** — Serializable struct (`zoneName`, `gameObjectName`, `parentObjectName`, `guid`); equality is defined purely on `guid`. One entry per handler, stored in the owning `GameStateModifier.gameStateModifierHandlerData` list

### Editor Tooling (`Editor/GameStateModifierEditor.cs`)

- **`GameStateModifierEditor`** (`[CustomEditor(typeof(GameStateModifier), true)]`) — UI Toolkit custom inspector replacing the default list rendering of `gameStateModifierHandlerData`
  - includes a foldout of styled entry cards comprising: 
    - zone/object/GUID fields
    - an "Open & Select" button (opens the entry's scene and selects the linked GameObject, falling back to a name search with a warning if the GUID can't be resolved)
    - a per-entry delete button
    - a "Remove Invalid Entries" button wired to `CleanDanglingModifierHandlerData`
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

| Assembly                                        | Root Namespace                                  | Platform    | References                                                                                            |
|-------------------------------------------------|-------------------------------------------------|-------------|----------------------------------------------------------------------------------------------------------|
| `LowDefMustard.GameStateModifiers.Tests.Editor` | `LowDefMustard.GameStateModifiers.Tests.Editor` | Editor only | `LowDefMustard.GameStateModifiers`, `LowDefMustard.GameStateModifiers.Editor`, `LowDefMustard.Utils` |

### Coverage at a glance

| Category                     | Tested / Total   | Notes                                                                                                                |
|------------------------------|------------------|----------------------------------------------------------------------------------------------------------------------|
| `ZoneToGameObjectLinkData`   | 6 / 6            | Constructor, `UpdateRecord`, `GetParentLabelStem`, `Equals`/`GetHashCode`, `Get*Ref()` helpers                       |
| `GameStateModifier`          | 6 / 7            | Guid generation, add/update/remove handler, dedup, partial clean-dangling, partial scene-path lookup                 |
| `IGameStateModifierHandler`  | 8 / 9            | `MakeZoneToGameObjectLinkData`, hash-check, add/remove-stale reconciliation, `OnBeforeSerialize`, `TriggerOnDestroy` |
| `GameStateModifierEditor`    | 5 / 7            | Inspector structure, edit-lock toggle, add/remove/delete buttons, entry display logic                                |

### Detail by type

| Type                         | Status  | Test file(s)                       | Notes                                                                   |
|------------------------------|---------|------------------------------------|-------------------------------------------------------------------------|
| `ZoneToGameObjectLinkData`   | Yes     | `ZoneToGameObjectLinkDataTests.cs` |                                                                         |
| `GameStateModifier`          | Partial | `GameStateModifierTests.cs`        | `GetGameStateModifier`/`BuildCache` skipped — needs real project assets |
| `IGameStateModifierHandler`  | Partial | `GameStateModifierHandlerTests.cs` | `TriggerOnGizmos` skipped - needs a live GUI/SceneView context          |
| `GameStateModifierEditor`    | Partial | `GameStateModifierEditorTests.cs`  | Click paths with destructive scene open / blocking dialogue skipped     |

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
