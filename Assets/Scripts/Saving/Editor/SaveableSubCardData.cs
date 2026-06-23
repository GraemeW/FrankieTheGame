using UnityEngine.UIElements;
using Frankie.Core;
using Frankie.Core.Predicates;
using Frankie.Control;
using Frankie.Sound;
using Frankie.Stats;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.World;
using Frankie.ZoneManagement;

namespace Frankie.Saving.Editor
{
    public abstract class SaveableSubCardData
    {
        protected SaveableEntityCardData saveableEntityCardData { get; private set; }
        public ISaveableBase saveable { get; protected set; }
        public SaveState saveState { get; protected set; }

        public static SaveableSubCardData CreateTypeSpecificSubCard(ISaveableBase saveable, SaveState saveState)
        {
            return saveable switch
            {
                Mover => new MoverSaveableSubCard(saveable, saveState),
                PlayerColliderTrigger => new SimpleBoolSaveableSubCard(saveable, saveState),
                BaseStats => new BaseStatsSubCard(saveable, saveState),
                Experience => new SimpleFloatSaveableSubCard(saveable, saveState),
                CombatParticipant => new CombatParticipantSaveableSubCard(saveable, saveState),
                Party => new PartySubCard(saveable, saveState),
                Knapsack => new KnapsackSaveableSubCard(saveable, saveState),
                Equipment => new EquipmentSaveableSubCard(saveable, saveState),
                WearablesLink => new WearablesLinkSubCard(saveable, saveState),
                Wallet => new WalletSaveableSubCard(saveable, saveState),
                QuestList => new QuestListSubCard(saveable, saveState),
                CheckBase => new SimpleBoolSaveableSubCard(saveable, saveState),
                PredicateChildToggler => new SimpleBoolSaveableSubCard(saveable, saveState),
                BackgroundMusicOverride => new SimpleBoolSaveableSubCard(saveable, saveState),
                CinematicTrigger => new SimpleBoolSaveableSubCard(saveable, saveState),
                FlickerOverlay => new SimpleBoolSaveableSubCard(saveable, saveState),
                WorldSpriteChanger => new SimpleBoolSaveableSubCard(saveable, saveState),
                WorldCashGiverTaker => new SimpleIntSaveableSubCard(saveable, saveState),
                WorldItemGiverTaker => new SimpleIntSaveableSubCard(saveable, saveState),
                Room => new SimpleBoolSaveableSubCard(saveable, saveState),
                _ => new GenericSaveableSubCard(saveable, saveState)
            };
        }
        
        public abstract void AddEditableFieldsToSubCardView(Box subCardView);
        
        public void SetSaveableEntityCardData(SaveableEntityCardData setSaveableEntityCardData)
        {
            saveableEntityCardData = setSaveableEntityCardData;
        }
    }
}
