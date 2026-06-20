using UnityEngine;

namespace MusicGame.Notes
{
    /// <summary>
    /// Tints a note SpriteRenderer.
    ///
    /// The notes are textureless VectorGraphics sprites drawn with the
    /// Hidden/VectorGraphics/Vector shader. That shader multiplies the mesh vertex
    /// colours by the material <c>_Color</c> but ignores <c>SpriteRenderer.color</c>,
    /// so the miss-dim (and any other tint) must be pushed through a
    /// MaterialPropertyBlock. We also keep <c>SpriteRenderer.color</c> in sync so the
    /// existing code that reads <c>renderer.color</c> still sees the intended colour,
    /// and so the tint behaves identically on every platform (Windows/macOS/iOS/Android).
    /// </summary>
    public static class NoteRendererTint
    {
        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void SetTint(this SpriteRenderer renderer, Color color)
        {
            if (renderer == null) return;

            renderer.color = color;

            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }
    }
}
