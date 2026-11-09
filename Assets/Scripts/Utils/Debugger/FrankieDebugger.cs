using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Core;
using LowDefMustard.Zones;
using LowDefMustard.Utils;
using LowDefMustard.Saving;
using Frankie.Core;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Quests;
using Frankie.Stats;
using Frankie.Inventory;
using Frankie.Saving;
using Frankie.Zones;

namespace Frankie.Utils
{
    public partial class FrankieDebugger : MonoBehaviour
    {
        // Fixed Tunables
        [SerializeField] private int fundsToAddToWallet = 100;
        [SerializeField] private bool resetSaveOnStart = false;
        [SerializeField] private bool isCinematicAutoplayDisabled = false;
        [SerializeField] private bool shouldEnableAllRooms = false;
        [SerializeField] private bool isDemo = false;
        [SerializeField] private Zone demoZoneOverride;
        
        // Play Mode Debug Tunables
        [SerializeField] private List<CharacterProperties> debugEnemies = new();
        [SerializeField][RestrictedEnum((int)TransitionType.None, (int)TransitionType.Zone, (int)TransitionType.BattleComplete)] private TransitionType debugCombatTransitionType = TransitionType.BattleNeutral;
        [SerializeField] private Zone debugZone;
        [SerializeField] private string debugZoneNodeID;
        [SerializeField][ModifiableStat] private Stat debugStatusEffectStat = Stat.Brawn;
        [SerializeField] private float debugStatusEffectAmount = 1f;
        [SerializeField] private float debugStatusEffectDuration = 100f;

        // Editor Field References
        internal const string debugEnemiesField = nameof(debugEnemies);
        internal const string debugCombatTransitionTypeField = nameof(debugCombatTransitionType);
        internal const string debugZoneField = nameof(debugZone);
        internal const string debugZoneNodeIDField = nameof(debugZoneNodeID);
        internal const string debugStatusEffectStatField = nameof(debugStatusEffectStat);
        internal const string debugStatusEffectAmountField = nameof(debugStatusEffectAmount);
        internal const string debugStatusEffectDurationField = nameof(debugStatusEffectDuration);

        // State
        private readonly List<GameObject> debugSpawnedEnemies = new();

        // Cached References
        private PlayerInput playerInput;

        // Lazy Values
        private ReInitLazyValue<QuestList> questList;
        private ReInitLazyValue<Party> party;
        private ReInitLazyValue<Wallet> wallet;
        
        // Static/Const
        private const string _debuggerTag = "Debugger";
        
        // Static State
        [AutoStaticsCleanup] private static FrankieDebugger _frankieDebugger;

        #region StaticMethods
        public static bool IsCinematicAutoplayDisabled()
        {
            FrankieDebugger frankieDebugger = FindDebugger();
            return frankieDebugger != null && frankieDebugger.isCinematicAutoplayDisabled;
        }

        public static bool ShouldEnableAllRooms()
        {
            FrankieDebugger frankieDebugger = FindDebugger();
            return frankieDebugger != null && frankieDebugger.shouldEnableAllRooms;
        }

        public static bool IsDemo()
        {
            FrankieDebugger frankieDebugger = FindDebugger();
            return frankieDebugger != null && frankieDebugger.isDemo;
        }
        
        private static FrankieDebugger FindDebugger()
        {
            if (_frankieDebugger != null) { return _frankieDebugger; }
            
            var debuggerGameObject = GameObject.FindGameObjectWithTag(_debuggerTag);
            _frankieDebugger = debuggerGameObject != null ? debuggerGameObject.GetComponent<FrankieDebugger>() : null;
            return _frankieDebugger;
        }
        
        private static QuestList SetupQuestList()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject !=null ? playerObject.GetComponent<QuestList>() : null;
        }
        private static Party SetupParty()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject !=null ? playerObject.GetComponent<Party>() : null;
        }

        private static Wallet SetupWallet()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject !=null ? playerObject.GetComponent<Wallet>() : null;
        }
        
        private static Zone GetDemoZoneOverride()
        {
            FrankieDebugger frankieDebugger = FindDebugger();
            if (!frankieDebugger.isDemo) { return null; }
            return frankieDebugger != null ? frankieDebugger.demoZoneOverride : null;
        }
        #endregion
        
        #region UnityMethods
        private void Awake()
        {
            // References
            playerInput = new PlayerInput();
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
            party = new ReInitLazyValue<Party>(SetupParty);
            wallet = new ReInitLazyValue<Wallet>(SetupWallet);

            // Debug Hook-Ups
            playerInput.Admin.Save.performed += _ => Save();
            playerInput.Admin.Load.performed += _ => Continue();
            playerInput.Admin.Delete.performed += _ => Delete();
            playerInput.Admin.NewGame.performed += _ => NewSave();
            playerInput.Admin.ClearPlayerPrefs.performed += _ => ClearPlayerPrefs();
            playerInput.Admin.WinBattle.performed += _ => AutoWinBattle();
            playerInput.Admin.QuestLog.performed += _ => PrintQuests();
            playerInput.Admin.LevelUpParty.performed += _ => LevelUpParty();
            playerInput.Admin.AddFundsToWallet.performed += _ => AddFundsToWallet();
        }

        private void Start()
        {
            questList ??= new ReInitLazyValue<QuestList>(SetupQuestList);
            party ??= new ReInitLazyValue<Party>(SetupParty);
            wallet ??= new ReInitLazyValue<Wallet>(SetupWallet);
            
            questList.ForceInit();
            party.ForceInit();
            wallet.ForceInit();

            if (resetSaveOnStart) { NewSave(); }
        }

        private void OnEnable()
        {
            _frankieDebugger = this;
            SceneLoaderBase.demoZoneOverrideProvider = GetDemoZoneOverride;
            
            playerInput.Admin.Enable();
            SceneManager.sceneLoaded += ResetReferences;
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(HandleBattleStagingEvent);
        }

        private void OnDisable()
        {
            if (SceneLoaderBase.demoZoneOverrideProvider == GetDemoZoneOverride) { SceneLoaderBase.demoZoneOverrideProvider = null; }
            
            playerInput.Admin.Disable();
            SceneManager.sceneLoaded -= ResetReferences;
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleFadeTransitionEvent>.UnsubscribeFromEvent(HandleBattleFadeTransitionEvent);
        }
        #endregion

        #region PlayModeDebug
        public static PlayerStateType? GetPlayerStateType()
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            return playerStateMachine != null ? playerStateMachine.GetPlayerStateType() : null;
        }

        public void DebugEnterCombat()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            if (playerStateMachine.GetPlayerStateType() == PlayerStateType.InTransition) { LDMLog.Warning(LDMLogCategory.Combat, "Debugger: the player is in transition (a combat request would be dropped). Cannot enter combat.", this); return; }
            if (debugCombatTransitionType is not (TransitionType.BattleGood or TransitionType.BattleBad or TransitionType.BattleNeutral))
            {
                LDMLog.Warning(LDMLogCategory.Combat, $"Debugger: '{debugCombatTransitionType}' is not a battle transition. Cannot enter combat.", this);
                return;
            }

            Vector3 spawnPosition = playerStateMachine.transform.position;
            var enemies = new List<CombatParticipant>();
            foreach (CharacterProperties characterProperties in debugEnemies)
            {
                if (TrySpawnDebugEnemy(characterProperties, spawnPosition, out CombatParticipant enemy)) { enemies.Add(enemy); }
            }
            if (enemies.Count == 0) { LDMLog.Warning(LDMLogCategory.Combat, "Debugger: no viable enemies to spawn. Cannot enter combat.", this); return; }

            LDMLog.Info(LDMLogCategory.Combat, $"Debugger: entering combat with {enemies.Count} enemies from player state '{playerStateMachine.GetPlayerStateType()}'.", this);
            playerStateMachine.EnterCombat(enemies, debugCombatTransitionType);
        }

        public void DebugEnterZone()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            if (!TryGetDebugZoneNode(out ZoneNode zoneNode)) { return; }

            if (debugZone == SceneLoaderBase.GetCurrentZone())
            {
                // Same scene:  plain warp, no transition
                if (playerStateMachine.GetPlayerStateType() != PlayerStateType.InWorld) { LDMLog.Warning(LDMLogCategory.Zones, "Debugger: a same-zone warp needs the player in World.", this); return; }
                WarpPlayerToZoneNode(zoneNode.GetNodeID());
                return;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (quirk of SceneReference)
            if (debugZone.GetSceneReference() == null || string.IsNullOrEmpty(debugZone.GetSceneReference().SceneName))
            {
                LDMLog.Warning(LDMLogCategory.Zones, $"Debugger: zone '{debugZone.name}' has no scene. Cannot enter it.", this);
                return;
            }

            string zoneNodeID = zoneNode.GetNodeID();
            var faderEventTriggers = new FaderEventTriggers<TransitionType>(null, null, () => CompleteDebugZoneTransition(zoneNodeID), null);
            if (!Fader.StartSceneLoadFade(debugZone, faderEventTriggers))
            {
                LDMLog.Warning(LDMLogCategory.Zones, "Debugger: the fader is busy (or missing). Cannot enter the zone.", this);
                return;
            }

            LDMLog.Info(LDMLogCategory.Zones, $"Debugger: entering zone '{debugZone.name}' at node '{zoneNodeID}' from player state '{playerStateMachine.GetPlayerStateType()}'.", this);
            playerStateMachine.EnterZoneTransition();
        }

        public void DebugEnterCutscene()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            playerStateMachine.EnterCutscene();
        }

        public void DebugEnterWorld()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            playerStateMachine.EnterWorld();
        }

        public void DebugApplyStatusEffect()
        {
            if (Array.IndexOf(BaseStats.GetNonModifyingStats(), debugStatusEffectStat) >= 0)
            {
                LDMLog.Warning(LDMLogCategory.Stats, $"Debugger: '{debugStatusEffectStat}' cannot be modified. Cannot apply the status effect.", this);
                return;
            }
            if (!party.TryGetSafely(out Party partyInstance)) { LDMLog.Warning(LDMLogCategory.Stats, "Debugger: no party found. Cannot apply the status effect.", this); return; }

            string effectGUID = Guid.NewGuid().ToString();
            foreach (BaseStats member in partyInstance.GetMembers())
            {
                if (member == null || !member.TryGetComponent(out CombatParticipant combatParticipant)) { continue; }

                var persistentStatus = combatParticipant.gameObject.AddComponent<PersistentStatModifierStatus>();
                persistentStatus.Setup(effectGUID, debugStatusEffectDuration, debugStatusEffectStat, debugStatusEffectAmount, true);
                combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, persistentStatus);
            }
            LDMLog.Info(LDMLogCategory.Stats, $"Debugger: applied {debugStatusEffectAmount} '{debugStatusEffectStat}' to the party for {debugStatusEffectDuration} seconds.", this);
        }

        public void DebugKnockOutParty()
        {
            if (!party.TryGetSafely(out Party partyInstance)) { LDMLog.Warning(LDMLogCategory.Stats, "Debugger: no party found. Cannot knock it out.", this); return; }

            foreach (BaseStats member in partyInstance.GetMembers())
            {
                if (member == null || !member.TryGetComponent(out CombatParticipant combatParticipant)) { continue; }
                combatParticipant.SelfImplode(true);
            }
            LDMLog.Info(LDMLogCategory.Stats, "Debugger: knocked out the party.", this);
        }
        #endregion

        #region SavingWrapperDebug
        private void Save()
        {
            LDMLog.Info(LDMLogCategory.Saving, "Debugger: saving the game.");
            SaveFileManager.Save();
        }

        private void Continue()
        {
            LDMLog.Info(LDMLogCategory.Saving, "Debugger: loading the game.");
            SaveFileManager.Continue();
        }

        private void Delete()
        {
            LDMLog.Info(LDMLogCategory.Saving, "Debugger: deleting the session.");
            SaveFileManager.DeleteSession();
            SaveFileManager.Delete();
        }

        private void NewSave()
        {
            Save();
            Delete();
            Save();
            Continue();
        }

        private void ClearPlayerPrefs()
        {
            LDMLog.Info(LDMLogCategory.Saving, "Debugger: clearing player prefs.");
            PlayerPrefsController.ClearPlayerPrefs();
        }
        #endregion

        #region CombatDebug

        private void AutoWinBattle()
        {
            BattleController battleController = BattleController.FindBattleController();
            if (battleController == null) { return; }
            battleController.DefeatAllEnemies();
        }
        #endregion
        
        #region QuestListDebug
        private void ResetReferences(Scene scene, LoadSceneMode loadSceneMode)
        {
            // Since Debugger is a persistent object, force reset on scene load
            questList.ForceInit();
            wallet.ForceInit();
            party.ForceInit();
        }

        private void PrintQuests()
        {
            if (!questList.TryGetSafely(out QuestList questListInstance)) { LDMLog.Warning(LDMLogCategory.Quests, "Debugger: no quest list found. Cannot print quests.", this); return; }
            
            LDMLog.Info(LDMLogCategory.Quests, "Debugger: active quests:");
            foreach (QuestStatus questStatus in questListInstance.GetActiveQuests())
            {
                Quest quest = questStatus.GetQuest();
                LDMLog.Info(LDMLogCategory.Quests, $"Quest '{quest.GetName()}': {quest.GetDetail()} ({questStatus.GetCompletedObjectiveCount()} of {quest.GetObjectiveCount()} objectives complete).", quest);
            }
        }
        #endregion

        #region PartyDebug
        private void LevelUpParty()
        {
            if (!party.TryGetSafely(out Party partyInstance)) { LDMLog.Warning(LDMLogCategory.Stats, "Debugger: no party found. Cannot level up.", this); return; }
            
            foreach (BaseStats character in partyInstance.GetMembers())
            {
                LDMLog.Info(LDMLogCategory.Stats, $"Debugger: levelling up '{CharacterProperties.GetCharacterDisplayName(character)}'.");
                character.IncrementLevel();
            }
        }
        #endregion

        #region WalletDebug
        private void AddFundsToWallet()
        {
            if (!wallet.TryGetSafely(out Wallet walletInstance)) { LDMLog.Warning(LDMLogCategory.Inventory, "Debugger: no wallet found. Cannot add funds.", this); return; }
            
            LDMLog.Info(LDMLogCategory.Inventory, $"Debugger: adding ${fundsToAddToWallet} to the wallet.");
            walletInstance.UpdateCash(fundsToAddToWallet);
        }
        #endregion
        
        #region PlayModeHelperMethods
        private bool TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)
        {
            playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine != null) { return true; }

            LDMLog.Warning(LDMLogCategory.Core, "Debugger: no player state machine found.", this);
            return false;
        }

        private bool TrySpawnDebugEnemy(CharacterProperties characterProperties, Vector3 spawnPosition, out CombatParticipant enemy)
        {
            enemy = null;
            if (characterProperties == null) { return false; }

            GameObject enemyPrefab = characterProperties.GetCharacterNPCPrefab();
            if (enemyPrefab == null || !enemyPrefab.TryGetComponent(out CombatParticipant _) || !enemyPrefab.TryGetComponent(out SkillHandler _) || !enemyPrefab.TryGetComponent(out BattleAI _))
            {
                LDMLog.Warning(LDMLogCategory.Combat, $"Debugger: character '{characterProperties.name}' has no NPC prefab with a CombatParticipant, SkillHandler and BattleAI. Skipping it.", characterProperties);
                return false;
            }

            GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            PrepareDebugEnemy(spawnedEnemy);
            debugSpawnedEnemies.Add(spawnedEnemy);
            BattleEventBus<BattleFadeTransitionEvent>.SubscribeToEvent(HandleBattleFadeTransitionEvent); // Within-battle ~ cleared at BattleTornDown
            enemy = spawnedEnemy.GetComponent<CombatParticipant>();
            return true;
        }

        private static void PrepareDebugEnemy(GameObject spawnedEnemy)
        {
            // Battle-only enemy:  inert in world, never saved
            //  - note:  deliberately left visible ~ avoid NPCStateHandler inadvertently destroying the out-of-view NPC
            foreach (Collider2D enemyCollider in spawnedEnemy.GetComponentsInChildren<Collider2D>()) { enemyCollider.enabled = false; }
            foreach (SaveableEntity saveableEntity in spawnedEnemy.GetComponentsInChildren<SaveableEntity>(true)) { saveableEntity.ForcePreventSave(); }
            if (spawnedEnemy.TryGetComponent(out NPCChaser npcChaser)) { npcChaser.SetChaseDisposition(false); }
            if (spawnedEnemy.TryGetComponent(out NPCStateHandler npcStateHandler)) { npcStateHandler.ForceNPCOccupied(); }
        }

        private void DespawnDebugEnemies()
        {
            foreach (GameObject spawnedEnemy in debugSpawnedEnemies)
            {
                if (spawnedEnemy != null) { Destroy(spawnedEnemy); }
            }
            debugSpawnedEnemies.Clear();
        }

        private void HandleBattleFadeTransitionEvent(BattleFadeTransitionEvent battleFadeTransitionEvent)
        {
            if (battleFadeTransitionEvent.fadePhase == BattleFadePhase.ExitPeak) { DespawnDebugEnemies(); }
        }

        private void HandleBattleStagingEvent(BattleStagingEvent battleStagingEvent)
        {
            if (battleStagingEvent.battleStagingType == BattleStagingType.BattleTornDown) { DespawnDebugEnemies(); }
        }

        private bool TryGetDebugZoneNode(out ZoneNode zoneNode)
        {
            zoneNode = null;
            if (debugZone == null) { LDMLog.Warning(LDMLogCategory.Zones, "Debugger: no zone set. Cannot enter a zone.", this); return false; }

            zoneNode = debugZone.GetNodeFromID(debugZoneNodeID);
            if (zoneNode != null) { return true; }

            LDMLog.Warning(LDMLogCategory.Zones, $"Debugger: zone '{debugZone.name}' has no node '{debugZoneNodeID}'. Cannot enter it.", this);
            return false;
        }

        private void CompleteDebugZoneTransition(string zoneNodeID)
        {
            // Scene loaded, fade not yet out ~ mirrors ZoneHandler's queued move
            WarpPlayerToZoneNode(zoneNodeID);

            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }
            playerStateMachine.SetZoneTransitionStatus(true);
            playerStateMachine.EnterWorld();
        }
        
        private void WarpPlayerToZoneNode(string zoneNodeID)
        {
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject == null || !playerObject.TryGetComponent(out PlayerController playerController)) { LDMLog.Warning(LDMLogCategory.Zones, "Debugger: no player controller found. Cannot warp.", this); return; }

            ZoneHandler zoneHandler = ZoneHandler.FindZoneHandler(zoneNodeID);
            if (zoneHandler == null) { LDMLog.Warning(LDMLogCategory.Zones, $"Debugger: no zone handler in the scene for node '{zoneNodeID}'. Leaving the player where they are.", this); return; }
            zoneHandler.ReceiveExternallyWarpedPlayer(playerController);
        }
        #endregion
    }
}
