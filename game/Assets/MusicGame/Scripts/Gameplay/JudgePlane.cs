using UnityEngine;

namespace MusicGame.Gameplay
{
    public class JudgePlane : MonoBehaviour
    {
        [SerializeField] private float planeZ = 0f;
        [SerializeField] private float planeWidth = 8f;
        [SerializeField] private float planeHeight = 6f;

        public float PlaneZ => planeZ;
        public Vector2 PlaneSize => new Vector2(planeWidth, planeHeight);

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3(0, 0, planeZ);
            Vector3 size = new Vector3(planeWidth, planeHeight, 0.01f);
            Gizmos.DrawWireCube(center, size);
        }

        public Vector3 GetClosestPointOnPlane(Vector3 worldPos)
        {
            worldPos.z = planeZ;
            return worldPos;
        }
    }
}
