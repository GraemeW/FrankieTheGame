using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.UIBox.Tests
{
    public class UIChoiceListenerLifecycleTests
    {
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.Destroy(go);
            }
            spawned.Clear();
        }

        // Tests
        [UnityTest]
        public IEnumerator AddOnHighlightListener_WhileInactive_AttachesOnEnable()
        {
            var go = new GameObject("Choice", typeof(RectTransform));
            spawned.Add(go);
            go.SetActive(false);
            var button = go.AddComponent<UnityEngine.UI.Button>();
            var choice = go.AddComponent<UIChoiceButton>();
            choice.button = button;
            choice.itemHighlighted = new UnityEngine.Events.UnityEvent();
            bool invoked = false;

            choice.AddOnHighlightListener(() => invoked = true); // object still inactive here
            go.SetActive(true);
            yield return null; // let OnEnable actually run and re-subscribe the extra listener

            choice.Highlight(true);

            Assert.IsTrue(invoked);
        }

        [UnityTest]
        public IEnumerator OnDisable_DetachesExtraListeners_SoHighlightNoLongerInvokesThem()
        {
            UIChoiceButton choice = TestChoiceFactory.CreateWiredButton("Choice", spawned);
            bool invoked = false;
            choice.AddOnHighlightListener(() => invoked = true);

            choice.gameObject.SetActive(false);
            yield return null; // let OnDisable actually run and detach the extra listener

            choice.Highlight(true); // direct call still runs even though the GameObject is inactive

            Assert.IsFalse(invoked);
        }
    }
}
