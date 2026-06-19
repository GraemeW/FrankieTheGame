using UnityEngine.UIElements;
using Frankie.Core;
using Frankie.Combat;
using Frankie.Control;
using Frankie.Core.Predicates;
using Frankie.Inventory;

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
                CombatParticipant => new CombatParticipantSaveableSubCard(saveable, saveState),
                Mover => new MoverSaveableSubCard(saveable, saveState),
                PlayerColliderTrigger => new SimpleBoolSaveableSubCard(saveable, saveState),
                Knapsack => new KnapsackSaveableSubCard(saveable, saveState),
                Equipment => new EquipmentSaveableSubCard(saveable, saveState),
                WearablesLink => new WearablesLinkSubCard(saveable, saveState),
                Wallet => new WalletSaveableSubCard(saveable, saveState),
                CheckBase => new SimpleBoolSaveableSubCard(saveable, saveState),
                PredicateChildToggler => new SimpleBoolSaveableSubCard(saveable, saveState),
                CinematicTrigger => new SimpleBoolSaveableSubCard(saveable, saveState),
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
