# Low Def Mustard Rendering

Camera + display resolution handling, pixel-art shaders, a library of HLSL functions exposed as Shader Graph nodes (signed distance functions, domain operations, time, colour, hashing), and a composable SDF battle effect framework.

- **Package name:** `com.lowdefmustard.rendering`
- **Version:** 0.1.0
- **Unity:** 6000.6+
- **Dependencies:** 
  - `com.unity.cinemachine` 6.6.0
  - `com.unity.render-pipelines.universal` 17.6.0
  - `com.unity.modules.uielements` 1.0.0

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.rendering": "file:../path/to/com.lowdefmustard.rendering"` in your project's `manifest.json`.

Note that HLSL files include each other by project-relative path (`Packages/com.lowdefmustard.rendering/...`), so the package must keep its name for the shaders to compile.

## Samples

- **Battle Effect** (`Samples~/BattleEffect`, import from the Package Manager's Samples tab)
  - Includes a shared `BattleEffect` shader graph, a material using it and an example recipe (`CirclePopRecipe`)
  - Battle effects need this graph (or one like it) to draw ~ the package otherwise ships only the HLSL it's built around

## Assembly Structure

| Assembly                         | Root Namespace                   | Platform    | References                                                     |
|----------------------------------|----------------------------------|-------------|----------------------------------------------------------------|
| `LowDefMustard.Rendering`        | `LowDefMustard.Rendering`        | Runtime     | `Unity.Cinemachine`, `Unity.RenderPipelines.Universal.Runtime` |
| `LowDefMustard.Rendering.Editor` | `LowDefMustard.Rendering.Editor` | Editor only | `LowDefMustard.Rendering`                                      |

## Contents

### Camera (`Runtime/Camera`)

- **`CameraControllerBase`** (abstract `MonoBehaviour`, found via a `"CameraController"` tag through `GetCameraController()`) — Drives a Cinemachine state-driven camera with an active + idle virtual camera pair, both following a default target and animated by that target's `Animator`
  - Subclasses supply the default target (`FindDefaultFollowTarget`) and subscribe to whatever changes it (`SubscribeToDefaultFollowTarget`), calling `SetDefaultFollowTarget` when it does (e.g. a party leader swap)
  - `OverrideCameraFollower(animator, target)` temporarily follows something else, and `RefreshDefaultCameras()` returns to the default target
  - Rescales both cameras' orthographic sizes on `DisplayResolutions.resolutionUpdated`, raising `activeOrthoSizeUpdated`
  - Stacks the spawn-assist camera onto the main camera (URP camera stack)
- **`CameraSpawnAssist`** — Keeps an overlay camera's orthographic size a fixed multiple of the active camera's (i.e. sees slightly past the screen edge, so things can spawn just out of view)

### Display (`Runtime/Display`)

- **`DisplayResolutions`** (static) — Best-fit windowed resolutions for the current display (integer divisions of the display, scaled 4:3 for windowed mode), full-screen width resolution (capped for ultra-wide), resolution change detection + the `resolutionUpdated(ResolutionScaler, cameraScaling)` event, and window centring
- **`DisplaySettingMonitorBase`** (abstract `MonoBehaviour`) — On first run, picks the best windowed resolution and centres the window; polls for resolution changes each frame; saves the resolution on destroy
  - Subclasses decide whether the resolution is already configured (`IsResolutionAlreadyConfigured`) and how it's persisted (`SaveResolutionSetting`) - typically via custom PlayerPrefs implementations
- **`ResolutionSetting`** / **`ResolutionScaler`** — Serializable full-screen mode + size, and a numerator / denominator scale

### Shaders (`Runtime/Shaders`)

- **`ShaderPropertyRefs`** (static) — Shared shader property names (`_MainTex`, `_Tint`, `_Phase`, `_FadeTime`, `_FadeOutTime`), with setters for renderers (one reused `MaterialPropertyBlock`), materials and UI Toolkit `MaterialDefinition`s
- **`BattleEntryShaderControl`** — Sets a battle entry filter material's tint, phase (`Time.time`) and fade timings once per fade
- **`ShaderQuad`** (static) — Draws one quad over a `VisualElement`'s content rect from `generateVisualContent`, for elements drawn by a custom material (`style.unityMaterial`); UV v runs upwards, as sprite shaders expect
- **`_PixelArtShaders/`** — `PixelArtShader` (+ material) for pixel art at arbitrary resolutions: nearest-neighbour inside texels, bilinear across texel edges, so art stays sharp without shimmer at non-integer scales (see [Pixel-Perfect Rendering](../../Assets/Scripts/Rendering/README.md#pixel-perfect-rendering) in Frankie)
  - `Deprecated-PAS_TBNN` is kept for reference only
- **`HLSLUtilities/`** — HLSL functions exposed to Shader Graph via the [reflection API](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.6/manual/Custom-nodes-hlsl-create-node-by-reflection.html), under `LDM/` in the node search:

| File                                            | Nodes                                                                                                                                                                                                            |
|-------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `SDFShapes`                                     | `SDF Circle`, `SDF Rhombus`, `SDF Segment`, `SDF Chevron`, `SDF Squiggle`, `SDF Lens`, `SDF Jagged Line`, `SDF Wedge`, `SDF Box`, `SDF Triangle`, `SDF Star`, `SDF Regular Polygon`, `SDF Ziggurat`, `SDF Onion` |
| `SDFDomain`                                     | `SDF Aspect Fit`, `SDF Transform`, `SDF Mirror`, `SDF Repeat Line`, `SDF Repeat Grid`, `SDF Repeat Radial`, `SDF Elongate`, `SDF Stripe Mask`, `Waveform Value`                                                  |
| `TimeUtility`                                   | `Time Since Initialization`, `Period Time`, `Stepped Time`, `Ease Progress`, `Step Progress`, `Cycle Index`, `Pattern Index`, `Flicker`, `Flicker Pattern`, `Pulse`, `Jitter`                                    |
| `ColourUtility`                                 | `Colour Gradient`                                                                                                                                                                                                |
| `HashUtility`                                   | `Hash 2-2`                                                                                                                                                                                                       |
| `SDFParser`                                     | `SDF Parser`                                                                                                                                                                                                     |
| `BattleEffectComposite`                         | `Battle Effect Composite` (per-item pipeline behind a battle effect graph)                                                                                                                                       |
| `BattleEffectShapes`                            | Shape adapters (`<ClassName>Distance`), read by the generated dispatch                                                                                                                                           |
| `Generated/BattleEffectDispatch.generated.hlsl` | Written by `BattleEffectDispatchGenerator`                                                                                                                                                                       |

Note that the library guards its own inputs - every graph using a node is protected.

### Battle Effects (`Runtime/BattleEffects`)

Battle effects employ one shared shader graph (built around the `Battle Effect Composite` node) with custom recipe assets to define their behaviour.

- **`BattleEffectRecipe`** (`ScriptableObject`, `BattleAction/Effects/Battle Effect Recipe`) — Frame rate (stepped, held-frame time), scale ratio, edge softness (pixels), glow width (EU), a list of items, and instances (other recipes flattened in at bake:  re-timed, mirrored about the centre, scaled per axis about the centre + offset, recoloured)
  - Items are baked into a cached `RGBAFloat` data texture (one row per item, columns per `BattleEffectDataLayout`), and updated in place on edit so live effects + previews pick up changes
  - `ApplyTo(ref MaterialDefinition)` binds the texture + recipe settings onto an element's material
  - Editor-only `ValidateShapes` repairs inspector list edits - e.g. zero-filled first entries, shapeless entries (→ circle) and duplicated entries sharing one `[SerializeReference]` shape
  - Editor-only `ValidateInstances` resets a zero-filled instance scale ratio (a fresh first list entry) to 1
- **`BattleEffectItem`** — Label, `[SerializeReference]` shape, timing (start / duration / easing [incl. approach + its ratio] / progress steps), transform ranges (position, scale, elongation, item rotation, shape rotation), style (fill / outline / fill and outline, blend over / erase, thickness, colour, outline colour or palette step from the fill), alt colours (palette entries B-D), outline edge gradient, stripes (hard band mask in effect space), clip (box / diamond cut in local space, w/ animatable centre offset), repetition (none / line / cross / grid / radial / staggered grid, count, spacing, copy scale ratio, copy wave [line / cross copies offset across their line by a waveform], mirror x / y [+ spread beat offsets:  after the fold]), colour cycle (by time, per repetition copy or a palette-index step pattern), beats (up to 4 held steps:  offset, elongation, colour), jitter (random / orbit), flicker (square wave or step pattern) and pulse
  - Ranges run start → end over the eased duration
  - Flicker / jitter / pulse run on the effect's clock to ensure items stay in sync (flicker pattern runs from the item's start)
  - Positions + sizes are in effect units: 0–1 spans the effect's shorter side, centred, so shapes keep their proportions on any element size
  - Setting names carry their unit as a suffix: `EU` effect units, `SU` shape units (1 = the item's scale), `Ratio`, `Fraction` (0–1, or signed where noted), `Seconds`, `Degrees`, `Pixels`, `Rate` (per second), `PerEU`
  - `ApplyTo(ref materialDefinition, fit)` picks how effect units map onto the element
    - `BattleEffectFit.AspectFit` (default, 0–1 = the centred square)
    - `BattleEffectFit.Stretch` (0–1 = the whole element, e.g. a full-screen effect, where shapes stretch with it)
- **`BattleEffectShape`** (abstract) + **`[BattleEffectShapeId(n)]`** — Subclasses hold only shape-specific settings (≤ 8 animatable ranges): `BattleEffectCircle` (0), `BattleEffectBox` (1), `BattleEffectTriangle` (2), `BattleEffectSegment` (3), `BattleEffectDiamond` (4), `BattleEffectStar` (5), `BattleEffectPolygon` (6), `BattleEffectLens` (7), `BattleEffectChevron` (8), `BattleEffectWedge` (9), `BattleEffectSquiggle` (10), `BattleEffectJaggedLine` (11), `BattleEffectZiggurat` (12)
- **`BattleEffectTint`** — Solid / linear / wave / radial gradient between two colours (angle degrees, bands per EU, scroll rate), applied in the shader, such that one recipe can serve many colour variants
- **`BattleEffectDataLayout`** — Texel columns of an item row, grouped + ordered as the item's sections (every public const is then mirrored into HLSL)
- **`BattleEffectFloatRange`** / **`BattleEffectVector2Range`** — Start → end values (equal = constant)
- **Item settings groups** — `[Serializable]` classes w/ their own `WriteData` + drawer:  `BattleEffectStyle`, `BattleEffectAltColours`, `BattleEffectOutlineEdgeGradient`, `BattleEffectStripes`, `BattleEffectClip`, `BattleEffectRepetition`, `BattleEffectColourCycle`, `BattleEffectBeats`, `BattleEffectJitter`, `BattleEffectFlicker`, `BattleEffectPulse` (timing + transform stay loose item fields)
- Enums: `BattleEffectFit`, `BattleEffectEasing`, `BattleEffectStyleMode`, `BattleEffectRepetitionMode`, `BattleEffectJitterMode`, `BattleEffectTintMode` (values match their HLSL counterparts, and those marked `[BattleEffectMirroredEnum]` are generated as defines)

### Battle Effect Editor Tooling (`Editor/BattleEffects`)

- **`BattleEffectDispatchGenerator`** (`[InitializeOnLoad]`, `Tools/Battle Effects/Regenerate Shape Dispatch`) — After every script reload, writes `BattleEffectDataLayout` consts, `[BattleEffectMirroredEnum]` enums and the shape ID → adapter dispatch into `Generated/BattleEffectDispatch.generated.hlsl` (only when the output changes); logs an error for a shape without an ID or a duplicate ID
- **Battle Effect Preview** (`BattleEffectPreviewWindow`, `Tools/Battle Effects/Preview`, double-click a recipe, or a recipe's context menu) — Plays a recipe w/o entering play mode: tint + background, size presets + fit mode, play / pause / frame step / loop / speed, retro toggles (stepped time, flicker, jitter), a timeline w/ one bar per item (click / drag to scrub), reference frames (a folder of PNGs side by side or overlaid, at their own frame rate) and offscreen frame export to `Temp/BattleEffectPreview/<recipe>/frame_####.png` (optionally transparent)
  - **`BattleEffectOffscreenRenderer`** — Draws the preview + export through a hidden runtime panel into a render texture, so their colours match the game (editor UI renders in gamma, which would leave the effect's linear output unconverted)
  - **`BattleEffectPreviewSizes`** — Size presets for the window
    - Register a project's own (e.g. its UI element sizes) from an `[InitializeOnLoad]` class via `Register(name, size)`
    - A default square is listed while nothing is registered
  - Export renders through a hidden `PanelRenderer` into a `RenderTexture` and reads that back (never the screen); transparent exports are un-premultiplied
- **Recipe inspector** (`BattleEffectRecipeEditor`) — Recipe settings, then every item + instance in timeline order (start, then end):  rows edit their label in place (double-click;  items also their start + shape) + delete;  `Add` / `Add+` (item or instance) append one starting w/ / after the last entry.  Selected entries (multi-select) show their full settings below, in list order (instances tinted)
  - The selection lives in `BattleEffectRecipeSelection`:  while the preview is paused, scrubbing / stepping selects the items + instances active at that time
  - Copy / paste (`BattleEffectClipboard`, right-click):  whole items / instances (header or row), sections, shapes;  keyed on the owner's type + the copied fields (pastes can only land on a matching set);  undoable
  - Each item's settings are `BattleEffectSection` foldouts:  timing, transform + style always open;  the rest open + label green only while in use;  fields that don't apply are hidden (e.g. outline settings for a fill, a rate's settings at rate 0, count y outside grids)
- **Drawers** — `BattleEffectShapeDrawer` (type picker listing every shape subclass;  honours `[BattleEffectShownWhen]`), `BattleEffectRangeDrawer` (start → end on one row;  inputs tinted green where start != end), `BattleEffectTintDrawer` (shows only the selected mode's fields), one per item settings group (`BattleEffectSection`s:  optional groups go green + open while in use;  `BattleEffectRateDrawer` serves jitter / flicker / pulse)

## Extending Battle Effects

### Adding a battle effect shape

1. A primitive in `SDFShapes.hlsl` (origin-centred), if one doesn't exist
2. An adapter `<ClassName>Distance(BattleEffectShapeInput)` in `BattleEffectShapes.hlsl`
3. A `[Serializable]` sealed `BattleEffectShape` subclass with a unique `[BattleEffectShapeId(n)]`, whose `GetParameters` writes ≤ 8 ranges in the order the adapter reads them
   - A setting that only applies to some values of another (e.g. Squiggle's duty, pulse only) takes `[BattleEffectShownWhen(nameof(otherField), values...)]` so the inspector hides it otherwise

The drawer and the generated dispatch pick the new shape up automatically. A missing adapter shows up as a shader error naming the function.

### Adding per-item data

A `BattleEffectDataLayout` const (in its section's group) + its write in `BattleEffectItem.TryWriteData` (same order) + its read in the HLSL (the generator mirroring the const):
* `BattleEffectItemData.hlsl` for per-item settings
* `BattleEffectRepetition.hlsl` for repetition
* `BattleEffectComposite.hlsl` for transform / style / compositing

## Design Notes

- The camera follow target and resolution persistence are supplied by the project (`CameraControllerBase` / `DisplaySettingMonitorBase` subclasses, `SetBattleEntryParameters(tint, …)`) to ensure zero package dependencies on game / save system
- Battle effect graph + materials should live in the project
  - This package ships the HLSL that the graph is built around, plus a copy of the graph as a sample
  - `BattleEffectRenderSettings` finds the first material under `Assets` whose shader has the recipe properties
- Shape IDs are baked into recipe data textures at runtime and into the generated dispatch, so keep them unique and don't renumber existing shapes

## Tests

| Assembly                               | Root Namespace                         | Platform       | References                                                                                |
|----------------------------------------|----------------------------------------|----------------|-------------------------------------------------------------------------------------------|
| `LowDefMustard.Rendering.Tests.Editor` | `LowDefMustard.Rendering.Tests.Editor` | Editor only    | `LowDefMustard.Rendering`, `LowDefMustard.Rendering.Editor`                               |
| `LowDefMustard.Rendering.Tests`        | `LowDefMustard.Rendering.Tests`        | Any (PlayMode) | `LowDefMustard.Rendering`, `Unity.Cinemachine`, `Unity.RenderPipelines.Universal.Runtime` |

`Tests/Runtime` holds the PlayMode tests (matching the other packages). They set private serialized hookups through `SerializedObject`, so the test files are editor-only (`#if UNITY_EDITOR`).

`Tests/Editor/Shaders/RenderingTestProperties.shader` is a test-only shader declaring every property the package sets, so material + `MaterialDefinition` writes can be read back. Rendering tests (the frame exporter + the window's export) instead need a battle effect material and are ignored when the project has none; importing the Battle Effect sample provides one.

### Coverage at a glance

| Category                        | Tested / Total | Notes                                                                                                        |
|---------------------------------|----------------|--------------------------------------------------------------------------------------------------------------|
| `BattleEffectItem`              | 6 / 6          | Texel layout, effect scale, guards, accessors                                                                |
| `BattleEffectRecipe`            | 8 / 8          | Bake, `ApplyTo`, `OnDisable` cleanup, end time, revision, `ValidateShapes`                                   |
| `BattleEffectInstance`          | 14 / 14        | Flattening order, start offset + end time, mirror x / x+y, scale + offset, colour swaps, depth limit, rebake |
| Shapes                          | 2 / 2          | Unique + stable IDs, parameters fit the layout                                                               |
| Ranges + tint                   | 6 / 6          | Constructors, `ApplyTo`, drawer field names                                                                  |
| `BattleEffectPropertyRefs`      | 4 / 4          | Material detection + every `MaterialDefinition` write                                                        |
| `BattleEffectDispatchGenerator` | 2 / 3          | Shape validation + generated content + file write (via up-to-date check)                                     |
| HLSL library                    | 2 / 2          | Includes resolve, none reach into `Assets`                                                                   |
| Preview window support          | 12 / 12        | Size presets, retro toggles, render settings, reference frames                                               |
| Preview window                  | 8 / 10         | Controls, transport + buttons, sizes, reference frames, export                                               |
| Timeline                        | 4 / 4          | Rows + bars, highlighting, rebuild-on-change, scrubbing, instance headers (expand / collapse)                |
| Frame exporter                  | 5 / 5          | Frames + size, stale frames, background / transparent / drawn pixels, cancel, un-premultiply                 |
| Drawers                         | 9 / 9          | Range, tint (per mode + live mode change), shape (picker, swap, external swap), sections                     |
| Recipe inspector                | 1 / 1          | Entry order, selection, add / delete, item sections + shown fields, copy / paste, undo                       |
| Display                         | 5 / 7          | Value types, `DisplayResolutions` invariants, monitor (configured path + save on destroy)                    |
| Camera                          | 2 / 2          | `CameraControllerBase` lifecycle, following, ortho sizes, tag lookup + `CameraSpawnAssist`                   |
| Shaders (C#)                    | 2 / 3          | `ShaderPropertyRefs`, `BattleEntryShaderControl`                                                             |

### Detail by type

| Type                                                            | Status   | Test file(s)                                                                    | Notes                                                                                                                          |
|-----------------------------------------------------------------|----------|---------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------|
| `BattleEffectItem`                                              | Yes      | `BattleEffectItemTests.cs`                                                      | Fields are set from partial JSON (`BattleEffectTestData`)                                                                      |
| `BattleEffectRecipe`                                            | Yes      | `BattleEffectRecipeTests.cs`, `BattleEffectMaterialDefinitionTests.cs`          |
| `BattleEffectInstance` + `BattleEffectColourSwap`               | Yes      | `BattleEffectInstanceTests.cs`                                                  |                                                                                                                                |
| `BattleEffectShape` + subclasses                                | Yes      | `BattleEffectShapeTests.cs`                                                     | No test-only shape on purpose: generator + drawer would pick it up project-wide                                                |
| `BattleEffectFloatRange` / `BattleEffectVector2Range`           | Yes      | `BattleEffectRangeTests.cs`                                                     |                                                                                                                                |
| `BattleEffectTint` + `BattleEffectTintSettings`                 | Yes      | `BattleEffectTintTests.cs`, `BattleEffectMaterialDefinitionTests.cs`            |                                                                                                                                |
| `BattleEffectPropertyRefs`                                      | Yes      | `BattleEffectMaterialDefinitionTests.cs`, `BattleEffectPreviewSettingsTests.cs` |                                                                                                                                |
| `BattleEffectDataLayout`                                        | Yes      | `BattleEffectItemTests.cs`, `BattleEffectDispatchGeneratorTests.cs`             | Via texel layout + mirrored defines                                                                                            |
| `BattleEffectDispatchGenerator`                                 | Partial  | `BattleEffectDispatchGeneratorTests.cs`                                         | Write + reimport skipped (would touch the generated file)                                                                      |
| HLSL includes                                                   | Yes      | `HlslIncludeTests.cs`                                                           | Static check of `#include` paths, not a shader compile                                                                         |
| `BattleEffectPreviewSizes` + `BattleEffectPreviewSize`          | Yes      | `BattleEffectPreviewSettingsTests.cs`                                           |                                                                                                                                |
| `BattleEffectRetroToggles`                                      | Yes      | `BattleEffectPreviewSettingsTests.cs`                                           |                                                                                                                                |
| `BattleEffectRenderSettings`                                    | Yes      | `BattleEffectPreviewSettingsTests.cs`, `BattleEffectMaterialDefinitionTests.cs` | `FindMaterialIfUnset` ignored when the project has no battle effect material                                                   |
| `BattleEffectReferenceFrames`                                   | Yes      | `BattleEffectReferenceFramesTests.cs`                                           |                                                                                                                                |
| `BattleEffectPreviewWindow`                                     | Partial  | `BattleEffectPreviewWindowTests.cs`                                             | Skipped: scheduled playback `Tick` (wall-clock driven) and open-from-asset / menu entry points                                 |
| `BattleEffectTimelineElement`                                   | Yes      | `BattleEffectTimelineElementTests.cs`                                           | Scrub maths via `TryGetScrubTime`, w/ one pointer press                                                                        |
| `BattleEffectFrameExporter`                                     | Yes      | `BattleEffectFrameExporterTests.cs`                                             |                                                                                                                                |
| Battle effect drawers + `BattleEffectSection`                   | Yes      | `BattleEffectDrawerTests.cs`                                                    |                                                                                                                                |
| `BattleEffectRecipeEditor` + entries / selection / item element | Yes      | `BattleEffectRecipeEditorTests.cs`, `BattleEffectPreviewWindowTests.cs`         | Hosted directly (not via `InspectorElement`) to drive its internal methods;  row double-click editing untested                 |
| `ResolutionScaler` / `ResolutionSetting`                        | Yes      | `ResolutionTests.cs`                                                            |                                                                                                                                |
| `DisplayResolutions`                                            | Partial  | `DisplayResolutionsTests.cs`                                                    | `UpdateScreenResolution` / `ForceScreenResolution` / `SetWindowToCenter` skipped since they'd resize / move the editor window) |
| `DisplaySettingMonitorBase`                                     | Partial  | `DisplaySettingMonitorBaseTests.cs` (PlayMode)                                  | First-run setup skipped (resizes + moves the editor window) && `Update` polling can't be driven from a test                    |
| `CameraControllerBase`                                          | Yes      | `CameraControllerBaseTests.cs` (PlayMode)                                       | Tag lookup is project-dependent (ignored w/o a `CameraController` tag)                                                         |
| `CameraSpawnAssist`                                             | Yes      | `CameraSpawnAssistTests.cs` (PlayMode)                                          |                                                                                                                                |
| `ShaderPropertyRefs`                                            | Yes      | `ShaderPropertyRefsTests.cs`                                                    | Round trips through the test-only shader                                                                                       |
| `BattleEntryShaderControl`                                      | Yes      | `BattleEntryShaderControlTests.cs`                                              |                                                                                                                                |
| `ShaderQuad`                                                    | No       | —                                                                               | Skipped, thin quad-write (low value)                                                                                           |

## License

Internal package — Low Def Mustard Games. See GIT LICENSE file for further details.
