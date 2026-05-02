using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.InvitationLetters.Commands;
using Mms.Domain.Entities;

namespace Mms.Web.Api;

[ApiController]
[Authorize(Roles = "admin,operator")]
public class LettersController : ControllerBase
{
    private readonly IMediator _mediator;

    public LettersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/meetings/{meetingId:guid}/letters/export/docx")]
    public async Task<IActionResult> ExportDocx(
        Guid meetingId,
        [FromQuery] CodeMarkType codeMarkType = CodeMarkType.Barcode,
        CancellationToken ct = default)
    {
        var (fileBytes, fileName) = await _mediator.Send(
            new ExportLettersDocxCommand(meetingId, codeMarkType), ct);

        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    [HttpGet("api/meetings/{meetingId:guid}/letters/export/pdf")]
    public async Task<IActionResult> ExportPdf(
        Guid meetingId,
        [FromQuery] CodeMarkType codeMarkType = CodeMarkType.Barcode,
        CancellationToken ct = default)
    {
        var (fileBytes, fileName) = await _mediator.Send(
            new ExportLettersPdfCommand(meetingId, codeMarkType), ct);

        return File(fileBytes, "application/pdf", fileName);
    }
}
