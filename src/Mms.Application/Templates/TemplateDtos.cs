using Mms.Domain.Enums;

namespace Mms.Application.Templates;

public record TemplateListItemDto(
    Guid Id, string Name, TemplateType TemplateType,
    string Language, int Version, long? FileSize,
    bool IsFinalized, bool HasHtmlContent,
    bool UseSignatureAndSeal, DateTime? UploadedAt);

public record TemplateUploadResultDto(Guid Id);

public record TemplateDetailDto(
    Guid Id, string Name, TemplateType TemplateType,
    string Language, string? HtmlContent,
    string? SelectedTokens, bool UseSignatureAndSeal,
    bool IsFinalized, float MarginTop, float MarginBottom,
    float MarginLeft, float MarginRight, string? FilePath);
