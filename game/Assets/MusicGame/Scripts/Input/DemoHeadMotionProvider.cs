using UnityEngine;
using UnityEngine.InputSystem;


namespace MusicGame.Input
{
    public class DemoHeadMotionProvider : MonoBehaviour, IHeadMotionProvider
    {
        [Tooltip("鼠标模拟甩头的角速度阈值（rad/s）。与 BCI 数据量纲不同，需独立调节")]
        [SerializeField] private float flickThreshold = 0.25f;

        private Quaternion lastRotation;
        private Vector3 angularVelocity;
        private float lastTime;

        private void Start()
        {
            lastRotation = Quaternion.identity;
            lastTime = Time.time;
        }

        private void Update()
        {
            float deltaTime = Time.time - lastTime;
            if (deltaTime > 0.001f)
            {
                Quaternion currentRotation = GetHeadRotation();
                Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastRotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                angularVelocity = axis * (angle * Mathf.Deg2Rad / deltaTime);
                lastRotation = currentRotation;
                lastTime = Time.time;
            }
        }

public Quaternion GetHeadRotation()
        {
            Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            float yaw = delta.x * 0.02f;
            float pitch = -delta.y * 0.02f;
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        public Vector3 GetAngularVelocity()
        {
            // Game convention: x>0 = head flicking up, y>0 = head flicking right.
            // The raw axis-angle derivative carries Unity pitch, where negative
            // means looking up — so x must be negated or mouse-up reads as Down.
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
