using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.UIBox.Tests
{
    public class SimpleTextLinkTests
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

        #region PrivateMethods
        private SimpleTextLink CreateTextLink(bool disableOnLoad, out TextMeshProUGUI textField)
        {
            var go = new GameObject("TextLink", typeof(RectTransform));
            spawned.Add(go);
            textField = go.AddComponent<TextMeshProUGUI>();
            var link = go.AddComponent<SimpleTextLink>();
            link.textField = textField;
            link.disableOnLoad = disableOnLoad;
            return link;
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator Start_DisableOnLoadTrue_ClearsTextAndDeactivates()
        {
            SimpleTextLink link = CreateTextLink(true, out TextMeshProUGUI textField);
            textField.text = "leftover";

            yield return null; // let Start() run

            Assert.AreEqual("", textField.text);
            Assert.IsFalse(link.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Start_DisableOnLoadFalse_LeavesTextAndActiveStateUntouched()
        {
            SimpleTextLink link = CreateTextLink(false, out TextMeshProUGUI textField);
            textField.text = "kept";

            yield return null; // let Start() run

            Assert.AreEqual("kept", textField.text);
            Assert.IsTrue(link.gameObject.activeSelf);
        }

        [Test]
        public void Setup_InactiveObject_ReactivatesAndSetsText()
        {
            SimpleTextLink link = CreateTextLink(false, out TextMeshProUGUI textField);
            link.gameObject.SetActive(false);

            link.Setup("hello");

            Assert.IsTrue(link.gameObject.activeSelf);
            Assert.AreEqual("hello", textField.text);
        }

        [Test]
        public void Setup_AlreadyActive_JustSetsText()
        {
            SimpleTextLink link = CreateTextLink(false, out TextMeshProUGUI textField);

            link.Setup("world");

            Assert.IsTrue(link.gameObject.activeSelf);
            Assert.AreEqual("world", textField.text);
        }
        #endregion
    }
}
