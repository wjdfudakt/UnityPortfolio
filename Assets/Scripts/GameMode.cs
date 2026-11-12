public enum GameMode
{
    Round,
    TimeAttack,
    ReactionTime,
    Tracking
}

public static class GameSettings
{
    public static GameMode SelectedMode = GameMode.Round;
}
