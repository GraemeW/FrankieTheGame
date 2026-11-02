# Assets - Scripts : Rendering

## Preamble

The reusable rendering code (camera + display resolution handling, pixel art shaders, HLSL library and battle effect framework) lives in the [com.lowdefmustard.rendering](../../../Packages/com.lowdefmustard.rendering/README.md) package.  

This folder keeps Frankie's game-specific implementations and rendering:
* [CameraController](./CameraController.cs):  implements `CameraControllerBase` to follow the party leader
* [DisplaySettingMonitor](./DisplaySettingMonitor.cs):  implements `DisplaySettingMonitorBase`, persisting the display resolution in player prefs + re-announcing it on zone changes
* [MapCamera](./MapCamera.cs):  the mini-map camera
* [Cinematics](./Cinematics/):  cinematic triggers + their overlay

## Camera + World View

### [Camera Controller](./CameraController.cs)

This controller makes use of the [Unity Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html), keeping track of existing virtual cameras and allowing camera swapping/override (see `CameraControllerBase` in the [rendering package](../../../Packages/com.lowdefmustard.rendering/README.md)).

The camera follows the player object, which is obtained on enable / start.  The cinemachine state machine / virtual camera follows the 'lead character' in the party.  This allows the virtual camera to **always** reflect the animation of the lead character, even if Frankie is swapped out of the party or he's placed in an alternate position (e.g. 2nd, 3rd, 4th).

Camera control can be overriden via `OverrideCameraFollower(Animator animator, Transform target)` to enable following alternate objects.

#### Notable State / Cached References:

* virtualCamera:  list of existing CinemachineVirtualCameras, of which there are 2 (handled via [IdleActiveBlend](../../Game/Core/Camera-IdleActiveBlend.asset)):
    * VCam Active -- tighter zoom on the player while there is player input
    * VCam Idle -- zooms out from the player when no player input detected

## Shader Code

Frankie's custom shader code lives in the [rendering package](../../../Packages/com.lowdefmustard.rendering/README.md), while the shader graphs and materials themselves live in [VFX/Shaders](../../Game/VFX/Shaders/README.md):
* [HLSLUtilities](../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/):  a library of HLSL functions exposed as Shader Graph nodes, incl. signed distance functions, domain operations (transform, repetition, mirroring), time + animation, colour gradients and hashing
  * also includes the battle effect compositor and its generated shape dispatch (detailed [here](../../Game/VFX/Shaders/README.md#shader-utilities))
* [ShaderPropertyRefs](../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/ShaderPropertyRefs.cs):  shared shader property names, with setters for renderers (`MaterialPropertyBlock`), materials and UI Toolkit `MaterialDefinition`s
* [BattleEntryShaderControl](../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/BattleEntryShaderControl.cs):  sets up the battle entry swirl per fade, w/ the colour chosen by the [Fader](../Zones/Fader.cs) (see [Battle Entry Animations](../../Game/VFX/Shaders/README.md#battle-entry-animations))
* [_PixelArtShaders](../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/_PixelArtShaders/):  see [Pixel-Perfect Rendering](#pixel-perfect-rendering) below

## Pixel-Perfect Rendering

Rendering pixel art accurately on arbitrary displays at arbitrary resolutions is a wildly non-trivial exercise.  

The challenge can be summarized as:
* ideal pixel art should not have any filter (e.g. nearest neighbour, bilinear, trilinear, etc.) applied to it
  * pixels should appear sharp and as-drawn per artistic intent (original canvas)
* game display resolutions and window sizes can vary, and are often different than the original art canvas
  * thus, artwork is stretched or shrunk to fit the game display resolution
  * moreover, a display's resolution is often a non-integer multiple of the original canvas resolution
* Thus, an asset's pixels can straddle and be partially split across display pixels

This straddling results in jarring and ugly display artifacts:  varying line widths with camera movement, perceived shimmering/flicker, etc.  Ultimately it was found that some amount of shader work (w/ aliasing) is required to render pixel art work effectively.  Described below is [the solution that Frankie employs to address this problem](#combined-nearest-neighbour--bilinear-filtering), as well as alternative approaches that were scoped and deemed unsatisfactory.

### Combined Nearest Neighbour / Bilinear Filtering

Frankie applies a custom [Pixel Art Shader](../../../Packages/com.lowdefmustard.rendering/Runtime/Shaders/_PixelArtShaders/PixelArtShader.shader) 

This shader is based on the approach described [here](https://www.youtube.com/watch?v=d6tp43wZqps), which is further based on the implementation discussed [here](https://colececil.dev/blog/2017/scaling-pixel-art-without-destroying-it/). The shader employs an intelligent, light-touch **combination** of nearest neighbour filtering and bilinear filtering.  

In effect, for window resolutions that are integer-divisible of the display resolution, the artwork remains pixel-perfect.  For non-integer-divisible resolutions the pixel art is aliased; however, the result is (for all intents and purposes) visually indistinguishable from pixel-perfect art when rendered on modern high PPI displays.  The benefit of using this approach cannot be overstated:  complete flexibility on window resolutions, game zoom, etc.

*N.B.  In order for the shader to work correctly, the corresponding sprite settings must be configured correctly, as described [here](../../Game/WorldObjects/README.md#configuration--sprite-renderer--sprite).*

### Alternative Approaches Scoped for Pixel-Perfect Art

#### Naive Approach:  Snap to 'perfect' viable resolutions

The naive solution to the challenge stated above is simply:
* set the camera orthographic lens size to result in an 'ideal' zoom level -- i.e. ensuring no pixel splitting across physical display pixels
* limit the viable window resolutions to be integer-divisible from the display resolution

This approach was and is still implemented for the 'recommended' display resolutions in [DisplayResolutions.cs](../../../Packages/com.lowdefmustard.rendering/Runtime/Display/DisplayResolutions.cs) -- where ortho sizes of **1.8/3.6** result in pixel perfect rendering for the 100 PPU assets for a screen resolution matched to the display's native resolution (or integer-divided variants).

This approach is, however, extremely fragile and thus not considered a complete solution for the reasons described below:
1. By limiting the orthographic size to ensure whole pixels, we effectively limit the zoom levels we may use in the game
   * *alternative zooms are technically feasible by scaling display resolution and orthographic size in concert, but in an extremely limited fashion*
   * *this is done in [DisplayResolutions.cs](../../../Packages/com.lowdefmustard.rendering/Runtime/Display/DisplayResolutions.cs) in concert with [CameraControllerBase.cs](../../../Packages/com.lowdefmustard.rendering/Runtime/Camera/CameraControllerBase.cs) via the `resolutionUpdated` event*
2. Fixed window sizes is a considerable limitation in usability and accessibility (Frankie's build setting configuration should **not** lock window size)
3. The entire premise breaks if **any** scaling occurs in the display pipe
   * *e.g. for non-standard display resolutions, video sources will often scale content -- e.g. 1920x1080 content upscaled onto a 1920x1200 display*

#### Unity's Pixel-Perfect Camera

Unity offers a [pixel-perfect camera](https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@1.0/manual/index.html) that, at first glance, may appear to solve our rendering woes.  However, it is doing much the same as described [above](#naive-approach--snap-to-perfect-viable-resolutions).  Notably, Unity's [pixel-perfect camera](https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@1.0/manual/index.html) takes control of and modifies the camera orthographic lens size to attempt to hit perfect ratios for pixel-perfect rendering.  It thus intrinsically limits our ability to manage the zoom level of the game.  

Moreover:
* in spite of the [Cinemachine Pixel Perfect Extension](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/pixel-cinemachine.html), there is significant jitter that occurs when using a [Cinemachine Follower](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineFollow.html) while the [pixel-perfect camera](https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@1.0/manual/index.html) is active
  * this follower functionality is needed and used by the [Cinemachine Cameras](../../Game/Core/Cameras.prefab) in Frankie
* for window resolutions that result in orthographic lens sizes far from the golden/ideal values, the pixel art looks horrendous (and still suffers from the pixel straddling issues)

Practically, it may be feasible to achieve good enough performance using the pixel-perfect camera at **some** display resolutions, but it still breaks for many, and the jitter/fighting is itself a deal breaker for practical application.
