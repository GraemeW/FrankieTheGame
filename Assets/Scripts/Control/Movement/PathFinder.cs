using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Control
{
    public class PathFinder : MonoBehaviour
    {
        // Tunables
        [Header("Initialization and Mesh Setup")]
        [SerializeField][Tooltip("Circle cast to find move mesh that pathfinder sits on")] private float moveMeshFinderSize = 0.1f;
        [SerializeField][Tooltip("Approximate radius of entity's collider")] private float entitySizeForNoCollider = 0.13f;
        [Header("Polling and Volatile Memory Timing")]
        [SerializeField] private float pathPollingSeconds = 0.1f;
        [SerializeField] private float lastTargetMemorySeconds = 2.0f;
        [Header("Pathfinding thresholds")]
        [SerializeField][Tooltip("Heap Size:  cell count / divider")] private int initialHeapSizeDivider = 4;
        [SerializeField] private float crossThreshold = 0.01f;
        [SerializeField] private float deltaSquaredThreshold = 0.0016f;

        // State
        private float entitySize = 0.13f;
        
        private bool isCacheInitialized = false;
        private bool hasPathed = false;
        private float timeSinceLastPath;
        private float timeSinceLastTarget;
        private Vector2? lastViableTarget = null;
        private readonly List<Vector2> currentPath = new();
        
        private bool[] erodedCells;
        private bool[] closed;
        private float[] gridCosts;
        private AStarNode[] nodeMap;
        private List<AStarNode> openHeap;

        // Cached References
        private MoveMesh cachedMoveMesh;
        private int cachedColumns;
        private int cachedRows;
        private float cachedCellSize;
        
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

        private AStarNode RentNode(int column, int row, float gridCost, float heuristicCost, AStarNode parent)
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
            node.Initialize(column, row, gridCost, heuristicCost, parent);
            return node;
        }

        #region UnityMethods
        private void Awake()
        {
            entitySize = TryGetComponent(out CircleCollider2D circleCollider2D) ? circleCollider2D.radius : entitySizeForNoCollider;
        }
        
        private void Start()
        {
            InitialisePathfindingCache();
        }

        private void OnEnable()
        {
            hasPathed = false;
        }

        private void Update()
        {
            if (timeSinceLastPath < pathPollingSeconds) { timeSinceLastPath += Time.deltaTime; }
            if (timeSinceLastTarget < lastTargetMemorySeconds) { timeSinceLastTarget += Time.deltaTime; }
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
            int numberOfColliderHits = Physics2D.OverlapCircle(transform.position, moveMeshFinderSize, contactFilter2D, colliderHits);
            return numberOfColliderHits > 0 && colliderHits.Any(colliderHit => colliderHit.transform.TryGetComponent(out cachedMoveMesh));
        }
        #endregion
        
        #region PublicMethods
        public bool IsValidPathFinder() => isCacheInitialized;

        public Vector2 GetNextPathTarget()
        {
            if (!isCacheInitialized) { return Vector2.zero; }
            if (currentPath.Count > 0) { return currentPath.First(); }
            return transform.position;
        }
        
        public bool FindPath(Vector2 currentPosition, Vector2 targetPosition, PathFindingCheckType pathFindingCheckType = PathFindingCheckType.Check)
        {
            if (!isCacheInitialized) { return false; }
            
            bool forcePathing = pathFindingCheckType == PathFindingCheckType.ForceCheck;
            if (!forcePathing && hasPathed && timeSinceLastPath < pathPollingSeconds) { return true; }
            
            hasPathed = true;
            timeSinceLastPath = 0f;
            currentPath.Clear();
            if (timeSinceLastTarget >= lastTargetMemorySeconds) { lastViableTarget = null; }
            List<Vector2> path = new List<Vector2> { currentPosition };
            currentPath.AddRange(path);
            
            if (!IsCacheValid()) { return false; }
            bool isStartCellValid = cachedMoveMesh.WorldToCell(currentPosition, out int startColumn, out int startRow);
            if (!isStartCellValid || !IsWalkableEroded(startColumn, startRow))  { return false; }
            bool isTargetCellValid = cachedMoveMesh.WorldToCell(targetPosition, out int targetColumn, out int targetRow);

            // Keep memory of last viable location to move to in case target drops off grid
            if (!isTargetCellValid || !IsWalkableEroded(targetColumn, targetRow))
            {
                if (lastViableTarget == null) { return false; }
                cachedMoveMesh.WorldToCell((Vector2)lastViableTarget, out targetColumn, out targetRow);
            }
            else { lastViableTarget = targetPosition;}
            
            // --Heavy Lifting Here--
            if (!RunAStar(startColumn, startRow, targetColumn, targetRow, out path)) { return false; }
            
            currentPath.Clear();
            currentPath.AddRange(path);
            return true;
        }
        
        public Vector2 FindBestReachablePosition(Vector2 currentPosition, Vector2 targetPosition, float allowableTravelDistance)
        {
            if (!isCacheInitialized) { return Vector2.zero; }
            
            cachedMoveMesh.WorldToCell(currentPosition, out int currentColumn, out int currentRow);
            cachedMoveMesh.WorldToCell(targetPosition, out int targetColumn, out int targetRow);
            
            float allowableCellDistance = allowableTravelDistance / cachedCellSize;
            
            int searchRadius = Mathf.CeilToInt(allowableCellDistance);
            int minColumn = Mathf.Max(0, currentColumn - searchRadius);
            int maxColumn = Mathf.Min(cachedColumns -1, currentColumn + searchRadius);
            int minRow = Mathf.Max(0, currentRow - searchRadius);
            int maxRow = Mathf.Min(cachedRows -1, currentRow + searchRadius);

            int bestColumn = -1;
            int bestRow = -1;
            float bestDistanceToIdeal = float.MaxValue;

            for (int testRow = minRow; testRow <= maxRow; testRow++)
            {
                for (int testColumn = minColumn; testColumn <= maxColumn; testColumn++)
                {
                    if (!IsWalkableEroded(testColumn, testRow)) { continue; }
                    
                    Vector2 testVector = new Vector2(testColumn, testRow);
                    float distanceFromCurrent = Vector2.Distance(testVector, new Vector2(currentColumn, currentRow));
                    if (distanceFromCurrent > allowableCellDistance) { continue; }
                    
                    float distanceToIdeal = Vector2.Distance(testVector, new Vector2(targetColumn, targetRow));
                    if (distanceToIdeal >= bestDistanceToIdeal) { continue; }

                    bestDistanceToIdeal = distanceToIdeal;
                    bestColumn = testColumn;
                    bestRow = testRow;
                }
            }
            
            return bestColumn != -1 ? cachedMoveMesh.CellToWorld(bestColumn, bestRow) : currentPosition;
        }
        #endregion
        
        #region InitializationMethods
        private void InitialisePathfindingCache()
        {
            if (cachedMoveMesh == null && !TryFindMoveMesh()) { return; }

            WalkabilityGrid grid = cachedMoveMesh.walkabilityGrid;
            cachedColumns = grid.columns;
            cachedRows = grid.rows;
            cachedCellSize = grid.cellSize;
            int cellCount = cachedColumns * cachedRows;

            closed = new bool[cellCount];
            gridCosts = new float[cellCount];
            nodeMap = new AStarNode[cellCount];
            openHeap = new List<AStarNode>(cellCount / initialHeapSizeDivider);
            
            erodedCells = MoveMesh.BakeErodedGrid(grid, entitySize);
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
                    path = ReconstructPath(startColumn, startRow, currentNode);
                    return true;
                }

                foreach (var (dx, dy, moveCost) in _directions)
                {
                    int testColumn = currentNode.column + dx;
                    int testRow = currentNode.row + dy;
                    if (testColumn < 0 || testColumn >= cachedColumns || testRow < 0 || testRow >= cachedRows) { continue; }

                    int neighbourIndex = testRow * cachedColumns + testColumn;
                    if (closed[neighbourIndex]) { continue; }
                    if (!IsWalkableEroded(testColumn, testRow)) { continue; }

                    if (dx != 0 && dy != 0)
                    {
                        // If angled direction, check for viability to walk each individual direction to avoid clipping
                        bool xWalkViable = IsWalkableEroded(currentNode.column + dx, currentNode.row);
                        bool yWalkViable = IsWalkableEroded(currentNode.column, currentNode.row + dy);
                        if (!xWalkViable || !yWalkViable) { continue; }
                    }

                    float traversalCost = cachedMoveMesh.walkabilityGrid.GetTraversalCost(testColumn, testRow);
                    float tentativeGridCost = currentNode.gridCost + moveCost * traversalCost;
                    if (tentativeGridCost >= gridCosts[neighbourIndex]) { continue; }

                    gridCosts[neighbourIndex] = tentativeGridCost;
                    AStarNode neighbourNode = RentNode(testColumn, testRow, tentativeGridCost, DistanceEquivalentHeuristic(testColumn, testRow, targetColumn, targetRow), currentNode);
                    nodeMap[neighbourIndex] = neighbourNode;
                    HeapPush(openHeap, neighbourNode);
                }
            }

            path = new List<Vector2> { cachedMoveMesh.CellToWorld(startColumn, startRow) };
            return false;
        }
        
        private bool IsWalkableEroded(int column, int row)
        {
            if (column < 0 || column >= cachedColumns || row < 0 || row >= cachedRows) { return false; }
            return erodedCells[row * cachedColumns + column];
        }

        private void ReinitializeGrid(int startColumn, int startRow, int targetColumn, int targetRow)
        {
            int cellCount = cachedColumns * cachedRows;

            // Reset per-call state without reallocating
            System.Array.Clear(closed, 0, cellCount);
            System.Array.Fill (gridCosts, float.MaxValue, 0, cellCount);
            System.Array.Clear(nodeMap, 0, cellCount);
            openHeap.Clear();
            nodePoolIndex = 0;

            int startIndex = startRow * cachedColumns + startColumn;
            gridCosts[startIndex] = 0f;

            AStarNode startNode = RentNode(startColumn, startRow, 0f, DistanceEquivalentHeuristic(startColumn, startRow, targetColumn, targetRow), null);
            nodeMap[startIndex] = startNode;
            HeapPush(openHeap, startNode);
        }
        
        private List<Vector2> ReconstructPath(int startColumn, int startRow, AStarNode targetNode)
        {
            var cellPath = new List<AStarNode>();
            AStarNode currentNode = targetNode;
            while (currentNode != null)
            {
                cellPath.Add(currentNode);
                currentNode = currentNode.parent;
            }
            cellPath.Reverse();

            Vector2 initialPosition = cachedMoveMesh.CellToWorld(startColumn, startRow);
            var worldPath = new List<Vector2>(cellPath.Count);
            worldPath.AddRange(cellPath.Select(node => cachedMoveMesh.CellToWorld(node.column, node.row)));

            return StringPull(initialPosition, worldPath, deltaSquaredThreshold, crossThreshold);
        }
        #endregion

        #region StaticMethods
        private static List<Vector2> StringPull(Vector2 initialPosition, List<Vector2> path, float deltaSquaredThreshold, float crossThreshold)
        {
            if (path.Count <= 1) { return path; }

            // Insert initial position so we can check path and prevent returning a path entry immediately adjacent
            // Note:  Next check below will be viable since path >=2 and we add a third entry here
            path.Insert(0, initialPosition);
            
            var result = new List<Vector2> { path[0] };
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 previous = result[^1];
                Vector2 current = path[i];
                Vector2 next = path[i + 1];
                Vector2 aDirection = (current - previous).normalized;
                Vector2 bDirection = (next - current).normalized;
                
                float cross = Mathf.Abs(aDirection.x * bDirection.y - aDirection.y * bDirection.x);
                if (cross < crossThreshold) { continue; }
                
                float deltaSquared = Mathf.Pow(current.x - previous.x, 2) + Mathf.Pow(current.y - previous.y, 2);
                if (deltaSquared < deltaSquaredThreshold) { continue; }
                
                result.Add(current);
            }
            
            // Final entry check & add (distance only since no next directional viable)
            Vector2 finalPrevious = result[^1];
            Vector2 finalCurrent = path[^1];
            if (Mathf.Pow(finalCurrent.x - finalPrevious.x, 2) + Mathf.Pow(finalCurrent.y - finalPrevious.y, 2) >= deltaSquaredThreshold)
            {
                result.Add(path[^1]);
            }
            
            // Remove initial position out of results
            if (result.Count > 1) { result.RemoveAt(0); }
            
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
