using UnityEngine;

namespace MusicGame.Core
{
    public static class SpriteGenerator
    {
        private static Sprite whiteSprite;
        private static Sprite circleSprite;

        public static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.SetPixel(0, 1, Color.white);
                tex.SetPixel(1, 0, Color.white);
                tex.SetPixel(1, 1, Color.white);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            }
            return whiteSprite;
        }

        public static Sprite GetCircleSprite()
        {
            if (circleSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color white = Color.white;
                Color clear = Color.clear;
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float radius = size / 2f - 1;

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        tex.SetPixel(x, y, dist <= radius ? white : clear);
                    }
                }
                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            }
            return circleSprite;
        }
    }
}
