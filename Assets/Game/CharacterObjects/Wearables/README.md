# Assets: Game - Wearables

New wearables should be built as variants of the [WearablesRoot](./WearableRoot.prefab) prefab.  

By default, this prefab includes:
* Components:
  * [Wearable](../../../Scripts/Inventory/Wearable.cs), which:
    * interfaces with the wearables system - notably [WearablesLink](../../../Scripts/Inventory/WearablesLink.cs) on the character
    * adjusts the wearables animator state as a function of the character position/look direction & speed
    * modifies the character's stats via [IModifierProvider](../../../Scripts/Stats/IModifierProvider.cs)
  * Animator -- as above, adjusts the wearable animation (sprite) as a function of character animation state
    * generally, the animator controller can derive from the [PCAnimatorController](../PCs/PCAnimatorController.controller), or any animator controller using animation parameters:
      * xLook
      * yLook
      * Speed
* Two childed game objects with sprite renderers:
  * CharacterBackground: for any sprite elements that appear behind the character
  * CharacterForeground: for any sprite elements that appear in front of the character
