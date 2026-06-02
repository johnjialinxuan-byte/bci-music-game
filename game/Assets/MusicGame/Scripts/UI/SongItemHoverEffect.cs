using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicGame.UI
{
    public class SongItemHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Graphic backgroundGraphic;
        [SerializeField] private float hoverScale = 1.16f;
        [SerializeField] private float animationSpeed = 12f;
        [SerializeField] private Color hoverColor = new Color(0.18f, 0.95f, 1f, 1f);
        [SerializeField] private bool scaleTargetGraphic = true;
        [SerializeField] private bool scaleBackgroundGraphic = true;

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

public void SetScaleTargets(bool scaleTarget, bool scaleBackground)
        {
            scaleTargetGraphic = scaleTarget;
            scaleBackgroundGraphic = scaleBackground;
        }


public void SetBackgroundGraphic(Graphic target)
        {
            backgroundGraphic = target;
        }


public void SetHoverColor(Color color)
        {
            hoverColor = color;
            UpdateVisualTarget();
        }


public void SetHoverScale(float scale)
        {
            hoverScale = scale;
            UpdateVisualTarget();
        }


        private void Awake()
        {
            if (targetGraphic == null)
                targetGraphic = GetComponentInChildren<Text>(true);
            if (targetGraphic != null)
                normalColor = targetGraphic.color;
            if (backgroundGraphic == null)
                backgroundGraphic = GetComponent<Image>();
        }

private void Update()
        {
            if (targetGraphic != null && scaleTargetGraphic)
            {
                targetGraphic.rectTransform.localScale = Vector3.Lerp(
                    targetGraphic.rectTransform.localScale,
                    targetScale,
                    Time.unscaledDeltaTime * animationSpeed);
            }
            else if (targetGraphic != null)
            {
                targetGraphic.rectTransform.localScale = Vector3.one;
            }

            if (backgroundGraphic != null && scaleBackgroundGraphic)
            {
                backgroundGraphic.rectTransform.localScale = Vector3.Lerp(
                    backgroundGraphic.rectTransform.localScale,
                    targetScale,
                    Time.unscaledDeltaTime * animationSpeed);
            }
            else if (backgroundGraphic != null)
            {
                backgroundGraphic.rectTransform.localScale = Vector3.one;
            }
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
            targetScale = Vector3.one * (isPointerInside ? hoverScale : 1f);
            if (targetGraphic != null)
                targetGraphic.color = emphasized ? hoverColor : normalColor;
        }
    }
}
