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

        private Material lineMaterial;

        private void Start()
        {
            BuildGuides();
        }

        private void OnValidate()
        {
        }

        private void BuildGuides()
        {
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
                CreateDashedLine($"PerspectiveDash_{i}", nearCorners[i], farCorners[i]);
            }

            CreateDashedLine("CenterVerticalDash", Vector3.down * height * 0.5f, Vector3.up * height * 0.5f);
            CreateDashedLine("CenterHorizontalDash", Vector3.left * width * 0.5f, Vector3.right * width * 0.5f);
        }

        private void CreateDashedLine(string lineName, Vector3 start, Vector3 end)
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
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
