using NUnit.Framework;

namespace LowDefMustard.Control.Tests.Editor
{
    public class AStarNodeTests
    {
        [Test]
        public void Initialize_SetsAllFields()
        {
            var parent = new AStarNode();
            var node = new AStarNode();
            node.Initialize(3, 5, 1.5f, 2.5f, parent);

            Assert.That(node.column, Is.EqualTo(3));
            Assert.That(node.row, Is.EqualTo(5));
            Assert.That(node.gridCost, Is.EqualTo(1.5f));
            Assert.That(node.heuristicCost, Is.EqualTo(2.5f));
            Assert.That(node.parent, Is.SameAs(parent));
        }

        [Test]
        public void Initialize_NullParent_IsAccepted()
        {
            var node = new AStarNode();
            node.Initialize(0, 0, 0f, 0f, null);
            Assert.That(node.parent, Is.Null);
        }

        [Test]
        public void GetFinalCost_SumsGridAndHeuristicCost()
        {
            var node = new AStarNode();
            node.Initialize(0, 0, 4f, 6f, null);
            Assert.That(node.GetFinalCost(), Is.EqualTo(10f));
        }

        [Test]
        public void Initialize_CalledAgain_OverwritesPreviousValues()
        {
            // Mirrors the pooling pattern in PathFinder.RentNode (the same instance is reused across searches)
            var node = new AStarNode();
            node.Initialize(1, 1, 1f, 1f, null);
            var newParent = new AStarNode();
            node.Initialize(9, 9, 9f, 9f, newParent);

            Assert.That(node.column, Is.EqualTo(9));
            Assert.That(node.row, Is.EqualTo(9));
            Assert.That(node.gridCost, Is.EqualTo(9f));
            Assert.That(node.heuristicCost, Is.EqualTo(9f));
            Assert.That(node.parent, Is.SameAs(newParent));
        }
    }
}
