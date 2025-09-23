using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class TileMapCopyTool : EditorWindow
{
    private Tilemap sourceTilemap;
    private Tilemap destinationTilemap;
    private Vector3Int offset = Vector3Int.zero;

    [MenuItem("Tools/Tilemap Copy Tool")]
    public static void ShowWindow()
    {
        GetWindow<TileMapCopyTool>("Tilemap Copy Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tilemap Copy Tool", EditorStyles.boldLabel);
        sourceTilemap = (Tilemap)EditorGUILayout.ObjectField("Source Tilemap", sourceTilemap, typeof(Tilemap), true);
        destinationTilemap = (Tilemap)EditorGUILayout.ObjectField("Destination Tilemap", destinationTilemap, typeof(Tilemap), true);
        offset = EditorGUILayout.Vector3IntField("Offset", offset);

        if (GUILayout.Button("Copy Tiles"))
        {
            CopyTiles();
        }
    }

    private void CopyTiles()
    {
        if (sourceTilemap == null || destinationTilemap == null)
        {
            Debug.LogError("Source and Destination Tilemaps must be assigned.");
            return;
        }

        BoundsInt bounds = sourceTilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = sourceTilemap.GetTile(pos);
            if (tile != null)
            {
                Vector3Int destPos = pos + offset;
                destinationTilemap.SetTile(destPos, tile);
            }
        }

        Debug.Log("Tiles copied successfully.");
    }
}
