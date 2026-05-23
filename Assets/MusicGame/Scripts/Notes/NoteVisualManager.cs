using UnityEngine;
using MusicGame.Core;

namespace MusicGame.Notes
{
    public static class NoteVisualManager
    {
        private const string NotesPath = "Images/Notes/";
        
        /// <summary>
        /// Returns the sprite path based on note type and direction.
        /// Rules:
        /// - Hold notes use: {color}_click.svg for head
        /// - Flick notes use: {color}_round.svg for base, arrow uses direction color
        /// - Direction colors: Right=Cyan, Left=White, Up=Red, Down=Blue
        /// </summary>
        public static string GetNoteSpritePath(NoteData data)
        {
            string color = GetNoteColor(data);
            string shape = GetNoteShape(data);
            
            return NotesPath + $"{color}_{shape}";
        }

        public static string GetHoldSpritePath(NoteData data, string shape)
        {
            return GetDirectionalSpritePath(data.flickDirection, shape);
        }

        public static string GetDirectionalSpritePath(FlickDirection direction, string shape)
        {
            return NotesPath + $"{GetDirectionColor(direction)}_{shape}";
        }
        
        /// <summary>
        /// Gets the color for the note based on type and direction.
        /// </summary>
        public static string GetDirectionColor(FlickDirection direction)
        {
            return direction switch
            {
                FlickDirection.Right => "miku",
                FlickDirection.Left => "white",
                FlickDirection.Up => "red",
                FlickDirection.Down => "blue",
                _ => "white"
            };
        }

        private static string GetNoteColor(NoteData data)
        {
            return GetDirectionColor(data.flickDirection);
        }
        
        /// <summary>
        /// Gets the shape type for the note.
        /// </summary>
        private static string GetNoteShape(NoteData data)
        {
            if (data.noteType == NoteType.Hold)
            {
                // Hold head is a click point
                return "click";
            }
            
            if (data.noteType == NoteType.Flick)
            {
                // Directional flick is the slide endpoint of a note path.
                return "slide";
            }
            
            return "round";
        }
        
        /// <summary>
        /// Gets the tail/end sprite path for hold notes (slide shape).
        /// </summary>
        public static string GetHoldTailSpritePath(NoteData data)
        {
            string color = GetNoteColor(data);
            return NotesPath + $"{color}_slide";
        }
        
        /// <summary>
        /// Attempts to load a sprite from Resources.
        /// </summary>
        public static Sprite LoadNoteSprite(string path)
        {
            // Try loading as Sprite directly
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            
            // Fallback: load as Texture2D and create sprite
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            
            return null;
        }
    }
}
