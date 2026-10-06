using System;
using System.Collections.Generic;

namespace Frankie.Combat.UI
{
    public interface ICharacterSlide
    {
        BattleEntity GetBattleEntity();
        void HighlightSlide(BattleEntitySelectionType selectionType, IEnumerable<BattleEntity> battleEntities);
        void HighlightSlide(BattleEntitySelectionType selectionType, bool enable);
        void AddButtonClickEvent(Action action);
        void RemoveButtonClickEvents();
    }
}
