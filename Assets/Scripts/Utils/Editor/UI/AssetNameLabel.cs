using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Utils.UI.Editor
{
    public sealed class AssetNameLabel : VisualElement
    {
        // Use for elements that appear as a label, but double-click switches to an editable field

        // Const Tunables
        private const string _emptyText = "None";

        // Element References
        private readonly Label label;
        private readonly TextField field;

        // State
        private Object asset;
        private bool isCancelling;

        // Constructor
        public AssetNameLabel(bool isBold)
        {
            style.flexDirection = FlexDirection.Row;
            style.flexGrow = 1f;
            style.flexShrink = 1f;
            style.alignItems = Align.Center;

            label = new Label
            {
                style =
                {
                    flexGrow = 1f,
                    flexShrink = 1f,
                    unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap,
                    textOverflow = TextOverflow.Ellipsis
                }
            };
            field = new TextField { isDelayed = true, style = { flexGrow = 1f, flexShrink = 1f, display = DisplayStyle.None } };
            Add(label);
            Add(field);

            label.RegisterCallback<ClickEvent>(HandleLabelClicked);
            field.RegisterCallback<FocusOutEvent>(_ => EndEditing());
            field.RegisterCallback<KeyDownEvent>(HandleFieldKeyDown, TrickleDown.TrickleDown);
        }

        #region PublicMethods
        public void SetAsset(Object setAsset)
        {
            asset = setAsset;
            RefreshName();
            ShowLabel();
        }

        public void RefreshName()
        {
            label.text = asset != null ? asset.name : _emptyText;
            label.tooltip = asset != null ? $"{AssetDatabase.GetAssetPath(asset)}\n(double-click to rename)" : null;
        }
        #endregion

        #region EventHandlers
        private void HandleLabelClicked(ClickEvent clickEvent)
        {
            if (clickEvent.clickCount != 2 || asset == null || !EditorUtility.IsPersistent(asset)) { return; }
            isCancelling = false;
            field.SetValueWithoutNotify(asset.name);
            label.style.display = DisplayStyle.None;
            field.style.display = DisplayStyle.Flex;
            field.schedule.Execute(() =>
            {
                field.Focus();
                field.SelectAll();
            });
            clickEvent.StopPropagation();
        }

        private void HandleFieldKeyDown(KeyDownEvent keyDownEvent)
        {
            if (keyDownEvent.keyCode is KeyCode.Escape) { isCancelling = true; }
            if (keyDownEvent.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape) { field.Blur(); }
        }
        #endregion

        #region PrivateMethods
        private void EndEditing()
        {
            if (field.style.display == DisplayStyle.None) { return; }
            if (!isCancelling) { TryRename(field.text); }
            ShowLabel();
            RefreshName();
        }

        private void ShowLabel()
        {
            label.style.display = DisplayStyle.Flex;
            field.style.display = DisplayStyle.None;
        }

        private void TryRename(string newName)
        {
            newName = newName?.Trim();
            if (asset == null || string.IsNullOrEmpty(newName) || newName == asset.name) { return; }

            string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(asset), newName);
            if (!string.IsNullOrEmpty(error)) { Debug.LogWarning($"Could not rename {asset.name} to {newName}:  {error}"); }
        }
        #endregion
    }
}
