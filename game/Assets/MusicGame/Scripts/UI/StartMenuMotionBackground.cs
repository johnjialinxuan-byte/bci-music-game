using UnityEngine;
using UnityEngine.UI;

namespace MusicGame.UI
{
    [ExecuteAlways]
    public class StartMenuMotionBackground : MonoBehaviour
    {
        [SerializeField] private int waveCount = 5;
        [SerializeField] private int waveSamples = 72;
        [SerializeField] private int circleCount = 4;
        [SerializeField] private int circleSamples = 160;
        [SerializeField] private float speed = 0.24f;
        [SerializeField] private Color cyan = new Color(0.10f, 0.92f, 1f, 0.58f);
        [SerializeField] private Color blue = new Color(0.10f, 0.46f, 1f, 0.44f);
        [SerializeField] private Color violet = new Color(0.62f, 0.30f, 1f, 0.48f);

        private const string LayerName = "StartMenuMotionBackground";

        private RectTransform layer;
        private StartMenuMotionGraphic graphic;
        private int desiredSiblingIndex = 1;

        private void OnEnable()
        {
            EnsureBuilt();
        }

        private void Start()
        {
            EnsureBuilt();
        }

        private void OnValidate()
        {
            if (graphic != null)
                ConfigureGraphic();
        }

        private void Update()
        {
            EnsureBuilt();
            if (graphic == null) return;

            graphic.MotionTime = (Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup) * speed;
            graphic.SetVerticesDirty();
        }

        public void SetLayerSiblingIndex(int siblingIndex)
        {
            desiredSiblingIndex = Mathf.Max(0, siblingIndex);
            ApplySiblingIndex();
        }

        public void EnsureBuilt()
        {
            if (layer != null && graphic != null)
            {
                EnsureCanvasRenderer(graphic.gameObject);
                return;
            }

            Build();
        }

        private void Build()
        {
            Transform oldLayer = transform.Find(LayerName);
            if (oldLayer != null)
                DestroyLayer(oldLayer.gameObject);

            GameObject layerObject = new GameObject(LayerName);
            layerObject.transform.SetParent(transform, false);
            layer = layerObject.AddComponent<RectTransform>();
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            layer.pivot = new Vector2(0.5f, 0.5f);

            EnsureCanvasRenderer(layerObject);
            graphic = layerObject.AddComponent<StartMenuMotionGraphic>();
            graphic.raycastTarget = false;
            graphic.material = Graphic.defaultGraphicMaterial;
            ConfigureGraphic();
            ApplySiblingIndex();
        }

        private void ConfigureGraphic()
        {
            graphic.Configure(
                waveCount,
                waveSamples,
                circleCount,
                circleSamples,
                cyan,
                blue,
                violet);
        }

        private void ApplySiblingIndex()
        {
            if (layer == null || layer.parent == null) return;

            int maxIndex = Mathf.Max(0, layer.parent.childCount - 1);
            layer.SetSiblingIndex(Mathf.Clamp(desiredSiblingIndex, 0, maxIndex));
        }

        private static void DestroyLayer(GameObject target)
        {
            if (target == null) return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private static void EnsureCanvasRenderer(GameObject target)
        {
            if (target != null && target.GetComponent<CanvasRenderer>() == null)
                target.AddComponent<CanvasRenderer>();
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class StartMenuMotionGraphic : MaskableGraphic
    {
        private const int NodeCount = 4;
        private static readonly Vector2[][] BrokenCircleRanges =
        {
            new[]
            {
                new Vector2(0.015f, 0.085f),
                new Vector2(0.137f, 0.244f),
                new Vector2(0.318f, 0.366f),
                new Vector2(0.421f, 0.563f),
                new Vector2(0.642f, 0.713f),
                new Vector2(0.781f, 0.917f)
            },
            new[]
            {
                new Vector2(0.035f, 0.126f),
                new Vector2(0.203f, 0.257f),
                new Vector2(0.334f, 0.476f),
                new Vector2(0.536f, 0.603f),
                new Vector2(0.688f, 0.734f),
                new Vector2(0.812f, 0.963f)
            }
        };

        private int waveCount = 5;
        private int waveSamples = 72;
        private int circleCount = 4;
        private int circleSamples = 160;
        private Color cyan = new Color(0.10f, 0.92f, 1f, 0.58f);
        private Color blue = new Color(0.10f, 0.46f, 1f, 0.44f);
        private Color violet = new Color(0.62f, 0.30f, 1f, 0.48f);

        public float MotionTime { get; set; }

        public void Configure(int waves, int waveSampleCount, int circles, int circleSampleCount, Color cyanColor, Color blueColor, Color violetColor)
        {
            waveCount = Mathf.Max(1, waves);
            waveSamples = Mathf.Max(16, waveSampleCount);
            circleCount = Mathf.Max(1, circles);
            circleSamples = Mathf.Max(48, circleSampleCount);
            cyan = cyanColor;
            blue = blueColor;
            violet = violetColor;
            color = Color.white;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float width = Mathf.Max(1f, rect.width);
            float height = Mathf.Max(1f, rect.height);

            DrawCircles(vh, width, height);
            DrawWaves(vh, width, height);
            DrawNodes(vh, width, height);
        }

        private void DrawWaves(VertexHelper vh, float width, float height)
        {
            for (int wave = 0; wave < waveCount; wave++)
            {
                float depth = wave / Mathf.Max(1f, waveCount - 1f);
                float lane = wave - (waveCount - 1f) * 0.5f;
                float baseY = -height * 0.055f + lane * height * 0.024f;
                float phase = MotionTime * (0.72f + wave * 0.025f) + wave * 0.32f;
                float depthThickness = Mathf.Lerp(1.18f, 0.52f, depth);
                float depthAlpha = Mathf.Lerp(1f, 0.44f, depth);
                int samples = Mathf.Max(16, waveSamples);

                Vector2[] points = new Vector2[samples + 1];
                float[] widths = new float[samples + 1];
                Color[] colors = new Color[samples + 1];
                Color startColor = GetWaveColor(wave, 0f);
                Color endColor = GetWaveColor(wave, 1f);

                for (int i = 0; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    Vector2 point = EvaluateWavePoint(t, width, height, baseY, phase, wave);
                    points[i] = point;
                    widths[i] = Mathf.Lerp(8.5f, 2.3f, t) * depthThickness;

                    Color pointColor = Color.Lerp(startColor, endColor, t);
                    pointColor.a *= depthAlpha * WaveFade(t) * LeftFade(point.x, width);
                    colors[i] = ClampAlpha(pointColor);
                }

                AddPolyline(vh, points, widths, colors, false);
            }
        }

        private Vector2 EvaluateWavePoint(float t, float width, float height, float baseY, float phase, int wave)
        {
            float x = Mathf.Lerp(width * 0.52f, -width * 0.05f, t);
            float ampA = height * 0.060f;
            float ampB = height * 0.013f;
            float y = baseY
                + Mathf.Sin((t * 1.42f + phase) * Mathf.PI * 2f) * ampA
                + Mathf.Sin((t * 2.7f - phase * 0.42f + wave * 0.18f) * Mathf.PI * 2f) * ampB;
            return new Vector2(x, y);
        }

        private void DrawCircles(VertexHelper vh, float width, float height)
        {
            Vector2 center = new Vector2(width * 0.34f, height * 0.04f);

            for (int circle = 0; circle < circleCount; circle++)
            {
                bool solid = circle < 2;
                float depth = circle / Mathf.Max(1f, circleCount - 1f);
                float radius = width * (0.155f + circle * 0.066f);
                float rotation = MotionTime * (0.12f + circle * 0.026f) * (circle % 2 == 0 ? 1f : -1f);
                float thickness = GetCircleThickness(circle, depth);
                Color circleColor = Color.Lerp(blue, violet, depth);
                circleColor.a *= Mathf.Lerp(0.96f, 0.48f, depth);

                if (solid)
                {
                    DrawCircleLoop(vh, center, radius, rotation, circle, thickness, circleColor, width);
                }
                else
                {
                    DrawCircleArcs(vh, center, radius, rotation, circle, thickness, circleColor, width);
                }
            }
        }

        private void DrawCircleLoop(VertexHelper vh, Vector2 center, float radius, float rotation, int circle, float thickness, Color circleColor, float width)
        {
            int samples = Mathf.Max(48, circleSamples);
            Vector2[] points = new Vector2[samples];
            float[] widths = new float[samples];
            Color[] colors = new Color[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples + rotation;
                Vector2 point = EvaluateCirclePoint(center, radius, t, circle);
                points[i] = point;
                widths[i] = thickness;
                Color pointColor = circleColor;
                pointColor.a *= LeftFade(point.x, width);
                colors[i] = ClampAlpha(pointColor);
            }

            AddPolyline(vh, points, widths, colors, true);
        }

        private void DrawCircleArcs(VertexHelper vh, Vector2 center, float radius, float rotation, int circle, float thickness, Color circleColor, float width)
        {
            Vector2[] ranges = GetBrokenCircleRanges(circle);
            int arcCount = ranges.Length;
            int samplesPerArc = 8;

            for (int arc = 0; arc < arcCount; arc++)
            {
                float arcStart = ranges[arc].x + rotation + circle * 0.013f;
                float arcLength = ranges[arc].y - ranges[arc].x;
                Vector2[] points = new Vector2[samplesPerArc + 1];
                float[] widths = new float[samplesPerArc + 1];
                Color[] colors = new Color[samplesPerArc + 1];

                for (int i = 0; i <= samplesPerArc; i++)
                {
                    float t = arcStart + arcLength * i / samplesPerArc;
                    Vector2 point = EvaluateCirclePoint(center, radius, t, circle);
                    points[i] = point;
                    widths[i] = thickness;
                    Color pointColor = circleColor;
                    pointColor.a *= LeftFade(point.x, width);
                    colors[i] = ClampAlpha(pointColor);
                }

                AddPolyline(vh, points, widths, colors, false);
            }
        }

        private static float GetCircleThickness(int circle, float depth)
        {
            float[] thicknesses = { 3.8f, 2.55f, 2.05f, 1.35f };
            if (circle >= 0 && circle < thicknesses.Length)
                return thicknesses[circle];

            return Mathf.Lerp(3.5f, 1.35f, depth);
        }

        private static Vector2[] GetBrokenCircleRanges(int circle)
        {
            int index = Mathf.Clamp(circle - 2, 0, BrokenCircleRanges.Length - 1);
            return BrokenCircleRanges[index];
        }

        private static Vector2 EvaluateCirclePoint(Vector2 center, float radius, float t, int circle)
        {
            float angle = t * Mathf.PI * 2f + circle * 0.17f;
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * 4f + circle * 0.8f) * 0.006f;
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * pulse;
        }

        private void DrawNodes(VertexHelper vh, float width, float height)
        {
            for (int i = 0; i < NodeCount; i++)
            {
                int wave = Mathf.Clamp(i + 1, 0, waveCount - 1);
                float t = 0.14f + i * 0.17f;
                float lane = wave - (waveCount - 1f) * 0.5f;
                float baseY = -height * 0.055f + lane * height * 0.024f;
                float phase = MotionTime * (0.72f + wave * 0.025f) + wave * 0.32f;
                Vector2 center = EvaluateWavePoint(t, width, height, baseY, phase, wave);

                Color nodeColor = Color.Lerp(cyan, violet, i / Mathf.Max(1f, NodeCount - 1f));
                nodeColor.a *= LeftFade(center.x, width) * WaveFade(t) * 1.08f;
                AddDisc(vh, center, Mathf.Lerp(6f, 9.5f, i / Mathf.Max(1f, NodeCount - 1f)), ClampAlpha(nodeColor), 24);
            }
        }

        private Color GetWaveColor(int wave, float t)
        {
            Color[] palette =
            {
                cyan,
                new Color(0.10f, 0.62f, 1f, 0.50f),
                blue,
                new Color(0.42f, 0.26f, 1f, 0.46f),
                violet
            };

            int index = Mathf.Abs(wave) % palette.Length;
            int next = (index + 1) % palette.Length;
            return Color.Lerp(palette[index], palette[next], t);
        }

        private static void AddPolyline(VertexHelper vh, Vector2[] points, float[] widths, Color[] colors, bool closed)
        {
            int count = points.Length;
            if (count < 2) return;

            int startIndex = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 tangent;
                if (closed)
                {
                    Vector2 previous = points[(i - 1 + count) % count];
                    Vector2 next = points[(i + 1) % count];
                    tangent = (next - previous).normalized;
                }
                else if (i == 0)
                {
                    tangent = (points[1] - points[0]).normalized;
                }
                else if (i == count - 1)
                {
                    tangent = (points[count - 1] - points[count - 2]).normalized;
                }
                else
                {
                    tangent = (points[i + 1] - points[i - 1]).normalized;
                }

                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = Vector2.right;

                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                Vector2 offset = normal * widths[i] * 0.5f;
                vh.AddVert(CreateVertex(points[i] - offset, colors[i]));
                vh.AddVert(CreateVertex(points[i] + offset, colors[i]));
            }

            int segmentCount = closed ? count : count - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                int next = (i + 1) % count;
                int a = startIndex + i * 2;
                int b = startIndex + i * 2 + 1;
                int c = startIndex + next * 2;
                int d = startIndex + next * 2 + 1;
                vh.AddTriangle(a, b, d);
                vh.AddTriangle(a, d, c);
            }
        }

        private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color color, int segments)
        {
            if (color.a <= 0f) return;

            int startIndex = vh.currentVertCount;
            vh.AddVert(CreateVertex(center, color));
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Color edgeColor = color;
                edgeColor.a *= 0.16f;
                vh.AddVert(CreateVertex(point, edgeColor));
            }

            for (int i = 1; i <= segments; i++)
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
        }

        private static float WaveFade(float t)
        {
            return 1f - Smooth01(Mathf.InverseLerp(0.50f, 0.74f, t));
        }

        private static float LeftFade(float x, float width)
        {
            return Smooth01(Mathf.InverseLerp(-width * 0.02f, width * 0.20f, x));
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Color ClampAlpha(Color value)
        {
            value.a = Mathf.Clamp01(value.a);
            return value;
        }

        private static UIVertex CreateVertex(Vector2 position, Color vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            return vertex;
        }
    }
}
