using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill Tree", menuName = "Skills/New Skill Tree")]
    public class SkillTree : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] private Vector2 newBranchOffset = new(150f, 150f);
        [SerializeField] private int branchWidth = 250;
        [SerializeField] private int branchHeight = 155;

#if UNITY_EDITOR
        private const float _upOffsetMultiplier = 2.0f;
        private const float _leftOffsetMultiplier = 2.5f;
#endif
        
        // State
        [HideInInspector] [SerializeField] private List<SkillBranch> skillBranches = new();

        private void Awake()
        {
#if UNITY_EDITOR
            // CreateRootSkillBranchIfMissing();
            // Note:  Making in SkillTreeEditor due to some intricacies in how Unity calls Awake on ScriptableObjects in Editor vs. the serialization callback
            // For (unknown) reasons, the root node gets made and then killed by the time serialization occurs
#endif
        }
        public IEnumerable<SkillBranch> GetAllBranches() => skillBranches;
        public SkillBranch GetRootSkillBranch() => skillBranches[0];
        public SkillBranch GetSkillBranchFromID(string branchName) => skillBranches.FirstOrDefault(skillBranch => skillBranch.name == branchName);

        public SkillBranch GetChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping)
        {
            if (parentSkillBranch == null) { return null; }
            string childUniqueID = parentSkillBranch.GetBranch(skillBranchMapping);
            return !string.IsNullOrWhiteSpace(childUniqueID) ? skillBranches.Select(skillBranch => skillBranch.name == childUniqueID ? skillBranch : null).FirstOrDefault() : null;
        }

        // Dialogue editing functionality
#if UNITY_EDITOR
        private SkillBranch CreateSkillBranch(SkillBranchMapping skillBranchMapping)
        {
            var skillBranch = CreateInstance<SkillBranch>();
            Undo.RegisterCreatedObjectUndo(skillBranch, "Created Skill Branch Object");
            skillBranch.Initialize(branchWidth, branchHeight, skillBranchMapping);
            skillBranch.name = System.Guid.NewGuid().ToString();

            Undo.RecordObject(this, "Add Skill Branch");
            skillBranches.Add(skillBranch);

            return skillBranch;
        }

        public SkillBranch CreateChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping)
        {
            if (parentSkillBranch == null) { return null; }

            SkillBranch childBranch = CreateSkillBranch(skillBranchMapping);

            if (skillBranchMapping == SkillBranchMapping.up)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMin,
                    parentSkillBranch.GetRect().yMin - (float)(newBranchOffset.y * _upOffsetMultiplier));
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.left)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMin - (float)(newBranchOffset.x * _leftOffsetMultiplier),
                    parentSkillBranch.GetRect().yMin);
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.right)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMax + newBranchOffset.x,
                    parentSkillBranch.GetRect().yMin);
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.down)
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

        private void CleanDanglingChildren(SkillBranch skillBranchToDelete, SkillBranchMapping skillBranchMapping)
        {
            foreach (SkillBranch skillBranch in skillBranches)
            {
                skillBranch.RemoveChild(skillBranchToDelete.name, skillBranchMapping);
            }
        }
#endif

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
