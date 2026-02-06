using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [Serializable]
    public class SkillBranch : ScriptableObject
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
        [HideInInspector] [SerializeField] private Rect draggingRect = new(0, 0, 250, 45);

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
        public Vector2 GetPosition() => rect.position;
        public Rect GetRect() => rect;
        public Rect GetDraggingRect() => draggingRect;
#if UNITY_EDITOR
        public void Initialize(int width, int height, SkillBranchMapping setMappedFromBranch)
        {
            rect.width = width;
            rect.height = height;
            mappedFromBranch = setMappedFromBranch;
            EditorUtility.SetDirty(this);
        }

        public void SetSkill(string skillName, SkillBranchMapping skillBranchMapping)
        {
            if (Skill.GetSkillFromName(skillName) == null) // Skill does not exist
            {
                skillName = ""; // override to whitespace, parses as null
            }
            
            if (GetSkill(skillBranchMapping) == null || GetSkill(skillBranchMapping).name != skillName)
            {
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
            }
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

        public void SetPosition(Vector2 position)
        {
            Undo.RecordObject(this, "Move Skill Branch");
            rect.position = position;
            EditorUtility.SetDirty(this);
        }

        public void SetDraggingRect(Rect setDraggingRect)
        {
            if (setDraggingRect == draggingRect) { return; }
            draggingRect = setDraggingRect;
            EditorUtility.SetDirty(this);
        }
#endif
        #endregion
    }
}