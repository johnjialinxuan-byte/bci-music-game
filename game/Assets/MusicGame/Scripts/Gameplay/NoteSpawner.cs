using System.Collections.Generic;
using UnityEngine;
using MusicGame.Core;
using MusicGame.Notes;

namespace MusicGame.Gameplay
{
    public class NoteSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private HoldNote holdNotePrefab;
        [SerializeField] private FlickNote flickNotePrefab;

        [Header("Pooling")]
        [SerializeField] private int poolSize = 100;

        private List<NoteBase> notePool = new List<NoteBase>();
        private List<NoteBase> activeNotes = new List<NoteBase>();
        private ChartData currentChart;
        private int nextNoteIndex;
        private float songTime;
        private bool isSpawning;

        public bool HasUnspawnedNotes => currentChart != null && currentChart.notes != null && nextNoteIndex < currentChart.notes.Count;
        public IReadOnlyList<NoteBase> ActiveNotes => activeNotes;

        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            if (holdNotePrefab == null || flickNotePrefab == null)
            {
                Debug.LogError("[NoteSpawner] Prefabs not assigned!");
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                HoldNote hold = Instantiate(holdNotePrefab, transform);
                hold.gameObject.SetActive(false);
                notePool.Add(hold);

                FlickNote flick = Instantiate(flickNotePrefab, transform);
                flick.gameObject.SetActive(false);
                notePool.Add(flick);
            }
        }

        public void LoadChart(ChartData chart)
        {
            currentChart = chart;
            nextNoteIndex = 0;
            activeNotes.Clear();

            // Return all to pool
            foreach (var note in notePool)
            {
                note.gameObject.SetActive(false);
            }
        }

        public void StartSpawning()
        {
            isSpawning = true;
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        private void Update()
        {
            if (!isSpawning || currentChart == null) return;

            songTime = Audio.MusicTime.Current;

            SpawnNotes();
            UpdateActiveNotes();
            CleanupNotes();
        }

        private void SpawnNotes()
        {
            if (currentChart?.notes == null) return;

            while (nextNoteIndex < currentChart.notes.Count)
            {
                NoteData data = currentChart.notes[nextNoteIndex];
                if (data.time - data.approachTime <= songTime)
                {
                    SpawnNote(data);
                    nextNoteIndex++;
                }
                else
                {
                    break;
                }
            }
        }

private void SpawnNote(NoteData data)
        {
            NoteBase note = GetPooledNote(data.noteType);
            if (note == null) return;

            note.gameObject.SetActive(false);
            note.Initialize(data);
            note.gameObject.SetActive(true);
            activeNotes.Add(note);
        }

        private NoteBase GetPooledNote(NoteType type)
        {
            foreach (var note in notePool)
            {
                if (!note.gameObject.activeInHierarchy)
                {
                    if (type == NoteType.Hold && note is HoldNote) return note;
                    if (type == NoteType.Flick && note is FlickNote) return note;
                }
            }
            return null;
        }

        private void UpdateActiveNotes()
        {
            foreach (var note in activeNotes)
            {
                if (note.gameObject.activeInHierarchy)
                {
                    // Update is called automatically by MonoBehaviour
                }
            }
        }

        private void CleanupNotes()
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                if (!activeNotes[i].gameObject.activeInHierarchy)
                {
                    activeNotes.RemoveAt(i);
                }
            }
        }

        public NoteBase FindNearestNote(Vector2 screenPos, NoteType? typeFilter = null)
        {
            NoteBase nearest = null;
            float nearestDist = float.MaxValue;

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
            worldPos.z = 0;

            foreach (var note in activeNotes)
            {
                if (!note.gameObject.activeInHierarchy) continue;
                if (typeFilter.HasValue && note.Type != typeFilter.Value) continue;

                Vector3 notePos = note.transform.position;
                notePos.z = 0;
                float dist = Vector3.Distance(worldPos, notePos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = note;
                }
            }

            return nearest;
        }

        public FlickNote FindNearestFlickNote(Vector2 screenPos)
        {
            return FindNearestNote(screenPos, NoteType.Flick) as FlickNote;
        }

        public HoldNote FindNearestHoldNote(Vector2 screenPos)
        {
            return FindNearestNote(screenPos, NoteType.Hold) as HoldNote;
        }
    }
}
