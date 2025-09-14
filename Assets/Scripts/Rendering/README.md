# Assets - Scripts : Rendering

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

Frankie applies a custom [Pixel Art Shader](./Shaders/_PixelArtShaders/PixelArtShader.shader) 

This shader is based on the approach described [here](https://www.youtube.com/watch?v=d6tp43wZqps), which is further based on the implementation discussed [here](https://colececil.dev/blog/2017/scaling-pixel-art-without-destroying-it/). The shader employs an intelligent, light-touch **combination** of nearest neighbour filtering and bilinear filtering.  

In effect, for window resolutions that are integer-divisible of the display resolution, the artwork remains pixel-perfect.  For non-integer-divisible resolutions the pixel art is aliased; however, the result is (for all intents and purposes) visually indistinguishable from pixel-perfect art when rendered on modern high PPI displays.  The benefit of using this approach cannot be overstated:  complete flexibility on window resolutions, game zoom, etc.

*N.B.  In order for the shader to work correctly, the corresponding sprite settings must be configured correctly, as described [here](../../Game/WorldObjects/).*

### Alternative Approaches Scoped for Pixel-Perfect Art

#### Naive Approach:  Snap to 'perfect' viable resolutions

The naive solution to the challenge stated above is simply:
* set the camera orthographic lens size to result in an 'ideal' zoom level -- i.e. ensuring no pixel splitting across physical display pixels
* limit the viable window resolutions to be integer-divisible from the display resolution

This approach was and is still implemented for the 'recommended' display resolutions in [DisplayResolutions.cs](./DisplayResolutions.cs) -- where ortho sizes of **1.8/3.6** result in pixel perfect rendering for the 100 PPU assets for a screen resolution matched to the display's native resolution (or integer-divided variants).

This approach is, however, extremely fragile and thus not considered a complete solution for the reasons described below:
1. By limiting the orthographic size to ensure whole pixels, we effectively limit the zoom levels we may use in the game
   * *alternative zooms are technically feasible by scaling display resolution and orthographic size in concert, but in an extremely limited fashion*
   * *this is done in [DisplayResolutions.cs](./DisplayResolutions.cs) in concert with [CameraController.cs](../Core/CameraController.cs) via the `resolutionUpdated` event*
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
