namespace LinkUpPro.Core.Domain.Entities;

/// <summary>Records an immutable attack result for one player and board cell.</summary>
public sealed class Attack
{
    public int GameId { get; set; }
    public string UserId { get; set; } = "";
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsHit { get; set; }
    public DateTime CreatedAt { get; set; }
}

