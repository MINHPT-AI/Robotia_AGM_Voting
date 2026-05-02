using Microsoft.AspNetCore.Identity;

namespace Mms.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}
