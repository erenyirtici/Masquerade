using System.Collections.Generic; // Listeler için gerekli

[System.Serializable]
public class ScoreEntry
{
    public string name;
    public float score;
}

[System.Serializable]
public class LeaderboardData
{
    public List<ScoreEntry> entryList;
}