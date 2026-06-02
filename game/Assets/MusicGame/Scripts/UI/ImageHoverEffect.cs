using UnityEngine;
using UnityEngine.EventSystems;

namespace MusicGame.UI
{
    public class ImageHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.16f;
        [SerializeField] private float animationSpeed = 12f;

        private Vector3 targetScale = Vector3.one;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (rectTransform == null) return;
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = Vector3.one * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }
    }
}
