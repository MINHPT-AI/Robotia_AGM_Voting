using MediatR;
using Mms.Application.InvitationLetters.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.InvitationLetters.Commands;

// ── Generate invitation letters from shareholder data ──
public record GenerateLettersCommand(Guid MeetingId) : IRequest<int>;

// ── Export merged DOCX ──
public record ExportLettersDocxCommand(
    Guid MeetingId,
    CodeMarkType CodeMarkType) : IRequest<(byte[] FileBytes, string FileName)>;

// ── Export merged PDF (via LibreOffice) ──
public record ExportLettersPdfCommand(
    Guid MeetingId,
    CodeMarkType CodeMarkType) : IRequest<(byte[] FileBytes, string FileName)>;

// ── Import CPN postal report ──
public record ImportCpnReportCommand(
    Guid MeetingId,
    Stream FileStream,
    CpnColumnMapping Mapping,
    bool DryRun) : IRequest<CpnImportResult>;

// ── Update single letter status ──
public record UpdateLetterStatusCommand(
    Guid LetterId,
    InvitationStatus Status,
    string? FailureReason) : IRequest<bool>;
