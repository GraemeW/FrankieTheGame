using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    public class MoveMesh : MonoBehaviour
    {
        // Tunables
        [Header("Functional Input")]
        [SerializeField, Tooltip("Add parent game objects to check for child colliders")] private List<GameObject> additionalColliderSources = new();
        [SerializeField, Tooltip("World units per grid cell")] private float cellSize = 0.01f;
        [SerializeField, Tooltip("Extra grid margin around the collider bounding box.")] private float padding = 1f;
        [SerializeField, Tooltip("Error allowance in polygon simplification")] private float polygonSimplificationEpsilon = 0.02f;
        [SerializeField, Tooltip("Number of segments to make circle to polygon")] private int circleSegments = 24;
        [Header("Gizmo Parameters")]
        [SerializeField] private Color gizmoColor = new(0.2f, 0.5f, 1.0f, 0.35f);
        [Header("State")]
        [field: SerializeField, HideInInspector] public List<SerializablePolygon> enclosedRegions { get; private set; } = new();
        [field: SerializeField, HideInInspector] public List<SerializablePolygon> additionalColliderPolygons { get; private set; } = new();
        [NonSerialized] private readonly List<Mesh> regionMeshes = new();

        // Static
        private const string _gizmoMeshName = "EnclosedRegionGizmoMesh";
        
        #region PublicMethods
        public void RunDetection(Action<string, float> onProgress = null)
        {
            onProgress?.Invoke("Clearing previous data...", 0f);
            enclosedRegions.Clear();
            additionalColliderPolygons.Clear();

            onProgress?.Invoke("Gathering collider contours...", 0.05f);
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

            onProgress?.Invoke("Computing bounding box...", 0.1f);
            Rect bounds = ComputeBounds(allContours);

            onProgress?.Invoke("Building occupancy grid...", 0.15f);
            bool[] grid = BuildOccupancyGrid(allContours, bounds, out int cols, out int rows);

            onProgress?.Invoke("Rasterising additional colliders...", 0.3f);
            RasteriseAdditionalColliders(grid, cols, rows, bounds);

            onProgress?.Invoke("Flood filling exterior...", 0.5f);
            bool[] outside = FloodFillOutside(grid, cols, rows);

            onProgress?.Invoke("Identifying enclosed cells...", 0.65f);
            bool[] enclosed = BuildEnclosedGrid(grid, outside, cols, rows);

            onProgress?.Invoke("Tracing region contours...", 0.75f);
            var regions = TraceRegionContours(enclosed, cols, rows, bounds);

            onProgress?.Invoke("Storing results...", 0.88f);
            foreach (var pts in regions.Where(pts => pts is { Count: >= 3 }))
            {
                enclosedRegions.Add(new SerializablePolygon { points = pts });
            }

            onProgress?.Invoke("Baking gizmo meshes...", 0.93f);
            BakeGizmoMeshes();

            onProgress?.Invoke("Done.", 1f);
            Debug.Log($"[EnclosedRegionFinder] Found {enclosedRegions.Count} enclosed region(s).");
        }
        
        public void ClearData()
        {
            enclosedRegions.Clear();
            additionalColliderPolygons.Clear();
            foreach (Mesh m in regionMeshes.Where(m => m != null))
            {
                DestroyImmediate(m);
            }
            regionMeshes.Clear();
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
        
        private void RasteriseAdditionalColliders(bool[] grid, int columns, int rows, Rect bounds)
        {
            additionalColliderPolygons.Clear();
            if (additionalColliderSources == null) { return; }
            
            foreach (Collider2D col in additionalColliderSources.Where(source => source != null).SelectMany(source => source.GetComponentsInChildren<Collider2D>()))
            {
                switch (col)
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
            }
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
            if (enclosedRegions == null || enclosedRegions.Count == 0) { return; }
            if (regionMeshes.Count != enclosedRegions.Count) { BakeGizmoMeshes(); }

            var outlineColor = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            for (int i = 0; i < enclosedRegions.Count; i++)
            {
                SerializablePolygon region = enclosedRegions[i];
                if (region.points == null || region.points.Count < 3) continue;

                Mesh mesh = (i < regionMeshes.Count) ? regionMeshes[i] : null;
                if (mesh != null)
                {
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawMesh(mesh, Vector3.zero, Quaternion.identity, Vector3.one);
                }
                
                Gizmos.color = outlineColor;
                var pts = region.points;
                for (int j = 0; j < pts.Count; j++)
                {
                    int k = (j + 1) % pts.Count;
                    Gizmos.DrawLine(new Vector3(pts[j].x, pts[j].y, 0f), new Vector3(pts[k].x, pts[k].y, 0f));
                }
            }
        }
        
        private void BakeGizmoMeshes()
        {
            foreach (Mesh mesh in regionMeshes.Where(mesh => mesh != null))
            {
                DestroyImmediate(mesh);
            }
            regionMeshes.Clear();

            foreach (var region in enclosedRegions)
            {
                if (region.points == null || region.points.Count < 3)
                {
                    regionMeshes.Add(null);
                    continue;
                }
                regionMeshes.Add(BuildRegionMesh(region.points));
            }
        }
        
        private Mesh BuildRegionMesh(List<Vector2> points)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                if (p.y < minY) { minY = p.y; }
                if (p.y > maxY) { maxY = p.y; }
            }

            for (float y = minY + cellSize * 0.5f; y < maxY; y += cellSize)
            {
                List<(float left, float right)> regionSpans = GetScanlineSpans(points, y);
                if (regionSpans.Count == 0) { continue; }
                
                var carveSpans = new List<(float left, float right)>();
                foreach (var carvePoly in additionalColliderPolygons)
                {
                    carveSpans.AddRange(GetScanlineSpans(carvePoly.points, y));
                }
                
                List<(float left, float right)> finalSpans = SubtractSpans(regionSpans, carveSpans);

                foreach (var (xLeft, xRight) in finalSpans)
                {
                    float yBot = y - cellSize * 0.5f;
                    float yTop = y + cellSize * 0.5f;
                    int baseIdx= vertices.Count;

                    vertices.Add(new Vector3(xLeft, yBot, 0f));
                    vertices.Add(new Vector3(xRight, yBot, 0f));
                    vertices.Add(new Vector3(xRight, yTop, 0f));
                    vertices.Add(new Vector3(xLeft, yTop, 0f));

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
