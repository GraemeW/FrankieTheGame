using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Control
{

    public class PathFinder : MonoBehaviour
    {
        // Tunables
        [SerializeField] private float moveMeshProbeRadius = 0.1f;
        [SerializeField][Tooltip("Heap Size:  cell count / divider")] private int initialHeapSizeDivider = 4;
        [SerializeField] private float crossThreshold = 0.01f;

        // State
        private bool isCacheInitialized = false;
        private int cachedColumns;
        private int cachedRows;
        private bool[] closed;
        private float[] goalCosts;
        private AStarNode[] nodeMap;
        private List<AStarNode> openHeap;

        // Cached References
        private MoveMesh cachedMoveMesh;
        
        // Static
        private static readonly (int dx, int dy, float cost)[] _directions =
        {
            ( 0, 1, 1f),
            ( 0, -1, 1f),
            ( 1, 0, 1f),
            (-1, 0, 1f),
            ( 1, 1, 1.41421f),
            ( 1, -1, 1.41421f),
            (-1, 1, 1.41421f),
            (-1, -1, 1.41421f),
        };

        // Simple Pool
        private readonly List<AStarNode> nodePool = new();
        private int nodePoolIndex = 0;

        private AStarNode RentNode(int column, int row, float goalCost, float heuristicCost, AStarNode parent)
        {
            AStarNode node;
            if (nodePoolIndex < nodePool.Count)
            {
                node = nodePool[nodePoolIndex];
            }
            else
            {
                node = new AStarNode();
                nodePool.Add(node);
            }
            nodePoolIndex++;
            node.Initialize(column, row, goalCost, heuristicCost, parent);
            return node;
        }

        #region UnityMethods
        private void Start()
        {
            InitialisePathfindingCache();
        }

        private bool TryFindMoveMesh()
        {
            var contactFilter2D = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = MoveMesh.GetMoveMeshLayerMask(),
                useTriggers = true
            };
            List<Collider2D> colliderHits = new List<Collider2D>();
            int numberOfColliderHits = Physics2D.OverlapCircle(transform.position, moveMeshProbeRadius, contactFilter2D, colliderHits);
            return numberOfColliderHits > 0 && colliderHits.Any(colliderHit => colliderHit.transform.TryGetComponent(out cachedMoveMesh));
        }
        #endregion
        
        #region PublicMethods
        public bool IsValidPathFinder() => isCacheInitialized;
        
        public bool FindPath(Vector2 currentPosition, Vector2 targetPosition, out List<Vector2> path)
        {
            path = new List<Vector2> { currentPosition };
            if (!IsCacheValid()) { return false; }
            bool isStartCellValid = cachedMoveMesh.WorldToCell(currentPosition, out int startColumn, out int startRow);
            if (!isStartCellValid || !cachedMoveMesh.IsWalkable(startColumn, startRow))  { return false; }
            bool isTargetCellValid = cachedMoveMesh.WorldToCell(targetPosition, out int targetColumn, out int targetRow);
            if (!isTargetCellValid || !cachedMoveMesh.IsWalkable(targetColumn, targetRow)) { return false; }
            
            return RunAStar(startColumn, startRow, targetColumn, targetRow, out path);
        }
        #endregion
        
        #region InitializationMethods
        private void InitialisePathfindingCache()
        {
            if (cachedMoveMesh == null && !TryFindMoveMesh()) { return; }

            WalkabilityGrid grid = cachedMoveMesh.walkabilityGrid;
            cachedColumns = grid.columns;
            cachedRows = grid.rows;
            int cellCount = cachedColumns * cachedRows;

            closed = new bool[cellCount];
            goalCosts = new float[cellCount];
            nodeMap = new AStarNode[cellCount];
            openHeap = new List<AStarNode>(cellCount / initialHeapSizeDivider);
            isCacheInitialized = true;
        }

        private bool IsCacheValid()
        {
            if (cachedMoveMesh == null || !isCacheInitialized) { InitialisePathfindingCache(); }
            if (cachedMoveMesh == null) { return false; }
            WalkabilityGrid walkabilityGrid = cachedMoveMesh.walkabilityGrid;
            if (walkabilityGrid == null || walkabilityGrid.IsEmpty()) { return false; }
            return cachedColumns == walkabilityGrid.columns && cachedRows == walkabilityGrid.rows;
        }
        #endregion

        #region CoreAStarMethods
        private bool RunAStar(int startColumn, int startRow, int targetColumn, int targetRow, out List<Vector2> path)
        {
            ReinitializeGrid(startColumn, startRow, targetColumn, targetRow);

            while (openHeap.Count > 0)
            {
                AStarNode currentNode = HeapPop(openHeap);
                int currentIdx = currentNode.row * cachedColumns + currentNode.column;

                if (closed[currentIdx]) { continue; }
                closed[currentIdx] = true;
                
                // Early exit, success met
                if (currentNode.column == targetColumn && currentNode.row == targetRow)
                {
                    path = ReconstructPath(currentNode);
                    return true;
                }

                foreach (var (dx, dy, moveCost) in _directions)
                {
                    int testColumn = currentNode.column + dx;
                    int testRow = currentNode.row + dy;
                    if (testColumn < 0 || testColumn >= cachedColumns || testRow < 0 || testRow >= cachedRows) { continue; }

                    int neighbourIndex = testRow * cachedColumns + testColumn;
                    if (closed[neighbourIndex]) { continue; }
                    if (!cachedMoveMesh.IsWalkable(testColumn, testRow)) { continue; }

                    if (dx != 0 && dy != 0)
                    {
                        // If angled direction, check for viability to walk each individual direction to avoid clipping
                        bool xWalkViable = cachedMoveMesh.IsWalkable(currentNode.column + dx, currentNode.row);
                        bool yWalkViable = cachedMoveMesh.IsWalkable(currentNode.column, currentNode.row + dy);
                        if (!xWalkViable || !yWalkViable) { continue; }
                    }

                    float tentativeGoalCost = currentNode.goalCost + moveCost;
                    if (tentativeGoalCost >= goalCosts[neighbourIndex]) { continue; }

                    goalCosts[neighbourIndex] = tentativeGoalCost;
                    AStarNode neighbourNode = RentNode(testColumn, testRow, tentativeGoalCost, DistanceEquivalentHeuristic(testColumn, testRow, targetColumn, targetRow), currentNode);
                    nodeMap[neighbourIndex] = neighbourNode;
                    HeapPush(openHeap, neighbourNode);
                }
            }

            path = new List<Vector2> { cachedMoveMesh.CellToWorld(startColumn, startRow) };
            return false;
        }

        private void ReinitializeGrid(int startColumn, int startRow, int targetColumn, int targetRow)
        {
            int cellCount = cachedColumns * cachedRows;

            // Reset per-call state without reallocating
            System.Array.Clear(closed, 0, cellCount);
            System.Array.Fill (goalCosts, float.MaxValue, 0, cellCount);
            System.Array.Clear(nodeMap, 0, cellCount);
            openHeap.Clear();
            nodePoolIndex = 0;

            int startIndex = startRow * cachedColumns + startColumn;
            goalCosts[startIndex] = 0f;

            AStarNode startNode = RentNode(startColumn, startRow, 0f, DistanceEquivalentHeuristic(startColumn, startRow, targetColumn, targetRow), null);
            nodeMap[startIndex] = startNode;
            HeapPush(openHeap, startNode);
        }
        
        private List<Vector2> ReconstructPath(AStarNode targetNode)
        {
            var cellPath = new List<AStarNode>();
            AStarNode currentNode = targetNode;
            while (currentNode != null)
            {
                cellPath.Add(currentNode);
                currentNode = currentNode.parent;
            }
            cellPath.Reverse();

            var worldPath = new List<Vector2>(cellPath.Count);
            worldPath.AddRange(cellPath.Select(node => cachedMoveMesh.CellToWorld(node.column, node.row)));

            return StringPull(worldPath, crossThreshold);
        }
        #endregion

        #region StaticMethods
        private static List<Vector2> StringPull(List<Vector2> path, float crossThreshold)
        {
            if (path.Count <= 2) { return path; }

            var result = new List<Vector2> { path[0] };
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 previous = result[^1];
                Vector2 current = path[i];
                Vector2 next = path[i + 1];
                Vector2 aDirection = (current - previous).normalized;
                Vector2 bDirection = (next - current).normalized;
                float cross = Mathf.Abs(aDirection.x * bDirection.y - aDirection.y * bDirection.x);
                if (cross > crossThreshold) { result.Add(current); }
            }
            result.Add(path[^1]);
            return result;
        }
        
        private static float DistanceEquivalentHeuristic(int aColumn, int aRow, int bColumn, int bRow)
        {
            int deltaColumn = Mathf.Abs(aColumn - bColumn);
            int deltaRow = Mathf.Abs(aRow - bRow);
            return (deltaColumn + deltaRow) + (1.41421f - 2f) * Mathf.Min(deltaColumn, deltaRow);
        }
        
        private static void HeapPush(List<AStarNode> heap, AStarNode node)
        {
            heap.Add(node);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[parent].GetFinalCost() <= heap[i].GetFinalCost()) { break; }
                (heap[i], heap[parent]) = (heap[parent], heap[i]);
                i = parent;
            }
        }

        private static AStarNode HeapPop(List<AStarNode> heap)
        {
            AStarNode top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;
                if (left < heap.Count && heap[left].GetFinalCost() < heap[smallest].GetFinalCost()) smallest = left;
                if (right < heap.Count && heap[right].GetFinalCost() < heap[smallest].GetFinalCost()) smallest = right;
                if (smallest == i) { break; }
                
                (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
                i = smallest;
            }
            return top;
        }
        #endregion
    }
}
