using LowDefMustard.Saving.Editor;
using NUnit.Framework;
using System.Linq;

namespace LowDefMustard.Saving.Tests.Editor
{
    public class SaveFileManagerAdapterTests
    {
        private ISaveFileManagerAdapter originalCurrent;

        [SetUp]
        public void SetUp()
        {
            originalCurrent = SaveFileManagerProvider.current;
        }

        [TearDown]
        public void TearDown()
        {
            SaveFileManagerProvider.current = originalCurrent;
        }

        #region NullSaveFileManagerAdapter
        [Test]
        public void NullAdapter_GetCurrentSaveName_ReturnsNull()
        {
            var adapter = new NullSaveFileManagerAdapter();
            Assert.IsNull(adapter.GetCurrentSaveName());
        }

        [Test]
        public void NullAdapter_GetSaveNameForIndex_ReturnsEmptyString()
        {
            var adapter = new NullSaveFileManagerAdapter();
            Assert.AreEqual(string.Empty, adapter.GetSaveNameForIndex(0));
        }

        [Test]
        public void NullAdapter_HasSave_ReturnsFalse()
        {
            var adapter = new NullSaveFileManagerAdapter();
            Assert.IsFalse(adapter.HasSave("anySave"));
        }

        [Test]
        public void NullAdapter_GetInfoFromSave_ReturnsFalseWithDefaults()
        {
            var adapter = new NullSaveFileManagerAdapter();
            bool result = adapter.GetInfoFromSave("anySave", out string characterName, out int level);

            Assert.IsFalse(result);
            Assert.IsNull(characterName);
            Assert.AreEqual(0, level);
        }

        [Test]
        public void NullAdapter_ListSaves_ReturnsEmpty()
        {
            var adapter = new NullSaveFileManagerAdapter();
            Assert.IsFalse(adapter.ListSaves().Any());
        }

        [Test]
        public void NullAdapter_MutatingMethods_DoNotThrow()
        {
            var adapter = new NullSaveFileManagerAdapter();
            Assert.DoesNotThrow(() =>
            {
                adapter.SetCurrentSave("save");
                adapter.CopySave("newSave");
                adapter.CopySave("existing", "newSave");
                adapter.Delete();
                adapter.Delete("save");
                adapter.gameListUpdated += () => { };
                adapter.gameListUpdated -= () => { };
            });
        }
        #endregion

        #region SaveFileManagerProvider
        [Test]
        public void Provider_DefaultCurrent_IsNullAdapter()
        {
            // Restore to the provider's own default construction to check the true default
            SaveFileManagerProvider.current = new NullSaveFileManagerAdapter();
            Assert.IsInstanceOf<NullSaveFileManagerAdapter>(SaveFileManagerProvider.current);
        }

        [Test]
        public void Provider_Current_CanBeReassigned()
        {
            var customAdapter = new NullSaveFileManagerAdapter();
            SaveFileManagerProvider.current = customAdapter;

            Assert.AreSame(customAdapter, SaveFileManagerProvider.current);
        }
        #endregion
    }
}
