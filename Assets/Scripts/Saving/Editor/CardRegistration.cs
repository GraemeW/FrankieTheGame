using UnityEditor;
using LowDefMustard.Saving.Editor;
using Frankie.Core;
using Frankie.Core.Predicates;
using Frankie.Core.GameStateModifiers;
using Frankie.Control;
using Frankie.Sound;
using Frankie.Stats;
using Frankie.Combat;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.Rendering;
using Frankie.Speech;
using Frankie.World;
using Frankie.ZoneManagement;

namespace Frankie.Saving.Editor
{
    [InitializeOnLoad]
    public static class CardRegistration
    {
        static CardRegistration()
        {
            RegisterSubCards();
            RegisterSubCardPriorities();
            RegisterEntityCardPriorities();
        }

        private static void RegisterSubCards()
        {
            SaveableSubCardData.RegisterSubCard<PlayerMover>((saveable, saveState) => new MoverSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<NPCMover>((saveable, saveState) => new MoverSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<PlayerColliderTrigger>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<BaseStats>((saveable, saveState) => new BaseStatsSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<Experience>((saveable, saveState) => new SimpleFloatSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<CombatParticipant>((saveable, saveState) => new CombatParticipantSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<Party>((saveable, saveState) => new PartySubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<PartyAssist>((saveable, saveState) => new PartyAssistSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<InactiveParty>((saveable, saveState) => new InactivePartySubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<Knapsack>((saveable, saveState) => new KnapsackSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<Equipment>((saveable, saveState) => new EquipmentSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<Wallet>((saveable, saveState) => new WalletSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<QuestList>((saveable, saveState) => new QuestListSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<AIConversant>((saveable, saveState) => new SimpleIntSaveableSubCard(saveable, saveState)); // Child of CheckBase -> above in switch
            SaveableSubCardData.RegisterSubCard<CheckBase>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<PredicateChildToggler>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<CinematicTrigger>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<BackgroundMusicOverride>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<FlickerOverlay>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<WorldSpriteChanger>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<WorldCashGiverTaker>((saveable, saveState) => new SimpleIntSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<WorldItemGiverTaker>((saveable, saveState) => new SimpleIntSaveableSubCard(saveable, saveState));
            SaveableSubCardData.RegisterSubCard<Room>((saveable, saveState) => new SimpleBoolSaveableSubCard(saveable, saveState));
        }

        private static void RegisterSubCardPriorities()
        {
            int order = 0;
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is PlayerMover, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is NPCMover, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is QuestList, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is Wallet, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is Party, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is PartyAssist, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is InactiveParty, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is CombatParticipant, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is Experience, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is BaseStats, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is Equipment, order++);
            SaveableSubCardData.RegisterSubCardPriority(saveable => saveable is Knapsack, order++);
        }

        private static void RegisterEntityCardPriorities()
        {
            int sortOrder = 0;
            SaveableEntityCardData.RegisterEntityCardPriority(entity => entity.gameObject.TryGetComponent(out CinematicTrigger _), sortOrder++);
            SaveableEntityCardData.RegisterEntityCardPriority(entity => entity.gameObject.TryGetComponent(out Player _), sortOrder++);
            SaveableEntityCardData.RegisterEntityCardPriority(entity => entity.gameObject.TryGetComponent(out IGameStateModifierHandler handler) && handler.hasGameStateModifiers, sortOrder++);
            SaveableEntityCardData.RegisterEntityCardPriority(entity => entity.gameObject.TryGetComponent(out BaseStats _), sortOrder++);
        }
    }
}
