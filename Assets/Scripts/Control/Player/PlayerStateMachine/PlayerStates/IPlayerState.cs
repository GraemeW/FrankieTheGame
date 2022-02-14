using Frankie.Combat;
using Frankie.ZoneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IPlayerState
    {
        void EnterWorld(IPlayerStateContext playerStateContext);
        void EnterTransition(IPlayerStateContext playerStateContext);
        void EnterCombat(IPlayerStateContext playerStateContext);
        void EnterDialogue(IPlayerStateContext playerStateContext);
        void EnterTrade(IPlayerStateContext playerStateContext);
        void EnterOptions(IPlayerStateContext playerStateContext);
    }
}