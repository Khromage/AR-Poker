// MatchStartEventData.cs
using System;

[Serializable]
public class GameStartEventData : EventArgs
{
    public string GameMode { get; private set; }
    public string Difficulty { get; private set; }
    public int NPCCount { get; private set; }

    public GameStartEventData(string gameMode, string difficulty, int npcCount)
    {
        GameMode = gameMode;
        Difficulty = difficulty;
        NPCCount = npcCount;
    }
}
