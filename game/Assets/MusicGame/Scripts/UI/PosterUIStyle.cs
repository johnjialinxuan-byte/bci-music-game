using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public static class PosterUIStyle
    {
        public static readonly Color Ink = new Color32(0x11, 0x11, 0x11, 0xFF);
        public static readonly Color Paper = new Color32(0xF1, 0xEF, 0xE7, 0xFF);
        public static readonly Color Blue = new Color32(0x12, 0x28, 0xE8, 0xFF);
        public static readonly Color Red = new Color32(0xE7, 0x3E, 0x4B, 0xFF);
        public static readonly Color Slate = new Color32(0x5E, 0x66, 0x72, 0xFF);

        private static Sprite paperSprite;
        private static Sprite stripSprite;
        private static Sprite solidSprite;

        public static void ApplyCanvasBackdrop(Canvas canvas, string key)
        {
            if (canvas == null) return;

            RectTransform background = EnsureLayer(canvas.transform, "PosterPaper_" + key, 0);
            Image backgroundImage = EnsureImage(background.gameObject);
            backgroundImage.sprite = GetPaperSprite();
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            Stretch(background);
            background.SetAsFirstSibling();

            RectTransform grid = EnsureLayer(canvas.transform, "PosterGrid_" + key, 1);
            ClearChildren(grid);
            Stretch(grid);
            grid.SetSiblingIndex(Mathf.Min(1, canvas.transform.childCount - 1));
            CreateGrid(grid);

            RectTransform scan = EnsureLayer(canvas.transform, "PosterScanlines_" + key, 2);
            ClearChildren(scan);
            Stretch(scan);
            scan.SetSiblingIndex(Mathf.Min(2, canvas.transform.childCount - 1));
            CreateScanlines(scan);
        }

        public static void StyleTitle(Text text, string value, int size, Vector2 position, Color accent)
        {
            if (text == null) return;

            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Ink;
            text.rectTransform.anchoredPosition = position;
            UIThemeFont.Apply(text);

            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(3f, -2f);

            Shadow shadow = text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
            shadow.effectDistance = new Vector2(-2f, 2f);
        }

        public static void StyleText(Text text, int size, TextAnchor alignment, Color color, bool bold = false)
        {
            if (text == null) return;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = alignment;
            text.color = color;
            UIThemeFont.Apply(text);
        }

        public static void ApplyPosterButton(Button button, Color accent, bool filled = false)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.sprite = GetStripSprite();
            image.type = Image.Type.Sliced;
            image.color = filled ? accent : new Color(1f, 1f, 1f, 0.68f);
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = filled ? Color.Lerp(accent, Color.white, 0.16f) : new Color(accent.r, accent.g, accent.b, 0.24f);
            colors.pressedColor = filled ? Color.Lerp(accent, Ink, 0.22f) : new Color(accent.r, accent.g, accent.b, 0.40f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = filled ? Color.white : Ink;
                label.fontStyle = FontStyle.Bold;
                UIThemeFont.Apply(label);
            }
        }

public static Sprite GetStripSprite()
        {
            if (stripSprite != null) return stripSprite;

            const int width = 128;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "PosterStripTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool border = x < 3 || x >= width - 3 || y < 3 || y >= height - 3;
                    pixels[y * width + x] = border ? Ink : new Color(1f, 1f, 1f, 0.72f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            stripSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6f, 6f, 6f, 6f));
            stripSprite.name = "PosterStripSprite";
            return stripSprite;
        }

        public static Sprite GetSolidSprite()
        {
            if (solidSprite != null) return solidSprite;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "PosterSolidTexture" };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            return solidSprite;
        }

        private static Sprite GetPaperSprite()
        {
            if (paperSprite != null) return paperSprite;

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PosterPaperTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int n = (x * 37 + y * 73 + x * y * 3) & 31;
                    float grain = (n - 16) / 255f;
                    Color color = Paper + new Color(grain, grain, grain, 0f);
                    if (((x * 19 + y * 11) & 127) == 0) color = Color.Lerp(color, Blue, 0.16f);
                    if (((x * 13 + y * 23) & 151) == 0) color = Color.Lerp(color, Red, 0.12f);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            paperSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            paperSprite.name = "PosterPaperSprite";
            return paperSprite;
        }

        private static RectTransform EnsureLayer(Transform parent, string name, int sibling)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetSiblingIndex(Mathf.Clamp(sibling, 0, parent.childCount - 1));
            return rect;
        }

        private static Image EnsureImage(GameObject obj)
        {
            Image image = obj.GetComponent<Image>();
            if (image == null) image = obj.AddComponent<Image>();
            return image;
        }

        private static void CreateGrid(RectTransform parent)
        {
            for (int i = -5; i <= 5; i++)
            {
                CreateLine(parent, "V" + i, new Vector2(i * 145f, 0f), new Vector2(1.5f, 940f), new Color(Red.r, Red.g, Red.b, 0.23f));
            }

            for (int i = -3; i <= 3; i++)
            {
                CreateLine(parent, "H" + i, new Vector2(0f, i * 112f), new Vector2(1600f, 1.5f), new Color(Blue.r, Blue.g, Blue.b, 0.15f));
            }

            CreateLine(parent, "TopRule", new Vector2(0f, 408f), new Vector2(1550f, 22f), new Color(0f, 0f, 0f, 0.78f));
            CreateLine(parent, "LeftRule", new Vector2(-760f, 0f), new Vector2(22f, 820f), new Color(0f, 0f, 0f, 0.70f));
            CreateLine(parent, "RedDisk", new Vector2(565f, 180f), new Vector2(174f, 174f), new Color(Red.r, Red.g, Red.b, 0.82f));
        }

        private static void CreateScanlines(RectTransform parent)
        {
            for (int i = -24; i <= 24; i++)
            {
                CreateLine(parent, "Scan" + i, new Vector2(0f, i * 18f), new Vector2(1650f, 1f), new Color(0f, 0f, 0f, 0.045f));
            }
        }

        private static void CreateLine(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
