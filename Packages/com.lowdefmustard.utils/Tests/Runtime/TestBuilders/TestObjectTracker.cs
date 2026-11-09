using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.Utils.Tests
{
    public sealed class TestObjectTracker
    {
        private readonly List<Object> trackedObjects = new();

        public T Create<T>(string objectName) where T : ScriptableObject
        {
            var createdObject = ScriptableObject.CreateInstance<T>();
            createdObject.name = objectName;
            trackedObjects.Add(createdObject);
            return createdObject;
        }

        public void DestroyAll()
        {
            foreach (Object trackedObject in trackedObjects) { if (trackedObject != null) { Object.DestroyImmediate(trackedObject); } }
            trackedObjects.Clear();
        }
    }
}
