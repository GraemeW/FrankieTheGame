using NUnit.Framework;
using UnityEngine.Localization;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class LocalizedStringExtensionsTests
    {
        [Test]
        public void GetSafeLocalizedString_NullLocalizedString_ReturnsEmptyString()
        {
            LocalizedString localizedString = null;
            Assert.AreEqual("", localizedString.GetSafeLocalizedString());
        }

        [Test]
        public void GetSafeLocalizedString_EmptyLocalizedString_ReturnsEmptyString()
        {
            var localizedString = new LocalizedString();
            Assert.IsTrue(localizedString.IsEmpty);
            Assert.AreEqual("", localizedString.GetSafeLocalizedString());
        }
    }
}
