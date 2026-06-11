namespace Frankie.Control
{
    public class AStarNode
    {
        public int column;
        public int row;
        public float goalCost;
        public float heuristicCost;
        public AStarNode parent;

        public float GetFinalCost() => goalCost + heuristicCost;

        public void Initialize(int setColumn, int setRow, float setGoalCost, float setHeuristicCost, AStarNode setParent)
        {
            column = setColumn;
            row = setRow;
            goalCost = setGoalCost;
            heuristicCost = setHeuristicCost;
            parent = setParent;
        }
    }
}
