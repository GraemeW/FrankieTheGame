using System;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.GameStateModifiers.Editor;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class GameStateModifierCacheInvalidatorTests
    {
        // Not covered:  OnPostprocessAllAssets itself (needs real asset imports) ~ verified live via a quest reimport

        [Test]
        public void ShouldResetCache_ModifierSubtypeChangedOrDeleted_ReturnsTrue()
        {
            Assert.IsTrue(GameStateModifierCacheInvalidator.ShouldResetCache(new[] { typeof(TestGameStateModifier) }, false));
        }

        [Test]
        public void ShouldResetCache_OtherAssetTypes_ReturnsFalse()
        {
            Assert.IsFalse(GameStateModifierCacheInvalidator.ShouldResetCache(new[] { typeof(Material), null }, false));
        }

        [Test]
        public void ShouldResetCache_UnrecordedAssetDeletion_ReturnsTrue()
        {
            Assert.IsTrue(GameStateModifierCacheInvalidator.ShouldResetCache(Array.Empty<Type>(), true));
        }

        [Test]
        public void GetGameStateModifier_BlankGuid_ReturnsNull()
        {
            Assert.IsNull(GameStateModifier.GetGameStateModifier(""));
            Assert.IsNull(GameStateModifier.GetGameStateModifier(null));
        }

        [Test]
        public void GetGameStateModifier_UnknownGuid_ReturnsNull()
        {
            Assert.IsNull(GameStateModifier.GetGameStateModifier("not-a-real-modifier-guid"));
        }
    }
}
