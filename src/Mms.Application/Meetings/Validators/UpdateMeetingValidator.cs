using FluentValidation;
using Mms.Application.Meetings.Commands;

namespace Mms.Application.Meetings.Validators;

public class UpdateMeetingValidator : AbstractValidator<UpdateMeetingCommand>
{
    public UpdateMeetingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TotalVotingShares).GreaterThan(0);
    }
}
