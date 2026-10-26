using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    public class SkillTreeEdgesLayer : VisualElement
    {
        // Tunables
        private const float _bezierOffsetMultiplier = 0.7f;
        private const float _strokeWidth = 3f;
        private static readonly Color _colorStart = Color.sienna;
        private static readonly Color _colorEnd = Color.lemonChiffon;

        // State
        private readonly Func<SkillTree> getSkillTree;
        private static readonly Gradient _connectionGradient = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(_colorStart, 0f),
                new GradientColorKey(_colorEnd, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };
        
        public SkillTreeEdgesLayer(Func<SkillTree> getSkillTree)
        {
            this.getSkillTree = getSkillTree;

            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            SkillTree skillTree = getSkillTree?.Invoke();
            if (skillTree == null) { return; }

            Painter2D painter2D = context.painter2D;
            foreach (SkillBranch skillBranch in skillTree.GetAllBranches())
            {
                DrawMappingConnection(painter2D, skillTree, skillBranch, SkillBranchMapping.Up);
                DrawMappingConnection(painter2D, skillTree, skillBranch, SkillBranchMapping.Left);
                DrawMappingConnection(painter2D, skillTree, skillBranch, SkillBranchMapping.Right);
                DrawMappingConnection(painter2D, skillTree, skillBranch, SkillBranchMapping.Down);
            }
        }

        private static void DrawMappingConnection(Painter2D painter2D, SkillTree skillTree, SkillBranch skillBranch, SkillBranchMapping skillBranchMapping)
        {
            SkillBranch child = skillTree.GetChildSkillBranch(skillBranch, skillBranchMapping);
            if (child == null) { return; }

            Rect fromRect = skillBranch.GetRect();
            Rect toRect = child.GetRect();

            Vector2 startPoint, endPoint, tangent1, tangent2;
            switch (skillBranchMapping)
            {
                case SkillBranchMapping.Up:
                    startPoint = new Vector2(fromRect.center.x, fromRect.yMin);
                    endPoint = new Vector2(toRect.center.x, toRect.yMax);
                    float upOffset = (endPoint.y - startPoint.y) * _bezierOffsetMultiplier;
                    tangent1 = startPoint + Vector2.up * upOffset;
                    tangent2 = endPoint + Vector2.down * upOffset;
                    break;
                case SkillBranchMapping.Left:
                    startPoint = new Vector2(fromRect.xMin, fromRect.center.y);
                    endPoint = new Vector2(toRect.xMax, toRect.center.y);
                    float leftOffset = (startPoint.x - endPoint.x) * _bezierOffsetMultiplier;
                    tangent1 = startPoint + Vector2.left * leftOffset;
                    tangent2 = endPoint + Vector2.right * leftOffset;
                    break;
                case SkillBranchMapping.Right:
                    startPoint = new Vector2(fromRect.xMax, fromRect.center.y);
                    endPoint = new Vector2(toRect.xMin, toRect.center.y);
                    float rightOffset = (endPoint.x - startPoint.x) * _bezierOffsetMultiplier;
                    tangent1 = startPoint + Vector2.right * rightOffset;
                    tangent2 = endPoint + Vector2.left * rightOffset;
                    break;
                case SkillBranchMapping.Down:
                    startPoint = new Vector2(fromRect.center.x, fromRect.yMax);
                    endPoint = new Vector2(toRect.center.x, toRect.yMin);
                    float downOffset = (startPoint.y - endPoint.y) * _bezierOffsetMultiplier;
                    tangent1 = startPoint + Vector2.down * downOffset;
                    tangent2 = endPoint + Vector2.up * downOffset;
                    break;
                default:
                    return;
            }
            
            painter2D.lineWidth = _strokeWidth;
            painter2D.lineCap = LineCap.Round;
            painter2D.strokeGradient = _connectionGradient;

            painter2D.BeginPath();
            painter2D.MoveTo(startPoint);
            painter2D.BezierCurveTo(tangent1, tangent2, endPoint);
            painter2D.Stroke();
        }
    }
}
