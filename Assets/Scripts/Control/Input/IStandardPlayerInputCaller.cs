using System;
using UnityEngine;

namespace Frankie.Control
{
    public interface IStandardPlayerInputCaller
    {
        public event Action<PlayerInputType> globalInput;
        public void VerifyUnique(); // Define and call in awake, each controller should be a singleton
        
        public static PlayerInputType NavigationVectorToInputType(Vector2 navigationVector)
        {
            float verticalMagnitude = Vector2.Dot(navigationVector, Vector2.up);
            float horizontalMagnitude = Vector2.Dot(navigationVector, Vector2.right);
            float vectorSelect = Mathf.Abs(verticalMagnitude) - Mathf.Abs(horizontalMagnitude);

            return vectorSelect switch
            {
                > 0 => verticalMagnitude > 0 ? PlayerInputType.NavigateUp : PlayerInputType.NavigateDown,
                < 0 => horizontalMagnitude > 0 ?  PlayerInputType.NavigateRight : PlayerInputType.NavigateLeft,
                _ => PlayerInputType.DefaultNone
            };
        }
    }
}
