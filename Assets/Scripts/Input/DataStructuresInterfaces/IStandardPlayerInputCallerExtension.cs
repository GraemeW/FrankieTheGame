using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public static class IStandardPlayerInputCallerExtension
    {
        public static PlayerInputType NavigationVectorToInputType(this IStandardPlayerInputCaller standardPlayerInputCaller, Vector2 navigationVector)
        {
            float verticalMagnitude = Vector2.Dot(navigationVector, Vector2.up);
            float horizontalMagnitude = Vector2.Dot(navigationVector, Vector2.right);

            if (Mathf.Abs(verticalMagnitude) > Mathf.Abs(horizontalMagnitude))
            {
                if (verticalMagnitude > 0)
                {
                    return PlayerInputType.NavigateUp;
                }
                else if (verticalMagnitude < 0)
                {
                    return PlayerInputType.NavigateDown;
                }
            }
            else
            {
                if (horizontalMagnitude > 0)
                {
                    return PlayerInputType.NavigateRight;
                }
                else if (horizontalMagnitude < 0)
                {
                    return PlayerInputType.NavigateLeft;
                }
            }

            return PlayerInputType.DefaultNone;
        }

    }
}
