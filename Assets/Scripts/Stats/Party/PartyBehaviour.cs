using System;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Control;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Stats
{
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public abstract class PartyBehaviour : MonoBehaviour
    {
        // Tunables
        [SerializeField][Range(1, 8)] protected int partyLimit = 4;
        [SerializeField] protected List<BaseStats> members = new List<BaseStats>();
        [SerializeField] protected Transform container;
        [SerializeField] protected int partyOffset = 16;
        
        // Static
        private const int _initialOffset = 0;

        // State
        protected readonly Dictionary<BaseStats, CharacterSpriteLink> characterSpriteLinkLookup = new Dictionary<BaseStats, CharacterSpriteLink>();
        int lastMemberOffsetIndex = 0;

        // Cached References
        protected PlayerMover playerMover;
        private PlayerStateMachine playerStateMachine;

        // Abstract Methods
        protected abstract bool AddToParty(BaseStats character); // AddToParty -- Parent
        public abstract bool AddToParty(CharacterNPCSwapper characterNPCSwapper); // AddToParty -- Derivative:  Add from character NPC Swapper
        public abstract bool AddToParty(CharacterProperties characterProperties); // AddToParty -- Derivative:  Add from nothing
        public abstract bool RemoveFromParty(BaseStats character); // RemoveFromParty -- Parent:  Instantiate nothing
        public abstract bool RemoveFromParty(CharacterProperties characterProperties); // RemoveFromParty -- Derivative:  In case no knowledge if member in party
        public abstract bool RemoveFromParty(BaseStats character, Transform worldTransform); // RemoveFromParty -- Derivative:  Instantiate an NPC at the defined location
        
        // Standard Behaviours
        #region ProtectedMethods
        protected virtual void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            playerStateMachine = GetComponent<PlayerStateMachine>();
            RefreshAnimatorLookup();
        }

        protected virtual void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyOffsets;
            playerMover.playerMoved += UpdatePartyOffsets;
            playerStateMachine.playerLayerChanged += HandlePlayerLayerChanged;
        }

        protected virtual void OnDisable()
        {
            playerMover.movementHistoryReset -= ResetPartyOffsets;
            playerMover.playerMoved -= UpdatePartyOffsets;
            playerStateMachine.playerLayerChanged -= HandlePlayerLayerChanged;
        }

        protected virtual int GetInitialPartyOffset() => _initialOffset;
        protected virtual bool ShouldSkipFirstEntryOffset() => true;
        protected bool HasMember(BaseStats member) => HasMember(member.GetCharacterProperties());

        protected void RefreshAnimatorLookup()
        {
            characterSpriteLinkLookup.Clear();
            foreach (BaseStats character in members)
            {
                characterSpriteLinkLookup.Add(character, character.GetComponent<CharacterSpriteLink>());
            }
        }

        public void TogglePartyVisible(bool enable)
        {
            foreach (BaseStats member in members)
            {
                if (member.TryGetComponent(out CharacterSpriteLink characterSpriteLink))
                {
                    SpriteRenderer spriteRenderer = characterSpriteLink.GetSpriteRenderer();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.enabled = enable;
                    }
                }
            }
        }

        protected void UpdatePartySpeed(float speed)
        {
            int characterIndex = 0;
            foreach (BaseStats character in members)
            {
                if (characterIndex == 0) { characterIndex++; continue; }
                characterSpriteLinkLookup[character].UpdateCharacterAnimation(speed);
                characterIndex++;
            }
        }
        #endregion

        #region PublicMethods
        public List<BaseStats> GetParty() => members;
        public int GetLastMemberOffsetIndex() => lastMemberOffsetIndex;

        public BaseStats GetMember(CharacterProperties member)
        {
            foreach (BaseStats character in members)
            {
                CharacterProperties characterProperties = character.GetCharacterProperties();
                if (characterProperties.GetCharacterNameID() == member.GetCharacterNameID())
                {
                    return character;
                }
            }
            return null;
        }

        public BaseStats GetNextMember(BaseStats currentMember, bool traverseForward)
        {
            BaseStats nextMember = null;

            // Simple case
            if (currentMember == null || members.Count == 1)
            {
                nextMember = members[0];
            }
            else
            {
                // Normal handling
                for (int index = 0; index < members.Count; index++)
                {
                    if (currentMember != members[index]) { continue; }

                    if (traverseForward)
                    {
                        nextMember = index == members.Count - 1 ? members[0] : members[index + 1];
                    }
                    else
                    {
                        nextMember = index == 0 ? members[^1] : members[index - 1];
                    }
                }
            }

            return nextMember;
        }
        #endregion
        
        #region PrivateMethods
        private bool HasMember(CharacterProperties member)
        {
            foreach (BaseStats character in members)
            {
                CharacterProperties characterProperties = character.GetCharacterProperties();
                if (characterProperties.GetCharacterNameID() == member.GetCharacterNameID())
                {
                    return true;
                }
            }
            return false;
        }
        
        private void UpdatePartyOffsets(CircularBuffer<Tuple<Vector2, Vector2>> movementHistory)
        {
            Vector2 leaderPosition = movementHistory.GetFirstEntry().Item1;

            int characterIndex = 0;
            int bufferIndex = 0;
            foreach (BaseStats character in members)
            {
                if (ShouldSkipFirstEntryOffset() && characterIndex == 0) { characterIndex++; continue; }

                Vector2 localPosition;
                Vector2 lookDirection;
                bufferIndex = characterIndex * partyOffset + GetInitialPartyOffset();
                if (bufferIndex >= movementHistory.GetCurrentSize())
                {
                    localPosition = movementHistory.GetLastEntry().Item1 - leaderPosition;
                    lookDirection = movementHistory.GetLastEntry().Item2;
                }
                else
                {
                    localPosition = movementHistory.GetEntryAtPosition(bufferIndex).Item1 - leaderPosition;
                    lookDirection = movementHistory.GetEntryAtPosition(bufferIndex).Item2;
                }
                character.gameObject.transform.localPosition = localPosition;
                characterSpriteLinkLookup[character].UpdateCharacterAnimation(lookDirection.x, lookDirection.y);

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
            foreach (var characterSpriteLink in characterSpriteLinkLookup)
            {
                characterSpriteLink.Value.SetIsFlashing(isPlayerImmune);
            }
        }
        #endregion
    }
}
