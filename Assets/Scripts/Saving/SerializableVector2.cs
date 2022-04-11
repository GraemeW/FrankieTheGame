using UnityEngine;

namespace Frankie.Saving
{
    [System.Serializable]
    public class SerializableVector2
    {
        public float x, y;

        public SerializableVector2(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public Vector2 ToVector()
        {
            return new Vector2(x, y);
        }
    }
}
