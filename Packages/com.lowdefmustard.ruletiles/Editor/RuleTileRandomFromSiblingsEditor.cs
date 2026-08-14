using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace LowDefMustard.RuleTiles.Editor
{
    [CustomEditor(typeof(RuleTileRandomFromSiblings))]
    public class RuleTileRandomFromSiblingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label("Random Tile from Siblings") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }});

            var spriteField = new PropertyField(serializedObject.FindProperty("m_DefaultSprite")) { tooltip = "Default sprite not used in painting, only relevant for palette." };
            root.Add(spriteField);
            
            var perlinScaleField = new PropertyField(serializedObject.FindProperty("m_PerlinScale")) { tooltip = "Adjust to alter random position of painted tiles"};
            root.Add(perlinScaleField);
            
            var siblingsField = new PropertyField(serializedObject.FindProperty("siblings")) { tooltip = "Tile will be selected randomly from below list"};
            root.Add(siblingsField);
            
            return root;
        }
    }
}
