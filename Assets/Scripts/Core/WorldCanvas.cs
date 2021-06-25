using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCanvas : MonoBehaviour
{
    [SerializeField] Transform worldOptionsParent = null;

    public void DestroyExistingWorldOptions()
    {
        foreach (Transform child in worldOptionsParent)
        {
            Destroy(child.gameObject);
        }
    }

    public Transform GetWorldOptionsParent()
    {
        return worldOptionsParent;
    }
}
