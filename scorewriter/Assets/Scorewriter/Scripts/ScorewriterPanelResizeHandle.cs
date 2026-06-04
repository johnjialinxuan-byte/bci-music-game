using UnityEngine;
using UnityEngine.EventSystems;

namespace Scorewriter
{
    public sealed class ScorewriterPanelResizeHandle : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private ScorewriterApp app;

        public void Bind(ScorewriterApp owner)
        {
            app = owner;
        }

        public void OnDrag(PointerEventData eventData)
        {
            app?.AdjustTimelineWidth(eventData.delta.x);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
