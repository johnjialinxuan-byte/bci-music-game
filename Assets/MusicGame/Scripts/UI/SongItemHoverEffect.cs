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
        private bool isPointerInside;
        private bool isSelected;

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

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisualTarget();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            UpdateVisualTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            UpdateVisualTarget();
        }

        private void UpdateVisualTarget()
        {
            bool emphasized = isPointerInside || isSelected;
            targetScale = Vector3.one * (emphasized ? hoverScale : 1f);
            if (targetGraphic != null)
                targetGraphic.color = emphasized ? hoverColor : normalColor;
        }
    }
}
