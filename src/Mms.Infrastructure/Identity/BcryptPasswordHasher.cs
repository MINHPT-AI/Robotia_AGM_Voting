using Microsoft.AspNetCore.Identity;

namespace Mms.Infrastructure.Identity;

public class BcryptPasswordHasher : IPasswordHasher<ApplicationUser>
{
    public string HashPassword(ApplicationUser user, string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user, string hashedPassword, string providedPassword)
    {
        var valid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        return valid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
}
