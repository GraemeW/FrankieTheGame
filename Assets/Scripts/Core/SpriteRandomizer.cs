using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRandomizer : MonoBehaviour, ISerializationCallbackReceiver
{
    // Tunables
    [SerializeField][Range(0.01f, 1f)] float varietyFactor = 0.5f;
    [SerializeField] Sprite[] sprites = null;

    // State & Fixed Tunables
    float minDenominator = 0.001f;
    [HideInInspector][SerializeField] Vector3 oldPosition = Vector3.zero;


    // Methods
    private Sprite GetSpriteByPosition()
    {
        if (sprites == null || sprites.Length <= 1) { return null; }
        float positionalFactor = 0.5f * (Mathf.Sin(100f * varietyFactor * ((transform.position.x % 1) / (transform.position.y % 1 + minDenominator))) + 1f);
        for (int i = sprites.Length; i > 0; i--)
        {
            if (positionalFactor < 1f / i)
            {
                return sprites[i-1];
            }
        }

        return null;
    }

    private bool HasPositionShifted()
    {
        float squarePositionShift = (transform.position.x - oldPosition.x) * (transform.position.x - oldPosition.x) + (transform.position.y - oldPosition.y) * (transform.position.y - oldPosition.y);
        if (squarePositionShift < Mathf.Epsilon) { return false; }

        oldPosition = transform.position;
        return true;
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (sprites == null || sprites.Length <= 1) { return; }
        if (!HasPositionShifted()) { return; }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite newSprite = GetSpriteByPosition();
        if (newSprite != null) { spriteRenderer.sprite = newSprite; }
#endif
    }
    public void OnAfterDeserialize()
    {
        // Unused, required for interface
    }
}
