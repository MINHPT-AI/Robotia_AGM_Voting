using FluentValidation;

namespace Mms.Application.Templates;

public class UploadTemplateValidator : AbstractValidator<UploadTemplateCommand>
{
    public UploadTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Language)
            .Must(l => new[] { "VN", "EN", "DUAL" }.Contains(l))
            .WithMessage("Ngôn ngữ phải là VN, EN hoặc DUAL");
        RuleFor(x => x.OriginalFileName)
            .Must(f => f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Chỉ chấp nhận file .docx");
    }
}

public class UpdateTemplateNameValidator : AbstractValidator<UpdateTemplateNameCommand>
{
    public UpdateTemplateNameValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Language)
            .Must(l => new[] { "VN", "EN", "DUAL" }.Contains(l))
            .WithMessage("Ngôn ngữ phải là VN, EN hoặc DUAL");
    }
}

public class SaveTemplateContentValidator : AbstractValidator<SaveTemplateContentCommand>
{
    public SaveTemplateContentValidator()
    {
        RuleFor(x => x.HtmlContent).NotEmpty()
            .WithMessage("Nội dung văn bản không được để trống");
    }
}
