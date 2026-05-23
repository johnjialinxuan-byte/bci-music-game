using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public class SciFiCurveBackground : MonoBehaviour
    {
        [SerializeField] private int curveCount = 5;
        [SerializeField] private int segmentsPerCurve = 28;
        [SerializeField] private float width = 1700f;
        [SerializeField] private float height = 900f;
        [SerializeField] private float amplitude = 76f;
        [SerializeField] private float lineWidth = 3f;
        [SerializeField] private float speed = 0.45f;
        [SerializeField] private Color primaryColor = new Color(0.05f, 0.95f, 1f, 0.28f);
        [SerializeField] private Color secondaryColor = new Color(0.55f, 0.25f, 1f, 0.18f);

        private RectTransform layer;
        private Image[,] segmentImages;
        private RectTransform[,] segmentRects;

        private void Awake()
        {
            BuildCurves();
        }

        private void Update()
        {
            if (segmentRects == null) return;

            float time = Time.unscaledTime * speed;
            for (int curve = 0; curve < curveCount; curve++)
            {
                for (int segment = 0; segment < segmentsPerCurve; segment++)
                {
                    Vector2 start = EvaluatePoint(curve, segment / (float)segmentsPerCurve, time);
                    Vector2 end = EvaluatePoint(curve, (segment + 1f) / segmentsPerCurve, time);
                    PositionSegment(curve, segment, start, end, time);
                }
            }
        }

        private void BuildCurves()
        {
            GameObject layerObject = new GameObject("SciFiCurveBackground");
            layerObject.transform.SetParent(transform, false);
            layerObject.transform.SetAsFirstSibling();
            layer = layerObject.AddComponent<RectTransform>();
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;

            segmentImages = new Image[curveCount, segmentsPerCurve];
            segmentRects = new RectTransform[curveCount, segmentsPerCurve];

            for (int curve = 0; curve < curveCount; curve++)
            {
                for (int segment = 0; segment < segmentsPerCurve; segment++)
                {
                    GameObject segmentObject = new GameObject($"Curve_{curve:00}_Segment_{segment:00}");
                    segmentObject.transform.SetParent(layer, false);

                    Image image = segmentObject.AddComponent<Image>();
                    image.raycastTarget = false;
                    image.color = Color.Lerp(primaryColor, secondaryColor, curve / Mathf.Max(1f, curveCount - 1f));

                    RectTransform rect = segmentObject.GetComponent<RectTransform>();
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    segmentImages[curve, segment] = image;
                    segmentRects[curve, segment] = rect;
                }
            }
        }

        private Vector2 EvaluatePoint(int curve, float t, float time)
        {
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float baseY = Mathf.Lerp(-height * 0.38f, height * 0.38f, curve / Mathf.Max(1f, curveCount - 1f));
            float waveA = Mathf.Sin((t * 2.4f + time + curve * 0.31f) * Mathf.PI * 2f) * amplitude;
            float waveB = Mathf.Sin((t * 5.1f - time * 0.7f + curve * 0.19f) * Mathf.PI * 2f) * amplitude * 0.35f;
            return new Vector2(x, baseY + waveA + waveB);
        }

        private void PositionSegment(int curve, int segment, Vector2 start, Vector2 end, float time)
        {
            Vector2 delta = end - start;
            RectTransform rect = segmentRects[curve, segment];
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, lineWidth);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            float pulse = 0.45f + Mathf.Sin(time * Mathf.PI * 2f + segment * 0.28f + curve) * 0.25f;
            Image image = segmentImages[curve, segment];
            Color color = image.color;
            color.a = Mathf.Clamp01(pulse) * (curve % 2 == 0 ? primaryColor.a : secondaryColor.a);
            image.color = color;
        }
    }
}
