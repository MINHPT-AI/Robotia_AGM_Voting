using Microsoft.AspNetCore.Identity;

namespace Mms.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
