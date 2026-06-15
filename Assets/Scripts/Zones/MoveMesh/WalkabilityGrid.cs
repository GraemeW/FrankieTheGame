using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.ZoneManagement
{
    [System.Serializable]
    public class WalkabilityGrid
    {
        public int columns;
        public int rows;
        public float cellSize;
        public float originX;
        public float originY;
        public List<bool> cells = new();
        [NonSerialized] public List<float> traversalCosts = new();

        public bool IsEmpty() => columns == 0 || rows == 0 || cells.Count == 0;
        
        public bool GetCell(int column, int row)
        {
            if (column < 0 || column >= columns || row < 0 || row >= rows) { return false; }
            return cells[row * columns + column];
        }

        public float GetTraversalCost(int column, int row)
        {
            if (column < 0 || column >= columns || row < 0 || row >= rows) { return Mathf.Infinity; }
            return traversalCosts[row * columns + column];
        }

        // Local-space cell centre (no transform applied — used via MoveMesh)
        public Vector2 CellToLocal(int column, int row) => new(originX + (column + 0.5f) * cellSize, originY + (row + 0.5f) * cellSize);
    }
}
