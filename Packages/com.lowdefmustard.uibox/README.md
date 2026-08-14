# Low Def Mustard UIBox

Reusable input-driven UI box architecture: a state-based strategy pattern for cursor-navigable menus/dialogue boxes, a set of cursor-movement strategies (linear, fixed-grid, and screen-space spatial), a `UIChoice` widget family (buttons, sliders, toggles, sub-option containers), and a ready-made `TextScanBox` for scrolling/typewriter text.

- **Package name:** `com.lowdefmustard.uibox`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** 
  - `com.unity.ugui` 2.5.0
  - `com.lowdefmustard.control` 0.1.0
  - `com.lowdefmustard.utils` 0.1.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.uibox": "file:../path/to/com.lowdefmustard.uibox"` in your project's `manifest.json`. `com.lowdefmustard.control` and `com.lowdefmustard.utils` must also be present.

## Assembly Structure

| Assembly              | Root Namespace        | Platform  | References                                                          |
|-----------------------|-----------------------|-----------|---------------------------------------------------------------------|
| `LowDefMustard.UIBox` | `LowDefMustard.UIBox` | Runtime   | `LowDefMustard.Control`, `LowDefMustard.Utils`, `Unity.TextMeshPro` |

This package ships Runtime-only — there's no custom inspector here; every box is configured via the standard inspector and `BuildStateBehaviours()`.

## Contents

### Core Architecture — `UIBoxBase` / `UIBox<TBoxState>` (`Runtime/UIBoxBase.cs`, `Runtime/UIBox.cs`)

`UIBox<TBoxState>` is the abstract base every input-driven UI window/box in the game should extend — it implements `com.lowdefmustard.control`'s `IInputReceiver`, so it plugs directly into a `BaseController`. `UIBoxBase` holds the state-independent plumbing (choice-option bookkeeping, cursor-movement math); `UIBox<TBoxState>` layers the generic per-state strategy system and the sealed Unity lifecycle on top.

- **State-based strategy pattern:** `TBoxState` is any `enum` (the trivial `UIBoxState.Default` is provided for single-state boxes). 
  - A child class overrides `BuildStateBehaviours()` to return an `EnumLookup<TBoxState, UIBoxStateBehaviour>` mapping each state to a `UIBoxStateBehaviour` — effectively a set of nullable delegates:
    - `moveCursor`, 
    - `handleGlobalInput`, 
    - `choose`, 
    - `prepareChooseAction`, 
    - `setupChoiceOptions`, 
    - `reconcileChoiceOptions`, 
    - `isBackInput`, 
    - `tryHandleBackNavigation` 
  - Any delegate left `null` for a state falls back to the corresponding `Standard*` implementation, so a box only needs to supply the behaviours that actually diverge per-state, rather than re-implementing everything. 
  - The active state's behaviour is looked up fresh on every call via `uiState`, which a child class sets directly (of type `TBoxState`, backed by an internal `Enum` field).
- **Sealed Unity lifecycle:** because a `UIBox` is silently receiving/consuming input, an uncontrolled override of `Awake`/`Start`/`OnEnable`/`OnDisable`/`OnDestroy` risks leaving input silently swallowed or the box's lifecycle bookkeeping skipped. 
  - All of these are `private` (sealed in effect) on `UIBox<TBoxState>`; a child class hooks in via the corresponding `virtual void ...Triggered()` method (`AwakeTriggered`, `StartTriggered`, `EnableTriggered`, `DisableTriggered`, `DestroyTriggered`), called at the end of the real Unity method after the base bookkeeping has already run.
- **Back-navigation hooks:** `IsBackInput(ControllerInputType)` (default: `Cancel`/`Option`) and `TryHandleBackNavigation(ControllerInputType)` (default: `false`) run first in `StandardHandleGlobalInput`, ahead of `TryEarlyExit` — letting a box intercept and consume "back" input for its own purposes (e.g. stepping back a sub-state) before falling through to the default exit-and-destroy behaviour.
- **Fallback safety/destruction mechanisms:** a `UIBox` that never gets linked to a controller can lock up input entirely, so two independent guards exist:
  - `TryAcquireDependencies()` (virtual, default `true`) — override for any box that self-acquires a controller/dependency in `Awake`; returning `false` destroys the box immediately, before `OnEnable`/`Start` ever run.
  - A `Start()`-launched coroutine gives a one-frame grace period, then destroys the box if `controller` is still `null` while `handleGlobalInput` is true — catching the case where a *parent* forgets to call `controller.AddInputReceiver(...)` after spawning a child box. Because of this, any box **must** have its controller link established by the end of `Start()` — in practice, this means instantiating and calling `AddInputReceiver` in the same synchronous block, before the new box's own `Start()` runs.

### Cursor Movement (`Runtime/UIBoxBase.cs`, `Runtime/CursorMovementStyle.cs`, `Runtime/IUIMoveInterceptor.cs`)

Three interchangeable cursor-movement strategies are available to plug into a `UIBoxStateBehaviour.moveCursor`:

- **`StandardMoveCursor`** — linear next/prev cycling through `choiceOptions`, filtered by `CursorMovementStyle` (`Horizontal`, `Vertical`, or `Combined`) so a box can restrict which input directions actually move the cursor.
- **`MoveCursor2D`** — linear cycling assuming a fixed 2-column layout (up/down jump by 2, left/right by 1, with wraparound), for simple two-column choice lists without real per-row bookkeeping.
- **`StandardMoveCursorSpatial`** — screen-space navigation for arbitrary (non-linear) layouts: casts a ray from the highlighted choice's screen rect in the input direction and picks the closest AABB hit; if nothing is hit directly, falls back to scoring every candidate by `alignment / distance²` within an ~85° acceptance cone of the input direction (the same heuristic Unity's built-in gamepad/keyboard UI navigation uses), so an off-axis-but-clearly-"that way" element still gets picked.
- **`IUIMoveInterceptor`** — implemented by composite/self-navigating choices (`UIChoiceContainer`, `UIChoiceSlider`) so any of the above strategies defers movement input to the highlighted choice itself before falling through to normal cursor movement.

### Choice Widgets (`Runtime/UIChoice.cs`, `Runtime/UIChoiceButton.cs`, `Runtime/UIChoiceContainer.cs`, `Runtime/UIChoiceSlider.cs`, `Runtime/UIChoiceToggle.cs`)

- **`UIChoice`** (abstract) — Base for anything selectable by cursor: highlight visuals (marker + optional color/bold "selected" styling, optional dimming for invalid choices), an `itemHighlighted` `UnityEvent`, and `choiceOrder` for deterministic ordering when choices are auto-discovered from `optionParent`.
- **`UIChoiceButton`** — Standard clickable choice; `UseChoice()` invokes the underlying `Button`'s `onClick`.
- **`UIChoiceContainer`** — Groups several `UIChoice`s (e.g. a row of sub-buttons) behind a single top-level choice slot; implements `IUIMoveInterceptor` to navigate within the group before yielding back to the parent box's cursor movement. `UIBoxBase.FilterOutSubOptions` excludes a container's children from the box's own flat `choiceOptions` list so they aren't double-counted.
- **`UIChoiceSlider`** — Wraps a `Slider`; `IUIMoveInterceptor.TryMove` adjusts the value by `sliderAdjustmentStep` on left/right input instead of moving the cursor away.
- **`UIChoiceToggle`** — Wraps a `Toggle`; `UseChoice()` flips it, with a silent setter (`SetToggleValueSilently`) for programmatic updates that shouldn't fire listeners.

### `UIBackExit` (`Runtime/UIBackExit.cs`)

Thin wrapper around a `UIChoiceButton` that a box can instantiate as an explicit on-screen "back/exit" button, wired to route its click through the same input path (`HandleInputWrapper(ControllerInputType.Escape)`) as a controller Escape press. Skipped automatically when `preventEscapeOptionExit` is set.

### `TextScanBox` + `SimpleTextLink` (`Runtime/TextScanBox.cs`, `Runtime/SimpleTextLink.cs`)

A concrete `UIBox<UIBoxState>` implementation for scrolling/typewriter-style text — the base a dialogue or message box would build on:

- Queues text/speech entries (`AddText`, `AddSpeech`) and page breaks (`AddPageBreak`) for sequential display, printing one character at a time (`delayBetweenCharacters`) via `SimpleTextLink` (a thin `TextMeshProUGUI` wrapper that self-disables when empty).
- `Execute` input skips remaining characters on the current page (`SkipToEndOfPage`/`TryFastForwardActiveText`) rather than always advancing immediately, so mashing through dialogue feels responsive without accidentally skipping unread text.
- `UnescapeText` resolves common C#-style escape sequences (`\n`, `\t`, `\uXXXX`, `\xH`-`\xHHHH`, etc.) in authored text.
- `initialInputDelay` briefly blocks input right after the box appears, to absorb the same button-press that opened it.

## Design Notes

- Unity lifecycle methods are sealed private specifically to prevent the "silently eats input forever" failure mode — see **Fallback safety/destruction mechanisms** above. This is a deliberate constraint on all `UIBox<TBoxState>` subclasses, not an oversight.
- `MoveCursor2D` and `StandardMoveCursorSpatial` are kept as separate, independently-selectable strategies rather than one replacing the other: 2D is cheaper and predictable for a genuinely tabular layout, while spatial handles arbitrary/irregular layouts (e.g. a keyboard) correctly.

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
