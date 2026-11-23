public enum GameMode
{
    Round,
    TimeAttack,
    ReactionTime,
    Tracking,
    Memory
}

public static class GameSettings
{
    public static GameMode SelectedMode = GameMode.Round;
}
