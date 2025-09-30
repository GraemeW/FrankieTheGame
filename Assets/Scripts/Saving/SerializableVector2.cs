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

        public SerializableVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public SerializableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }        

        public Vector2 ToVector()
        {
            return new Vector2(x, y);
        }
    }
}
