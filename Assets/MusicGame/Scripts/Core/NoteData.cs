using System;
using UnityEngine;

namespace MusicGame.Core
{
    [Serializable]
    public class NoteData
    {
        public float time;
        public float x;
        public float y;
        public float z;
        public NoteType noteType;
        public float duration;
        public int threshold;
        public FlickDirection flickDirection;
        public float approachTime = 2f;

        public Vector3 SpawnPosition => new Vector3(x, y, z);
        public float EndTime => time + duration;
    }
}
