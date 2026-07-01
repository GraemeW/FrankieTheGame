using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Stats
{
    public class PartyAlteredData
    {
        private readonly List<BaseStats> members;
        public bool isPartyLeaderDataSet { get; private set; } = false;
        public string partyLeaderName { get; private set; } = string.Empty;
        public Animator partyLeaderAnimator { get; private set; }
        
        public PartyAlteredData(List<BaseStats> members)
        {
            this.members = members != null ? members.ToList() : new List<BaseStats>();
            isPartyLeaderDataSet = false;
        }

        public PartyAlteredData(List<BaseStats> members, string partyLeaderName, Animator partyLeaderAnimator)
        {
            this.members = members != null ? members.ToList() : new List<BaseStats>();
            isPartyLeaderDataSet = true;
            this.partyLeaderName = partyLeaderName;
            this.partyLeaderAnimator = partyLeaderAnimator;
        }
        
        public IList<BaseStats> GetMembers() => members;
        
        public BaseStats GetPartyLeader() => members.Count > 0 ? members[0] : null;

        public GameObject GetPartyLeaderObject()
        {
            BaseStats partyLeader = GetPartyLeader();
            return partyLeader != null ? partyLeader.gameObject : null;
        }
    }
}
