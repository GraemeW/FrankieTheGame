# UIBox

## Overview

The [UIBox](./Runtime/UIBox.cs) abstract class serves as the standard building block for most UI elements in Frankie.   To this end, all UI windows/boxes that act as [input receivers](../com.lowdefmustard.control/Runtime/InputReceiver/IInputReceiver.cs) should extend this class, since it contains all the necessary hooks to work with Frankie's [controllers](../../Assets/Game/Controllers/).

## State-Based Strategy Pattern

[UIBox](./Runtime/UIBox.cs) is defined with a generic `TBoxState`, which should be defined as an enum that can be used for unique state-dependent UIBox behaviours.   If the UIBox is simple in nature, it can be defined with the generic [UIBoxState](./Runtime/UIBoxState.cs) enum (though, in practice, any enum will work).

The state-based strategies for each UI box are managed through two internal state variables:
```C#
private Enum internalUIState; // current box state
private EnumLookupBase<UIBoxStateBehaviour> stateLookup; // state-dependent UIBox strategies
```
, which are accessed via protected attributes/methods:
```C#
protected TBoxState uiState
{
    get => (TBoxState)internalUIState;
    set => internalUIState = value;
} 
protected virtual EnumLookup<TBoxState,UIBoxStateBehaviour> BuildStateBehaviours();
```

Child classes of UIBox can override `BuildStateBehaviours()` to return their specific state-dependent strategies through instances of [UIBoxStateBehaviour](./Runtime/UIBoxStateBehaviour.cs).  UIBoxStateBehaviours allow alternate implementations for things like `MoveCursor()`, `HandleGlobalInput()`, `Choose()`, etc.  Naturally, since each UIBox child class defines its own `TBoxState` enum, the child class itself is responsible for updating and managing its `uiState`.

See:
* [DialogueBox](../../Assets/Scripts/UI/Speech/DialogueBox.cs):  for an example of a single-state strategy
* [InventoryBox](../../Assets/Scripts/UI/Inventory/Inventory/InventoryBox.cs):  for an example of a multi-state strategy

## Unity Methods / Overrides

Since the [UIBox](./Runtime/UIBox.cs) is an [IInputReceiver](../com.lowdefmustard.control/Runtime/InputReceiver/IInputReceiver.cs), which intercepts user input, it is necessary to strictly control and manage its lifecycle.  Failure to do so could result in a game lock-up, where a UIBox child is silently receiving and disposing of user input.  As such, enabling free access to override standard Unity methods (Awake, Start, OnEnable/Disable, Destroy) is particularly risky.

As such, all Unity methods are sealed as private in the UIBox abstract class.  Access to these methods is provided via `_____Triggered()` virtual methods, which are called at the end of the corresponding Unity methods.  This ensures that standard UIBox Unity methods are always called and the UIBoxes are always safely disposed.

## Fallback Safety Destruction Mechanisms

### Key Dependencies

[UIBox](./Runtime/UIBox.cs) provides a virtual method `TryAcquireDependencies()` that should be overridden for any child classes that have critical dependencies that **must** exist in order for the UIBox to function correctly.  This may include, e.g., acquiring a relevant [controller](../../Assets/Game/Controllers/) and establishing a controller link through its `AddInputReceiver(IInputReceiver inputReceiver, Action disableCallbacks)` method. 

Failure to establish these dependencies will result in destruction of the UIBox in the following `LateUpdate()` after its `Awake()`.

### Controller Check Coroutine

As a final stop-gap to ensure a controller is properly linked, the UIBox kicks off a controller check coroutine in `Start()`.  After a one-frame delay, if the UIBox is set to `handleGlobalInput`, this coroutine will check for existence of a controller. Failure to establish a controller will result in destruction of the UIBox in the following `LateUpdate()`.

### Requirements on UIBox Controller Link

Note that the above destruction mechanisms place a hard restriction on the initial setup of a UIBox.  Notably, any UIBox element **must** be properly configured with a controller by the end of its `Start()` (or ideally during `Awake()`).

In most cases, this is simply accomplished by instantiating and then immediately adding the UIBox as an input receiver to its corresponding controller.  Script execution is such that the UIBox will complete its `Awake()` method, but hold `Start()` until the function call that instantiated it has finished its execution.  

Or, as a specific example:
* someObject calls `SpawnUIBox()`, which:
  * instantiates a UIBox -- UIBox's `Awake()` and `OnEnable()` are called
  * passes the UIBox to a controller via `controller.AddInputReceiver(uiBox, null)`
* some time after someObject's SpawnUIBox() is completed -- UIBox's `Start()` is called, which:
  * kicks off the coroutine to check for a controller after 1-frame delay

In this case, since the controller link was established after `Awake()`, but before `Start()`, the check passes and the UIBox is not destroyed.
