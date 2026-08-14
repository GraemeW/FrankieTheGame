# Low Def Mustard Localization

Interfaces, helpers, and editor tooling built on top of Unity's Localization package: a `SimpleLocalizedString` inspector workflow for authoring/keying localized text in-place, an `ILocalizable` contract for objects that own localization entries, and automatic cleanup of orphaned entries on asset/instance deletion.

- **Package name:** `com.lowdefmustard.localization`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.unity.localization` 1.5.12
  - `com.unity.addressables` 2.9.1
  - `com.lowdefmustard.utils` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.localization": "file:../path/to/com.lowdefmustard.localization"` in your project's `manifest.json`. `com.lowdefmustard.utils` and Unity's Localization/Addressables packages must also be present.

## Assembly Structure

| Assembly                            | Root Namespace                      | Platform    | References                                                                                             |
|-------------------------------------|-------------------------------------|-------------|--------------------------------------------------------------------------------------------------------|
| `LowDefMustard.Localization`        | `LowDefMustard.Localization`        | Runtime     | `LowDefMustard.Utils`, `Unity.ResourceManager`, `Unity.Localization`, `Unity.Localization.Editor`      |
| `LowDefMustard.Localization.Editor` | `LowDefMustard.Localization.Editor` | Editor only | `LowDefMustard.Localization`, `LowDefMustard.Utils`, `Unity.Localization`, `Unity.Localization.Editor` |

**Note:** the Runtime assembly references `Unity.Localization.Editor`. This is a deliberate choice — most of `LocalizationTool`'s implementation is wrapped in `#if UNITY_EDITOR`, so the editor-only APIs it calls (`LocalizationEditorSettings`, `StringTableCollection` creation/lookup, etc.) need to compile there, while the runtime-safe surface (`MakeLocalizedString`, `GetCurrentLocalization`, `SetLocale`, ...) stays available at runtime with no compile-time split needed between two assemblies.

## Contents

### Core Types (`Runtime`)

- **`LocalizationTableType`** — Enum of standard string table collections:
  - `ChecksWorldObjects`
  - `Core`
  - `Inventory`
  - `Quests`
  - `Skills`
  - `Speech`
  - `UI`
  - `Zones`.
- **`SupportedLocalizationType`** — `English` / `French`.
  - _To-be-updated / expanded, or extracted out._
- **`LocalizationTool`** — Static hub for all table/entry interaction:
  - **Runtime-safe:** `MakeLocalizedString`, `GetCurrentLocalization`/`GetLocalizationByCode`, `GetLocaleCode`, `SetLocale`.
  - **Editor-only:** create-or-fetch a `StringTableCollection` per `LocalizationTableType` (auto-creating the asset under `Assets/Localization` on first use), add/update/remove English entries, rename/create keys, resolve a `LocalizedString`'s current key name, and safely rebind a `LocalizedString` to a new key by ID (never by name, to avoid stale-name drift). 
    - Caches table collections and the English `StringTable` per `LocalizationTableType` to avoid repeated asset lookups.
- **`LocalizedStringExtensions.GetSafeLocalizedString()`** — Null/empty-safe wrapper around `LocalizedString.GetLocalizedString()`.
- **`DefaultKeyGenerator.GenerateKindaUniqueKey(...)`** — Key generator producing a semi-readable, semi-random key from an object's type, parent name (with a Canvas-name skip for UI), prefab/scene context, and property name, suffixed with a short random hex string.
  - Intended to be called from Editor.
  - When called at Runtime (not expected), will produce the short random hex string.

### `ILocalizable` (`Runtime/ILocalizable.cs`)

Default-interface-method contract for any `MonoBehaviour` or `ScriptableObject` that owns one or more localization entries and needs those entries kept in sync with the object's name and lifecycle:

- **`localizationTableType`** — Which table this object's entries live in.
- **`GetLocalizationEntries()`** — All `TableEntryReference`s the object owns.
- **`iCachedName`** — Override point (backed by a serialized field in the implementer) enabling automatic key renaming when the object's name changes.
- **`TryLocalizeStandardEntries(...)`** — Editor-only. Given a list of `(propertyName, LocalizedString, setToName)` tuples, initializes each entry's key (`Type.id` or `Type.id.propertyName`) if it doesn't already have an English value, and reconciles/renames all owned keys if the object's name has changed since last time (via `iCachedName`).
- **`TriggerOnDestroy(ILocalizable)`** / **`onBeforeDestroyedInEditor`** — Editor-only hook a `MonoBehaviour.OnDestroy()` can call to notify `LocalizationDeletionHandler` that its entries may need cleanup.

Implementers configure themselves differently depending on type — see the extensive **"CRITICAL NOTES ON CONFIGURATION"** comment block at the top of `ILocalizable.cs` before implementing:
- **ScriptableObjects:** put `ILocalizable` on the parent-most asset only (deletion detection doesn't fire for SOs nested inside other SOs); the parent-most object's `GetLocalizationEntries()` must aggregate its children's entries.
- **MonoBehaviours:** add `[ExecuteInEditMode]` and call `ILocalizable.TriggerOnDestroy(this)` from `OnDestroy()` — only if the entries should actually be deleted when the instance goes away (e.g. can skip this for persistent/UI elements).

### Automatic Cleanup (`Editor/LocalizationDeletionHandler.cs`)

`[InitializeOnLoad]` `AssetModificationProcessor` that deletes localization entries an `ILocalizable` uniquely owns when that object is deleted:

- Hooks Unity's `OnWillDeleteAsset` for `ScriptableObject`/`GameObject` assets, and `ILocalizable.onBeforeDestroyedInEditor` for scene instances calling `TriggerOnDestroy`.
- For prefab instances/variants, only deletes entries that are **unique to the target** (i.e. not shared with the corresponding prefab source) — comparing key IDs directly rather than relying on `PrefabUtility.GetPropertyModifications()`, since that API isn't reliable when called from `OnDestroy()`/`OnDisable()`.

### `[SimpleLocalizedString]` Attribute + Drawer (`Runtime/SimpleLocalizedStringAttribute.cs`, `Editor/SimpleLocalizedStringDrawer.cs`)

A `PropertyAttribute`/`PropertyDrawer` pair for authoring a `LocalizedString` field directly in the inspector, without leaving the component:

- **`[SimpleLocalizedString(LocalizationTableType, isKeyEditable)]`** — Apply to any `LocalizedString` field.
- Drawer shows the key (editable or locked, per `isKeyEditable`) and the English content side-by-side, plus buttons to generate a new key, auto-rename the current key, and delete the key-entry.
- **Lock toggle** — Editable keys start locked; unlocking is required before the key field or the new/rename/delete buttons become active, guarding against accidental key edits.
- **Prefab-aware:** editing content on a prefab instance whose key is still shared with the prefab source generates a new, instance-unique key automatically rather than silently overwriting the prefab's entry. The delete button is disabled whenever the current key is still shared with a prefab source.
- **`SimpleLocalizedStringDrawer.TypeSpecificKeyGenerator`** — Static hook (`Func<Object, string, Type, bool, string>`) a project can assign via `[InitializeOnLoad]` to override `DefaultKeyGenerator` with project-specific key formatting.
- Note that since `PropertyDrawer` instances are shared across every array/list element sharing the attribute, all per-element state lives in a local `ElementState` object built fresh per `CreatePropertyGUI` call, never on the drawer instance itself.

## Design Notes

- English is treated as the authoring source of truth throughout `LocalizationTool`.
  - Other locales (e.g. French) are populated via the string table assets/CSV workflow rather than through this package's editor tooling.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
