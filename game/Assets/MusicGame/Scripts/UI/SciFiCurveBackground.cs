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
        [SerializeField] private bool perspectiveFlow;

        private RectTransform layer;
        private RectTransform sideBlockLayer;
        private Image[,] segmentImages;
        private RectTransform[,] segmentRects;
        private Image[] sideBlockImages;
        private RectTransform[] sideBlockRects;
        private const int SideBlockCountPerSide = 18;
        

        private void Start()
        {
            BuildCurves();
        }

        private void Update()
        {
            if (segmentRects == null) return;

            float time = Time.unscaledTime * speed;
            if (perspectiveFlow)
            {
                UpdatePerspectiveFlow(time);
                return;
            }

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

            GameObject blockLayerObject = new GameObject("PerspectiveSideBlocks");
            blockLayerObject.transform.SetParent(transform, false);
            blockLayerObject.transform.SetAsFirstSibling();
            sideBlockLayer = blockLayerObject.AddComponent<RectTransform>();
            sideBlockLayer.anchorMin = Vector2.zero;
            sideBlockLayer.anchorMax = Vector2.one;
            sideBlockLayer.offsetMin = Vector2.zero;
            sideBlockLayer.offsetMax = Vector2.zero;
            blockLayerObject.SetActive(perspectiveFlow);

            segmentImages = new Image[curveCount, segmentsPerCurve];
            segmentRects = new RectTransform[curveCount, segmentsPerCurve];
            sideBlockImages = new Image[SideBlockCountPerSide * 2];
            sideBlockRects = new RectTransform[SideBlockCountPerSide * 2];

            for (int index = 0; index < sideBlockImages.Length; index++)
            {
                GameObject blockObject = new GameObject($"SideBlock_{index:00}");
                blockObject.transform.SetParent(sideBlockLayer, false);
                Image image = blockObject.AddComponent<Image>();
                image.raycastTarget = false;
                image.color = Color.clear;

                RectTransform rect = blockObject.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);
                sideBlockImages[index] = image;
                sideBlockRects[index] = rect;
            }

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
    

public void Configure(int curves, int segments, float canvasWidth, float canvasHeight, float waveAmplitude, float strokeWidth, float motionSpeed, Color primary, Color secondary)
        {
            curveCount = curves;
            segmentsPerCurve = segments;
            width = canvasWidth;
            height = canvasHeight;
            amplitude = waveAmplitude;
            lineWidth = strokeWidth;
            speed = motionSpeed;
            primaryColor = primary;
            secondaryColor = secondary;
            perspectiveFlow = false;
            if (sideBlockLayer != null)
                sideBlockLayer.gameObject.SetActive(false);
        }

public void ConfigurePerspectiveFlow(int curves, int segments, float canvasWidth, float canvasHeight, float waveAmplitude, float strokeWidth, float motionSpeed, Color primary, Color secondary)
        {
            Configure(curves, segments, canvasWidth, canvasHeight, waveAmplitude, strokeWidth, motionSpeed, primary, secondary);
            perspectiveFlow = true;
            if (sideBlockLayer != null)
                sideBlockLayer.gameObject.SetActive(true);
        }


private void UpdatePerspectiveFlow(float time)
        {
            int lanesPerCorner = Mathf.Max(1, Mathf.CeilToInt(curveCount / 4f));
            for (int curve = 0; curve < curveCount; curve++)
            {
                int ray = curve % 4;
                int laneIndex = (curve / 4) % lanesPerCorner;
                float lane = lanesPerCorner <= 1 ? 0f : Mathf.Lerp(-0.07f, 0.07f, laneIndex / Mathf.Max(1f, lanesPerCorner - 1f));

                // Mirror the lane offset per corner so matching top/bottom and left/right rays use paired geometry.
                float mirrorSign = ray == 0 || ray == 2 ? 1f : -1f;
                lane *= mirrorSign;

                for (int segment = 0; segment < segmentsPerCurve; segment++)
                {
                    float startT = segment / (float)segmentsPerCurve;
                    float endT = (segment + 1f) / segmentsPerCurve;
                    Vector2 start = EvaluatePerspectivePoint(ray, startT, lane, time);
                    Vector2 end = EvaluatePerspectivePoint(ray, endT, lane, time);
                    PositionPerspectiveSegment(curve, segment, start, end, startT, endT, time);
                }
            }

            UpdatePerspectiveSideBlocks(time);
        }

private Vector2 EvaluatePerspectivePoint(int ray, float t, float lane, float time)
        {
            float nearX = ray == 0 || ray == 3 ? -width * 0.5f : width * 0.5f;
            float nearY = ray == 0 || ray == 1 ? -height * 0.5f : height * 0.5f;
            float farX = nearX * 0.16f;
            float farY = nearY * 0.16f;

            Vector2 near = new Vector2(nearX, nearY);
            Vector2 far = new Vector2(farX, farY);
            Vector2 perpendicular = new Vector2(-near.y, near.x).normalized * lane * Mathf.Lerp(0.06f, 1f, 1f - t) * Mathf.Min(width, height);
            float shimmer = Mathf.Sin((t * 3.5f + time * 0.35f + ray) * Mathf.PI * 2f) * amplitude * 0.08f;
            Vector2 direction = (near - far).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            return Vector2.Lerp(near, far, t) + perpendicular + normal * shimmer;
        }

private void PositionPerspectiveSegment(int curve, int segment, Vector2 start, Vector2 end, float startT, float endT, float time)
        {
            Vector2 delta = end - start;
            RectTransform rect = segmentRects[curve, segment];
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude + 2f, Mathf.Lerp(lineWidth * 1.35f, lineWidth * 0.45f, (startT + endT) * 0.5f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            float depth = Mathf.Clamp01((startT + endT) * 0.5f);
            float nearAmount = 1f - depth;
            Image image = segmentImages[curve, segment];
            Color baseColor = GetPerspectiveColor(curve, depth, time);
            baseColor.a *= Mathf.Lerp(0.62f, 1f, nearAmount);
            image.color = baseColor;
        }


private Color GetPerspectiveColor(int curve, float depth, float time)
        {
            Color cyan = primaryColor;
            Color blue = new Color(0.04f, 0.36f, 1f, primaryColor.a * 0.86f);
            Color violet = secondaryColor;
            Color deepViolet = new Color(0.34f, 0.12f, 1f, secondaryColor.a * 0.92f);

            Color[] palette = { cyan, blue, violet, deepViolet };
            int lanesPerCorner = Mathf.Max(1, Mathf.CeilToInt(curveCount / 4f));
            int laneIndex = (curve / 4) % lanesPerCorner;
            float laneOffset = laneIndex * 0.12f;
            float band = Mathf.Repeat(depth * 0.35f + time * 1.15f + laneOffset, 1f) * palette.Length;
            int index = Mathf.FloorToInt(band) % palette.Length;
            int next = (index + 1) % palette.Length;
            float blend = Mathf.SmoothStep(0f, 1f, band - Mathf.Floor(band));
            return Color.Lerp(palette[index], palette[next], blend);
        }


private void UpdatePerspectiveSideBlocks(float time)
        {
            if (sideBlockLayer == null || sideBlockRects == null || sideBlockImages == null) return;

            sideBlockLayer.gameObject.SetActive(true);
            for (int region = 0; region < 2; region++)
            {
                bool leftSide = region == 0;
                for (int i = 0; i < SideBlockCountPerSide; i++)
                {
                    int index = region * SideBlockCountPerSide + i;
                    if (index >= sideBlockRects.Length) continue;

                    float phase = i / (float)SideBlockCountPerSide;
                    float depth = Mathf.Repeat(phase - time * 0.42f, 1f);
                    float nearAmount = 1f - depth;
                    float crossT = 0.16f + Mathf.Repeat(i * 0.37f, 1f) * 0.68f;

                    Vector2 edgeA = EvaluatePerspectiveStraightPoint(leftSide ? 0 : 1, depth);
                    Vector2 edgeB = EvaluatePerspectiveStraightPoint(leftSide ? 3 : 2, depth);
                    Vector2 center = Vector2.Lerp(edgeA, edgeB, crossT);
                    float inwardPush = Mathf.Lerp(8f, 54f, nearAmount);
                    center.x += leftSide ? inwardPush : -inwardPush;

                    Vector2 toCenter = -center;
                    float rotation = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;
                    float shortSide = Mathf.Lerp(8f, 58f, nearAmount);
                    float longSide = shortSide * Mathf.Lerp(3.6f, 2.9f, nearAmount);

                    RectTransform rect = sideBlockRects[index];
                    rect.anchoredPosition = center;
                    rect.sizeDelta = new Vector2(longSide, shortSide);
                    rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

                    float alpha = Mathf.Lerp(0.014f, 0.092f, nearAmount);
                    sideBlockImages[index].color = GetFixedBlockColor(i + region * 2, alpha);
                }
            }
        }


private Color GetFixedBlockColor(int index, float alpha)
        {
            Color[] palette =
            {
                new Color(0.05f, 0.95f, 1f, alpha),
                new Color(0.04f, 0.36f, 1f, alpha),
                new Color(0.55f, 0.25f, 1f, alpha),
                new Color(0.34f, 0.12f, 1f, alpha),
                new Color(0.08f, 0.78f, 0.92f, alpha),
                new Color(0.42f, 0.18f, 0.95f, alpha)
            };
            return palette[Mathf.Abs(index) % palette.Length];
        }


private Vector2 EvaluatePerspectiveStraightPoint(int ray, float t)
        {
            float nearX = ray == 0 || ray == 3 ? -width * 0.5f : width * 0.5f;
            float nearY = ray == 0 || ray == 1 ? -height * 0.5f : height * 0.5f;
            Vector2 near = new Vector2(nearX, nearY);
            Vector2 far = new Vector2(nearX * 0.16f, nearY * 0.16f);
            return Vector2.Lerp(near, far, t);
        }
}
}
