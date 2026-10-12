using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    public abstract class EnemySlideRow : VisualElement
    {
        // Note:  One typed row per BattleRow, so slide handles place themselves by container type

        // Const Tunables
        private const string _ussClassName = "enemy-slide-row";

        protected EnemySlideRow()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
        }

        // Note:  UI Toolkit raises no child-added event - slides call this once attached
        public void SortByColumn() => Sort((first, second) => GetColumn(first).CompareTo(GetColumn(second)));

        private static int GetColumn(VisualElement slide) => slide is EnemySlideElement enemySlideElement ? enemySlideElement.column : int.MaxValue;
    }
}
