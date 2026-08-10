using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    [Serializable]
    public class SkillBranch : ScriptableObject, IStandardGraphNode
    {
        [Header("Skill Properties")]
        [SerializeField] private string upSkillReference;
        [SerializeField] private string upBranch;
        [SerializeField] private string leftSkillReference;
        [SerializeField] private string leftBranch;
        [SerializeField] private string rightSkillReference;
        [SerializeField] private string rightBranch;
        [SerializeField] private string downSkillReference;
        [SerializeField] private string downBranch;
        [Header("Branch Properties")]
        [HideInInspector] [SerializeField] private SkillBranchMapping mappedFromBranch;
        [Header("Editor Properties")]
        [SerializeField] private Rect rect = new(30, 30, 250, 155);

        #region NodeInterface
        // Note:  Must be outside pragma for compilation
        public ScriptableObject scriptableObject => this;
        public Vector2 GetPosition() => rect.position;
        public void SetPosition(Vector2 position)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Move Skill Branch");
            rect.position = position;
            EditorUtility.SetDirty(this);
#endif
        }
        #endregion
        
        #region SkillGetters
        public bool HasSkill(SkillBranchMapping skillBranchMapping) => GetSkill(skillBranchMapping) != null;
        public Skill GetSkill(SkillBranchMapping skillBranchMapping)
        {
            return skillBranchMapping switch
            {
                SkillBranchMapping.Up => Skill.GetSkillFromName(upSkillReference),
                SkillBranchMapping.Left => Skill.GetSkillFromName(leftSkillReference),
                SkillBranchMapping.Right => Skill.GetSkillFromName(rightSkillReference),
                SkillBranchMapping.Down => Skill.GetSkillFromName(downSkillReference),
                _ => null
            };
        }

        public IEnumerable<Skill> GetAllSkills()
        {
            foreach (SkillBranchMapping skillBranchMapping in Enum.GetValues(typeof(SkillBranchMapping)))
            {
                if (HasSkill(skillBranchMapping))
                {
                    yield return GetSkill(skillBranchMapping);
                }
            }
        }
        #endregion

        #region BranchGetters
        public bool HasBranch(SkillBranchMapping skillBranchMapping) => !string.IsNullOrWhiteSpace(GetBranch(skillBranchMapping));
        public SkillBranchMapping GetParentBranchMapping() => mappedFromBranch;
        public string GetBranch(SkillBranchMapping skillBranchMapping)
        {
            return skillBranchMapping switch
            {
                SkillBranchMapping.Up => upBranch,
                SkillBranchMapping.Left => leftBranch,
                SkillBranchMapping.Right => rightBranch,
                SkillBranchMapping.Down => downBranch,
                _ => null
            };
        }
        #endregion

        #region BranchSetters
        public void SetBranch(SkillBranchMapping skillBranchMapping, string skillBranchReference)
        {
            switch (skillBranchMapping)
            {
                case SkillBranchMapping.Up:
                    upBranch = skillBranchReference;
                    break;
                case SkillBranchMapping.Left:
                    leftBranch = skillBranchReference;
                    break;
                case SkillBranchMapping.Right:
                    rightBranch = skillBranchReference;
                    break;
                case SkillBranchMapping.Down:
                    downBranch = skillBranchReference;
                    break;
            }
        }
        #endregion
        
        #region EditorMethods
        public Rect GetRect() => rect;
#if UNITY_EDITOR
        public void Initialize(int width, int height, SkillBranchMapping setMappedFromBranch)
        {
            rect.width = width;
            rect.height = height;
            mappedFromBranch = setMappedFromBranch;
            EditorUtility.SetDirty(this);
        }

        public bool SetSkill(string skillName, SkillBranchMapping skillBranchMapping)
        {
            bool wasSkillFound = true;
            if (Skill.GetSkillFromName(skillName) == null)
            {
                skillName = string.Empty;
                wasSkillFound = false;
            }

            if (GetSkill(skillBranchMapping) != null && GetSkill(skillBranchMapping).name == skillName) { return wasSkillFound; }
            
            Undo.RecordObject(this, "Update Skill");
            switch (skillBranchMapping)
            {
                case SkillBranchMapping.Up:
                    upSkillReference = skillName;
                    break;
                case SkillBranchMapping.Left:
                    leftSkillReference = skillName;
                    break;
                case SkillBranchMapping.Right:
                    rightSkillReference = skillName;
                    break;
                case SkillBranchMapping.Down:
                    downSkillReference = skillName;
                    break;
            }
            EditorUtility.SetDirty(this);
            return wasSkillFound;
        }

        public void AddChild(string childID, SkillBranchMapping skillBranchMapping)
        {
            Undo.RecordObject(this, "Add Branch Relation");
            SetBranch(skillBranchMapping, childID);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChild(string childID, SkillBranchMapping skillBranchMapping)
        {
            Undo.RecordObject(this, "Remove Branch Relation");
            if (GetBranch(skillBranchMapping) == childID)
            {
                SetBranch(skillBranchMapping, null);
            }
            EditorUtility.SetDirty(this);
        }
#endif
        #endregion
    }
}