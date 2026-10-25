namespace LinkUpPro.Core.Domain.Enums;

/// <summary>Describes the preparation, combat, and completed phases of a game.</summary>
public enum BattleshipStatus { Setup, Active, Finished }

/// <summary>Defines the four directions available when placing a ship.</summary>
public enum ShipDirection { Up, Down, Left, Right }

/// <summary>Records why a completed game ended.</summary>
public enum GameEndReason { FleetDestroyed, Surrender, Abandonment }

