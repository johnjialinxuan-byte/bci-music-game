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
            PlayFrom(target, color, null);
        }

public static void PlayFrom(GameScene target, Color color, RectTransform source)
        {
            GameObject go = new GameObject("ScreenWipe");
            DontDestroyOnLoad(go);
            ScreenWipe wipe = go.AddComponent<ScreenWipe>();
            wipe.Begin(target, color, source);
        }


        private const float ExpandDuration = 0.38f;
        private const float HoldDuration = 0.10f;
        private const float RevealDuration = 0.58f;

        private RectTransform panel;
        private Image image;

private void Begin(GameScene target, Color color, RectTransform source)
        {
            var canvasObject = new GameObject("WipeCanvas", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            var panelObject = new GameObject("WipePanel", typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            panel = image.rectTransform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;

            Rect startRect = GetStartRect(source);
            SetPanelRect(startRect.xMin, startRect.xMax, startRect.yMin, startRect.yMax);

            StartCoroutine(Run(target, startRect));
        }

private IEnumerator Run(GameScene target, Rect startRect)
        {
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            float firstLeft = Mathf.Clamp(startRect.xMin, 0f, screenWidth);
            float firstStartRight = Mathf.Clamp(startRect.xMax, firstLeft, screenWidth);
            float firstStartBottom = Mathf.Clamp(startRect.yMin, 0f, screenHeight);
            float firstStartTop = Mathf.Clamp(startRect.yMax, firstStartBottom, screenHeight);

            float t = 0f;
            while (t < ExpandDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ExpandDuration));
                SetPanelRect(
                    firstLeft,
                    Mathf.Lerp(firstStartRight, screenWidth, k),
                    Mathf.Lerp(firstStartBottom, 0f, k),
                    Mathf.Lerp(firstStartTop, screenHeight, k));
                yield return null;
            }
            SetPanelRect(firstLeft, screenWidth, 0f, screenHeight);

            t = 0f;
            while (t < ExpandDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ExpandDuration));
                SetPanelRect(Mathf.Lerp(firstLeft, 0f, k), screenWidth, 0f, screenHeight);
                yield return null;
            }
            SetPanelRect(0f, screenWidth, 0f, screenHeight);

            yield return new WaitForSecondsRealtime(HoldDuration);
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.ChangeScene(target);

            yield return WaitForSceneVisualsReady(target);

            t = 0f;
            while (t < RevealDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / RevealDuration));
                SetPanelRect(Mathf.Lerp(0f, -screenWidth, k), Mathf.Lerp(screenWidth, 0f, k), 0f, screenHeight);
                yield return null;
            }

            Destroy(gameObject);
        }

private static Rect GetStartRect(RectTransform source)
        {
            if (source == null)
                return new Rect(Screen.width, 0f, 0f, Screen.height);

            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            Camera camera = null;
            Canvas canvas = source.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                minX = Mathf.Min(minX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxX = Mathf.Max(maxX, screen.x);
                maxY = Mathf.Max(maxY, screen.y);
            }

            minX = Mathf.Clamp(minX, 0f, Screen.width);
            maxX = Mathf.Clamp(maxX, minX, Screen.width);
            minY = Mathf.Clamp(minY, 0f, Screen.height);
            maxY = Mathf.Clamp(maxY, minY, Screen.height);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private void SetPanelRect(float left, float right, float bottom, float top)
        {
            panel.anchoredPosition = new Vector2(left, bottom);
            panel.sizeDelta = new Vector2(Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom));
        }

    

private static IEnumerator WaitForSceneVisualsReady(GameScene target)
        {
            if (target != GameScene.Gameplay)
            {
                yield return null;
                yield break;
            }

            for (int i = 0; i < 12; i++)
            {
                Canvas.ForceUpdateCanvases();
                SciFiCurveBackground background = Object.FindAnyObjectByType<SciFiCurveBackground>();
                if (background != null && background.IsBuilt)
                    yield break;

                yield return null;
            }
        }
}
}
