namespace LinkUpPro.Core.Application.Dtos.Social;

/// <summary>Exposes only the public user data needed by social modules.</summary>
public sealed class UserSummaryDto
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string ProfilePicturePath { get; set; } = "";
    public bool IsActive { get; set; }
}
