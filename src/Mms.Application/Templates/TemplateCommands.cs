using MediatR;
using Mms.Domain.Enums;

namespace Mms.Application.Templates;

// ── Upload a new DOCX template (step 1-3 of the workflow) ──
public record UploadTemplateCommand(
    Stream FileStream, string OriginalFileName,
    string Name, TemplateType TemplateType,
    string Language,
    IList<string> SelectedTokenCodes,
    bool UseSignatureAndSeal,
    Guid? UploadedBy = null) : IRequest<TemplateUploadResultDto>;

// ── Save HTML content from WYSIWYG editor (draft save) ──
public record SaveTemplateContentCommand(Guid Id, string HtmlContent) : IRequest;

// ── Update template name + language (draft only) ──
public record UpdateTemplateNameCommand(Guid Id, string Name, string Language) : IRequest;

// ── Lock template as finalized ──
public record FinalizeTemplateCommand(
    Guid Id
) : IRequest;

public record UpdateTemplateMarginsCommand(
    Guid Id,
    float MarginTop,
    float MarginBottom,
    float MarginLeft,
    float MarginRight
) : IRequest;

// ── Clone template with new name ──
public record CloneTemplateCommand(Guid SourceId, string NewName, Guid? ClonedBy = null) : IRequest<Guid>;

// ── Delete draft template ──
public record DeleteTemplateCommand(Guid Id) : IRequest;
