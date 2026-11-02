namespace LowDefMustard.Rendering
{
    public enum GraphicsWarmupPath
    {
        UIToolkitMaterial, // VisualElement drawn by a custom material (style.unityMaterial)
        UIToolkitBackdropFilter, // VisualElement w/ a custom backdrop filter (style.backdropFilter)
        Sprite, // SpriteRenderer drawn by the camera
        SpriteShape // SpriteShapeRenderer drawn by the camera
    }
}
