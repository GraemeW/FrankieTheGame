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
        public List<bool> cells = new List<bool>();

        public bool GetCell(int column, int row)
        {
            if (column < 0 || column >= columns || row < 0 || row >= rows) { return false; }
            return cells[row * columns + column];
        }

        // Local-space cell centre (no transform applied — used via MoveMesh)
        public Vector2 CellToLocal(int col, int row) => new Vector2(originX + (col + 0.5f) * cellSize, originY + (row + 0.5f) * cellSize);
    }
}
