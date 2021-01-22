using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill Tree", menuName = "Skills/New Skill Tree")]
    public class SkillTree : ScriptableObject
    {
        // Tunables
        [Header("Editor Settings")]
        [SerializeField] Vector2 newBranchOffset = new Vector2(100f, 25f);
        [SerializeField] int branchWidth = 250;
        [SerializeField] int branchHeight = 150;

        // State
        [HideInInspector] [SerializeField] List<SkillBranch> skillBranches = new List<SkillBranch>();
        [HideInInspector] [SerializeField] Dictionary<string, SkillBranch> skillBranchLookup = new Dictionary<string, SkillBranch>();

        private void Awake()
        {
#if UNITY_EDITOR
            CreateRootSkillBranchIfMissing();
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
        private SkillBranch CreateSkillBranch()
        {
            SkillBranch skillBranch = CreateInstance<SkillBranch>();
            Undo.RegisterCreatedObjectUndo(skillBranch, "Created Skill Branch Object");
            skillBranch.Initialize(branchWidth, branchHeight);
            skillBranch.name = System.Guid.NewGuid().ToString();

            Undo.RecordObject(this, "Add Skill Branch");
            skillBranches.Add(skillBranch);

            return skillBranch;
        }

        public SkillBranch CreateChildSkillBranch(SkillBranch parentSkillBranch, SkillBranchMapping skillBranchMapping)
        {
            if (parentSkillBranch == null) { return null; }
            // TODO:  Update offsets so they make sense on each position

            SkillBranch childNode = CreateSkillBranch();
            Vector2 offsetPosition = new Vector2(parentSkillBranch.GetRect().xMax + newBranchOffset.x,
                parentSkillBranch.GetRect().yMin + (parentSkillBranch.GetRect().height + newBranchOffset.y));
            childNode.SetPosition(offsetPosition);

            parentSkillBranch.AddChild(childNode.name, skillBranchMapping);
            OnValidate();

            return childNode;
        }

        private SkillBranch CreateRootSkillBranchIfMissing()
        {
            if (skillBranches.Count == 0)
            {
                SkillBranch rootSkillBranch = CreateSkillBranch();

                OnValidate();
                return rootSkillBranch;
            }

            return null;
        }

        public void DeleteSkillBranch(SkillBranch skillBranchToDelete, SkillBranchMapping skillBranchMapping)
        {
            if (skillBranchToDelete == null) { return; }

            Undo.RecordObject(this, "Delete Zone Node");
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

        public void OnAfterDeserialize()// Unused, required for interface
        {
        }

        public void OnBeforeSerialize()
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
    }
}
