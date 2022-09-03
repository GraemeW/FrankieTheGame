using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill Tree", menuName = "Skills/New Skill Tree")]
    public class SkillTree : ScriptableObject, ISerializationCallbackReceiver
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] Vector2 newBranchOffset = new Vector2(150f, 150f);
        float upOffsetMultiplier = 2.0f;
        float leftOffsetMultiplier = 2.5f;
        [SerializeField] int branchWidth = 250;
        [SerializeField] int branchHeight = 155;

        // State
        [HideInInspector] [SerializeField] List<SkillBranch> skillBranches = new List<SkillBranch>();
        [HideInInspector] [SerializeField] Dictionary<string, SkillBranch> skillBranchLookup = new Dictionary<string, SkillBranch>();

        private void Awake()
        {
#if UNITY_EDITOR
            // CreateRootSkillBranchIfMissing();
            // Note:  Making in SkillTreeEditor due to some intricacies in how Unity calls Awake on ScriptableObjects in Editor vs. the serialization callback
            // For (unknown) reasons, the root node gets made and then killed by the time serialization occurs
#endif
        }

        private void OnValidate()
        {
            skillBranchLookup = new Dictionary<string, SkillBranch>();
            foreach (SkillBranch skillBranch in skillBranches)
            {
                skillBranchLookup.Add(skillBranch.name, skillBranch);
            }
        }

        public IEnumerable<SkillBranch> GetAllBranches()
        {
            return skillBranches;
        }

        public SkillBranch GetRootSkillBranch()
        {
            return skillBranches[0];
        }

        public SkillBranch GetSkillBranchFromID(string name)
        {
            foreach (SkillBranch skillBranch in skillBranches)
            {
                if (skillBranch.name == name)
                {
                    return skillBranch;
                }
            }
            return null;
        }

        public SkillBranch GetChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping)
        {
            if (parentSkillBranch == null) { return null; }
            string childUniqueID = parentSkillBranch.GetBranch(skillBranchMapping);
            if (string.IsNullOrWhiteSpace(childUniqueID)) { return null; }

            if (skillBranchLookup.ContainsKey(childUniqueID))
            {
                return skillBranchLookup[childUniqueID];
            }

            return null;
        }

        // Dialogue editing functionality
#if UNITY_EDITOR
        private SkillBranch CreateSkillBranch(SkillBranchMapping skillBranchMapping)
        {
            SkillBranch skillBranch = CreateInstance<SkillBranch>();
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
                    parentSkillBranch.GetRect().yMin - (float)(newBranchOffset.y * upOffsetMultiplier));
                childBranch.SetPosition(offsetPosition);
            }
            else if (skillBranchMapping == SkillBranchMapping.left)
            {
                Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMin - (float)(newBranchOffset.x * leftOffsetMultiplier),
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
            OnValidate();

            return childBranch;
        }

        public SkillBranch CreateRootSkillBranchIfMissing()
        {
            if (skillBranches.Count == 0)
            {
                SkillBranch rootSkillBranch = CreateSkillBranch(0);

                OnValidate();
                return rootSkillBranch;
            }

            return null;
        }

        public void DeleteSkillBranch(SkillBranch skillBranchToDelete, SkillBranchMapping skillBranchMapping)
        {
            if (skillBranchToDelete == null) { return; }

            Undo.RecordObject(this, "Delete Skill Node");
            skillBranches.Remove(skillBranchToDelete);
            CleanDanglingChildren(skillBranchToDelete, skillBranchMapping);
            OnValidate();

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
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (SkillBranch skillBranch in GetAllBranches())
                {
                    if (AssetDatabase.GetAssetPath(skillBranch) == "")
                    {
                        AssetDatabase.AddObjectToAsset(skillBranch, this);
                    }
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
