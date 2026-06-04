using UnityEngine;
using UnityEngine.EventSystems;

namespace Scorewriter
{
    public sealed class ScorewriterTimelineInput : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ScorewriterApp app;

        public void Bind(ScorewriterApp owner)
        {
            app = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            app?.HandleTimelineClick(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            app?.BeginTimelineDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            app?.HandleTimelineDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            app?.EndTimelineDrag(eventData);
        }
    }
}
