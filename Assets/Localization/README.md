# Assets: Localization

Frankie makes use of the [Unity Localization Package](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html) to support different languages.  All text that can be rendered in Frankie lives on Localization tables in this directory.  The tables are broken down into several main categories of text, as denoted by the enum [LocalizationTableType](../Scripts/Utils/Localization/LocalizationTableType.cs), and sub-directoried here:
* [Checks / World Objects](./Table_ChecksWorldObjects):  Anything that can be interacted with (to display text) in the world
* [Core](./Table_Core):  Character names, stat translations, status effect flavours, etc.
* [Inventory](./Table_Inventory):  Inventory / equipment object names and descriptions, as well as shop interaction text
* [Quests](./Table_Quests):  Quest/objective names and descriptions
* [Skills](./Table_Skills):  Skill names and descriptions
* [UI](./Table_UI):  Any text on UI menus - including any prefabs living in the [Game/UI](../Game/UI) directory
* [Zones](./Table_Zones):  Zone and ZoneNode names

## ILocalizable Interface

Any game object or scriptable object that uses [LocalizedStrings](https://docs.unity3d.com/Packages/com.unity.localization@1.5/api/UnityEngine.Localization.LocalizedString.html) must implement the [ILocalizable](../Scripts/Utils/Localization/ILocalizable.cs) interface.  This interface allows for automatic updates to Localization Table entries when their corresponding objects are deleted, re-named (for scriptable objects), etc.  In other words, the [ILocalizable](../Scripts/Utils/Localization/ILocalizable.cs) interface ensures good coherency between the localization tables and the physical assets that reference them, such that the Localization tables are up-to-date & not containing stale entries for deleted or altered objects.

### Scriptable Object - Configuration Notes

* ILocalizable should be employed on the parent-most scriptable object
  * LocalizationDeletionHandler.OnWillDeleteAsset() does not trigger for scriptable objects that are childed to other scriptable objects!
  * The parent-most object must take gather localization entries from all children for `GetLocalizationEntries()`
* In order to auto-set and auto-rename localized entries:
  * override iCachedName to link to a serialized cachedName backing field
  * create a custom inspector editor that calls TryLocalizedStandardEntries() during the editor's OnEnable()
  * pass all relevant propertyName-localizedString pairs to this method
  
### MonoBehaviour / Game Object - Limitations and Configuration Notes

#### Configuration Notes

The following may be manually configured:
  * Add [ExecuteInEditMode] attribute to the class 
  * Include `ILocalizable.TriggerOnDestroy(this)` to the `OnDestroy()` method

, in order to allow the localization entries to automatically delete on game object deletion (e.g. for game objects placed into scenes).

Note:  this is not strictly necessary and not always configured (e.g. for fixed UI elements based solely on Prefabs).  With the above configuration, cleanup for prefabs/prefab variants is handled by `LocalizationDeletionHandler.OnWillDeleteAsset()`, while cleanup for instanced objects in scenes is handled by `OnDestroy()`.  Without the above configuration, only the cleanup for prefabs/prefab variants will occur.

Also Note:  ILocalizable only cleans up localization entries for unique parent-most prefabs, and will not delete derived entries from prefabs variants / prefab instances (so don't worry).  

#### Limitations

The following limitations are acknowledged:
* Disabled game objects placed in the scene will NOT trigger `OnDestroy()`, and thus will not automatically delete their corresponding localization table entries
  * Workaround:  Ensure ILocalizble game obejcts are enabled before deleting them in the scene

## SimpleLocalizedString Editor Attribute

All serialized [LocalizedStrings](https://docs.unity3d.com/Packages/com.unity.localization@1.5/api/UnityEngine.Localization.LocalizedString.html) present on game objects or scriptable objects must include the [SimpleLocalizedString](../Scripts/Utils/CustomAttributesDrawers/SimpleLocalizedStringAttribute.cs) attribute.

This attribute allows for simpler editing of localized entries.  Notably, it removes the need for manually linking localization tables and keys, and it allows for the following functionality:
* auto-update english table entries simply and directly on the inspector
* auto-generate/rename unique key entries following a standardized key generation methodology
* delete localization entries without opening localization tables

An example of this attribute in use is shown below:
<img src="../../InfoTools/Documentation/Game/Localization/SimpleLocalizedString.png" width="300">

Note that in order to prevent accidental localization table adjustments, by default, the key field and button functionalities are grayed out.  They can be enabled by clicking on the lock toggle. 
