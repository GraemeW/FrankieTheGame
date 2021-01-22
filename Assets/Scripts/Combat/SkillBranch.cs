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
        [SerializeField] Skill upSkill = null;
        [SerializeField] string upBranch = null;
        [SerializeField] Skill leftSkill = null;
        [SerializeField] string leftBranch = null;
        [SerializeField] Skill rightSkill = null;
        [SerializeField] string rightBranch = null;
        [SerializeField] Skill downSkill = null;
        [SerializeField] string downBranch = null;
        [SerializeField] string detail = null;
        [SerializeField] string skillBranchName = null;
        [SerializeField] Rect rect = new Rect(30, 30, 250, 150);
        [HideInInspector] [SerializeField] Rect draggingRect = new Rect(0, 0, 250, 45);

        public string GetSkillBranchName()
        {
            return skillBranchName;
        }

        public Skill GetSkill(SkillBranchMapping skillBranchMapping)
        {
            if (skillBranchMapping == SkillBranchMapping.up) { return upSkill; }
            else if (skillBranchMapping == SkillBranchMapping.left) { return leftSkill; }
            else if (skillBranchMapping == SkillBranchMapping.right) { return rightSkill; }
            else if (skillBranchMapping == SkillBranchMapping.down) { return downSkill; }
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

#if UNITY_EDITOR
        public void Initialize(int width, int height)
        {
            rect.width = width;
            rect.height = height;
            EditorUtility.SetDirty(this);
        }

        public void SetSkillBranchName(string skillBranchName)
        {
            if (skillBranchName != this.skillBranchName)
            {
                Undo.RecordObject(this, "Update Zone");
                this.skillBranchName = skillBranchName;
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