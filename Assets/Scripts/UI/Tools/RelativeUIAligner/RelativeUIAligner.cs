using System;
using UnityEngine;

namespace Frankie.Utils.UI
{
    public abstract class RelativeUIAligner : ScriptableObject
    {
        // Abstract Methods
        public abstract void AssertAlignment(RectTransform rectTransform, RectTransform xReference, RectTransform yReference, Func<RectTransform, Vector2, Vector2, Vector2> canvasMapper);
    }
}
