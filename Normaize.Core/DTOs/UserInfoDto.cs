namespace Normaize.Core.DTOs;

/// <summary>
/// DTO containing user information extracted from Auth0 JWT claims
/// </summary>
public class ProfileInfoDto
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
}