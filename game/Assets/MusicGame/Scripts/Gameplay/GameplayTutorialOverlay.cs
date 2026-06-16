using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Audio;
using MusicGame.Core;
using MusicGame.UI;

namespace MusicGame.Gameplay
{
    public class GameplayTutorialOverlay : MonoBehaviour
    {
        private const float FadeDuration = 0.45f;
        private const float DefaultHoldDuration = 2.6f;
        private const int MaxMainLines = 4;


        private Canvas canvas;
        private RectTransform overlayRoot;

        private CanvasGroup mainGroup;
        private Text mainText;
        private readonly Text[] mainLineTexts = new Text[MaxMainLines];
        private readonly CanvasGroup[] mainLineGroups = new CanvasGroup[MaxMainLines];

        private readonly Text[] hudTexts = new Text[3];
        private readonly RectTransform[] hudHighlightRects = new RectTransform[3];
        private readonly CanvasGroup[] hudHighlightGroups = new CanvasGroup[3];


        public void Begin(float firstNoteTime)
        {
            EnsureUI();
            StartCoroutine(Run(firstNoteTime));
        }

private IEnumerator Run(float firstNoteTime)
        {
            SetHudHintsVisible(false);

            yield return ShowMain("亲爱的玩家\n欢迎您来到脑波协奏的世界\n这是一个用于训练玩家注意力的音乐游戏", 5.6f, new Vector2(0f, 28f), 42);
            yield return ShowMain("现在我们开始新手教程", 2.4f, new Vector2(0f, 28f), 46);

            yield return ShowMain("在游戏中，你需要根据音乐节拍来判定上不同的音符", 3.0f, new Vector2(0f, 56f), 36);
            yield return ShowHudHint(0, "这是您的游玩分数\n每判定上一个音符均会加分", new Vector2(70f, 144f), TextAnchor.MiddleRight, new Vector2(720f, 338f), new Vector2(330f, 82f), 3.0f);
            yield return ShowHudHint(1, "这是您的游玩准确率\n用以实时显示您的判定情况", new Vector2(58f, 18f), TextAnchor.MiddleRight, new Vector2(704f, 256f), new Vector2(370f, 88f), 3.0f);
            yield return ShowHudHint(2, "这是您的专注度数值\n初期测试阶段会实时显示您的专注度", new Vector2(-8f, 214f), TextAnchor.MiddleLeft, new Vector2(-806f, 486f), new Vector2(178f, 34f), 3.2f);
            SetHudHintsVisible(false);

            float holdIntroTime = Mathf.Max(0f, firstNoteTime - 2f);
            yield return WaitUntilMusicTime(holdIntroTime);
            yield return ShowMain("现在出现的是 hold 音符，只要你在判定时保持一定的专注度\n专注度达到一定阈值就可以判定上\n建议一直盯着音符看哦，这样可以有效集中注意力", 5.6f, new Vector2(0f, 250f), 34);

            yield return WaitUntilMusicTime(42f);
            yield return ShowMain("hold 音符具有不同的颜色，只要保持一定的专注度即可以判定上", DefaultHoldDuration, new Vector2(0f, 250f), 34);

            yield return WaitUntilMusicTime(48f);
            yield return ShowMain("再来试试吧", 2f, new Vector2(0f, 250f), 40);

            yield return WaitUntilMusicTime(53f);
            yield return ShowMain("接下来是 flick 音符，只需要在音符到达判定面时头朝着音符指示方向转动即可", 4.2f, new Vector2(0f, 250f), 34);

            yield return WaitUntilMusicTime(76f);
            yield return ShowMain("现在你已经学会了脑波协奏中的所有音符，它们会出现在屏幕中的不同位置\n现在综合起来，再来试试吧", 4.4f, new Vector2(0f, 250f), 34);

            yield return WaitUntilMusicTime(149f);
            GameStateManager.Instance?.CompleteTutorial();
            yield return ShowMain("恭喜你，完成了新手教程\n现在开始你的脑波协奏之旅吧！\n另外，你也可以随时在设置里重新开始新手教程", 6.2f, new Vector2(0f, 120f), 40);
        }

private IEnumerator ShowMain(string text, float holdDuration, Vector2 position, int fontSize)
        {
            if (position.y > 200f)
                position.y -= 80f;
            else if (position.y > 80f)
                position.y -= 70f;

            string[] lines = text.Split('\n');
            int lineCount = Mathf.Min(lines.Length, MaxMainLines);
            float lineHeight = Mathf.Clamp(fontSize * 1.45f, 52f, 82f);
            float topOffset = (lineCount - 1) * lineHeight * 0.5f;

            SetMainLinesVisible(false);
            for (int i = 0; i < lineCount; i++)
            {
                Text lineText = mainLineTexts[i];
                CanvasGroup lineGroup = mainLineGroups[i];
                lineText.text = lines[i];
                lineText.fontSize = fontSize;
                lineText.rectTransform.sizeDelta = new Vector2(1280f, lineHeight + 12f);
                lineText.rectTransform.anchoredPosition = new Vector2(position.x, position.y + topOffset - i * lineHeight);
                lineGroup.alpha = 0f;
                lineText.gameObject.SetActive(true);
            }

            float revealTime = 0f;
            for (int i = 0; i < lineCount; i++)
            {
                yield return Fade(mainLineGroups[i], 0f, 1f);
                revealTime += FadeDuration;
                if (i < lineCount - 1)
                {
                    yield return new WaitForSecondsRealtime(0.55f);
                    revealTime += 0.55f;
                }
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.7f, holdDuration - revealTime));
            yield return FadeMainLinesOut(lineCount);
            SetMainLinesVisible(false);
        }

private IEnumerator ShowHudHint(int index, string text, Vector2 textPosition, TextAnchor alignment, Vector2 highlightPosition, Vector2 highlightSize, float holdDuration)
        {
            Text label = hudTexts[index];
            label.text = text;
            label.fontSize = 36;
            label.lineSpacing = 1.18f;
            label.alignment = alignment;
            label.rectTransform.sizeDelta = new Vector2(720f, 150f);
            label.rectTransform.anchoredPosition = textPosition;

            RectTransform highlight = hudHighlightRects[index];
            CanvasGroup highlightGroup = hudHighlightGroups[index];
            if (index == 0)
                ApplyHighlightToText(highlight, "SCORE", new Vector2(18f, 14f));
            else if (index == 1)
                ApplyHighlightToText(highlight, "ACC", new Vector2(18f, 14f));
            else
                ApplyAttentionHighlight(highlight);

            CanvasGroup labelGroup = label.GetComponent<CanvasGroup>();
            labelGroup.alpha = 0f;
            highlightGroup.alpha = 0f;
            label.gameObject.SetActive(true);
            highlight.gameObject.SetActive(true);

            yield return Fade(labelGroup, 0f, 1f);
            yield return Fade(highlightGroup, 0f, 1f);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return Fade(highlightGroup, 1f, 0f);
            yield return Fade(labelGroup, 1f, 0f);
            label.gameObject.SetActive(false);
            highlight.gameObject.SetActive(false);
        }

        private IEnumerator WaitUntilMusicTime(float targetTime)
        {
            while (MusicTime.Current < targetTime)
                yield return null;
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / FadeDuration));
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            group.alpha = to;
        }

private void EnsureUI()
        {
            if (canvas != null) return;

            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            GameObject root = new GameObject("GameplayTutorialOverlay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            overlayRoot = rootRect;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.transform.SetAsLastSibling();

            GameObject textObject = new GameObject("TutorialMainText_0", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
            textObject.transform.SetParent(root.transform, false);
            mainText = textObject.GetComponent<Text>();
            mainGroup = textObject.GetComponent<CanvasGroup>();
            ConfigureText(mainText, new Vector2(1280f, 92f), TextAnchor.MiddleCenter);
            mainLineTexts[0] = mainText;
            mainLineGroups[0] = mainGroup;

            for (int i = 1; i < MaxMainLines; i++)
            {
                GameObject lineObject = new GameObject($"TutorialMainText_{i}", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
                lineObject.transform.SetParent(root.transform, false);
                mainLineTexts[i] = lineObject.GetComponent<Text>();
                mainLineGroups[i] = lineObject.GetComponent<CanvasGroup>();
                ConfigureText(mainLineTexts[i], new Vector2(1280f, 92f), TextAnchor.MiddleCenter);
            }
            SetMainLinesVisible(false);

            for (int i = 0; i < hudTexts.Length; i++)
            {
                GameObject hintObject = new GameObject($"TutorialHudHint_{i}", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
                hintObject.transform.SetParent(root.transform, false);
                hudTexts[i] = hintObject.GetComponent<Text>();
                ConfigureText(hudTexts[i], new Vector2(920f, 150f), TextAnchor.MiddleRight);
                hudTexts[i].fontSize = 36;
                hintObject.SetActive(false);
            }
            for (int i = 0; i < hudHighlightRects.Length; i++)
            {
                GameObject highlightObject = new GameObject($"TutorialHudHighlight_{i}", typeof(RectTransform), typeof(CanvasGroup));
                highlightObject.transform.SetParent(root.transform, false);
                highlightObject.transform.SetAsLastSibling();

                RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
                ConfigureRect(highlightRect, new Vector2(240f, 80f));
                hudHighlightRects[i] = highlightRect;
                hudHighlightGroups[i] = highlightObject.GetComponent<CanvasGroup>();
                CreateHighlightLine(highlightObject.transform, "Top", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -3f), new Vector2(0f, 3f));
                CreateHighlightLine(highlightObject.transform, "Bottom", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -3f), new Vector2(0f, 3f));
                CreateHighlightLine(highlightObject.transform, "Left", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
                CreateHighlightLine(highlightObject.transform, "Right", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
                PositionHighlightLines(highlightRect);
                highlightObject.SetActive(false);
            }

        }

        private static void ConfigureText(Text text, Vector2 size, TextAnchor alignment)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            UIThemeFont.Apply(text);
        }

private static void ConfigureRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }

        private static void CreateHighlightLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(parent, false);
            RectTransform rect = lineObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = lineObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.86f, 1f, 0.95f);
            image.raycastTarget = false;
        }

        private static void PositionHighlightLines(RectTransform highlightRect)
        {
            RectTransform top = highlightRect.Find("Top") as RectTransform;
            RectTransform bottom = highlightRect.Find("Bottom") as RectTransform;
            RectTransform left = highlightRect.Find("Left") as RectTransform;
            RectTransform right = highlightRect.Find("Right") as RectTransform;
            if (top != null)
            {
                top.anchorMin = new Vector2(0f, 1f);
                top.anchorMax = new Vector2(1f, 1f);
                top.offsetMin = new Vector2(0f, -6f);
                top.offsetMax = Vector2.zero;
            }
            if (bottom != null)
            {
                bottom.anchorMin = new Vector2(0f, 0f);
                bottom.anchorMax = new Vector2(1f, 0f);
                bottom.offsetMin = Vector2.zero;
                bottom.offsetMax = new Vector2(0f, 6f);
            }
            if (left != null)
            {
                left.anchorMin = new Vector2(0f, 0f);
                left.anchorMax = new Vector2(0f, 1f);
                left.offsetMin = Vector2.zero;
                left.offsetMax = new Vector2(6f, 0f);
            }
            if (right != null)
            {
                right.anchorMin = new Vector2(1f, 0f);
                right.anchorMax = new Vector2(1f, 1f);
                right.offsetMin = new Vector2(-6f, 0f);
                right.offsetMax = Vector2.zero;
            }
        }


private void SetHudHintsVisible(bool visible)
        {
            for (int i = 0; i < hudTexts.Length; i++)
            {
                if (hudTexts[i] != null)
                    hudTexts[i].gameObject.SetActive(visible);
                if (i < hudHighlightRects.Length && hudHighlightRects[i] != null)
                    hudHighlightRects[i].gameObject.SetActive(visible);
            }
        }
    

private void SetMainLinesVisible(bool visible)
        {
            for (int i = 0; i < mainLineTexts.Length; i++)
            {
                if (mainLineTexts[i] != null)
                    mainLineTexts[i].gameObject.SetActive(visible);
            }
        }


private IEnumerator FadeMainLinesOut(int lineCount)
        {
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / FadeDuration));
                float alpha = Mathf.Lerp(1f, 0f, t);
                for (int i = 0; i < lineCount; i++)
                {
                    if (mainLineGroups[i] != null)
                        mainLineGroups[i].alpha = alpha;
                }
                yield return null;
            }

            for (int i = 0; i < lineCount; i++)
            {
                if (mainLineGroups[i] != null)
                    mainLineGroups[i].alpha = 0f;
            }
        }


private void ApplyHighlightToText(RectTransform highlight, string prefix, Vector2 padding)
        {
            Text target = FindHudText(prefix);
            if (target == null || overlayRoot == null)
                return;

            RectTransform targetRect = target.rectTransform;
            Vector3[] worldCorners = new Vector3[4];
            targetRect.GetWorldCorners(worldCorners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screen, uiCamera, out Vector2 local);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            Vector2 rectSize = max - min;
            float preferredWidth = Mathf.Clamp(target.preferredWidth + padding.x, 48f, rectSize.x + padding.x);
            float preferredHeight = Mathf.Clamp(target.preferredHeight + padding.y, 32f, rectSize.y + padding.y);
            Vector2 center = (min + max) * 0.5f;

            if (target.alignment == TextAnchor.UpperRight || target.alignment == TextAnchor.MiddleRight || target.alignment == TextAnchor.LowerRight)
                center.x = max.x - preferredWidth * 0.5f;
            else if (target.alignment == TextAnchor.UpperLeft || target.alignment == TextAnchor.MiddleLeft || target.alignment == TextAnchor.LowerLeft)
                center.x = min.x + preferredWidth * 0.5f;

            highlight.anchorMin = new Vector2(0.5f, 0.5f);
            highlight.anchorMax = new Vector2(0.5f, 0.5f);
            highlight.pivot = new Vector2(0.5f, 0.5f);
            highlight.anchoredPosition = center;
            highlight.sizeDelta = new Vector2(preferredWidth, preferredHeight);
        }

        private void ApplyAttentionHighlight(RectTransform highlight)
        {
            highlight.anchorMin = new Vector2(0f, 1f);
            highlight.anchorMax = new Vector2(0f, 1f);
            highlight.pivot = new Vector2(0f, 1f);
            highlight.anchoredPosition = new Vector2(8f, -8f);
            highlight.sizeDelta = new Vector2(124f, 28f);
        }

        private Text FindHudText(string prefix)
        {
            if (canvas == null)
                return null;

            Text[] texts = canvas.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text == null || text.transform.IsChildOf(transform))
                    continue;
                string value = text.text != null ? text.text.TrimStart() : string.Empty;
                if (value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return text;
            }
            return null;
        }
}
}
