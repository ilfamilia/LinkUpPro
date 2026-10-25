namespace LinkUpPro.Core.Application.Interfaces;

/// <summary>Provides a trusted authenticated user identifier instead of accepting it from forms.</summary>
public interface ICurrentUserService
{
    /// <summary>Returns the authenticated identifier or rejects an anonymous caller.</summary>
    string GetRequiredUserId();
}
