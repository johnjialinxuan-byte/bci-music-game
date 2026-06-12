using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    /// <summary>
    /// Swaps a UI Text for a pre-rendered title sprite (the white pixel-glitch
    /// art in Resources/Images/Titles). The Text stays on the object (disabled)
    /// so existing references keep working; an Image sibling shows the art at
    /// the same rect. Only used on the handful of labels art exists for.
    /// </summary>
    public static class TextArt
    {
        public static void ReplaceWithSprite(Text text, string resourcePath, float scale = 1f)
        {
            if (text == null) return;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null) return; // art missing -> keep the original text

            text.enabled = false;
            Outline outline = text.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            const string artName = "TitleArt";
            Transform existing = text.transform.parent != null ? text.transform.parent.Find(artName + "_" + text.name) : null;
            GameObject artObject = existing != null ? existing.gameObject
                : new GameObject(artName + "_" + text.name, typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(text.transform.parent, false);

            RectTransform src = text.rectTransform;
            RectTransform rect = (RectTransform)artObject.transform;
            rect.anchorMin = src.anchorMin;
            rect.anchorMax = src.anchorMax;
            rect.pivot = src.pivot;
            rect.anchoredPosition = src.anchoredPosition;
            rect.sizeDelta = src.sizeDelta;
            rect.localScale = Vector3.one * scale;

            Image image = artObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
    }
}
