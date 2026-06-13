using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.Gameplay
{
    public class SpaceGuide : MonoBehaviour
    {
        [SerializeField] private float width = 8f;
        [SerializeField] private float height = 5f;
        [SerializeField] private float depth = 10f;
        [SerializeField] private int dashCount = 12;
        [SerializeField] private float dashFill = 0.48f;
        [SerializeField] private float lineWidth = 0.018f;
        [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.34f);

        [Header("Motion")]
        [SerializeField] private bool animatePerspectiveDashes = true;
        [SerializeField] private float forwardSpeed = 0.34f;
        [SerializeField, Range(0f, 1f)] private float nearBrightnessBoost = 0.65f;

        private Material lineMaterial;
        private readonly List<AnimatedDash> perspectiveDashes = new List<AnimatedDash>();
        private float dashPhase;
        private bool built;

        private void OnEnable()
        {
            EnsureGuides();
        }

        private void Start()
        {
            EnsureGuides();
        }

        private void Update()
        {
            EnsureGuides();
            if (!animatePerspectiveDashes || perspectiveDashes.Count == 0) return;

            dashPhase = Mathf.Repeat(dashPhase + Time.deltaTime * forwardSpeed, 1f);
            UpdatePerspectiveDashes();
        }

        private void OnValidate()
        {
        }

        private void BuildGuides()
        {
            built = false;
            ClearGuides();
            lineMaterial = CreateLineMaterial();

            Vector3[] nearCorners =
            {
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f),
                new Vector3(-width * 0.5f, height * 0.5f, 0f)
            };

            Vector3[] farCorners =
            {
                new Vector3(-width * 0.16f, -height * 0.16f, depth),
                new Vector3(width * 0.16f, -height * 0.16f, depth),
                new Vector3(width * 0.16f, height * 0.16f, depth),
                new Vector3(-width * 0.16f, height * 0.16f, depth)
            };

            for (int i = 0; i < nearCorners.Length; i++)
            {
                CreateDashedLine($"PerspectiveDash_{i:00}", nearCorners[i], farCorners[i], true);
            }

            CreateSolidLine("CenterVerticalLine", Vector3.down * height * 0.5f, Vector3.up * height * 0.5f);
            CreateSolidLine("CenterHorizontalLine", Vector3.left * width * 0.5f, Vector3.right * width * 0.5f);
            UpdatePerspectiveDashes();
            built = true;
        }

        private void CreateDashedLine(string lineName, Vector3 start, Vector3 end, bool animated)
        {
            int count = Mathf.Max(1, dashCount);
            for (int i = 0; i < count; i++)
            {
                float dashStart = i / (float)count;
                float dashEnd = Mathf.Min(1f, dashStart + dashFill / count);
                GameObject dash = new GameObject($"{lineName}_{i:00}");
                dash.transform.SetParent(transform, false);

                LineRenderer renderer = dash.AddComponent<LineRenderer>();
                renderer.sharedMaterial = lineMaterial;
                renderer.useWorldSpace = false;
                renderer.positionCount = 2;
                renderer.startWidth = lineWidth;
                renderer.endWidth = lineWidth;
                renderer.startColor = guideColor;
                renderer.endColor = guideColor;
                renderer.SetPosition(0, Vector3.Lerp(start, end, dashStart));
                renderer.SetPosition(1, Vector3.Lerp(start, end, dashEnd));

                if (animated)
                {
                    perspectiveDashes.Add(new AnimatedDash(renderer, start, end, i, count));
                }
            }
        }

        private void UpdatePerspectiveDashes()
        {
            float segmentLength = Mathf.Clamp01(dashFill) / Mathf.Max(1, dashCount);
            foreach (AnimatedDash dash in perspectiveDashes)
            {
                float dashStart = Mathf.Repeat(dash.Index / (float)dash.Count - dashPhase, 1f);
                float dashEnd = Mathf.Min(1f, dashStart + segmentLength);
                float depth = Mathf.Clamp01((dashStart + dashEnd) * 0.5f);
                float nearAmount = 1f - depth;
                Color animatedColor = guideColor;
                animatedColor.a = Mathf.Clamp01(guideColor.a * (1f + nearAmount * nearBrightnessBoost));

                dash.Renderer.startWidth = Mathf.Lerp(lineWidth * 1.8f, lineWidth * 0.45f, dashStart);
                dash.Renderer.endWidth = Mathf.Lerp(lineWidth * 1.8f, lineWidth * 0.45f, dashEnd);
                dash.Renderer.startColor = animatedColor;
                dash.Renderer.endColor = animatedColor;
                dash.Renderer.SetPosition(0, Vector3.Lerp(dash.Start, dash.End, dashStart));
                dash.Renderer.SetPosition(1, Vector3.Lerp(dash.Start, dash.End, dashEnd));
            }
        }

        private Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                color = guideColor
            };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = 3000;
            return material;
        }

        private void ClearGuides()
        {
            perspectiveDashes.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        public void EnsureGuides()
        {
            if (HasValidGuides())
                return;

            BuildGuides();
        }

        private bool HasValidGuides()
        {
            if (!built || lineMaterial == null || transform.childCount == 0)
                return false;

            if (animatePerspectiveDashes && perspectiveDashes.Count == 0)
                return false;

            for (int i = 0; i < perspectiveDashes.Count; i++)
            {
                if (perspectiveDashes[i].Renderer == null)
                    return false;
            }

            return true;
        }

        private readonly struct AnimatedDash
        {
            public AnimatedDash(LineRenderer renderer, Vector3 start, Vector3 end, int index, int count)
            {
                Renderer = renderer;
                Start = start;
                End = end;
                Index = index;
                Count = count;
            }

            public LineRenderer Renderer { get; }
            public Vector3 Start { get; }
            public Vector3 End { get; }
            public int Index { get; }
            public int Count { get; }
        }
    

private void CreateSolidLine(string lineName, Vector3 start, Vector3 end)
        {
            GameObject line = new GameObject(lineName);
            line.transform.SetParent(transform, false);

            LineRenderer renderer = line.AddComponent<LineRenderer>();
            renderer.sharedMaterial = lineMaterial;
            renderer.useWorldSpace = false;
            renderer.positionCount = 2;
            renderer.startWidth = lineWidth * 2.2f;
            renderer.endWidth = lineWidth * 2.2f;

            Color solidColor = guideColor;
            solidColor.a = Mathf.Min(0.09f, guideColor.a * 0.26f);
            renderer.startColor = solidColor;
            renderer.endColor = solidColor;
            renderer.SetPosition(0, start);
            renderer.SetPosition(1, end);
        }
}
}
