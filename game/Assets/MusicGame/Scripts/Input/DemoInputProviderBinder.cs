using UnityEngine;
using MusicGame.Managers;

namespace MusicGame.Input
{
    public class DemoInputProviderBinder : MonoBehaviour
    {
        private void Start()
        {
            if (InputManager.Instance == null)
            {
                return;
            }

            IHoldValueProvider holdProvider = GetComponent<IHoldValueProvider>();
            IHeadMotionProvider headMotionProvider = GetComponent<IHeadMotionProvider>();

            if (holdProvider != null)
            {
                InputManager.Instance.SetHoldProvider(holdProvider);
            }

            if (headMotionProvider != null)
            {
                InputManager.Instance.SetHeadMotionProvider(headMotionProvider);
            }
        }
    }
}
