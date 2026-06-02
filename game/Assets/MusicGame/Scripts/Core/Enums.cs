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

    public enum FlickDirection
    {
        Left,
        Right,
        Up,
        Down
    }
}
