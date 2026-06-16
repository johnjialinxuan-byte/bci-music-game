using UnityEngine;
using UnityEngine.InputSystem;

namespace MusicGame.Input
{
    public class DemoHeadMotionProvider : MonoBehaviour, IHeadMotionProvider
    {
        [Tooltip("Mouse flick threshold in rad/s. Mobile phone sensors are UI-only; gameplay flicks use BCI data.")]
        [SerializeField] private float flickThreshold = 0.25f;

        private Quaternion lastRotation;
        private Vector3 angularVelocity;
        private float lastTime;

        private void Start()
        {
            lastRotation = Quaternion.identity;
            lastTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (Application.isMobilePlatform)
            {
                angularVelocity = Vector3.zero;
                return;
            }

            float deltaTime = Time.unscaledTime - lastTime;
            if (deltaTime <= 0.001f)
                return;

            Quaternion currentRotation = GetHeadRotation();
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            angularVelocity = axis * (angle * Mathf.Deg2Rad / deltaTime);
            lastRotation = currentRotation;
            lastTime = Time.unscaledTime;
        }

        public Quaternion GetHeadRotation()
        {
            if (Application.isMobilePlatform)
                return Quaternion.identity;

            Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            float yaw = delta.x * 0.02f;
            float pitch = -delta.y * 0.02f;
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        public Vector3 GetAngularVelocity()
        {
            if (Application.isMobilePlatform)
                return Vector3.zero;

            return new Vector3(-angularVelocity.x, angularVelocity.y, 0f);
        }

        public bool IsActive()
        {
            return true;
        }

        public float GetFlickThreshold()
        {
            return flickThreshold;
        }
    }
}
