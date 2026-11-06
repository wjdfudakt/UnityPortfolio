public enum GameMode
{
    Round,
    TimeAttack,
    ReactionTime
}

public static class GameSettings
{
    public static GameMode SelectedMode = GameMode.Round;
}