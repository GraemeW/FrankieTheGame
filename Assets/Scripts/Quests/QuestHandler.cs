using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using Frankie.Core;
using Frankie.Core.GameStateModifiers;

namespace Frankie.Quests
{
    [ExecuteInEditMode] 
    public class QuestHandler : MonoBehaviour, IGameStateModifierHandler
    {
        // Interface Properties
        [SerializeField] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }

        [SerializeField][HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 

        // Tunables
        [SerializeField][Tooltip("Configure for Give Quest")] private Quest quest;
        [SerializeField][Tooltip("Configure for Complete Objective")] protected QuestObjective questObjective;
        
        // Cached References
        private ReInitLazyValue<QuestList> questList;

        #region StaticMethods

        private static QuestList SetupQuestList()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject == null ? null : playerObject.GetComponent<QuestList>();
        }
        #endregion
        
        #region UnityMethods
        private void Awake()
        {
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
        }

        private void Start()
        {
            questList ??= new ReInitLazyValue<QuestList>(SetupQuestList);
            questList.ForceInit();
        }

        private void OnDestroy()
        {
            IGameStateModifierHandler.TriggerOnDestroy(this);
        }
        private void OnDrawGizmos()
        {
            IGameStateModifierHandler.TriggerOnGizmos(this);
        }
        #endregion

        #region PublicMethods
        public void GiveConfiguredQuest()
        {
            if (quest == null) { return; }
            questList.value.TryAddQuest(quest);
        }
        
        public void CompleteConfiguredObjective()
        {
            if (questObjective == null) { return; }
            questList.value.CompleteObjective(questObjective);
        }
        #endregion
        
        #region InterfaceMethods
        public IList<GameStateModifier> GetGameStateModifiers()
        {
            List<GameStateModifier> gameStateModifiers = new();
            if (quest != null)
            {
                gameStateModifiers.Add(quest);
            }

            if (questObjective != null)
            {
                Quest questObjectiveQuest = Quest.GetFromID(questObjective.GetQuestID());
                if (questObjectiveQuest != null && questObjectiveQuest != quest)
                {
                    gameStateModifiers.Add(questObjectiveQuest);
                }
            }

            return gameStateModifiers;
        }
        #endregion
    }
}
