using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // View-owned entry:  logic holds the handle (via its interfaces); the view (re)builds its element on every UI (re)load
    public abstract class EntryHandle
    {
        // State
        protected UIToolkitBoxView view { get; }
        public Type containerType { get; } // The view places the element in the first UXML element of this type
        protected bool isRemoved { get; private set; } = false;
        protected VisualElement element { get; private set; }

        // Constructor
        protected EntryHandle(UIToolkitBoxView view, Type containerType)
        {
            if (!typeof(VisualElement).IsAssignableFrom(containerType)) { throw new ArgumentException($"{containerType} is not a VisualElement", nameof(containerType)); }
            this.view = view;
            this.containerType = containerType;
        }

        // Abstract/Virtual
        protected abstract VisualElement CreateElement();
        protected virtual void UnhookElement(VisualElement detachingElement) { }

        #region PublicMethods
        public bool isAlive => !isRemoved && view != null;

        public void Remove()
        {
            if (isRemoved) { return; }
            isRemoved = true;
            if (view != null) { view.RemoveEntry(this); }
        }

        public VisualElement BuildElement()
        {
            DetachElement();
            element = CreateElement();
            return element;
        }

        public bool TryGetScreenRect(Camera renderCamera, out Rect screenRect)
        {
            // Note:  Panel space (y down) flipped to screen convention (y up) - uniform panel scaling preserves relative geometry
            screenRect = default;
            if (element?.panel == null) { return false; }
            Rect worldBound = element.worldBound;
            screenRect = new Rect(worldBound.x, -worldBound.yMax, worldBound.width, worldBound.height);
            return true;
        }

        public void DetachElement()
        {
            if (element == null) { return; }
            UnhookElement(element);
            element.RemoveFromHierarchy();
            element = null;
        }
        #endregion

        #region InternalMethods
        internal void Invalidate() // View-driven removal (i.e. clearing all entries)
        {
            DetachElement();
            isRemoved = true;
        }
        #endregion
    }
}
