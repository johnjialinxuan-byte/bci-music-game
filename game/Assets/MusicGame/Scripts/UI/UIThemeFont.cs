using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public static class UIThemeFont
    {
        private static readonly string[] PreferredFamilies =
        {
            "HarmonyOS Sans SC",
            "HarmonyOS Sans",
            "PingFang SC",
            "Heiti SC",
            "Arial Unicode MS"
        };

        private static Font cachedFont;

        public static Font Font
        {
            get
            {
                if (cachedFont != null)
                    return cachedFont;

                cachedFont = Font.CreateDynamicFontFromOSFont(PreferredFamilies, 18);
                if (cachedFont == null)
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return cachedFont;
            }
        }

        public static void Apply(Text text)
        {
            if (text == null) return;

            Font font = Font;
            if (font != null)
                text.font = font;
        }

        public static void ApplyAll(Transform root)
        {
            if (root == null) return;

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
                Apply(text);
        }
    }
}
