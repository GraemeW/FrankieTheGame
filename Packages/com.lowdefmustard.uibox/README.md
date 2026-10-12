# Low Def Mustard UIBox

Input-driven UI box architecture: a state-based strategy pattern for cursor-navigable menus/dialogue boxes, a set of cursor-movement strategies (linear, fixed-grid, and screen-space spatial), UI Toolkit views that turn a box's choices and text into elements (text choices, sliders, toggles, choice groups), and a `TextScanBox` for scrolling/typewriter text.

- **Package name:** `com.lowdefmustard.uibox`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.unity.modules.uielements` 1.0.0
  - `com.lowdefmustard.control` 0.1.0
  - `com.lowdefmustard.utils` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.uibox": "file:../path/to/com.lowdefmustard.uibox"` in your project's `manifest.json`. `com.lowdefmustard.control` and `com.lowdefmustard.utils` must also be present.

## Assembly Structure

| Assembly              | Root Namespace        | Platform  | References                                                          |
|-----------------------|-----------------------|-----------|---------------------------------------------------------------------|
| `LowDefMustard.UIBox` | `LowDefMustard.UIBox` | Runtime   | `LowDefMustard.Control`, `LowDefMustard.Utils`                      |

This package ships Runtime-only (no custom inspector required). Every box is configured via the standard inspector and `BuildStateBehaviours()`.

## Contents

### Core Architecture — `UIBoxBase` / `UIBox<TBoxState>` (`Runtime/UIBoxBase.cs`, `Runtime/UIBox.cs`)

`UIBox<TBoxState>` is the abstract base every input-driven UI window/box in the game should extend — it implements `com.lowdefmustard.control`'s `IInputReceiver`, so it plugs directly into a `BaseController`. `UIBoxBase` holds the state-independent plumbing (choice-option bookkeeping, cursor-movement math, the link to the box's view); `UIBox<TBoxState>` layers the generic per-state strategy system and the sealed Unity lifecycle on top.

- **State-based strategy pattern:** `TBoxState` is any `enum` (the trivial `UIBoxState.Default` is provided for single-state boxes) 
  - A child class overrides `BuildStateBehaviours()` to return an `EnumLookup<TBoxState, UIBoxStateBehaviour>` mapping each state to a `UIBoxStateBehaviour` — effectively a set of nullable delegates:
    - `moveCursor`, 
    - `handleGlobalInput`, 
    - `choose`, 
    - `prepareChooseAction`, 
    - `setupChoiceOptions`, 
    - `reconcileChoiceOptions`, 
    - `isBackInput`, 
    - `tryHandleBackNavigation` 
  - Any delegate left `null` for a state falls back to the corresponding `Standard*` implementation, so a box only needs to supply the behaviours that actually diverge per-state, rather than re-implementing everything
  - The active state's behaviour is looked up fresh on every call via `uiState`, which a child class sets directly (of type `TBoxState`, backed by an internal `Enum` field)
- **Sealed Unity lifecycle:** because a `UIBox` is silently receiving/consuming input, an uncontrolled override of `Awake`/`Start`/`OnEnable`/`OnDisable`/`OnDestroy` risks leaving input silently swallowed or the box's lifecycle bookkeeping skipped
  - All of these are `private` (sealed in effect) on `UIBox<TBoxState>`; a child class hooks in via the corresponding `virtual void ...Triggered()` method (`AwakeTriggered`, `StartTriggered`, `EnableTriggered`, `DisableTriggered`, `DestroyTriggered`), called at the end of the real Unity method after the base bookkeeping has already run
- **Back-navigation hooks:** `IsBackInput(ControllerInputType)` (default: `Cancel`/`Option`) and `TryHandleBackNavigation(ControllerInputType)` (default: `false`) run first in `StandardHandleGlobalInput`, ahead of `TryEarlyExit` — letting a box intercept and consume "back" input for its own purposes (e.g. stepping back a sub-state) before falling through to the default exit-and-destroy behaviour
- **Fallback safety/destruction mechanisms:** a `UIBox` that never gets linked to a controller can lock up input entirely, so two independent guards exist:
  - 1- `TryAcquireDependencies()` (virtual, default `true`) — override for any box that self-acquires a controller/dependency in `Awake`; returning `false` destroys the box immediately, before `OnEnable`/`Start` ever run
  - 2- A `Start()`-launched coroutine gives a one-frame grace period, then destroys the box if `controller` is still `null` while `handleGlobalInput` is true — catching the case where a *parent* forgets to call `controller.AddInputReceiver(...)` after spawning a child box
    - Because of this, any box **must** have its controller link established by the end of `Start()` — in practice, this means instantiating and calling `AddInputReceiver` in the same synchronous block, before the new box's own `Start()` runs

### Cursor Movement (`Runtime/UIBoxBase.cs`, `Runtime/Utility/CursorMovementStyle.cs`, `Runtime/EntryHandles/IUIMoveInterceptor.cs`)

Three interchangeable cursor-movement strategies are available to plug into a `UIBoxStateBehaviour.moveCursor`:

- **`StandardMoveCursor`** — linear next/prev cycling through `choiceOptions`, filtered by `CursorMovementStyle` (`Horizontal`, `Vertical`, or `Combined`) so a box can restrict which input directions actually move the cursor
- **`MoveCursor2D`** — linear cycling assuming a fixed 2-column layout (up/down jump by 2, left/right by 1, with wraparound), for simple two-column choice lists without real per-row bookkeeping
- **`StandardMoveCursorSpatial`** — screen-space navigation for arbitrary (non-linear) layouts: casts a ray from the highlighted choice's navigation rect (`IUIChoice.TryGetNavigationRect`) in the input direction and picks the closest AABB hit
  - if nothing is hit directly, falls back to scoring every candidate by `alignment / distance²` within an ~85° acceptance cone of the input direction, so an off-axis-but-clearly-"that way" element still gets picked
- **`IUIMoveInterceptor`** — implemented by composite/self-navigating choices (`ChoiceGroupHandle`, `SliderChoiceHandle`) so any of the above strategies defers movement input to the highlighted choice itself before falling through to normal cursor movement

### Choices — `IUIChoice` (`Runtime/EntryHandles/IUIChoice.cs`)

A box only knows its choices through `IUIChoice`: `isAlive`, `Highlight`, `UseChoice`, `SetText` and `TryGetNavigationRect` (for spatial cursor movement). `UIBoxBase.choiceOptions` is a `List<IUIChoice>`, so box logic is testable with plain doubles.

- Choices **persist across enables** by default
  - A box that re-creates its choices each time sets `clearChoiceOptionsOnEnable` (e.g. as `TextScanBox` does)
- `AddChoiceOption` (destroys the box once chosen) / `AddNonDestroyChoiceOption` (optionally with an on-highlight action) create a text choice in the box's view. Both route through `StandardChoiceExecution`, which a box can also call from its own custom choices.
- Highlight and select raise `ReceiverModifiedType.ItemHighlighted` / `ItemSelected` on the box, so sounds are wired once per box rather than per choice.

### Views, Entries, Elements (`Runtime/Views/`, `Runtime/EntryHandles/`, `Runtime/*Elements/`, `Runtime/*Models/`)

A box's visuals come from a view component on the same GameObject, backed by a `PanelRenderer`. A box without a view has no visuals (it cannot create choices, and visibility calls do nothing).

- **`UIToolkitBoxView`** (abstract) — binds whenever the renderer reloads its UI: applies the view's style sheets, visibility, pointer blocking (an inactive box ignores clicks), a root data source for model-bound elements, and the back-exit button (`BackExitButton` in the UXML, shown once the box sets an action && skipped when `preventEscapeOptionExit` is set)
  - It owns the box's **entries** and places each in the first UXML element of the entry's container type
- **`UIToolkitMenuView`** — the view for menus
  - **`UIToolkitTextScanView`** — adds text entries and the choice layout for `TextScanBox`
- **Entries (`EntryHandles/`)** — The box logic holds the `EntryHandle`, while the view rebuilds its element on every UI reload
  - `ChoiceHandle` (abstract, an `IUIChoice`): shared click / choose / highlight handling for selectable entries (subclass it for custom rows)
  - `ChoiceEntryHandle` (text choice), `SliderChoiceHandle`, `ToggleChoiceHandle`, `ChoiceGroupHandle` (a row of sub-choices behind one choice slot), `TextEntryHandle`, `SeparatorEntryHandle`
- **Models + elements** — handles write to `BindableModel`s (`TextEntryModel`, `ChoiceEntryModel`, ...) && the matching `[UxmlElement]` controls bind to them
  - `ModelBoundLabel` is the base for typed labels bound to a screen model. `ChoiceRowParts` and `USSClassNames` are the shared construction and class names for choice rows
- A view nested under another view's GameObject shares its parent's visual tree (it is stretched over the parent's root, and inherits its data source and pointer blocking)

### `TextScanBox` (`Runtime/TextScanBox.cs`)

A concrete `UIBox<UIBoxState>` implementation for scrolling/typewriter-style text — the base a dialogue or message box would build on. It requires a `UIToolkitTextScanView`.

- Queues text/speech entries (`AddText`, `AddSpeech`), choices (`QueueChoiceForPrinting`) and page breaks (`AddPageBreak`) for sequential display, printing text one character at a time (`delayBetweenCharacters`)
  - a queued choice is revealed once the text before it has printed
- `Execute` input skips remaining characters on the current page (`SkipToEndOfPage`/`TryFastForwardActiveText`) rather than always advancing immediately, so mashing through dialogue feels responsive without accidentally skipping unread text
- `UnescapeText` resolves common C#-style escape sequences (`\n`, `\t`, `\uXXXX`, `\xH`-`\xHHHH`, etc.) in authored text
- `initialInputDelay` briefly blocks input right after the box appears, to absorb the same button-press that opened it

## Tests

### Testing Notes

Any test activating a `UIBox<TBoxState>` double needs a controller set via `TrySetController` first, or the missing-controller destroy coroutine kills it mid-test.

Box and handle logic is tested without any UI loaded: `TestChoice` is a plain `IUIChoice` (with a given navigation rect), and a view with no loaded panel simply never binds, leaving its entries as handles + models (the handles expose a few `internal` model values for this).

Views and elements are tested on a loaded panel (`PanelTestBase`): each test spawns a `PanelRenderer` + view with a runtime `PanelSettings` and a small UXML from `Tests/Runtime/TestAssets`, then waits for the UI reload that binds the view. Bindings, styles and layout only resolve in a frame's panel update, so these tests `Settle()` after a model or view change before asserting on elements. The assets are loaded through the `AssetDatabase` (`TestPanelAssets`), so the panel tests are editor play-mode only.

### Assemblies

| Assembly                            | Root Namespace                      | Platform    | References                                                                                                                                                                  |
|--------------------------------------|--------------------------------------|-------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LowDefMustard.UIBox.Tests.Editor`  | `LowDefMustard.UIBox.Tests.Editor`  | Editor only | `LowDefMustard.UIBox`, `LowDefMustard.UIBox.Tests`, `LowDefMustard.Control`, `LowDefMustard.Utils`, `UnityEditor.TestRunner`, `UnityEngine.TestRunner` |
| `LowDefMustard.UIBox.Tests` | `LowDefMustard.UIBox.Tests` | Runtime     | Same as above                                                                                                                                                               |

### Coverage at a glance

Tested / Total counts the units named in each row (static functions, behaviours or types).

| Category                                                         | Tested / Total | Notes                                                                                                          |
|------------------------------------------------------------------|:--------------:|----------------------------------------------------------------------------------------------------------------|
| Cursor/ray math (`UIBoxBase` internals) + `UnescapeText`         |      6/6       | Move, 2D move, ray hit, angle match, ray/rect intersection, unescape                                           |
| `UIBoxBase` / `UIBox<TBoxState>`                                 |      6/7       | `AddChoiceOption`'s click-through-destroy skipped                                                              |
| `TextScanBox`                                                    |      8/8       | Text, choice and page-break jobs, through handles + models                                                     |
| Entry handles                                                    |      7/7       | All handles tested on a laid-out panel; `SeparatorEntryHandle` through its element                             |
| Views (`UIToolkitBoxView`, `UIToolkitMenuView`, `UIToolkitTextScanView`) |   3/3    | Rebind on UI reload, style sheets, containers, view state, back-exit, pointer blocking, nesting, choice layout |
| Elements + models                                                |     10/10      | Model -> element bindings, classes, clicks and slider changes on a loaded panel                                |

### Detail by type

| Type                                                 | Status  | Test file(s)                                                                                                                                             | Notes                                                                                                                |
|------------------------------------------------------|---------|----------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `UIBoxBase` (static cursor/ray math)                 | Yes     | `UIBoxBaseCursorMathTests.cs`, `UIBoxBaseScreenSpaceTests.cs`                                                                                            |                                                                                                                      |
| `UIBoxBase` / `UIBox<TBoxState>` (instance)          | Partial | `UIBoxChoiceAndCursorTests.cs`, `UIBoxHandleGlobalInputTests.cs`, `UIBoxLifecycleTests.cs`, `UIBoxStateBehaviourDispatchTests.cs`, `EntryHandleTests.cs` | `AddChoiceOption` click-through-destroy skipped - `Destroy()` timing inside a click callback is unreliable to assert |
| `UIBoxStateBehaviour`                                | Yes     | `UIBoxStateBehaviourTests.cs`                                                                                                                            |                                                                                                                      |
| `TextScanBox`                                        | Yes     | `TextScanBoxTests.cs`, `TextScanBoxUnescapeTextTests.cs`                                                                                                 |                                                                                                                      |
| `ChoiceHandle` / `ChoiceEntryHandle`                 | Yes     | `EntryHandleTests.cs`, `EntryHandleNavigationRectTests.cs`                                                                                               | Logic without a panel; navigation rect once laid out                                                                 |
| `TextEntryHandle`                                    | Yes     | `TextScanBoxTests.cs`, `TextScanElementTests.cs`                                                                                                         | Through the text scan; element binding on a panel                                                                    |
| `SliderChoiceHandle`                                 | Yes     | `EntryHandleTests.cs`, `MenuElementTests.cs`                                                                                                             |                                                                                                                      |
| `ToggleChoiceHandle`                                 | Yes     | `EntryHandleTests.cs`, `MenuElementTests.cs`                                                                                                             |                                                                                                                      |
| `ChoiceGroupHandle`                                  | Yes     | `EntryHandleTests.cs`, `MenuElementTests.cs`                                                                                                             |                                                                                                                      |
| `UIToolkitBoxView` (+ menu / text scan)              | Yes     | `UIToolkitBoxViewTests.cs`, `UIToolkitTextScanViewTests.cs`, `EntryHandleTests.cs`, `TextScanBoxTests.cs`                                                | Pointer blocking checked at the root (a pointer down never reaches a child), not through a full pointer click        |
| Elements (`*Element`, containers, `ModelBoundLabel`) | Yes     | `TextScanElementTests.cs`, `MenuElementTests.cs`                                                                                                         | Slider drag stood in for by a value change on the inner `Slider`                                                     |

## Design Notes

- Unity lifecycle methods are sealed private specifically to prevent the "silently eats input forever" failure mode — see **Fallback safety/destruction mechanisms** above. This is a deliberate constraint on all `UIBox<TBoxState>` subclasses, not an oversight.
- `MoveCursor2D` and `StandardMoveCursorSpatial` are kept as separate, independently-selectable strategies rather than one replacing the other: 2D is cheaper and predictable for a genuinely tabular layout, while spatial handles arbitrary/irregular layouts (e.g. a keyboard) correctly.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
