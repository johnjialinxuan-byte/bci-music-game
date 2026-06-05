using UnityEngine;
using UnityEngine.EventSystems;

namespace Scorewriter
{
    public sealed class ScorewriterNoteHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ScorewriterApp app;
        private ScorewriterNote note;
        private bool editsTail;

        public void Bind(ScorewriterApp owner, ScorewriterNote targetNote, bool tailHandle)
        {
            app = owner;
            note = targetNote;
            editsTail = tailHandle;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            app?.SelectNote(note);
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            app?.BeginNoteDrag(note);
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            app?.HandleNoteDrag(note, editsTail, eventData);
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            app?.EndNoteDrag();
            eventData.Use();
        }
    }
}
