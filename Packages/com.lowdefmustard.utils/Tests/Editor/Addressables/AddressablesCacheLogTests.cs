using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Core;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AddressablesCacheLogTests
    {
        [Test]
        public void LogBuild_LoadFailed_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"Failed to load 'Quest' assets from Addressables\."));
            AddressablesCacheLog.LogBuild(LDMLogCategory.Quests, "Quest", false, 0);
        }

        [Test]
        public void LogBuild_NoAssets_LogsWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"No 'Quest' assets found in Addressables\."));
            AddressablesCacheLog.LogBuild(LDMLogCategory.Quests, "Quest", true, 0);
        }

        [Test]
        public void LogBuild_AssetsLoaded_LogsCount()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"Built the 'Quest' cache \(3 assets\)\.$"));
            AddressablesCacheLog.LogBuild(LDMLogCategory.Quests, "Quest", true, 3);
        }

        [Test]
        public void LogDuplicate_LogsErrorNamingBothAssets()
        {
            var existing = new GameObject("Existing");
            var incoming = new GameObject("Incoming");

            LogAssert.Expect(LogType.Error, new Regex(@"Duplicate Quest ID 'id-1' \('Existing' and 'Incoming'\)\. Overwriting the cached entry with 'Incoming'\.$"));
            AddressablesCacheLog.LogDuplicate(LDMLogCategory.Quests, "Quest", "id-1", existing, incoming);

            Object.DestroyImmediate(existing);
            Object.DestroyImmediate(incoming);
        }
    }
}
