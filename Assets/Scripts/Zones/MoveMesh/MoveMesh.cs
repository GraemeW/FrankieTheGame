using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class MoveMesh : MonoBehaviour
    {
        // Tunables
        [Header("Functional Input")]
        [SerializeField, Tooltip("Add parent game objects to check for child colliders")] private List<GameObject> additionalColliderSources = new();
        [SerializeField, Tooltip("World units per grid cell")] private float cellSize = 0.05f;
        [SerializeField, Tooltip("Amount to erode inward from obstacle edges")] private float walkabilityErosionRadius = 0.07f;
        [SerializeField][Tooltip("Max cost added to immediate unwalkable cells")] private float edgeCostPenalty = 2f;
        [SerializeField][Tooltip("Cost falloff with distance from edge (higher == steeper falloff")] private float edgeCostFalloff = 1.5f;
        [SerializeField, Tooltip("Extra grid margin around the collider bounding box.")] private float padding = 1f;
        [SerializeField, Tooltip("Error allowance in polygon simplification")] private float polygonSimplificationEpsilon = 0.065f;
        [SerializeField, Tooltip("Number of segments to make circle to polygon")] private int circleSegments = 24;
        [Header("Gizmo Parameters")]
        [SerializeField] private Color gizmoColor = new(0.2f, 0.5f, 1.0f, 0.35f);
        
        // State
        [field: SerializeField, HideInInspector] public WalkabilityGrid walkabilityGrid { get; private set; } = new();
        [field: SerializeField, HideInInspector] private bool isMeshOutlineInitialized;
        [NonSerialized] private readonly List<SerializablePolygon> enclosedRegions = new();
        [NonSerialized] private readonly List<SerializablePolygon> additionalColliderPolygons = new();
        [NonSerialized] private Mesh walkabilityMesh;
        
        // Capture State
        private float currentProgress;
        
        // Cached References
        private BoxCollider2D meshOutline;
        
        // Static
        private const string _moveMeshLayer = "MoveMesh";
        private const string _gizmoMeshName = "EnclosedRegionGizmoMesh";
        
        #region UnityMethods
        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer(_moveMeshLayer);
            InitializeCollider(new Rect(0f, 0f, 1f, 1f), false);
        }

        private void Start()
        {
            if (walkabilityGrid == null || walkabilityGrid.IsEmpty()) { return; }
            float[] traversalCosts = BakeTraversalCosts(walkabilityGrid, edgeCostPenalty, edgeCostFalloff);
            walkabilityGrid.traversalCosts = traversalCosts.ToList();
        }

        private void InitializeCollider(Rect bounds, bool setInitialized = true)
        {
            if (meshOutline == null && !TryGetComponent(out meshOutline)) { return; }
            if (isMeshOutlineInitialized) { return; }
            
            meshOutline.isTrigger = true;
            meshOutline.size = new Vector2(bounds.width, bounds.height);
            Vector2 localCenter= transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.center.y, 0f));
            meshOutline.offset = localCenter;
            meshOutline.enabled = setInitialized;
            isMeshOutlineInitialized = setInitialized;
        }
        #endregion
        
        #region MeshAccessMethods
        public static int GetMoveMeshLayerMask() => LayerMask.GetMask(_moveMeshLayer);
        
        public bool WorldToCell(Vector2 worldPos, out int column, out int row)
        {
            if (walkabilityGrid == null)
            {
                column = 0;
                row = 0;
                return false;
            }
            
            Vector2 local = transform.InverseTransformPoint(worldPos);
            column = Mathf.FloorToInt((local.x - walkabilityGrid.originX) / walkabilityGrid.cellSize);
            row = Mathf.FloorToInt((local.y - walkabilityGrid.originY) / walkabilityGrid.cellSize);
            return column >= 0 && column < walkabilityGrid.columns && row >= 0 && row < walkabilityGrid.rows;
        }
        
        public Vector2 CellToWorld(int column, int row)
        {
            if (walkabilityGrid == null) { return Vector2.zero; }
            Vector2 local = walkabilityGrid.CellToLocal(column, row);
            return transform.TransformPoint(local);
        }
        #endregion
        
        #region MeshGenerationMethods
        public void RunDetection(Action<string, float> onProgress = null)
        {
            currentProgress = 0f;
            onProgress?.Invoke("Clearing previous data...", currentProgress);
            enclosedRegions.Clear();
            additionalColliderPolygons.Clear();

            currentProgress = 0.05f;
            onProgress?.Invoke("Gathering collider contours...", currentProgress);
            var allContours = new List<List<Vector2>>();
            foreach (CompositeCollider2D compositeCollider2D in GetComponentsInChildren<CompositeCollider2D>())
            {
                allContours.AddRange(GetWorldSpaceContours(compositeCollider2D));
            }

            if (allContours.Count == 0)
            {
                Debug.LogWarning("[EnclosedRegionFinder] No CompositeCollider2D children found.");
                return;
            }

            currentProgress = 0.1f;
            onProgress?.Invoke("Computing bounding box...", currentProgress);
            Rect bounds = ComputeBounds(allContours);

            currentProgress = 0.2f;
            onProgress?.Invoke("Building occupancy grid...", currentProgress);
            bool[] grid = BuildOccupancyGrid(allContours, bounds, out int cols, out int rows);

            currentProgress = 0.3f;
            onProgress?.Invoke("Rasterising additional colliders...", currentProgress);
            RasteriseAdditionalColliders(grid, cols, rows, bounds, onProgress);

            currentProgress = 0.8f;
            onProgress?.Invoke("Flood filling exterior...", currentProgress);
            bool[] outside = FloodFillOutside(grid, cols, rows);

            currentProgress = 0.85f;
            onProgress?.Invoke("Identifying enclosed cells...", currentProgress);
            bool[] enclosed = BuildEnclosedGrid(grid, outside, cols, rows);

            currentProgress = 0.9f;
            onProgress?.Invoke("Tracing region contours...", currentProgress);
            var regions = TraceRegionContours(enclosed, cols, rows, bounds);

            currentProgress = 0.93f;
            onProgress?.Invoke("Storing results...", currentProgress);
            foreach (var pts in regions.Where(pts => pts is { Count: >= 3 }))
            {
                enclosedRegions.Add(new SerializablePolygon { points = pts });
            }
            
            onProgress?.Invoke("Baking walkability grid...", 0.95f);
            BakeWalkabilityGrid(bounds);

            currentProgress = 0.98f;
            onProgress?.Invoke("Baking gizmo meshes...", currentProgress);
            BakeGizmoMeshes();

            currentProgress = 0.99f;
            onProgress?.Invoke("Setting up collider bounds...", currentProgress);
            InitializeCollider(bounds);
            
            currentProgress = 1.0f;
            onProgress?.Invoke("Done.", currentProgress);
            Debug.Log($"[EnclosedRegionFinder] Found {enclosedRegions.Count} enclosed region(s).");
        }
        
        public void ClearData()
        {
            enclosedRegions.Clear();
            additionalColliderPolygons.Clear();
            walkabilityGrid = new WalkabilityGrid();
            if (walkabilityMesh != null) { DestroyImmediate(walkabilityMesh); }
            isMeshOutlineInitialized = false;
        }
        #endregion
        
        #region PrivateMethods
        private static List<List<Vector2>> GetWorldSpaceContours(CompositeCollider2D compositeCollider2D)
        {
            var result = new List<List<Vector2>>();
            Transform transform = compositeCollider2D.transform;
            for (int i = 0; i < compositeCollider2D.pathCount; i++)
            {
                int count = compositeCollider2D.GetPathPointCount(i);
                var points = new Vector2[count];
                compositeCollider2D.GetPath(i, points);
                var world = new List<Vector2>(count);
                world.AddRange(points.Select(p => transform.TransformPoint(p)).Select(dummy => (Vector2)dummy));
                result.Add(world);
            }
            return result;
        }

        private Rect ComputeBounds(List<List<Vector2>> contours)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (Vector2 point in contours.SelectMany(c => c))
            {
                if (point.x < minX) minX = point.x;
                if (point.y < minY) minY = point.y;
                if (point.x > maxX) maxX = point.x;
                if (point.y > maxY) maxY = point.y;
            }
            return new Rect(minX - padding, minY - padding, (maxX - minX) + padding * 2, (maxY - minY) + padding * 2);
        }

        private bool[] BuildOccupancyGrid(List<List<Vector2>> contours, Rect bounds, out int columns, out int rows)
        {
            columns = Mathf.CeilToInt(bounds.width  / cellSize);
            rows = Mathf.CeilToInt(bounds.height / cellSize);

            var winding = new int[columns * rows];
            foreach (var poly in contours)
            {
                RasterisePolygonWindingToGrid(poly, winding, columns, rows, bounds);
            }

            var grid = new bool[columns * rows];
            for (int i = 0; i < winding.Length; i++)
            {
                grid[i] = winding[i] != 0;
            }

            return grid;
        }

        private void RasterisePolygonWindingToGrid(List<Vector2> polygon, int[] winding, int columns, int rows, Rect bounds)
        {
            int numberOfPoints = polygon.Count;
            var crossings = new List<(float x, int sign)>();

            for (int row = 0; row < rows; row++)
            {
                float y = bounds.yMin + (row + 0.5f) * cellSize;
                crossings.Clear();

                for (int i = 0, j = numberOfPoints - 1; i < numberOfPoints; j = i++)
                {
                    float y0 = polygon[j].y;
                    float y1 = polygon[i].y;
                    if ((y0 < y) == (y1 < y)) { continue; }

                    float x = polygon[j].x + (y - y0) / (y1 - y0) * (polygon[i].x - polygon[j].x);
                    int sign = (y1 > y0) ? 1 : -1;
                    crossings.Add((x, sign));
                }

                if (crossings.Count == 0) { continue; }
                crossings.Sort((a, b) => a.x.CompareTo(b.x));

                int runningWinding = 0;
                int crossingIndex = 0;

                for (int c = 0; c < columns; c++)
                {
                    float cellCentreX = bounds.xMin + (c + 0.5f) * cellSize;

                    while (crossingIndex < crossings.Count && crossings[crossingIndex].x < cellCentreX)
                    {
                        runningWinding += crossings[crossingIndex].sign;
                        crossingIndex++;
                    }

                    winding[row * columns + c] += runningWinding;
                }
            }
        } 
        
        private void RasteriseAdditionalColliders(bool[] grid, int columns, int rows, Rect bounds, Action<string, float> onProgress = null)
        {
            additionalColliderPolygons.Clear();
            if (additionalColliderSources == null) { return; }

            List<Collider2D> additionalColliders = additionalColliderSources.Where(source => source != null).SelectMany(source => source.GetComponentsInChildren<Collider2D>()).ToList();

            int colliderIndex = 1;
            int totalColliders = additionalColliders.Count;
            foreach (Collider2D additionalCollider in additionalColliders)
            {
                onProgress?.Invoke($"Rasterising additional colliders... On:  {colliderIndex} of {totalColliders}", currentProgress);
                switch (additionalCollider)
                {
                    case BoxCollider2D boxCollider2D:
                        RasteriseBox(boxCollider2D, grid, columns, rows, bounds);
                        break;
                    case CircleCollider2D circleCollider2D:
                        RasteriseCircle(circleCollider2D, grid, columns, rows, bounds);
                        break;
                    case CapsuleCollider2D capsuleCollider2D:
                        RasteriseCapsule(capsuleCollider2D, grid, columns, rows, bounds);
                        break;
                    case PolygonCollider2D polygonCollider2D:
                        RasterisePolygon2D(polygonCollider2D, grid, columns, rows, bounds);
                        break;
                }
                colliderIndex++;
            }
        }
        private void BakeWalkabilityGrid(Rect bounds)
        {
            int columns = Mathf.CeilToInt(bounds.width  / cellSize);
            int rows = Mathf.CeilToInt(bounds.height / cellSize);

            walkabilityGrid.columns = columns;
            walkabilityGrid.rows = rows;
            walkabilityGrid.cellSize = cellSize;

            // Store origin in local space so the grid moves with the GameObject
            Vector2 localOrigin= transform.InverseTransformPoint(new Vector3(bounds.xMin, bounds.yMin, 0f));
            walkabilityGrid.originX = localOrigin.x;
            walkabilityGrid.originY = localOrigin.y;

            var cells = new bool[columns * rows];

            for (int row = 0; row < rows; row++)
            {
                float worldY = bounds.yMin + (row + 0.5f) * cellSize;

                var regionSpans = new List<(float left, float right)>();
                foreach (SerializablePolygon polygon in enclosedRegions)
                {
                    regionSpans.AddRange(GetScanlineSpans(polygon.points, worldY));
                }
                if (regionSpans.Count == 0) { continue; }

                var carveSpans = new List<(float left, float right)>();
                foreach (SerializablePolygon carvePoly in additionalColliderPolygons)
                {
                    carveSpans.AddRange(GetScanlineSpans(carvePoly.points, worldY));

                }

                var walkableSpans = SubtractSpans(regionSpans, carveSpans);

                foreach (var (xLeft, xRight) in walkableSpans)
                {
                    // Convert world-space span extents to local space for column indexing
                    float localXLeft = transform.InverseTransformPoint(new Vector3(xLeft,  worldY, 0f)).x;
                    float localXRight = transform.InverseTransformPoint(new Vector3(xRight, worldY, 0f)).x;

                    int startColumn = Mathf.Max(0, Mathf.FloorToInt((localXLeft  - walkabilityGrid.originX) / cellSize));
                    int endColumn = Mathf.Min(columns - 1, Mathf.FloorToInt((localXRight - walkabilityGrid.originX) / cellSize));
                    for (int column = startColumn; column <= endColumn; column++)
                    {
                        cells[row * columns + column] = true;
                    }
                }
            }
            walkabilityGrid.cells = cells.ToList();
            
            bool[] erodedGrid = BakeErodedGrid(walkabilityGrid, walkabilityErosionRadius);
            walkabilityGrid.cells = erodedGrid.ToList();
            
            // Note:  Volatile, re-set on Start()
            float[] traversalCosts = BakeTraversalCosts(walkabilityGrid, edgeCostPenalty, edgeCostFalloff);
            walkabilityGrid.traversalCosts = traversalCosts.ToList();
        }

        private void RasteriseBox(BoxCollider2D boxCollider2D, bool[] grid, int columns, int rows, Rect bounds)
        {
            Vector2 halfSize = boxCollider2D.size * 0.5f;
            Vector2 offset = boxCollider2D.offset;
            var corners = new List<Vector2>
            {
                TransformPoint(boxCollider2D.transform, offset + new Vector2(-halfSize.x, -halfSize.y)),
                TransformPoint(boxCollider2D.transform, offset + new Vector2(halfSize.x, -halfSize.y)),
                TransformPoint(boxCollider2D.transform, offset + new Vector2(halfSize.x, halfSize.y)),
                TransformPoint(boxCollider2D.transform, offset + new Vector2(-halfSize.x, halfSize.y)),
            };
            additionalColliderPolygons.Add(new SerializablePolygon { points = EnsureCounterClockwise(corners) });
            RasteriseSimplePolygonToGrid(corners, grid, columns, rows, bounds);
        }

        private void RasteriseCircle(CircleCollider2D col, bool[] grid, int columns, int rows, Rect bounds)
        {
            Vector2 centre = TransformPoint(col.transform, col.offset);

            // Scale the radius by the transform's lossy scale (take the larger axis so the circle always fully covers what the collider covers)
            float worldRadius = col.radius * Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.y);

            var points = new List<Vector2>(circleSegments);
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = 2f * Mathf.PI * i / circleSegments;
                points.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * worldRadius);
            }
            additionalColliderPolygons.Add(new SerializablePolygon { points = EnsureCounterClockwise(points) });
            RasteriseSimplePolygonToGrid(points, grid, columns, rows, bounds);
        }

        private void RasteriseCapsule(CapsuleCollider2D capsuleCollider2D, bool[] grid, int columns, int rows, Rect bounds)
        {
            // A capsule is two hemicircles joined by a rectangle.
            // Build it in local space then transform each point to world space.
            int hemiSegments = Mathf.RoundToInt(circleSegments / 2f);
            Vector2 size = capsuleCollider2D.size;
            Vector2 off = capsuleCollider2D.offset;
            CapsuleDirection2D capsuleDirection = capsuleCollider2D.direction;

            var points = new List<Vector2>();

            if (capsuleDirection == CapsuleDirection2D.Vertical)
            {
                float radius = size.x * 0.5f;
                float halfBody = Mathf.Max(0f, size.y * 0.5f - radius);

                // Top hemisphere
                for (int i = 0; i <= hemiSegments; i++)
                {
                    float angle = Mathf.PI * i / hemiSegments; // 0-to-PI  (right to left across top)
                    points.Add(TransformPoint(capsuleCollider2D.transform, off + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius + new Vector2(0f, halfBody)));
                }
                // Bottom hemisphere
                for (int i = 0; i <= hemiSegments; i++)
                {
                    float angle = Mathf.PI + Mathf.PI * i / hemiSegments; // PI-to-2PI
                    points.Add(TransformPoint(capsuleCollider2D.transform, off + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius + new Vector2(0f, -halfBody)));
                }
            }
            else // Horizontal
            {
                float radius   = size.y * 0.5f;
                float halfBody = Mathf.Max(0f, size.x * 0.5f - radius);

                // Right hemisphere
                for (int i = 0; i <= hemiSegments; i++)
                {
                    float angle = -Mathf.PI * 0.5f + Mathf.PI * i / hemiSegments;
                    points.Add(TransformPoint(capsuleCollider2D.transform, off + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius + new Vector2(halfBody, 0f)));
                }
                // Left hemisphere
                for (int i = 0; i <= hemiSegments; i++)
                {
                    float angle = Mathf.PI * 0.5f + Mathf.PI * i / hemiSegments;
                    points.Add(TransformPoint(capsuleCollider2D.transform, off + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius + new Vector2(-halfBody, 0f)));
                }
            }

            additionalColliderPolygons.Add(new SerializablePolygon { points = EnsureCounterClockwise(points) });
            RasteriseSimplePolygonToGrid(points, grid, columns, rows, bounds);
        }

        private void RasterisePolygon2D(PolygonCollider2D polygonCollider2D, bool[] grid, int columns, int rows, Rect bounds)
        {
            var winding = new int[grid.Length];

            for (int i = 0; i < polygonCollider2D.pathCount; i++)
            {
                Vector2[] local = polygonCollider2D.GetPath(i);
                if (local == null || local.Length == 0) { continue; }
                
                var world = new List<Vector2>(local.Length);
                world.AddRange(local.Select(p => TransformPoint(polygonCollider2D.transform, p)));
                additionalColliderPolygons.Add(new SerializablePolygon { points = EnsureCounterClockwise(world) });
                RasterisePolygonWindingToGrid(world, winding, columns, rows, bounds);
            }

            for (int i = 0; i < grid.Length; i++)
            {
                if (winding[i] != 0) { grid[i] = true; }
            }
        }
        
        private void RasteriseSimplePolygonToGrid(List<Vector2> poly, bool[] grid, int cols, int rows, Rect bounds)
        {
            int n = poly.Count;
            var crossings = new List<float>();

            for (int row = 0; row < rows; row++)
            {
                float y = bounds.yMin + (row + 0.5f) * cellSize;
                crossings.Clear();

                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float y0 = poly[j].y, y1 = poly[i].y;
                    if ((y0 < y) == (y1 < y)) continue;
                    float x = poly[j].x + (y - y0) / (y1 - y0) * (poly[i].x - poly[j].x);
                    crossings.Add(x);
                }
                crossings.Sort();

                for (int k = 0; k + 1 < crossings.Count; k += 2)
                {
                    int startCol = Mathf.Max(0, Mathf.FloorToInt((crossings[k] - bounds.xMin) / cellSize));
                    int endCol = Mathf.Min(cols - 1, Mathf.CeilToInt ((crossings[k + 1] - bounds.xMin) / cellSize));
                    for (int c = startCol; c <= endCol; c++)
                    {
                        grid[row * cols + c] = true;
                    }
                }
            }
        }

        private List<List<Vector2>> TraceRegionContours(bool[] enclosedGrid, int columns, int rows, Rect bounds)
        {
            var labels = new int[enclosedGrid.Length];
            int numberOfRegions = 0;

            for (int entry = 0; entry < enclosedGrid.Length; entry++)
            {
                if (!enclosedGrid[entry] || labels[entry] != 0) { continue; }
                numberOfRegions++;
                var queue = new Queue<int>();
                queue.Enqueue(entry);
                labels[entry] = numberOfRegions;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    int row = index / columns;
                    int column = index % columns;

                    int[] nbs = { (row + 1) * columns + column, (row - 1) * columns + column, row * columns + (column + 1), row * columns + (column - 1) };
                    foreach (int nb in nbs)
                    {
                        if (nb < 0 || nb >= enclosedGrid.Length) { continue; }
                        if (!enclosedGrid[nb] || labels[nb] != 0) { continue; }
                        labels[nb] = numberOfRegions;
                        queue.Enqueue(nb);
                    }
                }
            }

            var results = new List<List<Vector2>>();
            for (int id = 1; id <= numberOfRegions; id++)
            {
                List<Vector2> contour = MarchingSquaresContour(enclosedGrid, labels, id, columns, rows, bounds);
                if (contour is { Count: >= 3 }) { results.Add(contour); }
            }
            return results;
        }

        private List<Vector2> MarchingSquaresContour(bool[] enclosedGrid, int[] labels, int regionId, int columns, int rows, Rect bounds)
        {
            // Find the bottom-most cell in this region with empty space below it
            int startColumn = -1, startRow = -1;
            for (int r = 0; r < rows && startColumn == -1; r++)
            {
                for (int c = 0; c < columns && startColumn == -1; c++)
                {
                    if (!Inside(c, r) || Inside(c, r - 1)) { continue; }
                    startColumn = c;
                    startRow = r;
                }
            }

            if (startColumn == -1) { return null; }

            var contourCells = new List<(int c, int r)>();
            int currentColumn = startColumn, currentRow = startRow, direction = 0;
            int maxSteps = columns * rows * 4;

            for (int step = 0; step < maxSteps; step++)
            {
                contourCells.Add((currentColumn, currentRow));

                // Prefer left turn, then straight, then right turn, then reverse
                int[] tryDirections = { (direction + 1) % 4, direction, (direction + 3) % 4, (direction + 2) % 4 };
                bool moved = false;
                foreach (int tryDirection in tryDirections)
                {
                    (int dx, int dy) = DirectionalDelta(tryDirection);
                    int newColumn = currentColumn + dx, newRow = currentRow + dy;
                    if (Inside(newColumn, newRow))
                    {
                        direction = tryDirection; currentColumn = newColumn; currentRow = newRow;
                        moved = true;
                        break;
                    }
                }

                if (!moved || (currentColumn == startColumn && currentRow == startRow)) { break; }
            }

            var polygon = new List<Vector2>(contourCells.Count);
            foreach ((int column, int row) in contourCells)
            {
                polygon.Add(CellMid(column, row));
            }
            return SimplifyPolygon(polygon, polygonSimplificationEpsilon);


            Vector2 CellMid(int column, int row) => new(bounds.xMin + (column + 0.5f) * cellSize, bounds.yMin + (row + 0.5f) * cellSize);
            bool Inside(int column, int row)
            {
                if (column < 0 || column >= columns || row < 0 || row >= rows) { return false; }
                int index = row * columns + column;
                return enclosedGrid[index] && labels[index] == regionId;
            }
        }
        #endregion

        #region StaticMethods
        public static bool[] BakeErodedGrid(WalkabilityGrid walkabilityGrid, float erosionRadius)
        {
            bool[] cells = walkabilityGrid.cells.ToArray();
            if (erosionRadius <= 0f) { return cells; }
            int rows = walkabilityGrid.rows;
            int columns = walkabilityGrid.columns;

            var erodedCells = new bool[cells.Length];
            int radiusCells = Mathf.CeilToInt(erosionRadius / walkabilityGrid.cellSize);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (!walkabilityGrid.GetCell(column, row))
                    {
                        erodedCells[row * columns + column] = false;
                        continue;
                    }

                    bool tooClose = false;
                    for (int deltaRow = -radiusCells; deltaRow <= radiusCells && !tooClose; deltaRow++)
                    {
                        for (int deltaColumn = -radiusCells; deltaColumn <= radiusCells && !tooClose; deltaColumn++)
                        {
                            float worldDistance = Mathf.Sqrt(deltaColumn * deltaColumn + deltaRow * deltaRow) * walkabilityGrid.cellSize;
                            if (worldDistance > erosionRadius) { continue; }

                            int testColumn = column + deltaColumn;
                            int testRow = row + deltaRow;

                            if (testColumn < 0 || testColumn >= columns || testRow < 0 || testRow >= rows || !walkabilityGrid.GetCell(testColumn, testRow))
                            {
                                tooClose = true;
                            }
                        }
                    }
                    erodedCells[row * columns + column] = !tooClose;
                }
            }
            return erodedCells;
        }

        private static float[] BakeTraversalCosts(WalkabilityGrid walkabilityGrid, float edgeCostPenalty, float edgeCostFalloff)
        {
            bool[] cells = walkabilityGrid.cells.ToArray();
            int rows = walkabilityGrid.rows;
            int columns = walkabilityGrid.columns;
            
            int cellCount = columns * rows;
            var costs = new float[cellCount];
            if (edgeCostPenalty <= 0f || edgeCostFalloff <= 0f) { return costs; }
            
            // Initialize based on walkability cell existence
            var distanceCells = new float[cellCount];
            var queue = new Queue<int>();
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    if (!cells[index])
                    {
                        distanceCells[index] = 0f;
                        queue.Enqueue(index);
                    }
                    else { distanceCells[index] = Mathf.Infinity; }
                }
            }
            
            // Generate distances
            int[] deltaColumns = {0, 0,  1, -1};
            int[] deltaRows = {1, -1, 0, 0};
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int row = index / columns;
                int column = index % columns;

                for (int delta = 0; delta < 4; delta++)
                {
                    int testColumn = column + deltaColumns[delta];
                    int testRow = row + deltaRows[delta];
                    if (testColumn < 0 || testColumn >= columns || testRow < 0 || testRow >= rows) { continue; }

                    int testIndex = testRow * columns + testColumn;
                    float tentative = distanceCells[index] + 1f;
                    if (tentative >= distanceCells[testIndex]) continue;

                    distanceCells[testIndex] = tentative;
                    queue.Enqueue(testIndex);
                }
            }

            // Max distance used for normalization
            float maxDist = 0f;
            for (int i = 0; i < cellCount; i++)
            {
                if (cells[i] && distanceCells[i] > maxDist) { maxDist = distanceCells[i]; }
            }
            
            // Convert distance to traversal cost
            for (int i = 0; i < cellCount; i++)
            {
                if (!cells[i])
                {
                    costs[i] = Mathf.Infinity;
                    continue;
                }

                float normDist = maxDist > 0f ? Mathf.Clamp01(distanceCells[i] / maxDist) : 1f;
                costs[i] = 1f + edgeCostPenalty * Mathf.Pow(1f - normDist, edgeCostFalloff);
            }

            return costs;
        }
        
        private static Vector2 TransformPoint(Transform t, Vector2 localPoint) => t.TransformPoint(localPoint);
        
        private static (int dx, int dy) DirectionalDelta(int direction) => direction switch
        {
            0 => (1, 0),
            1 => (0, 1),
            2 => (-1, 0),
            _ => (0, -1)
        };
        
        private static float ComputeSignedArea(List<Vector2> pts)
        {
            float area = 0f;
            int n = pts.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                area += (pts[j].x + pts[i].x) * (pts[j].y - pts[i].y);
            }
            return area * 0.5f;
        }

        private static List<Vector2> EnsureCounterClockwise(List<Vector2> pts)
        {
            if (!(ComputeSignedArea(pts) < 0f)) { return pts; }
            var copy = new List<Vector2>(pts);
            copy.Reverse();
            return copy;
        }
        
        private static bool[] FloodFillOutside(bool[] grid, int columns, int rows)
        {
            var outside = new bool[columns * rows];
            var queue = new Queue<int>();

            for (int column = 0; column < columns; column++)
            {
                Enqueue(column);
                Enqueue((rows - 1) * columns + column);
            }
            for (int row = 0; row < rows; row++)
            {
                Enqueue(row * columns);
                Enqueue(row * columns + (columns - 1));
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int row = index / columns; 
                int column = index % columns;
                Enqueue((row + 1) * columns + column);
                Enqueue((row - 1) * columns + column);
                Enqueue(row * columns + (column + 1));
                Enqueue(row * columns + (column - 1));
            }
            return outside;

            
            void Enqueue(int index)
            {
                if (index < 0 || index >= grid.Length) { return; }
                if (grid[index] || outside[index]) { return; }
                outside[index] = true;
                queue.Enqueue(index);
            }
        }

        private static bool[] BuildEnclosedGrid(bool[] grid, bool[] outside, int columns, int rows)
        {
            var enclosed = new bool[columns * rows];
            for (int i = 0; i < grid.Length; i++)
            {
                enclosed[i] = !grid[i] && !outside[i];
            }
            return enclosed;
        }

        private static List<Vector2> SimplifyPolygon(List<Vector2> points, float epsilon)
        {
            if (points.Count < 3) { return points; }
            
            var result = new List<Vector2>();
            SimplifyPolygonRDP(points, 0, points.Count - 1, epsilon, result);
            result.Add(points[^1]);
            return result;
        }

        private static void SimplifyPolygonRDP(List<Vector2> pts, int start, int end, float epsilon, List<Vector2> result)
        {
            // Ramer-Douglas-Peucker polygon simplification
            // Reduces the staircase-y cell-centre polygon to clean straight edges
            
            if (end <= start + 1) { result.Add(pts[start]); return; }

            float maxDist = 0f;
            int maxIdx = start;
            for (int i = start + 1; i < end; i++)
            {
                float d = PointToSegmentDist(pts[i], pts[start], pts[end]);
                if (!(d > maxDist)) { continue; }
                
                maxDist = d; 
                maxIdx = i;
            }

            if (maxDist > epsilon)
            {
                SimplifyPolygonRDP(pts, start, maxIdx, epsilon, result);
                SimplifyPolygonRDP(pts, maxIdx, end, epsilon, result);
            }
            else
            {
                result.Add(pts[start]);
            }
        }

        private static float PointToSegmentDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
            return (ap - ab * t).magnitude;
        }
        #endregion
        
        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if (walkabilityGrid?.cells == null || walkabilityGrid.cells.Count == 0) { return; }
            if (walkabilityMesh == null) { BakeGizmoMeshes(); }
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawMesh(walkabilityMesh, Vector3.zero, Quaternion.identity, Vector3.one);
        }
        
        private void BakeGizmoMeshes()
        {
            if (walkabilityMesh != null) { DestroyImmediate(walkabilityMesh); }
            walkabilityMesh = BuildRegionMesh();
        }
        
        private Mesh BuildRegionMesh()
        {
            var grid = walkabilityGrid;
            var vertices  = new List<Vector3>();
            var triangles = new List<int>();
            float halfCell = grid.cellSize * 0.5f;

            for (int row = 0; row < grid.rows; row++)
            {
                for (int column = 0; column < grid.columns; column++)
                {
                    if (!grid.GetCell(column, row)) { continue; }
                    
                    Vector2 worldCentre = CellToWorld(column, row);
                    float wx = worldCentre.x;
                    float wy = worldCentre.y;

                    int baseIdx = vertices.Count;
                    vertices.Add(new Vector3(wx - halfCell, wy - halfCell, 0f));
                    vertices.Add(new Vector3(wx + halfCell, wy - halfCell, 0f));
                    vertices.Add(new Vector3(wx + halfCell, wy + halfCell, 0f));
                    vertices.Add(new Vector3(wx - halfCell, wy + halfCell, 0f));

                    triangles.Add(baseIdx + 0);
                    triangles.Add(baseIdx + 2);
                    triangles.Add(baseIdx + 1);
                    triangles.Add(baseIdx + 0);
                    triangles.Add(baseIdx + 3);
                    triangles.Add(baseIdx + 2);
                }
            }

            if (vertices.Count == 0) { return null; }

            var mesh = new Mesh
            {
                name = _gizmoMeshName,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<(float left, float right)> GetScanlineSpans(List<Vector2> polygon, float y)
        {
            var result = new List<(float, float)>();
            var xs = new List<float>();
            int n = polygon.Count;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float y0 = polygon[j].y, y1 = polygon[i].y;
                if ((y0 < y) == (y1 < y)) continue;
                float x = polygon[j].x + (y - y0) / (y1 - y0) * (polygon[i].x - polygon[j].x);
                xs.Add(x);
            }
            xs.Sort();

            for (int k = 0; k + 1 < xs.Count; k += 2)
                result.Add((xs[k], xs[k + 1]));

            return result;
        }

        private static List<(float left, float right)> SubtractSpans( List<(float left, float right)> fill, List<(float left, float right)> carve)
        {
            var result = new List<(float, float)>(fill);

            foreach (var (carveLeft, carveRight) in carve)
            {
                var next = new List<(float, float)>();
                foreach (var (fillLeft, fillRight) in result)
                {
                    if (carveRight <= fillLeft || carveLeft >= fillRight)
                    {
                        next.Add((fillLeft, fillRight));
                        continue;
                    }
                    
                    if (fillLeft < carveLeft) { next.Add((fillLeft, carveLeft)); }
                    if (fillRight > carveRight) { next.Add((carveRight, fillRight)); }
                }
                result = next;
            }
            return result;
        }
        #endregion
    }
}
