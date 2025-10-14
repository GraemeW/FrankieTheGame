using UnityEngine;

public class WorldCanvas : MonoBehaviour
{
    [SerializeField] private Transform worldOptionsParent;

    private const string _worldCanvasTag = "WorldCanvas";

    public static WorldCanvas FindWorldCanvas() 
    {
        var worldCanvasGameObject = GameObject.FindGameObjectWithTag(_worldCanvasTag);
        return worldCanvasGameObject != null ? worldCanvasGameObject.GetComponent<WorldCanvas>() : null;
    }

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
