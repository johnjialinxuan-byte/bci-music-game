#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MusicGame.Core;

namespace MusicGame.Editor
{
    public class ChartEditorWindow : EditorWindow
    {
        private ChartData currentChart;
        private Vector2 scrollPos;
        private int selectedNoteIndex = -1;
        private float currentTime = 0f;
        private bool isPlaying = false;

        [MenuItem("MusicGame/Chart Editor")]
        public static void ShowWindow()
        {
            GetWindow<ChartEditorWindow>("Chart Editor");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Chart Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            currentChart = EditorGUILayout.ObjectField("Chart Data", currentChart, typeof(ChartData), false) as ChartData;

            if (currentChart == null)
            {
                EditorGUILayout.HelpBox("Assign a ChartData asset to begin editing.", MessageType.Info);
                if (GUILayout.Button("Create New Chart"))
                {
                    CreateNewChart();
                }
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Hold Note"))
            {
                AddNote(NoteType.Hold);
            }
            if (GUILayout.Button("Add Flick Note"))
            {
                AddNote(NoteType.Flick);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Notes: {currentChart.notes.Count}", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < currentChart.notes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var note = currentChart.notes[i];
                EditorGUILayout.LabelField($"{i}: {note.noteType} @ {note.time:F2}s", GUILayout.Width(150));
                note.time = EditorGUILayout.FloatField(note.time, GUILayout.Width(60));
                EditorGUILayout.LabelField("Pos:", GUILayout.Width(30));
                note.x = EditorGUILayout.FloatField(note.x, GUILayout.Width(40));
                note.y = EditorGUILayout.FloatField(note.y, GUILayout.Width(40));
                note.z = EditorGUILayout.FloatField(note.z, GUILayout.Width(40));
                if (note.noteType == NoteType.Hold)
                {
                    note.duration = EditorGUILayout.FloatField(note.duration, GUILayout.Width(50));
                }
                if (note.noteType == NoteType.Flick)
                {
                    note.flickDirection = (FlickDirection)EditorGUILayout.EnumPopup(note.flickDirection, GUILayout.Width(80));
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    currentChart.notes.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(currentChart);
            }
        }

        private void CreateNewChart()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Chart", "NewChart", "asset", "Create Chart Data");
            if (string.IsNullOrEmpty(path)) return;

            ChartData chart = CreateInstance<ChartData>();
            chart.difficulty = Difficulty.Normal;
            chart.level = 5;
            AssetDatabase.CreateAsset(chart, path);
            AssetDatabase.SaveAssets();
            currentChart = chart;
        }

        private void AddNote(NoteType type)
        {
            NoteData note = new NoteData
            {
                time = currentTime,
                x = Random.Range(-3f, 3f),
                y = Random.Range(-2f, 2f),
                z = 10f,
                noteType = type,
                duration = type == NoteType.Hold ? 1f : 0f,
                flickDirection = type == NoteType.Flick ? FlickDirection.Up : FlickDirection.Left,
                approachTime = 2f
            };
            currentChart.notes.Add(note);
            EditorUtility.SetDirty(currentChart);
        }
    }
}
#endif
