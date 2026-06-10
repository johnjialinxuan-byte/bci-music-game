using UnityEngine;

namespace MusicGame.Input
{
    public interface IHeadMotionProvider
    {
        /// <summary>
        /// Returns the current head rotation as a quaternion.
        /// </summary>
        Quaternion GetHeadRotation();

        /// <summary>
        /// Returns the angular velocity vector (delta rotation / time).
        /// </summary>
        Vector3 GetAngularVelocity();

        /// <summary>
        /// Returns true if the provider is currently active/connected.
        /// </summary>
        bool IsActive();

        /// <summary>
        /// Flick trigger threshold for this provider's angular-velocity units.
        /// Demo (mouse axis-angle rad/s) and BCI (pose-offset units) report very
        /// different magnitudes, so a single shared threshold cannot fit both.
        /// </summary>
        float GetFlickThreshold();
    }
}
