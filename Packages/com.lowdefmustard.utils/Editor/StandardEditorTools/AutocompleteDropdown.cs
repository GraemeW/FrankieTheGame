using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    public sealed class AutocompleteDropdown : VisualElement
    {
        // Suggestion list shown under a text field while typing
        //  - Add it to the window root (it positions itself in that space && stays unscaled)
        //  - Behaviour:
        //      - never takes focus (to allow typing to continue)
        //      - up / down to highlight, enter / click to choose, escape to close
        //      - matching is case-insensitive:  options starting w/ the typed text come first, then options containing it
        //  - One dropdown can be shared by every field attached to it (i.e. set up for 'n' inputs in a field, if you only need one dropdown to appear at a time)
        
        // Const Tunables
        private const int _maxSuggestions = 10;
        private const long _hideDelayMs = 150; // Lets a click on a row land before the field's focus-out hides the list
        private const float _rowHeight = 18f;
        private const float _minWidth = 160f;
        
        // Style Classes
        private const string _dropdownClass = "autocomplete-dropdown";
        private const string _rowClass = "autocomplete-dropdown__row";
        private const string _highlightedRowClass = "autocomplete-dropdown__row--highlighted";

        // State
        private readonly List<string> suggestions = new();
        private TextField activeField;
        private int highlightedIndex = -1;

        // Constructor
        public AutocompleteDropdown()
        {
            AddToClassList(_dropdownClass);
            style.position = Position.Absolute;
            style.display = DisplayStyle.None;
            style.minWidth = _minWidth;
        }

        #region PublicMethods
        public void Attach(TextField field, IEnumerable<string> options)
        {
            if (field == null || options == null) { return; }
            field.RegisterCallback<InputEvent>(_ => Show(field, options));
            field.RegisterCallback<KeyDownEvent>(keyDownEvent => HandleKeyDown(field, keyDownEvent), TrickleDown.TrickleDown);
            field.RegisterCallback<FocusOutEvent>(_ => schedule.Execute(() => HideFor(field)).StartingIn(_hideDelayMs));
            field.RegisterCallback<DetachFromPanelEvent>(_ => HideFor(field));
        }
        #endregion

        #region EventHandlers
        private void HandleKeyDown(TextField field, KeyDownEvent keyDownEvent)
        {
            if (field != activeField || style.display == DisplayStyle.None) { return; }
            switch (keyDownEvent.keyCode)
            {
                case KeyCode.DownArrow:
                    SetHighlighted(Mathf.Min(highlightedIndex + 1, suggestions.Count - 1));
                    keyDownEvent.StopPropagation();
                    break;
                case KeyCode.UpArrow:
                    SetHighlighted(Mathf.Max(highlightedIndex - 1, 0));
                    keyDownEvent.StopPropagation();
                    break;
                case KeyCode.Return or KeyCode.KeypadEnter when highlightedIndex >= 0:
                    Choose(highlightedIndex);
                    break;
                case KeyCode.Escape:
                    Hide();
                    break;
            }
        }
        #endregion

        #region PrivateMethods
        private static IEnumerable<string> GetMatches(IEnumerable<string> options, string text)
        {
            List<string> matches = options.Where(option => !string.IsNullOrEmpty(option) && option.Contains(text, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
            return matches.Where(option => option.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                .Concat(matches.Where(option => !option.StartsWith(text, StringComparison.OrdinalIgnoreCase)));
        }

        private void Show(TextField field, IEnumerable<string> options)
        {
            activeField = field;
            suggestions.Clear();
            if (!string.IsNullOrWhiteSpace(field.text)) { suggestions.AddRange(GetMatches(options, field.text.Trim()).Take(_maxSuggestions)); }
            if (suggestions.Count == 0 || parent == null)
            {
                Hide();
                return;
            }

            Clear();
            for (int i = 0; i < suggestions.Count; i++) { Add(MakeRow(i)); }
            highlightedIndex = -1;

            // Positioned below the field's input box, in this dropdown's parent space
            VisualElement input = field.Q(className: TextField.inputUssClassName) ?? field;
            Rect inputBounds = input.worldBound;
            Vector2 position = parent.WorldToLocal(new Vector2(inputBounds.xMin, inputBounds.yMax));
            style.left = position.x;
            style.top = position.y;
            style.width = Mathf.Max(inputBounds.width, _minWidth);
            style.display = DisplayStyle.Flex;
            BringToFront();
        }
        
        private VisualElement MakeRow(int index)
        {
            var row = new Label(suggestions[index]) { style = { height = _rowHeight, paddingLeft = 4f, unityTextAlign = TextAnchor.MiddleLeft } };
            row.AddToClassList(_rowClass);
            row.RegisterCallback<PointerDownEvent>(pointerDownEvent =>
            {
                Choose(index);
                pointerDownEvent.StopPropagation();
            });
            return row;
        }
        
        private void Choose(int index)
        {
            if (activeField == null || index < 0 || index >= suggestions.Count) { return; }
            TextField field = activeField;
            Hide();
            field.value = suggestions[index]; // Commits like a typed entry (fires the field's change callback)
        }

        private void SetHighlighted(int index)
        {
            highlightedIndex = index;
            for (int i = 0; i < childCount; i++) { this[i].EnableInClassList(_highlightedRowClass, i == highlightedIndex); }
        }

        private void HideFor(TextField field)
        {
            if (field == activeField) { Hide(); }
        }

        private void Hide()
        {
            style.display = DisplayStyle.None;
            activeField = null;
            highlightedIndex = -1;
        }
        #endregion
    }
}
