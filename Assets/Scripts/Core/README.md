# Frankie:  Core

## Camera + World View

### [Camera Controller](./CameraController.cs)

This controller makes use of the [Unity Cinemachine](https://unity.com/unity/features/editor/art-and-design/cinemachine), keeping track of existing virtual cameras and allowing camera swapping/override.

The camera will follow the player object, which is obtained in Awake+Start and cached via lazy initialization.  The cinemachine state machine / virtual camera will follow the 'lead character' in the party (also obtained + cached in Awake+Start).  This allows the virtual camera to **always** reflect the animation of the lead character, even if Frankie is swapped out of the party or he's placed in an alternate position (e.g. 2nd, 3rd, 4th).

Camera control can be overriden via `OverrideCameraFollower(Animator animator, Transform target)` to enable following alternate objects.

#### Notable State / Cached References:

* virtualCamera:  list of existing CinemachineVirtualCameras, of which there are 2 (handled via [IdleActiveBlend](../../Game/Camera/IdleActiveBlend.asset)):
    * VCam Active -- tighter zoom on the player while there is player input
    * VCam Idle -- zooms out from the player when no player input detected

