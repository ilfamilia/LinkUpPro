namespace LinkUpPro.Core.Domain.Exceptions;

/// <summary>Reports a business rule violation without exposing infrastructure details.</summary>
public sealed class DomainRuleException(string message) : Exception(message);

