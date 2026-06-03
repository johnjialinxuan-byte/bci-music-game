using UnityEngine;
using UnityEngine.EventSystems;

namespace Scorewriter
{
    public sealed class ScorewriterTimelineInput : MonoBehaviour, IPointerClickHandler
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
    }
}
