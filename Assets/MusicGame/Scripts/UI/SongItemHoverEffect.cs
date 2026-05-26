using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public class SongItemHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private float hoverScale = 1.16f;
        [SerializeField] private float animationSpeed = 12f;
        [SerializeField] private Color hoverColor = new Color(0.18f, 0.95f, 1f, 1f);

        private Vector3 targetScale = Vector3.one;
        private Color normalColor = Color.white;

        public void SetLabel(Text targetLabel)
        {
            SetGraphic(targetLabel);
        }

        public void SetGraphic(Graphic target)
        {
            targetGraphic = target;
            if (targetGraphic != null)
                normalColor = targetGraphic.color;
        }

        private void Awake()
        {
            if (targetGraphic == null)
                targetGraphic = GetComponentInChildren<Text>(true);
            if (targetGraphic != null)
                normalColor = targetGraphic.color;
        }

        private void Update()
        {
            if (targetGraphic == null) return;
            targetGraphic.rectTransform.localScale = Vector3.Lerp(
                targetGraphic.rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = Vector3.one * hoverScale;
            if (targetGraphic != null)
                targetGraphic.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
            if (targetGraphic != null)
                targetGraphic.color = normalColor;
        }
    }
}
