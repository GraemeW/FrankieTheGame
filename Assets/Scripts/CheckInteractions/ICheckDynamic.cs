using System.Collections.Generic;
using LowDefMustard.Utils;
using Frankie.Core;

namespace Frankie.Control
{
    public interface ICheckDynamic
    {
        public string GetMessage();
        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine);
    }
}
