using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mms.Web.Api;

[ApiController]
[Authorize(Roles = "admin")]
[IgnoreAntiforgeryToken]  // File upload from Blazor — no antiforgery token in multipart POST
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadsController> _logger;

    private static readonly HashSet<string> AllowedMimes =
        ["image/jpeg", "image/png", "image/svg+xml"];

    public UploadsController(IWebHostEnvironment env, ILogger<UploadsController> logger)
        => (_env, _logger) = (env, logger);

    [HttpPost("api/uploads/image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest("File trống");
        if (file.Length > 15 * 1024 * 1024) return BadRequest("File tối đa 15MB");
        if (!AllowedMimes.Contains(file.ContentType.ToLower()))
            return BadRequest("Chỉ chấp nhận PNG, JPEG, SVG");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        _logger.LogInformation("Uploaded image: {FileName}", fileName);
        return Ok(new { path = $"/uploads/{fileName}" });
    }
}
