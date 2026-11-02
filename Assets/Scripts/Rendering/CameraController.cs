using UnityEngine;
using LowDefMustard.Rendering;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Rendering
{
    public sealed class CameraController : CameraControllerBase
    {
        // Note:  Follows the party leader (re-targeted whenever the party leader changes)

        #region AbstractImplementations
        protected override GameObject FindDefaultFollowTarget() => TryGetParty(out Party party) ? party.GetPartyLeaderObject() : null;

        protected override void SubscribeToDefaultFollowTarget(bool subscribe)
        {
            if (TryGetParty(out Party party)) { party.SubscribeToMembersAlteredUpdates(subscribe, HandlePartyLeaderAnnouncements); }
        }
        #endregion

        #region PrivateMethods
        private static bool TryGetParty(out Party party)
        {
            party = null;
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject != null && playerObject.TryGetComponent(out party);
        }

        private void HandlePartyLeaderAnnouncements(PartyAlteredData partyAlteredData)
        {
            if (partyAlteredData is not { isPartyLeaderDataSet: true }) { return; }
            SetDefaultFollowTarget(partyAlteredData.GetPartyLeaderObject());
        }
        #endregion
    }
}
