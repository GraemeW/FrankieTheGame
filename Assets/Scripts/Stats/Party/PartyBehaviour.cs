using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Control;
using Frankie.Utils;

namespace Frankie.Stats
{
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public abstract class PartyBehaviour : MonoBehaviour
    {
        // Tunables
        [SerializeField][Range(1, 8)] protected int partyLimit = 4;
        [SerializeField] protected List<BaseStats> members = new();
        [SerializeField] protected Transform container;
        [SerializeField] protected int partyOffset = 16;
        
        // Static
        private const int _initialOffset = 0;

        // State
        private readonly Dictionary<BaseStats, Rigidbody2D> rigidBody2DLookup = new();
        protected readonly Dictionary<BaseStats, CharacterMoveLink> characterSpriteLinkLookup = new();
        private int lastMemberOffsetIndex = 0;

        // Cached References
        protected PlayerMover playerMover;
        private PlayerStateMachine playerStateMachine;
        
        // Events
        public event Action<PartyAlteredData> membersAltered;
        
        #region UnityMethods
        protected virtual void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            playerStateMachine = GetComponent<PlayerStateMachine>();
            RefreshLookups();
        }

        protected virtual void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyOffsets;
            playerMover.playerMoved += UpdatePartyPositions;
            playerStateMachine.playerLayerChanged += HandlePlayerLayerChanged;
        }

        protected virtual void OnDisable()
        {
            playerMover.movementHistoryReset -= ResetPartyOffsets;
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
        protected virtual PartyAlteredData PackPartyAlteredData() => new(members);
        
        protected void RefreshLookups()
        {
            rigidBody2DLookup.Clear();
            characterSpriteLinkLookup.Clear();
            foreach (BaseStats character in members.Where(character => character != null))
            {
                if (character.TryGetComponent(out Rigidbody2D characterRigidBody)) { rigidBody2DLookup[character] = characterRigidBody; }
                if (character.TryGetComponent(out CharacterMoveLink characterSpriteLink)) { characterSpriteLinkLookup[character] = characterSpriteLink; }
            }
        }

        protected void UpdatePartySpeedAndOffsets(float speed, Vector2 pixelPerfectOffset)
        {
            int characterIndex = 0;
            foreach (BaseStats character in members)
            {
                if (characterIndex == 0) { characterIndex++; continue; }
                characterSpriteLinkLookup[character].UpdateCharacterSpeed(speed);
                characterSpriteLinkLookup[character].UpdateSpriteOffset(pixelPerfectOffset);
                characterIndex++;
            }
        }
        #endregion

        #region PublicMethods
        public List<BaseStats> GetMembers() => members;
        public int GetLastMemberOffsetIndex() => lastMemberOffsetIndex;

        public BaseStats GetMember(CharacterProperties matchCharacterProperties)
        {
            return members.FirstOrDefault(baseStats => CharacterProperties.AreCharacterPropertiesMatched(matchCharacterProperties, baseStats.GetCharacterProperties()));
        }
        
        public void TogglePartyVisible(bool enable)
        {
            foreach (BaseStats member in members)
            {
                if (!member.TryGetComponent(out CharacterMoveLink characterSpriteLink)) { continue; }
                SpriteRenderer spriteRenderer = characterSpriteLink.GetSpriteRenderer();
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
                if (characterSpriteLinkLookup.TryGetValue(character, out CharacterMoveLink characterSpriteLink)) { characterSpriteLink.UpdateCharacterLook(lookDirection); }

                characterIndex++;
            }
            lastMemberOffsetIndex = bufferIndex;
        }
        
        private void ResetPartyOffsets()
        {
            foreach (BaseStats character in members)
            {
                character.gameObject.transform.localPosition = Vector2.zero;
            }
        }
        
        private void HandlePlayerLayerChanged(int layer, bool isPlayerImmune)
        {
            foreach (BaseStats character in members)
            {
                character.gameObject.layer = layer;
            }
            
            foreach (var characterSpriteLink in characterSpriteLinkLookup)
            {
                characterSpriteLink.Value.SetIsFlashing(isPlayerImmune);
            }
        }
        #endregion
    }
}
