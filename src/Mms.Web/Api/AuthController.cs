using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mms.Infrastructure.Identity;

namespace Mms.Web.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwt;

    public AuthController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwt)
    {
        _signIn = signIn;
        _userManager = userManager;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _signIn.PasswordSignInAsync(
            req.Username, req.Password,
            isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return result.IsLockedOut
                ? StatusCode(429, new { error = "Account locked. Try again later." })
                : Unauthorized(new { error = "Invalid credentials" });
        }

        var user = (await _userManager.FindByNameAsync(req.Username))!;
        var roles = await _userManager.GetRolesAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var accessToken = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // set true in production with HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(8),
        });

        return Ok(new
        {
            accessToken,
            user = new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.MustChangePassword,
                roles,
            },
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword()
        => Ok(new { message = "Password reset email sent (stub — SMTP not configured)" });

    [HttpPost("reset-password")]
    public IActionResult ResetPassword()
        => Ok(new { message = "Password reset complete (stub)" });
}

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
