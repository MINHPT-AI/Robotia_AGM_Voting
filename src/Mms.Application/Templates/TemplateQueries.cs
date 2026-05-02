using MediatR;
using Mms.Domain.Enums;

namespace Mms.Application.Templates;

// ── Get list of templates ──
public record GetTemplatesQuery(
    TemplateType? FilterType = null,
    bool GlobalOnly = true) : IRequest<IList<TemplateListItemDto>>;

// ── Get full template detail for editor page ──
public record GetTemplateDetailQuery(Guid Id) : IRequest<TemplateDetailDto>;

// ── Get DOCX bytes for Mammoth.js conversion ──
public record GetTemplateDocxBytesQuery(Guid Id) : IRequest<byte[]>;

// ── Get all available tokens (from static registry) ──
public record GetAllTokensQuery() : IRequest<IList<TokenRegistry.TokenInfo>>;

public record PreviewTemplateHtmlQuery(
    string HtmlContent,
    Guid? MeetingId,
    float MarginTop = 2.0f,
    float MarginBottom = 2.0f,
    float MarginLeft = 3.0f,
    float MarginRight = 2.0f
) : IRequest<byte[]>;
