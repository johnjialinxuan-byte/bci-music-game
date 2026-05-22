using UnityEngine;

namespace MusicGame.Input
{
    public class DemoHeadMotionProvider : MonoBehaviour, IHeadMotionProvider
    {
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
            // Simulated head rotation based on mouse movement for desktop testing
            float yaw = UnityEngine.Input.GetAxis("Mouse X") * 2f;
            float pitch = -UnityEngine.Input.GetAxis("Mouse Y") * 2f;
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        public Vector3 GetAngularVelocity()
        {
            return angularVelocity;
        }

        public bool IsActive()
        {
            return true;
        }
    }
}
