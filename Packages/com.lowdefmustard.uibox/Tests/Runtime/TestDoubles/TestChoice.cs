using System;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests
{
    public class TestChoice : IUIChoice
    {
        // UI Testing Properties
        public bool isAlive { get; set; } = true;
        public bool isHighlighted { get; private set; }
        public int useChoiceCount { get; private set; }
        public string text { get; private set; }
        public Rect? navigationRect;
        public Action onUseChoice;

        public TestChoice() { }
        public TestChoice(Vector2 center, Vector2 size)
        {
            navigationRect = new Rect(center - size / 2f, size);
        }

        // IUIChoice
        public void Highlight(bool enable) => isHighlighted = enable;
        public void SetText(string setText) => text = setText;

        public void UseChoice()
        {
            useChoiceCount++;
            onUseChoice?.Invoke();
        }

        public bool TryGetNavigationRect(out Rect resolvedNavigationRect)
        {
            resolvedNavigationRect = navigationRect ?? default;
            return navigationRect.HasValue;
        }
    }
}
