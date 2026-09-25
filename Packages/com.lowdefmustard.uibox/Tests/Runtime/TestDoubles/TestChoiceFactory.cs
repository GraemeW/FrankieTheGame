using System.Collections.Generic;
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
            choiceButton.button = button;
            choiceButton.itemHighlighted = new UnityEvent();
            return choiceButton;
        }

        public static UIChoiceToggle CreateWiredToggle(string name, List<GameObject> spawned)
        {
            var go = new GameObject(name, typeof(RectTransform));
            spawned.Add(go);
            var toggle = go.AddComponent<Toggle>();
            var choiceToggle = go.AddComponent<UIChoiceToggle>();
            choiceToggle.toggle = toggle;
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
            choiceSlider.slider = slider;
            choiceSlider.itemHighlighted = new UnityEvent();
            return choiceSlider;
        }
    }
}
