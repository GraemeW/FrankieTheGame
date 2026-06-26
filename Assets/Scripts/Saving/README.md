# Assets - Scripts : Saving

Note: This README is currently a WIP, thus incomplete, and will be updated in due time.

## Saveable Entities

In order for a GameObject to have its state saved, it must have a [SaveableEntity](./SaveableEntity.cs) component attached to it.  This tags the object so that is can be found by the [SavingSystem](./SavingSystem.cs).  

The [SaveableEntity](./SaveableEntity.cs) in turn finds all other components on the GameObject that implement the [ISaveable](./ISaveable.cs) interface.  Notably, it does **not** find [ISaveables](./ISaveable.cs) in the game object's children/extended hierarchy, only on the GameObject with the [SaveableEntity](./SaveableEntity.cs) itself.

### Unique Identifiers

Each [SaveableEntity](./SaveableEntity.cs) has a unique identifier for look-up (to save/restore the given GameObject's state) in the [SavingSystem](./SavingSystem.cs).  There are two high-level requirements for use with prefabs that require special note:
* we wish to have fixed (single-value) GUIDs for the **prefabs** of special / recurring entities
* separately, we wish to have fixed GUIDs for the **instances** generated from prefabs for other more generic entities.

#### UID Implementation

For the case of generic entities, we want the unique identifier to be generated once an instance of a prefab is spawned in the scene (whether at runtime or during scene creation in Unity).  

In order to achieve this effect, we could have the `uniqueIdentifier` field generated on `Awake()`, which is called in both the Unity editor and at runtime.  However, this breaks if, for example, we duplicate a GameObject already present in the scene, as it will copy the unique identifier from its sibling.  

As such, we instead opt to check for both existence and uniqueness of the `uniqueIdentifier` field in the `Update()` loop, but we only do so when in the UnityEditor (i.e. by using compiler directives).

A second issue arises when viewing a prefab in prefab isolation view (i.e. double-clicking on a given prefab), as this view also triggers Unity's game loop methods, including `Update()`.  In order to prevent the `uniqueIdentifier` from being pre-filled in the prefab directly, is is necessary to guard against non-scene views, making use of `StageUtility` methods found in `UnityEditor.SceneManagement`, as below:

```C#
      private void Update()
        {
            if (Application.IsPlaying(gameObject)) { return; }
            if (string.IsNullOrEmpty(gameObject.scene.path)) { return; }
            if (StageUtility.GetStage(gameObject) != StageUtility.GetMainStage()) { return; }

            …
        }
```

#### UID Configuration

Based on the above implementation, is crucially important to set the `uniqueIdentifier` field appropriately:
* for recurring characters/objects that progress through the story, this value should be set to some fixed value
  * e.g. using a character's unique name - for Frankie, it's set to `frankie`
* for non-recurring characters/objects:
  * on pre-fabs:  this value must be kept **BLANK/EMPTY**
  * when placed on scene:  this value will set with an automatically generated GUID

## Save Editor

Access the Save Editor window via the menu bar:  `Tools` -> `Save Editor`

As shown below, the Save Editor has three main sections:
1. A header to rename, duplicate and delete the current save
2. A list view in the left panel to set the current save from available saves
3. A main view in the right panel to edit the current save

<img src="../../../InfoTools/Documentation/Scripts/Saving/SaveEditor.png" width="650">

In order to edit a save file, ensure the relevant save is currently selected in PlayerPrefs (via the `Set to Current` button), and then click the `Load Scene Data` button.  The save editor will display all saveable entities in the currently open scene view in the right panel.  

Note that as long as a [player object](../../Game/Core/Player.prefab) exists in the scene view, the save editor will show all key player properties (aside from start menu scenes, the player object should always be present in the scene view).  Player properties include player mover, party - including all party member equipment/knapsack/stats, quest list, wallet, etc. 

All other saveable entities in the current scene, such as NPC properties, check interaction (presents, doors, etc.) state, room visibility state, etc. are listed below the player cards, sorted first by any entities that are [GameStateModifiers](../Core/GameStateModifiers).

The Save Editor functionality is relatively straightforward:  edit a parameter, save the appropriate entity (or click apply all data to save all entities).  When a parameter has been adjusted, but not yet saved, the entity's card will appear red, and the specific saveable component will show `Data Sync:  Volatile` until it is saved (or the save file reloaded)

Some notable feature highlights:
* Click the `Select Entity` button to select either the object in the scene view or the prefab (e.g. for party members)
  * likewise - click the entity's game object in the scene view while the save editor is opened, and the save editor will scroll to the corresponding entity card and colour it blue
* On Mover entities, click the `Pick Position In Scene`, then click any where in the scene view to update the entity position
  * for the PlayerMover, this will also update the `Last Saved Scene` parameter if it is not matched to the currently opened scene in the scene view
  * i.e. this ensures that the scene and player position stay in sync (otherwise you may end up choosing a position for the incorrect scene and appear in unpainted landscape)
* When updating equipment, if the item is not currently in the character's knapsack, it will be added to the knapsack (if there is room)
  * likewise - if an equipped item is removed from the knapsack, it will be unequipped
* BaseStats cards include convenience buttons to automatically level/de-level characters (incrementing level by level, or in one fell swoop to a defined level)
* CombatParticipant cards include a convenience button to automatically set HP/AP to the max values defined in BaseStats

