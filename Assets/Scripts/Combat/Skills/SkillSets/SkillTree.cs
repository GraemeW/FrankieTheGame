using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using LowDefMustard.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill Tree", menuName = "Combat/Skills/New Skill Tree", order = 4)]
    public class SkillTree : SkillSet, ISerializationCallbackReceiver
    {
        // Tunables
#if UNITY_EDITOR
        [Header("Editor Settings")]
        [SerializeField] private Vector2 newBranchOffset = new(150f, 150f);
        [SerializeField] private int branchWidth = 250;
        [SerializeField] private int branchHeight = 155;
        private const float _upOffsetMultiplier = 2.0f;
        private const float _leftOffsetMultiplier = 2.5f;
#endif
        
        // State
        [HideInInspector] [SerializeField] private List<SkillBranch> skillBranches = new();
        
        #region PublicMethods
        public IEnumerable<SkillBranch> GetAllBranches() => skillBranches;
        public SkillBranch GetRootSkillBranch() => skillBranches.Count > 0 ? skillBranches[0] : null; // New trees are empty until opened in the editor
        public SkillBranch GetSkillBranchFromID(string branchName) => skillBranches.FirstOrDefault(skillBranch => skillBranch.name == branchName);

        public override IEnumerable<IObjectProbabilityPair<Skill>> GetWeightedSkills()
        {
            return skillBranches.Where(skillBranch => skillBranch != null).SelectMany(skillBranch => skillBranch.GetAllSkills()).Select(skill => new SkillProbabilityPair(skill, 1));
        }

        public SkillBranch GetChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping)
        {
            if (parentSkillBranch == null) { return null; }
            string childUniqueID = parentSkillBranch.GetBranch(skillBranchMapping);
            return string.IsNullOrWhiteSpace(childUniqueID) ? null : skillBranches.FirstOrDefault(skillBranch => skillBranch.name == childUniqueID);
        }
        #endregion

        #region SkillSetEditorMethods
#if UNITY_EDITOR
        public SkillBranch CreateChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping)
        {
            if (parentSkillBranch == null) { return null; }

            SkillBranch childBranch = CreateSkillBranch(skillBranchMapping);

            if (skillBranchMapping == SkillBranchMapping.Up)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMin,
                    parentSkillBranch.GetRect().yMin - (float)(newBranchOffset.y * _upOffsetMultiplier));
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.Left)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMin - (float)(newBranchOffset.x * _leftOffsetMultiplier),
                    parentSkillBranch.GetRect().yMin);
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.Right)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMax + newBranchOffset.x,
                    parentSkillBranch.GetRect().yMin);
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.Down)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMin,
                    parentSkillBranch.GetRect().yMax + newBranchOffset.y);
                childBranch.SetPosition(offsetPosition);
            }
            parentSkillBranch.AddChild(childBranch.name, skillBranchMapping);

            return childBranch;
        }

        public SkillBranch CreateRootSkillBranchIfMissing()
        {
            if (skillBranches.Count != 0) return null;
            SkillBranch rootSkillBranch = CreateSkillBranch(0);
            return rootSkillBranch;
        }

        public void DeleteSkillBranch(SkillBranch skillBranchToDelete, SkillBranchMapping skillBranchMapping)
        {
            if (skillBranchToDelete == null) { return; }

            Undo.RecordObject(this, "Delete Skill Node");
            skillBranches.Remove(skillBranchToDelete);
            CleanDanglingChildren(skillBranchToDelete, skillBranchMapping);

            Undo.DestroyObjectImmediate(skillBranchToDelete);
        }
        
        private SkillBranch CreateSkillBranch(SkillBranchMapping skillBranchMapping)
        {
            var skillBranch = CreateInstance<SkillBranch>();
            Undo.RegisterCreatedObjectUndo(skillBranch, "Created Skill Branch Object");
            skillBranch.Initialize(branchWidth, branchHeight, skillBranchMapping);
            skillBranch.name = System.Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

            Undo.RecordObject(this, "Add Skill Branch");
            skillBranches.Add(skillBranch);

            return skillBranch;
        }

        private void CleanDanglingChildren(SkillBranch skillBranchToDelete, SkillBranchMapping skillBranchMapping)
        {
            foreach (SkillBranch skillBranch in skillBranches)
            {
                skillBranch.RemoveChild(skillBranchToDelete.name, skillBranchMapping);
            }
        }
#endif
        #endregion

        #region Interfaces
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetAssetPath(this) == "") return;
            foreach (SkillBranch skillBranch in GetAllBranches())
            {
                if (AssetDatabase.GetAssetPath(skillBranch) == "")
                {
                    AssetDatabase.AddObjectToAsset(skillBranch, this);
                }
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}
