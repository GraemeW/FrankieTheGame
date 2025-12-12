# Assets - Scripts : Saving

Note: This README is currently a WIP, thus incomplete, and will be updated in due time.

## Saveable Entities

In order for a GameObject to have its state saved, it must have a [SaveableEntity](./SaveableEntity.cs) component attached to it.  This tags the object so that is can be found by the [SavingSystem](./SavingSystem.cs).  

The [SaveableEntity](./SaveableEntity.cs) in turn finds all other components on the GameObject that implement the [ISaveable](./ISaveable.cs) interface.  Notably, it does **not** find [ISaveables](./ISaveable.cs) in the game object's children/extended hierarchy, only on the GameObject with the [SaveableEntity](./SaveableEntity.cs) itself.

### Unique Identifiers

Each [SaveableEntity](./SaveableEntity.cs) has a unique identifier for look-up (to save/restore the given GameObject's state) in the [SavingSystem](./SavingSystem.cs).  We wish to have fixed (single-value) GUIDs for the **prefabs** of special / recurring entities.  Separately, we wish to have fixed GUIDs for the **instances** generated from prefabs for other more generic entities.

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
