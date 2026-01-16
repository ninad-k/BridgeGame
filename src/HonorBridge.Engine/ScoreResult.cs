namespace HonorBridge.Engine;

public class ScoreResult
{
    public int Points { get; }
    public bool Made { get; }
    public string Description { get; }

    public ScoreResult(int points, bool made, string description)
    {
        Points = points;
        Made = made;
        Description = description;
    }

    public override string ToString() => $"{Points} ({Description})";
}
