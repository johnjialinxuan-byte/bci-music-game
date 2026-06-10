namespace MusicGame.Core
{
    public enum GameScene
    {
        MainMenu,
        SongSelect,
        Settings,
        About,
        Gameplay,
        Result
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum NoteType
    {
        Hold,
        Flick
    }

    public enum JudgmentType
    {
        Perfect,
        Good,
        Miss
    }

    /// <summary>
    /// Scoring category of a single judgment. Weights: Click > Flick >> Round.
    /// </summary>
    public enum NoteCategory
    {
        Click,
        Flick,
        Round
    }

    public enum FlickDirection
    {
        Left,
        Right,
        Up,
        Down
    }
}
