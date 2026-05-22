using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private Input.IHoldValueProvider holdProvider;
        private Input.IHeadMotionProvider headMotionProvider;

        public int CurrentHoldValue => holdProvider?.GetHoldValue() ?? 0;
        public Vector3 CurrentAngularVelocity => headMotionProvider?.GetAngularVelocity() ?? Vector3.zero;
        public bool IsHoldProviderActive => holdProvider?.IsActive() ?? false;
        public bool IsHeadMotionProviderActive => headMotionProvider?.IsActive() ?? false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetHoldProvider(Input.IHoldValueProvider provider)
        {
            holdProvider = provider;
        }

        public void SetHeadMotionProvider(Input.IHeadMotionProvider provider)
        {
            headMotionProvider = provider;
        }

        /// <summary>
        /// Detects flick direction based on angular velocity.
        /// </summary>
        public FlickDirection DetectFlickDirection()
        {
            Vector3 angularVel = CurrentAngularVelocity;
            float threshold = 1.5f; // rad/s threshold

            if (Mathf.Abs(angularVel.x) < threshold && Mathf.Abs(angularVel.y) < threshold)
                return (FlickDirection)(-1); // Invalid

            if (Mathf.Abs(angularVel.x) > Mathf.Abs(angularVel.y))
            {
                return angularVel.x > 0 ? FlickDirection.Up : FlickDirection.Down;
            }
            else
            {
                return angularVel.y > 0 ? FlickDirection.Right : FlickDirection.Left;
            }
        }
    }
}
