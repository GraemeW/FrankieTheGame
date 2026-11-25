using UnityEditor;

namespace LowDefMustard.Zones.Editor
{
    internal readonly struct SceneProperties
    {
        public readonly SerializedProperty asset;
        public readonly SerializedProperty name;
        public readonly SerializedProperty path;
        public readonly SerializedProperty guid;

        public SceneProperties(SerializedProperty asset, SerializedProperty name, SerializedProperty path, SerializedProperty guid)
        {
            this.asset = asset;
            this.name = name;
            this.path = path;
            this.guid = guid;
        }
    }
}
