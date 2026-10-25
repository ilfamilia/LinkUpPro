namespace LinkUpPro.Core.Domain.Entities;

/// <summary>Represents a zero-based cell on the twelve-by-twelve board.</summary>
public readonly record struct BoardCoordinate(int Row, int Column)
{
    public const int BoardSize = 12;

    /// <summary>Checks whether this coordinate belongs to the playable board.</summary>
    public bool IsInside() => Row >= 0 && Row < BoardSize && Column >= 0 && Column < BoardSize;
}

