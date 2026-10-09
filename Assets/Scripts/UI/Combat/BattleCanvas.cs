using UnityEngine;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Combat.UI
{
    public sealed class BattleCanvas : MonoBehaviour
    {
        // Battle UI root -> wires ~
        //  - BattleStage:  background, enemy + party slides, battle effects
        //  - CombatMessages:  every dialogue box the battle raises (intro, item prompt, run failure, outro)
        //  - CombatOptions / SkillSelectionUI:  the pre-combat menu and the in-combat skill wheel

        // Tunables
        [Header("Battle UI Parts")]
        [SerializeField] private BattleStage battleStage;
        [SerializeField] private CombatMessages combatMessages;
        [SerializeField] private CombatOptions combatOptions;
        [SerializeField] private SkillSelectionUI skillSelection;
        [SerializeField][Tooltip("Parent for combat options spawn (stats, knapsack)")] private Transform infoChooseParent;

        // State
        private BattleState lastBattleState = BattleState.Inactive;

        #region StaticFind
        private const string _battleCanvasTag = "BattleCanvas";

        public static BattleCanvas FindBattleCanvas()
        {
            var battleCanvasGameObject = GameObject.FindGameObjectWithTag(_battleCanvasTag);
            return battleCanvasGameObject != null ? battleCanvasGameObject.GetComponent<BattleCanvas>() : null;
        }
        #endregion

        #region UnityMethods
        private void Awake()
        {
            GameObject playerObject = Player.FindPlayerObject();
            PartyCombatConduit partyCombatConduit = playerObject != null ? playerObject.GetComponent<PartyCombatConduit>() : null;
            BattleController battleController = BattleController.FindBattleController();
            if (partyCombatConduit == null || battleController == null) { Destroy(gameObject); return; }

            battleStage.Setup(battleController);
            combatMessages.Setup(battleController, partyCombatConduit);
            skillSelection.SetupBattleController(battleController);
            combatOptions.Setup(battleController, partyCombatConduit, combatMessages, infoChooseParent);
            battleController.AddInputReceiver(combatOptions, null);
        }

        private void OnEnable()
        {
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleFadeTransitionEvent>.SubscribeToEvent(HandleBattleFadeTransitionEvent);
        }

        private void OnDisable()
        {
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleFadeTransitionEvent>.UnsubscribeFromEvent(HandleBattleFadeTransitionEvent);
        }
        #endregion

        #region PublicMethods
        public BattleStage GetBattleStage() => battleStage;
        #endregion

        #region PrivateMethods
        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            BattleState battleState = battleStateChangedEvent.battleState;
            if (battleState == lastBattleState) { return; } // Only act on state changes
            lastBattleState = battleState;

            switch (battleState)
            {
                case BattleState.PreCombat:
                    skillSelection.gameObject.SetActive(false);
                    combatOptions.EnableCombatOptions();
                    break;
                case BattleState.Combat:
                    combatOptions.gameObject.SetActive(false);
                    skillSelection.gameObject.SetActive(true);
                    break;
                case BattleState.Outro:
                case BattleState.Rewards:
                    combatOptions.gameObject.SetActive(false);
                    skillSelection.gameObject.SetActive(false);
                    break;
            }
        }

        private void HandleBattleFadeTransitionEvent(BattleFadeTransitionEvent battleFadeTransitionEvent)
        {
            if (battleFadeTransitionEvent.fadePhase != BattleFadePhase.ExitPeak) { return; }
            Destroy(gameObject);
        }
        #endregion
    }
}
