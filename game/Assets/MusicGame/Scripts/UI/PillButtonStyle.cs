using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public static class PillButtonStyle
    {
        private static Sprite pillSprite;
        public static readonly Color Cyan = PosterUIStyle.Blue;
        public static readonly Color Panel = new Color(1f, 1f, 1f, 0.68f);

public static void Apply(Button button, Color color)
        {
            PosterUIStyle.ApplyPosterButton(button, color, false);
        }

public static Sprite GetSprite()
        {
            return PosterUIStyle.GetStripSprite();
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
            label.color = PosterUIStyle.Ink;
            return label;
        }
    }
}
