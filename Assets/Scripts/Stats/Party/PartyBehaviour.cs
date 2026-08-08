using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core;
using Frankie.Core.Predicates;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Utils;

namespace Frankie.Stats
{
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public abstract class PartyBehaviour : MonoBehaviour, IPredicateEvaluator
    {
        // Tunables
        [SerializeField][Range(1, 8)] protected int partyLimit = 4;
        [SerializeField] protected List<BaseStats> members = new();
        [SerializeField] protected Transform container;
        [SerializeField] protected int partyOffset = 16;
        
        // Static
        private const int _initialOffset = 0;
        private const string _errName = "???";

        // State
        private readonly Dictionary<BaseStats, Rigidbody2D> rigidBody2DLookup = new();
        protected readonly Dictionary<BaseStats, CharacterMoveLink> characterMoveLinkLookup = new();
        protected readonly Dictionary<BaseStats, CombatParticipant> combatParticipantLookup = new();
        private int lastMemberOffsetIndex = 0;

        // Cached References
        protected PlayerMover playerMover;
        protected PlayerStateMachine playerStateMachine;
        
        // Events
        private event Action<PartyAlteredData> membersAltered;
        
        #region StaticMethods
        public static bool IsPartyBehaviourType(PartyBehaviour partyBehaviour, PartyBehaviourType partyBehaviourType)
        {
            switch (partyBehaviour)
            {
                case Party when partyBehaviourType == PartyBehaviourType.Party:
                case PartyAssist when partyBehaviourType == PartyBehaviourType.PartyAssist:
                    return true;
                default:
                    return false;
            }
        }
        #endregion
        
        #region UnityMethods
        protected virtual void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            playerStateMachine = GetComponent<PlayerStateMachine>();
            RefreshLookups();
        }

        protected virtual void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyPositions;
            playerMover.playerMoved += UpdatePartyPositions;
            playerStateMachine.playerLayerChanged += HandlePlayerLayerChanged;
        }

        protected virtual void OnDisable()
        {
            playerMover.movementHistoryReset -= ResetPartyPositions;
            playerMover.playerMoved -= UpdatePartyPositions;
            playerStateMachine.playerLayerChanged -= HandlePlayerLayerChanged;
        }
        #endregion

        #region AbstractMethods
        protected abstract bool AddToParty(BaseStats characterBaseStats); // AddToParty -- Parent
        public abstract bool AddToParty(CharacterNPCSwapper characterNPCSwapper); // AddToParty -- Derivative:  Add from character NPC Swapper
        public abstract bool AddToParty(CharacterProperties characterProperties); // AddToParty -- Derivative:  Add from nothing
        public abstract bool RemoveFromParty(BaseStats character); // RemoveFromParty -- Parent:  Instantiate nothing
        public abstract bool RemoveFromParty(CharacterProperties characterProperties); // RemoveFromParty -- Derivative:  In case no knowledge if member in party
        public abstract bool RemoveFromParty(BaseStats character, Transform worldTransform); // RemoveFromParty -- Derivative:  Instantiate an NPC at the defined location
        #endregion
        
        #region ProtectedMethods
        protected virtual int GetInitialPartyOffset() => _initialOffset;
        protected virtual bool ShouldSkipFirstEntryOffset() => true;
        protected bool HasMember(BaseStats member) => HasMember(member.GetCharacterProperties());

        public void SubscribeToMembersAlteredUpdates(bool enable, Action<PartyAlteredData> onMembersAltered)
        {
            membersAltered -= onMembersAltered;
            if (enable) { membersAltered += onMembersAltered; }
        }

        protected void TriggerMembersAltered() => membersAltered?.Invoke(PackPartyAlteredData());
        protected abstract PartyAlteredData PackPartyAlteredData();
        
        protected void RefreshLookups()
        {
            rigidBody2DLookup.Clear();
            characterMoveLinkLookup.Clear();
            foreach (BaseStats character in members.Where(character => character != null))
            {
                if (character.TryGetComponent(out Rigidbody2D characterRigidBody)) { rigidBody2DLookup[character] = characterRigidBody; }
                if (character.TryGetComponent(out CharacterMoveLink characterMoveLink)) { characterMoveLinkLookup[character] = characterMoveLink; }
                if (character.TryGetComponent(out CombatParticipant combatParticipant)) { combatParticipantLookup[character] = combatParticipant; }
            }
        }
        
        protected void RefreshColliders(bool setLeaderToCollide = true)
        {
            int index = 0;
            foreach (Collider2D characterCollider2D in members.Select(partyCharacter => partyCharacter.GetComponent<Collider2D>()))
            {
                characterCollider2D.isTrigger = !(setLeaderToCollide && index == 0);
                index++;
            }
        }

        protected void UpdatePartySpeedAndOffsets(float speed, Vector2 pixelPerfectOffset)
        {
            int characterIndex = 0;
            foreach (BaseStats character in members)
            {
                if (characterIndex == 0) { characterIndex++; continue; }
                characterMoveLinkLookup[character].UpdateCharacterSpeed(speed);
                characterMoveLinkLookup[character].UpdateSpriteOffset(pixelPerfectOffset);
                characterIndex++;
            }
        }
        #endregion

        #region PublicMethods
        public bool IsPartyLeader(BaseStats checkMember) => TryGetPartyLeader(out BaseStats partyLeader) && partyLeader == checkMember;
        public bool TryGetPartyLeader(out BaseStats partyLeader)
        {
            partyLeader = members is { Count: > 0 } ? members[0] : null;
            return partyLeader != null;
        }
        public GameObject GetPartyLeaderObject() => TryGetPartyLeader(out BaseStats partyLeader) ? partyLeader.gameObject : null;

        public string GetPartyLeaderName() => TryGetPartyLeader(out BaseStats partyLeader) ? CharacterProperties.GetCharacterDisplayName(partyLeader) : _errName;
        
        public List<BaseStats> GetMembers() => members;
        public int GetPartySize() => members.Count;
        public int GetLastMemberOffsetIndex() => lastMemberOffsetIndex;
        public BaseStats GetMember(CharacterProperties matchCharacterProperties)
        {
            return members.FirstOrDefault(baseStats => CharacterProperties.AreCharacterPropertiesMatched(matchCharacterProperties, baseStats.GetCharacterProperties()));
        }
        
        public void TogglePartyVisible(bool enable)
        {
            foreach (BaseStats member in members)
            {
                if (!member.TryGetComponent(out CharacterMoveLink characterMoveLink)) { continue; }
                SpriteRenderer spriteRenderer = characterMoveLink.GetSpriteRenderer();
                if (spriteRenderer == null) { continue; }
                
                spriteRenderer.enabled = enable;
            }
        }
        #endregion
        
        #region PrivateMethods
        private bool HasMember(CharacterProperties matchCharacterProperties)
        {
            return members.Any(baseStats => CharacterProperties.AreCharacterPropertiesMatched(matchCharacterProperties, baseStats.GetCharacterProperties()));
        }
        
        private void UpdatePartyPositions(CircularBuffer<Tuple<Vector2, Vector2>> movementHistory)
        {
            int characterIndex = 0;
            int bufferIndex = 0;
            foreach (BaseStats character in members)
            {
                if (ShouldSkipFirstEntryOffset() && characterIndex == 0) { characterIndex++; continue; }

                Vector2 position;
                Vector2 lookDirection;
                bufferIndex = characterIndex * partyOffset + GetInitialPartyOffset();
                if (bufferIndex >= movementHistory.GetCurrentSize())
                {
                    position = movementHistory.GetLastEntry().Item1;
                    lookDirection = movementHistory.GetLastEntry().Item2;
                }
                else
                {
                    position = movementHistory.GetEntryAtPosition(bufferIndex).Item1;
                    lookDirection = movementHistory.GetEntryAtPosition(bufferIndex).Item2;
                }
                if (rigidBody2DLookup.TryGetValue(character, out Rigidbody2D characterRigidBody)) { characterRigidBody.MovePosition(position); }
                if (characterMoveLinkLookup.TryGetValue(character, out CharacterMoveLink characterMoveLink)) { characterMoveLink.UpdateCharacterLook(lookDirection); }

                characterIndex++;
            }
            lastMemberOffsetIndex = bufferIndex;
        }
        
        private void ResetPartyPositions(Vector2 newPosition)
        {
            foreach (BaseStats character in members)
            {
                character.transform.position = newPosition;
            }
        }
        
        private void HandlePlayerLayerChanged(int playerLayer, int probeLayer, bool isPlayerImmune)
        {
            foreach (BaseStats character in members.Where(character => character != null))
            {
                if (!characterMoveLinkLookup.TryGetValue(character, out CharacterMoveLink characterMoveLink)) { continue; }

                // Note:  Must use the CharacterMoveLink functions to change layer (can be extremely costly otherwise)
                characterMoveLink.SetCharacterLayer(playerLayer);
                characterMoveLink.SetInteractionProbeLayer(probeLayer);
                characterMoveLink.SetIsFlashing(isPlayerImmune);
            }
        }
        #endregion
        
        #region PredicateInterface
        public bool? Evaluate(Predicate predicate)
        {
            var predicateParty = predicate as PredicateParty;
            return predicateParty != null ? predicateParty.Evaluate(this) : null;
        }
        #endregion
    }
}
