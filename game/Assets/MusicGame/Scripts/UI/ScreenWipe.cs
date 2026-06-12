using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.UI
{
    /// <summary>
    /// Full-screen transition: a colored panel expands from the right edge
    /// leftward until it covers the screen, then loads the target scene and
    /// fades away over the new scene. Self-contained — survives the scene load
    /// via DontDestroyOnLoad so there is no flash at the swap.
    /// </summary>
    public sealed class ScreenWipe : MonoBehaviour
    {
        public static void Play(GameScene target, Color color)
        {
            GameObject go = new GameObject("ScreenWipe");
            DontDestroyOnLoad(go);
            ScreenWipe wipe = go.AddComponent<ScreenWipe>();
            wipe.Begin(target, color);
        }

        private const float ExpandDuration = 0.38f;
        private const float HoldDuration = 0.10f;
        private const float FadeDuration = 0.30f;

        private RectTransform panel;
        private Image image;

        private void Begin(GameScene target, Color color)
        {
            var canvasObject = new GameObject("WipeCanvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // above everything

            var panelObject = new GameObject("WipePanel", typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true; // block input during the transition

            panel = image.rectTransform;
            // Anchored to the right edge, full height; width grows toward the left.
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 0.5f);
            panel.offsetMin = new Vector2(0f, 0f);
            panel.offsetMax = new Vector2(0f, 0f);
            panel.sizeDelta = new Vector2(0f, 0f);

            StartCoroutine(Run(target));
        }

        private IEnumerator Run(GameScene target)
        {
            float screenWidth = Screen.width;

            // 1. Expand right -> left to full cover.
            float t = 0f;
            while (t < ExpandDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ExpandDuration));
                panel.sizeDelta = new Vector2(screenWidth * k, 0f);
                yield return null;
            }
            panel.sizeDelta = new Vector2(screenWidth, 0f);

            // 2. Fully covered -> load the next scene behind the panel.
            yield return new WaitForSecondsRealtime(HoldDuration);
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.ChangeScene(target);

            // Let the new scene build a frame before revealing it.
            yield return null;
            yield return null;

            // 3. Fade the cover out over the new scene, then clean up.
            float f = 0f;
            Color start = image.color;
            while (f < FadeDuration)
            {
                f += Time.unscaledDeltaTime;
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, Mathf.Clamp01(f / FadeDuration));
                image.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
