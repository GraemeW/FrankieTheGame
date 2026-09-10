# Low Def Mustard Localization

Interfaces, helpers, and editor tooling built on top of Unity's Localization package: a `SimpleLocalizedString` inspector workflow for authoring/keying localized text in-place, an `ILocalizable` contract for objects that own localization entries, and automatic cleanup of orphaned entries on asset/instance deletion.

The package APIs is generic over a caller-supplied `TTableType : struct, Enum`, with the enum-to-`StringTableCollection`-name mapping supplied by the caller.

High-Level Notes:
- **`LocalizationToolBase<TTableType>`** and **`ILocalizableBase<TTableType>`** are deliberately named `...Base<TTableType>`
  - a project should define its own non-generic alias type closing over its enum (i.e. `LocalizationTool`, `ILocalizable`)
- **`SimpleLocalizedStringAttribute`** (and its drawer) is **not** generic
  - this is a hard requirement because a generic class can never derive from `System.Attribute`
  - so it stays a single package-level type that validates its table-type argument at construction
- Classes that cannot reference *any* project-specific `TTableType` (e.g. independent packages using this package) implement `ILocalizableCore` directly
  - they register their `TTableType` by their own `Type` via `LocalizableClassTableTypeRegistry`
  - see "Implementing `ILocalizableCore` directly" below

- **Package name:** `com.lowdefmustard.localization`
- **Version:** 0.5.0
- **Unity:** 6000.5+
- **Dependencies:**
  - `com.unity.localization` 1.5.12
  - `com.unity.addressables` 2.9.1
  - `com.lowdefmustard.utils` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.localization": "file:../path/to/com.lowdefmustard.localization"` in your project's `manifest.json`. `com.lowdefmustard.utils` and Unity's Localization/Addressables packages must also be present.

## Getting Started (per-project setup)

1. Define your own table-type enum e.g.:
   ```c#
   public enum LocalizationTableType { Core, Inventory, Quests, UI, /* ... */ }
   ```
2. Define your project's `ILocalizable`/`LocalizationLocale`/`LocalizationTool` aliases, closing each package `...Base<T>` type over your enum, e.g.:
   ```c#
   public interface ILocalizable : ILocalizableBase<LocalizationTableType> { }
   public sealed class LocalizationLocale : LocalizationLocaleBase<SupportedLocalizationType> { }
   public sealed class LocalizationTool : LocalizationToolBase<LocalizationTableType> { }
   ```
3. Once, before any table-type-aware API is used (e.g. from an `[InitializeOnLoad]` static constructor), register the table-collection-name mapping, (if any) relevant ClassTableTypes and (if using) a custom key generator:
   ```c#
   LocalizationTool.RegisterTableCollectionNames(new Dictionary<LocalizationTableType, string>
   {
       { LocalizationTableType.Core, "Core" },
       { LocalizationTableType.Inventory, "Inventory" },
       // ...
   });
   
   LocalizableClassTableTypeRegistry.Register(typeof(Zone), LocalizationTableType.Zones);
   LocalizableClassTableTypeRegistry.Register(typeof(ZoneNode), LocalizationTableType.Zones);
  
   SimpleLocalizedStringDrawer.typeSpecificKeyGenerator = LocalizationNames.GenerateTypeSpecificKey;
   ```
5. Implement `ILocalizable` on your `MonoBehaviour`s/`ScriptableObject`s, and use the package's `[SimpleLocalizedString(LocalizationTableType.Quests, isKeyEditable: true)]` on `LocalizedString` fields

## Assembly Structure

| Assembly                            | Root Namespace                      | Platform    | References                                                                                             |
|-------------------------------------|-------------------------------------|-------------|--------------------------------------------------------------------------------------------------------|
| `LowDefMustard.Localization`        | `LowDefMustard.Localization`        | Runtime     | `LowDefMustard.Utils`, `Unity.ResourceManager`, `Unity.Localization`, `Unity.Localization.Editor`      |
| `LowDefMustard.Localization.Editor` | `LowDefMustard.Localization.Editor` | Editor only | `LowDefMustard.Localization`, `LowDefMustard.Utils`, `Unity.Localization`, `Unity.Localization.Editor` |

**Note:** the Runtime assembly references `Unity.Localization.Editor`. This is a deliberate choice — most of `LocalizationToolBase<T>`'s implementation (and the bridge/registry) are wrapped in `#if UNITY_EDITOR`, so the editor-only APIs they call (`LocalizationEditorSettings`, `StringTableCollection` creation/lookup, etc.) need to compile there, while the runtime-safe surface (`MakeLocalizedString`, and locale switching via `LocalizationLocaleBase<TLocaleType>`) stays available at runtime.

## Contents

### Core Types (`Runtime`)

- **`LocalizationToolBase<TTableType>`** (`TTableType : struct, Enum`, `abstract class` — abstract only to block direct instantiation; all members are `static`) — hub for all table/entry interaction:
  - **`RegisterTableCollectionNames(IReadOnlyDictionary<T, string>)`** — Caller-supplied mapping from enum value to `StringTableCollection` name
    - Must be called before other members are used for that `TTableType`; safe to call more than once (later calls overwrite earlier entries for the same key)
    - Also registers this closed type's `Bridge` with `LocalizationToolBridgeRegistry` as a side effect
  - **Runtime-safe:** `MakeLocalizedString`
  - **Editor-only:** create-or-fetch a `StringTableCollection` per `TTableType` value (auto-creating the asset under `Assets/Localization` on first use), add/update/remove English entries, rename/create keys, resolve a `LocalizedString`'s current key name, and safely rebind a `LocalizedString` to a new key by ID (never by name, to avoid stale-name drift)
    - Caches table collections and the English `StringTable` per `TTableType` value to avoid repeated asset lookups
    - Static state (caches, registered mapping) is independent per closed `LocalizationToolBase<TTableType>` — no cross-contamination between different enums used in the same project
  - **`Bridge`** — Editor-only. Exposes this closed `LocalizationToolBase<T>` as a non-generic `ILocalizationToolBridge`, so editor infrastructure that can't be generic itself (`LocalizationDeletionHandler`, `SimpleLocalizedStringDrawer`) can still call into it
    - Resolved via `LocalizationToolBridgeRegistry`
  - A project's alias, e.g. `public sealed class LocalizationTool : LocalizationToolBase<LocalizationTableType> { }`, lets project code write `LocalizationTool.MakeLocalizedString(...)`
- **`LocalizationLocaleBase<TLocaleType>`** (`TLocaleType : struct, Enum`, `abstract class` — locale selection, a project defines its own alias (e.g. `LocalizationLocale : LocalizationLocaleBase<SupportedLocalizationType>`)
  - **`RegisterLocaleCodes(IReadOnlyDictionary<TLocaleType, string>, TLocaleType defaultLocale)`** — Caller-supplied mapping (e.g. `{ English, "en" }`) plus a fallback default. **Must run in actual builds, not just the editor** — `GetCurrentLocalization`/`SetLocale` are used at runtime (e.g. applying a saved language preference on boot), so (**critically**) register this via `[RuntimeInitializeOnLoadMethod]`
  - `GetCurrentLocalization`/`GetLocalizationByCode`, `GetLocaleCode`, `SetLocale` — runtime-safe
  - Editor-only `InitializeDefaultLocale` — forces the localization system's async init to complete and switches the active preview locale to the registered default, for editor authoring workflows
  - Core (non-generic) `TriggerLocalizationSettingsInitialization` is used as an alias to force the localization system's async init to complete for (e.g.) editor scripts
- **`LocalizableClassTableTypeRegistry`** — A plain `Dictionary<Type, Enum>`, `Register(Type owningType, Enum tableType)` / `GetTableType(Type owningType)`
  - For `ILocalizableCore` implementers, with no project-specific `TTableType` at all, maps the implementing *class* directly to a table-type value, so `MyClass.localizationTableTypeValue` can be `LocalizableClassTableTypeRegistry.GetTableType(GetType())` instead of a compile-time-typed field
  - _To-be-extracted out_
- **`LocalizationTool`** — Static hub for all table/entry interaction:
  - **Runtime-safe:** `MakeLocalizedString`, `GetCurrentLocalization`/`GetLocalizationByCode`, `GetLocaleCode`, `SetLocale`
  - **Editor-only:** create-or-fetch a `StringTableCollection` per `LocalizationTableType` (auto-creating the asset under `Assets/Localization` on first use), add/update/remove English entries, rename/create keys, resolve a `LocalizedString`'s current key name, and safely rebind a `LocalizedString` to a new key by ID (never by name, to avoid stale-name drift)
    - Caches table collections and the English `StringTable` per `LocalizationTableType` to avoid repeated asset lookups
- **`LocalizedStringExtensions.GetSafeLocalizedString()`** — Null/empty-safe wrapper around `LocalizedString.GetLocalizedString()`
- **`DefaultKeyGenerator.GenerateKindaUniqueKey(...)`** — Key generator producing a semi-readable, semi-random key from an object's type, parent name (with a Canvas-name skip for UI), prefab/scene context, and property name, suffixed with a short random hex string
  - Intended to be called from Editor
  - When called at Runtime (not expected), will produce the short random hex string

### `ILocalizableCore` / `ILocalizableBase<T>` (`Runtime/ILocalizable.cs`)

`ILocalizableCore` is the non-generic root interface, which exposes 
- `localizationTableTypeValue` (boxed `Enum`)
- `GetLocalizationEntries()`
- `iCachedName`
- `onBeforeDestroyedInEditor` event + `TriggerOnDestroy` hook 
- orchestration logic for `TryLocalizeStandardEntries`/`ReconcileCachedName`

`ILocalizableBase<T> : ILocalizableCore` (`T : struct, Enum`) is the generic contract most implementers actually use

A project implements its own alias instead (`public interface ILocalizable : ILocalizableBase<LocalizationTableType> { }`) — default-interface-method contract for any `MonoBehaviour` or `ScriptableObject` that owns one or more localization entries and needs those entries kept in sync with the object's name and lifecycle:

- **`localizationTableType`** — which table this object's entries live in, typed as `TTableType`
- **`GetLocalizationEntries()`** — all `TableEntryReference`s the object owns
- **`iCachedName`** — override point (backed by a serialized field in the implementer) enabling automatic key renaming when the object's name changes
  - Implement via explicit interface e.g. `string ILocalizable.iCachedName { get => cachedName; set => cachedName = value; }
- **`TryLocalizeStandardEntries(...)`** — editor-only - given a list of `(propertyName, LocalizedString, setToName)` tuples, initializes each entry's key (`Type.id` or `Type.id.propertyName`) if it doesn't already have an English value, and reconciles/renames all owned keys if the object's name has changed since last time (via `iCachedName`)
- **`TriggerOnDestroy(ILocalizableCore)`** / **`onBeforeDestroyedInEditor`** — editor-only - a hook that a `MonoBehaviour.OnDestroy()` can call to notify `LocalizationDeletionHandler` that its entries may need cleanup — call as `ILocalizable.TriggerOnDestroy(this)`

Implementers configure themselves differently depending on type — see the extensive **"CRITICAL NOTES ON CONFIGURATION"** comment block above `ILocalizableBase<TTableType>` in `ILocalizable.cs` before implementing:
- **ScriptableObjects:** put your `ILocalizable` alias on the parent-most asset only (deletion detection doesn't fire for SOs nested inside other SOs); the parent-most object's `GetLocalizationEntries()` must aggregate its children's entries.
- **MonoBehaviours:** add `[ExecuteInEditMode]` and call `ILocalizableCore.TriggerOnDestroy(this)` from `OnDestroy()` — only if the entries should actually be deleted when the instance goes away (e.g. can skip this for persistent/UI elements).

#### Implementing `ILocalizableCore` directly

Implement `ILocalizableCore` directly, without an `ILocalizable` alias, when there is no single project-specific `TTableType` available for the class — the usual case is a class that lives in a shared package (so it can't reference the consuming project's enum without creating a reverse dependency).

```c#
// In the specific package implementation
public class MySharedType : ScriptableObject, ILocalizableCore
{
    [SimpleLocalizedString(isKeyEditable: false)][SerializeField] private LocalizedString localizedDisplayName;
    
    Enum ILocalizableCore.localizationTableTypeValue => LocalizableClassTableTypeRegistry.GetTableType(GetType());

    public List<TableEntryReference> GetLocalizationEntries() => new() { localizedDisplayName.TableEntryReference };
}

// In the consuming project's bootstrap, alongside RegisterTableCollectionNames(...):
LocalizableClassTableTypeRegistry.Register(typeof(MySharedType), LocalizationTableType.SomeValue);
```

Any editor-side operations that the shared type needs to do itself (rename a key, delete an entry, etc.) go through `LocalizationToolBridgeRegistry.TryGetBridge(tableType.GetType(), out ILocalizationToolBridge bridge)` rather than a `LocalizationTool` alias.  This mirrors `LocalizationDeletionHandler` and `SimpleLocalizedStringDrawer`.

### `SimpleLocalizedStringAttribute` + `SimpleLocalizedStringDrawer` (`Runtime/SimpleLocalizedStringAttribute.cs`, `Editor/SimpleLocalizedStringDrawer.cs`)

A `PropertyAttribute`/`PropertyDrawer` pair for authoring a `LocalizedString` field directly in the inspector, without leaving the component. Unlike `LocalizationTool`/`ILocalizable`, **neither of these has a per-project alias** — use the package's own types directly:

```csharp
[SimpleLocalizedString(LocalizationTableType.Quests, isKeyEditable: true)][SerializeField] private LocalizedString questTitle;
```

- **`[SimpleLocalizedString(tableTypeValue, isKeyEditable)]`** — Apply to any `LocalizedString` field. 
  - The attribute takes `object` and validates it's an enum value at construction, rather than being generic over `TTableType`
  - notably, a generic class can never derive from `System.Attribute`
  - `SimpleLocalizedStringDrawer` follows for the same reason, and resolves the right `LocalizationToolBase<TTableType>` for whatever enum `Type` was passed in via `LocalizationToolBridgeRegistry`
- **`[SimpleLocalizedString(isKeyEditable)]`** — Overload with no table type, for fields on a class implementing `ILocalizableCore` directly with no project-specific `TTableType` available (as described above)
  - The drawer falls back to reading the table type off the field's containing object (`property.serializedObject.targetObject as ILocalizableCore`) at draw time instead of off the attribute
  - if that object doesn't implement `ILocalizableCore`, or its `localizationTableTypeValue` is `null` (nothing registered in `LocalizableClassTableTypeRegistry` yet), the drawer shows an inline error box

- Drawer shows the key (editable or locked, per `isKeyEditable`) and the English content side-by-side, plus buttons to generate a new key, auto-rename the current key, and delete the key-entry
- **Lock toggle** — Editable keys start locked; unlocking is required before the key field or the new/rename/delete buttons become active, guarding against accidental key edits
- **Prefab-aware:** editing content on a prefab instance whose key is still shared with the prefab source generates a new, instance-unique key automatically rather than silently overwriting the prefab's entry
  - The delete button is disabled whenever the current key is still shared with a prefab source
- **`SimpleLocalizedStringDrawer.typeSpecificKeyGenerator`** — Static hook (`Func<Object, string, Type, bool, string>`) that a project can assign via `[InitializeOnLoad]` to override `DefaultKeyGenerator` with project-specific key formatting
- Note that since `PropertyDrawer` instances are shared across every array/list element sharing the attribute, all per-element state lives in a local `ElementState` object built fresh per `CreatePropertyGUI` call, never on the drawer instance itself

### Automatic Cleanup (`Editor/LocalizationDeletionHandler.cs`)

`[InitializeOnLoad]` `AssetModificationProcessor` that deletes the localization entries that an `ILocalizableCore` uniquely owns when that object is deleted.  As with the property drawer, this stays non-generic and works against `ILocalizableCore`/`Enum`, reaching the right `LocalizationToolBase<T>` for a given deletion via `LocalizationToolBridgeRegistry.GetBridge(tableType.GetType())`
- Hooks Unity's `OnWillDeleteAsset` for `ScriptableObject`/`GameObject` assets, and `ILocalizableCore.onBeforeDestroyedInEditor` for scene instances calling `TriggerOnDestroy`
- For prefab instances/variants, only deletes entries that are **unique to the target** (i.e. not shared with the corresponding prefab source)

## Two independent enums in one project

Since each closed `LocalizationToolBase<TTableType>`/`ILocalizableBase<TTableType>` has independent static state, a project that genuinely needs two unrelated table-type enums can define a second, differently-named alias set (e.g. `DialogueLocalizationTool : LocalizationToolBase<DialogueTableType>`) alongside the first with no interference — each `RegisterTableCollectionNames` call, cache, and mapping is scoped to its own `TTableType`.

## Design Notes

- English is treated as the authoring source of truth throughout `LocalizationToolBase<TTableType>`.
  - Other locales (e.g. French) are populated via the string table assets/CSV workflow rather than through this package's editor tooling
- `LocalizationToolBase<TTableType>` requires `RegisterTableCollectionNames(...)` to be called for a given `TTableType` before other members are used; unregistered lookups log an error and return an empty table-collection name

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
