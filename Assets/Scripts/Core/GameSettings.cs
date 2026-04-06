public enum GameMode { Local, AI, Online }
public enum AIDifficulty { Easy, Normal, Hard }

public static class GameSettings
{
    public static GameMode Mode = GameMode.Local;
    public static AIDifficulty Difficulty = AIDifficulty.Normal;
}
