using UnityEngine;
using UnityEngine.InputSystem;
using MusicGame.Core;
using MusicGame.Notes;
using MusicGame.Managers;
using MusicGame.Audio;

namespace MusicGame.Gameplay
{
    public class MouseClickJudge : MonoBehaviour
    {
        [Header("Judge Settings")]
        [SerializeField] private float judgeRadius = 1.5f;
        [SerializeField] private bool showDebugRay = true;

        private Camera mainCamera;
        private bool wasMouseDown;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            bool isMouseDown = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (isMouseDown && !wasMouseDown)
            {
                TryJudgeNearestNote();
            }
            wasMouseDown = isMouseDown;
        }

        private void TryJudgeNearestNote()
        {
            if (NoteManager.Instance == null) return;
            if (mainCamera == null) return;

            Vector3 mouseWorldPos = GetMouseWorldPositionOnJudgePlane();
            if (mouseWorldPos == Vector3.zero) return;

            // Find nearest active note within radius
            NoteBase nearestNote = FindNearestNoteInRadius(mouseWorldPos, judgeRadius);
            if (nearestNote != null && nearestNote.IsActive)
            {
                float timeDiff = MusicTime.Current - nearestNote.TargetTime;
                if (JudgeManager.Instance.IsInHitWindow(timeDiff))
                {
                    JudgmentType judgment = JudgeManager.Instance.Judge(timeDiff);
                    nearestNote.OnHit(judgment);
                    Debug.Log($"[MouseClickJudge] Hit {nearestNote.Type} note at time diff: {timeDiff:F3}s, judgment: {judgment}");
                }
            }
        }

        private Vector3 GetMouseWorldPositionOnJudgePlane()
        {
            Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
            
            // Intersect with judge plane at Z=0
            float t = -ray.origin.z / ray.direction.z;
            if (t < 0) return Vector3.zero;
            
            return ray.origin + ray.direction * t;
        }

        private NoteBase FindNearestNoteInRadius(Vector3 center, float radius)
        {
            NoteBase nearest = null;
            float nearestDist = float.MaxValue;

            // Access active notes through NoteSpawner
            if (NoteManager.Instance == null) return null;
            
            // Find all active NoteBase in scene
            NoteBase[] allNotes = FindObjectsByType<NoteBase>(FindObjectsInactive.Include);
            foreach (var note in allNotes)
            {
                if (!note.IsActive) continue;
                
                float dist = Vector3.Distance(center, note.transform.position);
                if (dist < radius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = note;
                }
            }

            return nearest;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugRay || !Application.isPlaying) return;
            if (mainCamera == null) return;

            Vector3 mouseWorldPos = GetMouseWorldPositionOnJudgePlane();
            if (mouseWorldPos != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(mouseWorldPos, judgeRadius);
            }
        }
    }
}
