using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    public class WalkabilityGridTests
    {
        private static WalkabilityGrid Make3X2Grid()
        {
            return new WalkabilityGrid
            {
                columns = 3,
                rows = 2,
                cellSize = 1f,
                originX = 0f,
                originY = 0f,
                cells = new List<bool> { true, false, true, false, true, false },
                traversalCosts = new List<float> { 1f, 2f, 3f, 4f, 5f, 6f }
            };
        }

        [Test]
        public void IsEmpty_ZeroColumns_ReturnsTrue()
        {
            var grid = new WalkabilityGrid { columns = 0, rows = 5, cells = new List<bool> { true } };
            Assert.That(grid.IsEmpty(), Is.True);
        }

        [Test]
        public void IsEmpty_ZeroRows_ReturnsTrue()
        {
            var grid = new WalkabilityGrid { columns = 5, rows = 0, cells = new List<bool> { true } };
            Assert.That(grid.IsEmpty(), Is.True);
        }

        [Test]
        public void IsEmpty_NoCells_ReturnsTrue()
        {
            var grid = new WalkabilityGrid { columns = 5, rows = 5, cells = new List<bool>() };
            Assert.That(grid.IsEmpty(), Is.True);
        }

        [Test]
        public void IsEmpty_PopulatedGrid_ReturnsFalse()
        {
            Assert.That(Make3X2Grid().IsEmpty(), Is.False);
        }

        [TestCase(0, 0, true)]
        [TestCase(1, 0, false)]
        [TestCase(2, 0, true)]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, true)]
        [TestCase(2, 1, false)]
        public void GetCell_ValidCoordinate_ReturnsStoredValue(int column, int row, bool expected)
        {
            Assert.That(Make3X2Grid().GetCell(column, row), Is.EqualTo(expected));
        }

        [TestCase(-1, 0)]
        [TestCase(3, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void GetCell_OutOfRangeCoordinate_ReturnsFalse(int column, int row)
        {
            Assert.That(Make3X2Grid().GetCell(column, row), Is.False);
        }

        [Test]
        public void GetTraversalCost_ValidCoordinate_ReturnsStoredValue()
        {
            // Index (column=1, row=1) -> row*columns+column = 1*3+1 = 4 -> value 5f
            Assert.That(Make3X2Grid().GetTraversalCost(1, 1), Is.EqualTo(5f));
        }

        [TestCase(-1, 0)]
        [TestCase(3, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void GetTraversalCost_OutOfRangeCoordinate_ReturnsInfinity(int column, int row)
        {
            Assert.That(Make3X2Grid().GetTraversalCost(column, row), Is.EqualTo(Mathf.Infinity));
        }

        [Test]
        public void CellToLocal_ReturnsCellCenterInLocalSpace()
        {
            var grid = new WalkabilityGrid { cellSize = 2f, originX = 10f, originY = -4f };
            Vector2 result = grid.CellToLocal(3, 1);
            // originX + (column+0.5)*cellSize, originY + (row+0.5)*cellSize
            Assert.That(result.x, Is.EqualTo(10f + 3.5f * 2f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(-4f + 1.5f * 2f).Within(0.0001f));
        }
    }
}
