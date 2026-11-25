using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests
{
    public sealed class TestObjectTracker
    {
        private readonly List<Object> trackedObjects = new();

        public T Create<T>(string objectName = null) where T : ScriptableObject
        {
            var createdObject = ScriptableObject.CreateInstance<T>();
            if (objectName != null) { createdObject.name = objectName; } // null keeps CreateInstance's blank name
            trackedObjects.Add(createdObject);
            return createdObject;
        }

        public GameObject CreateGameObject(string objectName, params Type[] components) => Track(new GameObject(objectName, components));
        public T CreateComponent<T>(string objectName) where T : Component => CreateGameObject(objectName).AddComponent<T>();

        public T Track<T>(T trackedObject) where T : Object
        {
            trackedObjects.Add(trackedObject);
            return trackedObject;
        }

        public void DestroyAll()
        {
            foreach (Object trackedObject in trackedObjects) { if (trackedObject != null) { Object.DestroyImmediate(trackedObject); } }
            trackedObjects.Clear();
        }
    }
}
