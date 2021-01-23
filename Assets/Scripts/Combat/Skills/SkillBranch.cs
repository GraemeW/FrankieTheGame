using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frankie.Combat
{
    [System.Serializable]
    public class SkillBranch : ScriptableObject
    {
        [Header("Dialogue Properties")]
        [SerializeField] string upSkillReference = null;
        [SerializeField] string upBranch = null;
        [SerializeField] string leftSkillReference = null;
        [SerializeField] string leftBranch = null;
        [SerializeField] string rightSkillReference = null;
        [SerializeField] string rightBranch = null;
        [SerializeField] string downSkillReference = null;
        [SerializeField] string downBranch = null;
        [SerializeField] string detail = null;
        [SerializeField] Rect rect = new Rect(30, 30, 250, 155);
        [HideInInspector] [SerializeField] Rect draggingRect = new Rect(0, 0, 250, 45);
        [Header("Branch Properties")]
        [HideInInspector] [SerializeField] SkillBranchMapping mappedFromBranch = default;

        public Skill GetSkill(SkillBranchMapping skillBranchMapping)
        {
            if (skillBranchMapping == SkillBranchMapping.up) { return Skill.GetSkillFromName(upSkillReference); }
            else if (skillBranchMapping == SkillBranchMapping.left) { return Skill.GetSkillFromName(leftSkillReference); }
            else if (skillBranchMapping == SkillBranchMapping.right) { return Skill.GetSkillFromName(rightSkillReference); }
            else if (skillBranchMapping == SkillBranchMapping.down) { return Skill.GetSkillFromName(downSkillReference); }
            return null;
        }

        public string GetBranch(SkillBranchMapping skillBranchMapping)
        {
            if (skillBranchMapping == SkillBranchMapping.up) { return upBranch; }
            else if (skillBranchMapping == SkillBranchMapping.left) { return leftBranch; }
            else if (skillBranchMapping == SkillBranchMapping.right) { return rightBranch; }
            else if (skillBranchMapping == SkillBranchMapping.down) { return downBranch; }
            return null;
        }

        public void SetBranch(SkillBranchMapping skillBranchMapping, string skillBranchReference)
        {
            if (skillBranchMapping == SkillBranchMapping.up) { upBranch = skillBranchReference; }
            else if (skillBranchMapping == SkillBranchMapping.left) { leftBranch = skillBranchReference; }
            else if (skillBranchMapping == SkillBranchMapping.right) { rightBranch = skillBranchReference; }
            else if (skillBranchMapping == SkillBranchMapping.down) { downBranch = skillBranchReference; }
        }

        public bool HasBranch(SkillBranchMapping skillBranchMapping)
        {
            return (!string.IsNullOrWhiteSpace(GetBranch(skillBranchMapping)));
        }

        public Vector2 GetPosition()
        {
            return rect.position;
        }

        public Rect GetRect()
        {
            return rect;
        }

        public Rect GetDraggingRect()
        {
            return draggingRect;
        }

        public SkillBranchMapping GetParentBranchMapping()
        {
            return mappedFromBranch;
        }

#if UNITY_EDITOR
        public void Initialize(int width, int height, SkillBranchMapping mappedFromBranch)
        {
            rect.width = width;
            rect.height = height;
            this.mappedFromBranch = mappedFromBranch;
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
                if (skillBranchMapping == SkillBranchMapping.up) { upSkillReference = skillName; }
                else if (skillBranchMapping == SkillBranchMapping.left) { leftSkillReference = skillName; }
                else if (skillBranchMapping == SkillBranchMapping.right) { rightSkillReference = skillName; }
                else if (skillBranchMapping == SkillBranchMapping.down) { downSkillReference = skillName; }
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

        public void SetDraggingRect(Rect draggingRect)
        {
            if (draggingRect != this.draggingRect)
            {
                this.draggingRect = draggingRect;
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}