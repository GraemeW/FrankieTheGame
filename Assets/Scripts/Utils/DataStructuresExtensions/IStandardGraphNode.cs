using UnityEngine;

namespace Frankie.Utils
{
    public interface IStandardGraphNode
    {
        public ScriptableObject scriptableObject { get; }
        public Vector2 GetPosition();
        public void SetPosition(Vector2 position);
    }
}
