using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public class SongItemHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private float hoverScale = 1.16f;
        [SerializeField] private float animationSpeed = 12f;
        [SerializeField] private Color hoverColor = new Color(0.18f, 0.95f, 1f, 1f);

        private Vector3 targetScale = Vector3.one;
        private Color normalColor = Color.white;

        public void SetLabel(Text targetLabel)
        {
            label = targetLabel;
            if (label != null)
                normalColor = label.color;
        }

        private void Awake()
        {
            if (label == null)
                label = GetComponentInChildren<Text>(true);
            if (label != null)
                normalColor = label.color;
        }

        private void Update()
        {
            if (label == null) return;
            label.rectTransform.localScale = Vector3.Lerp(
                label.rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = Vector3.one * hoverScale;
            if (label != null)
                label.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
            if (label != null)
                label.color = normalColor;
        }
    }
}
