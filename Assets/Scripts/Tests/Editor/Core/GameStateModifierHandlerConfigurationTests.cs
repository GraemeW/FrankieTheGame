using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Utils.Tests;

namespace Frankie.Tests.Editor
{
    public class GameStateModifierHandlerConfigurationTests
    {
        // Covers the manual configuration IGameStateModifierHandler requires of every concrete implementer (Frankie + packages, test assemblies excluded)
        //  - [ExecuteInEditMode] isn't inherited -> without it on the concrete type, OnDestroy never fires in edit mode + stale handler entries stay on modifiers
        
        // Private Methods
        private static IEnumerable<Type> GetConcreteHandlerTypes() => TypeCache.GetTypesDerivedFrom<IGameStateModifierHandler>()
            .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type) && !type.IsAbstract && !TestAssemblyCheck.IsTestAssemblyType(type))
            .OrderBy(type => type.FullName);

        // Tests
        [Test]
        public void ConcreteHandlerTypes_Exist()
        {
            CollectionAssert.IsNotEmpty(GetConcreteHandlerTypes().ToList());
        }

        [TestCaseSource(nameof(GetConcreteHandlerTypes))]
        public void ConcreteHandlerType_HasExecuteInEditMode(Type handlerType)
        {
            Assert.IsTrue(handlerType.IsDefined(typeof(ExecuteInEditMode), false) || handlerType.IsDefined(typeof(ExecuteAlways), false), $"{handlerType.Name} needs [ExecuteInEditMode] on the concrete class");
        }
    }
}
