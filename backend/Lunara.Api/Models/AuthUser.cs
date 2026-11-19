namespace Lunara.Api.Models;

public class AuthUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int? ClientId { get; set; }
    public int? TechnicianId { get; set; }
    public bool IsActive { get; set; } = true;
    public string SessionKey { get; set; } = Guid.NewGuid().ToString("N");
}
