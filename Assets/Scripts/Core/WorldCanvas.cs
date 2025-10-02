using UnityEngine;

public class WorldCanvas : MonoBehaviour
{
    [SerializeField] Transform worldOptionsParent = null;

    private static string worldCanvasTag = "WorldCanvas";
    public static WorldCanvas FindWorldCanvas() => GameObject.FindGameObjectWithTag(worldCanvasTag)?.GetComponent<WorldCanvas>();

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
