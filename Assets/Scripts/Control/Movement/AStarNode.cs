namespace Frankie.Control
{
    public class AStarNode
    {
        public int column;
        public int row;
        public float gridCost;
        public float heuristicCost;
        public AStarNode parent;

        public float GetFinalCost() => gridCost + heuristicCost;

        public void Initialize(int setColumn, int setRow, float setGridCost, float setHeuristicCost, AStarNode setParent)
        {
            column = setColumn;
            row = setRow;
            gridCost = setGridCost;
            heuristicCost = setHeuristicCost;
            parent = setParent;
        }
    }
}
