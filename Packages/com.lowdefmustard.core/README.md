# Low Def Mustard Core

Core functionality shared by every Low Def Mustard package. It has zero dependencies.

- **Package name:** `com.lowdefmustard.core`
- **Version:** 0.1.0
- **Unity:** 6000.5+
- **Dependencies:** None

## Installation

Add via the Unity Package Manager using a Git URL (adjust to your repo/path), or reference locally with `"com.lowdefmustard.core": "file:../path/to/com.lowdefmustard.core"` in your project's `manifest.json`.

## Assembly Structure

| Assembly                    | Root Namespace              | Platform    | References           |
|-----------------------------|-----------------------------|-------------|----------------------|
| `LowDefMustard.Core`        | `LowDefMustard.Core`        | Runtime     | —                    |
| `LowDefMustard.Core.Editor` | `LowDefMustard.Core.Editor` | Editor only | `LowDefMustard.Core` |

## LDMLog

```csharp
LDMLog.Info(LDMLogCategory.Combat, $"{attacker.name} hits {target.name} for {damage}");
LDMLog.Important(LDMLogCategory.Saving, "Save complete", this);
LDMLog.Warning(LDMLogCategory.Zones | LDMLogCategory.World, "Zone has no spawn point");
LDMLog.Error(LDMLogCategory.Inventory, $"Missing item {itemID}");
LDMLog.Exception(exception);
```

| Method      | Builds                                              | Filtered by category | Routes to            |
|-------------|-----------------------------------------------------|----------------------|----------------------|
| `Info`      | Editor + Checked / Debug managed code variants only | Yes                  | `Debug.Log`          |
| `Important` | All                                                 | Yes                  | `Debug.Log`          |
| `Warning`   | All                                                 | No (tag only)        | `Debug.LogWarning`   |
| `Error`     | All                                                 | No (tag only)        | `Debug.LogError`     |
| `Exception` | All                                                 | No category          | `Debug.LogException` |

- `Info` is stripped with `[Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]`
  - in a Release or Instrumented build the call and the evaluation of its arguments disappear entirely
  - N.B. Unity 6.6 deprecated `DEVELOPMENT_BUILD` ~ Development Build no longer implies the Checked variant
    - thus: set **Player Settings > Optimization > Managed Code Variant** to Checked to keep `Info` in a test build
- All methods have an overload taking an `Object context` (for click-to-ping in the console)
- All methods carry `[HideInCallstack]` (for double-click -> open the caller)
  - requires the console's **Strip logging callstack** option enabled
- `LDMLog.IsEnabled(category)` lets a call site skip building an expensive `Important` message when its category is off

### Categories

`LDMLogCategory` is a `[Flags]` enum such that one call can carry several tags. 

A message shows if any of its flags is enabled. Its colour comes from the first tagged category in declaration order. `None` and `All` are convenience values and never get settings entries. 

Add new categories as the next unused single bit (16 of the 32 bits are in use today).

### Settings

`LDMLogSettings` is a ScriptableObject that defines logging behaviour (category enablement, category colours, stack traces).

#### Registration

`LDMLogSettings` uses the same registration pattern as Unity's Input System and Localization settings:

- **Builds:** the asset is listed in **Player Settings > Preloaded Assets**
  - Unity loads it before the first scene, and its `OnEnable` registers it with `LDMLog`
- **Editor:** `LDMLog` reads the asset from an `EditorBuildSettings` config object (`com.lowdefmustard.core.logsettings`)
  - occurs on every domain reload and on entering Play Mode
  - required since preloaded Assets aren't loaded in the editor

**Tools > Low Def Mustard > Log Settings** selects the registered asset, or creates one at `Assets/Settings/LDMLogSettings.asset`. It also registers the asset in both places and removes any other `LDMLogSettings` from Preloaded Assets. 

Before each player build, `LDMLogSettingsBuildCheck` re-adds the registered asset if it has dropped off the Preloaded Assets list, and logs a warning when it does.

#### Default Behaviour

Anything logged before the preloaded assets load uses the no-asset defaults below:

- One row per category, with an enabled toggle and a console colour
  - Rows are kept in sync with the enum automatically
  - missing categories are added, and stale or duplicate entries are dropped
- Stack trace type per log type (Log, Warning, Error / Assert, Exception) ~ applied through `Application.SetStackTraceLogType`
  - `Info` and `Important` share the Log setting because both call `Debug.Log`
- Colour tags are added in the editor only, while player logs get a plain `[Category] message` prefix

- Without an asset, every category is enabled, no colour is applied and Unity's stack trace defaults stay in place.

### Future: lazy interpolated messages (C# 10+)

Messages are plain `string` parameters because Unity compiles at C# 9. One consequence is that an `Important` call with an interpolated `$"..."` message builds its string even when the category is disabled. A C# 10+ [custom interpolated string handler](https://learn.microsoft.com/dotnet/csharp/advanced-topics/performance/interpolated-string-handler) allows us to remove that cost.  Notably, the compiler skips every formatting call and the evaluation of its arguments when the handler (bound at the call site) reports the category as disabled.

Today, a C# 9 caller silently picks the `string` overload. 

Once we compile at C# 10+ (expected for Unity 7), the following adjustments to LDMLog should be made such that `$"..."` arguments rebind to the new overloads automatically:

1. Add `LDMLogInterpolatedStringHandler`, a `ref struct` marked `[InterpolatedStringHandler]`
   - Its constructor `(int literalLength, int formattedCount, LDMLogCategory category, out bool shouldAppend)` sets `shouldAppend = LDMLog.IsEnabled(category)`
2. Next to each `Info` / `Important` overload, add one whose message parameter is `[InterpolatedStringHandlerArgument(nameof(category))] ref LDMLogInterpolatedStringHandler message`
   - Add context variants as well / as needed
3. (only if the base class library in use doesn't already provide them) -> Polyfill `InterpolatedStringHandlerAttribute` and `InterpolatedStringHandlerArgumentAttribute`

Two rules keep this painless meanwhile:
- Keep the message as the `string` parameter directly after `category`
- Don't add `params object[]` / format-string overloads

## Tests

| Assembly                          | Platform    | References                                                    |
|-----------------------------------|-------------|---------------------------------------------------------------|
| `LowDefMustard.Core.Tests.Editor` | Editor only | `LowDefMustard.Core`, `LowDefMustard.Core.Editor`, TestRunner |

### Coverage at a glance

| Area             | Tested / Total | Notes                                                    |
|------------------|----------------|----------------------------------------------------------|
| Runtime types    | 2 / 4          | `LDMLogCategory` / `LDMLogCategoryEntry` are plain data  |
| Editor types     | 1 / 3          | Inspector + build check verified by hand                 |

### Detail by type

| Type                       | Tested  | Test file(s)                 | Notes                                                                       |
|----------------------------|---------|------------------------------|-----------------------------------------------------------------------------|
| `LDMLog`                   | Partial | `LDMLogTests.cs`             | Stack trace application and `[Conditional]` stripping are not unit-testable |
| `LDMLogSettings`           | Yes     | `LDMLogSettingsTests.cs`     |                                                                             |
| `LDMLogCategory`           | No      | —                            | N/A - enum only                                                             |
| `LDMLogCategoryEntry`      | No      | —                            | N/A - serializable data only                                                |
| `LDMLogSettingsEditor`     | No      | —                            | Simple UITK inspector (skipped, low value)                                  |
| `LDMLogSettingsMenu`       | Partial | `LDMLogSettingsMenuTests.cs` | The menu item's asset creation was verified by hand                         |
| `LDMLogSettingsBuildCheck` | No      | —                            | Skipped, thin wrapper over `Register`                                       |
