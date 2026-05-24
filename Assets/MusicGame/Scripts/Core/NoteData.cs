using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.Core
{
    [Serializable]
    public class NotePathPoint
    {
        public float timeOffset;
        public float x;
        public float y;
        public float z;

        public Vector3 Position => new Vector3(x, y, z);
    }

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
        
        public bool hasTailFlick = true;
public FlickDirection flickDirection;
        public float approachTime = 2f;
        public bool useCustomEndPoint;
        public float endX;
        public float endY;
        public float endZ;
        public List<NotePathPoint> attentionPoints = new List<NotePathPoint>();

        public Vector3 SpawnPosition => new Vector3(x, y, z);
        public Vector3 EndPosition => useCustomEndPoint ? new Vector3(endX, endY, endZ) : SpawnPosition;
        
        public bool HasTailFlick => noteType == NoteType.Hold && hasTailFlick;
public float EndTime => time + duration;
    }
}
