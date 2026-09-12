using NUnit.Framework;

namespace LowDefMustard.Localization.Tests.Editor
{
    public class ILocalizableCoreTests
    {
        [Test]
        public void GetStandardLocalizationKey_NamePropertyContainsName_OmitsPropertySegment()
        {
            // "localizedName" strips to "Name", which contains "Name" -> id-only key
            string key = ILocalizableCore.GetStandardLocalizationKey("NPC_01", "Villager", "localizedName");
            Assert.AreEqual("Villager.NPC_01", key);
        }

        [Test]
        public void GetStandardLocalizationKey_NonNameProperty_AppendsSanitizedPropertySegment()
        {
            string key = ILocalizableCore.GetStandardLocalizationKey("NPC_01", "Villager", "localizedDescription");
            Assert.AreEqual("Villager.NPC_01.Description", key);
        }

        [Test]
        public void GetStandardLocalizationKey_NullPropertyName_ProducesTrailingDot()
        {
            string key = ILocalizableCore.GetStandardLocalizationKey("NPC_01", "Villager", null);
            Assert.AreEqual("Villager.NPC_01.", key);
        }

        [Test]
        public void GetStandardLocalizationKey_PropertyNameWithoutLocalizedPrefix_StillStrips()
        {
            // "localized" is stripped wherever it appears, not just as a prefix
            string key = ILocalizableCore.GetStandardLocalizationKey("NPC_01", "Villager", "somelocalizedThing");
            Assert.AreEqual("Villager.NPC_01.someThing", key);
        }
    }
}
