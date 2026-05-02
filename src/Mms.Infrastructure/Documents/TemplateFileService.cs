using Microsoft.AspNetCore.Hosting;
using Mms.Application.Interfaces;

namespace Mms.Infrastructure.Documents;

/// <summary>
/// Handles physical file storage for DOCX template files.
/// Files stored at wwwroot/uploads/templates/{guid}.docx
/// </summary>
public class TemplateFileService : ITemplateFileService
{
    private readonly IWebHostEnvironment _env;

    public TemplateFileService(IWebHostEnvironment env) => _env = env;

    public async Task<(string FilePath, long FileSize)> SaveAsync(Stream stream, CancellationToken ct = default)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "templates");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}.docx";
        var fullPath = Path.Combine(dir, fileName);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);
        var fileSize = fs.Length;
        return ($"uploads/templates/{fileName}", fileSize);
    }

    public async Task<byte[]> GetDocxBytesAsync(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, filePath);
        return await File.ReadAllBytesAsync(fullPath);
    }

    public void Delete(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, filePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
