using Frankie.Control;
using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [RequireComponent(typeof(PlayerMover))]
    public abstract class PartyBehaviour : MonoBehaviour
    {
        // Tunables
        [SerializeField][Range(1, 8)] protected int partyLimit = 4;
        [SerializeField] protected List<BaseStats> members = new List<BaseStats>();
        [SerializeField] protected Transform container = null;
        [SerializeField] protected int partyOffset = 16;
        protected int initialOffset = 0;

        // State
        protected Dictionary<BaseStats, Animator> animatorLookup = new Dictionary<BaseStats, Animator>();
        int lastMemberOffsetIndex = 0;

        // Cached References
        protected PlayerMover playerMover = null;

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
            RefreshAnimatorLookup();
        }

        protected virtual void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyOffsets;
            playerMover.playerMoved += UpdatePartyOffsets;
        }

        protected virtual void OnDisable()
        {
            playerMover.movementHistoryReset -= ResetPartyOffsets;
            playerMover.playerMoved -= UpdatePartyOffsets;
        }

        protected virtual int GetInitialPartyOffset() => initialOffset;

        protected virtual bool ShouldSkipFirstEntryOffset() => true;

        protected void UpdatePartyOffsets(CircularBuffer<Tuple<Vector2, Vector2>> movementHistory)
        {
            Vector2 leaderPosition = movementHistory.GetFirstEntry().Item1;

            int characterIndex = 0;
            int bufferIndex = 0;
            foreach (BaseStats character in members)
            {
                if (ShouldSkipFirstEntryOffset() && characterIndex == 0) { characterIndex++; continue; }

                Vector2 localPosition = Vector2.zero;
                Vector2 lookDirection = Vector2.zero;
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
                animatorLookup[character].SetFloat("xLook", lookDirection.x);
                animatorLookup[character].SetFloat("yLook", lookDirection.y);

                characterIndex++;
            }
            lastMemberOffsetIndex = bufferIndex;
        }

        protected void ResetPartyOffsets()
        {
            foreach (BaseStats character in members)
            {
                character.gameObject.transform.localPosition = Vector2.zero;
            }
        }

        protected void RefreshAnimatorLookup()
        {
            animatorLookup.Clear();
            foreach (BaseStats character in members)
            {
                animatorLookup.Add(character, character.GetComponent<Animator>());
            }
        }

        protected void UpdatePartySpeed(float speed)
        {
            int characterIndex = 0;
            foreach (BaseStats character in members)
            {
                if (characterIndex == 0) { characterIndex++; continue; }
                animatorLookup[character].SetFloat("Speed", speed);
                characterIndex++;
            }
        }
        #endregion

        #region PublicMethods
        public List<BaseStats> GetParty() => members;
        public bool HasMember(BaseStats member) => HasMember(member.GetCharacterProperties());
        public int GetLastMemberOffsetIndex() => lastMemberOffsetIndex;

        public bool HasMember(CharacterProperties member)
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
                        if (index == members.Count - 1) { nextMember = members[0]; }
                        else { nextMember = members[index + 1]; }
                    }
                    else
                    {
                        if (index == 0) { nextMember = members[members.Count - 1]; }
                        else { nextMember = members[index - 1]; }
                    }
                }
            }

            return nextMember;
        }
        #endregion
    }
}