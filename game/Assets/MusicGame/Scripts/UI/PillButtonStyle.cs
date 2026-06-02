using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public static class PillButtonStyle
    {
        private static Sprite pillSprite;
        public static readonly Color Cyan = new Color32(0x39, 0xC5, 0xBB, 0xFF);
        public static readonly Color Panel = new Color(0.04f, 0.10f, 0.16f, 0.78f);

        public static void Apply(Button button, Color color)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = GetSprite();
            image.type = Image.Type.Sliced;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }

        public static Sprite GetSprite()
        {
            if (pillSprite != null) return pillSprite;

            const int width = 128;
            const int height = 48;
            const float radius = height * 0.5f;
            const float borderWidth = 2.5f;
            const float fillAlpha = 0.24f;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "RuntimeSharedPillTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float centerX = x < radius ? radius : x > width - radius ? width - radius : x;
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, radius));
                    float outerAlpha = Mathf.Clamp01(radius - distance + 0.5f);
                    float borderMask = Mathf.Clamp01(distance - (radius - borderWidth) + 0.5f);
                    float alpha = outerAlpha * Mathf.Lerp(fillAlpha, 1f, borderMask);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            pillSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            pillSprite.name = "RuntimeSharedPillSprite";
            return pillSprite;
        }

        public static Text CreateLabel(Transform parent, string text, int fontSize = 20)
        {
            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }
    }
}
