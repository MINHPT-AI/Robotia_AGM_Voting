using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mms.Infrastructure.Identity;

namespace Mms.Web.Api;

[ApiController]
public class AccountController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(SignInManager<ApplicationUser> signIn) => _signIn = signIn;

    [HttpGet("account/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return LocalRedirect("/login");
    }
}
