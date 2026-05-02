using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Interfaces;
using Mms.Infrastructure.Persistence;

namespace Mms.Web.Api;

[ApiController]
public class TemplatesController : ControllerBase
{
    private readonly MmsDbContext _db;
    private readonly ITemplateFileService _fileService;

    public TemplatesController(MmsDbContext db, ITemplateFileService fileService)
    {
        _db = db;
        _fileService = fileService;
    }

    [HttpGet("api/templates/{id:guid}/download")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Download(Guid id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template?.FilePath is null) return NotFound();

        var bytes = await _fileService.GetDocxBytesAsync(template.FilePath);
        var fileName = $"{template.Name}_v{template.Version}.docx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }
}
