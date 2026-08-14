# Low Def Mustard Saving

Save-data serialization, the `SaveableEntity`/`ISaveable` framework, and the `SaveEditor` in-editor save inspector/mutator tool.

- **Package name:** `com.lowdefmustard.saving`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.unity.nuget.newtonsoft-json` 3.2.2

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.saving": "file:../path/to/com.lowdefmustard.saving"` in your project's `manifest.json`.

## Assembly Structure

| Assembly                      | Root Namespace                | Platform    | References                                |
|-------------------------------|-------------------------------|-------------|-------------------------------------------|
| `LowDefMustard.Saving`        | `LowDefMustard.Saving`        | Runtime     | `Newtonsoft.Json`                         |
| `LowDefMustard.Saving.Editor` | `LowDefMustard.Saving.Editor` | Editor only | `LowDefMustard.Saving`, `Newtonsoft.Json` |

This package is intentionally decoupled from any project-specific save-slot management or game data model — see **Extension Hooks** below for how a consuming project wires itself in.

## Contents

### Core Save Types (`Runtime`)

- **`LoadPriority`** — `ObjectInstantiation` / `ObjectProperty`. Restore happens in two passes across all entities (see `SavingSystem` below) so that anything relying on another object already existing (`ObjectProperty`) runs after every object has had a chance to instantiate itself (`ObjectInstantiation`).
- **`SaveState`** — Serializable envelope pairing a `LoadPriority` with a Newtonsoft `JToken` payload (`state`). `TryGetState<T>()` deserializes the payload; construction always goes through `JToken.FromObject`.
- **`JTokenExtensions`** — `TryToObject<T>()` (safe scalar/object conversion off a `JToken`, treating null/undefined as "no data" rather than throwing) and `IsNullOrEmpty()`.
- **`SymmetricEncryptor`** — AES symmetric encrypt/decrypt of a string, keyed off an internal fixed passphrase (MD5-hashed to a key). This is basic obfuscation of save files on disk, not a security-grade encryption scheme — don't rely on it to protect sensitive data.

### `ISaveableBase` / `ISaveable<T>` (`Runtime/SaveableInterface`)

- **`ISaveableBase`** — The interface any component implements to participate in saving: `GetLoadPriority()`, `CaptureState()`, `RestoreState(SaveState)`, plus default-implemented `IsCorePlayerState()` (false by default) and `ApplyFinishingTouches()` (no-op by default, called after all restore passes complete).
- **`ISaveable<T>`** — Extends `ISaveableBase` with a typed value (`ManualGetStateFromData(T)`, `TryManualGetDataFromState(SaveState, out T)`).
- **`ISaveableGroupRoot`** — Empty marker interface for identifying a hierarchy root (e.g. a player/party root) so `SaveEditor` doesn't re-pull the same entities when they also appear nested under that root.

### `SaveableEntity` (`Runtime/SaveableEntity.cs`)

Attach to any `GameObject` that needs its state saved. `SaveableEntity` is what `SavingSystem` looks for; it in turn collects every sibling component on the *same* GameObject implementing `ISaveable<T>` (it does **not** search children) and drives their capture/restore calls.

**Unique identifiers:** each `SaveableEntity` needs a stable ID to key the save file by. Two cases need different handling:
- **Recurring/fixed entities** (e.g. a specific named character) — set `uniqueIdentifier` to a fixed value by hand.
- **Generic/prefab-spawned entities** — leave `uniqueIdentifier` blank on the prefab; an instance-unique GUID is generated automatically once the instance exists in a scene.

Generating that GUID reliably is trickier than it sounds — `Awake()` alone breaks under GameObject duplication (the copy inherits the original's GUID), so uniqueness is instead checked continuously in `Update()`, editor-only, guarded so it doesn't fire while viewing a prefab in isolation (only the main stage, not prefab-isolation stages) and skipped entirely at runtime:
```csharp
private void Update()
{
    if (Application.IsPlaying(gameObject)) { return; }
    if (string.IsNullOrEmpty(gameObject.scene.path)) { return; }
    if (StageUtility.GetStage(gameObject) != StageUtility.GetMainStage()) { return; }
    // ...check + regenerate uniqueIdentifier if missing or a duplicate...
}
```
`ForcePreventSave()`/`IsSaveRestricted()` let an entity opt out of saving entirely (e.g. torn-down or transient objects).

### `SavingSystem` (`Runtime/SavingSystem.cs`)

Static entry point for all save/load I/O — file read/write (optionally AES-encrypted via `SymmetricEncryptor`, Newtonsoft-serialized), scene reload on load, and orchestrating capture/restore across every `SaveableEntity` in the scene:

- **`Save`/`LoadLastScene`/`LoadWithinScene`/`Delete`/`ListSaves`** — top-level save-slot operations.
- **`CopySessionToSave`/`CopySaveToSession`/`CopySaveToSave`/`Append`** — session-vs-persisted-save copying, and appending a single entity's state into a session file without a full save pass.
- **`CopyCorePlayerStateToSave`** — captures only entities/components flagged `IsCorePlayerState()`. 
  - e.g. for a game-over save that shouldn't include full-world state like position
- Restore runs in three passes per `RestoreState`: all entities' `ObjectInstantiation`-priority state, then all `ObjectProperty`-priority state, then `ApplyFinishingTouches()` on everything — see `LoadPriority` above.
- `Manual*` methods (`ManualGetFullState`, `ManualSave`, `ManualUpdateLastScene`, `ManualAddOverWriteToState`, ...) expose the same primitives `SaveEditor` needs to mutate a save file directly, without going through a live scene load/save cycle.

### Save Editor (`Editor/SaveEditor.cs` + supporting types)

Access via `Tools > Save Editor`. Lets you inspect and directly edit the contents of a save file against the currently open scene, without needing to actually run the game to reach a given state.

- **Layout:** a header (rename/duplicate/delete the current save), a save-list panel (pick which save is "current" via `Set To Current`, copy to the next open slot, delete), and a main panel showing every `SaveableEntity` found in the open scene once `Load Scene Data` is clicked.
- **Entity cards** (`SaveableEntityCardData`) — one per `SaveableEntity`, listing a sub-card per `ISaveableBase` component. `Select Entity` focuses the GameObject in the scene view; selecting the GameObject in-scene scrolls/highlights its card. `Save Entity` writes just that entity; `Apply All Data` (in the header) writes everything at once.
- **Sub-cards** (`SaveableSubCardData`, abstract) — render/edit a single component's `SaveState`. Ships with `SimpleBoolSaveableSubCard`, `SimpleFloatSaveableSubCard`, `SimpleIntSaveableSubCard` (for `ISaveable<bool>`/`<float>`/`<int>`) and `GenericSaveableSubCard` as the always-available fallback ("SubCardView not implemented") for anything without a registered drawer.
  - Additional SubCard views must be generated via `RegisterSubCard` extension hooks - see below.
- **Sync status** — editing a field marks its sub-card (and rolls up to the entity card) "Volatile"/desynced (shown in a distinct color) until that entity is saved or the file is reloaded, so it's obvious what hasn't been persisted yet.
- **`PriorityRegistry<T>`** — small ordered-rule matcher (`Register(predicate, priority)` / `GetPriority(item)`) used to sort both entity cards and sub-cards; unmatched items sort last (`int.MaxValue`).
- **`SceneSelectorContext`** — passed to the optional `SaveEditor.SceneSelectorFactory` hook (see below) so a project can render its own "last saved scene" picker in place of the default read-only text field.

### Extension Hooks

The package ships no project-specific save-slot logic or game-data knowledge — a consuming project wires itself in via these seams:

- **`ISaveFileManagerAdapter` / `SaveFileManagerProvider` (`Editor/SaveFileManagerAdapter`)** — `SaveEditor` talks to save slots entirely through `SaveFileManagerProvider.current`, an `ISaveFileManagerAdapter` (list/set/copy/delete saves, current-save name, per-save summary info). 
  - The default is `NullSaveFileManagerAdapter`, a silent no-op.
  - Each project should assign its own adapter to `SaveFileManagerProvider.current` (typically via an `[InitializeOnLoad]` class) wrapping its actual save-slot manager.
- **`SaveableSubCardData.RegisterSubCard<T>(...)` / `RegisterSubCardPriority(...)`** — register a custom `SaveableSubCardData` factory for a given `ISaveableBase` type, and/or a sort priority for it, so project-specific components get a purpose-built editor UI.
- **`SaveableEntityCardData.RegisterEntityCardPriority(...)`** — sort priority for entity cards themselves (e.g. pinning a player/root entity to the top of the list).
- **`SaveEditor.SceneSelectorFactory`** — optional `Func<SceneSelectorContext, VisualElement>` to replace the default read-only "last saved scene" field with a project-specific scene picker.

## Design Notes

- No default `ISaveFileManagerAdapter` implementation ships with the package (`NullSaveFileManagerAdapter` is an inert stand-in) — a consuming project must supply and register its own before `SaveEditor` is functional.
- `ISaveFileManagerAdapter`/`SaveFileManagerProvider`/`NullSaveFileManagerAdapter` live in the `.Editor` assembly/namespace rather than Runtime, since `SaveEditor` is their only consumer and it's editor-only. 
  - Any future runtime code needing save-file info should go through the consuming project's own save-file manager directly, not through this editor-tooling seam.
- `SaveableEntityCardData` and its factory methods take a `GameObject` prefab directly (rather than any richer project-specific data type) to keep this package free of dependencies on a particular game's data model.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
