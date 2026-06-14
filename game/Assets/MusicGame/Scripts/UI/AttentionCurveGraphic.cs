using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MusicGame.Core;

namespace MusicGame.UI
{
    public class AttentionCurveGraphic : MaskableGraphic
    {
        [SerializeField] private float lineWidth = 5.5f;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.22f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPlotRect(GetPixelAdjustedRect());
            DrawGrid(vh, rect);

            IReadOnlyList<AttentionSample> samples = AttentionRunRecorder.Samples;
            if (samples == null || samples.Count == 0)
            {
                DrawFlatZeroLine(vh, rect);
                return;
            }

            bool allZero = true;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].Value > 0.001f)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero)
            {
                DrawFlatZeroLine(vh, rect);
                return;
            }

            if (samples.Count == 1)
            {
                Vector2 single = SampleToPoint(samples[0], rect, 1f);
                AddLine(vh, new Vector2(rect.xMin, single.y), new Vector2(rect.xMax, single.y), lineWidth, GetSegmentColor(samples[0].Value));
                return;
            }

            float duration = Mathf.Max(0.001f, AttentionRunRecorder.Duration);
            Vector2 previous = SampleToPoint(samples[0], rect, duration);
            for (int i = 1; i < samples.Count; i++)
            {
                Vector2 current = SampleToPoint(samples[i], rect, duration);
                float segmentValue = (samples[i - 1].Value + samples[i].Value) * 0.5f;
                AddLine(vh, previous, current, lineWidth, GetSegmentColor(segmentValue));
                previous = current;
            }
        }

        private static Rect GetPlotRect(Rect rect)
        {
            const float padding = 10f;
            return new Rect(
                rect.xMin + padding,
                rect.yMin + padding,
                Mathf.Max(1f, rect.width - padding * 2f),
                Mathf.Max(1f, rect.height - padding * 2f));
        }

        private static Vector2 SampleToPoint(AttentionSample sample, Rect rect, float duration)
        {
                        float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(sample.Time / duration));
            float normalizedY = Mathf.Clamp01(sample.Value / 100f);
            if (normalizedY <= 0.001f)
                normalizedY = 0.015f;
            float y = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY);
            return new Vector2(x, y);
        }

        private void DrawGrid(VertexHelper vh, Rect rect)
        {
            const int horizontalLines = 5;
            const int verticalLines = 4;
            Color axisColor = new Color(1f, 1f, 1f, 0.62f);
            Color borderColor = new Color(1f, 1f, 1f, 0.40f);

            for (int i = 0; i <= horizontalLines; i++)
            {
                float y = Mathf.Lerp(rect.yMin, rect.yMax, i / (float)horizontalLines);
                AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), i == 0 ? 2.8f : 1.5f, i == 0 ? axisColor : gridColor);
            }

            for (int i = 0; i <= verticalLines; i++)
            {
                float x = Mathf.Lerp(rect.xMin, rect.xMax, i / (float)verticalLines);
                AddLine(vh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), i == 0 ? 2.8f : 1.5f, i == 0 ? axisColor : gridColor);
            }

            AddLine(vh, new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax), 2f, borderColor);
            AddLine(vh, new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), 2f, borderColor);
        }

        private void DrawFlatZeroLine(VertexHelper vh, Rect rect)
        {
            float y = Mathf.Lerp(rect.yMin, rect.yMax, 0.015f);
            AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), lineWidth + 1.5f, GetSegmentColor(0f));
        }

        private static Color GetSegmentColor(float value)
        {
            if (value < 20f)
                return new Color(1f, 0.08f, 0.10f, 1f);
            if (value < 60f)
                return new Color(0.10f, 0.68f, 1f, 1f);
            return new Color(0.12f, 1f, 0.42f, 1f);
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}

