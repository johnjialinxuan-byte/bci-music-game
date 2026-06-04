using UnityEngine;
using UnityEngine.EventSystems;

namespace Scorewriter
{
    public sealed class ScorewriterPlayheadHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        private ScorewriterApp app;

        public void Bind(ScorewriterApp owner)
        {
            app = owner;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            app?.HandlePlayheadDrag(eventData);
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            app?.HandlePlayheadDrag(eventData);
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            app?.HandlePlayheadDrag(eventData);
            eventData.Use();
        }
    }
}
