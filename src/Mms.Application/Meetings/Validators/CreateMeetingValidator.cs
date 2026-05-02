using FluentValidation;
using Mms.Application.Meetings.Commands;

namespace Mms.Application.Meetings.Validators;

public class CreateMeetingValidator : AbstractValidator<CreateMeetingCommand>
{
    public CreateMeetingValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MeetingDate).GreaterThan(DateTime.UtcNow.AddHours(-1))
            .WithMessage("Ngày họp không hợp lệ");
        RuleFor(x => x.Location).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TotalVotingShares).GreaterThan(0)
            .WithMessage("Tổng cổ phần biểu quyết phải lớn hơn 0");
        RuleFor(x => x.CompanyId).NotEmpty()
            .WithMessage("Chưa cấu hình thông tin công ty");
    }
}
