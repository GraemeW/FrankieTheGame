using System;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils;

namespace Frankie.Speech.Editor
{
    [Serializable]
    [UseWithContext(typeof(ConditionGraphNode))]
    [Node("Dialogue", "", "Predicate")]
    internal sealed class PredicateGraphBlock : BlockNode
    {
        // Const Tunables
        public const string predicateOption = "Predicate";
        public const string negateOption = "Negate";

        #region UnityMethods
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<Predicate>(predicateOption).WithDisplayName("Predicate").ShowInInspectorOnly().Build();
            context.AddOption<bool>(negateOption).WithDisplayName("Negate").ShowInInspectorOnly().Build();
        }
        #endregion

        #region PublicMethods
        public Predicate GetPredicate() => this.GetOptionValue<Predicate>(predicateOption);
        public bool GetNegate() => this.GetOptionValue<bool>(negateOption);

        public void SetPredicate(Predicate predicate, bool negate)
        {
            GetNodeOptionByName(predicateOption).TrySetValue(predicate);
            GetNodeOptionByName(negateOption).TrySetValue(negate);
        }
        #endregion
        
        #region Visuals
        public void RefreshVisuals()
        {
            if (!this.HasOptionsDefined(predicateOption)) { return; }
            Predicate predicate = GetPredicate();
            Title = predicate == null ? "(no predicate)" : $"{(GetNegate() ? "NOT " : "")}{predicate.name}";
        }
        #endregion
    }
}
