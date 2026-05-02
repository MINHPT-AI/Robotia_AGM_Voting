using FluentValidation;
using Mms.Application.Companies.Commands;

namespace Mms.Application.Companies.Validators;

public class UpsertCompanyValidator : AbstractValidator<UpsertCompanyCommand>
{
    public UpsertCompanyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TaxCode)
            .NotEmpty()
            .Matches(@"^[0-9]{10}([0-9]{3})?$")
            .WithMessage("Mã số thuế phải là 10 hoặc 13 chữ số");
        RuleFor(x => x.LegalRepName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.LegalRepTitle).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CharterCapital).GreaterThan(0)
            .WithMessage("Vốn điều lệ phải lớn hơn 0");
        RuleFor(x => x.TotalSharesIssued).GreaterThan(0);
        RuleFor(x => x.TotalVotingShares)
            .GreaterThan(0)
            .LessThanOrEqualTo(x => x.TotalSharesIssued)
            .WithMessage("Cổ phần có quyền biểu quyết không được vượt quá tổng cổ phần phát hành");
    }
}
