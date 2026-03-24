# Frankie Requirements

**NOTE:  THIS README IS INCOMPLETE AND A WORK IN PROGRESS**

## Nomenclature: Definitions

* **must**:  indicates a requirement -- an absolute contract with no room for discretion
* **should**:  indicates a recommendation -- normative, but not strictly binding
* **may**:  indicates a permission -- technically permissible, but not always normative
* **can**:  indicates a possibility or capability -- typically used in definitions

## Framework, Language and Dependencies

Frankie is developed using [Unity](https://unity.com).  

Since development is still at a relatively early stage, Frankie is typically developed on the latest stable LTS release -- see [ProjectVersion](../../ProjectSettings/ProjectVersion.txt).

Beyond the default/standard packages, the following packages are used:
* Universal Render Pipeline
* 2D - Common, Sprite, Animation, Tilemap Editor, Tilemap Extras, Pixel Perfect
* Addressables
* Input System (i.e. "New" Input System)
* Cinemachine (version 2.x -- TBU to 3.x pending)
* Timeline
* Shader Graph
* uGUI
* Newtonsoft Json

Frankie must **not** include any other dependencies or libraries beyond those provided through Unity or C#.

## Core

### Singletons and Singleton Handling

#### Player

The Player game object must include a Player component (MonoBehaviour) attached to its root.  

The Player component must:
* implement the singleton design pattern for the player game object
* provide static look-up methods for the player game object
  * it may provide static look-up methods for other critical components on the Player (e.g. controllers, state machines, etc.)
* provide static look-up methods for any player-specific properties (e.g. layers, tags, etc.)

An example static look-up method using Unity's tag system is provided below for reference:
```C#
public static Player FindPlayer()
{
    var playerGameObject = GameObject.FindGameObjectWithTag(_playerTag);
    return playerGameObject != null ? playerGameObject.GetComponent<Player>() : null;
}
```

The Player component should also include a method for handling player state changes, such as to check for game over criteria.

#### Persistent Objects

Other singleton game objects must be childed to a PersistentObjects game object.  The PersistentObjects

### Addressables

### Camera Cinematics

## Zones (Scene Management)

Zones are addressable scriptable objects that allow for referencing and transitioning between scenes during playtime.  They include ZoneNodes to define locations with the zone, and are manipulated by ZoneHandlers, which further employ the SceneLoader/Fader classes to allow traversal throughout sed locations.

### Zone Class

####  Properties:  Interfaces and Tunables

Since they are scriptable objects, the Zone class must derive from the Unity ScriptableObject class.  It must furthermore implement the following interfaces:
* IAddressablesCache:  to implement default addressables cache building methods
* ISerializationCallbackReceiver:  to allow for custom editor properties

A zone object must include the below properties:
* Scene reference:  matching to the corresponding Unity Scene
* Audio clip:  to play audio when the zone is active
  * + any additional audio properties (such as looping behaviour, etc.)
* Boolean map update flag:  to indicate if the minimap should update when in the zone

, along with relevant public getter methods for each property.

#### State:  ZoneNode Class

A Zone must constain a serialized list of ZoneNodes, which are scriptable objects childed to the Zone that define various points of interest/travel within the Zone.  This is the master list of ZoneNodes within a given Zone.

THe ZoneNode class in turn must contain:
1. a list of linked ZoneNodes within the same Zone
2. an optional reference to a ZoneNode in a different Zone

, with corresponding public getter methods for each.

#### Public Methods




### Zone Handler



### Transitions:  



## Rendering

## Sound

## Controllers (Input)

Player input must follow the Unity "New" input system, listening to `performed` and `canceled` events.  Input must be allowed via either keyboard/mouse and/or gamepad, with joystick movement mapped to behave like D-pad input.  Diffrent controllers may use different input profiles and may therefore have different events to follow (i.e. depending on the context of interaction).

The following controllers are required:
* Player Controller:  for movement/control in the world
* Dialogue Controller:  for navigating through dialogue menus
* Battle Controller:  for interfacing with the battle system
* StartMenu/Splash Controllers:  lightweight controllers for the splash/start/game win/game over screens

### Interfaces 

### Player Controller

### Check Interactions

## Predicates


## Stats

## Combat

## Dialogue/Speech

## Inventory

## Quests

## UI

## Save System
