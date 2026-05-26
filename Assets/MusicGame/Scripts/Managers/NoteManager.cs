using UnityEngine;
using MusicGame.Gameplay;
using MusicGame.Core;

namespace MusicGame.Managers
{
    public class NoteManager : MonoBehaviour
    {
        public static NoteManager Instance { get; private set; }

        [SerializeField] private NoteSpawner noteSpawner;

        public bool HasUnspawnedNotes => noteSpawner != null && noteSpawner.HasUnspawnedNotes;
        public bool HasActiveNotes => noteSpawner != null && noteSpawner.ActiveNotes.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void LoadChart(ChartData chart)
        {
            if (noteSpawner != null)
            {
                noteSpawner.LoadChart(chart);
            }
        }

        public void StartSpawning()
        {
            noteSpawner?.StartSpawning();
        }

        public void StopSpawning()
        {
            noteSpawner?.StopSpawning();
        }
    }
}
