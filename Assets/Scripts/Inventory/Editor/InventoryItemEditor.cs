using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Frankie.Inventory.Editor
{
    [CustomEditor(typeof(InventoryItem), true)]
    public class InventoryItemEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            var inventoryItem = (InventoryItem)target;
            if (inventoryItem == null) { return; }
            inventoryItem.TryLocalizeDefaults();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
