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
    }
}
