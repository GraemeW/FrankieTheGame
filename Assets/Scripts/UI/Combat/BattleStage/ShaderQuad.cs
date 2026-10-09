using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    public static class ShaderQuad
    {
        // For use w/ elements drawn by a custom material (style.unityMaterial) - one quad over an element's content rect
        // Note:  UV v runs upwards (bottom edge = uvRect.yMin), as shaders authored for sprites would expect

        public static void Draw(MeshGenerationContext meshGenerationContext, Rect uvRect, Color tint, Texture texture = null)
        {
            Rect bounds = meshGenerationContext.visualElement.contentRect;
            if (bounds.width <= 0f || bounds.height <= 0f) { return; }

            MeshWriteData mesh = meshGenerationContext.Allocate(4, 6, texture);
            mesh.SetNextVertex(new Vertex { position = new Vector3(bounds.xMin, bounds.yMin, Vertex.nearZ), tint = tint, uv = new Vector2(uvRect.xMin, uvRect.yMax) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(bounds.xMax, bounds.yMin, Vertex.nearZ), tint = tint, uv = new Vector2(uvRect.xMax, uvRect.yMax) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(bounds.xMax, bounds.yMax, Vertex.nearZ), tint = tint, uv = new Vector2(uvRect.xMax, uvRect.yMin) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(bounds.xMin, bounds.yMax, Vertex.nearZ), tint = tint, uv = new Vector2(uvRect.xMin, uvRect.yMin) });
            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
            mesh.SetNextIndex(0);
        }
    }
}
