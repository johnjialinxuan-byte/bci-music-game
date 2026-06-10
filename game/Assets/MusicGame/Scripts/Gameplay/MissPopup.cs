using UnityEngine;

namespace MusicGame.Gameplay
{
    /// <summary>
    /// Self-contained world-space "MISS" popup: spawns at the failed note's
    /// judge position, punches in, floats up and fades out. No prefab/canvas
    /// needed — safe to call from any note script.
    /// </summary>
    public class MissPopup : MonoBehaviour
    {
        private const float Lifetime = 0.6f;
        private const float FloatSpeed = 1.6f;
        private static Font cachedFont;

        private TextMesh textMesh;
        private float timer;
        private Vector3 baseScale;

        public static void Show(Vector3 worldPosition)
        {
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                return;

            GameObject popupObject = new GameObject("MissPopup");
            popupObject.transform.position = worldPosition;

            TextMesh text = popupObject.AddComponent<TextMesh>();
            text.text = "MISS";
            text.font = cachedFont;
            text.fontSize = 72;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = 0.045f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.22f, 0.28f, 1f);

            MeshRenderer meshRenderer = popupObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = cachedFont.material;
            meshRenderer.sortingOrder = 50;

            popupObject.AddComponent<MissPopup>();
        }

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = timer / Lifetime;
            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (FloatSpeed * Time.deltaTime);

            // Quick punch-in at spawn, then fade out over the back half.
            float punch = 1f + 0.45f * Mathf.Clamp01(1f - progress * 4f);
            transform.localScale = baseScale * punch;

            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) * 2f);
                textMesh.color = color;
            }
        }
    }
}
