using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LowDefMustard.UIBox.Tests
{
    public static class TestChoiceFactory
    {
        public static UIChoiceButton CreateWiredButton(string name, List<GameObject> spawned)
        {
            var go = new GameObject(name, typeof(RectTransform));
            spawned.Add(go);
            var button = go.AddComponent<Button>();
            var choiceButton = go.AddComponent<UIChoiceButton>();
            Wire(choiceButton, "button", button);
            choiceButton.itemHighlighted = new UnityEvent();
            return choiceButton;
        }

        public static UIChoiceToggle CreateWiredToggle(string name, List<GameObject> spawned)
        {
            var go = new GameObject(name, typeof(RectTransform));
            spawned.Add(go);
            var toggle = go.AddComponent<Toggle>();
            var choiceToggle = go.AddComponent<UIChoiceToggle>();
            Wire(choiceToggle, "toggle", toggle);
            choiceToggle.itemHighlighted = new UnityEvent();
            return choiceToggle;
        }

        public static UIChoiceSlider CreateWiredSlider(string name, List<GameObject> spawned, float min = 0f, float max = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            spawned.Add(go);
            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            var choiceSlider = go.AddComponent<UIChoiceSlider>();
            Wire(choiceSlider, "slider", slider);
            choiceSlider.itemHighlighted = new UnityEvent();
            return choiceSlider;
        }
        
        // Walks the type hierarchy since some wired fields are declared on a base class
        //  - avoiding SerializedObject -> FindProperty since this is a Runtime namespace
        public static void Wire(object target, string fieldName, object value)
        {
            System.Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                type = type.BaseType;
            }
            field?.SetValue(target, value);
        }
    }
}
