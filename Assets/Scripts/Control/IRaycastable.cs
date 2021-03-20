using Frankie.Core;

namespace Frankie.Control
{
    public interface IRaycastable
    {
        CursorType GetCursorType();
        bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType);

        // Extended in IRaycastableExtension
        bool CheckDistanceTemplate();
    }
}
